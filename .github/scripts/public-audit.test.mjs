// Tests for the public repository audit: node --test .github/scripts/*.test.mjs
import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { secretKinds, credentialFile, audit, report, mitGrant } from './public-audit.mjs';

test('secret-looking lines are named by kind; references to secret stores are not secrets', () => {
  assert.deepEqual(secretKinds(`token = "${'ghp_'}${'a'.repeat(36)}"`), ['GitHub token', 'assigned secret']);
  assert.deepEqual(secretKinds('-----BEGIN RSA PRIVATE KEY-----'), ['private key']);
  assert.deepEqual(secretKinds('password: "correct-horse-battery"'), ['assigned secret']);
  assert.deepEqual(secretKinds('VERCEL_TOKEN: ${{ secrets.VERCEL_TOKEN }}'), []);
  assert.deepEqual(secretKinds("const token = process.env.GITHUB_TOKEN;"), []);
  assert.deepEqual(secretKinds('colorToken: "--color-meadow"'), []);
});

test('credential and machine files are recognised by name', () => {
  for (const file of ['.env', 'site/.env.local', 'certs/signing.pfx', 'android/release.keystore', 'MicroEngine.csproj.user', 'site/.vercel/project.json']) {
    assert.ok(credentialFile.test(file), file);
  }
  for (const file of ['docs/env.md', 'src/Keyboard.cs', 'web/toolchain.env']) assert.ok(!credentialFile.test(file), file);
});

test('audit: history is read, MIT commits are split by implementation, values never reported', () => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'audit-'));
  const git = (...args) => execFileSync('git', args, { cwd: root, encoding: 'utf8' }).trim();
  const write = (file, text) => { fs.mkdirSync(path.dirname(path.join(root, file)), { recursive: true }); fs.writeFileSync(path.join(root, file), text); };
  const commit = message => { git('add', '-A'); git('-c', 'user.name=T', '-c', 'user.email=t@example.org', 'commit', '-q', '-m', message); };
  git('init', '-q');
  write('LICENSE', `MIT License\n\n${mitGrant} to any person`);
  write('assets/music/song.mp3', 'x');
  commit('old');
  git('tag', 'legacy');
  const secret = `password = "${'s3cr3t'.repeat(3)}"`;
  write('config.txt', secret);
  write('.github/scripts/public-audit.test.mjs', secret);
  commit('new with MIT still');
  const rebuild = git('rev-parse', 'HEAD');
  write('LICENSE', 'All rights reserved.');
  fs.rmSync(path.join(root, 'assets'), { recursive: true });
  fs.rmSync(path.join(root, 'config.txt'));
  commit('licence changed, file removed');

  const result = audit({ root, legacy: 'legacy', rebuild });
  assert.equal(result.commits, 3);
  assert.deepEqual(result.licence.mit, 2);
  assert.equal(result.licence.legacy, 1);
  assert.equal(result.licence.other, 1, 'a removed file or licence is still in history');
  assert.equal(result.licence.current, 1, 'the commit of the new implementation that still said MIT');
  assert.equal(result.licence.head, 'not MIT');
  assert.equal(result.secrets.length, 1);
  assert.equal(result.secrets[0].file, 'config.txt'); // the audit's own fixture file is not reported
  assert.deepEqual(result.media.folders, [{ dir: 'assets/music', files: 1, notice: false }]);
  const text = report(result);
  assert.ok(!text.includes('s3cr3t'), 'the report never shows the value');
  assert.ok(!text.includes('t@example.org'), 'personal addresses are counted, not shown');
  assert.match(text, /no: review/);
});

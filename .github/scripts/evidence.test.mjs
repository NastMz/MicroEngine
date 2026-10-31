// Tests for check evidence: node --test .github/scripts/*.test.mjs
import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { checkKey, isInput, findEvidence, artifactName } from './evidence.mjs';

const git = (root, ...args) => execFileSync('git', args, { cwd: root, encoding: 'utf8' }).trim();
function repo() {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'evidence-repo-'));
  git(root, 'init', '-q');
  git(root, 'config', 'user.email', 'ci@example.com');
  git(root, 'config', 'user.name', 'ci');
  for (const [file, text] of Object.entries({
    'src/Engine.cs': 'engine', 'games/Alpha/Game.cs': 'game', 'games/Alpha/README.md': '# a',
    'games/Alpha/Content/notes.md': 'shipped', 'games/Alpha/screenshots/01.png': 'png', 'docs/guide.md': '# g',
    'site/src/page.astro': 'page', 'web/Host/main.js': 'host', 'web/tests/desktop.mjs': 'captures',
    '.github/workflows/ci.yml': 'on: push', 'platforms/android/A.cs': 'android', 'odd/tasks/x.md': 'log',
  })) write(root, file, text);
  commit(root);
  return root;
}
const write = (root, file, text) => { fs.mkdirSync(path.dirname(path.join(root, file)), { recursive: true }); fs.writeFileSync(path.join(root, file), text); };
const commit = root => { git(root, 'add', '-A'); git(root, 'commit', '-qm', 'c'); return git(root, 'rev-parse', 'HEAD'); };
const keyAfter = (root, check, file, text, extra) => { write(root, file, text); commit(root); return checkKey(check, { root, extra }); };

test('inputs: each check reads its own part of the tree, conservatively', () => {
  assert.equal(isInput('dotnet', 'docs/guide.md'), false);
  assert.equal(isInput('dotnet', 'games/Alpha/README.md'), false);
  assert.equal(isInput('dotnet', 'games/Alpha/Content/notes.md'), true, 'files a game ships count, Markdown or not');
  assert.equal(isInput('dotnet', 'newfolder/tool.cs'), true, 'an unknown folder is an input');
  assert.equal(isInput('dotnet', '.github/workflows/ci.yml'), true, 'CI changes invalidate every evidence');
  assert.equal(isInput('dotnet', 'web/tests/desktop.mjs'), false);
  assert.equal(isInput('native', 'web/tests/desktop.mjs'), true);
  assert.equal(isInput('native', 'web/Host/main.js'), false);
  assert.equal(isInput('site', 'games/Alpha/Game.cs'), false, 'game code reaches the site through its package key');
  assert.equal(isInput('site', 'games/Alpha/screenshots/01.png'), true);
  assert.equal(isInput('site', 'src/Engine.cs'), false);
  assert.equal(isInput('site', 'docs/guide.md'), true);
  for (const check of ['dotnet', 'windows', 'native', 'site']) assert.equal(isInput(check, 'odd/tasks/x.md'), false);
});

test('keys: same content, same key; a change to an input changes it, other changes do not', () => {
  const root = repo();
  const dotnet = checkKey('dotnet', { root });
  assert.match(dotnet, /^[0-9a-f]{20}$/);
  assert.equal(checkKey('dotnet', { root }), dotnet);
  assert.notEqual(checkKey('windows', { root }), dotnet, 'each check has its own key');
  assert.equal(keyAfter(root, 'dotnet', 'docs/guide.md', '# new'), dotnet, 'documentation is not a .NET input');
  assert.equal(keyAfter(root, 'dotnet', 'odd/tasks/x.md', 'more'), dotnet);
  assert.notEqual(keyAfter(root, 'dotnet', 'games/Alpha/Game.cs', 'changed'), dotnet);
});

test('site keys include the demo packages the build uses', () => {
  const root = repo();
  const site = checkKey('site', { root, extra: 'Alpha=1111 suites=layout' });
  assert.notEqual(checkKey('site', { root, extra: 'Alpha=2222 suites=layout' }), site);
  assert.equal(keyAfter(root, 'site', 'games/Alpha/Game.cs', 'changed', 'Alpha=1111 suites=layout'), site);
  assert.notEqual(keyAfter(root, 'site', 'docs/guide.md', '# docs', 'Alpha=1111 suites=layout'), site);
});

test('evidence: accepted only when unexpired, produced by this repository and with the same workflows', async () => {
  const root = repo();
  const produced = git(root, 'rev-parse', 'HEAD');
  write(root, 'docs/guide.md', '# later docs');
  const later = commit(root);
  write(root, '.github/workflows/ci.yml', 'on: [push, pull_request]');
  const otherWorkflows = commit(root);
  git(root, 'checkout', '-q', later);
  const key = checkKey('dotnet', { root });
  const listing = artifacts => async url => {
    assert.ok(url.includes(`name=${artifactName('dotnet', key)}`));
    return { artifacts };
  };
  const artifact = (sha, expired = false, headRepository = 7) => ({ expired, workflow_run: { id: 42, head_sha: sha, repository_id: 7, head_repository_id: headRepository } });

  assert.deepEqual(await findEvidence('dotnet', key, { root, api: listing([artifact(produced)]) }), { run: 42, commit: produced });
  assert.equal(await findEvidence('dotnet', key, { root, api: listing([artifact(produced, true)]) }), null, 'expired');
  assert.equal(await findEvidence('dotnet', key, { root, api: listing([artifact(otherWorkflows)]) }), null, 'another workflow version');
  assert.equal(await findEvidence('dotnet', key, { root, api: listing([artifact('0'.repeat(40))]) }), null, 'unknown commit');
  assert.equal(await findEvidence('dotnet', key, { root, api: listing([artifact(produced, false, 99)]) }), null, 'a run of a fork pull request');
  assert.equal(await findEvidence('dotnet', key, { root, api: listing([{ expired: false, workflow_run: { id: 42, head_sha: produced } }]) }), null, 'a run whose repository is unknown');
  assert.equal(await findEvidence('dotnet', key, { root, api: listing([]) }), null);
  assert.equal(await findEvidence('dotnet', key, { root, api: async () => { throw new Error('HTTP 500'); } }), null, 'an API failure runs the check');
});

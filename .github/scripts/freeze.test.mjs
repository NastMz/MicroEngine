// Tests for the publication freeze and ci's deployment target: node --test .github/scripts/*.test.mjs
import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { deployTarget, freezeReason, freezeFile } from './freeze.mjs';
import { planChecks } from './changes.mjs';
import * as version from '../../sdk/version.mjs';

const script = fileURLToPath(new URL('./freeze.mjs', import.meta.url));
const site = { browser: 'layout' };

function tree(freeze) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'freeze-'));
  if (freeze !== undefined) {
    fs.mkdirSync(path.join(root, '.github'), { recursive: true });
    fs.writeFileSync(path.join(root, freezeFile), freeze);
  }
  return root;
}

test('not frozen: a push to main that changes the published site deploys production, otherwise nothing', () => {
  for (const browser of ['layout', 'full']) assert.equal(deployTarget({ event: 'push', fork: false, browser, frozen: false }), 'production');
  assert.equal(deployTarget({ event: 'push', fork: false, browser: 'none', frozen: false }), 'none', 'a change outside the published site deploys nothing');
});

test('frozen: a push to main never deploys production', () => {
  for (const browser of ['none', 'layout', 'full']) assert.equal(deployTarget({ event: 'push', fork: false, browser, frozen: true }), 'none');
});

test('pull requests: a preview when the site changes, nothing from a fork, frozen or not', () => {
  for (const frozen of [false, true]) {
    assert.equal(deployTarget({ event: 'pull_request', fork: false, ...site, frozen }), 'preview');
    // A game's code reaches the site only through a release package, which daily CI never builds: a preview would
    // show the released demos, so a change that does not touch the site gets none.
    assert.equal(deployTarget({ event: 'pull_request', fork: false, browser: 'none', build: '["MeadowRun"]', frozen }), 'none');
    assert.equal(deployTarget({ event: 'pull_request', fork: false, browser: 'none', frozen }), 'none');
    assert.equal(deployTarget({ event: 'pull_request', fork: true, browser: 'full', frozen }), 'none');
  }
});

test('the reason is the first line that is neither blank nor a comment; no file, no freeze', () => {
  assert.equal(freezeReason(tree()), null);
  assert.equal(freezeReason(tree('# lifted by deleting this file\r\n\r\n  Phase two in progress  \r\nmore\r\n')), 'Phase two in progress');
  assert.equal(freezeReason(tree('# only comments\n')), 'publication frozen', 'an empty file still freezes');
  assert.equal(freezeReason(tree('')), 'publication frozen');
});

test('sdk/version.mjs reads the same freeze file the same way (it cannot import this script)', () => {
  assert.equal(version.freezeFile, freezeFile);
  for (const text of [undefined, '', '# c\n\nwhy\n', 'a\nb\n']) assert.equal(version.freezeReason(tree(text)), freezeReason(tree(text)));
});

test('the freeze changes no check: only the site layout runs for it, so lifting it deploys main', () => {
  const none = { dotnet: false, windows: false, native: false, browser: 'none', workflows: false, sdk: false, macos: false, mobile: false, web: false, box2d: false };
  assert.deepEqual(planChecks([freezeFile]), { ...none, browser: 'layout' });
  // Every other path keeps its checks whether or not the freeze is in the same change.
  for (const file of ['src/MicroEngine/Core/GameLoop.cs', 'docs/guide/platforms.md', 'games/MeadowRun/MeadowRunGame.cs', '.github/copilot-instructions.md']) {
    const alone = planChecks([file]), withFreeze = planChecks([file, freezeFile]);
    assert.deepEqual({ ...withFreeze, browser: alone.browser }, alone, file);
  }
});

test('deploy command: prints the target from the freeze file next to the script and explains a withheld production', () => {
  const run = args => execFileSync(process.execPath, [script, 'deploy', ...args], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }).trim();
  const frozen = Boolean(freezeReason());
  assert.equal(run(['--event', 'push', '--fork', 'false', '--browser', 'layout']), frozen ? 'none' : 'production');
  assert.equal(run(['--event', 'pull_request', '--fork', 'false', '--browser', 'layout']), 'preview');
  assert.equal(run(['--event', 'pull_request', '--fork', 'true', '--browser', 'full']), 'none');
});

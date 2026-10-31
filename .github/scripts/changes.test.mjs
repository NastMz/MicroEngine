// Tests for the change planner: node --test .github/scripts
import test from 'node:test';
import assert from 'node:assert/strict';
import { planChecks, everything } from './changes.mjs';

const plan = files => planChecks(files);

test('documentation only: site built and its pages checked, no .NET, no demo play', () => {
  assert.deepEqual(plan(['docs/guide/platforms.md']), {
    dotnet: false, macos: false, native: false, browser: 'layout', mobile: false,
  });
});

test('a game change runs the .NET tests and native captures, not macOS or the browser demos', () => {
  const checks = plan(['games/MeadowRun/MeadowRunGame.cs']);
  assert.equal(checks.dotnet, true);
  assert.equal(checks.native, true);
  assert.equal(checks.macos, false);
  assert.equal(checks.browser, 'none'); // its demo key changes: the workflow plays rebuilt packages anyway
});

test('engine core runs every desktop check', () => {
  assert.deepEqual(plan(['src/MicroEngine/Core/GameLoop.cs']), {
    dotnet: true, macos: true, native: true, browser: 'none', mobile: false,
  });
});

test('the raylib backend also concerns the mobile hosts', () => {
  const checks = plan(['src/MicroEngine.Raylib/RaylibPlatform.cs']);
  assert.equal(checks.mobile, true);
  assert.equal(checks.macos, true);
});

test('tests of one game only run the .NET tests', () => {
  assert.deepEqual(plan(['tests/TrailDuel.Tests/BattleTests.cs']), {
    dotnet: true, macos: false, native: false, browser: 'none', mobile: false,
  });
});

test('native backend tests run on macOS and natively on Linux', () => {
  const checks = plan(['tests/MicroEngine.Raylib.Tests/KeyboardTests.cs']);
  assert.equal(checks.macos, true);
  assert.equal(checks.native, true);
});

test('site sources need the full browser checks', () => {
  assert.equal(plan(['site/src/components/Player.astro']).browser, 'full');
  assert.equal(plan(['web/Host/main.js']).browser, 'full');
});

test('desktop capture scripts and references run the native checks only', () => {
  assert.deepEqual(plan(['web/tests/reference/linux-x64.json']), {
    dotnet: false, macos: false, native: true, browser: 'none', mobile: false,
  });
});

test('mobile hosts never touch the publishing checks', () => {
  assert.deepEqual(plan(['platforms/android/MainActivity.cs']), {
    dotnet: false, macos: false, native: false, browser: 'none', mobile: true,
  });
});

test('the pipeline itself and build settings run everything', () => {
  assert.deepEqual(plan(['.github/workflows/pipeline.yml']), everything);
  assert.deepEqual(plan(['Directory.Build.props']), everything);
  assert.deepEqual(plan(['.github/scripts/changes.mjs']), everything);
});

test('an unknown path runs everything rather than skip a check', () => {
  assert.deepEqual(plan(['newfolder/thing.txt']), everything);
});

test('task records and experiments run nothing', () => {
  assert.deepEqual(plan(['odd/tasks/platform-ci.md', 'experiments/notes.txt']), {
    dotnet: false, macos: false, native: false, browser: 'none', mobile: false,
  });
});

test('checks from several files combine, the strongest browser level wins', () => {
  const checks = plan(['docs/manual/input.md', 'site/tests/browser.mjs', 'tests/MeadowRun.Tests/A.cs']);
  assert.equal(checks.browser, 'full');
  assert.equal(checks.dotnet, true);
  assert.equal(checks.native, false);
});

test('Markdown inside a game folder is published: its pages are checked too', () => {
  const checks = plan(['games/NeonRift/README.md']);
  assert.equal(checks.browser, 'layout');
  assert.equal(checks.dotnet, false);
});

test('no changed files runs nothing', () => {
  assert.deepEqual(plan([]), { dotnet: false, macos: false, native: false, browser: 'none', mobile: false });
});

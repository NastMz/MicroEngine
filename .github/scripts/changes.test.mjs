// Tests for the change planner: node --test .github/scripts/*.test.mjs
import test from 'node:test';
import assert from 'node:assert/strict';
import { planChecks, planScope, planWebGames, everything } from './changes.mjs';
import { readGraph } from './projects.mjs';

const plan = files => planChecks(files);
const none = { dotnet: false, windows: false, native: false, browser: 'none', workflows: false, sdk: false, macos: false, mobile: false, web: false };

test('documentation only: site built and its pages checked, no .NET, no demo play', () => {
  assert.deepEqual(plan(['docs/guide/platforms.md']), { ...none, browser: 'layout' });
});

test('a game change runs the Linux .NET tests, native captures and browser engine suites; Windows and macOS are not needed', () => {
  assert.deepEqual(plan(['games/MeadowRun/MeadowRunGame.cs']), { ...none, dotnet: true, native: true, web: true });
  // its demo key changes: the workflow builds and plays that package whatever this planner says
});

test("a game's screenshots are published on the site, not tested", () => {
  assert.deepEqual(plan(['games/NeonRift/screenshots/01-title.png']), { ...none, browser: 'layout' });
});

test('engine core runs the .NET tests on Linux and Windows and the native checks; macOS is advisory', () => {
  assert.deepEqual(plan(['src/MicroEngine/Core/GameLoop.cs']), { ...none, dotnet: true, windows: true, native: true, sdk: true, macos: true, web: true });
});

test('the raylib backend also recommends the mobile hosts, which never run automatically', () => {
  const checks = plan(['src/MicroEngine.Raylib/RaylibPlatform.cs']);
  assert.equal(checks.windows, true);
  assert.equal(checks.mobile, true);
  assert.equal(checks.macos, true);
  assert.equal(checks.web, true, 'the browser build runs on raylib');
});

test('the SDL backend and the desktop launcher check the desktops natively and recommend the mobile hosts', () => {
  for (const file of ['src/MicroEngine.Sdl/SdlPlatform.cs', 'src/MicroEngine.Desktop/DesktopLauncher.cs'])
    assert.deepEqual(plan([file]), { ...none, dotnet: true, windows: true, native: true, sdk: true, macos: true, mobile: true });
  assert.deepEqual(plan(['tests/MicroEngine.Desktop.Tests/PlatformEquivalenceTests.cs']), { ...none, dotnet: true, windows: true, native: true, macos: true });
});

test('tests of one game only run the Linux .NET tests', () => {
  assert.deepEqual(plan(['tests/TrailDuel.Tests/BattleTests.cs']), { ...none, dotnet: true });
  assert.deepEqual(plan(['samples/HelloEngine/Program.cs']), { ...none, dotnet: true });
});

test('engine and backend tests run on Windows too; backend tests natively on Linux', () => {
  assert.deepEqual(plan(['tests/MicroEngine.Tests/SceneTests.cs']), { ...none, dotnet: true, windows: true, macos: true });
  assert.deepEqual(plan(['tests/MicroEngine.Raylib.Tests/KeyboardTests.cs']), { ...none, dotnet: true, windows: true, native: true, macos: true });
});

test('site sources and the web host need the full browser checks', () => {
  assert.equal(plan(['site/src/components/Player.astro']).browser, 'full');
  assert.equal(plan(['src/MicroEngine.Cli/Hosts/web/wwwroot/main.js']).browser, 'full');
  assert.equal(plan(['web/README.md']).browser, 'layout');
});

test('desktop capture scripts and references run the native checks only', () => {
  assert.deepEqual(plan(['web/tests/reference/linux-x64.json']), { ...none, native: true });
});

test('the browser engine suites run when their scripts, the scenarios or the browser build change', () => {
  assert.deepEqual(plan(['web/tests/run.mjs']), { ...none, web: true });
  assert.deepEqual(plan(['web/tests/scenarios.json']), { ...none, native: true, web: true });
  assert.deepEqual(plan(['web/tests/desktop.mjs']), { ...none, native: true, web: true }, 'it compares browser frames with the desktop');
  assert.deepEqual(plan(['web/build.sh']), { ...none, browser: 'full', web: true });
  assert.deepEqual(plan(['web/serve.mjs']), { ...none, browser: 'full', sdk: true, web: true });
  assert.deepEqual(plan(['src/MicroEngine.Cli/Hosts/web/wwwroot/main.js']), { ...none, browser: 'full', sdk: true, web: true });
  assert.equal(plan(['src/MicroEngine.Sdl/SdlPlatform.cs']).web, false, 'SDL is not in the browser build');
  assert.equal(plan(['site/src/components/Player.astro']).web, false, 'the site plays the demos itself');
  assert.equal(plan(['tests/MeadowRun.Tests/A.cs']).web, false);
});

test('the browser engine suites play the changed games, or all of them when a shared input changed', () => {
  assert.deepEqual(planWebGames(['games/NeonRift/Stages/Stage1.cs', 'docs/guide/platforms.md']), ['NeonRift']);
  assert.deepEqual(planWebGames(['games/NeonRift/A.cs', 'games/MeadowRun/B.cs']), ['MeadowRun', 'NeonRift']);
  assert.equal(planWebGames(['games/NeonRift/A.cs', 'src/MicroEngine/Core/GameLoop.cs']), 'all');
  assert.equal(planWebGames(['web/tests/run.mjs']), 'all');
  assert.equal(planWebGames(['newfolder/thing.txt']), 'all', 'unknown paths stay conservative');
  assert.deepEqual(planWebGames(['docs/guide/platforms.md']), []);
});

test('mobile hosts are advisory: nothing runs automatically', () => {
  assert.deepEqual(plan(['platforms/android/MainActivity.cs']), { ...none, mobile: true });
  assert.deepEqual(plan(['.github/workflows/platforms.yml']), { ...none, workflows: true, mobile: true });
});

test('the daily workflows, their scripts and build settings run everything', () => {
  for (const file of ['.github/workflows/ci.yml', '.github/workflows/checks.yml', '.github/workflows/deploy-site.yml',
    '.github/scripts/changes.mjs', 'Directory.Build.props', 'global.json']) {
    assert.deepEqual(plan([file]), everything, file);
  }
});

test('manual workflows are linted, not exercised', () => {
  assert.deepEqual(plan(['.github/workflows/release.yml']), { ...none, workflows: true });
  assert.deepEqual(plan(['.github/workflows/validate.yml']), { ...none, workflows: true });
  assert.deepEqual(plan(['.github/copilot-instructions.md']), none);
});

test('every input of an SDK package or of the external project test runs the SDK check', () => {
  assert.deepEqual(plan(['templates/MicroEngine.Templates/content/Game/Program.cs']), { ...none, dotnet: true, sdk: true });
  assert.deepEqual(plan(['src/MicroEngine.Cli/Commands/PackageCommand.cs']), { ...none, dotnet: true, windows: true, sdk: true });
  assert.deepEqual(plan(['src/MicroEngine.Cli/Hosts/android/MainActivity.cs']), { ...none, sdk: true, mobile: true });
  assert.deepEqual(plan(['sdk/tests/external.mjs']), { ...none, sdk: true });
  assert.deepEqual(plan(['THIRD-PARTY-NOTICES.md']), { ...none, browser: 'layout', sdk: true });
  assert.deepEqual(plan(['web/toolchain.env']), { ...none, browser: 'full', sdk: true, web: true });
  assert.deepEqual(plan(['web/tests/package-lock.json']), { ...none, native: true, sdk: true, web: true });
  assert.equal(plan(['tests/TrailDuel.Tests/BattleTests.cs']).sdk, false, 'tests never reach a package');
  assert.equal(plan(['games/NeonRift/NeonRiftGame.cs']).sdk, false, 'nor do the games');
  assert.deepEqual(plan(['.github/workflows/sdk.yml']), everything, 'daily CI runs sdk.yml');
});

test('the documented version and its samples check run the .NET tests, not the SDK check', () => {
  for (const file of ['sdk/version.json', 'sdk/version.mjs', 'sdk/tests/docs.mjs', 'sdk/tests/version.test.mjs'])
    assert.deepEqual(plan([file]), { ...none, dotnet: true }, file);
  assert.deepEqual(planScope(['sdk/version.json'], readGraph()), { tests: 'all', games: 'all' }, 'outside every project: the samples check is selected');
});

test('an unknown path runs everything rather than skip a check', () => {
  assert.deepEqual(plan(['newfolder/thing.txt']), everything);
});

test('task records and experiments run nothing', () => {
  assert.deepEqual(plan(['odd/tasks/platform-ci.md', 'experiments/notes.txt']), none);
});

test('checks from several files combine, the strongest browser level wins', () => {
  const checks = plan(['docs/manual/input.md', 'site/tests/browser.mjs', 'tests/MeadowRun.Tests/A.cs']);
  assert.equal(checks.browser, 'full');
  assert.equal(checks.dotnet, true);
  assert.equal(checks.windows, false);
  assert.equal(checks.native, false);
});

test('Markdown inside a game folder is published: its pages are checked too', () => {
  assert.deepEqual(plan(['games/NeonRift/README.md']), { ...none, browser: 'layout' });
});

test('no changed files runs nothing', () => {
  assert.deepEqual(plan([]), none);
});

test('scope: the project graph narrows the .NET tests and native captures to what the change can affect', () => {
  const graph = readGraph();
  const scope = files => planScope(files, graph);
  assert.deepEqual(scope(['games/NeonRift/Stages/Stage1.cs']), { tests: ['tests/NeonRift.Tests/NeonRift.Tests.csproj'], games: ['NeonRift'] });
  assert.deepEqual(scope(['games/NeonRift/Stages/Stage1.cs', 'docs/guide/platforms.md']),
    { tests: ['tests/NeonRift.Tests/NeonRift.Tests.csproj'], games: ['NeonRift'] }, 'documentation does not widen the scope');
  assert.deepEqual(scope(['src/MicroEngine/Core/GameRunner.cs']), { tests: 'all', games: 'all' });
  assert.deepEqual(scope(['web/tests/scenarios.json']), { tests: 'all', games: 'all' }, 'a file outside every project');
  assert.deepEqual(scope(['Directory.Build.props']), { tests: 'all', games: 'all' });
  assert.deepEqual(scope(['newfolder/thing.txt']), { tests: 'all', games: 'all' }, 'unknown paths stay conservative');
  assert.deepEqual(scope(['docs/guide/platforms.md']), { tests: [], games: [] });
});

test('licence files are published and packaged, not tested: their pages are checked, demo keys rebuild packages', () => {
  assert.deepEqual(plan(['LICENSE']), { ...none, browser: 'layout', sdk: true });
  assert.deepEqual(plan(['games/LICENSE']), { ...none, browser: 'layout' });
});

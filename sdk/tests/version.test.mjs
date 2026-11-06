// Tests for the SDK version tool: node --test sdk/tests/version.test.mjs (daily CI runs them in Plan)
import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { check, release, pendingChanges, findReferences, activeFiles, documentedVersion, preparedVersion, publicState, isPublic, waitPublic, packageIds, releasePlan } from '../version.mjs';
import { reportOnlyVersion, publishedBuildRequired } from './docs.mjs';

function fixture(files = {}) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'sdk-version-'));
  const all = {
    'Directory.Build.props': '<Project><PropertyGroup>\n    <MicroEngineVersion>0.4.0-alpha.2</MicroEngineVersion>\n  </PropertyGroup></Project>\n',
    'sdk/version.json': '{\n  "documented": "0.4.0-alpha.2"\n}\n',
    'docs/guide/release-notes.md': '# Release notes\n\n## 0.4.0-alpha.2\n\nFixes. Packages at 0.4.0-alpha.2.\n\n## 0.4.0-alpha.1\n\nFirst. `Version="0.4.0-alpha.1"`\n',
    'docs/migrations.md': '## 0.4.0-alpha.1\n\n<PackageReference Include="MicroEngine.Desktop" Version="0.4.0-alpha.1" />\n',
    'docs/guide/getting-microengine.md': 'The current version is **0.4.0-alpha.2**, an Alpha.\n\n```bash\ndotnet new install MicroEngine.Templates::0.4.0-alpha.2\ndotnet tool install -g MicroEngine.Cli --version 0.4.0-alpha.2\n```\n\n`microengine version` prints `0.4.0-alpha.2`.\n',
    'docs/guide/status.md': '**MicroEngine SDK 0.4.0-alpha.2.** A working engine.\n',
    'docs/manual/audio.md': 'Intro.\n\n<!-- sdk-version: history -->\nIn 0.4.0-alpha.1 it rises; it starts at full volume from\n0.4.0-alpha.2. One track.\n\nThe current version is **0.4.0-alpha.2** here too.\n',
    'samples/Hello/Hello.csproj': '<Project>\n  <ItemGroup>\n    <PackageReference Include="MicroEngine.Desktop" Version="0.4.0-alpha.2" />\n  </ItemGroup>\n</Project>\n',
    'samples/Hello/bin/Release/Hello.csproj': 'build output: Version="0.0.1"',
    'docs/decisions/0001.md': 'The first SDK is 0.4.0-alpha.1. Version="0.4.0-alpha.1"\n',
    'site/src/roadmap.ts': "'SDK 0.4.0-alpha.1 on nuget.org'\n",
    ...files,
  };
  for (const [file, text] of Object.entries(all)) {
    fs.mkdirSync(path.dirname(path.join(root, file)), { recursive: true });
    fs.writeFileSync(path.join(root, file), text);
  }
  return root;
}
const read = (root, file) => fs.readFileSync(path.join(root, file), 'utf8');

test('versions: prepared from Directory.Build.props, documented from sdk/version.json', () => {
  const root = fixture({ 'Directory.Build.props': '<MicroEngineVersion>0.5.0-alpha.1</MicroEngineVersion>' });
  assert.equal(preparedVersion(root), '0.5.0-alpha.1');
  assert.equal(documentedVersion(root), '0.4.0-alpha.2');
  assert.throws(() => documentedVersion(fixture({ 'sdk/version.json': '{"documented": "next"}' })), /not a version/);
});

test('active files: documentation, samples and entry points; never history, generated or build output', () => {
  const files = activeFiles(fixture());
  assert.ok(files.includes('docs/guide/getting-microengine.md'));
  assert.ok(files.includes('samples/Hello/Hello.csproj'));
  for (const file of ['docs/guide/release-notes.md', 'docs/migrations.md', 'docs/decisions/0001.md', 'site/src/roadmap.ts', 'samples/Hello/bin/Release/Hello.csproj']) {
    assert.ok(!files.includes(file), `${file} is not active`);
  }
});

test('references: each pattern captures the version alone, never a trailing full stop', () => {
  const refs = findReferences('**MicroEngine SDK 0.4.0-alpha.2.** and `Version="1.0.0"` Include="MicroEngine.Desktop" Version="1.2.3"');
  assert.deepEqual(refs.map(r => [r.name, r.version]), [['SDK title', '0.4.0-alpha.2'], ['package reference', '1.2.3']]);
  const tool = findReferences("the tool's version (`0.4.0-alpha.2`). Engine       MicroEngine 0.4.0-alpha.2, .NET 10.0.14\nReplace `0.4.0-alpha.2` with");
  assert.deepEqual(tool.map(r => r.name), ['tool output', 'tool output', 'update instruction']);
});

test('check: a consistent tree passes', () => {
  assert.deepEqual(check(fixture()), []);
});

test('check: a stale active reference fails with its file and line', () => {
  const root = fixture({ 'docs/guide/status.md': 'Intro.\n**MicroEngine SDK 0.4.0-alpha.1.** A working engine.\n' });
  assert.deepEqual(check(root), ['docs/guide/status.md:2: SDK title names 0.4.0-alpha.1, the documented version is 0.4.0-alpha.2 (node sdk/version.mjs release 0.4.0-alpha.2).']);
});

test('check: an unclassified mention of a known version fails; history and unknown versions do not', () => {
  const root = fixture({ 'docs/manual/text.md': 'No font API in 0.4.0-alpha.2. Runs on .NET 10.0.14 and Node 22.23.4; not 0.4.0-alpha.20.\n' });
  assert.deepEqual(check(root), ['docs/manual/text.md:1: 0.4.0-alpha.2 is neither an active reference (sdk/version.mjs, references) nor marked "<!-- sdk-version: history -->".']);
});

test('check: a marker quoted in prose is not a marker', () => {
  const root = fixture({ 'docs/manual/text.md': 'Mark it with `<!-- sdk-version: history -->` above the paragraph. Fonts in 0.4.0-alpha.2.\n' });
  assert.equal(check(root).length, 1);
});

test('check: the documented version needs a release-notes section', () => {
  const root = fixture({ 'sdk/version.json': '{ "documented": "0.4.0-alpha.3" }', 'Directory.Build.props': '<MicroEngineVersion>0.4.0-alpha.3</MicroEngineVersion>' });
  assert.ok(check(root).includes('docs/guide/release-notes.md has no "## 0.4.0-alpha.3" section for the documented version.'));
});

// Release notes and migration notes with SDK changes merged since 0.4.0-alpha.2, written as published.
const notes = '# Release notes\n\n## Next release\n\n- Fonts ([migrating](../migrations.md#next-release)).\n\n## 0.4.0-alpha.2\n\nFixes. Packages at 0.4.0-alpha.2.\n\n## 0.4.0-alpha.1\n\nFirst. `Version="0.4.0-alpha.1"`\n';
const migrations = '# Migration notes\n\n## Next release\n\n`Font` is `IFont`.\n\n## 0.4.0-alpha.1\n\n<PackageReference Include="MicroEngine.Desktop" Version="0.4.0-alpha.1" />\n';
const pendingTree = files => fixture({ 'docs/guide/release-notes.md': notes, 'docs/migrations.md': migrations, ...files });

test('pending changes: a "## Next release" section with content in the release notes', () => {
  assert.equal(pendingChanges(fixture()), false);
  assert.equal(pendingChanges(pendingTree()), true);
  assert.deepEqual(check(pendingTree()), [], 'pending changes are a consistent state');
});

test('check: an empty "## Next release" heading fails, and so do migration notes without release notes', () => {
  const empty = pendingTree({ 'docs/guide/release-notes.md': notes.replace('- Fonts ([migrating](../migrations.md#next-release)).\n\n', '') });
  assert.equal(pendingChanges(empty), false);
  assert.deepEqual(check(empty), [
    'docs/guide/release-notes.md: the "## Next release" section is empty; remove the heading until something is written under it.',
    'docs/migrations.md has a "## Next release" section but docs/guide/release-notes.md has none: list the change in the release notes too.',
  ]);
});

test('release: renames each "## Next release" to the version, relinks it, and rewrites every active reference; history stays', () => {
  const root = pendingTree({ 'docs/manual/text.md': 'See [the release notes](../guide/release-notes.md#next-release) and [migrating](../migrations.md#next-release).\n' });
  const before = Object.fromEntries(['docs/decisions/0001.md', 'site/src/roadmap.ts'].map(f => [f, read(root, f)]));
  const changed = release('0.5.0-alpha.1', root);
  assert.deepEqual(changed.sort(), ['Directory.Build.props', 'docs/guide/getting-microengine.md', 'docs/guide/release-notes.md', 'docs/guide/status.md',
    'docs/manual/audio.md', 'docs/manual/text.md', 'docs/migrations.md', 'samples/Hello/Hello.csproj', 'sdk/version.json']);
  assert.equal(preparedVersion(root), '0.5.0-alpha.1');
  assert.equal(documentedVersion(root), '0.5.0-alpha.1');
  assert.equal(read(root, 'docs/guide/release-notes.md'), notes.replace('## Next release', '## 0.5.0-alpha.1').replace('#next-release', '#050-alpha1'));
  assert.equal(read(root, 'docs/migrations.md'), migrations.replace('## Next release', '## 0.5.0-alpha.1'));
  assert.equal(read(root, 'docs/manual/text.md'), 'See [the release notes](../guide/release-notes.md#050-alpha1) and [migrating](../migrations.md#050-alpha1).\n');
  assert.match(read(root, 'docs/guide/getting-microengine.md'), /\*\*0\.5\.0-alpha\.1\*\*[\s\S]*Templates::0\.5\.0-alpha\.1[\s\S]*--version 0\.5\.0-alpha\.1[\s\S]*prints `0\.5\.0-alpha\.1`/);
  const audio = read(root, 'docs/manual/audio.md');
  assert.match(audio, /In 0\.4\.0-alpha\.1 it rises; it starts at full volume from\n0\.4\.0-alpha\.2\. One track\./, 'the history paragraph is untouched');
  assert.match(audio, /The current version is \*\*0\.5\.0-alpha\.1\*\* here too/, 'the paragraph after it is active again');
  for (const [file, text] of Object.entries(before)) assert.equal(read(root, file), text, `${file} unchanged`);
  assert.equal(pendingChanges(root), false);
  assert.deepEqual(check(root), []);
  assert.deepEqual(release('0.5.0-alpha.1', root), [], 'a second run changes nothing');
});

test('release: migration notes are optional; only the release notes must say what ships', () => {
  const root = pendingTree({ 'docs/migrations.md': '# Migration notes\n\n## 0.4.0-alpha.1\n\nOld.\n' });
  release('0.4.0-alpha.3', root);
  assert.equal(read(root, 'docs/migrations.md'), '# Migration notes\n\n## 0.4.0-alpha.1\n\nOld.\n');
  assert.deepEqual(check(root), []);
});

test('release: refuses a version with nothing to ship, or one already released', () => {
  const root = fixture();
  const before = read(root, 'Directory.Build.props');
  assert.throws(() => release('0.4.0-alpha.3', root), /Nothing to release: docs\/guide\/release-notes\.md has no "## Next release" section with content/);
  assert.equal(read(root, 'Directory.Build.props'), before, 'nothing written');
  assert.throws(() => release('0.4.0-alpha.2', pendingTree()), /already has a "## 0\.4\.0-alpha\.2" section/);
  assert.deepEqual(release('0.4.0-alpha.2', root), [], 'the documented version again is a no-op');
});

test('release: a history paragraph ends at its blank line in a CRLF checkout too', () => {
  const root = pendingTree({ 'docs/manual/audio.md': 'Intro.\r\n\r\n<!-- sdk-version: history -->\r\nFrom 0.4.0-alpha.2 it starts.\r\n\r\nThe current version is **0.4.0-alpha.2** here.\r\n' });
  release('0.4.0-alpha.3', root);
  assert.equal(read(root, 'docs/manual/audio.md'), 'Intro.\r\n\r\n<!-- sdk-version: history -->\r\nFrom 0.4.0-alpha.2 it starts.\r\n\r\nThe current version is **0.4.0-alpha.3** here.\r\n');
});

test('release: refuses something that is not a version', () => {
  assert.throws(() => release('v0.4.0', fixture()), /not a version/);
  assert.throws(() => release(undefined, fixture()), /not a version/);
});

const index = versions => ({ versions });
test('public state: every package, case-insensitive; a missing package id is not public', async () => {
  const served = { microengine: ['0.4.0-alpha.1', '0.4.0-alpha.2'], 'microengine.templates': ['0.4.0-alpha.1'] };
  const fetchJson = async url => { const id = url.split('/').at(-2); return id in served ? index(served[id]) : (id.startsWith('microengine') ? index(['0.4.0-alpha.2']) : null); };
  const state = await publicState('0.4.0-ALPHA.2', { fetchJson });
  assert.deepEqual(Object.keys(state), packageIds);
  assert.equal(state['MicroEngine.Templates'], false);
  assert.equal(isPublic(state), false);
  assert.equal(isPublic(await publicState('0.4.0-alpha.2', { fetchJson: async () => index(['0.4.0-alpha.2']) })), true);
  assert.equal(isPublic(await publicState('0.4.0-alpha.2', { fetchJson: async () => null })), false, 'an unknown package (404) is not public');
  await assert.rejects(publicState('0.4.0-alpha.2', { fetchJson: async () => { throw new Error('HTTP 503'); } }), /503/);
});

test('wait-public: polls until all six are served, survives a failed request, gives up at the deadline', async () => {
  let calls = 0;
  const fetchJson = async () => { calls++; if (calls === 2) throw new Error('HTTP 503'); return index(calls > 8 ? ['1.0.0'] : []); };
  const state = await waitPublic('1.0.0', { fetchJson, sleep: async () => {}, timeoutSeconds: 60 });
  assert.equal(isPublic(state), true);
  await assert.rejects(waitPublic('1.0.0', { fetchJson: async () => index([]), sleep: async () => {}, timeoutSeconds: 0 }), /does not serve MicroEngine, MicroEngine\.Sdl/);
});

// sdk-release.yml's preflight: one test per state a release can be found in.
const all = value => Object.fromEntries(packageIds.map(id => [id, value]));
const some = n => Object.fromEntries(packageIds.map((id, i) => [id, i < n]));
const state = over => ({ documented: '0.5.0-alpha.1', prepared: '0.5.0-alpha.1', packages: all(false), release: null, notes: true,
  event: 'workflow_run', sha: 'b'.repeat(40), isAncestor: () => true, ...over });

test('release plan: a merge that documents a new version checks, drafts and publishes it', () => {
  const plan = releasePlan(state());
  assert.equal(plan.action, 'release');
  assert.deepEqual(plan.lines, ['nuget.org has 0 of 6 packages; GitHub release sdk-v0.5.0-alpha.1: none', 'Checking and drafting this commit, then publishing after approval.']);
});

test('release plan: any other merge after the release has nothing to do; by hand it verifies and deploys again', () => {
  const published = { packages: all(true), release: { draft: false, target: 'a'.repeat(40) } };
  assert.equal(releasePlan(state(published)).action, 'none');
  assert.equal(releasePlan(state({ ...published, event: 'workflow_dispatch' })).action, 'finish');
  // MicroEngineVersion raised for the next cycle, documentation still on the published version: nothing to do.
  assert.equal(releasePlan(state({ prepared: '0.5.0-alpha.2', packages: all(true), release: { draft: false, target: 'a' } })).action, 'none');
});

test('release plan: a draft of this very commit is published, never rebuilt (rejected approval, run again)', () => {
  assert.equal(releasePlan(state({ release: { draft: true, target: 'b'.repeat(40) } })).action, 'publish');
});

test('release plan: a fix merged after the draft replaces it while nothing is public', () => {
  const plan = releasePlan(state({ release: { draft: true, target: 'a'.repeat(40) } }));
  assert.equal(plan.action, 'release');
  assert.match(plan.lines.join('\n'), /The draft of aaaaaaaaaaaa is replaced by one of this commit/);
});

test('release plan: a publication that stopped part way finishes from its draft, whatever main is now', () => {
  for (const packages of [some(1), some(5), all(true)]) {
    assert.equal(releasePlan(state({ packages, release: { draft: true, target: 'a'.repeat(40) } })).action, 'publish');
  }
  assert.throws(() => releasePlan(state({ packages: some(3), release: { draft: true, target: 'a'.repeat(40) }, isAncestor: () => false })), /not in the history of b{40}/);
});

test('release plan: states no run can repair are refused with what to do', () => {
  assert.throws(() => releasePlan(state({ packages: some(2) })), /2 of 6 packages of 0\.5\.0-alpha\.1 but there is no draft .* by hand/);
  assert.throws(() => releasePlan(state({ packages: some(4), release: { draft: false, target: 'a' } })), /published on GitHub but nuget\.org lacks MicroEngine\.Cli, MicroEngine\.Templates/);
  assert.throws(() => releasePlan(state({ prepared: '0.5.0-alpha.2' })), /presents 0\.5\.0-alpha\.1, which nuget\.org does not serve, but this commit packs 0\.5\.0-alpha\.2/);
  assert.throws(() => releasePlan(state({ notes: false })), /no "## 0\.5\.0-alpha\.1" section/);
});

// Publication freeze (docs/releasing.md, "Publication freeze"): every state stops in preflight.
test('release plan: frozen, no state releases, publishes or deploys anything, by hand either', () => {
  const states = [state(), state({ release: { draft: true, target: 'b'.repeat(40) } }), state({ packages: some(3), release: { draft: true, target: 'a'.repeat(40) } }),
    state({ packages: all(true), release: { draft: false, target: 'a' }, event: 'workflow_dispatch' }), state({ packages: some(2) }), state({ notes: false })];
  for (const s of states) {
    const plan = releasePlan({ ...s, frozen: 'Phase two in progress' });
    assert.equal(plan.action, 'frozen');
    assert.deepEqual(plan.lines, ['Frozen: publication is frozen (.github/phase-freeze: Phase two in progress). Nothing is checked, drafted, pushed to nuget.org or deployed, whatever the state of 0.5.0-alpha.1. To release, delete .github/phase-freeze in a pull request (docs/releasing.md, "Publication freeze").']);
  }
  // Not frozen (null, as freezeReason returns without the file): the plans above are unchanged.
  assert.equal(releasePlan(state({ frozen: null })).action, 'release');
});

test('release-plan: a frozen tree answers frozen without asking nuget.org or GitHub', () => {
  const root = fixture({ '.github/phase-freeze': '# lift by deleting\nPhase two in progress\n' });
  fs.copyFileSync(new URL('../version.mjs', import.meta.url), path.join(root, 'sdk/version.mjs'));
  const output = path.join(root, 'github-output');
  // GitHub's API at a closed local port: any request fails at once, so passing proves none was made.
  const env = { PATH: process.env.PATH, SystemRoot: process.env.SystemRoot ?? '', GITHUB_OUTPUT: output, GITHUB_API_URL: 'http://127.0.0.1:9' };
  const plan = args => spawnSync(process.execPath, [path.join(root, 'sdk/version.mjs'), 'release-plan', '--sha', 'c'.repeat(40), ...args], { encoding: 'utf8', env });
  const run = plan(['--event', 'workflow_dispatch']);
  assert.equal(run.status, 0, run.stderr);
  assert.match(run.stdout, /### SDK 0\.4\.0-alpha\.2 from `cccccccccccc`\n\n- Frozen: publication is frozen \(\.github\/phase-freeze: Phase two in progress\)/);
  assert.equal(fs.readFileSync(output, 'utf8'), 'version=0.4.0-alpha.2\naction=frozen\n');
  // Without the file the same tree asks GitHub, which fails here: the freeze is what stopped it.
  fs.rmSync(path.join(root, '.github/phase-freeze'));
  assert.notEqual(plan([]).status, 0);
});

test('the documented samples check reports the published SDK build only while the documented version is its anchor', () => {
  // sdk/tests/docs.mjs: the anchor is the last SDK published before stage 5's breaking renames.
  assert.equal(publishedBuildRequired(reportOnlyVersion), false, 'documented version == anchor: report only');
  assert.equal(publishedBuildRequired('0.4.0-alpha.3'), true, 'the next release documents another version: required again');
  assert.equal(publishedBuildRequired('1.0.0'), true);
  // A release rewrites only active references: the anchor is not one, so releasing never moves it.
  assert.ok(!activeFiles().includes('sdk/tests/docs.mjs'));
  assert.deepEqual(findReferences(fs.readFileSync(new URL('./docs.mjs', import.meta.url), 'utf8')), []);
});

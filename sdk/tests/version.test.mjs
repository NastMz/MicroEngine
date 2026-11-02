// Tests for the SDK version tool: node --test sdk/tests/version.test.mjs (daily CI runs them in Plan)
import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { check, release, findReferences, activeFiles, documentedVersion, preparedVersion, publicState, isPublic, waitPublic, packageIds } from '../version.mjs';

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

test('check: the documented version needs a release-notes section', () => {
  const root = fixture();
  release('0.4.0-alpha.3', root);
  assert.deepEqual(check(root), ['docs/guide/release-notes.md has no "## 0.4.0-alpha.3" section for the documented version.']);
});

test('release: rewrites prepared, documented and every active reference, and leaves history alone', () => {
  const root = fixture();
  const before = Object.fromEntries(['docs/guide/release-notes.md', 'docs/migrations.md', 'docs/decisions/0001.md', 'site/src/roadmap.ts'].map(f => [f, read(root, f)]));
  const changed = release('0.4.0-alpha.3', root);
  assert.deepEqual(changed.sort(), ['Directory.Build.props', 'docs/guide/getting-microengine.md', 'docs/guide/status.md', 'docs/manual/audio.md', 'samples/Hello/Hello.csproj', 'sdk/version.json']);
  assert.equal(preparedVersion(root), '0.4.0-alpha.3');
  assert.equal(documentedVersion(root), '0.4.0-alpha.3');
  assert.match(read(root, 'docs/guide/getting-microengine.md'), /\*\*0\.4\.0-alpha\.3\*\*[\s\S]*Templates::0\.4\.0-alpha\.3[\s\S]*--version 0\.4\.0-alpha\.3[\s\S]*prints `0\.4\.0-alpha\.3`/);
  const audio = read(root, 'docs/manual/audio.md');
  assert.match(audio, /In 0\.4\.0-alpha\.1 it rises; it starts at full volume from\n0\.4\.0-alpha\.2\. One track\./, 'the history paragraph is untouched');
  assert.match(audio, /The current version is \*\*0\.4\.0-alpha\.3\*\* here too/, 'the paragraph after it is active again');
  for (const [file, text] of Object.entries(before)) assert.equal(read(root, file), text, `${file} unchanged`);
  // With the section written, the tree is consistent again, and a second run changes nothing.
  fs.writeFileSync(path.join(root, 'docs/guide/release-notes.md'), `# Release notes\n\n## 0.4.0-alpha.3\n\nMore.\n\n${before['docs/guide/release-notes.md'].slice('# Release notes\n\n'.length)}`);
  assert.deepEqual(check(root), []);
  assert.deepEqual(release('0.4.0-alpha.3', root), []);
});

test('release: a history paragraph ends at its blank line in a CRLF checkout too', () => {
  const root = fixture({ 'docs/manual/audio.md': 'Intro.\r\n\r\n<!-- sdk-version: history -->\r\nFrom 0.4.0-alpha.2 it starts.\r\n\r\nThe current version is **0.4.0-alpha.2** here.\r\n' });
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

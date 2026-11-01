// Native demo packages in the content-addressed pipeline (scripts/demos.mjs, packaging/README.md): keys per target,
// only checked packages stored, releases that carry them under player-facing names, and the site's download list.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import crypto from 'node:crypto';
import { execFileSync } from 'node:child_process';
import {
  nativeKey, demoKey, planNative, publishNative, publish, createRelease, fetchDemos, openStore, noticeFiles, playDir,
  verifyDownloads, releaseRecord, nativeArtifactName,
} from '../scripts/demos.mjs';
import { downloadName, target, androidVersionCode, requiredFiles, checkBuildInfo, installSteps } from '../../packaging/targets.mjs';

const git = (root, ...args) => execFileSync('git', args, { cwd: root, encoding: 'utf8' });
const write = (root, file, text) => {
  fs.mkdirSync(path.dirname(path.join(root, file)), { recursive: true });
  fs.writeFileSync(path.join(root, file), text);
};
const change = (root, file, text) => { write(root, file, text); git(root, 'add', '-A'); };
const sha256 = data => crypto.createHash('sha256').update(data).digest('hex');

function fixtureRepo() {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'native-repo-'));
  write(root, 'web/toolchain.env', 'DOTNET_SDK_VERSION=10.0.403\n');
  write(root, 'src/MicroEngine.Cli/Hosts/web/Program.cs', 'class Host {}');
  write(root, 'src/Engine.cs', 'class Engine {}');
  write(root, 'packaging/package.mjs', '// packager');
  write(root, 'packaging/tests/x.test.mjs', '// test');
  write(root, 'src/MicroEngine.Cli/Hosts/android/GameActivity.cs', 'class Activity {}');
  write(root, 'platforms/android/device-checks.mjs', '// device checks');
  write(root, 'THIRD-PARTY-NOTICES.md', '# notices');
  write(root, 'games/Alpha/Alpha.csproj', '<Project><PropertyGroup><ApplicationTitle>Alpha</ApplicationTitle><ApplicationId>dev.example.alpha</ApplicationId></PropertyGroup></Project>');
  write(root, 'games/Alpha/Game.cs', 'class Alpha {}');
  write(root, 'games/Alpha/README.md', '# Alpha');
  write(root, 'games/Alpha/scripts/run.script', 'wait 1');
  git(root, 'init', '-q');
  git(root, 'add', '-A');
  return root;
}

function keepPlayDir(t) {
  const backup = fs.existsSync(playDir) ? fs.mkdtempSync(path.join(os.tmpdir(), 'play-backup-')) : null;
  if (backup) fs.cpSync(playDir, backup, { recursive: true });
  t.after(() => {
    fs.rmSync(playDir, { recursive: true, force: true });
    if (backup) fs.cpSync(backup, playDir, { recursive: true });
  });
}

function webPackage(root) {
  const from = path.join(root, 'dist', 'web');
  const files = { 'index.html': '<!doctype html>', 'main.js': '', 'host.css': '', 'game.json': JSON.stringify({ id: 'Alpha', title: 'Alpha', dev: false }),
    '_framework/dotnet.js': '', '_framework/game.abcdefghij.wasm': 'wasm', ...Object.fromEntries(noticeFiles.map(f => [f, 'notice'])) };
  for (const [file, text] of Object.entries(files)) write(from, `Alpha/${file}`, text);
  return from;
}

/** A package file and the record packaging/verify.mjs would write for it after a passing check. */
function checkedPackage(root, targetId, { signing = targetId === 'android' ? 'release' : 'none', failed = false, played = true } = {}) {
  const t = target(targetId);
  const file = path.join(root, 'dist', `Alpha-${targetId}.${t.archive}`);
  write(root, `dist/Alpha-${targetId}.${t.archive}`, `package of Alpha for ${targetId}`);
  const data = fs.readFileSync(file);
  const levels = t.os === 'android' ? ['integrity', 'build-info', 'structure', 'dependencies', 'device', 'rendered'] : ['integrity', 'structure', 'build-info', 'dependencies', 'platforms', 'report', 'rendered'];
  const record = {
    game: 'Alpha', target: targetId, file: path.basename(file), checkedOn: 'test', sha256: sha256(data), size: data.length,
    levels: Object.fromEntries(levels.filter(l => played || !['platforms', 'report', 'rendered', 'device'].includes(l)).map(l => [l, { ok: true, detail: `${l} ok` }])),
    buildInfo: { game: 'Alpha', title: 'Alpha', gameVersion: '1.2.0', engineVersion: '0.2.0-alpha', target: targetId, key: nativeKey('Alpha', targetId, { root }).key, commit: 'c0ffee', dotnet: '10.0.14', signing },
    ...(failed && { failed: true }),
  };
  return { file, record };
}

const storeEnv = () => ({ DEMOS_STORE: `dir:${fs.mkdtempSync(path.join(os.tmpdir(), 'native-store-'))}` });

test('native keys: one per target, changed only by what that target is built from', () => {
  const root = fixtureRepo();
  const windows = nativeKey('Alpha', 'windows-x64', { root }).key;
  const android = nativeKey('Alpha', 'android', { root }).key;
  assert.match(windows, /^[0-9a-f]{16}$/);
  assert.notEqual(windows, nativeKey('Alpha', 'linux-x64', { root }).key, 'each target has its own key');
  const web = demoKey('Alpha', { root }).key;

  change(root, 'games/Alpha/README.md', '# Alpha, documented');
  change(root, 'packaging/tests/x.test.mjs', '// another test');
  change(root, 'platforms/android/device-checks.mjs', '// changed checks');
  assert.equal(nativeKey('Alpha', 'windows-x64', { root }).key, windows, 'docs and test tools are not inputs');
  assert.equal(nativeKey('Alpha', 'android', { root }).key, android);

  change(root, 'games/Alpha/scripts/run.script', 'wait 2');
  assert.equal(nativeKey('Alpha', 'windows-x64', { root }).key, windows, 'desktop packages do not ship input scripts');
  assert.notEqual(nativeKey('Alpha', 'android', { root }).key, android, 'the APK ships them');

  change(root, 'src/MicroEngine.Cli/Hosts/android/GameActivity.cs', 'class Activity { }');
  assert.equal(nativeKey('Alpha', 'windows-x64', { root }).key, windows, 'the Android host is not a desktop input');

  change(root, 'src/MicroEngine.Cli/Hosts/web/Program.cs', 'class Host { }');
  assert.equal(nativeKey('Alpha', 'windows-x64', { root }).key, windows, 'the web host is not a native input');
  assert.notEqual(demoKey('Alpha', { root }).key, web, 'but it is a web input');

  change(root, 'packaging/package.mjs', '// packager, changed');
  assert.notEqual(nativeKey('Alpha', 'windows-x64', { root }).key, windows, 'the packager is an input');
  assert.throws(() => nativeKey('Alpha', 'amiga', { root }), /unknown target "amiga"/);
});

test('store: only played, matching, release-signed packages are stored, once', async () => {
  const root = fixtureRepo();
  const store = openStore(storeEnv());
  const failed = checkedPackage(root, 'windows-x64', { failed: true });
  await assert.rejects(publishNative(store, { ...failed, verification: failed.record, root }), /its check failed/);
  const unplayed = checkedPackage(root, 'windows-x64', { played: false });
  await assert.rejects(publishNative(store, { ...unplayed, verification: unplayed.record, root }), /did not run platforms, report, rendered/);
  const debug = checkedPackage(root, 'android', { signing: 'debug' });
  await assert.rejects(publishNative(store, { ...debug, verification: debug.record, root }), /signed with a debug key/);
  const swapped = checkedPackage(root, 'linux-x64');
  fs.appendFileSync(swapped.file, 'tampered');
  await assert.rejects(publishNative(store, { ...swapped, verification: swapped.record, root }), /not the file that was checked/);

  const good = checkedPackage(root, 'windows-x64');
  const first = await publishNative(store, { ...good, verification: good.record, root });
  assert.equal(first.stored, true);
  assert.equal((await publishNative(store, { ...good, verification: good.record, root })).stored, false, 'never replaced');
  const plan = await planNative(store, { root, targetIds: ['windows-x64', 'linux-x64'] });
  assert.deepEqual(plan.map(p => [p.target, p.stored, p.runner]), [['windows-x64', true, 'windows-latest'], ['linux-x64', false, 'ubuntu-latest']]);

  change(root, 'games/Alpha/Game.cs', 'class Alpha { int changed; }');
  await assert.rejects(publishNative(store, { ...good, verification: good.record, root }), /built for key .* the current key is/);
});

test('release: native packages under player-facing names, complete or refused, and listed for the site', async t => {
  keepPlayDir(t);
  const root = fixtureRepo();
  const store = openStore(storeEnv());
  await publish(['Alpha'], store, { from: webPackage(root), root });
  for (const id of ['windows-x64', 'android']) {
    const p = checkedPackage(root, id);
    await publishNative(store, { ...p, verification: p.record, root });
  }
  await assert.rejects(createRelease(store, 'v0.2.0', { root, targets: ['windows-x64', 'linux-x64'] }), /Alpha: no linux-x64 package/);
  assert.equal(await store.releases.get('v0.2.0'), null, 'an incomplete release leaves no draft');

  const record = await createRelease(store, 'v0.2.0', { root, commit: 'c0ffee', targets: ['windows-x64', 'android'] });
  assert.deepEqual(Object.keys(record.downloads.Alpha), ['windows-x64', 'android']);
  assert.equal(record.downloads.Alpha['windows-x64'].file, 'Alpha-1.2.0-windows-x64.zip');
  assert.equal(record.downloads.Alpha.android.file, 'Alpha-1.2.0-android.apk');
  const draft = await store.releases.get('v0.2.0');
  assert.equal(await draft.has('Alpha-1.2.0-windows-x64.zip'), true);
  assert.equal((await verifyDownloads(store, 'v0.2.0')).checked.length, 2);

  const site = await fetchDemos({ DEMOS_SOURCE: 'release:v0.2.0' }, { store, root });
  assert.equal(site.downloads.Alpha['windows-x64'].gameVersion, '1.2.0');
  assert.equal(site.downloads.Alpha['windows-x64'].release, 'v0.2.0');
  assert.equal(site.downloads.Alpha['windows-x64'].url, null, 'a folder release has no public address');
  const written = JSON.parse(fs.readFileSync(path.join(playDir, 'demos.json'), 'utf8'));
  assert.deepEqual(Object.keys(written.downloads.Alpha), ['windows-x64', 'android']);

  // A release without native targets keeps the earlier shape: no downloads at all.
  const plain = await createRelease(store, 'v0.2.1', { root });
  assert.equal(plain.downloads, undefined);
  await store.releases.publish('v0.2.1');
  assert.equal((await fetchDemos({ DEMOS_SOURCE: 'release' }, { store, root })).downloads, undefined, 'no buttons for packages that do not exist');

  // A download altered after the release was drafted is caught.
  const dir = path.join(root, 'tampered');
  fs.mkdirSync(dir);
  const name = nativeArtifactName('Alpha', 'windows-x64', nativeKey('Alpha', 'windows-x64', { root }).key);
  assert.ok(await store.has(`${name}.json`));
  await draft.upload('Alpha-1.2.0-windows-x64.zip', Buffer.from('other bytes'));
  await assert.rejects(verifyDownloads(store, 'v0.2.0'), /Alpha-1.2.0-windows-x64.zip in release v0.2.0: .*release.json says/);
  assert.ok(await releaseRecord(draft));
});

test('targets: names, layouts, versions and install steps', () => {
  assert.equal(downloadName('NeonRift', '1.0.0', target('macos-arm64')), 'NeonRift-1.0.0-macos-arm64.zip');
  assert.throws(() => downloadName('Neon Rift', '1.0.0', target('android')), /not a game id/);
  assert.throws(() => downloadName('NeonRift', 'latest', target('android')), /not a version/);
  const mac = requiredFiles({ game: 'NeonRift', title: 'Neon Rift', content: ['music/a.ogg'] }, target('macos-arm64'));
  assert.ok(mac.includes('Neon Rift.app/Contents/MacOS/NeonRift'));
  assert.ok(mac.includes('Neon Rift.app/Contents/Resources/Content/music/a.ogg'));
  assert.ok(requiredFiles({ game: 'NeonRift', title: 'Neon Rift' }, target('windows-x64')).includes('NeonRift.exe'));
  assert.ok(requiredFiles({ game: 'NeonRift', title: 'Neon Rift' }, target('linux-x64')).includes('libSDL3.so'));
  assert.equal(androidVersionCode(Date.UTC(2026, 0, 1, 1) / 1000), 60);
  assert.ok(androidVersionCode(Date.UTC(2026, 10, 1) / 1000) < androidVersionCode(Date.UTC(2026, 10, 2) / 1000));
  assert.throws(() => androidVersionCode(Date.UTC(2025, 11, 31) / 1000), /before 2026-01-01/);
  assert.throws(() => checkBuildInfo('{"game":"A"}', { game: 'B' }), /says game "A", expected "B"/);
  assert.throws(() => checkBuildInfo('{"game":"A"}', { game: 'A' }), /has no title/);
  assert.match(installSteps(target('macos-arm64'), 'NeonRift', 'Neon Rift').join(' '), /"Neon Rift\.app"/);
});

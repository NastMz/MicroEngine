// The content-addressed demo pipeline: keys, publishing to a store, verified fetching (scripts/demos.mjs).
import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import http from 'node:http';
import { execFileSync } from 'node:child_process';
import {
  registry, demoKey, publish, plan, fetchDemos, openStore, noticeFiles, playDir, cacheDir, createRelease, verifyRelease,
  versionTag, nextVersion, candidatePackages, releaseRecord,
} from '../scripts/demos.mjs';

const git = (root, ...args) => execFileSync('git', args, { cwd: root, encoding: 'utf8' });
const write = (root, file, text) => {
  fs.mkdirSync(path.dirname(path.join(root, file)), { recursive: true });
  fs.writeFileSync(path.join(root, file), text);
};

// A repository with the same layout as MicroEngine and two games (games/<Id>/<Id>.csproj).
function fixtureRepo() {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'demo-repo-'));
  write(root, 'web/toolchain.env', 'DOTNET_SDK_VERSION=10.0.403   # pinned\nEMSCRIPTEN_VERSION=3.1.56\n');
  write(root, 'src/Engine.cs', 'class Engine {}');
  write(root, 'THIRD-PARTY-NOTICES.md', '# notices');
  for (const id of ['Alpha', 'Beta']) {
    write(root, `games/${id}/${id}.csproj`, `<Project><PropertyGroup><ApplicationTitle>${id}</ApplicationTitle><MicroEngineGameFactory>new ${id}.Game()</MicroEngineGameFactory></PropertyGroup></Project>`);
    write(root, `games/${id}/Game.cs`, `class ${id} {}`);
    write(root, `games/${id}/README.md`, `# ${id}`);
    write(root, `games/${id}/screenshots/01.png`, 'png');
    write(root, `games/${id}/scripts/run.script`, 'wait 1');
  }
  git(root, 'init', '-q');
  git(root, 'add', '-A');
  return root;
}

const change = (root, file, text) => { write(root, file, text); git(root, 'add', '-A'); };

// Release packages for the fixture's games, shaped like web/build.sh output.
function builtPackages(root, ids = ['Alpha', 'Beta']) {
  const from = path.join(root, 'dist', 'web');
  for (const id of ids) {
    const files = {
      'index.html': '<!doctype html>', 'main.js': '', 'host.css': '', 'game.json': JSON.stringify({ id, title: id, dev: false }),
      '_framework/dotnet.js': '', '_framework/game.abcdefghij.wasm': `wasm ${id}`, '_framework/game.abcdefghij.wasm.br': 'br',
      ...Object.fromEntries(noticeFiles.map(file => [file, 'notice'])),
    };
    for (const [file, text] of Object.entries(files)) write(from, `${id}/${file}`, text);
  }
  return from;
}

// fetchDemos writes the site's real public/play folder: keep and restore what was there.
function keepPlayDir(t) {
  const backup = fs.existsSync(playDir) ? fs.mkdtempSync(path.join(os.tmpdir(), 'play-backup-')) : null;
  if (backup) fs.cpSync(playDir, backup, { recursive: true });
  t.after(() => {
    fs.rmSync(playDir, { recursive: true, force: true });
    if (backup) fs.cpSync(backup, playDir, { recursive: true });
  });
}

test('registry: games come from their projects, with their folder', () => {
  const root = fixtureRepo();
  assert.deepEqual(registry(root).map(g => [g.id, g.title, g.dir]), [['Alpha', 'Alpha', 'games/Alpha'], ['Beta', 'Beta', 'games/Beta']]);
  assert.deepEqual(registry().map(g => g.id), ['MeadowRun', 'CourtyardKey', 'TrailDuel', 'NeonRift']);
});

test('keys: stable, per game, and only changed by what a web package is built from', () => {
  const root = fixtureRepo();
  const key = id => demoKey(id, { root }).key;
  const [alpha, beta] = [key('Alpha'), key('Beta')];
  assert.match(alpha, /^[0-9a-f]{16}$/);
  assert.notEqual(alpha, beta);
  assert.equal(key('Alpha'), alpha, 'same inputs, same key');

  change(root, 'games/Alpha/README.md', '# Alpha, new docs');
  change(root, 'games/Alpha/screenshots/01.png', 'new png');
  change(root, 'games/Alpha/scripts/run.script', 'wait 2');
  change(root, 'docs/guide.md', '# docs');
  change(root, 'site/src/page.astro', 'landing');
  assert.equal(key('Alpha'), alpha, 'docs, screenshots, input scripts and the site are not inputs');

  change(root, 'games/Beta/Game.cs', 'class Beta { int x; }');
  assert.equal(key('Alpha'), alpha, 'another game does not change this key');
  assert.notEqual(key('Beta'), beta);

  change(root, 'games/Alpha/Alpha.csproj', fs.readFileSync(path.join(root, 'games/Alpha/Alpha.csproj'), 'utf8').replace('new Alpha.Game()', 'new Alpha.Game(1)'));
  assert.notEqual(key('Alpha'), alpha, "the game's project (its identity and factory) is an input");
  const afterEntry = key('Alpha');
  change(root, 'src/Engine.cs', 'class Engine { int y; }');
  assert.notEqual(key('Alpha'), afterEntry, 'the engine is an input of every game');
  const afterEngine = key('Alpha');
  change(root, 'THIRD-PARTY-NOTICES.md', '# notices, updated');
  assert.notEqual(key('Alpha'), afterEngine, 'the notices ship in every package');
});

test('keys: line endings in the working copy do not matter', () => {
  const root = fixtureRepo();
  const before = demoKey('Alpha', { root }).key;
  write(root, 'src/Engine.cs', 'class Engine {}\r\n');
  assert.equal(demoKey('Alpha', { root }).key, before, 'only staged blobs count');
});

test('publish and fetch: built once, reused, and installed only after verification', async t => {
  keepPlayDir(t);
  const root = fixtureRepo();
  const from = builtPackages(root);
  const store = openStore({ DEMOS_STORE: `dir:${fs.mkdtempSync(path.join(os.tmpdir(), 'store-'))}` });

  assert.deepEqual((await plan(store, { root })).map(g => g.stored), [false, false]);
  const first = await publish(['Alpha', 'Beta'], store, { from, root });
  assert.deepEqual(first.map(r => r.stored), [true, true]);
  assert.deepEqual((await publish(['Alpha'], store, { from, root })).map(r => r.stored), [false], 'an existing artifact is never replaced');
  assert.deepEqual((await plan(store, { root })).map(g => g.stored), [true, true]);

  const record = await fetchDemos({}, { store, root });
  assert.equal(record.games.Alpha.key, demoKey('Alpha', { root }).key);
  assert.equal(fs.readFileSync(path.join(playDir, 'Alpha', '_framework', 'game.abcdefghij.wasm'), 'utf8'), 'wasm Alpha');
  assert.ok(!fs.existsSync(path.join(playDir, 'Alpha', '_framework', 'game.abcdefghij.wasm.br')), 'no .br/.gz copies');

  // A docs-only change keeps every key: the stored artifacts are reused, nothing to build.
  change(root, 'games/Beta/README.md', '# Beta docs');
  assert.deepEqual((await plan(store, { root })).filter(g => !g.stored), []);
  // An input change needs a new artifact: the site build refuses to go on without it.
  change(root, 'games/Beta/Game.cs', 'class Beta { int z; }');
  assert.deepEqual((await plan(store, { root })).filter(g => !g.stored).map(g => g.id), ['Beta']);
  await assert.rejects(fetchDemos({}, { store, root }), /no web package of Beta for key [0-9a-f]{16}/);
});

test('fetch: a tampered archive or file is refused', async t => {
  keepPlayDir(t);
  const root = fixtureRepo();
  const from = builtPackages(root);
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'store-'));
  const store = openStore({ DEMOS_STORE: `dir:${dir}` });
  await publish(['Alpha', 'Beta'], store, { from, root });
  const { key } = demoKey('Alpha', { root });
  const manifestFile = path.join(dir, `Alpha-${key}.json`);
  const manifest = JSON.parse(fs.readFileSync(manifestFile, 'utf8'));
  const archiveFile = path.join(dir, manifest.archive);
  const original = fs.readFileSync(archiveFile);
  // Archives verified earlier are reused from the local cache by SHA-256; start without it.
  fs.rmSync(path.join(cacheDir, `${manifest.sha256}.tar.gz`), { force: true });

  fs.writeFileSync(archiveFile, Buffer.concat([original, Buffer.from('x')]));
  await assert.rejects(fetchDemos({}, { store, root }), /Alpha-[0-9a-f]+.tar.gz: SHA-256 is .*its manifest says/);

  fs.writeFileSync(archiveFile, original);
  fs.writeFileSync(manifestFile, JSON.stringify({ ...manifest, files: { ...manifest.files, 'main.js': '0'.repeat(64) } }));
  await assert.rejects(fetchDemos({}, { store, root }), /Alpha: main.js does not match its manifest's SHA-256/);
});

test('fetch: development packages are refused, production refuses a build without demos', async () => {
  const root = fixtureRepo();
  const from = builtPackages(root);
  write(from, 'Alpha/game.json', JSON.stringify({ id: 'Alpha', title: 'Alpha', dev: true }));
  const store = openStore({ DEMOS_STORE: `dir:${fs.mkdtempSync(path.join(os.tmpdir(), 'store-'))}` });
  await assert.rejects(publish(['Alpha'], store, { from, root }), /development package/);
  await assert.rejects(fetchDemos({ SITE_DEMOS: 'none', VERCEL_ENV: 'production' }), /not allowed for a production/);
});

// A stand-in for the GitHub API of a private repository: every call needs the token, the release is created on
// first upload, and asset downloads redirect to "storage" on another origin, which must never receive the token.
async function fakeGitHub(token) {
  const seen = { storageAuth: [], created: 0 };
  const assets = new Map();
  let release = null;
  const storage = http.createServer((req, res) => {
    seen.storageAuth.push(req.headers.authorization ?? null);
    const asset = assets.get(decodeURIComponent(new URL(req.url, 'http://x').pathname.slice(1)));
    res.writeHead(asset ? 200 : 404);
    res.end(asset?.data);
  });
  await new Promise(resolve => storage.listen(0, '127.0.0.1', resolve));
  const api = http.createServer(async (req, res) => {
    const base = `http://127.0.0.1:${api.address().port}`;
    const body = await new Promise(resolve => { const chunks = []; req.on('data', c => chunks.push(c)); req.on('end', () => resolve(Buffer.concat(chunks))); });
    const json = (status, value) => { res.writeHead(status, { 'Content-Type': 'application/json' }); res.end(JSON.stringify(value)); };
    if (req.headers.authorization !== `Bearer ${token}`) return json(404, { message: 'Not Found' });
    const url = new URL(req.url, base);
    if (req.method === 'GET' && url.pathname === '/repos/owner/repo/releases/tags/demo-artifacts') return release ? json(200, release) : json(404, {});
    if (req.method === 'POST' && url.pathname === '/repos/owner/repo/releases') { seen.created++; release = { id: 7, ...JSON.parse(body) }; return json(201, release); }
    if (req.method === 'GET' && url.pathname === '/repos/owner/repo/releases/7/assets') {
      const page = Number(url.searchParams.get('page'));
      return json(200, page === 1 ? [...assets.values()].map(a => ({ name: a.name, url: `${base}/assets/${a.name}` })) : []);
    }
    if (req.method === 'POST' && url.pathname === '/repos/owner/repo/releases/7/assets') {
      const name = url.searchParams.get('name');
      if (assets.has(name)) return json(422, { errors: [{ code: 'already_exists' }] });
      assets.set(name, { name, data: body });
      return json(201, { name, url: `${base}/assets/${name}` });
    }
    if (req.method === 'GET' && url.pathname.startsWith('/assets/') && req.headers.accept === 'application/octet-stream') {
      res.writeHead(302, { Location: `http://127.0.0.1:${storage.address().port}/${url.pathname.slice(8)}?sig=abc` });
      return res.end();
    }
    json(404, {});
  });
  await new Promise(resolve => api.listen(0, '127.0.0.1', resolve));
  return { seen, apiBase: `http://127.0.0.1:${api.address().port}`, close: () => { api.close(); storage.close(); } };
}

test('GitHub store: artifacts go to the demo-artifacts prerelease; the token never leaves the API', async t => {
  keepPlayDir(t);
  const root = fixtureRepo();
  const from = builtPackages(root);
  const token = 'test-token-value';
  const github = await fakeGitHub(token);
  t.after(() => github.close());
  const env = { DEMOS_STORE: 'github', DEMOS_REPOSITORY: 'owner/repo', GITHUB_TOKEN: token, GITHUB_API_URL: github.apiBase };

  assert.throws(() => openStore({ ...env, GITHUB_TOKEN: undefined }), /needs GITHUB_TOKEN/);
  await assert.rejects(plan(openStore({ ...env, GITHUB_TOKEN: 'wrong' }), { root }).then(() => publish(['Alpha'], openStore({ ...env, GITHUB_TOKEN: 'wrong' }), { from, root })),
    error => /HTTP 404/.test(error.message) && !error.message.includes('wrong'));

  await publish(['Alpha', 'Beta'], openStore(env), { from, root });
  assert.equal(github.seen.created, 1, 'the release is created once');
  assert.deepEqual((await plan(openStore(env), { root })).map(g => g.stored), [true, true]);
  const record = await fetchDemos({}, { store: openStore(env), root });
  assert.equal(record.games.Beta.key, demoKey('Beta', { root }).key);
  assert.ok(github.seen.storageAuth.length > 0 && github.seen.storageAuth.every(a => a === null), 'the storage host received no Authorization header');
});

// Releases (docs/ci.md, "Releases"): what production serves, separate from the integration store daily CI fills.
const dirStoreEnv = () => ({ DEMOS_STORE: `dir:${fs.mkdtempSync(path.join(os.tmpdir(), 'store-'))}` });

test('release: a draft of the stored packages, published only on request, then served as the latest', async t => {
  keepPlayDir(t);
  const root = fixtureRepo();
  const store = openStore(dirStoreEnv());
  await publish(['Alpha', 'Beta'], store, { from: builtPackages(root), root });

  const record = await createRelease(store, versionTag('1.0.0'), { root, commit: 'c0ffee' });
  assert.equal(record.tag, 'v1.0.0');
  assert.deepEqual(Object.keys(record.games), ['Alpha', 'Beta']);
  assert.equal(record.games.Alpha.key, demoKey('Alpha', { root }).key);
  assert.equal((await store.releases.get('v1.0.0')).draft, true);
  assert.equal(await store.releases.latest(), null, 'a draft is not what production serves');
  await assert.rejects(fetchDemos({ DEMOS_SOURCE: 'release' }, { store, root }), /no published demo release/);

  // The release workflow tests the draft's own packages before publishing it.
  const draft = await fetchDemos({ DEMOS_SOURCE: 'release:v1.0.0' }, { store, root });
  assert.equal(draft.release, 'v1.0.0');
  await verifyRelease(store, 'v1.0.0', playDir);

  await store.releases.publish('v1.0.0');
  const served = await fetchDemos({ DEMOS_SOURCE: 'release', VERCEL_ENV: 'production' }, { store, root });
  assert.equal(served.games.Beta.from, 'release v1.0.0');
  assert.equal(served.games.Beta.sha256, record.games.Beta.sha256);
  await assert.rejects(createRelease(store, 'v1.0.0', { root }), /already published; choose a new version/);
});

test('release: a failed release leaves no draft and the public version unchanged', async t => {
  keepPlayDir(t);
  const root = fixtureRepo();
  const env = dirStoreEnv();
  const store = openStore(env);
  await publish(['Alpha', 'Beta'], store, { from: builtPackages(root), root });
  await createRelease(store, 'v1.0.0', { root });
  await store.releases.publish('v1.0.0');

  change(root, 'games/Beta/Game.cs', 'class Beta { int unbuilt; }');
  await assert.rejects(createRelease(store, 'v1.1.0', { root }), /Beta: no package for key [0-9a-f]{16}/);
  assert.equal(await store.releases.get('v1.1.0'), null, 'no draft left behind');
  assert.equal((await store.releases.latest()).tag, 'v1.0.0');

  // An altered archive in the integration store is refused before anything is copied.
  await publish(['Beta'], store, { from: builtPackages(root, ['Beta']), root });
  const dir = env.DEMOS_STORE.slice(4);
  const manifest = JSON.parse(fs.readFileSync(path.join(dir, `Beta-${demoKey('Beta', { root }).key}.json`), 'utf8'));
  fs.appendFileSync(path.join(dir, manifest.archive), 'x');
  await assert.rejects(createRelease(store, 'v1.1.0', { root }), /SHA-256 is .*its manifest says/);
  assert.equal(await store.releases.get('v1.1.0'), null);

  // A deployed file that differs from the release is caught before publishing.
  await fetchDemos({ DEMOS_SOURCE: 'release' }, { store, root });
  fs.appendFileSync(path.join(playDir, 'Alpha', 'main.js'), '// changed');
  await assert.rejects(verifyRelease(store, 'latest', playDir), /Alpha: main.js does not match/);
});

test('sources: candidates use a stored current package, else the released one; production refuses them', async t => {
  keepPlayDir(t);
  const root = fixtureRepo();
  const store = openStore(dirStoreEnv());
  await publish(['Alpha', 'Beta'], store, { from: builtPackages(root), root });
  await createRelease(store, 'v1.0.0', { root });
  await store.releases.publish('v1.0.0');

  change(root, 'games/Beta/Game.cs', 'class Beta { int next; }');
  let record = await fetchDemos({ DEMOS_SOURCE: 'candidates' }, { store, root });
  assert.equal(record.games.Alpha.from, 'candidate', 'same key as released: the stored package');
  assert.equal(record.games.Beta.from, 'release v1.0.0', 'not built yet: the approved package, under its own key');
  assert.notEqual(record.games.Beta.key, demoKey('Beta', { root }).key);

  await publish(['Beta'], store, { from: builtPackages(root, ['Beta']), root });
  record = await fetchDemos({ DEMOS_SOURCE: 'candidates' }, { store, root });
  assert.equal(record.games.Beta.from, 'candidate');
  assert.equal(record.games.Beta.key, demoKey('Beta', { root }).key);

  for (const DEMOS_SOURCE of ['keys', 'candidates']) {
    await assert.rejects(fetchDemos({ DEMOS_SOURCE, VERCEL_ENV: 'production' }, { store, root }), /production serves released demo packages only/);
  }
  await assert.rejects(fetchDemos({ DEMOS_SOURCE: 'nightly' }, { store, root }), /DEMOS_SOURCE must be/);
});

test('plan, ci policy: a game change builds that game, an engine change builds the canary only', async () => {
  const root = fixtureRepo();
  const store = openStore(dirStoreEnv());
  const builds = async options => (await plan(store, { root, ...options })).filter(g => g.build).map(g => g.id);

  assert.deepEqual(await builds({ policy: 'ci', canary: 'Beta' }), ['Alpha', 'Beta'], 'no release yet: build everything missing');
  await publish(['Alpha', 'Beta'], store, { from: builtPackages(root), root });
  await createRelease(store, 'v1.0.0', { root });
  await store.releases.publish('v1.0.0');
  assert.deepEqual(await builds({ policy: 'ci', canary: 'Beta' }), [], 'everything released and stored');

  change(root, 'games/Alpha/Game.cs', 'class Alpha { int x; }');
  assert.deepEqual(await builds({ policy: 'ci', canary: 'Beta' }), ['Alpha']);
  change(root, 'src/Engine.cs', 'class Engine { int y; }');
  assert.deepEqual(await builds({ policy: 'ci', canary: 'Beta' }), ['Alpha', 'Beta'], 'Alpha changed itself, Beta is the canary');
  assert.deepEqual(await builds({ policy: 'ci', canary: 'Alpha' }), ['Alpha']);
  assert.deepEqual(await builds({ policy: 'all' }), ['Alpha', 'Beta'], 'validation and releases build every missing package');
  assert.deepEqual(candidatePackages(await plan(store, { root, policy: 'ci', canary: 'Alpha' })),
    { Alpha: `candidate:${demoKey('Alpha', { root }).key}`, Beta: `release:${(await releaseRecord(await store.releases.latest())).games.Beta.key}` });
});

test('next version: the minor after the newest demo release; SDK tags and pre-releases never count', () => {
  assert.equal(nextVersion([]), '0.1.0');
  assert.equal(nextVersion(['v0.3.0', 'sdk-v0.4.0-alpha.2', 'v0.4.0', 'demo-artifacts', 'v0.10.0-rc.1', 'v0.2.0']), '0.5.0');
  assert.equal(nextVersion(['v0.9.0', 'v0.10.0', 'v1.0.2']), '1.1.0', 'numeric order, not text order');
});

test('versions: 1.2.0, v1.2.0 and pre-releases are accepted, anything else refused', () => {
  assert.equal(versionTag('1.2.0'), 'v1.2.0');
  assert.equal(versionTag(' v1.2.0 '), 'v1.2.0');
  assert.equal(versionTag('1.2.0-rc.1'), 'v1.2.0-rc.1');
  for (const bad of ['1.2', 'latest', 'v1.2.0;rm', '', undefined]) assert.throws(() => versionTag(bad), /is not a version/);
});

test('overlay: a fork pull request plays the packages it built without writing to the shared store', async t => {
  keepPlayDir(t);
  const root = fixtureRepo();
  const shared = dirStoreEnv().DEMOS_STORE;
  await publish(['Alpha', 'Beta'], openStore({ DEMOS_STORE: shared }), { from: builtPackages(root), root });

  change(root, 'games/Beta/Game.cs', 'class Beta { int forked; }');
  const overlay = fs.mkdtempSync(path.join(os.tmpdir(), 'overlay-'));
  await publish(['Beta'], openStore({ DEMOS_STORE: `dir:${overlay}` }), { from: builtPackages(root, ['Beta']), root });

  const store = openStore({ DEMOS_STORE: shared, DEMOS_OVERLAY: overlay });
  assert.match(store.describe, /over/);
  const record = await fetchDemos({ DEMOS_SOURCE: 'keys' }, { store, root });
  assert.equal(record.games.Beta.key, demoKey('Beta', { root }).key, 'the fork build, from the overlay');
  assert.equal(record.games.Alpha.key, demoKey('Alpha', { root }).key, 'unchanged games, from the shared store');
  const sharedDir = shared.slice(4);
  assert.equal(fs.readdirSync(sharedDir).some(name => name.includes(demoKey('Beta', { root }).key)), false, 'the shared store is untouched');
  change(root, 'games/Beta/Game.cs', 'class Beta { int again; }');
  await assert.rejects(publish(['Beta'], store, { from: builtPackages(root, ['Beta']), root }), /read-only/);
});

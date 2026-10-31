#!/usr/bin/env node
// Web demo artifacts for the site, addressed by content (docs/deployment.md, "Pipeline").
//
// Every game in the registry (web/games.props) has a key: a hash of the Git blobs of everything its web package is
// built from (engine, web host and toolchain files, the game's folder, its registry entry, the third-party
// notices). The same inputs give the same key, so a package is built once and reused until one of its inputs
// changes; documentation and site edits never change a key.
//
//   plan     lists each game's key and whether the store already holds its artifact (--github-output for CI)
//              node site/scripts/demos.mjs plan
//   publish  packs built packages (web/build.sh output) and adds them to the store, unless already there
//              node site/scripts/demos.mjs publish MeadowRun [NeonRift ...] [--from dist/web]
//   fetch    before `astro build`: puts every game's package in site/public/play/<Game>/, from
//              DEMOS_DIR=<dir>     packages built locally (development; not verified against keys)
//              the store           the artifact for the game's current key, checked against its manifest
//                                  (archive SHA-256, then every file's SHA-256)
//            SITE_DEMOS=none builds the site without demos (documentation work); production builds refuse it.
//   key      prints one game's key and the files it was computed from (diagnosis)
//
// Stores (DEMOS_STORE): "github" (default in GitHub Actions): assets of the prerelease `demo-artifacts` in this
// private repository, read and written with GITHUB_TOKEN; "dir:<path>": a local folder (tests, offline work).
// Each artifact is <Game>-<key>.tar.gz plus <Game>-<key>.json, written in that order: the manifest marks a
// complete artifact. Artifacts are never replaced.
//
// Packages are stored without their .br/.gz copies: Vercel compresses JavaScript, JSON and WebAssembly itself.
import fs from 'node:fs';
import os from 'node:os';
import fsp from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import zlib from 'node:zlib';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import * as tar from 'tar';

const siteRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
export const repoRoot = path.dirname(siteRoot);
// Licence notices web/build.sh puts in every package; a package without them must not be published.
export const noticeFiles = ['THIRD-PARTY-NOTICES.txt', 'licenses/dotnet-LICENSE.txt', 'licenses/dotnet-THIRD-PARTY-NOTICES.txt', 'licenses/emscripten-LICENSE.txt', 'licenses/raylib-LICENSE.txt'];
export const playDir = path.join(siteRoot, 'public', 'play');
export const cacheDir = path.join(siteRoot, 'node_modules', '.cache', 'microengine-demos');
const compressible = new Set(['.wasm', '.js', '.mjs', '.json', '.txt', '.css', '.html', '.svg']);
const skipped = file => /\.(br|gz)$/.test(file);
export const releaseTag = 'demo-artifacts';

// Inputs shared by every game's web package. Paths are Git pathspecs relative to the repository root.
const sharedInputs = [
  'src', 'web/Host', 'web/Directory.Build.props', 'web/Directory.Build.targets', 'web/build.sh', 'web/build-raylib-wasm.sh',
  'web/setup.sh', 'web/toolchain.env', 'Directory.Build.props', 'global.json', 'NuGet.config', 'THIRD-PARTY-NOTICES.md',
];
// Files inside those paths that never reach a release package.
const notAnInput = file => (/\.md$/i.test(file) && file !== 'THIRD-PARTY-NOTICES.md') || /(^|\/)(screenshots|scripts)\//.test(file);

class DemoError extends Error {}
const fail = message => { throw new DemoError(message); };

function walk(dir, base = dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap(entry => {
    const full = path.join(dir, entry.name);
    return entry.isDirectory() ? walk(full, base) : [path.relative(base, full).split(path.sep).join('/')];
  });
}

export const sha256 = data => crypto.createHash('sha256').update(data).digest('hex');
export const sha256File = file => sha256(fs.readFileSync(file));

/** The games the web host can build, read from web/games.props: id, title, game folder and registry entry. */
export function registry(root = repoRoot) {
  const text = fs.readFileSync(path.join(root, 'web', 'games.props'), 'utf8').replace(/\r\n/g, '\n');
  return [...text.matchAll(/<PropertyGroup Condition="'\$\(Game\)' == '([A-Za-z0-9_]+)'">([\s\S]*?)<\/PropertyGroup>/g)].map(([entry, id, body]) => {
    const project = body.match(/<GameProject>([^<]+)<\/GameProject>/)?.[1];
    if (!project) fail(`web/games.props: ${id} has no <GameProject>.`);
    return {
      id,
      title: body.match(/<GameTitle>([^<]+)<\/GameTitle>/)?.[1] ?? id,
      dir: `games/${path.posix.dirname(project)}`,
      entry: entry.replace(/\s+/g, ' ').trim(),
    };
  });
}

export const gameIds = registry().map(game => game.id);

function git(args, root) {
  return execFileSync('git', args, { cwd: root, encoding: 'utf8', maxBuffer: 64 * 1024 * 1024 });
}

/**
 * The key of one game's web package: SHA-256 (first 16 hex digits) over its registry entry and the Git blob ids of
 * its input files in the index. Blob ids do not depend on line-ending conversion, so Windows and Linux checkouts
 * agree. Uncommitted changes count only once staged.
 */
export function demoKey(id, { root = repoRoot } = {}) {
  const game = registry(root).find(g => g.id === id) ?? fail(`unknown game "${id}" (web/games.props).`);
  const listing = git(['ls-files', '-s', '-z', '--', ...sharedInputs, game.dir], root)
    .split('\0').filter(Boolean)
    .map(line => { const [meta, file] = line.split('\t'); return { blob: meta.split(' ')[1], file }; })
    .filter(({ file }) => !notAnInput(file))
    .sort((a, b) => (a.file < b.file ? -1 : a.file > b.file ? 1 : 0));
  if (!listing.some(({ file }) => file.startsWith(`${game.dir}/`))) fail(`${id}: no files tracked in ${game.dir}.`);
  const text = ['microengine-demo-key 1', `game ${id}`, `entry ${game.entry}`, ...listing.map(({ blob, file }) => `${blob} ${file}`)].join('\n');
  return { id, key: sha256(text).slice(0, 16), inputs: listing.map(({ file }) => file) };
}

const artifactName = (id, key) => `${id}-${key}`;

/** Checks that `dir` holds release packages for the given games; returns their game.json contents. */
export function checkPackages(dir, ids = gameIds) {
  if (!fs.existsSync(dir)) fail(`${dir} does not exist. Build the packages with web/build.sh first.`);
  return ids.map(id => {
    const folder = path.join(dir, id);
    const required = ['index.html', 'main.js', 'host.css', 'game.json', '_framework/dotnet.js', ...noticeFiles];
    for (const file of required) if (!fs.existsSync(path.join(folder, file))) fail(`${id}: ${file} is missing in ${folder}.`);
    const game = JSON.parse(fs.readFileSync(path.join(folder, 'game.json'), 'utf8'));
    if (game.id !== id) fail(`${id}: game.json names "${game.id}".`);
    if (game.dev) fail(`${id} is a development package (game.json "dev": true). Build release packages: web/build.sh.`);
    if (!walk(folder).some(file => file.endsWith('.wasm'))) fail(`${id}: no .wasm files in _framework.`);
    return game;
  });
}

/** Byte counts of one package: raw files and an estimate of what a Brotli-compressing CDN transfers. */
export function measure(folder) {
  let bytes = 0, transfer = 0, files = 0;
  for (const file of walk(folder).filter(f => !skipped(f))) {
    const data = fs.readFileSync(path.join(folder, file));
    files++;
    bytes += data.length;
    transfer += compressible.has(path.extname(file)) && data.length > 1024
      ? zlib.brotliCompressSync(data, { params: { [zlib.constants.BROTLI_PARAM_QUALITY]: 5 } }).length
      : data.length;
  }
  return { files, bytes, transfer };
}

function gitCommit(root = repoRoot) {
  try { return git(['rev-parse', 'HEAD'], root).trim(); }
  catch { return 'unknown'; }
}

// Follows redirects by hand so a token only ever goes to the GitHub API: asset downloads redirect to signed
// storage URLs on another host, which must not receive it (and whose signatures must not reach the build log).
async function get(url, headers, hops = 5) {
  for (let i = 0; i <= hops; i++) {
    const response = await fetch(url, { headers, redirect: 'manual' });
    if (response.status < 300 || response.status >= 400) return response;
    const next = new URL(response.headers.get('location'), url);
    if (next.origin !== new URL(url).origin) headers = Object.fromEntries(Object.entries(headers).filter(([k]) => k.toLowerCase() !== 'authorization'));
    url = next.href;
  }
  fail('too many redirects while downloading a demo artifact.');
}

/** A folder of artifacts. */
function dirStore(dir) {
  return {
    describe: `folder ${dir}`,
    has: async name => fs.existsSync(path.join(dir, name)),
    read: async name => fs.readFileSync(path.join(dir, name)),
    write: async (name, data) => {
      await fsp.mkdir(dir, { recursive: true });
      const file = path.join(dir, name);
      if (fs.existsSync(file)) return false;
      await fsp.writeFile(file, data);
      return true;
    },
  };
}

/** Assets of the `demo-artifacts` prerelease of a (private) GitHub repository, through the API. */
function githubStore({ repository, token, apiBase = 'https://api.github.com', uploadBase }) {
  if (!token) fail('the GitHub artifact store needs GITHUB_TOKEN (or DEMOS_TOKEN) with read access to the repository contents.');
  uploadBase ??= apiBase === 'https://api.github.com' ? 'https://uploads.github.com' : apiBase;
  const headers = { 'User-Agent': 'microengine-site', Authorization: `Bearer ${token}`, Accept: 'application/vnd.github+json', 'X-GitHub-Api-Version': '2022-11-28' };
  let release, assets;
  const load = async () => {
    const response = await get(`${apiBase}/repos/${repository}/releases/tags/${releaseTag}`, headers);
    if (response.status === 404) { release = null; assets = new Map(); return; }
    if (!response.ok) fail(`GitHub release ${releaseTag} of ${repository}: HTTP ${response.status}.`);
    release = await response.json();
    assets = new Map();
    for (let page = 1; ; page++) {
      const list = await get(`${apiBase}/repos/${repository}/releases/${release.id}/assets?per_page=100&page=${page}`, headers);
      if (!list.ok) fail(`listing assets of ${releaseTag}: HTTP ${list.status}.`);
      const batch = await list.json();
      for (const asset of batch) assets.set(asset.name, asset);
      if (batch.length < 100) break;
    }
  };
  return {
    describe: `release ${releaseTag} of ${repository}`,
    has: async name => { if (!assets) await load(); return assets.has(name); },
    read: async name => {
      if (!assets) await load();
      const asset = assets.get(name) ?? fail(`${name} is not in ${releaseTag}.`);
      const response = await get(asset.url, { ...headers, Accept: 'application/octet-stream' });
      if (!response.ok) fail(`downloading ${name} from ${releaseTag}: HTTP ${response.status}.`);
      return Buffer.from(await response.arrayBuffer());
    },
    write: async (name, data) => {
      if (!assets) await load();
      if (assets.has(name)) return false;
      if (!release) {
        const created = await fetch(`${apiBase}/repos/${repository}/releases`, {
          method: 'POST', headers: { ...headers, 'Content-Type': 'application/json' },
          body: JSON.stringify({ tag_name: releaseTag, name: 'Demo artifacts', prerelease: true, body: 'Web packages of the demos, one per game and input key. Written by the pipeline; do not edit by hand (docs/deployment.md).' }),
        });
        if (created.status === 422) await load(); // created meanwhile by another job
        else if (!created.ok) fail(`creating release ${releaseTag}: HTTP ${created.status}.`);
        else release = await created.json();
      }
      const type = name.endsWith('.json') ? 'application/json' : 'application/gzip';
      const response = await fetch(`${uploadBase}/repos/${repository}/releases/${release.id}/assets?name=${encodeURIComponent(name)}`, {
        method: 'POST', headers: { ...headers, 'Content-Type': type }, body: data,
      });
      if (response.status === 422) return false; // the same artifact was uploaded by a concurrent run
      if (!response.ok) fail(`uploading ${name} to ${releaseTag}: HTTP ${response.status}.`);
      assets.set(name, await response.json());
      return true;
    },
  };
}

/** The artifact store selected by the environment (DEMOS_STORE), or null when none is configured. */
export function openStore(env = process.env) {
  const choice = env.DEMOS_STORE ?? (env.GITHUB_ACTIONS === 'true' ? 'github' : '');
  if (!choice) return null;
  if (choice.startsWith('dir:')) return dirStore(path.resolve(choice.slice(4)));
  if (choice === 'github') {
    return githubStore({
      repository: env.DEMOS_REPOSITORY ?? env.GITHUB_REPOSITORY ?? 'NastMz/MicroEngine',
      token: env.DEMOS_TOKEN ?? env.GITHUB_TOKEN,
      apiBase: env.GITHUB_API_URL,
      uploadBase: env.DEMOS_UPLOAD_URL,
    });
  }
  fail(`DEMOS_STORE must be "github" or "dir:<path>", not "${choice}".`);
}

/** Each registry game with its key and whether its artifact is already stored. */
export async function plan(store, { root = repoRoot } = {}) {
  const games = [];
  for (const game of registry(root)) {
    const { key } = demoKey(game.id, { root });
    games.push({ id: game.id, key, stored: store ? await store.has(`${artifactName(game.id, key)}.json`) : false });
  }
  return games;
}

/** Packs the built package of each game and adds it to the store under the game's current key. */
export async function publish(ids, store, { from = path.join(repoRoot, 'dist', 'web'), root = repoRoot } = {}) {
  const results = [];
  for (const id of ids) {
    const [game] = checkPackages(from, [id]);
    const { key } = demoKey(id, { root });
    const name = artifactName(id, key);
    if (await store.has(`${name}.json`)) { results.push({ id, key, stored: false }); continue; }
    const folder = path.join(from, id);
    const files = walk(folder).filter(f => !skipped(f)).sort();
    const archive = path.join(fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()), 'demo-')), `${name}.tar.gz`);
    // Fixed order, owner and timestamps, so the same package gives the same archive.
    await tar.create({ gzip: { level: 9 }, file: archive, cwd: from, portable: true, noMtime: true, mtime: new Date(0) }, files.map(f => `${id}/${f}`));
    const data = fs.readFileSync(archive);
    const manifest = {
      schema: 1,
      game: id,
      title: game.title,
      key,
      archive: `${name}.tar.gz`,
      sha256: sha256(data),
      files: Object.fromEntries(files.map(f => [f, sha256File(path.join(folder, f))])),
      commit: gitCommit(root),
      toolchain: Object.fromEntries(fs.readFileSync(path.join(root, 'web', 'toolchain.env'), 'utf8').split('\n')
        .map(line => line.replace(/#.*/, '').trim()).filter(line => line.includes('=')).map(line => line.split('='))),
    };
    await store.write(`${name}.tar.gz`, data);
    const stored = await store.write(`${name}.json`, Buffer.from(`${JSON.stringify(manifest, null, 2)}\n`));
    await fsp.rm(path.dirname(archive), { recursive: true, force: true });
    results.push({ id, key, stored, sha256: manifest.sha256 });
  }
  return results;
}

/** Downloads, verifies and extracts one game's artifact for its current key into `target`. */
async function fetchArtifact(store, id, key, target) {
  const name = artifactName(id, key);
  if (!(await store.has(`${name}.json`))) {
    fail(`no web package of ${id} for key ${key} in ${store.describe}. The pipeline's build-demos job builds it; locally, build with web/build.sh and set DEMOS_DIR.`);
  }
  const manifest = JSON.parse((await store.read(`${name}.json`)).toString('utf8'));
  if (manifest.game !== id || manifest.key !== key) fail(`${name}.json describes ${manifest.game} ${manifest.key}, not ${id} ${key}.`);
  await fsp.mkdir(cacheDir, { recursive: true });
  const cached = path.join(cacheDir, `${manifest.sha256}.tar.gz`);
  if (!(fs.existsSync(cached) && sha256File(cached) === manifest.sha256)) {
    const data = await store.read(manifest.archive);
    if (sha256(data) !== manifest.sha256) fail(`${manifest.archive}: SHA-256 is ${sha256(data)}, its manifest says ${manifest.sha256}.`);
    await fsp.writeFile(cached, data);
  }
  await tar.extract({ file: cached, cwd: target, strict: true });
  const folder = path.join(target, id);
  const extracted = walk(folder).sort();
  const expected = Object.keys(manifest.files).sort();
  if (extracted.join('\n') !== expected.join('\n')) fail(`${id}: the archive's files differ from its manifest.`);
  for (const file of expected) {
    if (sha256File(path.join(folder, file)) !== manifest.files[file]) fail(`${id}: ${file} does not match its manifest's SHA-256.`);
  }
  return manifest;
}

export async function fetchDemos(env = process.env, { store = openStore(env), root = repoRoot } = {}) {
  if (env.SITE_DEMOS === 'none') {
    if (env.VERCEL_ENV === 'production') fail('SITE_DEMOS=none is not allowed for a production deployment.');
    await fsp.rm(playDir, { recursive: true, force: true });
    return { skipped: true };
  }
  const games = registry(root);
  const ids = games.map(g => g.id);
  await fsp.rm(playDir, { recursive: true, force: true });
  await fsp.mkdir(playDir, { recursive: true });
  const record = { source: '', commit: gitCommit(root), games: {} };
  if (env.DEMOS_DIR) {
    const dir = path.resolve(env.DEMOS_DIR);
    checkPackages(dir, ids);
    for (const id of ids) await fsp.cp(path.join(dir, id), path.join(playDir, id), { recursive: true, filter: src => !skipped(src) });
    record.source = `directory ${env.DEMOS_DIR} (not verified against keys)`;
    for (const game of games) record.games[game.id] = { title: game.title, key: 'local' };
  } else {
    if (!store) fail('no demo packages: set DEMOS_DIR to packages built with web/build.sh, DEMOS_STORE to an artifact store, or SITE_DEMOS=none.');
    record.source = store.describe;
    for (const game of games) {
      const { key } = demoKey(game.id, { root });
      const manifest = await fetchArtifact(store, game.id, key, playDir);
      record.games[game.id] = { title: game.title, key, sha256: manifest.sha256, commit: manifest.commit };
    }
  }
  checkPackages(playDir, ids);
  for (const id of ids) Object.assign(record.games[id], measure(path.join(playDir, id)));
  await fsp.writeFile(path.join(playDir, 'demos.json'), `${JSON.stringify(record, null, 2)}\n`);
  return record;
}

async function main(argv) {
  const [command, ...rest] = argv;
  const option = name => { const i = rest.indexOf(`--${name}`); return i >= 0 ? rest[i + 1] : undefined; };
  const store = openStore();
  if (command === 'plan') {
    const games = await plan(store);
    for (const g of games) console.log(`${g.id.padEnd(14)} ${g.key}  ${g.stored ? 'stored' : 'to build'}`);
    if (rest.includes('--github-output')) {
      const missing = games.filter(g => !g.stored).map(g => g.id);
      fs.appendFileSync(process.env.GITHUB_OUTPUT, `missing=${JSON.stringify(missing)}\nkeys=${JSON.stringify(Object.fromEntries(games.map(g => [g.id, g.key])))}\n`);
    }
  } else if (command === 'publish') {
    if (!store) fail('publish needs DEMOS_STORE (or GitHub Actions).');
    const ids = rest.filter(a => !a.startsWith('--') && a !== option('from'));
    if (!ids.length) fail('publish needs at least one game id.');
    for (const r of await publish(ids, store, { from: option('from') && path.resolve(option('from')) })) {
      console.log(`${r.id} ${r.key}: ${r.stored ? `stored (sha256 ${r.sha256})` : 'already in the store'}`);
    }
  } else if (command === 'fetch') {
    const result = await fetchDemos(process.env, { store });
    if (result.skipped) console.log('demos: skipped (SITE_DEMOS=none); demo pages will say the games are not in this build.');
    else console.log(`demos: ${Object.entries(result.games).map(([id, g]) => `${id} ${g.key}`).join(', ')} from ${result.source}`);
  } else if (command === 'key') {
    const { key, inputs } = demoKey(rest[0] ?? fail('key needs a game id.'));
    console.log(`${key}\n${inputs.length} input files:\n${inputs.join('\n')}`);
  } else {
    console.log('usage: node site/scripts/demos.mjs plan [--github-output] | publish <Game...> [--from DIR] | fetch | key <Game>');
    process.exitCode = command ? 1 : 0;
  }
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main(process.argv.slice(2)).catch(error => {
    console.error(`error: ${error instanceof DemoError ? error.message : error?.stack ?? error}`);
    process.exit(1);
  });
}

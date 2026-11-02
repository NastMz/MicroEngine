#!/usr/bin/env node
// Web demo artifacts for the site, addressed by content (docs/deployment.md, "Demo packages are addressed by content").
//
// Every game in the registry (games/<Id>/<Id>.csproj) has a key: a hash of the Git blobs of everything its web package is
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
//              DEMOS_SOURCE=keys   (default) the integration store's artifact for each game's current key
//              DEMOS_SOURCE=release[:<tag>]  a demo release (default: the latest published); production only
//                                  accepts this
//              DEMOS_SOURCE=candidates       the current key's artifact when stored, else the latest release's
//            every package is checked against its manifest (archive SHA-256, then every file's SHA-256).
//            SITE_DEMOS=none builds the site without demos (documentation work); production builds refuse it.
//   key      prints one game's key and the files it was computed from (diagnosis)
//   release-*  create, verify, publish, discard or show a demo release (docs/ci.md, "Releases")
//
// Native packages (packaging/README.md) share the store and the releases: one per game and target of
// packaging/targets.json, keyed over what a native build reads (nativeKey), stored only after packaging/verify.mjs
// played them (publish-native), copied into a release under player-facing names (release-create --targets) and
// listed in /play/demos.json as the site's downloads.
//
// Two kinds of artifact: integration packages (the store below, filled by daily CI and validation, keyed by
// content) and approved packages (releases `v<version>`, copies made by the release workflow). Production serves
// the latest published release only, so merging code never publishes a demo.
//
// Stores (DEMOS_STORE): "github" (default in GitHub Actions): assets of the prerelease `demo-artifacts` in this
// repository, read and written with GITHUB_TOKEN; "dir:<path>": a local folder (tests, offline work).
// Each artifact is <Game>-<key>.tar.gz plus <Game>-<key>.json, written in that order: the manifest marks a
// complete artifact. Artifacts are never replaced.
// DEMOS_OVERLAY=<dir>: a folder of artifacts read before the store and never written through it. A fork pull request
// has a read-only token, so it stores the packages it built in a folder (DEMOS_STORE=dir:<dir>), passes them to the
// site job as a workflow artifact and reads them back through the overlay; the shared store is never touched.
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
import { targets as nativeTargets, target as nativeTarget, downloadName } from '../../packaging/targets.mjs';

// tar for packaging/package.mjs, which has no node_modules of its own.
export { nativeTargets, tar };

const siteRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
export const repoRoot = path.dirname(siteRoot);
// Licence notices web/build.sh puts in every package (the game's notice, MicroEngine's MIT licence, third-party
// licences); a package without them must not be published.
export const noticeFiles = ['LICENSE.txt', 'licenses/microengine-LICENSE.txt', 'THIRD-PARTY-NOTICES.txt', 'licenses/dotnet-LICENSE.txt', 'licenses/dotnet-THIRD-PARTY-NOTICES.txt', 'licenses/emscripten-LICENSE.txt', 'licenses/raylib-LICENSE.txt'];
export const playDir = path.join(siteRoot, 'public', 'play');
export const cacheDir = path.join(siteRoot, 'node_modules', '.cache', 'microengine-demos');
const compressible = new Set(['.wasm', '.js', '.mjs', '.json', '.txt', '.css', '.html', '.svg']);
const skipped = file => /\.(br|gz)$/.test(file);
export const releaseTag = 'demo-artifacts';

// Inputs shared by every game's web package. Paths are Git pathspecs relative to the repository root.
// The browser host and the tool that builds it (`microengine package web`) live in src/MicroEngine.Cli.
const sharedInputs = [
  'src', 'web/build.sh', 'web/build-raylib-wasm.sh', 'web/setup.sh', 'web/toolchain.env', 'Directory.Build.props', 'global.json',
  'NuGet.config', 'THIRD-PARTY-NOTICES.md', 'LICENSE', 'games/LICENSE', 'games/Directory.Build.targets',
];
// Files inside those paths that never reach a release package.
const notAnInput = file => (/\.md$/i.test(file) && file !== 'THIRD-PARTY-NOTICES.md') || /(^|\/)(screenshots|scripts)\//.test(file)
  || /^src\/MicroEngine\.Cli\/Hosts\/(android|ios|mobile)\//.test(file);

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

// The demos in the order the site lists them; any other game folder with a project follows, alphabetically.
const demoOrder = ['MeadowRun', 'CourtyardKey', 'TrailDuel', 'NeonRift'];

/**
 * The demo games: every games/<Id>/<Id>.csproj, with the title its project declares (ApplicationTitle, the same
 * identity `microengine package` reads: docs/guide/app-identity.md), its folder and its registry entry.
 */
export function registry(root = repoRoot) {
  const dir = path.join(root, 'games');
  const ids = fs.readdirSync(dir, { withFileTypes: true })
    .filter(e => e.isDirectory() && fs.existsSync(path.join(dir, e.name, `${e.name}.csproj`))).map(e => e.name)
    .sort((a, b) => ((demoOrder.indexOf(a) + 1 || 99) - (demoOrder.indexOf(b) + 1 || 99)) || (a < b ? -1 : 1));
  return ids.map(id => {
    const project = fs.readFileSync(path.join(dir, id, `${id}.csproj`), 'utf8');
    const title = project.match(/<ApplicationTitle>([^<]+)<\/ApplicationTitle>/)?.[1] ?? fail(`games/${id}/${id}.csproj declares no <ApplicationTitle>.`);
    const applicationId = project.match(/<ApplicationId>([^<]+)<\/ApplicationId>/)?.[1];
    return { id, title, applicationId, dir: `games/${id}`, project: `games/${id}/${id}.csproj`, entry: `project games/${id}/${id}.csproj` };
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
  const game = registry(root).find(g => g.id === id) ?? fail(`unknown game "${id}" (games/<Id>/<Id>.csproj).`);
  const listing = git(['ls-files', '-s', '-z', '--', ...sharedInputs, game.dir], root)
    .split('\0').filter(Boolean)
    .map(line => { const [meta, file] = line.split('\t'); return { blob: meta.split(' ')[1], file }; })
    .filter(({ file }) => !notAnInput(file))
    .sort((a, b) => (a.file < b.file ? -1 : a.file > b.file ? 1 : 0));
  if (!listing.some(({ file }) => file.startsWith(`${game.dir}/`))) fail(`${id}: no files tracked in ${game.dir}.`);
  const lines = listing.map(({ blob, file }) => `${blob} ${file}`);
  const text = ['microengine-demo-key 1', `game ${id}`, `entry ${game.entry}`, ...lines].join('\n');
  // The two halves of the key: what every game shares (engine, web host, toolchain) and what is the game's own (its
  // folder and registry entry). Daily CI uses them to tell an engine-wide change from a change to one game.
  const own = line => line.slice(41).startsWith(`${game.dir}/`);
  return {
    id,
    key: sha256(text).slice(0, 16),
    shared: sha256(['shared', ...lines.filter(line => !own(line))].join('\n')).slice(0, 16),
    own: sha256(['own', game.entry, ...lines.filter(own)].join('\n')).slice(0, 16),
    inputs: listing.map(({ file }) => file),
  };
}

// Native packages (packaging/README.md): one per game and target of packaging/targets.json, keyed like web packages
// but over what a native build reads: the engine and `microengine` (src, which holds the Android host), the packaging
// scripts and the build settings. Android packages also ship the game's input scripts.
const nativeShared = ['src', 'packaging', 'Directory.Build.props', 'global.json', 'NuGet.config', 'THIRD-PARTY-NOTICES.md', 'LICENSE', 'games/LICENSE', 'games/Directory.Build.targets'];
const nativeExtra = {};
const notANativeInput = (file, t) => (/\.md$/i.test(file) && file !== 'THIRD-PARTY-NOTICES.md') || /(^|\/)screenshots\//.test(file)
  || file.startsWith('packaging/tests/') || /^platforms\/android\/[^/]+\.(mjs|sh)$/.test(file)
  || (t.os !== 'android' && /(^|\/)scripts\//.test(file))
  // Hosts of other targets: the browser and iOS hosts never reach a native package, the phone hosts never a desktop one.
  || /^src\/MicroEngine\.Cli\/Hosts\/(web|ios)\//.test(file) || (t.os !== 'android' && /^src\/MicroEngine\.Cli\/Hosts\/(android|mobile)\//.test(file));

/** The key of one game's native package for a target of packaging/targets.json, computed like {@link demoKey}. */
export function nativeKey(id, targetId, { root = repoRoot } = {}) {
  const t = nativeTarget(targetId);
  const game = registry(root).find(g => g.id === id) ?? fail(`unknown game "${id}" (games/<Id>/<Id>.csproj).`);
  const listing = git(['ls-files', '-s', '-z', '--', ...nativeShared, ...(nativeExtra[t.id] ?? []), game.dir], root)
    .split('\0').filter(Boolean)
    .map(line => { const [meta, file] = line.split('\t'); return { blob: meta.split(' ')[1], file }; })
    .filter(({ file }) => !notANativeInput(file, t))
    .sort((a, b) => (a.file < b.file ? -1 : a.file > b.file ? 1 : 0));
  if (!listing.some(({ file }) => file.startsWith(`${game.dir}/`))) fail(`${id}: no files tracked in ${game.dir}.`);
  const lines = listing.map(({ blob, file }) => `${blob} ${file}`);
  const text = ['microengine-native-key 1', `game ${id}`, `target ${JSON.stringify(t)}`, `entry ${game.entry}`, ...lines].join('\n');
  return { id, target: t.id, key: sha256(text).slice(0, 16), inputs: listing.map(({ file }) => file) };
}

const artifactName = (id, key) => `${id}-${key}`;
/** Store name of a native package: `<Game>-<target>-<key>`, then `.json` (manifest) or the archive's extension. */
export const nativeArtifactName = (id, targetId, key) => `${id}-${targetId}-${key}`;

const contentTypes = { '.json': 'application/json', '.gz': 'application/gzip', '.zip': 'application/zip', '.apk': 'application/vnd.android.package-archive' };
const contentType = name => contentTypes[path.extname(name)] ?? 'application/octet-stream';

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
          body: JSON.stringify({ tag_name: releaseTag, name: 'Demo artifacts', prerelease: true, body: 'Web packages of the demos, one per game and input key. Written by CI; do not edit by hand (docs/ci.md).' }),
        });
        if (created.status === 422) await load(); // created meanwhile by another job
        else if (!created.ok) fail(`creating release ${releaseTag}: HTTP ${created.status}.`);
        else release = await created.json();
      }
      const type = contentType(name);
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

// Demo releases (docs/ci.md, "Releases"): a release `v<version>` holds copies of the approved archives and manifests
// plus release.json, so it never depends on the integration store keeping them. A release is created as a draft,
// verified, and published only by the release workflow; the latest published release is what production serves.
// Both kinds of release store give the same handle: { tag, draft, target, has(name), read(name), upload(name, data) }.

/** Releases kept in <dir>/releases/<tag>/ (tests, offline work). */
function dirReleases(dir) {
  const root = path.join(dir, 'releases');
  const stateFile = tag => path.join(root, tag, 'release-state.json');
  const handle = tag => {
    const folder = path.join(root, tag);
    const state = JSON.parse(fs.readFileSync(stateFile(tag), 'utf8'));
    return {
      tag, draft: state.draft, target: state.target,
      has: async name => fs.existsSync(path.join(folder, name)),
      read: async name => fs.existsSync(path.join(folder, name)) ? fs.readFileSync(path.join(folder, name)) : fail(`${name} is not in release ${tag}.`),
      upload: async (name, data) => fsp.writeFile(path.join(folder, name), data),
      // A folder release is never public: its packages have no address a player could download from.
      downloadUrl: () => null,
    };
  };
  const exists = tag => fs.existsSync(stateFile(tag));
  return {
    describe: `folder ${root}`,
    latest: async () => {
      const file = path.join(root, 'LATEST');
      return fs.existsSync(file) ? handle(fs.readFileSync(file, 'utf8').trim()) : null;
    },
    get: async tag => (exists(tag) ? handle(tag) : null),
    createDraft: async (tag, target) => {
      if (exists(tag)) fail(`release ${tag} already exists.`);
      await fsp.mkdir(path.join(root, tag), { recursive: true });
      await fsp.writeFile(stateFile(tag), JSON.stringify({ draft: true, target }));
      return handle(tag);
    },
    publish: async tag => {
      const state = JSON.parse(fs.readFileSync(stateFile(tag), 'utf8'));
      await fsp.writeFile(stateFile(tag), JSON.stringify({ ...state, draft: false }));
      await fsp.writeFile(path.join(root, 'LATEST'), tag);
      return handle(tag);
    },
    discard: async tag => {
      if (!exists(tag)) return false;
      if (!handle(tag).draft) fail(`release ${tag} is published; only drafts are discarded.`);
      await fsp.rm(path.join(root, tag), { recursive: true, force: true });
      return true;
    },
  };
}

/** Releases of a (private) GitHub repository. Drafts are found by listing, since they have no tag yet. */
function githubReleases({ repository, token, apiBase = 'https://api.github.com', uploadBase }) {
  uploadBase ??= apiBase === 'https://api.github.com' ? 'https://uploads.github.com' : apiBase;
  const headers = { 'User-Agent': 'microengine-site', Authorization: `Bearer ${token}`, Accept: 'application/vnd.github+json', 'X-GitHub-Api-Version': '2022-11-28' };
  const call = async (method, url, body) => {
    const response = await fetch(`${apiBase}/repos/${repository}${url}`, { method, headers: { ...headers, 'Content-Type': 'application/json' }, body: body && JSON.stringify(body) });
    if (!response.ok && response.status !== 404) fail(`GitHub ${method} ${url}: HTTP ${response.status}.`);
    return response.status === 404 ? null : response.status === 204 ? {} : response.json();
  };
  const handle = async release => {
    const assets = new Map();
    for (let page = 1; ; page++) {
      const list = await call('GET', `/releases/${release.id}/assets?per_page=100&page=${page}`);
      for (const asset of list ?? []) assets.set(asset.name, asset);
      if (!list || list.length < 100) break;
    }
    return {
      tag: release.tag_name, draft: release.draft, target: release.target_commitish, id: release.id,
      has: async name => assets.has(name),
      read: async name => {
        const asset = assets.get(name) ?? fail(`${name} is not in release ${release.tag_name}.`);
        const response = await get(asset.url, { ...headers, Accept: 'application/octet-stream' });
        if (!response.ok) fail(`downloading ${name} from ${release.tag_name}: HTTP ${response.status}.`);
        return Buffer.from(await response.arrayBuffer());
      },
      upload: async (name, data) => {
        const type = contentType(name);
        const response = await fetch(`${uploadBase}/repos/${repository}/releases/${release.id}/assets?name=${encodeURIComponent(name)}`, {
          method: 'POST', headers: { ...headers, 'Content-Type': type }, body: data,
        });
        if (!response.ok) fail(`uploading ${name} to ${release.tag_name}: HTTP ${response.status}.`);
        assets.set(name, await response.json());
      },
      // The address GitHub serves a release asset at once the release is published (a draft's assets stay private).
      downloadUrl: name => `https://github.com/${repository}/releases/download/${encodeURIComponent(release.tag_name)}/${encodeURIComponent(name)}`,
    };
  };
  const find = async tag => {
    for (let page = 1; ; page++) {
      const list = await call('GET', `/releases?per_page=100&page=${page}`) ?? [];
      const found = list.find(r => r.tag_name === tag);
      if (found || list.length < 100) return found ?? null;
    }
  };
  return {
    describe: `releases of ${repository}`,
    latest: async () => { const release = await call('GET', '/releases/latest'); return release && handle(release); },
    get: async tag => { const release = await find(tag); return release && handle(release); },
    createDraft: async (tag, target, body = '') => {
      if (await find(tag)) fail(`release ${tag} already exists.`);
      return handle(await call('POST', '/releases', { tag_name: tag, target_commitish: target, name: tag, body, draft: true, prerelease: false, make_latest: 'false' }));
    },
    publish: async tag => {
      const release = await find(tag) ?? fail(`release ${tag} does not exist.`);
      return handle(await call('PATCH', `/releases/${release.id}`, { draft: false, make_latest: 'true' }));
    },
    discard: async tag => {
      const release = await find(tag);
      if (!release) return false;
      if (!release.draft) fail(`release ${tag} is published; only drafts are discarded.`);
      await call('DELETE', `/releases/${release.id}`);
      return true;
    },
  };
}

/** `base` with the artifacts of the folder `dir` read first; writing through it is refused. */
function overlayStore(dir, base) {
  const top = dirStore(dir);
  return {
    ...base,
    describe: `${top.describe} over ${base.describe}`,
    has: async name => (await top.has(name)) || base.has(name),
    read: async name => ((await top.has(name)) ? top.read(name) : base.read(name)),
    write: async () => fail('the overlay store is read-only: store packages in DEMOS_STORE=dir:<dir> instead.'),
  };
}

/** The artifact store selected by the environment (DEMOS_STORE, DEMOS_OVERLAY), or null when none is configured. */
export function openStore(env = process.env) {
  const store = baseStore(env);
  return store && env.DEMOS_OVERLAY ? overlayStore(path.resolve(env.DEMOS_OVERLAY), store) : store;
}

function baseStore(env) {
  const choice = env.DEMOS_STORE ?? (env.GITHUB_ACTIONS === 'true' ? 'github' : '');
  if (!choice) return null;
  if (choice.startsWith('dir:')) {
    const dir = path.resolve(choice.slice(4));
    return { ...dirStore(dir), releases: dirReleases(dir) };
  }
  if (choice === 'github') {
    const options = {
      repository: env.DEMOS_REPOSITORY ?? env.GITHUB_REPOSITORY ?? 'NastMz/MicroEngine',
      token: env.DEMOS_TOKEN ?? env.GITHUB_TOKEN,
      apiBase: env.GITHUB_API_URL,
      uploadBase: env.DEMOS_UPLOAD_URL,
    };
    return { ...githubStore(options), releases: githubReleases(options) };
  }
  fail(`DEMOS_STORE must be "github" or "dir:<path>", not "${choice}".`);
}

/** release.json of a release handle, or null without one. */
export async function releaseRecord(release) {
  return release ? JSON.parse((await release.read('release.json')).toString('utf8')) : null;
}

/**
 * Each registry game with its key, whether its artifact is stored, what the latest release holds for it, and
 * whether this run should build it. Policies: "all" builds every missing package (validation, releases); "ci"
 * builds a missing package only when the game's own inputs differ from its released package, plus the canary game
 * when the shared inputs (engine, web host, toolchain) differ: one build proves an engine-wide change still compiles
 * to WebAssembly, and the other games are built by the next validation or release. Without a release, "ci" builds
 * every missing package.
 */
export async function plan(store, { root = repoRoot, policy = 'all', canary } = {}) {
  const released = (await releaseRecord(store?.releases ? await store.releases.latest() : null))?.games;
  const games = [];
  for (const game of registry(root)) {
    const { key, shared, own } = demoKey(game.id, { root });
    const stored = store ? await store.has(`${artifactName(game.id, key)}.json`) : false;
    const was = released?.[game.id] ?? null;
    const build = !stored && (policy === 'all' || !was || was.own !== own || (game.id === canary && was.shared !== shared));
    games.push({ id: game.id, key, shared, own, stored, released: was, build });
  }
  return games;
}

/**
 * Each game's native package for each target of `targetIds` (default every target): its key, whether the store holds
 * it, and the runner that builds and checks it. A missing package is built by validation or a release; a stored one
 * passed its checks when it was stored, so it is reused for as long as its key does not change.
 */
export async function planNative(store, { root = repoRoot, targetIds = nativeTargets.map(t => t.id) } = {}) {
  const packages = [];
  for (const game of registry(root)) {
    for (const targetId of targetIds) {
      const t = nativeTarget(targetId);
      const { key } = nativeKey(game.id, t.id, { root });
      const stored = store ? await store.has(`${nativeArtifactName(game.id, t.id, key)}.json`) : false;
      packages.push({ game: game.id, target: t.id, key, stored, runner: t.runner });
    }
  }
  return packages;
}

/**
 * Adds one checked native package to the store: the archive, then its manifest (SHA-256, size, build-info.json and
 * the result of packaging/verify.mjs). Refuses a package whose check failed, did not run it, or that was checked for
 * another game, target or key, and an APK signed with a debug key: only packages a player could be given are stored.
 */
export async function publishNative(store, { file, verification, root = repoRoot }) {
  const record = typeof verification === 'string' ? JSON.parse(fs.readFileSync(verification, 'utf8')) : verification;
  const t = nativeTarget(record?.target ?? fail('the verification record names no target.'));
  const { key } = nativeKey(record.game, t.id, { root });
  const data = fs.readFileSync(file);
  if (record.failed) fail(`${record.game} ${t.id}: its check failed; it is not stored.`);
  const ran = t.os === 'android' ? ['device', 'rendered'] : ['platforms', 'report', 'rendered'];
  const notRun = ran.filter(level => !record.levels?.[level]?.ok);
  if (notRun.length) fail(`${record.game} ${t.id}: the check did not run ${notRun.join(', ')}; a package is stored only after it was played.`);
  if (record.sha256 !== sha256(data)) fail(`${path.basename(file)} is not the file that was checked (SHA-256 differs).`);
  if (record.buildInfo?.key !== key) fail(`${record.game} ${t.id} was built for key ${record.buildInfo?.key}, the current key is ${key}.`);
  if (t.signing === 'release' && record.buildInfo.signing !== 'release') fail(`${record.game} ${t.id} is signed with a ${record.buildInfo.signing} key; only release-signed packages are stored.`);
  const name = nativeArtifactName(record.game, t.id, key);
  if (await store.has(`${name}.json`)) return { game: record.game, target: t.id, key, stored: false };
  const manifest = {
    schema: 1, kind: 'native', game: record.game, target: t.id, key, archive: `${name}.${t.archive}`,
    sha256: record.sha256, size: data.length, buildInfo: record.buildInfo,
    verification: { checkedOn: record.checkedOn, levels: Object.fromEntries(Object.entries(record.levels).map(([k, v]) => [k, v.detail])), ...(record.certificate && { certificate: record.certificate }), ...(record.glibc && { glibc: record.glibc }), ...(record.gatekeeper && { gatekeeper: record.gatekeeper }) },
  };
  await store.write(manifest.archive, data);
  const stored = await store.write(`${name}.json`, Buffer.from(`${JSON.stringify(manifest, null, 2)}\n`));
  return { game: record.game, target: t.id, key, stored, sha256: manifest.sha256 };
}

/** The package each game gets in a "candidates" build: its current key when stored or being built, else the release's. */
export function candidatePackages(games) {
  return Object.fromEntries(games.map(g => [g.id, g.stored || g.build ? `candidate:${g.key}` : g.released ? `release:${g.released.key}` : 'missing']));
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

/** Downloads, verifies and extracts one game's artifact for `key` from `source` (a store or a release) into `target`. */
async function fetchArtifact(source, id, key, target, { describe = source.describe, sha256: expectedSha } = {}) {
  const name = artifactName(id, key);
  if (!(await source.has(`${name}.json`))) {
    fail(`no web package of ${id} for key ${key} in ${describe}. Daily CI or a release builds it; locally, build with web/build.sh and set DEMOS_DIR.`);
  }
  const manifest = JSON.parse((await source.read(`${name}.json`)).toString('utf8'));
  if (manifest.game !== id || manifest.key !== key) fail(`${name}.json describes ${manifest.game} ${manifest.key}, not ${id} ${key}.`);
  if (expectedSha && manifest.sha256 !== expectedSha) fail(`${name}.json says SHA-256 ${manifest.sha256}, the release records ${expectedSha}.`);
  await fsp.mkdir(cacheDir, { recursive: true });
  const cached = path.join(cacheDir, `${manifest.sha256}.tar.gz`);
  if (!(fs.existsSync(cached) && sha256File(cached) === manifest.sha256)) {
    const data = await source.read(manifest.archive);
    if (sha256(data) !== manifest.sha256) fail(`${manifest.archive}: SHA-256 is ${sha256(data)}, its manifest says ${manifest.sha256}.`);
    await fsp.writeFile(cached, data);
  }
  await tar.extract({ file: cached, cwd: target, strict: true });
  verifyFolder(path.join(target, id), manifest);
  return manifest;
}

/** Fails unless `folder` holds exactly the files of `manifest`, each with its SHA-256. */
export function verifyFolder(folder, manifest) {
  const id = manifest.game;
  const found = fs.existsSync(folder) ? walk(folder).sort() : [];
  const expected = Object.keys(manifest.files).sort();
  if (found.join('\n') !== expected.join('\n')) fail(`${id}: the files differ from its manifest.`);
  for (const file of expected) {
    if (sha256File(path.join(folder, file)) !== manifest.files[file]) fail(`${id}: ${file} does not match its manifest's SHA-256.`);
  }
}

/** A release by tag, or the latest published one for "latest"; fails when it does not exist. */
async function openRelease(store, tag) {
  const release = tag === 'latest' ? await store.releases.latest() : await store.releases.get(tag);
  return release ?? fail(tag === 'latest'
    ? `no published demo release in ${store.releases.describe}. Publish one with the release workflow (docs/ci.md, "Releases").`
    : `release ${tag} does not exist in ${store.releases.describe}.`);
}

/**
 * Puts every game's package in site/public/play/ from the source DEMOS_SOURCE names:
 *   keys (default)   the integration store's package for each game's current key
 *   release[:<tag>]  the packages of a release (default: the latest published); production accepts only this
 *   candidates       the current key's package when the integration store has it, else the latest release's
 */
export async function fetchDemos(env = process.env, { store = openStore(env), root = repoRoot } = {}) {
  const source = env.DEMOS_SOURCE ?? 'keys';
  if (env.SITE_DEMOS === 'none') {
    if (env.VERCEL_ENV === 'production') fail('SITE_DEMOS=none is not allowed for a production deployment.');
    await fsp.rm(playDir, { recursive: true, force: true });
    return { skipped: true };
  }
  if (env.VERCEL_ENV === 'production' && (env.DEMOS_DIR || !/^release(:|$)/.test(source))) {
    fail('production serves released demo packages only: DEMOS_SOURCE=release or release:<tag>.');
  }
  if (!['keys', 'candidates'].includes(source) && !/^release(:[\w.+-]+)?$/.test(source)) fail(`DEMOS_SOURCE must be keys, candidates, release or release:<tag>, not "${source}".`);
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
    let release = null, released = null;
    if (source !== 'keys') {
      release = source === 'candidates' ? await store.releases.latest() : await openRelease(store, source.split(':')[1] ?? 'latest');
      released = await releaseRecord(release);
      if (release) record.release = release.tag;
    }
    record.source = source === 'keys' ? store.describe : source === 'candidates' ? `${store.describe}, else release ${release?.tag ?? '(none)'}` : `release ${release.tag}`;
    for (const game of games) {
      const { key } = demoKey(game.id, { root });
      const fromStore = source === 'keys' || (source === 'candidates' && await store.has(`${artifactName(game.id, key)}.json`));
      let manifest;
      if (fromStore) manifest = await fetchArtifact(store, game.id, key, playDir);
      else {
        const entry = released?.games?.[game.id] ?? fail(`${game.id} is not in ${release ? `release ${release.tag}` : 'any published release'} and its current package is not stored.`);
        manifest = await fetchArtifact(release, game.id, entry.key, playDir, { describe: `release ${release.tag}`, sha256: entry.sha256 });
      }
      record.games[game.id] = { title: game.title, key: manifest.key, sha256: manifest.sha256, commit: manifest.commit, from: !fromStore ? `release ${release.tag}` : source === 'keys' ? 'store' : 'candidate' };
    }
    // What a visitor can download: the native packages of the release this build reads, never unreleased ones.
    if (release && released?.downloads) record.downloads = downloadsOf(release, released);
  }
  checkPackages(playDir, ids);
  for (const id of ids) Object.assign(record.games[id], measure(path.join(playDir, id)));
  await fsp.writeFile(path.join(playDir, 'demos.json'), `${JSON.stringify(record, null, 2)}\n`);
  return record;
}

/** `1.2.0` or `v1.2.0` → `v1.2.0`; anything else fails. */
export function versionTag(version) {
  const match = /^v?(\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?)$/.exec(String(version ?? '').trim());
  return match ? `v${match[1]}` : fail(`"${version}" is not a version (1.2.0, v1.2.0 or 1.2.0-rc.1).`);
}

/**
 * The release workflow's default version: the minor after the newest demo release `v<major>.<minor>.<patch>`
 * (`v0.4.0` → `0.5.0`), or `0.1.0` before the first. Pre-release tags and other series (`sdk-v*`) never count.
 */
export function nextVersion(tags) {
  const versions = tags.map(tag => /^v(\d+)\.(\d+)\.(\d+)$/.exec(tag)).filter(Boolean).map(m => m.slice(1).map(Number));
  if (!versions.length) return '0.1.0';
  versions.sort((a, b) => a[0] - b[0] || a[1] - b[1] || a[2] - b[2]);
  const [major, minor] = versions.at(-1);
  return `${major}.${minor + 1}.0`;
}

/**
 * Creates the draft release `tag` for the commit checked out at `root`: every game's package for that commit's key,
 * copied from the integration store after checking the archive against its manifest, plus release.json. Fails,
 * leaving no draft behind, when a package is missing or altered or the tag already belongs to a published release;
 * an earlier draft with the same tag (a failed attempt) is replaced.
 */
export async function createRelease(store, tag, { root = repoRoot, commit = gitCommit(root), targets = [] } = {}) {
  const existing = await store.releases.get(tag);
  if (existing && !existing.draft) fail(`release ${tag} is already published; choose a new version.`);
  if (existing) await store.releases.discard(tag);
  const games = {}, files = [], downloads = {};
  for (const game of registry(root)) {
    // Native packages of the targets this release offers: every game must have each, or the release is refused.
    for (const targetId of targets) {
      const t = nativeTarget(targetId);
      const { key } = nativeKey(game.id, t.id, { root });
      const name = nativeArtifactName(game.id, t.id, key);
      if (!(await store.has(`${name}.json`))) fail(`${game.id}: no ${t.id} package for key ${key} in ${store.describe}; build and check it first.`);
      const manifest = JSON.parse((await store.read(`${name}.json`)).toString('utf8'));
      if (manifest.game !== game.id || manifest.target !== t.id || manifest.key !== key) fail(`${name}.json describes ${manifest.game} ${manifest.target} ${manifest.key}.`);
      if (t.signing === 'release' && manifest.buildInfo.signing !== 'release') fail(`${name} is not release-signed.`);
      const data = await store.read(manifest.archive);
      if (sha256(data) !== manifest.sha256) fail(`${manifest.archive}: SHA-256 is ${sha256(data)}, its manifest says ${manifest.sha256}.`);
      const file = downloadName(game.id, manifest.buildInfo.gameVersion, t);
      const { gameVersion, engineVersion, commit: built, dotnet, signing } = manifest.buildInfo;
      (downloads[game.id] ??= {})[t.id] = { file, key, sha256: manifest.sha256, size: manifest.size, gameVersion, engineVersion, built, dotnet, signing, checkedOn: manifest.verification.checkedOn, checks: Object.keys(manifest.verification.levels) };
      files.push([file, data]);
    }
    const { key, shared, own } = demoKey(game.id, { root });
    const name = artifactName(game.id, key);
    if (!(await store.has(`${name}.json`))) fail(`${game.id}: no package for key ${key} in ${store.describe}; build it first.`);
    const manifestData = await store.read(`${name}.json`);
    const manifest = JSON.parse(manifestData.toString('utf8'));
    if (manifest.game !== game.id || manifest.key !== key) fail(`${name}.json describes ${manifest.game} ${manifest.key}.`);
    const archive = await store.read(manifest.archive);
    if (sha256(archive) !== manifest.sha256) fail(`${manifest.archive}: SHA-256 is ${sha256(archive)}, its manifest says ${manifest.sha256}.`);
    games[game.id] = { title: game.title, key, shared, own, archive: manifest.archive, sha256: manifest.sha256, built: manifest.commit, toolchain: manifest.toolchain };
    files.push([manifest.archive, archive], [`${name}.json`, manifestData]);
  }
  const record = { schema: 1, tag, commit, created: new Date().toISOString(), games, ...(targets.length && { downloads }) };
  const body = [`Demo packages of ${commit}, published by the release workflow (docs/ci.md).`, '',
    ...(targets.length ? ['The demos are all rights reserved (games/LICENSE); MicroEngine is MIT. Install steps for each system: docs/guide/downloads.md.', '',
      '| Download | Game version | Engine | SHA-256 |', '| --- | --- | --- | --- |',
      ...Object.values(downloads).flatMap(byTarget => Object.values(byTarget).map(d => `| \`${d.file}\` | ${d.gameVersion} | ${d.engineVersion} | \`${d.sha256}\` |`)), '',
      'Browser packages (used by the site):', ''] : []),
    '| Game | Key | Archive SHA-256 |', '| --- | --- | --- |',
    ...Object.entries(games).map(([id, g]) => `| ${id} | \`${g.key}\` | \`${g.sha256}\` |`)].join('\n');
  const draft = await store.releases.createDraft(tag, commit, body);
  try {
    for (const [name, data] of files) await draft.upload(name, data);
    await draft.upload('release.json', Buffer.from(`${JSON.stringify(record, null, 2)}\n`));
  } catch (error) {
    await store.releases.discard(tag).catch(() => {});
    throw error;
  }
  return record;
}

/** Checks a built site's play/ folder against a release: every game, every file, every SHA-256. */
export async function verifyRelease(store, tag, playFolder) {
  const release = await openRelease(store, tag);
  const record = await releaseRecord(release);
  for (const [id, entry] of Object.entries(record.games)) {
    const manifest = JSON.parse((await release.read(`${artifactName(id, entry.key)}.json`)).toString('utf8'));
    if (manifest.sha256 !== entry.sha256) fail(`${id}: its manifest says ${manifest.sha256}, release.json ${entry.sha256}.`);
    verifyFolder(path.join(playFolder, id), manifest);
  }
  return record;
}

/** The downloads of a release for the site: each file with its public address (null for a folder release). */
export function downloadsOf(release, record) {
  return Object.fromEntries(Object.entries(record.downloads ?? {}).map(([id, byTarget]) => [id, Object.fromEntries(Object.entries(byTarget)
    .map(([targetId, d]) => [targetId, { ...d, release: release.tag, url: release.downloadUrl(d.file) }]))]));
}

/**
 * Downloads every native package of a release (a draft through the API, or a published one) and checks each file's
 * size and SHA-256 against release.json: what players get is exactly what was checked.
 */
export async function verifyDownloads(store, tag) {
  const release = await openRelease(store, tag);
  const record = await releaseRecord(release);
  const checked = [];
  for (const byTarget of Object.values(record.downloads ?? {})) {
    for (const d of Object.values(byTarget)) {
      const data = await release.read(d.file);
      if (data.length !== d.size || sha256(data) !== d.sha256) fail(`${d.file} in release ${tag}: ${data.length} bytes, SHA-256 ${sha256(data)}; release.json says ${d.size} and ${d.sha256}.`);
      checked.push(d.file);
    }
  }
  return { record, checked };
}

/** After publication: each download's public address answers (following GitHub's redirect to its storage). */
export async function checkPublicDownloads(store, tag) {
  const release = await openRelease(store, tag);
  const results = [];
  for (const d of Object.values(downloadsOf(release, await releaseRecord(release))).flatMap(Object.values)) {
    if (!d.url) fail(`${d.file} has no public address (${store.releases.describe}).`);
    const response = await fetch(d.url, { method: 'HEAD', redirect: 'follow' });
    const size = Number(response.headers.get('content-length'));
    if (!response.ok || size !== d.size) fail(`${d.url}: HTTP ${response.status}, ${size} bytes (expected ${d.size}).`);
    results.push(d.url);
  }
  return results;
}

async function main(argv) {
  const [command, ...rest] = argv;
  const option = name => { const i = rest.indexOf(`--${name}`); return i >= 0 ? rest[i + 1] : undefined; };
  const store = openStore();
  if (command === 'plan') {
    const policy = option('policy') ?? 'all';
    if (!['all', 'ci'].includes(policy)) fail(`--policy must be all or ci, not "${policy}".`);
    const games = await plan(store, { policy, canary: option('canary') });
    const latest = store?.releases ? await store.releases.latest() : null;
    console.log(`| Game | Key | Integration store | Latest release (${latest?.tag ?? 'none'}) | This run |\n| --- | --- | --- | --- | --- |`);
    for (const g of games) {
      const why = g.build ? 'builds it' : g.stored ? 'reuses it' : `uses the released package (${policy} policy)`;
      console.log(`| ${g.id} | \`${g.key}\` | ${g.stored ? 'stored' : 'missing'} | ${g.released ? (g.released.key === g.key ? 'same key' : `\`${g.released.key}\``) : '—'} | ${why} |`);
    }
    if (rest.includes('--github-output')) {
      const ids = filter => JSON.stringify(games.filter(filter).map(g => g.id));
      const packages = Object.entries(candidatePackages(games)).map(([id, p]) => `${id}=${p}`).join(' ');
      fs.appendFileSync(process.env.GITHUB_OUTPUT, [`missing=${ids(g => !g.stored)}`, `build=${ids(g => g.build)}`,
        `keys=${JSON.stringify(Object.fromEntries(games.map(g => [g.id, g.key])))}`, `release=${latest?.tag ?? ''}`,
        `packages=${packages}`, `changed=${games.some(g => g.released?.key !== g.key)}`].join('\n') + '\n');
    }
    // --native <ids|all>: the native packages too (validation and releases build the missing ones).
    if (option('native')) {
      const targetIds = option('native') === 'all' ? nativeTargets.map(t => t.id) : option('native').split(',').filter(Boolean);
      const packages = await planNative(store, { targetIds });
      console.log(`\n| Game | Target | Key | Integration store | This run |\n| --- | --- | --- | --- | --- |`);
      for (const p of packages) console.log(`| ${p.game} | ${p.target} | \`${p.key}\` | ${p.stored ? 'stored' : 'missing'} | ${p.stored ? 'reuses it' : `builds and checks it (${p.runner})`} |`);
      if (rest.includes('--github-output')) {
        const missing = packages.filter(p => !p.stored).map(({ game, target, runner }) => ({ game, target, runner }));
        fs.appendFileSync(process.env.GITHUB_OUTPUT, `native=${JSON.stringify(missing)}\nnative-targets=${targetIds.join(',')}\n`);
      }
    }
  } else if (command === 'release-create') {
    const tag = versionTag(option('version'));
    const targets = (option('targets') ?? '').split(',').filter(Boolean);
    const record = await createRelease(store ?? fail('release-create needs DEMOS_STORE (or GitHub Actions).'), tag, { targets });
    console.log(`Draft release ${tag} of ${record.commit}:`);
    for (const [id, g] of Object.entries(record.games)) console.log(`- ${id}: key ${g.key}, sha256 ${g.sha256}`);
    for (const d of Object.values(record.downloads ?? {}).flatMap(Object.values)) console.log(`- ${d.file}: ${d.size} bytes, sha256 ${d.sha256}`);
    if (process.env.GITHUB_OUTPUT) fs.appendFileSync(process.env.GITHUB_OUTPUT, `tag=${tag}\n`);
  } else if (command === 'release-verify') {
    const record = await verifyRelease(store, option('tag') ?? 'latest', path.resolve(option('dir') ?? playDir));
    console.log(`${option('dir') ?? playDir} matches release ${record.tag}: ${Object.keys(record.games).join(', ')}, every file checked.`);
    if (rest.includes('--downloads')) {
      const { checked } = await verifyDownloads(store, option('tag') ?? 'latest');
      console.log(`${checked.length} downloads match release.json (size and SHA-256)${checked.length ? `: ${checked.join(', ')}` : ''}.`);
    }
  } else if (command === 'release-downloads') {
    const urls = await checkPublicDownloads(store, versionTag(option('tag')));
    console.log(`${urls.length} public downloads answer with their recorded size${urls.length ? `:\n${urls.join('\n')}` : '.'}`);
  } else if (command === 'publish-native') {
    if (!store) fail('publish-native needs DEMOS_STORE (or GitHub Actions).');
    const r = await publishNative(store, { file: path.resolve(option('file') ?? fail('publish-native needs --file.')), verification: path.resolve(option('verification') ?? fail('publish-native needs --verification (packaging/verify.mjs --record).')) });
    console.log(`${r.game} ${r.target} ${r.key}: ${r.stored ? `stored (sha256 ${r.sha256})` : 'already in the store'}`);
  } else if (command === 'release-publish') {
    const release = await store.releases.publish(versionTag(option('tag')));
    console.log(`Published ${release.tag}: it is now the latest release, the one production serves.`);
  } else if (command === 'release-discard') {
    console.log((await store.releases.discard(versionTag(option('tag')))) ? `Draft ${option('tag')} discarded.` : `No draft ${option('tag')}.`);
  } else if (command === 'release-show') {
    console.log(JSON.stringify(await releaseRecord(await openRelease(store, option('tag') ?? 'latest')), null, 2));
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
  } else if (command === 'next-version') {
    console.log(nextVersion(rest));
  } else if (command === 'key') {
    const { key, inputs } = demoKey(rest[0] ?? fail('key needs a game id.'));
    console.log(`${key}\n${inputs.length} input files:\n${inputs.join('\n')}`);
  } else {
    console.log(['usage: node site/scripts/demos.mjs <command>',
      '  plan [--policy all|ci] [--canary <Game>] [--native all|<ids>] [--github-output]   keys, stored packages, what to build',
      '  publish <Game...> [--from DIR]                               add built packages to the integration store',
      '  publish-native --file F --verification V                     add a checked native package (packaging/verify.mjs --record)',
      '  fetch                                                        packages for the site build (DEMOS_SOURCE)',
      '  key <Game>                                                   one key and its input files',
      '  next-version <tag...>                                        the version after the newest v<x.y.z> among these tags',
      '  release-create --version <1.2.0> [--targets <ids>]           draft release of this commit\'s packages (and native downloads)',
      '  release-verify [--tag <v1.2.0>|latest] [--dir <play folder>] [--downloads]   check built files (and downloads) against a release',
      '  release-downloads --tag <v1.2.0>                             the published downloads answer at their public addresses',
      '  release-publish --tag <v1.2.0>                               publish a draft; it becomes the latest',
      '  release-discard --tag <v1.2.0>                               delete a draft',
      '  release-show [--tag <v1.2.0>|latest]                         print release.json'].join('\n'));
    process.exitCode = command ? 1 : 0;
  }
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main(process.argv.slice(2)).catch(error => {
    console.error(`error: ${error instanceof DemoError ? error.message : error?.stack ?? error}`);
    process.exit(1);
  });
}

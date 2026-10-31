#!/usr/bin/env node
// Web demo artifacts for the site. Two steps of the pipeline (docs/deployment.md):
//
//   pack   Step 2, after web/build.sh: turns release packages (dist/web/<Game>/) into one versioned archive
//          (microengine-web-demos-<version>.tar.gz) plus its SHA-256, ready to attach to a GitHub release.
//            node site/scripts/demos.mjs pack --version 0.1.0 [--from dist/web] [--out dist/web-release] [--lock]
//          --lock also pins the new archive in site/demos.lock.json.
//
//   fetch  Step 3, before `astro build`: puts the four packages in site/public/play/<Game>/, from the first of
//            DEMOS_DIR=<dir>         a folder of packages built locally (web/build.sh output)
//            DEMOS_ARCHIVE=<file>    an archive made by `pack`, checked against demos.lock.json unless the lock is empty
//            demos.lock.json         the pinned release asset, downloaded and checked against its SHA-256; a private
//                                    repository needs DEMOS_TOKEN (read-only contents), used only for the GitHub API
//          SITE_DEMOS=none builds the site without demos (documentation work); production builds refuse it.
//
// Packages are copied without their .br/.gz copies: Vercel compresses JavaScript, JSON and WebAssembly itself.
import fs from 'node:fs';
import fsp from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import zlib from 'node:zlib';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import * as tar from 'tar';

const siteRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const repoRoot = path.dirname(siteRoot);
export const gameIds = ['MeadowRun', 'CourtyardKey', 'TrailDuel', 'NeonRift'];
// Licence notices web/build.sh puts in every package; a package without them must not be published.
export const noticeFiles = ['THIRD-PARTY-NOTICES.txt', 'licenses/dotnet-LICENSE.txt', 'licenses/dotnet-THIRD-PARTY-NOTICES.txt', 'licenses/emscripten-LICENSE.txt', 'licenses/raylib-LICENSE.txt'];
export const lockPath = path.join(siteRoot, 'demos.lock.json');
export const playDir = path.join(siteRoot, 'public', 'play');
const cacheDir = path.join(siteRoot, 'node_modules', '.cache', 'microengine-demos');
const compressible = new Set(['.wasm', '.js', '.mjs', '.json', '.txt', '.css', '.html', '.svg']);
const skipped = file => /\.(br|gz)$/.test(file);

class DemoError extends Error {}
const fail = message => { throw new DemoError(message); };

function walk(dir, base = dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap(entry => {
    const full = path.join(dir, entry.name);
    return entry.isDirectory() ? walk(full, base) : [path.relative(base, full).split(path.sep).join('/')];
  });
}

export const sha256File = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');

/** Checks that `dir` holds the four release packages; returns their game.json contents. */
export function checkPackages(dir) {
  if (!fs.existsSync(dir)) fail(`${dir} does not exist. Build the packages with web/build.sh first.`);
  return gameIds.map(id => {
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

/** Per-game byte counts: raw files and an estimate of what a Brotli-compressing CDN transfers. */
export function measure(dir) {
  return Object.fromEntries(gameIds.map(id => {
    const folder = path.join(dir, id);
    let bytes = 0, transfer = 0, files = 0;
    for (const file of walk(folder).filter(f => !skipped(f))) {
      const data = fs.readFileSync(path.join(folder, file));
      files++;
      bytes += data.length;
      transfer += compressible.has(path.extname(file)) && data.length > 1024
        ? zlib.brotliCompressSync(data, { params: { [zlib.constants.BROTLI_PARAM_QUALITY]: 5 } }).length
        : data.length;
    }
    return [id, { files, bytes, transfer }];
  }));
}

function gitCommit() {
  try { return execFileSync('git', ['rev-parse', 'HEAD'], { cwd: repoRoot, encoding: 'utf8' }).trim(); }
  catch { return 'unknown'; }
}

export async function pack({ version, from = path.join(repoRoot, 'dist', 'web'), out = path.join(repoRoot, 'dist', 'web-release'), lock = false, repository = 'NastMz/MicroEngine' }) {
  if (!version || !/^\d+\.\d+\.\d+([-.][0-9A-Za-z.]+)?$/.test(version)) fail('--version must be a version such as 0.1.0.');
  const games = checkPackages(from);
  const staging = path.join(out, `staging-${version}`);
  await fsp.rm(staging, { recursive: true, force: true });
  for (const id of gameIds) {
    await fsp.cp(path.join(from, id), path.join(staging, id), { recursive: true, filter: src => !skipped(src) });
  }
  const manifest = {
    version,
    commit: gitCommit(),
    games: Object.fromEntries(games.map(game => [game.id, { title: game.title }])),
  };
  await fsp.writeFile(path.join(staging, 'demos.json'), `${JSON.stringify(manifest, null, 2)}\n`);
  const asset = `microengine-web-demos-${version}.tar.gz`;
  const archive = path.join(out, asset);
  // Fixed order, owner and timestamps, so the same packages give the same archive.
  const entries = ['demos.json', ...gameIds.flatMap(id => walk(path.join(staging, id)).map(f => `${id}/${f}`))].sort();
  await tar.create({ gzip: { level: 9 }, file: archive, cwd: staging, portable: true, noMtime: true, mtime: new Date(0) }, entries);
  await fsp.rm(staging, { recursive: true, force: true });
  const sha256 = sha256File(archive);
  const pin = { version, release: `demos-v${version}`, asset, sha256, repository };
  if (lock) await fsp.writeFile(lockPath, `${JSON.stringify(pin, null, 2)}\n`);
  return { archive, sha256, pin };
}

function readLock() {
  if (!fs.existsSync(lockPath)) return null;
  const pin = JSON.parse(fs.readFileSync(lockPath, 'utf8'));
  return pin.sha256 ? pin : null;
}

// Follows redirects by hand so the token only ever goes to the GitHub API: release downloads redirect to signed
// storage URLs on another host, which must not receive it (and whose signatures must not reach the build log).
async function get(url, headers, hops = 5) {
  for (let i = 0; i <= hops; i++) {
    const response = await fetch(url, { headers, redirect: 'manual' });
    if (response.status < 300 || response.status >= 400) return response;
    const next = new URL(response.headers.get('location'), url);
    if (next.origin !== new URL(url).origin) headers = Object.fromEntries(Object.entries(headers).filter(([k]) => k.toLowerCase() !== 'authorization'));
    url = next.href;
  }
  fail('too many redirects while downloading the demo archive.');
}

async function download(pin, { token, apiBase = 'https://api.github.com' } = {}) {
  await fsp.mkdir(cacheDir, { recursive: true });
  const cached = path.join(cacheDir, `${pin.sha256}.tar.gz`);
  if (fs.existsSync(cached) && sha256File(cached) === pin.sha256) return cached;
  let url = pin.url ?? `https://github.com/${pin.repository}/releases/download/${pin.release}/${pin.asset}`;
  let shown = url;
  const headers = { 'User-Agent': 'microengine-site' };
  if (token && !pin.url) {
    // A private repository's assets are only reachable through the API, with a token that can read its contents.
    const api = `${apiBase}/repos/${pin.repository}/releases/tags/${pin.release}`;
    const release = await get(api, { ...headers, Authorization: `Bearer ${token}`, Accept: 'application/vnd.github+json' });
    if (!release.ok) fail(`GitHub release ${pin.release} of ${pin.repository}: HTTP ${release.status}${release.status === 404 ? ' (missing release, or a token without read access to the repository contents)' : ''}.`);
    const asset = (await release.json()).assets?.find(a => a.name === pin.asset);
    if (!asset) fail(`release ${pin.release} has no asset ${pin.asset}.`);
    url = asset.url;
    shown = `asset ${pin.asset} of release ${pin.release}`;
    Object.assign(headers, { Authorization: `Bearer ${token}`, Accept: 'application/octet-stream' });
  }
  const response = await get(url, headers);
  if (!response.ok) fail(`downloading ${shown}: HTTP ${response.status}${token ? '' : ' (a private repository needs DEMOS_TOKEN)'}.`);
  const partial = `${cached}.part`;
  await fsp.writeFile(partial, Buffer.from(await response.arrayBuffer()));
  const actual = sha256File(partial);
  if (actual !== pin.sha256) { await fsp.rm(partial); fail(`${pin.asset}: SHA-256 is ${actual}, demos.lock.json expects ${pin.sha256}.`); }
  await fsp.rename(partial, cached);
  return cached;
}

async function install(source, describe) {
  await fsp.rm(playDir, { recursive: true, force: true });
  await fsp.mkdir(playDir, { recursive: true });
  await source(playDir);
  checkPackages(playDir);
  const manifestFile = path.join(playDir, 'demos.json');
  const previous = fs.existsSync(manifestFile) ? JSON.parse(fs.readFileSync(manifestFile, 'utf8')) : {};
  const sizes = measure(playDir);
  const manifest = { version: previous.version ?? 'local', commit: previous.commit ?? gitCommit(), source: describe, games: {} };
  for (const id of gameIds) manifest.games[id] = { ...(previous.games?.[id] ?? {}), ...sizes[id] };
  await fsp.writeFile(manifestFile, `${JSON.stringify(manifest, null, 2)}\n`);
  return manifest;
}

export async function fetchDemos(env = process.env, { pin = readLock() } = {}) {
  if (env.SITE_DEMOS === 'none') {
    if (env.VERCEL_ENV === 'production') fail('SITE_DEMOS=none is not allowed for a production deployment.');
    await fsp.rm(playDir, { recursive: true, force: true });
    return { skipped: true };
  }
  if (env.DEMOS_DIR) {
    const dir = path.resolve(env.DEMOS_DIR);
    checkPackages(dir);
    return install(target => fsp.cp(dir, target, { recursive: true, filter: src => !skipped(src) && !/web-dev|web-tests/.test(src) }), `directory ${env.DEMOS_DIR}`);
  }
  let archive;
  if (env.DEMOS_ARCHIVE) {
    archive = path.resolve(env.DEMOS_ARCHIVE);
    if (!fs.existsSync(archive)) fail(`DEMOS_ARCHIVE ${archive} does not exist.`);
    if (pin && path.basename(archive) === pin.asset && sha256File(archive) !== pin.sha256) fail(`${archive} does not match the SHA-256 in demos.lock.json.`);
  } else {
    if (!pin) fail('no demo release is pinned in site/demos.lock.json yet. Set DEMOS_DIR to locally built packages, or publish a release (docs/deployment.md).');
    archive = await download(pin, { token: env.DEMOS_TOKEN ?? env.GITHUB_TOKEN, apiBase: env.GITHUB_API_URL });
  }
  return install(target => tar.extract({ file: archive, cwd: target, strict: true }), env.DEMOS_ARCHIVE ? `archive ${path.basename(archive)}` : `release ${pin.release}`);
}

async function main(argv) {
  const [command, ...rest] = argv;
  const option = name => { const i = rest.indexOf(`--${name}`); return i >= 0 ? rest[i + 1] : undefined; };
  if (command === 'pack') {
    const { archive, sha256 } = await pack({
      version: option('version'),
      from: option('from') && path.resolve(option('from')),
      out: option('out') && path.resolve(option('out')),
      lock: rest.includes('--lock'),
    });
    console.log(`${archive}\nsha256 ${sha256}`);
  } else if (command === 'fetch') {
    const result = await fetchDemos();
    if (result.skipped) console.log('demos: skipped (SITE_DEMOS=none); demo pages will say the games are not in this build.');
    else console.log(`demos: ${gameIds.length} packages from ${result.source} (version ${result.version}) in ${path.relative(process.cwd(), playDir)}`);
  } else {
    console.log('usage: node site/scripts/demos.mjs pack --version X.Y.Z [--from DIR] [--out DIR] [--lock] | fetch');
    process.exitCode = command ? 1 : 0;
  }
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main(process.argv.slice(2)).catch(error => {
    console.error(`error: ${error instanceof DemoError ? error.message : error?.stack ?? error}`);
    process.exit(1);
  });
}

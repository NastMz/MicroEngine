import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { classify } from '../src/lib/capability.mjs';
import { pack, fetchDemos, checkPackages, gameIds, noticeFiles, playDir, sha256File } from '../scripts/demos.mjs';
import http from 'node:http';
import { checkDist, checkPublicContent } from '../scripts/check-dist.mjs';
import { headerRules, headersFor } from './serve-dist.mjs';

const siteRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const desktop = { wasm: true, webgl: true, finePointer: true, hover: true, width: 1280, height: 800 };

test('capability: desktop browsers are offered the demos', () => {
  assert.deepEqual(classify(desktop), { verdict: 'desktop', reasons: [] });
});

test('capability: touch-only or small devices get a notice, not a block', () => {
  assert.deepEqual(classify({ ...desktop, finePointer: false, hover: false, width: 390, height: 844 }), { verdict: 'unvalidated', reasons: ['touch', 'small-screen'] });
  assert.deepEqual(classify({ ...desktop, width: 600 }), { verdict: 'unvalidated', reasons: ['small-screen'] });
});

test('capability: a hardware key press upgrades a touch device (hybrids, tablets with keyboards)', () => {
  assert.equal(classify({ ...desktop, finePointer: false, hover: false, keyboardSeen: true }).verdict, 'desktop');
});

test('capability: no WebAssembly or WebGL means the demos cannot run', () => {
  assert.deepEqual(classify({ ...desktop, webgl: false }), { verdict: 'unsupported', reasons: ['webgl'] });
});

// A minimal set of packages with the same shape as web/build.sh output.
function fakePackages({ dev = false } = {}) {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'demos-'));
  for (const id of gameIds) {
    const files = {
      'index.html': '<!doctype html>', 'main.js': '', 'host.css': '', 'game.json': JSON.stringify({ id, title: id, dev }),
      '_framework/dotnet.js': '', '_framework/game.abcdefghij.wasm': 'wasm', '_framework/game.abcdefghij.wasm.br': 'br',
      'content.txt': 'music/theme.ogg\n', 'Content/music/theme.ogg': 'ogg',
      ...Object.fromEntries(noticeFiles.map(file => [file, 'notice'])),
    };
    for (const [file, text] of Object.entries(files)) {
      fs.mkdirSync(path.dirname(path.join(dir, id, file)), { recursive: true });
      fs.writeFileSync(path.join(dir, id, file), text);
    }
  }
  return dir;
}

test('packaging: release packages become one versioned archive without .br/.gz copies', async t => {
  const from = fakePackages();
  const out = fs.mkdtempSync(path.join(os.tmpdir(), 'pack-'));
  const first = await pack({ version: '1.2.3', from, out });
  assert.equal(path.basename(first.archive), 'microengine-web-demos-1.2.3.tar.gz');
  assert.equal(first.pin.release, 'demos-v1.2.3');
  assert.equal(first.sha256, sha256File(first.archive));
  const again = await pack({ version: '1.2.3', from, out });
  assert.equal(again.sha256, first.sha256, 'the same packages give the same archive');

  const backup = fs.existsSync(playDir) ? fs.mkdtempSync(path.join(os.tmpdir(), 'play-backup-')) : null;
  if (backup) fs.cpSync(playDir, backup, { recursive: true });
  t.after(() => {
    fs.rmSync(playDir, { recursive: true, force: true });
    if (backup) fs.cpSync(backup, playDir, { recursive: true });
  });
  const manifest = await fetchDemos({ DEMOS_ARCHIVE: first.archive });
  assert.equal(manifest.version, '1.2.3');
  checkPackages(playDir);
  assert.ok(!fs.existsSync(path.join(playDir, 'NeonRift', '_framework', 'game.abcdefghij.wasm.br')));
  assert.ok(manifest.games.NeonRift.bytes > 0);
});

test('packaging: development packages and incomplete folders are refused', async () => {
  await assert.rejects(pack({ version: '1.0.0', from: fakePackages({ dev: true }), out: os.tmpdir() }), /development package/);
  const broken = fakePackages();
  fs.rmSync(path.join(broken, 'TrailDuel', '_framework', 'dotnet.js'));
  await assert.rejects(pack({ version: '1.0.0', from: broken, out: os.tmpdir() }), /TrailDuel: _framework\/dotnet.js is missing/);
  await assert.rejects(pack({ version: 'latest', from: fakePackages(), out: os.tmpdir() }), /--version/);
  const unlicensed = fakePackages();
  fs.rmSync(path.join(unlicensed, 'NeonRift', 'licenses', 'dotnet-LICENSE.txt'));
  await assert.rejects(pack({ version: '1.0.0', from: unlicensed, out: os.tmpdir() }), /NeonRift: licenses\/dotnet-LICENSE.txt is missing/);
});

test('fetch: production refuses to build without demos', async () => {
  await assert.rejects(fetchDemos({ SITE_DEMOS: 'none', VERCEL_ENV: 'production' }), /not allowed for a production/);
});

test('check-dist: reports a missing package file and a package listed content file', () => {
  const dist = fs.mkdtempSync(path.join(os.tmpdir(), 'dist-'));
  const problems = checkDist(dist);
  assert.ok(problems.includes('missing index.html'));
  assert.ok(problems.includes('missing play/NeonRift/_framework/dotnet.js'));
  assert.ok(!checkDist(dist, { demos: false }).some(p => p.includes('play/')));
});

test('vercel.json: hashed package files are immutable, everything under /play/ is noindex', () => {
  const rules = headerRules(JSON.parse(fs.readFileSync(path.join(siteRoot, 'vercel.json'), 'utf8')));
  const hashed = headersFor(rules, '/play/NeonRift/_framework/dotnet.native.um72e38wqy.js');
  assert.equal(hashed['Cache-Control'], 'public, max-age=31536000, immutable');
  assert.equal(hashed['X-Robots-Tag'], 'noindex');
  assert.equal(headersFor(rules, '/play/NeonRift/_framework/dotnet.js')['Cache-Control'], undefined, 'dotnet.js is not fingerprinted');
  assert.equal(headersFor(rules, '/play/NeonRift/Content/music/stage.ogg')['Cache-Control'], undefined);
  assert.equal(headersFor(rules, '/play/NeonRift/_framework/NeonRift.g4i6s41me9.wasm')['Content-Type'], 'application/wasm');
  assert.equal(headersFor(rules, '/_astro/index.Ab12.css')['Cache-Control'], 'public, max-age=31536000, immutable');
  assert.equal(headersFor(rules, '/docs/')['X-Robots-Tag'], undefined);
  assert.equal(headersFor(rules, '/docs/')['X-Content-Type-Options'], 'nosniff');
});

test('check-dist: private repository links, open source claims, internal files and secrets are refused', () => {
  const dist = fs.mkdtempSync(path.join(os.tmpdir(), 'dist-'));
  fs.writeFileSync(path.join(dist, 'index.html'), '<a href="https://github.com/NastMz/MicroEngine">Source</a> Open source, MIT licensed');
  fs.writeFileSync(path.join(dist, 'notes.md'), '# internal');
  fs.mkdirSync(path.join(dist, 'play'));
  fs.writeFileSync(path.join(dist, 'play', 'x.js'), 'const t = "ghp_secretvalue123";');
  const problems = checkPublicContent(dist, { secrets: ['ghp_secretvalue123'] });
  assert.ok(problems.includes('index.html contains a link to the private repository'));
  assert.ok(problems.includes('index.html contains an "open source" claim'));
  assert.ok(problems.includes('index.html contains an MIT licence claim'));
  assert.ok(problems.includes('internal file published: notes.md'));
  assert.ok(problems.includes('a build secret appears in play/x.js'));
  assert.ok(!problems.some(p => p.includes('ghp_secretvalue123')), 'the secret itself is never printed');
  fs.writeFileSync(path.join(dist, 'play', 'a.wasm'), Buffer.from('\0asm C:\\Users\\someone\\tools\\driver.c\0', 'latin1'));
  assert.ok(checkPublicContent(dist).includes("play/a.wasm contains a build machine's user folder path"));
});

// A stand-in for the GitHub API of a private repository: the release lookup and the asset download need the token,
// and the asset redirects to "storage" on another origin, which must never receive the token.
async function fakeGitHub(archive, token) {
  const seen = { storageAuth: [] };
  const storage = http.createServer((req, res) => {
    seen.storageAuth.push(req.headers.authorization ?? null);
    res.writeHead(200, { 'Content-Type': 'application/octet-stream' });
    fs.createReadStream(archive).pipe(res);
  });
  await new Promise(resolve => storage.listen(0, '127.0.0.1', resolve));
  const storageUrl = `http://127.0.0.1:${storage.address().port}/signed?sig=abc`;
  const api = http.createServer((req, res) => {
    if (req.headers.authorization !== `Bearer ${token}`) { res.writeHead(404); return res.end(); }
    const base = `http://127.0.0.1:${api.address().port}`;
    if (req.url === '/repos/owner/repo/releases/tags/demos-v1.2.3') {
      res.writeHead(200, { 'Content-Type': 'application/json' });
      return res.end(JSON.stringify({ assets: [{ name: 'microengine-web-demos-1.2.3.tar.gz', url: `${base}/assets/7` }] }));
    }
    if (req.url === '/assets/7' && req.headers.accept === 'application/octet-stream') {
      res.writeHead(302, { Location: storageUrl });
      return res.end();
    }
    res.writeHead(404);
    res.end();
  });
  await new Promise(resolve => api.listen(0, '127.0.0.1', resolve));
  return { seen, apiBase: `http://127.0.0.1:${api.address().port}`, close: () => { api.close(); storage.close(); } };
}

test('fetch: a private release comes through the API, is verified, and the token never leaves the API', async t => {
  const { archive, pin } = await pack({ version: '1.2.3', from: fakePackages(), out: fs.mkdtempSync(path.join(os.tmpdir(), 'pack-')), repository: 'owner/repo' });
  const token = 'test-token-value';
  const github = await fakeGitHub(archive, token);
  const backup = fs.existsSync(playDir) ? fs.mkdtempSync(path.join(os.tmpdir(), 'play-backup-')) : null;
  if (backup) fs.cpSync(playDir, backup, { recursive: true });
  const cached = path.join(siteRoot, 'node_modules', '.cache', 'microengine-demos', `${pin.sha256}.tar.gz`);
  fs.rmSync(cached, { force: true });
  t.after(() => {
    github.close();
    fs.rmSync(cached, { force: true });
    fs.rmSync(playDir, { recursive: true, force: true });
    if (backup) fs.cpSync(backup, playDir, { recursive: true });
  });

  await assert.rejects(fetchDemos({ GITHUB_API_URL: github.apiBase, DEMOS_TOKEN: 'wrong-token' }, { pin }),
    error => /HTTP 404 \(missing release, or a token without read access/.test(error.message) && !error.message.includes('wrong-token'));
  const manifest = await fetchDemos({ GITHUB_API_URL: github.apiBase, DEMOS_TOKEN: token }, { pin });
  assert.equal(manifest.version, '1.2.3');
  checkPackages(playDir);
  assert.deepEqual(github.seen.storageAuth, [null], 'the storage host received no Authorization header');
  fs.rmSync(cached, { force: true });
  await assert.rejects(fetchDemos({ GITHUB_API_URL: github.apiBase, DEMOS_TOKEN: token }, { pin: { ...pin, sha256: '0'.repeat(64) } }),
    /SHA-256 is .*, demos.lock.json expects 0{64}/);
});

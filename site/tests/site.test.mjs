import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { classify } from '../src/lib/capability.mjs';
import { gameIds } from '../scripts/demos.mjs';
import { checkDiagrams, checkDist, checkPublicContent } from '../scripts/check-dist.mjs';
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

test('capability: a package with touch controls is offered to phones and tablets', () => {
  assert.deepEqual(classify({ ...desktop, finePointer: false, hover: false, width: 390, height: 844, touchControls: true }), { verdict: 'desktop', reasons: [] });
  assert.equal(classify({ ...desktop, webgl: false, touchControls: true }).verdict, 'unsupported');
});

test('capability: a hardware key press upgrades a touch device (hybrids, tablets with keyboards)', () => {
  assert.equal(classify({ ...desktop, finePointer: false, hover: false, keyboardSeen: true }).verdict, 'desktop');
});

test('capability: no WebAssembly or WebGL means the demos cannot run', () => {
  assert.deepEqual(classify({ ...desktop, webgl: false }), { verdict: 'unsupported', reasons: ['webgl'] });
});

test('every game in the web registry has a page on the site', () => {
  const data = fs.readFileSync(path.join(siteRoot, 'src', 'data', 'games.ts'), 'utf8');
  const siteIds = [...data.matchAll(/^\s+id: '([A-Za-z0-9_]+)',$/gm)].map(m => m[1]);
  assert.deepEqual([...siteIds].sort(), [...gameIds].sort());
});

test('check-dist: reports a missing package file', () => {
  const dist = fs.mkdtempSync(path.join(os.tmpdir(), 'dist-'));
  const problems = checkDist(dist);
  assert.ok(problems.includes('missing index.html'));
  assert.ok(problems.includes('missing play/NeonRift/_framework/dotnet.js'));
  assert.ok(!checkDist(dist, { demos: false }).some(p => p.includes('play/')));
});

test('check-dist: every mermaid block of a public page is published as a diagram', () => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'repo-'));
  const dist = fs.mkdtempSync(path.join(os.tmpdir(), 'dist-'));
  fs.writeFileSync(path.join(root, 'a.md'), '# A\n\n```mermaid\nflowchart TB\n  a --> b\n```\n\n```text\nnot a diagram\n```\n');
  fs.mkdirSync(path.join(dist, 'docs', 'a'), { recursive: true });
  const list = [{ source: 'a.md', slug: 'docs/a' }];
  fs.writeFileSync(path.join(dist, 'docs', 'a', 'index.html'), '<pre data-language="mermaid"><code>flowchart TB</code></pre>');
  assert.deepEqual(checkDiagrams(dist, { list, root }), ['docs/a/index.html has 0 of the 1 diagram(s) in a.md']);
  fs.writeFileSync(path.join(dist, 'docs', 'a', 'index.html'), '<pre class="mermaid">flowchart TB\n  a --&gt; b</pre>');
  assert.deepEqual(checkDiagrams(dist, { list, root }), []);
});

test('vercel.json: hashed package files are immutable, /play/ is noindex, Git deployments are off', () => {
  const config = JSON.parse(fs.readFileSync(path.join(siteRoot, 'vercel.json'), 'utf8'));
  assert.equal(config.git?.deploymentEnabled, false, 'only the pipeline deploys (vercel deploy --prebuilt)');
  const rules = headerRules(config);
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

test('check-dist: repository links and the MIT engine are fine; demo pages need their notice; internal files, secrets and user paths are refused', () => {
  const dist = fs.mkdtempSync(path.join(os.tmpdir(), 'dist-'));
  fs.writeFileSync(path.join(dist, 'index.html'), '<a href="https://github.com/NastMz/MicroEngine">Source</a> Open source, MIT licensed');
  fs.writeFileSync(path.join(dist, 'notes.md'), '# internal');
  fs.mkdirSync(path.join(dist, 'demos', 'meadow-run'), { recursive: true });
  fs.mkdirSync(path.join(dist, 'demos', 'neon-rift'), { recursive: true });
  fs.writeFileSync(path.join(dist, 'demos', 'index.html'), 'All four demos');
  fs.writeFileSync(path.join(dist, 'demos', 'meadow-run', 'index.html'), 'Meadow Run. The engine is MIT.');
  fs.writeFileSync(path.join(dist, 'demos', 'neon-rift', 'index.html'), 'Neon Rift. Demo game: all rights reserved.');
  fs.mkdirSync(path.join(dist, 'play'));
  fs.writeFileSync(path.join(dist, 'play', 'x.js'), 'const t = "ghp_secretvalue123";');
  const problems = checkPublicContent(dist, { secrets: ['ghp_secretvalue123'] });
  assert.ok(!problems.some(p => p.startsWith('index.html')), 'links to the public repository and MIT claims for the engine are allowed');
  assert.ok(problems.includes('demos/meadow-run/index.html does not say the demo game is all rights reserved'));
  assert.ok(!problems.some(p => p.startsWith('demos/neon-rift/') || p.startsWith('demos/index.html')));
  assert.ok(problems.includes('internal file published: notes.md'));
  assert.ok(problems.includes('a build secret appears in play/x.js'));
  assert.ok(!problems.some(p => p.includes('ghp_secretvalue123')), 'the secret itself is never printed');
  fs.rmSync(path.join(dist, 'demos'), { recursive: true });
  fs.writeFileSync(path.join(dist, 'play', 'a.wasm'), Buffer.from('\0asm C:\\Users\\someone\\tools\\driver.c\0', 'latin1'));
  assert.ok(checkPublicContent(dist).includes("play/a.wasm contains a build machine's user folder path"));
  fs.writeFileSync(path.join(dist, 'play', 'a.wasm'), Buffer.from('\0asm /home/runner/work/x/obj/x.pdb\0', 'latin1'));
  assert.ok(!checkPublicContent(dist).some(p => p.includes('a.wasm')), 'public CI runner paths in third-party assemblies are allowed');
});

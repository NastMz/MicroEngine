// Size, start-up and frame time of web packages, for comparing web backends (raylib, SDL) on the same machine.
//
//   node web/tests/measure.mjs [--packages DIR] [--games A,B] [--runs N] [--seconds S] [--out DIR] [--headed]
//
// Per game, the package's download (raw, and with the .br copies a server sends), the native module alone, then N runs
// in fresh Chrome profiles: page load to the ready screen, the click that starts the game (WebHost.Start runs inside it,
// so it is the platform's and the game's start-up), and S seconds of play with the time spent inside each animation
// frame callback (the whole game frame) and the updates and frames counted. Medians over the runs. The backend is the
// package's own (game.json, recorded by packages that could run on another platform; SDL otherwise). Measured, never a gate: a shared CI runner draws with a software GPU (llvmpipe) and
// localhost hides the network, so numbers compare backends, not what a player sees.
// Writes a Markdown table to stdout and <out>/measure-<Game>.json.
import fs from 'node:fs';
import path from 'node:path';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer-core';

const here = path.dirname(fileURLToPath(import.meta.url));
const repo = path.resolve(here, '../..');
const args = process.argv.slice(2);
const option = (name, fallback) => { const i = args.indexOf(name); return i >= 0 ? args.splice(i, 2)[1] : fallback; };
const flag = name => { const i = args.indexOf(name); return i >= 0 && args.splice(i, 1).length > 0; };
const packages = path.resolve(option('--packages', path.join(repo, 'dist/web-dev')));
const games = option('--games', 'MeadowRun,CourtyardKey,TrailDuel,NeonRift').split(',');
const runs = Number(option('--runs', '3'));
const seconds = Number(option('--seconds', '6'));
const out = path.resolve(option('--out', path.join(repo, 'dist/web-tests')));
const headed = flag('--headed');
const port = Number(option('--port', '8095'));
const base = `http://127.0.0.1:${port}`;
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
const median = values => { const v = [...values].sort((a, b) => a - b); return v.length ? v[Math.floor(v.length / 2)] : NaN; };
const quantile = (values, q) => { const v = [...values].sort((a, b) => a - b); return v.length ? v[Math.min(v.length - 1, Math.floor(q * v.length))] : NaN; };

/** Bytes a browser downloads: every file, and the same files when the .br copies are sent (as web/build.sh counts). */
function size(dir) {
  let raw = 0, br = 0, native = 0;
  const walk = d => {
    for (const entry of fs.readdirSync(d, { withFileTypes: true })) {
      const file = path.join(d, entry.name);
      if (entry.isDirectory()) { walk(file); continue; }
      if (/\.(br|gz)$/.test(entry.name)) continue;
      const n = fs.statSync(file).size;
      const compressed = fs.existsSync(`${file}.br`) ? fs.statSync(`${file}.br`).size : n;
      raw += n;
      br += compressed;
      if (/^dotnet\.native\..*\.wasm$/.test(entry.name)) native = compressed;
    }
  };
  walk(dir);
  return { rawKB: Math.round(raw / 1024), brotliKB: Math.round(br / 1024), nativeBrotliKB: Math.round(native / 1024) };
}

async function run(browser, game) {
  const context = await browser.createBrowserContext();
  const page = await context.newPage();
  await page.setViewport({ width: 960, height: 600 });
  // Time inside every animation frame callback: the page's callback is the whole game frame (WebHost.Frame).
  await page.evaluateOnNewDocument(() => {
    const raf = window.requestAnimationFrame.bind(window);
    window.__work = [];
    window.requestAnimationFrame = cb => raf(t => {
      const start = performance.now();
      try { cb(t); } finally { window.__work.push(performance.now() - start); }
    });
  });
  const errors = [];
  page.on('pageerror', e => errors.push(String(e)));
  const t0 = Date.now();
  await page.goto(`${base}/${game}/`, { waitUntil: 'domcontentloaded' });
  for (;;) {
    const state = await page.evaluate(() => window.microengine?.state ?? 'loading');
    if (state === 'ready') break;
    if (state === 'failed' || Date.now() - t0 > 120_000) throw new Error(`${game} did not load (${state})`);
    await sleep(20);
  }
  const load = Date.now() - t0;
  // The click starts the game synchronously (WebHost.Start inside the gesture): its duration is the start-up.
  const start = await page.evaluate(() => {
    const before = performance.now();
    document.getElementById('play').click();
    return { ms: performance.now() - before, state: window.microengine.state };
  });
  if (start.state !== 'playing') throw new Error(`${game} did not start (${start.state})`);
  await sleep(1000);
  await page.evaluate(() => { window.__work = []; });
  const [u0, f0] = (await page.evaluate(() => window.microengine.stats())).split(' ').map(Number);
  await sleep(seconds * 1000);
  const [u1, f1] = (await page.evaluate(() => window.microengine.stats())).split(' ').map(Number);
  const work = await page.evaluate(() => window.__work);
  const webgl = await page.evaluate(() => {
    const canvas = document.getElementById('canvas');
    return canvas.getContext('webgl2') ? 'WebGL 2' : canvas.getContext('webgl') ? 'WebGL 1' : 'unknown';
  });
  await context.close();
  return { load, start: start.ms, workMedian: median(work), workP95: quantile(work, 0.95), workMax: Math.max(...work),
    fps: (f1 - f0) / seconds, ups: (u1 - u0) / seconds, webgl, errors: errors.length };
}

const executablePath = [process.env.CHROME_PATH, 'C:/Program Files/Google/Chrome/Application/chrome.exe', '/usr/bin/google-chrome',
  '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'].find(p => p && fs.existsSync(p));
if (!executablePath) throw new Error('Chrome not found; set CHROME_PATH.');
const chromeFlags = (process.env.CHROME_FLAGS ?? '').split(/\s+/).filter(Boolean);
const server = spawn(process.execPath, [path.join(repo, 'web/serve.mjs'), packages, String(port)], { stdio: 'ignore' });
await sleep(500);
const browser = await puppeteer.launch({ browser: 'chrome', executablePath, headless: !headed,
  args: ['--no-first-run', '--autoplay-policy=no-user-gesture-required', '--window-size=1024,768', ...chromeFlags] });
let failed = 0;
try {
  console.log(`### Web package measurements (${await browser.version()}, medians of ${runs} runs)\n`);
  console.log('| Game | Backend | Download (Brotli / raw) | Native module (Brotli) | Load to ready | Click to playing | Frame work median / p95 / max | Frames, updates per second | WebGL | Page errors |');
  console.log('| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |');
  fs.mkdirSync(out, { recursive: true });
  for (const game of games) {
    const dir = path.join(packages, game);
    const backend = JSON.parse(fs.readFileSync(path.join(dir, 'game.json'), 'utf8')).backend ?? 'sdl';
    const sizes = size(dir);
    const results = [];
    try {
      for (let i = 0; i < runs; i++) results.push(await run(browser, game));
    } catch (e) {
      failed++;
      console.log(`| ${game} | ${backend} | ${sizes.brotliKB} / ${sizes.rawKB} KB | ${sizes.nativeBrotliKB} KB | failed: ${e.message} | | | | | |`);
      continue;
    }
    const m = key => median(results.map(r => r[key]));
    const summary = { game, backend, ...sizes, runs: results, loadMs: m('load'), startMs: m('start'), workMedianMs: m('workMedian'),
      workP95Ms: m('workP95'), workMaxMs: m('workMax'), fps: m('fps'), ups: m('ups') };
    fs.writeFileSync(path.join(out, `measure-${game}.json`), JSON.stringify(summary, null, 2));
    const f = (v, d = 0) => v.toFixed(d);
    console.log(`| ${game} | ${backend} | ${sizes.brotliKB} / ${sizes.rawKB} KB | ${sizes.nativeBrotliKB} KB | ${f(summary.loadMs)} ms | ${f(summary.startMs)} ms | `
      + `${f(summary.workMedianMs, 1)} / ${f(summary.workP95Ms, 1)} / ${f(summary.workMaxMs, 1)} ms | ${f(summary.fps)}, ${f(summary.ups)} | ${results[0].webgl} | ${results.reduce((n, r) => n + r.errors, 0)} |`);
  }
} finally {
  await browser.close();
  server.kill();
}
process.exit(failed ? 1 : 0);

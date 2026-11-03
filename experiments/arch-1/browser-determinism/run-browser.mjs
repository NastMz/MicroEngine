// Plays one scenario of web/tests/scenarios.json in a browser on a probed development package and keeps its state
// trace and captures, as `web/tests/run.mjs scripts` does.
//
//   node run-browser.mjs --copy DIR --game G --script S --out FILE [--captures DIR] [--browser chrome|firefox]
//                        [--env K=V,...] [--load N] [--headed]
//
// DIR is a probe copy (apply-probe.mjs) with development packages in DIR/dist/web-dev (web/build.sh --dev) and
// puppeteer-core installed in DIR/web/tests (npm ci). --env adds UPPER_CASE URL parameters, which development packages
// turn into environment variables (MICROENGINE_T0_DUMP=a-b). --load N runs N busy workers beside the page (CPU load,
// to make frame pacing irregular, as on a busy CI runner).
import fs from 'node:fs';
import path from 'node:path';
import { spawn } from 'node:child_process';
import { Worker } from 'node:worker_threads';
import { createRequire } from 'node:module';

const args = process.argv.slice(2);
const option = (name, fallback) => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : fallback; };
const copy = path.resolve(option('--copy', ''));
const game = option('--game', '');
const script = option('--script', '');
const out = path.resolve(option('--out', ''));
const captures = option('--captures', '');
const browserName = option('--browser', 'chrome');
const load = Number(option('--load', '0'));
const port = Number(option('--port', '8097'));
// K=V pairs separated by commas; a value may itself hold commas (MICROENGINE_T0_SKIPFIELDS=_view,_hud).
const extra = Object.fromEntries(option('--env', '').split(/,(?=[A-Z][A-Z0-9_]*=)/).filter(Boolean).map(kv => [kv.slice(0, kv.indexOf('=')), kv.slice(kv.indexOf('=') + 1)]));
const puppeteer = createRequire(path.join(copy, 'web/tests/package.json'))('puppeteer-core');

const executables = {
  chrome: [process.env.CHROME_PATH, 'C:/Program Files/Google/Chrome/Application/chrome.exe', '/usr/bin/google-chrome'],
  firefox: [process.env.FIREFOX_PATH, 'C:/Program Files/Mozilla Firefox/firefox.exe', '/usr/bin/firefox'],
};
const { scenarios } = JSON.parse(fs.readFileSync(path.join(copy, 'web/tests/scenarios.json'), 'utf8'));
const scenario = scenarios.find(s => s.game === game && s.script === script);
if (!scenario) throw new Error(`no scenario ${game} ${script} in scenarios.json`);

const workers = Array.from({ length: load }, () => new Worker('for (;;) {}', { eval: true }));
const server = spawn(process.execPath, [path.join(copy, 'web/serve.mjs'), path.join(copy, 'dist/web-dev'), String(port)], { stdio: 'ignore' });
await new Promise(resolve => setTimeout(resolve, 500));
const executablePath = executables[browserName].find(p => p && fs.existsSync(p));
const browser = await puppeteer.launch({ browser: browserName, executablePath, headless: !args.includes('--headed'), timeout: 90_000,
  args: browserName === 'chrome' ? ['--no-first-run', '--window-size=1024,768'] : ['--width=1024', '--height=768'] });
const lines = [];
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 960, height: 600 });
  page.on('console', m => { const t = m.text(); if (t.startsWith('T0')) lines.push(t); });
  const query = new URLSearchParams({ ...scenario.env, ...extra, script, autostart: '1' });
  await page.goto(`http://127.0.0.1:${port}/${game}/?${query}`, { waitUntil: 'domcontentloaded' });
  const end = Date.now() + 900_000;
  let state;
  for (;;) {
    state = await page.evaluate(() => window.microengine?.state ?? 'loading');
    if (state === 'ended' || state === 'failed') break;
    if (Date.now() > end) throw new Error(`timed out in state ${state}`);
    await new Promise(resolve => setTimeout(resolve, 200));
  }
  await new Promise(resolve => setTimeout(resolve, 500)); // last console messages
  if (captures) {
    fs.rmSync(captures, { recursive: true, force: true });
    fs.mkdirSync(captures, { recursive: true });
    for (const name of await page.evaluate(() => window.microengine.captures())) {
      const data = await page.evaluate(n => window.microengine.capture(n), name);
      fs.writeFileSync(path.join(captures, name), Buffer.from(data, 'base64'));
    }
  }
  fs.mkdirSync(path.dirname(out), { recursive: true });
  fs.writeFileSync(out, lines.join('\n') + '\n');
  console.log(`${game} ${script} in ${browserName}${load ? ` (load ${load})` : ''}: ${state}, ${lines.filter(l => l.startsWith('T0 ')).length} steps traced -> ${out}`);
} finally {
  await browser.close();
  server.kill();
  for (const w of workers) await w.terminate();
}

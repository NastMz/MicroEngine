#!/usr/bin/env node
// Builds the Box2D backend's browser tests with the pinned web toolchain (web/setup.sh: .NET with wasm-tools) and runs
// them in headless Chrome through puppeteer-core (installed by `npm ci --prefix web/tests`). Exit status 1 when a test
// failed, none ran, or the page did not finish.
//
//   node tests/MicroEngine.Physics.Box2D.Tests/Browser/run.mjs [--out <dir>] [--no-build] [--headed] [--timeout <seconds>]
//
// Needs: artifacts/box2d/browser-wasm (src/MicroEngine.Physics.Box2D/native/build.sh browser-wasm), the web toolchain
// ($MICROENGINE_WEB_TOOLS, default ~/.microengine/web-tools) and Chrome ($CHROME_PATH or the usual install locations).
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { execFileSync, spawn } from 'node:child_process';
import { createRequire } from 'node:module';
import { fileURLToPath, pathToFileURL } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const repo = path.resolve(here, '..', '..', '..');
const args = process.argv.slice(2);
const value = name => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : undefined; };
const out = path.resolve(value('--out') ?? fs.mkdtempSync(path.join(os.tmpdir(), 'box2d-browser-')));
const timeout = Number(value('--timeout') ?? 600) * 1000;

if (!args.includes('--no-build')) {
  const tools = process.env.MICROENGINE_WEB_TOOLS ?? path.join(os.homedir(), '.microengine', 'web-tools');
  const dotnet = path.join(tools, 'dotnet', process.platform === 'win32' ? 'dotnet.exe' : 'dotnet');
  if (!fs.existsSync(dotnet)) { console.error(`error: no web toolchain at ${tools}: run web/setup.sh.`); process.exit(1); }
  const env = { ...process.env, DOTNET_ROOT: path.dirname(dotnet), DOTNET_MULTILEVEL_LOOKUP: '0', DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1' };
  console.log(`building into ${out}`);
  execFileSync(dotnet, ['publish', path.join(here, 'MicroEngine.Physics.Box2D.Browser.csproj'), '-c', 'Release', '-nologo', '-v', 'q',
    '--artifacts-path', path.join(out, 'artifacts'), '-o', path.join(out, 'site')], { stdio: 'inherit', env, cwd: repo });
}
const site = path.join(out, 'site', 'wwwroot');
if (!fs.existsSync(path.join(site, 'index.html'))) { console.error(`error: ${site} has no index.html`); process.exit(1); }

const port = 8300 + Math.floor(Math.random() * 600);
const server = spawn(process.execPath, [path.join(repo, 'web', 'serve.mjs'), site, String(port)], { stdio: 'ignore' });
const require = createRequire(path.join(repo, 'web', 'tests', 'package.json'));
const { default: puppeteer } = await import(pathToFileURL(require.resolve('puppeteer-core')).href);
const chrome = [process.env.CHROME_PATH, 'C:/Program Files/Google/Chrome/Application/chrome.exe', '/usr/bin/google-chrome',
  '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'].find(p => p && fs.existsSync(p));
if (!chrome) { console.error('error: Chrome not found; set CHROME_PATH.'); server.kill(); process.exit(1); }

let status = 1;
const browser = await puppeteer.launch({ executablePath: chrome, headless: !args.includes('--headed'), args: ['--no-first-run'], timeout: 90_000 });
try {
  const page = await browser.newPage();
  const errors = [];
  page.on('pageerror', e => errors.push(String(e.message).slice(0, 2000)));
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text().slice(0, 2000)); });
  console.log(`# ${await browser.version()}`);
  await new Promise(resolve => setTimeout(resolve, 500));
  await page.goto(`http://127.0.0.1:${port}/`);
  await page.waitForFunction(() => window.box2d && window.box2d.done, { timeout, polling: 500 });
  const r = await page.evaluate(() => window.box2d);
  console.log(r.lines.join('\n'));
  for (const e of errors) console.log(`page: ${e}`);
  status = !r.error && r.failed === 0 && r.passed > 0 ? 0 : 1;
} catch (e) {
  console.log(`error: ${e.message}`);
} finally {
  await browser.close();
  server.kill();
}
process.exit(status);

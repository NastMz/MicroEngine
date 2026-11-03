// ARCH-1 P5: runs the web probe in Chrome (puppeteer-core) for each configuration and prints one JSON line per run.
//   PUPPETEER_DIR=<dir with puppeteer-core> node measure-web.mjs <base-url> [runs] [--headed] [--firefox] [config ...]
// A config is mode:targetMs[:framesHint], e.g. sdl-pull:0 sdl-pull:0:256 sdl-push:150 worklet:40 worklet:150.
// The page is started by a real (CDP) click, so audio starts under the default gesture-required autoplay policy.
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { createRequire } from 'node:module';

const args = process.argv.slice(2);
const [base, runsArg] = args.filter(a => !a.startsWith('--') && !a.includes(':') || /^https?:/.test(a));
const configs = args.filter(a => /^(sdl-pull|sdl-push|worklet):/.test(a));
const runs = Number(runsArg ?? 1);
const dir = process.env.PUPPETEER_DIR;
if (!base || !dir) { console.error('usage: PUPPETEER_DIR=<dir> node measure-web.mjs <base-url> [runs] [config ...]'); process.exit(2); }
const { default: puppeteer } = await import(pathToFileURL(createRequire(path.join(dir, 'x.js')).resolve('puppeteer-core')).href);
const firefox = args.includes('--firefox');
const chrome = [process.env.CHROME_PATH, 'C:/Program Files/Google/Chrome/Application/chrome.exe', '/usr/bin/google-chrome'].find(p => p && fs.existsSync(p));
const list = configs.length ? configs : ['sdl-pull:0', 'sdl-pull:0:256', 'sdl-push:60', 'sdl-push:150', 'sdl-push:400', 'worklet:40', 'worklet:150', 'worklet:400'];

for (const config of list) {
  const [mode, target, frames = '0'] = config.split(':');
  for (let i = 0; i < runs; i++) {
    const browser = firefox
      ? await puppeteer.launch({ browser: 'firefox', executablePath: process.env.FIREFOX_PATH ?? 'C:/Program Files/Mozilla Firefox/firefox.exe', headless: !args.includes('--headed') })
      : await puppeteer.launch({ executablePath: chrome, headless: !args.includes('--headed'), args: ['--autoplay-policy=user-gesture-required', '--no-first-run'] });
    const errors = [];
    try {
      const page = await browser.newPage();
      page.on('pageerror', e => errors.push(String(e).slice(0, 500)));
      await page.goto(`${base}?mode=${mode}&target=${target}&frames=${frames}`);
      await page.waitForFunction('window.page && window.page.ready', { timeout: 60000 });
      await page.mouse.click(100, 100);
      await page.waitForFunction('window.result || window.page.error', { timeout: 60000, polling: 250 });
      const out = await page.evaluate(() => window.result ?? { error: window.page.error });
      out.browser = await browser.version();
      out.errors = errors;
      console.log(JSON.stringify(out));
    } catch (e) {
      console.log(JSON.stringify({ config, error: String(e), errors }));
    } finally {
      await browser.close();
    }
  }
}

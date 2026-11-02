// ARCH-1 P4: does the game see keyboard and touch input, and does the canvas follow the page size? Needs a
// development package (`microengine package web --dev`), whose page keeps a per-frame trace of the scene, the input
// actions that are down and the touch controls ("touch=shown:1" = drawn, one finger down).
//   PUPPETEER_DIR=<dir> node input.mjs <url>
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { createRequire } from 'node:module';

const url = process.argv[2];
const { default: puppeteer } = await import(pathToFileURL(createRequire(path.join(process.env.PUPPETEER_DIR, 'x.js')).resolve('puppeteer-core')).href);
const chrome = [process.env.CHROME_PATH, 'C:/Program Files/Google/Chrome/Application/chrome.exe', '/usr/bin/google-chrome'].find(p => p && fs.existsSync(p));
const browser = await puppeteer.launch({ executablePath: chrome, headless: true, args: ['--autoplay-policy=user-gesture-required', '--window-size=1024,768'] });
const out = { url, errors: [] };
const wait = ms => new Promise(r => setTimeout(r, ms));
try {
  const page = await browser.newPage();
  page.on('pageerror', e => out.errors.push(String(e).slice(0, 300)));
  await page.setViewport({ width: 1024, height: 768 });
  await page.goto(url, { waitUntil: 'load' });
  await page.waitForFunction("document.documentElement.dataset.state === 'ready'", { timeout: 120000 });
  await page.mouse.click(512, 300);
  await page.waitForFunction("document.documentElement.dataset.state === 'playing'", { timeout: 60000 });
  await wait(1500);
  const traced = () => page.evaluate(() => window.microengine.traced());
  const since = n => page.evaluate(k => window.microengine.trace(window.microengine.traced() - k), n);
  // Keyboard: hold Right for 300 ms; frames from keydown until the first frame whose trace shows the action.
  let n0 = await traced();
  await page.keyboard.down('ArrowRight');
  await wait(300);
  await page.keyboard.up('ArrowRight');
  await wait(200);
  let t = await since(n0);
  out.keyRight = { framesUntilSeen: t.findIndex(s => / \S*right\S* /.test(` ${s.split(' ')[1] ?? ''} `)), sample: t.find(s => /right/.test(s)) ?? t[t.length - 1] };
  // Touch: one finger held on the left of the canvas for 300 ms, then released.
  const cdp = await page.createCDPSession();
  n0 = await traced();
  await cdp.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x: 120, y: 600, id: 1 }] });
  await wait(300);
  t = await since(n0);
  out.touchDown = { framesUntilSeen: t.findIndex(s => /touch=\w+:1/.test(s)), sample: t.find(s => /touch=\w+:1/.test(s)) ?? t[t.length - 1] };
  await cdp.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
  await wait(300);
  out.touchUp = (await since(1))[0];
  out.controls = (await page.evaluate(() => window.microengine.controls())).split('\n').slice(-1)[0];
  // Page resize: does the canvas backing store follow?
  out.canvasBefore = await page.evaluate(() => [canvas.width, canvas.height, canvas.clientWidth, canvas.clientHeight].join('x'));
  await page.setViewport({ width: 640, height: 480 });
  await wait(500);
  out.canvasAfterResize = await page.evaluate(() => [canvas.width, canvas.height, canvas.clientWidth, canvas.clientHeight].join('x'));
  out.controlsAfterResize = (await page.evaluate(() => window.microengine.controls())).split('\n').slice(-1)[0];
  // Hiding the page: SDL sends SDL_EVENT_WINDOW_HIDDEN; raylib's host suspends through WebHost.Visibility.
  out.state = await page.evaluate(() => document.documentElement.dataset.state);
} catch (e) {
  out.errors.push(String(e).slice(0, 300));
} finally {
  await browser.close();
}
console.log(JSON.stringify(out, null, 2));

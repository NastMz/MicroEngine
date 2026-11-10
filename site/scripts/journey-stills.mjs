#!/usr/bin/env node
// Takes the stills of the landing's journey: one picture of each chapter's scene, in both themes, for readers who get
// the plain chapters instead of the live journey (reduced motion, no WebGL, no JavaScript).
//
//   npm run build && node scripts/journey-stills.mjs
//
// It serves the built site, opens the landing with ?journey=still (the picture centred, no words over it), moves the
// journey to each chapter's moment and saves the canvas to src/assets/journey/<chapter>-<theme>.png. Run it again when
// the scene (src/scripts/journey/scene.ts) changes, then rebuild.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer-core';
import { serve } from '../tests/serve-dist.mjs';

const out = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../src/assets/journey');
// The progress at which each chapter's scene is at its clearest.
const moments = { kernel: 0.04, pieces: 0.262, game: 0.325, games: 0.425, loop: 0.75, mark: 0.96 };
const executable = [process.env.CHROME_PATH, 'C:/Program Files/Google/Chrome/Application/chrome.exe', '/usr/bin/google-chrome',
  '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'].find(candidate => candidate && fs.existsSync(candidate));
if (!executable) throw new Error('Chrome not found; set CHROME_PATH.');

fs.mkdirSync(out, { recursive: true });
const server = await serve();
const browser = await puppeteer.launch({ browser: 'chrome', executablePath: executable, headless: true, args: ['--enable-unsafe-swiftshader', '--ignore-gpu-blocklist'] });
try {
  for (const theme of ['light', 'dark']) {
    const page = await browser.newPage();
    await page.setViewport({ width: 1120, height: 630 });
    await page.emulateMediaFeatures([{ name: 'prefers-color-scheme', value: theme }]);
    await page.goto(`${server.url}/?journey=still`, { waitUntil: 'networkidle0' });
    if (!await page.evaluate(() => document.documentElement.classList.contains('journey-live'))) throw new Error('The journey did not start: no WebGL in this Chrome?');
    await page.addStyleTag({ content: '.journey .chapter, .page-tape, .site-header { visibility: hidden !important; }' });
    for (const [chapter, progress] of Object.entries(moments)) {
      await page.evaluate(p => { const root = document.querySelector('[data-journey]'); scrollTo(0, root.offsetTop + p * (root.offsetHeight - innerHeight)); }, progress);
      await new Promise(resolve => setTimeout(resolve, 1800));
      await page.screenshot({ path: path.join(out, `${chapter}-${theme}.png`) });
      console.log(`${chapter}-${theme}.png`);
    }
    await page.close();
  }
} finally {
  await browser.close();
  await server.close();
}

// ARCH-1 P6: loads the browser harness in headless Chrome (puppeteer-core) and prints what the page reports.
//   PUPPETEER_DIR=<dir with puppeteer-core> node measure.mjs <url> [timeout-seconds=600] [--headed]
// The page runs the contract checks and the benchmark itself (Web/wwwroot/main.js); this only waits and collects.
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { createRequire } from 'node:module';

const [url, timeoutArg] = process.argv.slice(2).filter(a => !a.startsWith('--'));
const dir = process.env.PUPPETEER_DIR;
if (!url || !dir) { console.error('usage: PUPPETEER_DIR=<dir> node measure.mjs <url> [timeout-seconds]'); process.exit(2); }
const { default: puppeteer } = await import(pathToFileURL(createRequire(path.join(dir, 'x.js')).resolve('puppeteer-core')).href);
const chrome = [process.env.CHROME_PATH, 'C:/Program Files/Google/Chrome/Application/chrome.exe', '/usr/bin/google-chrome']
  .find(p => p && fs.existsSync(p));
const browser = await puppeteer.launch({ executablePath: chrome, headless: !process.argv.includes('--headed'), args: ['--no-first-run'] });
const errors = [];
try {
  const page = await browser.newPage();
  page.on('pageerror', e => errors.push(String(e.message).slice(0, 2000)));
  page.on('console', m => { if (m.type() === 'error' || m.text().startsWith('raw:')) errors.push(m.text().slice(0, 2000)); });
  console.log(`# ${await browser.version()} ${url}`);
  await page.goto(url);
  await page.waitForFunction(() => window.p6 && window.p6.done, { timeout: Number(timeoutArg ?? 600) * 1000, polling: 500 });
  const r = await page.evaluate(() => window.p6);
  console.log(r.lines.join('\n'));
  if (r.error) console.log('ERROR ' + r.error);
} catch (e) {
  console.log('measure error: ' + e.message);
  try { console.log((await (await browser.pages())[1]?.evaluate(() => window.p6?.lines?.join('\n'))) ?? ''); } catch { }
} finally {
  for (const e of errors) console.log('page: ' + e);
  await browser.close();
}

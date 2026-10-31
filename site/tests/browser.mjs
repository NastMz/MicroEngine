#!/usr/bin/env node
// Browser checks for the built site (site/dist, with demo packages), in the installed Chrome and Firefox:
//   node tests/browser.mjs [--browsers chrome,firefox] [--out test-results]
// - every page at desktop, tablet and phone widths: no horizontal scroll, images with alt text, no request to a
//   demo package (/play/) until Play is pressed; full-page screenshots are written to --out for review;
// - each demo page on desktop: Play creates the iframe, the host's messages drive the status (ready, playing),
//   Restart reloads, Exit removes the game; the size shown before loading is honoured;
// - phone and tablet with touch (Chrome device emulation, not a real device): the "Made for a keyboard" panel shows
//   instead of the Play button, "Try anyway" still loads the game.
import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer-core';
import { serve } from './serve-dist.mjs';

const args = process.argv.slice(2);
const option = (name, fallback) => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : fallback; };
const browsers = option('--browsers', 'chrome,firefox').split(',');
const out = path.resolve(option('--out', 'test-results'));
fs.mkdirSync(out, { recursive: true });

const executables = {
  chrome: [process.env.CHROME_PATH, 'C:/Program Files/Google/Chrome/Application/chrome.exe', '/usr/bin/google-chrome', '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'],
  firefox: [process.env.FIREFOX_PATH, 'C:/Program Files/Mozilla Firefox/firefox.exe', '/usr/bin/firefox', '/Applications/Firefox.app/Contents/MacOS/firefox'],
};
const games = [
  ['meadow-run', 'MeadowRun'], ['courtyard-key', 'CourtyardKey'], ['trail-duel', 'TrailDuel'], ['neon-rift', 'NeonRift'],
];
const pages = ['/', '/demos/', ...games.map(([slug]) => `/demos/${slug}/`), '/roadmap/', '/docs/', '/docs/getting-started/', '/docs/manual/scenes/', '/docs/reference/contracts/', '/docs/credits/neon-rift/', '/no-such-page/'];
const viewports = {
  desktop: { width: 1280, height: 800 },
  tablet: { width: 768, height: 1024, isMobile: true, hasTouch: true },
  phone: { width: 375, height: 812, isMobile: true, hasTouch: true },
  small: { width: 320, height: 640, isMobile: true, hasTouch: true },
};

let failures = 0;
function check(name, ok, detail = '') {
  if (!ok) failures++;
  console.log(`${ok ? 'ok  ' : 'FAIL'} ${name}${detail ? ` — ${detail}` : ''}`);
}

async function launch(name) {
  const executablePath = executables[name].find(p => p && fs.existsSync(p));
  if (!executablePath) throw new Error(`${name} not found; set ${name.toUpperCase()}_PATH.`);
  return puppeteer.launch(name === 'chrome'
    ? { browser: 'chrome', executablePath, headless: true, args: ['--no-first-run', '--autoplay-policy=user-gesture-required'] }
    : { browser: 'firefox', executablePath, headless: true });
}

async function openPage(browser, label, url, viewport) {
  const context = await browser.createBrowserContext();
  const page = await context.newPage();
  // Firefox (WebDriver BiDi) has no touch or mobile emulation: it gets the size only.
  await page.setViewport(label === 'chrome' ? viewport : { width: viewport.width, height: viewport.height });
  const requests = [];
  page.on('request', request => requests.push(new URL(request.url()).pathname));
  const errors = [];
  page.on('pageerror', error => errors.push(String(error)));
  const response = await page.goto(url, { waitUntil: 'load' });
  return { page, context, requests, errors, response };
}

async function layout(browser, label, base) {
  const sizes = label === 'chrome' ? Object.keys(viewports) : ['desktop', 'phone'];
  for (const size of sizes) {
    for (const route of pages) {
      const { page, context, requests, errors, response } = await openPage(browser, label, base + route, viewports[size]);
      const where = `${label} ${size} ${route}`;
      if (route === '/no-such-page/') check(`${where}: unknown route answers 404 with the site page`, response.status() === 404 && (await page.title()).includes('not found'));
      const { scroll, width, clipped, missingAlt, smallTargets, repoLinks, roadmapLink } = await page.evaluate(() => ({
        repoLinks: [...document.querySelectorAll('a[href]')].map(a => a.href).filter(h => /github\.com/i.test(h)),
        roadmapLink: !!document.querySelector('header a[href="/roadmap/"], .sidebar-content a[href="/roadmap/"], #starlight__sidebar a[href="/roadmap/"], a.docs-section[href="/roadmap/"]'),
        scroll: document.documentElement.scrollWidth,
        width: window.innerWidth,
        // overflow-x: clip on the page hides overflow instead of scrolling, so look for content that ends past the
        // viewport edge, outside elements that scroll horizontally on purpose (code blocks, wide tables).
        clipped: [...document.body.querySelectorAll('*')].filter(el => {
          const rect = el.getBoundingClientRect();
          if (!rect.width || rect.right <= window.innerWidth + 1) return false;
          for (let p = el.parentElement; p && p !== document.body; p = p.parentElement) {
            if (/auto|scroll|hidden/.test(getComputedStyle(p).overflowX)) return false;
          }
          return getComputedStyle(el).position !== 'fixed';
        }).slice(0, 3).map(el => `${el.tagName.toLowerCase()}.${el.className}`.slice(0, 60)),
        missingAlt: [...document.images].filter(img => !img.hasAttribute('alt')).map(img => img.src),
        // Visible links and buttons in the page's own chrome and controls that are smaller than 24px on a side.
        smallTargets: [...document.querySelectorAll('header a, footer a, .button, button')]
          .filter(el => el.offsetParent && el.getClientRects().length)
          .map(el => [el.textContent.trim().slice(0, 30), el.getBoundingClientRect()])
          .filter(([, r]) => r.height < 24 || r.width < 24).map(([t]) => t),
      }));
      check(`${where}: no horizontal scroll`, scroll <= width, `${scroll} > ${width}`);
      check(`${where}: no content cut off at the right edge`, clipped.length === 0, clipped.join(' | '));
      check(`${where}: every image has alt text`, missingAlt.length === 0, missingAlt.join(', '));
      check(`${where}: no links to GitHub (the repository is private)`, repoLinks.length === 0, repoLinks.slice(0, 3).join(', '));
      check(`${where}: the roadmap is reachable from the navigation`, roadmapLink);
      check(`${where}: controls at least 24px`, smallTargets.length === 0, smallTargets.join(' | '));
      check(`${where}: no demo package requested`, !requests.some(p => p.startsWith('/play/')), requests.filter(p => p.startsWith('/play/')).slice(0, 3).join(', '));
      check(`${where}: no script errors`, errors.length === 0, errors.join(' | '));
      if (size === 'desktop' && (route === '/' || route === '/demos/neon-rift/')) {
        await page.keyboard.press('Tab');
        const skip = await page.evaluate(() => {
          const el = document.activeElement;
          return { skip: el?.classList.contains('skip'), outline: getComputedStyle(el).outlineStyle, top: el.getBoundingClientRect().top };
        });
        check(`${where}: Tab reaches a visible skip link first`, skip.skip && skip.outline !== 'none' && skip.top >= 0, JSON.stringify(skip));
        await page.keyboard.press('Enter');
        await page.keyboard.press('Tab');
        const next = await page.evaluate(() => document.activeElement?.closest('main') !== null);
        check(`${where}: the skip link moves focus into the main content`, next);
      }
      if (size !== 'tablet') await page.screenshot({ path: path.join(out, `${label}-${size}-${route.replace(/\W+/g, '_') || 'home'}.png`), fullPage: true });
      await context.close();
    }
  }
}

const stateOf = page => page.$eval('[data-player]', el => el.dataset.state);
const statusOf = page => page.$eval('[data-status]', el => el.textContent);
const waitState = (page, states, timeout = 90_000) =>
  page.waitForFunction(s => s.includes(document.querySelector('[data-player]').dataset.state), { timeout }, states);

async function play(browser, label, base) {
  for (const [slug, id] of games) {
    const { page, context, requests, errors } = await openPage(browser, label, `${base}/demos/${slug}/`, viewports.desktop);
    const where = `${label} desktop ${slug}`;
    try {
      check(`${where}: desktop offers Play`, await stateOf(page) === 'desktop');
      const idleControls = await page.$$eval('.toolbar .button', buttons => buttons.filter(b => b.getClientRects().length).map(b => b.textContent));
      check(`${where}: game controls stay hidden until a game is loaded`, idleControls.length === 0, idleControls.join(', '));
      check(`${where}: nothing from the package before Play`, !requests.some(p => p.startsWith(`/play/`)));
      await page.click('[data-panel="desktop"] [data-action="load"]');
      await waitState(page, ['ready']);
      check(`${where}: host reported ready`, (await statusOf(page)).startsWith('Ready'), await statusOf(page));
      check(`${where}: package loaded on demand`, requests.some(p => p.startsWith(`/play/${id}/_framework/`)));
      check(`${where}: other games not downloaded`, !requests.some(p => p.startsWith('/play/') && !p.startsWith(`/play/${id}/`)));
      const frame = await (await page.$('iframe.game-frame')).contentFrame();
      await frame.click('#play');
      await waitState(page, ['playing']);
      check(`${where}: host reported playing`, true);
      await page.screenshot({ path: path.join(out, `${label}-play-${slug}.png`) });
      const visible = await page.$eval('[data-action="fullscreen"]', el => !el.hidden);
      check(`${where}: fullscreen control offered`, visible === (await page.evaluate(() => document.fullscreenEnabled)));
      await page.click('.toolbar [data-action="restart"]');
      await waitState(page, ['ready']);
      check(`${where}: restart reloads the game`, true);
      await page.click('.toolbar [data-action="exit"]');
      await page.waitForFunction(() => !document.querySelector('iframe.game-frame'), { timeout: 5000 });
      check(`${where}: exit removes the game`, await stateOf(page) === 'desktop');
      check(`${where}: no script errors`, errors.length === 0, errors.join(' | '));
    } catch (error) {
      check(`${where}: demo flow`, false, `${error.message}; state ${await stateOf(page).catch(() => '?')}`);
    }
    await context.close();
  }
}

async function touch(browser, base) {
  for (const size of ['phone', 'tablet']) {
    const { page, context } = await openPage(browser, 'chrome', `${base}/demos/meadow-run/`, viewports[size]);
    const where = `chrome ${size} (emulated touch) meadow-run`;
    check(`${where}: keyboard notice instead of Play`, await stateOf(page) === 'unvalidated');
    check(`${where}: alternatives offered`, await page.$eval('[data-panel="unvalidated"]', el => !!el.querySelector('a[href="#screenshots"]')));
    await page.click('[data-panel="unvalidated"] [data-action="load"]');
    await waitState(page, ['ready']).then(() => check(`${where}: "Try anyway" still loads the game`, true),
      error => check(`${where}: "Try anyway" still loads the game`, false, error.message));
    await page.screenshot({ path: path.join(out, `chrome-${size}-try-anyway.png`) });
    await context.close();
  }
}

const server = await serve();
console.log(`site served at ${server.url}; screenshots in ${out}`);
try {
  for (const label of browsers) {
    const browser = await launch(label);
    try {
      await layout(browser, label, server.url);
      await play(browser, label, server.url);
      if (label === 'chrome') await touch(browser, server.url);
    } finally {
      await browser.close();
    }
  }
} finally {
  await server.close();
}
console.log(failures ? `${failures} check(s) failed` : 'all checks passed');
process.exitCode = failures ? 1 : 0;

#!/usr/bin/env node
// Browser checks for the built site (site/dist, with demo packages), in the installed Chrome and Firefox:
//   node tests/browser.mjs [--browsers chrome,firefox] [--suites layout,play,touch] [--out test-results] [--concurrency 4]
// Browsers run at the same time; layout checks up to --concurrency pages at once per browser.
// Suites (all by default; CI runs only `layout` when a change touches documentation alone):
// - layout: every page at desktop, tablet and phone widths: no horizontal scroll, images with alt text, no request to a
//   demo package (/play/) until Play is pressed; full-page screenshots are written to --out for review;
// - play: each demo page on desktop: Play creates the iframe, the host's messages drive the status (ready, playing),
//   Restart reloads, Exit removes the game; the size shown before loading is honoured;
// - touch: phone and tablet with touch (Chrome device emulation, not a real device): the "Made for a keyboard" panel shows
//   instead of the Play button, "Try anyway" still loads the game;
// - layout, pages with diagrams: every diagram is drawn (an SVG, no Mermaid error) in the documentation's colours.
import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer-core';
import { serve } from './serve-dist.mjs';

const args = process.argv.slice(2);
const option = (name, fallback) => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : fallback; };
const browsers = option('--browsers', 'chrome,firefox').split(',');
const suites = new Set(option('--suites', 'layout,play,touch').split(','));
for (const suite of suites) if (!['layout', 'play', 'touch'].includes(suite)) throw new Error(`unknown suite '${suite}' (layout, play, touch)`);
const out = path.resolve(option('--out', 'test-results'));
fs.mkdirSync(out, { recursive: true });

const executables = {
  chrome: [process.env.CHROME_PATH, 'C:/Program Files/Google/Chrome/Application/chrome.exe', '/usr/bin/google-chrome', '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'],
  firefox: [process.env.FIREFOX_PATH, 'C:/Program Files/Mozilla Firefox/firefox.exe', '/usr/bin/firefox', '/Applications/Firefox.app/Contents/MacOS/firefox'],
};
const games = [
  ['meadow-run', 'MeadowRun'], ['courtyard-key', 'CourtyardKey'], ['trail-duel', 'TrailDuel'], ['neon-rift', 'NeonRift'],
];
const pages = ['/', '/demos/', ...games.map(([slug]) => `/demos/${slug}/`), '/roadmap/', '/docs/', '/docs/getting-started/', '/docs/architecture/', '/docs/manual/scenes/', '/docs/reference/components/', '/docs/reference/contracts/', '/docs/credits/neon-rift/', '/no-such-page/'];
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
    // Without a GPU (CI runners), Firefox only offers WebGL when told to accept software rendering, and on Linux only
    // with a display: CI runs the checks under xvfb-run with HEADED=1.
    : { browser: 'firefox', executablePath, headless: process.env.HEADED !== '1',
        extraPrefsFirefox: { 'webgl.force-enabled': true, 'webgl.disable-fail-if-major-performance-caveat': true } });
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

/** Runs fn over the items with at most `limit` at a time. */
async function pool(items, limit, fn) {
  const queue = [...items];
  await Promise.all(Array.from({ length: Math.min(limit, queue.length) }, async () => {
    while (queue.length) await fn(queue.shift());
  }));
}

// Pages are independent browser contexts, so several are checked at once. Checks that press keys need the window's
// focus and run afterwards, one at a time.
const concurrency = Number(option('--concurrency', '4'));
const usesKeyboard = (size, route) => size === 'desktop' && (route === '/' || route === '/demos/neon-rift/');

async function layout(browser, label, base) {
  const sizes = label === 'chrome' ? Object.keys(viewports) : ['desktop', 'phone'];
  const items = sizes.flatMap(size => pages.map(route => [size, route]));
  await pool(items.filter(([size, route]) => !usesKeyboard(size, route)), concurrency, item => layoutPage(browser, label, base, ...item));
  for (const item of items.filter(([size, route]) => usesKeyboard(size, route))) await layoutPage(browser, label, base, ...item);
}

async function layoutPage(browser, label, base, size, route) {
  const { page, context, requests, errors, response } = await openPage(browser, label, base + route, viewports[size]);
  const where = `${label} ${size} ${route}`;
  if (route === '/no-such-page/') check(`${where}: unknown route answers 404 with the site page`, response.status() === 404 && (await page.title()).includes('not found'));
  if (await page.$('pre.mermaid')) {
    const drawn = await page.waitForFunction(() => [...document.querySelectorAll('pre.mermaid')].every(p => p.dataset.processed), { timeout: 20_000 })
      .then(() => true, () => false);
    const diagrams = await page.evaluate(() => {
      // A node's fill must be the theme's diagram colour, not Mermaid's own palette.
      const probe = document.createElement('div');
      probe.style.color = 'var(--diagram-node)';
      document.body.append(probe);
      const expected = getComputedStyle(probe).color;
      probe.remove();
      return [...document.querySelectorAll('pre.mermaid')].map(p => {
        const node = p.querySelector('.node rect, .node polygon');
        return { svg: !!p.querySelector('svg'), error: /error/i.test(p.textContent) && !p.querySelector('svg'),
          themed: !!node && getComputedStyle(node).fill === expected };
      });
    });
    check(`${where}: every diagram is drawn`, drawn && diagrams.every(d => d.svg && !d.error), JSON.stringify(diagrams));
    check(`${where}: diagrams use the documentation's colours`, diagrams.every(d => d.themed), JSON.stringify(diagrams));
  }
  const { scroll, width, clipped, missingAlt, smallTargets, repoLinks, roadmapLink } = await page.evaluate(() => ({
    repoLinks: [...document.querySelectorAll('a[href]')].map(a => a.href).filter(h => /github\.com/i.test(h) && !/^https:\/\/github\.com\/NastMz\/MicroEngine(\/|$)/.test(h)),
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
  check(`${where}: GitHub links go to the MicroEngine repository only`, repoLinks.length === 0, repoLinks.slice(0, 3).join(', '));
  check(`${where}: the roadmap is reachable from the navigation`, roadmapLink);
  check(`${where}: controls at least 24px`, smallTargets.length === 0, smallTargets.join(' | '));
  check(`${where}: no demo package requested`, !requests.some(p => p.startsWith('/play/')), requests.filter(p => p.startsWith('/play/')).slice(0, 3).join(', '));
  check(`${where}: no script errors`, errors.length === 0, errors.join(' | '));
  if (usesKeyboard(size, route)) {
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

const stateOf = page => page.$eval('[data-player]', el => el.dataset.state);
const statusOf = page => page.$eval('[data-status]', el => el.textContent);
const waitState = (page, states, timeout = 90_000) =>
  page.waitForFunction(s => s.includes(document.querySelector('[data-player]').dataset.state), { timeout }, states);

async function play(browser, label, base) {
  for (const [slug, id] of games) {
    const { page, context, requests, errors } = await openPage(browser, label, `${base}/demos/${slug}/`, viewports.desktop);
    const where = `${label} desktop ${slug}`;
    try {
      // Headless browsers on a server may report no mouse; a desktop visitor's key press is the product's own
      // signal for "keyboard present" (capability.mjs), so the check uses it when needed.
      if (await stateOf(page) === 'unvalidated') await page.keyboard.press('Shift');
      check(`${where}: desktop offers Play`, await stateOf(page) === 'desktop', await stateOf(page));
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
  // The browsers are independent processes reading the same local server: they run at the same time.
  await Promise.all(browsers.map(async label => {
    const browser = await launch(label);
    try {
      if (suites.has('layout')) await layout(browser, label, server.url);
      if (suites.has('play')) await play(browser, label, server.url);
      if (suites.has('touch') && label === 'chrome') await touch(browser, server.url);
    } finally {
      await browser.close();
    }
  }));
} finally {
  await server.close();
}
console.log(failures ? `${failures} check(s) failed` : 'all checks passed');
process.exitCode = failures ? 1 : 0;

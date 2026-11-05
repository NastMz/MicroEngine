// Browser checks for MicroEngine web packages, run in the installed Chrome and Firefox through puppeteer-core.
//
//   node web/tests/run.mjs [suite ...] [--packages DIR] [--browsers chrome,firefox] [--games A,B] [--out DIR] [--headed]
//
// Suites (default: platform scripts):
//   platform  per game: loading, start by gesture, audio under a blocking autoplay policy, 60 updates/s, held,
//             released and very short key presses, focus loss, browser keys, saves across reload, corrupt and
//             blocked storage, end and restart without reloading (the storage checks use Neon Rift's and Meadow
//             Run's saves and run only when --games includes them)
//   scripts   replays the games' own input scripts (scenarios.json) and stores their captures in --out, to
//             compare with desktop captures (desktop.mjs with the same --out compares them byte for byte)
//   repeat    replays each script twice in fresh pages and checks that both runs write byte-identical captures
//             (scripted runs are deterministic in pages as on desktop); differing pairs go to --out/<browser>/repeat
//   autopilot Neon Rift's built-in autopilot plays stages 1 to 3 until the ending
//   audio     per game: the sound actually reaching the speakers (signal level of the page's audio output); a hidden page
//             suspends its sound and resumes it when shown
//   framerate per game: updates per second while the page draws every frame, every 2nd and every 4th
//   keyboard  replays the same scripts through the browser's keyboard (real key events at the scripted update
//             numbers) and checks that the game reaches the scene the scripted run ends in; page screenshots
//             at the script's capture points go to --out for review
//   gamepad   per game: an emulated standard-mapping gamepad (Gamepad API): d-pad and left stick held and released,
//             unplugging releases what it held
//
// Needs development packages (web/build.sh --dev --out dist/web-dev) of the games selected. Browsers are found in
// their default install folders or through CHROME_PATH and FIREFOX_PATH. Exits 1 when any check failed. Daily CI
// runs it on the games a change reaches (.github/workflows/checks.yml, browser-engine; docs/ci.md).
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
const browserNames = option('--browsers', 'chrome,firefox').split(',');
const gameFilter = option('--games', '');
const out = path.resolve(option('--out', path.join(repo, 'dist/web-tests')));
const headed = flag('--headed');
const suites = args.length ? args : ['platform', 'scripts'];
const port = Number(option('--port', '8093'));
const base = `http://127.0.0.1:${port}`;
const allGames = ['MeadowRun', 'CourtyardKey', 'TrailDuel', 'NeonRift'];
const games = gameFilter ? gameFilter.split(',') : allGames;

const executables = {
  chrome: [process.env.CHROME_PATH, 'C:/Program Files/Google/Chrome/Application/chrome.exe', '/usr/bin/google-chrome', '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'],
  firefox: [process.env.FIREFOX_PATH, 'C:/Program Files/Mozilla Firefox/firefox.exe', '/usr/bin/firefox', '/Applications/Firefox.app/Contents/MacOS/firefox'],
};

let failures = 0;
const results = [];
function check(name, ok, detail = '') {
  results.push({ name, ok, detail });
  if (!ok) failures++;
  console.log(`${ok ? 'PASS' : 'FAIL'} ${name}${detail ? ` (${detail})` : ''}`);
}
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

async function launch(name) {
  const executablePath = executables[name].find(p => p && fs.existsSync(p));
  if (!executablePath) throw new Error(`${name} not found; set ${name.toUpperCase()}_PATH.`);
  // Both browsers are told to require a user gesture before audio plays, as they do for real visitors. CHROME_FLAGS
  // adds Chrome switches (CI: --ignore-gpu-blocklist, so a headed Chrome on Xvfb draws WebGL with Mesa's llvmpipe,
  // the desktop build's renderer, instead of refusing WebGL on a software GPU).
  const chromeFlags = (process.env.CHROME_FLAGS ?? '').split(/\s+/).filter(Boolean);
  const options = name === 'chrome'
    ? { browser: 'chrome', executablePath, headless: !headed, args: ['--autoplay-policy=user-gesture-required', '--no-first-run', '--window-size=1024,768', ...chromeFlags] }
    : { browser: 'firefox', executablePath, headless: !headed, args: ['--width=1024', '--height=768'],
        extraPrefsFirefox: { 'media.autoplay.default': 1, 'media.autoplay.block-webaudio': true } };
  // Starting a browser on a busy CI runner occasionally misses puppeteer's default 30 s for the browser to print its
  // endpoint (a known flake of the site job, docs/ci.md "Troubleshooting"). The launch alone, never a check, gets a
  // longer timeout and one retry; the retry prints the browser's own output, so a real failure says why.
  try {
    return await puppeteer.launch({ ...options, timeout: 90_000 });
  } catch (e) {
    console.log(`note ${name} did not start (${e.message.split('\n')[0]}); retrying once with its output shown`);
    return puppeteer.launch({ ...options, timeout: 90_000, dumpio: true });
  }
}

/** The renderer behind the browser's WebGL (which GPU or software rasterizer draws the frames), or why there is none. */
async function webglRenderer(browser) {
  const page = await browser.newPage();
  try {
    return await page.evaluate(() => {
      const gl = document.createElement('canvas').getContext('webgl2') ?? document.createElement('canvas').getContext('webgl');
      if (!gl) return 'unavailable';
      const info = gl.getExtension('WEBGL_debug_renderer_info');
      return info ? gl.getParameter(info.UNMASKED_RENDERER_WEBGL) : gl.getParameter(gl.RENDERER);
    });
  } catch (e) {
    return `unknown (${e.message})`;
  } finally {
    await page.close();
  }
}

/** Opens a game in a fresh profile and waits until it is ready to play. */
async function open(browser, game, query = '', context, whileLoading) {
  context ??= await browser.createBrowserContext();
  const page = await context.newPage();
  await page.setViewport({ width: 960, height: 600 });
  const errors = [];
  page.on('pageerror', e => errors.push(String(e)));
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
  await page.goto(`${base}/${game}/${query}`, { waitUntil: 'domcontentloaded' });
  await whileLoading?.(page);
  await waitFor(page, s => s === 'ready' || s === 'failed' || s === 'playing', 120_000);
  return { page, context, errors };
}

async function waitFor(page, predicate, timeout = 30_000) {
  const end = Date.now() + timeout;
  for (;;) {
    const state = await page.evaluate(() => window.microengine?.state ?? 'loading');
    if (predicate(state)) return state;
    if (Date.now() > end) throw new Error(`timed out in state ${state}`);
    await sleep(100);
  }
}

const probe = page => page.evaluate(() => window.microengine.trace(1)[0] ?? '');
const recent = (page, frames) => page.evaluate(n => window.microengine.trace(n), frames);
const mark = page => page.evaluate(() => window.microengine.traced());
/**
 * Frames traced since a mark (browsers may draw far more than 60 frames per second), read in one call inside the page.
 * Counting with traced() and then reading trace(n) in a second call lets frames arrive in between: the window then
 * slides past the oldest frames, which are the ones that show a short press (it is down for one frame only). None
 * when no frame was drawn since (trace(0) would return the whole trace).
 */
const since = (page, from) => page.evaluate(start => {
  const count = Math.min(2000, window.microengine.traced() - start);
  return count > 0 ? window.microengine.trace(count) : [];
}, from);
const actionsIn = entry => (entry.split(' ')[1] ?? '').split(',').filter(Boolean);
const stats = async page => (await page.evaluate(() => window.microengine.stats())).split(' ').map(Number);
const updates = async page => (await stats(page))[0];
/**
 * Waits until the game has run at least `count` fixed updates and drawn `count` frames after `from` (stats() taken
 * before the action under test), or `timeout` ms. A check that reads the frames after an action needs the game to have
 * run, not a fixed time to have passed: a loaded runner (llvmpipe, two vCPUs) can draw no frame for hundreds of
 * milliseconds. Returns what was waited for, for the check's detail.
 */
async function progress(page, [u0, f0], count = 3, timeout = 5000) {
  const start = Date.now();
  for (;;) {
    const [u, f] = await stats(page);
    if ((u - u0 >= count && f - f0 >= count) || Date.now() - start > timeout) return `${u - u0} updates, ${f - f0} frames in ${Date.now() - start} ms`;
    await sleep(50);
  }
}

async function platform(browser, label, game) {
  const name = `${label} ${game}`;
  // A key pressed and held while the page loads must not reach the game once it starts.
  const { page, context, errors } = await open(browser, game, '', undefined, p => p.keyboard.down('ArrowRight'));
  check(`${name}: loads to the ready screen`, await page.evaluate(() => window.microengine.state) === 'ready');
  check(`${name}: no audio before the player starts`, (await page.evaluate(() => window.microengine.audio())).length === 0);
  await page.click('#play');
  await waitFor(page, s => s === 'playing');
  await sleep(400);
  const leaked = (await since(page, 0)).some(e => actionsIn(e).includes('right'));
  await page.keyboard.up('ArrowRight');
  check(`${name}: a key held while loading is not seen by the game`, !leaked);
  await sleep(1500);
  const audio = await page.evaluate(() => window.microengine.audio());
  check(`${name}: audio runs after the starting click`, audio.length > 0 && audio.every(s => s === 'running'), audio.join(','));

  const u0 = await updates(page), t0 = Date.now();
  await sleep(3000);
  const rate = (await updates(page) - u0) / ((Date.now() - t0) / 1000);
  check(`${name}: about 60 updates per second`, rate > 54 && rate < 66, rate.toFixed(1));

  await page.keyboard.down('ArrowLeft');
  await sleep(300);
  const held = actionsIn(await probe(page)).includes('left');
  await page.keyboard.up('ArrowLeft');
  await sleep(300);
  const released = !actionsIn(await probe(page)).includes('left');
  check(`${name}: held and released keys`, held && released, `held ${held}, released ${released}`);

  // Key down and up back to back, with no frame between them: the game must still see the press in an update.
  const before = await mark(page), counters = await stats(page);
  await page.keyboard.press('ArrowRight');
  const waited = await progress(page, counters);
  const frames = await since(page, before);
  check(`${name}: a press shorter than a frame is seen`, frames.some(e => actionsIn(e).includes('right')),
    `${waited}; ${frames.length} frames traced${frames.length ? `, last: ${frames.at(-1)}` : ''}`);

  await page.keyboard.down('ArrowUp');
  await sleep(300);
  const upHeld = actionsIn(await probe(page)).includes('up');
  await page.evaluate(() => window.dispatchEvent(new FocusEvent('blur')));
  await sleep(300);
  const after = actionsIn(await probe(page)).includes('up');
  await page.keyboard.up('ArrowUp');
  check(`${name}: losing focus releases held keys`, upHeld && !after, `before ${upHeld}, after ${after}`);

  const prevented = await page.evaluate(async () => {
    const seen = {};
    const listen = e => { seen[e.key] = e.defaultPrevented; };
    window.addEventListener('keydown', listen);
    for (const key of [' ', 'PageDown', 'ArrowDown', '/', "'", 'Tab']) window.dispatchEvent(new KeyboardEvent('keydown', { key, cancelable: true }));
    window.removeEventListener('keydown', listen);
    return { seen, scroll: document.scrollingElement.scrollTop };
  });
  check(`${name}: game keys do not scroll or open browser features`,
    Object.values(prevented.seen).every(Boolean) && prevented.scroll === 0, JSON.stringify(prevented.seen));

  // Fullscreen through the page's button, and back with Alt+Enter (both are user gestures).
  await page.click('#fullscreen');
  await sleep(800);
  const entered = await page.evaluate(() => ({ on: !!document.fullscreenElement, w: innerWidth, canvas: document.getElementById('canvas').width }));
  await page.keyboard.down('Alt'); await page.keyboard.press('Enter'); await page.keyboard.up('Alt');
  await sleep(800);
  const left = await page.evaluate(() => !document.fullscreenElement);
  check(`${name}: fullscreen button and Alt+Enter`, entered.on && entered.canvas === entered.w && left, JSON.stringify(entered));

  // Saves: the page's storage is the game's localStorage namespace; reloading keeps it.
  await page.evaluate(id => localStorage.setItem(`microengine/${id}/web-test`, '{"kept":true}'), game);
  await page.reload();
  await waitFor(page, s => s === 'ready');
  const kept = await page.evaluate(id => localStorage.getItem(`microengine/${id}/web-test`), game);
  check(`${name}: storage survives a reload`, kept === '{"kept":true}');

  // End and restart without reloading: a short script quits the game; Play again starts a new run.
  await page.close();
  const quitter = { MeadowRun: 'smoke.script', CourtyardKey: 'look.script', TrailDuel: 'title.script', NeonRift: 'menus.script' }[game];
  const run = await open(browser, game, `?script=${quitter}`, context);
  await run.page.click('#play');
  const ended = await waitFor(run.page, s => s === 'ended' || s === 'failed', 120_000);
  const canvasHidden = await run.page.evaluate(() => getComputedStyle(document.getElementById('canvas')).visibility === 'hidden');
  check(`${name}: quitting shows the end screen, not the last frame`, ended === 'ended' && canvasHidden);
  await run.page.click('#play');
  await waitFor(run.page, s => s === 'playing' || s === 'ended' || s === 'failed');
  await sleep(1000);
  const restarted = await updates(run.page);
  check(`${name}: plays again without reloading`, restarted > 30 && (await run.page.evaluate(() => window.microengine.state)) !== 'failed', `${restarted} updates`);
  check(`${name}: no page errors`, errors.length + run.errors.length === 0, [...errors, ...run.errors].join(' | ').slice(0, 300));
  const keys = await run.page.evaluate(() => Object.keys(localStorage));
  check(`${name}: saves stay in the game's own namespace`, keys.every(k => k.startsWith(`microengine/${game}/`)), keys.join(', '));
  await context.close();
}

/** Corrupt saves are set aside, blocked or full storage leaves the game running and says so. */
async function storage(browser, label) {
  // Each check uses one game's saves; with --games, only the checks of the games selected (their packages) run.
  if (games.includes('NeonRift')) {
    const context = await browser.createBrowserContext();
    const page = await context.newPage();
    await page.goto(`${base}/NeonRift/`);
    await page.evaluate(() => localStorage.setItem('microengine/NeonRift/achievements', '{ not json'));
    await page.reload();
    await waitFor(page, s => s === 'ready');
    await page.click('#play');
    await waitFor(page, s => s === 'playing');
    await sleep(1000);
    const kept = await page.evaluate(() => localStorage.getItem('microengine/NeonRift/achievements.corrupt'));
    check(`${label} storage: corrupt data is kept aside and the game runs`, kept === '{ not json' && await updates(page) > 0);
    await context.close();
  }
  if (games.includes('MeadowRun')) {
    const context = await browser.createBrowserContext();
    const page = await context.newPage();
    await page.goto(`${base}/MeadowRun/`);
    await waitFor(page, s => s === 'ready');
    // Fill the origin's quota, then play Meadow Run's smoke script, which starts a new game (a save) and
    // walks into level 1: saving must fail visibly without stopping the game.
    await page.evaluate(() => {
      const chunk = 'x'.repeat(256 * 1024);
      try { for (let i = 0; i < 400; i++) localStorage.setItem(`filler/${i}`, chunk); } catch { /* full */ }
      try { for (let i = 0; i < 4096; i++) localStorage.setItem(`filler/small/${i}`, 'x'.repeat(1024)); } catch { /* full */ }
    });
    await page.goto(`${base}/MeadowRun/?script=smoke.script&autostart=1`);
    const state = await waitFor(page, s => s === 'ended' || s === 'failed', 120_000);
    const notice = await page.evaluate(() => document.getElementById('notice').hidden ? '' : document.getElementById('notice').textContent);
    const saved = await page.evaluate(() => localStorage.getItem('microengine/MeadowRun/progress'));
    check(`${label} storage: a full quota is reported and the game keeps running`,
      state === 'ended' && saved === null && /storage is full/.test(notice), `${state}; ${notice || 'no notice'}`);
    await page.evaluate(() => { for (const k of Object.keys(localStorage)) if (k.startsWith('filler/')) localStorage.removeItem(k); });
    await context.close();
  }
}

/** Firefox with site storage disabled: the game still runs, keeps progress for the session and says so. */
async function blockedStorage() {
  const executablePath = executables.firefox.find(p => p && fs.existsSync(p));
  const browser = await puppeteer.launch({ browser: 'firefox', executablePath, headless: !headed,
    extraPrefsFirefox: { 'dom.storage.enabled': false } });
  try {
    const page = await browser.newPage();
    await page.goto(`${base}/MeadowRun/?script=smoke.script&autostart=1`);
    const state = await waitFor(page, s => s === 'ended' || s === 'failed', 120_000);
    const notice = await page.evaluate(() => document.getElementById('notice').hidden ? '' : document.getElementById('notice').textContent);
    check('firefox storage: blocked site storage is reported and the game runs', state === 'ended' && /blocks site storage/.test(notice), `${state}; ${notice || 'no notice'}`);
  } finally {
    await browser.close();
  }
}

async function scripts(browser, label) {
  const { scenarios } = JSON.parse(fs.readFileSync(path.join(here, 'scenarios.json'), 'utf8'));
  for (const s of scenarios.filter(s => games.includes(s.game))) {
    const name = `${label} ${s.game} ${s.script}`;
    const query = new URLSearchParams({ ...s.env, script: s.script, autostart: '1' });
    const { page, context, errors } = await open(browser, s.game, `?${query}`);
    const state = await waitFor(page, st => st === 'ended' || st === 'failed', 600_000);
    const captures = await page.evaluate(() => window.microengine.captures());
    const dir = path.join(out, label, s.game, path.basename(s.script, '.script'));
    fs.rmSync(dir, { recursive: true, force: true });
    fs.mkdirSync(dir, { recursive: true });
    for (const capture of captures) {
      const data = await page.evaluate(n => window.microengine.capture(n), capture);
      fs.writeFileSync(path.join(dir, capture), Buffer.from(data, 'base64'));
    }
    check(`${name}: runs to its quit`, state === 'ended' && errors.length === 0, `${captures.length} captures${errors.length ? `; ${errors[0]}` : ''}`);
    await context.close();
  }
}

// One scripted run in a fresh page: whether it reached its quit, and its captures by name.
async function scriptedRun(browser, s) {
  const query = new URLSearchParams({ ...s.env, script: s.script, autostart: '1' });
  const { page, context, errors } = await open(browser, s.game, `?${query}`);
  try {
    const state = await waitFor(page, st => st === 'ended' || st === 'failed', 600_000);
    const captures = new Map();
    for (const capture of await page.evaluate(() => window.microengine.captures()))
      captures.set(capture, Buffer.from(await page.evaluate(n => window.microengine.capture(n), capture), 'base64'));
    return { ended: state === 'ended' && errors.length === 0, error: errors[0], captures };
  } finally {
    await context.close();
  }
}

// Every scripted run is deterministic in a page as on desktop (one fixed step per frame, seed 1, the same launcher
// model), so two runs of the same package and script must write byte-identical captures. The second run's differing
// captures are kept in --out for inspection.
async function repeat(browser, label) {
  const { scenarios } = JSON.parse(fs.readFileSync(path.join(here, 'scenarios.json'), 'utf8'));
  for (const s of scenarios.filter(s => games.includes(s.game))) {
    const name = `${label} ${s.game} ${s.script}`;
    const [first, second] = [await scriptedRun(browser, s), await scriptedRun(browser, s)];
    const ended = first.ended && second.ended;
    check(`${name}: both runs reach their quit`, ended, first.error ?? second.error ?? '');
    if (!ended) continue;
    const names = [...first.captures.keys()].sort();
    const same = names.length === second.captures.size && names.every(n => second.captures.has(n));
    const differing = names.filter(n => second.captures.has(n) && !first.captures.get(n).equals(second.captures.get(n)));
    if (differing.length) {
      const dir = path.join(out, label, 'repeat', s.game, path.basename(s.script, '.script'));
      fs.rmSync(dir, { recursive: true, force: true });
      fs.mkdirSync(dir, { recursive: true });
      for (const n of differing) {
        fs.writeFileSync(path.join(dir, `run1-${n}`), first.captures.get(n));
        fs.writeFileSync(path.join(dir, `run2-${n}`), second.captures.get(n));
      }
    }
    check(`${name}: two runs write byte-identical captures`, same && differing.length === 0 && names.length > 0,
      `${names.length - differing.length}/${names.length} identical${same ? '' : `; run 2 wrote ${[...second.captures.keys()].sort().join(', ')}`}${differing.length ? `; differing: ${differing.join(', ')}` : ''}`);
  }
}

// The key a player would press for each scripted action (the first binding that no other action shares).
const keyFor = { left: 'ArrowLeft', right: 'ArrowRight', up: 'ArrowUp', down: 'ArrowDown', confirm: 'Enter', cancel: 'Backspace',
  action: 'KeyC', pause: 'Escape', jump: 'KeyZ', run: 'KeyX', fire: 'KeyX', lock: 'KeyC' };

/** Script edges as { update, key, down } plus capture and quit updates (the InputScript format). */
function parseScript(text) {
  const edges = [], captures = [];
  let cursor = 0, quit = Infinity;
  for (const raw of text.split('\n')) {
    const line = raw.split('#')[0].trim();
    if (!line) continue;
    const [at, command, a, b] = line.split(/\s+/);
    const update = at.startsWith('@') ? cursor + Number(at.slice(2)) : Number(at);
    cursor = update;
    if (command === 'press' || command === 'hold') {
      edges.push({ update, key: keyFor[a], down: true });
      edges.push({ update: update + Math.max(1, command === 'press' ? 2 : Number(b)), key: keyFor[a], down: false });
    } else if (command === 'down' || command === 'up') edges.push({ update, key: keyFor[a], down: command === 'down' });
    else if (command === 'capture') captures.push({ update, name: a });
    else if (command === 'quit') quit = update;
  }
  return { edges: edges.sort((x, y) => x.update - y.update), captures, quit };
}

async function keyboard(browser, label) {
  const { scenarios } = JSON.parse(fs.readFileSync(path.join(here, 'scenarios.json'), 'utf8'));
  for (const s of scenarios.filter(s => games.includes(s.game) && s.keyboard)) {
    const name = `${label} ${s.game} ${s.script} by keyboard`;
    const text = fs.readFileSync(path.join(repo, 'games', s.game, 'scripts', s.script), 'utf8');
    const { edges, captures, quit } = parseScript(text);
    // Where the scripted run ends, to compare with.
    const scripted = await open(browser, s.game, `?${new URLSearchParams({ ...s.env, script: s.script, autostart: '1' })}`);
    await waitFor(scripted.page, st => st === 'ended' || st === 'failed', 600_000);
    const expected = (await recent(scripted.page, 50)).filter(Boolean).at(-1)?.split(' ')[0];
    await scripted.context.close();

    const { page, context, errors } = await open(browser, s.game, `?${new URLSearchParams(s.env)}`);
    await page.click('#play');
    await waitFor(page, st => st === 'playing');
    const dir = path.join(out, 'keyboard', label, s.game, path.basename(s.script, '.script'));
    fs.rmSync(dir, { recursive: true, force: true });
    fs.mkdirSync(dir, { recursive: true });
    let e = 0, c = 0, lag = 0;
    for (;;) {
      const now = await updates(page);
      while (e < edges.length && edges[e].update <= now) {
        lag = Math.max(lag, now - edges[e].update);
        await (edges[e].down ? page.keyboard.down(edges[e].key) : page.keyboard.up(edges[e].key));
        e++;
      }
      while (c < captures.length && captures[c].update <= now) await page.screenshot({ path: path.join(dir, captures[c++].name) });
      if (now >= quit || await page.evaluate(() => window.microengine.state) !== 'playing') break;
      await sleep(4);
    }
    const reached = (await probe(page)).split(' ')[0];
    check(`${name}: reaches ${expected}`, reached === expected && errors.length === 0, `${reached}; keys up to ${lag} updates late`);
    await context.close();
  }
}

/** Measures the level of what the game sends to the speakers: music and effects share the engine's one output node. */
async function audio(browser, label) {
  for (const game of games) {
    const context = await browser.createBrowserContext();
    const page = await context.newPage();
    // Tap every node that connects to the speakers with an analyser, before the game creates its audio.
    await page.evaluateOnNewDocument(() => {
      const connect = AudioNode.prototype.connect;
      window.__levels = [];
      AudioNode.prototype.connect = function (target, ...rest) {
        if (target instanceof AudioDestinationNode) {
          const analyser = this.context.createAnalyser();
          analyser.fftSize = 2048;
          connect.call(this, analyser);
          const data = new Float32Array(analyser.fftSize);
          setInterval(() => {
            analyser.getFloatTimeDomainData(data);
            window.__levels.push(Math.sqrt(data.reduce((sum, v) => sum + v * v, 0) / data.length));
          }, 50);
        }
        return connect.call(this, target, ...rest);
      };
    });
    await page.goto(`${base}/${game}/`);
    await waitFor(page, s => s === 'ready', 120_000);
    await page.click('#play');
    await waitFor(page, s => s === 'playing');
    const started = Date.now();
    await sleep(3000);
    // Menus play an effect when the cursor moves. Title music may start later: songs are rendered in the background,
    // which takes longer on a slow machine; the check waits for the first sound and says when it came.
    await page.keyboard.press('ArrowDown');
    let peak = 0;
    while (Date.now() - started < 15_000) {
      await sleep(250);
      peak = Math.max(0, ...await page.evaluate(() => window.__levels));
      if (peak > 0.005) break;
    }
    const after = ((Date.now() - started) / 1000).toFixed(1);
    check(`${label} ${game}: sound reaches the output`, peak > 0.005, `peak RMS ${peak.toFixed(3)}, ${peak > 0.005 ? `heard within ${after} s of the start` : `nothing in ${after} s`}`);
    await context.close();
  }
  // When the browser holds audio suspended (no gesture yet, or a policy change), the page must say so and the
  // next click must bring the sound back. The test suspends the context itself: whether a browser blocks audio
  // under automation varies (Chrome over CDP counts navigation as user activation). Neon Rift, or the game selected.
  const { page, context } = await open(browser, games.includes('NeonRift') ? 'NeonRift' : games[0]);
  await page.click('#play');
  await waitFor(page, s => s === 'playing');
  await sleep(1000);
  await page.evaluate(() => window.microengine.suspendAudio());
  await sleep(1200);
  const state = () => page.evaluate(() => ({ audio: window.microengine.audio(), button: !document.getElementById('sound').hidden }));
  const blocked = await state();
  await page.click('#sound');
  await sleep(1200);
  const resumed = await state();
  check(`${label} audio: suspended sound is explained and one click brings it back`,
    blocked.button && blocked.audio.includes('suspended') && !resumed.button && resumed.audio.every(a => a === 'running'),
    `${JSON.stringify(blocked)} -> ${JSON.stringify(resumed)}`);

  // A hidden page (another tab, a locked phone) stops drawing frames, so nothing fills the audio queue: the page
  // suspends the output and resumes it when shown again. Visibility is emulated here, as headless browsers stay visible.
  const visibility = hidden => page.evaluate(h => {
    Object.defineProperty(document, 'visibilityState', { configurable: true, get: () => h ? 'hidden' : 'visible' });
    document.dispatchEvent(new Event('visibilitychange'));
  }, hidden);
  await visibility(true);
  await sleep(800);
  const hiddenAudio = await page.evaluate(() => window.microengine.audio());
  await visibility(false);
  await sleep(800);
  const shownAudio = await page.evaluate(() => window.microengine.audio());
  check(`${label} audio: a hidden page suspends its sound and resumes it when shown`,
    hiddenAudio.length > 0 && hiddenAudio.every(a => a === 'suspended') && shownAudio.every(a => a === 'running'),
    `${hiddenAudio.join(',')} -> ${shownAudio.join(',')}`);
  await context.close();
}

/** The simulation keeps 60 updates per second whatever the page's frame rate (down to the 5-steps cap). */
async function framerate(browser, label) {
  for (const game of games) {
    const { page, context } = await open(browser, game);
    await page.click('#play');
    await waitFor(page, s => s === 'playing');
    await sleep(1500);
    for (const every of [1, 2, 4]) {
      // Drop frames by answering only every Nth animation frame request.
      await page.evaluate(n => {
        const native = window.__nativeRaf ??= window.requestAnimationFrame.bind(window);
        let count = 0;
        window.requestAnimationFrame = cb => native(function tick(t) { if (++count % n === 0) cb(t); else native(tick); });
      }, every);
      await sleep(500);
      const [u0, f0] = (await page.evaluate(() => window.microengine.stats())).split(' ').map(Number), t0 = Date.now();
      await sleep(3000);
      const [u1, f1] = (await page.evaluate(() => window.microengine.stats())).split(' ').map(Number), seconds = (Date.now() - t0) / 1000;
      const ups = (u1 - u0) / seconds, fps = (f1 - f0) / seconds;
      check(`${label} ${game}: 60 updates per second at ${fps.toFixed(0)} frames per second`, ups > 54 && ups < 66, ups.toFixed(1));
    }
    await context.close();
  }
}

/**
 * A gamepad the browser reports through the Gamepad API (standard mapping), emulated in the page: navigator.getGamepads
 * returns it and a gamepadconnected event announces it, as when a player presses a button on a real pad. Per game: the
 * d-pad and the left stick move (the engine's default bindings), a release is seen, and unplugging releases what was
 * held. Real pads (vendor mappings, rumble) are not emulated.
 */
async function gamepad(browser, label) {
  for (const game of games) {
    const name = `${label} ${game}`;
    const { page, context, errors } = await open(browser, game);
    await page.click('#play');
    await waitFor(page, s => s === 'playing');
    await sleep(1000);
    await page.evaluate(() => {
      const pad = {
        id: 'Emulated Pad (STANDARD GAMEPAD Vendor: 045e Product: 028e)', index: 0, connected: true, mapping: 'standard',
        timestamp: performance.now(), axes: [0, 0, 0, 0], buttons: Array.from({ length: 17 }, () => ({ pressed: false, touched: false, value: 0 })),
      };
      window.__pad = pad;
      window.__pads = [pad, null, null, null];
      Object.defineProperty(navigator, 'getGamepads', { configurable: true, value: () => window.__pads });
      window.__padSet = (button, down) => {
        pad.buttons[button] = { pressed: down, touched: down, value: down ? 1 : 0 };
        pad.timestamp = performance.now();
      };
      window.__padAxis = (axis, value) => { pad.axes[axis] = value; pad.timestamp = performance.now(); };
      window.__padEvent = type => {
        const event = new Event(type);
        Object.defineProperty(event, 'gamepad', { value: pad });
        window.dispatchEvent(event);
      };
      window.__padEvent('gamepadconnected');
    });
    await sleep(300);
    // A real stick at rest reports a little noise, which SDL takes as each axis's resting value; an axis that never
    // moved before would have its first movement taken as that value instead.
    await page.evaluate(() => [0.004, -0.003, 0.002, -0.001].forEach((v, i) => window.__padAxis(i, v)));
    await sleep(300);
    const holds = async (action, set, clear) => {
      await page.evaluate(set);
      await sleep(400);
      const held = actionsIn(await probe(page)).includes(action);
      await page.evaluate(clear);
      await sleep(400);
      return { held, released: !actionsIn(await probe(page)).includes(action) };
    };
    const dpad = await holds('right', () => window.__padSet(15, true), () => window.__padSet(15, false));
    check(`${name}: a gamepad's d-pad is held and released`, dpad.held && dpad.released, JSON.stringify(dpad));
    const stick = await holds('left', () => window.__padAxis(0, -1), () => window.__padAxis(0, 0));
    check(`${name}: a gamepad's left stick moves`, stick.held && stick.released, JSON.stringify(stick));
    await page.evaluate(() => window.__padSet(14, true));
    await sleep(400);
    const before = actionsIn(await probe(page)).includes('left');
    await page.evaluate(() => { window.__pad.connected = false; window.__pads = [null, null, null, null]; window.__padEvent('gamepaddisconnected'); });
    await sleep(400);
    const after = actionsIn(await probe(page)).includes('left');
    check(`${name}: unplugging a gamepad releases what it held`, before && !after, `before ${before}, after ${after}`);
    check(`${name}: no page errors with a gamepad`, errors.length === 0, errors.join(' | ').slice(0, 300));
    await context.close();
  }
}

async function autopilot(browser, label) {
  for (const stage of [1, 2, 3]) {
    // As in the desktop test (Autopilot_clears_the_stage), shots do not kill the autopilot; pits and lava still do.
    const query = new URLSearchParams({ RIFT_START: 'stage', RIFT_STAGE: String(stage), RIFT_AUTO: '1', RIFT_GOD: '1', autostart: '1' });
    const { page, context, errors } = await open(browser, 'NeonRift', `?${query}`);
    await waitFor(page, s => s === 'playing' || s === 'failed');
    // The stage is cleared when the game leaves PlayScene for the next stage card or the ending.
    const end = Date.now() + 15 * 60_000;
    let scene = '', seen = new Set();
    while (Date.now() < end) {
      scene = (await probe(page)).split(' ')[0];
      if (scene) seen.add(scene);
      if (seen.has('PlayScene') && scene && scene !== 'PlayScene') break;
      if (await page.evaluate(() => window.microengine.state) !== 'playing') break;
      await sleep(1000);
    }
    const expected = stage === 3 ? 'EndingScene' : 'StageCardScene';
    check(`${label} NeonRift autopilot: stage ${stage} is cleared`, scene === expected && errors.length === 0,
      `${scene} after ${await updates(page)} updates; scenes ${[...seen].join(' > ')}`);
    await context.close();
  }
}

if (!fs.existsSync(packages)) throw new Error(`${packages} does not exist; build with web/build.sh --dev --out ${path.relative(repo, packages)}`);
const server = spawn(process.execPath, [path.join(repo, 'web/serve.mjs'), packages, String(port)], { stdio: 'ignore' });
await sleep(500);
try {
  for (const label of browserNames) {
    const browser = await launch(label);
    console.log(`== ${label} ${await browser.version()}; WebGL: ${await webglRenderer(browser)}`);
    try {
      if (suites.includes('platform')) {
        for (const game of games) await platform(browser, label, game).catch(e => check(`${label} ${game}: platform checks`, false, e.message));
        await storage(browser, label).catch(e => check(`${label} storage checks`, false, e.message));
        if (label === 'firefox' && games.includes('MeadowRun')) await blockedStorage().catch(e => check('firefox blocked storage', false, e.message));
      }
      if (suites.includes('scripts')) await scripts(browser, label).catch(e => check(`${label} scripts`, false, e.message));
      if (suites.includes('repeat')) await repeat(browser, label).catch(e => check(`${label} repeat`, false, e.message));
      if (suites.includes('audio')) await audio(browser, label).catch(e => check(`${label} audio`, false, e.message));
      if (suites.includes('framerate')) await framerate(browser, label).catch(e => check(`${label} framerate`, false, e.message));
      if (suites.includes('keyboard')) await keyboard(browser, label).catch(e => check(`${label} keyboard`, false, e.message));
      if (suites.includes('gamepad')) await gamepad(browser, label).catch(e => check(`${label} gamepad`, false, e.message));
      if (suites.includes('autopilot')) await autopilot(browser, label).catch(e => check(`${label} autopilot`, false, e.message));
    } finally {
      await browser.close();
    }
  }
} finally {
  server.kill();
}
console.log(`${results.length - failures} passed, ${failures} failed`);
fs.mkdirSync(out, { recursive: true });
fs.writeFileSync(path.join(out, `results-${suites.join('-')}.json`), JSON.stringify(results, null, 2));
process.exit(failures ? 1 : 0);

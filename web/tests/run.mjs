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
//   autopilot Neon Rift's built-in autopilot plays stages 1 to 3 until the ending
//   audio     per game: the sound actually reaching the speakers (signal level of the page's audio output)
//   framerate per game: updates per second while the page draws every frame, every 2nd and every 4th
//   keyboard  replays the same scripts through the browser's keyboard (real key events at the scripted update
//             numbers) and checks that the game reaches the scene the scripted run ends in; page screenshots
//             at the script's capture points go to --out for review
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
  // Both browsers are told to require a user gesture before audio plays, as they do for real visitors.
  return puppeteer.launch(name === 'chrome'
    ? { browser: 'chrome', executablePath, headless: !headed, args: ['--autoplay-policy=user-gesture-required', '--no-first-run', '--window-size=1024,768'] }
    : { browser: 'firefox', executablePath, headless: !headed, args: ['--width=1024', '--height=768'],
        extraPrefsFirefox: { 'media.autoplay.default': 1, 'media.autoplay.block-webaudio': true } });
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
/** Frames traced since a mark (browsers may draw far more than 60 frames per second). */
const since = async (page, from) => recent(page, Math.min(2000, await mark(page) - from));
const actionsIn = entry => (entry.split(' ')[1] ?? '').split(',').filter(Boolean);
const updates = async page => Number((await page.evaluate(() => window.microengine.stats())).split(' ')[0]);

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

  const before = await mark(page);
  await page.keyboard.press('ArrowRight');
  await sleep(400);
  check(`${name}: a press shorter than a frame is seen`, (await since(page, before)).some(e => actionsIn(e).includes('right')));

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

/** Measures the level of what the game sends to the speakers: music and effects share miniaudio's output node. */
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
    await sleep(3000);
    // Menus play an effect when the cursor moves.
    await page.keyboard.press('ArrowDown');
    await sleep(1500);
    const levels = await page.evaluate(() => window.__levels.slice(-60));
    const peak = Math.max(0, ...levels);
    check(`${label} ${game}: sound reaches the output`, peak > 0.005, `peak RMS ${peak.toFixed(3)}`);
    await context.close();
  }
  // When the browser holds audio suspended (no gesture yet, or a policy change), the page must say so and the
  // next click must bring the sound back. The test suspends the context itself: whether a browser blocks audio
  // under automation varies (Chrome over CDP counts navigation as user activation). Neon Rift, or the game selected.
  const { page, context } = await open(browser, games.includes('NeonRift') ? 'NeonRift' : games[0]);
  await page.click('#play');
  await waitFor(page, s => s === 'playing');
  await sleep(1000);
  await page.evaluate(() => Promise.all(window.miniaudio.devices.map(d => d?.webaudio?.suspend())));
  await sleep(1200);
  const state = () => page.evaluate(() => ({ audio: window.microengine.audio(), button: !document.getElementById('sound').hidden }));
  const blocked = await state();
  await page.click('#sound');
  await sleep(1200);
  const resumed = await state();
  check(`${label} audio: suspended sound is explained and one click brings it back`,
    blocked.button && blocked.audio.includes('suspended') && !resumed.button && resumed.audio.every(a => a === 'running'),
    `${JSON.stringify(blocked)} -> ${JSON.stringify(resumed)}`);
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
    console.log(`== ${label} ${await browser.version()}`);
    try {
      if (suites.includes('platform')) {
        for (const game of games) await platform(browser, label, game).catch(e => check(`${label} ${game}: platform checks`, false, e.message));
        await storage(browser, label).catch(e => check(`${label} storage checks`, false, e.message));
        if (label === 'firefox' && games.includes('MeadowRun')) await blockedStorage().catch(e => check('firefox blocked storage', false, e.message));
      }
      if (suites.includes('scripts')) await scripts(browser, label).catch(e => check(`${label} scripts`, false, e.message));
      if (suites.includes('audio')) await audio(browser, label).catch(e => check(`${label} audio`, false, e.message));
      if (suites.includes('framerate')) await framerate(browser, label).catch(e => check(`${label} framerate`, false, e.message));
      if (suites.includes('keyboard')) await keyboard(browser, label).catch(e => check(`${label} keyboard`, false, e.message));
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

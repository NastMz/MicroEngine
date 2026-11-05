// Mobile browser checks for the four web packages: real multi-touch input on emulated phones and tablets.
//
//   node web/tests/mobile.mjs [--packages DIR] [--games A,B] [--devices a,b] [--out DIR] [--headed]
//
// Chrome (or Chromium) emulates each device (viewport, density, touch, mobile user agent) and receives touches through
// the DevTools protocol (Input.dispatchTouchEvent): the browser turns them into the same touch and Pointer Events a
// phone's touch screen produces, with several fingers at once. Each check reads what reached the game from the
// development trace (scene, actions down, the touch controls' state and the scene's own description, such as the
// player's position) and fails unless the game did what the touches asked: walk, jump, aim, open a menu.
//
// Devices: android (Pixel 7 class, landscape), iphone (iPhone 14 class, landscape, notch insets; emulated in Chromium,
// this is NOT Safari), portrait (a small phone held upright), tablet (iPad class). Screenshots of every device with the
// controls on screen go to --out for review.
//
// Needs development packages (web/build.sh --dev --out dist/web-dev). The browser is CHROME_PATH, Google Chrome or
// the Chromium that Playwright installs.
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
const out = path.resolve(option('--out', path.join(repo, 'dist/web-mobile')));
const games = option('--games', 'MeadowRun,CourtyardKey,TrailDuel,NeonRift').split(',');
const deviceNames = option('--devices', 'android,iphone,portrait,tablet').split(',');
const headed = flag('--headed');
const port = Number(option('--port', '8094'));
const base = `http://127.0.0.1:${port}`;

const android = 'Mozilla/5.0 (Linux; Android 14; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Mobile Safari/537.36';
const ios = 'Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1';
const devices = {
  android: { viewport: { width: 915, height: 412, deviceScaleFactor: 2.625, isMobile: true, hasTouch: true, isLandscape: true }, userAgent: android },
  iphone: { viewport: { width: 844, height: 390, deviceScaleFactor: 3, isMobile: true, hasTouch: true, isLandscape: true }, userAgent: ios,
    safeArea: { top: 0, left: 47, bottom: 21, right: 47 } },
  portrait: { viewport: { width: 360, height: 640, deviceScaleFactor: 3, isMobile: true, hasTouch: true, isLandscape: false }, userAgent: android },
  tablet: { viewport: { width: 1024, height: 768, deviceScaleFactor: 2, isMobile: true, hasTouch: true, isLandscape: true }, userAgent: ios },
};
// How each game is reached for the gameplay checks (development packages read UPPER_CASE URL parameters as the
// desktop developer shortcuts), and the touch control that confirms its menus.
const setup = {
  MeadowRun: { play: '?MEADOW_START=level', confirm: 'jump', playScene: 'PlayScene' },
  CourtyardKey: { play: '?COURTYARD_START=road', confirm: 'confirm', playScene: 'PlayScene' },
  TrailDuel: { play: '?TRAIL_DEBUG=town', confirm: 'confirm', playScene: 'OverworldScene' },
  NeonRift: { play: '?RIFT_START=stage&RIFT_GOD=1', confirm: 'jump', playScene: 'PlayScene' },
};

let failures = 0;
const results = [];
function check(name, ok, detail = '') {
  results.push({ name, ok: !!ok, detail });
  if (!ok) failures++;
  console.log(`${ok ? 'PASS' : 'FAIL'} ${name}${detail ? ` (${detail})` : ''}`);
}
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

/** Trace entry: "Scene actions touch=shown:2 description". */
function parse(entry = '') {
  const [scene = '', actions = '', touch = '', ...rest] = entry.split(' ');
  const description = rest.join(' ');
  const number = key => { const m = description.match(new RegExp(`${key}=(-?[\\d.]+)`)); return m ? Number(m[1]) : NaN; };
  return { scene, actions: actions.split(',').filter(Boolean), shown: touch.startsWith('touch=shown'), fingers: Number(touch.split(':')[1] ?? 0),
    description, x: number('x'), y: number('y'), grounded: /grounded/.test(description), aim: description.match(/aim=(\S*)/)?.[1] ?? '' };
}
const trace = async (page, frames = 1) => (await page.evaluate(n => window.microengine.trace(n), frames)).map(parse);
const now = async page => (await trace(page, 1))[0] ?? parse();
const traced = page => page.evaluate(() => window.microengine.traced());
// Counted and read in one call inside the page: frames arriving between two calls would slide the window past the
// oldest frames, the ones that show a very short tap (run.mjs, since()). None when no frame was drawn since.
const since = async (page, mark) => (await page.evaluate(start => {
  const count = Math.min(2000, window.microengine.traced() - start);
  return count > 0 ? window.microengine.trace(count) : [];
}, mark)).map(parse).filter(Boolean);

/** Fingers on the touch screen, all sent together as the device would (each event lists every finger down). */
class Fingers {
  constructor(client) { this.client = client; this.points = new Map(); }
  async send(type) {
    await this.client.send('Input.dispatchTouchEvent', {
      type, touchPoints: [...this.points].map(([id, [x, y]]) => ({ x, y, id, radiusX: 10, radiusY: 10, force: 1 })),
    });
  }
  async down(id, [x, y]) { this.points.set(id, [x, y]); await this.send('touchStart'); }
  async move(id, [x, y]) { this.points.set(id, [x, y]); await this.send('touchMove'); }
  async up(id) { this.points.delete(id); await this.send('touchEnd'); }
  async upAll() { this.points.clear(); await this.send('touchEnd'); }
  async tap(id, at) { await this.down(id, at); await sleep(80); await this.up(id); }
  /** A drag in small steps, as a thumb moves. */
  async slide(id, from, to, steps = 6) {
    for (let i = 1; i <= steps; i++) await this.move(id, [from[0] + (to[0] - from[0]) * i / steps, from[1] + (to[1] - from[1]) * i / steps]);
  }
}

/** The touch controls in page (CSS) pixels: { id: { at: [x, y], r } }. */
async function controls(page) {
  const { text, scale, left, top } = await page.evaluate(() => {
    const c = document.getElementById('canvas'), r = c.getBoundingClientRect();
    return { text: window.microengine.controls(), scale: c.width / r.width, left: r.left, top: r.top };
  });
  const map = {};
  for (const line of text.split('\n').filter(Boolean)) {
    if (line.startsWith('frame ')) {
      const [, x, y, w, h] = line.split(' ').map(Number);
      map.frame = { x: left + x / scale, y: top + y / scale, w: w / scale, h: h / scale };
      continue;
    }
    const [id, x, y, r, visible] = line.split(' ');
    map[id] = { at: [left + Number(x) / scale, top + Number(y) / scale], r: Number(r) / scale, visible: visible === '1' };
  }
  return map;
}
const offset = (control, dx, dy) => [control.at[0] + dx * control.r, control.at[1] + dy * control.r];

async function waitFor(page, predicate, timeout = 30_000, what = 'condition') {
  for (const end = Date.now() + timeout; Date.now() < end; await sleep(100)) if (await predicate()) return true;
  throw new Error(`timed out waiting for ${what}`);
}

async function open(browser, device, game, query) {
  const context = await browser.createBrowserContext();
  const page = await context.newPage();
  const errors = [];
  page.on('pageerror', e => errors.push(String(e)));
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
  await page.emulate(devices[device]);
  const client = await page.createCDPSession();
  let safeArea = 'none';
  if (devices[device].safeArea) {
    try {
      await client.send('Emulation.setSafeAreaInsetsOverride', { insets: devices[device].safeArea });
      safeArea = 'emulated';
    } catch (e) { safeArea = `not supported by this browser (${e.message.split('\n')[0]})`; }
  }
  await page.goto(`${base}/${game}/${query}`, { waitUntil: 'domcontentloaded' });
  await waitFor(page, async () => ['ready', 'failed'].includes(await page.evaluate(() => window.microengine?.state)), 120_000, 'the ready screen');
  return { page, context, client, fingers: new Fingers(client), errors, safeArea };
}

/** Starts the game the way a phone user does: a tap on the Play button. */
async function start(o) {
  const box = await (await o.page.$('#play')).boundingBox();
  await o.fingers.tap(0, [box.x + box.width / 2, box.y + box.height / 2]);
  await waitFor(o.page, async () => (await o.page.evaluate(() => window.microengine.state)) === 'playing', 10_000, 'playing');
  await waitFor(o.page, async () => (await traced(o.page)) > 30, 20_000, 'frames');
}

async function screenshot(page, name) {
  fs.mkdirSync(out, { recursive: true });
  await page.screenshot({ path: path.join(out, `${name}.png`) });
}

/** Menus: the confirm control takes the game past its title by touch alone. */
async function menus(browser, device, game) {
  const label = `${device} ${game}`;
  const o = await open(browser, device, game, '');
  try {
    check(`${label}: the start screen asks for a tap`, /Tap to play/.test(await o.page.$eval('#message', e => e.textContent)));
    await start(o);
    const first = (await now(o.page)).scene;
    check(`${label}: touch controls are drawn on a touch-first device`, (await now(o.page)).shown);
    const c = await controls(o.page);
    const confirm = c[setup[game].confirm];
    for (let i = 0; i < 8 && (await now(o.page)).scene === first; i++) { await o.fingers.tap(1, confirm.at); await sleep(700); }
    const reached = (await now(o.page)).scene;
    check(`${label}: tapping the confirm control leaves the title`, reached !== first, `${first} → ${reached}`);
    const scrolled = await o.page.evaluate(() => ({ x: scrollX, y: scrollY, zoom: visualViewport?.scale ?? 1 }));
    check(`${label}: taps neither scroll nor zoom the page`, scrolled.x === 0 && scrolled.y === 0 && scrolled.zoom === 1, JSON.stringify(scrolled));
    await screenshot(o.page, `${device}-${game}-menu`);
    check(`${label}: no page errors`, o.errors.length === 0, o.errors.slice(0, 2).join(' | '));
  } finally { await o.context.close(); }
}

/** Gameplay: each game's real controls, two thumbs at once, and the game's response. */
async function gameplay(browser, device, game) {
  const label = `${device} ${game}`;
  const o = await open(browser, device, game, setup[game].play);
  const { page, fingers } = o;
  try {
    await start(o);
    await waitFor(page, async () => (await now(page)).scene === setup[game].playScene && !Number.isNaN((await now(page)).x), 30_000, setup[game].playScene);
    await sleep(1500);
    const c = await controls(page);
    const size = await page.evaluate(() => [innerWidth, innerHeight]);
    const inside = Object.entries(c).filter(([id, k]) => id !== 'frame' && k.visible).map(([, k]) => k).every(k => k.at[0] - k.r >= -1 && k.at[0] + k.r <= size[0] + 1 && k.at[1] - k.r >= -1 && k.at[1] + k.r <= size[1] + 1);
    check(`${label}: every control is on screen`, inside);
    // No control may cover the game: the frame is fitted around them (VirtualControls.Frame).
    const { frame } = c;
    const covering = Object.entries(c).filter(([id, k]) => id !== 'frame' && k.visible && (() => {
      const nx = Math.max(frame.x, Math.min(k.at[0], frame.x + frame.w)), ny = Math.max(frame.y, Math.min(k.at[1], frame.y + frame.h));
      return Math.hypot(k.at[0] - nx, k.at[1] - ny) < k.r - 1;
    })()).map(([id]) => id);
    check(`${label}: no control covers the game frame`, frame && covering.length === 0,
      `frame ${Math.round(frame?.w)}×${Math.round(frame?.h)} at ${Math.round(frame?.x)},${Math.round(frame?.y)} in ${size.join('×')}${covering.length ? `; covered by ${covering.join(', ')}` : ''}`);
    if (devices[device].safeArea && o.safeArea === 'emulated') {
      const s = devices[device].safeArea;
      const safe = Object.entries(c).filter(([id, k]) => id !== 'frame' && k.visible).map(([, k]) => k).every(k => k.at[0] - k.r >= s.left - 1 && k.at[0] + k.r <= size[0] - s.right + 1 && k.at[1] + k.r <= size[1] - s.bottom + 1);
      check(`${label}: controls keep out of the notch and home-indicator insets`, safe, JSON.stringify(s));
    } else if (devices[device].safeArea) check(`${label}: safe-area emulation`, true, `skipped: ${o.safeArea}`);

    if (game === 'MeadowRun') await meadow(page, fingers, c, label);
    if (game === 'NeonRift') await neon(page, fingers, c, label);
    if (game === 'CourtyardKey') await courtyard(page, fingers, c, label);
    if (game === 'TrailDuel') await trail(page, fingers, c, label);
    await common(o, c, label, game);
    check(`${label}: no page errors`, o.errors.length === 0, o.errors.slice(0, 2).join(' | '));
  } finally { await o.context.close(); }
}

async function meadow(page, fingers, c, label) {
  const start = await now(page);
  await fingers.down(1, offset(c.dpad, 0.8, 0));
  await sleep(700);
  const walking = await now(page);
  check(`${label}: holding right on the d-pad walks right`, walking.actions.includes('right') && walking.x > start.x + 10, `x ${start.x} → ${walking.x}`);
  await screenshot(page, `${label.replace(' ', '-')}-dpad-held`);
  const mark = await traced(page);
  await fingers.down(2, c.jump.at);
  await sleep(250);
  await screenshot(page, `${label.replace(' ', '-')}-jump-pressed`);
  await sleep(250);
  await fingers.up(2);
  await sleep(400);
  const frames = await since(page, mark);
  const both = frames.some(f => f.actions.includes('right') && f.actions.includes('jump'));
  const top = Math.min(...frames.map(f => f.y));
  check(`${label}: jump while the direction stays held (two thumbs)`, both && top < walking.y - 10 && frames.at(-1).x > walking.x + 10,
    `lowest y ${top} from ${walking.y}, x ${walking.x} → ${frames.at(-1).x}`);
  await fingers.up(1);
  await sleep(600);
  const stop = await now(page);
  await sleep(400);
  check(`${label}: lifting both thumbs stops the player`, (await now(page)).x === stop.x && stop.actions.length === 0, stop.actions.join(','));
  // A finger that starts on A and slides far away must release it (no stuck jump).
  await fingers.down(3, c.jump.at);
  await sleep(200);
  await fingers.slide(3, c.jump.at, offset(c.jump, -4, -4));
  await sleep(200);
  check(`${label}: a finger sliding far off a button releases it`, !(await now(page)).actions.includes('jump'));
  await fingers.up(3);
}

async function neon(page, fingers, c, label) {
  await fingers.down(1, c.stick.at);
  await fingers.slide(1, c.stick.at, offset(c.stick, 0.75, -0.75));
  await fingers.down(2, c.fire.at);
  await sleep(700);
  const aiming = await now(page);
  check(`${label}: joystick up-right with FIRE held shoots diagonally`, aiming.actions.includes('up') && aiming.actions.includes('right') && aiming.actions.includes('fire') && aiming.aim.split('/').includes('1,-1'),
    `${aiming.actions.join(',')} aim ${aiming.aim}`);
  await screenshot(page, `${label.replace(' ', '-')}-aim-fire`);
  const mark = await traced(page);
  await fingers.down(3, c.jump.at);
  await sleep(300);
  await fingers.up(3);
  await sleep(400);
  const frames = await since(page, mark);
  check(`${label}: a third finger jumps while aiming and firing`, frames.some(f => ['up', 'right', 'fire', 'jump'].every(a => f.actions.includes(a))) && Math.min(...frames.map(f => f.y)) < aiming.y - 8,
    `lowest y ${Math.min(...frames.map(f => f.y))} from ${aiming.y}`);
  await fingers.upAll();
  await sleep(300);
  await fingers.tap(4, c.lock.at);
  await sleep(300);
  check(`${label}: LOCK toggles on with one tap`, (await now(page)).actions.includes('lock'));
  await fingers.tap(4, c.lock.at);
  await sleep(300);
  check(`${label}: and off with another`, !(await now(page)).actions.includes('lock'));
}

async function courtyard(page, fingers, c, label) {
  // Full push, then a light push, each held for a second after the thumb arrives; the light one must walk, slower.
  // Horizontal: from the road start there is room to the right for a full second, then back to the left.
  const walk = async (id, dx) => {
    await fingers.down(id, c.stick.at);
    await fingers.slide(id, c.stick.at, offset(c.stick, dx, 0));
    await sleep(150);
    const a = await now(page);
    await sleep(1000);
    const b = await now(page);
    await fingers.up(id);
    await sleep(300);
    return Math.abs(b.x - a.x);
  };
  const full = await walk(1, 1);
  const half = await walk(2, -0.45);
  check(`${label}: the joystick walks, slower when pushed less`, full > 60 && half > full * 0.15 && half < full * 0.6, `full ${full}, light ${half} pixels in 1 s`);
  await screenshot(page, `${label.replace(' ', '-')}-stick`);
}

async function trail(page, fingers, c, label) {
  const a = await now(page);
  await fingers.down(1, offset(c.dpad, 0, 0.8));
  await sleep(900);
  await fingers.up(1);
  await sleep(500);
  const b = await now(page);
  check(`${label}: the d-pad walks the grid`, a.x !== b.x || a.y !== b.y, `(${a.x},${a.y}) → (${b.x},${b.y})`);
  await fingers.tap(2, c.pause.at);
  await sleep(600);
  const menu = (await now(page)).scene;
  await screenshot(page, `${label.replace(' ', '-')}-menu`);
  await fingers.tap(3, c.cancel.at);
  await sleep(600);
  check(`${label}: MENU opens the start menu and B closes it`, menu === 'StartMenuScene' && (await now(page)).scene === 'OverworldScene', menu);
}

/** Behaviour every game shares: focus loss, rotation, quick taps, gestures. */
async function common(o, c, label, game) {
  const { page, fingers } = o;
  const move = c.dpad ?? c.stick;
  const direction = game === 'TrailDuel' ? 'down' : 'right';
  const at = game === 'TrailDuel' ? [0, 0.8] : [0.8, 0];
  await fingers.down(1, move.at);
  await fingers.move(1, offset(move, ...at));
  await sleep(300);
  const held = (await now(page)).actions.includes(direction);
  await page.evaluate(() => window.dispatchEvent(new FocusEvent('blur')));
  await sleep(300);
  check(`${label}: losing focus releases held touch controls`, held && !(await now(page)).actions.includes(direction) && (await now(page)).fingers === 0);
  await fingers.upAll();
  await sleep(200);

  // A tap shorter than a frame (down and up in the same frame) still presses once.
  const mark = await traced(page);
  const quick = c.cancel ?? c.run ?? c.fire;
  const action = quick === c.fire ? 'fire' : quick === c.run ? 'run' : 'cancel';
  await o.client.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x: quick.at[0], y: quick.at[1], id: 9 }] });
  await o.client.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
  await sleep(300);
  check(`${label}: a very short tap is seen`, (await since(page, mark)).some(f => f.actions.includes(action)));
  if (game === 'TrailDuel') { await sleep(500); if ((await now(page)).scene !== 'OverworldScene') { await fingers.tap(3, c.cancel.at); await sleep(500); } }

  // The same tap read by a slow frame (a loaded device): the page stalls 50 ms right after the finger lifts, so the
  // next frame catches up with several fixed updates. The tap is down in the first of them only (as a key's is), and
  // the trace, which lists the actions down in any update of a frame, must still show it (run.mjs's keyboard check).
  const slowMark = await traced(page);
  await page.evaluate(() => addEventListener('touchend', () => {
    const until = performance.now() + 50;
    while (performance.now() < until);
  }, { once: true, capture: true }));
  await o.client.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x: quick.at[0], y: quick.at[1], id: 9 }] });
  await o.client.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
  await sleep(300);
  const slowFrames = await since(page, slowMark);
  check(`${label}: a very short tap is seen when its frame catches up with several updates`, slowFrames.some(f => f.actions.includes(action)),
    `${slowFrames.length} frames traced`);
  if (game === 'TrailDuel') { await sleep(500); if ((await now(page)).scene !== 'OverworldScene') { await fingers.tap(3, c.cancel.at); await sleep(500); } }

  // Rotation with a finger down: the finger is released, the controls move, and touch works in the new orientation.
  const viewport = page.viewport();
  await fingers.down(1, move.at);
  await fingers.move(1, offset(move, ...at));
  await sleep(200);
  await page.setViewport({ ...viewport, width: viewport.height, height: viewport.width, isLandscape: !viewport.isLandscape });
  await sleep(800);
  check(`${label}: rotating releases a held finger`, !(await now(page)).actions.includes(direction));
  await fingers.upAll();
  await sleep(200);
  const rotated = await controls(page);
  const rotatedMove = rotated.dpad ?? rotated.stick;
  await fingers.down(2, rotatedMove.at);
  await fingers.move(2, offset(rotatedMove, ...at));
  await sleep(400);
  check(`${label}: controls work after rotating`, (await now(page)).actions.includes(direction) && rotatedMove.at[1] !== move.at[1]);
  await screenshot(page, `${label.replace(' ', '-')}-rotated`);
  await fingers.upAll();
  await page.setViewport(viewport);
  await sleep(500);

  // A pinch on the game is not a zoom; a long press opens no menu.
  await fingers.down(5, [viewport.width * 0.45, viewport.height * 0.3]);
  await fingers.down(6, [viewport.width * 0.55, viewport.height * 0.3]);
  await fingers.move(5, [viewport.width * 0.3, viewport.height * 0.3]);
  await fingers.move(6, [viewport.width * 0.7, viewport.height * 0.3]);
  await fingers.upAll();
  await sleep(300);
  const view = await page.evaluate(() => ({ zoom: visualViewport?.scale ?? 1, x: scrollX, y: scrollY }));
  check(`${label}: a pinch neither zooms nor scrolls`, view.zoom === 1 && view.x === 0 && view.y === 0, JSON.stringify(view));
  const menuOpened = await page.evaluate(() => {
    const e = new MouseEvent('contextmenu', { bubbles: true, cancelable: true });
    document.getElementById('canvas').dispatchEvent(e);
    return !e.defaultPrevented;
  });
  check(`${label}: no context menu on long press`, !menuOpened);
}

if (!fs.existsSync(packages)) throw new Error(`${packages} does not exist; build with web/build.sh --dev --out ${path.relative(repo, packages)}`);
const executablePath = [process.env.CHROME_PATH, '/usr/bin/google-chrome', '/opt/pw-browsers/chromium-1243/chrome-linux64/chrome',
  'C:/Program Files/Google/Chrome/Application/chrome.exe', '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome']
  .find(p => p && fs.existsSync(p));
if (!executablePath) throw new Error('Chrome not found; set CHROME_PATH.');
const server = spawn(process.execPath, [path.join(repo, 'web/serve.mjs'), packages, String(port)], { stdio: 'ignore' });
await sleep(500);
const browser = await puppeteer.launch({ browser: 'chrome', executablePath, headless: !headed, args: ['--no-first-run', '--no-sandbox', '--autoplay-policy=no-user-gesture-required'] });
console.log(`== ${await browser.version()}`);
try {
  for (const device of deviceNames) {
    for (const game of games) {
      await menus(browser, device, game).catch(e => check(`${device} ${game}: menus`, false, e.message));
      await gameplay(browser, device, game).catch(e => check(`${device} ${game}: gameplay`, false, e.message));
    }
  }
} finally {
  await browser.close();
  server.kill();
}
console.log(`${results.length - failures} passed, ${failures} failed`);
fs.mkdirSync(out, { recursive: true });
fs.writeFileSync(path.join(out, 'results-mobile.json'), JSON.stringify(results, null, 2));
const summary = ['### Mobile browser checks', '', `${results.length - failures} passed, ${failures} failed.`, '', '| Check | Result | Detail |', '| --- | --- | --- |',
  ...results.map(r => `| ${r.name} | ${r.ok ? 'pass' : 'FAIL'} | ${String(r.detail).replace(/\|/g, '/')} |`), ''].join('\n');
if (process.env.GITHUB_STEP_SUMMARY) fs.appendFileSync(process.env.GITHUB_STEP_SUMMARY, summary + '\n');
process.exit(failures ? 1 : 0);

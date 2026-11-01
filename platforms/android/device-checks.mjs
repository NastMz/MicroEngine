#!/usr/bin/env node
// Checks the Android host on a running emulator or device, level by level (platforms/README.md, "Evidence levels"):
//   installed   the APK installs
//   started     the activity starts and the .NET host logs "microengine: start"
//   rendered    each scripted run of the game (web/tests/scenarios.json) ends with all its captures; they are pulled
//               to --out/desktop/<Game>/<script> so `web/tests/desktop.mjs --skip-run` compares them with a reference
//   audio       the audio device opened (not listened to)
//   input       keys sent by the system (adb input keyevent) change what the game shows
//   touch       taps (adb input tap) on the control that confirms menus, at the position the game logs for this screen,
//               take the game past its first scene
//   touch-drag  a finger held on the movement control (adb input motionevent) presses the direction and moves the player
//   touch-two-thumbs  touch.script holds movement and a second control at once on the device's layout
//   lifecycle   going to the background suspends the game and coming back resumes it
//
//   node platforms/android/device-checks.mjs --apk FILE [--serial ID] [--game MeadowRun] [--out DIR]
// Needs adb on PATH or in $ANDROID_HOME/platform-tools. Exit code 1 if any level failed.
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { decodePng } from '../../web/tests/captures.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const repo = path.resolve(here, '../..');
const args = process.argv.slice(2);
const option = (name, fallback) => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : fallback; };
const apk = option('--apk', '');
const game = option('--game', 'MeadowRun');
const out = path.resolve(option('--out', path.join(repo, 'dist/android-checks')));
const serial = option('--serial', process.env.ANDROID_SERIAL ?? '');
const pkg = `dev.microengine.${game.toLowerCase()}`;
const activity = `${pkg}/dev.microengine.GameActivity`;
if (!apk || !fs.existsSync(apk)) throw new Error(`--apk: '${apk}' not found; build platforms/android first.`);

const sdk = process.env.ANDROID_HOME ?? process.env.ANDROID_SDK_ROOT;
const adbPath = sdk && fs.existsSync(path.join(sdk, 'platform-tools')) ? path.join(sdk, 'platform-tools', process.platform === 'win32' ? 'adb.exe' : 'adb') : 'adb';
function adb(...a) {
  const run = spawnSync(adbPath, [...(serial ? ['-s', serial] : []), ...a], { encoding: 'latin1', maxBuffer: 64 * 1024 * 1024 });
  if (run.error) throw run.error;
  return { status: run.status, out: `${run.stdout}${run.stderr}` };
}
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
const log = () => adb('logcat', '-d', '-v', 'brief', 'MicroEngine:I', 'AndroidRuntime:E', 'DEBUG:F', 'mono-rt:F', 'SDL:W', '*:S').out;
const crashed = text => /FATAL EXCEPTION|Fatal signal|FATAL UNHANDLED|Abort message/.test(text);

const results = [];
function record(level, ok, detail) {
  results.push({ level, ok, detail });
  console.log(`${ok ? 'PASS' : 'FAIL'} ${level}: ${detail}`);
}

async function waitFor(pattern, timeoutMs) {
  for (const end = Date.now() + timeoutMs; Date.now() < end; await sleep(1000)) {
    const text = log();
    if (pattern.test(text) || crashed(text)) return text;
  }
  return log();
}

function screenshot(file) {
  const run = spawnSync(adbPath, [...(serial ? ['-s', serial] : []), 'exec-out', 'screencap', '-p'], { maxBuffer: 64 * 1024 * 1024 });
  fs.writeFileSync(file, run.stdout);
  return run.stdout;
}

// A device that just booted may be asleep or locked: the activity then never gets its window and SDL waits for it.
function awake() {
  adb('shell', 'svc', 'power', 'stayon', 'true');
  adb('shell', 'input', 'keyevent', 'KEYCODE_WAKEUP');
  adb('shell', 'wm', 'dismiss-keyguard');
}

// The screen once at least 5% of it is not black (the game has drawn), or the last attempt.
async function picture(file, timeoutMs) {
  let shot = Buffer.alloc(0);
  for (const end = Date.now() + timeoutMs; Date.now() < end; await sleep(1000)) {
    shot = screenshot(file);
    try {
      const { rgba } = decodePng(shot);
      let lit = 0;
      for (let i = 0; i < rgba.length; i += 4) if (rgba[i] + rgba[i + 1] + rgba[i + 2] > 24) lit++;
      if (lit > rgba.length / 4 * 0.05) return shot;
    } catch { /* not a complete PNG yet */ }
  }
  return shot;
}

fs.rmSync(out, { recursive: true, force: true });
fs.mkdirSync(out, { recursive: true });
const install = adb('install', '-r', apk);
record('installed', install.status === 0 && /Success/.test(install.out), install.out.trim().split('\n').pop());
if (install.status !== 0) process.exit(1);
awake();

// Scripted runs: the same scenarios, environment and seed as desktop.mjs.
const { scenarios } = JSON.parse(fs.readFileSync(path.join(repo, 'web/tests/scenarios.json'), 'utf8'));
let started = false, audio = null;
const runs = [];
for (const s of scenarios.filter(s => s.game === game)) {
  const name = path.basename(s.script, '.script');
  // Every scripted run starts from a fresh install state, like desktop.mjs's empty save folder per scenario: a save
  // left by the previous scenario changes the title menu and the path a script takes. `pm clear` also stops the app.
  adb('shell', 'pm', 'clear', pkg);
  adb('logcat', '-c');
  const env = Object.entries(s.env).map(([k, v]) => `${k}=${v}`).join(';');
  // One command line for the device shell, which would otherwise split the env extra at its ';'.
  adb('shell', `am start -W -n ${activity} --es script '${s.script}'${env ? ` --es env '${env}'` : ''}`);
  const text = await waitFor(/microengine: exit/, 240_000);
  fs.writeFileSync(path.join(out, `${name}.log`), text);
  if (/microengine: start/.test(text)) started = true;
  const done = text.match(/microengine: done (\d+) updates, (\d+) frames, (\d+) captures, audio (\w+)/);
  if (done) audio = audio === false ? false : done[4] === 'available';
  const dir = path.join(out, 'desktop', game, name);
  fs.mkdirSync(path.dirname(dir), { recursive: true });
  adb('pull', `/sdcard/Android/data/${pkg}/files/captures/${name}`, dir);
  const pulled = fs.existsSync(dir) ? fs.readdirSync(dir).filter(f => f.endsWith('.png')).length : 0;
  const expected = (text.match(/microengine: capture /g) ?? []).length;
  runs.push({ name, ok: !!done && /microengine: exit 0/.test(text) && pulled === expected && expected > 0 && !crashed(text),
    detail: done ? `${done[1]} updates, ${pulled}/${expected} captures pulled` : (text.match(/microengine: error: .*/)?.[0] ?? (crashed(text) ? 'crashed' : `no result after 240 s (${expected} captures by then)`)) });
}
record('started', started, started ? 'the .NET host logged its start' : 'no "microengine: start" in the log');
record('rendered', runs.length > 0 && runs.every(r => r.ok), runs.map(r => `${r.name}: ${r.detail}`).join('; ') || `no scenario for ${game}`);
record('audio', audio === true, audio === null ? 'no run finished' : audio ? 'SDL opened the audio device in every run' : 'no audio device (silent run)');

// Input and lifecycle: an interactive run, keys from the system, then background and foreground.
adb('shell', 'am', 'force-stop', pkg);
adb('logcat', '-c');
awake();
adb('shell', 'am', 'start', '-W', '-n', activity);
// The host logs each scene change; keys from the system must take the game past its first scene. Screenshots are kept
// for review only: some emulator images return a black screencap of a GL surface.
const scenes = text => [...text.matchAll(/microengine: scene (\w+)/g)].map(m => m[1]);
const first = scenes(await waitFor(/microengine: scene /, 120_000))[0];
await picture(path.join(out, 'input-before.png'), 20_000);
for (const key of ['KEYCODE_ENTER', 'KEYCODE_ENTER', 'KEYCODE_DPAD_RIGHT', 'KEYCODE_ENTER']) { adb('shell', 'input', 'keyevent', key); await sleep(1500); }
await sleep(3000);
screenshot(path.join(out, 'input-after.png'));
const reached = scenes(log());
record('input', !!first && reached.length > 1 && !crashed(log()),
  first ? `scenes after Enter, Enter, Right, Enter: ${reached.join(' → ')}` : 'the game logged no scene');
adb('shell', 'input', 'keyevent', 'KEYCODE_HOME');
await sleep(3000);
adb('shell', 'am', 'start', '-W', '-n', activity);
await sleep(4000);
const alive = adb('shell', 'pidof', pkg).out.trim();
const resumed = screenshot(path.join(out, 'resumed.png'));
const lifecycleLog = log();
fs.writeFileSync(path.join(out, 'interactive.log'), lifecycleLog);
const suspendedThenResumed = /microengine: suspended[\s\S]*microengine: resumed/.test(lifecycleLog);
record('lifecycle', alive.length > 0 && suspendedThenResumed && !crashed(lifecycleLog) && resumed.length > 0,
  !alive ? 'the process is gone after Home and relaunch'
    : suspendedThenResumed ? 'Home suspended the game, relaunching resumed it (resumed.png)' : 'still running, but no suspended/resumed pair in the log');
adb('shell', 'am', 'force-stop', pkg);

// Touch. The host logs where the game's on-screen controls are on this screen (MICROENGINE_PROBE=1) and, whenever they
// change, the actions down with the scene's description (the player's position). Each game: how its gameplay checks
// start, the control that confirms its menus, the movement control and the direction a push to its right (or down)
// presses.
const touchSetup = {
  MeadowRun: { env: 'MEADOW_START=level', confirm: 'jump', move: 'dpad', push: [0.8, 0], action: 'right', second: 'jump' },
  NeonRift: { env: 'RIFT_START=stage;RIFT_GOD=1', confirm: 'jump', move: 'stick', push: [0.7, -0.7], action: 'up', second: 'fire' },
  CourtyardKey: { env: 'COURTYARD_START=road', confirm: 'confirm', move: 'stick', push: [0, 1], action: 'down', second: 'confirm' },
  TrailDuel: { env: 'TRAIL_DEBUG=town', confirm: 'confirm', move: 'dpad', push: [0, 0.8], action: 'down', second: 'cancel' },
}[game];
const controlsIn = text => Object.fromEntries([...text.matchAll(/microengine: control (\S+) (-?\d+) (-?\d+) (\d+)/g)].map(m => [m[1], { x: +m[2], y: +m[3], r: +m[4] }]));
const probes = text => [...text.matchAll(/microengine: probe (\S+) \[([^\]]*)\] ?(.*)/g)].map(m => ({ scene: m[1], actions: m[2].split(',').filter(Boolean), description: m[3],
  x: Number(m[3].match(/x=(-?\d+)/)?.[1]), y: Number(m[3].match(/y=(-?\d+)/)?.[1]) }));
async function interactive(env) {
  adb('shell', 'am', 'force-stop', pkg);
  adb('shell', 'pm', 'clear', pkg);
  adb('logcat', '-c');
  awake();
  adb('shell', `am start -W -n ${activity} --es env '${env}'`);
  await waitFor(/microengine: control /, 120_000);
  await sleep(2500);
  return controlsIn(log());
}

// Real system taps (adb input tap) on the control that confirms menus must take the game past its title.
{
  const controls = await interactive('MICROENGINE_PROBE=1');
  const confirm = controls[touchSetup.confirm];
  const first = scenes(log())[0];
  if (confirm) for (let i = 0; i < 3 && scenes(log()).length < 2; i++) { adb('shell', 'input', 'tap', String(confirm.x), String(confirm.y)); await sleep(1500); }
  await sleep(1500);
  screenshot(path.join(out, 'touch-after.png'));
  const touched = scenes(log());
  record('touch', !!confirm && touched.length > 1 && !crashed(log()),
    confirm ? `taps on '${touchSetup.confirm}' at ${confirm.x},${confirm.y} (radius ${confirm.r} px): ${touched.join(' → ')}` : `no control layout in the log (first scene ${first})`);
  fs.writeFileSync(path.join(out, 'touch.log'), log());
}

// A real system touch held on the movement control must press the direction and move the player. `input motionevent`
// (Android 10+) puts the finger down, pushes it to the edge of the control and keeps it there: a swipe would move it
// linearly for its whole duration and lift it on arrival, which on a joystick is a slowly growing push that only
// reaches the direction threshold as it ends.
{
  const controls = await interactive(`${touchSetup.env};MICROENGINE_PROBE=1`);
  const move = controls[touchSetup.move];
  let detail = 'no control layout in the log';
  let ok = false;
  if (move) {
    const before = probes(log()).at(-1);
    const to = [Math.round(move.x + touchSetup.push[0] * move.r), Math.round(move.y + touchSetup.push[1] * move.r)];
    adb('shell', 'input', 'motionevent', 'DOWN', String(move.x), String(move.y));
    for (let i = 1; i <= 4; i++)
      adb('shell', 'input', 'motionevent', 'MOVE', String(Math.round(move.x + (to[0] - move.x) * i / 4)), String(Math.round(move.y + (to[1] - move.y) * i / 4)));
    await sleep(2500);
    adb('shell', 'input', 'motionevent', 'UP', String(to[0]), String(to[1]));
    await sleep(1000);
    const seen = probes(log());
    const pushed = seen.filter(p => p.actions.includes(touchSetup.action));
    const last = seen.at(-1);
    const moved = before && last && (before.x !== last.x || before.y !== last.y);
    ok = pushed.length > 0 && !!moved && !crashed(log());
    detail = `finger down at ${move.x},${move.y}, pushed to ${to.join(',')} and held 2.5 s: '${touchSetup.action}' in ${pushed.length} probes, ${before?.description ?? '?'} → ${last?.description ?? '?'}`;
  }
  screenshot(path.join(out, 'touch-drag.png'));
  record('touch-drag', ok, detail);
  fs.writeFileSync(path.join(out, 'touch-drag.log'), log());
}

// Two thumbs at once, scripted on the device's own layout (touch.script, through the same hit testing as fingers):
// adb's input command has one pointer only, so this level shows the controls' multitouch on this screen, not the
// system's delivery of two fingers.
{
  adb('shell', 'am', 'force-stop', pkg);
  adb('shell', 'pm', 'clear', pkg);
  adb('logcat', '-c');
  adb('shell', `am start -W -n ${activity} --es script 'touch.script' --es env '${touchSetup.env};MICROENGINE_PROBE=1'`);
  const text = await waitFor(/microengine: exit/, 240_000);
  fs.writeFileSync(path.join(out, 'touch-script.log'), text);
  const seen = probes(text);
  const both = seen.some(p => p.actions.includes(touchSetup.action) && p.actions.includes(touchSetup.second));
  const positions = new Set(seen.map(p => p.description)).size;
  record('touch-two-thumbs', /microengine: exit 0/.test(text) && both && positions > 2,
    `'${touchSetup.action}' and '${touchSetup.second}' down together: ${both}; ${positions} distinct states`);
}
adb('shell', 'am', 'force-stop', pkg);

const summary = ['### Android device checks', '', `Game ${game}, APK \`${path.basename(apk)}\`.`, '', '| Level | Result | Detail |', '| --- | --- | --- |',
  ...results.map(r => `| ${r.level} | ${r.ok ? 'pass' : 'fail'} | ${r.detail.replace(/\|/g, '/')} |`), ''].join('\n');
if (process.env.GITHUB_STEP_SUMMARY) fs.appendFileSync(process.env.GITHUB_STEP_SUMMARY, summary + '\n');
process.exit(results.every(r => r.ok) ? 0 : 1);

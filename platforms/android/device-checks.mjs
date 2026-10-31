#!/usr/bin/env node
// Checks the Android host on a running emulator or device, level by level (platforms/README.md, "Evidence levels"):
//   installed   the APK installs
//   started     the activity starts and the .NET host logs "microengine: start"
//   rendered    each scripted run of the game (web/tests/scenarios.json) ends with all its captures; they are pulled
//               to --out/desktop/<Game>/<script> so `web/tests/desktop.mjs --skip-run` compares them with a reference
//   audio       the raylib audio device opened (not listened to)
//   input       keys sent by the system (adb input keyevent) change what the game shows
//   lifecycle   the game survives going to the background and back, then stops cleanly
//
//   node platforms/android/device-checks.mjs --apk FILE [--serial ID] [--game MeadowRun] [--out DIR]
// Needs adb on PATH or in $ANDROID_HOME/platform-tools. Exit code 1 if any level failed.
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

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
const log = () => adb('logcat', '-d', '-v', 'brief', 'MicroEngine:I', 'raylib:W', 'AndroidRuntime:E', 'DEBUG:F', 'mono-rt:F', '*:S').out;
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

fs.rmSync(out, { recursive: true, force: true });
fs.mkdirSync(out, { recursive: true });
const install = adb('install', '-r', apk);
record('installed', install.status === 0 && /Success/.test(install.out), install.out.trim().split('\n').pop());
if (install.status !== 0) process.exit(1);

// Scripted runs: the same scenarios, environment and seed as desktop.mjs.
const { scenarios } = JSON.parse(fs.readFileSync(path.join(repo, 'web/tests/scenarios.json'), 'utf8'));
let started = false, audio = null;
const runs = [];
for (const s of scenarios.filter(s => s.game === game)) {
  const name = path.basename(s.script, '.script');
  adb('shell', 'am', 'force-stop', pkg);
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
    detail: done ? `${done[1]} updates, ${pulled}/${expected} captures pulled` : (text.match(/microengine: error: .*/)?.[0] ?? (crashed(text) ? 'crashed' : 'no result')) });
}
record('started', started, started ? 'the .NET host logged its start' : 'no "microengine: start" in the log');
record('rendered', runs.length > 0 && runs.every(r => r.ok), runs.map(r => `${r.name}: ${r.detail}`).join('; ') || `no scenario for ${game}`);
record('audio', audio === true, audio === null ? 'no run finished' : audio ? 'raylib opened the audio device in every run' : 'no audio device (silent run)');

// Input and lifecycle: an interactive run, keys from the system, then background and foreground.
adb('shell', 'am', 'force-stop', pkg);
adb('logcat', '-c');
adb('shell', 'am', 'start', '-W', '-n', activity);
await waitFor(/microengine: start/, 60_000);
await sleep(6000);
const before = screenshot(path.join(out, 'input-before.png'));
for (const key of ['KEYCODE_ENTER', 'KEYCODE_ENTER', 'KEYCODE_DPAD_RIGHT', 'KEYCODE_ENTER']) { adb('shell', 'input', 'keyevent', key); await sleep(700); }
await sleep(2000);
const after = screenshot(path.join(out, 'input-after.png'));
record('input', before.length > 0 && after.length > 0 && !before.equals(after) && !crashed(log()),
  before.equals(after) ? 'the screen did not change after Enter and Right' : 'the screen changed after Enter and Right (input-before.png, input-after.png)');
adb('shell', 'input', 'keyevent', 'KEYCODE_HOME');
await sleep(3000);
adb('shell', 'am', 'start', '-W', '-n', activity);
await sleep(4000);
const alive = adb('shell', 'pidof', pkg).out.trim();
const resumed = screenshot(path.join(out, 'resumed.png'));
const lifecycleLog = log();
fs.writeFileSync(path.join(out, 'interactive.log'), lifecycleLog);
record('lifecycle', alive.length > 0 && !crashed(lifecycleLog) && resumed.length > 0,
  alive ? 'still running after Home and relaunch (resumed.png)' : 'the process is gone after Home and relaunch');
adb('shell', 'am', 'force-stop', pkg);

const summary = ['### Android device checks', '', `Game ${game}, APK \`${path.basename(apk)}\`.`, '', '| Level | Result | Detail |', '| --- | --- | --- |',
  ...results.map(r => `| ${r.level} | ${r.ok ? 'pass' : 'fail'} | ${r.detail.replace(/\|/g, '/')} |`), ''].join('\n');
if (process.env.GITHUB_STEP_SUMMARY) fs.appendFileSync(process.env.GITHUB_STEP_SUMMARY, summary + '\n');
process.exit(results.every(r => r.ok) ? 0 : 1);

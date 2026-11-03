// Plays one scenario of web/tests/scenarios.json on a probed desktop build and keeps its state trace.
//
//   node run-desktop.mjs --copy DIR --game G --script S [--platform raylib|sdl|headless] --out FILE [--captures DIR]
//                        [--env K=V,...]
//
// DIR is a probe copy (apply-probe.mjs) with the game built in Release. The trace (FILE) holds the probe's T0 lines.
// The scenario's environment (scenarios.json) is applied, as desktop.mjs does; --env adds more (MICROENGINE_T0_DUMP).
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawnSync } from 'node:child_process';

const args = process.argv.slice(2);
const option = (name, fallback) => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : fallback; };
const copy = path.resolve(option('--copy', ''));
const game = option('--game', '');
const script = option('--script', '');
const platform = option('--platform', 'raylib');
const out = path.resolve(option('--out', ''));
const captures = option('--captures', '');
// K=V pairs separated by commas; a value may itself hold commas (MICROENGINE_T0_SKIPFIELDS=_view,_hud).
const extra = Object.fromEntries(option('--env', '').split(/,(?=[A-Z][A-Z0-9_]*=)/).filter(Boolean).map(kv => [kv.slice(0, kv.indexOf('=')), kv.slice(kv.indexOf('=') + 1)]));

const { scenarios } = JSON.parse(fs.readFileSync(path.join(copy, 'web/tests/scenarios.json'), 'utf8'));
const scenario = scenarios.find(s => s.game === game && s.script === script);
if (!scenario) throw new Error(`no scenario ${game} ${script} in scenarios.json`);
const saveDirVariable = { MeadowRun: 'MEADOW_SAVE_DIR', CourtyardKey: 'COURTYARD_SAVE_DIR', TrailDuel: 'TRAIL_SAVE_DIR', NeonRift: 'RIFT_SAVE_DIR' };
const saves = fs.mkdtempSync(path.join(os.tmpdir(), 'microengine-saves-'));
const exe = path.join(copy, 'games', game, 'bin/Release/net10.0', game + (process.platform === 'win32' ? '.exe' : ''));
const argv = ['--script', path.join(copy, 'games', game, 'scripts', script), '--hidden', '--mute', '--platform', platform];
// Without --captures, captures go to a temporary folder (the games' default is ./captures in the working directory).
const captureDir = captures ? path.resolve(captures) : path.join(saves, 'captures');
if (captures) fs.rmSync(captureDir, { recursive: true, force: true });
argv.push('--captures', captureDir);
const run = spawnSync(exe, argv, { env: { ...process.env, ...scenario.env, ...extra, [saveDirVariable[game]]: saves }, encoding: 'utf8', maxBuffer: 1 << 30 });
fs.rmSync(saves, { recursive: true, force: true });
const lines = run.stdout.split('\n').filter(l => l.startsWith('T0'));
fs.mkdirSync(path.dirname(out), { recursive: true });
fs.writeFileSync(out, lines.join('\n') + '\n');
console.log(`${game} ${script} on ${platform}: exit ${run.status}, ${lines.filter(l => l.startsWith('T0 ')).length} steps traced -> ${out}`);
if (run.status !== 0) console.log(run.stderr.slice(-2000));

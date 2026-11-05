// Replays scenarios.json on the desktop build (raylib, hidden window) and compares every capture with the ones
// the browser runs stored (run.mjs scripts). Equal files mean the browser build produced the same frames from
// the same input, seed and starting point.
//
//   node web/tests/desktop.mjs [--out DIR] [--games A,B] [--skip-run] [--backend NAME] [--jobs N]
//                              [--reference FILE [--names-only]] [--write-reference FILE]
//
// --backend runs the games on another desktop platform (sdl, sdl-software; the games' --platform); the default stays
// raylib. Compare browser captures with the desktop build of the platform the browser package runs on (--backend sdl
// for an SDL package): SDL and raylib already draw some frames differently on desktop (their own references).
// --platform labels a new reference (--write-reference). --jobs runs that many scenarios at once (default: up to 4).
// --skip-run compares captures already in --out/desktop/<Game>/<script> (for example pulled from a device).
// --packages DIR runs the executables of extracted native packages instead of the repository's build
// (DIR/<Game>/<Game>[.exe], or the executable inside DIR/<Game>/<Title>.app on macOS; packaging/verify.mjs).
//
// --reference also checks the native captures against a platform's visual reference (captures.mjs): every capture
// present, same size, no visible change. --names-only checks completeness and sizes only, for a platform without
// its own reference yet. --write-reference records a new reference from this run's captures.
// On Linux without a display, run it under xvfb-run. Needs the desktop games built (dotnet build MicroEngine.slnx -c Release).
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { compare, grid, summarise, tolerance } from './captures.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const repo = path.resolve(here, '../..');
const args = process.argv.slice(2);
const option = (name, fallback) => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : fallback; };
const out = path.resolve(option('--out', path.join(repo, 'dist/web-tests')));
const only = option('--games', '');
const skipRun = args.includes('--skip-run');
const referenceFile = option('--reference', '');
const writeReference = option('--write-reference', '');
const backend = option('--backend', 'raylib');
const packages = option('--packages', '');

/** The executable that plays a game: the repository's build, or the one inside an extracted package. */
function executableOf(game) {
  const name = game + (process.platform === 'win32' ? '.exe' : '');
  if (!packages) return path.join(repo, 'games', game, 'bin/Release/net10.0', name);
  const top = path.resolve(packages, game);
  const app = fs.existsSync(top) ? fs.readdirSync(top).find(f => f.endsWith('.app')) : undefined;
  return app ? path.join(top, app, 'Contents', 'MacOS', game) : path.join(top, name);
}
// Scripted runs are deterministic (fixed step, seed 1), so they run side by side; results print in scenario order.
const jobs = Math.max(1, Number(option('--jobs', String(Math.min(4, os.availableParallelism())))));
const saveDirVariable = { MeadowRun: 'MEADOW_SAVE_DIR', CourtyardKey: 'COURTYARD_SAVE_DIR', TrailDuel: 'TRAIL_SAVE_DIR', NeonRift: 'RIFT_SAVE_DIR' };

const { scenarios } = JSON.parse(fs.readFileSync(path.join(here, 'scenarios.json'), 'utf8'));
const selected = scenarios.filter(s => !only || only.split(',').includes(s.game));

/** Plays one scenario into its capture folder; resolves to { status, signal, stdout, stderr }. */
function play(s) {
  const name = path.basename(s.script, '.script');
  const dir = path.join(out, 'desktop', s.game, name);
  const exe = executableOf(s.game);
  if (!fs.existsSync(exe)) throw new Error(`${exe} is missing; ${packages ? 'extract the package there' : 'run dotnet build MicroEngine.slnx -c Release'}.`);
  fs.rmSync(dir, { recursive: true, force: true });
  fs.mkdirSync(dir, { recursive: true });
  const saves = fs.mkdtempSync(path.join(os.tmpdir(), 'microengine-saves-'));
  return new Promise(resolve => {
    const child = spawn(exe, ['--script', path.join(repo, 'games', s.game, 'scripts', s.script), '--hidden', '--mute', '--platform', backend, '--captures', dir],
      { env: { ...process.env, ...s.env, [saveDirVariable[s.game]]: saves } });
    let stdout = '', stderr = '';
    child.stdout.on('data', d => stdout += d);
    child.stderr.on('data', d => stderr += d);
    child.on('error', error => { stderr += String(error); });
    child.on('close', (status, signal) => {
      fs.rmSync(saves, { recursive: true, force: true });
      resolve({ status, signal, stdout, stderr });
    });
  });
}

const runs = new Map();
if (!skipRun) {
  const queue = [...selected];
  await Promise.all(Array.from({ length: Math.min(jobs, queue.length) }, async () => {
    for (let s; (s = queue.shift());) runs.set(s, await play(s));
  }));
}

let failures = 0;
for (const s of selected) {
  const name = path.basename(s.script, '.script');
  const dir = path.join(out, 'desktop', s.game, name);
  if (!skipRun) {
    const run = runs.get(s);
    if (run.status !== 0) {
      failures++;
      // raylib prints its warnings (the reason a window failed) on stdout, the launcher its error on stderr.
      const output = `${run.stdout}\n${run.stderr}`.split('\n').filter(l => /WARNING|ERROR|error|Assertion/.test(l)).slice(0, 6).join(' | ');
      console.log(`FAIL desktop ${s.game} ${s.script}: exit ${run.status ?? run.signal} ${output.slice(0, 600)}`);
      continue;
    }
  }
  if (!fs.existsSync(dir)) {
    failures++;
    console.log(`FAIL desktop ${s.game} ${s.script}: no captures in ${dir}`);
    continue;
  }
  const desktop = fs.readdirSync(dir).filter(f => f.endsWith('.png')).sort();
  for (const browser of ['chrome', 'firefox']) {
    const web = path.join(out, browser, s.game, name);
    if (!fs.existsSync(web)) continue;
    const captures = fs.readdirSync(web).filter(f => f.endsWith('.png')).sort();
    const differing = desktop.filter(f => !captures.includes(f) || !fs.readFileSync(path.join(dir, f)).equals(fs.readFileSync(path.join(web, f))));
    const ok = desktop.length > 0 && captures.length === desktop.length && differing.length === 0;
    if (!ok) failures++;
    console.log(`${ok ? 'PASS' : 'FAIL'} ${browser} ${s.game} ${s.script}: ${desktop.length - differing.length}/${desktop.length} captures identical to desktop${differing.length ? ` (differ: ${differing.slice(0, 5).join(', ')})` : ''}`);
  }
}
if (referenceFile || writeReference) {
  const summaries = {};
  for (const s of scenarios.filter(s => !only || only.split(',').includes(s.game))) {
    const name = path.basename(s.script, '.script');
    const dir = path.join(out, 'desktop', s.game, name);
    if (!fs.existsSync(dir)) continue;
    for (const file of fs.readdirSync(dir).filter(f => f.endsWith('.png')).sort()) summaries[`${s.game}/${name}/${file}`] = summarise(path.join(dir, file));
  }
  if (writeReference) {
    const reference = {
      $comment: 'Visual reference of the native desktop captures (web/tests/captures.mjs); regenerate with desktop.mjs --write-reference after an intended visual change.',
      platform: option('--platform', `${process.platform}-${process.arch}`),
      grid, tolerance,
      captures: summaries,
    };
    fs.writeFileSync(writeReference, JSON.stringify(reference, null, 1) + '\n');
    console.log(`reference: ${Object.keys(summaries).length} captures written to ${writeReference}`);
  }
  if (referenceFile) {
    const reference = JSON.parse(fs.readFileSync(referenceFile, 'utf8'));
    // With --games, only those games' captures are expected.
    if (only) reference.captures = Object.fromEntries(Object.entries(reference.captures).filter(([name]) => only.split(',').includes(name.split('/')[0])));
    const result = compare(summaries, reference, { namesOnly: args.includes('--names-only') });
    for (const note of result.notes) console.log(`note ${note}`);
    for (const problem of result.problems) console.log(`FAIL ${problem}`);
    failures += result.problems.length;
    const expected = Object.keys(reference.captures).length;
    console.log(`${result.problems.length ? 'FAIL' : 'PASS'} reference ${reference.platform}: ${result.total}/${expected} captures, `
      + `${result.identical} pixel-identical, ${result.notes.length} within ${args.includes('--names-only') ? 'names-only check' : `tolerance ${reference.tolerance}`}`);
  }
}
process.exit(failures ? 1 : 0);

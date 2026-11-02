// ARCH-1 P1: plays every scripted capture scenario of the four demos (web/tests/scenarios.json, the set CI compares)
// and the asset gallery's tour on the SDL platform, with SDL's default render driver and with its `gpu` driver
// (`--renderer gpu`, the game's preferred-renderer list; no code change) in the variants below, N times each,
// sequentially. SDL_LOGGING=gpu=debug,render=debug makes SDL name the backend and adapter it picked in log.txt.
//
//   node experiments/arch-1/p1-sdl-gpu-driver/run.mjs --out DIR [--runs 5] [--drivers default,gpu,...] [--games A,B]
//        [--scripts smoke,title] [--visible] [--window windowed|fullscreen|exclusive]
//
// Each run writes DIR/<driver>/run<r>/<game>/<script>/ with the captures (*.png), profile.json (--profile),
// result.json (Measure: wall time, time to the --report line, peak memory) and log.txt (stdout/stderr).
// Driver order alternates per run so slow drift (thermal, background work) does not favour one driver.
// Needs: dotnet build -c Release of games/*/ and samples/AssetGallery; Windows (Measure uses psapi).
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const repo = path.resolve(here, '../../..');
const args = process.argv.slice(2);
const option = (name, fallback) => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : fallback; };
const out = path.resolve(option('--out', '') || (() => { throw new Error('--out DIR is required (keep it outside the repository)'); })());
const runs = Number(option('--runs', '5'));
// Variants: SDL's default driver; the gpu driver with SDL_GPU's own backend choice; the gpu driver forced onto Vulkan
// (SDL_GPU_DRIVER); the gpu driver asked for the low-power adapter (--gpu power-saving, the integrated GPU here).
const variants = {
  default: { args: [], env: {} },
  gpu: { args: ['--renderer', 'gpu'], env: {} },
  'gpu-vulkan': { args: ['--renderer', 'gpu'], env: { SDL_GPU_DRIVER: 'vulkan' } },
  'gpu-lowpower': { args: ['--renderer', 'gpu', '--gpu', 'power-saving'], env: {} },
  // Control for gpu-lowpower: the same integrated adapter through SDL_GPU's Vulkan backend.
  'gpu-vulkan-lowpower': { args: ['--renderer', 'gpu', '--gpu', 'power-saving'], env: { SDL_GPU_DRIVER: 'vulkan' } },
};
const drivers = option('--drivers', Object.keys(variants).join(',')).split(',');
for (const d of drivers) if (!variants[d]) throw new Error(`unknown variant ${d}; known: ${Object.keys(variants).join(', ')}`);
const only = option('--games', '');
const scriptFilter = option('--scripts', ''); // e.g. smoke,title: only these script names
const visible = args.includes('--visible'); // show the window (scripted runs stay fixed-step, so still unpaced)
const windowMode = option('--window', ''); // windowed | fullscreen | exclusive, passed to the game

// The measuring helper builds into a scratch artifacts folder next to the results, never into the repository.
const artifacts = path.join(out, '_measure');
const build = spawnSync('dotnet', ['build', path.join(here, 'measure', 'Measure.csproj'), '-c', 'Release', '-nologo', '-v', 'q', '--artifacts-path', artifacts], { encoding: 'utf8' });
if (build.status !== 0) { console.error(build.stdout, build.stderr); process.exit(1); }
const measure = path.join(artifacts, 'bin', 'Measure', 'release', 'Measure.exe');

const saveDirVariable = { MeadowRun: 'MEADOW_SAVE_DIR', CourtyardKey: 'COURTYARD_SAVE_DIR', TrailDuel: 'TRAIL_SAVE_DIR', NeonRift: 'RIFT_SAVE_DIR' };
const { scenarios: demoScenarios } = JSON.parse(fs.readFileSync(path.join(repo, 'web/tests/scenarios.json'), 'utf8'));
const scenarios = [
  ...demoScenarios.map(s => ({ ...s, exe: path.join(repo, 'games', s.game, 'bin/Release/net10.0', `${s.game}.exe`), scripts: path.join(repo, 'games', s.game, 'scripts') })),
  { game: 'AssetGallery', script: 'tour.script', env: {}, exe: path.join(repo, 'samples/AssetGallery/bin/Release/net10.0/AssetGallery.exe'), scripts: path.join(repo, 'samples/AssetGallery/scripts') },
].filter(s => (!only || only.split(',').includes(s.game)) && (!scriptFilter || scriptFilter.split(',').includes(path.basename(s.script, '.script'))));
for (const s of scenarios) if (!fs.existsSync(s.exe)) throw new Error(`${s.exe} is missing; build it with -c Release first.`);

function play(s, driver, run) {
  const name = path.basename(s.script, '.script');
  const dir = path.join(out, driver, `run${run}`, s.game, name);
  fs.rmSync(dir, { recursive: true, force: true });
  fs.mkdirSync(dir, { recursive: true });
  const saves = fs.mkdtempSync(path.join(os.tmpdir(), 'p1-saves-'));
  const gameArgs = ['--script', path.join(s.scripts, s.script), ...(visible ? [] : ['--hidden']), ...(windowMode ? ['--window', windowMode] : []), '--mute', '--platform', 'sdl', '--report',
    '--captures', dir, '--profile', path.join(dir, 'profile.json'), ...variants[driver].args];
  const env = { ...process.env, SDL_LOGGING: 'gpu=debug,render=debug', ...s.env, ...variants[driver].env };
  if (saveDirVariable[s.game]) env[saveDirVariable[s.game]] = saves;
  const r = spawnSync(measure, [path.join(dir, 'result.json'), path.join(dir, 'log.txt'), s.exe, ...gameArgs], { env, encoding: 'utf8' });
  fs.rmSync(saves, { recursive: true, force: true });
  const result = fs.existsSync(path.join(dir, 'result.json')) ? JSON.parse(fs.readFileSync(path.join(dir, 'result.json'), 'utf8')) : { exitCode: r.status };
  console.log(`run ${run} ${driver.padEnd(7)} ${s.game}/${name}: exit ${result.exitCode}, ${Math.round(result.wallMs ?? 0)} ms`);
}

for (let run = 1; run <= runs; run++) {
  const order = run % 2 ? drivers : [...drivers].reverse();
  for (const s of scenarios) for (const driver of order) play(s, driver, run);
}
console.log(`results: ${out}`);

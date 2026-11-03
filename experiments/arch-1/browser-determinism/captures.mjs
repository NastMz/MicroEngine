// Replays the selected scenarios with the probe's per-step tracing off (MICROENGINE_T0=0, so frames are paced as in
// CI) and reports, per scenario, the FrameBlend each capture was drawn at in two browser runs and whether the
// captures are byte-identical between the runs and with the desktop build (raylib, fixed step).
//
//   node captures.mjs --copy DIR --out DIR [--games A,B] [--browser chrome] [--load N] [--skip-desktop]
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const args = process.argv.slice(2);
const option = (name, fallback) => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : fallback; };
const copy = path.resolve(option('--copy', ''));
const out = path.resolve(option('--out', ''));
const games = option('--games', 'MeadowRun,CourtyardKey,NeonRift,TrailDuel').split(',');
const browser = option('--browser', 'chrome');
const load = option('--load', '0');
const { scenarios } = JSON.parse(fs.readFileSync(path.join(copy, 'web/tests/scenarios.json'), 'utf8'));
const node = (script, ...rest) => spawnSync(process.execPath, [path.join(here, script), ...rest], { encoding: 'utf8', maxBuffer: 1 << 28 });
const blends = file => fs.readFileSync(file, 'utf8').split(/\r?\n/).filter(l => l.startsWith('T0C '))
  .map(l => { const [, name, u, blend] = l.split(' '); return `${name}@${u.slice(2)}:${Number(blend.slice(6)).toFixed(2)}`; });

for (const s of scenarios.filter(s => games.includes(s.game))) {
  const name = path.basename(s.script, '.script');
  const dir = label => path.join(out, label, s.game, name);
  const trace = label => path.join(out, `${s.game}.${name}.${label}.txt`);
  if (!args.includes('--skip-desktop')) {
    node('run-desktop.mjs', '--copy', copy, '--game', s.game, '--script', s.script, '--out', trace('desktop'), '--captures', dir('desktop'), '--env', 'MICROENGINE_T0=0');
  }
  for (const label of ['browser1', 'browser2']) {
    const r = node('run-browser.mjs', '--copy', copy, '--game', s.game, '--script', s.script, '--browser', browser, '--load', load,
      '--out', trace(label), '--captures', dir(label), '--env', 'MICROENGINE_T0=0');
    if (r.status !== 0) console.log(r.stdout + r.stderr);
  }
  console.log(`== ${s.game} ${s.script}`);
  for (const label of ['browser1', 'browser2']) console.log(`  ${label} blends: ${blends(trace(label)).join(' ')}`);
  console.log(`  browser1 vs browser2: ${node('diffcaps.mjs', dir('browser1'), dir('browser2')).stdout.trim()}`);
  if (fs.existsSync(dir('desktop'))) console.log(`  desktop vs browser1: ${node('diffcaps.mjs', dir('desktop'), dir('browser1')).stdout.trim()}`);
}

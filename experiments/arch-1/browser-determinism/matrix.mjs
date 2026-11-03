// Runs the investigation matrix: every selected scenario of web/tests/scenarios.json on the probed desktop build
// (raylib twice, SDL, headless) and, with --browser, in the browser twice; then compares the traces in pairs.
//
//   node matrix.mjs --copy DIR --traces DIR [--games A,B] [--browser chrome] [--load N] [--skip-desktop]
//
// Prints one line per pair and component: "identical" or the first diverging update (compare.mjs).
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const args = process.argv.slice(2);
const option = (name, fallback) => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : fallback; };
const copy = path.resolve(option('--copy', ''));
const traces = path.resolve(option('--traces', ''));
const games = option('--games', 'MeadowRun,CourtyardKey,NeonRift,TrailDuel').split(',');
const browser = option('--browser', '');
const load = option('--load', '0');
const { scenarios } = JSON.parse(fs.readFileSync(path.join(copy, 'web/tests/scenarios.json'), 'utf8'));

const node = (script, ...rest) => spawnSync(process.execPath, [path.join(here, script), ...rest], { encoding: 'utf8', maxBuffer: 1 << 28 });
const file = (s, label) => path.join(traces, `${s.game}.${path.basename(s.script, '.script')}.${label}.txt`);
const caps = (s, label) => path.join(traces, 'captures', label, s.game, path.basename(s.script, '.script'));

for (const s of scenarios.filter(s => games.includes(s.game))) {
  const runs = [];
  if (!args.includes('--skip-desktop')) {
    for (const [label, platform] of [['raylib1', 'raylib'], ['raylib2', 'raylib'], ['sdl', 'sdl'], ['headless', 'headless']]) {
      const r = node('run-desktop.mjs', '--copy', copy, '--game', s.game, '--script', s.script, '--platform', platform, '--out', file(s, label),
        ...(platform === 'headless' ? [] : ['--captures', caps(s, label)]));
      process.stdout.write(r.stdout);
      runs.push(label);
    }
  }
  if (browser) {
    for (const label of [`${browser}1`, `${browser}2`]) {
      const r = node('run-browser.mjs', '--copy', copy, '--game', s.game, '--script', s.script, '--browser', browser, '--load', load,
        '--out', file(s, label), '--captures', caps(s, label));
      process.stdout.write(r.stdout + r.stderr);
      runs.push(label);
    }
  }
  const pairs = [['raylib1', 'raylib2'], ['raylib1', 'sdl'], ['raylib1', 'headless'], [`${browser}1`, `${browser}2`], ['raylib1', `${browser}1`], ['raylib1', `${browser}2`]]
    .filter(([a, b]) => fs.existsSync(file(s, a)) && fs.existsSync(file(s, b)) && (runs.includes(a) || runs.includes(b)));
  for (const [a, b] of pairs) {
    const r = node('compare.mjs', file(s, a), file(s, b));
    console.log(`== ${s.game} ${s.script}: ${a} vs ${b}\n${r.stdout.trimEnd()}`);
  }
}

// ARCH-1 P1: start-up, frame time and memory from run.mjs results, as Markdown tables (median and min-max over runs).
//
//   node experiments/arch-1/p1-sdl-gpu-driver/summary.mjs --out DIR [--drivers default,gpu,...]
//
// start-up   process start to the first frame: Measure's time to the launcher's "platform:" line (runtime start, SDL
//            video, window, renderer, gamepads) plus the profile's "start screen", "load" and "first frame" phases
// renderer   SDL_CreateRenderer alone (the SDL platform's start.renderer step)
// frame      the profile's median frame interval after its 60 warm-up frames; scripted runs use a fixed step on a
//            hidden window, so frames are unpaced: the interval is the whole CPU cost of a frame, submission included
// present    the profile's median EndFrame time (finish the frame, present; no VSync here)
// memory     peak commit (PeakPagefileUsage) and peak working set of the process
// It also lists, per driver, the SDL backend and adapter the logs name and every other distinct log line.
import fs from 'node:fs';
import path from 'node:path';

const args = process.argv.slice(2);
const option = (name, fallback) => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : fallback; };
const out = path.resolve(option('--out', ''));
const drivers = option('--drivers', fs.readdirSync(out).filter(d => !d.startsWith('_') && fs.statSync(path.join(out, d)).isDirectory()).sort((a, b) => (a === 'default' ? -1 : b === 'default' ? 1 : a.localeCompare(b))).join(',')).split(',');

const median = xs => { const s = [...xs].sort((a, b) => a - b); const m = s.length >> 1; return s.length % 2 ? s[m] : (s[m - 1] + s[m]) / 2; };
const scenarios = new Set();
const data = {}; // driver -> scenario -> metric -> [values]
const logs = {}; // driver -> line -> count
for (const d of drivers) {
  data[d] = {}; logs[d] = new Map();
  for (const run of fs.readdirSync(path.join(out, d)).filter(r => /^run\d+$/.test(r))) {
    const root = path.join(out, d, run);
    for (const game of fs.readdirSync(root)) for (const script of fs.readdirSync(path.join(root, game))) {
      const dir = path.join(root, game, script), key = `${game}/${script}`;
      scenarios.add(key);
      const m = (data[d][key] ??= { startup: [], renderer: [], frameUs: [], frameP95Us: [], presentUs: [], commitMB: [], workingSetMB: [], exit: [] });
      const result = JSON.parse(fs.readFileSync(path.join(dir, 'result.json'), 'utf8'));
      m.exit.push(result.exitCode);
      const log = fs.readFileSync(path.join(dir, 'log.txt'), 'utf8');
      for (const line of log.split(/\r?\n/)) {
        if (!line || /^out: (capture|phase|frames|interval|work|present|motion|steps|profile|done|audio)\b/.test(line) || /^out: platform: /.test(line)) continue;
        const text = line.replace(/\d+(\.\d+)? ms/g, 'N ms');
        logs[d].set(text, (logs[d].get(text) ?? 0) + 1);
      }
      if (result.exitCode !== 0 || !fs.existsSync(path.join(dir, 'profile.json'))) continue;
      const profile = JSON.parse(fs.readFileSync(path.join(dir, 'profile.json'), 'utf8'));
      const phase = name => profile.phases.find(p => p.phase === name)?.ms ?? 0;
      m.startup.push(result.reportMs + phase('start screen') + phase('load') + phase('first frame'));
      m.renderer.push(parseFloat(profile.environment['start.renderer']));
      m.frameUs.push(profile.summary.intervalMs.p50 * 1000);
      m.frameP95Us.push(profile.summary.intervalMs.p95 * 1000);
      m.presentUs.push(profile.summary.presentMs.p50 * 1000);
      m.commitMB.push(result.peakPrivateBytes / 2 ** 20);
      m.workingSetMB.push(result.peakWorkingSetBytes / 2 ** 20);
      m.env ??= profile.environment;
    }
  }
}

const cell = xs => xs.length ? `${Math.round(median(xs))} (${Math.round(Math.min(...xs))}-${Math.round(Math.max(...xs))})` : 'n/a';
const keys = [...scenarios].sort();
function table(metric, title) {
  console.log(`\n### ${title}\n`);
  console.log(`| Scenario | ${drivers.join(' | ')} |`);
  console.log(`| --- | ${drivers.map(() => '---').join(' | ')} |`);
  for (const k of keys) console.log(`| ${k} | ${drivers.map(d => cell(data[d][k]?.[metric] ?? [])).join(' | ')} |`);
  // Overall: median over scenarios of each scenario's median, and the ratio to the first driver.
  const overall = drivers.map(d => median(keys.map(k => median(data[d][k]?.[metric] ?? [NaN]))));
  console.log(`| **median of scenarios** | ${overall.map((v, i) => `**${Math.round(v)}**${i ? ` (x${(v / overall[0]).toFixed(2)})` : ''}`).join(' | ')} |`);
}
console.log(`## Results (${out})`);
console.log('\nmedian (min-max) over the runs of each scenario; the last row is the median over scenarios and its ratio to the first column.');
console.log('\n| Driver | runs | exit codes | renderer | SDL | adapter (profile) |');
console.log('| --- | --- | --- | --- | --- | --- |');
for (const d of drivers) {
  const all = Object.values(data[d]);
  const env = all.find(m => m.env)?.env ?? {};
  const exits = all.flatMap(m => m.exit);
  console.log(`| ${d} | ${Math.max(...all.map(m => m.exit.length))} | ${exits.filter(e => e === 0).length}/${exits.length} zero | ${env.renderer} | ${env.sdl} | ${env.adapter ?? 'not reported'} |`);
}
table('startup', 'Start-up: process start to first frame (ms)');
table('renderer', 'SDL_CreateRenderer (ms)');
table('frameUs', 'Frame interval p50, unpaced (µs)');
table('frameP95Us', 'Frame interval p95, unpaced (µs)');
table('presentUs', 'EndFrame/present p50 (µs)');
table('commitMB', 'Peak commit (MB)');
table('workingSetMB', 'Peak working set (MB)');
console.log('\n### Log lines other than the launcher\'s own (count over all runs and scenarios)\n');
for (const d of drivers) {
  console.log(`- **${d}**`);
  for (const [line, n] of [...logs[d]].sort((a, b) => b[1] - a[1])) console.log(`  - ${n} x \`${line.slice(0, 200).replaceAll('`', "'")}\``);
}

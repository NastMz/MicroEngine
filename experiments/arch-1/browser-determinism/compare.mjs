// Compares two state traces (run-desktop.mjs, run-browser.mjs) step by step.
//
//   node compare.mjs A B
//
// Prints, per state component (rnd, scn, game, aud, inp, ast, top, t), the first update whose hash differs, the
// steps traced by each side, how frames grouped steps (frames with 0, 1, 2+ steps) and the FrameBlend each capture
// was drawn at. Exit code 1 when any component differs.
import fs from 'node:fs';

const [a, b] = process.argv.slice(2).map(file => parse(fs.readFileSync(file, 'utf8')));
const components = ['t', 'rnd', 'scn', 'game', 'aud', 'inp', 'ast', 'top'];

function parse(text) {
  const steps = new Map(), frames = [], captures = [];
  for (const line of text.split(/\r?\n/)) {
    const fields = Object.fromEntries(line.split(' ').slice(1).map(kv => kv.split('=')).filter(kv => kv.length === 2));
    if (line.startsWith('T0 ')) steps.set(Number(fields.u), fields);
    else if (line.startsWith('T0F ')) frames.push(fields);
    else if (line.startsWith('T0C ')) captures.push({ name: line.split(' ')[1], ...fields });
  }
  return { steps, frames, captures };
}

function grouping(trace) {
  const counts = { 0: 0, 1: 0, '2+': 0 };
  for (const f of trace.frames) counts[Number(f.steps) >= 2 ? '2+' : f.steps]++;
  return `frames with 0/1/2+ steps: ${counts[0]}/${counts[1]}/${counts['2+']}`;
}

let differs = false;
const updates = [...a.steps.keys()].filter(u => b.steps.has(u)).sort((x, y) => x - y);
console.log(`steps: ${a.steps.size} vs ${b.steps.size} (${updates.length} in common)`);
console.log(`A ${grouping(a)}`);
console.log(`B ${grouping(b)}`);
for (const c of components) {
  const first = updates.find(u => a.steps.get(u)[c] !== b.steps.get(u)[c]);
  if (first === undefined) { console.log(`${c.padEnd(5)} identical`); continue; }
  differs = true;
  const differing = updates.filter(u => a.steps.get(u)[c] !== b.steps.get(u)[c]);
  console.log(`${c.padEnd(5)} first differs at update ${first} (${differing.length} of ${updates.length} steps differ): ${a.steps.get(first)[c]} vs ${b.steps.get(first)[c]}`);
  // Which updates, with each side's position in its frame (s=1: the first step after a render).
  if (differing.length < updates.length) {
    console.log(`      at ${differing.slice(0, 12).map(u => `${u}(s${a.steps.get(u).s}/s${b.steps.get(u).s})`).join(' ')}${differing.length > 12 ? ' ...' : ''}`);
  }
}
const blends = new Map(b.captures.map(c => [c.name, c]));
for (const c of a.captures) {
  const other = blends.get(c.name);
  console.log(`capture ${c.name}: A u=${c.u} blend=${c.blend}; B ${other ? `u=${other.u} blend=${other.blend}` : 'missing'}`);
}
process.exit(differs ? 1 : 0);

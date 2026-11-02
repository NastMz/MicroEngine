// Experiment: compare MicroEngine's per-platform visual references with each other using the repository's own
// comparison (web/tests/captures.mjs compare, tolerance 3). How many captures would pass against another platform's
// reference? Read-only: imports the comparison and reads the reference JSON files.
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
const repo = process.argv[2] ?? 'C:/Proyectos/MicroEngine';
const { compare } = await import(pathToFileURL(path.join(repo, 'web/tests/captures.mjs')).href);
const dir = path.join(repo, 'web/tests/reference');
const refs = Object.fromEntries(fs.readdirSync(dir).map(f => [f.replace('.json', ''), JSON.parse(fs.readFileSync(path.join(dir, f), 'utf8'))]));
const names = Object.keys(refs);
const worstOf = (a, b) => { let w = 0; const x = Buffer.from(a, 'base64'), y = Buffer.from(b, 'base64'); for (let i = 0; i < x.length; i++) w = Math.max(w, Math.abs(x[i] - y[i])); return w; };
console.log('actual -> reference: identical / within tol 3 / failing (of 138); median worst-cell delta of failing');
for (const a of names) for (const b of names) {
  if (a === b) continue;
  const r = compare(refs[a].captures, refs[b]);
  const fails = Object.keys(refs[a].captures).filter(n => refs[b].captures[n] && refs[a].captures[n].pixels !== refs[b].captures[n].pixels)
    .map(n => worstOf(refs[a].captures[n].grid, refs[b].captures[n].grid)).filter(w => w > 3).sort((p, q) => p - q);
  console.log(`${a.padEnd(24)} -> ${b.padEnd(24)} ${String(r.identical).padStart(3)} / ${String(r.notes.length).padStart(3)} / ${String(r.problems.length).padStart(3)}  median ${fails.length ? fails[fails.length >> 1] : '-'}  max ${fails.length ? fails.at(-1) : '-'}`);
}

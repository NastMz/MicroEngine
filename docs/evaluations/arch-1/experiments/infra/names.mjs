import fs from 'node:fs'; import path from 'node:path'; import { pathToFileURL } from 'node:url';
const repo = 'C:/Proyectos/MicroEngine';
const { compare } = await import(pathToFileURL(path.join(repo, 'web/tests/captures.mjs')).href);
const ref = n => JSON.parse(fs.readFileSync(path.join(repo, 'web/tests/reference', n + '.json'), 'utf8'));
for (const [a, b] of process.argv.slice(2).map(p => p.split(':'))) {
  const r = compare(ref(a).captures, ref(b));
  const byScenario = {}; for (const p of r.problems) { const k = p.split('/').slice(0, 2).join('/'); byScenario[k] = (byScenario[k] || 0) + 1; }
  console.log(a, '->', b, JSON.stringify(byScenario));
}

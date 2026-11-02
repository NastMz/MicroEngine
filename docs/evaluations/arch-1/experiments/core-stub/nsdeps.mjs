// Namespace dependency map: an edge A -> B when a file in namespace A names a type declared in namespace B.
// Comments and string literals are stripped first. Type names are matched lexically (approximation, documented in A1-core.md).
import fs from 'fs'; import path from 'path';
const root = process.argv[2];
const files = [];
(function walk(d) {
  for (const e of fs.readdirSync(d, { withFileTypes: true })) {
    if (e.name === 'obj' || e.name === 'bin') continue;
    const p = path.join(d, e.name);
    if (e.isDirectory()) walk(p); else if (p.endsWith('.cs')) files.push(p);
  }
})(root);
const typesByNs = {}; const nsOf = {}; const publicByNs = {};
const strip = (t) => t.replace(/\/\/.*$/gm, '').replace(/"(?:[^"\\\n]|\\.)*"/g, '""');
for (const f of files) {
  const t = fs.readFileSync(f, 'utf8');
  const m = t.match(/^namespace\s+([\w.]+)/m); const ns = m ? m[1] : '?'; nsOf[f] = ns;
  for (const tm of t.matchAll(/^(public|internal)\s+(?:sealed\s+|abstract\s+|static\s+|readonly\s+|partial\s+|unsafe\s+|ref\s+)*(?:class|interface|struct|enum|record(?:\s+struct)?)\s+(\w+)/gm)) {
    (typesByNs[ns] ??= new Set()).add(tm[2]);
    if (tm[1] === 'public') (publicByNs[ns] ??= new Set()).add(tm[2]);
  }
}
const owner = {};
for (const [ns, s] of Object.entries(typesByNs)) for (const t of s) owner[t] = owner[t] ? owner[t] + '|' + ns : ns;
const edges = {}; const detail = {};
for (const f of files) {
  const t = strip(fs.readFileSync(f, 'utf8')); const ns = nsOf[f];
  const words = new Set(t.match(/\b[A-Z]\w*\b/g) || []);
  for (const w of words) {
    const o = owner[w]; if (!o) continue;
    for (const tns of o.split('|')) {
      if (tns === ns) continue;
      const k = ns + ' -> ' + tns;
      edges[k] = (edges[k] || 0) + 1;
      (detail[k] ??= new Set()).add(w + '@' + path.basename(f));
    }
  }
}
for (const [k, v] of Object.entries(edges).sort()) console.log(k.padEnd(50), String(v).padStart(3), [...detail[k]].slice(0, 10).join(' '));
console.log('\nTypes per namespace (public / all top-level):');
for (const [ns, s] of Object.entries(typesByNs)) console.log(ns.padEnd(24), (publicByNs[ns]?.size ?? 0), '/', s.size);
// cycles between namespaces
const adj = {}; for (const k of Object.keys(edges)) { const [a, b] = k.split(' -> '); (adj[a] ??= new Set()).add(b); }
const pairs = [];
for (const [a, s] of Object.entries(adj)) for (const b of s) if (adj[b]?.has(a) && a < b) pairs.push(a + ' <-> ' + b);
console.log('\nMutual (2-cycle) dependencies:'); for (const p of pairs) console.log('  ' + p);

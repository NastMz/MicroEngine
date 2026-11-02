// ARCH-1 P1: compares the captures run.mjs wrote, by properties rather than as one pixel-equality verdict.
//
//   node experiments/arch-1/p1-sdl-gpu-driver/compare.mjs --out DIR [--base default] [--variants gpu,gpu-vulkan,...]
//        [--reference web/tests/reference/sdl.json] [--json FILE] [--diff-images DIR]
//
// For every capture of run 1, each variant against the base driver:
//   exact      identical RGBA pixels and size
//   diffPixels pixels where any channel differs; maxDiff the largest per-channel difference (0-255)
//   onEdges    how many differing pixels lie on the base image's edges, dilated by one pixel: a pixel is an edge when
//              one of its 8 neighbours has another colour; dilation adds the 8 neighbours of every edge pixel
//   bands      differing pixels by largest channel difference: 1, 2-3, 4-15, 16+
// Determinism: every later run of each driver against its own run 1 (exact count).
// Reference: each driver's run 1 against a repository visual reference (web/tests/captures.mjs compare: hash equality
// reported, grid-cell tolerance 3 enforced), as CI checks it. The asset gallery has no reference entry.
import fs from 'node:fs';
import zlib from 'node:zlib';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { decodePng, summarise, compare } from '../../../web/tests/captures.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const repo = path.resolve(here, '../../..');
const args = process.argv.slice(2);
const option = (name, fallback) => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : fallback; };
const out = path.resolve(option('--out', ''));
const base = option('--base', 'default');
const variants = option('--variants', fs.readdirSync(out).filter(d => d !== base && !d.startsWith('_') && fs.statSync(path.join(out, d)).isDirectory()).join(',')).split(',');
const referenceFile = path.resolve(repo, option('--reference', 'web/tests/reference/sdl.json'));
const jsonOut = option('--json', '');

/** Every capture of one driver's run, as "Game/script/file.png" -> absolute path. */
function captures(driver, run) {
  const root = path.join(out, driver, `run${run}`);
  const found = {};
  if (!fs.existsSync(root)) return found;
  for (const game of fs.readdirSync(root)) for (const script of fs.readdirSync(path.join(root, game)))
    for (const file of fs.readdirSync(path.join(root, game, script)).filter(f => f.endsWith('.png')))
      found[`${game}/${script}/${file}`] = path.join(root, game, script, file);
  return found;
}
const runsOf = driver => fs.readdirSync(path.join(out, driver)).filter(d => /^run\d+$/.test(d)).map(d => Number(d.slice(3))).sort((a, b) => a - b);

/** Edge mask of an image (8-neighbour colour change), dilated by one pixel. */
function edgeMask({ width, height, rgba }) {
  const edge = new Uint8Array(width * height);
  const same = (a, b) => rgba[a] === rgba[b] && rgba[a + 1] === rgba[b + 1] && rgba[a + 2] === rgba[b + 2] && rgba[a + 3] === rgba[b + 3];
  for (let y = 0; y < height; y++) for (let x = 0; x < width; x++) {
    const p = (y * width + x) * 4;
    for (let dy = -1; dy <= 1 && !edge[y * width + x]; dy++) for (let dx = -1; dx <= 1; dx++) {
      const nx = x + dx, ny = y + dy;
      if ((dx || dy) && nx >= 0 && ny >= 0 && nx < width && ny < height && !same(p, (ny * width + nx) * 4)) { edge[y * width + x] = 1; break; }
    }
  }
  const dilated = new Uint8Array(width * height);
  for (let y = 0; y < height; y++) for (let x = 0; x < width; x++) {
    if (!edge[y * width + x]) continue;
    for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) {
      const nx = x + dx, ny = y + dy;
      if (nx >= 0 && ny >= 0 && nx < width && ny < height) dilated[ny * width + nx] = 1;
    }
  }
  return dilated;
}

/** Writes RGBA pixels as an 8-bit RGBA PNG (filter 0). */
function encodePng(file, width, height, rgba) {
  const crcTable = Array.from({ length: 256 }, (_, n) => { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; return c >>> 0; });
  const crc = buf => { let c = 0xffffffff; for (const b of buf) c = crcTable[(c ^ b) & 0xff] ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; };
  const chunk = (type, body) => {
    const head = Buffer.alloc(8); head.writeUInt32BE(body.length, 0); head.write(type, 4, 'latin1');
    const tail = Buffer.alloc(4); tail.writeUInt32BE(crc(Buffer.concat([head.subarray(4), body])), 0);
    return Buffer.concat([head, body, tail]);
  };
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(width, 0); ihdr.writeUInt32BE(height, 4); ihdr.set([8, 6, 0, 0, 0], 8);
  const raw = Buffer.alloc((width * 4 + 1) * height);
  for (let y = 0; y < height; y++) rgba.copy(raw, y * (width * 4 + 1) + 1, y * width * 4, (y + 1) * width * 4);
  fs.writeFileSync(file, Buffer.concat([Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]), chunk('IHDR', ihdr), chunk('IDAT', zlib.deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]));
}

/** With --diff-images DIR: base image dimmed to 25 %, differing pixels in magenta. */
function writeDiffImage(name, a, b) {
  const dir = option('--diff-images', '');
  if (!dir) return;
  const img = Buffer.alloc(a.rgba.length);
  for (let i = 0; i < a.width * a.height; i++) {
    let d = 0;
    for (let k = 0; k < 4; k++) d = Math.max(d, Math.abs(a.rgba[i * 4 + k] - b.rgba[i * 4 + k]));
    if (d) img.set([255, 0, 255, 255], i * 4);
    else img.set([a.rgba[i * 4] >> 2, a.rgba[i * 4 + 1] >> 2, a.rgba[i * 4 + 2] >> 2, 255], i * 4);
  }
  fs.mkdirSync(dir, { recursive: true });
  encodePng(path.join(dir, name.replaceAll('/', '_')), a.width, a.height, img);
}

function diff(fileA, fileB, name) {
  const a = decodePng(fs.readFileSync(fileA)), b = decodePng(fs.readFileSync(fileB));
  if (a.width !== b.width || a.height !== b.height) return { sizeMismatch: `${a.width}x${a.height} vs ${b.width}x${b.height}` };
  if (a.rgba.equals(b.rgba)) return { exact: true, pixels: a.width * a.height };
  if (name) writeDiffImage(name, a, b);
  const mask = edgeMask(a);
  let diffPixels = 0, maxDiff = 0, onEdges = 0;
  const bands = [0, 0, 0, 0];
  const box = [Infinity, Infinity, -1, -1];
  for (let i = 0; i < a.width * a.height; i++) {
    let d = 0;
    for (let k = 0; k < 4; k++) d = Math.max(d, Math.abs(a.rgba[i * 4 + k] - b.rgba[i * 4 + k]));
    if (!d) continue;
    diffPixels++;
    maxDiff = Math.max(maxDiff, d);
    if (mask[i]) onEdges++;
    bands[d === 1 ? 0 : d <= 3 ? 1 : d <= 15 ? 2 : 3]++;
    const x = i % a.width, y = Math.floor(i / a.width);
    box[0] = Math.min(box[0], x); box[1] = Math.min(box[1], y); box[2] = Math.max(box[2], x); box[3] = Math.max(box[3], y);
  }
  const edgeShare = mask.reduce((s, v) => s + v, 0) / mask.length;
  return { exact: false, pixels: a.width * a.height, diffPixels, maxDiff, onEdges, bands, box, edgeShare };
}

const report = { base, variants: {}, determinism: {}, reference: {} };
const baseCaptures = captures(base, 1);
for (const v of variants) {
  const got = captures(v, 1);
  const rows = {};
  for (const name of Object.keys(baseCaptures).sort()) rows[name] = got[name] ? diff(baseCaptures[name], got[name], `${v}/${name}`) : { missing: true };
  for (const name of Object.keys(got)) if (!baseCaptures[name]) rows[name] = { extra: true };
  report.variants[v] = rows;
}
for (const d of [base, ...variants]) {
  const first = captures(d, 1);
  const runs = runsOf(d).filter(r => r > 1);
  let identical = 0, total = 0;
  const differing = [];
  for (const r of runs) {
    const later = captures(d, r);
    for (const [name, file] of Object.entries(first)) {
      total++;
      if (later[name] && fs.readFileSync(later[name]).equals(fs.readFileSync(file))) identical++;
      else if (later[name] && diff(file, later[name]).exact) identical++; // same pixels, different PNG encoding
      else differing.push(`run${r} ${name}`);
    }
  }
  report.determinism[d] = { runs: runs.length + 1, identical, total, differing: differing.slice(0, 20) };
}
if (fs.existsSync(referenceFile)) {
  const reference = JSON.parse(fs.readFileSync(referenceFile, 'utf8'));
  for (const d of [base, ...variants]) {
    const summaries = {};
    for (const [name, file] of Object.entries(captures(d, 1))) if (reference.captures[name]) summaries[name] = summarise(file);
    const r = compare(summaries, reference);
    report.reference[d] = { file: path.relative(repo, referenceFile).replaceAll('\\', '/'), identical: r.identical, total: r.total, expected: Object.keys(reference.captures).length, problems: r.problems, notes: r.notes.length };
  }
}

// Console summary
console.log(`base: ${base}, ${Object.keys(baseCaptures).length} captures`);
for (const [v, rows] of Object.entries(report.variants)) {
  const list = Object.entries(rows);
  const exact = list.filter(([, r]) => r.exact).length;
  console.log(`\n${v} vs ${base}: ${exact}/${list.length} exact`);
  for (const [name, r] of list) {
    if (r.exact) continue;
    if (r.missing || r.extra || r.sizeMismatch) { console.log(`  ${name}: ${r.missing ? 'missing' : r.extra ? 'extra' : r.sizeMismatch}`); continue; }
    console.log(`  ${name}: ${r.diffPixels}/${r.pixels} px (${(100 * r.diffPixels / r.pixels).toFixed(3)}%), max ${r.maxDiff}, on edges ${r.onEdges} (${(100 * r.onEdges / r.diffPixels).toFixed(1)}%; edge mask covers ${(100 * r.edgeShare).toFixed(1)}% of the image), bands 1:${r.bands[0]} 2-3:${r.bands[1]} 4-15:${r.bands[2]} 16+:${r.bands[3]}, box ${r.box.join(',')}`);
  }
}
console.log('\ndeterminism (later runs vs run 1, same driver):');
for (const [d, r] of Object.entries(report.determinism)) console.log(`  ${d}: ${r.identical}/${r.total} identical over ${r.runs} runs${r.differing.length ? `; differ: ${r.differing.join(', ')}` : ''}`);
console.log('\nreference:');
for (const [d, r] of Object.entries(report.reference)) console.log(`  ${d} vs ${r.file}: ${r.identical}/${r.total} exact of ${r.expected} expected, ${r.problems.length} beyond tolerance${r.problems.length ? `: ${r.problems.slice(0, 5).join('; ')}` : ''}`);
if (jsonOut) fs.writeFileSync(jsonOut, JSON.stringify(report, null, 1) + '\n');

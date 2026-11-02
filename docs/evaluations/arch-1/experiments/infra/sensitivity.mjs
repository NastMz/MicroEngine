// Experiment: how sensitive is MicroEngine's visual reference (web/tests/captures.mjs: 32x18 grid of cell means,
// tolerance 3/255) to realistic regressions and to benign renderer changes, on a synthetic 960x540 capture?
// Read-only use of the repository: imports summarise/compare from it, writes PNGs only next to this script.
import fs from 'node:fs';
import path from 'node:path';
import zlib from 'node:zlib';
import { fileURLToPath, pathToFileURL } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const repo = process.argv[2] ?? 'C:/Proyectos/MicroEngine';
const { summarise, compare } = await import(pathToFileURL(path.join(repo, 'web/tests/captures.mjs')).href);

const W = 960, H = 540;
function crc32(buf) { let c, crc = 0xffffffff; for (const b of buf) { c = (crc ^ b) & 0xff; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; crc = (crc >>> 8) ^ c; } return (crc ^ 0xffffffff) >>> 0; }
function chunk(type, body) { const len = Buffer.alloc(4); len.writeUInt32BE(body.length); const tb = Buffer.from(type, 'latin1'); const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(Buffer.concat([tb, body]))); return Buffer.concat([len, tb, body, crc]); }
function png(img) {
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(W, 0); ihdr.writeUInt32BE(H, 4); ihdr[8] = 8; ihdr[9] = 6;
  const raw = Buffer.alloc(H * (W * 4 + 1));
  for (let y = 0; y < H; y++) { raw[y * (W * 4 + 1)] = 0; img.copy(raw, y * (W * 4 + 1) + 1, y * W * 4, (y + 1) * W * 4); }
  return Buffer.concat([Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]), chunk('IHDR', ihdr), chunk('IDAT', zlib.deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]);
}
const blank = () => { const b = Buffer.alloc(W * H * 4); for (let i = 0; i < W * H; i++) { b[i * 4] = 20; b[i * 4 + 1] = 24; b[i * 4 + 2] = 40; b[i * 4 + 3] = 255; } return b; };
const rect = (img, x, y, w, h, [r, g, b]) => { for (let j = y; j < y + h; j++) for (let i = x; i < x + w; i++) { const o = (j * W + i) * 4; img[o] = r; img[o + 1] = g; img[o + 2] = b; } };
// A 5x7 glyph per digit, drawn at scale 3 (pixel-art HUD).
const glyphs = { 1: ['00100', '01100', '00100', '00100', '00100', '00100', '01110'], 7: ['11111', '00001', '00010', '00100', '01000', '01000', '01000'],
  3: ['11110', '00001', '00001', '01110', '00001', '00001', '11110'], 8: ['01110', '10001', '10001', '01110', '10001', '10001', '01110'] };
const digit = (img, d, x, y) => glyphs[d].forEach((row, j) => [...row].forEach((c, i) => { if (c === '1') rect(img, x + i * 3, y + j * 3, 3, 3, [255, 255, 255]); }));

function scene({ heroX = 400, bullet = true, hud = [1, 3], coin = true, gemX = 903 } = {}) {
  const img = blank();
  rect(img, 0, 480, W, 60, [60, 140, 60]);          // ground
  rect(img, heroX, 432, 48, 48, [220, 120, 40]);    // hero, 16 px sprite at scale 3
  if (bullet) rect(img, 610, 450, 6, 6, [255, 240, 120]); // a 2x2 bullet at scale 3
  if (coin) rect(img, 700, 400, 12, 12, [250, 210, 60]);  // a 4x4 coin at scale 3
  rect(img, gemX, 303, 12, 12, [80, 220, 255]);      // a gem inside cell 30,10 (x 900-930, y 300-330)
  hud.forEach((d, k) => digit(img, d, 24 + k * 18, 20));
  return img;
}
function boxBlur(img) { // stand-in for switching nearest -> linear sampling at a non-integer scale
  const out = Buffer.from(img);
  for (let y = 1; y < H - 1; y++) for (let x = 1; x < W - 1; x++) for (let k = 0; k < 3; k++) {
    let s = 0; for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) s += img[((y + dy) * W + x + dx) * 4 + k];
    out[(y * W + x) * 4 + k] = Math.round(s / 9);
  } return out;
}
const shift = (img, d) => img.map((v, i) => (i % 4 === 3 ? v : Math.min(255, Math.max(0, v + d))));

const dir = path.join(here, 'png'); fs.mkdirSync(dir, { recursive: true });
const save = (name, img) => { const f = path.join(dir, name + '.png'); fs.writeFileSync(f, png(img)); return summarise(f); };
const base = save('base', scene());
const cases = {
  'regression: bullet (6x6 px) missing': scene({ bullet: false }),
  'regression: coin (12x12 px) missing': scene({ coin: false }),
  'regression: HUD 13 -> 18': scene({ hud: [1, 8] }),
  'regression: HUD 13 -> 73': scene({ hud: [7, 3] }),
  'regression: hero 3 px to the right (one source pixel)': scene({ heroX: 403 }),
  'regression: hero 12 px to the right': scene({ heroX: 412 }),
  'regression: gem 12 px to the right, same cell': scene({ gemX: 915 }),
  'regression: gem 15 px down... (same cell, y)': scene({ gemX: 903 }),
  'benign: 3x3 blur (sampling/filter change)': boxBlur(scene()),
  'benign: +2 on every channel (driver rounding)': shift(scene(), 2),
  'benign: +4 on every channel (gamma/sRGB rounding)': shift(scene(), 4),
};
const reference = { tolerance: 3, captures: { base } };
for (const [name, img] of Object.entries(cases)) {
  const r = compare({ base: save(name.replace(/\W+/g, '_'), img) }, reference);
  const verdict = r.problems.length ? 'FAIL (detected)' : r.identical ? 'PASS (identical)' : 'PASS (within tolerance)';
  console.log(`${name.padEnd(56)} ${verdict.padEnd(24)} ${r.problems[0] ?? r.notes[0] ?? ''}`);
}

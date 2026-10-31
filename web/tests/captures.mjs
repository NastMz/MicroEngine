// Visual reference for native desktop captures (desktop.mjs --reference). Each capture is summarised by its size, a
// hash of its exact pixels and a fingerprint: the mean colour of each cell of a 32 x 18 grid. A run matches the
// reference when every capture is present with the same size and no cell's mean colour moved by more than the
// tolerance; exact pixel equality is reported, not required, because another GPU driver or Mesa version may round
// blending differently. A missing sprite, a wrong frame or a changed HUD digit moves a cell well past the tolerance.
import crypto from 'node:crypto';
import fs from 'node:fs';
import zlib from 'node:zlib';

export const grid = { columns: 32, rows: 18 };
export const tolerance = 3; // largest allowed change of one cell's mean, per channel, out of 255

/** Decodes the PNGs raylib writes (8-bit RGB or RGBA, not interlaced) into RGBA pixels. */
export function decodePng(buffer) {
  if (buffer.readUInt32BE(0) !== 0x89504e47) throw new Error('not a PNG file');
  let width = 0, height = 0, channels = 0;
  const data = [];
  for (let at = 8; at < buffer.length;) {
    const length = buffer.readUInt32BE(at);
    const type = buffer.toString('latin1', at + 4, at + 8);
    const body = buffer.subarray(at + 8, at + 8 + length);
    if (type === 'IHDR') {
      width = body.readUInt32BE(0);
      height = body.readUInt32BE(4);
      const [depth, colour, , , interlace] = body.subarray(8, 13);
      channels = { 2: 3, 6: 4 }[colour];
      if (depth !== 8 || !channels || interlace) throw new Error(`unsupported PNG (depth ${depth}, colour type ${colour}, interlace ${interlace})`);
    } else if (type === 'IDAT') data.push(body);
    else if (type === 'IEND') break;
    at += 12 + length;
  }
  const raw = zlib.inflateSync(Buffer.concat(data));
  const stride = width * channels;
  const rgba = Buffer.alloc(width * height * 4);
  let previous = Buffer.alloc(stride);
  for (let y = 0; y < height; y++) {
    const filter = raw[y * (stride + 1)];
    const line = Buffer.from(raw.subarray(y * (stride + 1) + 1, (y + 1) * (stride + 1)));
    for (let x = 0; x < stride; x++) {
      const a = x >= channels ? line[x - channels] : 0, b = previous[x], c = x >= channels ? previous[x - channels] : 0;
      const p = a + b - c, pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c);
      const predictor = [0, a, b, (a + b) >> 1, pa <= pb && pa <= pc ? a : pb <= pc ? b : c][filter];
      if (predictor === undefined) throw new Error(`unknown PNG filter ${filter}`);
      line[x] = (line[x] + predictor) & 0xff;
    }
    for (let x = 0; x < width; x++) {
      for (let k = 0; k < 4; k++) rgba[(y * width + x) * 4 + k] = k < channels ? line[x * channels + k] : 255;
    }
    previous = line;
  }
  return { width, height, rgba };
}

/** Size, exact pixel hash and grid fingerprint (mean R, G, B per cell, base64) of one capture file. */
export function summarise(file) {
  const { width, height, rgba } = decodePng(fs.readFileSync(file));
  const cells = Buffer.alloc(grid.columns * grid.rows * 3);
  for (let row = 0; row < grid.rows; row++) {
    for (let column = 0; column < grid.columns; column++) {
      const [x0, x1] = [Math.floor(column * width / grid.columns), Math.floor((column + 1) * width / grid.columns)];
      const [y0, y1] = [Math.floor(row * height / grid.rows), Math.floor((row + 1) * height / grid.rows)];
      const sum = [0, 0, 0];
      for (let y = y0; y < y1; y++) for (let x = x0; x < x1; x++) for (let k = 0; k < 3; k++) sum[k] += rgba[(y * width + x) * 4 + k];
      const count = (x1 - x0) * (y1 - y0);
      for (let k = 0; k < 3; k++) cells[(row * grid.columns + column) * 3 + k] = Math.round(sum[k] / count);
    }
  }
  return { size: [width, height], pixels: crypto.createHash('sha256').update(rgba).digest('hex'), grid: cells.toString('base64') };
}

/**
 * Compares summaries (name -> summary) with a reference. Returns the problems that fail the check and notes that
 * only inform (exact pixels differing within the tolerance). With namesOnly, only names and sizes must match: a
 * platform without its own reference yet (macOS) is checked for completeness, and its differences are reported.
 */
export function compare(actual, reference, { namesOnly = false } = {}) {
  const problems = [], notes = [];
  let identical = 0;
  for (const name of Object.keys(reference.captures)) if (!actual[name]) problems.push(`${name}: missing`);
  for (const [name, got] of Object.entries(actual)) {
    const want = reference.captures[name];
    if (!want) { problems.push(`${name}: not in the reference`); continue; }
    if (got.size.join('x') !== want.size.join('x')) { problems.push(`${name}: ${got.size.join('x')}, expected ${want.size.join('x')}`); continue; }
    if (got.pixels === want.pixels) { identical++; continue; }
    const [a, b] = [Buffer.from(got.grid, 'base64'), Buffer.from(want.grid, 'base64')];
    let worst = 0, cell = 0;
    for (let i = 0; i < a.length; i++) if (Math.abs(a[i] - b[i]) > worst) { worst = Math.abs(a[i] - b[i]); cell = Math.floor(i / 3); }
    const where = `cell ${cell % grid.columns},${Math.floor(cell / grid.columns)}`;
    if (worst > reference.tolerance && !namesOnly) problems.push(`${name}: differs from the reference (${where} moved by ${worst})`);
    else notes.push(`${name}: pixels differ, largest cell change ${worst} at ${where}${worst > reference.tolerance ? ' (beyond the tolerance)' : ''}`);
  }
  return { problems, notes, identical, total: Object.keys(actual).length };
}

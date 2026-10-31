// Reads excerpts of the repository's real source files at build time, so the site never quotes code that has
// drifted from the engine. A missing file or marker fails the build instead of shipping stale text.
import fs from 'node:fs';
import path from 'node:path';
import { repoRoot } from './repo-docs.mjs';

/** Lines of `file` from the first line matching `start` to the line before the first match of `end` after it. */
export function excerpt(file, start, end) {
  const lines = fs.readFileSync(path.join(repoRoot, file), 'utf8').replace(/\r\n/g, '\n').split('\n');
  const from = lines.findIndex(line => start.test(line));
  if (from < 0) throw new Error(`${file}: no line matches ${start}`);
  const to = end ? lines.findIndex((line, i) => i > from && end.test(line)) : lines.length;
  if (to < 0) throw new Error(`${file}: no line after ${start} matches ${end}`);
  const block = lines.slice(from, to);
  while (block.length && !block.at(-1).trim()) block.pop();
  const indent = Math.min(...block.filter(l => l.trim()).map(l => l.match(/^ */)[0].length));
  return { code: block.map(l => l.slice(indent)).join('\n'), firstLine: from + 1, lastLine: from + block.length };
}

/**
 * The first `PixelImage.FromMap(palette, ...)` sprite in a C# file, with the colours of its `Art.Palette(...)`:
 * `{ width, height, pixels: [{ x, y, color }] }`. '.' is transparent, as in the engine.
 */
export function pixelMap(file) {
  const text = fs.readFileSync(path.join(repoRoot, file), 'utf8');
  const palette = text.match(/Art\.Palette\(([^)]*)\)/);
  const map = text.match(/PixelImage\.FromMap\(\s*\w+\s*,([\s\S]*?)\);/);
  if (!palette || !map) throw new Error(`${file}: no Art.Palette(...) and PixelImage.FromMap(...) pair`);
  const colors = Object.fromEntries([...palette[1].matchAll(/"(.)=(#[0-9a-fA-F]{6})"/g)].map(m => [m[1], m[2]]));
  const rows = [...map[1].matchAll(/"([^"]*)"/g)].map(m => m[1]);
  const pixels = rows.flatMap((row, y) => [...row].flatMap((ch, x) => colors[ch] ? [{ x, y, color: colors[ch] }] : []));
  return { width: Math.max(...rows.map(r => r.length)), height: rows.length, pixels };
}

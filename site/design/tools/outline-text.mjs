#!/usr/bin/env node
// Turns the few fixed strings of the brand assets (the wordmark, the Open Graph line and the socket legends) into SVG
// outlines, so the logo files and the build-time Open Graph image do not depend on fonts being installed where they
// are rasterised. Output: src/assets/brand/outlines.json. Run it only when one of the strings or fonts changes:
//   cd site && npm i --no-save opentype.js@1.3.4 && node design/tools/outline-text.mjs
// (opentype.js is a design-time tool, not a dependency of the site.)
import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';

const site = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const require = createRequire(process.env.OPENTYPE_FROM ?? path.join(site, 'package.json'));
const opentype = require('opentype.js');

const font = file => opentype.loadSync(path.join(site, 'node_modules/@fontsource', file));
const display = font('bricolage-grotesque/files/bricolage-grotesque-latin-600-normal.woff');
const body = font('ibm-plex-sans/files/ibm-plex-sans-latin-400-normal.woff');
const mono = font('ibm-plex-mono/files/ibm-plex-mono-latin-500-normal.woff');

// Each entry is laid out at font size 100 with its baseline at y = 0; consumers scale and translate it.
const strings = {
  wordmark: [display, 'MicroEngine', -0.02],
  ogLine: [body, 'A code-first, modular game engine for C# and .NET', 0],
  legendGraphics: [mono, 'GRAPHICS', 0.06],
  legendAudio: [mono, 'AUDIO', 0.06],
  legendInput: [mono, 'INPUT', 0.06],
  legendPlatform: [mono, 'PLATFORM', 0.06],
  legendKernel: [mono, 'KERNEL', 0.06],
};

const size = 100;
const out = {};
for (const [name, [face, text, tracking]] of Object.entries(strings)) {
  let x = 0;
  const parts = [];
  const box = { y1: 0, y2: 0 };
  const glyphs = face.stringToGlyphs(text);
  glyphs.forEach((glyph, i) => {
    const glyphPath = glyph.getPath(x, 0, size);
    const b = glyphPath.getBoundingBox();
    if (glyphPath.commands.length) { box.y1 = Math.min(box.y1, b.y1); box.y2 = Math.max(box.y2, b.y2); }
    parts.push(glyphPath.toPathData(2));
    const kern = i + 1 < glyphs.length ? face.getKerningValue(glyph, glyphs[i + 1]) : 0;
    x += ((glyph.advanceWidth + kern) / face.unitsPerEm) * size + (i + 1 < glyphs.length ? tracking * size : 0);
  });
  const capHeight = ((face.tables.os2.sCapHeight || face.ascender * 0.7) / face.unitsPerEm) * size;
  out[name] = { text, d: parts.join(''), width: Math.round(x * 100) / 100, capHeight: Math.round(capHeight * 100) / 100,
    top: Math.round(box.y1 * 100) / 100, bottom: Math.round(box.y2 * 100) / 100 };
}
const target = path.join(site, 'src/assets/brand/outlines.json');
fs.mkdirSync(path.dirname(target), { recursive: true });
fs.writeFileSync(target, JSON.stringify(out, null, 1) + '\n');
console.log(`wrote ${path.relative(site, target)} (${Object.keys(out).length} strings)`);

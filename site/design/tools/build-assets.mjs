#!/usr/bin/env node
// Writes the standalone logo files in src/assets/brand/ from the brand geometry (src/components/brand/geometry.mjs)
// and the outlined wordmark (src/assets/brand/outlines.json). Run after changing either:
//   cd site && node design/tools/build-assets.mjs
// tests/site.test.mjs fails when the committed files differ from what this script writes.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const site = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const { mark, fixed, markSvg } = await import(pathToFileURL(path.join(site, 'src/components/brand/geometry.mjs')).href);
const outlines = JSON.parse(fs.readFileSync(path.join(site, 'src/assets/brand/outlines.json'), 'utf8'));

/** The horizontal lockup: the mark's drawing as tall as the wordmark's capitals, then the wordmark. */
export function lockupSvg({ board, module, text }) {
  const word = outlines.wordmark;
  const scale = word.capHeight / mark.crop.height;
  const markWidth = mark.crop.width * scale;
  const gap = Math.round(word.capHeight * 0.4);
  const width = Math.ceil(markWidth + gap + word.width);
  const top = Math.floor(word.top) - 1;
  const height = Math.ceil(word.bottom) + 1 - top;
  const place = `translate(0 ${-word.capHeight}) scale(${scale}) translate(0 ${-mark.crop.y})`;
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 ${top} ${width} ${height}" role="img" aria-label="MicroEngine">`
    + '<title>MicroEngine</title>'
    + `<g transform="${place}"><path fill="${board}" d="${mark.board}"/><path fill="${module}" d="${mark.module}"/></g>`
    + `<path fill="${text}" transform="translate(${markWidth + gap} 0)" d="${word.d}"/></svg>`;
}

export function files() {
  return {
    'mark-light.svg': markSvg({ board: fixed.light.board, module: fixed.light.module }),
    'mark-dark.svg': markSvg({ board: fixed.dark.board, module: fixed.dark.module }),
    'mark-mono.svg': markSvg({ board: 'currentColor', module: 'currentColor' }),
    'lockup-light.svg': lockupSvg({ board: fixed.light.board, module: fixed.light.module, text: fixed.light.board }),
    'lockup-dark.svg': lockupSvg({ board: fixed.dark.board, module: fixed.dark.module, text: fixed.dark.board }),
    'lockup-mono.svg': lockupSvg({ board: 'currentColor', module: 'currentColor', text: 'currentColor' }),
  };
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  for (const [name, svg] of Object.entries(files())) {
    fs.writeFileSync(path.join(site, 'src/assets/brand', name), svg + '\n');
    console.log(`wrote src/assets/brand/${name}`);
  }
}

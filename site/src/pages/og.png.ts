// Open Graph image (1200x630), drawn at build time from the brand itself: the lockup, one line saying what MicroEngine
// is, and a board of sockets with seated modules (graphics, audio, input, platform) plus one empty, dashed socket.
// Colours come from tokens.css (dark theme), shapes from the brand geometry, text from pre-outlined paths
// (src/assets/brand/outlines.json), so the result does not depend on fonts installed on the build machine.
import sharp from 'sharp';
import tokensCss from '../styles/tokens.css?raw';
import outlines from '../assets/brand/outlines.json';
import { families, mark } from '../components/brand/geometry.mjs';
import { mix, parseOklch, readTokens, resolve, toLinearSrgb } from '../../scripts/contrast.mjs';

const W = 1200;
const H = 630;
const tokens = readTokens(tokensCss);
const oklch = (name: string) => parseOklch(resolve(tokens, tokens[name], 'dark'));
const hex = (c: { l: number; c: number; h: number }) => '#' + toLinearSrgb(c)
  .map(x => Math.min(1, Math.max(0, x)))
  .map(x => Math.round(255 * (x <= 0.0031308 ? 12.92 * x : 1.055 * x ** (1 / 2.4) - 0.055)).toString(16).padStart(2, '0'))
  .join('');
const colour = (name: string) => hex(oklch(name));
const tint = (name: string) => hex(mix(oklch(name), oklch('--color-paper'), Number.parseFloat(tokens['--tint']) / 100));

type Outline = { d: string; width: number; capHeight: number };
/** Places an outlined string with its baseline at (x, y), scaled so its capitals are `cap` pixels tall. */
const text = (o: Outline, x: number, y: number, cap: number, fill: string) =>
  `<path fill="${fill}" transform="translate(${x} ${y}) scale(${cap / o.capHeight})" d="${o.d}"/>`;

function svg() {
  const paper = colour('--color-paper');
  const ink = colour('--color-ink');
  const parts: string[] = [`<rect width="${W}" height="${H}" fill="${paper}"/>`];

  // Lockup: mark at cap height, then the wordmark.
  const cap = 92;
  const left = 80;
  const baseline = 214;
  const scale = cap / mark.crop.height;
  parts.push(`<g transform="translate(${left} ${baseline - cap}) scale(${scale}) translate(0 ${-mark.crop.y})">`
    + `<path fill="${ink}" d="${mark.board}"/><path fill="${colour('--color-mark-module')}" d="${mark.module}"/></g>`);
  parts.push(text(outlines.wordmark, left + mark.crop.width * scale + cap * 0.3, baseline, cap, ink));
  parts.push(text(outlines.ogLine, left + 2, baseline + 74, 25, colour('--color-ink-2')));

  // The board: four seated modules and one empty socket on the kernel bar.
  const seated = [
    ['graphics', outlines.legendGraphics],
    ['audio', outlines.legendAudio],
    ['input', outlines.legendInput],
    ['platform', outlines.legendPlatform],
  ] as const;
  const count = seated.length + 1;
  const gap = 24;
  const bayW = (W - 2 * left - gap * (count - 1)) / count;
  const bayH = 112;
  const bayTop = 400;
  const legendCap = 13;
  for (let i = 0; i < count; i++) {
    const x = left + i * (bayW + gap);
    const entry = seated[i];
    if (!entry) {
      parts.push(`<rect x="${x + 0.5}" y="${bayTop + 0.5}" width="${bayW - 1}" height="${bayH - 1}" fill="none" `
        + `stroke="${colour('--color-ink-3')}" stroke-width="1.5" stroke-dasharray="6 6"/>`);
      continue;
    }
    const [family, legend] = entry;
    const famColour = colour(`--family-${family}`);
    const icon = families[family];
    // Legend above the socket: the family icon at legend size, then the name.
    const iconScale = (legendCap + 3) / 16;
    parts.push(`<g transform="translate(${x} ${bayTop - 16 - legendCap - 3}) scale(${iconScale})">`
      + `<path fill="${colour('--color-ink-2')}" fill-rule="evenodd" d="${icon.board}"/><path fill="${famColour}" d="${icon.module}"/></g>`);
    parts.push(text(legend, x + legendCap + 12, bayTop - 16, legendCap, famColour));
    // The bay, and the module seated in it with a gap all round.
    parts.push(`<rect x="${x + 0.5}" y="${bayTop + 0.5}" width="${bayW - 1}" height="${bayH - 1}" fill="none" stroke="${colour('--color-rule')}"/>`);
    const inset = 6;
    parts.push(`<rect x="${x + inset}" y="${bayTop + inset}" width="${bayW - 2 * inset}" height="${bayH - 2 * inset}" `
      + `fill="${tint(`--family-${family}`)}" stroke="${famColour}" stroke-width="1.5"/>`);
    const glyph = 44;
    const gx = x + (bayW - glyph) / 2;
    const gy = bayTop + (bayH - glyph) / 2;
    parts.push(`<g transform="translate(${gx} ${gy}) scale(${glyph / 16})"><path fill="${ink}" fill-rule="evenodd" d="${icon.board}"/>`
      + `<path fill="${famColour}" d="${icon.module}"/></g>`);
  }
  // The kernel bar every socket sits on, with its legend.
  const barY = bayTop + bayH + 14;
  parts.push(`<rect x="${left}" y="${barY}" width="${W - 2 * left}" height="10" fill="${ink}"/>`);
  parts.push(text(outlines.legendKernel, left, barY + 10 + 18 + legendCap, legendCap, colour('--color-ink-2')));
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}" viewBox="0 0 ${W} ${H}">${parts.join('')}</svg>`;
}

export async function GET() {
  const png = await sharp(Buffer.from(svg())).png().toBuffer();
  return new Response(new Uint8Array(png), { headers: { 'Content-Type': 'image/png' } });
}

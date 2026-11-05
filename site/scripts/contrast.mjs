#!/usr/bin/env node
// Contrast check for the design tokens: reads src/styles/tokens.css, resolves every colour in the light and the dark
// theme (light-dark(), var()), and checks each text/background pair the site uses against WCAG 2.2 AA:
// 4.5:1 for text, 3:1 for focus rings and other non-text marks. It also fails when a colour lies outside sRGB, where
// the browser would map it and the computed ratio would no longer be the one shown.
//   node scripts/contrast.mjs            prints the table, exits 1 on any failure
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
export const tokensPath = path.join(here, '../src/styles/tokens.css');

const surfaces = ['--color-paper', '--color-paper-2', '--color-paper-3'];
const families = ['kernel', 'graphics', 'physics', 'audio', 'input', 'platform', 'tools'].map(f => `--family-${f}`);
const textOnSurfaces = [
  '--color-ink', '--color-ink-2', '--color-ink-3', '--color-accent', '--color-accent-strong',
  '--color-ok', '--color-partial', '--color-no',
  '--status-available', '--status-next', '--status-repo', '--status-planned',
  ...families, '--color-meadow', '--color-courtyard', '--color-trail', '--color-neon',
];

/** Every pair the check enforces: [foreground, background, minimum ratio, what it is]. A background written as
 * `tint:<token>` is that token mixed into the paper at the --tint percentage (a socket's fill). */
export function pairs() {
  const list = [];
  for (const fg of textOnSurfaces) for (const bg of surfaces) list.push([fg, bg, 4.5, 'text']);
  for (const fam of families) {
    list.push([fam, `tint:${fam}`, 4.5, 'family label on its socket tint']);
    list.push(['--color-ink', `tint:${fam}`, 4.5, 'ink on a socket tint']);
  }
  list.push(['--color-accent-ink', '--color-accent-fill', 4.5, 'text on the amber fill (buttons)']);
  list.push(['--color-accent-ink', '--color-accent-fill-hover', 4.5, 'text on the hovered amber fill']);
  // Starlight: accent text on the low accent (asides, badges), and its inverted text on the accent (buttons).
  for (const fg of ['--color-accent', '--color-accent-strong', '--color-ink']) list.push([fg, '--color-accent-low', 4.5, 'text on the low accent']);
  list.push(['--color-accent-low', '--color-accent', 4.5, 'inverted text on the accent']);
  for (const bg of surfaces) list.push(['--color-focus', bg, 3, 'focus ring']);
  list.push(['--color-mark-board', '--color-paper', 3, 'mark board']);
  return list;
}

/** Reads `--name: value;` declarations from the :root blocks of a stylesheet. */
export function readTokens(css) {
  const tokens = {};
  const body = css.replace(/\/\*[\s\S]*?\*\//g, '');
  for (const block of body.matchAll(/:root\s*\{([^}]*)\}/g)) {
    for (const decl of block[1].matchAll(/(--[\w-]+)\s*:\s*([^;]+);/g)) tokens[decl[1]] = decl[2].trim();
  }
  return tokens;
}

/** Splits the top-level arguments of a CSS function call. */
function args(text) {
  const out = []; let depth = 0; let start = 0;
  for (let i = 0; i < text.length; i++) {
    if (text[i] === '(') depth++;
    else if (text[i] === ')') depth--;
    else if (text[i] === ',' && depth === 0) { out.push(text.slice(start, i).trim()); start = i + 1; }
  }
  out.push(text.slice(start).trim());
  return out;
}

/** Resolves a token to an oklch() string in one theme. */
export function resolve(tokens, value, theme, seen = new Set()) {
  value = value.trim();
  const v = value.match(/^var\((--[\w-]+)\)$/);
  if (v) {
    if (seen.has(v[1]) || !(v[1] in tokens)) throw new Error(`cannot resolve ${v[1]}`);
    return resolve(tokens, tokens[v[1]], theme, new Set([...seen, v[1]]));
  }
  const ld = value.match(/^light-dark\(([\s\S]*)\)$/);
  if (ld) {
    const [light, dark] = args(ld[1]);
    return resolve(tokens, theme === 'light' ? light : dark, theme, seen);
  }
  return value;
}

/** Parses oklch(L% C H [/ alpha]) into numbers. */
export function parseOklch(text) {
  const m = text.match(/^oklch\(\s*([\d.]+)%\s+([\d.]+)\s+([\d.]+)\s*(?:\/\s*([\d.]+))?\s*\)$/);
  if (!m) throw new Error(`not an oklch() colour: ${text}`);
  return { l: Number(m[1]) / 100, c: Number(m[2]), h: Number(m[3]), alpha: m[4] === undefined ? 1 : Number(m[4]) };
}

/** OKLCH to linear-light sRGB (Björn Ottosson's matrices). */
export function toLinearSrgb({ l, c, h }) {
  const a = c * Math.cos((h * Math.PI) / 180);
  const b = c * Math.sin((h * Math.PI) / 180);
  const l_ = (l + 0.3963377774 * a + 0.2158037573 * b) ** 3;
  const m_ = (l - 0.1055613458 * a - 0.0638541728 * b) ** 3;
  const s_ = (l - 0.0894841775 * a - 1.291485548 * b) ** 3;
  return [
    4.0767416621 * l_ - 3.3077115913 * m_ + 0.2309699292 * s_,
    -1.2684380046 * l_ + 2.6097574011 * m_ - 0.3413193965 * s_,
    -0.0041960863 * l_ - 0.7034186147 * m_ + 1.707614701 * s_,
  ];
}

export const inGamut = rgb => rgb.every(x => x >= -0.0005 && x <= 1.0005);
const luminance = rgb => {
  const [r, g, b] = rgb.map(x => Math.min(1, Math.max(0, x)));
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
};
export function ratio(fg, bg) {
  const [a, b] = [luminance(toLinearSrgb(fg)), luminance(toLinearSrgb(bg))].sort((x, y) => y - x);
  return (a + 0.05) / (b + 0.05);
}

/** Mixes two OKLCH colours like color-mix(in oklch, a p%, b), taking the shorter hue arc. */
export function mix(a, b, p) {
  let dh = b.h - a.h;
  if (dh > 180) dh -= 360; else if (dh < -180) dh += 360;
  const t = 1 - p;
  return { l: a.l + (b.l - a.l) * t, c: a.c + (b.c - a.c) * t, h: (a.h + dh * t + 360) % 360, alpha: 1 };
}

export function check(css = fs.readFileSync(tokensPath, 'utf8')) {
  const tokens = readTokens(css);
  const tint = Number.parseFloat(tokens['--tint']) / 100;
  const results = [];
  const colour = (name, theme) => {
    if (name.startsWith('tint:')) return mix(colour(name.slice(5), theme), colour('--color-paper', theme), tint);
    if (!(name in tokens)) throw new Error(`token ${name} is not defined in tokens.css`);
    return parseOklch(resolve(tokens, tokens[name], theme));
  };
  for (const theme of ['light', 'dark']) {
    for (const [fg, bg, min, role] of pairs()) {
      const r = ratio(colour(fg, theme), colour(bg, theme));
      results.push({ theme, fg, bg, min, role, ratio: r, ok: r >= min });
    }
    for (const name of Object.keys(tokens)) {
      let value;
      try { value = resolve(tokens, tokens[name], theme); } catch { continue; }
      if (!value.startsWith('oklch(')) continue;
      const rgb = toLinearSrgb(parseOklch(value));
      if (!inGamut(rgb)) results.push({ theme, fg: name, bg: '(sRGB gamut)', min: 0, role: 'gamut', ratio: 0, ok: false });
    }
  }
  return results;
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const results = check();
  const failures = results.filter(r => !r.ok);
  for (const r of results) {
    if (process.argv.includes('--all') || !r.ok) {
      console.log(`${r.ok ? 'ok  ' : 'FAIL'} ${r.theme.padEnd(5)} ${r.fg} on ${r.bg}: ${r.ratio.toFixed(2)} (min ${r.min}, ${r.role})`);
    }
  }
  console.log(`contrast: ${results.length - failures.length}/${results.length} checks pass in light and dark`);
  process.exit(failures.length ? 1 : 0);
}

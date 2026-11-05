// The design tokens keep every text/background pair at WCAG AA in the light and the dark theme.
import test from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { check, ratio, parseOklch, readTokens, resolve } from '../scripts/contrast.mjs';

const script = path.join(path.dirname(fileURLToPath(import.meta.url)), '../scripts/contrast.mjs');

test('white on black is 21:1 and a colour on itself is 1:1', () => {
  const white = parseOklch('oklch(100% 0 0)');
  const black = parseOklch('oklch(0% 0 0)');
  assert.ok(Math.abs(ratio(white, black) - 21) < 0.01);
  assert.equal(ratio(white, white), 1);
});

test('light-dark() and var() resolve per theme', () => {
  const tokens = readTokens(':root { --a: light-dark(oklch(90% 0 0), oklch(10% 0 0)); --b: var(--a); }');
  assert.equal(resolve(tokens, tokens['--b'], 'light'), 'oklch(90% 0 0)');
  assert.equal(resolve(tokens, tokens['--b'], 'dark'), 'oklch(10% 0 0)');
});

test('every token pair in tokens.css passes AA in both themes, inside sRGB', () => {
  const results = check();
  const failures = results.filter(r => !r.ok).map(r => `${r.theme} ${r.fg} on ${r.bg} ${r.ratio.toFixed(2)}`);
  assert.deepEqual(failures, []);
  assert.ok(results.some(r => r.theme === 'light') && results.some(r => r.theme === 'dark'));
  assert.ok(results.some(r => r.fg === '--family-graphics' && r.bg === '--color-paper'), 'family colours are checked');
});

test('a token set with a low-contrast pair fails the check', async () => {
  const fs = await import('node:fs');
  const css = fs.readFileSync(path.join(path.dirname(script), '../src/styles/tokens.css'), 'utf8')
    .replace(/--color-ink-3:[^;]+;/, '--color-ink-3: light-dark(oklch(80% 0.01 260), oklch(30% 0.01 260));');
  const failing = check(css).filter(r => !r.ok && r.fg === '--color-ink-3');
  assert.ok(failing.length >= 2, 'ink-3 fails in both themes');
});

test('the command exits 0 and reports the count', () => {
  const out = execFileSync(process.execPath, [script], { encoding: 'utf8' });
  assert.match(out, /contrast: (\d+)\/\1 checks pass in light and dark/);
});

// The brand's geometry, in one place: the mark and the family icons, all drawn on the same 16-unit grid with the same
// rules (see site/design/README.md, "The mark"). Every shape is a board (drawn in ink) and at most one module (drawn
// in a signal or family colour) seated in it with a one-unit gap. Components, the favicon and the Open Graph image
// read these paths; nothing redraws them.

export const grid = 16;

/** The mark: the kernel bar with two sockets, the right one holding a module. Read as a lowercase "m". */
export const mark = {
  board: 'M0 2H16V14H14V5H9V14H7V5H2V14H0Z',
  module: 'M10 6H13V14H10Z',
  // The drawing occupies y 2..14; lockups crop to it.
  crop: { x: 0, y: 2, width: 16, height: 12 },
};

/** One icon per subsystem family, in the mark's construction. */
export const families = {
  kernel: {
    label: 'Kernel',
    // The board itself, both sockets empty: the kernel is what modules seat into.
    board: mark.board,
    module: null,
  },
  graphics: {
    label: 'Graphics',
    // A frame with a stepped image seated inside.
    board: 'M0 2H16V14H0Z M2 4V12H14V4Z',
    module: 'M3 11V9H6V6H9V8H13V11Z',
  },
  physics: {
    label: 'Physics',
    // A wall and a floor, and a body above the floor, falling.
    board: 'M0 3H2V13H16V15H0Z',
    module: 'M6 4H12V10H6Z',
  },
  audio: {
    label: 'Audio',
    // Three level bars; the loudest is the module.
    board: 'M0 8H4V14H0Z M12 6H16V14H12Z',
    module: 'M6 2H10V14H6Z',
  },
  input: {
    label: 'Input',
    // A direction pad with one arm pressed out.
    board: 'M6 1H10V15H6V10H1V6H6Z',
    module: 'M11 6H15V10H11Z',
  },
  platform: {
    label: 'Platform',
    // A base with a socket, and a host standing in it.
    board: 'M0 11H5V13H11V11H16V15H0Z',
    module: 'M6 3H10V12H6Z',
  },
  tools: {
    label: 'Tools and SDK',
    // Code brackets around a module.
    board: 'M0 1H5V3H2V13H5V15H0Z M16 1H11V3H14V13H11V15H16Z',
    module: 'M4 5H12V11H4Z',
  },
};

export const familyNames = Object.keys(families);

/** Fixed colours for files that cannot read CSS tokens (favicon, Open Graph image, standalone SVGs), in hex because
 * SVG rasterisers do not all read oklch(). Each one is the sRGB value of the token named beside it in tokens.css
 * (tests/site.test.mjs checks that they still match). */
export const fixed = {
  light: { board: '#171b20' /* --color-ink */, module: '#dd9314' /* --color-mark-module */, paper: '#f9f6f2' /* --color-paper */ },
  dark: {
    board: '#f1eee9' /* --color-ink */, module: '#fbc044' /* --color-mark-module */, paper: '#0f1216' /* --color-paper */,
    paper2: '#161a1f' /* --color-paper-2 */, rule: '#34383f' /* --color-rule */, ink2: '#b9bec6' /* --color-ink-2 */,
    ink3: '#a0a5ac' /* --color-ink-3 */,
  },
};

const escape = text => String(text).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]);

/** A standalone SVG of the mark. `board` and `module` are fill values; `style` adds a <style> element. */
export function markSvg({ board, module, title = 'MicroEngine', style = '', boardClass = '', moduleClass = '' }) {
  const cls = name => (name ? ` class="${name}"` : '');
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${grid} ${grid}" role="img" aria-label="${escape(title)}">`
    + `<title>${escape(title)}</title>${style ? `<style>${style}</style>` : ''}`
    + `<path${cls(boardClass)} fill="${board}" d="${mark.board}"/><path${cls(moduleClass)} fill="${module}" d="${mark.module}"/></svg>`;
}

/** The favicon: fixed colours that follow the browser's light or dark scheme. */
export function faviconSvg() {
  const style = `.b{fill:${fixed.light.board}}.m{fill:${fixed.light.module}}`
    + `@media (prefers-color-scheme:dark){.b{fill:${fixed.dark.board}}.m{fill:${fixed.dark.module}}}`;
  return markSvg({ board: fixed.light.board, module: fixed.light.module, style, boardClass: 'b', moduleClass: 'm' });
}

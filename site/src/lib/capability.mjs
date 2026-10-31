// Decides whether a visitor's device can be offered a demo directly. The games need WebAssembly, WebGL and a
// keyboard, and they were only verified on desktop browsers. The decision uses what the browser reports about its
// input devices and viewport, never the User-Agent, and it is a recommendation: "unvalidated" devices can still
// try at their own risk, and pressing a hardware key upgrades the device to "desktop".

/**
 * @param {{ wasm: boolean, webgl: boolean, finePointer: boolean, hover: boolean, width: number, height: number, keyboardSeen?: boolean }} env
 * @returns {{ verdict: 'desktop' | 'unvalidated' | 'unsupported', reasons: string[] }}
 */
export function classify(env) {
  const missing = [];
  if (!env.wasm) missing.push('webassembly');
  if (!env.webgl) missing.push('webgl');
  if (missing.length) return { verdict: 'unsupported', reasons: missing };
  if (env.keyboardSeen) return { verdict: 'desktop', reasons: [] };
  const reasons = [];
  if (!env.finePointer || !env.hover) reasons.push('touch');
  if (env.width < 720 || env.height < 400) reasons.push('small-screen');
  return { verdict: reasons.length ? 'unvalidated' : 'desktop', reasons };
}

/** Reads the environment from a browser window. */
export function probe(win = window, keyboardSeen = false) {
  let webgl = false;
  try {
    const canvas = win.document.createElement('canvas');
    webgl = !!(canvas.getContext('webgl2') || canvas.getContext('webgl'));
  } catch { webgl = false; }
  return {
    wasm: typeof win.WebAssembly === 'object' && typeof win.WebAssembly.instantiate === 'function',
    webgl,
    finePointer: win.matchMedia('(any-pointer: fine)').matches,
    hover: win.matchMedia('(any-hover: hover)').matches,
    width: win.innerWidth,
    height: win.innerHeight,
    keyboardSeen,
  };
}

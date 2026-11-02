// The browser owns the loop: Probe.Frame runs once per requestAnimationFrame. Init runs inside the first click or
// key, so SDL's AudioContext is created during a user gesture (autoplay policies).
import { dotnet } from './_framework/dotnet.js';
const t0 = performance.now();
const log = document.getElementById('log');
const canvas = document.getElementById('canvas');
const { getAssemblyExports, getConfig, runMain } = await dotnet.withModuleConfig({ canvas }).create();
const exports = await getAssemblyExports(getConfig().mainAssemblyName);
await runMain();
window.probe = { loadedMs: performance.now() - t0, frameTimes: [] };
log.textContent = 'ready: click or press a key';
function start() {
  removeEventListener('pointerdown', start); removeEventListener('keydown', start);
  const t = performance.now();
  const r = exports.Probe.Init();
  window.probe.initMs = performance.now() - t;
  window.probe.init = r;
  log.textContent = r;
  if (!r.startsWith('|')) return;
  canvas.focus();
  function tick() {
    const a = performance.now();
    if (!exports.Probe.Frame()) return;
    window.probe.frameTimes.push(performance.now() - a);
    if (window.probe.frameTimes.length > 600) window.probe.frameTimes.shift();
    window.probe.stats = exports.Probe.Stats();
    requestAnimationFrame(tick);
  }
  requestAnimationFrame(tick);
  window.probe.started = true;
}
addEventListener('pointerdown', start); addEventListener('keydown', start);
window.probe.start = start;

import { dotnet } from './_framework/dotnet.js';
const canvas = document.getElementById('canvas');
const { getAssemblyExports, getConfig, runMain } = await dotnet.withModuleConfig({ canvas }).create();
const exports = await getAssemblyExports(getConfig().mainAssemblyName);
await runMain();
let last = performance.now();
function tick(now) {
  exports.Probe.Frame((now - last) / 1000);
  last = now;
  requestAnimationFrame(tick);
}
requestAnimationFrame(tick);
window.probeReady = true;

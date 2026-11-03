// ARCH-1 P6: runs the harness in the page. Query: ?backends=native,managed&boxes=500&steps=600&runs=3&raw=1
// Results go to the page and to window.p6 ({ lines, done, error }) for measure.mjs.
import { dotnet } from './_framework/dotnet.js';
const q = new URLSearchParams(location.search);
const backends = (q.get('backends') ?? 'native,managed').split(',');
const boxes = Number(q.get('boxes') ?? 500), steps = Number(q.get('steps') ?? 600), runs = Number(q.get('runs') ?? 3);
const log = document.getElementById('log');
window.p6 = { lines: [], done: false, error: null };
const out = text => { window.p6.lines.push(text); log.textContent = window.p6.lines.join('\n'); };
const tick = () => new Promise(r => setTimeout(r, 0));
try {
  const t0 = performance.now();
  const { getAssemblyExports, getConfig, runMain } = await dotnet.create();
  const api = (await getAssemblyExports(getConfig().mainAssemblyName)).P6Web;
  await runMain();
  out(`loaded in ${(performance.now() - t0).toFixed(0)} ms; ${navigator.userAgent}`);
  out(api.Info());
  for (const b of backends) { await tick(); out(api.Contract(b)); }
  for (const b of backends)
    for (let run = 0; run <= runs; run++) { await tick(); out(api.Bench(b, boxes, steps, run)); }
  if (q.get('raw') === '1') { await tick(); out(api.Raw()); }
} catch (e) {
  window.p6.error = e instanceof Error ? `${e.message}
${e.stack}` : JSON.stringify(e);
  out('ERROR ' + window.p6.error);
}
window.p6.done = true;

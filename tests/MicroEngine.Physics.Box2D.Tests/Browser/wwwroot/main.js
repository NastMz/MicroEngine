// Runs the Box2D backend's tests one by one (Program.cs, Runner) and reports to the page and to window.box2d
// ({ lines, passed, failed, skipped, done, error }), which run.mjs reads.
import { dotnet } from './_framework/dotnet.js';

const log = document.getElementById('log');
const state = window.box2d = { lines: [], passed: 0, failed: 0, skipped: 0, done: false, error: null };
const out = text => { state.lines.push(text); log.textContent = state.lines.join('\n'); };
const tick = () => new Promise(resolve => setTimeout(resolve, 0));
try {
  const { getAssemblyExports, getConfig, runMain } = await dotnet.create();
  const runner = (await getAssemblyExports(getConfig().mainAssemblyName)).Runner;
  await runMain();
  out(`${runner.Info()}; ${navigator.userAgent}`);
  const count = runner.Count();
  for (let i = 0; i < count; i++) {
    await tick();
    const line = runner.Run(i);
    if (line.startsWith('PASS')) state.passed++;
    else if (line.startsWith('SKIP')) state.skipped++;
    else state.failed++;
    out(line);
  }
  out(`${state.passed} passed, ${state.failed} failed, ${state.skipped} skipped`);
} catch (e) {
  state.error = e instanceof Error ? `${e.message}\n${e.stack}` : String(e);
  out(`ERROR ${state.error}`);
}
state.done = true;

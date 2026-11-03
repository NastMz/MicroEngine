// ARCH-1 P5 web probe page. ?mode=sdl-pull|sdl-push|worklet&target=<ms>&frames=<SDL sample-frames hint>
// The browser owns the loop: Probe.Frame runs once per requestAnimationFrame. Probe.Init runs inside the first click or
// key (autoplay policies). When the 7 s scenario ends, window.result holds the managed report plus what the page
// measured: audio inserted (silence, repeats) or skipped at the output (tap worklet) per phase, the AudioContext latencies, ScriptProcessorNode sizes,
// the offline determinism hashes and the mixer cost under the .NET wasm interpreter.
import { dotnet } from './_framework/dotnet.js';

const params = new URLSearchParams(location.search);
const mode = params.get('mode') ?? 'worklet';
const targetMs = Number(params.get('target') ?? 100);
const framesHint = Number(params.get('frames') ?? 0);
const log = document.getElementById('log');
const page = { mode, targetMs, framesHint, scriptProcessors: [], dropouts: [], tapReady: null, frameMs: [] };
window.page = page;

// Every AudioContext output goes through the tap. SDL connects its ScriptProcessorNode to the destination itself, so
// that connection is redirected to a gain node that is routed through the tap once its module has loaded.
const connect = AudioNode.prototype.connect;
const createSP = BaseAudioContext.prototype.createScriptProcessor;
BaseAudioContext.prototype.createScriptProcessor = function (...a) {
  const node = createSP.apply(this, a);
  page.scriptProcessors.push({ bufferSize: node.bufferSize, sampleRate: this.sampleRate });
  page.ctx = this;
  return node;
};
AudioNode.prototype.connect = function (dest, ...rest) {
  if (this instanceof ScriptProcessorNode && dest instanceof AudioDestinationNode) {
    const bus = this.context.createGain();
    connect.call(bus, this.context.destination);
    addTap(this.context).then(tap => { bus.disconnect(); connect.call(bus, tap); });
    return connect.call(this, bus);
  }
  return connect.call(this, dest, ...rest);
};
async function addTap(ctx) {
  await ctx.audioWorklet.addModule('worklets.js');
  const tap = new AudioWorkletNode(ctx, 'p5-tap', { outputChannelCount: [2] });
  tap.port.onmessage = e => { if (e.data.heartbeat) page.tap = e.data; else page.dropouts.push(e.data); };
  connect.call(tap, ctx.destination);
  page.tapReady = ctx.currentTime;
  return tap;
}

// The worklet sink's page side: chunks go to the worklet; buffered/played are estimated from its last report.
const sink = { written: 0, report: { played: 0, starved: 0, time: 0 }, node: null, ctx: null };
page.sink = sink;
globalThis.p5sink = {
  write(view) {
    const bytes = view.slice();                      // copy out of the wasm heap before the call returns
    const chunk = new Float32Array(bytes.buffer, bytes.byteOffset, bytes.byteLength / 4);
    sink.written += chunk.length / 2;                // before the transfer detaches the buffer
    sink.node.port.postMessage(chunk, [chunk.buffer]);
  },
  played() {
    const r = sink.report;
    const since = Math.max(0, sink.ctx.currentTime - r.time) * sink.ctx.sampleRate;
    return Math.min(sink.written, r.played + since);
  },
  buffered() { return Math.max(0, Math.round(sink.written - globalThis.p5sink.played())); },
  starved() { return sink.report.starved; },
  quantum() { return 128; },
};

const { getAssemblyExports, getConfig, runMain } = await dotnet.create();
const exports = await getAssemblyExports(getConfig().mainAssemblyName);
await runMain();
log.textContent = `ready (${mode}, target ${targetMs} ms): click or press a key`;
page.ready = true;

async function start() {
  removeEventListener('pointerdown', start); removeEventListener('keydown', start);
  let rate = 48000;
  if (mode === 'worklet') {
    const ctx = new AudioContext({ latencyHint: 'interactive' });  // created inside the gesture
    page.ctx = ctx;
    await ctx.audioWorklet.addModule('worklets.js');
    const node = new AudioWorkletNode(ctx, 'p5-sink', { numberOfInputs: 0, outputChannelCount: [2] });
    node.port.onmessage = e => { sink.report = e.data; };
    sink.node = node; sink.ctx = ctx; rate = ctx.sampleRate;
    connect.call(node, await addTap(ctx));
  }
  const error = exports.Probe.Init(mode, targetMs, rate, framesHint);
  if (error) { log.textContent = page.error = error; return; }
  page.ctxStart = page.ctx.currentTime;
  page.started = performance.now();
  let last = performance.now();
  function tick() {
    const now = performance.now();
    page.frameMs.push(now - last);
    if (now - last > 80) (page.longFrames ??= []).push({ atS: +((last - page.started) / 1000).toFixed(3), ms: Math.round(now - last) });
    last = now;
    if (exports.Probe.Frame()) { requestAnimationFrame(tick); return; }
    setTimeout(finish, 400);   // let the tap report the last dropouts
  }
  requestAnimationFrame(tick);
}

function finish() {
  const result = JSON.parse(exports.Probe.Report());
  const ctx = page.ctx;
  // Context time → seconds since the scenario started, through the context's own clock/performance.now pairing.
  const stamp = ctx.getOutputTimestamp();
  const scenarioTime = at => (stamp.performanceTime + (at - stamp.contextTime) * 1000 - page.started) / 1000;
  const phases = Object.keys(result.phases);
  const starts = [0, 2.0, 4.0, 5.5];
  for (const p of phases) { result.phases[p].dropoutMs = 0; result.phases[p].skippedMs = 0; }
  for (const d of page.dropouts) {
    const t = scenarioTime(d.at) + 0.05;             // heard up to one buffer after its cause
    let i = 0; for (let k = 0; k < starts.length; k++) if (t >= starts[k]) i = k;
    const ms = d.frames * 1000 / ctx.sampleRate;      // > 0: silence or repeated audio inserted; < 0: audio skipped
    if (ms > 0) result.phases[phases[i]].dropoutMs += ms; else result.phases[phases[i]].skippedMs -= ms;
  }
  result.page = {
    mode, targetMs, framesHint, userAgent: navigator.userAgent, contextRate: ctx.sampleRate,
    baseLatencyMs: (ctx.baseLatency ?? 0) * 1000, outputLatencyMs: (ctx.outputLatency ?? 0) * 1000,
    scriptProcessors: page.scriptProcessors, tapReadyAfterMs: page.tapReady == null ? null : (page.tapReady - page.ctxStart) * 1000,
    dropouts: page.dropouts.map(d => ({ atS: +scenarioTime(d.at).toFixed(3), ms: +(d.frames * 1000 / ctx.sampleRate).toFixed(1) })),
    workletStarvedFrames: sink.report.starved, longFrames: page.longFrames,
  };
  result.offline = JSON.parse(exports.Probe.Offline());
  result.cost = [0, 16].map(v => JSON.parse(exports.Probe.Cost(v, 1024)));
  window.result = result;
  log.textContent = JSON.stringify(result, null, 1);
}

addEventListener('pointerdown', start); addEventListener('keydown', start);

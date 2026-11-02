// ARCH-1 P4: measures a served web package in headless Chrome (puppeteer-core, as web/tests uses).
//   node measure.mjs <url> <probe|game> [seconds] [--headed] [--shot=<png>]
// PUPPETEER_DIR names a folder with puppeteer-core installed (npm install --prefix <dir> puppeteer-core@25.13.0);
// CHROME_PATH overrides the Chrome executable. Prints one JSON object.
//
// What it measures, from inside the page:
//   loadMs     navigation start → the page says it is ready to start (runtime, assemblies and content loaded)
//   startMs    the first click → the first frame the game drew (window/renderer/audio creation, first scene)
//   frame      requestAnimationFrame interval (median/p95/max) and the time spent inside one game frame call
//   audio      AudioContext states seen before and after the starting gesture (both engines create one)
//   input      keys and taps sent with CDP; the page's own counters say whether the game saw them
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { createRequire } from 'node:module';

const [url, kind = 'game', secondsArg] = process.argv.slice(2).filter(a => !a.startsWith('--'));
const headed = process.argv.includes('--headed');
const shot = process.argv.find(a => a.startsWith('--shot='))?.slice(7);
const seconds = Number(secondsArg ?? 8);
const dir = process.env.PUPPETEER_DIR;
if (!url || !dir) { console.error('usage: PUPPETEER_DIR=<dir> node measure.mjs <url> <probe|game> [seconds]'); process.exit(2); }
const { default: puppeteer } = await import(pathToFileURL(createRequire(path.join(dir, 'x.js')).resolve('puppeteer-core')).href);
const chrome = [process.env.CHROME_PATH, 'C:/Program Files/Google/Chrome/Application/chrome.exe', '/usr/bin/google-chrome'].find(p => p && fs.existsSync(p));

// --firefox: Firefox through WebDriver BiDi, audio blocked until a gesture as in web/tests; no CDP touch there.
const firefox = process.argv.includes('--firefox');
const browser = firefox
  ? await puppeteer.launch({ browser: 'firefox', executablePath: process.env.FIREFOX_PATH ?? 'C:/Program Files/Mozilla Firefox/firefox.exe', headless: !headed,
      args: ['--width=1024', '--height=768'], extraPrefsFirefox: { 'media.autoplay.default': 1, 'media.autoplay.block-webaudio': true } })
  : await puppeteer.launch({
      executablePath: chrome, headless: !headed,
      args: ['--autoplay-policy=user-gesture-required', '--no-first-run', '--window-size=1024,768',
        ...(process.argv.includes('--swiftshader') ? ['--enable-unsafe-swiftshader', '--use-angle=swiftshader'] : [])],
    });
const result = { url, kind, chrome: await browser.version(), errors: [], console: [] };
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 1024, height: 768 });
  page.on('console', m => { if (result.console.length < 40) result.console.push(`${m.type()}: ${m.text()}`.slice(0, m.type() === "error" ? 3000 : 300)); });
  page.on('pageerror', e => result.errors.push(String(e.stack ?? e).slice(0, 1500)));
  // Count the AudioContexts the page creates and record their state transitions.
  await page.evaluateOnNewDocument(() => {
    window.__audio = [];
    for (const name of ['AudioContext', 'webkitAudioContext']) {
      const Base = window[name];
      if (!Base) continue;
      window[name] = class extends Base {
        constructor(...a) { super(...a); const i = window.__audio.length; window.__audio.push({ states: [this.state], baseLatency: this.baseLatency, outputLatency: this.outputLatency, sampleRate: this.sampleRate }); this.addEventListener('statechange', () => window.__audio[i].states.push(this.state)); }
      };
    }
    // Audio output: both raylib (miniaudio) and SDL feed a ScriptProcessorNode here. Record its buffer size (the
    // output latency it adds) and, after each callback, the peak sample it produced (0 = silence).
    window.__sp = [];
    const create = BaseAudioContext.prototype.createScriptProcessor;
    BaseAudioContext.prototype.createScriptProcessor = function (size, inputs, outputs) {
      const node = create.call(this, size, inputs, outputs);
      const rec = { bufferSize: node.bufferSize, sampleRate: this.sampleRate, callbacks: 0, nonSilent: 0, peak: 0 };
      window.__sp.push(rec);
      let handler = null;
      Object.defineProperty(node, 'onaudioprocess', {
        get: () => handler,
        set: h => {
          handler = h;
          EventTarget.prototype.addEventListener.call(node, 'audioprocess', e => {
            h.call(node, e);
            rec.callbacks++;
            let peak = 0;
            for (let c = 0; c < e.outputBuffer.numberOfChannels; c++)
              for (const s of e.outputBuffer.getChannelData(c)) peak = Math.max(peak, Math.abs(s));
            if (peak > 0) rec.nonSilent++;
            rec.peak = Math.max(rec.peak, peak);
          });
        },
      });
      return node;
    };
    // rAF intervals and the time spent inside each rAF callback chain (the game's frame).
    window.__raf = [];
    const raf = window.requestAnimationFrame.bind(window);
    let last = 0;
    window.requestAnimationFrame = cb => raf(t => { if (last) window.__raf.push(t - last); last = t; const a = performance.now(); cb(t); (window.__work ??= []).push(performance.now() - a); });
  });
  const navStart = Date.now();
  await page.goto(url, { waitUntil: 'load' });
  const readySelector = kind === 'probe' ? 'window.probe && window.probe.loadedMs !== undefined' : "document.documentElement.dataset.state === 'ready'";
  await page.waitForFunction(readySelector, { timeout: 120000, polling: 50 });
  result.loadMs = Date.now() - navStart;
  result.audioBeforeGesture = await page.evaluate(() => structuredClone(window.__audio));
  const clickAt = Date.now();
  await page.mouse.click(512, 300);
  if (kind === 'probe') await page.waitForFunction('window.probe.started && window.probe.stats', { timeout: 30000, polling: 10 });
  else await page.waitForFunction("document.documentElement.dataset.state === 'playing'", { timeout: 60000, polling: 10 });
  result.startMs = Date.now() - clickAt;
  // Firefox runs the instrumentation above in a sandbox the page's own scripts cannot read back: there, only the
  // game's own counters (updates, frames) are reported.
  if (firefox) {
    const before = await page.evaluate(() => window.microengine.stats());
    const t0 = Date.now();
    for (const key of ['ArrowRight', 'ArrowLeft', 'a', ' ']) { await page.keyboard.press(key); await new Promise(r => setTimeout(r, 120)); }
    await new Promise(r => setTimeout(r, seconds * 1000));
    const after = await page.evaluate(() => [window.microengine.stats(), document.documentElement.dataset.state, window.microengine.audio().join(',')]);
    const frames = Number(after[0].split(' ')[1]) - Number(before.split(' ')[1]);
    Object.assign(result, { statsBeforeInput: before, stats: after[0], state: after[1], audioStates: after[2], fps: +(1000 * frames / (Date.now() - t0)).toFixed(1) });
    if (shot) await page.screenshot({ path: shot });
    throw null;
  }
  await page.evaluate(() => { window.__raf.length = 0; window.__work.length = 0; });
  // Input: keys (arrows, A toggles the probe's tone) and two taps through CDP touch events.
  const before = await page.evaluate(k => k === 'probe' ? window.probe.stats : window.microengine.stats(), kind);
  for (const key of ['ArrowRight', 'ArrowLeft', 'KeyA', 'Space']) { await page.keyboard.press(key); await new Promise(r => setTimeout(r, 120)); }
  if (!firefox) {
    const cdp = await page.createCDPSession();
    for (const type of ['touchStart', 'touchEnd', 'touchStart', 'touchEnd'])
      await cdp.send('Input.dispatchTouchEvent', { type, touchPoints: type === 'touchEnd' ? [] : [{ x: 300, y: 300 }] }), await new Promise(r => setTimeout(r, 80));
  }
  await new Promise(r => setTimeout(r, seconds * 1000));
  const m = await page.evaluate(k => ({
    raf: window.__raf.slice(), work: window.__work.slice(), audio: structuredClone(window.__audio), sp: structuredClone(window.__sp),
    stats: k === 'probe' ? window.probe.stats : window.microengine.stats(), probeInit: k === 'probe' ? window.probe.init : null,
    state: document.documentElement.dataset.state ?? null,
    // The engine already holds the canvas's context; asking for the other kind returns null.
    webgl: (() => { const c = document.getElementById('canvas'); return c.getContext('webgl2') ? 'webgl2' : c.getContext('webgl') ? 'webgl1' : 'unknown'; })(),
  }), kind);
  result.webgl = m.webgl;
  const q = (a, p) => { const s = [...a].sort((x, y) => x - y); return s.length ? +s[Math.min(s.length - 1, Math.floor(p * s.length))].toFixed(2) : null; };
  result.frames = m.raf.length;
  result.rafIntervalMs = { median: q(m.raf, 0.5), p95: q(m.raf, 0.95), max: q(m.raf, 1) };
  result.frameWorkMs = { median: q(m.work, 0.5), p95: q(m.work, 0.95), max: q(m.work, 1) };
  result.audio = m.audio;
  result.scriptProcessors = m.sp.map(s => ({ ...s, peak: +s.peak.toFixed(3), bufferMs: +(1000 * s.bufferSize / s.sampleRate).toFixed(1) }));
  result.statsBeforeInput = before;
  result.stats = m.stats;
  result.probeInit = m.probeInit;
  result.state = m.state;
  if (shot) await page.screenshot({ path: shot });
} catch (e) {
  if (e !== null) result.errors.push(String(e).slice(0, 500));
} finally {
  await browser.close();
}
console.log(JSON.stringify(result, null, 2));

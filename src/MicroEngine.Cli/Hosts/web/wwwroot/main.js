// Page glue shared by every MicroEngine web package: loads the .NET runtime and the game's files, waits for the
// player, then drives the game one frame per requestAnimationFrame through WebHost (Program.cs). The game owns
// its timing (fixed 60 Hz steps from elapsed time); the page only supplies frames, input focus and storage.
//
// States: loading -> ready -> playing -> ended (-> playing again), or failed. A page that embeds the package in
// an iframe receives the same states as messages: { source: 'microengine', game, state, detail }.
import { dotnet } from './_framework/dotnet.js';

const $ = id => document.getElementById(id);
const canvas = $('canvas'), screen = $('screen'), message = $('message'), progress = $('progress');
const play = $('play'), detail = $('detail'), bar = $('bar'), notice = $('notice');
const soundButton = $('sound'), fullscreenButton = $('fullscreen');

let state = 'loading', game = { id: 'unknown', title: 'MicroEngine', dev: false }, exports, script = '';

function setState(next, info = '') {
  state = next;
  document.documentElement.dataset.state = next;
  window.parent?.postMessage({ source: 'microengine', game: game.id, state: next, detail: info }, '*');
}

function showScreen(text, { button, error, loading } = {}) {
  canvas.style.visibility = 'hidden';
  screen.hidden = false;
  message.textContent = text;
  progress.hidden = !loading;
  play.hidden = !button;
  if (button) play.textContent = button;
  detail.hidden = !error;
  detail.textContent = error ?? '';
}

function setProgress(done, total) {
  progress.firstElementChild.style.width = `${total ? Math.round(100 * done / total) : 0}%`;
}

function fail(text, error) {
  console.error(error);
  setState('failed', String(error));
  showScreen(text, { button: 'Reload', error: String(error?.message ?? error) });
  play.onclick = () => location.reload();
}

function notify(text) {
  notice.textContent = text;
  notice.hidden = false;
  bar.classList.add('alert');
}

// Saves (BrowserSaveStorage) go to localStorage. When the browser blocks it (some privacy settings), the game
// still runs with a session-only store and the page says that progress will not be kept.
function openStorage() {
  try {
    // Reading is enough: blocked storage throws (or is null) on any access, while full storage can still be read.
    window.localStorage.getItem('microengine/.probe');
    return window.localStorage;
  } catch (e) {
    notify('Progress will not be kept: this browser blocks site storage.');
    const memory = new Map();
    return { getItem: k => memory.get(k) ?? null, setItem: (k, v) => memory.set(k, String(v)), removeItem: k => memory.delete(k) };
  }
}
const storage = openStorage();
const storageImports = {
  get: key => storage.getItem(key),
  set: (key, value) => {
    try { storage.setItem(key, value); }
    catch (e) {
      // The game keeps its progress in memory (SaveStore.Save reports the failure); tell the player.
      notify(e?.name === 'QuotaExceededError' ? 'Progress could not be saved: site storage is full.' : `Progress could not be saved: ${e?.message ?? e}`);
      throw e;
    }
  },
  remove: key => storage.removeItem(key),
};

// Audio: the game's mixer renders on this thread, once per frame, up to 100 ms ahead, and the page plays what it
// rendered through an AudioWorklet (audio-worklet.js) on Web Audio's own thread. The AudioContext is created when the
// game starts, which happens inside the click or key press that starts it, so browsers let it play. If a browser still
// holds it suspended, every later key or click resumes it and a button says why the game is silent. A hidden page
// suspends it (below).
const sound = { ctx: null, node: null, pending: [], written: 0, report: { played: 0, time: 0 } };
function soundPlayed() {
  const r = sound.report;
  const since = Math.max(0, sound.ctx.currentTime - r.time) * sound.ctx.sampleRate;
  return Math.min(sound.written, r.played + since);
}
function soundClear() {
  sound.node?.port.postMessage('clear');
  sound.pending = [];
  sound.written = 0;
  sound.report = { played: 0, time: sound.ctx?.currentTime ?? 0 };
}
const audioImports = {
  // Returns the output's sample rate, or 0 when the browser has no Web Audio.
  start(channels) {
    if (!sound.ctx) {
      try { sound.ctx = new AudioContext({ latencyHint: 'interactive' }); }
      catch (e) { console.error(e); return 0; }
      sound.ctx.audioWorklet.addModule('audio-worklet.js').then(() => {
        sound.node = new AudioWorkletNode(sound.ctx, 'microengine-sink', { numberOfInputs: 0, outputChannelCount: [channels] });
        sound.node.port.onmessage = e => { sound.report = e.data; };
        sound.node.connect(sound.ctx.destination);
        for (const chunk of sound.pending) sound.node.port.postMessage(chunk, [chunk.buffer]);
        sound.pending = [];
      }, e => console.error(e));
    }
    soundClear();
    return sound.ctx.sampleRate;
  },
  write(view) {
    const bytes = view.slice(); // copied out of the runtime's memory before the call returns
    const chunk = new Float32Array(bytes.buffer, bytes.byteOffset, bytes.byteLength / 4);
    sound.written += chunk.length / 2;
    if (sound.node) sound.node.port.postMessage(chunk, [chunk.buffer]);
    else sound.pending.push(chunk);
  },
  buffered: () => sound.ctx ? Math.max(0, Math.round(sound.written - soundPlayed())) : 0,
  stop: soundClear,
};
const audioContexts = () => sound.ctx ? [sound.ctx] : [];
function resumeAudio() {
  for (const context of audioContexts()) if (context.state === 'suspended') context.resume().then(updateSound, () => {});
}
function updateSound() {
  const contexts = audioContexts();
  soundButton.hidden = !(state === 'playing' && contexts.some(c => c.state !== 'running'));
  bar.classList.toggle('alert', !soundButton.hidden || !notice.hidden);
}
setInterval(updateSound, 500);
soundButton.onclick = () => { resumeAudio(); canvas.focus(); };

// Fullscreen needs a user gesture, so the page owns it: Alt+Enter or the button. F11 stays the browser's own
// fullscreen, which also works because the canvas fills the page.
function toggleFullscreen() {
  if (document.fullscreenElement) document.exitFullscreen();
  else document.documentElement.requestFullscreen?.().catch(() => notify('Fullscreen is not allowed here.'));
}
fullscreenButton.hidden = !document.fullscreenEnabled;
fullscreenButton.onclick = () => { toggleFullscreen(); canvas.focus(); };
for (const button of [play, soundButton, fullscreenButton]) button.addEventListener('pointerdown', e => e.preventDefault());

// Keyboard. These listeners are registered before raylib's (which also listen on window, in the capture phase),
// so they see every key first. Until the game is playing, keys never reach it: the key that starts the game
// does not also confirm the title menu, and keys pressed while loading cannot arrive late. While playing,
// keys the game might use do not scroll the page or open browser features (Firefox's quick find, for
// example); browser shortcuts with Ctrl/Cmd and the function keys (F5, F11, F12) keep working. Releasing
// keys when the window loses focus is done by raylib's GLFW layer, so no key stays held after Alt+Tab.
const browserKey = e => e.ctrlKey || e.metaKey || /^F\d+$/.test(e.key);
window.addEventListener('keydown', e => {
  if (state !== 'playing') {
    e.stopImmediatePropagation();
    if ((state === 'ready' || state === 'ended') && !e.repeat && !browserKey(e) && !e.altKey && e.key !== 'Tab') {
      e.preventDefault();
      begin();
    }
    return;
  }
  if (e.altKey && e.key === 'Enter') {
    e.preventDefault();
    e.stopImmediatePropagation();
    if (!e.repeat) toggleFullscreen();
    return;
  }
  if (!browserKey(e)) e.preventDefault();
  resumeAudio();
}, true);
window.addEventListener('keyup', e => { if (state !== 'playing') e.stopImmediatePropagation(); }, true);
canvas.addEventListener('pointerdown', () => { canvas.focus(); resumeAudio(); });
play.onclick = () => begin();

// Touch. Fingers (and pens) on the canvas reach the game's on-screen controls (MicroEngine.Input.VirtualControls)
// through Pointer Events, which every current mobile browser has (Chrome on Android, Safari on iOS 13+): each pointer
// keeps its id from down to up, so two thumbs are tracked separately, and pointer capture keeps delivering a finger
// that slides off the canvas. Positions are sent in canvas pixels, the unit raylib draws in. The mouse stays with
// raylib. While playing the page never scrolls, zooms, selects text or opens a context menu (touch-action: none in
// host.css does most of it; Safari's pinch "gesture" events and long-press menus are stopped here).
const touchFirst = () => matchMedia('(pointer: coarse)').matches || (navigator.maxTouchPoints > 0 && !matchMedia('(any-pointer: fine)').matches);
const fingers = new Set();
function canvasPoint(e) {
  const r = canvas.getBoundingClientRect();
  return [(e.clientX - r.left) * canvas.width / Math.max(1, r.width), (e.clientY - r.top) * canvas.height / Math.max(1, r.height)];
}
function sendPointer(e, phase) {
  const [x, y] = canvasPoint(e);
  try { exports.WebHost.Pointer(e.pointerId, phase, x, y); } catch (error) { console.error(error); }
}
canvas.addEventListener('pointerdown', e => {
  if (state !== 'playing' || e.pointerType === 'mouse') return;
  e.preventDefault();
  try { canvas.setPointerCapture(e.pointerId); } catch { /* pointer already gone */ }
  fingers.add(e.pointerId);
  sendPointer(e, 0);
});
canvas.addEventListener('pointermove', e => {
  if (!fingers.has(e.pointerId)) return;
  e.preventDefault();
  // Every intermediate position, so a quick flick across the d-pad is not seen as a jump.
  for (const sample of e.getCoalescedEvents?.() ?? [e]) sendPointer(sample, 1);
});
for (const [type, phase] of [['pointerup', 2], ['pointercancel', 3], ['lostpointercapture', 3]]) {
  canvas.addEventListener(type, e => {
    if (!fingers.delete(e.pointerId)) return;
    sendPointer(e, phase);
  });
}
// The browser takes fingers away when the page loses focus (a notification, the app switcher, a system gesture) and
// may never send their pointerup: release them, so no control stays pressed.
function releaseFingers() {
  for (const id of fingers) try { exports.WebHost.Pointer(id, 3, 0, 0); } catch { /* not running */ }
  fingers.clear();
}
window.addEventListener('blur', releaseFingers);
for (const type of ['gesturestart', 'gesturechange', 'dblclick']) document.addEventListener(type, e => { if (state === 'playing') e.preventDefault(); }, { passive: false });
document.addEventListener('contextmenu', e => { if (state === 'playing' || e.target === canvas) e.preventDefault(); });
document.addEventListener('touchmove', e => { if (state === 'playing') e.preventDefault(); }, { passive: false });

// The screen as the game's touch controls need it: the safe area (notches, rounded corners, home indicator) from the
// CSS env(safe-area-inset-*) values (index.html asks for viewport-fit=cover), in canvas pixels; canvas pixels per CSS
// pixel; and whether touch is the main input. Sent again on every resize, rotation and browser-bar change.
const safeProbe = document.getElementById('safe');
function sendScreen() {
  if (!exports) return;
  const style = getComputedStyle(safeProbe), r = canvas.getBoundingClientRect();
  const scale = canvas.width / Math.max(1, r.width);
  const inset = side => (parseFloat(style.getPropertyValue(`padding-${side}`)) || 0) * scale;
  try { exports.WebHost.Screen(inset('left'), inset('top'), inset('right'), inset('bottom'), scale, touchFirst()); } catch (error) { console.error(error); }
}
window.addEventListener('resize', sendScreen);
window.addEventListener('orientationchange', () => { releaseFingers(); setTimeout(sendScreen, 250); });
window.visualViewport?.addEventListener('resize', sendScreen);
window.screen.orientation?.addEventListener?.('change', sendScreen);

// A hidden page (another tab, the phone locked or switched to another app) suspends the game: the next frame sees it,
// pauses the music and releases fingers. Animation frames stop while hidden, so one frame is run here to apply it.
document.addEventListener('visibilitychange', () => {
  if (!exports) return;
  const hidden = document.visibilityState === 'hidden';
  if (hidden) releaseFingers();
  // Animation frames stop while hidden, so the game stops filling the audio queue: suspend the output rather than
  // let it run dry, and resume it when the page is shown again.
  if (hidden) sound.ctx?.suspend().catch(() => {});
  else if (state === 'playing') resumeAudio();
  try {
    exports.WebHost.Visibility(hidden);
    if (state === 'playing') exports.WebHost.Frame();
  } catch (error) { console.error(error); }
});
screen.addEventListener('click', e => { if (e.target !== play) begin(); });

function begin() {
  if (state !== 'ready' && state !== 'ended') return;
  setState('starting');
  screen.hidden = true;
  canvas.style.visibility = 'visible';
  canvas.focus();
  let error;
  try { error = exports.WebHost.Start(script); }
  catch (e) { error = e; }
  if (error) return fail(`${game.title} could not start.`, error);
  setState('playing');
  // SDL's canvas keeps the size its window was created with until the page's next resize event; one now makes it
  // follow the page from the first frame (raylib reads the page's size itself).
  if (game.backend === 'sdl') window.dispatchEvent(new Event('resize'));
  sendScreen();
  updateSound();
  requestAnimationFrame(frame);
}

function frame() {
  if (state !== 'playing') return;
  let running;
  try { running = exports.WebHost.Frame(); }
  catch (e) { return fail(`${game.title} stopped because of an error.`, e); }
  if (game.dev) record(exports.WebHost.Probe());
  if (running) requestAnimationFrame(frame);
  else ended();
}

function ended() {
  const error = exports.WebHost.Outcome();
  if (error) return fail(`${game.title} stopped because of an error.`, error);
  if (document.fullscreenElement) document.exitFullscreen();
  fingers.clear();
  setState('ended');
  showScreen(`Thanks for playing ${game.title}.`, { button: 'Play again' });
  play.onclick = () => begin();
}

// Closing or reloading the page ends the run properly, so progress the game writes on shutdown (achievements)
// is saved.
window.addEventListener('pagehide', () => { if (state === 'playing') { setState('ended'); exports.WebHost.Stop(); } });

// Diagnostics for tests and validation (read-only). Development packages also keep, for recent frames, the
// current scene and the input actions that were down ("TitleScene confirm,left").
const trace = [];
let traced = 0;
function record(probe) {
  trace.push(probe);
  traced++;
  if (trace.length > 2000) trace.shift();
}
window.microengine = {
  get state() { return state; },
  trace: (frames = 60) => trace.slice(-frames),
  traced: () => traced,
  get game() { return game; },
  stats: () => exports?.WebHost.Stats() ?? '0 0',
  audio: () => audioContexts().map(c => c.state),
  suspendAudio: () => Promise.all(audioContexts().map(c => c.suspend())),
  captures: () => exports?.WebHost.Captures() ?? [],
  capture: name => exports?.WebHost.CaptureData(name) ?? '',
  controls: () => exports?.WebHost.Controls() ?? '',
};

async function load() {
  game = await fetch('game.json').then(r => { if (!r.ok) throw new Error(`game.json: HTTP ${r.status}`); return r.json(); });
  document.title = game.title;
  $('title').textContent = game.title;
  showScreen('Loading the engine…', { loading: true });

  // Development packages read options from the URL: UPPER_CASE parameters become environment variables (the
  // game's desktop developer shortcuts, such as RIFT_START=stage), script=<name> plays scripts/<name>, and
  // autostart=1 starts without waiting for the player. Release packages ignore the URL.
  const params = game.dev ? new URLSearchParams(location.search) : new URLSearchParams();
  const environment = Object.fromEntries([...params].filter(([key]) => /^[A-Z][A-Z0-9_]*$/.test(key)));
  if (params.get('script')) {
    const name = params.get('script');
    const response = await fetch(`scripts/${encodeURIComponent(name)}`);
    if (!response.ok) throw new Error(`scripts/${name}: HTTP ${response.status}`);
    script = await response.text();
  }

  const runtime = await dotnet
    .withModuleConfig({ canvas, onDownloadResourceProgress: (done, total) => setProgress(done, total) })
    .withEnvironmentVariables(environment)
    .create();
  runtime.setModuleImports('microengine', {
    storage: storageImports,
    // Touch-control haptics. Android browsers vibrate; Safari on iOS has no Vibration API, so this returns false there.
    haptics: { vibrate: ms => !!navigator.vibrate?.(ms) },
    // The game's audio output (MicroEngine.Raylib's browser sink).
    audio: audioImports,
  });

  // Copy the game's files into the virtual file system so ContentFolder finds /Content as on desktop.
  const list = await fetch('content.txt').then(r => r.ok ? r.text() : '');
  const files = list.split('\n').map(s => s.trim().replaceAll('\\', '/')).filter(Boolean);
  let loaded = 0;
  message.textContent = 'Loading the game…';
  setProgress(0, files.length);
  await Promise.all(files.map(async path => {
    const response = await fetch(`Content/${path}`);
    if (!response.ok) throw new Error(`Content/${path}: HTTP ${response.status}`);
    const bytes = new Uint8Array(await response.arrayBuffer());
    const dir = `/Content/${path.split('/').slice(0, -1).join('/')}`;
    runtime.Module.FS_createPath('/', dir.slice(1), true, true);
    runtime.Module.FS_createDataFile(dir, path.split('/').pop(), bytes, true, true, true);
    setProgress(++loaded, files.length);
  }));

  exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
  await runtime.runMain();
  setState('ready');
  sendScreen();
  showScreen(touchFirst() ? 'Tap to play. Landscape gives the largest view.' : 'Press any key or click to play.', { button: 'Play' });
  if (params.get('autostart') === '1') begin();
}

load().catch(e => fail(`${game.title} could not be loaded.`, e));

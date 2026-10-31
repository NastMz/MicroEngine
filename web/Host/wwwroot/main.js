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

// Audio: raylib's backend (miniaudio) creates its AudioContext when the game starts, which happens inside the
// click or key press that starts it, so browsers let it play. If a browser still holds it suspended, every
// later key or click resumes it and a button says why the game is silent.
const audioContexts = () => (window.miniaudio?.devices ?? []).map(d => d?.webaudio).filter(Boolean);
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
  setState('ended');
  showScreen(`Thanks for playing ${game.title}.`, { button: 'Play again' });
  play.onclick = () => begin();
screen.addEventListener('click', e => { if (e.target !== play) begin(); });
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
  captures: () => exports?.WebHost.Captures() ?? [],
  capture: name => exports?.WebHost.CaptureData(name) ?? '',
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
  runtime.setModuleImports('microengine', { storage: storageImports });

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
  showScreen('Press any key or click to play.', { button: 'Play' });
  if (params.get('autostart') === '1') begin();
}

load().catch(e => fail(`${game.title} could not be loaded.`, e));

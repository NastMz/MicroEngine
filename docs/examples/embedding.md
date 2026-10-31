# Embedding a web package

A MicroEngine browser package is a self-contained static folder that runs one game in a shared host page. It is
served as static files and embedded with an iframe, and it reports its lifecycle to the parent page through
`postMessage`. This page describes the package, the iframe, the message protocol and a minimal listener, as used
by the demos on this site.

Producing browser packages is not available to external developers yet; this page documents how the browser host
behaves so the demos and the planned SDK can be understood.

## The package

A package for one game contains:

| File | Purpose |
| --- | --- |
| `index.html`, `host.css`, `main.js` | The page: loading, start, end and error screens, fullscreen and sound buttons |
| `game.json` | `{ "id", "title", "dev" }`, read by the page and available to whoever embeds it |
| `_framework/` | .NET runtime, raylib and the game assemblies, with `.br` and `.gz` copies |
| `content.txt`, `Content/` | The game's asset files and their list (absent for fully procedural games) |

Serve it from any static server that sends `.wasm` as `application/wasm`. Opening `index.html` from disk does not
work, because browsers block the runtime's requests from `file://`.

## The iframe

```html
<iframe src="/play/NeonRift/" width="960" height="540" allow="fullscreen; autoplay" title="Neon Rift"></iframe>
```

- The canvas fills the iframe. The game is letterboxed inside it, scaled by whole numbers when there is room and
  scaled down, never cropped, when the iframe is smaller than the game's virtual canvas.
- `allow="fullscreen"` lets the package's Fullscreen button and Alt+Enter use the Fullscreen API from inside the
  iframe. Without it the Fullscreen button is hidden and Alt+Enter shows "Fullscreen is not allowed here."
- The game starts only after a click or key press inside the iframe. Starting inside that gesture is what lets
  audio play under browsers' autoplay policies; the starting key never reaches the game.
- While playing, keys that would scroll the page (arrows, Space, Tab, `/`, `'`) are kept from the browser, so the
  parent page does not scroll under the game. Ctrl/Cmd shortcuts and function keys stay with the browser.
- Release packages ignore URL parameters. Internal development packages read developer options from the URL and
  are never embedded publicly.

## Message protocol

The package posts one message to its parent on every state change. From the host page's script:

```js
// States: loading -> ready -> playing -> ended (-> playing again), or failed. A page that embeds the package in
// an iframe receives the same states as messages: { source: 'microengine', game, state, detail }.
// ...
function setState(next, info = '') {
  state = next;
  document.documentElement.dataset.state = next;
  window.parent?.postMessage({ source: 'microengine', game: game.id, state: next, detail: info }, '*');
}
```

| Field | Value |
| --- | --- |
| `source` | Always `'microengine'` |
| `game` | The package's id from `game.json` (for example `NeonRift`); `'unknown'` if `game.json` could not be read |
| `state` | `ready`, `starting`, `playing`, `ended` or `failed` |
| `detail` | The error text for `failed`; an empty string otherwise |

| State | Sent when |
| --- | --- |
| `ready` | Runtime and content are loaded; the page shows "Press any key or click to play." |
| `starting` | The player clicked or pressed a key; the game is being built |
| `playing` | The game started; frames are running |
| `ended` | The game quit, or the page is being closed or reloaded while playing; the page offers "Play again" |
| `failed` | Loading, starting or a frame threw; `detail` holds the error |

The initial `loading` state is set by the page but not posted. "Play again" starts a new run without reloading,
so `starting` and `playing` can follow `ended` any number of times. The protocol is one-way: the package does not
listen for messages from the parent.

The message is posted with target origin `'*'`, so a receiver must check where it came from.

## A minimal listener

The following is an example written for this page, not code from the engine. It uses only the fields above.

```html
<iframe id="game" src="/play/MeadowRun/" width="960" height="540" allow="fullscreen; autoplay" title="Meadow Run"></iframe>
<p id="status">Loading…</p>
<script>
  const frame = document.getElementById('game');
  const status = document.getElementById('status');
  window.addEventListener('message', event => {
    if (event.source !== frame.contentWindow) return;      // only this iframe
    const message = event.data;
    if (message?.source !== 'microengine') return;
    switch (message.state) {
      case 'ready': status.textContent = 'Click the game to start.'; break;
      case 'playing': status.textContent = 'Playing.'; break;
      case 'ended': status.textContent = 'Thanks for playing.'; break;
      case 'failed': status.textContent = 'The game could not run: ' + message.detail; break;
    }
  });
</script>
```

To stop a game, remove the iframe or navigate it elsewhere: the package's `pagehide` handler ends the run so
progress written at shutdown (achievements) is saved.

## Saves and origins

Saves go to the iframe origin's `localStorage`, one entry per slot under `microengine/<Game>/<slot>`. All four
packages can share one origin without conflict. That storage is also shared with anything else on the same origin
(about 5 MB in total), browsers may evict it, and private windows discard it. Serving the packages from a
different origin than the parent page keeps the parent's storage separate. Details:
[Persistence](../manual/persistence.md).

## What was validated

The packages are verified with automated tests in Chrome and Firefox on desktop, plus manual validation in the
browser. Safari was not tested, other Chromium-based browsers are not claimed, and the games have no dedicated touch
controls; see [Platforms](../guide/platforms.md) for the full list.

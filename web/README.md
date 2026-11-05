# Browser builds

All four games run in a browser from the same game assemblies as their desktop builds: raylib is compiled to
WebAssembly and linked into the .NET WebAssembly runtime, and one shared host turns any game into a static
package. The host is part of the SDK: `microengine package web` builds it for any game
([Packaging](../docs/guide/packaging.md#web)), and this folder builds the four demos with it, using a pinned private
toolchain. What was verified, in which browsers, and what is still open: [the WEB-1 evaluation](../docs/evaluations/web-support.md).
Decisions: [ADR 0007](../docs/decisions/0007-browser-host.md) and [ADR 0008](../docs/decisions/0008-web-packages.md).

| Path | Purpose |
| --- | --- |
| `toolchain.env` | Pinned versions: .NET SDK, workload set (and so emscripten), raylib commit |
| `setup.sh` | Installs that toolchain in a private folder, rebuilds raylib with its emscripten and builds SDL 3 for the web (`../src/MicroEngine.Sdl/native/build.sh`; `--no-sdl` skips it) |
| `build.sh` | Builds one or all games into `dist/web/<Game>/` |
| `serve.mjs` | Local static server (Brotli, correct MIME types) |
| [`../src/MicroEngine.Cli/Hosts/web`](../src/MicroEngine.Cli/Hosts/web) | The host template: `Host.csproj` (browser build settings, the game, rebuilt raylib), `Program.cs` (page bridge) and `wwwroot/` (page, styles, page glue) |
| `build-raylib-wasm.sh` | Compiles raylib for the web (called by `setup.sh`) |
| `tests/` | Browser checks in Chrome and Firefox (`run.mjs`), touch checks on emulated phones and tablets (`mobile.mjs`), the desktop comparison (`desktop.mjs`) and size, start-up and frame time of packages (`measure.mjs`) |

The host is not in `MicroEngine.slnx`: it needs a toolchain the normal build does not, and `microengine` generates its project per game.

## Requirements

- bash (Git Bash on Windows), `curl` and `git`; on Windows also PowerShell (for Microsoft's `dotnet-install.ps1`).
- About 2.5 GB of disk for the private toolchain (default `~/.microengine/web-tools`, or `$MICROENGINE_WEB_TOOLS`).
- Node.js 18 or later to serve packages locally and to run the browser tests.

`setup.sh` and `build.sh` were run on Windows 11 (Git Bash) and on Linux x64 (INPUT-1, which fixed
`build-raylib-wasm.sh` ending silently where no Python pack exists). The macOS branch is written but was not run.

## Set up the toolchain (once)

```bash
web/setup.sh
```

It installs .NET SDK 10.0.403 with Microsoft's install script into the private folder (the system .NET is not
touched), installs the `wasm-tools` workload set 10.0.403 into it (emscripten 3.1.56), clones raylib 6.0 at the
pinned commit and compiles it with that emscripten. Each step is skipped when its result is already there; a
missing tool, an SDK that did not install or a workload without the expected emscripten stops with a message
saying what to do. From scratch it took under two minutes on a fast connection.

**Why raylib is rebuilt.** Raylib-cs 8.1.0 ships a browser `raylib.a` compiled by a newer emscripten than .NET's;
linking it fails in `wasm-opt` with "Unknown option '--enable-bulk-memory-opt'". The same raylib version built
with the workload's own emscripten links cleanly. To move to another .NET SDK, change `toolchain.env` as a set
(SDK, workload set, runtime pack, emscripten) and run `setup.sh` again; it rebuilds raylib when the stamp changes.

## Build

```bash
web/build.sh                     # all games, into dist/web/<Game>/
web/build.sh NeonRift            # one game
web/build.sh --dev --out dist/web-dev   # development packages (scripts, URL parameters)
web/build.sh --backend sdl MeadowRun    # on SDL 3 instead of raylib (or MICROENGINE_WEB_BACKEND=sdl)
```

An unknown game, or a toolchain that is missing or not the pinned one, stops with a message. The build prints
each package's size: the files a browser downloads, and the same files when the server sends the `.br` copies.
Generated packages are not committed (`dist/` is ignored).

## Run locally

```bash
node web/serve.mjs dist/web
```

Open <http://127.0.0.1:8080/> and pick a game. Any static server works if it serves `.wasm` as
`application/wasm`; `serve.mjs` also sends the precompressed `.br` files, as a production server should.
Opening `index.html` from disk does not work (browsers block the runtime's requests from `file://`).

## What a package contains

`dist/web/<Game>/` is a self-contained static folder:

| File | Purpose |
| --- | --- |
| `index.html`, `host.css`, `main.js` | The page: loading screen, start, end and error screens, fullscreen and sound buttons |
| `game.json` | `{ "id", "title", "dev" }` for the page and for whoever embeds the package |
| `_framework/` | .NET runtime, raylib and the game assemblies (with `.br` and `.gz` copies) |
| `content.txt`, `Content/` | The game's asset files and their list (absent for fully procedural games) |
| `THIRD-PARTY-NOTICES.txt`, `licenses/` | MicroEngine's third-party notices and the complete licence files of the .NET runtime, Emscripten and raylib |

Release packages are compiled with `-p:MicroEngineWebPackage=true`: the engine and game assemblies get no debug
information and build into their own `obj/web/` and `bin/web/` folders (desktop builds keep theirs), and native code
is compiled with `-ffile-prefix-map`, so no build-machine path ends up in a package. Emscripten still writes one
.NET runtime file's location as a comment in `dotnet.native.*.js`: build releases with a toolchain folder outside
your user profile (`MICROENGINE_WEB_TOOLS`).

### Embedding (for the landing page)

Embed a package with an iframe; raylib sizes the canvas to the iframe's window and letterboxes the game inside
it, scaling by whole numbers when there is room and down (never cropped) when the iframe is smaller:

```html
<iframe src="/play/NeonRift/" width="960" height="540" allow="fullscreen; autoplay" title="Neon Rift"></iframe>
```

The page posts its state to the parent: `{ source: 'microengine', game, state, detail }` with `state` one of
`ready`, `starting`, `playing`, `ended`, `failed` (`detail` holds the error). All packages share one origin's
`localStorage` without conflict: each game uses its own `microengine/<Game>/` keys.

## How the host works

- `microengine package web` copies the host template into the game's `obj/microengine/web` folder and writes what is
  the game's, from its project's [identity](../docs/guide/app-identity.md): `GameEntry` (the entry point, from
  `MicroEngineGameFactory`, the same expression the desktop `Program.cs` passes to `DesktopLauncher`), `content.txt`,
  `game.json`, the page title and the favicon (`MicroEngineIcon`).
- `Program.cs` (`WebHost`) is the page's bridge: `Start`, `Frame` (one frame per `requestAnimationFrame`),
  `Outcome`, `Stop` and diagnostics. It drives a `GameSession` from `BrowserLauncher.CreateSession`, which picks
  `localStorage` saves before any game is built and creates a fresh raylib platform per run.
- `main.js` loads the runtime and the content (copied into the virtual file system at `/Content`, so
  `ContentFolder` works as on desktop), then waits for a click or key. Starting inside that gesture lets raylib's
  audio play under every browser's autoplay policy; the key that starts the game, and any key pressed while
  loading, never reaches the game. When the game quits, the canvas is hidden and an end screen offers "Play
  again", which starts a new run without reloading. Errors while loading or playing show a message and the
  exception text.
- Input while playing: keys that would scroll the page or open browser features (arrows, Space, Tab, `/`, `'`)
  are kept from the browser; Ctrl/Cmd shortcuts and function keys stay with it (F5 reloads, F11 is the browser's
  fullscreen, which also works because the canvas fills the page). Alt+Enter and the Fullscreen button use the
  Fullscreen API. raylib releases held keys when the window loses focus. Very short presses (within one frame)
  are kept until the game has read them.
- Touch ([Input: touch controls](../docs/manual/input.md#touch-controls), ADR 0020): `main.js` forwards touch and pen
  Pointer Events on the canvas (with pointer capture and coalesced moves) to `WebHost.Pointer`, which hands them to the
  raylib platform's touch screen and so to the game's on-screen controls, drawn by raylib over the frame. The mouse stays
  with raylib. `index.html` asks for `viewport-fit=cover`; the page reads the CSS `env(safe-area-inset-*)` insets, the
  canvas density and whether touch is the main input (`pointer: coarse`) and sends them with `WebHost.Screen` on start,
  resize, rotation and browser-bar changes. While playing the page blocks scrolling, pinch and double-tap zoom, text
  selection, the long-press callout and context menu (`touch-action: none` and event handlers); losing focus, rotating
  or hiding the page releases every finger, and a hidden page suspends the game (`WebHost.Visibility`). Haptics use
  `navigator.vibrate` where it exists (Android browsers, not Safari). On touch screens the page's buttons sit at the
  bottom centre, away from the controls in the corners, and the start screen says "Tap to play".
- If the browser still holds audio suspended, a "Sound is blocked" button appears; any key or click resumes it.

## Saves in the browser

`BrowserSaveStorage` keeps each slot as one `localStorage` entry, `microengine/<Game>/<slot>`, written whole and
synchronously, so a save survives a reload as soon as the game made it. `SaveStore` sets corrupt data aside
(`<slot>.corrupt`), never overwrites a slot it could not read, and reports a refused write instead of stopping
the game. The page:

- uses a session-only store and says "Progress will not be kept" when the browser blocks site storage;
- says "Progress could not be saved: site storage is full" when a write exceeds the quota;
- ends the run properly when the page is closed or reloaded, so progress written at shutdown is saved.

Limits to know: `localStorage` holds about 5 MB per origin (shared by everything on that origin, including a
landing page); the games' saves are a few kilobytes. Browsers may evict site data under storage pressure or
when the user clears it, private windows discard it at the end of the session, and Safari can delete script-
written storage of sites not visited for seven days. There is no cloud copy.

## Development packages

`build.sh --dev` adds the games' input scripts and makes the page read its URL:

| Parameter | Effect |
| --- | --- |
| `UPPER_CASE=value` | Environment variable for the game: the desktop developer shortcuts (`RIFT_START=stage`, `COURTYARD_START=boss`, `TRAIL_DEBUG=trainer`, `MEADOW_START=credits`, ...) |
| `script=<name>` | Plays `scripts/<name>` as `--script` does on desktop (seed 1, captures kept in memory) |
| `autostart=1` | Starts without waiting for a click (audio then stays suspended) |

`window.microengine` exposes the state, `stats()` ("updates frames"), `audio()`, `trace()` and `captures()`.
`trace()` (development packages only) holds one entry per recent frame: the current scene, the input actions that
were down in any of the frame's fixed updates (recorded after each update's input, so a press shorter than a frame
still shows when its frame catches up with several updates), the touch controls' state and the scene's
`Describe()`. Release packages ignore the URL.

## Tests

```bash
web/build.sh --dev --out dist/web-dev
cd web/tests && npm ci && cd ../..
node web/tests/run.mjs                       # platform and scripts suites, Chrome and Firefox
node web/tests/run.mjs keyboard framerate autopilot
node web/tests/run.mjs repeat gamepad       # scripts twice (identical captures), an emulated gamepad
node web/tests/mobile.mjs                    # touch on emulated Android, iPhone, portrait phone and tablet
node web/tests/measure.mjs --games MeadowRun # download, load, start-up and frame time (to compare backends)
dotnet build MicroEngine.slnx -c Release && node web/tests/desktop.mjs   # compare script captures with desktop
```

`run.mjs` drives the installed Chrome and Firefox through puppeteer-core (set `CHROME_PATH`/`FIREFOX_PATH` if
they are elsewhere) with autoplay blocked until a gesture. Suites: `platform` (loading, gesture start, audio,
60 updates/s, held/released/short keys, focus loss, browser keys, saves, corrupt/full/blocked storage, end and
restart), `scripts` (the games' own scripts listed in `scenarios.json`, captures stored in `dist/web-tests`),
`keyboard` (the same scripts typed through the browser keyboard), `framerate` and `autopilot` (Neon Rift's
autopilot clears all three stages). `desktop.mjs` replays the scenarios on the desktop build and compares the
captures byte for byte. With `--reference reference/linux-x64.json` it also checks the native captures against the
Linux visual reference (`captures.mjs`), as the pipeline's native Linux job does under Xvfb.

`mobile.mjs` emulates four devices in Chrome (Android phone and notched iPhone in landscape, a small phone in
portrait, a tablet; the iPhone is Chromium with an iOS user agent and safe-area insets, not Safari) and sends real
multi-touch through DevTools (`Input.dispatchTouchEvent`), which the browser delivers as touch and Pointer Events. Per
game it taps through the title, then plays with two or three fingers and checks the game's response from the
development trace (actions down, touch-control state and the scene's `Describe()`, such as the player's position):
walking, jumping while a direction is held, aiming and firing, analog walking speed, grid steps and menus, a finger
sliding off a button, a very short tap, focus loss, rotation with a finger down, pinch and long press. Screenshots go
to `dist/web-mobile`.

## Adding a fifth game

1. Make sure the game reads files through `ContentFolder` and saves through `SaveStore` (no direct file IO), and
   keeps its desktop `Program.cs` minimal: build the game, call `DesktopLauncher.Run`.
2. Give its project (`games/MyGame/MyGame.csproj`) its [identity](../docs/guide/app-identity.md): `ApplicationTitle`,
   `ApplicationId`, `MicroEngineIcon` and `MicroEngineGameFactory`:

   ```xml
   <ApplicationTitle>My Game</ApplicationTitle>
   <ApplicationId>dev.microengine.mygame</ApplicationId>
   <MicroEngineIcon>icon.png</MicroEngineIcon>
   <MicroEngineGameFactory>new MyGame.MyGameClass()</MicroEngineGameFactory>
   ```

3. `web/build.sh MyGame`, then `node web/serve.mjs dist/web` and play it.
4. Add it to `games` and to the quitting script table in `web/tests/run.mjs`, add scenarios to
   `web/tests/scenarios.json` (and its save-folder variable to `desktop.mjs`), and run the tests. New scenarios add
   captures: regenerate the Linux reference with `desktop.mjs --write-reference` under Xvfb.
5. Give it a page: add its entry to `site/src/data/games.ts` (`npm test` in `site/` fails until the site and the
   registry list the same games). On merge, the pipeline builds, stores and publishes its package
   ([publishing the site](../docs/deployment.md)); no workflow changes.

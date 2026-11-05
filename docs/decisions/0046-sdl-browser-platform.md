# 0046 — SDL 3 is the browser platform; raylib leaves the web

Status: Accepted (2026-11-05). Amends [0007](0007-browser-host.md) and [0008](0008-web-packages.md); amends the browser
half of [0017](0017-sdl3-native-platform.md) ("raylib for browsers").

## Context

Since WEB-0 the browser build ran on raylib compiled to WebAssembly, the one backend that then worked there, while SDL 3
became the native platform on desktops and phones (0017). The ARCH-1 prototype P4 showed that SDL 3, compiled from
source with the Emscripten of the .NET `wasm-tools` workload, runs the same games in Chrome and Firefox through
`MicroEngine.Sdl` with no game change, and that the platform contract needs no "integrated backend" for it: what a
page needs is host behaviour (who owns the loop, never block, canvas size, audio unlock, visibility). Two defects of
.NET's WebAssembly interpreter appeared there: 64-bit enum parameters and returns (SDL3-CS types `SDL_WindowFlags` as
one) are passed in a 32-bit slot, and function-pointer parameters (`delegate* unmanaged`) cannot be called at all.

Keeping raylib only for the web meant two platforms with two input, touch, audio and window paths to keep equal, a
raylib build pinned to the workload's Emscripten, and every engine change checked against a backend no other platform
uses.

## Decision

- **One platform everywhere.** The browser host runs `SdlPlatform`: `SdlPlatform.BrowserProfile(page)`, composed by
  `MicroEngine.Sdl.BrowserLauncher` (the same `GameHost` path as every host, 0038) for one `BrowserPage`. The page drives
  the frames; the platform reports `CanBlock = false` and never waits (no VSync, no `IPreciseDelay`, no wait while
  suspended); the window is the page's canvas, sized by the page's CSS (SDL then follows the page's resize events),
  one canvas pixel per CSS pixel as raylib had: SDL's high pixel density made the canvas stop following the page on
  high-density screens, so it is not requested in a browser (sharper canvases on such screens are a possible later
  step, with the page's touch coordinates and insets scaled alike); the page's screen size is the window's output
  whatever render target is bound; a hidden page suspends the game; touches (the page's Pointer Events), safe-area insets, density and
  vibration come from the page, and SDL's own finger events are ignored so no touch arrives twice; keyboard, mouse and
  gamepads (the Gamepad API) are SDL's; sound plays through the page's AudioWorklet sink (0033), never SDL's audio;
  full screen and the window's place belong to the page.
- **SDL for the web is built from source, once per content.** `src/MicroEngine.Sdl/native/build.sh` compiles the SDL
  commit `sdl.env` pins (the one the `ppy.SDL3-CS` bindings were made from) into `SDL3.a` with the workload's
  Emscripten: single-threaded like the .NET browser runtime, without GPU, camera or Vulkan. `web/setup.sh` runs it for
  developers; CI builds it in `sdl-web.yml`, cached by a hash of its inputs and toolchain, and hands it to every web job.
  The `MicroEngine.Sdl` package carries it (`native/browser-wasm/SDL3.a`, found through `MicroEngineWebSdlFromPackage`);
  a release pack fails without it.
- **The interpreter defects are worked around in the engine's own declarations.** In a browser the platform calls
  `SDL_CreateWindow` and `SDL_GetWindowFlags` through declarations with a plain `ulong` (`SdlWeb`), and registers no
  event watch (its only function-pointer call; a page announces backgrounding through `visibilitychange`). Elsewhere it
  calls SDL3-CS directly. Known issue K-P4-1, not reported upstream.
- **The bindings are trimmed in browser builds.** The web host marks SDL3-CS (and raylib's binding and backend, which a
  game referencing `MicroEngine.Desktop` carries) trimmable, so only the SDL functions the engine calls reach the native
  link.
- **raylib leaves the web, not the desktop.** `MicroEngine.Raylib` keeps its desktop platform as the second tier-0
  implementation; its browser launcher, page, browser sink and the web build of raylib (`raylib.a`, `web/build-raylib-wasm.sh`,
  the package's `native/browser-wasm/raylib.a`) are removed, and so are `microengine package web --raylib-wasm` and the
  temporary `--web-backend` option that chose between the two during the gates.
- **The switch was gated.** SDL became the default only after, in CI, on all four demos: the browser engine suites
  (`platform`, `scripts`, `repeat`, `keyboard`, `framerate`, `audio`, `gamepad`) in Chrome and Firefox, and touch on four
  emulated phones and tablets (`mobile.mjs`) passed; size, start-up and frame time were measured against raylib on the
  same runners (docs/evaluations and the pull requests hold the numbers).

## Consequences

- One input, touch, audio and window path on every platform; a page and a phone run the same platform code.
- The browser and desktop SDL builds are one SDL version; updating `ppy.SDL3-CS` means updating `sdl.env` and running the
  web checks, and an SDL release that needs an Emscripten newer than the workload's waits for a .NET workload update.
- The CI gates found what the P4 prototype had not exercised: the frame laid out on the game's own canvas (drawn at 1×
  in the corner) and the canvas not following the page on emulated phones, both fixed before the switch, and a check
  now asserts the layout (the canvas fills the page, the frame fills the canvas). Firefox's WebDriver types the keypad's
  Enter for puppeteer's `Enter`; the keyboard suite types the main Enter key there.
- SDL's Emscripten joystick takes an axis's first reported value as its resting value: a pad whose stick never moved
  before is not seen until it moves again (real sticks report noise at rest; the emulated pad in the suites does too).
- Not verified by CI and left to the owner: real phones and tablets, Safari on macOS and iOS (only Chromium with iOS
  user agents is emulated), real gamepads (vendor mappings, rumble), haptics on a real Android browser, high-density
  screens and long sessions.
- `site/scripts/demos.mjs` still requires `licenses/raylib-LICENSE.txt` in a demo package; packages keep it while
  they carry raylib's binding (the demos reference `MicroEngine.Desktop`).

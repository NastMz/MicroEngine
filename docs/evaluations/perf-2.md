# PERF-2: performance hardening and graphics configuration

Date: 2026-11-02. Branch `feat/perf-2` (from `feat/perf-1` at 29aca83, PR NastMz/MicroEngine#19 not merged). Task record:
[`odd/tasks/perf-2.md`](../../odd/tasks/perf-2.md). Decision: [ADR 0024](../decisions/0024-graphics-configuration.md).
Continues [PERF-1](perf-1.md); its evidence words apply: **run** means executed here and its output looked at, **desk**
means judged from source or published documentation (linked). No Android phone, iPhone, iPad, Mac or Linux desktop was
connected; the Android numbers below come from the emulator and are labelled so, never as phone measurements.

## 1. Reference machine and conditions

| | |
| --- | --- |
| CPU / RAM / GPUs | AMD Ryzen 5 7600X, 31 GB; NVIDIA GeForce RTX 4070 Ti SUPER (adapter 0), AMD Radeon integrated (adapter 1) |
| Displays (run) | DISPLAY3 primary, 1920x1080 at 100 Hz (LG FHD); DISPLAY1 left, 1920x1080 at 74.97 Hz; DISPLAY2 right, 1920x1080 at **60 Hz** (the owner described both secondaries as 75 Hz; the system reports 60 for this one) |
| System | Windows 11 Pro 10.0.26300, .NET 10.0.14, SDL 3.5.0, raylib 6.0, Chrome (headed, for the browser runs) |
| Tools | `--profile` (engine times), PresentMon 2.6.0 (presentation; runs without elevation here, but then cannot filter by process name, so captures were taken unfiltered and split by process), a stack sampler written for this mission (SuspendThread + StackWalk64, export names) |

PERF-1's "display reporting 74 Hz" came from WMI (`Win32_VideoController.CurrentRefreshRate` reports one display: 74 Hz
today too, while the primary runs at 100 Hz). Its 13.34 ms frame periods show that window opened on the 75 Hz display;
that run did not record which display. `--profile` now records the display's index, name, mode and scale, and the
same build opens on the 100 Hz primary display today (SDL centres new windows on the primary display).

## 2. PERF-1's open items

| Item | Result |
| --- | --- |
| `FrameBlend` in the demos | Done in all four (camera and every moving actor; Trail Duel also battle slides and orbs). Found first: PERF-1 documented `FrameBlend` as 0 in fixed-step runs, but `Lerp(previous, current, 0)` draws the *previous* update, so adopting it would have shifted every capture by one step. Fixed-step runs now report 1 (test checked failing first). Scenes not updated in the latest update (paused, under an overlay, in a transition) draw at 1, or they rock between their last two positions. Fixed-step captures: 138/138 of the shared scenarios byte-identical to the base build (run), and every script of every demo identical per demo. |
| Missed refresh ~1.9 s after an SDL window appears | Identified (run, PresentMon). On DISPLAY1 the window starts as **Composed: Flip** and Windows' compositor promotes it to **Hardware Composed: Independent Flip** about 0.3 s after its first frame; at that moment 1 to 3 frames are shown for 20 to 42 ms (4 of 4 runs), and the engine sees one 26.5 ms frame with 13 ms blocked in present. PERF-1's 1.9 s = ~1.5 s of gamepad start before the first frame + 0.3 s. On the primary display the window stays composed (1 frame of 20 ms in 1 of 7 runs). It is the system's, once per window; borderless full screen reaches independent flip from the third frame, where it cannot be seen. Not changed. |
| Gamepad start (about a second) | Cause found (run): sampling the main thread inside `SDL_InitSubSystem(GAMEPAD)` showed `GameInputRedist.dll!GameInputInitialize` sleeping ~950 ms; the GameInput service is installed but stopped here. Disabling every joystick driver hint left 965 ms, because SDL starts GameInput unless both `SDL_JOYSTICK_GAMEINPUT` and `SDL_JOYSTICK_GAMEINPUT_RAW` are off ([SDL source][sdl-gameinput]). With both off (SDL's RawInput, XInput and WGI drivers, its default before 3.4): gamepad step 1,065–1,120 → 105–125 ms, SDL platform start 1,450–1,570 → 440–470 ms (PerfBench, 3 runs each). The environment variables still restore GameInput (checked: 1,065 ms). Not tested with a physical controller: none was connected. |
| Avoidable allocations in the demos | See the table below. |
| `SceneManager` without scenes | A game with an empty stack updated index -1; it now updates and draws nothing (test checked failing first). |
| Late music errors | A song registered with `AddMusic(name, Song)` and an invalid note or drum failed only when first played (possibly mid-game, from inside `Update` with a fade). Its notes and drums are now validated in `AddMusic`, in `Load`, with the exceptions `Song.Render` throws; only a failure of the machine itself (memory) can still surface at play time. Test checked failing first. |
| The five "environmental" failures | They were `sdk/tests/packages.mjs`'s "no build-machine path" check for five packages, after a local `dotnet pack` without `-p:MicroEngineRelease=true`. Paths are mapped away only with `ContinuousIntegrationBuild`, set in CI or with that flag; packed with it, every check passes locally (run). The check now says so, and `docs/releasing.md` gives the command. |
| Capture variability | Meadow Run's `menus/controls.png` changed between runs because achievement progress is saved between runs and shown as a counter; PERF-1's manual check ran without `MEADOW_SAVE_DIR`. With a fresh save folder, three runs are identical; the repository's capture checks already use one. Running a capture script without a save folder writes into the player's real saves (Meadow's progress grew by 2 per run): noted, not changed (each game creates its store before the launcher runs). |
| Pacing and audio-capture fixes | Their tests (checked failing first in PERF-1) pass; on hardware 0 uneven frames in every paced run below; a 20 s audio capture still writes 20.00 s. |

Allocations per frame, `--profile` of the same script, fresh save folder, 3 runs each (run; the demo writers' numbers,
then the engine fixes on top):

| Demo, script | Before PERF-2 | After |
| --- | --- | --- |
| Meadow Run, level 1 | 2,816 B | 384 B |
| Courtyard Key, road fight | 2,270 B | 469 B (before the engine fixes) |
| Trail Duel, route trainer | 22,361 B | 286 B |
| Neon Rift, stage 1 | 14,176 B | about 1,010 B |

Engine fixes: `EntityList` enumerates with a struct and sorts drawing in a reused buffer (stable as before);
`SpriteAtlas.Frames` collects each name once; `BitmapFont.Measure` counts lines without LINQ. Remaining: `EntityList.OfType`
and `Overlapping` still return iterators (changing their return types would break published games), and
`ActionPrompts.Text` builds its prompt list per call (about 560 B): caching it needs a revision of the bindings, which
are public mutable lists. Collections per 20 s run: 0 to 2 before, 0 after in these scripts.

## 3. Graphics configuration (S10)

`GameSettings.Graphics` (`GraphicsSettings`): `Backend`, `Renderers`, `VSync`, `FrameRateLimit`, `Gpu`, `WindowMode`,
`Display`; launcher overrides `--platform`, `--renderer`, `--vsync`, `--fps-limit`, `--gpu`, `--window`, `--display`;
`Game.Screen` (`GameScreen`) changes VSync, the frame limit, the window mode and the display while running, through
`IDisplayControl` (SDL and raylib desktop windows), and reports the displays, the renderer and the adapter. Selection
order: command line or `MICROENGINE_PLATFORM` (must start) → `Backend` (falls back) → the catalog; inside SDL the
`Renderers` list (comma-separated, SDL tries each) → SDL's own order. Tests cover parsing, overrides, fallback,
run-time changes, headless refusals, the frame limit with a simulated clock and the page-driven skip.

## 4. VSync, frame limit and update rate (S11)

PerfBench, SDL Direct3D 11, 600 updates at 60 per second, primary display unless stated (run):

| Configuration | Frames/s | Interval p50 / p99 / max | Frames with 0 / 2+ steps | Present mode (PresentMon) | Display latency p50 |
| --- | --- | --- | --- | --- | --- |
| VSync on, window, 100 Hz | 100 | 10.00 / 10.43 / 11.4 ms | expected (60 on 100) | Composed: Flip | 29.5 ms |
| VSync off, no limit | 5,593 | 0.15 / 1.09 / 2.2 ms | expected | Composed: Flip (no tearing: composed) | — |
| VSync off, limit 144 | 144.0 | 6.94 / 7.25 / 7.6 ms | expected | Composed: Flip | — |
| VSync off, limit 100 | 100 | — | — | Composed: Flip | 25.8 ms |
| VSync off, limit 60 | 60.0 | 16.67 / 16.80 / 17.1 ms | 0 / 0 | — | — |
| VSync on, limit 50, 100 Hz | 50.0 | 20.00 / 20.17 / 20.6 ms | 0 / 88 (60 on 50: one frame in five runs two) | — | — |
| Borderless full screen, 100 Hz | 100 | 10.00 / 10.43 / 11.4 ms | expected | Hardware: Independent Flip from frame 3 | 19.7 ms |
| Exclusive full screen, 100 Hz | 100 | — | expected | Composed: Flip | 29.5 ms |
| Exclusive full screen, DISPLAY1 | 75.0 | 13.35 / 17.12 / 18.2 ms | expected | — | — |

With the update rate equal to the display rate (100 updates on 100 Hz, 1,440 frames): display pacing 0 / 0 uneven frames
in 2 of 2 runs; measured time 0 / 0 in one run and 340 / 341 in the other. VSync, the limit and the update rate are
independent: the limit's schedule is absolute (no drift), the loop paces time to its periods when VSync is off (a test
with ±1 ms timer jitter: 0 uneven frames paced, some measured), and `UpdateRate` never changes. Borderless full screen is
the better full screen on Windows: one refresh less latency; SDL's exclusive mode is not presented as independent flip
here. raylib keeps its own limiter at the monitor's rate with VSync and frees it without (desk + build; not timed again).

## 5. Several displays (S12)

| Window | Frames arrive at | Reported rate | Notes (run) |
| --- | --- | --- | --- |
| Window on the 100 Hz primary | 100 Hz | 100 | composed |
| Window on DISPLAY1 (74.97 Hz) | 75 Hz | 74.97 | promoted to independent flip after ~0.3 s |
| Window on DISPLAY2 (60 Hz) | **100 Hz** | 60 | composed at the primary's rate: 100 frames for 60 refreshes |
| Borderless full screen on DISPLAY2 | 60 Hz | 60 | |
| Window moved from the primary to DISPLAY1 while running | **100 Hz** (stays) | 74.97 | resetting VSync after the move did not change it |

So on Windows a composed window runs at the compositor's rate, not always its display's, and the engine cannot know
which from the display mode. Counted in the reported display's periods, 10 ms frames became 16.7 ms then 3.3 ms of drawn
time (judder with interpolation, though time stayed real). The loop now trusts the reported rate only while the median
of the last 16 frames is within 15% of its period (test checked failing first); on DISPLAY2 the drawn time then advances
9.75–10.27 ms per frame (p5–p95). Display identification (index, name, mode, scale, primary) comes from SDL and GLFW; all
three displays here are at 100% scale, so scaling was not exercised beyond reading it (`DisplayInfo.Scale`).

## 6. Two GPUs (S13)

Unchanged from PERF-1 and re-checked: SDL's Direct3D 11 uses the adapter Windows assigns (adapter 0, the RTX);
`GraphicsSettings.Gpu = PowerSaving` moves SDL's `gpu` renderer to the Radeon; raylib's OpenGL ignores it. The adapter in
use is now readable by games (`Game.Screen.Adapter`). Not done, for the reasons in PERF-1: forcing the dedicated GPU
(exported `NvOptimusEnablement` needs a native executable) or changing the user's Windows graphics preference.

## 7. Graphics APIs and the way to capabilities (S14)

Desk, with PERF-1's measurements (2D sprites and shapes only; nothing here says how a backend would do with shaders,
post effects or 3D, which no backend of MicroEngine draws today):

- **SDL_Renderer** (kept): automatic per-system choice and fallback (Direct3D 11/12, Metal, Vulkan, OpenGL, OpenGL ES,
  software); since SDL 3.4 its `gpu` driver runs on SDL_GPU and takes custom fragment shaders
  (`SDL_CreateGPURenderer`, `SDL_CreateGPURenderState`, [SDL wiki][sdl-renderstate]). `GraphicsSettings.Renderers` can
  already ask for it.
- **SDL_GPU directly** (Direct3D 12, Vulkan, Metal): the capability path (shaders, render targets, compute, 3D), with
  shaders per backend (SDL_shadercross) and SDL choosing the driver ([SDL_HINT_GPU_DRIVER][sdl-gpudriver]). Existing
  renderers built on SDL_GPU (FNA's FNA3D among them) are the references to study first in GRAPHICS-1; whether one can
  be reused as is was not checked here.
- **Direct3D, Vulkan, Metal, OpenGL / ES**: reached through SDL; no direct binding is needed.
- **raylib / WebGL**: stays for browsers. **WebGPU**: Chrome (Android 12+), Safari 26 (iOS, iPadOS, macOS) and desktop
  Firefox on Windows and Apple silicon ship it; Firefox for Android and Linux still did not in 2026
  ([web.dev][webgpu-web], [Wikipedia][webgpu-wiki]). A WebGPU backend would still need WebGL for those browsers.
- **Decision**: a capability-based renderer (querying what the device can do, choosing SDL_GPU, SDL_Renderer or WebGL
  by it, shaders in the public API) is a considerable implementation and belongs to a separate **GRAPHICS-1** mission
  ([ADR 0024](../decisions/0024-graphics-configuration.md)). PERF-2 delivers the configuration and fallback layer it will
  extend.

## 8. WebAssembly and Android (S15)

**WebAssembly** (run): development packages built with `web/build.sh --dev` (pinned toolchain, interpreter, no AOT) for
the base build (e4f0dae) and this branch, served locally and played in headed Chrome on the 100 Hz display. "Playing
after" is from the click on Play to the first game frame; "frame work" is the time each animation-frame callback spends
in the game, over 10 s starting 2 s after play.

| Demo | Base: playing after | Branch: playing after (2 runs) | Branch frame work p50 / p95 / p99 | Frames > 1.5 periods in 10 s |
| --- | --- | --- | --- | --- |
| Meadow Run | 8,193 ms | 1,213 / 1,159 ms | 4.9 / 5.6 / 8.3 ms | 6 (base 0) |
| Courtyard Key | 5,054 ms | 1,125 / 958 ms | 4.6 / 5.2 / 7.9 ms | 6 (base 0) |
| Trail Duel | 8,316 ms | 1,423 / 1,367 ms | 4.6 / 5.1 / 21.7 ms | 10 (base 0) |
| Neon Rift (music from files) | 1,032 ms | 997 / 1,008 ms | 1.0 / 1.5 / 2.0 ms | 0 (base 0) |

The base build's frame pacing was already perfect (interval p50 10.00, p99 10.10 ms, work 0.6–1.0 ms), but every song
registered with `AddMusic(name, Song)` rendered inside `Load` on the browser's only thread. Songs now render in slices
of 2,048 samples for up to 4 ms per update (first attempt: 32,768-sample slices and synchronous completion gave frame
work p99 21–61 ms and 300 ms freezes; rejected), and a song asked for before it is ready starts once rendered. The cost:
about 4 ms more work per update while songs render (the first 10–30 s), and one frame of 220–420 ms each time a finished
song reaches raylib's audio device (6–10 in the measured window), most likely `RaylibAudioDevice.LoadMusic` converting
the whole clip to WAV (`AudioClip.ToWav`) in interpreted WebAssembly (not profiled). JS heap 4.8–16.9 MB, unchanged.
Download sizes are unchanged (Meadow Run 12 MB uncompressed). Mono's AOT compiler for WebAssembly crashed on
`SDL3-CS.dll` (sgen assertion) with the pinned toolchain: not adopted.

**Android** (emulator only, `Medium_Phone_API_36.1`, x86_64 with WHPX; no phone connected, so none of this is a phone
measurement): Meadow Run's APK from `microengine package android --debug-key` installs and runs; cold start (`am start -W`
TotalTime) 1,554 / 1,359 / 958 ms; engine start to the title scene 2.15 s (logcat); PSS 159 MB after 20 s; no fatal
error. Frame pacing was not measured: the emulator's software GPU and host scheduling say nothing about a phone. iOS,
iPadOS and mobile browsers: no device.

## 9. Validation (S16)

- `dotnet test MicroEngine.slnx -c Release` on Windows, native tests included: every suite passes (core 235 + 1
  skipped, desktop 8 of which 5 native, raylib 1 + 4 skipped (Linux-only), samples 9, Meadow Run 53, Courtyard Key 35,
  Trail Duel 100, Neon Rift 38). Site: unit tests 43/43, quoted code blocks match their samples.
- New tests: graphics options and launcher overrides, backend preference and fallback, frame limit (cap, jitter
  pacing, page-driven skip, fixed-step), `Game.Screen` changes and headless refusals, wrong display rate, `FrameBlend`
  in fixed-step runs, empty scene stack, song validation, sliced song rendering (sample-identical) and the mixer without
  threads, allocation-free entity lists, frames and text measurement; each demo's interpolation and garbage tests.
- Captures: the 15 shared scenarios (`web/tests/scenarios.json`) give 138 PNGs, byte-identical between the base build
  (e4f0dae) and this branch (run).
- SDK package checks on a local feed packed as CI packs: all pass.

## 10. Open

- The demos' interpolation was checked by tests and identical captures, not watched on screen at 75/100/144 Hz.
- Gamepads on Windows without GameInput: no physical controller was connected to try one.
- Phones, iPad, iPhone, Mac and Linux desktops: not measured (emulator only for Android).
- Browsers: one 220–420 ms frame when each sliced song reaches raylib's audio device (WAV conversion of the whole clip
  suspected, not profiled); worth slicing too.
- WebAssembly AOT: Mono's AOT compiler crashed on `SDL3-CS.dll` (an sgen assertion) with the pinned toolchain; not
  adopted, worth retrying with SDL excluded from the web package's AOT inputs.
- The compositor's promotion hitch on secondary displays and composed windows running at another display's rate are
  Windows behaviour; full screen avoids both.
- GRAPHICS-1: the capability-based renderer.

[sdl-gameinput]: https://github.com/libsdl-org/SDL/blob/main/src/joystick/gdk/SDL_gameinputjoystick.cpp
[sdl-renderstate]: https://wiki.libsdl.org/SDL3/SDL_CreateGPURenderState
[sdl-gpudriver]: https://wiki.libsdl.org/SDL3/SDL_HINT_GPU_DRIVER
[webgpu-web]: https://web.dev/blog/webgpu-supported-major-browsers
[webgpu-wiki]: https://en.wikipedia.org/wiki/WebGPU

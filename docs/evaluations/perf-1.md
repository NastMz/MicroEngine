# PERF-1: performance engineering, graphics backends and runtime optimization

Date: 2026-11-01/02. Branch `feat/perf-1` (from `main` at dc3770d, GAME-POLISH merged). Task record:
[`odd/tasks/perf-1.md`](../../odd/tasks/perf-1.md). Decision: [ADR 0023](../decisions/0023-frame-pacing-and-measurement.md).

Evidence words are used strictly: **run** means executed on the reference machine below and its output looked at;
**browser** means the built-in Chromium pane of the development app; **desk** means judged from source, documentation
or published sources only (linked). No Android phone, iPhone, iPad, Mac or Linux desktop was available: nothing here
claims a measurement on one, and no desktop number is extrapolated to them.

## 1. Reference machine and conditions

| | |
| --- | --- |
| CPU / RAM | AMD Ryzen 5 7600X (6 cores, 12 threads), 31 GB |
| GPUs | NVIDIA GeForce RTX 4070 Ti SUPER (display attached; DXGI adapter 0), AMD Radeon integrated (adapter 1) |
| Display | 1920x1080 at 74.97 Hz (the owner reported a 100 Hz panel; this session's display ran at 75 Hz) |
| System | Windows 11 Pro 10.0.26300, .NET 10.0.14 (workstation GC), SDL 3.5.0, raylib 6.0 |
| Window | Windowed, scale 4 (1280x720 output for a 320x180 canvas), VSync on unless stated, sound muted (`--mute`) |

Every number below comes from `--profile` runs of the Release build (JSON kept in the session scratch folder, not in the
repository), first 60 frames left out of summaries (JIT and cache warm-up). Runs compared with each other use the same
build flags, window, platform and load; "3 runs" means three consecutive runs whose values are all given or averaged.

## 2. Instrumentation (deliverables 3, 13)

- `FrameProfiler` and the launcher's `--profile FILE` ([configuration](../reference/configuration.md#command-line-flags)):
  per frame, the interval since the previous frame start, event polling, fixed steps, drawing, presenting (the VSync wait
  when on), steps run, game time (stepped and interpolated), bytes allocated by the loop's thread and garbage
  collections; start phases (platform, start screen, load, first frame); facts the platform reports
  (`IPlatformDiagnostics`: SDL version, renderer, video driver, VSync, display mode, output size, adapter, the steps of
  SDL's start). JSON or CSV; summaries with mean, p50, p95, p99, p99.9 and max, steps-per-frame histogram, hitches
  (interval over 1.5x the median) and a **motion error**: per frame, |game time advanced − time the frame stood for|,
  the time a frame stood for being the nearest whole number of refresh periods when the display rate is known.
- Cost: a run without `--profile` takes the unprofiled branch of the loop (no timestamps, no allocation); a profiled
  frame records into a preallocated ring buffer. A test checks that a profiled run gives the same updates, frames and
  blends as an unprofiled one.
- What it cannot see: when the display actually shows a frame. That needs the platform's tools (PresentMon or GPUView
  on Windows, Perfetto/`dumpsys gfxinfo` on Android, Instruments on Apple systems, the browser's performance panel).
- `benchmarks/PerfBench`: a synthetic game with chosen update rate, canvas, sprites, shapes, text, particles and
  interpolation, for loads the demos cannot isolate.

Tools evaluated and not integrated: BenchmarkDotNet (microbenchmarks; S2 asks for whole-engine value, the profiler
measures real frames), `dotnet-trace`/`dotnet-counters` (not installed here; the allocation sources in section 7 were
found with the runtime's own `GCAllocationTick` events through an `EventListener`, which needs nothing installed),
Tracy and Superluminal (native profilers worth using by hand; no engine dependency needed).

## 3. Baseline (deliverables 1, 2)

Meadow Run title screen, SDL (Direct3D 11), VSync, 1,200 updates:

| Measure | Value |
| --- | --- |
| Frame interval p50 / p95 / p99 / p99.9 / max | 13.34 / 15.05 / 16.12 / 26.85 / 39.31 ms |
| CPU work per frame (events + update + draw) p50 / p99 | 0.24 / 0.41 ms |
| Steps per frame | 0: 287 frames, 1: 1,149, 3: 1 |
| Motion error mean / p95 | 5.21 / 13.38 ms |
| Allocations | 4,982 bytes per frame, 1 collection |
| Start | platform 1,451 ms, start screen 57 ms, load 897 ms, first frame 12 ms |

The desktop does almost no work per frame (0.24 ms of a 13.3 ms budget): neither CPU load nor the GPU explains any
irregularity on this machine. Load phases of the four demos before PERF-1 (3 runs each): Meadow Run 840/821/819 ms,
Courtyard Key 555/552/539 ms, Trail Duel 1,035/967/984 ms, Neon Rift 202/192/181 ms.

## 4. Micro-stutter analysis (deliverable 4)

Three separate causes were found; none of them is VSync, SDL3, the integrated GPU or the garbage collector.

1. **60 updates on a display that is not a multiple of 60 Hz.** At 75 Hz, one frame in five shows no new update (the
   287 zero-step frames above are exactly 1/5 of 1,437): the picture repeats a frame every 67 ms, a steady judder.
   At 100 Hz it would be two frames in five. The loop had no interpolation. Fix: `Game.FrameBlend` (the unsimulated part
   of a step) so a game can draw between updates; PerfBench with `--interpolate` brings the motion error at 60 updates
   on the 75 Hz display from 5.33 ms mean / 13.34 ms p95 to 0.00 / 0.00 (counted against refresh periods). The four
   demos do not interpolate yet (section 11): that is game work, and S10 forbids changing their time rules silently.
2. **Irregular wake-ups turned into 0/2-step frames.** SDL's presentation returns irregularly around the refresh
   (interval p95 15.3 ms for a 13.34 ms period, on every SDL render driver; raylib, whose own frame limiter wakes it up,
   stays within ±0.6 ms). The loop fed that jitter straight into its accumulator, so when the update rate matches the
   display rate (60 Hz on a 60 Hz panel; 75 on this one) some frames ran no step and the next ran two: the "occasional
   micro-stutter at full frame rate". PerfBench at 75 updates on the 75 Hz display, 1,500 frames, 5 runs each:
   frames whose steps do not match the refresh periods they stood for, measured time **0, 0, 9, 5, 8**; with the loop
   counting time in refresh periods (`IPlatform.DisplayRate`, `--pacing display`, now the default) **0, 0, 0, 0, 0**. The
   remainder is carried between frames, so time still adds up when VSync is not honoured (a test drives 500 frames per
   second on a display reporting 60 Hz) or the reported rate is wrong.
3. **Real missed refreshes.** Two frames of 26–39 ms, once, about 1.9 s after an SDL window appears, on every SDL
   driver (Direct3D 11/12, Vulkan, OpenGL, GPU), never with raylib; they move with window creation, not with game time,
   load, collections (none happened) or the gamepad subsystem (unchanged with every SDL joystick driver disabled). It
   is consistent with the compositor switching the window's presentation mode, but that needs PresentMon to confirm
   (open, section 11). It happens during the start screen or title, once per run.

Also measured and ruled out: garbage collections (0–2 per 20 s run, none at a hitch), JIT (the first ~10 frames cost
up to 10 ms of CPU, all inside the start), late resource loading (none after load in the demos), threads (the loop runs
on one thread; background work is the music rendering of section 6, which caused no early hitch in 240-frame windows).

## 5. Graphics backends and API evaluation (deliverables 6, 7, 8)

Measured (run), PerfBench hidden fixed-step (no VSync), mean CPU time to submit and present a frame:

| SDL render driver | 10,000 sprites: draw / present | 2,000 circles: draw / present | Pacing (75 Hz, VSync) |
| --- | --- | --- | --- |
| direct3d11 (default on Windows) | 1.04 / 0.18 ms | 1.49 / 0.34 ms | p95 15.3 ms wake-up jitter, absorbed by pacing |
| direct3d12 | 1.06 / 0.40 ms | 1.52 / 0.98 ms | same |
| gpu (SDL_GPU, here Direct3D 12) | 1.02 / 0.30 ms | 1.95 / 0.53 ms | same |
| vulkan | 1.22 / 0.37 ms | 1.68 / 0.67 ms | same; the VSync wait happens while drawing |
| opengl | 1.60 / 0.21 ms | 1.62 / 0.82 ms | p95 13.65 ms; wait while drawing |
| software | 0.79 / 2.54 ms | 6.97 / 34.1 ms | not for real-time use |
| raylib 6.0 (OpenGL) | 0.90 ms draw | 2.87 ms draw | p95 13.39 ms (its own limiter) |

Direct3D 11 is the cheapest and steadiest SDL driver here, which is SDL's own default order: there is no evidence for
changing the default (S7: "No asumas que Vulkan o Direct3D 12 siempre proporcionan mejor rendimiento"), and none was
changed. Desk evaluation of the alternatives:

- **SDL_Renderer** (kept): batches internally, picks a driver per system, falls back across drivers on its own when
  `SDL_CreateRenderer` gets no name, and since SDL 3.4 can run on SDL_GPU (the `gpu` driver, `SDL_CreateGPURenderer`)
  with custom fragment shaders through GPU render states ([SDL release notes as packaged by SDL3-CS][sdl3cs]). That is
  the incremental path to effects and, later, to 3D: the same 2D API, with the SDL_GPU device reachable underneath.
- **SDL_GPU directly**: a modern explicit API over Direct3D 12, Vulkan and Metal with 3D and compute ([SDL wiki][sdlgpu]),
  but shaders must be supplied per backend (SDL_shadercross cross-compiles at build or run time) and no maintained 2D
  batcher for SDL3's GPU API was found (the older SDL_gpu library targets SDL 1.2/2 ([grimfang4/sdl-gpu][sdlgpu-lib])).
  A public account of an SDL_GPU 2D renderer in Simutrans measured it slower than its software path at the time
  ([forum][simutrans]); our own `gpu` driver numbers are on par with Direct3D 11 for sprites and slower for geometry.
  Not adopted now: a custom renderer is a large migration with no measured bottleneck to justify it (S6).
- **Direct3D 11/12, Vulkan, Metal, OpenGL/ES**: reached through SDL's drivers; MicroEngine needs no direct binding.
- **raylib**: kept for browsers (WebGL through emscripten) and as the desktop alternative; its frame limiter gives the
  steadiest wake-ups measured, its draw cost per sprite is lower, its geometry is not.
- **WebGL / WebGPU**: WebGPU now ships in Chrome (Android 12+ from Chrome 121), Safari 26 (macOS, iOS, iPadOS) and
  desktop Firefox on Windows and Apple silicon, but not in Firefox for Android or on older phones
  ([web.dev][webgpu-web], [Wikipedia][webgpu-wiki]). raylib's browser path is WebGL; replacing it would cut the phones
  and browsers that matter most to S5. WebGPU stays a future backend once it covers mobile browsers broadly.

## 6. Start and loading (deliverable 9, part of 12)

| Phase | Before | After | Change |
| --- | --- | --- | --- |
| SDL platform creation (Meadow Run) | 1,339–1,451 ms | 269–287 ms | SDL's gamepad subsystem (1,070 ms here, 960 ms with every joystick driver off, so in SDL's common setup) starts after the start screen is shown |
| Window shows the game's colour and image | after ~1.4 s | after ~0.3 s | same |
| Load, Meadow Run (3 runs) | 840/821/819 ms | 254/263/259 ms | songs render in the background (`AudioMixer.AddMusic(name, Song)`) |
| Load, Courtyard Key | 555/552/539 ms | 244/249/263 ms | same |
| Load, Trail Duel | 1,035/967/984 ms | 473/437/443 ms | same |
| Load, Neon Rift (music from files) | 202/192/181 ms | 181/192/193 ms | control: unchanged |

Music synthesis was 646 of Meadow Run's 828 ms of load (art 105 ms, sound effects 38 ms). Time to the first playable
frame is shorter by the load saving; the gamepad second moved, it did not disappear (open, section 11). Browser:
the published Meadow Run (pre-PERF-1) downloads 2.4 MB in 36 requests and is ready to play 1.2 s after navigation
in the built-in Chromium (browser); frame timing there was not measured (the pane was hidden, which stops animation
frames). Android's ~4 s to the title seen in GAME-POLISH was on an emulator and was not reproduced on hardware.

## 7. Renderer and loop (deliverable 9)

| Change | Before → after (3 runs each) |
| --- | --- |
| SDL renderer: unchanged texture and draw state not set again in a frame; plain copy for unrotated, unflipped sprites; precomputed circle rim | 10,000 sprites 1.77 → 1.05 ms; 2,000 circles 2.07 → 1.46 ms; 60 text lines 0.178 → 0.148 ms; 3,000 particles 0.367 → 0.345 ms |
| Input sampling and touch controls without per-frame garbage | empty scene 2,584 → under 256 bytes per frame; Courtyard Key 2,791 → 304; Meadow Run 5,460 → 2,752; Neon Rift 4,472 → 1,472; Trail Duel 6,474 → 4,015 |
| raylib: no VSync in hidden windows | scripted 1,200-frame run 17.2 → ~2.0 s |

The 270 captures of the demo scripts are byte-identical before and after the renderer change, except one region of
Meadow Run's controls screen that also differs between two runs of the same build (environment-dependent, not the
renderer). The remaining allocations are the games' own (scenes returning new arrays every frame, string formatting
for the HUD). Batching, render targets and texture uploads were looked at and left alone: SDL batches already, the
frame uses one render target, textures are uploaded once at load.

## 8. Backend selection and multiple GPUs (deliverable 10)

- Selection: without `--platform`/`MICROENGINE_PLATFORM`, the launcher now tries every platform of the catalog that can
  run here except headless, in order (desktop: SDL, raylib, SDL software), and says which failed and why. Inside SDL,
  `SDL_CreateRenderer` already falls back across drivers; `SDL_RENDER_DRIVER` (a comma-separated list) remains the
  developer's override. A named platform still fails with its own error.
- Adapters: DXGI lists the RTX first, the Radeon second, Microsoft's software adapter third; SDL's Direct3D 11 renderer
  uses adapter 0 (the RTX, run). Neither the wake-up jitter nor the late missed refreshes come from the hybrid setup.
- `--gpu auto|high-performance|power-saving` (`GpuPreference`): SDL can choose a device only for its Metal and GPU
  renderers (`SDL_HINT_RENDER_METAL_PREFER_LOW_POWER_DEVICE`, `SDL_HINT_RENDER_GPU_LOW_POWER`); Direct3D 11 and OpenGL use
  the adapter Windows assigns (Settings, Display, Graphics). Run: `power-saving` moved the `gpu` renderer to the Radeon;
  `direct3d11` stayed on the RTX. Rendering on the Radeon for the RTX's display (cross-adapter) showed no missed refresh
  and no stutter at PerfBench's load. Not done: forcing the dedicated GPU (the `NvOptimusEnablement` /
  `AmdPowerXpressRequestHighPerformance` exports need a native executable exporting data, which .NET's app host does not
  provide), and changing the user's Windows graphics preference (a system setting).
- Diagnostics: `--profile` records the renderer, VSync, display, adapter and SDL's start steps.

## 9. Headless audio (deliverable 11)

Hidden windows and scripted runs opened the real sound device (`--hidden` did not imply `--mute`; a native test played
30 sounds). Now `PlatformRequest.AudioOutput` decides: `Automatic` opens a device only for real-time visible runs;
`--audio device` asks for one explicitly (the package checks, which must prove a device opens, pass it; phone hosts
keep theirs); `--audio silent` never. `--audio-capture FILE` records what would be heard through `CaptureAudioDevice`
(deterministic mix by game time, the devices' balance panning, looping, pause, music volume) as a WAV: a 20 s Meadow
Run script produced 20.00 s with the music present in 96.8% of samples and nothing on the speakers. SDL's own dummy
and disk drivers are reachable with `SdlAudioDevice.TryCreate(driver)`; the platform-equivalence test now uses dummy.

## 10. Mobile and WebAssembly (deliverable 5)

Not measured on hardware: no Android phone, iPhone or iPad was connected, `adb` is not installed, and the `wasm-tools`
workload is not installed (building a browser package would need it). What the changes mean there, by design: display
pacing applies to SDL's phone hosts (VSync is the only pacer there), the allocation work matters more on phones and in
browsers (WebAssembly's collector is not concurrent), background song rendering does not apply in browsers (no threads;
songs render at registration as before). Procedure for the pending measurements: build with `microengine package
android|ios|web`, run the demo with `MICROENGINE_PROBE=1` (phones) or the page's `window.microengine.stats()`, and use
Perfetto, Instruments or the browser's performance panel for presentation times; compare only like with like (same
device, build, demo, scene and duration).

## 11. Limitations and future opportunities (deliverable 15)

- The demos do not interpolate: on 75/100/144 Hz displays their motion still shows the 1-in-N repeated frame. Each
  demo can adopt `FrameBlend` for its camera and moving actors (previous/current positions), checked with captures and
  `--profile`.
- The gamepad subsystem still costs about a second on this machine, now behind the start screen. Worth reporting to
  SDL with a minimal reproduction, or starting it on demand.
- The one-off missed refreshes ~1.9 s after an SDL window appears need PresentMon to identify.
- No measurement on phones, tablets or in mobile browsers; none on Linux or macOS desktops.
- SDL_GPU through SDL's `gpu` driver with custom shaders is the candidate for effects and future 3D; to be measured on
  the phones and integrated GPUs it would run on before any default change.
- A game with no scene fails with an index error inside `SceneManager.Update` (seen while testing; unchanged).

[sdl3cs]: https://www.nuget.org/packages/SDL3-CS
[sdlgpu]: https://wiki.libsdl.org/SDL3/CategoryGPU
[sdlgpu-lib]: https://github.com/grimfang4/sdl-gpu
[simutrans]: https://forum.simutrans.com/index.php?msg=213848
[webgpu-web]: https://web.dev/blog/webgpu-supported-major-browsers
[webgpu-wiki]: https://en.wikipedia.org/wiki/WebGPU

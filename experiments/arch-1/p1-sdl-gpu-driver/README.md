# ARCH-1 P1: the demos on SDL_Renderer's `gpu` driver

Prototype P1 of the ARCH-1 dossier (§9). Engineering material; not part of the solution, CI or the site.

## Question

Do the four demos (Meadow Run, Courtyard Key, Trail Duel, Neon Rift) and the asset gallery run on SDL_Renderer's
`gpu` driver (the 2D renderer built on SDL_GPU) with output matching today's default driver, and what does it cost
(draw calls, frame time, start-up time, memory)? The answer decides the tier-0 SDL base driver before stage 2a. This
is a measurement, not a change of defaults.

## Verdict

**Keep the current default (SDL's own choice, Direct3D 11 on Windows) as the tier-0 base driver.** The `gpu` driver
is correct here (every demo capture pixel-identical to the default driver and to `web/tests/reference/sdl.json`, on
both SDL_GPU backends), so it is a sound tier-1 path and a sound opt-in, but on this evidence it costs more than it
gives to tier-0 games: about +220 ms start-up, +50 MB peak commit (+185 MB on Vulkan), a slower present on
Direct3D 12, and a behaviour change in full screen (Direct3D 12 waits for the display although VSync is off). Only
Windows was measured; Linux, macOS, Android and iOS remain **undecided** for P1's "every desktop and phone" scope.

## Method

- **Selecting the driver needs no code change.** `SdlPlatform` passes the game's preferred renderers
  (`GraphicsSettings.Renderers`, the launcher's `--renderer NAME[,NAME...]`) to `SDL_CreateRenderer` as a
  comma-separated list and falls back to SDL's own choice with a warning when none starts
  (`src/MicroEngine.Sdl/SdlPlatform.cs`, constructor). So every run used `--platform sdl`, plus `--renderer gpu` for
  the `gpu` variants. `SDL_GPU_DRIVER=vulkan` forced SDL_GPU's backend; `--gpu power-saving` (`SDL_HINT_RENDER_GPU_LOW_POWER`)
  moved SDL_GPU onto the integrated GPU. The `sdl-<driver>` profiles exist (`SdlPlatform.CreateProfile`) but only
  `sdl-software` is registered in `DesktopLauncher`, so `--platform sdl-gpu` is not available to the demos.
- **Scenarios.** The 15 demo scenarios CI compares (`web/tests/scenarios.json`, same environment variables, 138
  captures) and `samples/AssetGallery/scripts/tour.script` (3 captures), each started as CI starts them:
  `--script FILE --hidden --mute --captures DIR` (fixed step, seed 1), plus `--report` and `--profile FILE`.
- **Variants** (`run.mjs`): `default` (SDL's choice: `direct3d11`), `gpu` (SDL_GPU's choice: Direct3D 12),
  `gpu-vulkan`, `gpu-lowpower` (Direct3D 12 on the integrated AMD GPU), and, as a control for the last one, one run of
  `gpu-vulkan-lowpower`. `SDL_LOGGING=gpu=debug,render=debug` makes SDL name the backend and adapter it chose; each
  run's `log.txt` confirms the driver (`Created renderer: ...`, `SDL_GPU Driver: ...`).
- **Runs.** 5 runs per scenario and variant, sequential, driver order alternating per run: 320 processes, all exit 0.
  Extra: 3 visible-window runs (`--visible`) and 1 full-screen run (`--visible --window fullscreen`) of
  Meadow Run `smoke` and Neon Rift `stage1`.
- **Image comparison** (`compare.mjs`), run 1 of each variant against run 1 of `default`, by properties: exact match
  count; for mismatches, differing pixels, largest per-channel difference, how many differing pixels lie on the base
  image's edges (8-neighbour colour change, dilated by one pixel; the share of the image the mask covers is given
  for scale), bands of difference size and the bounding box. Also: determinism (runs 2-5 against run 1 of the same
  variant) and the repository references through `web/tests/captures.mjs` (hash equality reported, grid tolerance 3).
- **Timing and memory** (`measure/`, a small C# launcher, and `summary.mjs`):
  - *start-up* = process start (just before `CreateProcess`) to the launcher's `platform:` line (runtime, SDL video,
    window, renderer, gamepads) + the profile's `start screen`, `load` and `first frame` phases;
  - *renderer* = `SDL_CreateRenderer` alone (the SDL platform's `start.renderer` diagnostic);
  - *frame* = `FrameProfiler`'s median and p95 frame interval after its 60 warm-up frames. Scripted runs are fixed-step
    on a hidden window, hence unpaced: the interval is the CPU-side cost of a whole frame including submission;
  - *present* = median `EndFrame` time (finish, present);
  - *memory* = peak commit (`PeakPagefileUsage`, which includes the graphics driver's user-mode heaps) and peak
    working set, read with `GetProcessMemoryInfo` on the exited process. Dedicated GPU memory was not measured.
- **Draw calls / batches are not observable** without a graphics debugger: SDL_Renderer exposes no statistics and
  the engine counts draw calls only in its headless backend. Not measured.

## Environment

| | |
| --- | --- |
| Machine | AMD Ryzen 5 7600X (6 cores, 12 threads), Windows 11 Pro 10.0.26300, 3 displays (primary 1920x1080 @ 100 Hz) |
| Discrete GPU | NVIDIA GeForce RTX 4070 Ti SUPER, driver 32.0.16.1088 (Vulkan: NVIDIA 610.88, conformance 1.4.3.3) |
| Integrated GPU | AMD Radeon(TM) Graphics (Ryzen 7600X iGPU), driver 32.0.21042.62 |
| Runtime | .NET 10.0.14, SDK 10.0.403, Release builds |
| SDL | **3.5.0** (from `ppy.SDL3-CS` 2026.1002.1); the dossier and R1 speak of 3.4, where the GPU renderer arrived |
| Engine | `main` at d2b5147, unchanged |
| Linux | **Not run.** WSL (lavapipe/llvmpipe) could not be invoked from this session's sandbox. |

## Results

Full tables: `results/hidden-summary.md` (timing, memory, logs), `results/hidden-compare.txt` (every mismatch),
`results/lowpower-vs-macos.txt`, `results/visible-*.txt|md`, `results/fullscreen-*.txt|md`. Scratch paths in them
read `<scratch>`.

### Output

| Variant (run 1) | vs `default`: exact | vs `sdl.json` (138) | Determinism, runs 2-5 vs run 1 |
| --- | --- | --- | --- |
| `gpu` (D3D12, NVIDIA) | 138/138 demo captures; 0/3 gallery | 138 exact | 553/564 |
| `gpu-vulkan` (NVIDIA) | 138/138 demo captures; 0/3 gallery | 138 exact | 552/564 |
| `gpu-lowpower` (D3D12, AMD iGPU) | 11/141 | 11 exact, 25 beyond tolerance | 552/564 |
| `default` (D3D11, NVIDIA) | — | 138 exact | 552/564 |

- **On the NVIDIA GPU both SDL_GPU backends reproduce today's output exactly.** Visible windowed (13/13) and full-screen
  (13/13, Direct3D 12 and Vulkan) captures are exact too.
- **The gallery's differences are not rendering.** In every variant, including `default` against itself, the gallery's
  three captures change from run to run: they show a wall-clock "loaded in N ms" (the differing box is
  1173-1239 × 510-526, 735 px, 92 % on edges) and, in one trial run, the level scope still loading
  (`images/gallery-gpu-vs-default-diff.png`). Its captures are not deterministic, so they cannot judge a driver.
- **On the AMD integrated GPU the output changes, identically on Direct3D 12 and Vulkan** (`gpu-vulkan-lowpower` vs
  `gpu-lowpower`: 138/138 demo captures exact). Two kinds of difference (`results/hidden-compare.txt`):
  - ±1 levels in blended gradients (Courtyard Key's lit rooms: 12-20 % of pixels, almost all at difference 1, 96 % on
    edges; Courtyard Key's title and ending, Trail Duel and Meadow Run text: max 1);
    `images/amd-vs-nvidia-boss_b-diff.png`;
  - small sprites one pixel off (Courtyard Key's HUD hearts, Neon Rift and Trail Duel sprites: max 56-243 on a few
    hundred pixels, ~90-100 % on edges), the half-pixel rounding A2's probe P5 found between backends;
    `images/amd-vs-nvidia-hearts.png` (NVIDIA, AMD, differences > 15).
  
  The AMD output is **within tolerance of the macOS Metal reference** (`sdl-macos-arm64.json`: 0 captures beyond
  tolerance) while today's default on NVIDIA is not (25 beyond). So this is the cross-GPU variation the repository
  already records between platforms, not a defect of the `gpu` driver. The control that would prove it (Direct3D 11 on
  the AMD GPU) needs a Windows per-app graphics setting, a system setting this prototype did not change.

### Cost (hidden, unpaced; median of the 16 scenarios' medians over 5 runs; ratio to `default`)

| Metric | `default` (D3D11) | `gpu` (D3D12) | `gpu-vulkan` | `gpu-lowpower` (AMD) |
| --- | --- | --- | --- | --- |
| Start-up to first frame (ms) | 2006 | 2227 (x1.11) | 2326 (x1.16) | 2677 (x1.33) |
| `SDL_CreateRenderer` (ms) | 163 | 401 (x2.46) | 573 (x3.52) | 841 (x5.17) |
| Frame interval p50 (µs) | 274 | 403 (x1.47) | 237 (x0.86) | 1086 (x3.96) |
| Frame interval p95 (µs) | 755 | 1293 (x1.71) | 730 (x0.97) | 2301 (x3.05) |
| `EndFrame`/present p50 (µs) | 82 | 194 (x2.36) | 52 (x0.63) | 859 (x10.48) |
| Peak commit (MB) | 173 | 222 (x1.28) | 358 (x2.07) | 312 (x1.80) |
| Peak working set (MB) | 184 | 220 (x1.19) | 276 (x1.50) | 299 (x1.62) |

Spread: per-scenario min-max ranges are in `results/hidden-summary.md`; start-up ranges are typically 50-300 ms with
occasional outliers of seconds on every variant (e.g. Meadow Run `smoke` default 1978-5815 ms), which the medians
absorb. The start-up of every variant includes about 1 s of gamepad subsystem start, unrelated to the driver.

- Start-up grows by about the renderer's creation (+240 ms Direct3D 12, +410 ms Vulkan on NVIDIA); where inside SDL
  that time goes was not profiled.
- Frame cost is small in absolute terms for every variant (all well under 2 ms at p95 on NVIDIA). Direct3D 12 is the
  slowest backend on the CPU side (present 2.4×), Vulkan the fastest (below Direct3D 11).
- Visible windowed runs (3 runs, 2 scenarios; `results/visible-summary.md`) agree: start-up 1736 → 1971 ms, frame p50
  253 → 355 µs, present 67 → 183 µs.

### Behaviour

- No crash, no failed start, no missing feature: all 320 hidden runs, 12 visible and 6 full-screen runs exited 0 with
  every capture written; the logs hold no warning at SDL's default log levels. With debug logging, SDL_GPU's
  Direct3D 12 backend warns that `WinPixEventRuntime.dll` is missing (only GPU debug labels need it).
- **Full screen on Direct3D 12 does not honour VSync off.** With `--window fullscreen` (desktop full screen, VSync
  reported 0), `gpu` frames took 10.0 ms at p50 and `EndFrame` 9.8 ms: the present waits for the 100 Hz display.
  Direct3D 11 (0.27 ms) and SDL_GPU's Vulkan backend (0.33 ms) do not wait. A game with VSync on would not notice; one
  with `--vsync off`, a benchmark or an unpaced scripted run in full screen would (`results/fullscreen-summary.md`).
- The demo scripts do not toggle full screen or resize the window, so F11 and resizing on the `gpu` driver were **not
  exercised**; only starting in full screen was.
- A preferred renderer that cannot start falls back to SDL's choice with a warning on stderr (seen once while trying
  an empty `SDL_GPU_DRIVER`), so selecting `gpu` cannot leave a player without a renderer.

## Risks and open points

- Only Windows with a desktop NVIDIA GPU and an AMD iGPU was measured. Linux (Vulkan, lavapipe), macOS (Metal),
  Android (Vulkan driver quality) and iOS (SDL_GPU has no simulator support) are still open, as is CI: hosted Windows
  runners have no GPU, and SDL_GPU offers no Direct3D 11 WARP path; whether its Direct3D 12 or Vulkan backends run on
  WARP/lavapipe there was not tried.
- Default-driver references would break on the iGPU-class hardware variation shown above whichever driver is chosen;
  the grid tolerance does not absorb one-pixel sprite offsets (25 captures beyond tolerance).
- The full-screen VSync behaviour of SDL_GPU's Direct3D 12 swap chain needs an upstream check (SDL 3.5.0) before any
  default switch on Windows.
- The gallery's captures embed wall-clock time; they need a fixed clock before they can serve as references.

## Reproduce

```sh
dotnet build games/MeadowRun/MeadowRun.csproj -c Release      # likewise the other three games
dotnet build samples/AssetGallery/AssetGallery.csproj -c Release
node experiments/arch-1/p1-sdl-gpu-driver/run.mjs --out <scratch>/full --runs 5
node experiments/arch-1/p1-sdl-gpu-driver/run.mjs --out <scratch>/full --runs 1 --drivers gpu-vulkan-lowpower
node experiments/arch-1/p1-sdl-gpu-driver/run.mjs --out <scratch>/visible --runs 3 --drivers default,gpu --scripts smoke,stage1 --visible
node experiments/arch-1/p1-sdl-gpu-driver/run.mjs --out <scratch>/fullscreen --runs 1 --drivers default,gpu,gpu-vulkan --scripts smoke,stage1 --visible --window fullscreen
node experiments/arch-1/p1-sdl-gpu-driver/compare.mjs --out <scratch>/full --variants gpu,gpu-vulkan,gpu-lowpower,gpu-vulkan-lowpower --diff-images <scratch>/diff
node experiments/arch-1/p1-sdl-gpu-driver/summary.mjs --out <scratch>/full --drivers default,gpu,gpu-vulkan,gpu-lowpower
```

`run.mjs` builds `measure/` into `<scratch>/_measure` (never into the repository). Keep `--out` outside the
repository: a full run writes about 1,500 PNG files.

| File | Purpose |
| --- | --- |
| `run.mjs` | plays the scenarios per variant and run (captures, profile, timing, memory, logs) |
| `measure/` | Windows launcher: wall time, time to the `platform:` line, peak memory |
| `compare.mjs` | capture comparison by properties, determinism, repository references, diff images |
| `summary.mjs` | start-up, frame, present and memory tables; log lines per variant |
| `crop.mjs` | side-by-side crop with a difference mask (`images/amd-vs-nvidia-hearts.png`) |

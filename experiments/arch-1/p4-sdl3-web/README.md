# ARCH-1 P4: SDL3 on .NET WebAssembly

A throwaway prototype. Nothing here is built by `MicroEngine.slnx` or by CI (`experiments/` is classified as needing no
checks), and the repository's own code is not changed: the demo runs from a patched scratch copy.

## Question

Can SDL3, built with the Emscripten that the .NET 10 `browser-wasm` runtime uses, run a MicroEngine game in a browser
through the existing SDL backend (`MicroEngine.Sdl`), replacing raylib for the web?

That decides, before the stage 1 platform/device split is frozen, whether the platform contract must support
**integrated** backends (host and device fused, as raylib is) or whether **one SDL host** can cover all six platforms.

## Verdict

**SDL3 can replace raylib for the web.** Two real demos (Meadow Run, Courtyard Key) ran on `MicroEngine.Sdl` in
Chrome and Firefox with no game change, matched raylib in behaviour and frame cost, started faster and, with the SDL
binding trimmed, downloaded less. Two binding-level interop defects and four host-level browser rules had to be handled;
none of them needs host and device to be fused.

**Implication for the platform contract: integrated backends are not needed.** One SDL host (page-driven loop on the
web, callback or blocking loop elsewhere) plus SDL devices covers Windows, Linux, macOS, Android, iOS and browsers.
What the browser needs is host behaviour (who owns the loop, never block, canvas size, audio unlock, visibility), which
belongs in the host side of the split, not in a fused backend. If raylib is kept during migration, it can be adapted
as "a host and a device implemented by the same assembly" without the contract declaring a fused kind.

It is a feasibility result, not a production switch: see [What was not verified](#what-was-not-verified) and
[Risks](#risks).

## Environment

| Item | Version |
| --- | --- |
| OS | Windows 11 Pro 10.0.26300 (Git Bash) |
| .NET SDK / browser-wasm runtime pack | 10.0.403 / 10.0.14, private copy from `web/setup.sh` (`web/toolchain.env`) |
| Emscripten | 3.1.56, from the `wasm-tools` workload (`Microsoft.NET.Runtime.Emscripten.3.1.56.*` packs) |
| SDL | 3.5.0 at commit `ea7a2da`: the build `ppy.SDL3-CS` 2026.1002.1 ships (its `SDL3.dll` reports `SDL-3.5.0-ea7a2da`) |
| raylib (baseline) | 6.0, `raylib.a` rebuilt by `web/setup.sh` with the same Emscripten |
| CMake / Ninja | 4.1.0 / Ninja from `pip install ninja` |
| Browsers | Chrome 156.0.8078.76 headless (puppeteer-core 25.13.0), Firefox 150.0.3 headless (WebDriver BiDi) |

## How the repository builds the raylib web host today

- `web/toolchain.env` pins the SDK, the workload set and therefore Emscripten 3.1.56; `web/setup.sh` installs them
  privately and rebuilds raylib with that Emscripten (`web/build-raylib-wasm.sh`), because the `raylib.a` inside
  Raylib-cs was built with a newer Emscripten that `wasm-opt` rejects.
- The host template (`src/MicroEngine.Cli/Hosts/web/Host.csproj`) is a `Microsoft.NET.Sdk.WebAssembly` app with
  `WasmBuildNative=true`; a target swaps the package's `raylib.a` for the rebuilt one through `NativeFileReference`.
  Raylib-cs's build files add `-sUSE_GLFW=3`. `TrimMode=partial` (reflection-based saves).
- `Program.cs` exports `WebHost.Start/Frame/Stop/...`; `main.js` calls `Frame` once per `requestAnimationFrame`
  (ADR 0007: the browser owns the loop). `BrowserLauncher` builds a `GameSession` on `RaylibPlatform`.

The prototype keeps all of that and changes only what links and which platform the session creates.

## What was built

| File | What it does |
| --- | --- |
| `build-sdl3-wasm.sh` | Downloads SDL at `ea7a2da` and builds `SDL3.a` (static, `MinSizeRel`) with the workload's Emscripten through CMake + Ninja: no threads (`SDL_THREADS=OFF`, as the single-threaded .NET runtime), no GPU, camera or Vulkan |
| `SdlProbe/` | Minimal .NET 10 browser-wasm app on `ppy.SDL3-CS` + `SDL3.a`: window on the page canvas, `SDL_Renderer` drawing (fill, textured geometry, debug text, render target scaled up), keyboard/mouse/touch events, an audio stream fed every frame; `main.js` calls `Init` inside the first gesture and `Frame` per `requestAnimationFrame` |
| `build-probe.sh` | Publishes `SdlProbe` with the pinned toolchain (`-p:ProbeSymbols=true` keeps wasm function names for stack traces) |
| `sdl-web.patch` | The adaptations, applied to a scratch copy: host links `SDL3.a` and runs the game on `SdlPlatform`; `SdlPlatform` gets the browser rules below |
| `build-demo.sh` | Copies the repository sources to a scratch folder, applies the patch, runs that copy's `microengine package web`; `P4_BACKEND=raylib` builds the unpatched baseline with the same tool; `P4TrimSdl=true` lets the trimmer remove unused SDL3-CS P/Invokes |
| `measure.mjs`, `compare.sh` | Headless Chrome (or `--firefox`): load and start time, `requestAnimationFrame` interval and time inside each frame, AudioContext states, ScriptProcessorNode buffer size and whether it produced non-silent output, keys and CDP touches, WebGL version |
| `input.mjs` | On development packages (`--dev`): frames until a held key and a finger appear in the game's per-frame trace, which touch control was hit, and whether the canvas follows a page resize |
| `sizes.mjs` | Package size as `web/build.sh` counts it (raw and with the `.br` copies), and the native module alone |

## Steps to reproduce

```bash
web/setup.sh                                       # pinned .NET + wasm-tools (Emscripten 3.1.56) + raylib.a
W=$(mktemp -d)                                     # scratch, outside the repository
pip install --target "$W/ninja" ninja
P=experiments/arch-1/p4-sdl3-web
NINJA="$W/ninja/bin/ninja" $P/build-sdl3-wasm.sh ~/.microengine/web-tools/dotnet "$W/sdl"      # → $W/sdl/out/SDL3.a
$P/build-probe.sh "$W/sdl/out/SDL3.a" "$W/probe"
P4TrimSdl=true $P/build-demo.sh "$W/sdl/out/SDL3.a" "$W/sdl-demo" MeadowRun                   # also CourtyardKey
P4_BACKEND=raylib $P/build-demo.sh ~/.microengine/web-tools/raylib-web/raylib.a "$W/raylib-demo" MeadowRun
node web/serve.mjs "$W/sdl-demo/out" 8094 &  node web/serve.mjs "$W/raylib-demo/out" 8093 &
npm install --prefix "$W/node" puppeteer-core@25.13.0
PUPPETEER_DIR="$W/node" $P/compare.sh 3 6 http://127.0.0.1:8093/MeadowRun/ http://127.0.0.1:8094/MeadowRun/
node $P/sizes.mjs "$W/raylib-demo/out/MeadowRun" "$W/sdl-demo/out/MeadowRun"
```

`SdlProbe` writes `bin/` and `obj/` next to its project (ignored by Git); delete them after use.

## Results

### Probe

`|sdl=3.5.0 video=emscripten renderer=opengles2 audio=emscripten`. It drew the moving rectangle, the coloured
triangle (`SDL_RenderGeometry`) and debug text into a 320×180 target scaled ×3; four key presses and two CDP touches
arrived as SDL events; the 440 Hz tone reached the speakers (109 of 114 ScriptProcessorNode callbacks non-silent,
peak 0.2, the amplitude written). Package 1125 KB with Brotli, native module 715 KB. Frame time 0.2 ms median.

### Demo vs raylib (same demo, same tool, same toolchain, same machine)

Medians of three runs in headless Chrome, package served from localhost (`web/serve.mjs`, Brotli). "Frame work" is
the time spent inside the page's `requestAnimationFrame` callback, which is the whole game frame (`WebHost.Frame`).
"SDL" is the build with `P4TrimSdl=true`; the untrimmed SDL build is in the size rows.

| Measure | Meadow Run raylib | Meadow Run SDL | Courtyard Key raylib | Courtyard Key SDL |
| --- | --- | --- | --- | --- |
| Download, Brotli (whole package) | 2696 KB | **2544 KB** (2797 KB untrimmed) | 2690 KB | **2540 KB** (2792 KB untrimmed) |
| Download, raw | 8247 KB | 8033 KB (8692 KB untrimmed) | 8216 KB | 8001 KB (8660 KB untrimmed) |
| Native module (`dotnet.native.*`), Brotli | 800 KB | 731 KB (901 KB untrimmed) | 799 KB | 732 KB (900 KB untrimmed) |
| Page load to "ready" | 1234 ms | 1200 ms | 1190 ms | 1292 ms |
| Click to playing (platform + game start) | 864 ms | **727 ms** | 849 ms | **743 ms** |
| Frame work, median / p95 / max | 4.8 / 5.7 / 46.3 ms | 4.8 / 5.9 / 49.5 ms | 4.7 / 5.5 / 25.7 ms | 4.6 / 5.5 / 25.2 ms |
| Frames, game updates in ~7 s | 695 / 419 | 696 / 419 | 698 / 418 | 697 / 418 |
| AudioContexts created / final state | 3 (2 closed) / running | 1 / running | 3 (2 closed) / running | 1 / running |
| Audio output path | ScriptProcessorNode 2048 frames (42.7 ms) | same | same | same |
| Non-silent audio callbacks | 102 / 176 | 101 / 175 | 54 / 176 | 52–56 / 175 |
| WebGL context | WebGL 1 | WebGL 1 | WebGL 1 | WebGL 1 |
| Page errors | 0 | 0 | 0 | 0 |

Input and layout (development packages, `input.mjs`):

| Check | raylib | SDL |
| --- | --- | --- |
| Held Right key seen by the game (frames after keydown) | 1 | 0–1 (two runs) |
| Finger down on the canvas → touch controls shown, one finger | 2 frames | 2 frames |
| Control hit by a tap at (120, 600) on a 1024×768 page | `up` | `up` (after the resize fix below; `left` before it) |
| Game frame rectangle on 1024×768 / after resize to 640×480 | `32 54 960 540` / `53 0 533 299.8` | identical |

Firefox 150 (headless): both backends loaded, started from a click and ran Courtyard Key at the page's rate (99.8 fps
reported by the game's counters), audio context `running` for both (for SDL found through `Module.SDL3.audioContext`).
Firefox reports one page error in both runs that comes from `measure.mjs`'s own instrumentation (its preload script
runs in a sandbox), not from the game.

Screenshots of Courtyard Key on both backends show the same scene with no visible difference (taken at slightly
different animation times, so not pixel-compared).

### Reading the numbers

- **Size.** SDL3's native code is about the size of raylib + GLFW's JavaScript. The untrimmed SDL build is larger only
  because `TrimMode=partial` keeps all of SDL3-CS, so its ~1300 P/Invokes pull every SDL function into the link; with
  SDL3-CS trimmable the package is about 150 KB (Brotli) smaller than today's raylib package, which also carries
  SDL3-CS's managed assembly unused (ADR 0017).
- **Frame time** is the game's managed code under the .NET interpreter; the backend does not show in it.
- **Start-up** is faster on SDL (about 110–140 ms). Not profiled; one visible difference is that raylib's audio
  (miniaudio) creates three AudioContexts and closes two, SDL creates one.
- **Headless limits.** `requestAnimationFrame` ran at 100 Hz in headless Chrome and there is no display vsync, so
  these frame numbers compare backends; they are not what a player's screen shows. Load times are from localhost, so
  they measure runtime start-up, not network download.

## Blockers found and how they were handled

1. **64-bit enum parameters (interop, blocker until worked around).** `SDL_CreateWindow`'s flags are `SDL_WindowFlags`, a
   `ulong` enum in SDL3-CS. The Mono interpreter on wasm32 called it with the wrong wasm signature: `RuntimeError:
   function signature mismatch` in `do_icall` (stack symbolized with `-p:ProbeSymbols=true`). Declaring the same entry
   point with a plain `ulong` parameter works. `SDL_GetWindowFlags` (64-bit enum return) was replaced the same way. The
   generated `pinvoke-table.h` declares the right C types, so the defect is in the interpreter's call path, not the
   link. Every SDL3-CS API with a 64-bit enum needs such a shim on the web.
2. **Function-pointer parameters (interop, blocker until worked around).** `SDL_AddEventWatch` takes the filter as
   `delegate* unmanaged[Cdecl]<...>` (`MONO_TYPE_FNPTR`). The interpreter cannot build a native-call signature for it:
   `aot-runtime-wasm.c:90` then `:188` (`mono_wasm_get_interp_to_native_trampoline`, "CANNOT HANDLE INTERP ICALL SIG" in
   the runtime source; the message text is stripped in release builds). Passing the same pointer as `nint` works, and the
   `[UnmanagedCallersOnly]` callback itself is supported: the build generated
   `wasm_native_to_interp_MicroEngine_Sdl_MicroEngine_Sdl_SdlPlatform_WatchLifecycle`.
3. **Blocking calls (host rule).** `SdlPlatform.Wait` (`SDL_DelayPrecise`) and the suspended-state `SDL_Delay(50)` would
   block the page's only thread; on the web `Wait` returns false (the browser paces frames) and the delay is skipped,
   exactly what `RaylibPlatform` already does with `OperatingSystem.IsBrowser()`.
4. **Initial canvas size (host rule).** SDL's Emscripten window took the size passed to `SDL_CreateWindow` (640×360)
   instead of the canvas's CSS size until the first `resize` event, so the frame was stretched and taps hit the wrong
   control. The page now dispatches one `resize` after `WebHost.Start`; afterwards SDL tracks the page size itself.
5. **Audio unlock (host rule, no change needed).** SDL creates its AudioContext in `SDL_OpenAudioDevice`, which runs
   inside the click or key that starts the game, so it starts `running` under a gesture-required autoplay policy. SDL
   also has its own fallback (a silence timer that resumes the context once `navigator.userActivation` is set). The
   page's "Sound is blocked" check was pointed at `Module.SDL3.audioContext`.
6. **Things that were not problems.** Emscripten version: SDL 3.5.0 compiled and linked cleanly with 3.1.56 when built
   from source (unlike Raylib-cs's prebuilt `raylib.a`). Threads: SDL builds and runs without threads; its Emscripten
   audio is driven by the Web Audio callback. Static P/Invoke: naming the archive `SDL3.a` binds `[LibraryImport("SDL3")]`.
   The loop: no `SDL_AppIterate`/`emscripten_set_main_loop` is needed; `GameLoop.Tick` from `requestAnimationFrame` drives
   SDL as it drives raylib (`SDL_SetRenderVSync` cannot apply without an Emscripten main loop, so the game sees vsync off,
   as with raylib). WebGL: `SDL_Renderer`'s OpenGL ES 2 driver ran on WebGL 1 (the same as raylib) and covered everything
   the 2D renderer uses (render targets, textured geometry, blend modes, scale modes, clip rectangles).

### Adaptations (`sdl-web.patch`)

- `Hosts/web/Host.csproj`: link `SDL3.a` (passed through the tool's `--raylib-wasm` option, reused for the prototype)
  instead of `raylib.a`, drop `-sUSE_GLFW=3`, allow WebGL 2, optional `TrimmableAssembly SDL3-CS`.
- `Hosts/web/Program.cs`: the session creates `SdlPlatform`; saves stay on `BrowserSaveStorage` (JavaScript interop
  only); `Pointer`, `Screen` and `Visibility` become no-ops (SDL reads touch from the canvas itself).
- `Hosts/web/wwwroot/main.js`: one `resize` after start; the sound check also looks at SDL's AudioContext.
- `MicroEngine.Sdl/SdlPlatform.cs`: the two interop shims and the two non-blocking rules above.

## What was not verified

- Real phones and tablets (only CDP touch in desktop Chrome), Safari/iOS, gamepads, high-DPI screens, long sessions.
- Neon Rift and Trail Duel; the repository's browser test suite (`web/tests/run.mjs`, `mobile.mjs`) and its reference
  frames were not run against SDL packages.
- Page features the raylib host wires and the patch leaves out: safe-area insets and density (`WebHost.Screen`),
  haptics, and suspension of a hidden page (SDL sends `SDL_EVENT_WINDOW_HIDDEN`, which `SdlPlatform` does not treat as
  suspension, so music would keep playing in a hidden tab; not tested).
- Audio latency as heard: both backends use a 2048-frame ScriptProcessorNode (42.7 ms buffer); nothing measured end to end.
- Frame time on a real GPU with vsync, memory use, and the multithreaded .NET runtime (`WasmEnableThreads`, which
  would need SDL built with `-pthread`).

## Risks

- **Interop shims.** The two defects (64-bit enums, function-pointer parameters) are in the .NET wasm interpreter path,
  not in SDL. Any SDL3-CS call with those shapes fails at run time, not at build time. A real web host needs either a
  small browser shim layer for the SDL calls the engine makes (with a test that runs each one in a browser) or bindings
  generated without those shapes; reporting them upstream is worth doing.
- **Version pairing.** SDL must be compiled with the workload's Emscripten; SDL `main` may adopt Emscripten features
  newer than 3.1.56, so a binding update can require waiting for a .NET workload update (the same pairing raylib has).
  `ppy.SDL3-CS` tracks SDL `main`, not tagged releases.
- **ScriptProcessorNode.** SDL's Emscripten audio uses the deprecated ScriptProcessorNode on the main thread (as raylib's
  build does here); P5 (AudioWorklet sink) stays relevant whatever backend the web uses.
- **No GPU tier on the web.** `SDL_Renderer` gives WebGL 1/2 2D only; SDL_GPU has no WebGL or WebGPU backend yet, so tier 1
  remains unavailable in browsers, exactly as with raylib.
- **Prototype shortcuts.** The tool's `--raylib-wasm` option was reused to pass `SDL3.a`; the host ignores page insets and
  visibility. A real switch needs an `SdlBrowserLauncher` (or the host side of the stage 1 split) and the browser tests.

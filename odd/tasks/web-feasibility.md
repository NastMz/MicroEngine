# WEB-0: WebAssembly feasibility

Objective: find out, with runnable evidence, whether a real MicroEngine demo can run in a browser through
WebAssembly while sharing its gameplay code with desktop. Branch `feat/web-feasibility` (no push, no merge).
Delivery: single local branch. Test runner: `dotnet test MicroEngine.slnx -c Release`.

## Specs

- S1 Investigate the current stack: startup, raylib coupling, contracts vs concrete code, rendering/textures/audio/
  input/loop, asset paths, `SaveStore`, version constraints, available WebAssembly tooling and real alternatives.
- S2 Run bounded experiments for C# on WebAssembly, graphics context, primitives and textures, keyboard, audio,
  a browser-compatible loop, resource loading and persistent local storage.
- S3 Run one existing demo in the browser: "La prueba debe intentar ejecutar el juego real, no una reproducción
  independiente desarrollada específicamente para navegador." Gameplay code stays shared with desktop.
- S4 Evolve engine abstractions only where an experiment proves the need; no speculative platform framework,
  no platform conditionals in gameplay, no loss of desktop capabilities, raylib stays on desktop.
- S5 Validate: graphics, input, loop, speed independent of browser FPS, audio under browser restrictions,
  scenes/transitions, equivalent gameplay, saves, achievements, and no desktop regression.
- S6 Classify the result (A/B/C/D) from the experiments and document limitations, reproducible build/run
  steps, and what porting the other three demos would take. No landing page, no deployment infrastructure.

## Tasks

| ID | Specs | Route | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 | S1, S2 | inline | done | `experiments/web/RaylibProbe` renders, reads held keys, rAF loop (L4) |
| T2 | S4 | inline | done | `GameLoop`; RED then GREEN, commit d88582c |
| T3 | S3, S4 | inline | done | Browser guards, `BrowserLauncher`, `web/NeonRift.Web`, content preload; commit 214c029 |
| T4 | S4, S5 | inline | done | `ISaveStorage` + `BrowserSaveStorage`; RED then GREEN, commit 1c9c94c |
| T5 | S5 | inline | done | L6-L8 |
| T6 | S6 | inline | done | `docs/evaluations/web-feasibility.md`, ADR 0007, `web/README.md` |

## Log

- L1 (2026-10-30) Original request: "MicroEngine — WEB-0: Investigación y prueba de viabilidad WebAssembly" (full
  brief in the session that opened this branch): answer whether MicroEngine can run a real game in a browser, which
  architecture changes that requires, and whether continuing to port all four demos is reasonable.
- L2 Tooling: machine SDKs have no `wasm-tools` workload and the shell is not elevated, so a user-local .NET SDK
  10.0.403 was installed with `dotnet-install.ps1` into `~/.dotnet-wasm/sdk-root` and `dotnet workload install
  wasm-tools` run there (emscripten 3.1.56).
- L3 Raylib-cs 8.1.0 ships `runtimes/browser-wasm/native/raylib.a` (raylib 6.0) and adds it as a
  `NativeFileReference`. Linking it fails in `wasm-opt`: "Unknown option '--enable-bulk-memory-opt'" — the archive
  was built by a newer emscripten. Rebuilding raylib 6.0 from source with the workload's emcc
  (`web/build-raylib-wasm.sh`) links cleanly.
- L4 Probe result in Chromium: WebGL 1 context, rectangles, circle and a texture drawn; held ArrowRight moved the
  sprite 181 px in ~1.5 s at 120 px/s; frames driven by `requestAnimationFrame` calling a `[JSExport]` method.
  Very short synthetic taps (down+up within one frame) are not seen by `IsKeyDown`, same as desktop polling.
- L5 raylib web constraints read from source: `WindowShouldClose()` calls `emscripten_sleep(12)` (needs Asyncify,
  which the .NET runtime does not use); `SetTargetFPS` makes `EndDrawing` wait in `WaitTime` (busy/sleep);
  `GetMonitorRefreshRate` returns 0. So the blocking `while` loop in `GameRunner.Run` cannot run in a browser and
  the backend must skip those calls there.
- L6 Browser validation (Chromium, Claude browser pane): Neon Rift title, stage 1, pause, achievements list,
  quit (`done: 4506 updates, 6090 frames`); 60 updates/s at 100/50/25 page fps, 41.7 at 8.3 fps (5-step cap);
  OGG music RMS 0.119; achievements persist in localStorage across reload (1/5, 2/100).
- L7 Desktop regression: 271/271 tests; `games/NeonRift/scripts/stage1.script` hidden raylib run produces 7/7
  captures byte-identical to `main`.
- L8 Smoke test (throwaway hosts, not committed): Meadow Run, Courtyard Key and Trail Duel start and reach
  gameplay unchanged; Meadow Run saved progress to localStorage.
- L9 Classification B (viable with architectural evolution). Next step: reproducible pinned web build, shared
  host template, browser polish, full per-game browser validation.

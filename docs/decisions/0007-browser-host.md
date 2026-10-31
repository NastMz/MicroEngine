# 0007 — Browser host: page-driven loop and pluggable save storage

Status: Accepted (2026-10-30). Proposed by WEB-0 (`feat/web-feasibility`); WEB-1 kept it for all four games and
added the shared host, gesture start and save failure semantics in [0008](0008-web-packages.md).

## Context

WEB-0 asked whether a real MicroEngine game can run in a browser through WebAssembly while sharing its
gameplay code with desktop. Experiments (see [the evaluation](../evaluations/web-feasibility.md)) showed that
raylib itself works there: Raylib-cs 8.1.0 targets `browser-wasm`, and raylib 6.0 rebuilt with .NET's
emscripten links into the .NET runtime and draws through WebGL. Three things did not carry over:

- **The loop.** `GameRunner.Run` blocks in a `while` loop. A page has one thread; blocking it freezes the
  page, and raylib's `WindowShouldClose()` on the web calls `emscripten_sleep`, which needs Asyncify, which the
  .NET runtime does not use. Frames must come from the browser (`requestAnimationFrame`).
- **Saves.** `SaveStore` writes files. In the browser they land in an in-memory file system and were shown to
  disappear on reload.
- **Assets.** `ContentFolder` reads files next to the executable; the browser has no such folder.

## Decision

- **`GameLoop` owns the frame; `GameRunner.Run` is a blocking driver of it.** `Start`, `Tick` (one frame:
  poll, fixed steps from elapsed time, render, captures) and `Finish` (shutdown once, report). Desktop and
  headless behaviour is unchanged; a browser host calls `Tick` from `requestAnimationFrame`.
- **`SaveStore` writes through `ISaveStorage`.** The store keeps JSON and corrupt-data handling;
  `FileSaveStorage` is the previous file behaviour; `BrowserSaveStorage` (raylib backend, browser only) uses
  `localStorage`, which is synchronous, so a save is durable as soon as `Save` returns. Games keep calling
  `new SaveStore(gameId, directory)`; a launcher picks the medium for stores without a directory through
  `SaveStore.DefaultStorage` before it constructs the game.
- **Assets stay file-based.** The host copies the game's `Content` files into the runtime's virtual file
  system at `/Content` before `Main`, so `ContentFolder` and the loaders are unchanged.
- **Browser specifics live in the backend and the host.** `RaylibPlatform` skips `WindowShouldClose` and the
  frame limiter when `OperatingSystem.IsBrowser()`; `BrowserLauncher` wires platform, storage and loop;
  `web/<Game>.Web` hosts reference the unchanged game project. No game code changed.

## Consequences

- One engine and one backend serve desktop and browser; the four games need no changes to start in a browser.
- `SaveStore.DefaultStorage` is process-wide state set by launchers; tests that change it must not run in
  parallel with tests that create stores without a directory.
- The browser build depends on the `wasm-tools` workload and on raylib rebuilt with its emscripten version
  (`web/build-raylib-wasm.sh`); a Raylib-cs or .NET update can break that pairing.
- Not decided here: asset streaming instead of preloading and trimming-safe JSON (source generators). The shared
  host for all games is [0008](0008-web-packages.md).

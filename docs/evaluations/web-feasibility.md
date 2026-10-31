# Evaluation: WEB-0, running MicroEngine in a browser through WebAssembly

**Question.** Can an existing MicroEngine demo run in a modern browser through WebAssembly, sharing the
gameplay code the desktop build uses, with reasonable changes to the engine's infrastructure?

**Answer.** Yes. Neon Rift runs and plays in Chromium from the unchanged game assembly, on the same raylib
backend compiled to WebAssembly. It took two structural but small engine changes (a frame-by-frame loop and a
storage seam under `SaveStore`), a few browser guards in the raylib backend, and a thin host per game.
**Classification: B — viable with architectural evolution.** The other three demos also start and reach
gameplay with the same host and no game changes (smoke test only). The main costs left are toolchain
fragility, download size and browser polish, not architecture.

Validated on Windows 11 in the Claude desktop app's built-in Chromium browser, 2026-10-30. Branch
`feat/web-feasibility`. Superseded for current status by the WEB-1 report, [web-support.md](web-support.md);
this page is the record of the experiment (its host and build steps were replaced). Build steps: [web/README.md](../../web/README.md). Decision: [ADR 0007](../decisions/0007-browser-host.md).

## Starting point

| Area | Finding | Kind |
| --- | --- | --- |
| Layers | Core (`src/MicroEngine`) never references raylib; everything platform-specific is behind `IPlatform` (`IRenderer`, `IGraphicsDevice`, `IAudioDevice`, `IInputSource`). Games reference `MicroEngine.Raylib` only for `DesktopLauncher` in `Program.cs`. | Engine design, favourable |
| Loop | `GameRunner.Run` blocks in `while (!CloseRequested && !QuitRequested)`, fixed 60 Hz steps from wall-clock time, max five steps per frame. | Engine design, blocking |
| Rendering, textures, input, audio | All raylib calls are in `RaylibPlatform.cs` (renderer, textures from `PixelImage`, keys/pad) and `RaylibAudioDevice.cs` (sounds from WAV bytes, music streamed from memory: WAV/OGG/MP3). | Contained |
| Assets | `ContentFolder` reads files under `AppContext.BaseDirectory/Content`; PNG and WAV are decoded in managed code. | File-based |
| Saves | `SaveStore` writes JSON files under local application data with temp-file + move; used by every game and by achievements. | File-based |
| Versions | .NET 10, Raylib-cs 8.1.0 (raylib 6.0). Raylib-cs 8.1.0 ships a `browser-wasm` `raylib.a` and adds it as a native reference. | Library |
| Tooling | No `wasm-tools` workload on the machine SDKs and no elevated shell to install it; a user-local SDK (10.0.403) took the workload (emscripten 3.1.56). | Environment |

Alternatives considered:

1. **raylib compiled to WebAssembly, linked into the .NET runtime** (chosen). Keeps `RaylibPlatform`,
   `RaylibAudioDevice` and all rendering semantics; risk was toolchain compatibility.
2. **A managed browser backend** (WebGL or Canvas 2D through `[JSImport]`, Web Audio for sound). Fully under
   our control but a second renderer, audio device and input source to write and keep identical; tinting,
   blend modes and scissor would need a batching WebGL renderer to perform. Not needed once 1 worked.
3. **Another C# runtime or engine stack** (Blazor UI, KNI/MonoGame-style frameworks): replaces the backend or
   the engine model; out of scope unless 1 and 2 failed.

## Experiments and evidence

| # | Experiment | Result |
| --- | --- | --- |
| E1 | Link Raylib-cs's own browser `raylib.a` into a .NET 10 wasm app (`experiments/web/RaylibProbe`) | **Failed** in `wasm-opt`: "Unknown option '--enable-bulk-memory-opt'". The archive was built with a newer emscripten than .NET's 3.1.56. Library limitation, not a platform one. |
| E2 | Rebuild raylib 6.0 from source with the workload's emcc (`web/build-raylib-wasm.sh`) and link that instead | Links. WebGL 1 (OpenGL ES 2.0) context created on the page canvas. |
| E3 | Draw rectangles, a circle and a texture; read a held key; drive frames from `requestAnimationFrame` calling a `[JSExport]` method | Works. Holding ArrowRight moved the sprite 181 px in about 1.5 s at 120 px/s. |
| E4 | Read raylib's web platform code for loop calls | `WindowShouldClose()` calls `emscripten_sleep(12)` (requires Asyncify; .NET does not use it). `SetTargetFPS` makes `EndDrawing` wait. **Platform limitation**: a blocking loop cannot run on the page thread. |
| E5 | Run Neon Rift with `SaveStore` unchanged | Achievements saved to `/MicroEngine/NeonRift/achievements.json` in the in-memory file system; **gone after a reload** (only MEMFS is linked). |
| E6 | Ship `Content/` through `WasmFilesToIncludeInFileSystem` | Ignored by `Microsoft.NET.Sdk.WebAssembly` (only the legacy workload app builder reads it). Replaced by serving files with a manifest and copying them into the file system from `main.js`. |
| E7 | Publish with default trimming | Fails: `SaveStore` uses reflection-based `System.Text.Json` (IL2026 as error). Worked around with `TrimMode=partial` and reflection enabled for hosts. |

## What changed

| Change | Where | Why (experiment) |
| --- | --- | --- |
| `GameLoop` (`Start`/`Tick`/`Finish`); `GameRunner.Run` drives it in a blocking loop | `src/MicroEngine/Core/GameRunner.cs` | E4 |
| `ISaveStorage` under `SaveStore`; `FileSaveStorage` keeps the desktop behaviour; `SaveStore.DefaultStorage` lets a launcher choose | `src/MicroEngine/Core/SaveStore.cs` | E5 |
| `BrowserSaveStorage` (`localStorage` through `[JSImport]`), `BrowserLauncher`, two `OperatingSystem.IsBrowser()` guards in `RaylibPlatform` | `src/MicroEngine.Raylib/` | E4, E5 |
| Host project, page glue (frame driver, storage imports, content preload, audio resume on keys) | `web/` | E2, E6, E7 |

Engine and backend: +286/−85 lines (most of it the loop body moved into `GameLoop`); tests: +117; host files
under `web/`: +172 (before this report and the README). No game file changed; games still construct `SaveStore` and read `ContentFolder` as before.

## Validation (Neon Rift)

| # | Check | Result | Evidence |
| --- | --- | --- | --- |
| 1 | Loads and shows its graphics | Pass | Title, stage 1 tiles and parallax, sprites, HUD, fonts all from the 86 CC0 files; letterboxing follows window resizes (wide and narrow pane) |
| 2 | Responds to input | Pass | Menus, run, jump, fire, pause via keyboard events. Gamepad not tested. |
| 3 | Game loop | Pass | Title → stage card → play → pause overlay → quit to title → Quit ends the loop: `done: 4506 updates, 6090 frames`, clean shutdown |
| 4 | Speed independent of browser FPS | Pass | Updates per second at 100 / 50 / 25 fps of page frames: 59.8 / 60.0 / 59.8. At 8.3 fps: 41.7 (the five-steps-per-frame cap, same as desktop). Unit test `Real_time_loop_steps_by_elapsed_time_not_by_frame_rate` pins it. |
| 5 | Audio under browser restrictions | Pass, partly verified | OGG title music streams: RMS 0.119 measured on the Web Audio output. Resume on key/pointer added because raylib's audio backend only resumes on click/touch; the test browser did not enforce autoplay blocking, so the blocked-then-resumed path was not observed. |
| 6 | Scenes and transitions | Pass | Fades between title, card and play; pause overlay; achievements list scene |
| 7 | Gameplay equivalent to desktop | Pass by construction, spot-checked | Same `NeonRift.dll`; same fixed step. Played the first seconds of stage 1 (score, enemies, banner). Not played to the end in the browser. |
| 8 | Save and restore | Pass | `localStorage["microengine/NeonRift/achievements"]` written; after reload the list shows 1/5 with Scrap Dealer at 2/100 |
| 9 | Achievements | Pass | "First Scrap" unlocked in play with its banner and sound cue |
| 10 | Desktop has no regression | Pass | 271/271 tests (85 engine, 30 Neon Rift, 27 Courtyard Key, 44 Meadow Run, 85 Trail Duel). `stage1.script` on raylib desktop: 7/7 captures byte-identical to `main`. |

Inputs were synthetic keyboard events (held for 100 ms or more), not a person playing; only Chromium was tested.

## Limitations and open problems

- **Toolchain.** Needs the `wasm-tools` workload (an elevated install, or a user-local SDK) and raylib rebuilt
  with that workload's emscripten. A .NET or Raylib-cs update can break the pairing; the build should be pinned
  and scripted before relying on it.
- **Download.** Neon Rift: 11.9 MB framework + 11.5 MB content uncompressed, 3.6 MB + 2.8 MB with Brotli
  (when the server sends the `.br` files). *Correction (WEB-1): the 2.8 MB counted only the files that have a `.br`
  copy (the WAV effects). The OGG music (7.4 MB) is already compressed and is sent as is, so Neon Rift's content
  is about 10.2 MB on the wire and the whole package was about 13.8 MB, not 6.4 MB. Current figures:
  [web-support.md](web-support.md).* Every content file is fetched before `Main`; there is no streaming
  or per-stage loading.
- **Trimming.** Reflection-based JSON forces partial trimming; source-generated `JsonSerializerContext` for save
  types would allow full trimming and smaller downloads.
- **Audio.** raylib's backend uses the deprecated `ScriptProcessorNode` (console warning) and is fed once per
  frame, so music stops while the tab is hidden (the game pauses too, since `requestAnimationFrame` stops).
- **Browser polish not done.** F11 is taken by the browser (Alt+Enter goes to raylib's fullscreen, untested);
  when the game quits the canvas keeps the last frame; no loading screen beyond a text counter; no touch input.
- **Not explained.** Twice, the first key presses sent right after a fresh page load were not seen; the same
  presses a moment later worked. Probably the presses landed before loading finished, but it was not
  diagnosed.
- **Untested.** Firefox, Safari, mobile browsers, gamepads, long sessions, storage quota errors.

## The other three demos

A throwaway host per game (same files, constructor and content folder changed; not committed) built and ran:
Meadow Run, Courtyard Key and Trail Duel each reached gameplay from the title with keyboard input, and Meadow
Run wrote `microengine/MeadowRun/progress` to `localStorage`. To port them properly:

1. One host project each (about 40 lines plus the shared page files), ideally generated from one shared host
   template instead of copies.
2. A full browser playthrough per game with the checks above, especially Trail Duel's save/continue flow and
   Meadow Run's checkpoints.
3. Shared fixes that help all four: pinned toolchain script, Brotli serving, a loading screen and audio unlock
   prompt, an end-of-game screen.

## Recommendation

Continue. The architecture held: the core needed only a loop inversion and a storage seam, both justified by
experiments and both neutral for desktop. Before a landing page integrates the games, make the build
reproducible (pinned SDK, workload and raylib build in one script or CI job), add the shared host template and
the browser polish listed above, then validate each game in Chromium and Firefox. Switching away from raylib
for the browser is not warranted by anything found here.

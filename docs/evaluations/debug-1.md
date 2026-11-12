# DEBUG-1: developer diagnostics and runtime inspection

Issue NastMz/MicroEngine#119. Record: [`odd/tasks/debug-1.md`](../../odd/tasks/debug-1.md). Date: 2026-11-12.
The question: can a developer outside this repository find out why their game misbehaves (a hitch, a body that falls
through the floor, a jump that never fires, a crash) with the published SDK, without reading the engine's source? The
issue asks for a gap assessment, a small diagnostic workflow, and only the primitives that the gaps justify, optional
and cheap when disabled.

## 1. What exists today

| Area | What a game can read | Where | Default |
| --- | --- | --- | --- |
| Frame timing | `FrameProfiler`: per-frame events, update, render, present, steps, allocated bytes, GC counts; p50–p99.9, hitches; JSON/CSV | `src/MicroEngine/Core/FrameProfiler.cs`, `RunOptions.Profiler`, `--profile FILE` | off; the unprofiled loop takes no timestamps |
| Graphics | `GraphicsCounters`: passes, draw calls, pipeline changes, texture bindings, uploads, live resources; cumulative, subtractable | `GraphicsDeviceBase.Counters` (`GraphicsDeviceBase.cs:220`) | always on, one count per submitted list |
| Platform | `IPlatformDiagnostics`: renderer, driver, VSync, GPU facts | `src/MicroEngine/Core/Platform.cs:64` | SDL only |
| Assets | `AssetScope.Loaded` (path, kind, bytes, load ms, during play), `AssetCache.Statistics`, a warning for a slow load during play | `AssetScope.cs:225`, `AssetCache.cs:297` | always on |
| Audio | `ActiveVoices`, `MaxVoices`, `RefusedSounds` | `AudioEngine.cs:217` | always on |
| Scenes | `SceneManager.Current/Stack/Count`; `SceneWorld.Count`, `Children`, `All<T>()`, `WorldMatrix` | `src/MicroEngine/Scenes/Scene.cs` | always on |
| Physics | `Bodies`, `KindOf`, `PositionOf`, `RayCast`, `Overlap`, `ContactEvents`, `SensorEvents` | `IPhysicsWorld2D.cs` | always on |
| Input | `Devices`, `Players`, `Layers`, the frame's raw `Events` | `InputSystem` | always on |
| Logging | `ILog` (`ConsoleLog`, stderr, Info and above), `Game.Log`, replaceable service | `src/MicroEngine.Kernel/Logging/Log.cs` | on |
| Errors | `RunResult.Error`; typed exceptions (`AssetLoadException`, `GraphicsCapabilityException`, `PhysicsCapabilityException`...) | `GameRunner.cs:82` | on |
| Determinism | `HashState`, `--state-trace` | `StateTrace.cs` | off |

`src/` uses no `EventSource`, `System.Diagnostics.Metrics` or `ActivitySource`, and no samples or guide pages use any
of the rows above except `--profile` and asset statistics (`docs/manual/lifecycle.md` "Measuring a game",
`docs/manual/assets.md`).

## 2. Scenarios an external developer meets

Each scenario is a question someone asks while making a game; the verdict says whether the published SDK answers it
today.

1. **"My game stutters every few seconds."** `--profile` records hitches, allocations and GC counts per frame, and
   the summary reports them. The developer learns *that* a frame was slow and that it allocated, but not *which* of
   their systems took the time: update and render are one number each, though `FrameSchedule` already has named
   systems. They cannot watch it live either: the profiler writes a file at exit and the game cannot read it.
   **Partial.**
2. **"Why is rendering slow on this phone?"** Draw calls and pipeline changes are counted, but on
   `GraphicsDeviceBase`, while `Game.Graphics` is an `IGraphicsDevice`; the developer has to know to cast. The
   profiler does not record them per frame. **Partial, hidden.**
3. **"My character falls through the floor / the crate does not touch the ramp."** No way to see shapes, contacts or
   sensors: no debug draw exists, a body's shapes cannot be listed, and a shape's geometry cannot be read back from
   `IPhysicsWorld2D`. Drawing them means keeping a copy of every `ShapeDescription` in game code. **Fails.**
4. **"My jump does not fire."** Raw events and device state are readable, but which input layer consumed the event,
   or which actions fired this frame, is not recorded anywhere. **Fails.**
5. **"Something is wrong and the console says nothing."** `ILog` exists, but the engine itself writes platform,
   audio and launcher messages straight to `Console.Error` (20 call sites), so a game that routes `ILog` to a file or
   an overlay misses them. **Partial.**
6. **"The game crashed on a tester's machine."** The launcher prints the exception with its stack trace on standard
   error and nothing else: no file, no versions or platform facts, no record of the frames before it. A tester who
   double-clicked the game has no console and nothing to send. **Fails.** (Corrected after the map: the map read the
   host-creation error line, which prints the message only; a game's run error already printed the stack.)
7. **"Show me an FPS counter / timings while I play."** No debug drawing that update code can call (draw a line now,
   at this world position, for this frame), and no ready numbers to put on screen. The developer can build it on
   `DrawAboveScenes`, `IRenderer` and `Game.Font`, given the numbers. **Fails for the numbers; the drawing is
   possible but every game rebuilds it.**
8. **"Is this texture still loaded? Which sounds are playing?"** Assets: answered (`AssetScope.Loaded`). Audio:
   counts only behind a cast, no list of playing sounds. **Mostly answered.**
9. **"Use the .NET tools I already know."** `dotnet-counters`, `dotnet-trace` and Visual Studio's diagnostics show
   the runtime's GC and CPU, but nothing from the engine: no frame time, draw calls or voices appear there.
   **Fails for engine data.**

## 3. Options weighed

- **Build a profiler UI or an inspector window.** Rejected: the issue asks for reusable infrastructure over prebuilt
  UI, and perf-1 already ruled out embedding Tracy or Superluminal. A desktop inspector would not run on the browser
  or mobile targets.
- **Publish engine numbers through `System.Diagnostics.Metrics`.** Chosen for the external-tool side: it is in the
  base library (no package), `dotnet-counters`, `dotnet-monitor` and OpenTelemetry read it, and an instrument nobody
  listens to costs one branch. It covers scenario 9 without the engine writing any tool.
- **One in-game diagnostics surface the game reads each frame.** Chosen for the in-game side (scenarios 1, 2, 7):
  the same numbers the meter publishes, as a struct a game can put on screen, log or assert in a test.

## 4. Proposed primitives

Each is optional and costs nothing measurable when unused; each comes with a test, an example in `samples/Examples`
and a manual page section. Contract changes are marked.

1. **`Game.Diagnostics` (frame statistics).** The last frame's update, render and present time, fixed steps,
   allocated bytes, GC counts per generation, the graphics counters for that frame (`GraphicsCounters` delta) and
   audio voices. Off by default; `Diagnostics.Enabled = true` (or `--diagnostics`) turns on the timestamps the
   profiler already takes. Scenarios 1, 2, 7, 8.
2. **Per-system timing.** When enabled, `FrameSchedule` times each named system; `Diagnostics.Systems` lists name,
   phase and time. Scenario 1.
3. **Engine meter.** A `Meter("MicroEngine")` with frame time, draw calls, allocations and voices as observable
   instruments, read only when a listener is attached. Scenario 9.
4. **`DebugDraw`.** An immediate queue update code can call (`Line`, `Rect`, `Circle`, `Text`, in world or screen
   space, with a duration), drawn after the scenes and cleared each frame; calls do nothing while it is disabled.
   Scenarios 3, 4, 7.
5. **Physics shape inspection.** `IPhysicsWorld2D` gains `ShapesOf(body)` and `DescriptionOf(shape)` as default
   interface members (a contract addition, not a break: an external backend that does not override them throws
   `PhysicsCapabilityException`), and `PhysicsDebugDraw.Draw(world, DebugDraw)` draws shapes, sensors and the step's
   contacts. Scenario 3.
6. **Input trace.** While diagnostics are enabled, `InputSystem` records, per frame, which layer handled each event
   and which actions were pressed; `Diagnostics.Input` reads it. Scenario 4.
7. **Engine messages through `ILog`.** The 20 `Console.Error` call sites go through the game's `ILog` (the default
   still writes stderr, so the console output keeps its text). Scenario 5.
8. **Crash report.** On an unhandled game error the launcher writes `crash-<time>.txt` beside the save folder: the
   exception with its stack, the platform diagnostics, the last log lines and the last frames' statistics; the
   console line names the file. Scenario 6.
9. **Counters without casts.** `IGraphicsDevice.Counters` (default interface member returning the base class's
   counters) and `AudioMixer.Statistics`. Scenarios 2, 8.

**Workflow demonstrated** (`samples/Examples`, a "Diagnostics" example and a guide page "Diagnosing a game"): press
F3 to show an overlay with frame time, per-system times, draw calls and allocations, physics shapes and the input
trace; run with `dotnet-counters monitor -n <game> --counters MicroEngine` to watch the same numbers outside the game;
crash the example on purpose and read the crash file.

**Not proposed:** listing every entity or attachment type, a camera registry, asset holder counts, a list of playing
sounds. Each is a single game-side query today or needs a design of its own (an entity inspector belongs with the
ENTITY-1 work); they stay out until a scenario needs them.

## 5. Delivery

About 1,500 changed lines in four work units: (a) items 1–3 and 9, (b) items 4–5, (c) items 6–8, (d) the example,
guide and release notes. Items 5 and 9 add interface members with defaults, item 7 changes where engine messages go
(the default text stays the same): these are listed under `## Next release` in the release notes.

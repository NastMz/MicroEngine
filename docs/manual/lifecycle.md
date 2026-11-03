# Game lifecycle

A MicroEngine game is a subclass of `Game` that declares its settings, builds its content once in `Load` and
pushes a first scene. A launcher pairs it with a platform and runs a fixed-step loop: input is sampled and the
simulation advanced in exact steps of `1 / UpdateRate` seconds (60 Hz by default), then the frame is rendered once. This page describes that contract;
the step-by-step tutorial is [Gem Garden](../tutorial/README.md).

## The `Game` class

| Member | Purpose |
| --- | --- |
| `Settings` (abstract) | `GameSettings`: title, virtual canvas size, window scale, update rate, background colour |
| `Load()` (abstract) | Runs once after the platform exists: build atlases, sounds, music, push the first scene |
| `Unload()` | Runs once before the platform is disposed: release textures you created |
| `CreateInput()` | Returns the `InputMap`; default is `InputMap.CreateDefault()` (see [Input](input.md)) |
| `Graphics`, `Audio`, `Input`, `Scenes`, `Font` | Services available from `Load` on |
| `Time`, `UpdateCount` | Simulated seconds and fixed updates since start |
| `Random` | Per-run `Random`, seeded by the launcher (`RunOptions.Seed`) |
| `Width`, `Height` | The virtual canvas size from `Settings` |
| `Quit()` / `QuitRequested` | Asks the loop to stop after the current frame |
| `UseAchievements(...)`, `Achievements` | Optional achievements (see [Achievements](achievements.md)) |

```csharp
public abstract GameSettings Settings { get; }
public IGraphicsDevice Graphics => Platform.Graphics;
public AudioMixer Audio { get; private set; } = null!;
public InputMap Input { get; private set; } = null!;
public SceneManager Scenes { get; private set; } = null!;
public BitmapFont Font { get; private set; } = null!;
// ...
/// <summary>Called once after the platform exists. Create atlases, sounds and push the first scene.</summary>
protected abstract void Load();
/// <summary>Called once before the platform is disposed.</summary>
protected virtual void Unload() { }
/// <summary>Override to change bindings; the default suits most games.</summary>
protected virtual InputMap CreateInput() => InputMap.CreateDefault();
```

Source: [`src/MicroEngine/Core/Game.cs`](../../src/MicroEngine/Core/Game.cs)

## `GameSettings`

```csharp
public sealed record GameSettings(string Title, int VirtualWidth = 320, int VirtualHeight = 180, int Scale = 4)
{
    /// <summary>Fixed simulation rate in updates per second.</summary>
    public int UpdateRate { get; init; } = 60;
    public Color Background { get; init; } = Color.Black;
}
```

Source: [`src/MicroEngine/Core/Platform.cs`](../../src/MicroEngine/Core/Platform.cs)

`VirtualWidth`/`VirtualHeight` define the pixel-art canvas every scene draws into ([Rendering](rendering.md));
`Scale` is only the initial window size on desktop. Neon Rift uses a larger canvas:

```csharp
public override GameSettings Settings { get; } = new("Neon Rift", 480, 270, 3) { Background = Color.Hex("0d0b1a") };
```

Source: [`games/NeonRift/RiftGame.cs`](../../games/NeonRift/RiftGame.cs)

Next SDK version (not published yet: arrives with the next SDK version): the canvas does not have to be pixel art.
`Scaling = CanvasScaling.Smooth` scales it to fill the window with linear sampling, for HD games drawn on a 1280×720 or
1920×1080 canvas, and `TextureFilter` sets the default sampling of textures
([HD canvases](rendering.md#hd-canvases-and-smooth-scaling)). A window that would not fit the display opens at the
largest size that does. `Presentation` states each of those choices separately (scale rounding, the canvas's
sampling, the textures' default sampler, capture scale, whole-factor windows), starting from the presets
`Presentation.PixelArt` and `Presentation.HighResolution2D`
([Presentation](rendering.md#presentation-one-choice-per-field)).

## Start-up order

`Game.Initialize` (called by the loop) sets services up in a fixed order, then runs `Load` and applies the scene
changes `Load` requested:

```csharp
internal void Initialize(IPlatform platform, int seed)
{
    _platform = platform;
    Random = new Random(seed);
    Input = CreateInput();
    Audio = new AudioMixer(platform.Audio);
    Font = BitmapFont.CreateDefault(platform.Graphics);
    Scenes = new SceneManager(this);
    Load();
    Scenes.ApplyPending();
}
```

Source: [`src/MicroEngine/Core/Game.cs`](../../src/MicroEngine/Core/Game.cs)

Build every texture, sound and song in `Load`, never per frame: textures need the platform's graphics device,
and synthesis is too slow to repeat each update.

## Loop, runner and session

| Type | Role |
| --- | --- |
| `GameLoop` | One frame per `Tick()`. Captures errors instead of throwing; `Finish()` shuts the game down once and returns a `RunResult` |
| `GameRunner.Run(game, platform, options)` | Ticks a `GameLoop` in a blocking loop; used by desktop and headless runs |
| `GameSession` | For hosts that drive frames themselves and outlive a run (a browser page): `Start`, `Tick`, `Stop`, `Result` |
| `RunOptions` | `Script`, `CaptureDirectory`, `MaxUpdates`, `Seed`, `Mute`; next SDK version: `TimeMode` |
| `RunResult` | `Updates`, `Frames`, `Captures`, `Error` |

```csharp
public static RunResult Run(Game game, IPlatform platform, RunOptions? options = null)
{
    var loop = new GameLoop(game, platform, options);
    if (loop.Start())
        while (loop.Tick()) { }
    return loop.Finish();
}
```

Source: [`src/MicroEngine/Core/GameRunner.cs`](../../src/MicroEngine/Core/GameRunner.cs)

`GameSession` builds a fresh game and platform on every `Start`, and when a run ends (quit, error or `Stop`)
it calls `Finish` and disposes the platform, so the host can show its own screen and start again. See
[Backends](backends.md) for how the desktop and browser launchers use these types.

## Modules and the frame schedule

Next SDK version (not published yet: arrives with the next SDK version). The engine is composed of modules on an
`EngineBuilder`, and `Game` is one of them: its built-in services (`Input`, `Audio`, `Scenes`, `Assets`, `Touch`,
`Font`...) are registered in a typed service registry and read from it, and its per-frame work runs as named systems
in the frame's phases. Override `Configure` to add your own systems, replace a built-in service or change the time
policy; `Program.cs` stays one line.

```csharp
public override void Configure(EngineBuilder builder)
{
    // Runs once per frame at the display's rate, after the frame's fixed steps.
    builder.Schedule.Add(FramePhase.Update, "mygame.camera", frame => _camera.Follow(_player, frame.Blend));
    // Runs once per fixed step, right after the scenes updated.
    builder.Schedule.Add(FramePhase.FixedUpdate, "mygame.physics", step => _world.Step((float)step.FixedDelta), after: "engine.scenes");
    builder.Configure<TimeSettings>(time => time with { FixedRate = 120 });
    builder.Lifecycle.QuitRequested += request => { if (_unsaved) request.Veto(); };
}
```

The phases run in this order; the four fixed phases run once per fixed step, the others once per frame:

| Phase | Built-in systems | Use it for |
| --- | --- | --- |
| `BeginFrame` | `engine.assets` (finishes background loads within the frame budget) | Bookkeeping before the frame |
| `Input` | `engine.touch` (the on-screen controls) | Input that follows the display rate |
| `PreFixed` | `engine.input` (samples devices into `Input`) | Work before each step's simulation |
| `FixedUpdate` | `engine.time` (`Time`, `UpdateCount`), `engine.scenes` (`Scene.Update`) | Game rules |
| `Physics` | — | Physics and collision |
| `PostFixed` | `engine.audio`, `engine.achievements`, `engine.input.end` (edge detection), `engine.assets.playing` | What follows the simulated state |
| `Update`, `LateUpdate` | — | Camera smoothing, UI animation, interpolation, at the display's rate |
| `Extract` | — | Copying what will be drawn |
| `Render` | `engine.render` (clear, scenes, achievement banners) | Drawing |
| `Present` | `engine.present` | Presenting |
| `EndFrame` | — | Last |

Within a phase, systems run in the order they were added; `after` and `before` name another system of the same phase.
A constraint that names a missing system or one in another phase, and a cycle (`a -> b -> c -> a`), stop the game
from starting with an error that names the systems. Each system receives a `FrameContext`: `Delta` (the seconds this
frame stands for), `FixedDelta`, `Blend`, `FixedStep` and `Frame`.

Services are found by the exact type they were registered as: `Services.Get<InputMap>()`, `builder.Services.Replace<ILog>(myLog)`.
`Game.TryGetCapability<T>` asks for optional features (the platform's touch screen, its diagnostics) without casting.
`Game.Lifecycle` raises `Suspending`, `Resumed` and `QuitRequested` (the window's close button; a handler may veto it,
and the game quits later with `Quit()`), `Resized` (the window's or canvas's new size in pixels) and `FocusChanged`, as
the platform delivers them at the start of a frame ([The platform host](backends.md#the-platform-host)). `Game.Log` writes `category: message` lines to the standard error unless a host registers another `ILog`.

## Time

Next SDK version (not published yet: arrives with the next SDK version). The loop owns time; a platform only supplies a
monotonic clock (`IPlatform.Clock`). `TimeSettings`, edited in `Configure`, holds every value:

| Setting | Default | Meaning |
| --- | --- | --- |
| `FixedRate` | `GameSettings.UpdateRate` | Fixed steps per second |
| `MaxStepsPerFrame` | 5 | Steps one frame may run; a frame that needs more keeps at most one step pending, so the game slows down instead of falling further behind |
| `MaxFrameDelta` | 0.25 s | The longest time one real-time frame stands for |
| `Mode` | the platform's `RunPreset`, else `RealTime` | `RealTime` or `Deterministic` |

In `RealTime` a frame adds the time the loop measured (paced to the display, at most `MaxFrameDelta`) and runs the
steps it calls for. In `Deterministic` every frame is exactly one step, whatever the wall clock says, and `FrameBlend`
is 1, so the same input gives the same steps and captures on any machine. The mode comes from, in order: the run
(`RunOptions.TimeMode`, the launcher's `--time realtime|deterministic`; a `--script` means deterministic unless `--time`
says otherwise), the game's `Configure`, then the preset the platform states for its runs (`RunPreset`: the headless
platform's is deterministic, SDL and raylib state none), then real time. A preset is a default, never a platform
behaviour, and a hidden window implies nothing about time. `Game.TimeSettings` is the policy the run uses.

## Fixed step and frame order

Each `Tick` runs one frame:

1. `platform.PollEvents()` (next SDK version: `platform.PumpEvents(...)`, whose events reach the game as
   `Game.Lifecycle` events; no frame runs while the platform is suspended).
2. Accumulate time. Real-time platforms add wall-clock time (capped at 0.25 s per frame); headless and
   fixed-step platforms add exactly one step. Next SDK version (not published yet: arrives with the next SDK version): when the platform reports the rate
   VSync paces it to (`IPlatform.DisplayRate`, SDL and desktop raylib windows), a frame counts as the whole number of
   refresh periods nearest to its measured time, the remainder carried to the next frame. Frames reach the loop up to
   a few milliseconds early or late around each refresh; counted as measured, that jitter made some frames run no
   step and the next run two when the display rate matches `UpdateRate`. `RunOptions.PaceToDisplay = false`
   (`--pacing measured`) keeps the measured time. With VSync off and a frame limit
   (`GraphicsSettings.FrameRateLimit`, `Game.Screen.FrameRateLimit`), the loop waits for the limit before each frame
   and counts time in its periods the same way; the limit never changes `UpdateRate`.
3. Run fixed steps of `1 / UpdateRate` seconds, **at most five per frame**. Each step applies the input script,
   samples devices into the `InputMap`, then `Game.Step` updates scenes, the audio mixer and achievement banners,
   flushes achievement progress about once a second, and advances input edge detection. Next SDK version: these are
   the systems of the fixed phases ([Modules and the frame schedule](#modules-and-the-frame-schedule)), and the limit is
   `TimeSettings.MaxStepsPerFrame` ([Time](#time)).
4. Render once: clear to `Settings.Background`, draw the scene stack and any transition, draw achievement banners.
5. If the script asked for a capture at this update, save a PNG of the virtual frame.

```csharp
while (_accumulator >= _step - 1e-9 && steps < 5)
{
    script?.Apply(_game.UpdateCount, _game.Input);
    _game.Input.Sample(_platform.Input);
    _game.Step((float)_step);
    _accumulator -= _step;
    steps++;
    // ...
}
if (steps == 5) _accumulator = Math.Min(_accumulator, _step);
```

Source: [`src/MicroEngine/Core/GameRunner.cs`](../../src/MicroEngine/Core/GameRunner.cs)

When the machine falls behind, the cap drops the backlog instead of spiralling, so the game slows down rather
than freezing. Because simulation only ever advances in whole steps, scripted runs on headless or fixed-step
platforms are deterministic.

### Drawing between updates

Next SDK version (not published yet: arrives with the next SDK version). Updates run at `UpdateRate` and frames at the display's rate. On a display whose
rate is not a multiple of the update rate (75, 100, 144 Hz for 60 updates) some frames have no new update, and a game
that draws its last update shows the same picture twice: moving things judder although every frame is on time.
`Game.FrameBlend` is how far the frame being drawn is between the last update and the next one, from 0 to 1. Keep
each moving thing's position from the previous update and draw it at `Vector2.Lerp(previous, current, FrameBlend)`
(the camera too). Motion is then drawn up to one update behind, as in every loop that interpolates. It is always 1
when time advances one step per frame (headless and scripted runs), so captures show exactly the last update; it
changes only what is drawn, never the rules. Scene transitions are drawn between updates by the engine;
`ParticleSystem.Draw(renderer, blend)` draws particles between their last two updates when given the blend. A scene
that did not update in the latest update (paused under an overlay, waiting during a transition) draws its last state:
compare `Game.UpdateCount` with the one it saw in its own `Update`, and use 1 when they differ, or it would rock between
its last two positions.

## Measuring a game

Next SDK version (not published yet: arrives with the next SDK version). `--profile FILE` measures every frame and the start, prints a summary and writes the
results (`FrameProfiler`):

- per frame: the time since the previous frame started, polling events, the updates, drawing, presenting (with VSync,
  mostly the wait for the display), the number of updates, game time, bytes allocated and garbage collections;
- start phases: creating the platform, the start screen, `Load`, the first frame;
- facts the platform reports (`IPlatformDiagnostics`): on SDL its version, renderer, video driver, VSync, display mode,
  window size, the GPU (Direct3D 11 and SDL's GPU renderer) and how long each step of its start took.

The summary gives mean, median (p50), p95, p99, p99.9 and maximum per measure, how many frames ran 0, 1, 2... updates,
hitches (frames longer than 1.5 times the median) and the motion error: per frame, how far the game time it showed
moved from the time it stood on screen, for the last update and for a game drawing with `FrameBlend`. The JSON keeps
every frame (CSV when the file ends in `.csv`) for comparing runs; compare only runs on the same machine, platform,
window size and scene. The engine sees when presenting returns, not when the display shows the frame: for that, use the
system's tools (PresentMon on Windows, Perfetto on Android, Instruments on Apple systems, the browser's performance
panel). A run without `--profile` measures nothing.

## Quitting and shutdown

A run ends when `Game.Quit()` was called, the platform reports `CloseRequested` (the desktop window was closed; next
SDK version: the platform delivers a quit request and no `Game.Lifecycle.QuitRequested` handler vetoes it),
`RunOptions.MaxUpdates` was reached, an input script's `quit` update arrived, or an exception escaped. In every
case `GameLoop.Finish` calls `Game.Shutdown`:

```csharp
internal void Shutdown()
{
    Scenes.Clear();
    _achievements?.Flush();
    Unload();
    Font.Dispose();
}
```

Source: [`src/MicroEngine/Core/Game.cs`](../../src/MicroEngine/Core/Game.cs)

`Scenes.Clear()` pops every scene (so each one's `Exit` runs), pending achievement progress is written, then
your `Unload` runs. Errors are reported in `RunResult.Error`; `DesktopLauncher.Run` turns them into exit code 1.

## Suspension

A phone sends an app to the background when the player switches away or takes a call; the platform then reports
`Suspended` ([Backends](backends.md#iplatform); next SDK version: it delivers `Suspending` and `Resumed` from its event
pump, always alternating). The loop reacts once on each change:

- **Going away**: music pauses, pending achievement progress is written, then `OnSuspended()` runs. No update and no
  frame happens while suspended.
- **Coming back**: music resumes, the clock restarts (the time away is not simulated), then `OnResumed()` runs before
  the next update.

```csharp
protected override void OnSuspended() => Scenes.Push(new PauseScene(this));   // for example
```

Both hooks are optional; on desktops and in browsers they never run. Save anything the player would hate to lose in
`OnSuspended`: a phone may end a background app without warning. Source:
[`src/MicroEngine/Core/GameRunner.cs`](../../src/MicroEngine/Core/GameRunner.cs)

## Testing hooks

`InitializeForTest(platform, seed)` and `StepForTest()` initialise a game and advance single steps without the
loop, which suits unit tests over a `HeadlessPlatform` (see [Backends](backends.md#headless-platform)).

## Related

- [Scenes](scenes.md): what runs inside `Scenes.Update` and `Scenes.Draw`
- [Architecture](../architecture.md): layers and module map

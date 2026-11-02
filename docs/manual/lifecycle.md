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
| `RunOptions` | `Script`, `CaptureDirectory`, `MaxUpdates`, `Seed`, `Mute` |
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

## Fixed step and frame order

Each `Tick` runs one frame:

1. `platform.PollEvents()`.
2. Accumulate time. Real-time platforms add wall-clock time (capped at 0.25 s per frame); headless and
   fixed-step platforms add exactly one step. Next SDK version (not published yet: arrives with the next SDK version): when the platform reports the rate
   VSync paces it to (`IPlatform.DisplayRate`, SDL and desktop raylib windows), a frame counts as the whole number of
   refresh periods nearest to its measured time, the remainder carried to the next frame. Frames reach the loop up to
   a few milliseconds early or late around each refresh; counted as measured, that jitter made some frames run no
   step and the next run two when the display rate matches `UpdateRate`. `RunOptions.PaceToDisplay = false`
   (`--pacing measured`) keeps the measured time.
3. Run fixed steps of `1 / UpdateRate` seconds, **at most five per frame**. Each step applies the input script,
   samples devices into the `InputMap`, then `Game.Step` updates scenes, the audio mixer and achievement banners,
   flushes achievement progress about once a second, and advances input edge detection.
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
changes only what is drawn, never the rules.

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

A run ends when `Game.Quit()` was called, the platform reports `CloseRequested` (the desktop window was closed),
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
`Suspended` ([Backends](backends.md#iplatform)). The loop reacts once on each change:

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

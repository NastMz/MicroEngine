# Backends

The engine core (`src/MicroEngine`) never references a windowing, graphics or audio library. Everything
platform-specific sits behind `IPlatform` and four small interfaces. Three implementations exist: the raylib
backend (`src/MicroEngine.Raylib`) for desktop windows, the same raylib backend compiled to WebAssembly for
browsers, and `HeadlessPlatform` in the core for tests and CI. A game is an ordinary .NET program and does not
change between them.

## `IPlatform`

```csharp
public interface IPlatform : IDisposable
{
    IGraphicsDevice Graphics { get; }
    IAudioDevice Audio { get; }
    IInputSource Input { get; }
    bool CloseRequested { get; }
    /// <summary>Monotonic time in seconds.</summary>
    double Now { get; }
    /// <summary>Whether time advances with the wall clock (desktop) or one step per frame (headless).</summary>
    bool RealTime { get; }
    void PollEvents();
    IRenderer BeginFrame();
    void EndFrame();
    /// <summary>Writes the most recently completed virtual frame as a PNG.</summary>
    void SaveScreenshot(string path);
    void ToggleFullscreen();
}
```

Source: [`src/MicroEngine/Core/Platform.cs`](../../src/MicroEngine/Core/Platform.cs)

| Interface | Responsibility | raylib | Headless |
| --- | --- | --- | --- |
| `IRenderer` | Immediate-mode drawing into the virtual canvas ([Rendering](rendering.md#irenderer)) | `RaylibRenderer` | `HeadlessRenderer` (counts draw calls, checks camera balance) |
| `IGraphicsDevice` | `CreateTexture(PixelImage)` | `RaylibGraphicsDevice` | `HeadlessGraphicsDevice` |
| `IAudioDevice` | Load and play PCM sounds, looping and streamed music ([Audio](audio.md)) | `RaylibAudioDevice` | `SilentAudioDevice` |
| `IInputSource` | Raw key, pad button and left-stick state ([Input](input.md)) | `RaylibInput` | `HeadlessInput` |

The loop calls `PollEvents`, `BeginFrame`, `EndFrame` and `SaveScreenshot` in the order described in
[Lifecycle](lifecycle.md#fixed-step-and-frame-order).

## Desktop: raylib

`RaylibPlatform` opens a resizable window of `VirtualWidth × Scale` by `VirtualHeight × Scale`, draws into a
render texture of the virtual size and scales it by whole factors with letterboxing. Esc is not raylib's exit
key (games use it for pause); F11 and Alt+Enter toggle borderless fullscreen. Audio falls back to silence when no
device is available. The backend uses [Raylib-cs](https://github.com/ChrisDill/Raylib-cs).

Games start through `DesktopLauncher.Run`, which parses arguments, picks a platform and returns an exit code:

```csharp
var saveDir = Environment.GetEnvironmentVariable("RIFT_SAVE_DIR");
return DesktopLauncher.Run(new RiftGame(LaunchOptions.FromEnvironment(), saveDirectory: string.IsNullOrWhiteSpace(saveDir) ? null : saveDir), args);
```

Source: [`games/NeonRift/Program.cs`](../../games/NeonRift/Program.cs)

| Argument | Effect |
| --- | --- |
| `--script FILE` | Play an [input script](input.md#scripted-input); implies a fixed step |
| `--captures DIR` | Where script captures go (default `./captures`); saved at 3× scale |
| `--headless` | Run on `HeadlessPlatform`, no window (captures are recorded but not written) |
| `--hidden` | Render in a hidden window (captures still work); implies a fixed step |
| `--frames N` | Stop after N updates |
| `--seed N`, `--scale N`, `--mute` | Random seed, window scale, silence |

Exit codes: 0 success, 1 game error, 2 bad arguments. Unknown arguments are rejected, which is why the games read
developer shortcuts from environment variables. Source:
[`src/MicroEngine.Raylib/DesktopLauncher.cs`](../../src/MicroEngine.Raylib/DesktopLauncher.cs)

Validated on Windows. Linux and macOS are potentially compatible thanks to .NET and raylib, but not verified and
not officially supported.

## Browser: WebAssembly

The same raylib backend is compiled to WebAssembly and linked into the .NET browser runtime. A page cannot block
its only thread in a game loop, so the browser launcher returns a `GameSession` that the page drives:

```csharp
public static GameSession CreateSession(Func<Game> createGame)
{
    ArgumentNullException.ThrowIfNull(createGame);
    SaveStore.DefaultStorage = gameId => new BrowserSaveStorage(gameId);
    return new GameSession(createGame, game => new RaylibPlatform(game.Settings, new DesktopOptions()));
}
```

Source: [`src/MicroEngine.Raylib/BrowserLauncher.cs`](../../src/MicroEngine.Raylib/BrowserLauncher.cs)

The shared web host exports `Start`, `Frame`, `Outcome` and `Stop` to the page; `main.js` calls `Frame` once per
`requestAnimationFrame`:

```csharp
private static readonly GameSession Session = BrowserLauncher.CreateSession(GameEntry.Create);
// ...
/// <summary>Runs one frame; false once the run has ended (then see <see cref="Outcome"/>).</summary>
[JSExport]
public static bool Frame() => Session.Tick();
```

Source: [`web/Host/Program.cs`](../../web/Host/Program.cs)

Browser-specific behaviour inside `RaylibPlatform`: the window never reports `CloseRequested`, raylib's frame
limiter is off (the page paces frames), F11 belongs to the browser and the page owns fullscreen, and the canvas
may be smaller than the virtual size. Saves go to `localStorage` ([Persistence](persistence.md)). Embedding a
package and the page's messages are described in [Embedding](../examples/embedding.md); producing browser packages
is not available to external developers yet.

The browser build targets desktop browsers with a keyboard or gamepad. The four demos are verified with automated
tests in Chrome and Firefox on desktop, plus manual validation; there are no dedicated touch controls, and phones
and tablets are not validated ([Platforms](../guide/platforms.md)).

## Headless platform

`HeadlessPlatform(width, height)` has deterministic time (one step per frame), no window, silent audio and
keyboard state you set from code. `HeadlessPlatform.For(settings)` sizes it from a game. Tests use it two ways.

Stepping a game directly:

```csharp
var game = new MyGame();
var platform = new HeadlessPlatform();
game.InitializeForTest(platform);
platform.Keys.Set(Key.Right, true);
for (int i = 0; i < 60; i++) game.StepForTest();
```

Source: [Making a game](../making-a-game.md#9-testing-and-captures)

Running the real loop for a fixed number of updates:

```csharp
var game = new MeadowGame(TempDir(), new LaunchOptions(start, Level: 4));
var platform = new HeadlessPlatform();
var result = GameRunner.Run(game, platform, new RunOptions { MaxUpdates = 240 });
Assert.IsNull(result.Error, $"{start}: {result.Error}");
```

Source: [`tests/MeadowRun.Tests/PlaythroughTests.cs`](../../tests/MeadowRun.Tests/PlaythroughTests.cs)

Useful probes: `platform.Renderer.DrawCalls`, `platform.SilentAudio.Played`, `platform.Screenshots` (capture
paths requested), `HeadlessGraphicsDevice.TexturesCreated`, and `CloseRequested` (settable).

## Writing a new backend

A backend for another library (SDL, MonoGame, a native console SDK) would:

1. Implement `IRenderer` over the library's 2D drawing, honouring `DrawOptions` (flip, rotation, origin, scale),
   blend modes, clipping and the camera offset stack, and drawing into a virtual-size target.
2. Implement `IGraphicsDevice.CreateTexture` from `PixelImage.ToRgba()` (point filtering keeps pixels crisp).
3. Implement `IAudioDevice`, becoming silent rather than failing when no hardware is present.
4. Map the library's key and pad codes onto `Key` and `PadButton` in an `IInputSource`.
5. Implement `IPlatform` (time, events, frame begin/end, screenshots, fullscreen) and a launcher that calls
   `GameRunner.Run` (blocking hosts) or drives a `GameSession` (hosts that own the frame loop).

No game code changes. Only the raylib backend exists today; the headless renderer is the smallest reference for
the contract. See [Architecture](../architecture.md) and [ADR 0003](../decisions/0003-scenes-entities-runtime.md).

## Limitations

- One real backend (raylib); no Android, iOS or console targets.
- The engine is not distributed to external developers yet; NuGet packages are planned
  ([Getting MicroEngine](../guide/getting-microengine.md)).

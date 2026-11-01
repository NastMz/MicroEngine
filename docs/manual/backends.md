# Backends

The engine core (`src/MicroEngine`) never references a windowing, graphics or audio library. Everything
platform-specific sits behind `IPlatform` and four small interfaces. The official implementations:

| Platform | Project | Used for |
| --- | --- | --- |
| SDL 3 | `src/MicroEngine.Sdl` | The default on Windows, Linux, macOS, Android and iOS |
| raylib | `src/MicroEngine.Raylib` | Browsers (WebAssembly), and an alternative desktop platform |
| Headless | `src/MicroEngine` (`HeadlessPlatform`) | Automated tests |

A game is an ordinary .NET program and does not change between them. Which platform runs is the host's choice
(a command-line argument, an environment variable, the phone app), never the game's.

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
    /// <summary>Whether the system took the game away (a phone app sent to the background) ...</summary>
    bool Suspended => false;
    void PollEvents();
    IRenderer BeginFrame();
    void EndFrame();
    /// <summary>Writes the most recently completed virtual frame as a PNG.</summary>
    void SaveScreenshot(string path);
    void ToggleFullscreen();
}
```

Source: [`src/MicroEngine/Core/Platform.cs`](../../src/MicroEngine/Core/Platform.cs)

| Interface | Responsibility | SDL | raylib | Headless |
| --- | --- | --- | --- | --- |
| `IRenderer` | Immediate-mode drawing into the virtual canvas ([Rendering](rendering.md#irenderer)) | `SdlRenderer` | `RaylibRenderer` | `HeadlessRenderer` |
| `IGraphicsDevice` | `CreateTexture(PixelImage)` | `SdlGraphicsDevice` | `RaylibGraphicsDevice` | `HeadlessGraphicsDevice` |
| `IAudioDevice` | Sounds, looping and streamed music ([Audio](audio.md)) | `SdlAudioDevice` | `RaylibAudioDevice` | `SilentAudioDevice` |
| `IInputSource` | Keys, pad buttons and axes, gamepad connection, mouse ([Input](input.md)) | `SdlInput` | `RaylibInput` | `HeadlessInput` |
| `ITouchScreen` (`IPlatform.Touch`, optional) | Touches, safe area, density, vibration, the overlay drawn over the frame ([Touch controls](input.md#touch-controls)) | `SdlTouchScreen` | `RaylibTouchScreen` (fed by the browser page) | `HeadlessTouchScreen` |

While `Suspended` is true the loop neither simulates nor renders, pauses music, saves achievement progress and calls
the game's `OnSuspended`/`OnResumed`. See [Lifecycle](lifecycle.md#fixed-step-and-frame-order) for the frame order.

## Choosing a platform

Desktop games start through `MicroEngine.Desktop.DesktopLauncher`:

```csharp
var saveDir = Environment.GetEnvironmentVariable("RIFT_SAVE_DIR");
return DesktopLauncher.Run(new RiftGame(LaunchOptions.FromEnvironment(), saveDirectory: string.IsNullOrWhiteSpace(saveDir) ? null : saveDir), args);
```

Source: [`games/NeonRift/Program.cs`](../../games/NeonRift/Program.cs)

Its catalog offers, in order of preference: `sdl`, `raylib`, `sdl-software` and `headless`. The first that can run
is the default; `--platform NAME` or the `MICROENGINE_PLATFORM` variable picks another, and `--list-platforms` prints
them:

```text
$ MeadowRun --list-platforms
sdl: SDL 3: window, 2D renderer (Direct3D, Metal, OpenGL, Vulkan or software), input, touch and audio
raylib: raylib 6.0: GLFW window, OpenGL renderer, input and miniaudio sound
sdl-software: SDL 3 with its software renderer
headless: no window, devices or sound; one step per frame, for automated tests
```

| Argument | Effect |
| --- | --- |
| `--platform NAME` | Run on that platform of the catalog |
| `--list-platforms` | Print the catalog, with the reason a platform cannot run here, and exit |
| `--script FILE` | Play an [input script](input.md#scripted-input); implies a fixed step |
| `--captures DIR` | Where script captures go (default `./captures`); saved at 3× scale |
| `--headless` | The same as `--platform headless` (captures are recorded but not written) |
| `--hidden` | Render in a hidden window (captures still work); implies a fixed step |
| `--frames N` | Stop after N updates |
| `--seed N`, `--scale N`, `--mute` | Random seed, window scale, silence |

Exit codes: 0 success, 1 game or platform error (an unknown or unavailable platform, no display), 2 bad arguments.
Unknown arguments are rejected, which is why the games read developer shortcuts from environment variables. Sources:
[`src/MicroEngine/Core/GameLauncher.cs`](../../src/MicroEngine/Core/GameLauncher.cs),
[`src/MicroEngine.Desktop/DesktopLauncher.cs`](../../src/MicroEngine.Desktop/DesktopLauncher.cs).
`MicroEngine.Raylib.DesktopLauncher` is the same launcher with only `raylib` and `headless`.

## Replacing a part

A profile says which parts a host may supply itself. The official profiles allow the **audio device** and **extra
input sources**; the window, renderer, graphics device and frame presentation share one graphics context, so they are
replaced together by choosing another profile.

```csharp
// raylib's window and renderer with SDL's audio device instead of raylib's.
return DesktopLauncher.Run(game, args, DesktopLauncher.CreateCatalog(),
    request => request with { Audio = SdlAudioDevice.TryCreate });

// An extra input source (a network peer, an accessibility switch) read together with the keyboard and gamepad.
return DesktopLauncher.Run(game, args, DesktopLauncher.CreateCatalog(),
    request => request with { ExtraInput = [myInput] });
```

When audio is supplied the backend does not open its own device, and the platform disposes the replacement. A request
the profile cannot honour fails with `PlatformConfigurationException` (for example "Platform 'browser' does not allow
replacing its audio device."). Source: [`src/MicroEngine/Core/PlatformProfiles.cs`](../../src/MicroEngine/Core/PlatformProfiles.cs)

To offer a platform of your own, wrap it in a profile and add it to the catalog:

```csharp
var catalog = DesktopLauncher.CreateCatalog()
    .Add(new PlatformProfile("console", "my console SDK", request => new ConsolePlatform(request))
    {
        CheckSupport = () => ConsoleSdk.IsAvailable ? null : "the console SDK is not installed",
        Replaceable = PlatformParts.Input,
    });
return DesktopLauncher.Run(game, args, catalog);   // --platform console
```

## SDL 3

`SdlPlatform` opens a resizable window of `VirtualWidth × Scale` by `VirtualHeight × Scale` (full screen on phones),
draws through SDL's renderer into a texture of the virtual size and scales it by whole factors with letterboxing,
inside the window's safe area (`FrameLayout`).
SDL chooses the graphics API for the system; `SdlPlatform.CreateProfile("vulkan")` (or `direct3d11`, `direct3d12`,
`metal`, `opengl`, `opengles2`, `software`) fixes one, and `SdlPlatform.RenderDrivers` lists what this build offers.
Keyboard, mouse, the first gamepad (the next one takes over when it is unplugged) and touch are read; F11 and
Alt+Enter toggle fullscreen on desktops; losing focus releases keys and fingers. Audio uses SDL audio streams: wav and
ogg music, silence when no device exists. `SdlTouchScreen` reports finger events, `SDL_GetWindowSafeArea` and the
display scale, vibrates the device on Android (SDL's haptic API) and draws the game's on-screen controls over the frame
in window pixels (never into captures; [Touch controls](input.md#touch-controls)).
`MICROENGINE_SDL_EVENT_LOGGING=1` makes SDL log the events it delivers. The package is
[ppy.SDL3-CS](https://github.com/ppy/SDL3-CS) with SDL 3.5. If the system cannot load SDL (on Linux, a glibc older
than 2.34), the launcher ends with `error: platform 'sdl' could not load its native library` and the loader's reason,
exit code 1. A desktop game run from a macOS application bundle reads its content from `Contents/Resources/Content`
(`DesktopLauncher.BundleContentRoot`), so a signed bundle keeps only code in `Contents/MacOS`.

## raylib and browsers

`RaylibPlatform` behaves like the SDL platform on desktops (the `raylib` profile). raylib cannot run on iOS, and on
macOS it needs a GPU with accelerated OpenGL. Its main role is the browser: the same backend compiled to WebAssembly
and linked into the .NET browser runtime. A page cannot block its only thread in a game loop, so the browser launcher
returns a `GameSession` that the page drives:

```csharp
public static GameSession CreateSession(Func<Game> createGame)
{
    ArgumentNullException.ThrowIfNull(createGame);
    SaveStore.DefaultStorage = gameId => new BrowserSaveStorage(gameId);
    return new GameSession(createGame, game => new RaylibPlatform(game.Settings, new DesktopOptions()));
}
```

Source: [`src/MicroEngine.Raylib/BrowserLauncher.cs`](../../src/MicroEngine.Raylib/BrowserLauncher.cs)

The shared web host calls `Frame` once per `requestAnimationFrame`. In a browser the window never reports
`CloseRequested`, the page paces frames and owns fullscreen, and saves go to `localStorage`
([Persistence](persistence.md)). Embedding a package is described in [Embedding](../examples/embedding.md).

## Phones

`microengine package android` and `ios` build a host app around the game: it runs on the SDL platform with touch
controls, is suspended in the background and ships the game's `Content` files (unpacked from the APK on Android,
bundle resources on iOS, both through `ContentFolder.DefaultRoot`). See [Platforms](../guide/platforms.md#android).

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

More in [Testing your game](../examples/testing.md).

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

A backend for another library would:

1. Implement `IRenderer` over the library's 2D drawing, honouring `DrawOptions` (flip, rotation, origin, scale),
   blend modes, clipping and the camera offset stack, and drawing into a virtual-size target.
2. Implement `IGraphicsDevice.CreateTexture` from `PixelImage.ToRgba()` (point filtering keeps pixels crisp).
3. Implement `IAudioDevice`, becoming silent rather than failing when no hardware is present, and opening nothing
   when the request supplies a device (`PlatformRequest.OpensOwnAudio`).
4. Map the library's key and pad codes onto `Key` and `PadButton` in an `IInputSource`.
5. Implement `IPlatform` (time, events, frame begin/end, screenshots, fullscreen, and `Suspended` if the system
   can take the app away) and expose it as a `PlatformProfile`.

`tests/MicroEngine.Desktop.Tests` shows how to check a backend against the others: one probe frame exercising every
renderer operation, compared pixel by pixel.

## Limitations

- The phone hosts are built by `microengine package`; a game does not write its own Android activity or iOS app.
- What each platform needs and supports: [Platforms](../guide/platforms.md).

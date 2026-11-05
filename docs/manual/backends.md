# Backends

The engine core (`src/MicroEngine`) never references a windowing, graphics or audio library. Everything
platform-specific sits behind `IPlatform` and four small interfaces. The official implementations:

| Platform | Project | Used for |
| --- | --- | --- |
| SDL 3 | `src/MicroEngine.Sdl` | The default on Windows, Linux, macOS, Android and iOS |
| raylib | `src/MicroEngine.Raylib` | Browsers (WebAssembly), and an alternative desktop platform; next SDK version: the alternative desktop platform only (browsers run SDL) |
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
    /// <summary>The time mode a run uses when it does not choose one: real time (desktop) or one step per frame (headless).</summary>
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
| `IInputBackend` (next SDK version, a capability) | Device events: keys, text, mouse, wheel, every gamepad with hot-plug; with `IKeyLabels`, `IGamepadRumble`, `ITextInputControl` where the backend has them ([Devices, players and layers](input.md#devices-players-and-layers)) | `SdlInput` | `RaylibInput` | `HeadlessInputBackend` |
| `ITouchScreen` (`IPlatform.Touch`, optional; next SDK version: a capability, `TryGetCapability<ITouchScreen>`) | Touches, safe area, density, vibration, the overlay drawn over the frame ([Touch controls](input.md#touch-controls)) | `SdlTouchScreen` | `RaylibTouchScreen` (fed by the browser page) | `HeadlessTouchScreen` |

While `Suspended` is true the loop neither simulates nor renders, pauses music, saves achievement progress and calls
the game's `OnSuspended`/`OnResumed`. See [Lifecycle](lifecycle.md#fixed-step-and-frame-order) for the frame order.

Next SDK version (not published yet: arrives with the next SDK version): the loop owns time. `Now` is only a monotonic
clock on every backend; how many steps a frame runs, the catch-up limits and the blend are the loop's
([Time](lifecycle.md#time)). Nothing a platform does depends on the time mode. A platform may state a default for the
runs that choose none, a `RunPreset`: the headless platform's is one step per frame (`RunPreset.Testing`), SDL and
raylib state none (real time).

### The platform host

Not published yet: arrives with the next SDK version. The platform contracts move to `MicroEngine.Platform` (an
assembly the `MicroEngine` package carries), and `IPlatform` extends the host contract:

```csharp
public interface IPlatformHost : IDisposable
{
    IMonotonicClock Clock { get; }                 // seconds, never derived from frames
    bool PumpEvents(IEngineEventSink sink);        // once per frame; false while suspended
    ISurface? MainSurface { get; }                 // the window or canvas: pixel size, density, minimised
    bool TryGetCapability<T>(out T? capability) where T : class;
    bool CanBlock { get; }                         // false for a page-driven host (a browser)
}

public interface IPlatform : IPlatformHost
{
    IGraphicsDevice Graphics { get; }
    IAudioDevice Audio { get; }
    IInputSource Input { get; }
    IRenderer BeginFrame();
    void EndFrame();
    void SaveScreenshot(string path);
    void ToggleFullscreen();
    Color Letterbox { get; set; }
}
```

The rules every platform follows:

- **Events come only from the pump**, in the order they happened, on the game's thread, once at the start of each
  frame. The game sees them as `Game.Lifecycle` events before the frame's updates: `Resized` (the surface's new size in
  pixels; the surface's own `Resized` event fires in the same pump), `FocusChanged`, `Suspending`, `Resumed` and
  `QuitRequested` (which a handler may veto).
- **Suspension alternates**: `Suspending` then `Resumed`, never two of one in a row. A trip to the background and back
  between two frames arrives as both, in that order. No frame runs while the platform is suspended.
- **One quit request per request**: a window's close button is asked once; a later click is asked again.
- **Optional features are capabilities**, found with `TryGetCapability<T>`, never by casting the platform:
  `IDisplayControl` (displays, VSync, window mode), `IPlatformDiagnostics`, `ITouchScreen` (touches, safe area,
  vibration), `IDisplayPacing` (the refresh rate frames are paced to) and `IPreciseDelay` (a precise wait). A platform's
  capabilities do not change while it runs. `Game.TryGetCapability<T>` finds them too.
- **Page-driven platforms** (raylib in a browser) report `CanBlock = false` and never block: no waits or sleeps; the
  page paces the frames, and the loop skips a frame that comes early instead of waiting. They read the surface size
  from the canvas after start, never from the size requested at creation, and open audio inside the click that starts
  the game.
- **No time coupling**: what a platform does never depends on how the loop advances time. Display pacing
  (`PlatformRequest.WaitForDisplay`), when gamepads start (`StartGamepadsAfterFirstFrame`) and sound
  (`AudioOutput`, `Mute`) are explicit options of the request.

| Platform | Events it delivers | Capabilities | `CanBlock` |
| --- | --- | --- | --- |
| SDL | quit, focus, resize, suspension (phones) | `IDisplayControl`, `IPlatformDiagnostics`, `IDisplayPacing`, `IPreciseDelay`, `ITouchScreen` | yes |
| raylib | quit (desktop), focus, resize, suspension (a hidden browser page) | `IDisplayControl`, `IDisplayPacing`, `ITouchScreen`, `IPreciseDelay` (desktop) | desktop yes, browser no |
| Headless | what a test asks for (`Resize`, `Focus`, `Suspend`, `Resume`, `RequestQuit`) | `ITouchScreen` | yes |

No platform raises the surface's `Lost` event yet.

Storage and content files have contracts of their own in the same assembly: `IStorage` (the slots `SaveStore` writes;
`ISaveStorage` extends it) and `IContentSource` (`Exists`, `Open`, `Files`), with `FolderContentSource` for a folder on
disk (`ContentFolder` is one).

## Choosing a platform

Desktop games start through `MicroEngine.Desktop.DesktopLauncher`:

```csharp
return DesktopLauncher.Run(new MainGame(), args);
```

Next SDK version (not published yet: arrives with the next SDK version): the launcher takes the game's `Configure`
entry point instead, the composition every platform runs ([One composition for every
platform](#one-composition-for-every-platform)):

```csharp
return DesktopLauncher.Run(Startup.Configure, args);
```

Source: [`games/NeonRift/Program.cs`](../../games/NeonRift/Program.cs)

Its catalog offers, in order of preference: `sdl`, `raylib`, `sdl-software` and `headless`. The first that can run
is the default; `--platform NAME` or the `MICROENGINE_PLATFORM` variable picks another, and `--list-platforms` prints
them. Next SDK version (not published yet: arrives with the next SDK version): when nobody names a platform and the default cannot start (no usable
graphics, a library the system cannot load), the launcher tries the next platform that can run here, except
`headless`, and prints `platform: sdl could not start (...); trying raylib`:

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
| `--script FILE` | Play an [input script](input.md#scripted-input); implies a fixed step (next SDK version: deterministic time unless `--time` says otherwise) |
| `--captures DIR` | Where script captures go (default `./captures`); saved at 3× scale |
| `--headless` | The same as `--platform headless` (captures are recorded but not written); runs one step per frame unless `--time` says otherwise |
| `--hidden` | Render in a hidden window (captures still work); implies a fixed step (next SDK version: implies nothing about time or sound; add `--time deterministic` or `--audio silent`) |
| `--time realtime\|deterministic` | Next SDK version: how the loop advances time, whatever the window: with the wall clock, or one step per frame ([Time](lifecycle.md#time)) |
| `--frames N` | Stop after N updates |
| `--seed N`, `--scale N`, `--mute` | Random seed, window scale, silence |

Exit codes: 0 success, 1 game or platform error (an unknown or unavailable platform, no display), 2 bad arguments.
Unknown arguments are rejected, which is why the games read developer shortcuts from environment variables. Sources:
[`src/MicroEngine/Core/GameLauncher.cs`](../../src/MicroEngine/Core/GameLauncher.cs),
[`src/MicroEngine.Desktop/DesktopLauncher.cs`](../../src/MicroEngine.Desktop/DesktopLauncher.cs).
`MicroEngine.Raylib.DesktopLauncher` is the same launcher with only `raylib` and `headless`.

## One composition for every platform

Next SDK version (not published yet: arrives with the next SDK version). A game is composed once, by a `Configure`
entry point, and every host runs that composition: the desktop `Program.cs`, and the browser, Android and iOS hosts
that `microengine package` builds. The entry point is a public static method that takes a `HostBuilder`
(namespace `MicroEngine.Hosting`):

```csharp
public sealed class MainGame : Game
{
    public static void Configure(HostBuilder host)
    {
        host.UseGame(() => new MainGame());
        // Optional: everything below reaches web and phone builds too.
        host.Platforms().Add(myProfile);                                     // offered with --platform
        host.ConfigureRequest(request => request with { ExtraInput = [myInput] });
        host.ConfigureEngine(engine => engine.Services.Add(new MyService()));
    }
    ...
}
```

```csharp
return DesktopLauncher.Run(MainGame.Configure, args);   // Program.cs
```

`microengine` finds the method in the built game (one public static `Configure(HostBuilder)` on a public type) and
the hosts it generates call it, so the project file needs no property for it. Each host fills its defaults before the
game's `Configure` runs, which may change them:

| Part | Desktop | Browser | Android | iOS |
| --- | --- | --- | --- | --- |
| `host.Platforms()` | `sdl`, `raylib`, `sdl-software`, `headless` | `raylib` (the page's; next SDK version: `sdl`, the page's) | `sdl` | `sdl` |
| `host.Content` | `Content` next to the program; a macOS bundle's `Contents/Resources/Content` | `Content` next to the program | the folder the app unpacks its assets to | the bundle's `Content` |
| Saves, while `host.Storage` is unset (`SaveStore.DefaultStorage`) | files (`SaveStore.DefaultDirectory`) | `localStorage` | the app's files folder | `Library/Application Support` |

Add to `host.Platforms()` rather than replacing it, so every host keeps its own platform. `host.Kind` says which host is
composing, `host.Request` what the launch asked for, and `host.Environment.Variable(name)` reads a developer variable
(an environment variable on desktop, the page's or the launch's elsewhere). `host.SaveStore(gameId)` creates a store on
the composition's medium. Nothing here is process-wide: two hosts in one process (an editor, tests running in
parallel) keep their own content folder and storage.

The launch's rules are the same on every host (`LaunchRequest`, `GameHost`): a run with a script is in deterministic
time with seed 1 unless the launch says otherwise, asks the platform not to wait for the display and for no sound
device unless the launch asks for one (phones always do); a real-time run whose game shows a start screen starts
gamepads after the first frame. Tests compose the same way:

```csharp
var host = GameHost.Create(new HostEnvironment(HostKind.Test), new LaunchRequest { Frames = 120 }, MainGame.Configure,
    h => h.UsePlatforms(new PlatformCatalog().Add(HeadlessPlatform.Profile)));
RunResult result = host.Play();
```

Content sources other than folders are refused for now, with their reason.

## Graphics options

Next SDK version (not published yet: arrives with the next SDK version). `GameSettings.Graphics` (`GraphicsSettings`) is where a game chooses how it is shown. Every option
starts automatic:

| Option | Default | Changes while running |
| --- | --- | --- |
| `Backend` | none: the catalog in order | no |
| `Renderers` | none: the platform chooses | no |
| `VSync` | on | yes, `Game.Screen.VSync` |
| `FrameRateLimit` | 0 (no limit) | yes, `Game.Screen.FrameRateLimit` |
| `Gpu` | `GpuPreference.Automatic` | no |
| `WindowMode` | `WindowMode.Windowed` | yes, `Game.Screen.WindowMode` |
| `Display` | none: the system chooses | yes, `Game.Screen.MoveToDisplay` |

- `Backend` names the platform to try first (`sdl`, `raylib`, `sdl-software`); a platform that cannot start falls
  back to the next one and the reason is printed. `--platform` and `MICROENGINE_PLATFORM` name one that must start.
- `Renderers` lists graphics APIs in order of preference for platforms that offer several (SDL). Names this system lacks
  are skipped; after the list, the platform chooses as usual.
- VSync, the frame limit and `UpdateRate` are independent. VSync waits for the display's refresh; the frame limit caps
  frames per second (with VSync off it alone paces the frames, and the loop counts time in its periods as it does in
  refresh periods with VSync); `UpdateRate` is the simulation's rate and never changes with either. With VSync on, a
  limit below the display's rate skips refreshes: choose one that divides the rate (50 on a 100 Hz display), or draw
  with `Game.FrameBlend`.
- `WindowMode.Fullscreen` is a borderless window covering its display: switching is instant, and on Windows it is
  presented directly to the display (one refresh less latency than a window). `WindowMode.ExclusiveFullscreen` asks
  for the display's own mode where the backend supports it; it is not faster than `Fullscreen` with SDL on Windows.
  F11 and Alt+Enter switch between the window and `Fullscreen` (next SDK version: through the engine's `fullscreen`
  action, which a game can rebind or turn off; see [Input](input.md#full-screen)).
- `Display` is an index of `Game.Screen.Displays`. A window opened on a display (or full screen) is shown once it is
  there.

`Game.Screen` (`GameScreen`) reports the displays (name, mode, refresh rate, scale, primary), the one the game is on,
the refresh rate frames are paced to, the graphics API and GPU in use (`Renderer`, `Adapter`), and changes the options
marked above. Changes apply where the platform allows (`CanChange`): SDL and raylib windows on desktop; browsers and
phones keep their own window, and a headless run has none. Read a property back to see what applied. A graphics menu
saves `Game.Screen.Current` (the start-only options as the game started, the others as they are now) with its other
settings, and returns it from `Settings` at the next start (for example
`Graphics = Store.Load<GraphicsSettings>("graphics") ?? new GraphicsSettings()` in the `GameSettings` it builds).

The launcher's `--renderer`, `--vsync`, `--fps-limit`, `--gpu`, `--window` and `--display` replace one option for one
run ([Configuration](../reference/configuration.md)).

Next SDK version (not published yet): `GraphicsSettings.Device` (the launcher's `--graphics`) chooses the graphics
device on platforms that offer more than one. The SDL platform offers `sdl-renderer`, its default (SDL's 2D renderer,
tier 0), and `sdl-gpu` (SDL_GPU on Direct3D 12, Vulkan or Metal, tier 1: custom shaders, depth and
[the 3D path](rendering.md#the-3d-path)); a profile lists what it offers in `PlatformProfile.GraphicsDevices`. A game
that needs tier 1 sets `Graphics = new GraphicsSettings { Device = "sdl-gpu" }` in its `GameSettings`. A platform that
does not offer the named device refuses to start (the launcher then tries the next one and says why), and `sdl-gpu`
where SDL_GPU cannot start stops with the reason: a game never runs on another device than the one it asked for. On
`sdl-gpu`, VSync is chosen when the window opens (`SetVSync` cannot change it) and on-screen touch controls are not
drawn.

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

Next SDK version (not published yet: arrives with the next SDK version): the supplied parts are built into the backend
itself, never wrapped around it, so a replaced audio device or an extra input source keeps every capability of the
platform (display control, pacing, diagnostics, touch). A backend builds its audio device with
`request.CreateAudio(openOwn)` and its input with `request.CombineInput(own)`; a backend that ignores the parts its
profile allows is refused with `PlatformConfigurationException`.

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
Next SDK version (not published yet: arrives with the next SDK version): `GraphicsSettings.Gpu = GpuPreference.PowerSaving` (`--gpu power-saving`) asks for the
low-power GPU of a machine with two; SDL can choose only for its Metal and GPU renderers, while Direct3D 11 and OpenGL
use the GPU the system assigns to the application (on Windows: Settings, Display, Graphics). `Game.Screen.Adapter` and
`--profile` report the GPU in use, and `--profile` the display the window is on. On Windows a window can be shown at
another display's rate than its own (the desktop composes windows on slower displays, and windows moved between
displays, at the fastest display's rate); the loop counts frame time in refresh periods only while frames really
arrive at the display's rate, and in the periods they do arrive at otherwise, so motion stays even on every display.
Full screen always runs at its display's rate, and is the way to show a game at a secondary display's own rate. On Windows, SDL reads gamepads through Microsoft's GameInput
(its default since SDL 3.4), whose start can take about a second on machines where the GameInput service is not
running; a game with a start screen starts gamepad support once that screen is shown, so the window appears first.
Pads already connected are reported as usual.
Keyboard, mouse, the first gamepad (the next one takes over when it is unplugged; next SDK version: every gamepad, as
a device of its own, with text input and rumble) and touch are read; F11 and
Alt+Enter toggle fullscreen on desktops (next SDK version: the engine's `fullscreen` action does, not the backend); losing focus releases keys and fingers. Audio uses SDL audio streams: wav and
ogg music, silence when no device exists. `SdlTouchScreen` reports finger events, `SDL_GetWindowSafeArea` and the
display scale, vibrates the device on Android (SDL's haptic API) and draws the game's on-screen controls over the frame
in window pixels (never into captures; [Touch controls](input.md#touch-controls)).
`MICROENGINE_SDL_EVENT_LOGGING=1` makes SDL log the events it delivers. The package is
[ppy.SDL3-CS](https://github.com/ppy/SDL3-CS) with SDL 3.5. If the system cannot load SDL (on Linux, a glibc older
than 2.34), the launcher ends with `error: platform 'sdl' could not load its native library` and the loader's reason,
exit code 1. A desktop game run from a macOS application bundle reads its content from `Contents/Resources/Content`
(`DesktopLauncher.BundleContentRoot`), so a signed bundle keeps only code in `Contents/MacOS`.

Not published yet: arrives with the next SDK version. `SdlRendererDevice` implements the
[graphics device contract](rendering.md#the-graphics-device) on SDL's renderer, with SDL's default driver or the one
`GraphicsDeviceOptions.Api` names; it declares render targets, repeat addressing and (on every GPU driver) custom blend
states. SDL's `software` driver is a diagnostic device: its triangle edges differ from the contract's pixel-centre rule,
so it does not claim conformance. The platform places the canvas with the game's `Presentation` and samples it with
`Presentation.Upscale` ([Presentation](rendering.md#presentation-one-choice-per-field)).

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

Next SDK version (not published yet: arrives with the next SDK version): the browser launcher composes every run with
the game's `Configure` entry point and the same launcher rules as desktop and phones, on the raylib platform of one
page (`BrowserPage`, which receives the page's touches, visibility and vibration); a scripted run in a development
package is deterministic with seed 1, so two runs write byte-identical captures:

```csharp
private static readonly BrowserPage Page = new();
private static readonly GameSession Session = BrowserLauncher.CreateSession(GameEntry.Configure, Page);
// on "play": Session.Start(new LaunchRequest { ScriptText = script, CaptureDirectory = "/captures" });
```

Next SDK version (not published yet): browsers run the SDL platform instead (`MicroEngine.Sdl.BrowserLauncher`, `MicroEngine.Sdl.BrowserPage`,
`SdlPlatform.BrowserProfile`), with the same session and page model; `MicroEngine.Raylib` no longer has a browser
launcher, page or profile. In a page the SDL platform reports `CanBlock = false` and never waits (no VSync, no
`IPreciseDelay`, no wait while suspended), takes its size from the canvas, is suspended while the page is hidden, reads
touches, safe-area insets, density and vibration from the page, plays sound through the page's AudioWorklet and leaves
fullscreen to the page; keyboard, mouse and gamepads are SDL's own.

```csharp
private static readonly BrowserPage Page = new();   // MicroEngine.Sdl
private static readonly GameSession Session = BrowserLauncher.CreateSession(GameEntry.Configure, Page);
```

Source: [`src/MicroEngine.Sdl/BrowserLauncher.cs`](../../src/MicroEngine.Sdl/BrowserLauncher.cs)

Not published yet: arrives with the next SDK version. `RaylibDevice` implements the
[graphics device contract](rendering.md#the-graphics-device) on raylib's OpenGL layer, on raylib's own window only (raylib
has one window per process). On desktops it declares render targets, repeat and mirror addressing, separate filters
and custom blend states; in browsers only render targets. Like SDL, the platform places and samples the canvas as the
game's `Presentation` says.

The shared web host calls `Frame` once per `requestAnimationFrame`. In a browser the window never reports
`CloseRequested`, the page paces frames and owns fullscreen, and saves go to `localStorage`
([Persistence](persistence.md)). Embedding a package is described in [Embedding](../examples/embedding.md).

## Phones

`microengine package android` and `ios` build a host app around the game: it runs on the SDL platform with touch
controls, is suspended in the background and ships the game's `Content` files (unpacked from the APK on Android,
bundle resources on iOS, both through `ContentFolder.DefaultRoot`). Next SDK version (not published yet: arrives with
the next SDK version): the phone apps run the game's `Configure` entry point with the same launcher rules as desktop,
and hand their content folder to the composition (`host.Content`) instead of a process-wide setting; saves stay in the
same folders. See [Platforms](../guide/platforms.md#android).

## Headless platform

`HeadlessPlatform(width, height)` has deterministic time (one step per frame), no window, silent audio and
keyboard state you set from code. `HeadlessPlatform.For(settings)` sizes it from a game and sets its `UpdateRate`
from the game's, so its time equals `Game.Time`. Tests use it two ways.

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

Next SDK version (not published yet: arrives with the next SDK version): `HeadlessPlatform.For(settings, rasterize:
true)` (or `Rasterize = true` when creating it) also draws every frame on the CPU, following the rendering rules
([Rendering](rendering.md#irenderer)), so a test can check pixels without a window. `platform.Renderer.Frame` is the
current frame as an image, `platform.Renderer.Commands` lists the frame's draws as text (one line each, in screen
pixels, such as `rect 2,2 3x3 #FFFFFFFF`), and script captures are written as PNG files at scale 1.

Next SDK version (not published yet: arrives with the next SDK version): one step per frame is the headless
platform's stated preset (`HeadlessPlatform.Preset`, `RunPreset.Testing`), so the tests above stay reproducible with
no option. A run's `RunOptions.TimeMode` or the game's `Configure` win over it, and
`new HeadlessPlatform { Preset = new RunPreset() }` makes runs that choose nothing real-time. Its events are driven by hand and delivered at the next frame: `platform.Resize(w, h)`, `Focus(bool)`, `Suspend()`,
`Resume()` and `RequestQuit()`.

## Writing a new backend

A backend for another library would:

1. Implement `IRenderer` over the library's 2D drawing, honouring `DrawOptions` (flip, rotation, origin, scale),
   blend modes, clipping and the camera offset stack, and drawing into a virtual-size target.
2. Implement `IGraphicsDevice.CreateTexture` from `PixelImage.ToRgba()` (point filtering keeps pixels crisp).
3. Implement `IAudioDevice`, becoming silent rather than failing when no hardware is present, and opening nothing
   when the request supplies a device (`PlatformRequest.OpensOwnAudio`).
4. Map the library's key and pad codes onto `Key` and `PadButton` in an `IInputSource`.
5. Implement `IPlatform` (time, events, frame begin/end, screenshots, fullscreen, and `Suspended` if the system
   can take the app away) and expose it as a `PlatformProfile`. Next SDK version: implement the host contract
   instead ([The platform host](#the-platform-host)): a monotonic clock, an event pump that follows
   its rules, the main surface, `CanBlock`, and the optional features as capabilities.

Next SDK version (not published yet: arrives with the next SDK version): `PlatformConformance` in the same project
states the platform contract as tests (a monotonic clock, events delivered once and in order, suspension alternating,
no frame while suspended, resize and focus events, the capability query, a precise wait only where the platform can
block); the headless, SDL and raylib platforms run it by subclassing it and saying how a test makes them resize, lose
focus, go to the background or be asked to quit. An event a platform cannot be made to send in a test is reported as
skipped, never passed.

`tests/MicroEngine.Desktop.Tests` shows how to check a backend: a conformance suite (`RendererConformance`) states each
rendering rule as a test, and every backend (the headless CPU reference, each SDL driver, raylib) runs the same tests
by subclassing it. Next SDK version (not published yet: arrives with the next SDK version): a renderer calls
`Texture.CheckDrawable<TTexture>(texture, device)` before drawing a texture, so disposed and foreign textures fail as
on every other backend, and its textures pass their device to the `Texture` constructor.

Not published yet: arrives with the next SDK version. A graphics device derives from `GraphicsDeviceBase`
([The graphics device](rendering.md#the-graphics-device)) and implements only its `*Core` members; the base validates
every call first. `tests/Shared/Graphics/DeviceConformance.cs` states the device contract as tests (exact coverage,
placement, readback and ordering; blending within 8-bit quantization; filtering within the kernel's bounds; the typed
error for every undeclared feature), and a device runs it by subclassing it and saying how to create the device and
its surface, as the CPU reference device does in `tests/MicroEngine.Graphics.Tests`.

## Limitations

- The phone hosts are built by `microengine package`; a game does not write its own Android activity or iOS app.
- What each platform needs and supports: [Platforms](../guide/platforms.md).

# Platforms

A MicroEngine game is one .NET project that runs on Windows, Linux, macOS, Android, iOS and in web browsers. The
game's code is the same everywhere: it talks to `IPlatform`, and a platform underneath does the work. SDL 3 is the
platform on desktops, phones and browsers (compiled to WebAssembly there), raylib is an alternative platform on
desktops, and a headless platform runs games in tests ([Backends and platforms](../manual/backends.md)). The [Platforms page](/platforms/) summarises them;
[Packaging a game](packaging.md) explains how each package is built.

## At a glance

| Target | Runs on | Build with | Players need |
| --- | --- | --- | --- |
| Windows | `sdl` (default), `raylib`, `sdl-software` | `microengine package windows`, on Windows | Windows 10 or 11, 64-bit |
| Linux | `sdl` (default), `raylib`, `sdl-software` | `microengine package linux`, on any system | x86_64, glibc 2.34+, X11 or Wayland with OpenGL |
| macOS | `sdl` with Metal (default); `raylib` where OpenGL is available | `microengine package macos`, on a Mac | macOS 14+, Apple silicon |
| Android | `sdl` with touch controls | `microengine package android` | Android 7.0+, 64-bit |
| iOS and iPadOS (preview) | `sdl` with Metal and touch controls | `microengine package ios`, on a Mac with Xcode | iOS 15+ |
| Web browsers | `sdl` in WebAssembly | `microengine package web` | WebAssembly and WebGL (Chrome, Edge, Firefox, Safari) |

iOS is a preview: games build and run, and device builds need the developer's own Apple signing
([Signing](signing.md#ios)).

## Desktop: Windows, Linux, macOS

`DesktopLauncher.Run(MainGame.Configure, args)` takes the game's `Configure` entry point and opens a window on the first platform of its catalog that can run on the machine:
SDL 3. Players and tests can choose another with `--platform raylib`, `--platform sdl-software` (SDL's CPU renderer,
for machines without a working GPU driver) or `MICROENGINE_PLATFORM`; `--list-platforms` lists them. The window
scales the game's virtual canvas by the largest whole factor that fits and letterboxes it; F11 and Alt+Enter toggle
fullscreen through the engine's `fullscreen` action, which a game can rebind or turn off
([Full screen](../manual/input.md#full-screen)). The window and taskbar show the game's [icon](app-identity.md). The
browser, Android and iOS builds run the same composition with the same launch rules
([One composition for every platform](../manual/backends.md#one-composition-for-every-platform)).

- **Rendering**: SDL picks Direct3D 11, Vulkan or OpenGL on Windows, OpenGL on Linux and Metal on macOS; all draw the
  same pixels. raylib uses OpenGL, so on a Mac without OpenGL it cannot open a window and SDL is used.
- **Input**: keyboard, mouse and one gamepad at a time ([Input](../manual/input.md)).
- **Saves**: `%LOCALAPPDATA%/MicroEngine/<GameId>` on Windows, `$XDG_DATA_HOME/MicroEngine/<GameId>` (by default
  `~/.local/share`) on Linux, `~/Library/Application Support/MicroEngine/<GameId>` on macOS
  ([Persistence](../manual/persistence.md#where-saves-live)).
- **Native libraries** come with the NuGet packages: SDL 3.5 (`ppy.SDL3-CS`) and raylib 6.0 (`Raylib-cs`). Packages
  are built for x64 on Windows and Linux and for arm64 on macOS.
- **Linux**: needs glibc 2.34 or later for SDL and 2.38 for raylib; an older system gets a clear error naming the
  missing glibc version.

## Android

The Android host runs the game on SDL 3 inside SDL's own activity: full screen, the game's orientation
([`MicroEngineOrientation`](app-identity.md)), on-screen touch controls, keyboards and gamepads. The game is
suspended when the player leaves it (music pauses, `OnSuspended` can save) and resumes when they return
([Lifecycle](../manual/lifecycle.md)). Content files travel in the APK and are read through `ContentFolder` as on
desktop; saves go to the app's private storage. Build with `microengine package android`, which needs the .NET
`android` workload, an Android SDK and Java 17+; signing uses your own key ([Signing](signing.md#android)).

## iOS and iPadOS (preview)

The iOS host hands the app to SDL, which runs the game with Metal, touch controls and the same lifecycle as Android.
`microengine package ios-simulator` builds an app for Xcode's Simulator; `microengine package ios` builds an `.ipa`
for devices, signed with your Apple certificate and provisioning profile. Both need a Mac with Xcode and the .NET
`ios` workload. A Simulator build never installs on a device.

## Web browsers

`microengine package web` builds a static site: upload the folder to any web host. The game runs on SDL 3 compiled
to WebAssembly, the same platform as on desktops and phones, in desktop and mobile browsers, with keyboard, gamepads
and on-screen touch controls. raylib is a desktop platform only, so a game that references only `MicroEngine.Sdl` can
be packaged for the web.

- **Starting**: the page waits for a click, tap or key before starting, because browsers allow sound only after one;
  that first input is not passed to the game.
- **Display**: the canvas scales to the page; the page's own button and Alt+Enter toggle fullscreen (F11 stays the
  browser's).
- **Saves**: `localStorage`, under `microengine/<GameId>/`, kept across reloads and separate for each game.
- **Lifecycle**: a hidden tab pauses the game and its sound.
- **Size**: the download is the .NET runtime, the game and its content, compressed with Brotli on servers that
  support it; a small code-first game is about 2 MB.

How the page behaves and how to embed a game in another page: [The browser host](../examples/embedding.md).

## Headless

`HeadlessPlatform` runs a game without a window or audio device: one simulation step per frame, a renderer that
counts draw calls, a silent audio device that records what was played, and input that a test sets directly. Use it
for automated tests of your game ([Testing](../examples/testing.md)); `--headless` selects it from the command line.

## Your own platform

A new platform implements `IPlatform` and its four device interfaces (`IRenderer`, `IGraphicsDevice`,
`IAudioDevice`, `IInputSource`) and is registered as a profile in a catalog; no game code changes
([Backends and platforms](../manual/backends.md), [Contracts](../reference/contracts.md#iplatform)).

## Trimmed and NativeAOT publishing

A game project can also be published with .NET's own `PublishTrimmed` or `PublishAot` properties. Those builds have
no reflection-based JSON serialization, so a game that saves declares its saved types once and lets the compiler
write how to read and write them:

<!-- code: samples/Examples/SaveTypes.cs#types -->
```csharp
/// <summary>
/// The game's saved types, declared once. The compiler writes how to read and write each of them and the types they
/// hold, so saving needs no reflection: what a game published trimmed or with NativeAOT requires.
/// </summary>
[JsonSerializable(typeof(SaveExample.Progress))]
[JsonSerializable(typeof(Profile))]
[JsonSerializable(typeof(JsonObject))]   // a slot read as JSON, to upgrade old saves
public partial class SaveTypes : JsonSerializerContext;
```

and passes that metadata when it saves and loads:

<!-- code: samples/Examples/SaveTypes.cs#use -->
```csharp
public static SaveExample.Progress Load(SaveStore store) =>
    store.Load("example", SaveTypes.Default.Progress) ?? new SaveExample.Progress();   // null: no save yet, or unreadable

public static bool Save(SaveStore store, SaveExample.Progress progress) =>
    store.Save("example", progress, SaveTypes.Default.Progress);                        // false: the medium refused
```

The slot is the same JSON either way: a save written with `Save(slot, data)` loads with
`Load(slot, SaveTypes.Default.Progress)` and the reverse, so a game can adopt the declared types without losing its
players' saves ([Saving data](../manual/persistence.md)). What to expect in these builds:

- Saving and loading with declared types works, including nested objects, collections, enums, derived types declared
  with `JsonDerivedType`, and `JsonObject` for upgrading old saves.
- `Save(slot, data)` and `Load<T>(slot)` without metadata throw `InvalidOperationException` when the slot is written
  or read. The build reports each such call in the game's code: warning `IL2026` once the project sets
  `PublishTrimmed`, `IsTrimmable` or `EnableTrimAnalyzer`, and also `IL3050` once it sets `PublishAot`,
  `IsAotCompatible` or `EnableAotAnalyzer`.
- Achievements persist as in any other build: `AchievementTracker` reads and writes its slot without reflection, and
  the game declares nothing for it.
- NativeAOT is not one of the targets above: `microengine package` does not build it, and the SDK makes no promise
  about a game compiled that way beyond what this section states.

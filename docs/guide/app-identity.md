# Application identity

A game says once, in its project file, how players see it: its name, id, version, icon and orientation (the text in
the window's title bar stays `GameSettings.Title`, set in code). Every
package `microengine package` builds takes these from there, and the engine uses them at run time (window and taskbar
icon, the name desktops group windows by, phone orientation). Nothing else holds them.

```xml
<PropertyGroup>
  <ApplicationTitle>Star Courier</ApplicationTitle>          <!-- the name players see -->
  <ApplicationId>com.yourstudio.starcourier</ApplicationId>  <!-- unique, never changes after release -->
  <Version>1.2.0</Version>
  <Description>Deliver parcels across a pixel-art galaxy.</Description>
  <MicroEngineIcon>icon.png</MicroEngineIcon>                 <!-- square PNG: 256, 512 or 1024 pixels -->
  <MicroEngineOrientation>landscape</MicroEngineOrientation>  <!-- landscape, portrait or any (phones) -->
  <MicroEngineGameFactory>new StarCourier.MainGame()</MicroEngineGameFactory>
</PropertyGroup>
```

| Property | Default | Used for |
| --- | --- | --- |
| `ApplicationTitle` | the assembly name | Windows product name and file description, Linux desktop entry, macOS bundle name and Dock, Android launcher, iOS home screen, browser tab and page title |
| `ApplicationId` | none | Android package, Apple bundle id, Linux desktop entry and app id; required for `macos`, `android`, `ios` |
| `Version` | 1.0.0 | every package's version and file name; Android `versionName`, Apple `CFBundleShortVersionString` (the numeric part) |
| `Description` | empty | README.txt, the Linux desktop entry |
| `MicroEngineIcon` | none (system default) | Windows executable and window, Linux window and desktop entry, macOS `AppIcon.icns`, Android launcher, iOS app icon, browser favicon |
| `MicroEngineOrientation` | `landscape` | Android activity, iOS supported orientations, SDL on phones |
| `MicroEngineGameFactory` | `new <RootNamespace>.MainGame()` | the C# expression that creates the game in hosts that build it themselves (browser, Android, iOS) |
| `MicroEngineLicenseFile` | `LICENSE`, `LICENSE.txt` or `LICENSE.md` next to the project | shipped as `LICENSE.txt` |
| `MicroEngineThirdPartyNotices` | MicroEngine's own list | shipped as `THIRD-PARTY-NOTICES.txt`; list your own assets' licences here |
| `MicroEngineScriptsDir` | none | input scripts packaged in phone and development web builds, for automated play tests |
| `MicroEngineAndroidMinApi` | 24 (Android 7.0) | the oldest Android version the APK installs on |

## Rules the build checks

- **Application id**: two or more dot-separated parts, each starting with a letter, made of letters, digits and
  underscores (`com.example.my_game`). Android and Apple reject hyphens. Use a domain you control; stores and phones
  identify the game by it, so it must never change once players have it.
- **Icon**: a PNG, square, exactly 256, 512 or 1024 pixels (1024 is best: iOS asks for it, and pixel art is
  enlarged without blur when smaller). The tool writes each system's format from it: Windows `.ico`, macOS `.icns`,
  Android launcher icon, iOS asset catalog, browser favicon.
- **Orientation**: `landscape`, `portrait` or `any`.

A wrong value stops `dotnet build` with an error naming it (`ME0001` id, `ME0002` orientation, `ME0003` missing
icon, `ME0004` not a PNG, `ME0005` wrong size). `microengine info` shows what the project declares.

## At run time

`AppIdentity.Of(game)` reads the identity the build wrote into the game's assembly. The launcher passes it to the
platform (`PlatformRequest.Identity`): SDL sets the application name, version and id (`SDL_SetAppMetadata`, which
Wayland and X11 use to group windows) and the window icon; raylib sets the window icon; phone hosts follow the
orientation. A game built without the targets has no identity, and platforms keep their defaults.

## Where it comes from

The targets are `build/MicroEngine.targets` in the `MicroEngine` package; every game that references any MicroEngine
package imports them.

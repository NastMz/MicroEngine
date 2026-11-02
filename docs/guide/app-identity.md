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
| `MicroEngineSplash` | `true` | the [start screen](#start-screen); `false` opens on a plain black screen |
| `MicroEngineSplashColor` | the colour along the edge of the start screen's image | the start screen's background, `#RRGGBB` |
| `MicroEngineSplashImage` | the icon | a PNG shown in the middle of the start screen (any shape) |
| `MicroEngineSplashBranding` | `false` | `true` adds "Made with MicroEngine" below the image |
| `MicroEngineIconBackground` | the colour along the icon's edge | the background layer of Android's adaptive icon, `#RRGGBB` |

The five properties above are not in a published SDK yet; they arrive with the next release.

## Rules the build checks

- **Application id**: two or more dot-separated parts, each starting with a letter, made of letters, digits and
  underscores (`com.example.my_game`). Android and Apple reject hyphens. Use a domain you control; stores and phones
  identify the game by it, so it must never change once players have it.
- **Icon**: a PNG, square, exactly 256, 512 or 1024 pixels (1024 is best: iOS asks for it, and pixel art is
  enlarged without blur when smaller). The tool writes each system's format from it: Windows `.ico`, macOS `.icns`,
  Android launcher icon, iOS asset catalog, browser favicon.
- **Orientation**: `landscape`, `portrait` or `any`.

A wrong value stops `dotnet build` with an error naming it (`ME0001` id, `ME0002` orientation, `ME0003` missing
icon, `ME0004` not a PNG, `ME0005` wrong size, `ME0006` a colour not written `#RRGGBB`, `ME0007` missing start-screen
image, `ME0008` a start-screen switch that is not `true` or `false`). `microengine info` shows what the project declares.

## At run time

`AppIdentity.Of(game)` reads the identity the build wrote into the game's assembly. The launcher passes it to the
platform (`PlatformRequest.Identity`): SDL sets the application name, version and id (`SDL_SetAppMetadata`, which
Wayland and X11 use to group windows) and the window icon; raylib sets the window icon; phone hosts follow the
orientation. A game built without the targets has no identity, and platforms keep their defaults.

## Icons on each system

The one icon becomes: the Windows executable's and window's icon (`.ico`), the Linux window icon and `icon.png` beside
the desktop entry, the macOS `AppIcon.icns`, the iOS asset catalog's 1024-pixel image, the browser favicon and web app
manifest icon, and Android launcher icons at every density. On Android 8 and later the launcher masks icons to its own
shape (circle, squircle...); the package carries an adaptive icon whose foreground is your icon over the part every mask
shows and whose background is `MicroEngineIconBackground`, so the launcher shows your art instead of shrinking it onto
a white plate. Art that reaches the corners loses them to round masks: keep what matters away from the corners. (The
adaptive icon is not in a published SDK yet; the published SDK packages a square icon at one density.)

## Start screen

> **Not in a published SDK yet**: the start screen arrives with the next SDK release.

A game opens on its start screen: its image (`MicroEngineSplashImage`, by default the icon) in the middle of its
colour (`MicroEngineSplashColor`), with "Made with MicroEngine" below only when `MicroEngineSplashBranding` is `true`.
Each system shows it in its own way, and the engine continues it, so the player never sees a black screen between the
tap and the first frame:

| Where | Shown by |
| --- | --- |
| Android 12 and later | the system launch screen in the start-screen colour with the adaptive icon (or the start-screen image), kept until the engine has drawn its first frame and never longer than 8 seconds |
| Older Android | the window background: the colour with the image in the middle |
| iOS | `UILaunchScreen` in Info.plist: the colour and the image from the asset catalog |
| Browsers | the page itself, before any script: the colour, the image, the title and the loading bar (and the branding line) |
| Every platform | the engine, while the game's `Load` runs: the same image on the same colour, with the bands around the frame in that colour |

There is no added delay: the start screen lasts as long as loading does, and the game's first scene replaces it. An
error while loading is reported as before (on Android, the launch screen gives way after 8 seconds at most).
Scripted and headless runs skip it (`RunOptions.StartScreen`), so captures and tests are unchanged.

```xml
<MicroEngineIcon>icon.png</MicroEngineIcon>
<MicroEngineSplashColor>#5b9be8</MicroEngineSplashColor>
<MicroEngineSplashBranding>true</MicroEngineSplashBranding>
```

Source: [`games/MeadowRun/MeadowRun.csproj`](../../games/MeadowRun/MeadowRun.csproj)

## Where it comes from

The targets are `build/MicroEngine.targets` in the `MicroEngine` package; every game that references any MicroEngine
package imports them.

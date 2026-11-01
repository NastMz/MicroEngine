# Platforms

MicroEngine games run wherever an `IPlatform` implementation runs. The official ones: the SDL 3 platform, the default
on Windows, Linux, macOS, Android and iOS; the raylib platform, used in browsers (compiled to WebAssembly) and as an
alternative on desktops; and the headless platform used by tests ([Backends](../manual/backends.md)). All six systems
(Windows, Linux, macOS, Android, iOS and browsers) are official targets; they are not equally proven. This page lists
each target and exactly how far it has been validated. The site's [compatibility matrix](/platforms/) shows the same
evidence cell by cell, and [Downloads](downloads.md) what a player can install.

## Support table

| Target | Platform | Status |
| --- | --- | --- |
| Windows desktop (x64) | `sdl` (default), `raylib`, `sdl-software` through `DesktopLauncher` | Played by hand with raylib (builds, tests, scripted captures, manual play). SDL: the 15 scripted scenarios on real hardware, identical on Direct3D 11, OpenGL and Vulkan; the four downloadable ZIPs reproduce them and were played by hand on Windows 11, with keyboard and an Xbox controller ([INPUT-1](../evaluations/input-1.md#7b-played-by-hand)) |
| Linux desktop (x64) | `sdl` (default), `raylib`, `sdl-software` | Checked automatically on hosted runners, and the downloadable package in Ubuntu 22.04, Debian 12 and Fedora 41 containers ([Desktop validation](#desktop-validation)); the four downloads played by hand on an Ubuntu 24.04.4 LTS PC ([INPUT-1](../evaluations/input-1.md#7b-played-by-hand)) |
| macOS desktop (Apple silicon) | `sdl` (default); `raylib` needs a GPU | Checked automatically: on the hosted Mac (arm64, Metal) SDL draws the 15 scripted scenarios against a macOS reference, and so does each downloadable app; no person has played on a Mac |
| Linux on ARM (arm64) | `sdl` | Not checked: the SDL package has an arm64 library, but nothing has run on it |
| Chrome (Chromium) on desktop | `raylib` in WebAssembly through `BrowserLauncher` | The four demos are verified with automated tests, plus manual validation in the browser |
| Firefox on desktop | Same | The four demos are verified with automated tests, plus manual validation in the browser |
| Microsoft Edge and other Chromium-based browsers | Same | Edge: the four demos played by hand on the desktop ([INPUT-1](../evaluations/input-1.md#7b-played-by-hand)), not covered by the automated tests; other Chromium-based browsers not claimed |
| Safari | Same | Not tested |
| Phones and tablets in a browser | Same | On-screen touch controls; the four demos played by hand in Chrome on an Android 16 phone, and checked with real multi-touch in emulated mobile Chrome on four devices; Safari not run |
| Android app | `sdl` (the Android host) | Checked on the x86_64 emulator: all four demos install from their release APK, draw, open audio, take keys and touch, and survive the background ([Phones and tablets](#phones-and-tablets)); the four APKs played by hand on a Redmi Note 14 Pro (arm64, Android 16), with touch and a Bluetooth PlayStation controller |
| iOS app | `sdl` (the iOS host) | Checked in Apple's simulator only: the demos draw (captures within the SDL reference), open audio, respond to scripted touches and survive the background; no iPhone, and no installable download ([why](downloads.md#ios)) |
| Headless (tests, CI) | `HeadlessPlatform` | Validated: used by every test project; `--headless` launcher flag |

"Validated" and "verified" mean the configuration was run and its results recorded. Anything else is a statement
about .NET, SDL, raylib or the browser, not about MicroEngine. An automated check proves what it exercises and nothing
more: a capture on a CI runner is not a person playing, an emulator is not a phone, and the iOS simulator is not an iPhone.
Evidence for this page: [PLATFORM-3](../evaluations/platform-3.md) and [PRODUCT-1](../evaluations/product-1.md).

## Desktop validation

What has been shown on each desktop system, level by level. "Automated" means a check that runs on every engine
change; "recorded" means it was run and its result kept, once.

| Level | Windows x64 | Linux x64 | macOS |
| --- | --- | --- | --- |
| Build of the engine, the four games and the tests | Automated | Automated | Automated, not blocking; passing (macOS 26, arm64) |
| Simulation: engine tests and the four games' headless playthroughs | Automated | Automated | Automated, not blocking; passing |
| Native rendering, raylib: the 15 scripted scenarios (138 captures) | Recorded: compared byte for byte with the browser builds | Automated under a virtual display with software OpenGL (Mesa), compared with a Linux reference | Attempted, not blocking: raylib cannot open a window on the hosted runner |
| Native rendering, SDL: the same 138 captures | Recorded: Direct3D 11, OpenGL and Vulkan draw identical frames, kept as the SDL reference | Automated on Mesa (llvmpipe) against its own SDL reference | Automated in the platform workflow against a macOS reference (reproduced pixel for pixel by a second run) |
| The downloadable package, played (structure, start, 138 captures, audio device) | Recorded on real hardware (Direct3D 11, sound device); in CI on WARP without a sound device | In CI under Xvfb, and in Ubuntu 22.04, Debian 12 and Fedora 41 containers; Debian 11 refuses with a clear glibc error | In CI (Metal, ad-hoc signature verified) |
| SDL and raylib draw the same probe frame | Recorded | Recorded (local Linux) | Not yet run |
| Native keyboard: keys from the system reach the game | Manual play (raylib) | Automated with raylib: pressed, held, released, a tap shorter than a frame, two keys at once | Not covered |
| Audio device opens | Manual play (raylib); SDL's device opened and played sounds | Automated on a virtual sound server (PulseAudio) | Automated, not blocking: opens on the hosted runner |
| Audio output carries the sound played | Manual play (raylib) | Automated with raylib: a played tone is recorded from the virtual sound server and recognised; not listened to | Not covered |
| Gamepad | Played by hand with an Xbox controller | Not covered | Not covered |
| Manual play on real hardware (GPU, speakers, fullscreen, window manager) | Done with raylib and with the SDL downloads | The four downloads played on an Ubuntu 24.04.4 LTS PC | Pending |

Linux checks run on Ubuntu 24.04 x64 under Xvfb; the raylib reference was recorded with Mesa 25.2.8 (llvmpipe) and the
hosted CI runner reproduced all 138 captures pixel for pixel. On Mesa's software rasterizer, SDL draws a few rotated
and translucent sprites one texel or one blend level apart from GPU drivers, so Linux keeps its own SDL reference. On
macOS the hosted runner builds and passes the headless tests; GLFW (raylib) finds no accelerated OpenGL there and
cannot create a window, while SDL draws through Metal, which that runner offers. The four Linux downloads were played
by hand on an Ubuntu 24.04.4 LTS PC ([INPUT-1](../evaluations/input-1.md#7b-played-by-hand)); Wayland sessions, other distributions'
drivers, gamepads on Linux, F11 and Alt+Enter fullscreen and high-DPI screens have not been checked one by one.

## Gamepads

Both desktop backends read the first gamepad, and the same bindings apply in the browser (see
[Input](../manual/input.md)). In the browser, gamepad play was validated by hand in Edge on the desktop with an Xbox
controller and in Chrome on an Android phone with a PlayStation controller over Bluetooth. With SDL, the downloads were played with an Xbox controller on the desktop and
the Android apps with a PlayStation controller over Bluetooth ([INPUT-1](../evaluations/input-1.md#7b-played-by-hand)).
No claim is made for other controllers, or for gamepads in Firefox or Safari.

## Phones and tablets

The website works on phones and tablets, and so do the web demos: each game has its own on-screen controls (a d-pad
or a joystick and labelled buttons, [Input](../manual/input.md#touch-controls)) in the browser and in the apps. In
the browser they were played with real multi-touch on four emulated devices in Chrome (Android phone, notched iPhone
viewport, small portrait phone, tablet): walking, jumping while holding a direction, aiming and firing at once, analog
walking, menus, focus loss, rotation, pinch and long press ([INPUT-1](../evaluations/input-1.md#6-mobile-web-evidence)).
The four web demos were also played by hand in Chrome on an Android phone (Redmi Note 14 Pro, Android 16,
[INPUT-1](../evaluations/input-1.md#7b-played-by-hand)). Safari was not run; [the evaluation](../evaluations/input-1.md#7-checking-on-a-real-phone)
lists the steps to check on an iPhone.

The native apps run on the SDL platform with the same controls:

- **Android**: the four demos run on an Android emulator (x86_64) from their release APKs. Their 138 scripted captures
  match the emulator's own SDL reference (Meadow Run and Neon Rift are pixel-identical to Windows; Courtyard Key's
  half-pixel HUD and Trail Duel's rotated translucent sprites differ by a texel, as on Mesa and Metal); keys and taps on
  the on-screen controls (at the positions the game logs for that screen) take each game from its title into play; a
  held drag on the movement control moves the player, and a scripted two-thumb run holds movement and a second
  control together; Home suspends the game (music paused, progress
  saved) and relaunching resumes it; content files load from the app package. The four APKs were played by hand on a
  Redmi Note 14 Pro (arm64, Android 16), with touch and a PlayStation controller over Bluetooth; no tablet was used.
- **iOS**: the demos run in the iOS simulator; their scripted captures are within the SDL reference, the audio device
  opens, and opening another app suspends the game until it is brought back. The simulator cannot be sent system
  touches by script, so touch is checked with scripted touches on the simulator's own control layout (two controls
  held at once, the player moving); no iPhone was used.

## What CI proves

Every change runs only the checks it needs, once per content; full validation runs every check on demand, and the
platform workflow also runs once a month. It never decides whether anything is published, and new demo builds reach
this site only through an explicit release.

| Check | Runs | Proves |
| --- | --- | --- |
| .NET tests on Linux | engine, game or test changes; full validation | the engine and the four games' rules and headless playthroughs |
| .NET tests on Windows | engine, backend and build changes; full validation | the same on Windows |
| Native Linux | engine, game and capture changes; full validation | raylib and SDL draw the 138 scripted frames like their Linux references; SDL and raylib draw the same probe frame; keyboard and audio through the system |
| Browser suites | site, web host and rebuilt demos (all suites); documentation (page layout only); every release | pages at three widths, and each demo loaded, played, restarted and closed in Chrome and Firefox |
| macOS | full validation; monthly | the same tests on macOS, the 138 frames on SDL (Metal) against the SDL reference, and why raylib can or cannot open a window |
| Android emulator | full validation; monthly | two games (all four on request) install, start, draw like the SDL reference, take keys, taps, drags and two scripted thumbs, and resume |
| iOS simulator | full validation; monthly | two games (all four on request) install, start, draw like the SDL reference, respond to scripted touches and resume |
| Web on mobile | full validation; monthly | the four web demos played with real multi-touch on four emulated phones and tablets in Chrome |
| Native packages | full validation (missing ones); every release (missing ones) | each game's download for Windows, Linux, macOS and Android, checked as the file a player gets: structure, dependencies, then played on its own system against that system's reference ([packaging](../../packaging/README.md#what-a-package-check-proves)); Linux also in other distributions' containers |

## Desktop

`DesktopLauncher.Run(game, args)` opens a window on the default platform (SDL; `--platform raylib` or
`MICROENGINE_PLATFORM=raylib` for the other), scales the virtual canvas by the largest whole factor that fits and
letterboxes it. F11 and Alt+Enter toggle fullscreen. Keyboard and gamepad bindings are defined by the game's
`InputMap` (see [Input](../manual/input.md)). Saves go to the user's data folder: `%LOCALAPPDATA%/MicroEngine/<GameId>`
on Windows, `$XDG_DATA_HOME/MicroEngine/<GameId>` (by default `~/.local/share`) on Linux and
`~/Library/Application Support/MicroEngine/<GameId>` on macOS (see [Persistence](../manual/persistence.md#where-saves-live)).

Games target .NET 10. Native libraries arrive with NuGet packages: SDL 3.5 with `ppy.SDL3-CS` (Windows x64, x86,
arm64; Linux x64, x86, arm64, arm; macOS x64, arm64; Android; iOS) and raylib with `Raylib-cs` 8.1.0 (Windows x64,
x86; Linux x64; macOS x64, arm64). On Linux both open X11 windows (SDL also speaks Wayland) and need OpenGL from Mesa or
a GPU driver; Wayland sessions have not been checked yet.

## Browser

The four games run in desktop browsers as static packages that share one host page. In Chrome and Firefox on a
desktop, the automated tests recorded:

- every game run through its real mechanics, with scripted captures byte-identical to the desktop build
  (138 per browser);
- audio after the starting click or key, 60 updates per second at 25 to 100 drawn frames per second;
- fullscreen through the page's button and Alt+Enter (F11 stays the browser's own fullscreen);
- saves in `localStorage` under `microengine/<Game>/`, kept across reloads, isolated between games.

The demos were also validated manually in the browser. Transfer sizes range from 1.7 MB (Meadow Run, Courtyard
Key) to 11.9 MB (Neon Rift) with Brotli.

Producing browser packages is not available to external developers yet. How the host page behaves and how to
embed a package: [Embedding](../examples/embedding.md).

## Headless

`HeadlessPlatform` implements `IPlatform` without a window or audio device: one simulation step per frame, a
renderer that counts draw calls, a silent audio device that records what was played, and `HeadlessInput` whose
keys a test sets directly. It makes runs deterministic and is what every test project uses
(see [Testing](../examples/testing.md)).

## Adding a platform

A new backend implements `IPlatform` and its four device interfaces (`IRenderer`, `IGraphicsDevice`,
`IAudioDevice`, `IInputSource`); no game code changes. See [Backends](../manual/backends.md) and
[Contracts](../reference/contracts.md#iplatform).

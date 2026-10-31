# Platforms

MicroEngine games run wherever an `IPlatform` implementation runs. Today there are two: the raylib backend,
used on the desktop and, compiled to WebAssembly, in browsers; and the headless platform used by tests. This
page lists each target and exactly how far it has been validated.

## Support table

| Target | Backend | Status |
| --- | --- | --- |
| Windows desktop (x64) | `RaylibPlatform` through `DesktopLauncher` | Supported: builds, all tests and scripted captures, manual play |
| Linux desktop (x64) | `RaylibPlatform` through `DesktopLauncher` | Automatically checked, not yet supported: see [Desktop validation](#desktop-validation); manual validation pending |
| macOS desktop | `RaylibPlatform` | Under evaluation, not supported: builds and passes the headless tests in CI (not blocking yet); raylib cannot open a window on the hosted CI runner, so nothing native is shown |
| Linux on ARM (arm64) | — | Not available: the `Raylib-cs` package has no native raylib library for it |
| Chrome (Chromium) on desktop | `RaylibPlatform` in WebAssembly through `BrowserLauncher` | The four demos are verified with automated tests, plus manual validation in the browser |
| Firefox on desktop | Same | The four demos are verified with automated tests, plus manual validation in the browser |
| Other Chromium-based browsers (for example Edge) | Same | Not claimed: they share Chrome's engine, but they are not covered by the tests |
| Safari | Same | Not tested |
| Phones and tablets | Same | Not validated on real mobile devices; the demos have no dedicated touch controls |
| Headless (tests, CI) | `HeadlessPlatform` | Validated: used by every test project; `--headless` launcher flag |

"Validated" and "verified" mean the configuration was run and its results recorded. Anything else is a statement
about .NET, raylib or the browser, not about MicroEngine. An automated check proves what it exercises and nothing
more: a desktop system is called supported only once a person has also played on it.

## Desktop validation

What has been shown on each desktop system, level by level. "Automated" means a check that runs on every engine
change; "recorded" means it was run and its result kept, once.

| Level | Windows x64 | Linux x64 | macOS |
| --- | --- | --- | --- |
| Build of the engine, the four games and the tests | Automated | Automated | Automated, not blocking; passing (macOS 26, arm64) |
| Simulation: engine tests and the four games' headless playthroughs | Automated | Automated | Automated, not blocking; passing |
| Native rendering: raylib and OpenGL draw the 15 scripted scenarios (138 captures) | Recorded: the same captures, compared byte for byte with the browser builds | Automated under a virtual display with software OpenGL (Mesa), compared with a Linux reference | Attempted automatically, not blocking: raylib cannot open a window on the hosted runner, so no capture is produced yet |
| Native keyboard: keys from the system reach the game through raylib | Manual play | Automated: pressed, held, released, a tap shorter than a frame, two keys at once | Not covered |
| Audio device opens | Manual play | Automated on a virtual sound server (PulseAudio) | Automated, not blocking: opens on the hosted runner |
| Audio output carries the sound played | Manual play | Automated: a played tone is recorded from the virtual sound server and recognised; not listened to | Not covered |
| Gamepad | Supported by the backend | Not covered | Not covered |
| Manual play on real hardware (GPU, speakers, fullscreen, window manager) | Done | Pending | Pending |

Linux checks run on Ubuntu 24.04 x64 under Xvfb; the reference was recorded with Mesa 25.2.8 (llvmpipe) and the
hosted CI runner reproduced all 138 captures pixel for pixel. On macOS the hosted runner (macOS 26, arm64) builds and
passes the headless tests, but GLFW finds no suitable OpenGL pixel format on that virtual machine and cannot create a
window, so native rendering and input on macOS have no evidence yet; they need a Mac with a GPU. Wayland
sessions, real GPUs and drivers, gamepads, F11 and Alt+Enter fullscreen and high-DPI screens remain to be checked by
hand before Linux is called supported.

## Gamepads

The engine's desktop backend reads gamepads through raylib, and the same bindings apply in the browser (see
[Input](../manual/input.md)). In the browser, gamepad play was validated manually in one Chromium-based browser
with the controller that was tested. No claim is made for other controllers or other browsers.

## Phones and tablets

The website works on phones and tablets. The demos have no dedicated touch controls and were not validated on real
mobile devices; the site lets visitors try them at their own risk. Research into mobile and touch input is on the
[roadmap](/roadmap/).

## Desktop

`DesktopLauncher.Run(game, args)` opens a raylib window, scales the virtual canvas by the largest whole factor
that fits and letterboxes it. F11 toggles fullscreen. Keyboard and gamepad bindings are defined by the game's
`InputMap` (see [Input](../manual/input.md)). Saves go to the user's data folder: `%LOCALAPPDATA%/MicroEngine/<GameId>`
on Windows, `$XDG_DATA_HOME/MicroEngine/<GameId>` (by default `~/.local/share`) on Linux and
`~/Library/Application Support/MicroEngine/<GameId>` on macOS (see [Persistence](../manual/persistence.md#where-saves-live)).

Games target .NET 10. The raylib native library arrives with the `Raylib-cs` 8.1.0 NuGet package that the backend
depends on, for Windows (x64, x86), Linux (x64) and macOS (x64, arm64). On Linux, raylib opens its window through X11
(GLFW) and needs OpenGL (Mesa or a GPU driver); a Wayland session runs it through XWayland, which has not been
checked yet.

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

# Platforms

MicroEngine games run wherever an `IPlatform` implementation runs. Today there are two: the raylib backend,
used on the desktop and, compiled to WebAssembly, in browsers; and the headless platform used by tests. This
page lists each target and exactly how far it has been validated.

## Support table

| Target | Backend | Status |
| --- | --- | --- |
| Windows desktop | `RaylibPlatform` through `DesktopLauncher` | Validated: builds, all tests and scripted captures |
| Linux desktop | `RaylibPlatform` | Potentially compatible thanks to .NET and raylib; not verified and not officially supported |
| macOS desktop | `RaylibPlatform` | Potentially compatible thanks to .NET and raylib; not verified and not officially supported |
| Chrome (Chromium) on desktop | `RaylibPlatform` in WebAssembly through `BrowserLauncher` | The four demos are verified with automated tests, plus manual validation in the browser |
| Firefox on desktop | Same | The four demos are verified with automated tests, plus manual validation in the browser |
| Other Chromium-based browsers (for example Edge) | Same | Not claimed: they share Chrome's engine, but they are not covered by the tests |
| Safari | Same | Not tested |
| Phones and tablets | Same | Not validated on real mobile devices; the demos have no dedicated touch controls |
| Headless (tests, CI) | `HeadlessPlatform` | Validated: used by every test project; `--headless` launcher flag |

"Validated" and "verified" mean the configuration was run and its results recorded. Anything else is a statement
about .NET, raylib or the browser, not about MicroEngine.

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
`InputMap` (see [Input](../manual/input.md)). Saves go to the user's local application data folder
(`%LOCALAPPDATA%/MicroEngine/<GameId>` on Windows).

Games target .NET 10. The raylib native library arrives with the `Raylib-cs` 8.1.0 NuGet package that the backend
depends on.

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

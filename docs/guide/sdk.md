# Versions and compatibility

**MicroEngine SDK 0.5.0-alpha.2 — Alpha.** It is on nuget.org: [`MicroEngine.Cli`](https://www.nuget.org/packages/MicroEngine.Cli)
and the five other packages below ([release notes](release-notes.md)). [Getting MicroEngine](getting-microengine.md)
shows how to install it.

Alpha means two things:

- **The engine is complete enough to ship games.** Games built with it run on Windows, Linux, macOS, Android, iOS and
  in browsers ([Platforms](platforms.md)); the four [demo games](/demos/) are built with it.
- **Its public API is still evolving.** Between Alpha versions (0.4 → 0.5) public APIs may change incompatibly; every
  such change is listed in the [migration notes](../migrations.md) of the version that makes it, with how to update.
  There is no 1.0 and no promise of backward compatibility yet. The core types a game uses are listed in
  [Contracts](../reference/contracts.md).

## Packages

| Package | Contains | Depends on |
| --- | --- | --- |
| `MicroEngine` | the engine as separate modules: `MicroEngine.dll` (the `Game` class, scenes and testing support) beside the kernel, platform contract, hosting, graphics (the device contract and 2D), text, content, input, touch controls, audio, the scene world, physics with its tile backend, UI, saves and achievements, each its own assembly; the headless platform and the [identity](app-identity.md) build targets | .NET only |
| `MicroEngine.Sdl` | the SDL 3 platform for desktops, phones and browsers (SDL 3 compiled to WebAssembly comes inside it), and the opt-in SDL_GPU graphics device for desktops ([the graphics device](../manual/rendering.md#the-graphics-device)) | `MicroEngine`, `ppy.SDL3-CS`, `NVorbis` |
| `MicroEngine.Raylib` | the raylib platform (desktops only) | `MicroEngine`, `Raylib-cs` |
| `MicroEngine.Desktop` | `DesktopLauncher` and the official desktop catalog (`sdl`, `raylib`, `sdl-software`, `headless`) | `MicroEngine.Sdl`, `MicroEngine.Raylib` |
| `MicroEngine.Text.Shaping` | optional: `ShapedFont`, text shaped by HarfBuzz for Arabic, Hebrew, Indic and other complex scripts ([Shaped text](../manual/text.md#shaped-text-arabic-hebrew-and-indic-scripts)) | `MicroEngine`, `HarfBuzzSharp` and its native packages |
| `MicroEngine.Cli` | the [`microengine` tool](cli.md), with [`microengine shaders`](cli.md#shaders), and the browser, Android and iOS hosts it builds with | — |
| `MicroEngine.Templates` | `dotnet new microengine-game` | — |

A game installs only what it uses: one that ships SDL only references `MicroEngine.Sdl` and gets no raylib file.
Packages carry symbols (`.snupkg`, with Source Link to the commit), XML documentation, a README and the MIT
licence. The demo games are not in any package: they stay in the repository under their own terms
([Licensing](licensing.md)).

Two parts of the engine stay out of every published package: the 3D path
([The 3D path](../manual/rendering.md#the-3d-path)) is in the repository only, and the Box2D physics backend
([The Box2D backend](../manual/world.md#the-box2d-backend)) is an optional package that is not published.

## Versioning and compatibility

- SemVer with numbered pre-releases while in Alpha: `-alpha.1`, `-alpha.2`... A new minor (0.5) marks
  incompatible changes; numbered Alphas of one minor are for fixes and additions.
- All seven packages share one version, and the tool builds games with the hosts of its own version: use the same
  version for the tool and the engine packages (the template does).
- A game's own version is its own (`<Version>` in its project), independent of the SDK's.
- SDK releases are published on GitHub as `sdk-v<version>`; the demo games have releases of their own (`v0.1.0`,
  `v0.2.0`...), with unrelated numbers.

**Upgrading** a game: update the tool, set the `MicroEngine.*` package versions in its project, and read the
[migration notes](../migrations.md) from your version up ([Updating the SDK](updating.md)).

## Published version and development

The documentation describes the published SDK, **0.5.0-alpha.2**, and every code sample on this site is built against
the packages on nuget.org. The documentation and the SDK are published together, so a page never describes an engine
the packages do not have.

# Status and limitations

MicroEngine is a working framework with four complete games and a browser build. It is not distributed to
external developers yet. This page states plainly what exists today, what does not, and what has and has not been
validated, so you can judge whether it would fit your project.

## What exists

| Area | State | Evidence |
| --- | --- | --- |
| Core engine (`src/MicroEngine`) | Loop, scenes, input, graphics model, tile world, audio synthesis, UI, saves, achievements | [Architecture](../architecture.md), the engine's test project |
| Desktop backend (`src/MicroEngine.Raylib`) | raylib window, renderer, audio, keyboard and gamepad input, `DesktopLauncher` | Validated on Windows |
| Browser backend | Same raylib backend compiled to WebAssembly; `BrowserLauncher`, `localStorage` saves, one shared host | Automated browser tests and manual validation ([Platforms](platforms.md)) |
| Games | Meadow Run, Courtyard Key, Trail Duel, Neon Rift, each with five achievements and saves | Each game's test project; playable on the [demos page](/demos/) |
| Sample | Hello Engine, the smallest complete game | [Hello Engine walkthrough](../examples/hello-engine.md) |
| Tests | 279 automated tests at the time of the WEB-1 evaluation (engine 93, Meadow Run 44, Courtyard Key 27, Trail Duel 85, Neon Rift 30) | [Testing](../examples/testing.md) |
| Scripted runs and captures | `--script`, `--hidden`, `--captures` on every game; byte-identical captures desktop vs browser for 15 scenarios | [Testing](../examples/testing.md) |

## What does not exist

These are not implemented. Do not plan around them.

| Missing | Notes |
| --- | --- |
| Distribution to external developers: NuGet packages, project templates (`dotnet new`), installers, versioned releases | Planned; see [Getting MicroEngine](getting-microengine.md) |
| A published licence | The MicroEngine SDK License (free, commercial use, no royalties) is being prepared; see [Licensing](licensing.md) |
| Visual editor, level editor or asset pipeline tool | Content is authored in code (`PixelImage.FromMap`, `TileLayer.Parse` over ASCII rows) or loaded through `ContentFolder`. A visual editor is only a long-term vision on the roadmap |
| Touch controls | The demos have no dedicated touch controls |
| An ECS, physics engine, networking, scripting language or 3D | Not part of the engine's design ([Architecture](../architecture.md)). 3D is only a long-term vision on the roadmap |
| Cloud saves | Saves stay on the machine (files) or in the browser (`localStorage`) |

## Platforms and validation

| Configuration | Status |
| --- | --- |
| Windows desktop | Validated: builds, tests, games and scripted captures |
| Linux and macOS desktop | Potentially compatible thanks to .NET and raylib; not verified and not officially supported |
| Browsers | The four demos are verified with automated tests in Chromium (Chrome) and Firefox on desktop, plus manual validation in the browser |
| Safari | Not tested |
| Other Chromium-based browsers (for example Edge) | They share Chrome's engine, but no claim is made for them |
| Gamepad in the browser | Validated manually in one Chromium-based browser with the controller that was tested; no claim for other controllers or browsers |
| Gamepad on the desktop | Supported by the engine's desktop backend |
| Phones and tablets | The website works on them. The demos have no dedicated touch controls and are not validated on real mobile devices; the site lets visitors try them at their own risk |

Details per target: [Platforms](platforms.md).

## Known open items

- Neon Rift's browser download is 11.9 MB with Brotli, mostly OGG music.
- raylib's web audio uses the deprecated `ScriptProcessorNode`; sound stops while the tab is hidden.
- One Firefox scripted-capture run under heavy machine load diverged from desktop; repeated alone it matched.
  The cause was not found.
- Browser saves live in `localStorage` (about 5 MB per origin, evictable, discarded by private windows).

The full list with evidence is in [Known limitations](../reference/limitations.md).

## Planned work

The [roadmap](/roadmap/) lists planned work without dates: SDK distribution (NuGet packages, project templates,
versioned releases), research into mobile and touch input, engine robustness, and validation on more platforms. A
visual editor and 3D appear there only as a long-term vision, not as available or committed features.

## Stability of the API

There is no published package and no versioned API yet. Types documented in [Contracts](../reference/contracts.md)
are the ones the four games depend on, and changes to them go through recorded design decisions. Expect them to
evolve.

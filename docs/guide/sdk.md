# SDK status: Alpha

**MicroEngine SDK — Alpha, 0.x.** The current version is set in one place, `MicroEngineVersion` in
[`Directory.Build.props`](../../Directory.Build.props), and every engine assembly and demo package carries it.

The engine is functional: four complete games run on it, and it is checked on Windows, Linux, macOS, Android, iOS and in
browsers ([Platforms](/platforms/)). Its contracts, subsystems and tools are still evolving, so:

- public APIs may change incompatibly between Alpha versions (0.2 → 0.3), and a change is recorded in a decision
  record and in the migration notes of the version that makes it;
- there is no 1.0 and no promise of backward compatibility yet;
- the types the four games depend on are listed in [Contracts](../reference/contracts.md); they are the ones most likely
  to stay, not guaranteed to.

## How you use it today

From the public repository, with the .NET 10 SDK: reference `src/MicroEngine.Desktop` (or a single backend) from your
game's project. [Getting MicroEngine](getting-microengine.md) has the commands.

## SDK-1: what is prepared

SDK-1 is the next stage on the [roadmap](/roadmap/): installing MicroEngine without cloning it. Its structure is decided
([ADR 0019](../decisions/0019-sdk-packages.md)) and partly working; nothing is published yet.

### Packages

One NuGet package per project in `src/`, so a game installs only the backends it uses:

| Package | Contains | Depends on | Native libraries |
| --- | --- | --- | --- |
| `MicroEngine` | the core: loop, scenes, input, graphics model, world, audio synthesis, UI, saves, achievements, headless platform | .NET only | none |
| `MicroEngine.Sdl` | the SDL 3 platform (desktop and phones) | `MicroEngine`, `ppy.SDL3-CS`, `NVorbis` | SDL 3 for Windows, Linux, macOS, Android, iOS |
| `MicroEngine.Raylib` | the raylib platform (desktop; the browser build compiles it to WebAssembly) | `MicroEngine`, `Raylib-cs` | raylib for Windows, Linux, macOS |
| `MicroEngine.Desktop` | `DesktopLauncher` with the official desktop catalog (`sdl`, `raylib`, `sdl-software`, `headless`) | `MicroEngine.Sdl`, `MicroEngine.Raylib` | both |

`dotnet pack` already produces these four packages at the SDK version (`0.2.0-alpha`), with the MIT licence expression
and repository metadata (`src/Directory.Build.props`). They were consumed from a local feed by a new project, outside
the repository:

- referencing `MicroEngine.Desktop`, the Hello Engine sample ran headless and listed all four platforms;
- referencing only `MicroEngine.Sdl`, with its own catalog, it listed `sdl` and `headless`, and no raylib file was
  copied to its output. A game does not have to install every backend.

```csharp
// A game that ships SDL only: its own catalog instead of DesktopLauncher's.
return GameLauncher.Run(new HelloGame(), args, new PlatformCatalog().Add(SdlPlatform.Profile).Add(HeadlessPlatform.Profile));
```

### Default platform

Choosing a platform stays the host's business ([Backends](../manual/backends.md#choosing-a-platform)): the first profile
of the catalog that can run on the machine is the default. With `MicroEngine.Desktop` that is SDL 3 on every desktop
system; `--platform` and `MICROENGINE_PLATFORM` select another. A game that references one backend gets that backend.

### Still to build for SDK-1

| Piece | Plan |
| --- | --- |
| Publishing | Packages pushed to NuGet by the release workflow, from the same commit as a demo release, after the same checks; the version must equal `MicroEngineVersion` |
| Templates | A `MicroEngine.Templates` package with `dotnet new microengine-game` (a Hello Engine copy referencing `MicroEngine.Desktop`) and `microengine-game-sdl` (SDL only) |
| Samples | `samples/` stays the source of the templates; the template content is generated from it, so the sample and the template cannot drift |
| Phone and browser hosts | Not packages yet: the Android and iOS hosts (`platforms/`) and the browser host (`web/`) build one of this repository's games. Making them consumable (an MSBuild SDK or a template per host) is after SDK-1 |
| Migration notes | One section per version in `docs/migrations.md`, written with the change that breaks something: what changed, why, how to update |
| Versioning | SemVer with an `-alpha` suffix while 0.x; all engine packages share one version; a game's own version is its own (`<Version>` in its project, as the demos do) |

### Distribution of games made with it

A game built with MicroEngine is packaged like the demos ([packaging](../../packaging/README.md)): a self-contained
.NET publish with SDL next to it. SDK-1 may turn `packaging/package.mjs` into a documented tool for any game; today it
packages the four demos only.

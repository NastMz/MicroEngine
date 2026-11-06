# Repository structure (internal)

This page is for engine developers working in the MicroEngine repository. The repository is one .NET solution: the
engine as a set of assemblies, its backends and hosts, the `microengine` tool, four demo games, samples and their tests.
This page maps the folders and then shows how a game project is organised, using the real games as the reference.

## Repository layout

| Path | Contents |
| --- | --- |
| `src/MicroEngine.*/` | The engine, one assembly per boundary ([Engine source](#engine-source)) |
| `src/MicroEngine.Cli/` | The `microengine` tool, with the browser, Android and iOS hosts it packages games with (`Hosts/`) |
| `templates/` | The `dotnet new` template |
| `games/<Name>/` | A complete demo game: code, procedural art and audio, optional asset files, input scripts, README |
| `samples/` | `HelloEngine` (the smallest game, [walkthrough](../examples/hello-engine.md)), `GemGarden` (the tutorial), `Examples` (the focused examples), `AssetGallery`, `Slice3D` (the 3D path), `PhysicsSubstitution` (the two physics backends side by side) |
| `tests/` | Engine and backend tests (MSTest), each game's rule tests and headless playthroughs, the samples' build checks, suites shared between test projects (`tests/Shared`) |
| `web/` | The browser build scripts, the pinned toolchain, a local server and the browser tests ([web/README.md](../../web/README.md)) |
| `sdk/` | The documented SDK version and the checks that use the SDK from outside the repository |
| `packaging/` | Checks of the native downloads ([README](../../packaging/README.md)) |
| `platforms/` | macOS, Android, iOS and software-rendering experiments ([README](../../platforms/README.md)); not supported platforms |
| `site/` | The public site (Astro and Starlight), which publishes pages of `docs/` and `games/` |
| `docs/` | Architecture, guides, manual, examples, reference, the generated API reference, decision records and evaluations |
| `experiments/` | Prototypes kept as a record; not part of the build |
| `MicroEngine.slnx` | The solution |
| `Directory.Build.props` | .NET 10, nullable, implicit usings, warnings as errors, the engine version (`MicroEngineVersion`) |
| `global.json` | SDK selection (10.0.100, roll forward to the latest feature band) |

Build output (`bin/`, `obj/`) and generated packages (`dist/`, `artifacts/`) are not committed.

## Engine source

The engine is split into assemblies by boundary. The `MicroEngine` package bundles the first three groups below
(`src/MicroEngine/MicroEngine.csproj` lists them); a game references `MicroEngine.Desktop`, and the `microengine` tool's
hosts run the same composition in browsers and on phones. Which namespace and types each holds is in
[Architecture, Core modules](../architecture.md#core-modules); how they depend on each other is in
[Architecture, Layers](../architecture.md#layers).

| Group | Assemblies |
| --- | --- |
| Kernel and contracts | `MicroEngine.Kernel`, `MicroEngine.Platform`, `MicroEngine.Graphics` (the device contract), `MicroEngine.Input`, `MicroEngine.Audio`, `MicroEngine.Content` |
| Composition | `MicroEngine.Hosting`, `MicroEngine` (the core: `Game`, launchers, the headless platform, scenes, the asset cache) |
| Modules | `MicroEngine.Graphics2D`, `MicroEngine.Text`, `MicroEngine.UI`, `MicroEngine.Input.Touch`, `MicroEngine.Scene`, `MicroEngine.Physics`, `MicroEngine.Physics.Tiles`, `MicroEngine.Saves`, `MicroEngine.Achievements` |
| Backends and optional modules | `MicroEngine.Sdl`, `MicroEngine.Raylib`, `MicroEngine.Graphics.SdlGpu` (the tier-1 device), `MicroEngine.Graphics3D` (the minimal 3D path), `MicroEngine.Physics.Box2D` (native Box2D, an optional package) |
| Hosts and tools | `MicroEngine.Desktop`, `MicroEngine.Cli` |

There is no `MicroEngine.Migration.Entities` any more: the transitional copy of the old `Entity` API was removed once no
game used it, and the architecture test refuses any project that references it again. A scene keeps its objects in its
world (`Scene.Objects`, [The scene world](../manual/scenes.md#the-scene-world)).

## A game project

The smallest game is one project file and one source file:

```text
samples/HelloEngine/
  HelloEngine.csproj     references the MicroEngine.Desktop package
  Program.cs             entry point, Game subclass, one scene
  hello.script           input script for a smoke run
```

[`samples/HelloEngine/HelloEngine.csproj`](../../samples/HelloEngine/HelloEngine.csproj):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MicroEngine.Desktop" Version="0.5.0-alpha.1" />
  </ItemGroup>
</Project>
```

Inside the repository, [`samples/Directory.Build.targets`](../../samples/Directory.Build.targets) replaces that package
reference with a reference to `src/MicroEngine.Desktop`, so the samples build against the engine's source; a copy
outside the repository builds against the published package. The demo games reference `src/MicroEngine.Desktop`
directly.

The full games follow one shape, with folder names chosen per game:

| Part | Meadow Run | Courtyard Key | Trail Duel | Neon Rift |
| --- | --- | --- | --- | --- |
| Entry point | `Program.cs` | `Program.cs` | `Program.cs` | `Program.cs` |
| `Game` subclass | `MeadowGame.cs` | `CourtyardKeyGame.cs` | `TrailDuelGame.cs` | `RiftGame.cs` |
| Rules and game objects | `Gameplay/` | `Entities/`, `Model/`, `World/` | `Rules/`, `World/` | `Gameplay/` |
| Scenes | `Scenes/` | `Scenes/` | `Scenes/` | `Scenes/` |
| Art | `Art/` (procedural) | `Visuals/` | `Visuals/` | `Art/` (loading and cutting) |
| Audio | `Audio/` | `Audio/` | `Sound/` | `Art/` (sound registration) |
| UI helpers | in `Scenes/` | `UI/` | `Ui/` | in `Scenes/` |
| `Content/` | level definitions (code) | world definitions (code) and one CC0 sprite | CC0 sprites, effects and jingles | stage definitions (code) and CC0 sheets, tiles, WAV and OGG |
| Licences | none needed | `CREDITS.md` | `CREDITS.md` | `CREDITS.md` |
| Input scripts | `scripts/*.script` | `scripts/*.script` | `scripts/*.script` | `scripts/*.script` |
| Screenshots | `screenshots/` | `screenshots/` | `screenshots/` | `screenshots/` |
| Tests | `tests/MeadowRun.Tests` | `tests/CourtyardKey.Tests` | `tests/TrailDuel.Tests` | `tests/NeonRift.Tests` |

Each game's README has a "Code map" section with the details.

### Asset files

Games that load files keep them under `Content/`. The engine's build targets
([`src/MicroEngine/build/MicroEngine.targets`](../../src/MicroEngine/build/MicroEngine.targets)) copy everything there
next to the executable, so `ContentFolder` finds it at run time; `.cs` files under `Content/` compile as code (level,
world and stage definitions). A project that lists its own `Content/**` items keeps them as they are.

Each demo's project also names its test project, so the tests reach internal rules without making them public. From
[`games/NeonRift/NeonRift.csproj`](../../games/NeonRift/NeonRift.csproj):

```xml
<ItemGroup>
  <InternalsVisibleTo Include="NeonRift.Tests" />
</ItemGroup>
```

### Entry point

`Program.cs` stays minimal: it hands the game's composition to the launcher. Developer shortcuts come from the host's
variables (environment variables on desktop, the page's query in browsers, the launch's `env` option on phones),
because the launcher rejects unknown arguments. From [`games/TrailDuel/Program.cs`](../../games/TrailDuel/Program.cs):

```csharp
return DesktopLauncher.Run(Startup.Configure, args);

namespace TrailDuel
{
    public static class Startup
    {
        public static void Configure(HostBuilder host)
        {
            var saveDir = host.Environment.Variable("TRAIL_SAVE_DIR");
            var debug = host.Environment.Variable("TRAIL_DEBUG");
            var evolve = host.Environment.Variable("TRAIL_EVOLVE");
            host.UseGame(() => new TrailDuelGame(saveDir, debug, evolve));
        }
    }
}
```

`TRAIL_SAVE_DIR` keeps saves and achievements in another folder (capture runs), `TRAIL_DEBUG` jumps to a screen or
situation and `TRAIL_EVOLVE` picks who evolves under `TRAIL_DEBUG=evolve`.

`Startup.Configure` is the game's composition, and the browser and phone hosts call the same method, so they build the
same game ([One composition for every platform](../manual/backends.md#one-composition-for-every-platform)).

# Engine development setup (internal)

This page is for people working on the engine itself in a clone of the public repository; it is not an installation
guide for game developers (see [Getting MicroEngine](getting-microengine.md)). It takes you from a fresh machine to a running MicroEngine game: install the .NET 10 SDK, clone the
repository, build the solution, run the tests and play a demo on the desktop. Browser builds use a separate,
pinned toolchain and are covered by [web/README.md](../../web/README.md).

## Requirements

| Need | Version | Notes |
| --- | --- | --- |
| .NET SDK | 10 (`global.json` asks for 10.0.100 or a later feature band) | The only requirement for desktop builds and tests |
| Git | any recent | To clone the repository |
| OS | Windows, Linux or macOS | Windows 11 is where the engine is developed and played by hand; Linux x64 and macOS (Apple silicon) build and pass the tests on CI ([Platforms](platforms.md#desktop-validation)) |
| Node.js | 18 or later | Only for serving and testing browser packages |

The native libraries come with NuGet packages: SDL 3 with `ppy.SDL3-CS` (`src/MicroEngine.Sdl`) and raylib with
`Raylib-cs` (`src/MicroEngine.Raylib`); no separate install is needed for the desktop. Phone hosts need the
`android` or `ios` workload ([platforms/README.md](../../platforms/README.md)); native demo packages are built by
[packaging](../../packaging/README.md).

[`Directory.Build.props`](../../Directory.Build.props) sets the shared build settings for every project:

```xml
<TargetFramework>net10.0</TargetFramework>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<LangVersion>latest</LangVersion>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

Warnings are errors, so a build that compiles is also free of warnings.

## Clone

```bash
git clone https://github.com/NastMz/MicroEngine.git
cd MicroEngine
```

## Build and test

From the repository root:

```bash
dotnet build MicroEngine.slnx -c Release
dotnet test MicroEngine.slnx -c Release
```

`MicroEngine.slnx` contains the two engine projects, the four games, the HelloEngine sample and five MSTest
projects. The tests run headlessly (no window, no audio device) and include full scripted playthroughs, so they
take longer than plain unit tests. To run one project only:

```bash
dotnet test tests/MeadowRun.Tests/MeadowRun.Tests.csproj -c Release
```

## Run a demo

```bash
dotnet run --project samples/HelloEngine -c Release
dotnet run --project games/MeadowRun -c Release
dotnet run --project games/CourtyardKey -c Release
dotnet run --project games/TrailDuel -c Release
dotnet run --project games/NeonRift -c Release
```

Shared controls: arrows or WASD to move, Z or Space to confirm, X to cancel, C for the secondary action,
Esc or P to pause, F11 (or Alt+Enter) for fullscreen, the engine's default. Each game's README lists its own controls.

## Scripted runs and captures

Every game accepts the launcher flags described in [Configuration](../reference/configuration.md#command-line-flags).
This replays an input script in a hidden window and writes PNG captures:

```bash
dotnet run --project games/MeadowRun -c Release -- --script games/MeadowRun/scripts/smoke.script --hidden --mute --captures captures
```

Open the PNGs in `captures/` and look at them. For visual changes, passing tests are not enough.

## Your first game

1. Copy `samples/HelloEngine` to a new folder (for example `games/MyGame`), rename the project file and keep its
   `ProjectReference` path to `src/MicroEngine.Raylib/MicroEngine.Raylib.csproj` correct for the new location.
2. Read the [Hello Engine walkthrough](../examples/hello-engine.md) to understand each line.
3. Follow [Making a game](../making-a-game.md) for art, scenes, input, worlds, sound, achievements and tests.
4. Optionally add the project to `MicroEngine.slnx` so `dotnet build MicroEngine.slnx` includes it.

Games outside this repository use the NuGet packages and the `dotnet new` template instead ([Install the SDK](getting-microengine.md)).

## Optional: browser build

The browser build is outside `MicroEngine.slnx` because it needs a pinned .NET SDK with the `wasm-tools`
workload and a raylib rebuilt with that workload's emscripten. On Windows it needs Git Bash. The whole flow,
requirements, local serving and tests are in [web/README.md](../../web/README.md); the short version is:

```bash
web/setup.sh
web/build.sh MeadowRun
node web/serve.mjs dist/web
```

## Next steps

- [Project structure](project-structure.md): where things live.
- [Engine manual](../manual/lifecycle.md): each subsystem in depth.
- [Usage patterns](../examples/patterns.md): how the four games use the engine.
- [Contributing](../../CONTRIBUTING.md): rules for changes to this repository.

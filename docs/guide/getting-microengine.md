# Install the SDK

MicroEngine is a code-first 2D game engine for C# and .NET 10, open source under the MIT License. Its SDK is six NuGet
packages on [nuget.org](https://www.nuget.org/packages/MicroEngine): the engine, its platforms, the `microengine`
command-line tool and a project template. This page installs them and checks that everything works; the next page,
[Your first game](first-game.md), creates and runs a game.

The current version is **0.4.0-alpha.2**, an Alpha pre-release ([Versions and compatibility](sdk.md)). Because it is a
pre-release, NuGet commands need `--prerelease` or the exact version.

Only want to play the demos? They are on the [demo pages](/demos/), in the browser and as downloads
([Playing the demos](downloads.md)).

## 1. Install .NET 10

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download) for Windows, Linux or macOS, then check it from a
terminal:

```bash
dotnet --version
```

It should print `10.0.100` or later. Desktop games need nothing else: SDL 3 and raylib come inside the NuGet packages.
Android, iOS and browser packages need extra .NET workloads; `microengine doctor` (step 4) lists them when you need them.

## 2. Install the `microengine` tool

```bash
dotnet tool install -g MicroEngine.Cli --prerelease
microengine version
```

`microengine version` prints `0.4.0-alpha.2`. The tool creates, runs, builds and packages games
([The microengine tool](cli.md)).

If the shell says `microengine` is not found, the folder of .NET global tools is not on your `PATH`: add
`%USERPROFILE%\.dotnet\tools` (Windows) or `~/.dotnet/tools` (Linux, macOS) to it and open a new terminal. To install
an exact version instead of the newest pre-release: `dotnet tool install -g MicroEngine.Cli --version 0.4.0-alpha.2`.

## 3. The project template

`microengine new` installs the template (`MicroEngine.Templates`) at the tool's own version the first time it runs, so
there is nothing to do. To install it yourself, for example to use `dotnet new` or an IDE's "New project" dialog:

```bash
dotnet new install MicroEngine.Templates::0.4.0-alpha.2
dotnet new microengine-game -n StarCourier --app-id com.yourstudio.starcourier --title "Star Courier"
```

## 4. Check your machine

```bash
microengine doctor
```

`doctor` checks the .NET SDK, the workloads and the tools each package target needs (Android SDK, Java, Xcode) and,
for anything missing, prints how to install it. It changes nothing. `microengine doctor android` checks one target.
What each target needs is in [Packaging a game](packaging.md).

## The packages

A game references one of these; the template references `MicroEngine.Desktop`.

| Package | Contains |
| --- | --- |
| [`MicroEngine`](https://www.nuget.org/packages/MicroEngine) | the core: loop, scenes, input, graphics, tiles, audio synthesis, UI, saves, achievements, headless platform, identity build targets |
| [`MicroEngine.Sdl`](https://www.nuget.org/packages/MicroEngine.Sdl) | the SDL 3 platform (Windows, Linux, macOS, Android, iOS, browsers) |
| [`MicroEngine.Raylib`](https://www.nuget.org/packages/MicroEngine.Raylib) | the raylib platform (desktops) |
| [`MicroEngine.Desktop`](https://www.nuget.org/packages/MicroEngine.Desktop) | `DesktopLauncher` with SDL 3, raylib, SDL's software renderer and headless |
| [`MicroEngine.Cli`](https://www.nuget.org/packages/MicroEngine.Cli) | the `microengine` tool |
| [`MicroEngine.Templates`](https://www.nuget.org/packages/MicroEngine.Templates) | `dotnet new microengine-game` |

Dependencies, versioning rules and how the packages relate: [Versions and compatibility](sdk.md).

## Adding MicroEngine to an existing project

Any .NET 10 console project can become a MicroEngine game:

```bash
dotnet add package MicroEngine.Desktop --prerelease
```

Then subclass `Game`, give it a composition (`public static void Configure(HostBuilder host) =>
host.UseGame(() => new MyGame());`), start it with `DesktopLauncher.Run(MyGame.Configure, args)` from `Program.cs`,
and add the identity properties ([Name, id, version and icon](app-identity.md)) before packaging. A game that wants
SDL only references `MicroEngine.Sdl` instead and passes its own platform catalog
([Backends and platforms](../manual/backends.md)); it can be packaged for every target, the web included.

## Next

- [Your first game](first-game.md): create, run and understand a project.
- [Tutorial: Gem Garden](../tutorial/README.md): build a complete small game step by step.

Working on the engine itself rather than on a game? The repository's [contributing guide](../../CONTRIBUTING.md)
explains how to build and test it.

# Getting MicroEngine

MicroEngine is a code-first 2D game engine for .NET, open source under the MIT License
([github.com/NastMz/MicroEngine](https://github.com/NastMz/MicroEngine)). It is in Alpha ([SDK status](sdk.md)): the
engine is tested on six platforms, and its public API may still change between 0.x versions.

Only want to play the demos? They are on the [demo pages](/demos/), in the browser and as downloads ([Downloads](downloads.md)).

## What you need

- The [.NET 10 SDK](https://dotnet.microsoft.com/download), on Windows, Linux or macOS.
- For phones and browsers, the .NET workloads and tools of those targets; `microengine doctor` lists them
  ([Packaging](packaging.md)).

## Install the tool

The SDK is six packages on [nuget.org](https://www.nuget.org/packages/MicroEngine): the engine ([`MicroEngine`](https://www.nuget.org/packages/MicroEngine),
[`MicroEngine.Sdl`](https://www.nuget.org/packages/MicroEngine.Sdl), [`MicroEngine.Raylib`](https://www.nuget.org/packages/MicroEngine.Raylib),
[`MicroEngine.Desktop`](https://www.nuget.org/packages/MicroEngine.Desktop)), the `microengine` tool
([`MicroEngine.Cli`](https://www.nuget.org/packages/MicroEngine.Cli)) and the project template
([`MicroEngine.Templates`](https://www.nuget.org/packages/MicroEngine.Templates)). The current version is 0.4.0-alpha.1, an Alpha
pre-release, so NuGet commands need `--prerelease` (or the exact version).

```bash
dotnet tool install -g MicroEngine.Cli --prerelease
microengine version
```

`microengine new` installs the template at the tool's version the first time; to install it yourself:
`dotnet new install MicroEngine.Templates::0.4.0-alpha.1`. To update later: `dotnet tool update -g MicroEngine.Cli --prerelease`.

## Your first game

```bash
microengine new StarCourier --id com.yourstudio.starcourier --title "Star Courier"
cd StarCourier
microengine run
```

A window opens with a hero you move with the arrow keys, a gamepad or the on-screen controls, collecting coins.
The project is yours, with nothing from MicroEngine's repository in it:

```text
StarCourier/
  StarCourier.csproj    identity (name, id, version, icon, orientation) and the MicroEngine.Desktop package
  Program.cs            one line: DesktopLauncher.Run(new MainGame(), args)
  MainGame.cs           settings, shared assets (a sprite from Content, a sound made in code), the first scene
  Scenes/PlayScene.cs   input actions, movement, drawing
  Content/hero.png      asset files, read with ContentFolder
  icon.png              the game's icon
```

Then [Making a game](../making-a-game.md) and the [engine manual](/docs/manual/lifecycle/) explain scenes,
rendering, input, audio, assets, UI and saves.

## Build and distribute

```bash
microengine build                 # compile and check the identity
microengine package windows       # or linux, macos, android, ios, ios-simulator, web
microengine doctor                # what this machine can build
```

Each target, what it produces and what it needs: [Packaging](packaging.md). Keys for Android, macOS and iOS:
[Signing](signing.md). All commands: [the microengine tool](cli.md).

## An existing project

Add the engine to any .NET 10 console project with `dotnet add package MicroEngine.Desktop --prerelease`, then add
the [identity](app-identity.md) properties and package it with `microengine package`.

## Only SDL

A game that wants no raylib references `MicroEngine.Sdl` instead of `MicroEngine.Desktop` and passes its own
catalog: `GameLauncher.Run(new MainGame(), args, new PlatformCatalog().Add(SdlPlatform.Profile).Add(HeadlessPlatform.Profile))`.
It packages for every target except the web, whose platform is raylib.

## From the repository

To work on the engine itself, or to read the demos:

```bash
dotnet test MicroEngine.slnx -c Release
dotnet run --project samples/HelloEngine -c Release
dotnet run --project games/MeadowRun -c Release
```

The demos are there to play and learn from; they are not MIT licensed ([Licensing](licensing.md)).

## Licence

The engine is MIT: use it in personal and commercial games with no royalties, and ship it inside them with its
licence notice (every package `microengine` builds includes it). See [Licensing](licensing.md).

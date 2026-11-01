# Getting MicroEngine

MicroEngine is open source under the MIT License and lives in a public repository on GitHub:
[github.com/NastMz/MicroEngine](https://github.com/NastMz/MicroEngine). It is in Alpha ([SDK status](sdk.md)). There are
no published NuGet packages or project templates yet, so a game uses the engine from a copy of the repository.

Only want to play the demos? They are on the [demo pages](/demos/), in the browser and as downloads
([Downloads](downloads.md)).

## Requirements

- The [.NET 10 SDK](https://dotnet.microsoft.com/download).
- Windows, Linux or macOS to develop. Games run on Windows, Linux, macOS, Android, iOS and in browsers, each validated to
  the level stated in [Platforms](platforms.md).

## Build and run

```bash
git clone https://github.com/NastMz/MicroEngine.git
cd MicroEngine
dotnet test MicroEngine.slnx -c Release
dotnet run --project samples/HelloEngine -c Release
```

The demos run the same way, for example `dotnet run --project games/MeadowRun -c Release`. They are there to play
and to learn from; they are not MIT licensed (see [Licensing](licensing.md)).

## Start your own game

A game is a .NET console project that references the engine and the official desktop platforms (SDL 3 by default,
raylib as an alternative):

```xml
<ItemGroup>
  <ProjectReference Include="path/to/MicroEngine/src/MicroEngine.Desktop/MicroEngine.Desktop.csproj" />
</ItemGroup>
```

Copy `samples/HelloEngine` as a starting point (it is MIT licensed, so you may reuse it freely), then follow
[Making a game](../making-a-game.md). Keep the engine as a Git submodule or a copied folder: both work, and both
keep its licence file next to it.

To ship SDL only, reference `src/MicroEngine.Sdl` and pass your own catalog to `GameLauncher.Run`
([SDK status](sdk.md#packages)).

## Planned: SDK-1

NuGet packages (`MicroEngine`, `MicroEngine.Sdl`, `MicroEngine.Raylib`, `MicroEngine.Desktop`) and `dotnet new`
templates are prepared but not published ([SDK status](sdk.md#sdk-1-what-is-prepared)). The commands will be documented
here when they exist.

## Licence

The engine is MIT: use it in personal and commercial games with no royalties, and ship it inside them with its
licence notice. See [Licensing](licensing.md) for what that means in practice, and for the demo games' terms.

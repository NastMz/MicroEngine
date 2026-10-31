# Getting MicroEngine

MicroEngine is open source under the MIT License and lives in a public repository on GitHub:
[github.com/NastMz/MicroEngine](https://github.com/NastMz/MicroEngine). There are no NuGet packages or project
templates yet, so a game uses the engine from a copy of the repository.

## Requirements

- The [.NET 10 SDK](https://dotnet.microsoft.com/download).
- Windows 11 is the validated desktop system; Linux and macOS are checked automatically but not yet supported (see
  [Platforms](platforms.md)).

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

A game is a .NET console project that references the engine and its desktop backend:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/MicroEngine/src/MicroEngine.Desktop/MicroEngine.Desktop.csproj" />
</ItemGroup>
```

Copy `samples/HelloEngine` as a starting point (it is MIT licensed, so you may reuse it freely), then follow
[Making a game](../making-a-game.md). Keep the engine as a Git submodule or a copied folder: both work, and both
keep its licence file next to it.

## Planned

NuGet packages for the engine and its backend, C# project templates and versioned engine releases are planned; see
the [roadmap](/roadmap/). The commands will be documented here when they exist.

## Licence

The engine is MIT: use it in personal and commercial games with no royalties, and ship it inside them with its
licence notice. See [Licensing](licensing.md) for what that means in practice, and for the demo games' terms.

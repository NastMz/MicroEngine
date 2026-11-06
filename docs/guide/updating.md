# Updating the SDK

MicroEngine is in Alpha: new versions come often, and a new minor version (0.4 to 0.5) may change public APIs
([Versions and compatibility](sdk.md)). Updating a game is four steps; do them together, because the tool builds games
with the platform hosts of its own version.

## 1. See what is available

```bash
dotnet package search MicroEngine.Desktop --prerelease
microengine version
```

The [release notes](release-notes.md) say what each version adds, changes and fixes.

## 2. Update the tool and the template

```bash
dotnet tool update -g MicroEngine.Cli --prerelease
dotnet new install MicroEngine.Templates::0.5.0-alpha.1
```

Replace `0.5.0-alpha.1` with the version you are moving to. The template only matters for new projects:
`microengine new` installs the template of the tool's version when it is missing.

## 3. Update the game's packages

In the game's project file, set every `MicroEngine.*` package to the same version as the tool:

```xml
<PackageReference Include="MicroEngine.Desktop" Version="0.5.0-alpha.1" />
```

or let `dotnet` edit the file:

```bash
dotnet add package MicroEngine.Desktop --version 0.5.0-alpha.1
```

## 4. Read the migration notes, build and test

The [migration notes](../migrations.md) list every incompatible change with how to update your code, from the version
you leave to the one you reach. Then:

```bash
microengine build
microengine run
```

If the game has tests (see [Testing your game](../examples/testing.md)), run them before packaging.

## Pinning a version

A game always builds with the package versions in its project file, so updating the tool never changes an existing
game until you change those versions. To keep a game on a version while trying a new one elsewhere, install the tool
for that game only, as a local tool:

```bash
dotnet new tool-manifest
dotnet tool install MicroEngine.Cli --version 0.5.0-alpha.1
dotnet tool run microengine version
```

`dotnet tool run microengine` (or `dotnet microengine`) then uses the version recorded in the folder's tool manifest
(`dotnet-tools.json`, which you commit with the game), in that folder and the folders below it.

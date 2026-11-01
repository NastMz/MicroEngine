# 12. Build and package

Players do not run `microengine run`: they download a file, unpack or install it, and start the game. This step gives
Gem Garden its final identity and builds those files for Windows and Linux. [Step 13](13-other-targets.md) covers the
web, Android, macOS and iOS.

## The game's identity

Everything a system shows about the game (its name in the taskbar, its icon, its version, the id stores and phones know
it by) comes from the second group of the project file. For the release, set the version and a description:

<!-- code: samples/GemGarden/part4-complete/GemGarden.csproj -->
```xml title="GemGarden.csproj"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>GemGarden</RootNamespace>
  </PropertyGroup>

  <!-- The game's identity: every package `microengine package` builds (Windows, Linux, macOS, Android, iOS, web) takes
       its name, id, version and icon from here. https://github.com/NastMz/MicroEngine/blob/main/docs/guide/app-identity.md -->
  <PropertyGroup>
    <ApplicationTitle>Gem Garden</ApplicationTitle>
    <ApplicationId>com.example.gemgarden</ApplicationId>
    <Version>1.0.0</Version>
    <Description>Collect the gems of a walled garden before the sun sets.</Description>
    <MicroEngineIcon>icon.png</MicroEngineIcon>
    <MicroEngineOrientation>landscape</MicroEngineOrientation>
    <MicroEngineGameFactory>new GemGarden.MainGame()</MicroEngineGameFactory>
  </PropertyGroup>

  <ItemGroup>
    <!-- The engine with the desktop platforms (SDL 3 by default, raylib). A game that ships SDL only can reference
         MicroEngine.Sdl instead. -->
    <PackageReference Include="MicroEngine.Desktop" Version="0.4.0-alpha.1" />
  </ItemGroup>

  <ItemGroup>
    <!-- Asset files, copied next to the executable (ContentFolder reads them there). -->
    <None Include="Content/**" CopyToOutputDirectory="PreserveNewest" LinkBase="Content" />
  </ItemGroup>
</Project>
```

| Property | Gem Garden | Used for |
| --- | --- | --- |
| `ApplicationTitle` | `Gem Garden` | the name players see: Windows file description, Linux desktop entry, macOS and phone home screens, browser tab |
| `ApplicationId` | `com.example.gemgarden` | Android package name, Apple bundle id, Linux app id; never change it after release |
| `Version` | `1.0.0` | every package's version and file name |
| `Description` | one sentence | the package's README and the Linux desktop entry |
| `MicroEngineIcon` | `icon.png` | every system's icon, generated from one square PNG of 256, 512 or 1024 pixels |
| `MicroEngineOrientation` | `landscape` | phones: `landscape`, `portrait` or `any` |

Replace `icon.png` with your own art (1024x1024 is best: pixel art is enlarged without blur for the sizes each system
needs) and `com.example` with a domain you control. The build checks every value and stops with an error that names
the problem (an id with a hyphen, an icon that is not square). All the properties: [Name, id, version and icon](../guide/app-identity.md).

`microengine info` shows what the project declares:

```text
Title        Gem Garden
Id           com.example.gemgarden
Version      1.0.0
Description  Collect the gems of a walled garden before the sun sets.
Icon         ...\GemGarden\icon.png
Orientation  landscape
Factory      new GemGarden.MainGame()
Content      ...\GemGarden\Content\
Licence      (none)
Engine       MicroEngine 0.4.0-alpha.1, .NET 10.0.14
```

`Licence (none)`: a `LICENSE`, `LICENSE.txt` or `LICENSE.md` file next to the project is shipped in every package as
`LICENSE.txt`. Add one with the terms you want for your game. The engine's own MIT licence and the notices of everything
inside (the .NET runtime, SDL, raylib) are always included.

## Build

```bash
microengine build
```

compiles in Release and checks the identity. It is what CI should run on every change.

## Package for Windows and Linux

```bash
microengine package windows
microengine package linux
```

```text
Publishing Gem Garden for Windows 64-bit (x64)...
package: ...\GemGarden\dist\GemGarden-1.0.0-windows-x64.zip
Publishing Gem Garden for Linux 64-bit (x86_64, glibc 2.34+)...
package: ...\GemGarden\dist\GemGarden-1.0.0-linux-x64.tar.gz
```

Each package is one folder named after the game, about 35 MB compressed:

```text
GemGarden/
  GemGarden.exe            the game, with the .NET runtime inside: players need no .NET
  SDL3.dll, raylib.dll     the platforms (SDL 3 by default; raylib with --platform raylib)
  Content/                 hero.png, pickup.wav
  README.txt               how to run it, built from the identity
  build-info.json          game, version, SDK version, commit
  THIRD-PARTY-NOTICES.txt  and licenses/: MicroEngine's MIT licence, the .NET runtime's licence and notices
```

The Linux package has the same layout with a `GemGarden` executable, an `icon.png` and a `.desktop` entry. Unzip the
Windows package and double-click `GemGarden.exe`; extract the Linux one and run `./GemGarden/GemGarden`.

Where each package can be built and what players need:

| Target | Built on | Players need |
| --- | --- | --- |
| `windows` | Windows (only there can the executable get its icon and version) | Windows 10 or 11, 64-bit |
| `linux` | any system | x86_64 Linux with glibc 2.34 or newer (Ubuntu 22.04, Debian 12, Fedora 35 or later), X11 or Wayland |

The Windows executable is not code-signed by the tool, so SmartScreen may warn on first launch; signing it with your own
certificate is a separate step ([Signing and secrets](../guide/signing.md)). Everything about each target:
[Packaging a game](../guide/packaging.md).

## Distribute

The files in `dist/` are ready to upload wherever you share the game: your website, itch.io, a GitHub release. The
tool builds packages; it does not publish to stores.

## Next

[Step 13](13-other-targets.md) runs the same game on another platform, in a browser and on Android.

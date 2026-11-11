# 1. Create the project

Gem Garden starts as the official template. In this step you create it, run it, and look at what you will change.

## Create it

```bash
microengine new GemGarden --id com.example.gemgarden --title "Gem Garden"
cd GemGarden
microengine run
```

A window opens with the template's coin-collecting scene. Close it. The folder holds:

```text
GemGarden/
  GemGarden.csproj      project file and the game's identity
  Program.cs            starts the game on the desktop platforms
  MainGame.cs           the game class
  Scenes/PlayScene.cs   the template's scene (you replace it in step 2)
  Content/hero.png      the template's sprite (you replace it in step 3)
  icon.png              the game's icon
```

[Your first game](../guide/first-game.md) explains each of these files line by line; read it now if you have not.

## The project file

`GemGarden.csproj` was written by the template with your name, id and title:

<!-- code: samples/GemGarden/part1-scenes/GemGarden.csproj -->
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
    <Version>0.1.0</Version>
    <Description>A game made with MicroEngine.</Description>
    <MicroEngineIcon>icon.png</MicroEngineIcon>
    <MicroEngineOrientation>landscape</MicroEngineOrientation>
  </PropertyGroup>

  <ItemGroup>
    <!-- The engine with the desktop platforms (SDL 3 by default, raylib). A game that ships SDL only can reference
         MicroEngine.Sdl instead. -->
    <PackageReference Include="MicroEngine.Desktop" Version="0.5.0-alpha.2" />
  </ItemGroup>

  <ItemGroup>
    <!-- Asset files, copied next to the executable (ContentFolder reads them there). -->
    <None Include="Content/**" CopyToOutputDirectory="PreserveNewest" LinkBase="Content" />
  </ItemGroup>
</Project>
```

Three parts matter for now:

- `PackageReference` brings the engine: `MicroEngine.Desktop` includes the core (`MicroEngine`), the SDL 3 and raylib
  platforms and the desktop launcher.
- `None Include="Content/**"` copies every file under `Content/` next to the compiled game. That is where the game
  reads images and sounds at run time.
- The identity group (title, id, version, icon) is used by packaging in [step 12](12-packaging.md). You will not
  touch it before then.

`Program.cs` stays exactly as the template wrote it for the whole tutorial:

<!-- code: samples/GemGarden/part1-scenes/Program.cs -->
```csharp title="Program.cs"
using MicroEngine.Desktop;
using GemGarden;

// Runs the game on the desktop platforms: SDL 3 by default; --platform raylib, --list-platforms and the other launcher
// options work from the command line (microengine run -- --list-platforms). Browser, Android and iOS builds call the
// same MainGame.Configure, so whatever it composes reaches every platform.
return DesktopLauncher.Run(MainGame.Configure, args);
```

## Running and debugging

`microengine run` builds in Debug and starts the game; `microengine run -c Release` builds an optimised game. Options
after `--` go to the game itself:

```bash
microengine run -- --scale 2          # window at 2x the virtual size instead of 4x
microengine run -- --list-platforms   # which platforms this machine can run
```

You can also open `GemGarden.csproj` in Visual Studio, Rider or VS Code and run or debug it like any console
application. Build errors are ordinary C# compiler errors.

## Checkpoint

Nothing changed yet: the template runs. In the [next step](02-scenes.md) you replace its scene with a title screen and
the game's own scene.

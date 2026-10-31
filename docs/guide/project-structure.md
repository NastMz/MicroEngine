# Repository structure (internal)

This page is for engine developers with access to the private MicroEngine repository. The repository is a single .NET solution: an engine core, one backend, four games, a sample, their tests and a
separate browser host. This page maps the folders and then shows how a game project is organised, using the
real games as the reference.

## Repository layout

| Path | Contents |
| --- | --- |
| `src/MicroEngine/` | Backend-independent core: loop, scenes, input, graphics model, world, audio synthesis, UI, saves, achievements. No native dependencies |
| `src/MicroEngine.Sdl/` | SDL 3 backend: `SdlPlatform`, `SdlAudioDevice` |
| `src/MicroEngine.Raylib/` | raylib backend: `RaylibPlatform`, `RaylibAudioDevice`, `BrowserLauncher`, `BrowserSaveStorage` |
| `src/MicroEngine.Desktop/` | `DesktopLauncher` and the official desktop platforms; what games reference |
| `platforms/` | Android and iOS hosts ([README](../../platforms/README.md)) |
| `games/<Name>/` | A complete game: code, procedural art and audio, optional asset files, input scripts, README |
| `samples/HelloEngine/` | The smallest complete game ([walkthrough](../examples/hello-engine.md)) |
| `tests/MicroEngine.Tests/` | Engine contract tests (MSTest) |
| `tests/<Name>.Tests/` | Each game's rule tests and headless playthroughs |
| `web/` | Browser host, build scripts, pinned toolchain and browser tests ([web/README.md](../../web/README.md)) |
| `docs/` | Architecture, guides, manual, examples, reference, decision records and evaluations |
| `experiments/web/RaylibProbe/` | The WEB-0 feasibility probe; kept as a record, not part of the build |
| `MicroEngine.slnx` | The solution: engine, games, sample and tests (not the browser host) |
| `Directory.Build.props` | .NET 10, nullable, implicit usings, warnings as errors for every project |
| `global.json` | SDK selection (10.0.100, roll forward to the latest feature band) |

Build output (`bin/`, `obj/`) and generated packages (`dist/`) are not committed.

## Engine source

`src/MicroEngine` is organised by namespace:

| Folder | Namespace | Highlights |
| --- | --- | --- |
| `Core/` | `MicroEngine.Core` | `Game`, `GameSettings`, `IPlatform`, `GameRunner`, `GameLoop`, `GameSession`, `InputScript`, `HeadlessPlatform`, `SaveStore`, `ContentFolder`, achievements |
| `Scenes/` | `MicroEngine.Scenes` | `Scene`, `SceneManager`, `Transition`, coroutines and tweens |
| `Input/` | `MicroEngine.Input` | `Key`, `PadButton`, `IInputSource`, `InputMap` |
| `Graphics/` | `MicroEngine.Graphics` | `PixelImage`, `Art`, sprites and atlases, `Animator`, `Camera2D`, `ParallaxLayer`, particles, `LightMap`, `BitmapFont`, `IRenderer` |
| `World/` | `MicroEngine.World` | `TileLayer`, `TileGrid`, `TileMotion`, `Autotile`, `Entity`, `EntityList<T>` |
| `Audio/` | `MicroEngine.Audio` | `AudioClip`, `IAudioDevice`, `AudioMixer`, `Synth`, `Song` |
| `UI/` | `MicroEngine.UI` | `DialogueBox`, `SelectionList`, `Typewriter`, `UiTheme`, achievement banners and list |
| `Math/` | `MicroEngine` | `Color`, `RectF`, `MathX`, `Ease` |

Responsibilities and dependencies are summarised in [Components](../reference/components.md).

## A game project

The smallest game is one project file and one source file:

```text
samples/HelloEngine/
  HelloEngine.csproj     references src/MicroEngine.Desktop
  Program.cs             entry point, Game subclass, one scene
  hello.script           input script for a smoke run
```

[`samples/HelloEngine/HelloEngine.csproj`](../../samples/HelloEngine/HelloEngine.csproj):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/MicroEngine.Desktop/MicroEngine.Desktop.csproj" />
  </ItemGroup>
</Project>
```

The full games follow one shape, with folder names chosen per game:

| Part | Meadow Run | Courtyard Key | Trail Duel | Neon Rift |
| --- | --- | --- | --- | --- |
| Entry point | `Program.cs` | `Program.cs` | `Program.cs` | `Program.cs` |
| `Game` subclass | `MeadowGame.cs` | `CourtyardKeyGame.cs` | `TrailDuelGame.cs` | `RiftGame.cs` |
| Rules and entities | `Gameplay/` | `Entities/`, `Model/`, `World/` | `Rules/`, `World/` | `Gameplay/` |
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

Games that load files keep them under `Content/` and copy them next to the executable, so `ContentFolder` finds
them at run time. From [`games/NeonRift/NeonRift.csproj`](../../games/NeonRift/NeonRift.csproj):

```xml
<ItemGroup>
  <None Include="Content/**" CopyToOutputDirectory="PreserveNewest" LinkBase="Content" />
</ItemGroup>
<ItemGroup>
  <InternalsVisibleTo Include="NeonRift.Tests" />
</ItemGroup>
```

`InternalsVisibleTo` lets the game's test project reach internal rules without making them public.

### Entry point

`Program.cs` stays minimal: build the game, hand it to the launcher. Developer shortcuts come from environment
variables because the launcher rejects unknown arguments. From
[`games/TrailDuel/Program.cs`](../../games/TrailDuel/Program.cs):

```csharp
// TRAIL_SAVE_DIR (optional) keeps saves and achievements in another folder, e.g. for capture runs.
var saveDir = Environment.GetEnvironmentVariable("TRAIL_SAVE_DIR");
return DesktopLauncher.Run(new TrailDuelGame(string.IsNullOrWhiteSpace(saveDir) ? null : saveDir), args);
```

Keeping it this small is also what lets the browser host build the same game: `web/games.props` repeats the
constructor expression ([Configuration](../reference/configuration.md#web-build-options)).

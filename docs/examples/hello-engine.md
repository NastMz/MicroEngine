# Hello Engine walkthrough

Hello Engine is the smallest complete MicroEngine game: a procedural star sprite you move with the
arrow keys, a synthesized sound and a particle burst on confirm, and a looping song. It is one file of about 65
lines, and every concept it uses scales up unchanged to the four full games. This page walks through it top to
bottom.

Source: [`samples/HelloEngine/Program.cs`](../../samples/HelloEngine/Program.cs).

## Controls

Arrows (or WASD) move the star; Z or Space plays a sound and bursts particles. The sample lives in the engine's repository
(`samples/HelloEngine`), is MIT licensed like the engine, and may be copied into your own games; this page shows its
code in full. Its project file is a plain console project that references the `MicroEngine.Desktop` package, so the
folder builds anywhere:

<!-- code: samples/HelloEngine/HelloEngine.csproj -->
```xml title="HelloEngine.csproj"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MicroEngine.Desktop" Version="0.4.0-alpha.2" />
  </ItemGroup>
</Project>
```

```bash
microengine run --project samples/HelloEngine
```

## 1. Entry point

```csharp
using System.Numerics;
using MicroEngine;
using MicroEngine.Audio;
using MicroEngine.Audio.Retro;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Desktop;
using MicroEngine.Scenes;

return DesktopLauncher.Run(new HelloGame(), args);
```

The synthesizer (`Synth`, `SfxParams`, `Wave`, `Song`, `Instrument`) is in the `MicroEngine.Audio.Retro` namespace.

The program is a top-level statement. `DesktopLauncher.Run` parses the launcher flags (`--script`, `--hidden`,
`--captures`, ...), creates a window on the default platform (SDL) sized from the game's settings, runs the loop and returns a process
exit code: 0 on success, 1 on a game error, 2 on bad arguments.

## 2. The game class

```csharp
internal sealed class HelloGame : Game
{
    public override GameSettings Settings { get; } = new("Hello MicroEngine") { Background = Color.Hex("1d2b53") };
    public SpriteAtlas Atlas { get; private set; } = null!;
    // ...
    protected override void Unload() => Atlas.Dispose();
}
```

- `Settings` declares the window title and, through the record's defaults, a 320x180 virtual canvas drawn at
  scale 4 and simulated at 60 updates per second. `Background` is the clear colour of every frame.
- `Atlas` holds the textures built in `Load`; `Unload` releases them when the game shuts down.

## 3. Loading content once

```csharp
protected override void Load()
{
    var palette = Art.Palette("o=#000000", "y=#ffec27", "w=#fff1e8");
    var star = PixelImage.FromMap(palette,
        "....o....",
        "...oyo...",
        "ooooyoooo",
        "oyyywyyyo",
        ".oyyyyyo.",
        "..oyyyo..",
        ".oyyoyyo.",
        "oyyo.oyyo",
        "ooo...ooo");
    Atlas = new SpriteAtlasBuilder().Add("star", star, new Vector2(4.5f, 4.5f)).Build(Graphics);
    Audio.AddSound("ding", Synth.Render(new SfxParams { Wave = Wave.Square, Frequency = 880, Arpeggio = [1f, 1.5f], Sustain = 0.05f, Decay = 0.2f }));
    Audio.AddMusic("theme", new Song(120).Track(Instrument.Lead, "C5 - E5 - G5 - E5 - | C5 - - - . . . .").Track(Instrument.Bass, "C3 . C3 . G2 . G2 .").Drums("k . h . s . h .").Render());
    Scenes.Push(new HelloScene(this));
}
```

`Load` runs once, after the platform exists. It is the place to build every asset; never build art or sounds per
frame.

| Line | What it does |
| --- | --- |
| `Art.Palette(...)` | Maps characters to colours; characters not in the palette, such as `.`, stay transparent |
| `PixelImage.FromMap` | Builds a CPU image from text rows, one character per pixel |
| `SpriteAtlasBuilder().Add("star", star, origin)` | Packs the image into a texture; the origin (4.5, 4.5) is the sprite's centre, used as its pivot |
| `.Build(Graphics)` | Uploads the atlas through the platform's `IGraphicsDevice` |
| `Synth.Render(new SfxParams {...})` | Synthesizes a square-wave chime with a two-step arpeggio into an `AudioClip` |
| `new Song(120).Track(...).Drums(...).Render()` | Renders a 120 BPM step-sequenced song with lead, bass and drums |
| `Scenes.Push(new HelloScene(this))` | Puts the first scene on the stack |

## 4. The scene

```csharp
internal sealed class HelloScene(HelloGame game) : Scene
{
    private readonly ParticleSystem _particles = new();
    private Vector2 _position = new(160, 100);

    public override void Enter() => Audio.PlayMusic("theme");
    // ...
}
```

A `Scene` is one screen of the game. It reaches the game's `Input`, `Audio` and `Font` directly. `Enter` runs when
the scene becomes active; here it starts the music.

### Update

```csharp
public override void Update(float dt)
{
    var move = new Vector2(Input.Axis("left", "right"), Input.Axis("up", "down"));
    _position += move * 90 * dt;
    if (Input.Pressed("confirm"))
    {
        Audio.Play("ding", pitchJitter: 0.05f);
        _particles.Emit(_position, new ParticleSpec { StartColor = Color.Hex("ffec27"), EndColor = Color.Hex("ff004d00"), Gravity = 80 }, 24);
    }
    _particles.Update(dt);
}
```

- `Update` runs at the fixed rate; `dt` is always 1/60 of a second by default.
- Input is read by action name, never by key. `Axis("left", "right")` returns -1, 0 or 1. The default
  `InputMap` binds `left`/`right`/`up`/`down` to arrows, WASD, the d-pad and the left stick, and `confirm` to
  Z, Space, Enter, J and pad A.
- `Pressed` is true for exactly one update per press.
- `pitchJitter` varies each play slightly so repeated sounds do not feel mechanical.

### Draw

```csharp
public override void Draw(IRenderer r)
{
    Font.DrawCentered(r, "Hello, MicroEngine!", new Vector2(160, 40), Color.Hex("fff1e8"), 2, Color.Hex("000000"));
    Font.DrawCentered(r, "Arrows move · Z/Space bursts", new Vector2(160, 160), Color.Hex("c2c3c7"));
    _particles.Draw(r);
    r.Draw(game.Atlas["star"], _position + new Vector2(0, MathX.Wave(Age, 1.5f, 2)));
}
```

Drawing is immediate-mode in virtual pixels (320x180 here). The built-in font needs no asset. `Age` is the
seconds this scene has been updated, and `MathX.Wave` turns it into a gentle bob. The backend scales the canvas
to the window by whole factors and letterboxes it.

## 5. A scripted smoke run

[`samples/HelloEngine/hello.script`](../../samples/HelloEngine/hello.script) drives the same game without a
person:

```text
# Scripted run: move, burst, capture a screenshot, quit
20 hold right 30
60 press confirm
64 capture hello.png
70 quit
```

Started with `--script hello.script --hidden --mute --captures captures`, the desktop launcher plays it in a hidden
window without sound and writes `hello.png` to `captures/`.

Each line is an update number and a command; the script forces actions through the same `InputMap` the game
reads. See [Testing](testing.md).

## Where next

- [The tutorial](../tutorial/README.md) builds a complete game step by step.
- [Usage patterns](patterns.md) shows how the four games organise the same pieces at scale.
- [Lifecycle](../manual/lifecycle.md) and [Scenes](../manual/scenes.md) explain the loop and the stack.

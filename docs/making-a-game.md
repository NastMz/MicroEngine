# Making a game with MicroEngine

This guide builds a game step by step, from the project file to tests and a browser build. The finished minimal
version is the [Hello Engine walkthrough](examples/hello-engine.md); the four games built with the engine show
every system at full scale.

## 1. Project

A game project references the engine's projects directly, as below, from a copy of the engine's repository
([Getting MicroEngine](guide/getting-microengine.md)). Package-based setup (NuGet packages and project templates) is
planned. The rest of this guide shows the real API and does not depend on how the project is set up.

```xml
<!-- games/MyGame/MyGame.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType></PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/MicroEngine.Raylib/MicroEngine.Raylib.csproj" />
  </ItemGroup>
</Project>
```

In the engine's repository a shared `Directory.Build.props` sets .NET 10, nullable and warnings-as-errors.

```csharp
// Program.cs
return MicroEngine.Raylib.DesktopLauncher.Run(new MyGame(), args);
```

## 2. The game class

```csharp
sealed class MyGame : Game
{
    public override GameSettings Settings { get; } = new("My Game") { Background = Color.Hex("1d2b53") };
    public SpriteAtlas Atlas { get; private set; } = null!;

    protected override void Load()
    {
        Atlas = MyArt.Build(Graphics);   // build every sprite once
        MySounds.Register(Audio);         // synthesize every sound once
        Scenes.Push(new TitleScene(this));
    }

    protected override void Unload() => Atlas.Dispose();
}
```

`Load` runs once the window exists. Build art, sounds and music there, never per frame.

## 3. Art as code

```csharp
var pal = Art.Palette("o=#2b1d2f", "g=#5fcd5f", "d=#2f8f4a");
var bush = PixelImage.FromMap(pal,
    "..oooo..",
    ".oggggo.",
    "oggdggdo",
    "ogggdggo",
    ".oddddo.");
var atlas = new SpriteAtlasBuilder()
    .Add("bush", bush, origin: new Vector2(4, 5))   // origin = pivot, e.g. the feet
    .AddFrames("walk", walkFrames)                  // walk0, walk1, ...
    .Build(Graphics);
```

`PixelImage` also offers `FillRect`, `Line`, `FillCircle`, `Outline`, `Blit`, `Map`, `FlipX/FlipY`,
`Rotate90` and `Silhouette` (white copies for hit flashes); `Art.ValueNoise`, `Art.Dither` and `Art.Ramp`
produce textured tiles with dithered shading.

Drawing tips: `r.Draw(sprite, pos)` places the sprite's origin at `pos`; `r.DrawPivoted(sprite, pivot,
rotation, scale)` rotates and squashes around that origin; `r.FillEllipse` draws soft shadows.
Avoid naming a game folder/namespace `Art` or `Audio`: it hides the engine types of the same name.

### Art from files

Games that use existing packs keep the files under `games/<Name>/Content`, copy them to the output
(`<None Include="Content/**" CopyToOutputDirectory="PreserveNewest" />`) and list their licences in a
`CREDITS.md`:

```csharp
var content = new ContentFolder();                       // <exe dir>/Content, clear errors if missing
var atlas = new SpriteAtlasBuilder()
    .AddGrid("hero_run", content.Image("hero/run.png"), 71, 67, origin: new Vector2(31, 66))  // strip → hero_run0..7
    .Add("tile", content.Image("tiles.png").Crop(96, 176, 16, 16))
    .Build(Graphics);
Audio.AddSoundFile("shot", content.PathOf("sfx/shot.wav"));     // PCM WAV, decoded in managed code
Audio.AddMusicFile("stage", content.PathOf("music/stage.ogg")); // OGG/MP3 streamed by the backend
var sky = new ParallaxLayer(Graphics.CreateTexture(content.Image("sky.png"))) { Factor = 0.2f };
sky.Draw(r, camera.Position);                            // screen space, repeats horizontally
```

`PixelImage.Rotated`, `Crop` and `ClearRect` help derive extra poses from authored frames (Neon Rift
rotates the gun arm of its hero to aim in eight directions).

### Lighting

```csharp
var lights = new LightMap(Graphics) { Darkness = 0.8f, Shade = Color.Hex("101028") };   // create once
// every frame, after drawing the world:
lights.Clear();
lights.Add(hero.Center, radius: 48, brightness: 0.9f);                 // lifts the darkness
lights.Add(torch.Position, 40, 1f, glow: Color.Hex("ffb347"));        // ...and adds a halo
lights.Draw(r, camera);
```

`Darkness = 1` with glow-only lights (`brightness: 0`) gives a flat colour grade plus halos. Pass your own
glow image to the constructor to match your art style (Meadow Run uses banded pixel halos).

## 4. Scenes

```csharp
sealed class PlayScene(MyGame game) : Scene
{
    public override void Enter() => Audio.PlayMusic("theme");

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) Game.Scenes.Push(new PauseScene());   // overlay
        if (won) Game.Scenes.Replace(new WinScene(), Transition.Iris(playerScreenPos));
    }

    public override void Draw(IRenderer r)
    {
        r.PushCamera(camera);
        // world
        r.PopCamera();
        Font.DrawShadowed(r, "SCORE 100", new(4, 4), Color.White, Color.Black);  // HUD in screen space
    }
}
```

Overlay scenes return `IsOverlay => true`; they draw over the scene below, which is frozen unless
`BlocksUpdateBelow` is false. Use `Coroutines.Start(...)` for sequences (cutscenes, item-get pauses)
and `Tweens.Add(...)` for eased values.

## 5. Input

Default actions: `left right up down` (arrows/WASD/d-pad/stick), `confirm` (Z/Space/Enter/J/A),
`cancel` (X/Backspace/K/B), `action` (C/L), `pause` (Esc/P/Start). Override `CreateInput()` to add
your own with `InputMap.Bind`. Use `Pressed` for edges, `Down` for holds, `Axis` for directions.

## 6. Worlds and collision

```csharp
var layer = TileLayer.Parse(levelRows, new Dictionary<char, int> { ['#'] = 1, ['='] = 2 });
var grid = new TileGrid(layer, 16, new Dictionary<int, TileFlags> { [1] = TileFlags.Solid, [2] = TileFlags.OneWay });
var result = TileMotion.Move(player.Bounds, velocity * dt, grid, movingPlatforms);
if (result.HitFloor) grounded = true;
```

`TileMotion` resolves X then Y in sub-steps so fast bodies do not tunnel; extra solid rectangles
(platforms, push blocks, doors) take part in the same sweep. `TileLayer.Find(rows, 'E')` locates spawn
markers in the same ASCII map.

## 7. Sound and music

```csharp
Audio.AddSound("jump", Synth.Render(new SfxParams { Wave = Wave.Square, Frequency = 300, EndFrequency = 700, Decay = 0.15f }));
Audio.AddMusic("theme", new Song(132)
    .Track(Instrument.Lead, "E5 - G5 - | C6 - - . | ...")
    .Track(Instrument.Bass, "C3 . C3 . G2 . G2 .")
    .Drums("k . h . s . h .")
    .Render());
Audio.Play("jump", pitchJitter: 0.05f);   // TryPlay returns false for unregistered cues
Audio.PlayMusic("theme");   // fades the current track out, then the new one in
```

## 8. Achievements

Declare a catalog, turn the system on in `Load`, and report events where they already happen:

```csharp
public static class MyAchievements
{
    public static readonly Achievement[] Catalog =
    [
        new("first-win", "First Victory", "Win your first battle."),          // immediate (goal 1)
        new("ten-wins", "Veteran", "Win 10 battles.", Goal: 10),               // progressive
        new("explorer", "Explorer", "Visit every area.", Goal: Areas.Count),   // reported as a count
    ];
}

protected override void Load()
{
    var store = new SaveStore("MyGame");                     // the game's own folder: isolated from other games
    UseAchievements(MyAchievements.Catalog, store, myTheme); // banners use your UiTheme
    Achievements.Unlocked += _ => Audio.Play("jingle");      // optional sound
    ...
}

// in gameplay code:
Game.Achievements.Unlock("first-win");                 // true only the first time, then ignored
Game.Achievements.AddProgress("ten-wins");             // unlocks on reaching the goal
Game.Achievements.SetProgress("explorer", visited);    // absolute count, never goes down

// in a menu:
Game.Scenes.Push(new AchievementsScene(Game.Achievements, myTheme));
```

Your own progress goes through the same store: `store.Save("progress", data)` writes the whole slot at once and
returns false when the disk or browser storage refuses the write (keep playing; the progress stays in memory),
and `store.Load<T>("progress")` returns null for a missing or unreadable slot. A slot that exists but could not
be read is never overwritten by that store, so a passing glitch cannot replace real progress with a fresh one.

Each achievement unlocks and shows its banner exactly once, across sessions. Ids are the saved keys: rename
and reword freely, but keep ids stable. Adding achievements later is safe (they start locked); missing or
corrupt files start fresh, and `SaveStore` keeps a corrupt file as `<slot>.corrupt.json` for recovery. Unknown ids throw, so a typo fails your tests instead of silently doing nothing.
`AchievementToasts.Top` moves banners below a HUD; `AchievementsScene` takes `Title`, `CloseActions` and
`Moved`/`Closed` events to match your menus. Progress is written about once a second and on exit; unlocks
are written at once.

## 9. Testing and captures

```csharp
var game = new MyGame();
var platform = new HeadlessPlatform();
game.InitializeForTest(platform);
platform.Keys.Set(Key.Right, true);
for (int i = 0; i < 60; i++) game.StepForTest();
```

For end-to-end checks write an input script and play it through the real game:

```text
# scripts/smoke.script
30 press confirm
60 hold right 120
200 capture level.png
240 quit
```

A game started through `DesktopLauncher` plays the script when given `--script scripts/smoke.script`; adding
`--hidden --mute --captures captures` runs it without a visible window or sound and writes the captures to
`captures/` ([Backends](manual/backends.md#desktop-raylib)).

Look at the captures. Passing tests are necessary, not sufficient.

## 10. In a browser

A game that runs on desktop also runs in a browser without code changes, as long as it reads files through
`ContentFolder` and saves through `SaveStore`. The shared browser host builds the game with the same constructor
its desktop `Program.cs` uses. Producing browser packages is not available to external developers yet; how a
package behaves once built is described in [Embedding](examples/embedding.md).

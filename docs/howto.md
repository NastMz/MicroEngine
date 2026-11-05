# How do I...?

Short answers to the questions people ask most, each with working code and a link to the full explanation. The code is
quoted from the [tutorial's game](tutorial/README.md) and the [focused examples](examples/focused.md), which build and
run against the published SDK.

## ...create a scene?

Derive from `Scene`, implement `Update` and `Draw`, and put it on the stack with `Game.Scenes`:

<!-- code: samples/GemGarden/part1-scenes/Scenes/TitleScene.cs -->
```csharp title="Scenes/TitleScene.cs"
using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace GemGarden.Scenes;

/// <summary>The title screen: the game's name until the player presses confirm.</summary>
public sealed class TitleScene(MainGame game) : Scene
{
    public override void Update(float dt)
    {
        if (Input.Pressed("confirm"))
            Game.Scenes.Replace(new PlayScene(game), Transition.Fade());
    }

    public override void Draw(IRenderer r)
    {
        Font.DrawCentered(r, "GEM GARDEN", new Vector2(160, 60), Color.Hex("a7f070"), scale: 3, shadow: Color.Hex("257179"));
        Font.DrawCentered(r, "PRESS CONFIRM", new Vector2(160, 120), Color.White);
    }
}
```

Start the first one from `Game.Load` with `Scenes.Push(new TitleScene(this))`. `Push` adds a scene on top (a pause
menu), `Pop` removes it, `Replace` swaps the top scene, `Reset` empties the stack; each takes an optional transition.
[Scenes](manual/scenes.md), [tutorial step 2](tutorial/02-scenes.md).

## ...draw a texture?

Make a `PixelImage` (in code or from a PNG), upload it with `Game.Graphics.CreateTexture`, draw it, and dispose it when
you are done:

<!-- code: samples/Examples/Examples/ShapesExample.cs#texture -->
```csharp
private Texture _checker = null!;

public override void Enter()
{
    // A PixelImage is pixels in memory; CreateTexture uploads it to the graphics device once.
    var image = new PixelImage(16, 16).Map((x, y, _) => (x / 4 + y / 4) % 2 == 0 ? Color.Hex("ffcd75") : Color.Hex("b13e53"));
    _checker = Game.Graphics.CreateTexture(image);
}

// A texture you create is yours to release.
public override void Exit() => _checker.Dispose();
```

<!-- code: samples/Examples/Examples/ShapesExample.cs#draw-texture -->
```csharp
r.Draw(_checker, new Vector2(16, 120));                                                   // whole, at its top-left
r.DrawStretched(_checker, _checker.Bounds, new RectF(48, 116, 64, 24), Color.White);     // stretched into a rectangle
r.Draw(_checker, new RectF(0, 0, 8, 8), new Vector2(124, 124), Color.Hex("73eff7"));    // part of it, tinted
r.Draw(_checker, _checker.Bounds, new Vector2(176, 128), Color.White,
    new DrawOptions { Rotation = Age, Origin = new Vector2(8, 8), Scale = new Vector2(2) }); // rotated around its centre

r.SetBlendMode(BlendMode.Additive);                                                      // light adds up
r.FillCircle(new Vector2(250, 128), 14, Color.Hex("b13e53"));
r.FillCircle(new Vector2(266, 128), 14, Color.Hex("3b5dc9"));
r.SetBlendMode(BlendMode.Alpha);                                                         // back to normal
```

For game art, prefer sprites in an atlas (next answer): one texture for many images. [Graphics](manual/rendering.md).

## ...load a sprite?

Keep the PNG under `Content/` (the template's project file copies it next to the executable), load it with
`ContentFolder`, and put it in an atlas. A sheet of equal cells becomes numbered frames with `AddGrid`:

<!-- code: samples/Examples/Examples/SpritesExample.cs#load -->
```csharp
private SpriteAtlas _atlas = null!;

public override void Enter()
{
    // Content/hero.png is a 64x16 strip of four 16x16 frames, copied next to the executable by the project file.
    var content = new ContentFolder();
    var sheet = content.Image("hero.png");
    _atlas = new SpriteAtlasBuilder()
        .AddGrid("hero", sheet, 16, 16, origin: new Vector2(8, 16))      // hero0..hero3, origin at the feet
        .Add("head", sheet.Crop(0, 0, 16, 9), new Vector2(8, 9))         // part of the image
        .Build(Game.Graphics);                                           // one texture for all of them
}

public override void Exit() => _atlas.Dispose();
```

Then `r.Draw(atlas["hero0"], position)` draws it with its origin at the position. [Assets](manual/assets.md),
[tutorial step 3](tutorial/03-drawing.md).

## ...make the character walk?

Read a direction from the movement actions, multiply by a speed in pixels per second and by `dt`, and play a walk
animation while moving:

<!-- code: samples/GemGarden/part2-hero/World/Player.cs#update -->
```csharp title="World/Player.cs"
public void Update(float dt, InputMap input, RectF area)
{
    // Keys give unit directions (diagonals normalised); a stick or the touch joystick also gives how far it is pushed.
    var direction = input.Vector("left", "right", "up", "down");
    float speed = input.Down("run") ? RunSpeed : WalkSpeed;
    var moved = _bounds.Offset(direction * speed * dt);

    // Stay inside the area: the next part replaces this with collisions against the garden's hedges.
    _bounds = moved with
    {
        X = Math.Clamp(moved.X, area.Left, area.Right - moved.Width),
        Y = Math.Clamp(moved.Y, area.Top, area.Bottom - moved.Height),
    };

    if (direction.X != 0) _facingLeft = direction.X < 0;
    _animator.Play(direction == Vector2.Zero ? "idle" : "walk");
    _animator.Speed = speed / WalkSpeed;
    _animator.Update(dt);
}
```

To stop at walls, move with `TileMotion.Move` through a tile grid instead of clamping
([tutorial step 7](tutorial/07-collisions.md)). [Tutorial step 4](tutorial/04-input.md),
[step 5](tutorial/05-animation.md).

## ...set up a gamepad?

Nothing to do for the defaults: the d-pad and left stick press `left`/`right`/`up`/`down`, A is `confirm`, B `cancel`,
X `action`, Start `pause`. Add your own actions in `Game.CreateInput`:

<!-- code: samples/GemGarden/part4-complete/MainGame.cs#input -->
```csharp title="MainGame.cs"
/// <summary>The default actions (arrows, WASD, d-pad, left stick, confirm, cancel, pause) plus "run".</summary>
protected override InputMap CreateInput() => InputMap.CreateDefault()
    .Bind("run", [Key.LeftShift, Key.X], [PadButton.B, PadButton.RightShoulder]);

/// <summary>On touch screens: a floating joystick on the left, A (run, and confirm in menus) and pause on the right.</summary>
protected override TouchLayout CreateTouchLayout() => TouchLayout.Of(
    new TouchStick { Offset = new(104, 100) },
    new TouchButton("run", "run", "A") { Anchor = TouchAnchor.BottomRight, Offset = new(72, 92), Radius = 40, Also = ["confirm"] },
    new TouchButton("pause", "pause", "II") { Anchor = TouchAnchor.TopRight, Offset = new(30, 30), Radius = 20 });
```

Analog triggers and the right stick bind with `BindAxis` and read with `Input.Value`:

<!-- code: samples/Examples/Examples/GamepadExample.cs#bind -->
```csharp
public override void Enter()
{
    // Actions can be bound at any time; usually a game does it once in Game.CreateInput.
    Input.Bind("boost", [Key.LeftShift], [PadButton.A, PadButton.RightShoulder])
         .BindAxis("throttle", PadAxis.RightTrigger)                 // 0..1 as the trigger is pressed
         .BindAxis("throttle", PadAxis.RightY, positive: false);     // or the right stick pushed up
}

public override void Exit() => Input.Unbind("boost").Unbind("throttle");
```

[Input](manual/input.md), [gamepad example](examples/focused.md#gamepad-and-analog-input).

## ...add touch controls?

Every game already has on-screen controls for its default actions, shown on touch screens. To choose your own, override
`CreateTouchLayout` (the `CreateTouchLayout` in the previous answer is Gem Garden's: a floating joystick and two
buttons). Controls press actions, so the game reads them like keys. Options: d-pad or joystick, toggle buttons,
anchors and sizes in density-independent pixels, controls per scene (`Scene.TouchActions`).
[Input, touch controls](manual/input.md#touch-controls), [touch example](examples/focused.md#touch-controls).

## ...play a sound?

Register it once by name (a WAV file, or a sound made in code), then play it by name from anywhere:

<!-- code: samples/Examples/Examples/SoundExample.cs#register -->
```csharp
public override void Enter()
{
    // Register each sound once by name (a game does this in Load); playing it later is cheap.
    if (!Audio.HasSound("laser"))
    {
        Audio.AddSound("laser", Synth.Render(new SfxParams { Wave = Wave.Saw, Frequency = 900, EndFrequency = 200, Decay = 0.25f }));
        Audio.AddSoundFile("chime", new ContentFolder().PathOf("pickup.wav"));
        Audio.AddMusic("loop", new Song(126)
            .Track(Instrument.Lead, "A4 - C5 - E5 - C5 - | G4 - B4 - D5 - B4 -")
            .Track(Instrument.Bass, "A2 . A2 . A2 . A2 . | G2 . G2 . G2 . G2 .")
            .Drums("k . h . s . h . k k h . s . h .")
            .Render());
    }
}

public override void Exit() => Audio.StopMusic();
```

<!-- code: samples/Examples/Examples/SoundExample.cs#play -->
```csharp
public override void Update(float dt)
{
    if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
    if (Input.Pressed("confirm")) Audio.Play("laser", pitchJitter: 0.1f);                    // a little variation
    if (Input.Pressed("action")) Audio.Play("chime", pan: Game.Random.NextSingle());        // 0 left .. 1 right
    if (Input.Pressed("cancel"))
    {
        if (Audio.CurrentMusic is null) Audio.PlayMusic("loop");
        else Audio.StopMusic(fadeSeconds: 1);
    }
    Audio.MusicVolume += Input.Axis("left", "right") * dt;                                  // clamped to 0..1
}
```

Music: WAV or OGG files on every platform, MP3 only on raylib, or songs made in code with `Song`. [Audio](manual/audio.md).
Next SDK version (not published yet: arrives with the next SDK version): `Synth`, `SfxParams`, `Wave`, `Song` and
`Instrument` are in `MicroEngine.Audio.Retro` (`using MicroEngine.Audio.Retro;`); in the published SDK they are in
`MicroEngine.Audio`.

## ...draw text?

Every scene has `Font`, the built-in pixel font:

<!-- code: samples/Examples/Examples/TextExample.cs#text -->
```csharp
Font.Draw(r, "Plain text, upper and lower case: 0123456789 !?.,:;()+-*/", new Vector2(8, 20), Color.White);
Font.DrawShadowed(r, "Shadowed text reads on any background", new Vector2(8, 32), Color.Hex("ffcd75"), Color.Black);
Font.DrawOutlined(r, "Outlined", new Vector2(8, 44), Color.White, Color.Hex("b13e53"));
Font.Draw(r, "x2", new Vector2(70, 40), Color.Hex("a7f070"), scale: 2);
Font.DrawCentered(r, "CENTRED ON A POINT", new Vector2(240, 48), Color.Hex("73eff7"), shadow: Color.Black);

// Measure: the size of a string, for boxes and right alignment.
var score = $"SCORE {Game.UpdateCount}";
Font.Draw(r, score, new Vector2(312 - Font.MeasureWidth(score), 62), Color.White);

// Wrap: lines that fit a width; LineHeight spaces them.
var box = new RectF(8, 78, 180, 60);
r.Panel(box, MicroEngine.UI.UiTheme.Default.Panel);
var lines = Font.Wrap(Story, (int)box.Width - 12);
for (int i = 0; i < lines.Count; i++)
    Font.Draw(r, lines[i], new Vector2(box.X + 6, box.Y + 6 + i * (Font.LineHeight + 1)), Color.White);
```

It covers ASCII and a few symbols. Next SDK version (not published yet: arrives with the next SDK version): load a TTF or
OTF font from `Content/` and draw it at any size:

```csharp
var font = Assets.Font("fonts/Inter-Regular.ttf");     // in Load
font.Draw(r, "Score 1200", new Vector2(24, 24), 32, Color.White);
```

[Text](manual/text.md#scalable-fonts-ttf-and-otf).

## ...make an HD game?

Next SDK version (not published yet: arrives with the next SDK version). Choose an HD canvas and the high-resolution presentation, put
images, atlases and fonts under `Content/`, and load them through `Assets`:

```csharp
public sealed class MainGame : Game
{
    public override GameSettings Settings { get; } = new("My HD Game", 1280, 720, 1) { Presentation = Presentation.HighResolution2D };

    protected override void Load()
    {
        var hero = Assets.Atlas("atlases/hero.json");            // exported from TexturePacker or Aseprite
        var font = Assets.Font("fonts/Inter-Regular.ttf");
        font.Prepare(ScalableFont.Ascii, 24, 48);
        Scenes.Push(new PlayScene());
    }
}
```

Textures default to linear sampling and the canvas fills any window. [HD canvases](manual/rendering.md#hd-canvases-and-smooth-scaling),
[Assets](manual/assets.md); the [Asset Gallery](../samples/AssetGallery/GalleryScene.cs) sample is a complete one.

## ...save the game?

Use a `SaveStore` with your game's id and save any JSON-serializable class to a named slot:

<!-- code: samples/Examples/Examples/SaveExample.cs#use -->
```csharp
private SaveStore _store = null!;
private Progress _progress = null!;
private string _status = "";

public override void Enter()
{
    // One store per game: the id names the save folder (or the browser's storage keys).
    _store = new SaveStore("MicroEngineExamples", saveDirectory);
    _progress = _store.Load<Progress>("example") ?? new Progress();   // null: no save yet, or unreadable
    _status = _store.Exists("example") ? "LOADED" : "NO SAVE YET";
}

public override void Update(float dt)
{
    if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
    if (Input.Pressed("right")) _progress.Coins++;
    if (Input.Pressed("confirm"))
    {
        _progress.SavedAt = DateTime.UtcNow;
        _progress.Visited.Add($"room {_progress.Visited.Count + 1}");
        // Save writes the whole slot at once; false means the disk or the browser refused (keep playing).
        _status = _store.Save("example", _progress) ? "SAVED" : "COULD NOT SAVE";
    }
    if (Input.Pressed("cancel"))
    {
        _store.Delete("example");
        _progress = new Progress();
        _status = "DELETED";
    }
}
```

Saves go to the player's data folder on desktops and phones and to `localStorage` in browsers, with no code change.
[Saving data](manual/persistence.md), [tutorial step 11](tutorial/11-saving.md).

## ...build for Android?

Install the `android` workload, an Android SDK and Java 17+ (`microengine doctor android` says what is missing), then:

```bash
microengine package android --debug-key     # a test APK for your own devices
microengine package android                 # signed with your key: ANDROID_KEYSTORE and its passwords
```

The APK targets Android 7.0 and later, arm64 phones and x86_64 emulators. [Packaging](guide/packaging.md#android),
[Signing](guide/signing.md), [tutorial step 13](tutorial/13-other-targets.md#on-android).

## ...make a Windows executable?

On Windows:

```bash
microengine package windows
```

It writes `dist/<Game>-<version>-windows-x64.zip`: a folder with a self-contained `<Game>.exe` (players need no .NET),
SDL 3 and raylib, your `Content` and the licence notices. [Tutorial step 12](tutorial/12-packaging.md).

## ...change the game's name and icon?

In the project file: `ApplicationTitle` is the name players see, `MicroEngineIcon` a square PNG of 256, 512 or 1024
pixels, `ApplicationId` the reverse-domain id, `Version` the version. Every package takes them from there; the window's
title bar shows `GameSettings.Title` from your code.

```xml
<ApplicationTitle>Gem Garden</ApplicationTitle>
<ApplicationId>com.example.gemgarden</ApplicationId>
<Version>1.0.0</Version>
<MicroEngineIcon>icon.png</MicroEngineIcon>
```

[Name, id, version and icon](guide/app-identity.md).

## ...use another backend?

Desktop games carry SDL 3 (the default) and raylib. Choose at run time, without changing the game:

```bash
microengine run --platform raylib         # while developing
MyGame --platform raylib                  # a packaged game, or MICROENGINE_PLATFORM=raylib
```

`--list-platforms` shows what this machine can run. A game that should ship only SDL references `MicroEngine.Sdl`
instead of `MicroEngine.Desktop` and passes its own catalog to the launcher. [Backends and platforms](manual/backends.md).

## ...plug in my own implementation?

The engine is built from small contracts you can implement: input sources (`IActionSource`), save storage
(`ISaveStorage`), transitions, and whole platforms (`IPlatform`, registered as a profile in the launcher's catalog). For
example, a platform that wraps another one, added to the desktop catalog:

<!-- code: samples/Examples/Program.cs#launch -->
```csharp title="Program.cs"
// The official desktop catalog (sdl, raylib, sdl-software, headless) plus one platform of our own:
// "--platform sdl-reported" runs SDL and reports it at start (ReportingPlatform, Extending.cs).
var catalog = DesktopLauncher.CreateCatalog().Add(ReportingPlatform.Wrap(SdlPlatform.Profile));
return DesktopLauncher.Run(new ExamplesGame(), args, catalog);
```

[Extending the engine](manual/extending.md) shows each extension point with working code.

## More questions

| Question | Answer |
| --- | --- |
| How do I change the screen size? | `GameSettings(title, VirtualWidth, VirtualHeight, Scale)`: the virtual canvas and the initial window scale ([Game lifecycle](manual/lifecycle.md#gamesettings)) |
| How do I go full screen? | Players press F11 or Alt+Enter on desktops; browsers use the page's button; phones are always full screen |
| How do I pause the game? | Push an overlay scene ([tutorial step 10](tutorial/10-states.md#the-pause-overlay)) |
| How do I detect collisions? | `TileMotion.Move` against tiles, `RectF.Intersects` between objects ([Tile maps and collision](manual/world.md)) |
| How do I make the camera follow the player? | `Camera2D.Follow` and `PushCamera` ([tutorial step 6](tutorial/06-garden.md)) |
| How do I wait or run a sequence? | Coroutines: `yield return Wait.Seconds(1)` ([Scenes](manual/scenes.md#coroutines)) |
| How do I test my game automatically? | Headless runs and input scripts ([Testing your game](examples/testing.md)) |
| How do I publish on the web? | `microengine package web`, then upload the folder ([Publishing on the web](examples/embedding.md)) |
| How do I add achievements? | `UseAchievements` with a catalog ([Achievements](manual/achievements.md)) |
| Is there an editor, physics or an ECS? | No: MicroEngine is code-first; see [What it offers today](guide/status.md) |

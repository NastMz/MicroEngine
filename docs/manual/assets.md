# Assets and content

A game can draw everything from code (`PixelImage`, `Synth`, `Song`) or load files: images, sprite sheets and their
animations, atlases made by packing tools, TrueType and OpenType fonts, sounds and music. Files live in the game's
`Content/` folder, ship with every package, and are loaded, shared and released through `Game.Assets`. Both approaches
can be mixed per element.

Files load as they are: no build step stands between saving a file and running the game. Around that default, this
page follows the work of a game with many assets:

| To do this | Use | Section |
| --- | --- | --- |
| Cut a sprite sheet into frames and time its animations without code | a `.sheet` description, or the Aseprite file itself | [Sheets and animations](#sheets-and-animations) |
| Use an atlas a packing tool made | `Assets.Atlas` | [Atlases from packing tools](#atlases-from-packing-tools) |
| Have a renamed file, sprite or clip fail when compiling, not while playing | `microengine content names` | [Names checked by the compiler](#names-checked-by-the-compiler) |
| Find broken content before the game runs, and in CI | `microengine content check` | [Checking content](#checking-content) |
| Show a loading screen for any mix of assets | `Assets.Preload` | [Loading screens](#loading-screens-preload) |
| Ship one packed atlas instead of loose images | `microengine content pack`, or packing at load | [Packing](#packing) |

## Choosing an approach

| Approach | Example | What ships |
| --- | --- | --- |
| Fully procedural | Meadow Run | Code only: `PixelImage` art, `Synth` sounds, `Song` music |
| Hybrid | Courtyard Key, Trail Duel | Code plus CC0 or CC BY 4.0 tilesets, sprites and jingles, cut out, recoloured or colour-graded at load |
| File-based | Neon Rift | CC0 sprite sheets, tilesets, WAV effects and OGG music |
| File-based HD | The Asset Gallery sample | A 1280×720 canvas, PNG and JPEG images, tool atlases, TTF/OTF fonts |

Authoring art in code is covered in [Graphics](rendering.md#art-in-code-pixelimage-and-art) and sound in
[Audio](audio.md).

## Adding files to a game

Put files under `Content/` next to the project file, in any folders:

```text
MyGame/
  MyGame.csproj
  Content/
    backgrounds/sky.jpg
    atlases/hero.json
    atlases/hero.png
    fonts/Inter-Regular.ttf
    sfx/jump.wav
```

Every file under `Content/` is copied next to the executable and into every package (Windows, Linux, macOS, Android,
iOS, browsers) without an item in the project file. C# files there compile as code instead. A project that still lists
the folder itself (`<None Include="Content/**" CopyToOutputDirectory="PreserveNewest" LinkBase="Content" />`) gets each
file once, so the item can go. `<MicroEngineContentDir>` moves the folder,
`<MicroEngineContentItems>false</MicroEngineContentItems>` turns the copying off.

Names must match their letter case exactly: Linux, Android and browsers do not find `Hero.png` for `hero.png`, so the
engine refuses the wrong spelling everywhere, Windows and macOS included, and names the file's real spelling. The build
fails (error ME0010) when two files differ only in letter case.

Nothing is converted at build time: images, atlases and fonts are decoded when the game loads them, in managed code, the
same way on every platform and in headless tests. Decoding a game-sized image takes milliseconds. The offline steps of
the [`microengine content`](../guide/cli.md#content) command are optional, and what they write (a packed atlas, a file
of names) is an ordinary file of the project.

## `Game.Assets`

`Game.Assets` (and `Assets` in a scene) is the
game's asset cache: the same request returns the same object, so a texture used by ten scenes is decoded and uploaded
once.

```csharp
protected override void Load()
{
    var sky = Assets.Texture("backgrounds/sky.jpg");                 // any image: PNG, JPEG, BMP, TGA, GIF, PSD
    var hero = Assets.Atlas("atlases/hero.json");                    // TexturePacker or Aseprite JSON, Starling XML
    var enemies = Assets.Atlas("enemies/enemies.sheet");             // sheets cut and timed by a description
    var items = Assets.AtlasFromFolder("items");                     // loose images packed into one atlas
    var ui = Assets.Font("fonts/Inter-Regular.ttf");                 // TTF or OTF, drawn at any size
    var level = Assets.Text("levels/1.txt");                         // raw bytes or text of any file
    Scenes.Push(new PlayScene());
}
```

| Member | Returns |
| --- | --- |
| `Texture(path, filter?)` | the image as a texture; cached per path and filter |
| `Image(path)` | the image as a new `PixelImage` each call (not cached), to recolour or crop before packing |
| `Atlas(path, options?, filter?)` | a `SpriteAtlas` from a packing tool's file and the image it names, or from a `.sheet` description and its images, with their clips |
| `Sheet(path, options?)` | the frames and clips of a sheet as pixels, new each call (not cached), to change or to pack with others (`SpriteAtlasBuilder.AddSheet`) |
| `AtlasFromFolder(folder, pivot?, filter?)` | every image of a folder packed into one atlas, named by file name |
| `Font(path, options?)` | a `ScalableFont` ([Text](text.md#scalable-fonts-ttf-and-otf)) |
| `Material(path, fallback?)` | a `Material2D` from a compiled fragment shader (`.frag.shader`), to draw through with `renderer.PushState` ([Render targets and shaders](../guide/render-targets-and-shaders.md)); cached per path and fallback |
| `RenderTarget(name, width, height, filter?)` | a `RenderTarget2D` to draw into and then draw like a texture. No file describes it: it is known by the name the game gives it, and cached per name, size and filter |
| `Shader(path)` | a compiled shader file (`.shader`) as a `CompiledShader`, for games that create shaders on the graphics device themselves; a material needs only `Material` |
| `Sound(path)`, `Music(path)` | a PCM WAV decoded into an `AudioClip`; a music file (OGG, WAV, MP3 on raylib) as `EncodedAudio`, decoded while it plays. Play them with `Audio.AddSound(name, clip)` and `Audio.AddMusic(name, music)` |
| `Bytes(path)`, `Text(path)` | the file's content (not cached) |
| `IsLoaded(path)`, `Unload(path)` | whether a file is loaded; releases this cache's hold on it |
| `Loaded`, `Statistics` | what is loaded, texture memory (`TextureBytes`), audio memory (`AudioBytes`), load times, loads during play |
| `CreateScope(name?)` | a child cache that releases what it holds when disposed |
| `Preload(paths...)`, `Preload(load => ...)` | loads assets of any kind in the background, returning an `AssetBatch` |

Paths are relative to `Content/` with `/` separators. Everything `Game.Assets` loaded is disposed after `Game.Unload`;
do not dispose those objects yourself. Looking up an asset that is already loaded allocates nothing, so it can be done
while drawing.

Sounds and music also keep their own registry: `Audio.AddSoundFile` and `Audio.AddMusicFile`
([Audio](audio.md#sound-and-music-files)). Sound and music files are asset kinds of `Game.Assets` as well (see
[handles, loaders and sources](#handles-loaders-and-content-sources)).

### Levels and scenes: scopes

Assets used by one level should go away when the level ends. Scopes share what they load: a scope that asks for a file
another scope (`Game.Assets`, another level, an overlay scene) already loaded gets the same object, loaded once, and
holds it too. Disposing a scope, or `Unload(path)`, releases that scope's hold; an asset is disposed only when no scope
holds it any more, so one level ending never takes a texture away from another. A scope's `Count` is the number of
assets it holds, including those of its parents it asked for:

```csharp
public override void Enter()
{
    _level = Assets.CreateScope("forest");
    _tiles = _level.Atlas("levels/forest/tiles.xml");       // released with the scope
    _hero = _level.Atlas("atlases/hero.json");               // the same object if Game.Assets loaded it: not loaded twice
}

public override void Exit() => _level.Dispose();
```

### Loading screens: `Preload`

Loading a large image in the middle of play takes longer than a frame. `Preload` reads and decodes files on worker
threads and finishes them a few per frame (`FrameBudgetMilliseconds`, 4 ms by default); afterwards the usual calls
return them at once. It takes every kind of asset, so a loading screen holds no code per kind:

```csharp
_batch = Assets.Preload("backgrounds/castle.png", "enemies/enemies.sheet", "fonts/Title.otf", "sfx/door.wav", "music/boss.ogg");

public override void Update(float dt)
{
    if (_batch.IsDone) Game.Scenes.Replace(new BossScene());   // Errors lists what failed
}

public override void Draw(IRenderer r)
{
    DrawProgressBar(r, _batch.Progress);
    Font.Draw(r, _batch.Current ?? "", new Vector2(20, 160), Color.White);   // the file being loaded
}
```

By path, each file is loaded as the kind its extension chooses, with default options: images as textures, `.sheet`,
`.json` and `.xml` files as atlases, `.ttf` and `.otf` files as fonts, `.wav` files as sounds, `.ogg` and `.mp3`
files as music, `.frag.shader` files as materials, and the extensions of a loader the game registered as that
loader's kind. A file no loader reads is refused at once, naming it. To preload with options, name each asset the way
the direct call does:

```csharp
_batch = Assets.Preload(load =>
{
    load.Atlas("characters/hero.sheet", new AtlasOptions(Pivot: new Vector2(0.5f, 1)));
    load.Texture("backgrounds/castle.png", TextureFilter.Linear);
    load.Music("music/intro.wav");                      // a WAV as music
    load.Asset("level", "levels/castle.lvl");           // a kind the game registered
    load.Files("sfx/door.wav", "sfx/key.wav");          // by path, as above
});
```

In a browser, where .NET runs on one thread, decoding also happens within the frame budget. `AssetBatch.Wait()`
finishes a batch at once.

### When something is wrong

Every failure is an `AssetLoadException` with the path (`AssetPath`), what was asked for (`Kind`) and the reason: a
missing file (with its full path), a name spelled with the wrong letter case (with the real spelling), an image or font
the engine cannot read, an atlas whose frames do not fit its image.

A load during play that takes longer than `SlowLoadMilliseconds` (4 ms) is reported once on the error output:

```text
assets: atlas 'atlases/tiles.xml' was loaded during play and took 30.8 ms (a visible hitch); load it in Load, or Preload it behind a loading screen.
```

`Statistics` gives the number of assets, their texture memory, load time and loads during play; `--profile FILE` records
the same numbers in the profile's environment (`assets.loaded`, `assets.textureMB`, `assets.loadMs`,
`assets.loadsDuringPlay`).

## Sheets and animations

A sprite sheet is one image holding many frames. What the frames are (their size, their names, where each is
anchored) and how they animate is data that belongs next to the image, not in the game's code.

### Sheet descriptions

A `.sheet` file is a small JSON file (comments and trailing commas allowed) that describes one or more images.
`Assets.Atlas` cuts them and packs every frame into one atlas:

```json
{
  "pivot": [0.5, 1],
  "sheets": [
    { "image": "drone.png", "name": "drone", "cell": [55, 52], "origin": [27, 26], "fps": 12 },
    { "image": "palm.png", "name": "prop_palm" },
    { "image": "tiles.png", "cell": [16, 16], "margin": 0, "spacing": 0,
      "strips": { "walk": { "from": 8, "count": 6, "durations": [100, 100, 200, 100, 100, 200] },
                  "ledge": { "at": [6, 11], "count": 4 },
                  "fill": { "cells": [[4, 1], [1, 1]] },
                  "odd": { "rects": [[504, 80, 16, 16]] } },
      "sprites": { "gem": [2, 3], "logo": [0, 64, 48, 16], "key": { "cell": [3, 1], "origin": [8, 15] } } }
  ],
  "clips": { "hop": { "frames": ["drone1", "drone3", "drone1"], "fps": 6, "loop": false },
             "patrol": { "strip": "drone", "fps": 8, "direction": "pingpong" } }
}
```

| Entry | Meaning |
| --- | --- |
| `image` | The image, relative to the file |
| `name` with `cell` | The whole image is one strip: frames `name0`, `name1`... row by row. Fully transparent cells at the end are left out unless `count` says how many |
| `name` without `cell` | The whole image is one sprite |
| `margin`, `spacing` | Pixels around the grid and between cells (one number, or `[x, y]`) |
| `strips` | Named runs of frames of one image: `from` a cell index or `at` a column and row, with `count` (to the end of the row by default); or `cells`, a list of `[column, row]`; or `rects`, a list of `[x, y, width, height]` in pixels |
| `sprites` | Named single sprites: `[column, row]`, `[x, y, width, height]`, or an object with `cell` or `rect` and its own anchor |
| `origin` or `pivot` | The anchor: pixels from the frame's top-left, or a fraction of the frame (`[0.5, 1]` is the feet). On the file, a sheet, a strip or a sprite; the nearest one applies. Without any, `AtlasOptions.Pivot` |
| `fps` or `durations` | Makes a strip a clip of the same name: a rate, or milliseconds per frame |
| `direction`, `loop` | `forward`, `reverse`, `pingpong` or `pingpong_reverse`; `false` plays the clip once |
| `clips` | More clips: `frames` listed by name (a frame may repeat) or a whole `strip`, with the same timing entries |

A mistake names what is wrong and where: `strip 's' of 'a.png': the cell at column 4, row 0 lies outside the 16x8
image`, an unknown property with the list of known ones, a clip naming a frame the file does not have.

```csharp
var enemies = Assets.Atlas("enemies/enemies.sheet");
r.DrawPivoted(enemies.Clip("drone").FrameAt(Age), position);    // the strip, at the rate the file gives
var animator = enemies.CreateAnimator("patrol");                // every clip of the file, by name
```

A game that also derives sprites in code takes the frames instead and packs them with its own:

```csharp
var atlas = new SpriteAtlasBuilder()
    .AddSheet(Assets.Sheet("sprites.sheet"))      // every frame and clip the file describes
    .Add("gem", GardenArt.Gem(), new Vector2(4, 4))
    .Build(Graphics);
```

Source: [`games/NeonRift/Content/sprites.sheet`](../../games/NeonRift/Content/sprites.sheet) describes that game's
sprite images; [`Art/Assets.cs`](../../games/NeonRift/Art/Assets.cs) adds the poses it derives from them.

### Aseprite files

The optional `MicroEngine.Aseprite` package reads `.aseprite` and `.ase` files as the artist saved them, with no
export step:

```csharp
var hero = Assets.Aseprite("characters/hero.aseprite");     // an ordinary SpriteAtlas
var animator = hero.CreateAnimator("idle");                 // every frame tag is a clip
```

| In the file | In the atlas |
| --- | --- |
| Frames | Sprites `hero 0`, `hero 1`... (the names Aseprite's own sheet export gives), each the picture Aseprite shows: visible layers flattened with their blend modes and opacity |
| Frame tags | Clips with each frame's duration, the tag's direction and its repeat count (a tag that repeats a number of times plays once) |
| Slices | A sprite per slice, named after it, anchored at the slice's pivot |
| The first slice with a pivot | The anchor of every frame (the slice's corner plus its pivot), so the feet are set once in the editor; without one, `AtlasOptions.Pivot` |

`Assets.Aseprite` registers the format on first use; `Assets.Loaders.AddAseprite()` registers it up front, after which
`Assets.Atlas`, `Assets.Sheet` and `Assets.Preload` take Aseprite files like any other. Without the package,
loading one fails saying which package reads it.

## Names checked by the compiler

Paths, sprite names and clip names are strings: a renamed file fails when the game reaches it. `microengine content
names` writes them as constants, so the compiler finds the mistake:

```bash
microengine content names
```

```csharp
var atlas = Assets.Atlas(GameContent.Enemies.EnemiesSheet.Path);
r.Draw(atlas.Clip(GameContent.Enemies.EnemiesSheet.Clips.Drone).FrameAt(Age), position);
var sky = Assets.Texture(GameContent.Backgrounds.Sky);
```

The file (`GameContent.g.cs` next to the project) has a class per folder and a constant per file; a sheet, an
Aseprite file or an atlas is a class with its `Path`, `Sprites`, `Strips` (the names `atlas.Frames` takes) and `Clips`.
Run the command again after changing content and commit the file; `microengine content names --check` fails in CI
when it is out of date. The step is optional: strings keep working. See [the tool](../guide/cli.md#content).

## Checking content

`microengine content check` reads every file of the content folder the way the engine will and lists what would fail
or mislead: unreadable images, sounds, fonts and shaders, sheets and atlases whose image is missing or too small for
their frames, clips naming missing frames, names that differ only in letter case, source files that would ship,
oversized textures, music only one platform decodes. It exits with 1 on errors, so it runs in CI as it is.

```text
error: enemies/lost.sheet: The image 'gone.png' cannot be read: 'enemies/gone.png' does not exist in the content folder. Fix the description or the image it names.
warning: art/hero.psd: a source file (.psd) ships with the game: every file under the content folder is packaged. Keep sources outside it (for example in an Art folder next to the project).
content: 85 files, 1 errors, 1 warnings
```

## Images

`PixelImage.Decode(bytes)` and `PixelImage.Load(path)` read PNG (every colour type and bit depth, interlaced or not),
JPEG (baseline and progressive), BMP, TGA, GIF (the first frame) and PSD (the composited image), recognised by content,
not by name. Loaded images are ordinary `PixelImage`s, so they can be cropped, recoloured,
rotated or silhouetted before packing.

How textures are sampled (nearest for pixel art, linear for HD art) is described in
[Graphics](rendering.md#filtering-nearest-or-linear).

## Atlases from packing tools

`Assets.Atlas` and `SpriteAtlas.Load(device, path)` read:

| Format | Made by | Read from it |
| --- | --- | --- |
| JSON, hash or array of frames | TexturePacker, Free Texture Packer, Aseprite | frames, trimming, pivots |
| Aseprite JSON | Aseprite (File > Export Sprite Sheet, "JSON Data") | frame durations, frame tags and their directions |
| Starling / Sparrow XML | TexturePacker, ShoeBox, Kenney's packs | frames, trimming (`frameX`, `frameY`), pivots |

The image is the one the file names, next to it. Frame names lose image endings (`hero_run_0.png` is `hero_run_0`,
Aseprite's `slime 0.aseprite` is `slime 0`). Trimmed frames keep their place: a sprite's origin is the top-left of the
untrimmed frame, so trimmed and untrimmed frames line up; `new AtlasOptions(Pivot: new Vector2(0.5f, 1))` anchors every
frame without a pivot of its own at the middle of its bottom edge (feet). Frames the JSON formats store rotated are
turned upright when the atlas loads; rotated frames of the XML format are refused with a message: turn rotation off
in the tool.

```csharp
var enemies = Assets.Atlas("atlases/enemies.json", new AtlasOptions(Pivot: new Vector2(0.5f, 1)));
var fly = enemies.Clip("flying");                     // an Aseprite tag, with each frame's duration and direction
r.DrawPivoted(fly.FrameAt(Age), new Vector2(640, 400));
var walk = enemies.Frames("enemyWalking_");           // numbered frames, starting at 0 or 1
```

`SpriteAtlas.Animations` lists the tags; `Clip(tag)` builds the clip once and shares it, repeating as the tag says
(`Clip(tag, loop: false)` decides otherwise); ping-pong tags play `0 1 2 1 0 ...`; `CreateAnimator()` gives an
`Animator` holding every tag. `AnimationClip` also accepts a duration per frame directly
(`new AnimationClip(frames, durations, loop)`). `AtlasData.Parse` reads the same files without a graphics device, for
tools and tests.

## Packing

Loose images can be packed when the game loads them or ahead of time; the game draws the same either way.

`Assets.AtlasFromFolder("items")` packs every image of a folder at load, named by file name: nothing to run, a little
load time on every launch. `microengine content pack Art/items` packs the same folder once, offline, into
`Content/atlases/items.png` and `items.json`, which `Assets.Atlas("atlases/items.json")` loads like any tool's atlas:
the same sprite names and anchors, so only the loading line changes. The folder may also hold `.sheet` files, Aseprite
files and atlases, whose frames and clips go into the same atlas. `--trim` leaves transparent margins out,
`--extrude` protects linear filtering, and `--check` fails in CI when the packed files are stale
([the tool](../guide/cli.md#content)).

### Packing at run time

`SpriteAtlasBuilder` packs `PixelImage`s into one texture; `Assets.AtlasFromFolder("items")` does that for every image
of a folder. `builder.Pack()` gives the packed picture and its description without a graphics device, and
`PackedAtlas.ToJson` writes the description in the format above. Atlases sampled linearly repeat each image's edge pixels into a one-pixel border, so scaled sprites and
tiles never show their neighbours' colours.

#### Grid sheets

`SpriteAtlasBuilder.AddGrid(name, sheet, cellWidth, cellHeight, count?, origin?)` slices a row-major sheet into
frames `name0`, `name1`, ...; without `count`, fully transparent trailing cells of the last row are dropped.

```csharp
builder.AddGrid("hero", content.Image("hero.png"), 16, 16, origin: new Vector2(8, 16));   // hero0..hero3
```

For sheets that are files, a [sheet description](#sheet-descriptions) says the same without code.

#### Repainting imported art

Hybrid games repaint imported sprites with their own palette at load so they match the procedural art:

```csharp
private static PixelImage Repaint(PixelImage sheet, params (string From, Color To)[] swaps)
{
    foreach (var (from, to) in swaps) sheet.Recolor(Color.Hex(from), to);
    return sheet.Recolor(Color.Hex("141b1b"), Pal.Ink);
}
```

Source: [`games/CourtyardKey/Visuals/MonsterArt.cs`](../../games/CourtyardKey/Visuals/MonsterArt.cs)

## Handles, loaders and content sources

`Game.Assets` is built on an asset store (`MicroEngine.Content`, inside
the `MicroEngine` package) that games can also use directly.

**Typed handles.** `Assets.Scope.Load<T>(kind, path)` returns an `Asset<T>`, a small value that names the asset
instead of holding the object. `Assets.Store.Get(handle)` returns the object while some scope holds it; once every
holding scope has released it, the handle is stale and `Get` fails saying so, even if another asset later takes its
place. Handles follow the same scopes as the methods above: `Assets.Texture("a.png")` and
`Assets.Scope.Load<Texture>("texture", "a.png", variant: (long)filter)` hold the same object.

```csharp
var jump = Assets.Scope.Load<AudioClip>("sound", "sfx/jump.wav");   // decoded once, shared, released with the scope
Audio.AddSound("jump", Assets.Store.Get(jump));
```

**Loaders.** Each kind of asset has one loader in `Assets.Loaders`: `texture`, `atlas`, `sheet` (the frames and clips of a `.sheet` description, as pixels), `folder atlas`, `font`,
`shader`, `material`, `render target` (created from its options, a `RenderTargetSize`, and its name: a loader need not
read a file, and the content folder is opened only when one does), `sound` (a PCM WAV decoded into an `AudioClip`) and
`music` (a music file's bytes as `EncodedAudio`, decoded while it plays). A game adds a kind of its own, or replaces one, by registering an `IAssetLoader<T>` (or an
`AssetLoader<T>` built from a function) before the first load of that kind:

```csharp
Assets.Loaders.Add(new AssetLoader<LevelData>("level", c => LevelData.Parse(c.ReadBytes()), [".lvl"]));
var forest = Assets.Scope.Load<LevelData>("level", "levels/forest.lvl");
```

A loader reads through its context (`ReadBytes`, `Files`); a failure becomes an `AssetLoadException` naming the asset.
A loader of data that touches only the content can run on a worker thread during a preload:
`AssetLoader.Background<LevelData>("level", c => LevelData.Parse(c.ReadBytes()), [".lvl"])`. One that also creates a
device resource splits in two, `new StagedAssetLoader<TPrepared, T>(kind, prepare, complete)`: `prepare` decodes on
any thread, `complete` runs on the game's thread. A loader that makes a `SpriteSheet` for an extension (as the
Aseprite package's does) makes files of that extension loadable through `Assets.Atlas`. `Assets.Store.Read<T>(key)`
runs a loader without storing the result. `IAssetImporter` is reserved for a build-time conversion step and is not
called: loaders read source files.

**Content sources.** Content comes from any `IContentSource`, chosen by the composition (`host.Content`): a folder
(`FolderContentSource`), a zip archive (`ZipContentSource`), or files a host lists and opens itself
(`ListedContentSource`, for an app's packaged assets). `ContentSources.Open(path)` opens a folder or a `.zip` file.
Every source follows the folder rules: exact letter case on every system (naming the real spelling when only the case
differs), `/` separators, no path out of the source. `Assets.Source` is the source in use; `Assets.Content` (a
`ContentFolder`) works only when the content is a folder.

```csharp
public static void Configure(HostBuilder host)
{
    host.UseGame(() => new MainGame());
    host.Content = ContentSources.Open("game-content.zip");   // the whole Content folder as one archive
}
```

## `ContentFolder`

`ContentFolder` is the lower level under `Game.Assets`: it resolves paths in the content folder and reads files.

| Member | Behaviour |
| --- | --- |
| `new ContentFolder(root?)` | Default root is `ContentFolder.DefaultRoot` (`<exe dir>/Content`, read-only; hosts of your own compose their folder with `host.Content`); a missing folder throws with the full path |
| `PathOf("sfx/shot.wav")` | Full path from a `/`-separated relative path; throws if missing, outside the root or spelled with another letter case |
| `Image(relative)` | Loads an image as a `PixelImage` |
| `ReadBytes(relative)`, `Exists(relative)` | Raw access |
| `Files(folder)` | The files of a folder |

Games accept an optional root so tests can point at another folder (`new RiftGame(..., contentRoot: ...)`); a game
using `Game.Assets` overrides `ContentRoot`. In a browser the page copies the package's files into the runtime's
virtual file system at `/Content`, so the same code works unchanged ([web builds](../../web/README.md#how-the-host-works)).

## Audio files

`AudioMixer.AddSoundFile` decodes PCM WAV (8/16/24-bit, mono or stereo). `AddMusicFile` decodes WAV the same way
and hands any other extension to the device to stream: `.ogg` everywhere, `.mp3` on the raylib platform only. See
[Audio](audio.md#sound-and-music-files).

## Credits convention

Every game that ships files lists them in a `CREDITS.md`: which files come from which pack, author, source URL,
licence, the date the licence was checked, and any changes made (joined strips, downmixed tracks, recolouring at
load). CC BY art asks for exactly that attribution: title, author, source, licence and changes. Fonts usually come
under the SIL Open Font License, which asks for its text to travel with the font: keep the licence file next to the
font in `Content/`. Examples: [Neon Rift](../../games/NeonRift/CREDITS.md),
[Courtyard Key](../../games/CourtyardKey/CREDITS.md), [Trail Duel](../../games/TrailDuel/CREDITS.md),
[Asset Gallery](../../samples/AssetGallery/CREDITS.md).

## Limitations

- No compressed GPU texture formats (BCn, ASTC, ETC2) through `Assets.Texture`. Mip levels are a filter:
  `Assets.Texture(path, TextureFilter.LinearMipmap)`, on the devices that have them
  ([Mip levels](rendering.md#mip-levels)).
- A changed file is picked up the next time the game starts: assets are not reloaded while the game runs.
- One content folder serves every platform: there are no per-platform variants of a file.
- Content sources are read whole and synchronously: a zip archive decompresses a file when it is opened, and a
  browser page still copies the package's files into memory before the game starts.
- Rotated frames of Starling XML atlases, slices in Aseprite's exported JSON (the `.aseprite` file's slices are
  read), Aseprite tilemap layers as tile maps, libGDX `.atlas` and Spine files are not read.
- A packed atlas is one page: `microengine content pack` and `SpriteAtlasBuilder` grow the picture instead of
  starting a second one. Tiled maps are read by the
  optional `MicroEngine.Tiled` package ([Maps from Tiled](world.md#maps-from-tiled)).
- Animated GIFs load their first frame only; there is no video.
- Fonts: see [Text](text.md#limits).

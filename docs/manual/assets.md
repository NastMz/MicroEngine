# Assets and content

A game can draw everything from code (`PixelImage`, `Synth`, `Song`) or load files: images, atlases made by packing
tools, TrueType and OpenType fonts, sounds and music. Files live in the game's `Content/` folder, ship with every
package, and are loaded, shared and released through `Game.Assets`. Both approaches can be mixed per element.

## Choosing an approach

| Approach | Example | What ships |
| --- | --- | --- |
| Fully procedural | Meadow Run | Code only: `PixelImage` art, `Synth` sounds, `Song` music |
| Hybrid | Courtyard Key, Trail Duel | Mostly code, plus a few CC0 sprites or jingles repainted or blended in |
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

Next SDK version (not published yet: arrives with the next SDK version): every file under `Content/` is copied next to
the executable and into every package (Windows, Linux, macOS, Android, iOS, browsers) without an item in the project
file. C# files there compile as code instead. A project that still lists the folder itself
(`<None Include="Content/**" CopyToOutputDirectory="PreserveNewest" LinkBase="Content" />`, which the published SDK
needs) gets each file once; the item can go once the game uses the next SDK. `<MicroEngineContentDir>` moves the folder,
`<MicroEngineContentItems>false</MicroEngineContentItems>` turns the copying off.

Names must match their letter case exactly: Linux, Android and browsers do not find `Hero.png` for `hero.png`, so the
engine refuses the wrong spelling everywhere, Windows and macOS included, and names the file's real spelling. The build
fails (error ME0010) when two files differ only in letter case.

Nothing is converted at build time: images, atlases and fonts are decoded when the game loads them, in managed code, the
same way on every platform and in headless tests. Decoding a game-sized image takes milliseconds.

## `Game.Assets`

Next SDK version (not published yet: arrives with the next SDK version). `Game.Assets` (and `Assets` in a scene) is the
game's asset cache: the same request returns the same object, so a texture used by ten scenes is decoded and uploaded
once.

```csharp
protected override void Load()
{
    var sky = Assets.Texture("backgrounds/sky.jpg");                 // any image: PNG, JPEG, BMP, TGA, GIF, PSD
    var hero = Assets.Atlas("atlases/hero.json");                    // TexturePacker or Aseprite JSON, Starling XML
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
| `Atlas(path, options?, filter?)` | a `SpriteAtlas` from a packing tool's file and the image it names |
| `AtlasFromFolder(folder, pivot?, filter?)` | every image of a folder packed into one atlas, named by file name |
| `Font(path, options?)` | a `ScalableFont` ([Text](text.md#scalable-fonts-ttf-and-otf)) |
| `Bytes(path)`, `Text(path)` | the file's content (not cached) |
| `IsLoaded(path)`, `Unload(path)` | whether a file is loaded; releases this cache's hold on it |
| `Loaded`, `Statistics` | what is loaded, texture memory, load times, loads during play |
| `CreateScope(name?)` | a child cache that releases what it holds when disposed |
| `Preload(paths...)` | loads files in the background, returning an `AssetBatch` |

Paths are relative to `Content/` with `/` separators. Everything `Game.Assets` loaded is disposed after `Game.Unload`;
do not dispose those objects yourself. Looking up an asset that is already loaded allocates nothing, so it can be done
while drawing.

Sounds and music keep their own registry: `Audio.AddSoundFile` and `Audio.AddMusicFile` ([Audio](audio.md#sound-and-music-files)).

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
threads and uploads them a few per frame (`FrameBudgetMilliseconds`, 4 ms by default); afterwards `Texture`, `Atlas`
and `Font` return them at once:

```csharp
_batch = Assets.Preload("backgrounds/castle.png", "atlases/boss.json", "fonts/Title.otf");

public override void Update(float dt)
{
    if (_batch.IsDone) Game.Scenes.Replace(new BossScene());   // Errors lists what failed
}

public override void Draw(IRenderer r) => DrawProgressBar(r, _batch.Progress);
```

Images become textures, `.json` and `.xml` files atlases, `.ttf` and `.otf` files fonts, all with default options. In a
browser, where .NET runs on one thread, decoding also happens within the frame budget. `AssetBatch.Wait()` finishes a
batch at once.

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

## Images

Next SDK version (not published yet: arrives with the next SDK version): `PixelImage.Decode(bytes)` and
`PixelImage.Load(path)` read PNG (every colour type and bit depth, interlaced or not), JPEG (baseline and progressive),
BMP, TGA, GIF (the first frame) and PSD (the composited image), recognised by content, not by name. The published SDK
reads non-interlaced 8-bit PNGs only. Loaded images are ordinary `PixelImage`s, so they can be cropped, recoloured,
rotated or silhouetted before packing.

How textures are sampled (nearest for pixel art, linear for HD art) is described in
[Graphics](rendering.md#filtering-nearest-or-linear).

## Atlases from packing tools

Next SDK version (not published yet: arrives with the next SDK version). `Assets.Atlas` and
`SpriteAtlas.Load(device, path)` read:

| Format | Made by | Read from it |
| --- | --- | --- |
| JSON, hash or array of frames | TexturePacker, Free Texture Packer, Aseprite | frames, trimming, pivots |
| Aseprite JSON | Aseprite (File > Export Sprite Sheet, "JSON Data") | frame durations, frame tags and their directions |
| Starling / Sparrow XML | TexturePacker, ShoeBox, Kenney's packs | frames, trimming (`frameX`, `frameY`), pivots |

The image is the one the file names, next to it. Frame names lose image endings (`hero_run_0.png` is `hero_run_0`,
Aseprite's `slime 0.aseprite` is `slime 0`). Trimmed frames keep their place: a sprite's origin is the top-left of the
untrimmed frame, so trimmed and untrimmed frames line up; `new AtlasOptions(Pivot: new Vector2(0.5f, 1))` anchors every
frame without a pivot of its own at the middle of its bottom edge (feet). Rotated frames are refused with a message:
turn rotation off in the tool.

```csharp
var enemies = Assets.Atlas("atlases/enemies.json", new AtlasOptions(Pivot: new Vector2(0.5f, 1)));
var fly = enemies.Clip("flying");                     // an Aseprite tag, with each frame's duration and direction
r.DrawPivoted(fly.FrameAt(Age), new Vector2(640, 400));
var walk = enemies.Frames("enemyWalking_");           // numbered frames, starting at 0 or 1
```

`SpriteAtlas.Animations` lists the tags; `Clip(tag, loop)` builds the clip once and shares it; ping-pong tags play
`0 1 2 1 0 ...`. `AnimationClip` also accepts a duration per frame directly
(`new AnimationClip(frames, durations, loop)`). `AtlasData.Parse` reads the same files without a graphics device, for
tools and tests.

### Packing at run time

`SpriteAtlasBuilder` packs `PixelImage`s into one texture; `Assets.AtlasFromFolder("items")` does that for every image
of a folder. Atlases sampled linearly repeat each image's edge pixels into a one-pixel border, so scaled sprites and
tiles never show their neighbours' colours.

#### Grid sheets

`SpriteAtlasBuilder.AddGrid(name, sheet, cellWidth, cellHeight, count?, origin?)` slices a row-major sheet into
frames `name0`, `name1`, ...; without `count`, fully transparent trailing cells of the last row are dropped.

```csharp
// Hero (Warped City): every strip shares the 71×67 canvas and the feet pivot.
foreach (var name in new[] { "idle", "run", "run-shoot", "jump", "back-jump", "crouch", "shoot", "hurt" })
    b.AddGrid("hero_" + name, content.Image($"hero/{name}.png"), 71, 67, origin: HeroOrigin);
```

Source: [`games/NeonRift/Art/Assets.cs`](../../games/NeonRift/Art/Assets.cs)

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

## `ContentFolder`

`ContentFolder` is the lower level under `Game.Assets`: it resolves paths in the content folder and reads files.

| Member | Behaviour |
| --- | --- |
| `new ContentFolder(root?)` | Default root is `<exe dir>/Content` (or `ContentFolder.DefaultRoot`, which phone hosts set; next SDK version: `DefaultRoot` is read-only and hosts compose their folder, `host.Content`); a missing folder throws with the full path |
| `PathOf("sfx/shot.wav")` | Full path from a `/`-separated relative path; throws if missing, outside the root or (next SDK version) spelled with another letter case |
| `Image(relative)` | Loads an image as a `PixelImage` |
| `ReadBytes(relative)`, `Exists(relative)` | Raw access |
| `Files(folder)` | The files of a folder (next SDK version) |

Games accept an optional root so tests can point at another folder (`new RiftGame(..., contentRoot: ...)`); a game
using `Game.Assets` overrides `ContentRoot`. In a browser the page copies the package's files into the runtime's
virtual file system at `/Content`, so the same code works unchanged ([web builds](../../web/README.md#how-the-host-works)).

## Audio files

`AudioMixer.AddSoundFile` decodes PCM WAV (8/16/24-bit, mono or stereo). `AddMusicFile` decodes WAV the same way
and hands any other extension to the device to stream: `.ogg` everywhere, `.mp3` on the raylib platform only. See
[Audio](audio.md#sound-and-music-files).

## Credits convention

Every game that ships files lists them in a `CREDITS.md`: which files come from which pack, author, source URL,
licence, the date the licence was checked, and any changes made (joined strips, downmixed tracks). Fonts usually come
under the SIL Open Font License, which asks for its text to travel with the font: keep the licence file next to the
font in `Content/`. Examples: [Neon Rift](../../games/NeonRift/CREDITS.md),
[Courtyard Key](../../games/CourtyardKey/CREDITS.md), [Trail Duel](../../games/TrailDuel/CREDITS.md),
[Asset Gallery](../../samples/AssetGallery/CREDITS.md).

## Limitations

- No build-time conversion: no compressed GPU texture formats (BCn, ASTC, ETC2), no mipmaps, no hot reload.
- Atlases with rotated frames, Aseprite slices, libGDX `.atlas`, Spine and Tiled files are not read.
- Animated GIFs load their first frame only; there is no video.
- Fonts: see [Text](text.md#limits).

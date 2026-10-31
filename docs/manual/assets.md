# Assets and content

MicroEngine is code-first: sprites, tiles, fonts, sounds and music can all be generated in C# at start-up, so a
game can ship without a single binary file. When a game wants existing art or audio, it keeps the files under its
own `Content/` folder and loads them through `ContentFolder` with managed PNG and WAV decoders, while OGG and MP3
music is streamed by the backend. Both approaches can be mixed per element, and every shipped file is credited in
the game's `CREDITS.md`.

## Choosing an approach

| Approach | Example | What ships |
| --- | --- | --- |
| Fully procedural | Meadow Run | Code only: `PixelImage` art, `Synth` sounds, `Song` music |
| Hybrid | Courtyard Key, Trail Duel | Mostly code, plus a few CC0 sprites or jingles repainted or blended in |
| File-based | Neon Rift | CC0 sprite sheets, tilesets, WAV effects and OGG music |

The reasoning is recorded in [ADR 0002](../decisions/0002-code-first-content.md) (code-first default),
[ADR 0004](../decisions/0004-external-asset-files.md) (external files) and
[ADR 0005](../decisions/0005-hybrid-content.md) (hybrid games). Authoring art in code is covered in
[Rendering](rendering.md#art-in-code-pixelimage-and-art) and sound in [Audio](audio.md).

## Shipping a `Content` folder

Keep files in `games/<Name>/Content/` and copy them next to the executable at build time:

```xml
<ItemGroup>
  <None Include="Content/**" CopyToOutputDirectory="PreserveNewest" LinkBase="Content" />
</ItemGroup>
```

Source: [`games/NeonRift/NeonRift.csproj`](../../games/NeonRift/NeonRift.csproj)

Some games also keep C# level definitions under `Content/` (for example `games/NeonRift/Content/Stages.cs`); they
compile as normal code, and the web build leaves `.cs` files out of the package.

## `ContentFolder`

```csharp
public sealed class ContentFolder
{
    public string Root { get; }

    public ContentFolder(string? root = null)
    {
        Root = Path.GetFullPath(root ?? Path.Combine(AppContext.BaseDirectory, "Content"));
        if (!Directory.Exists(Root)) throw new DirectoryNotFoundException($"Content folder not found: {Root}");
    }
    // ...
    public bool Exists(string relative) => File.Exists(Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar)));
    public byte[] ReadBytes(string relative) => File.ReadAllBytes(PathOf(relative));
    public Graphics.PixelImage Image(string relative) => Graphics.PixelImage.Load(PathOf(relative));
}
```

Source: [`src/MicroEngine/Core/ContentFolder.cs`](../../src/MicroEngine/Core/ContentFolder.cs)

| Member | Behaviour |
| --- | --- |
| `new ContentFolder(root?)` | Default root is `<exe dir>/Content`; a missing folder throws with the full path |
| `PathOf("sfx/shot.wav")` | Full path from a `/`-separated relative path; throws if missing or outside the root |
| `Image(relative)` | Loads a PNG as a `PixelImage` |
| `ReadBytes(relative)`, `Exists(relative)` | Raw access |

Games accept an optional root so tests can point at another folder (`new RiftGame(..., contentRoot: ...)`). In a
browser the page copies the package's files into the runtime's virtual file system at `/Content`, so the same
code works unchanged ([web builds](../../web/README.md#how-the-host-works)).

## Images

`PixelImage.Load(path)` and `PixelImage.FromPng(bytes)` decode PNG in managed code: 8-bit greyscale, greyscale with
alpha, RGB and RGBA, and 1/2/4/8-bit paletted images with `tRNS` transparency, non-interlaced. Other variants
(16-bit channels, interlaced files) are rejected; `Load` names the file in the error. Loaded images are ordinary `PixelImage`s, so they can be cropped, recoloured, rotated or silhouetted before
packing.

### Grid sheets

`SpriteAtlasBuilder.AddGrid(name, sheet, cellWidth, cellHeight, count?, origin?)` slices a row-major sheet into
frames `name0`, `name1`, ...; without `count`, fully transparent trailing cells of the last row are dropped.

```csharp
// Hero (Warped City): every strip shares the 71×67 canvas and the feet pivot.
foreach (var name in new[] { "idle", "run", "run-shoot", "jump", "back-jump", "crouch", "shoot", "hurt" })
    b.AddGrid("hero_" + name, content.Image($"hero/{name}.png"), 71, 67, origin: HeroOrigin);
```

Source: [`games/NeonRift/Art/Assets.cs`](../../games/NeonRift/Art/Assets.cs)

### Repainting imported art

Hybrid games repaint imported sprites with their own palette at load so they match the procedural art:

```csharp
private static PixelImage Repaint(PixelImage sheet, params (string From, Color To)[] swaps)
{
    foreach (var (from, to) in swaps) sheet.Recolor(Color.Hex(from), to);
    return sheet.Recolor(Color.Hex("141b1b"), Pal.Ink);
}
```

Source: [`games/CourtyardKey/Visuals/MonsterArt.cs`](../../games/CourtyardKey/Visuals/MonsterArt.cs)

## Audio files

`AudioMixer.AddSoundFile` decodes PCM WAV (8/16/24-bit, mono or stereo). `AddMusicFile` decodes WAV the same way
and hands any other extension, such as `.ogg` or `.mp3`, to the device to stream. See
[Audio](audio.md#sound-and-music-files).

## Credits convention

Every game that ships files lists them in `games/<Name>/CREDITS.md`: which files come from which pack, author,
source URL, licence, the date the licence was checked, and any changes made (joined strips, downmixed tracks).
All shipped assets today are CC0. Examples: [Neon Rift](../../games/NeonRift/CREDITS.md),
[Courtyard Key](../../games/CourtyardKey/CREDITS.md), [Trail Duel](../../games/TrailDuel/CREDITS.md). Meadow Run
ships no files and has no credits file.

## Limitations

- No asset pipeline, hot reload or content packing: files are read directly at load time.
- Supported formats are PNG (images), WAV (sounds and music), and OGG/MP3 (music only).
- No importers for map editors, Aseprite files or texture packer layouts; slicing is grid-based or done in code.

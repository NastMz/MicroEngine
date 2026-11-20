# CONTENT-1 (second evaluation): content production from the developer's workflow

Issue NastMz/MicroEngine#113, part of [CONSOLIDATION-1](consolidation-1.md) (section 2.3). Decision:
[ADR 0066](../decisions/0066-content-production-as-data-and-optional-tools.md). The first attempt (pull request 172,
closed) started from a texture format and a measurement; this one starts from what a developer does. Dated
2026-11-19; library facts were read that day from nuget.org and the projects' repositories.

## A. The need

A representative game: about 300 assets. Character sheets drawn in Aseprite (a dozen characters, five to ten
animations each), three tile sheets bought as PNGs, sixty loose UI images, two fonts, forty sound effects, six music
tracks. It ships on desktop, in browsers and on Android. One artist and one programmer; art changes every day.

The developer must be able to add, change, rename and remove any of these without writing loading, slicing, timing
or packing infrastructure, and must learn about a mistake before a player does.

## B. What that developer did before this change

| Asset | Steps | Code the game owned |
| --- | --- | --- |
| Aseprite character | Export a sheet and its JSON by hand after every edit (or script Aseprite's command line); keep both in `Content/` | None for tags; slices ignored; the `.aseprite` source shipped too if it sat in `Content/` |
| Bought strip or tile sheet | Measure cells and pivots in an image editor | A table per game: `Strip(b, content, "drone", "enemies/drone.png", 55, 52, new Vector2(27, 26))` 25 times in Neon Rift, pixel coordinates for tiles, the same in Trail Duel and Courtyard Key |
| Animation timing | Decide a rate | `art.Frames("gunship")[(int)(Age * 10) % 3]` at every draw site, or `new AnimationClip(frames, 8)` per clip |
| Any file | Type its path, frame and clip names as strings | Nothing checked them: `AssetLoadException` or `KeyNotFoundException` when the game reached the line |
| Loose UI images | `Assets.AtlasFromFolder` | Packed on every launch; no packed output to ship |
| Loading screen | `Assets.Preload` for images, atlases and fonts | A second path for sounds, music and custom kinds, which `Preload` refused |
| Audio | WAV for effects, OGG for music, by hand | A `.mp3` loaded and then failed to decode on one backend |
| Packaging | Everything under `Content/` copied as is | No validation beyond letter case |
| An art tweak | Restart the game | — |

## C. Existing tools and libraries

| Tool | Facts verified | Use |
| --- | --- | --- |
| Aseprite file format | Documented by its authors (`docs/ase-file-specs.md` in the editor's repository); chunked and versioned | Read directly |
| **AsepriteDotNet** 1.9.1 | MIT, Copyright 2024 Christopher Whitley; published 2026-04-03; targets net6.0 and net8.0; no dependencies; one 80 KB managed assembly; reads RGBA, grayscale and indexed files, every blend mode, tilesets, tags, slices; loads from a stream | **Adopted**, in an optional package |
| Own Aseprite reader | — | Rejected: blend modes, linked cels, groups, indexed colour and tilemaps would be ours to write and to follow, which is the reasoning of ADR 0052 for Tiled |
| TexturePacker, Free Texture Packer | Their JSON (hash and array) and Starling XML were already imported | Kept; rotated JSON frames now load |
| The engine's `SpriteAtlasBuilder` | Shelf packing into a power-of-two page, 1-pixel gap, optional extrusion; deterministic | **Reused** for offline packing: one packer, so a folder packs the same at load and offline |
| MaxRects packers for .NET (RectpackSharp and similar) | Tighter packing | Not adopted: tightness is not what blocked anyone; revisit with multi-page output |
| MonoGame MGCB | A content project, an editor, a compulsory build that turns every file into XNB | Rejected again: it makes the first game harder and ties every file change to a build; its real gains (shader compilation, GPU formats per platform) are covered by `microengine shaders` and not needed for textures today |
| Tiled, LDtk | Tiled is read by the optional `MicroEngine.Tiled` package (ADR 0052) | Precedent for the Aseprite package's shape |
| KTX 2.0, BC7 (pull request 172) | A 2048x2048 PNG takes 44 to 79 ms to load and makes three to four times its pixel size in garbage; BC7 loads faster and takes a quarter of the memory on desktop GPUs; browsers and phones need other formats (ASTC, ETC2) or a transcoder | Deferred, see H |

Not verified: AsepriteDotNet under a NativeAOT publish (it uses no reflection that the public API shows, and the
package builds with the AOT analyzers on for its own code, but the trimming check does not reference it yet).

## D. The target experience

The same developer, after this change:

| Asset | Steps | Code |
| --- | --- | --- |
| Aseprite character | Save the file in `Content/` | `var hero = Assets.Aseprite("characters/hero.aseprite"); var animator = hero.CreateAnimator("idle");` |
| Bought strip or tile sheet | Write its cell size, pivot and rate once in a `.sheet` file next to it | `Assets.Atlas("enemies/enemies.sheet")`, `atlas.Clip("drone").FrameAt(Age)` |
| Names | `microengine content names` | `GameContent.Enemies.EnemiesSheet.Clips.Drone`: a rename fails to compile |
| Mistakes | `microengine content check`, locally and in CI | — |
| Loose UI images | `Assets.AtlasFromFolder`, or `microengine content pack Art/ui` and `Assets.Atlas("atlases/ui.json")` | One line differs |
| Loading screen | `Assets.Preload(everything)` | No code per kind |

Neon Rift, before and after:

```csharp
// before: 25 lines like this, and tile coordinates in a dictionary
Strip(b, content, "drone", "enemies/drone.png", 55, 52, new Vector2(27, 26));
// ... and at the draw site
DrawFlashing(r, art.Frames("drone")[(int)(Age * 12) % 4], position, flip, Flash);
```

```json
// after: Content/sprites.sheet
{ "image": "enemies/drone.png", "name": "drone", "cell": [55, 52], "origin": [27, 26], "fps": 12 },
```

```csharp
var b = new SpriteAtlasBuilder().AddSheet(content.Sheet(Names.Path));
DrawFlashing(r, art.Clip(Names.Clips.Drone).FrameAt(Age), position, flip, Flash);
```

## E. What the engine owns and what stays with the game

The engine owns: reading the descriptions and the artist's files, cutting, packing, clip construction, the asset
lifetime, validation rules, the names file's shape, staging loads across threads. The game owns: which files exist
and how they are organised, what a clip means, when assets load and which scope holds them, any sprite derived in
code (Neon Rift's aiming poses), and whether to run any offline step at all.

## F. How it fits

- **`Assets` and scopes**: a `.sheet` and an Aseprite file are loaded by `Assets.Atlas` under the `atlas` kind, cached,
  shared and released like any atlas. A loader that makes a `SpriteSheet` for an extension is the extension point; the
  Aseprite package uses it, so no second registry exists.
- **`SpriteAtlas`, `AnimationClip`, `Animator`**: unchanged types. The atlas gained clips from any source and
  `CreateAnimator`; a clip at a fixed rate is the same clip a game built by hand, so migrated games draw the same
  frames.
- **`IRenderer`**: untouched. Rotated frames are turned upright at load instead of adding a rotated draw path.
- **Packaging and the CLI**: `microengine content` sits beside `microengine shaders`; its outputs are ordinary files
  under `Content/` or the project, checked with `--check`.
- **Loading**: every loader may be staged; `Preload` drives the stages, so custom kinds get background loading too.

## G. Trade-offs

- A `.sheet` file is a format of ours. It is plain JSON describing ordinary images, optional, and small; the
  alternative was every game's own table in C#.
- Sheets are cut and packed at load (a copy of each frame, then one upload). For Neon Rift's 216 sprites this is what
  the game already did in code. A game that minds can pack offline.
- Generated names are a committed file that can go stale; `--check` and the compiler bound the damage. A source
  generator would never be stale but would have to re-implement three parsers outside the engine and run in every
  build.
- An eleventh (with the ECS package, twelfth) package to release.
- A packed clip's fixed rate is written as milliseconds per frame, so it comes back as per-frame durations
  (83.333 ms for 12 frames per second).

## H. Decisions per capability

| Capability | Decision | Reason |
| --- | --- | --- |
| Sheet descriptions (`.sheet`) | **Integrated**, core | Removes the slicing tables of every demo |
| Clips and animators from data | **Integrated**, core | Removes frame-rate constants from draw code |
| Aseprite files read directly | **Integrated**, optional package `MicroEngine.Aseprite` over AsepriteDotNet | Removes the export step; optional because games without Aseprite art should not carry a parser (the Tiled precedent) |
| Rotated frames | **Integrated** for JSON atlases, by un-rotating at load | A `Sprite` is an upright rectangle on three renderers; turning pixels once costs nothing at draw time. Starling XML stays refused: its rotated convention could not be checked against the tools here |
| Generated names | **Integrated** as `microengine content names` with `--check` | Chosen over a source generator: one set of parsers (the engine's), no cost in ordinary builds, the shape `shaders --check` already established. An opt-in build hook that runs it automatically is a possible later step |
| Validation | **Integrated** as `microengine content check` | The same loaders the game uses, run before it does |
| `Preload` for every kind | **Integrated**, with staged loaders | A loading screen needed a second path for audio and custom kinds |
| Offline packing | **Integrated** as `microengine content pack`, writing the JSON the engine already reads | No new format; the engine's packer, so load-time and offline packing agree |
| Trimming, extrusion | **Integrated** as options of packing | |
| Multi-page atlases, tighter packing | **Deferred** | One page grows to fit; a 4096 limit is reported by `check`. It needs `SpriteAtlas` to hold several textures, which is a change to a type every game uses; nothing measured here shows a game reaching the limit |
| Reload during development | **Deferred with a design** (below) | Doing it behind existing handles needs a change in how sprites refer to their atlas that is larger than this unit |
| Platform variants (`@web`, `@mobile`) | **Deferred** | The natural place is a content source that resolves a suffix, about fifty lines, but which variants exist is a packaging decision (what each package ships) that belongs with the packaging targets; no asset in the repository has two forms to prove it on |
| Baked glyph atlases, BMFont files | **Deferred** to the text work | They are a font kind (`IFont`), not a content-pipeline matter; the audit found no developer blocked, and scalable fonts already render on every platform |
| Compressed textures, mipmaps through `Assets.Texture` | **Deferred** to the graphics initiative | The obstacle of pull request 172 is gone (a `Texture` exposes its device texture), but a compressed path needs a format per platform family and a fallback, and mipmaps need device support that initiative owns. Measured value (a quarter of the memory, faster loads for 2048-pixel pages) is real for HD games and is the reason to do it there |
| `PixelImage.ToRgba` in one step | **Ported** | Self-contained, identical bytes |
| `PremultipliedColor.PremultiplyInPlace` without a division | **Not ported here** | It lives in the graphics contract assembly, outside this unit's files; it is self-contained on the closed branch and can be taken as is |
| Excluding source files from packages | **Guide and check** | `content check` warns about sources under `Content/`. Excluding by default would silently drop a `.psd` a game loads on purpose (the engine reads PSD) |
| Audio conversion | **Guide and check** | `content check` reports WAVs that are not PCM and MP3 music; decoding differences belong to the audio initiative |

### Reload: the design that was not built

Goal: a changed file under the content folder is reloaded while the game runs on desktop, behind the handles games
already hold.

1. `AssetStore.Reload(path)`: for every entry made from the file (a loader reports the files it read through its
   context), run the loader again and swap the entry's value, keeping its `Asset<T>` handle valid.
2. Objects games hold directly need to survive the swap. A `Texture` can take new pixels in place when the size is
   unchanged, and be re-created behind the same object otherwise. A `SpriteAtlas` cannot today: `Sprite` is an
   immutable record that games copy into fields, clips and animators, so a reloaded atlas would leave every held
   sprite pointing at old rectangles. Either sprites become views that resolve through their atlas at draw time (a
   change to the most used type of the 2D API, with a cost on the draw path to measure), or reload is limited to
   changes that keep the layout (pixels only), which silently fails for the common case of adding a frame.
3. Watching: a `FileSystemWatcher` on the content folder in desktop debug builds only, events marshalled to the
   game's thread and applied between frames (`Assets.Update`), behind a `--watch` launcher option; absent from trimmed
   builds and from sources that are not folders.

Step 2 is the reason it stops here: it is a decision about `Sprite`, not about content.

## I. Validation

- Tests: sheet parsing and its errors, clips, animators, rotated frames, packing round trip and trimming
  (`SpriteSheetTests`, `AtlasImportTests`); preloading every kind, with options, on and off worker threads
  (`AssetPreloadTests`); Aseprite files written from the documented format (`MicroEngine.Aseprite.Tests`); the tool's
  three commands and their `--check` (`MicroEngine.Cli.Tests/ContentTests`), including the repository's own content.
- Consumers: Neon Rift's strips, props and tiles moved to `Content/sprites.sheet` and its clip names to generated
  constants; 156 captures over its three stages, poses, menus and ending are byte-identical to the previous code.
  The tutorial game and the animation example take their clips from a description.
- Not run: browsers, Android, iOS (the code paths read through `IContentSource` and use no file system at run time);
  the tool on Linux and macOS; a NativeAOT publish that includes the Aseprite package.

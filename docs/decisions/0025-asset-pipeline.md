# ADR 0025: Asset pipeline: managed decoders, FontStashSharp fonts, imported atlases, an engine asset cache

## Status

Accepted. Amends [ADR 0004](0004-external-asset-files.md) (external files) and [ADR 0002](0002-code-first-content.md)
(code-first content stays possible, no longer the only comfortable path). Evidence: [ASSET-1](../evaluations/asset-1.md).

## Context

MicroEngine was shaped by pixel-art games: textures were always sampled nearest, the canvas was presented at integer
scales when possible, text came only from the code-built 5×7 `BitmapFont`, images had to be non-interlaced 8-bit PNGs,
atlases were packed at run time from `PixelImage`s, and every game declared its own `Content/**` item and opened its
files through `ContentFolder`. A 2D HD game (1280×720 or 1920×1080 art, smooth scaling, TTF/OTF text, atlases made by
packing tools) had to work around all of it. The owner asked for a first asset pipeline that serves both styles without
committing the engine to pixel art or blocking a later move to 3D.

## Options studied

| Need | Studied | Outcome |
| --- | --- | --- |
| Content build | MonoGame Content Pipeline (MGCB, MGCB Editor, `.mgcb` projects, XNB output); FNA (raw files, no pipeline); raylib and SDL (raw files at run time) | Raw files at run time; the build only checks and lists them |
| Images | Current managed PNG decoder; StbImageSharp (Unlicense/MIT, managed port of stb_image); ImageSharp; SkiaSharp; System.Drawing; SDL_image / raylib loaders | StbImageSharp for every format |
| Fonts | FontStashSharp (Zlib) over StbTrueTypeSharp; StbTrueTypeSharp alone; SixLabors.Fonts; FreeType (SharpFont, FreeTypeSharp); SDL_ttf; SDF/MSDF (msdf-atlas-gen); AngelCode BMFont | FontStashSharp |
| Atlases | TexturePacker, Free Texture Packer, ShoeBox, Aseprite exports; libGDX `.atlas`; MonoGame.Extended's importers; run-time packing | Import TexturePacker/Aseprite JSON and Starling XML; keep run-time packing |
| Asset management | MonoGame `ContentManager`; Unity Addressables (reference counting, async); Godot `ResourceLoader` (cache by path, threaded loading) | An engine cache with scopes, no reference counting exposed to games |

## Decision

- **Files ship as they are; the build checks, it does not convert.** MGCB earns its cost by compiling shaders and
  converting textures to GPU formats per platform. MicroEngine's renderers (SDL_Renderer, raylib's 2D batch) take
  neither, and decoding a PNG of game size takes milliseconds (measured in ASSET-1), so a content compiler would add a
  tool, a content project and rebuilds without benefit. `Content/**` becomes part of every game automatically (no item in
  the project file), and the build fails on files whose names differ only in letter case (ME0010). At run time
  `ContentFolder` compares each requested name with the directory listing and rejects a wrong letter case on systems that
  would forgive it (Windows, macOS), because Linux, Android and browsers would not; listings are read once per folder, so
  no build-written file list is needed. GPU texture formats and shaders return with GRAPHICS-1; the pipeline can then
  convert at build time where it pays.
- **Images are decoded in managed code on every platform, by StbImageSharp.** PNG (every variant), JPEG, BMP, TGA, GIF
  (first frame) and PSD. The engine's own PNG decoder was kept at first; measured, it decoded all 245 PNGs of the
  repository to the same pixels five times more slowly, so it was removed. Rejected:
  ImageSharp (Six Labors Split License: commercial terms above a revenue threshold), SkiaSharp (a native library per
  platform, tens of megabytes, and a WebAssembly build of its own), System.Drawing (Windows only), backend loaders
  (headless tests and the two backends would decode differently). Uploads pass the image's memory to the backend without
  an intermediate copy.
- **Scalable text uses FontStashSharp.** It rasterizes glyphs of TTF and OTF (TrueType and CFF outlines) with
  StbTrueTypeSharp on demand, at any size, into shared atlas pages, with kerning, fallback faces, measuring and
  outline/blur effects; it runs on any engine through two small interfaces (texture creation and region upload, quad
  drawing). Writing the same glyph cache over StbTrueTypeSharp would repeat mature code. Rejected: SixLabors.Fonts
  (licence, layout only), FreeType and SDL_ttf (native, per backend), SDF/MSDF (need a shader, which SDL_Renderer lacks:
  GRAPHICS-1), BMFont alone (one size per bake). The code-built `BitmapFont` stays the default pixel font; both implement
  `IFont`, which the UI widgets take.
- **Filtering is a property of each texture, scaling a property of the game.** `TextureFilter` (nearest, linear) is set
  per texture and can change at run time; `GameSettings.Scaling` chooses how the canvas reaches the window
  (`CanvasScaling.Pixel`: whole factors when they fill enough, nearest; `CanvasScaling.Smooth`: fill the window, linear),
  and the game's default texture filter follows it unless `GameSettings.TextureFilter` says otherwise. Nearest and whole
  factors remain the pixel-art default, no longer the engine's only behaviour. Atlases sampled linearly get extruded
  edges so neighbours never bleed.
- **Atlases come from the tools artists use.** The engine reads the JSON of TexturePacker, Free Texture Packer and
  Aseprite (hash and array forms; Aseprite's frame tags become animation clips with each frame's duration) and Starling
  XML (TexturePacker, ShoeBox, Kenney's packs), trimmed frames included. Rotated frames are refused with a message
  (SDL_Renderer would need a per-sprite rotation path); `SpriteAtlasBuilder` keeps packing loose images at run time.
- **`Game.Assets` owns loading, sharing and releasing.** One cache keyed by content path and kind returns the same
  texture, atlas or font to every caller and disposes them when its scope ends: the game's root scope at shutdown,
  `Assets.CreateScope()` for a level or a scene. Scopes look up their parents first, so nothing is loaded twice. No
  reference counting exposed to games: lifetimes follow scopes, which games already think in (see the amendment below
  for how the cache tracks scopes internally). Missing files, unsupported formats and
  wrong-case names fail with `AssetLoadException` naming the path; `Assets.Statistics` reports counts, texture memory
  and load times, and loads during play are counted so hitches are visible. `Assets.Preload` decodes files on worker
  threads where the platform has them and uploads a few per frame within a time budget; the browser, which runs
  .NET on one thread, decodes in that budget too.
- **Responsibilities.** The platform finds the files (the `Content` folder next to the executable, the unpacked Android
  assets, the browser's virtual file system) and turns pixels into textures; the engine decodes, caches, packs and
  releases; the game decides what to load and when, and which scope owns it.

## Consequences

- The core package gains its first dependencies: FontStashSharp and its four managed dependencies (StbImageSharp,
  StbTrueTypeSharp, FontStashSharp.Base, Cyotek.Drawing.BitmapFont), all permissive and managed, listed in
  `THIRD-PARTY-NOTICES.md`.
- Games no longer declare `Content/**`; a project that still does gets the same files once.
- `IGraphicsDevice` gains texture filters and region updates; custom graphics devices must implement them (Alpha).
- Text in HD games and UI widgets can use any font; pixel-art games keep the built-in face unchanged.
- No compressed GPU textures, mipmaps or signed-distance-field text until GRAPHICS-1 gives the renderers shaders and
  texture formats.

## Amendment (2026-11-02, ARCH-1 stage 0)

The ARCH-1 audit (defect D9) found that sibling scopes loaded the same file twice and that `Unload` or disposing a
scope disposed an object another scope still used. The decision above stands: games still never count references, and
lifetimes still follow scopes. Internally, the cache now keeps one entry per (file, kind, options) for the whole game
and tracks which scopes hold each entry. Every scope that asks for an asset holds it, so sibling scopes share one
object; `Unload` and disposing a scope release that scope's hold, and the asset is disposed when the last holding scope
releases it. A scope's `Count` is the number of assets it and its children hold, including parent assets they asked
for; `Loaded`, `Statistics` and the root's `Count` count a shared asset once.

# ASSET-1: Asset pipeline, scalable fonts and content management

Evaluation of the first asset pipeline (2026-11-02, branch `feat/asset-1`). Decisions: [ADR 0025](../decisions/0025-asset-pipeline.md).
Task record: [`odd/tasks/asset-1.md`](../../odd/tasks/asset-1.md).

## Starting point

The engine served pixel art only: textures sampled nearest, the canvas presented at whole factors, text from the
code-built 5×7 `BitmapFont`, non-interlaced 8-bit PNGs only, atlases packed at run time from `PixelImage`s, and every
game declaring its own `Content/**` item and opening files through `ContentFolder`. Nothing cached or released assets.

## Research: what exists and what was reused

| Need | Studied | Result |
| --- | --- | --- |
| Content build | MonoGame Content Pipeline (MGCB, MGCB Editor, `.mgcb` content projects, XNB output, per-platform processors); FNA (no pipeline, raw files, FNA3D formats); raylib and SDL (raw files) | MGCB pays for itself with compiled effects and GPU texture formats (DXT/ETC/PVRTC per platform). SDL_Renderer and raylib's 2D batch take neither, so a content compiler would add a tool and rebuilds for nothing today. Raw files at run time, with build-time checks only. Revisit with GRAPHICS-1. |
| Images | Engine PNG decoder; StbImageSharp 2.30 (Unlicense/MIT, managed stb_image); SixLabors.ImageSharp (Six Labors Split License: commercial terms above a revenue threshold); SkiaSharp (native per platform, own WebAssembly build); System.Drawing (Windows only); SDL_image, raylib loaders (backend-specific, headless tests could not decode) | StbImageSharp for every format; the engine's decoder removed after measurement (below). |
| Fonts | FontStashSharp 1.3 (zlib, renderer-agnostic glyph cache over StbTrueTypeSharp); StbTrueTypeSharp alone; SixLabors.Fonts (licence, layout without rasterization); FreeType via SharpFont/FreeTypeSharp (native); SDL_ttf (native, SDL only); SDF/MSDF with msdf-atlas-gen (needs a shader); AngelCode BMFont (one size per bake) | FontStashSharp: dynamic glyph pages, any size from one face, fallback faces, kerning, measuring, effects; two small interfaces to plug into the engine. |
| Atlases | TexturePacker (JSON hash/array, Starling XML), Free Texture Packer, ShoeBox, Aseprite (JSON with frame tags and durations), libGDX `.atlas`, MonoGame.Extended importers | Import the JSON and Starling XML formats those tools share; keep run-time packing for loose images. libGDX, Spine, slices and rotated frames left out. |
| Asset management | MonoGame `ContentManager` (cache by name, `Unload` all, one manager per level); Unity Addressables (reference counting, async handles); Godot `ResourceLoader` (cache by path, threaded loading) | A cache keyed by path, kind and options, with child scopes per level (the ContentManager pattern, without reference counting), and background preloading with a per-frame upload budget. |

Reused: StbImageSharp, FontStashSharp (with StbTrueTypeSharp, FontStashSharp.Base, Cyotek.Drawing.BitmapFont), all
managed and permissive. Written in the engine: the cache and its scopes, the atlas importers (JSON and XML parsing over
`System.Text.Json` and `System.Xml.Linq`, trim-safe), texture filters and region updates on both backends, smooth canvas
scaling, the build targets.

## What was built

- **Filtering and HD canvases.** `TextureFilter` per texture (changeable at run time), `GameSettings.Scaling`
  (`CanvasScaling.Pixel` / `Smooth`), a default texture filter that follows it, `Texture.Update` for regions, uploads
  without a conversion copy, devices that forget disposed textures, windows sized to the display
  (`WindowSizing`), smooth canvases captured at their own size.
- **Images.** PNG (all variants), JPEG, BMP, TGA, GIF (first frame), PSD through StbImageSharp.
- **Fonts.** `ScalableFont` (TTF, OTF with TrueType or CFF outlines), `SizedFont`, `FontOptions` (antialiased or hard
  pixel edges, filter, page size, kerning, pixel snapping), `Prepare`, fallback faces; `IFont` shared with `BitmapFont`;
  `DialogueBox` and `SelectionList` take any `IFont` and scale their margins with it (unchanged for the built-in font).
- **Atlases.** `AtlasData` (TexturePacker/Free Texture Packer/Aseprite JSON, Starling XML; trimming, pivots, durations,
  tags with directions), `SpriteAtlas.Load`/`Create`/`Clip`, `AnimationClip` with a duration per frame, `Frames` numbered
  from 0 or 1, extruded edges for linearly sampled run-time atlases.
- **Lifecycle.** `Game.Assets` (`AssetCache`): textures, atlases, folder atlases, fonts, bytes and text; scopes;
  `Unload`; `AssetLoadException`; exact letter case on every system; statistics, loads during play reported when slow
  and recorded by `--profile`; `Preload` with `AssetBatch`.
- **Pipeline.** `Content/**` ships with every game without a project item (projects that list it keep their item);
  names differing only in letter case fail the build (ME0010). The template and the demos dropped their item.
- **Consumer.** `samples/AssetGallery`: a 1280×720 smooth game made only from Kenney's CC0 art (PNG, a JPEG, Starling
  XML, an Aseprite-format JSON with tags, a folder of items) and three OFL fonts (Inter TTF, Inter Bold OTF, Source Serif
  4 OTF/CFF). `sdk/tests/external.mjs` builds it outside the repository from the packed feed, plays its tour, packages it
  and looks for its files inside the packages.

## Measurements

Reference machine: Windows 11, 12 cores, SDL with Direct3D 11, .NET 10.0.14. `benchmarks/AssetBench` (median of 9 runs
after a warm-up; allocations on the calling thread):

| Measurement | Median | Allocated |
| --- | ---: | ---: |
| PNG decode, 512×512 RGBA | 1.31 ms | 2.1 MB |
| PNG decode, 512×1024 RGBA | 3.19 ms | 4.2 MB |
| PNG decode, 512×1024, StbImageSharp alone | 2.14 ms | 2.1 MB |
| JPEG decode, 640×480 | 8.10 ms | 2.4 MB |
| Texture upload, 1024×1024 (4 MB) | 0.84 ms | 216 B |
| Texture upload, 2048×2048 (16 MB) | 3.14 ms | 216 B |
| Texture region update, 32×40 (one glyph) | 24.5 µs | 152 B |
| Atlas load, Starling XML + 512×512 PNG | 2.31 ms | 2.3 MB |
| Atlas load, Aseprite JSON + 256×256 PNG | 1.19 ms | 602 KB |
| Folder atlas, 10 PNGs packed at load | 2.09 ms | 167 KB |
| Cache hit, `Texture(path)` | 0.1 µs | 0 B |
| Font open (TTF 412 KB, OTF/CFF 241 KB) | 0.01 ms | 1.2 KB |
| `Prepare` printable ASCII at 24 px, TTF / OTF-CFF (94 glyphs) | 3.45 / 2.86 ms | 1.0 MB |
| `Prepare` printable ASCII at 64 px, TTF | 5.33 ms | 1.1 MB |
| Draw a prepared 38-character line | 15.8 µs | 0 B |
| Measure a 38-character line | 3.3 µs | 0 B |

Findings:

- **The engine's PNG decoder was the slow part.** Before the switch, the 512×1024 sheet took 15.99 ms with the engine's
  decoder against 2.30 ms with StbImageSharp (and the Starling atlas 9.79 ms against 2.31 ms after). Both decoded all
  245 PNGs of the repository (demo art, icons, captures) to identical pixels, so the engine's decoder was removed.
  `PixelImage.Decode` still allocates twice what StbImageSharp does (its byte buffer, then the `Color` array): a
  later optimisation can decode into the image's own array.
- **Glyphs cost 30 to 60 µs each** to rasterize and upload (two region uploads per glyph: FontStashSharp clears the
  padded area, then writes the glyph). Ninety-four glyphs at one size take about 3 ms, so `Prepare` belongs in `Load`;
  after it, drawing and measuring allocate nothing and do no font work (asserted by `ScalableFontTests`).
- **Glyph pages of 1024² cost 4 MB each.** The gallery's three fonts took 12 MB of its 16.8 MB of textures; with the
  default page reduced to 512² (1 MB, pages added as they fill) the gallery uses 7.8 MB.
- **Asset lookups allocate nothing.** The first version keyed the cache by concatenated strings and captured parameters
  in lambdas, about 42 bytes per lookup; a struct key and capture-free loaders brought it to 0 B (asserted by a test),
  so games may ask `Assets` for a texture while drawing.
- **Hitches are visible.** On a cold packaged run the gallery's first version loaded its backdrops on the first frame
  (13 to 111 ms each); the cache reported it, the external check failed on it, and the sample now loads everything in
  `Load` and preloads its level atlas (decoded on a worker, uploaded within the 4 ms budget; only time on the game's
  thread counts as a hitch).

Gallery in real time (`--frames 600 --profile`, 100 Hz display, SDL Direct3D 11): 0 hitches, work p50 0.13 ms and p99
0.26 ms, interval p99 11.04 ms; load phase 153 ms for 9 assets (95 ms of it decoding and uploading). Its per-frame garbage
went from 21.5 KB (strings, wrapping and LINQ in the sample's `Draw`) to about 200 bytes after moving them out of the
frame.

## Platforms

| Platform | Evidence |
| --- | --- |
| Windows | Unit, desktop-native (SDL and raylib filter and region-update captures) and sample tests; the gallery played, profiled and captured; `external.mjs` packaged and ran the gallery from the feed with its fonts, atlases and images inside |
| Linux | `external.mjs` packaged the template game and the gallery's Linux package locally (cross-built); CI runs tests and the external check on Linux (see the pull request) |
| WebAssembly | `external.mjs` built the gallery's web package: fonts, atlases and images in the page and in `content.txt` |
| Android | `external.mjs` built the template's APK; it could not be inspected locally (`ANDROID_HOME` unset); the platform workflow plays the demos, whose content now goes through the new targets and exact-case checks, on an emulator |
| macOS, iOS | Through CI (SDK external check on macOS, platform workflow on the iOS simulator), demos only for iOS |

Local environmental failures, not caused by this work: `sdk/tests/packages.mjs` reports build-machine paths for a local
pack without `-p:MicroEngineRelease=true` (as CI passes), and the APK check needs `ANDROID_HOME`.

**A defect found by a stalled test run.** The core tests stalled in about one run in five. Thread stacks of a stalled
run (`dotnet-stack`) showed StbTrueTypeSharp inside `stbtt__find_table` for the eight garbage bytes of the "not a font"
test: stb_truetype reads fonts through raw pointers without bounds checks, so a truncated or foreign file reads past its
array and hangs or crashes depending on the memory behind it. `ScalableFont` now checks the header and that every table
of the directory lies inside the file before the rasterizer sees it; fifteen runs in a row then passed. Fonts and images
remain trusted content shipped with the game: the check refuses broken files, it does not make the rasterizer safe for
deliberately crafted ones.

**A visual reference that predates PERF-2.** The validation run failed CourtyardKey's Windows capture comparison (6 of
26 captures, against a reference recorded from the PRODUCT-1 packages). Playing the same scripts with `main` (98b1a7d)
and with this branch gave byte-identical captures with hardware Direct3D 11 and with WARP, so the mismatch reproduces on
`main` and is left to whoever re-records the reference.

## Limitations

- No build-time conversion: no GPU-compressed textures (BCn, ASTC, ETC2), no mipmaps (art drawn far smaller than its
  pixels shimmers), no hot reload.
- Text: no shaping (Arabic, Devanagari, ligatures), no right-to-left, no colour emoji, no rich text, no SDF text; each
  size is rasterized separately, so freely zoomed text fills glyph pages.
- Atlases: rotated frames, Aseprite slices and 9-slices, libGDX, Spine and Tiled files are not read; animated GIFs give
  their first frame.
- `Preload` loads with default options only; there is no reference counting across scopes (a scope's asset is
  released with it even if another scope's code kept a reference).
- The UI widgets scale their margins with the font, but other UI measures (panels, toasts, achievement views) are
  still in pixel-art units.
- The gallery runs on desktops and its web package is built; it is not part of the phone experiments, which play the
  demos.

## Recommendations for DEMO-5

1. Start from `CanvasScaling.Smooth` at 1280×720 (or 1920×1080 for 4K-crisp text) with linear defaults, and keep
   nearest only for deliberately crisp layers; author art at no more than twice its drawn size.
2. Pack art with TexturePacker or Aseprite (rotation off), with pivots or a shared `AtlasOptions.Pivot`; use Aseprite
   tags for animation timing instead of fixed rates.
3. Load in `Load` and per-level scopes, show a loading screen with `Preload` between levels, and keep
   `assets.loadsDuringPlay` at 0 in `--profile` runs.
4. `Prepare` every font size the HUD uses; keep strings that change (scores) out of per-frame allocation by rebuilding
   them only when their value changes.
5. Before DEMO-5 depends on them, consider: decoding straight into `PixelImage` memory (half the load allocation), a
   resolution-independent UI layer (panels and toasts sized from the font), and GRAPHICS-1 for mipmaps, GPU formats and
   SDF text.

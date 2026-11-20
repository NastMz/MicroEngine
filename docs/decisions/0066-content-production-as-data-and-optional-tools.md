# ADR 0066: Content production: sheets and animations as data, optional offline tools, no content build

## Status

Accepted (2026-11-19). The SDK is in Alpha. Evidence: [CONTENT-1, second evaluation](../evaluations/content-2.md).
Issue NastMz/MicroEngine#113, part of [CONSOLIDATION-1](../evaluations/consolidation-1.md). Amends
[ADR 0025](0025-asset-pipeline.md) (rotated frames, what `Preload` takes, offline steps beside the build) and
[ADR 0019](0019-sdk-packages.md) (one more optional package); builds on
[0034](0034-content-handles-loaders-sources.md), [0052](0052-tiled-maps-optional-package.md) and
[0064](0064-drawing-api-over-the-device-layer.md).

## Context

Loading a file was easy; producing a game's content was not. Every demo cut its sprite sheets with a table typed in
C#, wrote frame rates into draw code, named content by strings that failed only when the game reached them, packed
loose images on every launch, and kept a second loading path for audio because `Preload` refused it. A first attempt
answered with a texture format (KTX) and was closed: it made the developer run a conversion and own a renderer, and
removed none of that work.

## Decision

- **No content build and no engine format.** Files load as they are. Everything below is optional, and what the
  optional steps write is an ordinary file a game could have made by hand.
- **A sprite sheet is described by data next to its images.** A `.sheet` file (JSON) gives cell size, margin,
  spacing, named strips, single sprites, pivots and clips with a rate or a duration per frame. `Assets.Atlas` loads it
  into an ordinary `SpriteAtlas`; `Assets.Sheet` gives the frames as pixels (`SpriteSheet`) for a game that packs
  them with sprites of its own.
- **Clips belong to the atlas.** Whatever described the atlas (a `.sheet`, an Aseprite file, Aseprite's JSON) gives
  its clips with their timing, direction and repeat; `SpriteAtlas.CreateAnimator` returns an `Animator` holding
  them.
- **Aseprite files are read directly, by an optional package.** `MicroEngine.Aseprite` reads `.aseprite` and `.ase`
  through AsepriteDotNet (pinned, MIT, listed in `THIRD-PARTY-NOTICES.md`); no AsepriteDotNet type is public. It
  plugs in as an asset loader that makes a `SpriteSheet`, which is the extension point for any other artist's format.
- **Rotated frames are turned upright at load** (JSON atlases), by cutting the frames out and packing them again. No
  renderer gains a rotated path.
- **Loaders may be staged.** `IStagedAssetLoader` splits a load into a part for any thread and a part for the game's
  thread; `Assets.Preload` drives it for every kind, with the options of the direct calls. The engine's loaders are
  staged.
- **`microengine content`** holds the offline steps: `check` (the engine's own loaders run over the folder),
  `names` (file, sprite, strip and clip names as C# constants) and `pack` (a folder into one atlas in the JSON the
  engine already reads, packed by the engine's own packer). `names` and `pack` have `--check`, in the manner of
  `microengine shaders --check`.

## Alternatives considered

- **A compulsory content build (MGCB and XNB, or our own)**: rejected. It ties every file change to a build and gives
  a 2D engine that decodes PNGs in milliseconds nothing it lacks.
- **KTX and block compression first**: deferred to the graphics work. It serves memory and load time of large
  pages, not the work of producing content.
- **A source generator for names**: rejected for now. It would re-implement the sheet, atlas and Aseprite parsers
  outside the engine (generators cannot reference it) and run in every build. A committed file with a `--check`
  uses the engine's own parsers and costs ordinary builds nothing.
- **An Aseprite reader of our own in the core**: rejected for the reason ADR 0052 rejected an own Tiled reader:
  blend modes, indexed colour, linked cels and tilemaps would be ours to follow.
- **AsepriteDotNet inside the `MicroEngine` package**: rejected. Games without Aseprite art would carry it.
- **A rotated draw path in the renderers**: rejected. Three renderers and every draw overload, to save what one
  repack at load already saves.
- **A documented recipe only**: every game would still write the tables.

## Consequences

- One more optional package to release, and the tool carries AsepriteDotNet.
- `SpriteAtlas.Clip(name)` repeats as the data says (a tag with a repeat count plays once): listed in the migration
  notes with the other signature changes.
- A `.sheet` file is a format this project now owns and must keep reading.
- Deferred, each with its reason and, for reload, its design, in the evaluation: reload while the game runs,
  per-platform variants, multi-page atlases, compressed and mipmapped textures, baked and bitmap fonts.
- Outside this change and still needed: the package list of the release tooling (`sdk/version.mjs`), the API
  reference's assembly list (`site/scripts/api-reference.mjs`) and the trimming check must learn the new package.

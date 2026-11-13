# TILE-1: maps from external editors and large 2D maps

Issue NastMz/MicroEngine#121. Record: [`odd/tasks/tile-1.md`](../../odd/tasks/tile-1.md). Date: 2026-11-13.
The question is how a developer outside this repository gets levels drawn in a map editor into a MicroEngine game,
and whether large maps need anything the engine lacks. The rule is that of every recent evaluation: could an external
developer build a game whose levels come from an editor today with the published SDK?

## 1. What exists today

- **Tile data.** `TileLayer` (a flat `int[]` of ids), `TileGrid` (ids to `TileFlags`), `ICollisionGrid` and
  `Autotile` live in `MicroEngine.World` (`src/MicroEngine.Physics.Tiles/TileMap.cs`) and ship inside the
  `MicroEngine` package. Square tiles only, one size per grid. The only input format is ASCII rows plus a
  character legend (`TileLayer.Parse`), and objects are placed by marker characters (`TileLayer.Find`).
- **Physics.** `TilePhysicsWorld2D.AddTiles(body, grid)` and `TileMotion` read any `ICollisionGrid`; a map's
  origin of data does not matter to them.
- **Drawing.** `TileRenderer.Draw(renderer, layer, tileSize, camera, spriteFor)` draws the cells in view, one
  `IRenderer.Draw` per cell. It culls with `Camera2D.View` (position and view size), not `VisibleBounds`, so a
  camera zoomed out below 1 leaves the outer tiles undrawn (`src/MicroEngine/World/TileRenderer.cs:18`). The
  manual says it "touches only visible cells", which is true only at zoom 1 without rotation. It cannot draw a
  flipped or rotated tile, although `DrawOptions` has `FlipX`, `FlipY` and `Rotation`.
- **Content.** A game adds a file type with `Assets.Loaders.Add(new AssetLoader<T>(kind, ctx => ..., extensions))`;
  `AssetLoadContext.ReadBytes(path)` reads files the asset references, as the glTF loader does for its buffers
  (`src/MicroEngine.Graphics3D/MeshAssets.cs:25`). Loading is synchronous on every target, browser included
  (ADR 0034).
- **Documentation.** `docs/manual/world.md` and `docs/manual/rendering.md` state there is no importer for map
  editors and tell the reader to parse the files themselves.

**Answer to the rule today:** an external developer can build tile games whose levels are typed as text in C#. A
developer who draws levels in Tiled or LDtk (the usual workflow for a platformer, a top-down RPG or a metroidvania)
must write a parser for the editor's format, the mapping from global tile ids and flip bits to sprites, the reading of
object layers and custom properties, and the bridge to `TileGrid`. That is a few hundred lines per game before the
first level shows, and nothing in the SDK helps. The gap is real.

## 2. Formats and parsers

| Format | Editor | What it stores | .NET readers |
| --- | --- | --- | --- |
| TMX (XML), TMJ (JSON), external tilesets TSX/TSJ, templates TX/TJ | Tiled (GPL editor, open format, the most used 2D map editor) | Tile layers (finite or infinite in chunks, ids with flip and rotation bits, optional base64 + gzip/zlib/zstd), object layers (rectangles, points, ellipses, polygons, tile objects), image layers, groups, typed custom properties and custom classes on everything, tile animations, Wang sets | **DotTiled** 1.0.0 (MIT, Oct 2025, last push Apr 2026, net8.0, no dependencies, TMX + TMJ + TSX + templates + infinite maps + custom types); TiledCS (MIT, unmaintained since 2022, TMX only); TiledSharp (Apache, unmaintained) |
| LDtk JSON (`.ldtk`, per-level `.ldtkl`) | LDtk (MIT, by the Dead Cells level designer) | Levels in a world, IntGrid layers (ids meant for collision), auto-layers, tile layers, entities with typed fields | Loaders tied to MonoGame's content pipeline (LDtkMonogame, Monogame-LDtk-Importer); code generated from the official JSON schema with QuickType |

**DotTiled in detail.** The public entry point is `Loader(IResourceReader, IResourceCache, customTypes)`;
`IResourceReader` is one method, `string Read(string path)`, and every referenced tileset or template is resolved
through it relative to the map's folder. That maps one to one onto `AssetLoadContext.ReadBytes`, so maps load
from any content source (folder, zip, browser package) without touching the file system. Parsing uses `XmlReader`
and `System.Text.Json` DOM types; reflection appears only in the optional `CustomClassDefinition.FromClass` helpers,
which an engine adapter does not need to call. The package is not marked `IsAotCompatible`, so the AOT claim must be
checked by publishing a consumer with NativeAOT, not assumed.

**Why not write our own reader.** A TMJ-only reader of tile and object layers is about 500 lines over `JsonDocument`
and has no dependency. Covering what Tiled users actually save (TMX by default, external tilesets, templates,
compressed and infinite layers, custom classes) is several thousand lines and follows every Tiled release. DotTiled
already carries that work under MIT. The risk is a single maintainer; the mitigation is that no DotTiled type
appears in the engine's public API, so the parser can be replaced or forked without breaking a game.

**Why Tiled first.** Tiled is the format most tutorials, asset packs and engines support (the Warped City packs that
Neon Rift uses ship Tiled maps), and its object layers and typed properties are the generic "placements plus data"
the issue asks for. LDtk's IntGrid and entity fields map onto the same engine types (§4), so a second importer can
reuse them later; doing both now doubles the surface before the first one is proven.

## 3. Large maps

- **Memory.** A tile layer costs 4 bytes per cell: a 1000 × 1000 layer is 4 MB, three layers 12 MB. Tiled's
  infinite maps can be flattened to their used bounds on load.
- **Drawing.** With culling, drawing costs what the screen shows (a 320 × 180 view at 16 px is about 260 cells per
  layer), independent of map size. The one defect is the zoom case in §1.
- **Collision.** `TileMotion` and `TilePhysicsWorld2D` read only the cells a box sweeps; map size does not enter.
- **Incremental loading.** Content loads synchronously and a map file of a few MB parses in milliseconds; streaming
  regions of one map has no measured need today. A game that wants rooms or regions loads one map file per area, as
  Tiled's world files (`.world`) and LDtk's per-level files already encourage.

No measurement was run for this section; it is arithmetic over the code paths named. The implementation adds a
benchmark of a large Tiled map (load time and draw time at zoom 1 and 0.5) so the claims become measurements.

## 4. Options

1. **Optional package `MicroEngine.Tiled` over DotTiled.** Managed only (no natives), released with the others like
   `MicroEngine.Physics.Box2D` (ADR 0051), so a game without maps carries no extra dependency. Public API in engine
   terms:
   - `TiledMap` loaded through Content (`.tmx`, `.tmj`): size, tile size, background colour, layers in order.
   - Tile layers as `TileLayer` (local ids) plus per-cell flip and rotation, and the `Sprite` of each id resolved
     from the tilesets' images through the asset cache (atlas regions, margins, spacing, image-collection tilesets).
   - Tile animations as frame lists the game can play with existing sprite animation.
   - Object layers as plain records: id, name, class, position, size, rotation, shape (rectangle, point, ellipse,
     polygon, polyline, tile), typed properties (bool, int, float, string, colour, file, object reference, class).
   - Properties exposed as data only: the engine never interprets `"enemy"` or `"door"`. A helper builds a
     `TileGrid` from a tile property the game names (for example `collision = solid | oneway`) into `TileFlags`.
   - A layer draw call over `TileRenderer` that honours flips.
2. **Our own TMJ reader inside the `MicroEngine` package.** No dependency, AOT certain, but JSON only, no templates,
   no compressed or infinite layers unless we write them; every Tiled feature becomes ours to maintain.
3. **Recipe only.** Document how to read a TMJ with `System.Text.Json` into a `TileLayer`. The external developer
   still writes the importer; the gap stays open.

Independent of the option, two engine fixes the mapping found: `TileRenderer` culls with `VisibleBounds` (zoom and
rotation), and it can draw a cell flipped or rotated.

## 5. Recommendation

Option 1. It closes the gap for the editor most developers already use, keeps game rules out of the engine (objects
and properties are data, collision is opt-in through a property the game names), keeps the parser replaceable, and
follows the optional-package path that ADR 0051 already set up. Option 2 costs more to reach the same coverage and
leaves TMX out. Option 3 does not close the gap. LDtk becomes a follow-up issue once the engine-side types prove
themselves with Tiled. No map editor is built (S4).

## 6. Implementation if the owner approves option 1

1. Engine fixes with tests: `TileRenderer` culls with `VisibleBounds`; flipped and rotated cells.
2. `src/MicroEngine.Tiled` (references Content, Graphics2D, Physics.Tiles and DotTiled only), its tests over real
   TMX and TMJ files covering every layer and object kind, flips, external tilesets and templates from a zip source.
3. Release path: ninth package in `sdk/version.mjs`, packed by `sdk.yml`, the external consumer check, third-party
   notice for DotTiled, ADR for the package.
4. Example consuming a real CC0 Tiled map through package references (the Warped City map, already the source of
   Neon Rift's art), with collision from tile properties and objects spawning entities; a NativeAOT publish of it
   and a browser build as the platform evidence.
5. Benchmark of a large map (§3) and the manual: a "Maps from Tiled" section in `docs/manual/world.md`, the limits
   lines updated, release notes under `## Next release`.

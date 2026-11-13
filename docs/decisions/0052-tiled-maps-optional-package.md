# 0052 — Tiled maps through an optional SDK package over DotTiled

Status: Accepted (2026-11-13). Amends [0019](0019-sdk-packages.md) (the published package set). Evidence:
[TILE-1](../evaluations/tile-1.md).

## Context

Tile levels reach the engine only as text written in C# (`TileLayer.Parse` with a character legend). A developer who
draws levels in a map editor, the usual workflow for platformers, top-down games and metroidvanias, must write a
parser for the editor's format, the mapping from tile ids and flip bits to sprites, the reading of objects and
custom properties, and the bridge to collision before the first level shows. Tiled is the most used 2D map editor and
its formats (TMX in XML, TMJ in JSON, external tilesets and templates) are open. DotTiled 1.0.0 (MIT, managed, no
dependencies) reads all of them.

## Decision

- **`MicroEngine.Tiled` is an optional SDK package**, released with the other packages, at the same version, by the
  same release workflow and approval (ADR 0022). No other package, the `dotnet new` template or a demo depends on it.
- **It reads Tiled maps through DotTiled** (pinned, listed in `THIRD-PARTY-NOTICES.md`) and the engine's asset
  sources: every file a map references (tilesets, templates, images) is read through the asset cache, so maps load
  from folders, archives and browser packages alike.
- **Its public API uses engine types only.** Tile layers become `TileLayer` plus per-cell flips, tilesets become
  `Sprite`s, objects become plain records with typed properties. No DotTiled type is public, so the parser can be
  replaced or forked without breaking a game.
- **Properties are data.** The package never gives a meaning to a class name or a property; collision comes from a
  tile property the game names, read into `TileFlags`.
- **No map editor** is built, and maps are loaded whole: streaming parts of one map waits for a measured need.

## Alternatives considered

- **An own TMJ reader inside `MicroEngine`**: no dependency, but JSON only, and templates, compressed and infinite
  layers and custom classes become ours to write and to follow on every Tiled release.
- **A documented recipe only**: every game would still write the importer.
- **LDtk in the same step**: its IntGrid layers and entity fields map onto the same engine types; it waits until the
  Tiled importer proves them.

## Consequences

- The release publishes one more package, and its check builds a game against it from the packed feed.
- A DotTiled defect is ours to work around or fix upstream; the pin makes any change deliberate.

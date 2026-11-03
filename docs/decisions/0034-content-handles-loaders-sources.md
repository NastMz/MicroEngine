# ADR 0034: Content handles, a loader registry and content sources beyond folders

## Status

Accepted (2026-11-03). Amends [ADR 0025](0025-asset-pipeline.md) (the asset cache becomes a façade over an asset store
with handles and loaders) and [ADR 0004](0004-external-asset-files.md) (content may come from an archive or a host's
listing, not only a folder). Builds on [ADR 0026](0026-kernel-modules-and-schedule.md) (generational handles in the
kernel) and [ADR 0037](0037-platform-host-contract.md) (`IContentSource` in the platform contracts). Evidence: ARCH-1
§3 (S13 assets, D9 shared holders), §6.3 (the `MicroEngine.Content` row), §6.5 (content: now, prepare, later), §9
(stage 2c), annex [A1](../evaluations/arch-1/A1-core.md) F7, annex A3 AU-4 (audio outside the asset lifecycle).

## Context

`AssetCache` was a sealed class with string kinds: images, atlases and fonts were hard-coded, a game could not add a
kind (level files, models, shader sources) or plug a different decoder in, and graphics loading lived in the same class
as the scope bookkeeping. It handed out live objects only, so code that kept an object after its scope released it
used a disposed texture with no way to tell. Content had to be a folder: archives, Android packaged assets read in
place and a browser's fetched files were refused (`NotSupportedException` since stage 1C), and sound files were read
with `File.ReadAllBytes` outside the content source and outside any scope. There was no place for a build-time
conversion step (compressed GPU textures, baked fonts), which mature engines reach for and which should not change the
loading code when it arrives.

## Decision

- **A `MicroEngine.Content` assembly** that references only the kernel and the platform contracts (the architecture
  test checks it) and ships inside the `MicroEngine` package. It holds:
  - `AssetStore`: one entry per `AssetKey` (loader kind, normalized path, options compared by value, and a small
    `Variant` that tells loads apart without boxing, such as a sampling filter) for the whole game.
  - `AssetScope`: the owner. Every scope that asks for an asset holds it, so sibling scopes share one object; disposing a
    scope or releasing a path releases that scope's holds, and the asset is disposed (when it is `IDisposable`) once the
    last scope holding it lets go. Child scopes are released with their parent. This is exactly the semantics of the
    ADR 0025 amendment (D9), now written once for every kind. A lookup of an asset the scope already holds allocates
    nothing.
  - `Asset<T>`: a typed handle (slot and generation over the kernel's `HandlePool`). It reads the object while some
    scope holds it; once the entry is destroyed the handle is stale and reading it fails saying so, even when a new
    asset reuses the slot.
  - `IAssetLoader<T>` and `AssetLoaderRegistry`: one loader per kind, chosen by kind (or by extension when only a path
    is given). A loader reads through its context; IO, format and argument failures become an `AssetLoadException`
    naming the asset. Registering a loader with an existing kind replaces it, which is how a module plugs its own in.
    `AssetLoadException` moves to this assembly with its namespace (`MicroEngine.Core`) unchanged; `MicroEngine.dll`
    forwards it.
  - `IAssetImporter`, **reserved**: the shape of a build-time conversion step driven by settings next to the source
    file. Nothing calls it yet; loaders read source files, so content works without any importer.
  - Content sources: `ZipContentSource` (a zip archive, decompressing a file when it is opened, readable from several
    threads), `ListedContentSource` (files a host lists and opens itself: an app's packaged assets, a page's fetched
    files) and `ContentSources.Open(path)` (a folder or a `.zip`). Every source follows the folder rules: exact letter
    case on every system with the real spelling named, `/` separators, no path out of the source.
- **`Game.Assets` stays the game-facing façade.** `AssetCache` keeps its API and behaviour; each cache scope wraps an
  `AssetScope` (`Assets.Scope`, `Assets.Store`, `Assets.Loaders`, `Assets.Source`). Its image, atlas, folder-atlas and
  font loaders are registered from the core under the kinds `texture`, `atlas`, `folder atlas` and `font` until the 2D
  graphics module takes them over with the same kinds and keys; preloading still decodes on workers and adds the result
  to the store. The composition's content (`host.Content`) may be any source; `Assets.Content` (a `ContentFolder`)
  works only for folders.
- **Audio is an asset kind.** `sound` decodes a PCM WAV into an `AudioClip` and `music` reads a music file as
  `EncodedAudio` (bytes and format), both through the content source and the scopes.

## Alternatives considered

- **A mandatory content pipeline** (every file imported at build time into engine formats, as MonoGame's content
  pipeline does): rejected for now. It makes the first game harder and ties every change of a file to a build step;
  the importer seam keeps the option open without making it a requirement.
- **A central database of asset identifiers** (GUIDs assigned to every file, as Godot considered and rejected for its
  own UIDs at first): rejected. Paths are what games and their authors already use, and a database must be kept in step
  with the files; handles identify loaded assets, not files.
- **Reference counting exposed to games**: rejected again (ADR 0025): lifetimes follow scopes.
- **Keeping `AssetCache` as the store and adding kinds to it**: rejected; the graphics loaders must be able to leave
  the core for the 2D graphics module without moving the scope bookkeeping with them.

## Consequences

- Games keep compiling and behaving the same; new code can hold `Asset<T>` handles and register loaders.
- Content shipped as one archive works on every host; a host that cannot hand .NET a folder can list its files.
- `Assets.Statistics.TextureBytes` counts what each loader reports, so sounds loaded through `Assets` add their samples.
- Android still unpacks its packaged assets to a folder and browser pages still copy the package's files into memory
  before starting: both can move to `ListedContentSource` in their hosts.
- Not done: hot reload, asynchronous or streaming sources, asset identifiers that survive renames, mod overlays
  (several sources layered).

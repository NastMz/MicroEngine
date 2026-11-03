# Namespace MicroEngine.Content

<div data-pagefind-weight="0.3">

Content: the asset store under Game.Assets (typed handles, scopes that share and release, the loader registry, the reserved import step) and content sources beyond folders (zip archives, files a host lists).

Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

### Classes

 [AssetLoadContext](MicroEngine.Content.AssetLoadContext.md)

What a loader is asked to load, and the content it reads it from.

 [AssetLoader<T\>](MicroEngine.Content.AssetLoader\-1.md)

A loader written as a function (see [IAssetLoader`1](MicroEngine.Content.md)).

 [AssetLoaderRegistry](MicroEngine.Content.AssetLoaderRegistry.md)

The loaders of a store, one per kind. The engine registers its own (images, atlases, fonts, sounds, music); a game
or module adds kinds of its own, or replaces one by registering another loader with the same kind before the first
load of that kind.

 [AssetPath](MicroEngine.Content.AssetPath.md)

Content paths: relative to the source, '/'-separated.

 [AssetScope](MicroEngine.Content.AssetScope.md)

An owner of assets: what a game, a level or a scene holds. Every scope that asks for an asset holds it, whichever
scope loaded it first, so sibling scopes share one object; disposing a scope (or [Release](MicroEngine.Content.AssetScope.md#MicroEngine_Content_AssetScope_Release_System_String_))
releases its holds, and an asset is disposed when no scope holds it any more. Child scopes are released with their
parent.

 [AssetStore](MicroEngine.Content.AssetStore.md)

One entry per asset key for the whole game, shared by the scopes that hold it: a scope that asks for an asset another
scope already loaded gets the same object and holds it too; the object is disposed (when it is
`IDisposable`) once the last scope holding it releases it, and its handles become stale. Games never count
references: lifetimes follow scopes.

 [ContentSources](MicroEngine.Content.ContentSources.md)

Reading from any content source, and opening one from a path.

 [ListedContentSource](MicroEngine.Content.ListedContentSource.md)

Content files a host lists and opens itself, where the files are not a folder .NET can read: an Android app's
packaged assets (opened through the platform's asset manager), files a browser page fetched. The listing is fixed
when the source is made; names follow the folder rules (exact letter case everywhere, '/' separators).

 [ZipContentSource](MicroEngine.Content.ZipContentSource.md)

Content files inside a zip archive: a game's content shipped as one file. Names follow the folder rules (exact
letter case everywhere, '/' separators, no way out of the archive). Each file is decompressed into memory when it is
opened, so streams are independent and the source can be read from several threads.

### Structs

 [Asset<T\>](MicroEngine.Content.Asset\-1.md)

A typed reference to an asset in an [AssetStore](MicroEngine.Content.AssetStore.md): a slot and the generation of the asset that held it.
Once the asset is released by every scope that held it (and disposed), the handle is stale: reading it fails instead
of reaching whatever reuses the slot. <code>default</code> is [None](MicroEngine.Content.md).

 [AssetKey](MicroEngine.Content.AssetKey.md)

What identifies a stored asset: the loader's kind, the content path and the settings it was loaded with. Two
requests with equal keys share one object. A struct of references and values, so a lookup allocates nothing.

 [AssetRecord](MicroEngine.Content.AssetRecord.md)

One asset a scope holds, as [Loaded](MicroEngine.Content.AssetScope.md#MicroEngine_Content_AssetScope_Loaded) lists it.

### Interfaces

 [IAssetImporter](MicroEngine.Content.IAssetImporter.md)

Reserved: a build-time step that turns a source file into the file a loader reads (a texture compressed for the GPU,
a model converted from glTF, a font baked into a distance field), driven by settings kept next to the source file.
The engine does not call importers yet; the interface fixes where that step will plug in so loaders never need to
change when it arrives. Content stays usable without any importer: loaders read source files directly.

 [IAssetLoader<T\>](MicroEngine.Content.IAssetLoader\-1.md)

A loader of assets of type <code class="typeparamref">T</code> (see [IAssetLoader](MicroEngine.Content.IAssetLoader.md)).

 [IAssetLoader](MicroEngine.Content.IAssetLoader.md)

Makes one kind of asset from content files: decodes an image into a texture, a WAV into an audio clip... A loader is
registered once per store under its [Kind](MicroEngine.Content.IAssetLoader.md#MicroEngine_Content_IAssetLoader_Kind) ([AssetLoaderRegistry](MicroEngine.Content.AssetLoaderRegistry.md)); the store calls it the
first time a key of that kind is asked for and shares the result.

 [IAssetStore](MicroEngine.Content.IAssetStore.md)

A game's assets: where content comes from, the loaders that read it, and the scopes that hold what was loaded.

</div>

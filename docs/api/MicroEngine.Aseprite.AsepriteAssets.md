# Class AsepriteAssets

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Aseprite](MicroEngine.Aseprite.md)  
Assembly: MicroEngine.Aseprite.dll  
Guide: [Aseprite files](../manual/assets.md#aseprite-files)  

Aseprite files (<code>.aseprite</code>, <code>.ase</code>) as sprite sheets, read as the artist saved them: no export step. Each
frame is the picture Aseprite shows (visible layers flattened with their blend modes and opacity), named
"<code>file</code> 0", "<code>file</code> 1"... like Aseprite's own sheet export; each frame tag is a clip with every frame's
duration, its direction and whether it repeats; each slice is a sprite of its own, named after it, and data of the
frames it covers: a box of its name ([Box](MicroEngine.Graphics.Sprite.md#MicroEngine_Graphics_Sprite_Box_System_String_System_Boolean_)) and, when it has a pivot, a point of its name
([Point](MicroEngine.Graphics.Sprite.md#MicroEngine_Graphics_Sprite_Point_System_String_System_Boolean_)), each following the slice's keys from frame to frame.

```csharp
public static class AsepriteAssets
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AsepriteAssets](MicroEngine.Aseprite.AsepriteAssets.md)

## Remarks

<p>
Frames are anchored at the pivot of the file's first slice that has one (the slice's corner plus its pivot, at
each frame's key), so the artist sets a character's feet once in Aseprite; a file without such a slice uses the
caller's [Pivot](MicroEngine.Graphics.AtlasOptions.md#MicroEngine_Graphics_AtlasOptions_Pivot). A slice's own sprite is anchored at its pivot the same way.
</p>
<p>
Register the format once ([AddAseprite](MicroEngine.Aseprite.AsepriteAssets.md#MicroEngine_Aseprite_AsepriteAssets_AddAseprite_MicroEngine_Content_AssetLoaderRegistry_), or just call [Aseprite](MicroEngine.Aseprite.AsepriteAssets.md#MicroEngine_Aseprite_AsepriteAssets_Aseprite_MicroEngine_Core_AssetCache_System_String_MicroEngine_Graphics_AtlasOptions_System_Nullable_MicroEngine_Graphics_TextureFilter__)) and the file is an atlas
like any other: <code>Assets.Atlas("hero.aseprite")</code>, <code>Assets.Sheet</code>, <code>Assets.Preload</code> and the scopes work
on it, from folders, archives and browser packages alike.
</p>

## Fields

### <a id="MicroEngine_Aseprite_AsepriteAssets_Kind"></a> Kind

The asset kind of the frames and clips of an Aseprite file (a [SpriteSheet](MicroEngine.Graphics.SpriteSheet.md)).

```csharp
public const string Kind = "aseprite"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Aseprite_AsepriteAssets_Loader"></a> Loader

The loader of the <code>aseprite</code> kind (extensions <code>.aseprite</code> and <code>.ase</code>).

```csharp
public static IAssetLoader<SpriteSheet> Loader { get; }
```

#### Property Value

 [IAssetLoader](MicroEngine.Content.IAssetLoader.md)<[SpriteSheet](MicroEngine.Graphics.SpriteSheet.md)\>

## Methods

### <a id="MicroEngine_Aseprite_AsepriteAssets_AddAseprite_MicroEngine_Content_AssetLoaderRegistry_"></a> AddAseprite\(AssetLoaderRegistry\)

Registers the <code>aseprite</code> kind on <code class="paramref">loaders</code>, so <code>Assets.Atlas</code> and <code>Assets.Sheet</code> read Aseprite files.

```csharp
public static AssetLoaderRegistry AddAseprite(this AssetLoaderRegistry loaders)
```

#### Parameters

`loaders` [AssetLoaderRegistry](MicroEngine.Content.AssetLoaderRegistry.md)

#### Returns

 [AssetLoaderRegistry](MicroEngine.Content.AssetLoaderRegistry.md)

<code class="paramref">loaders</code>, for chaining.

### <a id="MicroEngine_Aseprite_AsepriteAssets_Aseprite_MicroEngine_Core_AssetCache_System_String_MicroEngine_Graphics_AtlasOptions_System_Nullable_MicroEngine_Graphics_TextureFilter__"></a> Aseprite\(AssetCache, string, AtlasOptions?, TextureFilter?\)

The Aseprite file at <code class="paramref">path</code> as an atlas with its clips, loaded once and shared like any asset
(the format is registered on first use; afterwards <code>Assets.Atlas(path)</code> gives the same object).

```csharp
public static SpriteAtlas Aseprite(this AssetCache assets, string path, AtlasOptions? options = null, TextureFilter? filter = null)
```

#### Parameters

`assets` [AssetCache](MicroEngine.Core.AssetCache.md)

The game's assets (<code>Game.Assets</code>) or one of its scopes.

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

The file's content path.

`options` [AtlasOptions](MicroEngine.Graphics.AtlasOptions.md)?

The pivot of frames when the file has no slice with one; null for [Default](MicroEngine.Graphics.AtlasOptions.md#MicroEngine_Graphics_AtlasOptions_Default).

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)?

The texture's filter; null for the game's default.

#### Returns

 [SpriteAtlas](MicroEngine.Graphics.SpriteAtlas.md)

#### Exceptions

 [AssetLoadException](MicroEngine.Core.AssetLoadException.md)

The file is missing, wrongly cased or not a file Aseprite saves.

### <a id="MicroEngine_Aseprite_AsepriteAssets_Read_System_String_System_Byte___MicroEngine_Graphics_AtlasOptions_"></a> Read\(string, byte\[\], AtlasOptions?\)

Reads the bytes of an Aseprite file, without a graphics device or a content source: for tools and tests.

```csharp
public static SpriteSheet Read(string name, byte[] bytes, AtlasOptions? options = null)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The name frames take ("hero" gives "hero 0", "hero 1"...): the file's name without its extension.

`bytes` [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

The file.

`options` [AtlasOptions](MicroEngine.Graphics.AtlasOptions.md)?

The pivot of frames when the file has no slice with one; null for [Default](MicroEngine.Graphics.AtlasOptions.md#MicroEngine_Graphics_AtlasOptions_Default).

#### Returns

 [SpriteSheet](MicroEngine.Graphics.SpriteSheet.md)

#### Exceptions

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The bytes are not a file Aseprite saves; the message says what is wrong.

</div>

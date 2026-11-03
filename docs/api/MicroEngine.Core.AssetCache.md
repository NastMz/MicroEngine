# Class AssetCache

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Loads, shares and releases a game's content files (<code>Game.Assets</code>): images as textures, packing tools' atlases,
folders of images packed into one atlas, and TTF/OTF fonts. The same request returns the same object, so a texture
used by ten scenes is decoded and uploaded once, whichever scopes ask for it; an asset is disposed when the last scope
holding it releases it.

```csharp
public sealed class AssetCache : IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AssetCache](MicroEngine.Core.AssetCache.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Examples

```csharp
var hero = Assets.Atlas("atlases/hero.json");            // TexturePacker or Aseprite JSON, Starling XML
var sky = Assets.Texture("backgrounds/sky.jpg");
var ui = Assets.Font("fonts/Inter-Regular.ttf");

using var level = Assets.CreateScope();                   // released when the level ends
var tiles = level.Atlas("levels/forest/tiles.xml");
```

## Remarks

<p>
Paths are relative to the content source, with '/' separators, and must match the file names' letter case exactly
on every system, because Linux, Android and browsers would not find "Hero.png" for "hero.png" even where Windows does.
The content is a folder by default; the composition may name another source, such as a zip archive
([Content](MicroEngine.Hosting.HostBuilder.md#MicroEngine_Hosting_HostBuilder_Content), [ZipContentSource](MicroEngine.Content.ZipContentSource.md)).
</p>
<p>
<code>Game.Assets</code> lives as long as the game and is disposed after <code>Unload</code>. For assets of one level or scene,
create a child with [CreateScope](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_CreateScope_System_String_) and dispose it when the level ends. Scopes share: a scope that asks for
an asset another scope (a parent, a sibling) already loaded gets the same object instead of loading it again, and holds
it too; disposing a scope or calling [Unload](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Unload_System_String_) releases its hold, and an asset is disposed when no scope
holds it any more. Loading belongs in <code>Load</code>, a scene's
<code>Enter</code> or a loading screen; a load during play is reported, because decoding a large image takes longer than a
frame. [Preload](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Preload_System_String___) decodes on worker threads where the platform has them and uploads within a time budget
per frame, for loading screens.
</p>
<p>
The cache is built on the engine's asset store ([Store](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Store)): every scope is an [AssetScope](MicroEngine.Content.AssetScope.md)
([Scope](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Scope)), so typed handles ([Asset`1](MicroEngine.Content.md)) and loaders of other kinds registered in
[Loaders](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Loaders) share the same lifetimes.
</p>

## Constructors

### <a id="MicroEngine_Core_AssetCache__ctor_MicroEngine_Graphics_IGraphicsDevice_System_String_"></a> AssetCache\(IGraphicsDevice, string?\)

Creates the root scope of a game's assets.

```csharp
public AssetCache(IGraphicsDevice device, string? root = null)
```

#### Parameters

`device` IGraphicsDevice

Creates the textures.

`root` [string](https://learn.microsoft.com/dotnet/api/system.string)?

The content folder; null for the composition's ([Content](MicroEngine.Hosting.HostBuilder.md#MicroEngine_Hosting_HostBuilder_Content)) or the <code>Content</code> folder next to the executable. It is opened on the first load, so games without content files never need one.

## Properties

### <a id="MicroEngine_Core_AssetCache_BackgroundDecoding"></a> BackgroundDecoding

Whether [Preload](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Preload_System_String___) decodes on worker threads (true by default, except in browsers, where .NET runs on
one thread and decoding happens within [FrameBudgetMilliseconds](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_FrameBudgetMilliseconds) instead).

```csharp
public bool BackgroundDecoding { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AssetCache_Content"></a> Content

The content folder files are read from (opened on first use).

```csharp
public ContentFolder Content { get; }
```

#### Property Value

 [ContentFolder](MicroEngine.Core.ContentFolder.md)

#### Exceptions

 AssetLoadException

The folder does not exist.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The content is not a folder (a zip archive, for example); use [Source](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Source).

### <a id="MicroEngine_Core_AssetCache_Count"></a> Count

How many assets this scope and its children hold: those they loaded and those they asked for that another scope
(a parent included) had loaded, an asset several of them share counting once. On the root scope, every asset of
the game. Allocates nothing, so it can be checked every frame.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_AssetCache_FrameBudgetMilliseconds"></a> FrameBudgetMilliseconds

Time per frame [Update](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Update) spends finishing preloaded assets (decoding where needed, uploading), in milliseconds; at least one asset finishes per frame.

```csharp
public double FrameBudgetMilliseconds { get; set; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_AssetCache_Loaded"></a> Loaded

The assets of this scope and its children, each once, in the order each scope asked for them.

```csharp
public IReadOnlyList<AssetInfo> Loaded { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[AssetInfo](MicroEngine.Core.AssetInfo.md)\>

### <a id="MicroEngine_Core_AssetCache_Loaders"></a> Loaders

The loaders of the store, one per kind: "texture", "atlas", "folder atlas", "font", "sound", "music" and any a game registers.

```csharp
public AssetLoaderRegistry Loaders { get; }
```

#### Property Value

 AssetLoaderRegistry

### <a id="MicroEngine_Core_AssetCache_Name"></a> Name

The scope's name for diagnostics (null for the root and unnamed scopes).

```csharp
public string? Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Core_AssetCache_Playing"></a> Playing

Whether the game is past its first update; loads from then on count as loads during play ([DuringPlay](MicroEngine.Core.AssetInfo.md#MicroEngine_Core_AssetInfo_DuringPlay)).
The engine sets it.

```csharp
public bool Playing { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AssetCache_Scope"></a> Scope

The asset scope behind this cache scope: typed handles ([Load``1](MicroEngine.Content.AssetScope.md#MicroEngine_Content_AssetScope_Load__1_System_String_System_String_System_Object_System_Int64_)) with the same holds and lifetimes.

```csharp
public AssetScope Scope { get; }
```

#### Property Value

 AssetScope

### <a id="MicroEngine_Core_AssetCache_SlowLoadMilliseconds"></a> SlowLoadMilliseconds

A load during play that takes at least this many milliseconds is reported once on the error output, naming the
file (4 by default: a quarter of a 60 Hz frame). Zero or less turns the reports off.

```csharp
public double SlowLoadMilliseconds { get; set; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_AssetCache_Source"></a> Source

Where content files are read from (opened on first use): a folder, a zip archive or another source of the composition.

```csharp
public IContentSource Source { get; }
```

#### Property Value

 IContentSource

#### Exceptions

 AssetLoadException

The content folder does not exist.

### <a id="MicroEngine_Core_AssetCache_Statistics"></a> Statistics

Counts, memory and load time of this scope and its children.

```csharp
public AssetStatistics Statistics { get; }
```

#### Property Value

 [AssetStatistics](MicroEngine.Core.AssetStatistics.md)

### <a id="MicroEngine_Core_AssetCache_Store"></a> Store

The game's asset store: its content source, loaders and root scope.

```csharp
public AssetStore Store { get; }
```

#### Property Value

 AssetStore

## Methods

### <a id="MicroEngine_Core_AssetCache_Atlas_System_String_MicroEngine_Graphics_AtlasOptions_System_Nullable_MicroEngine_Graphics_TextureFilter__"></a> Atlas\(string, AtlasOptions?, TextureFilter?\)

The atlas a packing tool described at <code class="paramref">path</code> (TexturePacker or Aseprite JSON, Starling XML) with the
image it names next to it.

```csharp
public SpriteAtlas Atlas(string path, AtlasOptions? options = null, TextureFilter? filter = null)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

The atlas file.

`options` AtlasOptions?

Default pivot and naming; null for [Default](MicroEngine.Graphics.AtlasOptions.md#MicroEngine_Graphics_AtlasOptions_Default).

`filter` TextureFilter?

The texture's filter; null for the game's default.

#### Returns

 SpriteAtlas

#### Exceptions

 AssetLoadException

The file or its image is missing or invalid.

### <a id="MicroEngine_Core_AssetCache_AtlasFromFolder_System_String_System_Numerics_Vector2_System_Nullable_MicroEngine_Graphics_TextureFilter__"></a> AtlasFromFolder\(string, Vector2, TextureFilter?\)

Every image directly inside <code class="paramref">folder</code> packed into one atlas at load, named by file name without its
extension ("items/gem.png" is "gem"): a folder of loose sprites, no packing tool needed.

```csharp
public SpriteAtlas AtlasFromFolder(string folder, Vector2 pivot = default, TextureFilter? filter = null)
```

#### Parameters

`folder` [string](https://learn.microsoft.com/dotnet/api/system.string)

A folder of the content.

`pivot` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The anchor of every sprite as a fraction of its size; (0, 0), the top-left, by default.

`filter` TextureFilter?

The texture's filter; null for the game's default.

#### Returns

 SpriteAtlas

#### Exceptions

 AssetLoadException

The folder is missing or holds no image, or an image cannot be read.

### <a id="MicroEngine_Core_AssetCache_Bytes_System_String_"></a> Bytes\(string\)

The bytes of any content file (not cached).

```csharp
public byte[] Bytes(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

#### Exceptions

 AssetLoadException

The file is missing or wrongly cased.

### <a id="MicroEngine_Core_AssetCache_CreateScope_System_String_"></a> CreateScope\(string?\)

A child scope: an asset it asks for that another scope already loaded is the same object, and disposing the scope
releases what it holds, disposing only what no other scope holds. Use one per level or scene whose assets should be
freed when it ends.

```csharp
public AssetCache CreateScope(string? name = null)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [AssetCache](MicroEngine.Core.AssetCache.md)

### <a id="MicroEngine_Core_AssetCache_Dispose"></a> Dispose\(\)

Releases every asset this scope and its child scopes hold, disposing those no other scope holds. The root scope is
disposed by the engine after <code>Game.Unload</code>.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Core_AssetCache_Font_System_String_MicroEngine_Graphics_FontOptions_"></a> Font\(string, FontOptions?\)

The TTF or OTF font at <code class="paramref">path</code>, drawn at any size ([ScalableFont](MicroEngine.Graphics.ScalableFont.md)).

```csharp
public ScalableFont Font(string path, FontOptions? options = null)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`options` [FontOptions](MicroEngine.Graphics.FontOptions.md)?

#### Returns

 [ScalableFont](MicroEngine.Graphics.ScalableFont.md)

#### Exceptions

 AssetLoadException

The file is missing, wrongly cased or not a font the engine reads.

### <a id="MicroEngine_Core_AssetCache_Image_System_String_"></a> Image\(string\)

The image at <code class="paramref">path</code> decoded into a new [PixelImage](MicroEngine.Graphics.PixelImage.md) each call (not cached: the game may
change it, for example to recolour it before packing).

```csharp
public PixelImage Image(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 PixelImage

#### Exceptions

 AssetLoadException

The file is missing, wrongly cased or not an image the engine reads.

### <a id="MicroEngine_Core_AssetCache_IsLoaded_System_String_"></a> IsLoaded\(string\)

Whether this scope or one of its parents has <code class="paramref">path</code> loaded (in any form).

```csharp
public bool IsLoaded(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AssetCache_Music_System_String_"></a> Music\(string\)

The music file at <code class="paramref">path</code> (OGG, WAV; MP3 on raylib), read whole and decoded while it plays; play
it with <code>Audio.AddMusic(name, music)</code>.

```csharp
public EncodedAudio Music(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 EncodedAudio

#### Exceptions

 AssetLoadException

The file is missing or wrongly cased.

### <a id="MicroEngine_Core_AssetCache_Preload_System_String___"></a> Preload\(params string\[\]\)

Starts loading <code class="paramref">paths</code> without blocking: images become textures, ".json" and ".xml" files atlases,
".ttf" and ".otf" files fonts. Files are read and decoded on worker threads (see [BackgroundDecoding](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_BackgroundDecoding));
the engine uploads finished ones each frame within [FrameBudgetMilliseconds](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_FrameBudgetMilliseconds). Afterwards the usual
calls ([Texture](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Texture_System_String_System_Nullable_MicroEngine_Graphics_TextureFilter__), [Atlas](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Atlas_System_String_MicroEngine_Graphics_AtlasOptions_System_Nullable_MicroEngine_Graphics_TextureFilter__), [Font](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Font_System_String_MicroEngine_Graphics_FontOptions_)) return them at once.

```csharp
public AssetBatch Preload(params string[] paths)
```

#### Parameters

`paths` [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]

#### Returns

 [AssetBatch](MicroEngine.Core.AssetBatch.md)

The batch, to show progress on a loading screen or wait for.

### <a id="MicroEngine_Core_AssetCache_Sound_System_String_"></a> Sound\(string\)

The PCM WAV file at <code class="paramref">path</code> decoded into a clip, shared and released like any asset; play it with <code>Audio.AddSound(name, clip)</code>.

```csharp
public AudioClip Sound(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 AudioClip

#### Exceptions

 AssetLoadException

The file is missing, wrongly cased or not an 8, 16 or 24-bit PCM WAV.

### <a id="MicroEngine_Core_AssetCache_Text_System_String_"></a> Text\(string\)

The text of any content file, as UTF-8 (not cached).

```csharp
public string Text(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 AssetLoadException

The file is missing or wrongly cased.

### <a id="MicroEngine_Core_AssetCache_Texture_System_String_System_Nullable_MicroEngine_Graphics_TextureFilter__"></a> Texture\(string, TextureFilter?\)

The image at <code class="paramref">path</code> as a texture, sampled with <code class="paramref">filter</code> (the game's default when null).

```csharp
public Texture Texture(string path, TextureFilter? filter = null)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`filter` TextureFilter?

#### Returns

 Texture

#### Exceptions

 AssetLoadException

The file is missing, wrongly cased or not an image the engine reads.

### <a id="MicroEngine_Core_AssetCache_Unload_System_String_"></a> Unload\(string\)

Releases what this scope holds from <code class="paramref">path</code> (in any form): each object is disposed unless another
scope still holds it, and loads again if asked for later. What only other scopes hold is not touched.

```csharp
public bool Unload(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether this scope held anything from the file.

### <a id="MicroEngine_Core_AssetCache_Update"></a> Update\(\)

Finishes preloaded assets within [FrameBudgetMilliseconds](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_FrameBudgetMilliseconds) (at least one per call). The engine calls it
once per frame for <code>Game.Assets</code>; it also runs child scopes.

```csharp
public void Update()
```

</div>

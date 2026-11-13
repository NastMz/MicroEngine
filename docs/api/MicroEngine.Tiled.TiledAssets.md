# Class TiledAssets

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Tiled](MicroEngine.Tiled.md)  
Assembly: MicroEngine.Tiled.dll  
Guide: [Maps from Tiled](../manual/world.md#maps-from-tiled)  

The <code>tiled map</code> asset kind: a Tiled map (<code>.tmx</code> or <code>.tmj</code>) with the tilesets (<code>.tsx</code>,
<code>.tsj</code>) and templates (<code>.tx</code>, <code>.tj</code>) it references, read through the store's content source so it
loads from folders, archives and browser packages alike. The asset is data ([TiledMap](MicroEngine.Tiled.TiledMap.md)); its
tile images become textures with [TiledSprites](MicroEngine.Tiled.TiledSprites.md).

```csharp
public static class TiledAssets
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TiledAssets](MicroEngine.Tiled.TiledAssets.md)

## Fields

### <a id="MicroEngine_Tiled_TiledAssets_Kind"></a> Kind

The asset kind.

```csharp
public const string Kind = "tiled map"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Tiled_TiledAssets_Loader"></a> Loader

The loader of the <code>tiled map</code> kind (extensions <code>.tmx</code> and <code>.tmj</code>).

```csharp
public static IAssetLoader<TiledMap> Loader { get; }
```

#### Property Value

 [IAssetLoader](MicroEngine.Content.IAssetLoader.md)<[TiledMap](MicroEngine.Tiled.TiledMap.md)\>

## Methods

### <a id="MicroEngine_Tiled_TiledAssets_AddTiledMaps_MicroEngine_Content_AssetLoaderRegistry_"></a> AddTiledMaps\(AssetLoaderRegistry\)

Registers the <code>tiled map</code> kind on <code class="paramref">loaders</code>.

```csharp
public static AssetLoaderRegistry AddTiledMaps(this AssetLoaderRegistry loaders)
```

#### Parameters

`loaders` [AssetLoaderRegistry](MicroEngine.Content.AssetLoaderRegistry.md)

#### Returns

 [AssetLoaderRegistry](MicroEngine.Content.AssetLoaderRegistry.md)

<code class="paramref">loaders</code>, for chaining.

### <a id="MicroEngine_Tiled_TiledAssets_TiledMap_MicroEngine_Core_AssetCache_System_String_"></a> TiledMap\(AssetCache, string\)

The map at <code class="paramref">path</code>, loaded once per scope like any asset (the kind is registered on first use).

```csharp
public static TiledMap TiledMap(this AssetCache assets, string path)
```

#### Parameters

`assets` [AssetCache](MicroEngine.Core.AssetCache.md)

The game's assets (<code>Game.Assets</code>) or one of its scopes.

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

The map's content path.

#### Returns

 [TiledMap](MicroEngine.Tiled.TiledMap.md)

#### Exceptions

 [AssetLoadException](MicroEngine.Core.AssetLoadException.md)

The map, or a file it references, is missing or not a map Tiled saves; or the map is not orthogonal.

</div>

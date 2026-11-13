# Class TiledTileset

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Tiled](MicroEngine.Tiled.md)  
Assembly: MicroEngine.Tiled.dll  
Guide: [Maps from Tiled](../manual/world.md#maps-from-tiled)  

A tileset of a [TiledMap](MicroEngine.Tiled.TiledMap.md): one image cut into tiles, or a collection of images.

```csharp
public sealed class TiledTileset
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TiledTileset](MicroEngine.Tiled.TiledTileset.md)

## Properties

### <a id="MicroEngine_Tiled_TiledTileset_Class"></a> Class

The tileset's class; empty when unset.

```csharp
public string Class { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Tiled_TiledTileset_Columns"></a> Columns

Tiles per row of its image (0 for a collection of images).

```csharp
public int Columns { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledTileset_FirstGid"></a> FirstGid

The global id of its first tile.

```csharp
public int FirstGid { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledTileset_Image"></a> Image

The content path of its image, or null for a collection of images (each tile has its own).

```csharp
public string? Image { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Tiled_TiledTileset_Name"></a> Name

The tileset's name.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Tiled_TiledTileset_Properties"></a> Properties

The tileset's custom properties.

```csharp
public TiledProperties Properties { get; }
```

#### Property Value

 [TiledProperties](MicroEngine.Tiled.TiledProperties.md)

### <a id="MicroEngine_Tiled_TiledTileset_TileHeight"></a> TileHeight

Height of its tiles in pixels (the largest, for a collection of images).

```csharp
public int TileHeight { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledTileset_TileOffset"></a> TileOffset

Pixels every tile is drawn shifted by.

```csharp
public Vector2 TileOffset { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Tiled_TiledTileset_TileWidth"></a> TileWidth

Width of its tiles in pixels (the largest, for a collection of images).

```csharp
public int TileWidth { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledTileset_Tiles"></a> Tiles

Its tiles by increasing id: every cell of its image, or the images of a collection (whose ids may have gaps).

```csharp
public IReadOnlyList<TiledTile> Tiles { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[TiledTile](MicroEngine.Tiled.TiledTile.md)\>

## Methods

### <a id="MicroEngine_Tiled_TiledTileset_Contains_System_Int32_"></a> Contains\(int\)

Whether <code class="paramref">gid</code> is one of this tileset's tiles.

```csharp
public bool Contains(int gid)
```

#### Parameters

`gid` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

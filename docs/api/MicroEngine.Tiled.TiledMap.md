# Class TiledMap

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Tiled](MicroEngine.Tiled.md)  
Assembly: MicroEngine.Tiled.dll  
Guide: [Maps from Tiled](../manual/world.md#maps-from-tiled)  

A map made in the Tiled editor, read as data: its size, layers, tilesets and objects. Load it with
[TiledMap](MicroEngine.Tiled.TiledAssets.md#MicroEngine_Tiled_TiledAssets_TiledMap_MicroEngine_Core_AssetCache_System_String_); give its tiles textures with [TiledSprites](MicroEngine.Tiled.TiledSprites.md).

```csharp
public sealed class TiledMap
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TiledMap](MicroEngine.Tiled.TiledMap.md)

## Remarks

Positions are in map pixels: tile (0, 0) has its top-left at (0, 0). An infinite map is cropped to the cells its
layers use, and [Origin](MicroEngine.Tiled.TiledMap.md#MicroEngine_Tiled_TiledMap_Origin) says where that first cell was in Tiled. Only orthogonal maps load.

## Properties

### <a id="MicroEngine_Tiled_TiledMap_AllLayers"></a> AllLayers

Every layer, groups included, in drawing order (bottom to top, a group before its layers).

```csharp
public IEnumerable<TiledLayer> AllLayers { get; }
```

#### Property Value

 [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[TiledLayer](MicroEngine.Tiled.TiledLayer.md)\>

### <a id="MicroEngine_Tiled_TiledMap_Background"></a> Background

The background colour set in Tiled, if any.

```csharp
public Color? Background { get; }
```

#### Property Value

 [Color](MicroEngine.Color.md)?

### <a id="MicroEngine_Tiled_TiledMap_Class"></a> Class

The map's class (Tiled's "Class" field); empty when unset.

```csharp
public string Class { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Tiled_TiledMap_Height"></a> Height

Height in tiles.

```csharp
public int Height { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledMap_Layers"></a> Layers

The top-level layers, bottom to top; groups hold their own ([TiledGroupLayer](MicroEngine.Tiled.TiledGroupLayer.md)).

```csharp
public IReadOnlyList<TiledLayer> Layers { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[TiledLayer](MicroEngine.Tiled.TiledLayer.md)\>

### <a id="MicroEngine_Tiled_TiledMap_ObjectLayers"></a> ObjectLayers

Every object layer in drawing order.

```csharp
public IEnumerable<TiledObjectLayer> ObjectLayers { get; }
```

#### Property Value

 [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[TiledObjectLayer](MicroEngine.Tiled.TiledObjectLayer.md)\>

### <a id="MicroEngine_Tiled_TiledMap_Objects"></a> Objects

Every object of every object layer, layer by layer.

```csharp
public IEnumerable<TiledObject> Objects { get; }
```

#### Property Value

 [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[TiledObject](MicroEngine.Tiled.TiledObject.md)\>

### <a id="MicroEngine_Tiled_TiledMap_Origin"></a> Origin

Where tile (0, 0) was in Tiled, in tiles: (0, 0) except for an infinite map, which is cropped to the cells it uses.

```csharp
public (int X, int Y) Origin { get; }
```

#### Property Value

 \([int](https://learn.microsoft.com/dotnet/api/system.int32) [X](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.x), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Y](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.y)\)

### <a id="MicroEngine_Tiled_TiledMap_ParallaxOrigin"></a> ParallaxOrigin

The point where parallax shifts nothing, in map pixels (Tiled's parallax origin).

```csharp
public Vector2 ParallaxOrigin { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Tiled_TiledMap_Path"></a> Path

The map's content path.

```csharp
public string Path { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Tiled_TiledMap_PixelHeight"></a> PixelHeight

Height of the map in pixels.

```csharp
public int PixelHeight { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledMap_PixelWidth"></a> PixelWidth

Width of the map in pixels.

```csharp
public int PixelWidth { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledMap_Properties"></a> Properties

The map's custom properties.

```csharp
public TiledProperties Properties { get; }
```

#### Property Value

 [TiledProperties](MicroEngine.Tiled.TiledProperties.md)

### <a id="MicroEngine_Tiled_TiledMap_TileHeight"></a> TileHeight

Height of a grid cell in pixels.

```csharp
public int TileHeight { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledMap_TileLayers"></a> TileLayers

Every tile layer in drawing order.

```csharp
public IEnumerable<TiledTileLayer> TileLayers { get; }
```

#### Property Value

 [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[TiledTileLayer](MicroEngine.Tiled.TiledTileLayer.md)\>

### <a id="MicroEngine_Tiled_TiledMap_TileWidth"></a> TileWidth

Width of a grid cell in pixels.

```csharp
public int TileWidth { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledMap_Tilesets"></a> Tilesets

The tilesets, by increasing [FirstGid](MicroEngine.Tiled.TiledTileset.md#MicroEngine_Tiled_TiledTileset_FirstGid).

```csharp
public IReadOnlyList<TiledTileset> Tilesets { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[TiledTileset](MicroEngine.Tiled.TiledTileset.md)\>

### <a id="MicroEngine_Tiled_TiledMap_Width"></a> Width

Width in tiles.

```csharp
public int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Tiled_TiledMap_CollisionGrid_MicroEngine_Tiled_TiledTileLayer_System_String_"></a> CollisionGrid\(TiledTileLayer, string\)

A collision grid over <code class="paramref">layer</code> whose flags come from each tile's <code class="paramref">property</code>:
a bool property set to true is [Solid](MicroEngine.World.TileFlags.md); a text property lists flag names separated by
commas, bars or spaces (<code>solid</code>, <code>oneway</code>, <code>hazard</code>, <code>water</code>, <code>custom1</code> to
<code>custom4</code>, any case); a whole number is the flags' bits. Tiles without the property have no flags.

```csharp
public TileGrid CollisionGrid(TiledTileLayer layer, string property)
```

#### Parameters

`layer` [TiledTileLayer](MicroEngine.Tiled.TiledTileLayer.md)

`property` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [TileGrid](MicroEngine.World.TileGrid.md)

#### Exceptions

 [NotSupportedException](https://learn.microsoft.com/dotnet/api/system.notsupportedexception)

The map's cells are not square.

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

A tile's property names an unknown flag or has another type; the message names the tile.

### <a id="MicroEngine_Tiled_TiledMap_CollisionGrid_MicroEngine_Tiled_TiledTileLayer_System_Func_MicroEngine_Tiled_TiledTile_MicroEngine_World_TileFlags__"></a> CollisionGrid\(TiledTileLayer, Func<TiledTile, TileFlags\>\)

A collision grid over <code class="paramref">layer</code> whose flags <code class="paramref">flagsOf</code> chooses for each tile.

```csharp
public TileGrid CollisionGrid(TiledTileLayer layer, Func<TiledTile, TileFlags> flagsOf)
```

#### Parameters

`layer` [TiledTileLayer](MicroEngine.Tiled.TiledTileLayer.md)

`flagsOf` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[TiledTile](MicroEngine.Tiled.TiledTile.md), [TileFlags](MicroEngine.World.TileFlags.md)\>

#### Returns

 [TileGrid](MicroEngine.World.TileGrid.md)

#### Exceptions

 [NotSupportedException](https://learn.microsoft.com/dotnet/api/system.notsupportedexception)

The map's cells are not square.

### <a id="MicroEngine_Tiled_TiledMap_Layer__1_System_String_"></a> Layer<T\>\(string\)

The first layer called <code class="paramref">name</code> of type <code class="typeparamref">T</code>, groups included.

```csharp
public T Layer<T>(string name) where T : TiledLayer
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 T

#### Type Parameters

`T` 

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

There is none; the message lists the map's layers.

### <a id="MicroEngine_Tiled_TiledMap_Object_System_Int32_"></a> Object\(int\)

The object with id <code class="paramref">id</code> (what an object property refers to), or null.

```csharp
public TiledObject? Object(int id)
```

#### Parameters

`id` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [TiledObject](MicroEngine.Tiled.TiledObject.md)?

### <a id="MicroEngine_Tiled_TiledMap_ObjectLayer_System_String_"></a> ObjectLayer\(string\)

The object layer called <code class="paramref">name</code> (see [Layer``1](MicroEngine.Tiled.TiledMap.md#MicroEngine_Tiled_TiledMap_Layer__1_System_String_)).

```csharp
public TiledObjectLayer ObjectLayer(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [TiledObjectLayer](MicroEngine.Tiled.TiledObjectLayer.md)

### <a id="MicroEngine_Tiled_TiledMap_ParallaxShift_MicroEngine_Tiled_TiledLayer_System_Numerics_Vector2_"></a> ParallaxShift\(TiledLayer, Vector2\)

How far <code class="paramref">layer</code> moves from its place for a view centred on <code class="paramref">viewCenter</code>, as
Tiled shows it: (view centre − [ParallaxOrigin](MicroEngine.Tiled.TiledMap.md#MicroEngine_Tiled_TiledMap_ParallaxOrigin)) × (1 − the layer's parallax factor). Add it to
where the layer is drawn; it is zero for layers that move with the map.

```csharp
public Vector2 ParallaxShift(TiledLayer layer, Vector2 viewCenter)
```

#### Parameters

`layer` [TiledLayer](MicroEngine.Tiled.TiledLayer.md)

`viewCenter` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Tiled_TiledMap_Tile_System_Int32_"></a> Tile\(int\)

The tile with global id <code class="paramref">gid</code>, or null when no tileset holds it (0 included).

```csharp
public TiledTile? Tile(int gid)
```

#### Parameters

`gid` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [TiledTile](MicroEngine.Tiled.TiledTile.md)?

### <a id="MicroEngine_Tiled_TiledMap_TileLayer_System_String_"></a> TileLayer\(string\)

The tile layer called <code class="paramref">name</code> (see [Layer``1](MicroEngine.Tiled.TiledMap.md#MicroEngine_Tiled_TiledMap_Layer__1_System_String_)).

```csharp
public TiledTileLayer TileLayer(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [TiledTileLayer](MicroEngine.Tiled.TiledTileLayer.md)

</div>

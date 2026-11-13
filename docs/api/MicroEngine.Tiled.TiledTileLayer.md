# Class TiledTileLayer

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Tiled](MicroEngine.Tiled.md)  
Assembly: MicroEngine.Tiled.dll  
Guide: [Maps from Tiled](../manual/world.md#maps-from-tiled)  

A layer of tiles: one global tile id per cell (0 for none) and how each cell's tile is turned.

```csharp
public sealed class TiledTileLayer : TiledLayer
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TiledLayer](MicroEngine.Tiled.TiledLayer.md) ← 
[TiledTileLayer](MicroEngine.Tiled.TiledTileLayer.md)

#### Inherited Members

[TiledLayer.ToString\(\)](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_ToString), 
[TiledLayer.Id](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_Id), 
[TiledLayer.Name](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_Name), 
[TiledLayer.Class](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_Class), 
[TiledLayer.Visible](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_Visible), 
[TiledLayer.Opacity](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_Opacity), 
[TiledLayer.Tint](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_Tint), 
[TiledLayer.Offset](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_Offset), 
[TiledLayer.Parallax](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_Parallax), 
[TiledLayer.Properties](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_Properties), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Properties

### <a id="MicroEngine_Tiled_TiledTileLayer_Tiles"></a> Tiles

The global tile ids, the size of the map: [Tile](MicroEngine.Tiled.TiledMap.md#MicroEngine_Tiled_TiledMap_Tile_System_Int32_) gives each one's tileset, properties and
animation. Changing a cell here changes what is drawn and collided with.

```csharp
public TileLayer Tiles { get; }
```

#### Property Value

 [TileLayer](MicroEngine.World.TileLayer.md)

## Methods

### <a id="MicroEngine_Tiled_TiledTileLayer_OrientationAt_System_Int32_System_Int32_"></a> OrientationAt\(int, int\)

How the tile at (<code class="paramref">x</code>, <code class="paramref">y</code>) is mirrored or turned (none outside the map).

```csharp
public TileOrientation OrientationAt(int x, int y)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [TileOrientation](MicroEngine.World.TileOrientation.md)

### <a id="MicroEngine_Tiled_TiledTileLayer_SetOrientation_System_Int32_System_Int32_MicroEngine_World_TileOrientation_"></a> SetOrientation\(int, int, TileOrientation\)

Sets how the tile at (<code class="paramref">x</code>, <code class="paramref">y</code>) is mirrored or turned; ignored outside the map.

```csharp
public void SetOrientation(int x, int y, TileOrientation orientation)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`orientation` [TileOrientation](MicroEngine.World.TileOrientation.md)

</div>

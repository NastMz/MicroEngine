# Interface ICollisionGrid

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.World](MicroEngine.World.md)  
Assembly: MicroEngine.Physics.Tiles.dll  
Guide: [Tile maps and collision](../manual/world.md)  

Answers collision queries for [TileMotion](MicroEngine.World.TileMotion.md).

```csharp
public interface ICollisionGrid
```

## Properties

### <a id="MicroEngine_World_ICollisionGrid_TileSize"></a> TileSize

Size of a square tile in pixels.

```csharp
int TileSize { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_World_ICollisionGrid_FlagsAt_System_Int32_System_Int32_"></a> FlagsAt\(int, int\)

The flags of the tile at (<code class="paramref">tileX</code>, <code class="paramref">tileY</code>), in tiles; any coordinate, inside the map or not.

```csharp
TileFlags FlagsAt(int tileX, int tileY)
```

#### Parameters

`tileX` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`tileY` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [TileFlags](MicroEngine.World.TileFlags.md)

</div>

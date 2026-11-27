# Class CollisionGridViews

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.World](MicroEngine.World.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Tile maps and collision](../manual/world.md)  

Views of a tile grid for movers that are blocked by different tiles.

```csharp
public static class CollisionGridViews
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CollisionGridViews](MicroEngine.World.CollisionGridViews.md)

## Methods

### <a id="MicroEngine_World_CollisionGridViews_Blocking_MicroEngine_World_ICollisionGrid_MicroEngine_World_TileFlags_"></a> Blocking\(ICollisionGrid, TileFlags\)

The same tiles as a mover that is blocked by <code class="paramref">flags</code> sees them: a tile with any of those
flags is [Solid](MicroEngine.World.TileFlags.md), and no other tile is. A level whose walkers are stopped by water and
whose flyers are not adds two views of its one grid, each in a layer of its own:
<code>AddTiles(level, grid.Blocking(TileFlags.Solid | TileFlags.Water), ..., category: WalkerTiles)</code> and
<code>AddTiles(level, grid.Blocking(TileFlags.Solid), ..., category: FlyerTiles)</code>. The view reads the grid at
every call, so the two never differ; every other flag (one-way, hazard, slopes, the custom ones) is kept.

```csharp
public static ICollisionGrid Blocking(this ICollisionGrid grid, TileFlags flags)
```

#### Parameters

`grid` [ICollisionGrid](MicroEngine.World.ICollisionGrid.md)

The tiles.

`flags` [TileFlags](MicroEngine.World.TileFlags.md)

The flags that block this kind of mover.

#### Returns

 [ICollisionGrid](MicroEngine.World.ICollisionGrid.md)

</div>

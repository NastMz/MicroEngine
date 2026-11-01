# Class TileMotion

Namespace: [MicroEngine.World](MicroEngine.World.md)  
Assembly: MicroEngine.dll  
Guide: [Tile maps and collision](../manual/world.md)  

Moves an axis-aligned box through a tile grid (plus optional extra solid rectangles such as
moving platforms or push blocks). X is resolved before Y, in sub-steps no larger than half a
tile, so fast bodies never tunnel. One-way tiles only stop bodies falling onto their top edge.

```csharp
public static class TileMotion
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TileMotion](MicroEngine.World.TileMotion.md)

## Methods

### <a id="MicroEngine_World_TileMotion_IsOnGround_MicroEngine_RectF_MicroEngine_World_ICollisionGrid_System_Collections_Generic_IReadOnlyList_MicroEngine_RectF__System_Boolean_"></a> IsOnGround\(RectF, ICollisionGrid, IReadOnlyList<RectF\>?, bool\)

True when a 1px probe directly below the box would collide (standing on ground or a platform).

```csharp
public static bool IsOnGround(RectF box, ICollisionGrid grid, IReadOnlyList<RectF>? solids = null, bool ignoreOneWay = false)
```

#### Parameters

`box` [RectF](MicroEngine.RectF.md)

`grid` [ICollisionGrid](MicroEngine.World.ICollisionGrid.md)

`solids` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[RectF](MicroEngine.RectF.md)\>?

`ignoreOneWay` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_TileMotion_IsSolidAt_System_Numerics_Vector2_MicroEngine_World_ICollisionGrid_"></a> IsSolidAt\(Vector2, ICollisionGrid\)

Whether the tile under <code class="paramref">point</code> (world pixels) is solid. One-way tiles do not count.

```csharp
public static bool IsSolidAt(Vector2 point, ICollisionGrid grid)
```

#### Parameters

`point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`grid` [ICollisionGrid](MicroEngine.World.ICollisionGrid.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_TileMotion_Move_MicroEngine_RectF_System_Numerics_Vector2_MicroEngine_World_ICollisionGrid_System_Collections_Generic_IReadOnlyList_MicroEngine_RectF__System_Boolean_"></a> Move\(RectF, Vector2, ICollisionGrid, IReadOnlyList<RectF\>?, bool\)

Moves <code class="paramref">box</code> by <code class="paramref">delta</code>, stopping flush against solid tiles, one-way tiles (from above) and <code class="paramref">solids</code>.

```csharp
public static MoveResult Move(RectF box, Vector2 delta, ICollisionGrid grid, IReadOnlyList<RectF>? solids = null, bool ignoreOneWay = false)
```

#### Parameters

`box` [RectF](MicroEngine.RectF.md)

The body's hitbox in world pixels.

`delta` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Movement in pixels for this step (velocity × dt).

`grid` [ICollisionGrid](MicroEngine.World.ICollisionGrid.md)

The tiles to collide with.

`solids` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[RectF](MicroEngine.RectF.md)\>?

Extra solid rectangles; one the box already overlaps before moving is ignored, so a body is never trapped inside it.

`ignoreOneWay` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Passes through one-way tiles (dropping down through a platform).

#### Returns

 [MoveResult](MicroEngine.World.MoveResult.md)

The new box and what it hit. On a hit, movement along that axis stops for the rest of this step.

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">delta</code> is not finite.

### <a id="MicroEngine_World_TileMotion_TouchedFlags_MicroEngine_RectF_MicroEngine_World_ICollisionGrid_"></a> TouchedFlags\(RectF, ICollisionGrid\)

Union of flags of every tile overlapping the box.

```csharp
public static TileFlags TouchedFlags(RectF box, ICollisionGrid grid)
```

#### Parameters

`box` [RectF](MicroEngine.RectF.md)

`grid` [ICollisionGrid](MicroEngine.World.ICollisionGrid.md)

#### Returns

 [TileFlags](MicroEngine.World.TileFlags.md)

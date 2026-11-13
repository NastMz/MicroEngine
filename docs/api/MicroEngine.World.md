# Namespace MicroEngine.World

<div data-pagefind-weight="0.3">

Tile layers, collision grids, swept movement against tiles (`TileMotion`), autotiling and drawing tiles.

Guide: [Tile maps and collision](../manual/world.md)  

### Classes

 [Autotile](MicroEngine.World.Autotile.md)

Helpers for picking tile sprites from their neighbours.

 [TileGrid](MicroEngine.World.TileGrid.md)

A collision grid backed by a [TileLayer](MicroEngine.World.TileLayer.md) and a per-id flag table.
Cells outside the map use [OutsideFlags](MicroEngine.World.TileGrid.md#MicroEngine_World_TileGrid_OutsideFlags) (solid by default, so levels are closed).

 [TileLayer](MicroEngine.World.TileLayer.md)

A rectangular grid of integer tile ids (0 conventionally means empty).

 [TileMotion](MicroEngine.World.TileMotion.md)

Moves an axis-aligned box through a tile grid (plus optional extra solid rectangles such as moving platforms or push
blocks). X is resolved before Y, each as a swept test in sub-steps no larger than half a tile: the box stops flush
against the first solid tile or rectangle in its way, however thin, so it never tunnels. One-way tiles only stop
bodies falling onto their top edge.

 [TileOrientations](MicroEngine.World.TileOrientations.md)

Turns a [TileOrientation](MicroEngine.World.TileOrientation.md) into drawing options.

 [TileRenderer](MicroEngine.World.TileRenderer.md)

Draws the visible part of a layer, asking the game which sprite each tile uses.

### Structs

 [TileMoveResult](MicroEngine.World.TileMoveResult.md)

What happened during a [Move](MicroEngine.World.TileMotion.md#MicroEngine_World_TileMotion_Move_MicroEngine_RectF_System_Numerics_Vector2_MicroEngine_World_ICollisionGrid_System_Collections_Generic_IReadOnlyList_MicroEngine_RectF__System_Boolean_). A body of the physics contract reports its move in
[MoveResult](MicroEngine.Physics.MoveResult.md) instead.

### Interfaces

 [ICollisionGrid](MicroEngine.World.ICollisionGrid.md)

Answers collision queries for [TileMotion](MicroEngine.World.TileMotion.md).

### Enums

 [TileFlags](MicroEngine.World.TileFlags.md)

Collision semantics of a tile. Games may use the Custom bits for their own meaning.

 [TileOrientation](MicroEngine.World.TileOrientation.md)

How one cell's tile is mirrored or turned, as map editors store it (Tiled's flip bits). The diagonal flip (x and y
swapped) applies first, then the horizontal and the vertical flips, so the eight combinations cover every quarter
turn with or without a mirror: <code>Diagonal | FlipX</code> is a quarter turn clockwise, <code>Diagonal | FlipY</code> a
quarter turn anticlockwise, <code>FlipX | FlipY</code> a half turn.

</div>

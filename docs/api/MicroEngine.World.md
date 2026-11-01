# Namespace MicroEngine.World

<div data-pagefind-weight="0.3">

Tile layers, collision grids, swept movement against tiles, autotiling and plain entities.

Guide: [Tile maps and collision](../manual/world.md)  

### Classes

 [Autotile](MicroEngine.World.Autotile.md)

Helpers for picking tile sprites from their neighbours.

 [Entity](MicroEngine.World.Entity.md)

A lightweight game object: a position, a hitbox relative to it, and update/draw hooks.
MicroEngine deliberately prefers small inheritance-based entities over an ECS: the sample
games have dozens of objects, and plain classes keep game code readable and debuggable.

 [EntityList<T\>](MicroEngine.World.EntityList\-1.md)

Owns entities of a kind. Additions and removals made during iteration are deferred to
the end of the update, so entities can spawn and destroy each other safely.

 [TileGrid](MicroEngine.World.TileGrid.md)

A collision grid backed by a [TileLayer](MicroEngine.World.TileLayer.md) and a per-id flag table.
Cells outside the map use [OutsideFlags](MicroEngine.World.TileGrid.md#MicroEngine_World_TileGrid_OutsideFlags) (solid by default, so levels are closed).

 [TileLayer](MicroEngine.World.TileLayer.md)

A rectangular grid of integer tile ids (0 conventionally means empty).

 [TileMotion](MicroEngine.World.TileMotion.md)

Moves an axis-aligned box through a tile grid (plus optional extra solid rectangles such as
moving platforms or push blocks). X is resolved before Y, in sub-steps no larger than half a
tile, so fast bodies never tunnel. One-way tiles only stop bodies falling onto their top edge.

 [TileRenderer](MicroEngine.World.TileRenderer.md)

Draws the visible part of a layer, asking the game which sprite each tile uses.

### Structs

 [MoveResult](MicroEngine.World.MoveResult.md)

What happened during a [Move](MicroEngine.World.TileMotion.md#MicroEngine_World_TileMotion_Move_MicroEngine_RectF_System_Numerics_Vector2_MicroEngine_World_ICollisionGrid_System_Collections_Generic_IReadOnlyList_MicroEngine_RectF__System_Boolean_).

### Interfaces

 [ICollisionGrid](MicroEngine.World.ICollisionGrid.md)

Answers collision queries for [TileMotion](MicroEngine.World.TileMotion.md).

### Enums

 [TileFlags](MicroEngine.World.TileFlags.md)

Collision semantics of a tile. Games may use the Custom bits for their own meaning.

</div>

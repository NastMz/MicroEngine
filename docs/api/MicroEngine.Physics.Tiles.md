# Namespace MicroEngine.Physics.Tiles

<div data-pagefind-weight="0.3">

The tile backend of the physics contract: tile grids as static geometry, the exact swept mover, one-way platforms and moving solids that push and carry bodies.

Guide: [Physics worlds](../manual/world.md#physics-worlds)  

### Classes

 [TilePhysicsWorld2D](MicroEngine.Physics.Tiles.TilePhysicsWorld2D.md)

The tile backend of [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md): static and kinematic bodies with axis-aligned box shapes, tile grids
as static geometry ([AddTiles](MicroEngine.Physics.Tiles.TilePhysicsWorld2D.md#MicroEngine_Physics_Tiles_TilePhysicsWorld2D_AddTiles_MicroEngine_Physics_BodyId_MicroEngine_World_ICollisionGrid_System_UInt64_System_UInt64_)), and an exact swept mover. It declares
[TileGeometry](MicroEngine.Physics.PhysicsCapabilities.md), [OneWayPlatforms](MicroEngine.Physics.PhysicsCapabilities.md) and
[MovingSolids](MicroEngine.Physics.PhysicsCapabilities.md); it has no dynamic bodies, so [ContactEvents](MicroEngine.Physics.Tiles.TilePhysicsWorld2D.md#MicroEngine_Physics_Tiles_TilePhysicsWorld2D_ContactEvents) is always
empty, and every other capability throws [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md).

</div>

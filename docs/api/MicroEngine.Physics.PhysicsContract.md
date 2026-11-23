# Class PhysicsContract

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

The version of the 2D physics contract ([IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md)); a change to its rules raises it.

```csharp
public static class PhysicsContract
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PhysicsContract](MicroEngine.Physics.PhysicsContract.md)

## Fields

### <a id="MicroEngine_Physics_PhysicsContract_OneWayTileThickness"></a> OneWayTileThickness

How thick the platform of a one-way tile is, as a share of the tile, from the tile's top: what overlaps, point
queries and the nearest surface see of a [OneWay](MicroEngine.World.TileFlags.md) cell on every backend
(moves, rays and cast shapes meet its top edge).

```csharp
public const float OneWayTileThickness = 0.25
```

#### Field Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_PhysicsContract_Version"></a> Version

2: version 1 (the common core and capabilities of the first two backends) plus tile geometry as a contract member
([AddTiles](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_AddTiles_MicroEngine_Physics_BodyId_MicroEngine_World_ICollisionGrid_System_Int32_System_Int32_System_UInt64_System_UInt64_)), dynamics control (forces, impulses, torque, angular velocity,
[BodyDynamics](MicroEngine.Physics.BodyDynamics.md), sleep, mass data), joints, shape queries (cast shapes, every hit of a ray, shape and
point overlaps, the nearest surface) and contact detail (impacts, contact points). Every member added since version 1 has a default that refuses with the capability it needs, so
a backend written for version 1 still compiles and declares what it implements.

```csharp
public const int Version = 2
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

</div>

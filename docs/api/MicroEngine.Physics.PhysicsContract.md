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

### <a id="MicroEngine_Physics_PhysicsContract_Version"></a> Version

2: version 1 (the common core and capabilities of the first two backends) plus tile geometry as a contract member
([AddTiles](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_AddTiles_MicroEngine_Physics_BodyId_MicroEngine_World_ICollisionGrid_System_Int32_System_Int32_System_UInt64_System_UInt64_)), dynamics control (forces, impulses, torque, angular velocity,
[BodyDynamics](MicroEngine.Physics.BodyDynamics.md), sleep, mass data), joints, shape casts, point and circle queries, all-hits rays and
contact details. Every member added since version 1 has a default that refuses with the capability it needs, so
a backend written for version 1 still compiles and declares what it implements.

```csharp
public const int Version = 2
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

</div>

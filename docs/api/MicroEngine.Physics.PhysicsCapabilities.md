# Enum PhysicsCapabilities

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

What a physics backend offers beyond the common contract of [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md). Every backend passes the
common conformance suite; each flag it declares has a suite of its own that runs only on backends declaring it. A
request that needs an undeclared flag fails with [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md): a backend never emulates
a capability silently.

```csharp
[Flags]
public enum PhysicsCapabilities
```

## Fields

`Capsules = 32` 

Capsule shapes ([Capsule](MicroEngine.Physics.ShapeKind.md)), including a capsule as the shape [Move](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__) moves.



`Circles = 64` 

Circle shapes ([Circle](MicroEngine.Physics.ShapeKind.md)).



`ContinuousCollision = 256` 

Continuous collision for fast dynamic bodies ([Bullet](MicroEngine.Physics.BodyDescription.md#MicroEngine_Physics_BodyDescription_Bullet)).



`DynamicBodies = 4` 

Dynamic bodies ([Dynamic](MicroEngine.Physics.BodyKind.md)) moved by the solver: velocities, gravity, contact events.



`FrictionRestitution = 512` 

Per-shape friction and restitution ([Friction](MicroEngine.Physics.ShapeDescription.md#MicroEngine_Physics_ShapeDescription_Friction), [Restitution](MicroEngine.Physics.ShapeDescription.md#MicroEngine_Physics_ShapeDescription_Restitution)).



`Joints = 16` 

Joints between bodies.



`MassProperties = 1024` 

Per-shape density and the mass it gives a dynamic body ([Density](MicroEngine.Physics.ShapeDescription.md#MicroEngine_Physics_ShapeDescription_Density)).



`MovingSolids = 2048` 

Moving solids ([PushesBodies](MicroEngine.Physics.BodyDescription.md#MicroEngine_Physics_BodyDescription_PushesBodies)): a kinematic body that, when moved, pushes the
kinematic bodies in its way and carries the ones resting on it.



`None = 0` 

Only the common contract.



`OneWayPlatforms = 2` 

One-way platforms ([OneWay](MicroEngine.Physics.ShapeDescription.md#MicroEngine_Physics_ShapeDescription_OneWay)): a shape that stops only bodies coming down onto its
up-facing side (against [Up](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Up)), implemented inside the backend.



`Polygons = 128` 

Convex polygon shapes ([Polygon](MicroEngine.Physics.ShapeKind.md)).



`Rotation = 8` 

Rotated bodies ([Rotation](MicroEngine.Physics.BodyDescription.md#MicroEngine_Physics_BodyDescription_Rotation) other than 0) and bodies that rotate.



`ShapeInspection = 4096` 

Reading shapes back ([ShapesOf](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_ShapesOf_MicroEngine_Physics_BodyId_System_Span_MicroEngine_Physics_ShapeId__), [DescriptionOf](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_DescriptionOf_MicroEngine_Physics_ShapeId_)): what a
debug view needs to draw the world. Both built-in backends declare it.



`TileGeometry = 1` 

Grid-aligned static geometry fed directly from tile layers (the tile backend's own <code>AddTiles</code>).

</div>

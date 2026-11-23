# Interface IPhysicsWorld2D

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

A 2D physics world: bodies owned by entities, shapes with collision layers, kinematic collide-and-slide moves, ray
and overlap queries, sensors, and a step. The members below are the common contract every backend passes (one
conformance suite); everything else is a declared capability ([Capabilities](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Capabilities)), and a request that needs
an undeclared one throws [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md) without changing the world.

```csharp
public interface IPhysicsWorld2D
```

## Remarks

The rules of the contract:

<ol><li><b>Bodies and shapes.</b> Static and kinematic bodies and axis-aligned box shapes are common; a shape is placed
at its body's origin plus its offset. Ids are generation-checked: a stale id is never alive, and using one throws
`InvalidOperationException`; destroying a body destroys its shapes.</li><li><b>Layers.</b> Two shapes collide only when each one's [Mask](MicroEngine.Physics.ShapeDescription.md#MicroEngine_Physics_ShapeDescription_Mask) holds the other's
[Category](MicroEngine.Physics.ShapeDescription.md#MicroEngine_Physics_ShapeDescription_Category); a query or move sees a shape only when [Accepts](MicroEngine.Physics.QueryFilter.md#MicroEngine_Physics_QueryFilter_Accepts_System_UInt64_System_UInt64_) is
true for it.</li><li><b>Move</b> moves a kinematic body's one non-sensor shape, an axis-aligned box (the common character shape;
a capsule needs [Capsules](MicroEngine.Physics.PhysicsCapabilities.md)), by collide-and-slide against the non-sensor shapes the
filter accepts: it never passes through a shape at the speeds the conformance suite tests, and a blocked axis stops
while the others go on. It may stop short of a surface by a gap of at most [Skin](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Skin), the tolerance the
backend declares (0 on the tile backend). The body's contacts and its ground are in the [MoveResult](MicroEngine.Physics.MoveResult.md); a
shape the box already overlaps when the move starts does not block it, so a body is never trapped.</li><li><b>Queries.</b> [RayCast](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_RayCast_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__MicroEngine_Physics_RayHit__) returns the closest hit; [Overlap](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Overlap_MicroEngine_RectF__MicroEngine_Physics_QueryFilter__System_Span_MicroEngine_Physics_ShapeId__) is exact (shapes that
truly overlap the area, not their bounding boxes; sharing an edge is not overlapping). Both leave sensors out unless
the filter includes them.</li><li><b>Edges.</b> One rule for every backend. A point on a shape's edge or corner is not inside it. A ray hits a
shape where it enters it: one that starts inside a shape does not hit it; one that starts on its edge hits it at
fraction 0 going in, and not going out or along the edge; one that only runs along an edge or clips a corner does
not hit. A cast shape does not hit what it overlaps where it starts; what it only touches there it hits at fraction
0 going in, and not going out or sliding along it, which is what [Move](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__) does from the same place. On a
backend with a [Skin](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Skin), two shapes that overlap by less than the skin touch.</li><li><b>Tile geometry is one shape with one surface.</b> The faces between two solid tiles are not surfaces: a
shape that starts sunk into tiles is not stopped by the next tile. A one-way tile is a platform at the top of its
cell, [OneWayTileThickness](MicroEngine.Physics.PhysicsContract.md#MicroEngine_Physics_PhysicsContract_OneWayTileThickness) of a tile thick for overlaps, points and the nearest
surface.</li><li><b>Sensors.</b> A sensor reports the non-sensor shapes of kinematic and dynamic bodies that begin or stop
overlapping it, when the layers of both accept each other and the visitor has
[SensorEvents](MicroEngine.Physics.ShapeDescription.md#MicroEngine_Physics_ShapeDescription_SensorEvents) (the default). Sensors do not see other sensors or static shapes.</li><li><b>Events are buffered per step.</b> [Step](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Step_System_Single_) computes [SensorEvents](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_SensorEvents) and
[ContactEvents](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_ContactEvents) from the poses at its end (moves made before it count in it); they stay readable until
the next step, in an order that depends only on the world's history. Contact events only come from pairs with a
dynamic body. Destroying a shape ends its pairs with an exit or end event at the next step.</li><li><b>No managed callbacks during [Step](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Step_System_Single_).</b> Per-shape rules (one-way platforms) are shape flags the
backend implements itself; games read events after the step.</li><li><b>Stepping.</b> A scene steps its physics world after its own update, by the step of its world's clock, and
writes body poses to their owners' transforms, which the scene world interpolates for drawing (the engine's
<code>ScenePhysics</code>). [PhysicsModule](MicroEngine.Physics.PhysicsModule.md) steps worlds that belong to no scene, in the kernel's
<code>FramePhase.Physics</code>.</li><li><b>Determinism.</b> The same backend build gives identical results on every target for the same calls (native
backends are built with floating-point contraction off and step on one thread). Nothing is promised between two
backends, or between two versions of a backend's physics library: they differ from the first step.</li></ol>

A world is not thread-safe.

## Properties

### <a id="MicroEngine_Physics_IPhysicsWorld2D_Backend"></a> Backend

The backend's name, for messages ("tiles", "box2d").

```csharp
string Backend { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Physics_IPhysicsWorld2D_Bodies"></a> Bodies

The live bodies, in creation order (a destroyed body leaves its place).

```csharp
ReadOnlySpan<BodyId> Bodies { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[BodyId](MicroEngine.Physics.BodyId.md)\>

### <a id="MicroEngine_Physics_IPhysicsWorld2D_BrokenJoints"></a> BrokenJoints

The joints that broke in the latest step ([BreakForce](MicroEngine.Physics.JointDescription.md#MicroEngine_Physics_JointDescription_BreakForce)); they no longer exist.

```csharp
ReadOnlySpan<JointId> BrokenJoints { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[JointId](MicroEngine.Physics.JointId.md)\>

### <a id="MicroEngine_Physics_IPhysicsWorld2D_Capabilities"></a> Capabilities

What the backend offers beyond the common contract.

```csharp
PhysicsCapabilities Capabilities { get; }
```

#### Property Value

 [PhysicsCapabilities](MicroEngine.Physics.PhysicsCapabilities.md)

### <a id="MicroEngine_Physics_IPhysicsWorld2D_ContactEvents"></a> ContactEvents

The contacts that began or ended in the latest step (pairs with a dynamic body only).

```csharp
ReadOnlySpan<ContactEvent> ContactEvents { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[ContactEvent](MicroEngine.Physics.ContactEvent.md)\>

### <a id="MicroEngine_Physics_IPhysicsWorld2D_ContactImpacts"></a> ContactImpacts

The impacts of the latest step: every pair with a dynamic body whose surfaces met faster than the backend's
impact speed, with the point, the speed and the impulse ([ContactDetail](MicroEngine.Physics.PhysicsCapabilities.md)). Empty
on a backend that does not declare it.

```csharp
ReadOnlySpan<ContactImpact> ContactImpacts { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[ContactImpact](MicroEngine.Physics.ContactImpact.md)\>

### <a id="MicroEngine_Physics_IPhysicsWorld2D_Joints"></a> Joints

The live joints, in creation order.

```csharp
ReadOnlySpan<JointId> Joints { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[JointId](MicroEngine.Physics.JointId.md)\>

### <a id="MicroEngine_Physics_IPhysicsWorld2D_SensorEvents"></a> SensorEvents

The sensor entries and exits of the latest step.

```csharp
ReadOnlySpan<SensorEvent> SensorEvents { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[SensorEvent](MicroEngine.Physics.SensorEvent.md)\>

### <a id="MicroEngine_Physics_IPhysicsWorld2D_Skin"></a> Skin

The largest gap [Move](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__) may leave between a body and the surface that stopped it or that it rests on, in
world units: 0 when resting is exact.

```csharp
float Skin { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_IPhysicsWorld2D_StepCount"></a> StepCount

How many times [Step](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Step_System_Single_) ran.

```csharp
long StepCount { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Physics_IPhysicsWorld2D_Up"></a> Up

The world's up direction (unit length): it tells ground from ceilings and which side of a one-way platform stops bodies.

```csharp
Vector2 Up { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Methods

### <a id="MicroEngine_Physics_IPhysicsWorld2D_AddShape_MicroEngine_Physics_BodyId_MicroEngine_Physics_ShapeDescription__"></a> AddShape\(BodyId, in ShapeDescription\)

Attaches a shape to <code class="paramref">body</code>.

```csharp
ShapeId AddShape(BodyId body, in ShapeDescription shape)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`shape` [ShapeDescription](MicroEngine.Physics.ShapeDescription.md)

#### Returns

 [ShapeId](MicroEngine.Physics.ShapeId.md)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The description needs an undeclared capability.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The description is invalid ([Validate](MicroEngine.Physics.ShapeDescription.md#MicroEngine_Physics_ShapeDescription_Validate)).

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The body id is stale.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_AddTiles_MicroEngine_Physics_BodyId_MicroEngine_World_ICollisionGrid_System_Int32_System_Int32_System_UInt64_System_UInt64_"></a> AddTiles\(BodyId, ICollisionGrid, int, int, ulong, ulong\)

Attaches the tiles of <code class="paramref">grid</code> to a static body as one shape
([TileGeometry](MicroEngine.Physics.PhysicsCapabilities.md)): cell (x, y) is a square of [TileSize](MicroEngine.World.ICollisionGrid.md#MicroEngine_World_ICollisionGrid_TileSize)
units, x tiles sideways and y tiles downward (against [Up](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Up)) from the body's origin, so row 0 is the
top row whichever way the world's y grows. [Solid](MicroEngine.World.TileFlags.md) cells block from every side and
[OneWay](MicroEngine.World.TileFlags.md) cells are one-way platforms (where the backend declares them); other cells are
not geometry.

```csharp
ShapeId AddTiles(BodyId body, ICollisionGrid grid, int columns, int rows, ulong category = 1, ulong mask = 18446744073709551615)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

A static body.

`grid` [ICollisionGrid](MicroEngine.World.ICollisionGrid.md)

The tiles.

`columns` [int](https://learn.microsoft.com/dotnet/api/system.int32)

How many columns of the grid hold the level.

`rows` [int](https://learn.microsoft.com/dotnet/api/system.int32)

How many rows of the grid hold the level.

`category` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

The layers the tiles are in.

`mask` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

The layers they collide with.

#### Returns

 [ShapeId](MicroEngine.Physics.ShapeId.md)

#### Remarks

The tile backend reads the grid at every query, inside and outside that area, so changing a tile changes
collisions at once. A backend that builds shapes from the tiles reads the area and one tile around it (the border
a grid closes its level with) when this is called: remove the shape and add the tiles again after changing them.

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [TileGeometry](MicroEngine.Physics.PhysicsCapabilities.md).

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The body is stale or not static.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">columns</code> or <code class="paramref">rows</code> is not positive.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_AnchorAOf_MicroEngine_Physics_JointId_"></a> AnchorAOf\(JointId\)

The joint's anchor on body A, a world point, as the body is now.

```csharp
Vector2 AnchorAOf(JointId joint)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_AnchorBOf_MicroEngine_Physics_JointId_"></a> AnchorBOf\(JointId\)

The joint's anchor on body B, a world point, as the body is now.

```csharp
Vector2 AnchorBOf(JointId joint)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_AngularVelocityOf_MicroEngine_Physics_BodyId_"></a> AngularVelocityOf\(BodyId\)

A body's angular velocity in radians per second; positive turns the way [RotationOf](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_RotationOf_MicroEngine_Physics_BodyId_) grows.

```csharp
float AngularVelocityOf(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md) and [Rotation](MicroEngine.Physics.PhysicsCapabilities.md).

### <a id="MicroEngine_Physics_IPhysicsWorld2D_ApplyAngularImpulse_MicroEngine_Physics_BodyId_System_Single_System_Boolean_"></a> ApplyAngularImpulse\(BodyId, float, bool\)

Applies an angular impulse (inertia × radians per second) to a dynamic body: its angular velocity changes at once.

```csharp
void ApplyAngularImpulse(BodyId body, float impulse, bool wake = true)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`impulse` [float](https://learn.microsoft.com/dotnet/api/system.single)

`wake` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md) and [Rotation](MicroEngine.Physics.PhysicsCapabilities.md).

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale or the body is not dynamic.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_ApplyForce_MicroEngine_Physics_BodyId_System_Numerics_Vector2_System_Boolean_"></a> ApplyForce\(BodyId, Vector2, bool\)

Applies <code class="paramref">force</code> (mass × world units per second squared) through a dynamic body's centre of
mass for the next step only: apply it every step for as long as it acts.

```csharp
void ApplyForce(BodyId body, Vector2 force, bool wake = true)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

A dynamic body.

`force` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The force.

`wake` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether a sleeping body wakes; when false, a sleeping body ignores the force.

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md).

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale or the body is not dynamic.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The force is not finite.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_ApplyForce_MicroEngine_Physics_BodyId_System_Numerics_Vector2_System_Numerics_Vector2_System_Boolean_"></a> ApplyForce\(BodyId, Vector2, Vector2, bool\)

Applies <code class="paramref">force</code> at the world point <code class="paramref">point</code> for the next step only: off the centre of mass it also turns the body.

```csharp
void ApplyForce(BodyId body, Vector2 force, Vector2 point, bool wake = true)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`force` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`wake` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md).

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale or the body is not dynamic.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_ApplyImpulse_MicroEngine_Physics_BodyId_System_Numerics_Vector2_System_Boolean_"></a> ApplyImpulse\(BodyId, Vector2, bool\)

Applies <code class="paramref">impulse</code> (mass × world units per second) through a dynamic body's centre of mass: its
velocity changes at once by the impulse divided by its mass (a jump, a hit, an explosion).

```csharp
void ApplyImpulse(BodyId body, Vector2 impulse, bool wake = true)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`impulse` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`wake` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md).

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale or the body is not dynamic.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The impulse is not finite.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_ApplyImpulse_MicroEngine_Physics_BodyId_System_Numerics_Vector2_System_Numerics_Vector2_System_Boolean_"></a> ApplyImpulse\(BodyId, Vector2, Vector2, bool\)

Applies <code class="paramref">impulse</code> at the world point <code class="paramref">point</code>: off the centre of mass it also spins the body.

```csharp
void ApplyImpulse(BodyId body, Vector2 impulse, Vector2 point, bool wake = true)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`impulse` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`wake` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md).

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale or the body is not dynamic.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_ApplyTorque_MicroEngine_Physics_BodyId_System_Single_System_Boolean_"></a> ApplyTorque\(BodyId, float, bool\)

Applies a torque (inertia × radians per second squared) to a dynamic body for the next step only.

```csharp
void ApplyTorque(BodyId body, float torque, bool wake = true)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`torque` [float](https://learn.microsoft.com/dotnet/api/system.single)

`wake` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md) and [Rotation](MicroEngine.Physics.PhysicsCapabilities.md).

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale or the body is not dynamic.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_BodyOf_MicroEngine_Physics_ShapeId_"></a> BodyOf\(ShapeId\)

The body <code class="paramref">shape</code> is attached to.

```csharp
BodyId BodyOf(ShapeId shape)
```

#### Parameters

`shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

#### Returns

 [BodyId](MicroEngine.Physics.BodyId.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_BoundsOf_MicroEngine_Physics_ShapeId_"></a> BoundsOf\(ShapeId\)

The world bounds of <code class="paramref">shape</code> as it is now (the area of the level for tile geometry).

```csharp
RectF BoundsOf(ShapeId shape)
```

#### Parameters

`shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

#### Returns

 [RectF](MicroEngine.RectF.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [ShapeInspection](MicroEngine.Physics.PhysicsCapabilities.md).

### <a id="MicroEngine_Physics_IPhysicsWorld2D_CenterOfMassOf_MicroEngine_Physics_BodyId_"></a> CenterOfMassOf\(BodyId\)

A body's centre of mass, a world point (its origin for bodies without mass).

```csharp
Vector2 CenterOfMassOf(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [MassProperties](MicroEngine.Physics.PhysicsCapabilities.md).

### <a id="MicroEngine_Physics_IPhysicsWorld2D_ClosestPoint_System_Numerics_Vector2_System_Single_MicroEngine_Physics_QueryFilter__MicroEngine_Physics_ClosestPointHit__"></a> ClosestPoint\(Vector2, float, in QueryFilter, out ClosestPointHit\)

The surface nearest <code class="paramref">point</code> among the shapes within <code class="paramref">maxDistance</code> of it
(shapes at the same distance: the first created); a shape that holds the point is at distance 0.

```csharp
bool ClosestPoint(Vector2 point, float maxDistance, in QueryFilter filter, out ClosestPointHit hit)
```

#### Parameters

`point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`maxDistance` [float](https://learn.microsoft.com/dotnet/api/system.single)

`filter` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

`hit` [ClosestPointHit](MicroEngine.Physics.ClosestPointHit.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [ShapeQueries](MicroEngine.Physics.PhysicsCapabilities.md).

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The point is not finite, or the distance is negative or not finite.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_ContactsOf_MicroEngine_Physics_BodyId_System_Span_MicroEngine_Physics_ContactPoint__"></a> ContactsOf\(BodyId, Span<ContactPoint\>\)

Writes into <code class="paramref">results</code> the points at which <code class="paramref">body</code> touches other shapes now
(up to two per pair of shapes), as the latest step left them, and returns how many there are (which may exceed
the span's length). Contacts exist between a dynamic body and anything else: how hard a crate presses on a
plate, whether a dynamic body stands on something.

```csharp
int ContactsOf(BodyId body, Span<ContactPoint> results)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`results` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[ContactPoint](MicroEngine.Physics.ContactPoint.md)\>

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [ContactDetail](MicroEngine.Physics.PhysicsCapabilities.md).

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_CreateBody_MicroEngine_Scenes_EntityId_MicroEngine_Physics_BodyDescription__"></a> CreateBody\(EntityId, in BodyDescription\)

Creates a body owned by <code class="paramref">owner</code>.

```csharp
BodyId CreateBody(EntityId owner, in BodyDescription body)
```

#### Parameters

`owner` [EntityId](MicroEngine.Scenes.EntityId.md)

`body` [BodyDescription](MicroEngine.Physics.BodyDescription.md)

#### Returns

 [BodyId](MicroEngine.Physics.BodyId.md)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The description needs an undeclared capability.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The description is invalid ([Validate](MicroEngine.Physics.BodyDescription.md#MicroEngine_Physics_BodyDescription_Validate)).

### <a id="MicroEngine_Physics_IPhysicsWorld2D_CreateJoint_MicroEngine_Physics_JointDescription__"></a> CreateJoint\(in JointDescription\)

Creates a joint between two bodies ([Joints](MicroEngine.Physics.PhysicsCapabilities.md)).

```csharp
JointId CreateJoint(in JointDescription joint)
```

#### Parameters

`joint` [JointDescription](MicroEngine.Physics.JointDescription.md)

#### Returns

 [JointId](MicroEngine.Physics.JointId.md)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [Joints](MicroEngine.Physics.PhysicsCapabilities.md).

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The description is invalid ([Validate](MicroEngine.Physics.JointDescription.md#MicroEngine_Physics_JointDescription_Validate)).

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

A body id is stale.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_DescriptionOf_MicroEngine_Physics_JointId_"></a> DescriptionOf\(JointId\)

The description the joint was created with, kept up to date by the setters (anchors and axis are as they were when it was created).

```csharp
JointDescription DescriptionOf(JointId joint)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

#### Returns

 [JointDescription](MicroEngine.Physics.JointDescription.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_DescriptionOf_MicroEngine_Physics_ShapeId_"></a> DescriptionOf\(ShapeId\)

The description <code class="paramref">shape</code> was added with (its geometry is relative to its body's origin).

```csharp
ShapeDescription DescriptionOf(ShapeId shape)
```

#### Parameters

`shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

#### Returns

 [ShapeDescription](MicroEngine.Physics.ShapeDescription.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [ShapeInspection](MicroEngine.Physics.PhysicsCapabilities.md).

### <a id="MicroEngine_Physics_IPhysicsWorld2D_DestroyBody_MicroEngine_Physics_BodyId_"></a> DestroyBody\(BodyId\)

Destroys the body and its shapes; nothing for a stale or none id. Their sensor pairs end at the next step.

```csharp
void DestroyBody(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

### <a id="MicroEngine_Physics_IPhysicsWorld2D_DestroyJoint_MicroEngine_Physics_JointId_"></a> DestroyJoint\(JointId\)

Destroys a joint; nothing for a stale or none id. Destroying a body destroys its joints.

```csharp
void DestroyJoint(JointId joint)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

### <a id="MicroEngine_Physics_IPhysicsWorld2D_DynamicsOf_MicroEngine_Physics_BodyId_"></a> DynamicsOf\(BodyId\)

How the solver treats <code class="paramref">body</code>: damping, gravity scale, fixed rotation, sleep.

```csharp
BodyDynamics DynamicsOf(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [BodyDynamics](MicroEngine.Physics.BodyDynamics.md)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md).

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_InertiaOf_MicroEngine_Physics_BodyId_"></a> InertiaOf\(BodyId\)

A dynamic body's rotational inertia about its centre of mass (mass × world units squared); 0 for other bodies.

```csharp
float InertiaOf(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [MassProperties](MicroEngine.Physics.PhysicsCapabilities.md).

### <a id="MicroEngine_Physics_IPhysicsWorld2D_IsAlive_MicroEngine_Physics_BodyId_"></a> IsAlive\(BodyId\)

Whether <code class="paramref">body</code> exists.

```csharp
bool IsAlive(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_IPhysicsWorld2D_IsAlive_MicroEngine_Physics_JointId_"></a> IsAlive\(JointId\)

Whether <code class="paramref">joint</code> exists.

```csharp
bool IsAlive(JointId joint)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_IPhysicsWorld2D_IsAlive_MicroEngine_Physics_ShapeId_"></a> IsAlive\(ShapeId\)

Whether <code class="paramref">shape</code> exists.

```csharp
bool IsAlive(ShapeId shape)
```

#### Parameters

`shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_IPhysicsWorld2D_IsAwake_MicroEngine_Physics_BodyId_"></a> IsAwake\(BodyId\)

Whether a body is awake (simulated); bodies that rest fall asleep when they may ([AllowSleep](MicroEngine.Physics.BodyDynamics.md#MicroEngine_Physics_BodyDynamics_AllowSleep)).

```csharp
bool IsAwake(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md).

### <a id="MicroEngine_Physics_IPhysicsWorld2D_JointCoordinateOf_MicroEngine_Physics_JointId_"></a> JointCoordinateOf\(JointId\)

Where the joint is along what it frees: a revolute joint's angle in radians since it was created, a prismatic
joint's translation, a distance joint's length; 0 for the other kinds.

```csharp
float JointCoordinateOf(JointId joint)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_KindOf_MicroEngine_Physics_BodyId_"></a> KindOf\(BodyId\)

How <code class="paramref">body</code> moves.

```csharp
BodyKind KindOf(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [BodyKind](MicroEngine.Physics.BodyKind.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_MassOf_MicroEngine_Physics_BodyId_"></a> MassOf\(BodyId\)

A dynamic body's mass, from its shapes' densities; 0 for other bodies.

```csharp
float MassOf(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [MassProperties](MicroEngine.Physics.PhysicsCapabilities.md).

### <a id="MicroEngine_Physics_IPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__"></a> Move\(BodyId, Vector2, in QueryFilter\)

Moves a kinematic body by <code class="paramref">delta</code> with collide-and-slide against the shapes
<code class="paramref">filter</code> accepts (see the rules); a body that pushes bodies
([PushesBodies](MicroEngine.Physics.BodyDescription.md#MicroEngine_Physics_BodyDescription_PushesBodies)) is not blocked and moves the bodies in its way instead.

```csharp
MoveResult Move(BodyId body, Vector2 delta, in QueryFilter filter = default)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`delta` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`filter` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

#### Returns

 [MoveResult](MicroEngine.Physics.MoveResult.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale, the body is not kinematic, or it has no single non-sensor shape the backend can move.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">delta</code> is not finite.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_Overlap_MicroEngine_RectF__MicroEngine_Physics_QueryFilter__System_Span_MicroEngine_Physics_ShapeId__"></a> Overlap\(in RectF, in QueryFilter, Span<ShapeId\>\)

Writes into <code class="paramref">results</code> the shapes that truly overlap <code class="paramref">area</code>, in an order that
depends only on the world's history, and returns how many overlap (which may exceed the span's length: only the
first ones are written).

```csharp
int Overlap(in RectF area, in QueryFilter filter, Span<ShapeId> results)
```

#### Parameters

`area` [RectF](MicroEngine.RectF.md)

`filter` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

`results` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[ShapeId](MicroEngine.Physics.ShapeId.md)\>

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_IPhysicsWorld2D_Overlap_MicroEngine_Physics_QueryShape__System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__System_Span_MicroEngine_Physics_ShapeId__"></a> Overlap\(in QueryShape, Vector2, in QueryFilter, Span<ShapeId\>\)

Writes into <code class="paramref">results</code> the shapes that truly overlap <code class="paramref">shape</code> centred at
<code class="paramref">center</code> (touching is not overlapping), in an order that depends only on the world's history,
and returns how many overlap, like [Overlap](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Overlap_MicroEngine_RectF__MicroEngine_Physics_QueryFilter__System_Span_MicroEngine_Physics_ShapeId__): an explosion's
radius, a sword's reach.

```csharp
int Overlap(in QueryShape shape, Vector2 center, in QueryFilter filter, Span<ShapeId> results)
```

#### Parameters

`shape` [QueryShape](MicroEngine.Physics.QueryShape.md)

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`filter` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

`results` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[ShapeId](MicroEngine.Physics.ShapeId.md)\>

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [ShapeQueries](MicroEngine.Physics.PhysicsCapabilities.md).

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The shape is invalid or the centre is not finite.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_OverlapPoint_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__System_Span_MicroEngine_Physics_ShapeId__"></a> OverlapPoint\(Vector2, in QueryFilter, Span<ShapeId\>\)

Writes into <code class="paramref">results</code> the shapes that hold <code class="paramref">point</code> (inside them, not on their
edge) and returns how many do: what is under the pointer.

```csharp
int OverlapPoint(Vector2 point, in QueryFilter filter, Span<ShapeId> results)
```

#### Parameters

`point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`filter` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

`results` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[ShapeId](MicroEngine.Physics.ShapeId.md)\>

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [ShapeQueries](MicroEngine.Physics.PhysicsCapabilities.md).

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The point is not finite.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_OwnerOf_MicroEngine_Physics_BodyId_"></a> OwnerOf\(BodyId\)

The entity that owns <code class="paramref">body</code>.

```csharp
EntityId OwnerOf(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [EntityId](MicroEngine.Scenes.EntityId.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_PositionOf_MicroEngine_Physics_BodyId_"></a> PositionOf\(BodyId\)

The body's origin.

```csharp
Vector2 PositionOf(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_RayCast_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__MicroEngine_Physics_RayHit__"></a> RayCast\(Vector2, Vector2, in QueryFilter, out RayHit\)

The closest shape the segment from <code class="paramref">from</code> to <code class="paramref">to</code> enters. A shape holding
<code class="paramref">from</code> is not hit; one whose edge <code class="paramref">from</code> is on is hit at fraction 0 when the
segment goes into it, and not when it leaves it or runs along the edge.

```csharp
bool RayCast(Vector2 from, Vector2 to, in QueryFilter filter, out RayHit hit)
```

#### Parameters

`from` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`to` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`filter` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

`hit` [RayHit](MicroEngine.Physics.RayHit.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A point is not finite.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_RayCastAll_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__System_Span_MicroEngine_Physics_RayHit__"></a> RayCastAll\(Vector2, Vector2, in QueryFilter, Span<RayHit\>\)

Writes into <code class="paramref">hits</code> every shape the segment from <code class="paramref">from</code> to <code class="paramref">to</code>
enters, nearest first (shapes at the same distance in creation order), one hit per shape, and returns how many
it enters (which may exceed the span's length: only the nearest ones are written). The rules of
[RayCast](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_RayCast_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__MicroEngine_Physics_RayHit__) apply to each: a shape holding <code class="paramref">from</code> is not hit. For a shot that goes
through several targets, or a line of sight that must skip some of them.

```csharp
int RayCastAll(Vector2 from, Vector2 to, in QueryFilter filter, Span<RayHit> hits)
```

#### Parameters

`from` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`to` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`filter` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

`hits` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[RayHit](MicroEngine.Physics.RayHit.md)\>

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [ShapeQueries](MicroEngine.Physics.PhysicsCapabilities.md).

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A point is not finite.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_ReactionForceOf_MicroEngine_Physics_JointId_"></a> ReactionForceOf\(JointId\)

The force the joint applied in the latest step to hold its bodies together (mass × world units per second squared).

```csharp
Vector2 ReactionForceOf(JointId joint)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_ReactionTorqueOf_MicroEngine_Physics_JointId_"></a> ReactionTorqueOf\(JointId\)

The torque the joint applied in the latest step.

```csharp
float ReactionTorqueOf(JointId joint)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_RemoveShape_MicroEngine_Physics_ShapeId_"></a> RemoveShape\(ShapeId\)

Removes a shape; nothing for a stale or none id. Its sensor pairs end at the next step.

```csharp
void RemoveShape(ShapeId shape)
```

#### Parameters

`shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

### <a id="MicroEngine_Physics_IPhysicsWorld2D_RotationOf_MicroEngine_Physics_BodyId_"></a> RotationOf\(BodyId\)

The body's angle in radians (0 for backends without [Rotation](MicroEngine.Physics.PhysicsCapabilities.md)).

```csharp
float RotationOf(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_SetAngularVelocity_MicroEngine_Physics_BodyId_System_Single_"></a> SetAngularVelocity\(BodyId, float\)

Sets a dynamic body's angular velocity, in radians per second.

```csharp
void SetAngularVelocity(BodyId body, float radiansPerSecond)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`radiansPerSecond` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md) and [Rotation](MicroEngine.Physics.PhysicsCapabilities.md).

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale or the body is not dynamic.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_SetAwake_MicroEngine_Physics_BodyId_System_Boolean_"></a> SetAwake\(BodyId, bool\)

Wakes a body or puts it to sleep (with the bodies it touches).

```csharp
void SetAwake(BodyId body, bool awake)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`awake` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md).

### <a id="MicroEngine_Physics_IPhysicsWorld2D_SetDynamics_MicroEngine_Physics_BodyId_MicroEngine_Physics_BodyDynamics__"></a> SetDynamics\(BodyId, in BodyDynamics\)

Changes how the solver treats <code class="paramref">body</code>.

```csharp
void SetDynamics(BodyId body, in BodyDynamics dynamics)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`dynamics` [BodyDynamics](MicroEngine.Physics.BodyDynamics.md)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md).

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A value is out of range ([Validate](MicroEngine.Physics.BodyDynamics.md#MicroEngine_Physics_BodyDynamics_Validate)).

### <a id="MicroEngine_Physics_IPhysicsWorld2D_SetJointLength_MicroEngine_Physics_JointId_System_Single_"></a> SetJointLength\(JointId, float\)

Sets a distance joint's rest length (a winch).

```csharp
void SetJointLength(JointId joint, float length)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

`length` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale, or the joint is not a distance joint.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_SetJointLimit_MicroEngine_Physics_JointId_System_Boolean_System_Single_System_Single_"></a> SetJointLimit\(JointId, bool, float, float\)

Turns a joint's limit on or off and sets its bounds (revolute, prismatic, wheel, distance).

```csharp
void SetJointLimit(JointId joint, bool enabled, float lower, float upper)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

`enabled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`lower` [float](https://learn.microsoft.com/dotnet/api/system.single)

`upper` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale, or the joint's kind has no limit.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The lower bound is above the upper one.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_SetJointMotor_MicroEngine_Physics_JointId_System_Boolean_System_Single_System_Single_"></a> SetJointMotor\(JointId, bool, float, float\)

Turns a joint's motor on or off and sets its speed and the most force or torque it applies (revolute, prismatic, wheel, distance).

```csharp
void SetJointMotor(JointId joint, bool enabled, float speed, float maxForce)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

`enabled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`speed` [float](https://learn.microsoft.com/dotnet/api/system.single)

`maxForce` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale, or the joint's kind has no motor.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_SetJointSpring_MicroEngine_Physics_JointId_System_Boolean_System_Single_System_Single_"></a> SetJointSpring\(JointId, bool, float, float\)

Turns a joint's spring on or off and sets its frequency and damping ratio.

```csharp
void SetJointSpring(JointId joint, bool enabled, float hertz, float dampingRatio)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

`enabled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`hertz` [float](https://learn.microsoft.com/dotnet/api/system.single)

`dampingRatio` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_SetJointTarget_MicroEngine_Physics_JointId_System_Numerics_Vector2_"></a> SetJointTarget\(JointId, Vector2\)

Moves a target joint's world point.

```csharp
void SetJointTarget(JointId joint, Vector2 target)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

`target` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale, or the joint is not a target joint.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_SetPosition_MicroEngine_Physics_BodyId_System_Numerics_Vector2_"></a> SetPosition\(BodyId, Vector2\)

Teleports the body: no collision, no pushing; sensor events follow at the next step.

```csharp
void SetPosition(BodyId body, Vector2 position)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The position is not finite.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_SetRotation_MicroEngine_Physics_BodyId_System_Single_"></a> SetRotation\(BodyId, float\)

Turns the body to <code class="paramref">radians</code> where it is: no collision, like [SetPosition](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_SetPosition_MicroEngine_Physics_BodyId_System_Numerics_Vector2_).

```csharp
void SetRotation(BodyId body, float radians)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`radians` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [Rotation](MicroEngine.Physics.PhysicsCapabilities.md).

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The angle is not finite.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_SetVelocity_MicroEngine_Physics_BodyId_System_Numerics_Vector2_"></a> SetVelocity\(BodyId, Vector2\)

Sets a dynamic body's velocity.

```csharp
void SetVelocity(BodyId body, Vector2 velocity)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`velocity` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md).

### <a id="MicroEngine_Physics_IPhysicsWorld2D_ShapeCast_MicroEngine_Physics_QueryShape__System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__MicroEngine_Physics_RayHit__"></a> ShapeCast\(in QueryShape, Vector2, Vector2, in QueryFilter, out RayHit\)

Moves <code class="paramref">shape</code> with its centre going from <code class="paramref">from</code> to <code class="paramref">to</code> and
returns the first shape it would touch: [Fraction](MicroEngine.Physics.RayHit.md#MicroEngine_Physics_RayHit_Fraction) is how far along the way its centre is
then, [Point](MicroEngine.Physics.RayHit.md#MicroEngine_Physics_RayHit_Point) where the two touch and [Normal](MicroEngine.Physics.RayHit.md#MicroEngine_Physics_RayHit_Normal) the surface normal there. A
shape it overlaps where it starts is not hit, and one it touches there is hit at fraction 0 when it moves into
it and not when it leaves it or slides along it: like [Move](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__), which this predicts ("is there room
to stand up", "what would this dash hit", "can I move there"). One-way platforms are hit only from above, coming
down. The shape stops within [Skin](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Skin) of the exact place.

```csharp
bool ShapeCast(in QueryShape shape, Vector2 from, Vector2 to, in QueryFilter filter, out RayHit hit)
```

#### Parameters

`shape` [QueryShape](MicroEngine.Physics.QueryShape.md)

`from` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`to` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`filter` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

`hit` [RayHit](MicroEngine.Physics.RayHit.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [ShapeQueries](MicroEngine.Physics.PhysicsCapabilities.md).

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The shape is invalid ([Validate](MicroEngine.Physics.QueryShape.md#MicroEngine_Physics_QueryShape_Validate)) or a point is not finite.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_ShapesOf_MicroEngine_Physics_BodyId_System_Span_MicroEngine_Physics_ShapeId__"></a> ShapesOf\(BodyId, Span<ShapeId\>\)

Writes the live shapes of <code class="paramref">body</code>, in the order they were added, into <code class="paramref">results</code>
and returns how many it has (which may exceed the span's length: only the first ones are written).

```csharp
int ShapesOf(BodyId body, Span<ShapeId> results)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`results` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[ShapeId](MicroEngine.Physics.ShapeId.md)\>

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The body is stale.

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [ShapeInspection](MicroEngine.Physics.PhysicsCapabilities.md).

### <a id="MicroEngine_Physics_IPhysicsWorld2D_Step_System_Single_"></a> Step\(float\)

Advances the world by <code class="paramref">dt</code> seconds: dynamic bodies move, and the events of this step replace the
previous ones. No game code runs during it.

```csharp
void Step(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">dt</code> is negative or not finite.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_TilesOf_MicroEngine_Physics_ShapeId_"></a> TilesOf\(ShapeId\)

The tiles <code class="paramref">shape</code> stands for when it was added with [AddTiles](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_AddTiles_MicroEngine_Physics_BodyId_MicroEngine_World_ICollisionGrid_System_Int32_System_Int32_System_UInt64_System_UInt64_); null for any other shape.

```csharp
ICollisionGrid? TilesOf(ShapeId shape)
```

#### Parameters

`shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

#### Returns

 [ICollisionGrid](MicroEngine.World.ICollisionGrid.md)?

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_IPhysicsWorld2D_VelocityOf_MicroEngine_Physics_BodyId_"></a> VelocityOf\(BodyId\)

A dynamic body's velocity.

```csharp
Vector2 VelocityOf(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md).

</div>

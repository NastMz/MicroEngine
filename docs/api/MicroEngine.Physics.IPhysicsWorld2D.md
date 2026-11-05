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
the filter includes them.</li><li><b>Sensors.</b> A sensor reports the non-sensor shapes of kinematic and dynamic bodies that begin or stop
overlapping it, when the layers of both accept each other and the visitor has
[SensorEvents](MicroEngine.Physics.ShapeDescription.md#MicroEngine_Physics_ShapeDescription_SensorEvents) (the default). Sensors do not see other sensors or static shapes.</li><li><b>Events are buffered per step.</b> [Step](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Step_System_Single_) computes [SensorEvents](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_SensorEvents) and
[ContactEvents](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_ContactEvents) from the poses at its end (moves made before it count in it); they stay readable until
the next step, in an order that depends only on the world's history. Contact events only come from pairs with a
dynamic body. Destroying a shape ends its pairs with an exit or end event at the next step.</li><li><b>No managed callbacks during [Step](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Step_System_Single_).</b> Per-shape rules (one-way platforms) are shape flags the
backend implements itself; games read events after the step.</li><li><b>Stepping.</b> [PhysicsModule](MicroEngine.Physics.PhysicsModule.md) steps registered worlds in the kernel's
<code>FramePhase.Physics</code>, inside the fixed step, and writes body poses to their owners' transforms; the scene world
interpolates them for drawing.</li><li><b>Determinism.</b> The same backend build gives identical results on every target for the same calls (native
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

### <a id="MicroEngine_Physics_IPhysicsWorld2D_DestroyBody_MicroEngine_Physics_BodyId_"></a> DestroyBody\(BodyId\)

Destroys the body and its shapes; nothing for a stale or none id. Their sensor pairs end at the next step.

```csharp
void DestroyBody(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

### <a id="MicroEngine_Physics_IPhysicsWorld2D_IsAlive_MicroEngine_Physics_BodyId_"></a> IsAlive\(BodyId\)

Whether <code class="paramref">body</code> exists.

```csharp
bool IsAlive(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

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

The closest shape the segment from <code class="paramref">from</code> to <code class="paramref">to</code> enters; a shape holding <code class="paramref">from</code> is not hit.

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

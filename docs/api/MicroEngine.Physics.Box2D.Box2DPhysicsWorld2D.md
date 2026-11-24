# Class Box2DPhysicsWorld2D

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics.Box2D](MicroEngine.Physics.Box2D.md)  
Assembly: MicroEngine.Physics.Box2D.dll  
Guide: [Choosing a backend](../manual/physics.md#choosing-a-backend)  

The Box2D v3 backend of [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md): native Box2D (the release its native layer pins) behind the
engine's flat C layer, so no Box2D type reaches the game. It passes the common contract and declares
[Declared](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md#MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Declared): dynamic bodies, rotation, circles, capsules, convex polygons, continuous collision,
friction and restitution, mass properties, one-way platforms, joints, shape queries, contact detail, filter
predicates and tile geometry with slopes (a level is the outline of its tiles, as Box2D chains). It does not declare
[MovingSolids](MicroEngine.Physics.PhysicsCapabilities.md). [NativeIds](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md#MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_NativeIds) is the way out to Box2D's own API.

```csharp
public sealed class Box2DPhysicsWorld2D : IPhysicsWorld2D, IStateHashed, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Box2DPhysicsWorld2D](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md)

#### Implements

[IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md), 
[IStateHashed](MicroEngine.Kernel.IStateHashed.md), 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Remarks

<p>Units: Box2D works in metres with y up; [Box2DWorldOptions](MicroEngine.Physics.Box2D.Box2DWorldOptions.md) maps the game's units onto them
([UnitsPerMeter](MicroEngine.Physics.Box2D.Box2DWorldOptions.md#MicroEngine_Physics_Box2D_Box2DWorldOptions_UnitsPerMeter), [YDown](MicroEngine.Physics.Box2D.Box2DWorldOptions.md#MicroEngine_Physics_Box2D_Box2DWorldOptions_YDown)), so a pixel-space game passes
pixels and gets pixels back.</p>
<p>[Move](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md#MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__) moves a box (or a polygon or circle) along x, then y, each as a shape cast that stops a
little short of the first surface, and a capsule with Box2D's own mover. Resting is not exact: a body may rest up to
[Skin](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md#MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Skin) away from a surface (about 1 cm, two Box2D slops), and a capsule may sink into it by up to the
same amount. Contacts with static and kinematic bodies come from [MoveResult](MicroEngine.Physics.MoveResult.md) only (Box2D creates no
contacts between them); [ContactEvents](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md#MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_ContactEvents) carries the pairs with a dynamic body.</p>
<p>Angles go through Box2D's deterministic sine, cosine and arctangent approximations: an angle reads back
within about 0.002 radians of the one set.</p>
<p>Deterministic per native build: the same calls give the same bits on every target the build ships for
(floating-point contraction off, one thread); nothing is promised against another backend or another Box2D
version.</p>
<p>The world owns native memory: dispose it when done. Box2D keeps at most 128 worlds alive in a process, in
one table shared by all threads: create and dispose worlds from one thread at a time. A world that is never disposed
is released on a later creation or disposal after the garbage collector finds it (never on the finalizer thread).</p>

## Constructors

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D__ctor_MicroEngine_Physics_Box2D_Box2DWorldOptions_"></a> Box2DPhysicsWorld2D\(Box2DWorldOptions?\)

Creates an empty world.

```csharp
public Box2DPhysicsWorld2D(Box2DWorldOptions? options = null)
```

#### Parameters

`options` [Box2DWorldOptions](MicroEngine.Physics.Box2D.Box2DWorldOptions.md)?

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

An option is out of range ([Validate](MicroEngine.Physics.Box2D.Box2DWorldOptions.md#MicroEngine_Physics_Box2D_Box2DWorldOptions_Validate)).

 [DllNotFoundException](https://learn.microsoft.com/dotnet/api/system.dllnotfoundexception)

The native library of this runtime is missing ([IsSupported](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md#MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_IsSupported)).

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The native library is of another layer version than this assembly, or Box2D already holds 128 worlds.

## Fields

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Declared"></a> Declared

The capabilities this backend declares.

```csharp
public const PhysicsCapabilities Declared = TileGeometry | OneWayPlatforms | DynamicBodies | Rotation | Joints | Capsules | Circles | Polygons | ContinuousCollision | FrictionRestitution | MassProperties | ShapeInspection | ShapeQueries | ContactDetail | FilterPredicates | TileSlopes
```

#### Field Value

 [PhysicsCapabilities](MicroEngine.Physics.PhysicsCapabilities.md)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_MaxPolygonVertices"></a> MaxPolygonVertices

The most vertices a polygon may have (Box2D's limit).

```csharp
public const int MaxPolygonVertices = 8
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Properties

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Backend"></a> Backend

The backend's name, for messages ("tiles", "box2d").

```csharp
public string Backend { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Bodies"></a> Bodies

The live bodies, in creation order (a destroyed body leaves its place).

```csharp
public ReadOnlySpan<BodyId> Bodies { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[BodyId](MicroEngine.Physics.BodyId.md)\>

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Box2DVersion"></a> Box2DVersion

The Box2D release the native library was built from, for example "3.2.0".

```csharp
public static string Box2DVersion { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_BrokenJoints"></a> BrokenJoints

The joints that broke in the latest step ([BreakForce](MicroEngine.Physics.JointDescription.md#MicroEngine_Physics_JointDescription_BreakForce)); they no longer exist.

```csharp
public ReadOnlySpan<JointId> BrokenJoints { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[JointId](MicroEngine.Physics.JointId.md)\>

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Capabilities"></a> Capabilities

What the backend offers beyond the common contract.

```csharp
public PhysicsCapabilities Capabilities { get; }
```

#### Property Value

 [PhysicsCapabilities](MicroEngine.Physics.PhysicsCapabilities.md)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_ContactEvents"></a> ContactEvents

The contacts that began or ended in the latest step: ends first, then begins, in the order Box2D reports them.

```csharp
public ReadOnlySpan<ContactEvent> ContactEvents { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[ContactEvent](MicroEngine.Physics.ContactEvent.md)\>

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_ContactImpacts"></a> ContactImpacts

The impacts of the latest step, in the order Box2D reports them; the speed that makes a contact an impact is [ImpactSpeed](MicroEngine.Physics.Box2D.Box2DWorldOptions.md#MicroEngine_Physics_Box2D_Box2DWorldOptions_ImpactSpeed).

```csharp
public ReadOnlySpan<ContactImpact> ContactImpacts { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[ContactImpact](MicroEngine.Physics.ContactImpact.md)\>

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_IsSupported"></a> IsSupported

Whether this process can load the native library (the runtime's <code>runtimes/&lt;rid&gt;/native</code> asset, or
the browser's static library) and it is of this assembly's layer version.

```csharp
public static bool IsSupported { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Joints"></a> Joints

The live joints, in creation order.

```csharp
public ReadOnlySpan<JointId> Joints { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[JointId](MicroEngine.Physics.JointId.md)\>

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_NativeIds"></a> NativeIds

The Box2D ids behind this world, for calling Box2D's own API for what the contract does not cover
([Box2DNative](MicroEngine.Physics.Box2D.Box2DNative.md) says what that costs). Only for a world made with
[NativeAccess](MicroEngine.Physics.Box2D.Box2DWorldOptions.md#MicroEngine_Physics_Box2D_Box2DWorldOptions_NativeAccess).

```csharp
public Box2DNative NativeIds { get; }
```

#### Property Value

 [Box2DNative](MicroEngine.Physics.Box2D.Box2DNative.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The world was not made with [NativeAccess](MicroEngine.Physics.Box2D.Box2DWorldOptions.md#MicroEngine_Physics_Box2D_Box2DWorldOptions_NativeAccess).

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Options"></a> Options

The options the world was made with.

```csharp
public Box2DWorldOptions Options { get; }
```

#### Property Value

 [Box2DWorldOptions](MicroEngine.Physics.Box2D.Box2DWorldOptions.md)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_SensorEvents"></a> SensorEvents

The sensor entries and exits of the latest step: exits first, then entries, in the order Box2D reports them.

```csharp
public ReadOnlySpan<SensorEvent> SensorEvents { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[SensorEvent](MicroEngine.Physics.SensorEvent.md)\>

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Skin"></a> Skin

The largest gap [Move](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md#MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__) leaves to a surface (about 1.5 cm, in world units), and the deepest a capsule may sink into one.

```csharp
public float Skin { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_StepCount"></a> StepCount

How many times [Step](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Step_System_Single_) ran.

```csharp
public long StepCount { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Up"></a> Up

(0, 1), or (0, -1) with [YDown](MicroEngine.Physics.Box2D.Box2DWorldOptions.md#MicroEngine_Physics_Box2D_Box2DWorldOptions_YDown).

```csharp
public Vector2 Up { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Methods

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_AddShape_MicroEngine_Physics_BodyId_MicroEngine_Physics_ShapeDescription__"></a> AddShape\(BodyId, in ShapeDescription\)

```csharp
public ShapeId AddShape(BodyId body, in ShapeDescription shape)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`shape` [ShapeDescription](MicroEngine.Physics.ShapeDescription.md)

#### Returns

 [ShapeId](MicroEngine.Physics.ShapeId.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A polygon has more than [MaxPolygonVertices](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md#MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_MaxPolygonVertices) vertices.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

A polygon's vertices do not make a convex polygon of some area.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_AddTiles_MicroEngine_Physics_BodyId_MicroEngine_World_ICollisionGrid_System_Int32_System_Int32_System_UInt64_System_UInt64_"></a> AddTiles\(BodyId, ICollisionGrid, int, int, ulong, ulong\)

Attaches the tiles of <code class="paramref">grid</code> to a static body as one shape
([TileGeometry](MicroEngine.Physics.PhysicsCapabilities.md)): cell (x, y) is a square of [TileSize](MicroEngine.World.ICollisionGrid.md#MicroEngine_World_ICollisionGrid_TileSize)
units, x tiles sideways and y tiles downward (against [Up](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Up)) from the body's origin, so row 0 is the
top row whichever way the world's y grows. [Solid](MicroEngine.World.TileFlags.md) cells block from every side and
[OneWay](MicroEngine.World.TileFlags.md) cells are one-way platforms (where the backend declares them); other cells are
not geometry.

```csharp
public ShapeId AddTiles(BodyId body, ICollisionGrid grid, int columns, int rows, ulong category = 1, ulong mask = 18446744073709551615)
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

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_AnchorAOf_MicroEngine_Physics_JointId_"></a> AnchorAOf\(JointId\)

The joint's anchor on body A, a world point, as the body is now.

```csharp
public Vector2 AnchorAOf(JointId joint)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_AnchorBOf_MicroEngine_Physics_JointId_"></a> AnchorBOf\(JointId\)

The joint's anchor on body B, a world point, as the body is now.

```csharp
public Vector2 AnchorBOf(JointId joint)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_AngularVelocityOf_MicroEngine_Physics_BodyId_"></a> AngularVelocityOf\(BodyId\)

A body's angular velocity in radians per second; positive turns the way [RotationOf](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_RotationOf_MicroEngine_Physics_BodyId_) grows.

```csharp
public float AngularVelocityOf(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md) and [Rotation](MicroEngine.Physics.PhysicsCapabilities.md).

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_ApplyAngularImpulse_MicroEngine_Physics_BodyId_System_Single_System_Boolean_"></a> ApplyAngularImpulse\(BodyId, float, bool\)

Applies an angular impulse (inertia × radians per second) to a dynamic body: its angular velocity changes at once.

```csharp
public void ApplyAngularImpulse(BodyId body, float impulse, bool wake = true)
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

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_ApplyForce_MicroEngine_Physics_BodyId_System_Numerics_Vector2_System_Boolean_"></a> ApplyForce\(BodyId, Vector2, bool\)

Applies <code class="paramref">force</code> (mass × world units per second squared) through a dynamic body's centre of
mass for the next step only: apply it every step for as long as it acts.

```csharp
public void ApplyForce(BodyId body, Vector2 force, bool wake = true)
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

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_ApplyForce_MicroEngine_Physics_BodyId_System_Numerics_Vector2_System_Numerics_Vector2_System_Boolean_"></a> ApplyForce\(BodyId, Vector2, Vector2, bool\)

Applies <code class="paramref">force</code> at the world point <code class="paramref">point</code> for the next step only: off the centre of mass it also turns the body.

```csharp
public void ApplyForce(BodyId body, Vector2 force, Vector2 point, bool wake = true)
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

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_ApplyImpulse_MicroEngine_Physics_BodyId_System_Numerics_Vector2_System_Boolean_"></a> ApplyImpulse\(BodyId, Vector2, bool\)

Applies <code class="paramref">impulse</code> (mass × world units per second) through a dynamic body's centre of mass: its
velocity changes at once by the impulse divided by its mass (a jump, a hit, an explosion).

```csharp
public void ApplyImpulse(BodyId body, Vector2 impulse, bool wake = true)
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

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_ApplyImpulse_MicroEngine_Physics_BodyId_System_Numerics_Vector2_System_Numerics_Vector2_System_Boolean_"></a> ApplyImpulse\(BodyId, Vector2, Vector2, bool\)

Applies <code class="paramref">impulse</code> at the world point <code class="paramref">point</code>: off the centre of mass it also spins the body.

```csharp
public void ApplyImpulse(BodyId body, Vector2 impulse, Vector2 point, bool wake = true)
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

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_ApplyTorque_MicroEngine_Physics_BodyId_System_Single_System_Boolean_"></a> ApplyTorque\(BodyId, float, bool\)

Applies a torque (inertia × radians per second squared) to a dynamic body for the next step only.

```csharp
public void ApplyTorque(BodyId body, float torque, bool wake = true)
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

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_BodyOf_MicroEngine_Physics_ShapeId_"></a> BodyOf\(ShapeId\)

The body <code class="paramref">shape</code> is attached to.

```csharp
public BodyId BodyOf(ShapeId shape)
```

#### Parameters

`shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

#### Returns

 [BodyId](MicroEngine.Physics.BodyId.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_BoundsOf_MicroEngine_Physics_ShapeId_"></a> BoundsOf\(ShapeId\)

The world bounds of <code class="paramref">shape</code> as it is now (the area of the level for tile geometry).

```csharp
public RectF BoundsOf(ShapeId shape)
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

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_CenterOfMassOf_MicroEngine_Physics_BodyId_"></a> CenterOfMassOf\(BodyId\)

A body's centre of mass, a world point (its origin for bodies without mass).

```csharp
public Vector2 CenterOfMassOf(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [MassProperties](MicroEngine.Physics.PhysicsCapabilities.md).

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_ClosestPoint_System_Numerics_Vector2_System_Single_MicroEngine_Physics_QueryFilter__MicroEngine_Physics_ClosestPointHit__"></a> ClosestPoint\(Vector2, float, in QueryFilter, out ClosestPointHit\)

```csharp
public bool ClosestPoint(Vector2 point, float maxDistance, in QueryFilter filter, out ClosestPointHit hit)
```

#### Parameters

`point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`maxDistance` [float](https://learn.microsoft.com/dotnet/api/system.single)

`filter` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

`hit` [ClosestPointHit](MicroEngine.Physics.ClosestPointHit.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_ContactsOf_MicroEngine_Physics_BodyId_System_Span_MicroEngine_Physics_ContactPoint__"></a> ContactsOf\(BodyId, Span<ContactPoint\>\)

Writes into <code class="paramref">results</code> the points at which <code class="paramref">body</code> touches other shapes now
(up to two per pair of shapes), as the latest step left them, and returns how many there are (which may exceed
the span's length). Contacts exist between a dynamic body and anything else: how hard a crate presses on a
plate, whether a dynamic body stands on something.

```csharp
public int ContactsOf(BodyId body, Span<ContactPoint> results)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`results` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[ContactPoint](MicroEngine.Physics.ContactPoint.md)\>

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Remarks

Impulses are in kilograms × world units per second. Box2D keeps contacts between a dynamic body and anything else only.

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [ContactDetail](MicroEngine.Physics.PhysicsCapabilities.md).

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_CreateBody_MicroEngine_Scenes_EntityId_MicroEngine_Physics_BodyDescription__"></a> CreateBody\(EntityId, in BodyDescription\)

```csharp
public BodyId CreateBody(EntityId owner, in BodyDescription body)
```

#### Parameters

`owner` [EntityId](MicroEngine.Scenes.EntityId.md)

`body` [BodyDescription](MicroEngine.Physics.BodyDescription.md)

#### Returns

 [BodyId](MicroEngine.Physics.BodyId.md)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_CreateJoint_MicroEngine_Physics_JointDescription__"></a> CreateJoint\(in JointDescription\)

```csharp
public JointId CreateJoint(in JointDescription joint)
```

#### Parameters

`joint` [JointDescription](MicroEngine.Physics.JointDescription.md)

#### Returns

 [JointId](MicroEngine.Physics.JointId.md)

#### Remarks

Lengths, speeds, forces and torques are in world units ([UnitsPerMeter](MicroEngine.Physics.Box2D.Box2DWorldOptions.md#MicroEngine_Physics_Box2D_Box2DWorldOptions_UnitsPerMeter)); a target
joint is Box2D's motor joint with a linear spring.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_DescriptionOf_MicroEngine_Physics_JointId_"></a> DescriptionOf\(JointId\)

The description the joint was created with, kept up to date by the setters (anchors and axis are as they were when it was created).

```csharp
public JointDescription DescriptionOf(JointId joint)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

#### Returns

 [JointDescription](MicroEngine.Physics.JointDescription.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_DescriptionOf_MicroEngine_Physics_ShapeId_"></a> DescriptionOf\(ShapeId\)

The description <code class="paramref">shape</code> was added with (its geometry is relative to its body's origin).

```csharp
public ShapeDescription DescriptionOf(ShapeId shape)
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

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_DestroyBody_MicroEngine_Physics_BodyId_"></a> DestroyBody\(BodyId\)

Destroys the body and its shapes; nothing for a stale or none id. Their sensor pairs end at the next step.

```csharp
public void DestroyBody(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_DestroyJoint_MicroEngine_Physics_JointId_"></a> DestroyJoint\(JointId\)

Destroys a joint; nothing for a stale or none id. Destroying a body destroys its joints.

```csharp
public void DestroyJoint(JointId joint)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Dispose"></a> Dispose\(\)

Destroys the native world. Every later call except [Dispose](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md#MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Dispose) throws `ObjectDisposedException`.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_DynamicsOf_MicroEngine_Physics_BodyId_"></a> DynamicsOf\(BodyId\)

How the solver treats <code class="paramref">body</code>: damping, gravity scale, fixed rotation, sleep.

```csharp
public BodyDynamics DynamicsOf(BodyId body)
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

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Finalize"></a> \~Box2DPhysicsWorld2D\(\)

Hands an undisposed native world to the next creation or disposal (Box2D is not called from this thread).

```csharp
protected ~Box2DPhysicsWorld2D()
```

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the world's simulation state: the step count and every live body (id, kind, position, angle, velocity, shapes, and its dynamics once they were set).

```csharp
public void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_InertiaOf_MicroEngine_Physics_BodyId_"></a> InertiaOf\(BodyId\)

A dynamic body's rotational inertia about its centre of mass, in kilograms × world units squared; 0 for other bodies.

```csharp
public float InertiaOf(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_IsAlive_MicroEngine_Physics_BodyId_"></a> IsAlive\(BodyId\)

Whether <code class="paramref">body</code> exists.

```csharp
public bool IsAlive(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_IsAlive_MicroEngine_Physics_JointId_"></a> IsAlive\(JointId\)

Whether <code class="paramref">joint</code> exists.

```csharp
public bool IsAlive(JointId joint)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_IsAlive_MicroEngine_Physics_ShapeId_"></a> IsAlive\(ShapeId\)

Whether <code class="paramref">shape</code> exists.

```csharp
public bool IsAlive(ShapeId shape)
```

#### Parameters

`shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_IsAwake_MicroEngine_Physics_BodyId_"></a> IsAwake\(BodyId\)

Whether a body is awake (simulated); bodies that rest fall asleep when they may ([AllowSleep](MicroEngine.Physics.BodyDynamics.md#MicroEngine_Physics_BodyDynamics_AllowSleep)).

```csharp
public bool IsAwake(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md).

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_JointCoordinateOf_MicroEngine_Physics_JointId_"></a> JointCoordinateOf\(JointId\)

Where the joint is along what it frees: a revolute joint's angle in radians since it was created, a prismatic
joint's translation, a distance joint's length; 0 for the other kinds.

```csharp
public float JointCoordinateOf(JointId joint)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_KindOf_MicroEngine_Physics_BodyId_"></a> KindOf\(BodyId\)

How <code class="paramref">body</code> moves.

```csharp
public BodyKind KindOf(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [BodyKind](MicroEngine.Physics.BodyKind.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_MassOf_MicroEngine_Physics_BodyId_"></a> MassOf\(BodyId\)

A dynamic body's mass in kilograms, from its shapes' densities (kilograms per square metre); 0 for other bodies.

```csharp
public float MassOf(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__"></a> Move\(BodyId, Vector2, in QueryFilter\)

```csharp
public MoveResult Move(BodyId body, Vector2 delta, in QueryFilter filter = default)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`delta` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`filter` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

#### Returns

 [MoveResult](MicroEngine.Physics.MoveResult.md)

#### Remarks

Boxes, polygons and circles move along x, then y, each as one shape cast that stops within [Skin](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md#MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Skin)
of the first surface (contacts get axis normals); a capsule moves with Box2D's mover (contacts get the surfaces'
normals). The body is teleported to the result: dynamic bodies in the way block it like any other shape.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Overlap_MicroEngine_RectF__MicroEngine_Physics_QueryFilter__System_Span_MicroEngine_Physics_ShapeId__"></a> Overlap\(in RectF, in QueryFilter, Span<ShapeId\>\)

```csharp
public int Overlap(in RectF area, in QueryFilter filter, Span<ShapeId> results)
```

#### Parameters

`area` [RectF](MicroEngine.RectF.md)

`filter` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

`results` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[ShapeId](MicroEngine.Physics.ShapeId.md)\>

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Remarks

Shapes are returned in creation order.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Overlap_MicroEngine_Physics_QueryShape__System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__System_Span_MicroEngine_Physics_ShapeId__"></a> Overlap\(in QueryShape, Vector2, in QueryFilter, Span<ShapeId\>\)

```csharp
public int Overlap(in QueryShape shape, Vector2 center, in QueryFilter filter, Span<ShapeId> results)
```

#### Parameters

`shape` [QueryShape](MicroEngine.Physics.QueryShape.md)

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`filter` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

`results` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[ShapeId](MicroEngine.Physics.ShapeId.md)\>

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Remarks

Shapes are returned in creation order.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_OverlapPoint_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__System_Span_MicroEngine_Physics_ShapeId__"></a> OverlapPoint\(Vector2, in QueryFilter, Span<ShapeId\>\)

```csharp
public int OverlapPoint(Vector2 point, in QueryFilter filter, Span<ShapeId> results)
```

#### Parameters

`point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`filter` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

`results` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[ShapeId](MicroEngine.Physics.ShapeId.md)\>

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Remarks

Shapes are returned in creation order.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_OwnerOf_MicroEngine_Physics_BodyId_"></a> OwnerOf\(BodyId\)

The entity that owns <code class="paramref">body</code>.

```csharp
public EntityId OwnerOf(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [EntityId](MicroEngine.Scenes.EntityId.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_PositionOf_MicroEngine_Physics_BodyId_"></a> PositionOf\(BodyId\)

The body's origin.

```csharp
public Vector2 PositionOf(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_RayCast_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__MicroEngine_Physics_RayHit__"></a> RayCast\(Vector2, Vector2, in QueryFilter, out RayHit\)

```csharp
public bool RayCast(Vector2 from, Vector2 to, in QueryFilter filter, out RayHit hit)
```

#### Parameters

`from` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`to` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`filter` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

`hit` [RayHit](MicroEngine.Physics.RayHit.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_RayCastAll_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__System_Span_MicroEngine_Physics_RayHit__"></a> RayCastAll\(Vector2, Vector2, in QueryFilter, Span<RayHit\>\)

```csharp
public int RayCastAll(Vector2 from, Vector2 to, in QueryFilter filter, Span<RayHit> hits)
```

#### Parameters

`from` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`to` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`filter` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

`hits` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[RayHit](MicroEngine.Physics.RayHit.md)\>

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_ReactionForceOf_MicroEngine_Physics_JointId_"></a> ReactionForceOf\(JointId\)

The force the joint applied in the latest step to hold its bodies together (mass × world units per second squared).

```csharp
public Vector2 ReactionForceOf(JointId joint)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_ReactionTorqueOf_MicroEngine_Physics_JointId_"></a> ReactionTorqueOf\(JointId\)

The torque the joint applied in the latest step.

```csharp
public float ReactionTorqueOf(JointId joint)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_RemoveShape_MicroEngine_Physics_ShapeId_"></a> RemoveShape\(ShapeId\)

Removes a shape; nothing for a stale or none id. Its sensor pairs end at the next step.

```csharp
public void RemoveShape(ShapeId shape)
```

#### Parameters

`shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_RotationOf_MicroEngine_Physics_BodyId_"></a> RotationOf\(BodyId\)

The body's angle in radians (0 for backends without [Rotation](MicroEngine.Physics.PhysicsCapabilities.md)).

```csharp
public float RotationOf(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_SetAngularVelocity_MicroEngine_Physics_BodyId_System_Single_"></a> SetAngularVelocity\(BodyId, float\)

Sets a dynamic body's angular velocity, in radians per second.

```csharp
public void SetAngularVelocity(BodyId body, float radiansPerSecond)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`radiansPerSecond` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md) and [Rotation](MicroEngine.Physics.PhysicsCapabilities.md).

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale or the body is not dynamic.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_SetAwake_MicroEngine_Physics_BodyId_System_Boolean_"></a> SetAwake\(BodyId, bool\)

Wakes a body or puts it to sleep (with the bodies it touches).

```csharp
public void SetAwake(BodyId body, bool awake)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`awake` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md).

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_SetDynamics_MicroEngine_Physics_BodyId_MicroEngine_Physics_BodyDynamics__"></a> SetDynamics\(BodyId, in BodyDynamics\)

```csharp
public void SetDynamics(BodyId body, in BodyDynamics dynamics)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`dynamics` [BodyDynamics](MicroEngine.Physics.BodyDynamics.md)

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_SetJointLength_MicroEngine_Physics_JointId_System_Single_"></a> SetJointLength\(JointId, float\)

Sets a distance joint's rest length (a winch).

```csharp
public void SetJointLength(JointId joint, float length)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

`length` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale, or the joint is not a distance joint.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_SetJointLimit_MicroEngine_Physics_JointId_System_Boolean_System_Single_System_Single_"></a> SetJointLimit\(JointId, bool, float, float\)

Turns a joint's limit on or off and sets its bounds (revolute, prismatic, wheel, distance).

```csharp
public void SetJointLimit(JointId joint, bool enabled, float lower, float upper)
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

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_SetJointMotor_MicroEngine_Physics_JointId_System_Boolean_System_Single_System_Single_"></a> SetJointMotor\(JointId, bool, float, float\)

Turns a joint's motor on or off and sets its speed and the most force or torque it applies (revolute, prismatic, wheel, distance).

```csharp
public void SetJointMotor(JointId joint, bool enabled, float speed, float maxForce)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

`enabled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`speed` [float](https://learn.microsoft.com/dotnet/api/system.single)

`maxForce` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale, or the joint's kind has no motor.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_SetJointSpring_MicroEngine_Physics_JointId_System_Boolean_System_Single_System_Single_"></a> SetJointSpring\(JointId, bool, float, float\)

Turns a joint's spring on or off and sets its frequency and damping ratio.

```csharp
public void SetJointSpring(JointId joint, bool enabled, float hertz, float dampingRatio)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

`enabled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`hertz` [float](https://learn.microsoft.com/dotnet/api/system.single)

`dampingRatio` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_SetJointTarget_MicroEngine_Physics_JointId_System_Numerics_Vector2_"></a> SetJointTarget\(JointId, Vector2\)

Moves a target joint's world point.

```csharp
public void SetJointTarget(JointId joint, Vector2 target)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

`target` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale, or the joint is not a target joint.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_SetPosition_MicroEngine_Physics_BodyId_System_Numerics_Vector2_"></a> SetPosition\(BodyId, Vector2\)

Teleports the body: no collision, no pushing; sensor events follow at the next step.

```csharp
public void SetPosition(BodyId body, Vector2 position)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The position is not finite.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_SetRotation_MicroEngine_Physics_BodyId_System_Single_"></a> SetRotation\(BodyId, float\)

Turns the body to <code class="paramref">radians</code> where it is: no collision, like [SetPosition](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_SetPosition_MicroEngine_Physics_BodyId_System_Numerics_Vector2_).

```csharp
public void SetRotation(BodyId body, float radians)
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

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_SetVelocity_MicroEngine_Physics_BodyId_System_Numerics_Vector2_"></a> SetVelocity\(BodyId, Vector2\)

Sets a dynamic body's linear velocity, in world units per second.

```csharp
public void SetVelocity(BodyId body, Vector2 velocity)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`velocity` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale or the body is not dynamic.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The velocity is not finite.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_ShapeCast_MicroEngine_Physics_QueryShape__System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__MicroEngine_Physics_RayHit__"></a> ShapeCast\(in QueryShape, Vector2, Vector2, in QueryFilter, out RayHit\)

```csharp
public bool ShapeCast(in QueryShape shape, Vector2 from, Vector2 to, in QueryFilter filter, out RayHit hit)
```

#### Parameters

`shape` [QueryShape](MicroEngine.Physics.QueryShape.md)

`from` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`to` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`filter` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

`hit` [RayHit](MicroEngine.Physics.RayHit.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

The shape stops within a millimetre of where it touches. Where the cast starts, a shape it overlaps by less than
a Box2D slop (half a centimetre, how far resting bodies sink) counts as touched: it is hit at once when the
cast moves into it.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_ShapesOf_MicroEngine_Physics_BodyId_System_Span_MicroEngine_Physics_ShapeId__"></a> ShapesOf\(BodyId, Span<ShapeId\>\)

Writes the live shapes of <code class="paramref">body</code>, in the order they were added, into <code class="paramref">results</code>
and returns how many it has (which may exceed the span's length: only the first ones are written).

```csharp
public int ShapesOf(BodyId body, Span<ShapeId> results)
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

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Step_System_Single_"></a> Step\(float\)

Advances the world by <code class="paramref">dt</code> seconds: dynamic bodies move, and the events of this step replace the
previous ones. No game code runs during it.

```csharp
public void Step(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">dt</code> is negative or not finite.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_TilesOf_MicroEngine_Physics_ShapeId_"></a> TilesOf\(ShapeId\)

The tiles <code class="paramref">shape</code> stands for when it was added with [AddTiles](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_AddTiles_MicroEngine_Physics_BodyId_MicroEngine_World_ICollisionGrid_System_Int32_System_Int32_System_UInt64_System_UInt64_); null for any other shape.

```csharp
public ICollisionGrid? TilesOf(ShapeId shape)
```

#### Parameters

`shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

#### Returns

 [ICollisionGrid](MicroEngine.World.ICollisionGrid.md)?

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_VelocityOf_MicroEngine_Physics_BodyId_"></a> VelocityOf\(BodyId\)

The body's linear velocity in world units per second (zero for static bodies; kinematic bodies moved by [Move](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md#MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__) report zero).

```csharp
public Vector2 VelocityOf(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

</div>

# Class TilePhysicsWorld2D

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics.Tiles](MicroEngine.Physics.Tiles.md)  
Assembly: MicroEngine.Physics.Tiles.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

The tile backend of [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md): static and kinematic bodies with axis-aligned box shapes, tile grids
as static geometry ([AddTiles](MicroEngine.Physics.Tiles.TilePhysicsWorld2D.md#MicroEngine_Physics_Tiles_TilePhysicsWorld2D_AddTiles_MicroEngine_Physics_BodyId_MicroEngine_World_ICollisionGrid_System_UInt64_System_UInt64_)), and an exact swept mover. It declares
[TileGeometry](MicroEngine.Physics.PhysicsCapabilities.md), [OneWayPlatforms](MicroEngine.Physics.PhysicsCapabilities.md) and
[MovingSolids](MicroEngine.Physics.PhysicsCapabilities.md); it has no dynamic bodies, so [ContactEvents](MicroEngine.Physics.Tiles.TilePhysicsWorld2D.md#MicroEngine_Physics_Tiles_TilePhysicsWorld2D_ContactEvents) is always
empty, and every other capability throws [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md).

```csharp
public sealed class TilePhysicsWorld2D : IPhysicsWorld2D, IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TilePhysicsWorld2D](MicroEngine.Physics.Tiles.TilePhysicsWorld2D.md)

#### Implements

IPhysicsWorld2D, 
IStateHashed

## Remarks

<p>Space is the engine's 2D space: Y grows downward and [Up](MicroEngine.Physics.Tiles.TilePhysicsWorld2D.md#MicroEngine_Physics_Tiles_TilePhysicsWorld2D_Up) is (0, -1); units are whatever the game
uses (pixels in a pixel-art game). [Move](MicroEngine.Physics.Tiles.TilePhysicsWorld2D.md#MicroEngine_Physics_Tiles_TilePhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__) resolves X, then Y, each as one exact swept test: the box stops
flush against the first surface in its way however thin it is or however fast the box moves, and rests on it exactly
([Skin](MicroEngine.Physics.Tiles.TilePhysicsWorld2D.md#MicroEngine_Physics_Tiles_TilePhysicsWorld2D_Skin) is 0; a flush box may end within <code>0.001</code> units inside a surface from rounding, which never
counts as overlapping).</p>
<p>A tile grid is one shape made of cells: its [Solid](MicroEngine.World.TileFlags.md) cells block from every side, its
[OneWay](MicroEngine.World.TileFlags.md) cells are one-way platforms, and other cells are not geometry (read hazards and water
with [TouchedFlags](MicroEngine.World.TileMotion.md#MicroEngine_World_TileMotion_TouchedFlags_MicroEngine_RectF_MicroEngine_World_ICollisionGrid_)). Changing a tile changes collisions at once. Cells a box overlaps when a
move starts do not block it.</p>
<p>Deterministic: the same calls give the same results on every target (managed code with no threads). The world
takes part in the per-step state hash ([IStateHashed](MicroEngine.Kernel.IStateHashed.md)).</p>

## Constructors

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D__ctor"></a> TilePhysicsWorld2D\(\)

```csharp
public TilePhysicsWorld2D()
```

## Fields

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_ContactDistance"></a> ContactDistance

The distance within which a surface counts as touching a box at the end of a move (the ground probe).

```csharp
public const float ContactDistance = 0.01
```

#### Field Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_Declared"></a> Declared

The capabilities this backend declares.

```csharp
public const PhysicsCapabilities Declared = TileGeometry | OneWayPlatforms | MovingSolids
```

#### Field Value

 PhysicsCapabilities

## Properties

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_Backend"></a> Backend

The backend's name, for messages ("tiles", "box2d").

```csharp
public string Backend { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_Bodies"></a> Bodies

The live bodies, in creation order (a destroyed body leaves its place).

```csharp
public ReadOnlySpan<BodyId> Bodies { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<BodyId\>

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_Capabilities"></a> Capabilities

What the backend offers beyond the common contract.

```csharp
public PhysicsCapabilities Capabilities { get; }
```

#### Property Value

 PhysicsCapabilities

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_ContactEvents"></a> ContactEvents

Always empty: this backend has no dynamic bodies.

```csharp
public ReadOnlySpan<ContactEvent> ContactEvents { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<ContactEvent\>

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_SensorEvents"></a> SensorEvents

The sensor entries and exits of the latest step: exits first, then entries, each in sensor then visitor order.

```csharp
public ReadOnlySpan<SensorEvent> SensorEvents { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<SensorEvent\>

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_Skin"></a> Skin

0: [Move](MicroEngine.Physics.Tiles.TilePhysicsWorld2D.md#MicroEngine_Physics_Tiles_TilePhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__) rests exactly on surfaces.

```csharp
public float Skin { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_StepCount"></a> StepCount

How many times [Step](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Step_System_Single_) ran.

```csharp
public long StepCount { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_Up"></a> Up

(0, -1): Y grows downward.

```csharp
public Vector2 Up { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Methods

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_AddShape_MicroEngine_Physics_BodyId_MicroEngine_Physics_ShapeDescription__"></a> AddShape\(BodyId, in ShapeDescription\)

```csharp
public ShapeId AddShape(BodyId body, in ShapeDescription shape)
```

#### Parameters

`body` BodyId

`shape` ShapeDescription

#### Returns

 ShapeId

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_AddTiles_MicroEngine_Physics_BodyId_MicroEngine_World_ICollisionGrid_System_UInt64_System_UInt64_"></a> AddTiles\(BodyId, ICollisionGrid, ulong, ulong\)

Attaches <code class="paramref">grid</code> to a static body as one shape ([TileGeometry](MicroEngine.Physics.PhysicsCapabilities.md)):
cell (x, y) covers the body's origin plus (x, y) × [TileSize](MicroEngine.World.ICollisionGrid.md#MicroEngine_World_ICollisionGrid_TileSize). Solid cells block, one-way
cells are one-way platforms; the grid is read at every query, so changing a tile changes collisions at once.

```csharp
public ShapeId AddTiles(BodyId body, ICollisionGrid grid, ulong category = 1, ulong mask = 18446744073709551615)
```

#### Parameters

`body` BodyId

A static body.

`grid` [ICollisionGrid](MicroEngine.World.ICollisionGrid.md)

The tiles.

`category` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

The layers the grid is in.

`mask` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

The layers it collides with.

#### Returns

 ShapeId

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The body is stale or not static.

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_BodyOf_MicroEngine_Physics_ShapeId_"></a> BodyOf\(ShapeId\)

The body <code class="paramref">shape</code> is attached to.

```csharp
public BodyId BodyOf(ShapeId shape)
```

#### Parameters

`shape` ShapeId

#### Returns

 BodyId

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_CreateBody_MicroEngine_Scenes_EntityId_MicroEngine_Physics_BodyDescription__"></a> CreateBody\(EntityId, in BodyDescription\)

```csharp
public BodyId CreateBody(EntityId owner, in BodyDescription body)
```

#### Parameters

`owner` EntityId

`body` BodyDescription

#### Returns

 BodyId

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_DestroyBody_MicroEngine_Physics_BodyId_"></a> DestroyBody\(BodyId\)

Destroys the body and its shapes; nothing for a stale or none id. Their sensor pairs end at the next step.

```csharp
public void DestroyBody(BodyId body)
```

#### Parameters

`body` BodyId

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the world's simulation state: the step count, every live body (id, kind, position, shapes) and the sensor pairs.

```csharp
public void HashState(StateHasher hash)
```

#### Parameters

`hash` StateHasher

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_IsAlive_MicroEngine_Physics_BodyId_"></a> IsAlive\(BodyId\)

Whether <code class="paramref">body</code> exists.

```csharp
public bool IsAlive(BodyId body)
```

#### Parameters

`body` BodyId

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_IsAlive_MicroEngine_Physics_ShapeId_"></a> IsAlive\(ShapeId\)

Whether <code class="paramref">shape</code> exists.

```csharp
public bool IsAlive(ShapeId shape)
```

#### Parameters

`shape` ShapeId

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_KindOf_MicroEngine_Physics_BodyId_"></a> KindOf\(BodyId\)

How <code class="paramref">body</code> moves.

```csharp
public BodyKind KindOf(BodyId body)
```

#### Parameters

`body` BodyId

#### Returns

 BodyKind

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__"></a> Move\(BodyId, Vector2, in QueryFilter\)

```csharp
public MoveResult Move(BodyId body, Vector2 delta, in QueryFilter filter = default)
```

#### Parameters

`body` BodyId

`delta` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`filter` QueryFilter

#### Returns

 MoveResult

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_Overlap_MicroEngine_RectF__MicroEngine_Physics_QueryFilter__System_Span_MicroEngine_Physics_ShapeId__"></a> Overlap\(in RectF, in QueryFilter, Span<ShapeId\>\)

```csharp
public int Overlap(in RectF area, in QueryFilter filter, Span<ShapeId> results)
```

#### Parameters

`area` RectF

`filter` QueryFilter

`results` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<ShapeId\>

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_OwnerOf_MicroEngine_Physics_BodyId_"></a> OwnerOf\(BodyId\)

The entity that owns <code class="paramref">body</code>.

```csharp
public EntityId OwnerOf(BodyId body)
```

#### Parameters

`body` BodyId

#### Returns

 EntityId

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_PositionOf_MicroEngine_Physics_BodyId_"></a> PositionOf\(BodyId\)

The body's origin.

```csharp
public Vector2 PositionOf(BodyId body)
```

#### Parameters

`body` BodyId

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_RayCast_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__MicroEngine_Physics_RayHit__"></a> RayCast\(Vector2, Vector2, in QueryFilter, out RayHit\)

```csharp
public bool RayCast(Vector2 from, Vector2 to, in QueryFilter filter, out RayHit hit)
```

#### Parameters

`from` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`to` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`filter` QueryFilter

`hit` RayHit

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_RemoveShape_MicroEngine_Physics_ShapeId_"></a> RemoveShape\(ShapeId\)

Removes a shape; nothing for a stale or none id. Its sensor pairs end at the next step.

```csharp
public void RemoveShape(ShapeId shape)
```

#### Parameters

`shape` ShapeId

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_RotationOf_MicroEngine_Physics_BodyId_"></a> RotationOf\(BodyId\)

Always 0: this backend has no rotation.

```csharp
public float RotationOf(BodyId body)
```

#### Parameters

`body` BodyId

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_SetPosition_MicroEngine_Physics_BodyId_System_Numerics_Vector2_"></a> SetPosition\(BodyId, Vector2\)

Teleports the body: no collision, no pushing; sensor events follow at the next step.

```csharp
public void SetPosition(BodyId body, Vector2 position)
```

#### Parameters

`body` BodyId

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The position is not finite.

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_SetVelocity_MicroEngine_Physics_BodyId_System_Numerics_Vector2_"></a> SetVelocity\(BodyId, Vector2\)

Always throws: this backend has no dynamic bodies.

```csharp
public void SetVelocity(BodyId body, Vector2 velocity)
```

#### Parameters

`body` BodyId

`velocity` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 PhysicsCapabilityException

Always.

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_Step_System_Single_"></a> Step\(float\)

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

### <a id="MicroEngine_Physics_Tiles_TilePhysicsWorld2D_VelocityOf_MicroEngine_Physics_BodyId_"></a> VelocityOf\(BodyId\)

Always throws: this backend has no dynamic bodies.

```csharp
public Vector2 VelocityOf(BodyId body)
```

#### Parameters

`body` BodyId

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 PhysicsCapabilityException

Always.

</div>

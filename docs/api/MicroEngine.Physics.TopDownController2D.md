# Class TopDownController2D

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics](../manual/physics.md)  

A top-down character: one kinematic body that moves in any direction and slides along what it touches
([Move](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__)), with no gravity. It takes a direction instead of reading input, like
[CharacterController2D](MicroEngine.Physics.CharacterController2D.md), and works on every backend.

```csharp
public sealed class TopDownController2D : IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TopDownController2D](MicroEngine.Physics.TopDownController2D.md)

#### Implements

[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

## Examples

```csharp
var hero = TopDownController2D.Create(physics, entity, start, new Vector2(12, 12));
// each fixed step
hero.Step(dt, new Vector2(Input.Axis("left", "right"), Input.Axis("up", "down")));
```

## Constructors

### <a id="MicroEngine_Physics_TopDownController2D__ctor_MicroEngine_Physics_IPhysicsWorld2D_MicroEngine_Physics_BodyId_MicroEngine_Physics_TopDownMotion_"></a> TopDownController2D\(IPhysicsWorld2D, BodyId, TopDownMotion?\)

Drives <code class="paramref">body</code>, a kinematic body with one non-sensor shape.

```csharp
public TopDownController2D(IPhysicsWorld2D world, BodyId body, TopDownMotion? motion = null)
```

#### Parameters

`world` [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md)

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`motion` [TopDownMotion](MicroEngine.Physics.TopDownMotion.md)?

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The body is stale or not kinematic.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A value of <code class="paramref">motion</code> is out of range.

## Properties

### <a id="MicroEngine_Physics_TopDownController2D_Body"></a> Body

The character's body.

```csharp
public BodyId Body { get; }
```

#### Property Value

 [BodyId](MicroEngine.Physics.BodyId.md)

### <a id="MicroEngine_Physics_TopDownController2D_Filter"></a> Filter

The shapes that block the character; the default filter unless set.

```csharp
public QueryFilter Filter { get; set; }
```

#### Property Value

 [QueryFilter](MicroEngine.Physics.QueryFilter.md)

### <a id="MicroEngine_Physics_TopDownController2D_LastMove"></a> LastMove

The result of the latest step's move: every surface the character touches.

```csharp
public MoveResult LastMove { get; }
```

#### Property Value

 [MoveResult](MicroEngine.Physics.MoveResult.md)

### <a id="MicroEngine_Physics_TopDownController2D_Motion"></a> Motion

How the character moves.

```csharp
public TopDownMotion Motion { get; set; }
```

#### Property Value

 [TopDownMotion](MicroEngine.Physics.TopDownMotion.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A value is out of range.

### <a id="MicroEngine_Physics_TopDownController2D_Position"></a> Position

The body's origin. Setting it places the body there without collision; the velocity is kept.

```csharp
public Vector2 Position { get; set; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The position is not finite.

### <a id="MicroEngine_Physics_TopDownController2D_Velocity"></a> Velocity

The character's velocity, in units per second.

```csharp
public Vector2 Velocity { get; set; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_TopDownController2D_World"></a> World

The physics world.

```csharp
public IPhysicsWorld2D World { get; }
```

#### Property Value

 [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md)

## Methods

### <a id="MicroEngine_Physics_TopDownController2D_Create_MicroEngine_Physics_IPhysicsWorld2D_MicroEngine_Scenes_EntityId_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Physics_TopDownMotion_System_UInt64_System_UInt64_System_Numerics_Vector2_"></a> Create\(IPhysicsWorld2D, EntityId, Vector2, Vector2, TopDownMotion?, ulong, ulong, Vector2\)

Creates a kinematic body with one box of <code class="paramref">size</code> and a controller for it. The body's origin,
<code class="paramref">position</code>, is the box's centre unless <code class="paramref">offset</code> (the box's centre relative
to the origin) says otherwise: a game that places its characters by their feet passes half the height upward.

```csharp
public static TopDownController2D Create(IPhysicsWorld2D world, EntityId owner, Vector2 position, Vector2 size, TopDownMotion? motion = null, ulong category = 1, ulong mask = 18446744073709551615, Vector2 offset = default)
```

#### Parameters

`world` [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md)

`owner` [EntityId](MicroEngine.Scenes.EntityId.md)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`size` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`motion` [TopDownMotion](MicroEngine.Physics.TopDownMotion.md)?

`category` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

`mask` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

`offset` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [TopDownController2D](MicroEngine.Physics.TopDownController2D.md)

### <a id="MicroEngine_Physics_TopDownController2D_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the character's velocity.

```csharp
public void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_Physics_TopDownController2D_Step_System_Single_System_Numerics_Vector2_"></a> Step\(float, Vector2\)

Advances the character by <code class="paramref">dt</code> seconds toward <code class="paramref">move</code>, a direction whose length
is the share of [MoveSpeed](MicroEngine.Physics.TopDownMotion.md#MicroEngine_Physics_TopDownMotion_MoveSpeed) wanted (longer than 1 counts as 1, so a diagonal is no
faster), and returns the move's result.

```csharp
public MoveResult Step(float dt, Vector2 move)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

`move` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [MoveResult](MicroEngine.Physics.MoveResult.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">dt</code> is negative or not finite.

</div>

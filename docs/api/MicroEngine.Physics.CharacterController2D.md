# Class CharacterController2D

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

A side-view character: one kinematic body moved with [Move](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__) under gravity, with
variable jump height, coyote time, jump buffering, drops through one-way platforms, optional air jumps, wall slides
and wall jumps, and the ground, wall and ceiling state a game reads. It works on every backend, since it uses only
the common contract (and one-way platforms where the backend declares them); it takes intent
([CharacterInput](MicroEngine.Physics.CharacterInput.md)) instead of reading input, so a player and an AI drive it the same way.

```csharp
public sealed class CharacterController2D : IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CharacterController2D](MicroEngine.Physics.CharacterController2D.md)

#### Implements

[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

## Examples

```csharp
var hero = CharacterController2D.Create(physics, entity, new Vector2(40, 100), new Vector2(12, 14));
// each fixed step
hero.Step(dt, new CharacterInput(Input.Axis("left", "right"), Input.Pressed("jump"), Input.Down("jump"), Input.Down("down")));
if ((hero.Events & CharacterEvents.Landed) != 0) PlayThud();
```

## Remarks

<p>Call [Step](MicroEngine.Physics.CharacterController2D.md#MicroEngine_Physics_CharacterController2D_Step_System_Single_MicroEngine_Physics_CharacterInput__) once per fixed step, before the physics world steps. A character that stands still
is not moved at all; one that stands on a body that moved since its last step (a platform the game moved) is moved
with it first, unless the backend already carried it ([MovingSolids](MicroEngine.Physics.PhysicsCapabilities.md)).</p>
<p>Deterministic: its state is its velocity, its timers and what it touches, all in
[HashState](MicroEngine.Physics.CharacterController2D.md#MicroEngine_Physics_CharacterController2D_HashState_MicroEngine_Kernel_StateHasher_); add it to the state hash of whatever owns it.</p>

## Constructors

### <a id="MicroEngine_Physics_CharacterController2D__ctor_MicroEngine_Physics_IPhysicsWorld2D_MicroEngine_Physics_BodyId_MicroEngine_Physics_CharacterMotion_"></a> CharacterController2D\(IPhysicsWorld2D, BodyId, CharacterMotion?\)

Drives <code class="paramref">body</code>, a kinematic body with one non-sensor shape.

```csharp
public CharacterController2D(IPhysicsWorld2D world, BodyId body, CharacterMotion? motion = null)
```

#### Parameters

`world` [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md)

The physics world.

`body` [BodyId](MicroEngine.Physics.BodyId.md)

The character's body.

`motion` [CharacterMotion](MicroEngine.Physics.CharacterMotion.md)?

How it moves; the defaults when null.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The body is stale or not kinematic.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A value of <code class="paramref">motion</code> is out of range.

## Properties

### <a id="MicroEngine_Physics_CharacterController2D_AirTime"></a> AirTime

How long it has been in the air, in seconds; in the step it lands, the length of the flight that ended (for a
landing effect); 0 on the ground after that.

```csharp
public float AirTime { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterController2D_Body"></a> Body

The character's body.

```csharp
public BodyId Body { get; }
```

#### Property Value

 [BodyId](MicroEngine.Physics.BodyId.md)

### <a id="MicroEngine_Physics_CharacterController2D_Events"></a> Events

What happened in the latest [Step](MicroEngine.Physics.CharacterController2D.md#MicroEngine_Physics_CharacterController2D_Step_System_Single_MicroEngine_Physics_CharacterInput__).

```csharp
public CharacterEvents Events { get; }
```

#### Property Value

 [CharacterEvents](MicroEngine.Physics.CharacterEvents.md)

### <a id="MicroEngine_Physics_CharacterController2D_Filter"></a> Filter

The shapes that block the character ([Move](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__)'s filter); the default filter unless set.

```csharp
public QueryFilter Filter { get; set; }
```

#### Property Value

 [QueryFilter](MicroEngine.Physics.QueryFilter.md)

### <a id="MicroEngine_Physics_CharacterController2D_Ground"></a> Ground

The shape it stands on; [None](MicroEngine.Physics.ShapeId.md#MicroEngine_Physics_ShapeId_None) in the air.

```csharp
public ShapeId Ground { get; }
```

#### Property Value

 [ShapeId](MicroEngine.Physics.ShapeId.md)

### <a id="MicroEngine_Physics_CharacterController2D_IsGrounded"></a> IsGrounded

Whether it stands on ground.

```csharp
public bool IsGrounded { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_CharacterController2D_IsJumping"></a> IsJumping

Whether a jump is rising and can still be cut short by releasing jump.

```csharp
public bool IsJumping { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_CharacterController2D_IsOnWall"></a> IsOnWall

Whether it touches a wall.

```csharp
public bool IsOnWall { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_CharacterController2D_IsSkidding"></a> IsSkidding

Whether it skids: on the ground, braking against its motion before it turns ([SkidDeceleration](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_SkidDeceleration)).

```csharp
public bool IsSkidding { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_CharacterController2D_LandingSpeed"></a> LandingSpeed

The downward speed it had when it last landed, in units per second (for a landing's dust, sound or damage).

```csharp
public float LandingSpeed { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterController2D_LastMove"></a> LastMove

The result of the latest step's move: every surface the character touches.

```csharp
public MoveResult LastMove { get; }
```

#### Property Value

 [MoveResult](MicroEngine.Physics.MoveResult.md)

### <a id="MicroEngine_Physics_CharacterController2D_Motion"></a> Motion

How the character moves; replace it to change its feel (a power-up, water).

```csharp
public CharacterMotion Motion { get; set; }
```

#### Property Value

 [CharacterMotion](MicroEngine.Physics.CharacterMotion.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A value is out of range.

### <a id="MicroEngine_Physics_CharacterController2D_Position"></a> Position

The body's origin.

```csharp
public Vector2 Position { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_CharacterController2D_Solid"></a> Solid

Decides, shape by shape, what is solid for this character, beyond what the layers of [Filter](MicroEngine.Physics.CharacterController2D.md#MicroEngine_Physics_CharacterController2D_Filter) say:
it is asked about each shape near the character before each of its moves, with the character as it is then
(its [Velocity](MicroEngine.Physics.CharacterController2D.md#MicroEngine_Physics_CharacterController2D_Velocity) is the one it is about to move with), and a shape it returns false for is passed
through. For solids that depend on how they are met: a hidden block that only a head coming from below finds,
a gate that lets one character through. Null (the default) asks nothing. It needs a backend that declares
[FilterPredicates](MicroEngine.Physics.PhysicsCapabilities.md); it may read the world but not query or change it.

```csharp
public Func<CharacterController2D, ShapeId, bool>? Solid { get; set; }
```

#### Property Value

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-3)<[CharacterController2D](MicroEngine.Physics.CharacterController2D.md), [ShapeId](MicroEngine.Physics.ShapeId.md), [bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>?

### <a id="MicroEngine_Physics_CharacterController2D_Velocity"></a> Velocity

The character's velocity, in units per second.

```csharp
public Vector2 Velocity { get; set; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_CharacterController2D_WallNormal"></a> WallNormal

The normal of the wall it touches, pointing away from the wall; zero when it touches none.

```csharp
public Vector2 WallNormal { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_CharacterController2D_World"></a> World

The physics world.

```csharp
public IPhysicsWorld2D World { get; }
```

#### Property Value

 [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md)

## Methods

### <a id="MicroEngine_Physics_CharacterController2D_Create_MicroEngine_Physics_IPhysicsWorld2D_MicroEngine_Scenes_EntityId_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Physics_CharacterMotion_System_UInt64_System_UInt64_"></a> Create\(IPhysicsWorld2D, EntityId, Vector2, Vector2, CharacterMotion?, ulong, ulong\)

Creates a kinematic body with one box of <code class="paramref">size</code> centred at <code class="paramref">position</code> and a
controller for it, moving through the layers of <code class="paramref">mask</code>.

```csharp
public static CharacterController2D Create(IPhysicsWorld2D world, EntityId owner, Vector2 position, Vector2 size, CharacterMotion? motion = null, ulong category = 1, ulong mask = 18446744073709551615)
```

#### Parameters

`world` [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md)

The physics world.

`owner` [EntityId](MicroEngine.Scenes.EntityId.md)

The entity that owns the body.

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The centre of the box.

`size` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The box's width and height.

`motion` [CharacterMotion](MicroEngine.Physics.CharacterMotion.md)?

How it moves; the defaults when null.

`category` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

The layers the character is in.

`mask` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

The layers it collides with.

#### Returns

 [CharacterController2D](MicroEngine.Physics.CharacterController2D.md)

### <a id="MicroEngine_Physics_CharacterController2D_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the character's state: velocity, ground, wall, jump and skid state, timers, air time, the last landing's speed, air jumps used and the latest events.

```csharp
public void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_Physics_CharacterController2D_Launch_System_Numerics_Vector2_"></a> Launch\(Vector2\)

Throws the character with <code class="paramref">velocity</code> (a spring, a knock-back): it leaves the ground, and the
rise is not cut short by a released jump button.

```csharp
public void Launch(Vector2 velocity)
```

#### Parameters

`velocity` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_CharacterController2D_Step_System_Single_MicroEngine_Physics_CharacterInput__"></a> Step\(float, in CharacterInput\)

Advances the character by <code class="paramref">dt</code> seconds with <code class="paramref">input</code> and returns the move's result (also [LastMove](MicroEngine.Physics.CharacterController2D.md#MicroEngine_Physics_CharacterController2D_LastMove)).

```csharp
public MoveResult Step(float dt, in CharacterInput input)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

`input` [CharacterInput](MicroEngine.Physics.CharacterInput.md)

#### Returns

 [MoveResult](MicroEngine.Physics.MoveResult.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">dt</code> is negative or not finite.

### <a id="MicroEngine_Physics_CharacterController2D_Teleport_System_Numerics_Vector2_"></a> Teleport\(Vector2\)

Places the character at <code class="paramref">position</code> without collision; it keeps its velocity and is in the air until its next step.

```csharp
public void Teleport(Vector2 position)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

</div>

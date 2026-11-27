# Class CharacterController2D

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics](../manual/physics.md)  

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

The body's origin. Setting it places the body there without collision and changes nothing else: the character
keeps its ground, its velocity and its timers (keeping it inside the camera's view, a scripted placement).
[Teleport](MicroEngine.Physics.CharacterController2D.md#MicroEngine_Physics_CharacterController2D_Teleport_System_Numerics_Vector2_) is the jump to another place, which leaves the ground.

```csharp
public Vector2 Position { get; set; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The position is not finite.

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

### <a id="MicroEngine_Physics_CharacterController2D_Create_MicroEngine_Physics_IPhysicsWorld2D_MicroEngine_Scenes_EntityId_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Physics_CharacterMotion_System_UInt64_System_UInt64_System_Numerics_Vector2_"></a> Create\(IPhysicsWorld2D, EntityId, Vector2, Vector2, CharacterMotion?, ulong, ulong, Vector2\)

Creates a kinematic body with one box of <code class="paramref">size</code> and a controller for it, moving through the
layers of <code class="paramref">mask</code>. The body's origin is the box's centre unless <code class="paramref">offset</code> says
otherwise.

```csharp
public static CharacterController2D Create(IPhysicsWorld2D world, EntityId owner, Vector2 position, Vector2 size, CharacterMotion? motion = null, ulong category = 1, ulong mask = 18446744073709551615, Vector2 offset = default)
```

#### Parameters

`world` [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md)

The physics world.

`owner` [EntityId](MicroEngine.Scenes.EntityId.md)

The entity that owns the body.

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The body's origin: what [Position](MicroEngine.Physics.CharacterController2D.md#MicroEngine_Physics_CharacterController2D_Position) reads and what a scene's physics writes to the owner's transform.

`size` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The box's width and height.

`motion` [CharacterMotion](MicroEngine.Physics.CharacterMotion.md)?

How it moves; the defaults when null.

`category` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

The layers the character is in.

`mask` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

The layers it collides with.

`offset` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The box's centre relative to the body's origin; zero (the default) centres the box on it. A game that places
its characters by their feet passes half the height upward (<code>new Vector2(0, -size.Y / 2)</code> where y grows
downward); one that places them by their top-left corner passes <code>size / 2</code>.

#### Returns

 [CharacterController2D](MicroEngine.Physics.CharacterController2D.md)

### <a id="MicroEngine_Physics_CharacterController2D_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the character's state: velocity, ground, wall, jump and skid state, timers, air time, the last landing's speed, air jumps used and the latest events.

```csharp
public void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_Physics_CharacterController2D_Launch_System_Numerics_Vector2_System_Boolean_"></a> Launch\(Vector2, bool\)

Throws the character with <code class="paramref">velocity</code> (a spring, a knock-back, a bounce off an enemy): it
leaves the ground.

```csharp
public void Launch(Vector2 velocity, bool cuttable = false)
```

#### Parameters

`velocity` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The velocity it leaves with.

`cuttable` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the rise is a jump the player shapes: releasing jump cuts it short as it cuts a jump
([JumpReleaseGravityScale](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_JumpReleaseGravityScale), [JumpReleaseSpeedScale](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_JumpReleaseSpeedScale)),
so a bounce is higher while jump is held. False (the default): the rise is not cut by a released jump button.

### <a id="MicroEngine_Physics_CharacterController2D_Restore_MicroEngine_Physics_CharacterState__"></a> Restore\(in CharacterState\)

Makes the character what <code class="paramref">state</code> says ([Snapshot](MicroEngine.Physics.CharacterController2D.md#MicroEngine_Physics_CharacterController2D_Snapshot), or one built by the game): the
body is placed without collision, and the next step goes on from there as it would have. A ground shape that
no longer exists is no ground.

```csharp
public void Restore(in CharacterState state)
```

#### Parameters

`state` [CharacterState](MicroEngine.Physics.CharacterState.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A position, velocity or time of <code class="paramref">state</code> is not finite, or a time or count is negative.

### <a id="MicroEngine_Physics_CharacterController2D_Snapshot"></a> Snapshot\(\)

The character's whole state: where it is, how it moves, what it stands on, its timers and what its latest
step reported. [Restore](MicroEngine.Physics.CharacterController2D.md#MicroEngine_Physics_CharacterController2D_Restore_MicroEngine_Physics_CharacterState__) puts it back, on this controller or on another one of the same world: a
planner tries a jump and takes it back, a checkpoint keeps the character.

```csharp
public CharacterState Snapshot()
```

#### Returns

 [CharacterState](MicroEngine.Physics.CharacterState.md)

### <a id="MicroEngine_Physics_CharacterController2D_StandOn_MicroEngine_Physics_ShapeId_System_Numerics_Vector2_"></a> StandOn\(ShapeId, Vector2\)

Puts the character on ground the game decided: at <code class="paramref">position</code>, standing on
<code class="paramref">ground</code>, with no vertical speed. For ground with a rule of the game's own that the world's
shapes do not give (a platform that catches a character a little before it touches, the back of a creature):
call it after [Step](MicroEngine.Physics.CharacterController2D.md#MicroEngine_Physics_CharacterController2D_Step_System_Single_MicroEngine_Physics_CharacterInput__) when the rule holds. From then on the character is grounded as on any other
ground: it is carried when the body of <code class="paramref">ground</code> moves, it can jump, and it stays until a
step finds nothing under it, so a rule that still holds calls this again after each step. A character that
left the ground in the latest step and is put back has not left it (no [LeftGround](MicroEngine.Physics.CharacterEvents.md));
one that was in the air has landed.

```csharp
public void StandOn(ShapeId ground, Vector2 position)
```

#### Parameters

`ground` [ShapeId](MicroEngine.Physics.ShapeId.md)

The shape it stands on; its body need not collide with the character.

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Where the body's origin goes.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

<code class="paramref">ground</code> is not a live shape.

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

# Class CharacterMotion

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics](../manual/physics.md)  

How a [CharacterController2D](MicroEngine.Physics.CharacterController2D.md) moves, in the physics world's units and seconds. The defaults suit a
pixel-art game with 16-pixel tiles; a world in metres sets its own numbers.

```csharp
public sealed record CharacterMotion : IEquatable<CharacterMotion>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CharacterMotion](MicroEngine.Physics.CharacterMotion.md)

#### Implements

[IEquatable<CharacterMotion\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Physics_CharacterMotion__ctor"></a> CharacterMotion\(\)

```csharp
public CharacterMotion()
```

## Properties

### <a id="MicroEngine_Physics_CharacterMotion_Acceleration"></a> Acceleration

How fast the sideways speed grows toward the intent, in units per second squared; infinite (the default) reaches it at once.

```csharp
public float Acceleration { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_AirAcceleration"></a> AirAcceleration

How fast the sideways speed grows toward the intent in the air, in units per second squared: how much the
player steers a jump. Null (the default) is [Acceleration](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_Acceleration) times [AirControl](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_AirControl).

```csharp
public float? AirAcceleration { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="MicroEngine_Physics_CharacterMotion_AirControl"></a> AirControl

The share of [Acceleration](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_Acceleration) and [Deceleration](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_Deceleration) that applies in the air, from 0 (no air
control) to 1 (the default). [AirAcceleration](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_AirAcceleration) and [AirDeceleration](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_AirDeceleration) replace it with a
number each.

```csharp
public float AirControl { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_AirDeceleration"></a> AirDeceleration

How fast the sideways speed falls to zero in the air with no intent, in units per second squared: a low value
lets a jump keep travelling when the player lets go. Null (the default) is [Deceleration](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_Deceleration) times
[AirControl](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_AirControl).

```csharp
public float? AirDeceleration { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="MicroEngine_Physics_CharacterMotion_AirJumps"></a> AirJumps

Jumps allowed in the air before landing again (1 is a double jump); 0 by default.

```csharp
public int AirJumps { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_CharacterMotion_AirOverspeedDeceleration"></a> AirOverspeedDeceleration

The same in the air, in units per second squared: 0 keeps a run's momentum for the whole jump, whatever the
player does with run in the air. Null (the default) uses the air's acceleration.

```csharp
public float? AirOverspeedDeceleration { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="MicroEngine_Physics_CharacterMotion_ApexGravityScale"></a> ApexGravityScale

Gravity is multiplied by this near the top of a jump, while the vertical speed is under
[ApexSpeed](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_ApexSpeed): on the way up unless the jump was released, and on the way down while jump is held.
Under 1 it makes the top of a jump float; 1 (the default) has no such phase.

```csharp
public float ApexGravityScale { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_ApexSpeed"></a> ApexSpeed

The vertical speed under which [ApexGravityScale](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_ApexGravityScale) applies, in units per second; 0 (the default) turns the phase off.

```csharp
public float ApexSpeed { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_CeilingReboundSpeed"></a> CeilingReboundSpeed

The downward speed a character leaves a ceiling with, in units per second; 0 (the default) leaves it to gravity.

```csharp
public float CeilingReboundSpeed { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_CornerCorrection"></a> CornerCorrection

How far a rising character is moved sideways to get past the corner of a ceiling that only its edge would
clip, in units; 0 (the default) turns it off. It is moved the least of four equal parts of this that frees the
way, and only where there is room for it.

```csharp
public float CornerCorrection { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_CoyoteTime"></a> CoyoteTime

How long after leaving a ledge a jump is still allowed, in seconds.

```csharp
public float CoyoteTime { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_Deceleration"></a> Deceleration

How fast the sideways speed falls to zero with no intent, in units per second squared; infinite (the default) stops at once.

```csharp
public float Deceleration { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_DropThroughTime"></a> DropThroughTime

How long one-way platforms are passed through after a drop, in seconds.

```csharp
public float DropThroughTime { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_DropWithJump"></a> DropWithJump

Whether a drop needs a jump: with it, [Drop](MicroEngine.Physics.CharacterInput.md#MicroEngine_Physics_CharacterInput_Drop) held makes the jump of a character on
a one-way platform a drop (pressed in that step or still remembered by [JumpBuffer](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_JumpBuffer), which the
drop uses up), and holding it alone does nothing: "down and jump". False (the default) drops as soon as
[Drop](MicroEngine.Physics.CharacterInput.md#MicroEngine_Physics_CharacterInput_Drop) is held.

```csharp
public bool DropWithJump { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_CharacterMotion_FallGravityScale"></a> FallGravityScale

Gravity is multiplied by this while falling; above 1 makes falls faster than rises.

```csharp
public float FallGravityScale { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_Gravity"></a> Gravity

Downward acceleration, in units per second squared.

```csharp
public float Gravity { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_GroundSnap"></a> GroundSnap

How far down the character is pulled to stay on ground that falls away under it when it walks (going down a
slope or a step), in units; 0 (the default) turns it off. A character that walks off a drop taller than this
leaves the ground.

```csharp
public float GroundSnap { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_JumpBuffer"></a> JumpBuffer

How long a jump pressed in the air is remembered and performed on landing, in seconds.

```csharp
public float JumpBuffer { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_JumpReleaseGravityScale"></a> JumpReleaseGravityScale

Gravity is multiplied by this while a jump rises with jump released, which makes a tap a short hop (variable
jump height); 1 gives every jump the same height.

```csharp
public float JumpReleaseGravityScale { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_JumpReleaseSpeedScale"></a> JumpReleaseSpeedScale

A jump that rises with jump released is slowed at once to this share of [JumpSpeed](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_JumpSpeed) (a slower rise
is kept): 0.45 cuts a released jump to 45 % of its starting speed in the step jump is let go, a sharper hop
than [JumpReleaseGravityScale](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_JumpReleaseGravityScale) gives. 1 (the default) cuts nothing.

```csharp
public float JumpReleaseSpeedScale { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_JumpSpeed"></a> JumpSpeed

The upward speed a jump starts with, in units per second: the jump rises about <code>JumpSpeed² / (2 × Gravity)</code>.

```csharp
public float JumpSpeed { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_LandingReach"></a> LandingReach

How near the ground a character that is not rising counts as standing on it, in units; 0 (the default) lands
on contact. With 1, a fall that ends a step within a unit of the floor has landed in that step (it can jump,
its fall speed is gone) and closes the rest of the way in the next steps: landings come a step earlier, as in
games that test for ground a pixel under the feet. It is a pixel or two of forgiveness, not a hover: while
the character is within the reach it stands, and sinks to the floor one step of fall gravity at a time, so a
step down lower than the reach is walked down slowly without leaving the ground. At most
[MaxLandingReach](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_MaxLandingReach).

```csharp
public float LandingReach { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_MaxFallSpeed"></a> MaxFallSpeed

The fastest fall, in units per second.

```csharp
public float MaxFallSpeed { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_MaxLandingReach"></a> MaxLandingReach

The largest [LandingReach](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_LandingReach) this motion takes: what its fastest fall covers in a thirtieth of a second.

```csharp
public float MaxLandingReach { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_MoveSpeed"></a> MoveSpeed

The sideways speed at full intent, in units per second.

```csharp
public float MoveSpeed { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_Order"></a> Order

Whether the ground carries the character before or after its jump or drop is read; [CarryThenAct](MicroEngine.Physics.CharacterStepOrder.md) by default.

```csharp
public CharacterStepOrder Order { get; init; }
```

#### Property Value

 [CharacterStepOrder](MicroEngine.Physics.CharacterStepOrder.md)

### <a id="MicroEngine_Physics_CharacterMotion_OverspeedDeceleration"></a> OverspeedDeceleration

How fast a character on the ground loses the speed it has above what the intent asks for while it keeps
pushing the way it moves, in units per second squared: what happens when run is released at full speed, or
after a boost. Null (the default) uses the acceleration, as for any other change of speed; a game usually
sets its [Deceleration](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_Deceleration) here (the ground's friction).

```csharp
public float? OverspeedDeceleration { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="MicroEngine_Physics_CharacterMotion_RunAcceleration"></a> RunAcceleration

How fast the sideways speed grows on the ground while running, in units per second squared; null (the default) is [Acceleration](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_Acceleration).

```csharp
public float? RunAcceleration { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="MicroEngine_Physics_CharacterMotion_RunJumpBonus"></a> RunJumpBonus

Extra jump speed at the motion's top speed ([RunSpeed](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_RunSpeed) when it has one, else [MoveSpeed](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_MoveSpeed)),
in proportion to the sideways speed when the jump starts, in units per second: a run-up jumps higher. The top
speed is the measure whether run is held or not, so the same speed always gives the same jump.

```csharp
public float RunJumpBonus { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_RunSpeed"></a> RunSpeed

The sideways speed at full intent while [Run](MicroEngine.Physics.CharacterInput.md#MicroEngine_Physics_CharacterInput_Run) is held, in units per second; 0 (the
default) has no run: the character always moves at [MoveSpeed](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_MoveSpeed).

```csharp
public float RunSpeed { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_SkidDeceleration"></a> SkidDeceleration

How fast a character on the ground brakes when the intent is against its motion and it moves faster than
[SkidSpeed](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_SkidSpeed), in units per second squared: it skids to that speed before it turns
([IsSkidding](MicroEngine.Physics.CharacterController2D.md#MicroEngine_Physics_CharacterController2D_IsSkidding)). 0 (the default) turns skidding off: a reversal is ordinary
acceleration.

```csharp
public float SkidDeceleration { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_SkidSpeed"></a> SkidSpeed

The sideways speed above which reversing is a skid, in units per second.

```csharp
public float SkidSpeed { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_WallJumpLockTime"></a> WallJumpLockTime

How long after a wall jump the sideways intent is ignored, so the jump carries the character away from the wall, in seconds.

```csharp
public float WallJumpLockTime { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_WallJumpSpeed"></a> WallJumpSpeed

The speed of a jump off a wall: X away from the wall, Y upward, in units per second; zero (the default) turns wall jumps off.

```csharp
public Vector2 WallJumpSpeed { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_CharacterMotion_WallSlideSpeed"></a> WallSlideSpeed

The fastest fall while pushing against a wall, in units per second; 0 (the default) turns wall sliding off.

```csharp
public float WallSlideSpeed { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Physics_CharacterMotion__Clone__"></a> <Clone\>$\(\)

```csharp
public CharacterMotion <Clone>$()
```

#### Returns

 [CharacterMotion](MicroEngine.Physics.CharacterMotion.md)

### <a id="MicroEngine_Physics_CharacterMotion_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_CharacterMotion_Equals_MicroEngine_Physics_CharacterMotion_"></a> Equals\(CharacterMotion?\)

```csharp
public bool Equals(CharacterMotion? other)
```

#### Parameters

`other` [CharacterMotion](MicroEngine.Physics.CharacterMotion.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_CharacterMotion_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_CharacterMotion_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Physics_CharacterMotion_Validate"></a> Validate\(\)

Throws when a value is out of range.

```csharp
public void Validate()
```

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A negative or not-a-number value, a non-finite speed or time, an air control or a release share outside 0..1, a landing reach above [MaxLandingReach](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_MaxLandingReach), or an undefined order.

## Operators

### <a id="MicroEngine_Physics_CharacterMotion_op_Equality_MicroEngine_Physics_CharacterMotion_MicroEngine_Physics_CharacterMotion_"></a> operator ==\(CharacterMotion?, CharacterMotion?\)

```csharp
public static bool operator ==(CharacterMotion? left, CharacterMotion? right)
```

#### Parameters

`left` [CharacterMotion](MicroEngine.Physics.CharacterMotion.md)?

`right` [CharacterMotion](MicroEngine.Physics.CharacterMotion.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_CharacterMotion_op_Inequality_MicroEngine_Physics_CharacterMotion_MicroEngine_Physics_CharacterMotion_"></a> operator \!=\(CharacterMotion?, CharacterMotion?\)

```csharp
public static bool operator !=(CharacterMotion? left, CharacterMotion? right)
```

#### Parameters

`left` [CharacterMotion](MicroEngine.Physics.CharacterMotion.md)?

`right` [CharacterMotion](MicroEngine.Physics.CharacterMotion.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

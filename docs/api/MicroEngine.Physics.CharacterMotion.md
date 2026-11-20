# Class CharacterMotion

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

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

### <a id="MicroEngine_Physics_CharacterMotion_AirControl"></a> AirControl

The share of [Acceleration](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_Acceleration) and [Deceleration](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_Deceleration) that applies in the air, from 0 (no air control) to 1 (the default).

```csharp
public float AirControl { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterMotion_AirJumps"></a> AirJumps

Jumps allowed in the air before landing again (1 is a double jump); 0 by default.

```csharp
public int AirJumps { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

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

### <a id="MicroEngine_Physics_CharacterMotion_JumpSpeed"></a> JumpSpeed

The upward speed a jump starts with, in units per second: the jump rises about <code>JumpSpeed² / (2 × Gravity)</code>.

```csharp
public float JumpSpeed { get; init; }
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

### <a id="MicroEngine_Physics_CharacterMotion_MoveSpeed"></a> MoveSpeed

The sideways speed at full intent, in units per second.

```csharp
public float MoveSpeed { get; init; }
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

A negative or not-a-number value, a non-finite speed or time, or an air control outside 0..1.

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

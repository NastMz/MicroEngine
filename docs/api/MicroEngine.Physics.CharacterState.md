# Struct CharacterState

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics](../manual/physics.md)  

The whole state of a [CharacterController2D](MicroEngine.Physics.CharacterController2D.md) ([Snapshot](MicroEngine.Physics.CharacterController2D.md#MicroEngine_Physics_CharacterController2D_Snapshot),
[Restore](MicroEngine.Physics.CharacterController2D.md#MicroEngine_Physics_CharacterController2D_Restore_MicroEngine_Physics_CharacterState__)). Every value can be read and, with <code>with</code>, changed: a game that
keeps the character's state elsewhere (a save, a replay, another simulation) builds one.

```csharp
public readonly struct CharacterState
```

## Properties

### <a id="MicroEngine_Physics_CharacterState_AirJumpsUsed"></a> AirJumpsUsed

Air jumps used since it last stood on ground.

```csharp
public int AirJumpsUsed { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_CharacterState_AirTime"></a> AirTime

How long it has been in the air, in seconds.

```csharp
public float AirTime { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterState_CoyoteTime"></a> CoyoteTime

The coyote time left, in seconds.

```csharp
public float CoyoteTime { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterState_DropThroughTime"></a> DropThroughTime

How much longer one-way platforms are passed through, in seconds.

```csharp
public float DropThroughTime { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterState_Events"></a> Events

What its latest step reported.

```csharp
public CharacterEvents Events { get; init; }
```

#### Property Value

 [CharacterEvents](MicroEngine.Physics.CharacterEvents.md)

### <a id="MicroEngine_Physics_CharacterState_Ground"></a> Ground

The shape it stands on, which carries it when its body moves; [None](MicroEngine.Physics.ShapeId.md#MicroEngine_Physics_ShapeId_None) for none.

```csharp
public ShapeId Ground { get; init; }
```

#### Property Value

 [ShapeId](MicroEngine.Physics.ShapeId.md)

### <a id="MicroEngine_Physics_CharacterState_GroundPosition"></a> GroundPosition

Where the body of [Ground](MicroEngine.Physics.CharacterState.md#MicroEngine_Physics_CharacterState_Ground) was when the character last stepped: it is carried by how far that body has moved since.

```csharp
public Vector2 GroundPosition { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_CharacterState_IsGrounded"></a> IsGrounded

Whether it stands on ground.

```csharp
public bool IsGrounded { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_CharacterState_IsJumping"></a> IsJumping

Whether a jump is rising and can still be cut short.

```csharp
public bool IsJumping { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_CharacterState_IsOnWall"></a> IsOnWall

Whether it touches a wall.

```csharp
public bool IsOnWall { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_CharacterState_IsSkidding"></a> IsSkidding

Whether it skids.

```csharp
public bool IsSkidding { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_CharacterState_JumpBuffer"></a> JumpBuffer

How much longer a pressed jump is remembered, in seconds.

```csharp
public float JumpBuffer { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterState_LandingSpeed"></a> LandingSpeed

The downward speed of its last landing.

```csharp
public float LandingSpeed { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterState_LastMove"></a> LastMove

The result of its latest step's move.

```csharp
public MoveResult LastMove { get; init; }
```

#### Property Value

 [MoveResult](MicroEngine.Physics.MoveResult.md)

### <a id="MicroEngine_Physics_CharacterState_Position"></a> Position

The body's origin.

```csharp
public Vector2 Position { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_CharacterState_Velocity"></a> Velocity

The velocity, in units per second.

```csharp
public Vector2 Velocity { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_CharacterState_WallJumpLockTime"></a> WallJumpLockTime

How much longer the sideways intent is ignored after a wall jump, in seconds.

```csharp
public float WallJumpLockTime { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_CharacterState_WallNormal"></a> WallNormal

The normal of that wall.

```csharp
public Vector2 WallNormal { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

</div>

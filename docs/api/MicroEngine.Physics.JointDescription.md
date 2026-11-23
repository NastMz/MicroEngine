# Struct JointDescription

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics](../manual/physics.md)  

What [CreateJoint](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_CreateJoint_MicroEngine_Physics_JointDescription__) creates. Build one with a factory ([Revolute](MicroEngine.Physics.JointDescription.md#MicroEngine_Physics_JointDescription_Revolute_MicroEngine_Physics_BodyId_MicroEngine_Physics_BodyId_System_Numerics_Vector2_),
[Distance](MicroEngine.Physics.JointDescription.md#MicroEngine_Physics_JointDescription_Distance_MicroEngine_Physics_BodyId_MicroEngine_Physics_BodyId_System_Numerics_Vector2_System_Numerics_Vector2_), [Rope](MicroEngine.Physics.JointDescription.md#MicroEngine_Physics_JointDescription_Rope_MicroEngine_Physics_BodyId_MicroEngine_Physics_BodyId_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_), [Prismatic](MicroEngine.Physics.JointDescription.md#MicroEngine_Physics_JointDescription_Prismatic_MicroEngine_Physics_BodyId_MicroEngine_Physics_BodyId_System_Numerics_Vector2_System_Numerics_Vector2_), [Weld](MicroEngine.Physics.JointDescription.md#MicroEngine_Physics_JointDescription_Weld_MicroEngine_Physics_BodyId_MicroEngine_Physics_BodyId_System_Numerics_Vector2_), [Wheel](MicroEngine.Physics.JointDescription.md#MicroEngine_Physics_JointDescription_Wheel_MicroEngine_Physics_BodyId_MicroEngine_Physics_BodyId_System_Numerics_Vector2_System_Numerics_Vector2_),
[Target](MicroEngine.Physics.JointDescription.md#MicroEngine_Physics_JointDescription_Target_MicroEngine_Physics_BodyId_System_Numerics_Vector2_)) and set limits, motors and springs with <code>with</code>. Anchors and axes are given in world
space as the bodies are when the joint is created; the joint then keeps them fixed in each body.

```csharp
public readonly struct JointDescription : IEquatable<JointDescription>
```

#### Implements

[IEquatable<JointDescription\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Properties

### <a id="MicroEngine_Physics_JointDescription_AnchorA"></a> AnchorA

The world point of the joint on body A when it is created.

```csharp
public Vector2 AnchorA { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_JointDescription_AnchorB"></a> AnchorB

The world point of the joint on body B when it is created (the same as [AnchorA](MicroEngine.Physics.JointDescription.md#MicroEngine_Physics_JointDescription_AnchorA) except for distance joints).

```csharp
public Vector2 AnchorB { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_JointDescription_Axis"></a> Axis

The world direction body B slides along (prismatic, wheel); any length but zero.

```csharp
public Vector2 Axis { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_JointDescription_BodyA"></a> BodyA

The first body (for a wheel, the chassis; unused by a target joint).

```csharp
public BodyId BodyA { get; init; }
```

#### Property Value

 [BodyId](MicroEngine.Physics.BodyId.md)

### <a id="MicroEngine_Physics_JointDescription_BodyB"></a> BodyB

The second body (for a wheel, the wheel; for a target joint, the dragged body).

```csharp
public BodyId BodyB { get; init; }
```

#### Property Value

 [BodyId](MicroEngine.Physics.BodyId.md)

### <a id="MicroEngine_Physics_JointDescription_BreakForce"></a> BreakForce

The joint is destroyed at the end of a step in which the force it applies to hold the bodies together exceeds
this (it is then in [BrokenJoints](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_BrokenJoints)); 0 (the default) never breaks.

```csharp
public float BreakForce { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_JointDescription_CollideConnected"></a> CollideConnected

Whether the two bodies still collide with each other (false by default).

```csharp
public bool CollideConnected { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_JointDescription_DampingRatio"></a> DampingRatio

The spring's damping ratio: 0 oscillates forever, 1 settles without overshoot.

```csharp
public float DampingRatio { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_JointDescription_EnableLimit"></a> EnableLimit

Whether [Lower](MicroEngine.Physics.JointDescription.md#MicroEngine_Physics_JointDescription_Lower) and [Upper](MicroEngine.Physics.JointDescription.md#MicroEngine_Physics_JointDescription_Upper) bound the joint's coordinate.

```csharp
public bool EnableLimit { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_JointDescription_EnableMotor"></a> EnableMotor

Whether the motor drives the joint.

```csharp
public bool EnableMotor { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_JointDescription_EnableSpring"></a> EnableSpring

Whether the joint is springy: a distance spring, a wheel's suspension, a soft weld, a revolute or prismatic joint pulled back to where it was made.

```csharp
public bool EnableSpring { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_JointDescription_Hertz"></a> Hertz

The spring's stiffness as a frequency in cycles per second (0 with [EnableSpring](MicroEngine.Physics.JointDescription.md#MicroEngine_Physics_JointDescription_EnableSpring): no spring force).

```csharp
public float Hertz { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_JointDescription_Kind"></a> Kind

What the joint constrains.

```csharp
public JointKind Kind { get; init; }
```

#### Property Value

 [JointKind](MicroEngine.Physics.JointKind.md)

### <a id="MicroEngine_Physics_JointDescription_Length"></a> Length

A distance joint's rest length; negative (the factories' default) for the distance between the anchors when it is created.

```csharp
public float Length { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_JointDescription_Lower"></a> Lower

The lower limit: radians (revolute), translation (prismatic, wheel) or the shortest length (distance).

```csharp
public float Lower { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_JointDescription_MaxForce"></a> MaxForce

The most force a target joint's spring applies.

```csharp
public float MaxForce { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_JointDescription_MaxMotorForce"></a> MaxMotorForce

The most the motor applies to reach its speed: a torque (revolute, wheel) or a force (prismatic, distance).

```csharp
public float MaxMotorForce { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_JointDescription_MotorSpeed"></a> MotorSpeed

The speed the motor aims for: radians per second (revolute, wheel) or world units per second (prismatic, distance).

```csharp
public float MotorSpeed { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_JointDescription_TargetPoint"></a> TargetPoint

A target joint's world point ([SetJointTarget](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_SetJointTarget_MicroEngine_Physics_JointId_System_Numerics_Vector2_) moves it).

```csharp
public Vector2 TargetPoint { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_JointDescription_Upper"></a> Upper

The upper limit: radians (revolute), translation (prismatic, wheel) or the longest length (distance).

```csharp
public float Upper { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Physics_JointDescription_Distance_MicroEngine_Physics_BodyId_MicroEngine_Physics_BodyId_System_Numerics_Vector2_System_Numerics_Vector2_"></a> Distance\(BodyId, BodyId, Vector2, Vector2\)

A rod between a point of each body, as long as they are apart now. Add [EnableSpring](MicroEngine.Physics.JointDescription.md#MicroEngine_Physics_JointDescription_EnableSpring) for a spring.

```csharp
public static JointDescription Distance(BodyId a, BodyId b, Vector2 anchorA, Vector2 anchorB)
```

#### Parameters

`a` [BodyId](MicroEngine.Physics.BodyId.md)

`b` [BodyId](MicroEngine.Physics.BodyId.md)

`anchorA` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`anchorB` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [JointDescription](MicroEngine.Physics.JointDescription.md)

### <a id="MicroEngine_Physics_JointDescription_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_JointDescription_Equals_MicroEngine_Physics_JointDescription_"></a> Equals\(JointDescription\)

```csharp
public bool Equals(JointDescription other)
```

#### Parameters

`other` [JointDescription](MicroEngine.Physics.JointDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_JointDescription_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_JointDescription_Prismatic_MicroEngine_Physics_BodyId_MicroEngine_Physics_BodyId_System_Numerics_Vector2_System_Numerics_Vector2_"></a> Prismatic\(BodyId, BodyId, Vector2, Vector2\)

A slider through the world point <code class="paramref">anchor</code> along the world direction <code class="paramref">axis</code>.

```csharp
public static JointDescription Prismatic(BodyId a, BodyId b, Vector2 anchor, Vector2 axis)
```

#### Parameters

`a` [BodyId](MicroEngine.Physics.BodyId.md)

`b` [BodyId](MicroEngine.Physics.BodyId.md)

`anchor` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`axis` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [JointDescription](MicroEngine.Physics.JointDescription.md)

### <a id="MicroEngine_Physics_JointDescription_Revolute_MicroEngine_Physics_BodyId_MicroEngine_Physics_BodyId_System_Numerics_Vector2_"></a> Revolute\(BodyId, BodyId, Vector2\)

A pin at the world point <code class="paramref">anchor</code>.

```csharp
public static JointDescription Revolute(BodyId a, BodyId b, Vector2 anchor)
```

#### Parameters

`a` [BodyId](MicroEngine.Physics.BodyId.md)

`b` [BodyId](MicroEngine.Physics.BodyId.md)

`anchor` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [JointDescription](MicroEngine.Physics.JointDescription.md)

### <a id="MicroEngine_Physics_JointDescription_Rope_MicroEngine_Physics_BodyId_MicroEngine_Physics_BodyId_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_"></a> Rope\(BodyId, BodyId, Vector2, Vector2, float\)

A rope between a point of each body: slack up to <code class="paramref">maxLength</code>, taut beyond.

```csharp
public static JointDescription Rope(BodyId a, BodyId b, Vector2 anchorA, Vector2 anchorB, float maxLength)
```

#### Parameters

`a` [BodyId](MicroEngine.Physics.BodyId.md)

`b` [BodyId](MicroEngine.Physics.BodyId.md)

`anchorA` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`anchorB` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`maxLength` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [JointDescription](MicroEngine.Physics.JointDescription.md)

### <a id="MicroEngine_Physics_JointDescription_Target_MicroEngine_Physics_BodyId_System_Numerics_Vector2_"></a> Target\(BodyId, Vector2\)

A spring that pulls the world point <code class="paramref">anchor</code> of <code class="paramref">body</code> toward a target, which starts there.

```csharp
public static JointDescription Target(BodyId body, Vector2 anchor)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`anchor` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [JointDescription](MicroEngine.Physics.JointDescription.md)

### <a id="MicroEngine_Physics_JointDescription_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Physics_JointDescription_Validate"></a> Validate\(\)

Throws when the description is invalid whatever the backend declares.

```csharp
public void Validate()
```

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

An undefined kind, a value that is not finite, a negative frequency, damping, force or length limit, or a prismatic or wheel joint without an axis.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

Both bodies are the same, or the lower limit is above the upper one.

### <a id="MicroEngine_Physics_JointDescription_ValidateLimit_System_Single_System_Single_"></a> ValidateLimit\(float, float\)

Throws when a limit is not finite or its lower bound is above its upper one.

```csharp
public static void ValidateLimit(float lower, float upper)
```

#### Parameters

`lower` [float](https://learn.microsoft.com/dotnet/api/system.single)

`upper` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_JointDescription_Weld_MicroEngine_Physics_BodyId_MicroEngine_Physics_BodyId_System_Numerics_Vector2_"></a> Weld\(BodyId, BodyId, Vector2\)

Glue at the world point <code class="paramref">anchor</code>.

```csharp
public static JointDescription Weld(BodyId a, BodyId b, Vector2 anchor)
```

#### Parameters

`a` [BodyId](MicroEngine.Physics.BodyId.md)

`b` [BodyId](MicroEngine.Physics.BodyId.md)

`anchor` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [JointDescription](MicroEngine.Physics.JointDescription.md)

### <a id="MicroEngine_Physics_JointDescription_Wheel_MicroEngine_Physics_BodyId_MicroEngine_Physics_BodyId_System_Numerics_Vector2_System_Numerics_Vector2_"></a> Wheel\(BodyId, BodyId, Vector2, Vector2\)

A wheel centred at the world point <code class="paramref">anchor</code> whose suspension travels along <code class="paramref">axis</code> (usually the world's up).

```csharp
public static JointDescription Wheel(BodyId chassis, BodyId wheel, Vector2 anchor, Vector2 axis)
```

#### Parameters

`chassis` [BodyId](MicroEngine.Physics.BodyId.md)

`wheel` [BodyId](MicroEngine.Physics.BodyId.md)

`anchor` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`axis` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [JointDescription](MicroEngine.Physics.JointDescription.md)

## Operators

### <a id="MicroEngine_Physics_JointDescription_op_Equality_MicroEngine_Physics_JointDescription_MicroEngine_Physics_JointDescription_"></a> operator ==\(JointDescription, JointDescription\)

```csharp
public static bool operator ==(JointDescription left, JointDescription right)
```

#### Parameters

`left` [JointDescription](MicroEngine.Physics.JointDescription.md)

`right` [JointDescription](MicroEngine.Physics.JointDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_JointDescription_op_Inequality_MicroEngine_Physics_JointDescription_MicroEngine_Physics_JointDescription_"></a> operator \!=\(JointDescription, JointDescription\)

```csharp
public static bool operator !=(JointDescription left, JointDescription right)
```

#### Parameters

`left` [JointDescription](MicroEngine.Physics.JointDescription.md)

`right` [JointDescription](MicroEngine.Physics.JointDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

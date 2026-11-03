# Struct BodyDescription

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

What [CreateBody](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_CreateBody_MicroEngine_Scenes_EntityId_MicroEngine_Physics_BodyDescription__) creates.

```csharp
public readonly struct BodyDescription : IEquatable<BodyDescription>
```

#### Implements

[IEquatable<BodyDescription\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Physics_BodyDescription__ctor_MicroEngine_Physics_BodyKind_System_Numerics_Vector2_"></a> BodyDescription\(BodyKind, Vector2\)

What [CreateBody](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_CreateBody_MicroEngine_Scenes_EntityId_MicroEngine_Physics_BodyDescription__) creates.

```csharp
public BodyDescription(BodyKind Kind, Vector2 Position)
```

#### Parameters

`Kind` [BodyKind](MicroEngine.Physics.BodyKind.md)

How the body moves.

`Position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Its origin in world units; its shapes are placed relative to it.

## Properties

### <a id="MicroEngine_Physics_BodyDescription_Bullet"></a> Bullet

Continuous collision for a fast dynamic body; needs [ContinuousCollision](MicroEngine.Physics.PhysicsCapabilities.md).

```csharp
public bool Bullet { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_BodyDescription_Kind"></a> Kind

How the body moves.

```csharp
public BodyKind Kind { get; init; }
```

#### Property Value

 [BodyKind](MicroEngine.Physics.BodyKind.md)

### <a id="MicroEngine_Physics_BodyDescription_Position"></a> Position

Its origin in world units; its shapes are placed relative to it.

```csharp
public Vector2 Position { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_BodyDescription_PushesBodies"></a> PushesBodies

A kinematic body that, when moved with [Move](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__), is not blocked: it pushes the other
kinematic bodies in its way and carries those resting on it (moving platforms, closing doors). Needs
[MovingSolids](MicroEngine.Physics.PhysicsCapabilities.md); only kinematic bodies can push.

```csharp
public bool PushesBodies { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_BodyDescription_RequiredCapabilities"></a> RequiredCapabilities

The capabilities this description needs beyond the common contract.

```csharp
public PhysicsCapabilities RequiredCapabilities { get; }
```

#### Property Value

 [PhysicsCapabilities](MicroEngine.Physics.PhysicsCapabilities.md)

### <a id="MicroEngine_Physics_BodyDescription_Rotation"></a> Rotation

The body's angle in radians; anything but 0 needs [Rotation](MicroEngine.Physics.PhysicsCapabilities.md).

```csharp
public float Rotation { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Physics_BodyDescription_Deconstruct_MicroEngine_Physics_BodyKind__System_Numerics_Vector2__"></a> Deconstruct\(out BodyKind, out Vector2\)

```csharp
public void Deconstruct(out BodyKind Kind, out Vector2 Position)
```

#### Parameters

`Kind` [BodyKind](MicroEngine.Physics.BodyKind.md)

`Position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_BodyDescription_Dynamic_System_Numerics_Vector2_"></a> Dynamic\(Vector2\)

A dynamic body at <code class="paramref">position</code> (needs [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md)).

```csharp
public static BodyDescription Dynamic(Vector2 position)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [BodyDescription](MicroEngine.Physics.BodyDescription.md)

### <a id="MicroEngine_Physics_BodyDescription_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_BodyDescription_Equals_MicroEngine_Physics_BodyDescription_"></a> Equals\(BodyDescription\)

```csharp
public bool Equals(BodyDescription other)
```

#### Parameters

`other` [BodyDescription](MicroEngine.Physics.BodyDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_BodyDescription_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_BodyDescription_Kinematic_System_Numerics_Vector2_"></a> Kinematic\(Vector2\)

A kinematic body at <code class="paramref">position</code>.

```csharp
public static BodyDescription Kinematic(Vector2 position)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [BodyDescription](MicroEngine.Physics.BodyDescription.md)

### <a id="MicroEngine_Physics_BodyDescription_Static_System_Numerics_Vector2_"></a> Static\(Vector2\)

A static body at <code class="paramref">position</code>.

```csharp
public static BodyDescription Static(Vector2 position = default)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [BodyDescription](MicroEngine.Physics.BodyDescription.md)

### <a id="MicroEngine_Physics_BodyDescription_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Physics_BodyDescription_Validate"></a> Validate\(\)

Throws when the description is invalid whatever the backend declares.

```csharp
public void Validate()
```

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The kind is undefined, or the position or rotation is not finite.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

A static or dynamic body pushes bodies (only kinematic bodies can).

## Operators

### <a id="MicroEngine_Physics_BodyDescription_op_Equality_MicroEngine_Physics_BodyDescription_MicroEngine_Physics_BodyDescription_"></a> operator ==\(BodyDescription, BodyDescription\)

```csharp
public static bool operator ==(BodyDescription left, BodyDescription right)
```

#### Parameters

`left` [BodyDescription](MicroEngine.Physics.BodyDescription.md)

`right` [BodyDescription](MicroEngine.Physics.BodyDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_BodyDescription_op_Inequality_MicroEngine_Physics_BodyDescription_MicroEngine_Physics_BodyDescription_"></a> operator \!=\(BodyDescription, BodyDescription\)

```csharp
public static bool operator !=(BodyDescription left, BodyDescription right)
```

#### Parameters

`left` [BodyDescription](MicroEngine.Physics.BodyDescription.md)

`right` [BodyDescription](MicroEngine.Physics.BodyDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

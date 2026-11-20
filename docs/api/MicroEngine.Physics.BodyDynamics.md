# Struct BodyDynamics

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

How the solver treats a dynamic body ([Dynamics](MicroEngine.Physics.BodyDescription.md#MicroEngine_Physics_BodyDescription_Dynamics),
[SetDynamics](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_SetDynamics_MicroEngine_Physics_BodyId_MicroEngine_Physics_BodyDynamics__)). <code>default</code> is the default body: no damping, full gravity, free to
rotate, allowed to sleep.

```csharp
public readonly struct BodyDynamics : IEquatable<BodyDynamics>
```

#### Implements

[IEquatable<BodyDynamics\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Properties

### <a id="MicroEngine_Physics_BodyDynamics_AllowSleep"></a> AllowSleep

Whether the body may fall asleep when it rests (true by default); a sleeping body costs nothing until something touches it.

```csharp
public bool AllowSleep { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_BodyDynamics_AngularDamping"></a> AngularDamping

How fast the angular velocity decays, per second (0: never).

```csharp
public float AngularDamping { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_BodyDynamics_FixedRotation"></a> FixedRotation

Whether the body never rotates (a character made of a dynamic body, a crate that must stay upright).

```csharp
public bool FixedRotation { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_BodyDynamics_GravityScale"></a> GravityScale

The share of the world's gravity the body feels: 1 by default, 0 floats, negative rises.

```csharp
public float GravityScale { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_BodyDynamics_LinearDamping"></a> LinearDamping

How fast the linear velocity decays, per second (0: never); for drag and top-down bodies.

```csharp
public float LinearDamping { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Physics_BodyDynamics_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_BodyDynamics_Equals_MicroEngine_Physics_BodyDynamics_"></a> Equals\(BodyDynamics\)

```csharp
public bool Equals(BodyDynamics other)
```

#### Parameters

`other` [BodyDynamics](MicroEngine.Physics.BodyDynamics.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_BodyDynamics_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_BodyDynamics_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Physics_BodyDynamics_Validate"></a> Validate\(\)

Throws when a value is out of range.

```csharp
public void Validate()
```

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A damping that is negative or not finite, or a gravity scale that is not finite.

## Operators

### <a id="MicroEngine_Physics_BodyDynamics_op_Equality_MicroEngine_Physics_BodyDynamics_MicroEngine_Physics_BodyDynamics_"></a> operator ==\(BodyDynamics, BodyDynamics\)

```csharp
public static bool operator ==(BodyDynamics left, BodyDynamics right)
```

#### Parameters

`left` [BodyDynamics](MicroEngine.Physics.BodyDynamics.md)

`right` [BodyDynamics](MicroEngine.Physics.BodyDynamics.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_BodyDynamics_op_Inequality_MicroEngine_Physics_BodyDynamics_MicroEngine_Physics_BodyDynamics_"></a> operator \!=\(BodyDynamics, BodyDynamics\)

```csharp
public static bool operator !=(BodyDynamics left, BodyDynamics right)
```

#### Parameters

`left` [BodyDynamics](MicroEngine.Physics.BodyDynamics.md)

`right` [BodyDynamics](MicroEngine.Physics.BodyDynamics.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

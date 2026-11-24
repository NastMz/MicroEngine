# Class TopDownMotion

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics](../manual/physics.md)  

How a [TopDownController2D](MicroEngine.Physics.TopDownController2D.md) moves, in the physics world's units and seconds.

```csharp
public sealed record TopDownMotion : IEquatable<TopDownMotion>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TopDownMotion](MicroEngine.Physics.TopDownMotion.md)

#### Implements

[IEquatable<TopDownMotion\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Physics_TopDownMotion__ctor"></a> TopDownMotion\(\)

```csharp
public TopDownMotion()
```

## Properties

### <a id="MicroEngine_Physics_TopDownMotion_Acceleration"></a> Acceleration

How fast the velocity grows toward the intent, in units per second squared; infinite (the default) reaches it at once.

```csharp
public float Acceleration { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_TopDownMotion_Deceleration"></a> Deceleration

How fast the velocity falls to zero with no intent, in units per second squared; infinite (the default) stops at once.

```csharp
public float Deceleration { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_TopDownMotion_MoveSpeed"></a> MoveSpeed

The speed at full intent, in units per second.

```csharp
public float MoveSpeed { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Physics_TopDownMotion__Clone__"></a> <Clone\>$\(\)

```csharp
public TopDownMotion <Clone>$()
```

#### Returns

 [TopDownMotion](MicroEngine.Physics.TopDownMotion.md)

### <a id="MicroEngine_Physics_TopDownMotion_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_TopDownMotion_Equals_MicroEngine_Physics_TopDownMotion_"></a> Equals\(TopDownMotion?\)

```csharp
public bool Equals(TopDownMotion? other)
```

#### Parameters

`other` [TopDownMotion](MicroEngine.Physics.TopDownMotion.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_TopDownMotion_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_TopDownMotion_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Physics_TopDownMotion_Validate"></a> Validate\(\)

Throws when a value is out of range.

```csharp
public void Validate()
```

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A negative or not-a-number value, or a speed that is not finite.

## Operators

### <a id="MicroEngine_Physics_TopDownMotion_op_Equality_MicroEngine_Physics_TopDownMotion_MicroEngine_Physics_TopDownMotion_"></a> operator ==\(TopDownMotion?, TopDownMotion?\)

```csharp
public static bool operator ==(TopDownMotion? left, TopDownMotion? right)
```

#### Parameters

`left` [TopDownMotion](MicroEngine.Physics.TopDownMotion.md)?

`right` [TopDownMotion](MicroEngine.Physics.TopDownMotion.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_TopDownMotion_op_Inequality_MicroEngine_Physics_TopDownMotion_MicroEngine_Physics_TopDownMotion_"></a> operator \!=\(TopDownMotion?, TopDownMotion?\)

```csharp
public static bool operator !=(TopDownMotion? left, TopDownMotion? right)
```

#### Parameters

`left` [TopDownMotion](MicroEngine.Physics.TopDownMotion.md)?

`right` [TopDownMotion](MicroEngine.Physics.TopDownMotion.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

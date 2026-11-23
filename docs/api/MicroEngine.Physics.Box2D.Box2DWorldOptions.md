# Class Box2DWorldOptions

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics.Box2D](MicroEngine.Physics.Box2D.md)  
Assembly: MicroEngine.Physics.Box2D.dll  
Guide: [Choosing a backend](../manual/physics.md#choosing-a-backend)  

How a [Box2DPhysicsWorld2D](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md) maps the game's world units onto Box2D, which works in metres with y up.
The defaults are Box2D's own: one world unit is a metre and y grows upward. A pixel-art game that keeps its pixel,
y-down space (the engine's 2D space) sets [UnitsPerMeter](MicroEngine.Physics.Box2D.Box2DWorldOptions.md#MicroEngine_Physics_Box2D_Box2DWorldOptions_UnitsPerMeter) (for example 32) and [YDown](MicroEngine.Physics.Box2D.Box2DWorldOptions.md#MicroEngine_Physics_Box2D_Box2DWorldOptions_YDown); the
world then takes and returns pixels and every conversion happens inside it.

```csharp
public sealed record Box2DWorldOptions : IEquatable<Box2DWorldOptions>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Box2DWorldOptions](MicroEngine.Physics.Box2D.Box2DWorldOptions.md)

#### Implements

[IEquatable<Box2DWorldOptions\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Physics_Box2D_Box2DWorldOptions__ctor"></a> Box2DWorldOptions\(\)

```csharp
public Box2DWorldOptions()
```

## Properties

### <a id="MicroEngine_Physics_Box2D_Box2DWorldOptions_EnableSleep"></a> EnableSleep

Whether resting dynamic bodies may sleep (true by default); it saves work and changes no result.

```csharp
public bool EnableSleep { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_Box2D_Box2DWorldOptions_Gravity"></a> Gravity

Gravity in world units per second squared; null for 9.81 metres per second squared downward (against
[Up](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Up)).

```csharp
public Vector2? Gravity { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?

### <a id="MicroEngine_Physics_Box2D_Box2DWorldOptions_ImpactSpeed"></a> ImpactSpeed

How fast two shapes must approach each other for their contact to be reported as an impact
([ContactImpacts](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_ContactImpacts)), in world units per second; null for Box2D's default, one metre
per second.

```csharp
public float? ImpactSpeed { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="MicroEngine_Physics_Box2D_Box2DWorldOptions_NativeAccess"></a> NativeAccess

Whether the game may read the world's Box2D ids ([NativeIds](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md#MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_NativeIds)) to call Box2D's
own API. False by default: it is a decision with costs ([Box2DNative](MicroEngine.Physics.Box2D.Box2DNative.md)), made where the world is
made.

```csharp
public bool NativeAccess { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_Box2D_Box2DWorldOptions_SubSteps"></a> SubSteps

Box2D sub-steps per [Step](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Step_System_Single_) (4 by default: Box2D's recommendation).

```csharp
public int SubSteps { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_Box2D_Box2DWorldOptions_UnitsPerMeter"></a> UnitsPerMeter

World units in one metre (positive). Box2D is tuned for moving objects of 0.1 to 10 metres.

```csharp
public float UnitsPerMeter { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_Box2D_Box2DWorldOptions_YDown"></a> YDown

Whether y grows downward in world units (the engine's 2D space); [Up](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Up) is then (0, -1).

```csharp
public bool YDown { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Physics_Box2D_Box2DWorldOptions__Clone__"></a> <Clone\>$\(\)

```csharp
public Box2DWorldOptions <Clone>$()
```

#### Returns

 [Box2DWorldOptions](MicroEngine.Physics.Box2D.Box2DWorldOptions.md)

### <a id="MicroEngine_Physics_Box2D_Box2DWorldOptions_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_Box2D_Box2DWorldOptions_Equals_MicroEngine_Physics_Box2D_Box2DWorldOptions_"></a> Equals\(Box2DWorldOptions?\)

```csharp
public bool Equals(Box2DWorldOptions? other)
```

#### Parameters

`other` [Box2DWorldOptions](MicroEngine.Physics.Box2D.Box2DWorldOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_Box2D_Box2DWorldOptions_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_Box2D_Box2DWorldOptions_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Physics_Box2D_Box2DWorldOptions_Validate"></a> Validate\(\)

Throws when an option is out of range.

```csharp
public void Validate()
```

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A scale that is not positive and finite, a gravity that is not finite, fewer than one sub-step, or an impact speed that is negative or not finite.

## Operators

### <a id="MicroEngine_Physics_Box2D_Box2DWorldOptions_op_Equality_MicroEngine_Physics_Box2D_Box2DWorldOptions_MicroEngine_Physics_Box2D_Box2DWorldOptions_"></a> operator ==\(Box2DWorldOptions?, Box2DWorldOptions?\)

```csharp
public static bool operator ==(Box2DWorldOptions? left, Box2DWorldOptions? right)
```

#### Parameters

`left` [Box2DWorldOptions](MicroEngine.Physics.Box2D.Box2DWorldOptions.md)?

`right` [Box2DWorldOptions](MicroEngine.Physics.Box2D.Box2DWorldOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_Box2D_Box2DWorldOptions_op_Inequality_MicroEngine_Physics_Box2D_Box2DWorldOptions_MicroEngine_Physics_Box2D_Box2DWorldOptions_"></a> operator \!=\(Box2DWorldOptions?, Box2DWorldOptions?\)

```csharp
public static bool operator !=(Box2DWorldOptions? left, Box2DWorldOptions? right)
```

#### Parameters

`left` [Box2DWorldOptions](MicroEngine.Physics.Box2D.Box2DWorldOptions.md)?

`right` [Box2DWorldOptions](MicroEngine.Physics.Box2D.Box2DWorldOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

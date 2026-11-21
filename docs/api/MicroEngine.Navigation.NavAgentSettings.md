# Class NavAgentSettings

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Navigation](MicroEngine.Navigation.md)  
Assembly: MicroEngine.Navigation.dll  
Guide: [Navigation](../guide/navigation.md)  

How an agent of [NavAgents](MicroEngine.Navigation.NavAgents.md) moves, in the grid's world units and seconds.

```csharp
public sealed record NavAgentSettings : IEquatable<NavAgentSettings>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[NavAgentSettings](MicroEngine.Navigation.NavAgentSettings.md)

#### Implements

[IEquatable<NavAgentSettings\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Navigation_NavAgentSettings__ctor_System_Single_System_Single_"></a> NavAgentSettings\(float, float\)

Settings for a body of <code class="paramref">radius</code> that walks at <code class="paramref">speed</code>.

```csharp
public NavAgentSettings(float radius, float speed)
```

#### Parameters

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`speed` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A value is out of range.

## Properties

### <a id="MicroEngine_Navigation_NavAgentSettings_Acceleration"></a> Acceleration

How fast its velocity changes, in units per second squared; infinite (the default) changes it at once.

```csharp
public float Acceleration { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The value is not positive.

### <a id="MicroEngine_Navigation_NavAgentSettings_ArrivalTolerance"></a> ArrivalTolerance

How near its destination counts as there. 0 (the default) stops it on the point itself.

```csharp
public float ArrivalTolerance { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The value is negative or not finite.

### <a id="MicroEngine_Navigation_NavAgentSettings_Avoidance"></a> Avoidance

How strongly it turns aside from bodies it is about to run into ([Avoid](MicroEngine.Navigation.Steering.md#MicroEngine_Navigation_Steering_Avoid_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_System_Single_)): 1 by default, 0 for none.

```csharp
public float Avoidance { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The value is negative or not finite.

### <a id="MicroEngine_Navigation_NavAgentSettings_Collides"></a> Collides

Whether it takes part in the crowd (the default): it keeps apart from the others, never overlaps them and
they go around it. False for what passes through bodies (a flyer, a ghost, a projectile that homes).

```csharp
public bool Collides { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavAgentSettings_MovedByGame"></a> MovedByGame

Whether the game moves the body itself (a character controller, a physics body, an animation): the system
then only steers. Each step the game moves its body by [DesiredVelocity](MicroEngine.Navigation.NavAgent.md#MicroEngine_Navigation_NavAgent_DesiredVelocity) and tells the
agent where it ended ([Position](MicroEngine.Navigation.NavAgent.md#MicroEngine_Navigation_NavAgent_Position)). Other agents go around it and never push it. False by
default: the system moves the agent along the grid ([Slide](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_Slide_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_)).

```csharp
public bool MovedByGame { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavAgentSettings_Radius"></a> Radius

The body's radius: other bodies are kept that far from its centre, walls too (as a square of that half-side,
like [FindPath](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_FindPath_MicroEngine_Navigation_PathFinder_System_Numerics_Vector2_System_Numerics_Vector2_System_Collections_Generic_List_System_Numerics_Vector2__System_Single_)), and a body wider than a cell is led only where it fits. Zero or more.

```csharp
public float Radius { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The value is negative or not finite.

### <a id="MicroEngine_Navigation_NavAgentSettings_Separation"></a> Separation

How strongly it steers away from bodies that are close ([Separate](MicroEngine.Navigation.Steering.md#MicroEngine_Navigation_Steering_Separate_System_Numerics_Vector2_System_Single_System_Numerics_Vector2_System_Single_System_Single_)): 1 by default, 0 for none.

```csharp
public float Separation { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The value is negative or not finite.

### <a id="MicroEngine_Navigation_NavAgentSettings_SlowedByCost"></a> SlowedByCost

Whether costly ground slows it down: its speed is divided by the cost of the cell it is in
([SetCost](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_SetCost_System_Int32_System_Int32_System_Single_)), so a cell that costs 3 is walked at a third of the speed. False by default.

```csharp
public bool SlowedByCost { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavAgentSettings_SlowingDistance"></a> SlowingDistance

The distance from its destination at which it starts to slow down; 0 (the default) walks at full speed to the end.

```csharp
public float SlowingDistance { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The value is negative or not finite.

### <a id="MicroEngine_Navigation_NavAgentSettings_Speed"></a> Speed

The speed it walks at, in units per second. Positive.

```csharp
public float Speed { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The value is not positive and finite.

### <a id="MicroEngine_Navigation_NavAgentSettings_Weight"></a> Weight

How hard it is to push: when two bodies would overlap each gives way in proportion to the other's weight, and
a body that stands (it arrived, or has no order) holds its ground eight times as hard as one that walks. 1 by
default; a very large weight makes a body others go around. Positive.

```csharp
public float Weight { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The value is not positive and finite.

## Methods

### <a id="MicroEngine_Navigation_NavAgentSettings__Clone__"></a> <Clone\>$\(\)

```csharp
public NavAgentSettings <Clone>$()
```

#### Returns

 [NavAgentSettings](MicroEngine.Navigation.NavAgentSettings.md)

### <a id="MicroEngine_Navigation_NavAgentSettings_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavAgentSettings_Equals_MicroEngine_Navigation_NavAgentSettings_"></a> Equals\(NavAgentSettings?\)

```csharp
public bool Equals(NavAgentSettings? other)
```

#### Parameters

`other` [NavAgentSettings](MicroEngine.Navigation.NavAgentSettings.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavAgentSettings_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Navigation_NavAgentSettings_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Navigation_NavAgentSettings_op_Equality_MicroEngine_Navigation_NavAgentSettings_MicroEngine_Navigation_NavAgentSettings_"></a> operator ==\(NavAgentSettings?, NavAgentSettings?\)

```csharp
public static bool operator ==(NavAgentSettings? left, NavAgentSettings? right)
```

#### Parameters

`left` [NavAgentSettings](MicroEngine.Navigation.NavAgentSettings.md)?

`right` [NavAgentSettings](MicroEngine.Navigation.NavAgentSettings.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavAgentSettings_op_Inequality_MicroEngine_Navigation_NavAgentSettings_MicroEngine_Navigation_NavAgentSettings_"></a> operator \!=\(NavAgentSettings?, NavAgentSettings?\)

```csharp
public static bool operator !=(NavAgentSettings? left, NavAgentSettings? right)
```

#### Parameters

`left` [NavAgentSettings](MicroEngine.Navigation.NavAgentSettings.md)?

`right` [NavAgentSettings](MicroEngine.Navigation.NavAgentSettings.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

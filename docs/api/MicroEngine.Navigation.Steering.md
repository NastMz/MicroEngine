# Class Steering

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Navigation](MicroEngine.Navigation.md)  
Assembly: MicroEngine.Navigation.dll  
Guide: [Navigation](../guide/navigation.md)  

The steering rules [NavAgents](MicroEngine.Navigation.NavAgents.md) is made of, for games that move things their own way (objects of an
entity store, creatures that fly, a platformer's enemies): each takes where things are and returns a velocity or
a push to add to it. They keep nothing between calls and use only sums, products and square roots, so the same
inputs give the same result on every machine.

```csharp
public static class Steering
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Steering](MicroEngine.Navigation.Steering.md)

## Examples

```csharp
// A fish that swims to the bait and keeps clear of the others.
var wanted = Steering.Arrive(fish.Position, bait, fish.Speed, slowingDistance: 24);
foreach (var other in nearby)
    wanted += fish.Speed * Steering.Separate(fish.Position, fish.Radius, other.Position, other.Radius, distance: 8);
fish.Velocity = Steering.Limit(wanted, fish.Speed);
```

## Methods

### <a id="MicroEngine_Navigation_Steering_Arrive_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_System_Single_"></a> Arrive\(Vector2, Vector2, float, float\)

The velocity that heads for <code class="paramref">target</code> and slows down over the last
<code class="paramref">slowingDistance</code> units, in proportion to what is left, so the body comes to rest on the
target instead of running through it. With a slowing distance of 0 it is [Seek](MicroEngine.Navigation.Steering.md#MicroEngine_Navigation_Steering_Seek_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_).

```csharp
public static Vector2 Arrive(Vector2 position, Vector2 target, float speed, float slowingDistance)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`target` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`speed` [float](https://learn.microsoft.com/dotnet/api/system.single)

`slowingDistance` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Navigation_Steering_Avoid_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_System_Single_"></a> Avoid\(Vector2, Vector2, float, Vector2, Vector2, float, float\)

The push that turns a moving body aside from one neighbour it is about to run into: zero unless, both keeping
their velocities, the two bodies would touch within <code class="paramref">horizon</code> seconds; otherwise a direction
away from where the neighbour will be at their closest, longer the sooner that is (up to 1). Two bodies that
meet head-on each turn to their own right, so they pass. Add the pushes of the neighbours, multiply by a speed
and add the result to the velocity wanted.

```csharp
public static Vector2 Avoid(Vector2 position, Vector2 velocity, float radius, Vector2 other, Vector2 otherVelocity, float otherRadius, float horizon)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The body's centre.

`velocity` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The velocity the body wants.

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

The body's radius.

`other` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The neighbour's centre.

`otherVelocity` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The neighbour's velocity.

`otherRadius` [float](https://learn.microsoft.com/dotnet/api/system.single)

The neighbour's radius.

`horizon` [float](https://learn.microsoft.com/dotnet/api/system.single)

How many seconds ahead a meeting matters; positive.

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Navigation_Steering_Flee_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_"></a> Flee\(Vector2, Vector2, float\)

The velocity that heads straight away from <code class="paramref">threat</code> at <code class="paramref">speed</code>; zero when on it.

```csharp
public static Vector2 Flee(Vector2 position, Vector2 threat, float speed)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`threat` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`speed` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Navigation_Steering_Limit_System_Numerics_Vector2_System_Single_"></a> Limit\(Vector2, float\)

<code class="paramref">velocity</code>, shortened to <code class="paramref">speed</code> when it is faster.

```csharp
public static Vector2 Limit(Vector2 velocity, float speed)
```

#### Parameters

`velocity` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`speed` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Navigation_Steering_Seek_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_"></a> Seek\(Vector2, Vector2, float\)

The velocity that heads straight for <code class="paramref">target</code> at <code class="paramref">speed</code>; zero when already there.

```csharp
public static Vector2 Seek(Vector2 position, Vector2 target, float speed)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`target` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`speed` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Navigation_Steering_Separate_System_Numerics_Vector2_System_Single_System_Numerics_Vector2_System_Single_System_Single_"></a> Separate\(Vector2, float, Vector2, float, float\)

The push that keeps a body away from one neighbour: a direction away from it whose length grows from 0, when
the gap between the two bodies is <code class="paramref">distance</code> or more, to 1 when they touch. Add the pushes
of every neighbour, multiply by a speed and add the result to the velocity wanted.

```csharp
public static Vector2 Separate(Vector2 position, float radius, Vector2 other, float otherRadius, float distance)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The body's centre.

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

The body's radius.

`other` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The neighbour's centre.

`otherRadius` [float](https://learn.microsoft.com/dotnet/api/system.single)

The neighbour's radius.

`distance` [float](https://learn.microsoft.com/dotnet/api/system.single)

The gap the bodies try to keep between them; positive.

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

</div>

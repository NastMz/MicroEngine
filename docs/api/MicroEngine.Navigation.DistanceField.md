# Class DistanceField

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Navigation](MicroEngine.Navigation.md)  
Assembly: MicroEngine.Navigation.dll  
Guide: [Navigation](../guide/navigation.md)  

The cost of the cheapest route from every cell of a [NavGrid](MicroEngine.Navigation.NavGrid.md) to a goal, computed once for all of
them: what many agents with the same goal read instead of each searching its own path (a horde after the player,
units sent to one point, creatures fleeing to the nearest exit). Each agent asks for the cell to step to
([Next](MicroEngine.Navigation.DistanceField.md#MicroEngine_Navigation_DistanceField_Next_System_Int32_)); the game computes the field again when the goal moves to another cell or the grid changes
([Version](MicroEngine.Navigation.DistanceField.md#MicroEngine_Navigation_DistanceField_Version)).

```csharp
public sealed class DistanceField
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DistanceField](MicroEngine.Navigation.DistanceField.md)

## Constructors

### <a id="MicroEngine_Navigation_DistanceField__ctor_MicroEngine_Navigation_NavGrid_"></a> DistanceField\(NavGrid\)

A field over <code class="paramref">grid</code>; nothing is known until [Compute](MicroEngine.Navigation.DistanceField.md#MicroEngine_Navigation_DistanceField_Compute_System_Int32_System_Single_).

```csharp
public DistanceField(NavGrid grid)
```

#### Parameters

`grid` [NavGrid](MicroEngine.Navigation.NavGrid.md)

## Properties

### <a id="MicroEngine_Navigation_DistanceField_Grid"></a> Grid

The grid the field measures.

```csharp
public NavGrid Grid { get; }
```

#### Property Value

 [NavGrid](MicroEngine.Navigation.NavGrid.md)

### <a id="MicroEngine_Navigation_DistanceField_Version"></a> Version

The grid's [Version](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_Version) when the field was last computed (-1 before the first time): when it
differs from the grid's, the field describes a level that changed.

```csharp
public int Version { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Navigation_DistanceField_Compute_System_Int32_System_Single_"></a> Compute\(int, float\)

Computes the cost from every cell to the node <code class="paramref">goal</code>.

```csharp
public void Compute(int goal, float maxDistance = Infinity)
```

#### Parameters

`goal` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The goal's node ([NodeAt](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_NodeAt_System_Numerics_Vector2_)). A blocked goal is reached from nowhere.

`maxDistance` [float](https://learn.microsoft.com/dotnet/api/system.single)

Cells further than this cost stay unknown; a limit keeps the work near the goal on large maps.

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">goal</code> is not a node of the grid, or <code class="paramref">maxDistance</code> is negative or not a number.

### <a id="MicroEngine_Navigation_DistanceField_Compute_System_ReadOnlySpan_System_Int32__System_Single_"></a> Compute\(ReadOnlySpan<int\>, float\)

Computes the cost from every cell to the nearest of several <code class="paramref">goals</code> (the exits of a room, every pickup).

```csharp
public void Compute(ReadOnlySpan<int> goals, float maxDistance = Infinity)
```

#### Parameters

`goals` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[int](https://learn.microsoft.com/dotnet/api/system.int32)\>

`maxDistance` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A goal is not a node of the grid, or <code class="paramref">maxDistance</code> is negative or not a number.

### <a id="MicroEngine_Navigation_DistanceField_DistanceAt_System_Int32_"></a> DistanceAt\(int\)

The cost of the cheapest route from <code class="paramref">node</code> to the goal: 0 at a goal, infinity where no route exists or nothing was computed.

```csharp
public float DistanceAt(int node)
```

#### Parameters

`node` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">node</code> is not a node of the grid.

### <a id="MicroEngine_Navigation_DistanceField_Next_System_Int32_"></a> Next\(int\)

The neighbouring node an agent at <code class="paramref">node</code> steps to on its cheapest route: the node itself at a
goal, -1 where no route exists. Walk towards its centre ([CenterOf](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_CenterOf_System_Int32_)).

```csharp
public int Next(int node)
```

#### Parameters

`node` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">node</code> is not a node of the grid.

</div>

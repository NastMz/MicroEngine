# Class PathFinder

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Navigation](MicroEngine.Navigation.md)  
Assembly: MicroEngine.Navigation.dll  
Guide: [Navigation](../guide/navigation.md)  

Finds the cheapest path between two nodes of an [INavGraph](MicroEngine.Navigation.INavGraph.md) with the A* algorithm. A finder keeps the
memory of its searches, so searching again allocates nothing once it has seen a graph of that size; keep one per
game (or one per thread: a finder serves one search at a time). The same question on the same graph always gives
the same path, on every platform.

```csharp
public sealed class PathFinder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PathFinder](MicroEngine.Navigation.PathFinder.md)

## Constructors

### <a id="MicroEngine_Navigation_PathFinder__ctor"></a> PathFinder\(\)

```csharp
public PathFinder()
```

## Properties

### <a id="MicroEngine_Navigation_PathFinder_Cost"></a> Cost

The cost of [Path](MicroEngine.Navigation.PathFinder.md#MicroEngine_Navigation_PathFinder_Path): the sum of its steps.

```csharp
public float Cost { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Navigation_PathFinder_Expanded"></a> Expanded

How many nodes the last search looked at (a measure of its work).

```csharp
public int Expanded { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Navigation_PathFinder_IsSearching"></a> IsSearching

Whether a search begun with [Begin](MicroEngine.Navigation.PathFinder.md#MicroEngine_Navigation_PathFinder_Begin_MicroEngine_Navigation_INavGraph_System_Int32_System_Int32_) has not ended yet.

```csharp
public bool IsSearching { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_PathFinder_MaxExpansions"></a> MaxExpansions

The most nodes a search looks at before it gives up with [Incomplete](MicroEngine.Navigation.PathStatus.md); unlimited by
default. A goal that cannot be reached makes a search look at every node the start reaches: a limit bounds
that cost on large maps.

```csharp
public int MaxExpansions { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The value is not positive.

### <a id="MicroEngine_Navigation_PathFinder_Path"></a> Path

The nodes of the last search's path, from the start to its end, both included. The next search replaces it:
copy what must be kept.

```csharp
public IReadOnlyList<int> Path { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[int](https://learn.microsoft.com/dotnet/api/system.int32)\>

## Methods

### <a id="MicroEngine_Navigation_PathFinder_Begin_MicroEngine_Navigation_INavGraph_System_Int32_System_Int32_"></a> Begin\(INavGraph, int, int\)

Begins a search from <code class="paramref">start</code> to <code class="paramref">goal</code> that [Continue](MicroEngine.Navigation.PathFinder.md#MicroEngine_Navigation_PathFinder_Continue_System_Int32_) advances
a few nodes at a time, so a long search on a large map is spread over several steps of the game instead of
stalling one. It replaces a search in progress. The graph must not change until the search ends: begin again
when it does.

```csharp
public void Begin(INavGraph graph, int start, int goal)
```

#### Parameters

`graph` [INavGraph](MicroEngine.Navigation.INavGraph.md)

`start` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`goal` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">start</code> or <code class="paramref">goal</code> is not a node of the graph.

### <a id="MicroEngine_Navigation_PathFinder_Continue_System_Int32_"></a> Continue\(int\)

Advances the search begun with [Begin](MicroEngine.Navigation.PathFinder.md#MicroEngine_Navigation_PathFinder_Begin_MicroEngine_Navigation_INavGraph_System_Int32_System_Int32_) by at most <code class="paramref">expansions</code> nodes. It returns
[Searching](MicroEngine.Navigation.PathStatus.md) while the answer is not known ([Path](MicroEngine.Navigation.PathFinder.md#MicroEngine_Navigation_PathFinder_Path) is empty until then),
and otherwise how the search ended, with the path in [Path](MicroEngine.Navigation.PathFinder.md#MicroEngine_Navigation_PathFinder_Path): the same path, cost and
[Expanded](MicroEngine.Navigation.PathFinder.md#MicroEngine_Navigation_PathFinder_Expanded) as [Find](MicroEngine.Navigation.PathFinder.md#MicroEngine_Navigation_PathFinder_Find_MicroEngine_Navigation_INavGraph_System_Int32_System_Int32_) gives, however the work was divided. [MaxExpansions](MicroEngine.Navigation.PathFinder.md#MicroEngine_Navigation_PathFinder_MaxExpansions)
bounds the whole search, not one call.

```csharp
public PathStatus Continue(int expansions)
```

#### Parameters

`expansions` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [PathStatus](MicroEngine.Navigation.PathStatus.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

No search is in progress.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">expansions</code> is not positive.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The graph gave a step with a negative cost or to a node it does not have.

### <a id="MicroEngine_Navigation_PathFinder_Find_MicroEngine_Navigation_INavGraph_System_Int32_System_Int32_"></a> Find\(INavGraph, int, int\)

Searches <code class="paramref">graph</code> for the cheapest path from <code class="paramref">start</code> to <code class="paramref">goal</code>
and leaves it in [Path](MicroEngine.Navigation.PathFinder.md#MicroEngine_Navigation_PathFinder_Path). When the goal cannot be reached, or the search stops at
[MaxExpansions](MicroEngine.Navigation.PathFinder.md#MicroEngine_Navigation_PathFinder_MaxExpansions), the path leads as near to the goal as the search got (it may be the start alone).

```csharp
public PathStatus Find(INavGraph graph, int start, int goal)
```

#### Parameters

`graph` [INavGraph](MicroEngine.Navigation.INavGraph.md)

`start` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`goal` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [PathStatus](MicroEngine.Navigation.PathStatus.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">start</code> or <code class="paramref">goal</code> is not a node of the graph.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The graph gave a step with a negative cost or to a node it does not have.

</div>

# Interface INavGraph

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Navigation](MicroEngine.Navigation.md)  
Assembly: MicroEngine.Navigation.dll  
Guide: [Navigation](../guide/navigation.md)  

Space a [PathFinder](MicroEngine.Navigation.PathFinder.md) can search: nodes numbered from 0, the steps that leave each one, and an estimate
of the cost between two of them. [NavGrid](MicroEngine.Navigation.NavGrid.md) (cells) and [NavGraph](MicroEngine.Navigation.NavGraph.md) (waypoints joined by hand)
implement it; a game implements it for a space of its own (hexagons, rooms, a 3D lattice).

```csharp
public interface INavGraph
```

## Properties

### <a id="MicroEngine_Navigation_INavGraph_NodeCount"></a> NodeCount

How many nodes the graph has; nodes are the numbers from 0 to this, excluded.

```csharp
int NodeCount { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Navigation_INavGraph_Estimate_System_Int32_System_Int32_"></a> Estimate\(int, int\)

An estimate of the cost from <code class="paramref">from</code> to <code class="paramref">to</code> that never exceeds the true cost
(0 is always valid and makes the search look in every direction). The closer to the true cost, the fewer nodes
a search visits.

```csharp
float Estimate(int from, int to)
```

#### Parameters

`from` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`to` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Navigation_INavGraph_GetEdges_System_Int32_System_Collections_Generic_List_MicroEngine_Navigation_NavEdge__"></a> GetEdges\(int, List<NavEdge\>\)

Adds to <code class="paramref">edges</code> the steps that leave <code class="paramref">node</code>, always in the same order for the
same graph (the order decides between paths of equal cost).

```csharp
void GetEdges(int node, List<NavEdge> edges)
```

#### Parameters

`node` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`edges` [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[NavEdge](MicroEngine.Navigation.NavEdge.md)\>

</div>

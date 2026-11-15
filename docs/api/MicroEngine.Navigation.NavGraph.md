# Class NavGraph

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Navigation](MicroEngine.Navigation.md)  
Assembly: MicroEngine.Navigation.dll  
Guide: [Navigation](../guide/navigation.md)  

Walkable space as points joined by hand: waypoints along roads, rooms joined by doors, platforms joined by jumps.
Each node has a position; each connection has a cost (the distance between its ends unless told otherwise), a
direction (both ways, or one way for a drop, a jump or a slide) and a tag the game reads to know how to cross it.
A [PathFinder](MicroEngine.Navigation.PathFinder.md) searches it like any [INavGraph](MicroEngine.Navigation.INavGraph.md).

```csharp
public sealed class NavGraph : INavGraph
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[NavGraph](MicroEngine.Navigation.NavGraph.md)

#### Implements

[INavGraph](MicroEngine.Navigation.INavGraph.md)

## Constructors

### <a id="MicroEngine_Navigation_NavGraph__ctor"></a> NavGraph\(\)

```csharp
public NavGraph()
```

## Properties

### <a id="MicroEngine_Navigation_NavGraph_NodeCount"></a> NodeCount

How many nodes the graph has; nodes are the numbers from 0 to this, excluded.

```csharp
public int NodeCount { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Navigation_NavGraph_Version"></a> Version

A number that changes whenever a node or a connection is added, removed, enabled or disabled.

```csharp
public int Version { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Navigation_NavGraph_AddNode_System_Numerics_Vector2_"></a> AddNode\(Vector2\)

Adds a node at <code class="paramref">position</code> and returns its number (0 for the first, then 1...).

```csharp
public int AddNode(Vector2 position)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Navigation_NavGraph_Connect_System_Int32_System_Int32_System_Nullable_System_Single__System_Int32_System_Boolean_"></a> Connect\(int, int, float?, int, bool\)

Joins <code class="paramref">from</code> to <code class="paramref">to</code>, replacing a connection already there.

```csharp
public void Connect(int from, int to, float? cost = null, int tag = 0, bool bothWays = true)
```

#### Parameters

`from` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The node the connection leaves.

`to` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The node it arrives at.

`cost` [float](https://learn.microsoft.com/dotnet/api/system.single)?

What crossing it costs; the distance between the two positions when null. A cost below the distance (a teleporter, a fast road) is fine.

`tag` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Free for the game: how the connection is crossed. [TryGetEdge](MicroEngine.Navigation.NavGraph.md#MicroEngine_Navigation_NavGraph_TryGetEdge_System_Int32_System_Int32_MicroEngine_Navigation_NavEdge__) gives it back for each step of a path.

`bothWays` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the same connection is made from <code class="paramref">to</code> back to <code class="paramref">from</code>; false for a drop or a jump that works in one direction.

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A node is not of the graph, or <code class="paramref">cost</code> is negative or not finite.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

Both ends are the same node.

### <a id="MicroEngine_Navigation_NavGraph_Disconnect_System_Int32_System_Int32_System_Boolean_"></a> Disconnect\(int, int, bool\)

Removes the connection from <code class="paramref">from</code> to <code class="paramref">to</code> (and back, with <code class="paramref">bothWays</code>); false when there was none.

```csharp
public bool Disconnect(int from, int to, bool bothWays = true)
```

#### Parameters

`from` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`to` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`bothWays` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A node is not of the graph.

### <a id="MicroEngine_Navigation_NavGraph_Estimate_System_Int32_System_Int32_"></a> Estimate\(int, int\)

The straight distance between the two nodes times the cheapest cost per unit of distance of any connection
made, so it never exceeds a route's cost, whatever the connections cost.

```csharp
public float Estimate(int from, int to)
```

#### Parameters

`from` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`to` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Navigation_NavGraph_GetEdges_System_Int32_System_Collections_Generic_List_MicroEngine_Navigation_NavEdge__"></a> GetEdges\(int, List<NavEdge\>\)

The connections that leave <code class="paramref">node</code> to enabled nodes, in the order they were made.

```csharp
public void GetEdges(int node, List<NavEdge> edges)
```

#### Parameters

`node` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`edges` [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[NavEdge](MicroEngine.Navigation.NavEdge.md)\>

### <a id="MicroEngine_Navigation_NavGraph_IsEnabled_System_Int32_"></a> IsEnabled\(int\)

Whether paths may pass through <code class="paramref">node</code>.

```csharp
public bool IsEnabled(int node)
```

#### Parameters

`node` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">node</code> is not a node of the graph.

### <a id="MicroEngine_Navigation_NavGraph_Nearest_System_Numerics_Vector2_"></a> Nearest\(Vector2\)

The enabled node closest to <code class="paramref">point</code> (the first added among equals), or -1 when there is none.

```csharp
public int Nearest(Vector2 point)
```

#### Parameters

`point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Navigation_NavGraph_PositionOf_System_Int32_"></a> PositionOf\(int\)

The position <code class="paramref">node</code> was added at.

```csharp
public Vector2 PositionOf(int node)
```

#### Parameters

`node` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">node</code> is not a node of the graph.

### <a id="MicroEngine_Navigation_NavGraph_SetEnabled_System_Int32_System_Boolean_"></a> SetEnabled\(int, bool\)

Closes or reopens <code class="paramref">node</code> without touching its connections: no path enters a disabled node (a
locked room, a platform that fell), though a path may still start from it.

```csharp
public void SetEnabled(int node, bool enabled)
```

#### Parameters

`node` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`enabled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">node</code> is not a node of the graph.

### <a id="MicroEngine_Navigation_NavGraph_TryGetEdge_System_Int32_System_Int32_MicroEngine_Navigation_NavEdge__"></a> TryGetEdge\(int, int, out NavEdge\)

The connection from <code class="paramref">from</code> to <code class="paramref">to</code>, with its cost and tag: what a game reads for each step of a path.

```csharp
public bool TryGetEdge(int from, int to, out NavEdge edge)
```

#### Parameters

`from` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`to` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`edge` [NavEdge](MicroEngine.Navigation.NavEdge.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A node is not of the graph.

</div>

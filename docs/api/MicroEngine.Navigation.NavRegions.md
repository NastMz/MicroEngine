# Class NavRegions

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Navigation](MicroEngine.Navigation.md)  
Assembly: MicroEngine.Navigation.dll  
Guide: [Navigation](../guide/navigation.md)  

What can be reached from where on a [NavGrid](MicroEngine.Navigation.NavGrid.md): the cells joined by some route form a region, and two
places are joined exactly when they are in the same region. It answers without a search whether a target can be
reached ([Connected](MicroEngine.Navigation.NavRegions.md#MicroEngine_Navigation_NavRegions_Connected_System_Int32_System_Int32_)), where to go instead when it cannot ([NearestReachable](MicroEngine.Navigation.NavRegions.md#MicroEngine_Navigation_NavRegions_NearestReachable_System_Int32_System_Int32_)), and
whether a wall about to be built would cut a way that must stay open ([StaysConnected](MicroEngine.Navigation.NavRegions.md#MicroEngine_Navigation_NavRegions_StaysConnected_MicroEngine_RectF_System_Int32_System_Int32_)):
the placement rule of a tower defence. The regions follow the grid: after a change they are measured again by the
first question, so there is nothing to call.

```csharp
public sealed class NavRegions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[NavRegions](MicroEngine.Navigation.NavRegions.md)

## Examples

```csharp
var regions = new NavRegions(grid);
// A click on an island the unit cannot reach sends it to the shore instead.
int goal = regions.NearestReachable(grid.NodeAt(unit.Position), grid.NodeAt(click));
// A tower may be built only where the creeps keep a way from the gate to the keep.
if (regions.StaysConnected(towerArea, gate, keep)) grid.SetBlocked(towerArea, true);
```

## Constructors

### <a id="MicroEngine_Navigation_NavRegions__ctor_MicroEngine_Navigation_NavGrid_System_Single_"></a> NavRegions\(NavGrid, float\)

The regions of <code class="paramref">grid</code> for agents of one size.

```csharp
public NavRegions(NavGrid grid, float radius = 0)
```

#### Parameters

`grid` [NavGrid](MicroEngine.Navigation.NavGrid.md)

The grid.

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

Half the side of the agents' square, in world units: for agents wider than a cell, two rooms joined by a gap
they do not fit through are two regions ([ForAgent](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_ForAgent_System_Single_)). Agents of different widths ask
different regions.

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">radius</code> is negative or not finite.

## Properties

### <a id="MicroEngine_Navigation_NavRegions_Count"></a> Count

How many regions the grid has: 1 when every cell an agent can stand on is joined to every other.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Navigation_NavRegions_Grid"></a> Grid

The grid the regions describe.

```csharp
public NavGrid Grid { get; }
```

#### Property Value

 [NavGrid](MicroEngine.Navigation.NavGrid.md)

### <a id="MicroEngine_Navigation_NavRegions_Radius"></a> Radius

Half the side of the square of the agents the regions are for, in world units.

```csharp
public float Radius { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Navigation_NavRegions_Connected_System_Int32_System_Int32_"></a> Connected\(int, int\)

Whether an agent at node <code class="paramref">a</code> can walk to node <code class="paramref">b</code>.

```csharp
public bool Connected(int a, int b)
```

#### Parameters

`a` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`b` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A node is not of the grid.

### <a id="MicroEngine_Navigation_NavRegions_NearestReachable_System_Int32_System_Int32_"></a> NearestReachable\(int, int\)

Where an agent at node <code class="paramref">from</code> goes when it is sent to node <code class="paramref">to</code>:
<code class="paramref">to</code> itself when it can be reached, otherwise the cell of the agent's region nearest to it in
a straight line (the lowest node among equals), and -1 when the agent can go nowhere. A search to the answer
always finds its goal, so it never has to look at the whole region to learn that a click on an island or
inside a wall cannot be reached.

```csharp
public int NearestReachable(int from, int to)
```

#### Parameters

`from` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`to` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A node is not of the grid.

### <a id="MicroEngine_Navigation_NavRegions_RegionOf_System_Int32_"></a> RegionOf\(int\)

The region of <code class="paramref">node</code>, from 0: regions are numbered in the order of their first cell (row by
row from the top-left), so the same grid always gives the same numbers. For a cell an agent can only leave (a
blocked cell, or one too tight for a wide body pressed against a wall) it is the region of the first cell the
agent can step into, and -1 when there is none. The numbers change when the grid does: compare them, do not
keep them.

```csharp
public int RegionOf(int node)
```

#### Parameters

`node` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">node</code> is not a node of the grid.

### <a id="MicroEngine_Navigation_NavRegions_SizeOf_System_Int32_"></a> SizeOf\(int\)

How many cells <code class="paramref">region</code> has.

```csharp
public int SizeOf(int region)
```

#### Parameters

`region` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">region</code> is not a region of the grid.

### <a id="MicroEngine_Navigation_NavRegions_StaysConnected_MicroEngine_RectF_System_Int32_System_Int32_"></a> StaysConnected\(RectF, int, int\)

Whether nodes <code class="paramref">a</code> and <code class="paramref">b</code> would still be joined if every cell
<code class="paramref">area</code> overlaps were blocked (the cells [SetBlocked](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_SetBlocked_MicroEngine_RectF_System_Boolean_) would block). Nothing
is changed: ask before building. False when they are not joined now, or when the area covers one of them.

```csharp
public bool StaysConnected(RectF area, int a, int b)
```

#### Parameters

`area` [RectF](MicroEngine.RectF.md)

`a` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`b` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A node is not of the grid.

### <a id="MicroEngine_Navigation_NavRegions_StaysConnected_MicroEngine_RectF_System_ReadOnlySpan_System_Int32__"></a> StaysConnected\(RectF, ReadOnlySpan<int\>\)

Whether every one of <code class="paramref">nodes</code> would still be joined to the others if every cell
<code class="paramref">area</code> overlaps were blocked: the gate, the keep and the cell of each creep on the field, so
that a tower shuts nobody in. Nothing is changed. False when they are not all joined now, or when the area
covers one of them (for agents wider than a cell: when a body centred there would touch the new wall). It
looks at the cells reached from the first node, at most once each.

```csharp
public bool StaysConnected(RectF area, ReadOnlySpan<int> nodes)
```

#### Parameters

`area` [RectF](MicroEngine.RectF.md)

`nodes` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[int](https://learn.microsoft.com/dotnet/api/system.int32)\>

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A node is not of the grid.

</div>

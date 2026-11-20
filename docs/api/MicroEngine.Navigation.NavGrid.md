# Class NavGrid

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Navigation](MicroEngine.Navigation.md)  
Assembly: MicroEngine.Navigation.dll  
Guide: [Navigation](../guide/navigation.md)  

Walkable space as a grid of square cells, each walkable or blocked and with a cost to enter it. It is the graph a
[PathFinder](MicroEngine.Navigation.PathFinder.md) searches for tile maps and for any level that can be sampled on a grid; node
<code>y * Width + x</code> is cell (x, y). A grid is plain data the game owns: it is built once (by hand, from a tile map
with [FromTiles](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_FromTiles_MicroEngine_World_TileGrid_MicroEngine_World_TileFlags_), or from a physics world with [FromPhysics](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_FromPhysics_MicroEngine_Physics_IPhysicsWorld2D_MicroEngine_RectF_System_Single_MicroEngine_Physics_QueryFilter__)) and changed when the level
changes (a door closes, a crate is pushed, a bridge falls). It decides nothing about who walks it or why.

```csharp
public sealed class NavGrid : INavGraph
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[NavGrid](MicroEngine.Navigation.NavGrid.md)

#### Implements

[INavGraph](MicroEngine.Navigation.INavGraph.md)

## Constructors

### <a id="MicroEngine_Navigation_NavGrid__ctor_System_Int32_System_Int32_System_Single_System_Numerics_Vector2_"></a> NavGrid\(int, int, float, Vector2\)

A grid of walkable cells that cost 1.

```csharp
public NavGrid(int width, int height, float cellSize = 1, Vector2 origin = default)
```

#### Parameters

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Columns; positive.

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Rows; positive.

`cellSize` [float](https://learn.microsoft.com/dotnet/api/system.single)

The side of a cell in world units (a tile's size in pixels for a tile map); positive.

`origin` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The world position of the grid's top-left corner.

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A size is not positive.

## Properties

### <a id="MicroEngine_Navigation_NavGrid_CellSize"></a> CellSize

The side of a cell, in world units.

```csharp
public float CellSize { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Navigation_NavGrid_Diagonals"></a> Diagonals

How agents step between cells; [NoCornerCutting](MicroEngine.Navigation.DiagonalMovement.md) by default.

```csharp
public DiagonalMovement Diagonals { get; set; }
```

#### Property Value

 [DiagonalMovement](MicroEngine.Navigation.DiagonalMovement.md)

### <a id="MicroEngine_Navigation_NavGrid_Height"></a> Height

Rows.

```csharp
public int Height { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Navigation_NavGrid_NodeCount"></a> NodeCount

How many nodes the graph has; nodes are the numbers from 0 to this, excluded.

```csharp
public int NodeCount { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Navigation_NavGrid_Origin"></a> Origin

The world position of the top-left corner of cell (0, 0).

```csharp
public Vector2 Origin { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Navigation_NavGrid_Version"></a> Version

A number that changes whenever a cell's walkability or cost, or [Diagonals](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_Diagonals), changes: an agent that
remembers the version its path was found for knows when to check it ([IsOpen](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_IsOpen_System_Collections_Generic_IReadOnlyList_System_Int32__System_Int32_System_Single_)) or search again.

```csharp
public int Version { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Navigation_NavGrid_Width"></a> Width

Columns.

```csharp
public int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Navigation_NavGrid_CellAt_System_Numerics_Vector2_"></a> CellAt\(Vector2\)

The cell that holds the world point <code class="paramref">point</code>; it may be outside the grid ([InBounds](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_InBounds_System_Int32_System_Int32_)).

```csharp
public NavCell CellAt(Vector2 point)
```

#### Parameters

`point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [NavCell](MicroEngine.Navigation.NavCell.md)

### <a id="MicroEngine_Navigation_NavGrid_CellOf_System_Int32_"></a> CellOf\(int\)

The cell of <code class="paramref">node</code>.

```csharp
public NavCell CellOf(int node)
```

#### Parameters

`node` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [NavCell](MicroEngine.Navigation.NavCell.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">node</code> is not a node of the grid.

### <a id="MicroEngine_Navigation_NavGrid_CenterOf_System_Int32_"></a> CenterOf\(int\)

The world position of the centre of <code class="paramref">node</code>'s cell.

```csharp
public Vector2 CenterOf(int node)
```

#### Parameters

`node` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">node</code> is not a node of the grid.

### <a id="MicroEngine_Navigation_NavGrid_ClearanceAt_System_Int32_System_Int32_"></a> ClearanceAt\(int, int\)

How much free room surrounds cell (<code class="paramref">x</code>, <code class="paramref">y</code>): 0 for a blocked cell or one
outside the grid, 1 for a walkable cell with a wall (or the grid's edge) beside it, corners included, 2 when
the nearest wall is two cells away, and so on.

```csharp
public int ClearanceAt(int x, int y)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Navigation_NavGrid_CostAt_System_Int32_System_Int32_"></a> CostAt\(int, int\)

What entering cell (<code class="paramref">x</code>, <code class="paramref">y</code>) costs, as a multiple of open ground (1).

```csharp
public float CostAt(int x, int y)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The cell is outside the grid.

### <a id="MicroEngine_Navigation_NavGrid_Estimate_System_Int32_System_Int32_"></a> Estimate\(int, int\)

The cost between two cells over open ground with no wall between them: the least any route can cost.

```csharp
public float Estimate(int from, int to)
```

#### Parameters

`from` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`to` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Navigation_NavGrid_FindPath_MicroEngine_Navigation_PathFinder_System_Numerics_Vector2_System_Numerics_Vector2_System_Collections_Generic_List_System_Numerics_Vector2__System_Single_"></a> FindPath\(PathFinder, Vector2, Vector2, List<Vector2\>, float\)

The route an agent at <code class="paramref">from</code> walks to reach <code class="paramref">to</code> (world points): searches
the cells with <code class="paramref">finder</code> and fills <code class="paramref">waypoints</code> with the points to walk
through in order, straightened as [Smooth](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_Smooth_System_Collections_Generic_IReadOnlyList_System_Int32__System_Collections_Generic_List_System_Numerics_Vector2__System_Single_) does, ending at <code class="paramref">to</code> itself. The cells
stay in [Path](MicroEngine.Navigation.PathFinder.md#MicroEngine_Navigation_PathFinder_Path). When the point cannot be reached (it is in a wall, cut off, or outside
the grid) the route leads to the nearest cell that can; when the agent is outside the grid there is no route.

```csharp
public PathStatus FindPath(PathFinder finder, Vector2 from, Vector2 to, List<Vector2> waypoints, float radius = 0)
```

#### Parameters

`finder` [PathFinder](MicroEngine.Navigation.PathFinder.md)

The finder that searches; its last path becomes this route's cells.

`from` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Where the agent is.

`to` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Where it wants to be.

`waypoints` [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)\>

Replaced by the route; the agent's own position is not in it.

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

Half the side of the agent's square, in world units: straight legs keep that distance from walls, an agent
wider than a cell is led only through passages it fits in ([ForAgent](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_ForAgent_System_Single_)), and the route ends at the
point of the goal's cell nearest <code class="paramref">to</code> where the body touches no wall.

#### Returns

 [PathStatus](MicroEngine.Navigation.PathStatus.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">radius</code> is negative or not finite, or a point is not finite.

### <a id="MicroEngine_Navigation_NavGrid_Fits_System_Int32_System_Int32_System_Single_"></a> Fits\(int, int, float\)

Whether an agent whose square has the half-side <code class="paramref">radius</code> (world units) can stand centred in
cell (<code class="paramref">x</code>, <code class="paramref">y</code>) without touching a blocked cell or leaving the grid. An
agent no wider than a cell fits in every walkable cell.

```csharp
public bool Fits(int x, int y, float radius)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">radius</code> is negative or not finite.

### <a id="MicroEngine_Navigation_NavGrid_ForAgent_System_Single_"></a> ForAgent\(float\)

The grid as an agent whose square has the half-side <code class="paramref">radius</code> (world units) walks it: the
same nodes, with steps only into cells where the whole body fits ([Fits](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_Fits_System_Int32_System_Int32_System_Single_)).
Search it with [Find](MicroEngine.Navigation.PathFinder.md#MicroEngine_Navigation_PathFinder_Find_MicroEngine_Navigation_INavGraph_System_Int32_System_Int32_) when the game moves cell by cell; [FindPath](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_FindPath_MicroEngine_Navigation_PathFinder_System_Numerics_Vector2_System_Numerics_Vector2_System_Collections_Generic_List_System_Numerics_Vector2__System_Single_) does it
by itself. An agent no wider than a cell walks the grid itself, which is what this returns for it. A wider
agent is given whole cells of room on every side: wider than one cell it needs a passage three cells wide,
wider than three cells one of five. A wide agent never cuts a corner. The view follows the grid's changes.

```csharp
public INavGraph ForAgent(float radius)
```

#### Parameters

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [INavGraph](MicroEngine.Navigation.INavGraph.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">radius</code> is negative or not finite.

### <a id="MicroEngine_Navigation_NavGrid_FromPhysics_MicroEngine_Physics_IPhysicsWorld2D_MicroEngine_RectF_System_Single_MicroEngine_Physics_QueryFilter__"></a> FromPhysics\(IPhysicsWorld2D, RectF, float, in QueryFilter\)

A grid of <code class="paramref">cellSize</code> cells over <code class="paramref">bounds</code> of a physics world, for levels made
of shapes instead of tiles: a cell is blocked when a shape <code class="paramref">filter</code> accepts overlaps it. The
grid is a copy: when static shapes move, call [ReadPhysics](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_ReadPhysics_MicroEngine_Physics_IPhysicsWorld2D_MicroEngine_Physics_QueryFilter__) or [SetBlocked](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_SetBlocked_MicroEngine_RectF_System_Boolean_).

```csharp
public static NavGrid FromPhysics(IPhysicsWorld2D world, RectF bounds, float cellSize, in QueryFilter filter = default)
```

#### Parameters

`world` [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md)

`bounds` [RectF](MicroEngine.RectF.md)

`cellSize` [float](https://learn.microsoft.com/dotnet/api/system.single)

`filter` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

#### Returns

 [NavGrid](MicroEngine.Navigation.NavGrid.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The bounds are empty or <code class="paramref">cellSize</code> is not positive.

### <a id="MicroEngine_Navigation_NavGrid_FromTiles_MicroEngine_World_TileGrid_MicroEngine_World_TileFlags_"></a> FromTiles\(TileGrid, TileFlags\)

A grid over a tile map, one cell per tile: tiles with any of the <code class="paramref">blocking</code> flags are
blocked (solid tiles by default; add [Hazard](MicroEngine.World.TileFlags.md) or a custom flag for tiles agents must not
enter). The grid is a copy: when tiles change, call [ReadTiles](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_ReadTiles_MicroEngine_World_ICollisionGrid_MicroEngine_World_TileFlags_) or [SetWalkable](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_SetWalkable_System_Int32_System_Int32_System_Boolean_).

```csharp
public static NavGrid FromTiles(TileGrid tiles, TileFlags blocking = TileFlags.Solid)
```

#### Parameters

`tiles` [TileGrid](MicroEngine.World.TileGrid.md)

`blocking` [TileFlags](MicroEngine.World.TileFlags.md)

#### Returns

 [NavGrid](MicroEngine.Navigation.NavGrid.md)

### <a id="MicroEngine_Navigation_NavGrid_GetEdges_System_Int32_System_Collections_Generic_List_MicroEngine_Navigation_NavEdge__"></a> GetEdges\(int, List<NavEdge\>\)

The steps out of a cell: to each walkable neighbour (up, right, down, left, then the diagonals clockwise from
up-right, as [Diagonals](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_Diagonals) allows), at the step's length times the neighbour's cost. The cell itself
may be blocked: an agent caught in a closing door can still walk out.

```csharp
public void GetEdges(int node, List<NavEdge> edges)
```

#### Parameters

`node` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`edges` [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[NavEdge](MicroEngine.Navigation.NavEdge.md)\>

### <a id="MicroEngine_Navigation_NavGrid_HasLineOfSight_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_"></a> HasLineOfSight\(Vector2, Vector2, float\)

Whether an agent can go straight from <code class="paramref">from</code> to <code class="paramref">to</code> (world points): no
blocked cell, and nothing outside the grid, is touched by the square of half-side <code class="paramref">radius</code>
centred on it along the way. With a radius of 0 the agent is a point; a point does not pass between two
blocked cells that meet at a corner.

```csharp
public bool HasLineOfSight(Vector2 from, Vector2 to, float radius = 0)
```

#### Parameters

`from` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`to` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">radius</code> is negative or not finite.

### <a id="MicroEngine_Navigation_NavGrid_InBounds_System_Int32_System_Int32_"></a> InBounds\(int, int\)

Whether cell (<code class="paramref">x</code>, <code class="paramref">y</code>) is inside the grid.

```csharp
public bool InBounds(int x, int y)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavGrid_IsOpen_System_Collections_Generic_IReadOnlyList_System_Int32__System_Int32_System_Single_"></a> IsOpen\(IReadOnlyList<int\>, int, float\)

Whether <code class="paramref">path</code> (nodes, as [Path](MicroEngine.Navigation.PathFinder.md#MicroEngine_Navigation_PathFinder_Path) gives them) can still be walked from
its index <code class="paramref">from</code> on: every cell is walkable and every step is still allowed. An agent checks
the rest of its path when [Version](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_Version) changes and searches again only when this is false.

```csharp
public bool IsOpen(IReadOnlyList<int> path, int from = 0, float radius = 0)
```

#### Parameters

`path` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[int](https://learn.microsoft.com/dotnet/api/system.int32)\>

The nodes of the path.

`from` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The index of the path's cell the agent is in. The cells before it are not looked at, and the agent's own cell may be one its body does not fit in (it is pressed against a wall).

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

Half the side of the agent's square, in world units: for an agent wider than a cell, every cell ahead must still have room for it ([Fits](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_Fits_System_Int32_System_Int32_System_Single_)).

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">from</code> is negative, <code class="paramref">radius</code> is negative or not finite, or a node is not of this grid.

### <a id="MicroEngine_Navigation_NavGrid_IsWalkable_System_Int32_System_Int32_"></a> IsWalkable\(int, int\)

Whether cell (<code class="paramref">x</code>, <code class="paramref">y</code>) can be walked; cells outside the grid cannot.

```csharp
public bool IsWalkable(int x, int y)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavGrid_NodeAt_System_Numerics_Vector2_"></a> NodeAt\(Vector2\)

The node of the cell that holds the world point <code class="paramref">point</code>, or -1 outside the grid.

```csharp
public int NodeAt(Vector2 point)
```

#### Parameters

`point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Navigation_NavGrid_NodeOf_System_Int32_System_Int32_"></a> NodeOf\(int, int\)

The node of cell (<code class="paramref">x</code>, <code class="paramref">y</code>): <code>y * Width + x</code>.

```csharp
public int NodeOf(int x, int y)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The cell is outside the grid.

### <a id="MicroEngine_Navigation_NavGrid_ReadPhysics_MicroEngine_Physics_IPhysicsWorld2D_MicroEngine_Physics_QueryFilter__"></a> ReadPhysics\(IPhysicsWorld2D, in QueryFilter\)

Reads every cell again from a physics world: a cell is blocked when a shape <code class="paramref">filter</code> accepts
overlaps it. Give the filter of the agents that will walk the grid, so walls block and other agents, pickups
and sensors do not. Returns whether anything changed.

```csharp
public bool ReadPhysics(IPhysicsWorld2D world, in QueryFilter filter = default)
```

#### Parameters

`world` [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md)

`filter` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavGrid_ReadTiles_MicroEngine_World_ICollisionGrid_MicroEngine_World_TileFlags_"></a> ReadTiles\(ICollisionGrid, TileFlags\)

Reads every cell again from a tile map: a cell is blocked when its tile has any of the
<code class="paramref">blocking</code> flags. Call it after tiles change; for one cell, [SetWalkable](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_SetWalkable_System_Int32_System_Int32_System_Boolean_) is
cheaper. Returns whether anything changed.

```csharp
public bool ReadTiles(ICollisionGrid tiles, TileFlags blocking = TileFlags.Solid)
```

#### Parameters

`tiles` [ICollisionGrid](MicroEngine.World.ICollisionGrid.md)

`blocking` [TileFlags](MicroEngine.World.TileFlags.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavGrid_SetBlocked_MicroEngine_RectF_System_Boolean_"></a> SetBlocked\(RectF, bool\)

Blocks or frees every cell <code class="paramref">area</code> (in world units) overlaps: a door, a crate, a fallen bridge.
Cells the rectangle only touches at an edge are left alone; the part outside the grid is ignored.

```csharp
public void SetBlocked(RectF area, bool blocked)
```

#### Parameters

`area` [RectF](MicroEngine.RectF.md)

`blocked` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavGrid_SetCost_System_Int32_System_Int32_System_Single_"></a> SetCost\(int, int, float\)

Sets what entering cell (<code class="paramref">x</code>, <code class="paramref">y</code>) costs: 1 for open ground, more for ground
agents should avoid when a way around is cheaper (mud, water, a guarded corridor). A step costs its length (1,
or the square root of 2 diagonally) times the cost of the cell entered.

```csharp
public void SetCost(int x, int y, float cost)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`cost` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The cell is outside the grid, or <code class="paramref">cost</code> is less than 1 or not finite.

### <a id="MicroEngine_Navigation_NavGrid_SetWalkable_System_Int32_System_Int32_System_Boolean_"></a> SetWalkable\(int, int, bool\)

Makes cell (<code class="paramref">x</code>, <code class="paramref">y</code>) walkable or blocked. A blocked cell keeps its cost.

```csharp
public void SetWalkable(int x, int y, bool walkable)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`walkable` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The cell is outside the grid.

### <a id="MicroEngine_Navigation_NavGrid_Slide_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_"></a> Slide\(Vector2, Vector2, float\)

Moves a body whose square has the half-side <code class="paramref">radius</code> from <code class="paramref">position</code> by
<code class="paramref">delta</code> (world units) and returns where it ends: it stops at the face of a blocked cell or
of the grid's edge and slides along it, so it never enters a wall. For things that walk a grid without a
physics body; agents of [NavAgents](MicroEngine.Navigation.NavAgents.md) move this way. A body that is already in a wall (one was
built on it) can walk out.

```csharp
public Vector2 Slide(Vector2 position, Vector2 delta, float radius = 0)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`delta` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">radius</code> is negative or not finite.

### <a id="MicroEngine_Navigation_NavGrid_Smooth_System_Collections_Generic_IReadOnlyList_System_Int32__System_Collections_Generic_List_System_Numerics_Vector2__System_Single_"></a> Smooth\(IReadOnlyList<int\>, List<Vector2\>, float\)

Turns a path of cells into the points an agent walks through: the centres of its cells with every one removed
that the agent can skip by walking straight ([HasLineOfSight](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_HasLineOfSight_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_) with <code class="paramref">radius</code>), so
the route turns only where a wall makes it. <code class="paramref">waypoints</code> is replaced; the first cell, where the
agent is, is not in it. A straight leg never crosses ground costlier than the cells it replaces.

```csharp
public void Smooth(IReadOnlyList<int> path, List<Vector2> waypoints, float radius = 0)
```

#### Parameters

`path` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[int](https://learn.microsoft.com/dotnet/api/system.int32)\>

`waypoints` [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)\>

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">radius</code> is negative or not finite, or a node is not of this grid.

</div>

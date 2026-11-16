# Namespace MicroEngine.Navigation

<div data-pagefind-weight="0.3">

Pathfinding (optional package): the cheapest path over a grid of cells or a graph of waypoints, grids built from a tile map or a physics world, obstacles that change while the game runs, routes straightened for agents with a size, and distance fields for many agents with one goal.

Guide: [Navigation](../guide/navigation.md)  

### Classes

 [DistanceField](MicroEngine.Navigation.DistanceField.md)

The cost of the cheapest route from every cell of a [NavGrid](MicroEngine.Navigation.NavGrid.md) to a goal, computed once for all of
them: what many agents with the same goal read instead of each searching its own path (a horde after the player,
units sent to one point, creatures fleeing to the nearest exit). Each agent asks for the cell to step to
([Next](MicroEngine.Navigation.DistanceField.md#MicroEngine_Navigation_DistanceField_Next_System_Int32_)); the game computes the field again when the goal moves to another cell or the grid changes
([Version](MicroEngine.Navigation.DistanceField.md#MicroEngine_Navigation_DistanceField_Version)).

 [NavGraph](MicroEngine.Navigation.NavGraph.md)

Walkable space as points joined by hand: waypoints along roads, rooms joined by doors, platforms joined by jumps.
Each node has a position; each connection has a cost (the distance between its ends unless told otherwise), a
direction (both ways, or one way for a drop, a jump or a slide) and a tag the game reads to know how to cross it.
A [PathFinder](MicroEngine.Navigation.PathFinder.md) searches it like any [INavGraph](MicroEngine.Navigation.INavGraph.md).

 [NavGrid](MicroEngine.Navigation.NavGrid.md)

Walkable space as a grid of square cells, each walkable or blocked and with a cost to enter it. It is the graph a
[PathFinder](MicroEngine.Navigation.PathFinder.md) searches for tile maps and for any level that can be sampled on a grid; node
<code>y * Width + x</code> is cell (x, y). A grid is plain data the game owns: it is built once (by hand, from a tile map
with [FromTiles](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_FromTiles_MicroEngine_World_TileGrid_MicroEngine_World_TileFlags_), or from a physics world with [FromPhysics](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_FromPhysics_MicroEngine_Physics_IPhysicsWorld2D_MicroEngine_RectF_System_Single_MicroEngine_Physics_QueryFilter__)) and changed when the level
changes (a door closes, a crate is pushed, a bridge falls). It decides nothing about who walks it or why.

 [PathFinder](MicroEngine.Navigation.PathFinder.md)

Finds the cheapest path between two nodes of an [INavGraph](MicroEngine.Navigation.INavGraph.md) with the A* algorithm. A finder keeps the
memory of its searches, so searching again allocates nothing once it has seen a graph of that size; keep one per
game (or one per thread: a finder serves one search at a time). The same question on the same graph always gives
the same path, on every platform.

### Structs

 [NavCell](MicroEngine.Navigation.NavCell.md)

A cell of a [NavGrid](MicroEngine.Navigation.NavGrid.md): its column and row, from the top-left cell (0, 0).

 [NavEdge](MicroEngine.Navigation.NavEdge.md)

A step of a graph: the node it leads to, what it costs, and a number the game gives its own meaning.

### Interfaces

 [INavGraph](MicroEngine.Navigation.INavGraph.md)

Space a [PathFinder](MicroEngine.Navigation.PathFinder.md) can search: nodes numbered from 0, the steps that leave each one, and an estimate
of the cost between two of them. [NavGrid](MicroEngine.Navigation.NavGrid.md) (cells) and [NavGraph](MicroEngine.Navigation.NavGraph.md) (waypoints joined by hand)
implement it; a game implements it for a space of its own (hexagons, rooms, a 3D lattice).

### Enums

 [DiagonalMovement](MicroEngine.Navigation.DiagonalMovement.md)

How an agent may move between the cells of a [NavGrid](MicroEngine.Navigation.NavGrid.md).

 [PathStatus](MicroEngine.Navigation.PathStatus.md)

How a search ended.

</div>

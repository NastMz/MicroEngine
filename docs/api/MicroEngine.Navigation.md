# Namespace MicroEngine.Navigation

<div data-pagefind-weight="0.3">

Navigation (optional package): the cheapest path over a grid of cells or a graph of waypoints, grids built from a tile map or a physics world, obstacles that change while the game runs, routes for agents with a size, what can be reached without a search, distance fields for many agents with one goal, and agents that follow their routes, keep apart and arrive.

Guide: [Navigation](../guide/navigation.md)  

### Classes

 [DistanceField](MicroEngine.Navigation.DistanceField.md)

The cost of the cheapest route from every cell of a [NavGrid](MicroEngine.Navigation.NavGrid.md) to a goal, computed once for all of
them: what many agents with the same goal read instead of each searching its own path (a horde after the player,
units sent to one point, creatures fleeing to the nearest exit). Each agent asks for the cell to step to
([Next](MicroEngine.Navigation.DistanceField.md#MicroEngine_Navigation_DistanceField_Next_System_Int32_)); the game computes the field again when the goal moves to another cell or the grid changes
([Version](MicroEngine.Navigation.DistanceField.md#MicroEngine_Navigation_DistanceField_Version)).

 [NavAgentSettings](MicroEngine.Navigation.NavAgentSettings.md)

How an agent of [NavAgents](MicroEngine.Navigation.NavAgents.md) moves, in the grid's world units and seconds.

 [NavAgent](MicroEngine.Navigation.NavAgent.md)

One agent of [NavAgents](MicroEngine.Navigation.NavAgents.md): a body with a position that takes orders ([MoveTo](MicroEngine.Navigation.NavAgent.md#MicroEngine_Navigation_NavAgent_MoveTo_System_Numerics_Vector2_),
[Follow](MicroEngine.Navigation.NavAgent.md#MicroEngine_Navigation_NavAgent_Follow_MicroEngine_Navigation_DistanceField_), [Stop](MicroEngine.Navigation.NavAgent.md#MicroEngine_Navigation_NavAgent_Stop)) and tells how it is doing ([Status](MicroEngine.Navigation.NavAgent.md#MicroEngine_Navigation_NavAgent_Status)). The system creates it
([Add](MicroEngine.Navigation.NavAgents.md#MicroEngine_Navigation_NavAgents_Add_System_Numerics_Vector2_MicroEngine_Navigation_NavAgentSettings_)) and moves it each step; the game keeps the reference, reads
[Position](MicroEngine.Navigation.NavAgent.md#MicroEngine_Navigation_NavAgent_Position) to draw, and removes it when the thing it stands for is gone.

 [NavAgents](MicroEngine.Navigation.NavAgents.md)

The agents that walk a [NavGrid](MicroEngine.Navigation.NavGrid.md): each follows the route to where it was sent, slows down and stops
there, searches again when a change of the grid closes its way, keeps apart from the others and turns aside from
those it would run into, and never enters a wall or another body. It is what a game writes after it has a path;
what an agent wants stays the game's. Add it to a scene (<code>Systems.Add(new NavAgents(grid))</code>) and it is
stepped with the scene and is part of its state hash; or keep one and call [Step](MicroEngine.Navigation.NavAgents.md#MicroEngine_Navigation_NavAgents_Step_System_Single_).

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

 [NavRegions](MicroEngine.Navigation.NavRegions.md)

What can be reached from where on a [NavGrid](MicroEngine.Navigation.NavGrid.md): the cells joined by some route form a region, and two
places are joined exactly when they are in the same region. It answers without a search whether a target can be
reached ([Connected](MicroEngine.Navigation.NavRegions.md#MicroEngine_Navigation_NavRegions_Connected_System_Int32_System_Int32_)), where to go instead when it cannot ([NearestReachable](MicroEngine.Navigation.NavRegions.md#MicroEngine_Navigation_NavRegions_NearestReachable_System_Int32_System_Int32_)), and
whether a wall about to be built would cut a way that must stay open ([StaysConnected](MicroEngine.Navigation.NavRegions.md#MicroEngine_Navigation_NavRegions_StaysConnected_MicroEngine_RectF_System_Int32_System_Int32_)):
the placement rule of a tower defence. The regions follow the grid: after a change they are measured again by the
first question, so there is nothing to call.

 [PathFinder](MicroEngine.Navigation.PathFinder.md)

Finds the cheapest path between two nodes of an [INavGraph](MicroEngine.Navigation.INavGraph.md) with the A* algorithm. A finder keeps the
memory of its searches, so searching again allocates nothing once it has seen a graph of that size; keep one per
game (or one per thread: a finder serves one search at a time). The same question on the same graph always gives
the same path, on every platform.

 [Steering](MicroEngine.Navigation.Steering.md)

The steering rules [NavAgents](MicroEngine.Navigation.NavAgents.md) is made of, for games that move things their own way (objects of an
entity store, creatures that fly, a platformer's enemies): each takes where things are and returns a velocity or
a push to add to it. They keep nothing between calls and use only sums, products and square roots, so the same
inputs give the same result on every machine.

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

 [NavAgentStatus](MicroEngine.Navigation.NavAgentStatus.md)

What an agent of [NavAgents](MicroEngine.Navigation.NavAgents.md) is doing.

 [PathStatus](MicroEngine.Navigation.PathStatus.md)

How a search ended.

</div>

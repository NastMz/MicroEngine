# NAV-1: optional 2D/3D pathfinding and navigation

Issue NastMz/MicroEngine#112. Record: [`odd/tasks/nav-1.md`](../../odd/tasks/nav-1.md). Decision:
[ADR 0056](../decisions/0056-navigation-optional-package.md). Date: 2026-11-14.
The question is whether MicroEngine needs a navigation module, what can be reused, and which game need justifies it.
The rule is that of every recent evaluation: could an external developer build a typical game of that style today
with the published SDK? Three scenarios make it concrete: an enemy that chases the player around walls in a top-down
tile map; units that walk to a clicked point; a patrol that re-plans when a door closes.

Statements below are marked by what backs them: **measured** (run on this branch), **read** (a page or source file
fetched during the survey, URL given), or **assumed** (not verified; listed again in §9).

## 1. What exists today

- **Space.** `TileGrid` (`src/MicroEngine.Physics.Tiles/TileMap.cs`) answers what a cell holds (`FlagsAt`);
  `IPhysicsWorld2D` (`src/MicroEngine.Physics/IPhysicsWorld2D.cs`) answers moves, ray casts and overlaps on both
  backends; `MicroEngine.Tiled` turns a map's tile property into a `TileGrid`. All of them describe where things
  *are*.
- **Movement.** `TileMotion.Move` and `IPhysicsWorld2D.Move` slide a box along what it hits.
- **Search.** Nothing. No type in `src/` finds a route between two points, measures a distance through walls, or
  tells which cell leads towards a goal.
- **The demos.** Courtyard Key's enemies walk straight at the hero and try the other axis when they hit a wall
  (`games/CourtyardKey/Entities/Enemies.cs`, the shield enemy's `Think`); its rooms are one screen with few inner
  walls, which hides the weakness. Trail Duel's characters wander at most two cells from their post
  (`games/TrailDuel/Scenes/OverworldScene.cs`, `UpdateWanderers`). Neither demo searches a path.

**Answer to the rule today:** none of the three scenarios can be built without writing a path search. A correct A*
with a priority queue that supports cheaper re-insertion, a tie order that does not depend on the runtime, obstacle
changes and a straightening pass is 300 to 600 lines that every such game repeats. The gap is real, and it is the
same gap for a tower defence, a top-down shooter, a tactics game or an RTS.

## 2. Techniques

| Technique | Space it searches | Strengths | Limits | Fit here |
| --- | --- | --- | --- | --- |
| A* on a grid | Cells, 4 or 8 neighbours, a cost per cell | Maps one to one onto tile maps, the engine's level format; obstacles change in O(1); optimal with the octile estimate | Routes follow cell centres unless straightened; memory and worst case grow with the map's area | **Core**: the three scenarios are tile scenarios |
| A* / Dijkstra on a graph | Nodes joined by hand or by a tool | Any topology: roads, rooms, hex or 3D lattices; directed edges | Someone must author or generate the graph | **Core**: same algorithm behind an interface; covers platformer links and 3D |
| Distance field (Dijkstra map, flow field) | A grid, from one or more goals outwards | One computation serves every agent with that goal; O(1) per agent per step | Costs the whole reachable area per goal change; one field per goal | **Core**: hordes and "everybody to this point" |
| Jump Point Search | Uniform-cost 8-way grids | Expands far fewer nodes than A* on open maps | Uniform cost only: per-cell costs break it (Godot's `AStarGrid2D` documents that its jumping mode disables weights, **read**) | Deferred: conflicts with costs; no measured need |
| Hierarchical (HPA*) | Clusters of a grid | Long paths on very large maps in bounded time | Clusters must be rebuilt on change; near-optimal only | Deferred: §5 measures when it would matter |
| Any-angle (Theta*, string pulling) | A grid or a portal sequence | Routes that look walked, not stepped | Theta* checks line of sight during the search | String pulling over the A* path is in the core; Theta* is not needed on top of it |
| 2D navmesh (polygons + funnel) | Convex polygons | Few nodes for large open areas; exact any-angle routes; agent radius by eroding the mesh | Needs polygon authoring or generation (triangulation, clipping), and re-cutting for dynamic obstacles | Deferred: §7 |
| Platformer movement links | Nodes on platforms, directed links for jumps, drops, ladders | Models movement a grid cannot (one-way, cost by effort) | Whether a jump is possible is the game's physics; generating links needs it | The graph with directed tagged edges is in the core; generation stays with the game |
| 3D (navmesh with Recast/Detour, voxel grids) | Triangles or voxels | The standard for 3D levels | A toolchain of its own | The search is reusable through the graph interface (a 3D lattice is in the tests); meshes wait for a 3D game |

References: the A* formulation (Hart, Nilsson and Raphael, 1968, **assumed** from memory); implementation notes on
queues without re-prioritising, ties and early exit (Red Blob Games,
<https://www.redblobgames.com/pathfinding/a-star/implementation.html>, **read**); estimates for 4-way and 8-way
grids and admissibility (<https://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html>, **read**); string
pulling through portals (Mononen, <https://digestingduck.blogspot.com/2010/03/simple-stupid-funnel-algorithm.html>,
**read**); JPS (Harabor and Grastien, 2011), Theta* (Nash et al., 2007) and HPA* (Botea et al., 2004) are **assumed**
from memory.

How other engines expose it (**read**): Godot 4 ships `AStar2D` (a hand-built weighted graph) and `AStarGrid2D`
(diagonal modes, per-cell weights, solid cells) next to a navmesh server with regions, agents, obstacles and
`NavigationLink2D`, whose traversal the game scripts
(<https://docs.godotengine.org/en/stable/classes/class_astargrid2d.html>,
<https://docs.godotengine.org/en/stable/tutorials/navigation/navigation_introduction_2d.html>). Unity's navigation is
a 3D navmesh with `NavMeshLink`
(<https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshLink.html>); 2D games use third-party
packages. libGDX's gdx-ai has an indexed A* with hierarchical graphs and path smoothing
(<https://github.com/libgdx/gdx-ai/wiki/Pathfinding-API>). MonoGame.Extended lists no pathfinding
(<https://www.monogameextended.net/docs/about/introduction/>). The pattern: a grid A* and a graph A* are the entry
level every 2D engine with navigation offers; a navmesh is the second tier.

## 3. Existing implementations

All rows **read** on the pages given on 2026-11-14 unless marked. None was built or run for NativeAOT or
WebAssembly; that column reports what the package declares.

| Candidate | Licence | Latest release | Kind | AOT / browser | Fit |
| --- | --- | --- | --- | --- | --- |
| .NET `PriorityQueue<TElement,TPriority>` | MIT | in-box since .NET 6 | Managed heap | In-box | A building block, not a solver: no priority update, and the documentation states it "does not guarantee first-in-first-out semantics for elements of equal priority" (<https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.priorityqueue-2>). Ties would be decided by the heap's shape |
| QuikGraph | MS-PL | 2.5.0, July 2022 | Managed graph library with A* | Not declared | General graphs with generic vertex and edge objects; no grid, no world coordinates; allocations per query not documented (<https://www.nuget.org/packages/QuikGraph>) |
| RoyT.AStar | MIT | 3.0.2, August 2020 | Managed grid and graph A* | Not declared | Closest in scope; unmaintained for six years (<https://www.nuget.org/packages/RoyT.AStar>) |
| AStarLite | not read | 1.3.0, October 2025 | Managed 2D grid A* | Not checked | Recent but small (27K downloads), grid only (<https://www.nuget.org/packages/AStarLite>) |
| GoRogue | MIT | stable 2.6.4 (2020); 3.0.0-beta11, December 2025 | Managed roguelike toolkit with A* and goal maps | Not declared | The 3.x line pulls TheSadRogue.Primitives, ShaiRandom and OptimizedPriorityQueue and its own grid types (<https://www.nuget.org/packages/GoRogue>) |
| EpPathFinding.cs (JPS), Aptacode.PathFinder (HPA*), Pathfindax (flow fields) | MIT / not read | 2018, 2022, 2019 | Managed | n/a | Stale; the first targets .NET Framework with a C5 dependency (<https://www.nuget.org/packages/EpPathFinding.cs>) |
| DotRecast | Zlib | 2026.3.1, August 2026 | Managed port of Recast and Detour: 3D navmesh build, query, crowd | Its Core and Detour projects set neither `IsAotCompatible` nor `IsTrimmable` | Actively maintained; a 3D toolchain with nothing specific to 2D (<https://github.com/ikpil/DotRecast>, <https://www.nuget.org/packages/DotRecast.Detour>) |
| SharpNav | MIT | 1.0.0-alpha2, 2015 | Managed navmesh | n/a | Never left alpha (<https://github.com/Robmaister/SharpNav>) |
| recastnavigation | Zlib | 1.6.0, May 2023 | Native C++ | Would need a native build per runtime, the browser included | No .NET binding with prebuilt natives was found (<https://github.com/recastnavigation/recastnavigation>) |
| Clipper2 (Boost), LibTessDotNet (SGI B), Triangle.NET (unclear licence, no NuGet release) | — | 2025, 2021, — | Managed polygon clipping and triangulation | Not checked | The pieces a 2D navmesh builder would need (<https://www.nuget.org/packages/Clipper2>, <https://www.nuget.org/packages/LibTessDotNet>, <https://github.com/wo80/Triangle.NET>) |

**Reading.** For grids and graphs, the maintained options are a general graph library that knows nothing of cells
(QuikGraph), a toolkit with three dependencies and its own vocabulary (GoRogue 3), or a single small package
(AStarLite). The algorithm itself is about 150 lines; what a game needs around it (cells read from tiles or physics,
versions for re-planning, line of sight for an agent with a size, deterministic ties, no garbage per query) is
engine-specific and none of the candidates has it. Adopting one would add a dependency to wrap less code than the
wrapper. This is the opposite of TILE-1, where DotTiled carried thousands of lines of format knowledge.
For navmeshes the situation reverses: DotRecast carries real work, but it is a 3D toolchain and its AOT behaviour is
undeclared, and the native original would repeat Box2D's per-runtime build (ADR 0051) for a feature no 2D tile game
needs first.

## 4. Options

1. **Optional package `MicroEngine.Navigation`, the engine's own managed code.** A* behind a graph interface, a grid
   that reads tile maps and physics worlds, a waypoint graph with directed tagged links, a distance field. No
   dependency.
2. **The same code inside the `MicroEngine` package**, as one more bundled assembly (like saves or achievements).
   No tenth package to release; every game carries the assembly (27 KB) whether it navigates or not, and the
   navigation API, the newest and least proven, sits in the surface of the core package.
3. **A package over a third-party library** (QuikGraph or GoRogue for grids, DotRecast for meshes). §3: more
   dependency than code for grids; the wrong tier first for meshes.
4. **A documented recipe only.** Every game still writes the solver; the gap stays open.

## 5. What was built, and what it costs

Option 1, in `src/MicroEngine.Navigation` (967 lines with documentation):

- `INavGraph` and `PathFinder`: A* over any graph; an indexed binary heap with decrease-key; ties broken by a total
  order (total cost, then the node further along, then the lower number), so the same question gives the same path
  on every runtime; a search budget (`MaxExpansions`); when the goal cannot be reached, the path to the reachable
  node nearest to it; estimates that are admissible but uneven still give the cheapest path (nodes re-enter the
  queue).
- `NavGrid`: cells walkable or blocked, a cost per cell, three diagonal rules, a `Version` that changes with the
  grid, `IsOpen` to check a remembered path, `FromTiles` / `ReadTiles` and `FromPhysics` / `ReadPhysics`,
  `SetBlocked(RectF)`, line of sight for a square agent, and `FindPath`, which returns straightened world points.
- `NavGraph`: positions joined by hand, connections with a cost, a direction and a tag; nodes that can be disabled.
  Its estimate scales the straight distance by the cheapest cost per distance of any connection, so teleporters and
  fast roads keep paths optimal.
- `DistanceField`: the cost from every cell to one or several goals, with a distance limit.

**Correctness (measured).** `tests/MicroEngine.Navigation.Tests`, 41 tests: on 180 random 16 × 12 maps with walls
and costs, in the three diagonal modes, every path's cost equals that of an exhaustive Dijkstra search written
without a heap or an estimate, and every step is an edge; the same on random 3D lattices, on random directed graphs
with free and cheap links, and with random admissible estimates; every cell of 72 distance fields equals the
exhaustive cost and walking `Next` from any cell arrives at that cost; every leg of straightened routes is clear for
the agent's size; a closed door invalidates the path and the next search goes around; a reused finder and a fresh
one return identical paths; 80 searches and a field computation allocate 0 bytes once warm.

**Performance (measured).** `benchmarks/NavBench`, Windows x64, .NET 10.0.15, median of 5 runs after warm-up,
2026-11-14, on maps with walls scattered over 20% of the cells. The machine was running other builds; repeated runs
of the same row varied by up to a factor of two, so read these as orders of magnitude.

| Measurement | 128 × 128 | 1000 × 1000 | Allocated |
| --- | ---: | ---: | ---: |
| Read the tile map into a grid (`NavGrid.FromTiles`) | 0.18 ms | 49 ms | 80 KB / 4.9 MB |
| Read it again after tiles changed (`ReadTiles`) | 0.07 ms | 11 ms | 0 |
| A* between cells at least half the map apart (86 / 635 cells long; 1,278 / 61,187 cells looked at) | 0.28 ms | 17 ms | 0 |
| The same with the route straightened for a 12-unit agent (`FindPath`; 26 / 181 legs) | 0.31 ms | 14 to 28 ms | 0 |
| A* to a walled-in goal: every reachable cell looked at (13,092 / 799,278) | 1.4 ms | 149 ms | 0 |
| The same with `MaxExpansions = 20000` | 1.5 ms | 3.5 ms | 0 |
| Distance field over the whole map | 1.5 ms | 127 ms | 0 |
| Distance field up to 64 cells from the goal | 0.7 ms | 1.1 ms | 0 |
| The next cell of 1,000 agents from the field | 0.15 ms | 0.09 ms | 0 |

Memory at 1000 × 1000: the grid 4.8 MB, a finder 22.9 MB once it has searched that map, a field 7.6 MB.

**Reading.** On a level of ordinary size (128 × 128 is 2048 × 2048 pixels of 16-pixel tiles) a search costs a
fraction of a millisecond and the worst case under two: several agents can search in one frame. On a million cells
a long path costs about one frame and an unreachable goal nine; a game of that size sets `MaxExpansions`, spreads
searches over frames, or uses a bounded field. That is the point where hierarchical search would pay, and nothing in
the engine's games or samples is near it. The package is 27 KB (`dotnet pack`, **measured**) and depends on
`MicroEngine` only.

**NativeAOT and browsers.** The project sets `IsAotCompatible` and builds without trim or AOT analyzer warnings
(**measured**: warnings are errors in this repository). It uses arrays, lists and spans only: no reflection, no
native code, no threads. A published NativeAOT or browser build of a consumer was **not run** here.

## 6. Integration

- **Tile maps and Tiled.** `NavGrid.FromTiles(TileGrid)` takes the grid `TilePhysicsWorld2D`, `TileMotion` and
  `TiledMap.CollisionGrid` already use, so a map from the Tiled editor is navigable in one line and the package does
  not reference `MicroEngine.Tiled`.
- **Physics.** `NavGrid.FromPhysics(world, bounds, cellSize, filter)` samples `IPhysicsWorld2D.Overlap` per cell,
  with the agent's `QueryFilter`: it works on the tile backend (tested, tile grids included) and, through the same
  contract, on Box2D (**assumed**: not run against the Box2D backend). Routes are straightened with the agent's
  half-size, so a box moved by `TileMotion.Move` along them does not touch a wall (the sample's test checks every
  step).
- **Dynamic obstacles.** The grid is a copy the game updates (`SetWalkable`, `SetBlocked`, `ReadTiles`,
  `ReadPhysics`); `Version` and `IsOpen` let an agent re-plan only when its own path closed. Moving agents are not
  obstacles: avoiding each other is steering, not path search.
- **Scenes.** No coupling. A path is data; which entity follows it and what it does on arrival are the game's. The
  package registers no module and no system.
- **Example.** `samples/Pathfinding` plays the three scenarios plus a swarm on one field, with the door as a tile
  whose flags change; `tests/Samples.Tests/PathfindingTests.cs` plays it headlessly.

## 7. Deferred, with the reason

- **2D navmesh.** Needs polygon input (the engine's levels are tiles; Tiled polygons are the only source), a
  triangulator and a clipper for dynamic obstacles: two more dependencies (Clipper2 and a tessellator whose licence
  texts disagree between NuGet and README) or about two thousand lines of geometry. Grid paths straightened by line
  of sight already give any-angle routes on tile levels. It returns when a game with free-form geometry needs it;
  `INavGraph` is the seam it would plug into.
- **Generating platformer links.** Whether a jump connects two ledges depends on the game's gravity, jump speed and
  air control: a game rule. The graph carries the links; a generator would have to simulate each game's movement.
- **Agents larger than a cell during the search.** Line of sight and straightening honour the agent's size; the
  cell search treats the agent as at most one cell wide. Clearance needs either a per-size clearance map or a grid
  per size, and the right shape depends on a consumer that has both sizes. Until then a game blocks the cells next
  to walls for its large agents (a second `NavGrid`).
- **Local avoidance and crowds, hierarchical search, JPS, time-sliced searches, connected-region queries.** No
  measured need (§5); each adds API that the first external consumers should shape.
- **3D navmeshes.** There is no published 3D path to consume them (`MicroEngine.Graphics3D` is in the repository
  only).

## 8. The demos as consumers

Not changed by this work (outside its scope). Courtyard Key is the candidate: its shield enemy and its chasers
would stop sticking on pillars and inner walls with `NavGrid.FromTiles` over the room's grid and one search when the
hero changes cell; the rooms are 20 × 11 cells, so cost is not a concern. Trail Duel's trainers walk straight lines
by design and its wanderers stay near their post: no need. Neither justifies the module alone; the justification is
the external developer of §1.

## 9. Recommendation

MicroEngine needs navigation as an **optional module of its own code**: option 1. What can be reused is the BCL and
the engine's own tile and physics types; no third-party solver earns its dependency for grids and graphs, and the
navmesh libraries serve a tier no MicroEngine game has asked for. The actual need that justifies it is any top-down
or strategy game with walls: the three scenarios cannot be built today and can be with the package.

Not verified, to check before relying on them:

- A consumer published with NativeAOT and as a browser package (expected to work: managed code without reflection).
- `FromPhysics` against the Box2D backend.
- The licence texts of AStarLite, OptimizedPriorityQueue, Aptacode.PathFinder and Pathfindax; the three papers and
  the A* citation, quoted from memory.
- Benchmark figures on a quiet machine.

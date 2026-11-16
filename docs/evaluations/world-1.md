# WORLD-1: 2D world scalability and spatial queries

Issue NastMz/MicroEngine#126. Record: [`odd/tasks/world-1.md`](../../odd/tasks/world-1.md). Decision:
[ADR 0058](../decisions/0058-spatial-index-beside-the-world.md). Date: 2026-11-16. Builds on
[PERF-3](perf-3.md), whose section 5.1 measured iteration, creation and destruction, and the physics world's queries.

Evidence words are used as in PERF-3. **Measured** (or demonstrated): run on the machine of section 1 and read from
its output. **Hypothesis**: a cause proposed for a measured effect and not isolated by an experiment. **From the
code**: read in the sources, not timed. **Not run** means exactly that. Every limit is "this workload, this machine,
this build, 16.667 ms of CPU work at the 99th percentile" (one 60 Hz update), with nothing left for a game's own
logic; none is a promise.

## 1. Conditions

| | |
| --- | --- |
| CPU / RAM | AMD Ryzen 5 7600X (Zen 4, 6 cores, 12 threads; 1 MB of L2 per core, 32 MB of L3), 31.2 GB |
| GPU / display | NVIDIA GeForce RTX 4070 Ti SUPER, 1920x1080 at 100 Hz |
| System | Windows 11 Pro 10.0.26300, x64 |
| Runtime | .NET 10.0.15 (SDK 10.0.404), JIT with tiered compilation and dynamic PGO, workstation concurrent GC |
| Engine | 0.6.0-alpha.1; "before" is the engine of `main` at `d29b65e` with the scenarios of section 3 (as at `ec78a01`, where the world and the tile backend are still unchanged); "after" is the branch `feat/world-1` |
| Builds | Release, published once per build to a folder outside the repository and run from there |
| Canvas | 320x180 at scale 3, 60 updates per second (StressBench's game) |

**How idle the machine was.** The idle check of PERF-3 (`docs/evaluations/perf-3/campaign.sh OUT idle`: ten
one-second samples of the total CPU load, and the CPU time used meanwhile by build and test processes) ran before
each group of runs:

| When (UTC) | Before which runs | Load %, ten samples | Build and test processes' CPU seconds |
| --- | --- | --- | --- |
| 17:11 | the "before" runs and limits (section 4) | 26 13 7 6 7 9 12 9 21 12 | 0 |
| 17:32 | the first index runs | 18 4 28 11 8 5 9 21 8 16 | 0 |
| 17:42 | the world step experiment (section 5) | 17 18 14 26 20 5 19 4 12 2 | 0 |
| 17:45 | a first campaign of the changed build, stopped at 17:50 | 4 8 51 12 9 27 13 2 16 14 | 0 |
| 17:55 | everything "after" (section 7) | **41 59 51 65 66 56 52 51 51 56** | 0.05 |

The load that remains is the desktop's own, as in PERF-3: a game left open (`SkyrimSE`), peripheral software and
this session's clients. They were not closed (not this session's to close). From 17:50 the game was no longer idle
(four to five cores), and section 7 says under which conditions each "after" number was taken.

## 2. What the engine does today (from the code)

- **Iteration.** `IWorld.All<T>` walks one attachment type's dense array; `Local` is an array access behind a
  generation check. PERF-3 measured 3.4 to 5.3 ns per entity up to 1.28 million. Not a limit, and not measured again.
- **No spatial question on the world.** `IWorld` has no query by place. The physics contract has
  (`IPhysicsWorld2D.Overlap`, `RayCast`), for shapes of bodies only.
- **Drawing is the game's.** Nothing in the engine draws "the entities": a scene's `Draw` calls the renderer for what
  it wants drawn, so nothing culls entities either. What culls: `Camera2D.IsVisible(bounds)` for the game to call,
  `TileRenderer.Draw` (only the tiles in `VisibleBounds`), and `TiledSprites` for a map's tile objects. The renderer
  itself submits every draw it is given, on screen or not.
- **Physics broad phase.** The tile backend files box shapes in `ShapeGrid`, 64-unit cells in a
  `Dictionary<long, List<int>>`; an operation takes every shape of the cells its area reaches, sorted by slot. A
  body the game does not move costs nothing in a step (the backend has no dynamic bodies; `Step` only runs sensors).
- **Releasing destroyed entities.** `SceneWorld.Release` compacted the creation order and every attachment store
  over the whole world in every step with a destruction.
- **Loading by region.** Assets load and unload by scope (`AssetCache.CreateScope`, shared objects disposed with
  their last holder), `Preload` decodes on worker threads and uploads within a frame budget, `Unload` releases one
  file. A Tiled map loads whole (`Assets.TiledMap(path)`): an infinite map's chunks are all read at load and the map
  is cropped to the cells it uses; Tiled's `.world` files are not read. So a game can already keep a region's art in
  a scope and one map per region; what it cannot do is ask the engine to stream one map in parts. PERF-3 measured
  what loading during play costs (0.46 ms to decode a 128x128 PNG, 7.4 times its pixels allocated); nothing was
  measured again here, because no number would change the conclusion of section 8.

## 3. The consumer scenarios

Added to `benchmarks/StressBench` (PERF-3's harness: every scenario is a scene in a real `Game`, run by the engine's
loop, headless or in a hidden SDL window), each written the ways a game can write it:

| Scenario | The load counts | What a step does |
| --- | --- | --- |
| `neighbours-brute` | agents wandering at one per 24x24 pixels (the world grows with the load) | every agent counts the others within 48 pixels (about twelve) by walking `All<T>` and reading `Local` |
| `neighbours-physics` | the same agents | each has a kinematic body with an 8x8 box in the tile physics world, moved with `SetPosition` and asked with `Overlap` (96x96), then filtered by distance |
| `neighbours-index` | the same agents | each has a place in a `SpatialIndex2D`, moved with `Move` and asked with `QueryCircle` |
| `large-world` | entities at 2,000 per 320x180 view (200,000 cover 2400x2400 pixels), a camera panning over them | all moved, all drawn |
| `large-world-culled` | the same | all moved, drawn only where `Camera2D.IsVisible` says so |
| `large-world-region` | the same | filed in a `SpatialIndex2D`: only those within 96 pixels of the view are moved, only those in view are drawn |
| `entity-trickle` | live entities with two attachments | eight destroyed and created again per step (a tenth in PERF-3's `entity-churn`) |

The three neighbour scenarios end in the same checksum (positions, each agent's count and the total of pairs): a
test asserts it, so the three strategies are known to answer the same question. Swarms (boids), tower targeting and
bullets against enemies are this scenario with other radii and counts.

```
dotnet run -c Release --project benchmarks/StressBench -- list
dotnet run -c Release --project benchmarks/StressBench -- run --scenario neighbours-index --load 20000 --repeat 3 --isolate
dotnet run -c Release --project benchmarks/StressBench -- run --scenario large-world,large-world-culled,large-world-region --mode headless,render
dotnet run -c Release --project benchmarks/StressBench -- sweep --scenario neighbours-physics --frames 240 --warmup 60 --repeat 2 --isolate
dotnet run -c Release --project benchmarks/WorldBench
```

## 4. Before: where each way stops

Measured on the "before" build (idle check of 17:11). Runs: 300 frames after 60, best of 3, each in its own
process; limits: PERF-3's sweep (240 frames after 60, best of 2). Times in ms, p50 unless marked.

**Neighbours.**

| Agents | Walk over all (`neighbours-brute`) | per pair | Physics world (`neighbours-physics`) | per agent |
| ---: | ---: | ---: | ---: | ---: |
| 1,000 | 3.22 (p99 4.16) | 3.2 ns | 1.62 | 1.6 µs |
| 2,000 | 12.32 (p99 19.18) | 3.1 ns | (4.02 in the sweep) | 2.0 µs |
| 4,000 | not run (quadratic: about 50) | | 8.35 | 2.1 µs |
| 8,000 | | | 18.73 | 2.3 µs |

- The walk costs 3.1 ns per pair looked at and is quadratic: 1,000 agents take a fifth of the frame and 2,000 are
  over the budget at p99. The sweep put its limit between 1,125 and 1,250 with a tail of 16 to 22 ms that the
  isolated runs did not repeat (6 to 11 ms at p99 for 1,125 to 1,500): **the limit is between 1,500 and 2,000 agents**.
- The physics world costs 1.6 to 2.3 µs per agent (the `SetPosition` is 0.02 µs of it; the `Overlap` is the rest) and
  is linear: **limit 6,000 to 6,500 agents**. It also allocates 13 to 165 bytes per frame (the grid's cell lists
  growing as bodies enter new cells), and it requires a body and a shape for everything that must be found.

**A world larger than the view** (200,000 entities, about 2,300 in view).

| | Work | Update | of which the scenario's moves | Draw submission | Present |
| --- | ---: | ---: | ---: | ---: | ---: |
| All moved, all drawn, headless | 4.28 | 0.97 | 0.64 | 3.34 | |
| All moved, drawn if visible, headless | 10.14 | **6.95** | 0.69 | 3.15 | |
| All moved, all drawn, window | 17.45 (p99 22.75) | 1.71 | 1.01 | 15.68 | 15.49 |
| All moved, drawn if visible, window | 8.97 | **5.69** | 0.77 | 3.31 | 0.73 |

- **Drawing everything in a window takes the whole frame**: 15.7 ms of draw submission for 200,000 sprites of which
  2,300 can be seen, and as much again in present (the renderer and the device process every draw they are given).
- **Culling with `Camera2D.IsVisible` removes the renderer's part but not the walk**: 3.2 to 3.3 ms to compute
  200,000 draw positions and test them, as much as submitting them to a counting renderer. It scales with the world,
  not with the view.
- Moving 200,000 entities costs 0.64 to 1.0 ms: iteration is not the problem, as PERF-3 said.
- The culled runs show **5 to 6 ms of update outside the scenario** that the others do not. Section 5 explains it.

**A few destructions in a large world** (`entity-trickle`, eight per step; `entity-churn`, a tenth per step).

| Live entities | Step with 8 replaced | Step with a tenth replaced | of which create and destroy calls |
| ---: | ---: | ---: | ---: |
| 10,000 | 0.132 | 0.239 | 0.096 |
| 100,000 | 0.886 | 1.961 | 0.800 |
| 300,000 | 3.148 | 6.272 | 2.265 |

Eight destructions cost 0.89 ms in a world of 100,000 and 3.1 ms in one of 300,000: 9 to 10 ns per live entity for
the world's step (about 1.4 of them the copy of section 5, the rest the release pass), whatever the number destroyed;
the calls themselves take 0.003 ms. Confirms PERF-3.

**The tile physics world** (PERF-3's scenarios, measured again for the comparison of section 7).

| Scenario | Load | Work p50 | per operation |
| --- | ---: | ---: | --- |
| `physics` (kinematic boxes in one 1024x576 level, one `Move` each) | 1,000 | 1.86 | 1.9 µs per move |
| | 2,000 | 5.50 | 2.8 µs |
| | 4,000 | 20.00 | 5.0 µs |
| `spatial-query` (static boxes at constant density; half as many queries) | 4,000 | 1.41 | 0.58 µs per overlap, 0.83 per ray |
| | 8,000 | 3.58 | 0.75 / 1.03 |
| | 32,000 | 23.33 | 1.27 / 1.63 |

## 5. The anomaly of PERF-3, explained: the step's copy and the placement of two arrays

PERF-3 recorded "at exactly 160,000 entities the update takes longer than at 240,000 or 320,000" as reproduced and
not explained. It appeared again here at 200,000 (the 5 to 6 ms of the culled runs above, and of the region scenario
in a window), in some scenarios and not in others of the same size.

**Measured.** A program outside the repository builds a `SceneWorld`, times `BeginStep` 200 times and reads where
the world's two transform arrays are (`_local`, and `_previous`, which `BeginStep` fills with a copy of the first).
Over six processes and ten worlds each:

| Entities (capacity) | `_previous` - `_local` | `BeginStep`, ns per entity |
| --- | --- | --- |
| 160,000 to 240,000 (262,144) | 10,485,816 bytes: the next array, **56 bytes past a whole number of 4,096-byte pages** | **23 to 33** in 21 worlds of 29, 12 to 20 in 4, 3.0 and 3.3 in 2, 1.4 in 2 |
| the same | any other distance (the arrays not adjacent) | 1.3 to 1.5 in every world |
| 100,000 (131,072) | 5,242,936 bytes, also 56 past a page | 1.4 to 1.5 |
| 320,000 to 1,200,000 | 0, 3,648 or 3,984 past a page (never adjacent in these runs) | 1.3 to 2.9 |

So a world of 131,073 to 262,144 entities whose two arrays were allocated one after the other, which is what
`Grow` does and what usually happens, takes **5 to 7 ms to start every step instead of 0.2 to 0.35 ms**. Rebuilt by
hand (one native block, the source 0x58 bytes into a page, the destination a chosen distance after it, one
`Span.CopyTo` of 6.4 to 9.6 MB): 4.4 to 7.3 ms when the distance is 24, 56 or 64 bytes past a whole number of pages,
0.21 to 0.31 ms at a whole number of pages, 0.32 to 0.70 ms at half a page.

**Hypothesis** (not isolated further): the runtime's copy of several megabytes uses a path (`rep movsb` or
non-temporal stores) that is pathologically slow on this processor when source and destination have nearly the same
offset within a page; the 5 MB copy of a 100,000-entity world takes another path. Not run on another processor: it
may be specific to AMD Zen 4, or to this runtime's `memmove`.

**Whether it shows** depends on where the collector put the arrays, which depends on what the game allocated before:
that is why PERF-3 saw it at "exactly 160,000" and here two scenarios of the same size differ.

## 6. Survey: libraries and algorithms

**Managed libraries** (NuGet's registration data read on 2026-11-16; the query signatures and project files read in
each repository's default branch; allocation behaviour from those signatures, not measured).

| Package | Latest | Licence | What it is | Why not |
| --- | --- | --- | --- | --- |
| RBush 4.0.0 | 2024-11-14 | MIT | R-tree, `net8.0` and `netstandard2.0`, no dependency | `Search` returns a new `IReadOnlyList<T>` per query; items implement `ISpatialData`; a move is a delete and an insert; no trimming or AOT declaration in its project |
| NetTopologySuite 2.6.0 | 2025-03-12 | BSD-3-Clause | geometry suite with `Quadtree`, `STRtree`, `KdTree`, `HPRtree` | `Query` returns an `IList<T>` or calls a visitor object; `Envelope` is a class of doubles; the packed trees are built once and not changed; `netstandard2.0`, no trimming or AOT declaration |
| QuadTrees 1.0.4 | 2021-09-30 | none declared | quadtree | no licence expression, not updated in five years |
| KdTree 1.4.1, Supercluster.KDTree 1.0.4, UltimateQuadTree 1.0.3 | 2017 to 2018 | MIT-style files | k-d trees and a quadtree | unmaintained; k-d trees are for static sets |
| Aether.Physics2D 2.2.0, BepuPhysics 2.5.0-beta.33, Box2D.NET 3.1.654 | 2025 to 2026 | MS-PL, Apache-2.0, none declared | physics engines with a dynamic tree inside | a physics engine as a dependency for one structure; the engine already has an optional Box2D backend, whose tree is not exposed through its flat C layer |
| MonoGame.Extended 6.1.1 | 2026-08-18 | MIT | collision layers with a quadtree or a spatial hash | depends on MonoGame |
| Arch 2.1.0, Friflo.Engine.ECS 3.6.0, DefaultEcs 0.17.2 | 2022 to 2026 | Apache-2.0, MIT, MIT-0 | ECS frameworks | a model a game must adopt, which the issue rules out; they index components, not places |

None earns a place: the two maintained candidates allocate per query, which the engine's frame does not (PERF-3:
zero bytes per frame in ten of eleven scenarios), and would put a second vocabulary of rectangles in a game's code.

**Algorithms**, against what the scenarios need (points and small rectangles of similar size, most of them moving
every step, queries about as large as the things queried).

| Structure | Good for | Here |
| --- | --- | --- |
| Uniform grid or spatial hash | objects of similar size, moving, uniform or clustered; O(1) to move, a query costs the cells it reaches | fits the scenarios; chosen. A hash rather than an array of cells, so the world needs no bounds and empty space costs nothing |
| Loose grid | the same with sizes varying within a factor of a few, one cell per object | not needed: an item is filed in the cells it covers (at most 64), larger ones apart |
| Quadtree | sizes or densities that differ by orders of magnitude | no measured scenario has them; deeper to walk and costlier to move than a grid |
| Sweep and prune | all pairs that overlap, coherent motion | answers "which pairs", not "what is near this"; degrades when many objects share an axis interval (a platformer's floor) |
| Bounding volume hierarchy (dynamic AABB tree) | rays, very different sizes, physics broad phases | what Box2D uses; more code and a rebalancing policy for no measured need. The candidate if ray or segment queries are added |
| k-d tree, packed R-tree | static sets, nearest neighbour | the items move every step |

## 7. Decisions, what was implemented, and after

**Conditions of the "after" numbers.** From 17:50 UTC the machine was in use: the game left open took four to five
of the six cores until the session ended (the idle check: load 41 to 66%; a watch of 25-second checks: 19 to 25 CPU
seconds used per 5 seconds, 20 times in a row). No clean campaign of the final build exists. What exists, and how
each table below is marked:

- **clean**: taken before 17:50 with an idle check that passed (the first index runs and its sweep, the comparison
  of the two index versions, the world step experiment on the code before the change).
- **in use, alternating**: the "before" and the "after" build run in turn, three rounds, each run in its own
  process (300 frames after 60), while the machine was in use. The before figures come out 1.2 to 1.3 times their
  clean values of section 4 (`physics` at 2,000: 6.6 to 7.0 ms against 5.5 clean), so **the ratios stand and the
  absolute figures and the tails do not**. The lowest median of the three rounds is quoted for each build.
- **in use**: the final build alone under that load: an upper bound, not a figure.
- **exact**: counts, checksums and allocations, which do not depend on the load.

No limit sweep of the final build was run: a p99 against a budget needs a quiet machine.

### 7.1 Nearby objects: implemented, `SpatialIndex2D<T>` (optimize by a minimal extension)

The need failed the external-developer check (section 4: under 2,000 by a walk; 6,000 with a body each, and a body
for everything). No library passed the survey. Added: a public type of `MicroEngine.Scene`, opt-in, beside the world
([ADR 0058](../decisions/0058-spatial-index-beside-the-world.md)): items of any type under a point or a rectangle,
`Query` by rectangle and `QueryCircle`, into a span or a list, `Move` and `Remove` through a generational
`SpatialHandle`. A hash of cells with chained nodes that carry each item's edges; an item over more than 64 cells is
kept apart; an area of more cells than there are items walks the items. No allocation per query, move or removal
(tested), an order that depends only on the calls made (tested), no reflection (nothing for trimming or NativeAOT to
break; neither build was run). 14 tests, among them a randomized comparison with a walk over every item for three
cell sizes.

`neighbours-index`, every agent moved and asked every step (ms, p50):

| Agents | First version, clean | Final version, clean | Final, in use | per agent (final, clean) |
| ---: | ---: | ---: | ---: | ---: |
| 4,000 | 1.52 | 1.37 | 1.57 | 0.34 µs |
| 8,000 | 3.55 | | | |
| 20,000 | 10.46 to 10.80 | 8.75 (p99 9.64) | 12.72 | 0.44 µs |
| 50,000 | 33.1 | 28.7 | 39.3 | 0.57 µs |
| 100,000 | 72.2 | | | |

- **Limit, measured clean on the first version: 25,000 to 27,500 agents** (the walk: 1,500 to 2,000; the physics
  world: 6,000 to 6,500 before, section 7.3 after). The final version is 10 to 17% faster at the three loads
  measured; its limit was not swept.
- The cost per agent grows with the world (0.34 to 0.57 µs from 4,000 to 50,000): the nodes of a cell are not
  contiguous in memory. Hypothesis, not isolated: cache misses on the chains.
- Per operation (`benchmarks/WorldBench`, 20,000 points, best of 9, in use): `Add` 105 ns; `Move` within the cell
  4.2 ns, to another cell 103 ns; `QueryCircle` finding 13: 417 to 437 ns; `Query` 96x96 finding 16: 423 to 461 ns;
  a query that returns a tenth of everything (2,000 of 20,000, into a list): 25.5 to 26.4 µs, **no better than a
  plain loop over 20,000 positions in an array (22.8 to 23.8 µs)**. The index pays when the answer is a small part
  of the world.

### 7.2 A world larger than the view: implemented as a use of the index; no region system

`large-world-region` (200,000 entities; the view and its 96-pixel margin, 512x372 pixels, hold about 6,600, which are
moved; 2,300 are drawn; ms, p50):

| | Work | Update | Draw submission | Present | Conditions |
| --- | ---: | ---: | ---: | ---: | --- |
| 200,000, headless (first version) | 0.61 | 0.52 | 0.10 | | clean |
| 200,000, headless (final) | 0.70 | 0.58 | 0.12 | | in use |
| 200,000, window (final) | 0.93 | 0.62 | 0.30 | 0.21 | in use |
| 1,000,000, headless (first version) | 3.05 | 2.78 | 0.26 | | clean |
| 1,000,000, window (final) | 4.06 | 3.45 | 0.60 | 0.29 | in use |
| for comparison, 200,000 all moved and drawn, window | 17.45 | 1.71 | 15.68 | 15.49 | clean (section 4) |
| for comparison, 200,000 all moved, drawn if visible, window | 5.35 to 5.77 | 1.5 to 1.8 | 3.8 to 4.2 | 0.6 | in use, without the anomaly of section 5 |

A world of 200,000 entities costs under a millisecond when only what is near the view is touched, against a whole
frame when everything is drawn and 5 ms when everything is tested for visibility. At a million, 3 ms, of which 1.9
is the world keeping the step's start of a million transforms (not the index).

**Decision: no region, chunk or activation system.** The query with `Camera2D.VisibleBounds` is the whole
mechanism; which entities sleep and what sleeping means are game rules, and a system that decided them would be the
compulsory model the issue rules out. **Loading by region: defer.** Scopes, `Preload` and one map per region cover
it (section 2); streaming one map in parts has no measured need: tile drawing already costs the view, not the map,
and tile collisions read only the cells a body touches.

### 7.3 The tile backend's broad phase: optimized by reusing the index

PERF-3 ranked "the cost per `Move` grows with the crowd" first and listed three hypotheses. In isolation (a program
outside the repository gives the grid of the branch point, the same without its sort, and the current one the same
calls; candidate counts are **exact**, times are in use, lowest of 40 steps after 120, two runs):

| Workload | Shapes | Candidates per query, before | Shapes that touch the area (the candidates now) | Query before / before without sort / now, ns |
| --- | ---: | ---: | ---: | --- |
| the `physics` level (12x14 boxes in 1024x576), a mover's reach | 2,000 | 29.0 | 4.2 | 250 / 104 / 279 |
| | 4,000 | 57.4 | 7.3 | 559 / 180 / 545 |
| `spatial-query` (16x16 boxes, one per 40x40), a 64x64 area | 4,000 | 13.7 | 5.0 | 290 / 213 / 240 |
| | 32,000 | 13.9 | 5.0 | 848 / 722 / 355 |

- **"Other movers gathered as obstacles": demonstrated, and the main cost.** The grid returned every shape of the
  64-unit cells the area reached: seven to eight times the shapes that touch it in the dense level. Everything after
  the query (filters, sweeps, contact probes) ran over all of them.
- **"The candidate list sorted on every query": demonstrated, secondary.** 58 to 68% of the old query at 29 to 57
  candidates, 0.15 to 0.4 µs of a 2.8 to 5.0 µs move.
- **"`List.Remove` when a moved shape is refiled": not a cost here.** Refiling took 9 ns per shape (a body that
  moves a pixel rarely changes cells).
- **PERF-3 §8.7, "queries grow with the world at constant density"**: supported. With equal candidates the old
  query without its sort goes from 213 to 722 ns between 4,000 and 32,000 shapes (the dictionary of cell lists); the
  current one from 240 to 355.

**Implemented**: `ShapeGrid` files box shapes in a `SpatialIndex2D<int>`, which gives back the shapes that touch the
area, and sorts those; large shapes stay in its own list every query includes. Candidates are still in shape-slot
order. Its query alone is not faster in the dense level (279 against 250 ns at 2,000): the gain is everything that
no longer runs over shapes that could not be hit.

| Scenario | Load | Before, ms | After, ms | Change | Conditions |
| --- | ---: | ---: | ---: | ---: | --- |
| `physics` | 1,000 | 2.36 | 1.02 | -57% | in use, alternating |
| | 2,000 | 6.57 | 2.52 | -62% | |
| | 4,000 | 24.02 | 9.20 | -62% | |
| | 8,000 | not run | 25.0 | | in use |
| `spatial-query` | 8,000 | 4.59 | 2.32 | -49% | in use, alternating |
| | 32,000 | 41.97 | 16.37 | -61% | |
| `neighbours-physics` | 4,000 | 10.51 | 6.31 | -40% | |
| `integrated` (PERF-3: 93% physics), headless | 5,000 | 3.51 | 1.51 | -57% | |
| `integrated`, window | 5,000 | 4.32 | 2.52 | -42% | |

- Allocation per frame (exact): `physics` 38, 59 and 95 bytes at 1,000, 2,000 and 4,000 before, 0 after;
  `neighbours-physics` 77 before, 0 after; `integrated` 19 before, 3 after.
- **The limit of `physics` was 3,250 to 3,500 bodies; 4,000 now take 9.2 ms in use** (p99 11.6 to 13.4) and 8,000
  take 25: the limit is between the two and was not swept. A move costs 1.0 µs at 1,000 bodies and 2.3 at 4,000 (in
  use), against 1.9 and 5.0 clean before. It still grows with the crowd: in this level 4,000 boxes cover more than
  the level's area.
- `neighbours-physics` after, under a light load (17:46, the game at 0.4 of a core): 1.11 ms at 1,000, 5.59 at
  4,000, 12.40 at 8,000 (p99 14.6), 29.7 at 16,000. Its limit moved from 6,000 to 6,500 to somewhere above 8,000;
  the index (25,000 to 27,500) is still three times that and needs no body.
- **Results are the same**: the broad-phase tests (every move, query and event equal to a scan of every shape, for
  three cell sizes) and the physics conformance suites pass unchanged, and StressBench's suite gives the same
  checksum on both builds for every scenario both have (240 frames after 60; sixteen scenarios).

### 7.4 Releasing destroyed entities: optimized

A release marked nothing and then walked everything. Now the creation order and each attachment store keep the holes
a release leaves (every walk already skipped a hole) and close them, in order, once the holes are an eighth of their
length. Amortized, a release costs what it releases. Iteration order, attach order and the state hash are unchanged:
a 300-step history of destruction, detachment and slot reuse was written as a test against the previous code (order
checked against a plain list at every step, the folded hash pinned), and passes on the new one.

| Scenario | Load | Before, ms | After, ms | Change | Conditions |
| --- | ---: | ---: | ---: | ---: | --- |
| `entity-trickle` (8 replaced per step) | 100,000 | 1.02 | 0.150 | -85% | in use, alternating |
| | 300,000 | 3.55 | 0.417 | -88% | |
| `entity-churn` (a tenth replaced per step) | 10,000 | 0.47 | 0.44 | -5% | |
| | 100,000 | 2.62 | 2.33 | -11% | |
| | 300,000 | 9.16 | 6.51 | -29% | |

What is left of a trickle step is the copy of section 5 (1.3 to 1.5 ns per entity). A step that destroys and creates
one entity among 200,000 costs 0.89 µs besides it (WorldBench, in use). With a tenth of the world replaced every
step the holes reach the threshold almost every step, so the pass still runs: the gain there is smaller and is not
the case the change is for. PERF-3's limit of `entity-churn` (300,000 to 320,000) was not swept again.

### 7.5 The step's copy: fixed

Both transforms of every slot now live in one array (the local ones in the first half, the step-start ones in the
second), so the copy's source and destination are a whole number of pages apart whatever the collector does, and the
copy is the single one it was.

| | Before | After | Conditions |
| --- | --- | --- | --- |
| `BeginStep`, ns per entity, worlds of 100,000 to 320,000 | 1.3 to 1.5, or **12 to 33** when the arrays are adjacent (25 of 29 such worlds of 160,000 to 240,000) | 1.33 to 1.72 in 60 worlds, none slow | before: clean; after: in use (lowest and highest of 12 worlds per size) |
| the same, 600,000 and 1,200,000 | 1.5 to 2.9 | 1.7 to 2.7 | |
| `large-world-culled`, 160,000, update p50 of three rounds, ms | 6.37, 4.51, 1.26 | 1.10, 1.18, 1.14 | in use, alternating |
| `world-iterate`, 160,000, update p50 of three rounds, ms | 1.19, 1.22, 6.52 | 1.28, 1.14, 1.17 | |

A first attempt copied in blocks of 8,192 transforms. It was the fastest in the experiment that calls `BeginStep`
in a loop (0.57 ns per entity: the data stays in the cache) and the slowest in a game, where it does not: in
alternating runs of three builds, `large-world-region` at a million entities took 7.1 to 7.4 ms with blocks and 5.0
to 5.1 with the single copy, and `world-iterate` at 320,000 was 0.5 ms slower with blocks than before the change.
Discarded.

What the rest of the world's paths cost after the change (WorldBench, 1,000 entities, best of 9, two runs of each
build, in use): `Local` read 1.33 to 1.36 ns (1.32 to 1.34 before), `Local` write 1.57 to 1.64 (1.55 to 1.56),
`DrawPosition2D` 2.87 to 2.88 for a root (2.76 to 2.81) and 9.16 to 9.18 under a translated parent (8.95 to 8.96),
create, attach, destroy and release 105 to 109 per entity (108 to 109). **Reading a drawn position costs 2 to 4%
more** (an addition to find the step-start half). Scenarios that only iterate are unchanged in alternating runs
(`world-iterate` at 20,000: 0.50 against 0.49 ms; at 320,000: 7.25 against 6.67; `neighbours-brute` at 1,000: 3.96
against 3.86).

### 7.6 Deferred or rejected, with the number behind each

| Candidate | Decision | Why |
| --- | --- | --- |
| A region, chunk or activation system in the engine | defer | a query does it (0.6 to 0.9 ms for 200,000 entities); the policy is the game's; the issue forbids a compulsory one |
| Streaming one Tiled map in parts, Tiled `.world` files | defer | nothing measured needs it; a map's cost is its memory, and one map per region with a scope is available today |
| Nearest-first, ray and segment queries on the index | defer | no scenario needed them; a caller picks the nearest of a dozen results itself; the physics world has rays for bodies |
| A quadtree, a loose grid or a hierarchy behind the index | defer | no measured scenario has sizes that differ by orders of magnitude; the API hides the structure |
| Contiguous cells (a rebuild per step instead of linked nodes) | defer | would address the growth of 0.34 to 0.57 µs per query with the world (hypothesis); 20,000 agents already fit a frame |
| Queries on `IWorld`, an index kept by the world | rejected | the world cannot see transform writes; every scene would pay for it |
| Engine-side culling of entity draws | rejected | drawing is the game's call; `IsVisible` and a view query exist |
| The release pass for worlds that replace a tenth of themselves every step | defer | 6.5 ms at 300,000 after (in use); stores without a stable order would change a contract |
| `DrawPosition2D` under a rotated parent (PERF-3 §8.9) | not WORLD-1's | unchanged: 90 to 93 ns |
| The copy at the start of every step (1.3 to 2.7 ns per entity) | defer | the world cannot know what moved without changing `Local`; 0.3 ms at 200,000 |

## 8. What a developer and a player would perceive

- **A developer** can now write, in a few lines and with the published types, a swarm, tower targeting, bullets
  against enemies kept as data, or a world many screens wide that only works near the view: none needs a body, a
  grid of its own or an ECS. That is the change that matters, and it is a capability, not a speed.
- **A player of a game with a few hundred objects** perceives nothing: every figure here is far below a frame at
  that size, before and after.
- **A player of a game with thousands of colliding actors** in the tile physics world gets steps that take about
  40% of the time they took (demonstrated as a ratio; the absolute figures were taken on a busy machine): a crowd of
  2,000 that took a third of a 60 Hz frame takes about a seventh.
- **A game with 130,000 to 260,000 entities** no longer loses 5 to 7 ms of every step to the placement of two
  arrays. Whether a given game hit it depended on what it had allocated before; such games are rare.
- **A game that destroys a few entities per step in a world of 100,000 or more** (bullets expiring in a large
  simulation) gets back 0.7 ms per step at 100,000 and 2.7 ms at 300,000.
- Not demonstrated: anything on another processor, system or runtime (the page-distance effect of section 5 may be
  specific to this processor), any phone or browser, NativeAOT, Box2D (not built in this checkout).

## 9. Deferred work and open questions

Recorded here instead of new issues, as the task asked.

1. **A clean campaign of the final build**: the limits of `physics`, `neighbours-physics`, `neighbours-index` and
   `entity-churn`, and PERF-3's baseline comparison (section 10 has the commands). Until then the "after" columns
   are ratios.
2. **Why the copy is slow at 56 bytes past a page**, and whether other processors show it. The fix does not depend
   on the answer.
3. **Nearest-first, ray and segment queries** on the index, when a game asks.
4. **The index's cost per query grows with the world** (0.34 to 0.57 µs). Contiguous cells would test the cache
   hypothesis.
5. **Dense kinematic crowds still grow per move** (1.0 µs at 1,000 bodies, 2.3 at 4,000, in use). The remaining
   cost is the sweep against the shapes that really are in the way; not profiled further.
6. **Box2D** was not run in any scenario (its native library is not built in this checkout): 98 of its tests skip.
7. **PERF-3's proposal of a CI job on allocations and checksums** would now also cover the three neighbour
   scenarios' common checksum; workflows were outside this task.

## 10. Reproduction and limitations

```
dotnet test MicroEngine.slnx -c Release
dotnet run -c Release --project benchmarks/WorldBench
dotnet publish benchmarks/StressBench -c Release -o OUT/after
docs/evaluations/perf-3/campaign.sh OUT idle
OUT/after/StressBench run --scenario neighbours-brute,neighbours-physics,neighbours-index --repeat 3 --isolate
OUT/after/StressBench run --scenario large-world,large-world-culled,large-world-region --mode headless,render --repeat 3 --isolate
OUT/after/StressBench run --scenario entity-trickle --load 100000 --repeat 3 --isolate
OUT/after/StressBench sweep --scenario physics --frames 240 --warmup 60 --repeat 2 --isolate
OUT/after/StressBench suite --mode headless,render --repeat 3 --out OUT/suite.json
OUT/after/StressBench compare docs/evaluations/perf-3/baseline.json OUT/suite.json
```

A "before" build is the same publish at `ec78a01` (the scenarios without the index). To compare two builds on a
machine that is not quiet, run them in turn, each run in its own process, and compare the lowest medians. The
programs that measured `BeginStep` against the placement of the arrays and the three broad phases side by side were
written outside the repository for this evaluation and are not part of it; their method is in sections 5 and 7.3.

Limitations: one machine, one system, one runtime; the "after" timings were taken on a machine in use; the scenarios
are synthetic and a game's logic comes on top of every figure; GPU time is not measured; nothing was run on Linux,
macOS, phones, browsers, NativeAOT or with Box2D.


# 0056 — Navigation is an optional SDK package of the engine's own managed code

Status: Accepted (2026-11-14): the owner accepted it on the ground that the SDK is in Alpha, where a new package or
a change of public API carries no compatibility commitment. Amends [0019](0019-sdk-packages.md) (the published package set). Evidence:
[NAV-1](../evaluations/nav-1.md). Task record: [`odd/tasks/nav-1.md`](../../odd/tasks/nav-1.md).
Amended by [ADR 0069](0069-navigation-agents.md) (2026-11-20): the package also moves agents (following a route,
arriving, searching again, keeping apart), searches for agents wider than a cell and answers what can be reached; it
has a scene system. "Behaviour stays in games" now means what an agent wants, not how it gets there.

## Context

The SDK describes where things are (`TileGrid`, `IPhysicsWorld2D`, maps from Tiled) and moves boxes along what they
hit, but nothing finds a route. A developer outside the repository cannot build an enemy that chases the player
around walls, units that walk to a clicked point, or a patrol that re-plans when a door closes without writing a path
search, its queue, its tie rules and its link to the tile map: the same few hundred lines in every top-down,
strategy or tower-defence game. NAV-1 surveyed what could be reused. For grids and graphs every maintained managed
library is either a general graph library that knows nothing of cells, a roguelike toolkit with three dependencies,
or stale; the .NET `PriorityQueue` has no priority update and no stable order. For navmeshes DotRecast is
maintained, but it is a 3D toolchain that declares no AOT compatibility, and Recast itself would need a native build
per runtime, as Box2D does.

## Decision

- **`MicroEngine.Navigation` is an optional SDK package**, released with the other packages, at the same version,
  by the same release workflow and approval (ADR 0022). No other package, the `dotnet new` template or a demo
  depends on it. It depends on `MicroEngine` only.
- **It is the engine's own managed code**: no third-party solver and no native library, so it runs wherever the
  engine runs and is marked AOT compatible.
- **One search, behind an interface.** `PathFinder` runs A* over `INavGraph` (nodes, edges, an estimate). `NavGrid`
  (cells, for tile maps) and `NavGraph` (positions joined by hand, with directed tagged links for jumps, drops and
  doors) implement it; a game implements it for its own space, 3D included.
- **Space is a copy the game owns.** A grid is read from a `TileGrid` or sampled from an `IPhysicsWorld2D` and
  changed when the level changes; its `Version` tells agents when to check their paths. The package registers no
  module, no system and no scene type.
- **Paths are deterministic and allocation-free once warm.** Ties are broken by a total order, never by the queue's
  shape, so the same question gives the same path on every runtime.
- **Behaviour stays in games.** The package never decides what an agent wants, when it re-plans or how it moves
  along a route.
- **Not built now**: 2D navmeshes, generated platformer links, clearance for agents wider than a cell during the
  search, local avoidance, hierarchical search and jump point search. Each waits for a measured need
  ([NAV-1](../evaluations/nav-1.md), section 7).

## Alternatives considered

- **A bundled assembly inside the `MicroEngine` package**, like saves and achievements: no tenth package to release,
  but every game carries it and the newest, least proven API sits in the core package's surface. It remains a cheap
  change if the owner prefers it: the project moves into `MicroEngine.csproj`'s bundle and leaves the package list.
- **A package over QuikGraph, GoRogue or DotRecast**: more dependency than wrapped code for grids and graphs; the
  wrong tier first for meshes.
- **A documented recipe**: every game would still write the solver.

## Consequences

- The release publishes one more package (`sdk/version.mjs`), and its check verifies the package's contents; the
  documentation check builds and plays `samples/Pathfinding` from the packed feed.
- Correctness is the engine's to keep: the tests compare every path with an exhaustive search.
- A navmesh, when a game needs one, plugs in as another `INavGraph` (possibly over DotRecast) without changing games
  that use grids.

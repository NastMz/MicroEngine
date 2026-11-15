# NAV-1: optional 2D/3D pathfinding and navigation

Objective: decide, with evidence, whether MicroEngine needs a navigation module, what can be reused, and implement the
generic primitives an external developer cannot build a game without today. Issue NastMz/MicroEngine#112. Branch
`feat/nav-1`. Delivery strategy: `ask-on-risk` (no push, pull request or release without the owner). Test runner:
`dotnet test MicroEngine.slnx -c Release`. Evaluation: [`docs/evaluations/nav-1.md`](../../docs/evaluations/nav-1.md).
Decision: [ADR 0056](../../docs/decisions/0056-navigation-optional-package.md).

## Specs

- S1 Opportunity: "NPCs may need paths through grids, walls, obstacles or platforms. Navigation/pathfinding is general
  infrastructure; goals and behaviors belong to each game."
- S2 Techniques: "Compare grid A*, graph search, 2D navmeshes, platformer movement links and future 3D reuse."
- S3 Reuse first: "Investigate mature managed and native implementations before proposing a custom solver."
- S4 Evaluate: "Evaluate representation of traversable space, dynamic obstacles, integration with scenes/physics,
  performance, packaging and module optionality."
- S5 Consumers: "Use Courtyard Key or Trail Duel as possible consumers if evidence warrants a prototype."
- S6 Done when: "Recommend whether MicroEngine needs an optional navigation module, what can be reused and which
  actual game need would justify it."
- S7 Judge by an external developer (owner's rule, as in UI-1 S1, PHYS-1 S10 and TILE-1 S6): could someone outside the
  repository build a typical game of that style with the SDK today (an enemy chasing the player around walls in a
  top-down tile map; units walking to a clicked point; a patrol that re-plans when a door closes). The research is the
  starting point; the deliverable is the generic primitives that fail that check, each with an example and tests.
  Only what has a concrete cost reason is deferred, and the reason is recorded.
- S8 Scope limits (this task's brief): the demos (`games/**`) are not changed; no existing public API of another
  package changes; nothing is pushed, published or released.

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 Map the tile types, the physics contract, the scene world, the Tiled package and the package wiring | S4 | inline | done | evaluation §1 |
| T2 Survey managed and native implementations and the techniques (licences, AOT, WebAssembly, maintenance) | S2 S3 | delegated (research in parallel with T1; read-only) | done | evaluation §2–§3 |
| T3 `MicroEngine.Navigation`: `PathFinder` over `INavGraph`, `NavGrid`, `NavGraph`, `DistanceField`, tests | S1 S4 S7 | inline | done | `739ab29`, `b16cfde`; RED: 40 of 41 failing on stubs; `MicroEngine.Navigation.Tests` 41/41 (L3) |
| T4 Package wiring: solution, package list and checks, samples' targets, architecture test, API reference list | S4 | inline | done | `739ab29`; version tests 29/29, architecture tests 21/21, `dotnet pack` deps: MicroEngine only (L4) |
| T5 Example `samples/Pathfinding` (chase, click to move, a patrol and a door, a swarm; platform links and a hex board) with its tests and tour | S7 | inline | done | `2925840` and the documentation commit; `PathfindingTests` 7/7; tour captures inspected (L5) |
| T6 Benchmark `benchmarks/NavBench` on large maps | S4 | inline | done | `2925840`; evaluation §5 (L6) |
| T7 Evaluation, ADR 0056, guide page, API reference, release notes, SDK page, architecture table | S2–S6 | inline | done | the documentation commit; snippets check, `api-reference.mjs --check`, `version.mjs check` (L7) |
| T8 Owner decision on the package (ADR 0056 is Proposed) | S6 | owner | pending | — |

## Log

- L1 (2026-11-14) The issue, verbatim:

  > ## Opportunity
  > NPCs may need paths through grids, walls, obstacles or platforms. Navigation/pathfinding is general
  > infrastructure; goals and behaviors belong to each game.
  >
  > ## Investigate
  > - Compare grid A*, graph search, 2D navmeshes, platformer movement links and future 3D reuse.
  > - Investigate mature managed and native implementations before proposing a custom solver.
  > - Evaluate representation of traversable space, dynamic obstacles, integration with scenes/physics, performance,
  >   packaging and module optionality.
  > - Use Courtyard Key or Trail Duel as possible consumers if evidence warrants a prototype.
  >
  > ## Done when
  > Recommend whether MicroEngine needs an optional navigation module, what can be reused and which actual game need
  > would justify it.
  >
  > **Status:** Idea — implementation not approved.

  The brief that opened this task adds S7 and S8 and asks for the implementation the evidence justifies.
- L2 (2026-11-14) Mapping and survey. Nothing in the SDK searches a path: `TileGrid` answers "what is in this cell"
  and `IPhysicsWorld2D` answers moves, rays and overlaps. Every maintained managed candidate either brings a general
  graph library without grid support (QuikGraph, MS-PL, 2022), is stale (RoyT.AStar 2020, the JPS, HPA* and flow-field
  packages 2017 to 2022), pulls three dependencies of a roguelike toolkit (GoRogue 3 betas), or is a 3D navmesh
  toolchain that declares no AOT compatibility (DotRecast); Recast has no .NET binding with prebuilt natives. The .NET
  `PriorityQueue` has no priority update and no stable order. Recommendation: a small managed module of the engine's
  own, as an optional package.
- L3 (2026-11-14) `MicroEngine.Navigation` written test-first: the public API as stubs, 40 of 41 tests failing, then the
  implementation. Paths are compared with an exhaustive Dijkstra search without a heap or an estimate on random grids
  (three diagonal modes, walls, costs), 3D lattices, directed graphs with free links and uneven admissible estimates.
  One expectation was a pin, not a rule: which of the equal-cost paths an open grid returns (set to the observed one;
  the rule tested is that it never changes between finders and runs).
- L4 (2026-11-14) Wiring found by searching for `MicroEngine.Tiled`: `MicroEngine.slnx`, `sdk/version.mjs` (tenth
  package, absent from every published version), `sdk/tests/packages.mjs`, `sdk/tests/docs.mjs`,
  `samples/Directory.Build.targets`, the architecture test, and `site/scripts/api-reference.mjs` (the assembly list,
  the namespace's guide and description, the projects built). The last file was outside the edit surfaces named in the
  brief and is needed to generate `docs/api` for a new assembly; the edit is three list entries. No workflow lists
  packages or test projects by name, so `.github/workflows` is unchanged.
- L5 (2026-11-14) The sample plays the three scenarios of S7 plus a swarm on one distance field; its door is a tile
  whose flags change, read back into the grid. The first tour pressed the door while the patrol stood in it and the
  door (rightly) stayed open; the tour now closes it ahead of the patrol. Captures `routes.png`, `around.png` and
  `gathered.png` were looked at: routes skirt corners at the walkers' half-size and the patrol goes around under the
  wall.
- L6 (2026-11-14) NavBench (Windows x64, .NET 10.0.15, other builds running on the machine: rows varied up to 2×
  between runs): a search across a 128 × 128 map about 0.3 ms, across 1000 × 1000 about 17 ms (61,187 cells looked
  at), a walled-in goal 149 ms unbounded and 3.5 ms with `MaxExpansions = 20000`, a whole-map field 127 ms; no
  allocation in any search row.
- L7 (2026-11-14) Recommendation recorded as ADR 0056 with status Proposed: a new package changes the published set,
  which earlier records reserve for the owner (PHYS-1 S8, TILE-1 S7). The alternative that needs no new package (the
  same assembly bundled in `MicroEngine`) is in the ADR. Deferred with reasons (evaluation §7): 2D navmeshes,
  generated platformer links, clearance for agents wider than a cell, crowds, hierarchical search and JPS, 3D meshes.
  Not verified: a NativeAOT or browser build of a consumer, `FromPhysics` on the Box2D backend, the site build (its
  `node_modules` are not installed in this worktree; every public page's entry was built with `repo-docs.mjs`
  instead: 549 pages, links and quoted code resolved). Next: the owner's decision (T8).

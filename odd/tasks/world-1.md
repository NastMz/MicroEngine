# WORLD-1: 2D world scalability and spatial queries

Objective: measure where a large or dense 2D world stops, name a reproducible consumer scenario for each limit, and for
each candidate decide to optimize, reuse an external component or defer, implementing what the evidence justifies.
Issue NastMz/MicroEngine#126. Branch `feat/world-1`, from `main` at d29b65e. Delivery: local commits per work unit; no
push, pull request or release without the owner. Test runner: `dotnet test MicroEngine.slnx -c Release`. Evaluation:
[`docs/evaluations/world-1.md`](../../docs/evaluations/world-1.md). Decision:
[ADR 0058](../../docs/decisions/0058-spatial-index-beside-the-world.md). Builds on [PERF-3](perf-3.md), whose section
5.1 holds the starting numbers.

## Specs

Quoted fragments are the issue's text, verbatim (L1).

- S1 Why: "Dense or large 2D games may need efficient spatial queries, region activation and visibility policies.
  Investigate only after establishing current limits; do not introduce a compulsory ECS/chunk system."
- S2 Iteration and nearby objects: "Entity/world iteration costs and spatial indexing for nearby-object queries."
- S3 Regions: "Updating, rendering and loading only relevant regions for larger maps."
- S4 Responsibilities: "Coordination with physics broad phase, camera view and asset scopes without conflating
  responsibilities."
- S5 Reuse and stress cases: "Existing libraries/algorithms vs minimal extensions; profile real stress cases including
  PERF-3."
- S6 Done when: "Measured evidence identifies any scalability limit, a reproducible consumer scenario and a
  recommendation to optimize, reuse an external component or defer."
- S7 Status: "Conditional idea, not a beta blocker."
- S8 Judge by an external developer (owner's rule, as in NAV-1 S7): could someone outside the repository build that
  game today with the published SDK (a swarm whose members look for their neighbours, towers choosing targets, bullets
  against enemies without physics bodies, a world much larger than the view). A research issue ends in the
  implementation the evidence justifies; what is deferred carries its measured reason. The engine is in Alpha: a new
  public type or a behaviour change is valid and is recorded (ADR, release notes), not asked.
- S9 Bounds (this task's brief): nothing compulsory (no ECS, no chunk system a game must adopt); anything added is
  opt-in and genre-neutral; no redesign of an existing subsystem beyond what a measured limit justifies; existing
  contracts stay (the tile backend's candidates in shape-slot order, the world's state hash and iteration order); no
  new GitHub issue; nothing pushed, published or released.
- S10 Evidence words (this task's brief): the evaluation and the report distinguish what a measurement demonstrated,
  what is a hypothesis, and what a player or a developer would perceive.
- S11 Assumption: the budget every limit is read against is one 60 Hz update, 16.667 ms of CPU work at the 99th
  percentile, as in PERF-3; the scenarios are synthetic and a game's own logic comes on top.

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 Consumer scenarios in StressBench, measured on the code as it is (neighbours by a walk and through the physics world, a world larger than the view, a few destructions in a large world) | S2 S3 S5 S6 | inline | done | `ec78a01`; evaluation §3, §4 (L2) |
| T2 Where the tile broad phase and the world's step spend their time (PERF-3's hypotheses and its anomaly, isolated) | S4 S5 | inline | done | evaluation §5, §7.3 (L3, L6) |
| T3 Survey of managed libraries and the standard algorithms | S5 | inline | done | evaluation §6 (L4) |
| T4 `SpatialIndex2D<T>`, test first | S2 S3 S6 S8 S9 | inline | done | `b97ae1c`, `75f4b3a`, `b41264e`; RED: 14 of 14 failing on stubs, and the parameter name of `b41264e` seen wrong first; `SpatialIndex2DTests` 14/14 (L5) |
| T5 The world's release and step copy; the tile broad phase on the index | S2 S4 S9 | inline | done | `0e13537`, `c2e288a`; hash of a 300-step history pinned on the previous code; broad-phase tests unchanged; StressBench checksums equal (L6, L7) |
| T6 Example, guide page, API reference, release notes | S6 S8 | inline | done | `0a1c955` and the records commit; `NearbyExampleTests`; capture `32-nearby.png` inspected (L8) |
| T7 After numbers, evaluation, ADR 0058, architecture, AGENTS.md | S1-S10 | inline | done | the records commit; evaluation §7 to §10 (L9) |

## Log

- L1 (2026-11-16) The issue, verbatim:

  > WORLD-1 — Investigate 2D world scalability and spatial queries
  >
  > Why: Dense or large 2D games may need efficient spatial queries, region activation and visibility policies.
  > Investigate only after establishing current limits; do not introduce a compulsory ECS/chunk system.
  >
  > Research:
  > - Entity/world iteration costs and spatial indexing for nearby-object queries.
  > - Updating, rendering and loading only relevant regions for larger maps.
  > - Coordination with physics broad phase, camera view and asset scopes without conflating responsibilities.
  > - Existing libraries/algorithms vs minimal extensions; profile real stress cases including PERF-3.
  >
  > Done when: Measured evidence identifies any scalability limit, a reproducible consumer scenario and a
  > recommendation to optimize, reuse an external component or defer.
  >
  > Status: Conditional idea, not a beta blocker.

  The brief that opened this task adds S8 to S10 and asks for the implementation the evidence justifies.
- L2 (2026-11-16) Before anything was designed (machine idle, check recorded in the evaluation): a walk over every
  agent for every agent stops between 1,500 and 2,000 agents (3.1 ns per pair); the tile physics world with a body
  each at 6,000 to 6,500 (1.6 to 2.3 µs per agent); 200,000 entities drawn in a window take 15.7 ms of draw
  submission for 2,300 visible, and culling with `Camera2D.IsVisible` still walks the world (3.2 ms); eight
  destructions cost 0.89 ms among 100,000 entities and 3.1 ms among 300,000.
- L3 (2026-11-16) PERF-3's anomaly at "exactly 160,000 entities" reproduced at 200,000 and explained: `BeginStep`
  copies `_local` onto `_previous`, and when the two arrays are adjacent (56 bytes past a whole number of memory
  pages apart) the copy of a world of 131,073 to 262,144 entities costs 23 to 33 ns per entity instead of 1.3 to
  1.5 (5 to 7 ms a step). Reproduced by hand with one native block. The mechanism inside the runtime's copy is a
  hypothesis; nothing was run on another processor.
- L4 (2026-11-16) Survey: RBush and NetTopologySuite are the maintained candidates; both return a new list per query
  and declare nothing about trimming or AOT (read in their sources). The rest are unmaintained, tied to another
  framework, physics engines or ECS frameworks. A uniform grid behind a hash fits the scenarios; quadtrees, loose
  grids, sweep and prune and hierarchies have no measured scenario asking for them.
- L5 (2026-11-16) `SpatialIndex2D<T>` written test first (the API as stubs, 14 of 14 tests failing, then the
  implementation). The randomized test against a walk over every item hung on the first implementation: growing the
  bucket table walked the node chain of the item being filed, whose link was stale. Fixed before the first commit;
  that test and the allocation test cover it. A second pass put each item's edges in its cell nodes (10 to 17% less
  per query). The three neighbour scenarios of StressBench end in the same checksum, asserted by a test.
- L6 (2026-11-16) The world. Release: the creation order and the attachment stores keep their holes and close them
  once they are an eighth of their length. A test of 300 steps of destruction, detachment and reuse was written
  first and run on the unchanged world (attach order against a plain list at every step; the folded state hash
  pinned as 6352569735638433876), then the change was made. Step copy: a first attempt copied in blocks, which is
  faster when the transforms are in the cache and slower in a running game where they are not (the alternating runs
  of three builds showed it); the final change is one array for both transform sets, whose halves are a whole number
  of pages apart, and the single copy as before: 1.3 to 1.7 ns per entity in 60 worlds of 100,000 to 320,000, none
  slow.
- L7 (2026-11-16) The tile broad phase. In isolation (the old grid, the old grid without its sort and the new one
  given the same calls) the old one returned 15 to 57 candidates per move in the dense level where 2.6 to 7.3 shapes
  touch the area; the new one returns exactly those. `ShapeGrid` keeps its own list of large shapes and its sort, so
  the existing broad-phase test passes unchanged. StressBench's suite on the build of the branch point and on the
  final build: every scenario both have ends in the same checksum.
- L8 (2026-11-16) The example (3,000 fireflies, a lantern) and the guide page `docs/guide/large-worlds.md`, quoted
  from it. The tour's capture was looked at: 268 awake, 119 drawn, 8 lit inside the lantern's circle. The page is
  registered in `site/src/lib/repo-docs.mjs`; `site/scripts/api-reference.mjs` gained a phrase in the namespace's
  description. No package was added, so `sdk/version.mjs` and the solution are unchanged.
- L9 (2026-11-16) After numbers. From 17:50 UTC the machine was in use (a game took four to five of its six cores
  until the session ended; the idle check and a 25-second watch said so 20 times in a row), so the comparisons of the
  final build are alternating runs of both builds in that state, good for ratios and not for limits, and the
  evaluation says which number is which. Not verified: a clean limit sweep of the final build, the site build (its
  `node_modules` are not installed in this worktree), Box2D (native library not built: 98 tests skipped), a NativeAOT
  or browser build of a consumer. Deferred with reasons (evaluation §9): nearest-first, ray and segment queries, a
  region or chunk system, streaming one Tiled map in parts, a quadtree or a hierarchy, the index inside `IWorld`.
  Next: a clean run of `final.sh` (evaluation §10) when the machine is idle; the owner's review of the branch.

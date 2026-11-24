# PHYS-2: dynamics, joints, queries, tile levels on Box2D and character rules on the physics contract

Objective: make the physics contract reach what its backends can do, judged by what a developer outside the
repository must write. Issue NastMz/MicroEngine#178, task T10 of [CONSOLIDATION-1](consolidation-1.md). Slices 1 to 4
(characters, scene-owned worlds, dynamics, joints) merged with pull request #198; this record holds the remaining
slices, on branch `feat/phys-3`; nothing is pushed from this record's work. Test runner:
`dotnet test MicroEngine.slnx -c Release`. Evaluation: [`docs/evaluations/phys-2.md`](../../docs/evaluations/phys-2.md).
Decision: [ADR 0068](../../docs/decisions/0068-physics-contract-version-2.md).

## Specs

Quoted fragments are the issue's text or the task's brief, verbatim (L1, L2).

- S1 Queries: "shape cast, point, circle and all-hits queries" ("queries limited to closest ray and box overlap").
  Acceptance: every hit of a ray, nearest first; a cast box, circle and capsule; overlaps with those shapes and at a
  point; the nearest surface; on both backends, behind a capability, with one conformance suite both run.
- S2 Contact detail: "contact detail"; "richer contact data (contact points, normal impulse, relative speed at impact
  — what a game needs for impact sounds and damage)". Acceptance: the impacts of a step with point, speed and
  impulse, and the points a body touches at, on Box2D; refused with the typed error on the tile backend.
- S3 Tile levels on Box2D: "tile geometry on Box2D"; "a tile map as static geometry in a Box2D world without one body
  per tile (merged edges/chains, no ghost collisions at tile seams — test a box sliding along a flat row of tiles),
  one-way and slope tiles".
- S4 The native world: "an explicit opt-in handle to the native world"; "a documented access to the native world for
  what the contract does not cover ... state what is unsupported".
- S5 Character rules: "Design the hooks a platformer needs (gravity per phase, a turn/skid phase, corner correction,
  a filter callback for conditional solids) from the developer's side, add them to `CharacterController2D` with tests
  on both backends. Do NOT migrate Meadow Run itself ...; instead prove the hooks are enough with a test or a sample
  scene that reproduces those rules."
- S6 "Capabilities missing on the tile backend are declared, not silent." "Tiles stay the default, Box2D optional."

## Tasks

| ID | Task | Specs | Route | Status |
| --- | --- | --- | --- | --- |
| T1 | Shape queries and contact detail on the contract, both backends, native layer 4 | S1, S2, S6 | inline | [x] |
| T2 | Tile levels on Box2D (chains, one-way runs, slopes) and the native world | S3, S4, S6 | inline | [x] |
| T3 | Character rules: gravity phases, skid, corner correction, conditional solids | S5 | inline | [x] |
| T4 | Correction batch after the independent verification (defects a to k) | S1, S3, S4, S6 | inline | [x] |

## Log

- L1 (issue #178, verbatim): "## Problem The contract is thin against Box2D 3.2, which is already the optional
  backend: no joints (the `Joints` flag exists, no API uses it, the native layer has no joint call), no forces,
  impulses, angular velocity, damping or gravity scale, queries limited to closest ray and box overlap, and no way to
  reach the native world. Ropes, chains, vehicles and physics puzzles cannot be built on the published API. No
  evaluation weighed this. Separately, the engine has no character controller: gravity, grounded checks, coyote time,
  jump buffering and platform riding are rewritten in five places, and every demo uses the static `TileMotion` helper
  instead of the contract. ## Target Body dynamics API; joints (revolute, distance, prismatic, weld, wheel, motor,
  mouse) under the existing capability flag; shape cast, point, circle and all-hits queries; contact detail; tile
  geometry on Box2D; an explicit opt-in handle to the native world; a character controller over `Move` that works on
  both backends; physics worlds owned per scene through MODULE-1. Tiles stay the default, Box2D optional. ##
  Acceptance - [ ] A rope, a wheeled vehicle and a platformer character run from the public API, as tests and a
  sample. - [ ] Capabilities missing on the tile backend are declared, not silent. - [ ] The demos move characters
  through the engine controller. - [ ] ADR 0031 amended; manual corrected (it still says there are no forces or
  rotating bodies). Depends on MODULE-1."
- L2 (2026-11-20, the brief of the remaining slices, the parts quoted in the specs): slice 5 queries and contacts,
  slice 6 tile levels on Box2D and the way out, controller hooks so Meadow Run can move with the engine's controller,
  without migrating Meadow Run (`games/**` belongs to the demo modernization).
- L3 (2026-11-22) T1 done. Decisions: one capability `ShapeQueries` for the five queries, declared by both backends,
  and `ContactDetail` for impacts and contact points, declared by Box2D. A query shape (`QueryShape`) is its own
  type, not a `ShapeDescription`: the tile backend answers circle and capsule queries exactly (a query shape is a
  rounded box, and moving it against a rectangle is a ray against the rectangle grown and rounded), although its
  bodies have boxes only, because an explosion's radius is needed by games that never need a circle body. A cast
  reuses `RayHit`. Impacts are Box2D's hit events with the manifold's total normal impulse added, because the event
  alone gives a speed and damage needs the masses. `ContactEvent` gained `Point`. Native layer 4. RED: the suite on
  the tile backend failed 10 of 10 with the typed error before the implementation. Checks:
  `tests/MicroEngine.Tests --filter Physics` 91 passed; `tests/MicroEngine.Physics.Box2D.Tests` 223 passed (the two
  suites in metres and in pixels); the native self-test on win-x64 all passed, determinism hash unchanged
  (`93f3a79ebf294b23`); `Samples.Tests --filter PhysicsPlayground` 3 passed; the playground's scripted captures looked
  at (sparks at impacts, the beam's hits, a crate thrown by the blast). Found on the way: Box2D's shape cast stops a
  slop short of a surface for a polygon and a slop into it for a round shape, so the contract promises the place
  within `Skin`.
- L4 (2026-11-22) T3 done, before T2 (it shares no code with it). What Meadow Run needs was read from
  `games/MeadowRun/Gameplay/Player.cs` and `PlayerTuning.cs` and turned into rules, not copied: `ApexGravityScale` and
  `ApexSpeed` (its third and fourth gravities), `SkidDeceleration` and `SkidSpeed`, `CornerCorrection` (written over
  `Move` and `SetPosition` only: the same move is tried from four distances to each side, so it works on both
  backends and costs nothing unless a rising character hits a ceiling), `CeilingReboundSpeed` (its head bump leaves at
  20), `RunJumpBonus`, `AirTime` and `LandingSpeed` (its land impact). Conditional solids: a predicate on the filter,
  `QueryFilter.Where`, under a new capability `FilterPredicates`, and `CharacterController2D.Solid` over it. A
  callback cannot cross into the native layer (the browser's interpreter mishandles function pointers), so the Box2D
  backend asks the predicate about the shapes near the move before it runs and hands the layer a list of handles to
  leave out (`me_world_set_ignored`, native layer 5); the tile backend asks while it gathers obstacles. Rejected:
  generalizing `OneWay` to a direction per shape (four backends' worth of rules for one use), and filtering contacts
  after the move (a rejected contact needs the move again without that shape, which needs the same exclusion). Not
  made a rule: Meadow Run's air momentum that is never slowed toward a lower cap and its separate air drag; the
  manual says how a game writes a sideways model of its own (acceleration 0 and `Velocity`). Its stage has no physics
  world: `Physics.World.AddTiles` is that setup and needs nothing new. The proof is
  `The_rules_of_a_classic_platformer_fit_together` and the six rule tests in
  `tests/Shared/Physics/CharacterRulesConformance.cs`, run on tiles and on Box2D in metres and pixels, and the
  `ClassicScene` of `samples/PhysicsPlayground` with its test and scripted captures (skid dust, the block found:
  looked at). The character's state hash gained three values (skid, air time, landing speed). Checks:
  `tests/MicroEngine.Tests --filter Physics` 100 passed; `tests/MicroEngine.Physics.Box2D.Tests` 241 passed;
  `Samples.Tests --filter PhysicsPlayground` 4 passed; native self-test all passed. The rule tests were written before
  the rules but could not fail first as tests: they did not compile until the members existed.
- L5 (2026-11-22) T2 done. Tiles: chains, not merged boxes (the reasoning is in the evaluation, section 7). The
  outline is traced in managed code (`TileOutline`), so the native layer only takes loops and boxes
  (`me_tiles_create`) and maps every segment to one handle; `exact_bounds` became `id_bounds` over Box2D's shape type
  because a handle no longer has one Box2D shape. Slopes are two new `TileFlags` and a capability `TileSlopes`. The
  native world: the ids (`me_native_id`) and Box2D's API exported from the shared library by linking it whole; the
  self-test calls `b2GetVersion` so a platform that does not export it fails at link. Native layer 6. RED: the tile
  conformance suite did not exist for Box2D (its file was excluded from the project); the first run of the new tests
  had 5 failures, all mistakes of the tests' own scenes (two crates on one spot, a level with no wall, a ray that hit
  the crate, a wrong function name), none of the code. The seam test discriminates: over one box per tile the same
  box dips to 63.93 px/s and ends 0.2 px short, and the test's bounds (63.99, 0.05 px) fail there. Checks:
  `tests/MicroEngine.Physics.Box2D.Tests` 261 passed; native self-test on win-x64 all passed, determinism hash
  unchanged; `Samples.Tests --filter PhysicsPlayground` 5 passed; the tile scene's captures looked at (bodies at rest
  on the platform, the plateau and the floor; a crate thrown by the explosion). Also built here: `build.sh android-x64` (NDK 27.3 clang and lld: the
  shared library links whole and the self-test links against Box2D's exports, the way Linux does; not run, it needs a
  device) and `build.sh browser-wasm` (the static archives; the self-test ran under Node, all passed, same
  determinism hash). Not compiled here: Linux with gcc, macOS and iOS (CI builds them); on macOS the whole-archive
  link is `-force_load` and was not seen to work.
- L6 (2026-11-23) Close. `dotnet build MicroEngine.slnx -c Release`: 0 warnings. `dotnet test MicroEngine.slnx -c Release`: 17
  projects, 0 failed, no host aborted (Box2D 261, MicroEngine.Tests 1350 and 1 skipped, Samples 113, Desktop 833 and 17
  skipped). `docs/api` regenerated: 11 pages changed, 5 added; the Box2D package has no pages there
  (`site/scripts/api-reference.mjs` does not list it). Not run: the site build, a browser build of a game with Box2D,
  phones. Left to the owner: the row of T10 in `consolidation-1.md`, and CI for the natives of Linux, macOS and iOS.
- L7 (2026-11-23, the coordinator's correction batch, the part that sets the rule, verbatim): "an independent
  verification found no blocker ... It found eleven minor defects, mostly places where the two backends disagree or
  the manual says something the code does not do. Fix them in ONE batch, test-first where it is code ... Backends must
  agree (decide ONE rule for each, state it in the contract's XML comments and the manual, and make both backends keep
  it)".
- L8 (2026-11-23) T4 done. The rules are in the contract's remarks ("Edges", "Tile geometry is one shape with one
  surface") and the evaluation, section 7b. Tests that failed first: (a) and (b)
  `A_ray_that_starts_on_an_edge_hits_going_in_and_nothing_going_out_or_along_it` on Box2D in metres and pixels and
  `A_ray_on_the_surface_of_tiles_hits_going_in_and_nothing_going_out` on Box2D; (c)
  `A_cast_shape_that_touches_a_surface_hits_it_at_once_going_in_as_a_move_from_there_is_blocked` on Box2D in metres
  and pixels; (d) `A_shape_sunk_into_tiles_is_not_stopped_by_the_faces_between_them` on the tile backend; (e)
  `A_one_way_tile_is_a_platform_a_quarter_of_a_tile_thick_for_overlaps_points_and_the_nearest_surface` on the tile
  backend; (f) `The_count_of_a_ray_through_one_way_and_solid_tiles_does_not_depend_on_the_room_given` on Box2D; (g)
  the capsule test, with a new assertion that it meets no wall; (h)
  `The_bounds_of_tiles_follow_their_body_when_it_is_turned`. (j) has
  `An_engine_body_destroyed_through_Box2D_is_refused_with_an_error_instead_of_ending_the_process`, written after the
  guard (it could only end the process before). (i) measured with a throwaway test, not kept: 1000 x 1000 random
  tiles, removal 5705 ms (the verifier's number) to 149 to 161 ms; adding 1.5 to 1.7 s and about 520 MB after a
  collection, unchanged (3 MB of it managed); 250 x 250: 0.1 s, 27 MB, 8 ms. One regression on the way: mapping a tile
  shape's two handles to one changed which of two equally near surfaces `ClosestPoint` names; the lowest handle wins
  again. Native layer 7: no signature changed, but its rules did (the header raises the version for either) and
  `me_native_valid` was added. Not fixed: a box character does not climb slope tiles (the manual now says slopes need
  a capsule; a step-up rule in the controller is a feature of its own); the memory of a huge noise map (Box2D's, per
  chain segment; stated). No runtime refusal of a box character on a level with slopes: a box on such a level is
  legitimate for everything but walking the slopes. Checks are in L9.
- L9 (2026-11-23) Checks of T4. `dotnet build MicroEngine.slnx -c Release`: 0 warnings. `dotnet test MicroEngine.slnx -c Release`:
  17 projects, 0 failed, no host aborted (Box2D 271, MicroEngine.Tests 1356 and 1 skipped, Samples 113, Desktop 832 and
  18 skipped: one more skipped than the run before, outside physics). Native self-test: win-x64 and browser-wasm (under
  Node) all passed, determinism hash `93f3a79ebf294b23` on both. API pages regenerated (3 changed), snippets
  refreshed, the anchors of `physics.md` named by other pages exist. Not rebuilt: android-x64 with the last layer;
  not compiled: Linux with gcc, macOS, iOS.

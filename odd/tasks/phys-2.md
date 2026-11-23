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
| T2 | Tile levels on Box2D (chains, one-way runs, slopes) and the native world | S3, S4, S6 | inline | [ ] |
| T3 | Character rules: gravity phases, skid, corner correction, conditional solids | S5 | inline | [ ] |

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

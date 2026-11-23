# PHYS-2: what the physics contract reaches of its backends

Issue NastMz/MicroEngine#178; task T10 of [`odd/tasks/consolidation-1.md`](../../odd/tasks/consolidation-1.md) (S4, S7);
decision in [ADR 0068](../decisions/0068-physics-contract-version-2.md). The owner's request: examine the coverage of
the public contracts against what the backends can really do, with attention to joints, constraints and simulations
that could not be reached, keeping tiles by default and Box2D optional.

Method: each capability is judged by whether a developer outside the repository could build the game that needs it
with the published API. Box2D's own headers at the pinned commit (`box2d.env`, v3.2.0) are the source for what the
backend can do. No benchmark campaign was run: nothing here adds work to an existing path (see section 8).

## 1. What the audit confirmed in the code

| Finding | Where | Status after this work |
| --- | --- | --- |
| No joint API; `PhysicsCapabilities.Joints` declared by nobody | `IPhysicsWorld2D`, `me_box2d.h` | joints on the contract, Box2D implements them |
| Only `SetVelocity` to control a dynamic body | `IPhysicsWorld2D` | forces, impulses, torque, angular velocity, `BodyDynamics`, sleep, mass data |
| `MassOf`, `BoundsOf` only on the Box2D class | `Box2DPhysicsWorld2D` | contract members |
| A physics world is made with `new`, registered by hand, not hashed or disposed with its scene | `PhysicsModule.cs` | `Scene.Physics` (a scene system) |
| `DebugDraw.Physics` casts to `TilePhysicsWorld2D` | `DebugDraw.cs` | reads `TilesOf` from the contract |
| Character movement rewritten in five places over `TileMotion` | two demos, two samples | `CharacterController2D`, `TopDownController2D`; both samples moved to it |
| Queries: closest-hit ray and rectangle overlap only; no contact detail | `IPhysicsWorld2D` | shape queries on both backends, contact detail on Box2D (section 6) |
| Box2D lacks `TileGeometry` and `MovingSolids`; no way to reach the native world | `Box2DPhysicsWorld2D` | tile levels as chains with slopes, and the native ids with Box2D's API exported (section 7); `MovingSolids` not done |

## 2. Character controller

- **Need.** Every side-view game and most top-down ones. Today: about 60 to 290 lines per game, each with its own
  grounded check, coyote timer and platform rule.
- **Prior art.** Godot's `CharacterBody2D` (velocity + `move_and_slide`, `is_on_floor/wall/ceiling`, floor snap,
  platform velocity; the jump rules are left to the game). Unity ships no 2D controller; its 3D `CharacterController`
  is a capsule mover with `isGrounded`. Box2D 3.2 has a mover (`b2World_CollideMover`, `b2SolvePlanes`), already used
  by the backend's `Move` for capsules. None of them owns coyote time or jump buffering, which is exactly what games
  keep rewriting.
- **Decision.** One class per character in the contract's assembly, over `Move`/`MoveResult` only, so both backends
  and every game get it. It takes intent (`CharacterInput`), not input. Parameters are one record
  (`CharacterMotion`). A top-down character is a second small type rather than a mode: it shares no rule with the
  side-view one (no gravity, no ground).
- **Rules that needed a decision.**
  - *Rest.* A grounded character with no sideways speed is probed with a zero-length `Move`, not moved by gravity: it
    cannot creep down a slope and costs one probe.
  - *Carrying.* The controller remembers the body under it and its position; when that body moved and the character
    did not, it follows with a `Move`. A backend whose solids carry (`MovingSolids`) has already moved the character,
    which the controller sees and adds nothing. One rule, both backends.
  - *Drop.* Whether the ground is one-way only is asked with a zero-length `Move` that passes one-way platforms; no
    capability or shape inspection is needed.
  - *Slopes.* `GroundSnap` pulls a walking character down onto ground that fell away. Tested on Box2D with a capsule;
    without it the character leaves the slope going down (the test shows both).
  - *Wall jump.* The intent is ignored for `WallJumpLockTime`, or instant acceleration would cancel the push.
- **Validation.** One suite, 19 tests, run on tiles and on Box2D in metres and in pixels (57 runs), plus a tile test
  for pushing solids and two Box2D slope tests. `samples/PhysicsSubstitution` and `PlatformerExample` moved to it:
  the first gives the same positions at every reported point on both backends (one coin's sensor fires one step
  later, because a jump now applies gravity in its first step); the second passes its unchanged tests, and its tour
  capture is byte-identical.
- **Meadow Run was not migrated.** Its tests, captures and state hashes cannot stay unchanged:
  1. `Player` moves with `TileMotion` (half-tile sub-steps, three calls per step: platform delta, X, then Y) against a
     bare grid; the stage has no physics world, and platforms are `IRideable` rectangles with their own landing rule
     (a 2.5-pixel window above the top, a 1-pixel side inset, snap to the top).
  2. Its horizontal model has a skid on reversal above 30 px/s, a run modifier with its own acceleration, friction
     above the cap and air momentum that is never slowed toward a lower cap.
  3. It has four gravities (rise, release, apex hang below a speed threshold, fall) and a run-speed jump bonus.
  4. Ceiling: corner correction (a sideways nudge of a few pixels), hidden blocks hit from below, and a head bump
     that sets the fall speed to 20 instead of 0.
  5. Its state hash holds fields the controller does not have (skid, land impact, air time, bumped tile, form).
  The controller should not copy 2 to 4 as fixed rules: they are that game's feel. To host such a game it would need
  hooks (a gravity curve, a sideways model, a pre-move step for corner correction, a ceiling response) and the stage
  would need a physics world with its platforms as bodies. That is a redesign of the demo, not a migration.

## 3. Physics that lives with its scene

- **Need.** Any game with a physics world. Today: `new`, `PhysicsSystem.Add`, remove on exit, dispose, hash by hand.
- **Decision.** `ScenePhysics`, a scene system in the core (the physics assembly cannot reference `Scene`, as ADR 0060
  anticipated). `Scene.Physics` adds it on first read. It steps in `LateUpdate`, after the scene's `Update`, by the
  step the scene received, so the world's clock, a freeze and a covering overlay apply without code; it writes
  transforms, hashes the world and what the game hands to `Track`, and disposes the world. `Game.PhysicsBackend` is the
  one place a backend is named; the default is the tile backend.
- **`PhysicsModule`/`PhysicsSystem` stay** for worlds that belong to no scene. No engine code, demo or sample used
  them; nothing was migrated.
- **Debug drawing.** `AddTiles` and `TilesOf` became contract members under `TileGeometry`, so `DebugDraw` asks the
  contract. `ICollisionGrid` and `TileFlags` moved to the contract's assembly with their namespace.

## 4. Dynamics

Members with defaults that refuse; Box2D implements them through nine new `me_box2d` calls. Units are the world's:
a pixel-space game writes forces in mass × pixels per second squared. `BodyDynamics` is a struct whose `default` is
the default body (gravity scale 1, sleep allowed), so `BodyDescription.Dynamics` needs no nullable. The Box2D state
hash adds a body's dynamics only once they were set, so worlds that never set them hash as before.

## 5. Joints

- **Need.** Ropes, chains, ragdolls, vehicles, doors, pistons, dragging: none could be built before.
- **Source.** Box2D 3.2: revolute, distance, prismatic, weld, wheel, motor (plus filter, and the mover and pogo joints
  of its character sample). It has no mouse joint any more; its motor joint with a linear spring of bounded force is
  the replacement and is what `JointKind.Target` uses, from a static body the layer keeps.
- **Shape.** One `JointDescription` with a factory per kind and `with` for limits, motors and springs, instead of six
  description types: the options are the same few (limit, motor, spring) across kinds, and one type keeps the
  contract to one `CreateJoint`. Anchors are world points at creation; the layer turns them into Box2D's local frames
  so that a revolute angle and a prismatic translation are 0 where the joint is made.
- **Breaking.** Box2D raises joint events past a force threshold but does not break joints. `BreakForce` is checked by
  the backend after each step against the constraint force; broken joints are destroyed and listed in `BrokenJoints`.
- **Not exposed.** Motor joint as a velocity drive, filter joint, per-joint constraint tuning, joint events.
- **Validation.** 11 tests in metres and pixels; the native self-test gained a pendulum, a rope and a joint that ends
  with its body, so every platform's build runs them.

## 6. Queries and contacts

- **Need (A).** Hit scans that go through several targets, an explosion's radius, "is there room to stand up", what a
  dash would hit, picking with the pointer, the nearest wall, impact sounds, fall and crush damage. Before (B): a
  closest ray and a rectangle; a game wrote the rest over `ShapesOf` and `DescriptionOf`, shape by shape, and could
  not get an impact's strength at all.
- **Alternatives (C).** Box2D 3.2 has all of it (`b2World_CastRay` with a callback, `b2World_CastShape`,
  `b2World_OverlapShape`, `b2Shape_GetClosestPoint`, hit events, `b2Body_GetContactData`): the Box2D side is a thin
  export. For the tile backend the choice was between refusing round query shapes (it has no circle shapes) and
  answering them exactly. A query shape is a rounded box, and moving one against a rectangle is a ray against the
  rectangle grown and rounded: one exact routine of about sixty lines covers boxes, circles and standing capsules.
- **Target experience (D).** `world.Overlap(QueryShape.Circle(70), centre, default, near)` for a blast and
  `foreach (ref readonly var hit in world.ContactImpacts)` for sparks, written first in `samples/PhysicsPlayground`.
- **Limit (E).** The engine answers the geometric question; what a hit means (damage, sound) is the game's. Query
  shapes are a box, a circle and a standing capsule: rotated and polygon query shapes are not offered.
- **Decision (H): Extend.** One capability, `ShapeQueries`, declared by both backends, for `RayCastAll`, `ShapeCast`,
  `Overlap(QueryShape)`, `OverlapPoint` and `ClosestPoint`; `QueryShape` is its own type, so the tile backend takes a
  circle in a query without declaring circle shapes. `ContactDetail`, declared by Box2D, for `ContactImpacts` (Box2D's
  hit events, with the manifold's total normal impulse added, because the event has a speed and damage needs the
  masses) and `ContactsOf` (the manifold points of a body). `ContactEvent` gained `Point`.
- **Trade-offs (G).** A cast on Box2D stops a slop short of a surface for a polygon and a slop into it for a round
  shape, so the contract promises the place within `Skin`, as for `Move`. A surface less than a millimetre away where
  a cast starts counts as overlapped on Box2D. The tile backend has no dynamic bodies and so no contact detail: it
  refuses with the typed error.
- **Predicates.** `QueryFilter.Where` (`FilterPredicates`, both backends) leaves single shapes out: a ray that skips
  the body that fires it, and the mechanism behind a character's conditional solids (section 2b). A managed callback
  cannot be called from inside the native layer (the browser's interpreter mishandles function pointers), so the
  Box2D backend asks the predicate about the shapes near the query before it runs and hands the layer the handles to
  leave out.
- **Validation (I).** `ShapeQueryConformance` (9 tests) on tiles and on Box2D in metres and in pixels,
  `ContactDetailConformance` (3 tests) and `FilterPredicateConformance` (2 tests), the tile grid's queries in
  `TileGeometryConformance`, the native self-test, the playground's captures.

## 2b. Character rules for a game's own feel

- **Need.** Section 2 left Meadow Run on its own movement code because the controller's rules were fixed. Read again
  from the developer's side: a platform game with a feel of its own needs a gravity per phase of the jump, a skid, a
  nudge past ceiling corners, a ceiling response, and solids that depend on how they are met (its hidden blocks).
- **Alternatives.** Godot's `CharacterBody2D` leaves all of it to the game's script over `move_and_slide`; Unity ships
  nothing. A controller with virtual methods to override (gravity, sideways model) was weighed against parameters:
  every one of the rules above is a few numbers that games tune, the order of the step is where the bugs are, and a
  subclass would have to know that order. So the rules are parameters, and the escape is to write `Velocity` and
  replace `Motion`, which needs no subclass.
- **Decision: Extend.** `ApexGravityScale`, `ApexSpeed`, `SkidDeceleration`, `SkidSpeed`, `CornerCorrection`,
  `CeilingReboundSpeed`, `RunJumpBonus` on `CharacterMotion`; `IsSkidding`, `AirTime`, `LandingSpeed`, the events
  `Skidded` and `CornerCorrected`; `Solid`, a rule per character over `QueryFilter.Where`. Corner correction is written
  over `Move` and `SetPosition` only, so it is the same on both backends.
- **What stays the game's.** Meadow Run's air momentum that is never slowed toward a lower cap and its separate air
  drag: a sideways model of its own, written over `Velocity` (the manual says how). A stage without a physics world:
  `Physics.World.AddTiles` is that world. Hidden blocks that are cells of the level's grid: a conditional solid is a
  shape of its own, because a predicate sees shapes, not cells.
- **Meadow Run is still not migrated here**: `games/` belongs to the demo modernization. The proof that the rules are
  enough is `The_rules_of_a_classic_platformer_fit_together` and the six rule tests, on tiles and on Box2D, and
  `ClassicScene` in `samples/PhysicsPlayground`, which uses Meadow Run's numbers.

## 7. Tile levels on Box2D and the native world

- **Need.** A level drawn with tiles in a world of dynamic bodies: crates, balls and vehicles over tile floors.
  Before: `AddTiles` was refused on Box2D and a game merged boxes by hand; a body sliding over two boxes that meet
  catches on the seam (measured here: over one box per tile a frictionless box sent at 64 px/s dips to 63.93 at seams
  and ends 0.2 px short after six seconds; over the outline it never leaves 64).
- **Alternatives.** (1) One box per tile: seams everywhere, a shape per tile. (2) Boxes merged by rows and stacked:
  every walkable surface in one piece, wall faces still split. (3) Box2D's chain shapes: its own answer to ghost
  collisions, smooth and one-sided, at the price of a hollow inside. Chains were chosen: the mover, the casts and the
  rays of the layer already go through `b2World_CastShape` and `b2World_CastRay`, which take chain segments, so the
  work was the outline, not the layer.
- **Decision: Extend.** `Box2DPhysicsWorld2D.AddTiles` traces the outline of the solid cells in managed code
  (`TileOutline`): one loop per region of open space, collinear edges merged, wound so the segments face the open
  space; where two solid cells touch only at a corner the loops follow the open space, so the corner is the inner
  corner it is for a body there. Runs of one-way cells are boxes (the top quarter of a tile, so a body coming up
  through one is not pushed onto it from half way). `TileFlags.SlopeRight` and `SlopeLeft` are new flags, part of the
  outline under a new capability `TileSlopes`; the tile backend refuses a level that holds one when it is added with
  an area, instead of playing it without its slopes. One managed shape stands for the tile geometry; the native layer
  maps every segment to one handle, and the backend counts contacts per pair so that a body touching a floor and a
  wall of the tiles begins and ends one contact.
- **Hollow.** Overlaps, point queries and the nearest surface are answered for the inside from the copy of the flags
  the backend keeps. A dynamic body put inside solid tiles is not pushed out: stated in the manual.
- **Not done.** Slopes on the tile backend (a different sweep; its character is a box, which climbs a slope as a
  staircase anyway), ceiling slopes and half-height slopes (no flags for them; polygons on Box2D cover them),
  `MovingSolids` on Box2D.
- **The native world.** What a developer needs is to call a Box2D function with the id of an engine object. That
  takes two things: the ids, and Box2D's functions in a library the game can bind to. The package's shared library
  linked Box2D statically and exported only the layer, so the second was the real gap: it now links Box2D whole and
  exports its API (`box2d_EXPORTS`, `$<LINK_LIBRARY:WHOLE_ARCHIVE,box2d>`, CMake 3.24); the self-test calls
  `b2GetVersion` itself, so a platform whose library does not export it fails to link. `Box2DWorldOptions.NativeAccess`
  is the opt-in and `NativeIds` gives `B2WorldId`, `B2BodyId`, `B2ShapeId`, `B2JointId`. Alternatives rejected: a
  generic call gateway in the layer (as much API as Box2D itself), and growing the contract for every Box2D feature
  (the contract is what backends share). Cost in portability, stated in the manual: not in the browser (ids are
  structs passed by value, which the interpreter does not do), not on iOS (untested), outside the state hash, tied to
  Box2D's version. The library grew from 436 KB to 710 KB on win-x64.

## 8. Performance

No existing path does more work: the tile backend gained members that only refuse, a world without joints skips the
break check's loop body, and a character at rest costs one zero-length move instead of a gravity move. The physics
benchmarks were not rerun.

The later slices add one null check of the filter's predicate per shape a move or a query looks at on the tile
backend, hit events on Box2D's dynamic shapes (the self-test's determinism hash is unchanged) and three values in a
character's hash. Still not rerun.

## 9. Verification

See the task records ([consolidation-1](../../odd/tasks/consolidation-1.md) for slices 1 to 4,
[phys-2](../../odd/tasks/phys-2.md) for the rest) for commands and results. Not run: natives of other platforms (CI
builds them from the changed layer; the export of Box2D's API is checked there by the self-test's link), Box2D in the
browser, phones.

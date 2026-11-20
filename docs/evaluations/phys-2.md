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
| Queries: closest-hit ray and rectangle overlap only; no contact detail | `IPhysicsWorld2D` | **not done** (section 6) |
| Box2D lacks `TileGeometry` and `MovingSolids`; no way to reach the native world | `Box2DPhysicsWorld2D` | **not done** (section 7); `AddTiles` is a contract member the tile backend implements |

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

## 6. Queries and contacts (not done)

Still missing: shape casts (box, circle, capsule), a point query, a circle overlap, an all-hits ray into a caller's
buffer, and contact points and impulses. Design notes for when it is done: Box2D already casts shapes inside the
layer's mover (`b2World_CastShape`), so `me_cast_shape` is a thin export; the tile backend can cast a box with its
sweep and should refuse circles and capsules; contact detail needs `b2Contact_GetData` at the begin event (points)
and hit events for impulses (`b2ContactHitEvent` gives an approach speed, not an impulse).

## 7. Tile levels on Box2D and the native world (not done)

- `AddTiles` on Box2D: the contract member exists with its rule (row 0 is the top row whichever way y grows; a backend
  that builds shapes reads the area and one tile around it). The implementation needs one managed shape that stands
  for many native shapes. Boxes merged by rows, then stacked, keep every walkable surface in one piece; wall faces can
  be split, where a dynamic body may catch. Chains remove that but need outline tracing and chain support in the
  layer's mover, which reads polygons.
- An opt-in handle to the native world was not added.
- Slopes on the tile backend: not done; they need a new capability and a different sweep. `MovingSolids` on Box2D: not
  done; the controller's carrying covers characters on both backends, which is the case games hit first.

## 8. Performance

No existing path does more work: the tile backend gained members that only refuse, a world without joints skips the
break check's loop body, and a character at rest costs one zero-length move instead of a gravity move. The physics
benchmarks were not rerun.

## 9. Verification

See the task record for commands and results. Not run: natives of other platforms (CI builds them from the changed
layer), Box2D in the browser, phones.

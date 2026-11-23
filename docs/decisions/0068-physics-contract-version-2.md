# 0068 — The physics contract, version 2: characters, scene-owned worlds, dynamics and joints

Status: Accepted (2026-11-19). The SDK is in Alpha: public API changes carry no compatibility commitment. Amends
[0031](0031-physics-contract.md) ("the contract has no joint API"; stepping) and
[0051](0051-box2d-optional-package.md) (what the package declares). Evidence: [PHYS-2](../evaluations/phys-2.md).
Issue NastMz/MicroEngine#178.

## Context

The contract of ADR 0031 was thin against Box2D 3.2, the optional backend behind it: no joints, one way to move a
dynamic body (`SetVelocity`), mass and bounds only on the Box2D class. A physics world was created with `new` and
registered, stepped, hashed and disposed by game code. Character movement was rewritten in every game over a static
helper. A rope, a vehicle or a ragdoll could not be built with the published API.

## Decision

- **Tiles stay the default backend and Box2D optional.** Nothing in the engine references the Box2D package.
- **Contract version 2.** Every member added has a default implementation that refuses with
  `PhysicsCapabilityException`, naming the capability; a backend written for version 1 compiles unchanged and
  implements what it declares. The error names the backend of the engine that declares what is missing.
- **Characters** are `CharacterController2D` and `TopDownController2D` in the contract's assembly, written over `Move`
  only, taking intent, hashed through `IStateHashed`.
- **A scene owns its physics world** through `ScenePhysics` (a scene system in the core): stepped after `Update` by
  the scene's step, synced to transforms, hashed, disposed. `Game.PhysicsBackend` names the backend once.
  `PhysicsModule` stays for worlds outside scenes.
- **Dynamics** (`ApplyForce`, `ApplyImpulse`, `ApplyTorque`, `ApplyAngularImpulse`, angular velocity, `SetRotation`,
  `BodyDynamics`, sleep, mass data) under `DynamicBodies`, `Rotation` and `MassProperties`.
- **Joints** under `Joints`: one `JointDescription` with a factory per kind (revolute, distance, rope, prismatic,
  weld, wheel, target), typed `JointId`, world anchors at creation, setters, reaction force, a break force the backend
  checks after each step. Box2D declares it; the target joint is Box2D's motor joint with a bounded spring.
- **Tile geometry** (`AddTiles`, `TilesOf`) and `BoundsOf` are contract members; `ICollisionGrid` and `TileFlags` live
  in the contract's assembly, same namespace.
- **The native layer** is version 6 (2: dynamics, 3: joints, 4: queries and contact detail, 5: shapes left out of a
  query, 6: tile geometry and the native ids). The managed assembly refuses another version.

Amended 2026-11-22 (the remaining slices, still contract version 2, which no release has carried):

- **Shape queries** under `ShapeQueries`, on both backends: `RayCastAll`, `ShapeCast`, `Overlap(QueryShape)`,
  `OverlapPoint`, `ClosestPoint`. A `QueryShape` (box, circle, standing capsule) is not a `ShapeDescription`: the tile
  backend answers round queries exactly without having round shapes. A cast returns a `RayHit`, within `Skin`.
- **Contact detail** under `ContactDetail`, on Box2D: `ContactImpacts` (point, normal, approach speed, impulse) and
  `ContactsOf` (the points a body touches at); `ContactEvent.Point`.
- **Filter predicates** under `FilterPredicates`, on both backends: `QueryFilter.Where`. No managed callback is called
  from inside a backend's native code: the Box2D backend asks before the query and passes a list.
- **Character rules** are parameters of `CharacterMotion` (apex gravity, skid, corner correction, ceiling rebound,
  run-up bonus) and `CharacterController2D.Solid`; a game with rules beyond them writes `Velocity` and replaces
  `Motion` rather than subclassing.
- **Tile levels on Box2D** are the outline of the solid tiles as chain shapes, one managed shape for the level;
  `TileFlags.SlopeRight` and `SlopeLeft` under `TileSlopes`. The tile backend refuses slope tiles.
- **Box2D's own API** is the way out: `Box2DWorldOptions.NativeAccess`, `Box2DPhysicsWorld2D.NativeIds`, and a shared
  library that exports Box2D whole. Not in the browser or on iOS; outside the state hash.

## Alternatives

- **A description type per joint kind.** Closer to Box2D, six types and six create calls for options that repeat.
  Rejected for one description with factories.
- **Capability interfaces (`IJointWorld2D`) instead of defaulted members.** Type-safe discovery, but game code would
  cast, and the capability flags already answer "can it"; one interface with one error keeps the substitution seam.
- **A character controller in each backend.** Box2D's mover could drive one natively, but two implementations of jump
  buffering are two feels. One controller over `Move` gives the same rules everywhere.
- **Physics stepped by a frame system bound to a scene** (the previous `PhysicsSystem`). It needs registration and
  cannot be found, hashed or disposed from the scene.
- **Making Box2D the only backend.** Rejected by PHYS-1 and unchanged: exact resting, live tile grids and pushing
  solids are the tile backend's.

## Consequences

- Breaking: `TilePhysicsWorld2D.GridOf` is `TilesOf`; `ICollisionGrid` and `TileFlags` changed assembly (recompile);
  a scene class that wrote `Physics.X` for the namespace now finds the property. Recorded in `docs/migrations.md`.
- State hashes: a scene with `ScenePhysics` hashes its world; scenes without it are unchanged. A Box2D world hashes
  joints once it had one and a body's dynamics once they were set, so earlier worlds hash as before.
- The natives of every runtime are rebuilt: the layer's sources are in the cache key of `box2d-natives.yml`, so CI
  rebuilds them without a workflow change. `samples/PhysicsPlayground` uses the package.
- Not done, recorded in PHYS-2: slopes on the tile backend, ceiling and half-height slope tiles, `MovingSolids` on
  Box2D, rotated and polygon query shapes. Meadow Run keeps its own movement code until the demos are modernized; the
  controller has the rules it needs.
- The Box2D library exports Box2D's API (it grew from 436 KB to 710 KB on win-x64) and its build needs CMake
  3.24. A character's state hash has three more values.

## Verification

The character suite on both backends, the dynamics and joint suites on Box2D in metres and pixels, the scene physics
tests, the native self-test (impulse, dynamics, pendulum, rope), the two samples and the playground's scripted
captures.

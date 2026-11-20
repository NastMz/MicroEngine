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
- **The native layer** is version 3 (2: dynamics, 3: joints). The managed assembly refuses another version.

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
- Not done, recorded in PHYS-2: shape casts, point and circle queries, all-hits rays, contact points and impulses;
  `AddTiles` on Box2D; an opt-in handle to the native world; slopes on the tile backend; `MovingSolids` on Box2D.
  Meadow Run keeps its own movement code.

## Verification

The character suite on both backends, the dynamics and joint suites on Box2D in metres and pixels, the scene physics
tests, the native self-test (impulse, dynamics, pendulum, rope), the two samples and the playground's scripted
captures.

# ADR 0031: A 2D physics contract with declared capabilities; the tile mover as its first backend

## Status

Accepted (2026-11-03). Evidence: ARCH-1 §3 (S7 no physics contract, D6 `TileMotion` documented as swept but
sub-stepped), §6.3 (the `MicroEngine.Physics` and `MicroEngine.Physics.Box2D` rows), §6.4 (the physics sketch), §7
(this ADR), §9 (stage 3), §9.1 (common contract and declared capabilities, the substitution demonstration), annex A3
(WO-1, WO-2 and the `sim-probe` experiments E1 and E2), and the P6 report
([`experiments/arch-1/p6-box2d`](../../experiments/arch-1/p6-box2d/README.md)) with architect directive 8 (the P6
verdict and the contract revisions a-h) and directive 12 (stage 3b).

## Context

The engine had no place for physics: no body, collider, query, event or step hook. Collision was `TileMotion`, a
kinematic box against a tile grid that the documentation called swept although it tested overlaps in sub-steps of
half a tile: a 2 × 2 box moving 15.9 px tunnelled through a 4 px extra solid (E1), and a platform raised into a resting
box left it embedded with `IsOnGround` false (E2). The demos wrote their own overlap loops and collision grids. P6
showed that Box2D v3 can back a common contract on every target through a flat native layer, and which parts of the
first sketch Box2D cannot honour as written: there is no box mover, no kinematic–static contact, the overlap query is
broad-phase, queries see sensors, and per-pair policies run inside the step.

## Decision

- **`MicroEngine.Physics`** (references the scene world and the kernel) holds the contract, `IPhysicsWorld2D`
  (`PhysicsContract.Version = 1`): bodies owned by `EntityId`s with generation-checked `BodyId`/`ShapeId`; static and
  kinematic bodies and axis-aligned boxes; 64-bit collision layers (category and mask, both sides must accept);
  `Move`, a collide-and-slide of a kinematic body's one box, whose `MoveResult` carries the contacts with their normals
  and the ground; `RayCast` (closest hit, a shape holding the start is not hit); `Overlap`, exact; sensors whose
  enter and exit events are buffered per `Step`; `ContactEvents` only for pairs with a dynamic body; no game code runs
  during `Step`. It also holds `PhysicsModule`, which steps registered worlds in `FramePhase.Physics` (a world bound to
  a `SceneWorld` steps only when that world stepped, by its scaled step) and writes kinematic and dynamic body poses to
  their owners' transforms, which the scene world interpolates.
- **Declared capabilities.** `PhysicsCapabilities` lists what is not common (`TileGeometry`, `OneWayPlatforms`,
  `DynamicBodies`, `Rotation`, `Joints`, `Capsules`, `Circles`, `Polygons`, `ContinuousCollision`,
  `FrictionRestitution`, `MassProperties`, `MovingSolids`); descriptions state which flags they need, and an
  undeclared request throws `PhysicsCapabilityException` before anything changes. Nothing is emulated silently.
- **P6's revisions** are contract text: `Move` may rest within a backend-declared gap (`Skin`; the tile backend
  declares 0); a kinematic body's contacts come from `MoveResult`; `Overlap` is exact; `QueryFilter.IncludeSensors`
  states whether ray casts and overlaps return sensors (false by default; sensors never block a move); sensor events
  need the visiting shape's `SensorEvents` flag (true by default), and the backend enables what it needs from it;
  one-way platforms are a shape flag implemented inside the backend; the box is the common character shape (capsules
  are a capability); determinism is promised for the same backend build on every target, not between backends or
  between versions of a backend's physics library.
- **`MicroEngine.Physics.Tiles`** (references the contract) is the first backend, `TilePhysicsWorld2D`: tile grids as
  one static shape each (solid cells block, one-way cells are platforms), boxes, and an exact swept mover (X then Y,
  each one sweep that stops flush against the first surface in its way however thin); it declares `TileGeometry`,
  `OneWayPlatforms` and `MovingSolids` (a kinematic body flagged `PushesBodies` is not blocked; it pushes kinematic
  bodies out of its way and carries those resting on it). The tile types (`TileLayer`, `TileGrid`, `TileFlags`,
  `ICollisionGrid`, `Autotile`) and `TileMotion` moved into it with their namespace; `MicroEngine.dll` forwards them,
  and `TileRenderer` stays in the core because it draws.
- **`TileMotion` keeps its API** and runs on the same swept test, keeping its sub-steps so that a move with nothing in
  its way gives bit-identical positions: thin extra solids no longer tunnel (E1), and an extra solid whose top edge is
  inside the lower half of a box moving down lifts the box onto it (E2). The demos' state traces did not change.
- **Conformance.** One common suite every 2D backend runs (an abstract test class per backend subclass), asserting
  properties within the declared skin and never passing vacuously, plus one suite per capability that runs only where
  it is declared; it also checks that every undeclared capability is refused with the typed error.

## Alternatives considered

- **Bind Box2D first and abstract later**: Box2D's API would leak into games, and its version churn with it.
- **One contract presuming the tile mover and Box2D are equivalent**: they are not (no dynamic bodies on tiles; no box
  mover, no kinematic–static contacts in Box2D); the common core plus capabilities states the difference.
- **An in-house rigid-body solver**: reinvention; Box2D v3 is the backend for dynamics (stage 6).
- **Capsules as the common character shape** (Box2D's native mover): the tile backend would treat them as boxes, and
  every tile game moves boxes today.
- **Keep the old `TileMotion` semantics** for overlapped solids: E2 stays broken for games that move platforms
  themselves.
- **Sensors included in queries by default** (Box2D's behaviour): line-of-sight rays and overlaps would stop at
  triggers; the filter flag makes the choice explicit either way.

## Consequences

- A game that uses both `MicroEngine.World` and `MicroEngine.Physics` sees two `MoveResult` types and qualifies one.
- Body owners must be root entities: body poses are world positions.
- `MovingSolids` is a capability the Box2D backend may implement in its layer or leave undeclared; the substitution
  demonstration (stage 6) uses only the common contract.
- The common suite is written in pixel-art units with a `Scale` override; the Box2D backend runs it scaled to metres.

# 0051 — Box2D is published as an optional SDK package; the tile backend stays the default

Status: Accepted (2026-11-11). Amends [0031](0031-physics-contract.md) (the optional package "is not published") and
[0019](0019-sdk-packages.md) (the published package set). Evidence: [PHYS-1](../evaluations/phys-1.md).

## Context

ADR 0031 gave 2D physics one contract, `IPhysicsWorld2D`, with two backends: the tile backend inside the
`MicroEngine` package, and Box2D v3 behind the engine's own C layer, packed only on request and never published. A
developer outside the repository can therefore move boxes through tile levels, but cannot use dynamic bodies,
rotation, round or polygon shapes, continuous collision, friction or mass: the only backend that has them cannot be
installed. PHYS-1 asked first whether Box2D should replace the tile backend instead. It measured that Box2D declares
neither tile geometry nor moving solids, rests up to its skin away from surfaces, has no `TileMotion`, and runs the
kinematic tile scenario every demo plays 3 to 4.6 times slower than the tile backend.

## Decision

- **`MicroEngine.Physics.Box2D` is an SDK package**, released with the other packages, at the same version, by the
  same release workflow and approval (ADR 0022). Its native libraries come from the pinned Box2D build that
  `box2d.yml` produces and checks (checksums, self-test, cross-runtime determinism hash).
- **It is optional.** No other package, the `dotnet new` template or a demo depends on it. A game that needs dynamics
  adds the package and creates `Box2DPhysicsWorld2D` where it creates its world; the contract's capability errors
  already state what each backend refuses.
- **The tile backend and `TileMotion` stay in `MicroEngine`**, unchanged, as the default for tile games.
- **Declared platform levels** for the package: Windows x64, Linux x64 and the browser are tested on every change
  that reaches physics; macOS is built and tested when Apple runs are requested; Android and iOS are built, not run
  on a device. The documentation states these levels and the backend's limits (skin, at most 128 worlds per process,
  creation and disposal from one thread at a time).
- **Upgrading Box2D stays deliberate** (`native/box2d.env`): results change between Box2D versions, so an upgrade is
  a release-notes entry.

## Alternatives considered

- **Standardise on Box2D** (PHYS-1 option 2): the adapters it would need (tile grids rebuilt on cell changes, push and
  carry, `TileMotion`) are the tile backend's behaviour rewritten over shape casts; every game would ship a native
  library on every target; tile movement would be slower and rest off the floor. About 1,400 lines saved.
- **A staged transition** (option 3): there is no better destination for tile movement to move to.
- **Keep Box2D repository-only**: leaves dynamics out of reach of every game outside this repository.
- **Box2D.NET (managed port)** instead of native Box2D: ADR 0031's P6 measured native Box2D 3.4 to 4.4 times faster
  in the browser and 6 times faster on Windows; the port stays the documented fallback.

## Consequences

- The release publishes one more package, and its check builds and runs a game against it from the packed feed.
- The browser build of a game that references the package links Box2D's static archives into the WebAssembly runtime
  (the host already builds native WebAssembly for SDL).
- Every Box2D native runtime the package carries must be built before a release; a release cannot ship a package
  that lacks a runtime it declares.

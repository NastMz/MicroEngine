# PHYS-1: Box2D as an optional provider or the only 2D physics backend

Issue NastMz/MicroEngine#111. Record: [`odd/tasks/phys-1.md`](../../odd/tasks/phys-1.md). Date: 2026-11-11.
The question is how 2D physics reaches a developer outside this repository. The issue requires revisiting the premise
first: keep the in-house tile backend and publish Box2D next to it, replace the tile backend with Box2D, or move in
stages. The rule is that of every recent evaluation: could an external developer build a game that needs this physics
today with the published SDK?

## 1. What exists today

- **Contract.** `IPhysicsWorld2D` (`src/MicroEngine.Physics`, ADR 0031) is a common core plus 12 declared
  capabilities. A backend refuses every undeclared one with `PhysicsCapabilityException` before changing anything;
  nothing is emulated silently.
- **Tiles** (`MicroEngine.Physics.Tiles`, 1,394 lines of C#) ships inside the `MicroEngine` package. It declares
  `TileGeometry`, `OneWayPlatforms` and `MovingSolids`, and it holds `TileMotion`, the static helper every demo uses.
- **Box2D** (`MicroEngine.Physics.Box2D`, 780 lines of C# plus the `me_box2d` C layer over Box2D v3.2.0, MIT) packs
  only with `-p:MicroEngineBox2DPackage=true` and has never been published. It declares dynamic bodies, rotation,
  circles, capsules, polygons, continuous collision, friction and restitution, mass and one-way platforms. It does
  not declare `TileGeometry`, `MovingSolids` or `Joints`.
- **Who uses what.** The demos (Meadow Run, Neon Rift, Courtyard Key), `samples/Examples` (platformer) and the Gem
  Garden tutorial use `TileMotion` only. `samples/PhysicsSubstitution` is the only consumer of `IPhysicsWorld2D`, and
  it references Box2D as a project, so it runs only inside the repository. The `dotnet new` template gets Tiles
  through `MicroEngine.Desktop` and nothing else.

**Answer to the rule today:** an external developer can build a tile platformer, top-down or run-and-gun game. They
cannot build a game with dynamic bodies (crates, ragdolls, a physics puzzle, a pinball table, Angry-Birds-style
throwing): the only backend that has them cannot be installed.

## 2. Coverage: can Box2D do what Tiles does?

| Tiles feature | Box2D today | What replacing it would take |
| --- | --- | --- |
| Tile grids as one static shape, cells changed at runtime take effect at once | Not declared; levels must be built from boxes | An adapter that turns the grid into boxes or chains and rebuilds the touched region on every cell change |
| `MovingSolids`: a platform that pushes bodies aside and carries the ones on it | Not declared (Box2D makes no kinematic–static or kinematic–kinematic contacts) | An adapter that reimplements push and carry on top of shape casts: the same algorithm Tiles owns today |
| Swept collide-and-slide with `Skin` 0 (rests flush) | Emulated with axis-separated shape casts; rests up to 0.015 m × units per metre short | Accept the gap (0.24 px at 16 px/m) in every game, or not |
| One-way platforms, sensors, ray and overlap queries, layers | Yes | Nothing |
| `TileMotion` (box against a grid, no world) | No equivalent | Keep `TileMotion` over a grid, which is the tile code itself |
| Dynamic bodies, rotation, round and polygon shapes, CCD, friction, mass | Yes | Tiles refuses all of them, as the contract says |

The two backends are not substitutes: they overlap on the common core and each covers what the other refuses.
The solver features (dynamics) belong to Box2D. Tile geometry, push/carry and the exact mover are movement
behaviour that Box2D does not offer, and that any "Box2D only" engine would still have to write as adapters.

## 3. Measurements

All on the same Windows x64 machine, Release, Box2D natives built from the pinned commit with
`native/build.sh win-x64` (self-test passed, determinism hash `93f3a79ebf294b23`).

**Conformance.** Box2D tests: 101 of 101 passed (common suite in metres and pixels, one-way, every solver suite,
substitution scenarios). Tiles physics tests: 48 of 48 passed (common, one-way, moving solids, tile geometry,
`TileMotion`).

**Kinematic characters in a tile level.** `benchmarks/PhysicsBench` now takes `--backend tiles|box2d`. The scenario
is the same for both: N 12×14 boxes falling, walking and jumping through a 64×36 level with one-way platforms,
32 static boxes and 16 sensors, one `Move` per box and one `Step` per frame. Box2D gets the level as one box per
horizontal run of cells, at 16 px/m with y down (as `PhysicsSubstitution` sets it). Three runs after a warm-up:

| Bodies | Tiles world, mean (p95) | Box2D world, mean (p95) | `TileMotion`, mean | Allocation per step |
| --- | --- | --- | --- | --- |
| 200 | 0.19 ms (0.28) | 0.87 ms (1.09) | 0.09 ms | 35 B both |
| 800 | 1.58 ms (2.19) | 5.02 ms (6.35) | 0.15 ms | 69 B Tiles, 134 B Box2D |

In the scenario every shipped demo plays, Tiles is 3 to 4.6 times faster than Box2D, and `TileMotion` (no world) is
faster still. This is not "C# against C++": Box2D pays two shape casts and two native calls per move, plus a
broad phase built for rigid bodies. The Box2D level could be cheaper with chain shapes; that is an optimisation of an
adapter that does not exist yet.

**Dynamic bodies.** Tiles has none, so there is nothing to compare. The P6 experiment
(`experiments/arch-1/p6-box2d`) measured 500 dynamic boxes at 0.20 ms per step in the browser with native Box2D.

**Distribution.** `me_box2d.dll` (Box2D linked in) is 425 KiB on win-x64. Every runtime is built from source by
`native/build.sh`, and `box2d.yml` builds them (macOS and iOS on request), checks checksums and cross-runtime determinism and runs
the tests on Windows, Linux and macOS and in Chrome. The browser host already builds native WebAssembly for SDL
(`WasmBuildNative`), so Box2D's static archives add no new toolchain to a web build; that the host picks up the
package's `buildTransitive` targets from a `PackageReference` is not yet verified. iOS and Android arm64 are
build-only, and how the iOS host links the static archives is unverified (ADR 0031). Limits: 128 worlds per
process, created and disposed from one thread at a time.

## 4. The options

**1. Keep Tiles in the SDK and publish Box2D as an optional provider.**
Nothing changes for existing games, the template or the tutorial. A developer who needs dynamics adds one package
and creates `Box2DPhysicsWorld2D` instead of `TilePhysicsWorld2D`; the contract and capability errors already make
the choice explicit. Cost: a second package to release (its natives come from `box2d.yml`), its declared platform
levels, a consumer sample and its documentation. Box2D upgrades stay deliberate (`box2d.env`), as ADR 0031 requires.

**2. Standardise on Box2D, keeping only narrow tile and movement adapters.**
It would remove the tile sweep, ray and broad phase, but the adapters it needs (grid to shapes with runtime
rebuilds, push and carry, `TileMotion`) are the same behaviour rewritten on top of shape casts. Every game, the
template and the tutorial would ship a native library on every target, including platforms where Box2D is
build-only today. Characters would rest 0.24 px off the floor, and the demos' movement would run 3 to 4.6 times
slower. `TileMotion`, used by every demo and sample, would either stay (and the tile code with it) or break every
game. The saving is small (about 1,400 lines), and the cost falls on every game, including those that never need
dynamics.

**3. Staged transition towards Box2D.**
A transition needs a destination that is better. The evidence says Box2D is not better for tile movement, which is
what most 2D games here do; it is better for dynamics, which Tiles does not try to do. There is nothing to
transition to, only something to add.

## 5. Recommendation

**Option 1.** Keep `MicroEngine.Physics.Tiles` and `TileMotion` in the default SDK, and publish
`MicroEngine.Physics.Box2D` as an optional package. It answers the rule (dynamics become available outside the
repository) without moving any cost onto games that do not need them, and it is reversible: if Box2D later gains
tile geometry and moving solids with a measured cost close to Tiles, option 2 can be reconsidered with this
benchmark as its baseline.

**What remains uncertain:** the web build through a `PackageReference` (not run), the iOS link of static archives,
Android arm64 and iOS on a device, NativeAOT publish (the project is marked AOT-compatible; no publish was run), and
whether chain shapes close part of the kinematic gap.

## 6. Follow-up if the owner approves option 1

This changes the published package set, so it waits for the owner (issue: "require the owner's explicit
architectural approval").

1. **Audit the package:** pack it as `sdk-release.yml` would, with the natives from `box2d.yml`; check its
   dependencies, `runtimes/<rid>/native`, the `buildTransitive` targets, and NativeAOT publish on win-x64.
2. **Release path:** add the package to the SDK release (version from `sdk/version.json`, Trusted Publishing), so it
   ships with the same version as `MicroEngine`.
3. **External-consumer sample:** a small game against the published packages (bodies, collisions, sensors, crates
   that fall and stack, choosing the backend where the world is made), with its tests, run on Windows, Linux and the
   browser.
4. **Documentation:** a page in the physics manual on choosing a backend, the declared platform levels (Windows,
   Linux and browser tested; macOS experimental; Android and iOS build-only), the 128-world limit, and the release
   notes entry.

# ARCH-1 P6: Box2D v3 in .NET on every target

A throwaway prototype. Nothing here is built by `MicroEngine.slnx` or by CI (`experiments/` is classified as needing no
checks), and the engine is not changed. Build output and downloaded sources go to a scratch folder outside the
repository.

## Question

Which Box2D v3 option works on all six targets (Windows, Linux, macOS, Android, iOS, browser), so the stage 3 physics
contract (`IPhysicsWorld2D`, dossier §6.4, §9, §9.1) can be frozen?

- (a) **Box2D.NET** (ikpil), a pure C# port: which upstream version, API, maintenance, NuGet.
- (b) **native Box2D v3** (C17) through bindings: existing .NET bindings or a hand-written P/Invoke layer, including a
  static build for the Emscripten 3.1.56 that .NET 10's `browser-wasm` uses, and the interpreter pitfalls P4 found.
- (c) anything else.

Measured: 500 dynamic boxes + static geometry at 60 Hz (time per step); a kinematic body moved with collide-and-slide;
whether each option expresses the common-contract operations; determinism (two runs, managed vs native); the same in a
browser; Android and iOS where reachable.

## Verdict

**Use native Box2D v3 (the upstream release, v3.2.0 today), built from source by MicroEngine for every target, behind
a small flat C layer shaped after the common contract** (`native/me_box2d.h` is the prototype of it). Not Box2D.NET,
and not an existing binding.

Why:

1. **Browser, as MicroEngine ships it today (Mono interpreter): only native is viable.** 500 boxes cost **0.20 ms per
   step** natively and **about 23 ms per step** on Box2D.NET: the managed port alone exceeds a 60 Hz frame (16.7 ms).
2. **Raw Box2D bindings break on the wasm interpreter.** Box2D v3 passes its ids (`b2BodyId`, `b2ShapeId`: 8-byte
   structs) by value to almost every function; the interpreter has no trampoline for that signature and aborts
   (`aot-runtime-wasm.c:188` in `mono_wasm_get_interp_to_native_trampoline`, at the first `b2CreatePolygonShape`). Every
   existing binding P/Invokes that raw API, and none ships a wasm library. A layer that passes only `int`, `float`,
   `ulong` and pointers worked unchanged on all measured targets.
3. **Box2D.NET lags upstream and is effectively one maintainer.** Its main branch ports upstream up to PR #1062
   (2026-05-29); the published package 3.1.654 (2026-02-28) up to about PR #1022 (2025-12). Upstream v3.2.0
   (2026-10-19) is about 20 PRs ahead (large worlds, recording/snapshots, mover joint, tunable CCD, ghost-collision
   pre-solve, revised chains, SAT/cache optimisations, AVX2 dispatch). 675 of 691 commits are by its author.
4. **Native is faster everywhere measured**: 6× on Windows (v3.2.0 vs Box2D.NET main), 3.6× at the same upstream
   commit, 3.4–4.4× in the browser even against an AOT-compiled port, 8× on the Android emulator.
5. **Determinism holds across targets.** The same native build is bit-identical on Windows x64 (MSVC), the browser
   (Emscripten clang) and Android x86_64 (NDK clang); Box2D.NET is bit-identical to *native Box2D at the commit it
   ports* on every target too. Managed and native at different upstream versions diverge from the first step.

Box2D.NET is technically sound (it passed every contract check and matches upstream bit for bit at the same commit) and
stays the fallback if building native libraries for six targets turns out to be the larger cost; it would need an
AOT-compiled browser host (0.8–0.9 ms per step in the browser with `RunAOTCompilation`) and accepting its version lag.

**Changes the physics contract needs** (details in [Changes to the physics contract](#changes-to-the-physics-contract)):
the kinematic `Move` of the Box2D backend is native for capsules and emulated for boxes (with a skin), so the contract
must allow a backend-defined resting gap; `ContactEvents` cannot carry kinematic-vs-static contacts (Box2D creates
none), `MoveResult` must; `Overlap` must be specified as exact (Box2D's AABB query is broad-phase); queries see sensor
shapes unless the filter excludes them; per-pair policies (one-way) must be data on shapes, never managed callbacks
inside `Step`; `OneWayTiles` should become a one-way *platform* capability that Box2D can also declare.

## Environment

| Item | Version |
| --- | --- |
| OS / CPU | Windows 11 Pro 10.0.26300, x64, 12 logical cores; Git Bash |
| .NET (desktop, Android) | SDK 10.0.403, runtime 10.0.14 (CoreCLR desktop; Mono on Android) |
| .NET (browser) | `web/setup.sh`'s private SDK 10.0.403 + `wasm-tools` workload: runtime pack 10.0.14, **Emscripten 3.1.56** |
| Native compilers | MSVC 14.51 (Visual Studio 18, CMake 4.4.4 "Visual Studio 18 2026" generator), Android NDK 27.3.13750724 (clang), Emscripten 3.1.56 (clang); Ninja from Visual Studio |
| Box2D (native) | upstream **v3.2.0** (2026-10-19) and upstream commit **f2086ed6** "clean up UI code (#1062)" (2026-05-29) |
| Box2D.NET (managed) | main at **154bacd0** (2026-09-29), ports upstream up to #1062; NuGet **Box2D.NET 3.1.654** (2026-02-28, commit 5efc96de) |
| Browser | Chrome 156.0.8078.76 headless (puppeteer-core 25.13.0) |
| Android | emulator `sdk_gphone64_x86_64`, Android 16, x86_64 (already running on the machine) |

Every Box2D build here reports `b2GetVersion() = 3.2.0`, including Box2D.NET 3.1.654 and f2086ed6: the version number
does not identify the code; a commit does.

## What was built

| File | What it does |
| --- | --- |
| `fetch-sources.sh` | Downloads Box2D v3.2.0, Box2D f2086ed6 and Box2D.NET 154bacd0 as tarballs into a scratch folder |
| `native/me_box2d.h`, `.c` | The flat C layer: worlds, bodies, box/capsule shapes, step, transforms, ray cast, exact box overlap, broad-phase AABB overlap, box cast, the capsule mover (`b2World_CollideMover` + `b2SolvePlanes` + `b2World_CastMover`, the upstream "Mover" sample's loop), sensor and contact events, one-way platforms through pre-solve, counters. Only scalars and pointers cross it. Compiles against both Box2D API generations (`ME_ORIGIN_API`) |
| `native/CMakeLists.txt`, `build-native.sh` | Box2D as a CMake subproject (so its own `-ffp-contract=off` rule applies) + the layer, for `windows-x64` (DLL, MSVC), `android-x64`/`android-arm64` (`.so`, NDK), `wasm` (`me_box2d.a` + `box2d.a`, Emscripten 3.1.56 from the .NET workload); desktop and Android also export Box2D's own functions for the raw probe |
| `Harness/IBackend.cs` | The operations both backends implement |
| `Harness/NativeBackend.cs` | `IBackend` over `me_box2d` (`[DllImport]`, blittable signatures only) |
| `Harness/ManagedBackend.cs` | `IBackend` over Box2D.NET, the same algorithms and defaults as the C layer (compiles against Box2D.NET main or, with `B2NET_PKG`, the 3.1.654 package) |
| `Harness/Scenarios.cs` | The benchmark scene, the state hash (FNV-1a over the bits of every body's x, y, cos, sin) and the 14 contract checks |
| `Harness/RawProbe.cs` | Calls Box2D's own API the way existing bindings do (structs by value) |
| `Desktop/`, `run-desktop.sh` | Console harness: `bench`, `contract`, `state`, `raw` |
| `Web/`, `build-web.sh`, `measure.mjs` | The harness in a .NET 10 `browser-wasm` page (interpreter, or AOT with `-p:P6Aot=true`), driven by headless Chrome |
| `Android/`, `build-android.sh` | The harness as a .NET 10 Android app (both ABIs); results through logcat |

## Steps to reproduce

```bash
W=$(mktemp -d)                                     # scratch, outside the repository
P=experiments/arch-1/p6-box2d
B2NET="$W/b2net-154bacd0/src/Box2D.NET/Box2D.NET.csproj"
$P/fetch-sources.sh "$W"
$P/build-native.sh "$W/box2d-v3.2.0"   "$W" windows-x64 ON
$P/build-native.sh "$W/box2d-f2086ed6" "$W" windows-x64 OFF
$P/build-native.sh "$W/box2d-v3.2.0"   "$W" wasm ON           # needs web/setup.sh's toolchain
$P/build-native.sh "$W/box2d-v3.2.0"   "$W" android-x64 ON
$P/build-native.sh "$W/box2d-v3.2.0"   "$W" android-arm64 ON
# desktop: <native lib> <Box2D.NET project, or - for the NuGet package> <command>
$P/run-desktop.sh "$W" "$W/out/windows-x64-box2d-v3.2.0/me_box2d.dll" "$B2NET" contract native
$P/run-desktop.sh "$W" "$W/out/windows-x64-box2d-v3.2.0/me_box2d.dll" "$B2NET" bench managed 500 600 3
$P/run-desktop.sh "$W" "$W/out/windows-x64-box2d-f2086ed6/me_box2d.dll" "$B2NET" state native
$P/run-desktop.sh "$W" "$W/out/windows-x64-box2d-v3.2.0/me_box2d.dll" - bench managed     # Box2D.NET 3.1.654
$P/run-desktop.sh "$W" "$W/out/windows-x64-box2d-v3.2.0/me_box2d.dll" "$B2NET" raw
# browser (interpreter, then AOT)
$P/build-web.sh "$W/out/wasm-box2d-v3.2.0" "$B2NET" "$W/web"
$P/build-web.sh "$W/out/wasm-box2d-v3.2.0" "$B2NET" "$W/web-aot" -p:P6Aot=true
node web/serve.mjs "$W/web/site/wwwroot" 8361 &  node web/serve.mjs "$W/web-aot/site/wwwroot" 8362 &
npm install --prefix "$W/node" puppeteer-core@25.13.0
PUPPETEER_DIR="$W/node" node $P/measure.mjs "http://127.0.0.1:8361/?backends=native,managed&runs=3&raw=1"
# Android: emulator or device attached
$P/build-android.sh "$W/out" "$B2NET" "$W/android" emulator-5554
```

## Results

### The options

| Option | What it is | Upstream version | Desktop | Browser (interpreter) | Browser (AOT) | Android | iOS |
| --- | --- | --- | --- | --- | --- | --- | --- |
| (a) Box2D.NET main | C# port, MIT, `netstandard2.1`–`net10.0` | upstream #1062 (2026-05-29) | works | works, **≈23 ms/step** | works, 0.9 ms/step | works (emulator) | not run (pure IL; full AOT on iOS) |
| (a) Box2D.NET 3.1.654 (NuGet) | same, published 2026-02-28 | ≈ upstream #1022 (2025-12) | works (14/14), 0.84 ms/step | not run | not run | not run | not run |
| (b) existing bindings: HughPH.Box2D 2026.7.22 | P/Invoke + native, "Box2D 3.1" | 3.1 | (win/linux/osx x86/x64/arm64 natives) | **no wasm library; raw API fails** | no wasm library | no Android/iOS natives | none |
| (b) Box2dNet 3.1.8.11 (thomasvt) | thin P/Invoke wrapper | 3.1 | win-x64, linux-x64 only | none | none | none | none |
| (b) Box2D.NET.Release 3.1.0 (BeanCheeseBurrito) | raw generated bindings + natives | 3.1.0, last release 2025-05 | desktop RIDs | none | none | `.so` for arm64/x64 | static `.a` for device and simulator |
| (b) **flat layer `me_box2d` + Box2D built by us** | this prototype | **v3.2.0** (any commit) | works | **works, 0.20 ms/step** | works, 0.27 ms/step | works (emulator) | not built (needs macOS/Xcode) |
| (c) Jolt (JoltPhysicsSharp), BepuPhysics2 | 3D engines | — | — | — | — | — | — |

The binding rows were not run: their packages were inspected on nuget.org (versions, dates, runtime folders). They are
listed for what they prove: Box2D v3 builds for Linux, macOS, Android and iOS with stock toolchains, and nobody ships a
.NET wasm build. Jolt and Bepu are 3D-only (R2 §3.2–3.3) and are not 2D options; Box2D.NET is the only managed v3.

### Time per step, 500 dynamic boxes (60 Hz, 4 substeps, sleep off, single-threaded)

The scene: 500 boxes of 0.5 m, each slightly rotated, rained in 20 columns into a 12 m static container; they tumble and
pile up (≈1750 contacts at the end, all 500 awake). 600 steps (10 s simulated), one warm-up run, then three measured
runs; the table gives the mean of the three runs' means and the range of their medians / p95.

| Target | Native v3.2.0 (`me_box2d`) | Native f2086ed6 | Box2D.NET main | Box2D.NET 3.1.654 |
| --- | --- | --- | --- | --- |
| Windows x64, CoreCLR (desktop) | **0.092 ms** (median 0.082–0.083, p95 0.14) | 0.156 ms (median 0.137–0.144, p95 0.21–0.22) | 0.563 ms (median 0.52–0.53, p95 0.74–0.88) | 0.839 ms (median 0.81–0.82, p95 1.08–1.11) |
| Browser, Chrome, interpreter | **0.196 ms** (median 0.2, p95 0.3–0.4) | — | **23.2 ms** (median 22.5–25.1, p95 28–32) | — |
| Browser, Chrome, AOT | 0.270 ms (median 0.2, p95 0.4–0.5) | — | 0.915 ms (median 0.8, p95 1.2–1.7) | — |
| Android emulator x86_64, Mono | 0.794 ms (median 0.58–0.65, p95 1.4–1.7) | — | 6.42 ms (median 6.0–6.5, p95 8.2–10.1) | — |

- Browser timings come from `Stopwatch` over `performance.now()`, which Chrome coarsens to 0.1 ms; the totals agree
  (native 110–129 ms for 600 steps on the interpreter build). An earlier session of the same builds measured
  21.7–24.3 ms (interpreter, managed), 0.18 ms (AOT, native) and 0.80 ms (AOT, managed): run-to-run spread in the
  browser is about ±15 %.
- The browser and Android contract checks ran with the harness before one `NOTE` (queries and sensors) was added; the
  14 checks are the same.
- The emulator runs x86_64 code on the host CPU under virtualisation; its numbers compare backends, not phones. Android
  Release used the default profiled AOT, so most of Box2D.NET ran on Mono's JIT (the APK's `libaot-Box2D.NET.dll.so` is
  84 KB).
- The first managed run on desktop is twice as slow (tiered JIT); its first steps reach 30–40 ms.
- Desktop numbers are the last of two sessions, taken with nothing else running; the first session agreed within 10 %.
- v3.2.0 vs f2086ed6 natively: upstream's optimisations after May 2026 (cache, SAT, AVX2 dispatch by CPU id) are worth
  about 1.7× on this machine.

### Determinism

State hash after steps 1, 10, 60, 120, 300 and 600 of the benchmark scene (FNV-1a 64 over every body's x, y, cos, sin):

| Build | Windows x64 | Browser, interpreter | Browser, AOT | Android x86_64 emulator |
| --- | --- | --- | --- | --- |
| Native v3.2.0 | `9befa564108053e9` | `9befa564108053e9` | `9befa564108053e9` | `9befa564108053e9` |
| Native f2086ed6 | `d5c8d09ecbc52cb8` | — | — | — |
| Box2D.NET main (ports f2086ed6) | `d5c8d09ecbc52cb8` | `d5c8d09ecbc52cb8` | `d5c8d09ecbc52cb8` | `d5c8d09ecbc52cb8` |
| Box2D.NET 3.1.654 | `f0dadb3943e4d092` | — | — | — |

- Every checkpoint matches too, not only the final hash. Two runs in one process and two separate processes give the
  same hashes for every build.
- **Managed vs native: identical at the same upstream commit** (Box2D.NET main = native f2086ed6 at all six
  checkpoints), different from step 1 across versions (v3.2.0 vs #1062, and 3.1.654 vs either).
- **Native across compilers and SIMD paths:** MSVC x64 (Box2D v3.2.0 dispatches to AVX2 on this CPU), Emscripten clang
  with `-msimd128 -msse2` (SSE2 intrinsics mapped to wasm SIMD) and NDK clang x86_64 produce the same bits. Box2D's
  CMake disables FMA contraction for GNU/Clang (`-ffp-contract=off`); MSVC does not contract by default. The C layer
  is compiled with the same flag.
- The contract scenario's outputs (positions, fractions, normals) printed identically on every target and backend.

### Common-contract checks

`Scenarios.Contract`: 14 checks over a small level (ground, wall, a 0.1 m solid, a sensor, an "enemy"-layer box, a
one-way platform) with a kinematic capsule player. Tolerances: the mover's 0.01 m iteration tolerance plus Box2D's
linear slop (0.005 m), named in the code.

| Check | What it asserts | Native | Managed |
| --- | --- | --- | --- |
| `move.lands` | 30 moves down: rests at y = 0.795 (expected 0.8), a ground plane (normal y > 0.7) | pass | pass |
| `move.blocked-by-wall` | walking right stops at x = 4.705 (4.7), wall plane normal (−1, 0) | pass | pass |
| `sensor.enter-exit` | walking through the sensor: 1 begin, 1 end, begin in the same step the move entered | pass | pass |
| `layers.mask-skips-enemy` | the player's mask has no Enemy bit: walks through the enemy box | pass | pass |
| `sensor.second-pass` | walking back: 2 begins, 2 ends | pass | pass |
| `layers.ray-mask` | the same ray hits the enemy (mask Enemy) or the wall (mask Static) | pass | pass |
| `ray.closest` | ray down: ground, point y = 0, normal (0, 1), fraction 0.5 | pass | pass |
| `overlap.exact` | a box 1 cm right of the wall finds nothing; over the wall finds it | pass | pass |
| `move.no-tunnel-capsule` | one 5 m move down onto the 0.1 m solid stops on it (WO-2's E1 case) | pass | pass |
| `cast.no-tunnel-box` | a 5 m box sweep hits the 0.1 m solid, normal (0, 1) | pass | pass |
| `emulated.box-slide` | box collide-and-slide on the box sweep reaches the wall along the floor | pass (with skin) | pass (with skin) |
| `events.contact-normal` | a dynamic crate lands: begin-touch event with the ground, normal (0, ±1) | pass | pass |
| `oneway.dynamic-presolve` | a box thrown up through a one-way platform lands on it; one dropped on it stays | pass | pass |
| `oneway.mover` | the player rises through the one-way platform and lands on it | pass | pass |

Results: 14/14 on every run — Windows (native v3.2.0, native f2086ed6, Box2D.NET main, Box2D.NET 3.1.654), browser
interpreter and AOT (native, Box2D.NET main), Android emulator (native, Box2D.NET main). Observations printed as
`NOTE`, the same everywhere:

- `b2World_OverlapAABB` with the box 1 cm right of the wall returns the wall (it tests fat AABBs).
- Without a skin the box slide never moves: once the box touches the floor, Box2D's shape cast reports an initial
  overlap (fraction 0, zero normal) for every further cast. With a 0.01 m back-off along the hit normal it slides and
  rests 1.5 cm above the floor (0.815 instead of 0.8).
- Contact begin events involving the kinematic player (contact events enabled on it): **0**.
- A ray through the sensor with mask All hits the **sensor** (y = 2.0), not the ground; an overlap inside it returns it.

### Raw binding probe (Box2D's own API, structs by value)

| Target | Result |
| --- | --- |
| Windows x64, CoreCLR | pass (falls 1 s to y = 4.979169) |
| Android x86_64 emulator, Mono | pass (y = 4.979169) |
| Browser, AOT | pass (y = 4.979169) |
| Browser, **interpreter** | **abort** at `b2CreatePolygonShape(b2BodyId, …)`: `[MONO] aot-runtime-wasm.c:188` from `mono_wasm_get_interp_to_native_trampoline`, then `exit(1)`. Calls before it worked: 12-byte struct return (`b2GetVersion`), large struct returns (`b2DefaultWorldDef`, `b2MakeBox`), a 4-byte struct parameter (`b2WorldId`), an 8-byte struct *return* (`b2CreateBody`) |

So the failing shape is an 8-byte, multi-field struct *parameter*: exactly `b2BodyId`/`b2ShapeId`/`b2JointId`, which
nearly every Box2D v3 function takes. In the WebAssembly C ABI such a struct is passed by pointer, so declaring it as
`ulong` in C# would not be a workaround (analysis, not tested). The flat layer avoids the issue by design.

### Size

| Item | Size |
| --- | --- |
| Native Box2D v3.2.0 + layer, Windows DLL | 543 KB (250 KB gzip -9); f2086ed6: 329 KB |
| Native, Android arm64 `.so` stripped / x86_64 stripped | 512 KB / 554 KB (224 KB gzip -9 for arm64). `AndroidNativeLibrary` packaged them unstripped (3.6 MB, debug info): strip in a real build |
| Native in the browser: Box2D + layer functions inside `dotnet.native.wasm` | 211 KB of wasm code (374 functions, from the name section of a symbol build) |
| Browser `dotnet.native.wasm` with Box2D, interpreter build | 1.72 MB raw, 537 KB Brotli (P4's SDL probe: 715 KB Brotli for SDL alone) |
| Browser `dotnet.native.wasm`, AOT build (Box2D.NET and BCL compiled in) | 6.7 MB raw, 1.57 MB Brotli |
| Box2D.NET assembly | 333 KB (main), 317 KB (3.1.654); in the browser 220 KB `.wasm` (Webcil), 75 KB Brotli |

## Mapping the common contract onto Box2D v3

| `IPhysicsWorld2D` (dossier §6.4, §9.1) | Box2D v3 | Native or emulated |
| --- | --- | --- |
| `CreateBody` static / kinematic / dynamic | `b2CreateBody` with `b2_staticBody` / `b2_kinematicBody` / `b2_dynamicBody` | native |
| `AddShape` AABB box (offset) | `b2MakeOffsetBox` + `b2CreatePolygonShape` | native |
| Layers and masks | `b2Filter` (64-bit `categoryBits`/`maskBits`, plus `groupIndex`); both shapes must accept each other | native |
| Sensor flag | `b2ShapeDef.isSensor` + `enableSensorEvents` on the sensor **and** on every shape that should be seen | native (opt-in on both sides) |
| `Move(kinematic, delta, filter)` with contacts and normals, capsule | `b2World_CollideMover` → planes → `b2SolvePlanes` → `b2World_CastMover`, iterated (≤ 5); planes are the contacts | native (the loop is ~20 lines in the layer) |
| `Move` with an AABB box | no box mover in Box2D: `b2World_CastShape` with a box proxy + slide loop + skin | **emulated** |
| Moving the kinematic body | `b2Body_SetTransform` (teleport) or `b2Body_SetTargetTransform` (velocity that reaches the target, pushes dynamics) | native |
| `RayCast` closest with point, normal, fraction | `b2World_CastRayClosest` | native |
| `Overlap(aabb)` exact | `b2World_OverlapShape` with a box proxy (`b2MakeProxy`) | native |
| (broad-phase overlap) | `b2World_OverlapAABB` — fat AABBs, may return non-touching shapes | native, but not the contract's meaning |
| `ContactEvents` with normals | `b2World_GetContactEvents` (begin/end/hit; opt-in `enableContactEvents`) + `b2Contact_GetData(contactId).manifold.normal` | native for pairs with a dynamic body; **none for kinematic–static** |
| `SensorEvents` enter/exit | `b2World_GetSensorEvents` (begin/end) | native |
| Ground/contact normals per step for a kinematic character | the mover's planes (`MoveResult`) | native via the mover |
| Step in the fixed phase | `b2World_Step(dt, subSteps)` | native |
| `PhysicsCapabilities`: dynamic bodies, rotation, joints, friction/restitution, CCD (`isBullet`, tunable CCD), circles/capsules/polygons, mass | all native | native |
| One-way platforms, dynamic bodies | pre-solve (`b2World_SetPreSolveCallback`, `enablePreSolveEvents`): set `manifold->pointCount = 0` (v3.2.0) / return false (≤ #1062) unless the normal points up from the platform | native hook, policy in the C layer |
| One-way platforms, kinematic mover | filtered in the mover: one-way shapes left out of the query mask while moving up, kept only as floor planes (normal y > 0.7) otherwise | **emulated** (no pre-solve for movers) |
| `TileGeometry` (grid-aligned static geometry from tile layers) | static boxes (or chain shapes) generated from the grid | emulated by construction; not declared |

### Sensor and event semantics (observed)

- Events are **buffered per step** and read after `b2World_Step`; they are valid until the next step. A begin event
  arrived in the same step as the move that entered the sensor (the teleported kinematic body was seen).
- Sensor events are **off by default on both sides**: the visitor shape needs `enableSensorEvents` too. Sensors do not
  detect other sensors (Box2D documentation; not tested here).
- **Queries see sensors.** A ray or overlap with an all-ones mask returns the sensor shape. The mover's filter in the
  prototype excludes the Sensor layer; the contract needs a rule (see below).
- **No contacts between kinematic and static bodies** (Box2D's broad phase only pairs proxies with a dynamic body). A
  kinematic character's contacts with the level exist only as the mover's planes.
- Contact begin events carry a contact id; the normal comes from the manifold (points from shape A to B).
- v3.2.0 runs the pre-solve and the new pre-continuous callback possibly on worker threads when a task system is set;
  the prototype stepped single-threaded.

## Changes to the physics contract

Proposed for the stage 3 contract; none needs a different backend, all come from the measurements above.

1. **`Move` and shapes.** The Box2D backend has a native mover for capsules only. Either the common character shape
   becomes a capsule on both backends (the tile backend can treat it as its bounding box), or `Move` with an AABB
   stays common and the Box2D backend emulates it with a shape cast and a skin. In both cases the contract states that
   the resting gap to a surface is backend-defined within a declared tolerance (Box2D: 0.005 m slop, 0.01 m mover
   tolerance, the skin of the box emulation; tiles: exact) and the common suite tests within it.
2. **Contacts of kinematic bodies come from `MoveResult`.** `ContactEvents` covers pairs with at least one dynamic body;
   a kinematic body's contacts and ground normals are returned by `Move`. The tile backend should follow the same rule
   so the scenario code does not depend on the backend.
3. **`Overlap` is exact.** State it; the Box2D backend implements it with `b2World_OverlapShape`, not
   `b2World_OverlapAABB`.
4. **Sensors and queries.** Decide whether queries return sensor shapes (Box2D does) or `QueryFilter` excludes them by
   default; and state that sensor events need opt-in on the visiting shape (or the backend always enables it).
5. **No managed callbacks inside `Step`.** Per-pair policies (one-way platforms, custom filtering) are data on shapes,
   implemented inside the backend (in C for the native backend). A managed callback from native code during the step
   would cross the interpreter boundary on the web on every contact and run on worker threads once Box2D is
   multithreaded. (Native→managed callbacks on wasm were not tested.)
6. **One-way platforms are a capability of their own** (`OneWayPlatforms` rather than `OneWayTiles`): Box2D implements
   them through pre-solve for dynamic bodies and in its mover for kinematic ones, so it can declare it; tile-specific
   geometry stays `TileGeometry`.
7. **Determinism claim.** The contract can promise bit-identical results for the same backend build on every target
   measured, given FMA contraction is off in the native build and the step runs single-threaded; it cannot promise
   identical results between backends or between Box2D versions (they diverge from the first step).
8. **The binding is internal and flat.** Box2D's own API changed between f2086ed6 and v3.2.0 (`b2Pos` origin
   parameters on every query, `b2Body_GetPosition` returning `b2Pos`, the pre-solve signature, `b2PlaneSolverResult`
   `translation` → `delta`, a new first field in `b2Counters`). The layer absorbed all of it with a few `#ifdef`s; a
   full generated binding would not. `MicroEngine.Physics.Box2D` should own a C layer of this kind and its native
   builds, and expose only the engine contract.

## Risks

- **Six native builds to own.** MSVC, Linux gcc/clang, Xcode for macOS and iOS (an xcframework, loaded like SDL3's
  framework through `NativeLibrary.SetDllImportResolver` in the iOS host), the NDK for two ABIs, and Emscripten pinned
  to the version the .NET workload uses (3.1.56 today): the wasm archives must be rebuilt when .NET moves Emscripten.
  This is a CI matrix and a packaging decision (P6 does not choose where the binaries live).
- **Wasm SIMD.** Box2D's Emscripten build uses `-msimd128`; the browser package then needs wasm SIMD (all current
  engines; Safari since 16.4). `BOX2D_DISABLE_SIMD` exists if an older engine matters; whether the scalar path gives the
  same bits was not tested.
- **Interpreter signatures.** Any future function in the layer must keep to scalars and pointers; a test should call
  every export on the wasm interpreter.
- **Upstream churn.** Box2D v3 changes its API between minor versions; pin a release and update deliberately.
- **Threading.** Box2D's task system (multithreaded step) was not used; enabling it makes pre-solve run concurrently
  (the prototype's counters are not thread-safe) and needs a determinism re-check (upstream claims multithreaded
  determinism).
- **Box2D.NET as fallback** would bring its version lag and a single maintainer; in the browser it needs AOT, which
  adds about 1 MB Brotli to the package (6.7 MB raw `dotnet.native.wasm`).

## What was not verified

- **Linux and macOS**: no host was available; Box2D's CMake supports gcc/clang (with `-ffp-contract=off`) and the
  HughPH/BeanCheeseBurrito packages ship Linux and macOS binaries; Box2D.NET is pure IL. Determinism there is expected
  from the same flags but was not measured.
- **iOS**: not built (needs macOS and Xcode). Box2D.NET would run under Mono full AOT, where struct-by-value P/Invoke
  is not an issue; the native layer would be an xcframework loaded like SDL3 today. BeanCheeseBurrito's package proves
  Box2D v3 compiles for iOS device and simulator.
- **Android on a device and arm64**: the arm64 library was built and packaged, but only the x86_64 emulator ran (one
  session; the emulator, shared with other work on the machine, was shut down before a second run); emulator timings
  are not phone timings.
- Firefox and Safari; Chrome only (headless).
- Native→managed callbacks (`UnmanagedCallersOnly`) on wasm; large-world (double precision) mode; Box2D's task system;
  the Box2D.NET package in the browser or on Android.
- Pushing dynamic bodies with the kinematic mover (`b2Body_SetTargetTransform` is in the layer but no check asserts it).

## Sources

- Box2D: repository and v3.2.0 release, https://github.com/erincatto/box2d (tag `v3.2.0`; commit list via the GitHub API
  for PRs #1022–#1129); determinism, https://box2d.org/posts/2024/08/determinism/; v3.1 release notes,
  https://box2d.org/documentation/md_release__notes__v310.html
- Box2D headers read: `include/box2d/box2d.h`, `types.h`, `collision.h`, `math_types.h`, `base.h`; `CMakeLists.txt`
  (FMA, SIMD, Emscripten flags); `src/broad_phase.c` (pairs only against the dynamic tree); `samples/sample_character.cpp`
  (the mover loop)
- Box2D.NET: https://github.com/ikpil/Box2D.NET (commits, `[upstream]` port commits naming upstream PRs, contributors,
  `CHANGELOG.md`, README); NuGet https://www.nuget.org/packages/Box2D.NET (3.1.654, commit 5efc96de)
- Bindings inspected on NuGet: https://www.nuget.org/packages/HughPH.Box2D, https://www.nuget.org/packages/Box2dNet,
  https://www.nuget.org/packages/Box2D.NET.Release (+ `Box2D.NET.Bindings.Release`, `Box2D.NET.Native.Release`)
- WebAssembly C ABI (aggregates passed indirectly): https://github.com/WebAssembly/tool-conventions/blob/main/BasicCABI.md
- MicroEngine: dossier `docs/evaluations/arch-1.md` §6.4, §9, §9.1; annex R2 §3; annex A3 WO-1, WO-2;
  `experiments/arch-1/p4-sdl3-web/README.md` (Emscripten 3.1.56 static linking, interpreter P/Invoke pitfalls);
  `src/MicroEngine.Cli/Hosts/ios/Program.cs` (framework loading for SDL3)

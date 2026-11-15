# PERF-3: extreme engine stress benchmarks and .NET GC profiling

Issue NastMz/MicroEngine#118. Record: [`odd/tasks/perf-3.md`](../../odd/tasks/perf-3.md). Date: 2026-11-14/15.
Builds on [PERF-1](perf-1.md) and [PERF-2](perf-2.md), which measured the demos and frame pacing; this evaluation
loads the engine far beyond what the demos do, to find where it stops and why.

Evidence words are used strictly. **Measured** means run on the machine of section 1 in the final campaign and read
from its output. **Hypothesis** means a cause proposed for a measured effect and not isolated by an experiment.
**Not run** means exactly that. No number here is a frame-rate promise: every limit is "this workload, this machine,
this build, this budget" (section 5).

## 1. Conditions

| | |
| --- | --- |
| CPU / RAM | AMD Ryzen 5 7600X (6 cores, 12 threads), 31.2 GB |
| GPU / display | NVIDIA GeForce RTX 4070 Ti SUPER, 1920x1080 at 100 Hz (primary, "LG FHD") |
| System | Windows 11 Pro 10.0.26300, x64 |
| Runtime | .NET 10.0.15 (SDK 10.0.404); JIT with tiered compilation and dynamic PGO unless stated; workstation concurrent GC unless stated |
| Graphics | SDL 3.5.0, `direct3d11` render driver, video driver `windows` |
| Engine | 0.6.0-alpha.1 at commit `52c0134` (`feat/perf-3`); Release builds, published once and run from the published folder |
| Canvas | 320x180 at scale 3, 60 updates per second |

**The machine was otherwise idle, and how that was checked.** `campaign.sh idle` takes ten one-second samples of the
total CPU load and adds the CPU time used meanwhile by every `dotnet`, `testhost`, `MSBuild`, `VBCSCompiler` and
`StressBench` process. Before the final campaign (2026-11-15 01:49 UTC, commit `52c0134`): load `15 53 15 12 20 17 6
14 25 20` %, four such processes alive, **0 CPU seconds** used by them. The remaining load is the desktop's own: a
game left open and idle (`SkyrimSE`, about 0.1 core), peripheral software, the Task Manager and this session's
clients, about 5% of the machine in a five-second sample. They were not closed (not this session's to close). The
game holds the same GPU; the render and end-to-end figures were taken with it open.

Three earlier campaigns of this session are **discarded**: one ran while two other sessions built and tested on the
machine, and two ran beside orphaned copies of the campaign itself (stopping a campaign did not stop its script; the
idle check then showed 12 to 16 CPU seconds used by build and test processes, which is how it was found). The
numbers below are all from the one campaign whose idle check is quoted above.

## 2. The harness

`benchmarks/StressBench` is one program. It does not replace the existing benchmarks, which time single operations
(`WorldBench`: the scene world's hot paths; `PhysicsBench`: a physics backend, whose level StressBench now shares
through `benchmarks/Shared/PhysicsLevel.cs`; `MapBench`, `AssetBench`: maps and the asset pipeline; `PerfBench`: a
synthetic game for the launcher's `--profile`). StressBench runs **whole frames at extreme loads**: every scenario is
a scene in a real `Game`, run by `GameRunner` with the engine's own `FrameProfiler`, so the engine's per-step work
(world steps, the frame schedule, input, the renderer) is inside every measurement.

```
dotnet run -c Release --project benchmarks/StressBench -- list
dotnet run -c Release --project benchmarks/StressBench -- run --scenario sprites,particles --mode render --load 50000
dotnet run -c Release --project benchmarks/StressBench -- suite --mode headless,render --repeat 3 --out results.json
dotnet run -c Release --project benchmarks/StressBench -- sweep --scenario physics --budget-ms 16.667 --metric p99
dotnet run -c Release --project benchmarks/StressBench -- compare docs/evaluations/perf-3/baseline.json results.json
```

| Scenario | The load counts | Default |
| --- | --- | ---: |
| `entity-churn` | live entities with two attachments; a tenth destroyed and created again every step | 10,000 |
| `world-iterate` | entities walked through `All<T>`, moved through `Local`, drawn through `DrawPosition2D` | 20,000 |
| `spatial-query` | static boxes in the tile physics world at constant density; a quarter ask `Overlap` (64x64) and a quarter `RayCast` (128 px) every step | 8,000 |
| `physics` | kinematic boxes in PhysicsBench's 64x36 tile level, one `Move` each and one `Step` | 2,000 |
| `sprites` | moving sprites of one texture (one batch) | 20,000 |
| `batch-breaks` | the same over 16 textures: a texture switch every draw, a blend switch every other | 20,000 |
| `shapes` | filled circles, plus half as many lines and half as many triangles | 5,000 |
| `text` | lines of 24 characters in the built-in bitmap font | 500 |
| `particles` | live particles of one additive system | 20,000 |
| `asset-churn` | 128x128 PNG textures decoded, uploaded, drawn once and unloaded every step | 4 |
| `integrated` | entities moved and drawn over four textures, a quarter as many physics bodies, twice as many particles, 2% churn, a HUD | 5,000 |

**Three modes keep CPU, rendering and end to end apart.** `headless`: the headless platform, no graphics device, one
fixed step per frame, unpaced: update plus draw submission to a counting renderer. `render`: a hidden SDL window, the
real renderer and device, unpaced: the same plus batching, the SDL calls and present. `e2e`: a visible window in real
time with VSync: frame intervals as the display paces them.

**What a run records.** CPU work per frame (events + update + draw submission) and its update, draw and present
parts, each as mean, p50, p95, p99, p99.9 and max; the frame interval and hitches (e2e); bytes allocated per frame
by the loop's thread (mean and worst frame); collections of each generation and the time the runtime paused the
process (`GC.GetTotalPauseDuration`, total and worst frame); time per subsystem inside the update (the scenario
times its own parts); draw calls of a frame and the texture and blend switches asked for; resource counts; managed
heap, heap committed, working set, private bytes and the difference (native). A checksum of the final simulation
state makes two runs comparable only when they did the same work.

**Reproducibility.** Seeded; deterministic time in `headless` and `render`; the same scenario, load, seed and frame
count end in the same checksum (tested). 600 measured frames after 120 of warm-up by default. `suite` runs every
scenario in a process of its own (`--isolate`) so memory, collections and compiled code are not what the previous
scenario left. `--repeat N` keeps the run with the lowest median, the measure least disturbed by the machine.

**What it cannot see.** GPU time (present is the CPU side of presenting; nothing here measures the GPU's own work or
when the display shows a frame: PresentMon does, as in PERF-2); time inside native code separately from managed code
(a native profiler would); the GC pause of a collection that starts and ends between two steps is attributed to the
step that follows.

**Tests.** `tests/Benchmarks.Tests` (33 tests): percentiles, option parsing and its errors, result files round trip,
the comparison's rules, the limit search, and every scenario run headless: repeats exactly, reports what it ran,
allocates nothing per frame where the engine should allocate nothing.

## 3. Results at the default loads (the baseline)

Measured; `suite --mode headless,render --repeat 3`; stored as [`perf-3/baseline.json`](perf-3/baseline.json).
Times in ms.

| Scenario | Load | Headless work p50 / p99 / p99.9 | Render work p50 / p99 / p99.9 | Render: draw / present p50 | Draws | Allocated B/frame (headless / render) | GC 0/1/2, pause |
| --- | ---: | --- | --- | --- | ---: | --- | --- |
| entity-churn | 10,000 | 0.213 / 0.390 / 0.512 | 0.194 / 0.334 / 0.533 | 0.001 / 0.046 | 0 | 0 / 432 | none |
| world-iterate | 20,000 | 0.348 / 0.574 / 0.660 | 1.493 / 2.159 / 2.554 | 1.320 / 1.226 | 20,000 | 0 / 432 | none |
| spatial-query | 8,000 | 3.705 / 4.641 / 6.113 | 3.762 / 4.642 / 5.219 | 0.003 / 0.093 | 0 | 0 / 432 | none |
| physics | 2,000 | 5.976 / 6.991 / 7.628 | 6.151 / 7.113 / 7.471 | 0.113 / 0.393 | 2,000 | 7 / 439 | none |
| sprites | 20,000 | 0.254 / 0.427 / 0.761 | 1.315 / 1.866 / 2.082 | 1.264 / 1.194 | 20,000 | 0 / 432 | none |
| batch-breaks | 20,000 | 0.167 / 0.267 / 0.333 | 1.476 / 2.321 / 3.053 | 1.409 / **8.097** | 20,000 | 0 / 432 | none |
| shapes | 5,000 | 0.066 / 0.116 / 0.299 | 1.446 / 2.398 / 2.983 | 1.407 / **4.848** | 10,000 | 0 / 432 | none |
| text | 500 | 0.049 / 0.083 / 0.146 | 0.526 / 2.793 / 3.342 | 0.520 / 0.566 | 10,000 | 0 / 432 | none |
| particles | 20,000 | 0.252 / 0.432 / 0.543 | 0.805 / 1.277 / 1.426 | 0.659 / 2.957 | 19,564 | 0 / 432 | none |
| asset-churn | 4 | 1.852 / 2.612 / 4.519 | 2.160 / 2.937 / 3.345 | 0.002 / 0.077 | 4 | **1,920,247 / 2,183,312** | **104/104/104**, 34 ms total, 0.55 ms worst frame |
| integrated | 5,000 | 3.244 / 3.935 / 4.233 | 3.953 / 4.820 / 5.114 | 0.718 / 2.735 | 16,126 | 8 / 440 | none |

Time per subsystem inside the update (headless, p50 / p99, ms): entity-churn create 0.046 / 0.089, destroy 0.044 /
0.085; world-iterate iterate 0.060 / 0.126; spatial-query overlap 1.550 / 2.243, raycast 2.112 / 2.831; physics
5.966 / 6.974; particles 0.119 / 0.170; asset-churn load 1.848 / 2.607, unload 0.001; integrated: physics 3.030 /
3.705, particles 0.060, world 0.016, churn 0.006. In the integrated scene **physics is 93% of the update** (3.03 of
3.24 ms) for a quarter as many bodies as entities.

Memory at the end of a run (each scenario in its own process): managed heap 1 to 8 MB headless and 5 to 50 MB in a
window (the renderer's vertex lists and the device's buffers grow with the draws: 26.7 MB at 20,000 sprites, 49.9 MB
at 10,000 shape draws); native (private bytes minus the committed managed heap) 9 to 15 MB headless and 69 to 287
MB in a window (SDL, Direct3D 11 and the driver; 287 MB with 10,000 shapes).

## 4. Runtime modes

Measured; the same published build with runtime switches set from outside, and the same source published with
NativeAOT (`-p:PublishAot=true`, 9.7 MB executable, no trim or AOT warning). Work p50 / p99 in ms; default loads.

| Scenario (mode) | JIT, default | Server GC | Non-concurrent GC | No dynamic PGO | No tiering | NativeAOT |
| --- | --- | --- | --- | --- | --- | --- |
| entity-churn (headless) | 0.215 / 0.360 | 0.239 / 0.401 | 0.240 / 0.419 | 0.236 / 0.399 | 0.253 / 0.395 | 0.247 / 0.473 |
| world-iterate (headless) | 0.389 / 0.584 | 0.363 / 0.505 | 0.312 / 0.523 | 0.524 / 0.890 | 0.527 / 0.816 | 0.523 / 0.784 |
| physics (headless) | 6.183 / 7.783 | 6.193 / 7.473 | 6.313 / 7.598 | 8.505 / 10.864 | 8.675 / 11.162 | **10.864 / 13.846** |
| asset-churn (headless) | 1.954 / 4.029 | 2.056 / 3.659 | 1.916 / 2.745 | 1.737 / 2.754 | 1.776 / 2.783 | 2.068 / 3.382 |
| integrated (headless) | 3.275 / 4.253 | 3.368 / 4.886 | 3.330 / 4.320 | 4.551 / 6.179 | 4.618 / 6.212 | **5.800 / 7.789** |
| sprites (render) | 1.781 / 2.788 | 1.399 / 2.248 | 1.403 / 2.204 | 1.796 / 2.858 | 1.774 / 2.815 | 2.201 / 3.436 |
| batch-breaks (render) | 1.567 / 2.368 | 1.623 / 2.517 | 1.581 / 2.440 | 1.871 / 3.018 | 1.872 / 3.005 | 2.284 / 3.539 |
| integrated (render) | 4.158 / 5.238 | 4.254 / 5.715 | 4.117 / 5.134 | 5.682 / 7.641 | 5.715 / 7.605 | **7.177 / 9.631** |

Garbage collection in asset-churn, the one scenario that allocates (headless; collections 0/1/2, total pause, worst
frame): default 104/104/104, 35.8 ms, 0.76 ms; server 186/88/76, 51.3 ms, **2.69 ms**; non-concurrent 104/104/104,
34.1 ms, 0.60 ms; NativeAOT 104/104/104, 28.9 ms, 0.46 ms.

What this demonstrates:

- **The collector's mode does not matter to a game that does not allocate**, which is every scenario but one: no
  collection ran in any of them in any mode.
- **Server GC is not a gain here** and is worse where collections happen: longer worst pause (2.7 against 0.76 ms)
  and more memory. Non-concurrent GC is indistinguishable from the default at these heap sizes (a few MB).
- **Without dynamic PGO the simulation takes 38% longer** (physics 6.18 to 8.51 ms, integrated 3.28 to 4.55 ms):
  the default JIT configuration should be kept, and "no tiering" (a common tip for steady frame times) costs the
  same.
- **NativeAOT is 73 to 77% slower on the simulation-heavy scenarios** (physics 10.86 against 6.18 ms; integrated
  5.80 against 3.28 ms headless and 7.18 against 4.16 ms in a window) and 24 to 46% slower on draw submission. Its
  figures are close to "JIT without PGO" plus a further gap. It also **allocates where the JIT build does not**:
  96,725 B per frame in physics (48 B per `Move`) and 60,702 in the integrated scene, with gen-0 collections (4 in
  600 frames). Hypothesis: an object the .NET 10 JIT keeps on the stack (escape analysis) and the AOT compiler does
  not. Its native memory is lower (5 against 14 MB headless). The SDK does not claim NativeAOT as a target
  ([AOT-1](aot-1.md)); this is the first measurement of what it would cost at load.
- Not run: any other runtime (Mono, WebAssembly), any other machine or system, any phone. No native-engine
  comparison was made: no methodology comparable to these scenarios was available.

## 5. Measured limits

Measured; `sweep`: the load doubles until the **99th percentile of the CPU work** of a frame exceeds **16.667 ms**
(one 60 Hz update), then the gap is halved three times; 240 measured frames after 60, best of 2, each run in its own
process. "Limit" is the largest load measured within the budget and the smallest over it. The budget is CPU work
only: it leaves nothing for a game's own logic, and in a window the present comes on top (section 6). These are
ceilings of the engine's paths on this machine, not loads a game should plan for.

| Scenario | Headless limit (within / over) | Render limit (within / over) | What the load is |
| --- | --- | --- | --- |
| entity-churn | 300,000 / 320,000 | (no drawing) | live entities, 10% replaced per step |
| world-iterate | 720,000 / 800,000 | not swept | entities iterated, moved and drawn |
| spatial-query | 22,000 / 24,000 | (no drawing) | bodies; half as many queries per step |
| physics | 3,250 / 3,500 | not swept | kinematic bodies in one 1024x576 level |
| sprites | 1,120,000 / 1,200,000 | **180,000 / 200,000** | sprites, one texture |
| batch-breaks | 1,600,000 / 1,760,000 | **160,000 / 180,000** | sprites, a texture switch each |
| shapes | 1,440,000 / 1,600,000 | **32,500 / 35,000** | circles (two draws per unit of load) |
| text | 126,976 / 142,848 | **7,936 / 8,928** | lines of 24 characters |
| particles | 1,120,000 / 1,200,000 | **280,000 / 300,000** | live particles |
| asset-churn | 30 / 32 | not swept | 128x128 textures loaded and unloaded per step |
| integrated | 12,500 / 13,750 | **11,250 / 12,500** | entities (plus bodies, particles, HUD) |

Reading them:

- **In a window the renderer is the limit of everything that draws**, by a factor of 6 (sprites), 10 (batch breaks),
  16 (text) and 44 (shapes) below the headless figure. The headless figures for drawing scenarios measure the
  counting renderer and are an upper bound nobody reaches.
- **The integrated scene is limited by physics**, in both modes (12,500 entities are 3,125 bodies: the physics
  scenario's own limit, 3,250).
- The entity-churn limit is where the tail crosses, not the median: 300,000 entities take 6.7 ms at p50 and 9.8 ms
  at p99, and 320,000 take 7.3 ms at p50 and 20 ms at p99. One of the discarded campaigns placed it at 640,000:
  treat it as "several hundred thousand", with a tail that turns first.
- Near a limit two runs differ by one refinement step (physics 3,250 measured 16.16 ms at p99 once and 15.15 ms
  another time). The brackets are a step wide for that reason.

### 5.1 Entities, world iteration and spatial queries (for WORLD-1, issue 126)

Everything WORLD-1 needs in one place. Measured unless marked.

**Per operation** (`benchmarks/WorldBench`, 1,000 entities, best of 9, ns): `All<T>` walk 1.19 per element (2.25
when one entity in ten has it); `TryGet<T>` hit 3.70, miss 4.35; `Local(id).Position2D` read 1.32, write 1.57;
`DrawPosition2D` 2.72 for a root, 8.88 under a translated parent, **102.36 under a rotated parent**; `Children`
walk 1.12 per child; create + attach + destroy + release 145.5 per entity; `BeginStep` 0.76 per entity. None
allocates.

**At scale, whole frames** (StressBench headless, p50):

| Entities | Iterate + move (`All<T>` + `Local`), ms | ns per entity | Update outside the scenario (the world's step), ms | Draw (`DrawPosition2D` + submit), ms |
| ---: | ---: | ---: | ---: | ---: |
| 20,000 | 0.088 | 4.4 | 0.021 | 0.242 |
| 80,000 | 0.272 | 3.4 | 0.132 | 0.946 |
| 320,000 | 1.312 | 4.1 | 0.555 | 3.782 |
| 640,000 | 3.128 | 4.9 | 1.306 | 7.730 |
| 1,280,000 | 6.775 | 5.3 | 2.884 | 15.417 |

Iteration is linear to 1.28 million entities at 3.4 to 5.3 ns each; the world's own step costs about 2 ns per live
entity (it copies every transform as the step's start, `BeginStep`); drawing costs 12 ns per entity on the counting
renderer. Limit: 720,000 to 800,000 entities iterated, moved and drawn headless within 16.667 ms at p99.

**Creation and destruction** (entity-churn: the load live, a tenth replaced per step, attachments pooled):

| Live entities | Replaced per step | Create, ms (ns each) | Destroy, ms (ns each) | Update outside the scenario, ms | ns per live entity |
| ---: | ---: | --- | --- | ---: | ---: |
| 10,000 | 1,000 | 0.046 (46) | 0.044 (44) | 0.123 | 12 |
| 80,000 | 8,000 | 0.426 (53) | 0.216 (27) | 0.926 | 12 |
| 300,000 | 30,000 | 1.624 (54) | 0.752 (25) | 4.328 | 14 |

Create (with two attachments and a position) costs about 50 ns and destroy 25 to 45 ns, flat with the world's size.
**The cost that grows is not in the calls: it is the release at the step's end**, 12 to 14 ns per *live* entity in
every step in which anything was destroyed, six times the 2 ns of a step without destruction, and 64% of the frame at
300,000 entities. `SceneWorld.Release` compacts the creation order and every attachment store over the whole world,
and removes each destroyed slot from every store (source: `src/MicroEngine.Scene/SceneWorld.cs`, `Release`). One
entity destroyed costs the same pass as thirty thousand.

**One anomaly, reproduced and not explained**: at exactly 160,000 entities the update takes longer than at 240,000
or 320,000 (entity-churn 9.0 ms against 5.6 and 7.3; world-iterate 5.0 ms outside the scenario against 0.56 at
320,000), in two campaigns. Hypothesis only: an alignment effect between the world's parallel arrays at that size.

**Spatial queries.** The engine has no spatial query over the scene world itself: a game asks the physics world
(`IPhysicsWorld2D.Overlap` and `RayCast`), so only entities with a body can be found. Tile backend, static 16x16
boxes at a constant density of one per 40x40 pixels, 64x64 overlaps and 128-pixel rays:

| Bodies | Queries per step (half each) | Overlap, ms (µs each) | RayCast, ms (µs each) | Step total, ms |
| ---: | ---: | --- | --- | ---: |
| 1,000 | 500 | 0.311 (1.24) | 0.567 (2.27) | 0.90 |
| 4,000 | 2,000 | 0.590 (0.59) | 0.839 (0.84) | 1.44 |
| 8,000 | 4,000 | 1.533 (0.77) | 2.108 (1.05) | 3.67 |
| 16,000 | 8,000 | 3.783 (0.95) | 5.076 (1.27) | 8.92 |
| 32,000 | 16,000 | 11.313 (1.41) | 14.283 (1.79) | 25.71 |

A query costs 0.6 to 1.4 µs for an overlap and 0.8 to 1.8 µs for a ray, and **grows with the world although the
density around each query is constant** (0.59 µs at 4,000 bodies, 1.41 µs at 32,000). The grid is a
`Dictionary<long, List<int>>` of cells and every query sorts what it found (`ShapeGrid.Query`); hypothesis: hash
lookups that miss the cache as the dictionary grows. Limit: 22,000 to 24,000 bodies with 11,000 to 12,000 queries
per step. No query allocates. The 1,000-body row is slower per query than the 4,000 one; a 240-frame run of 0.9 ms
frames is dominated by warm-up there.

**Dense kinematic bodies** (the `physics` scenario, all in one 1024x576 level, each other's obstacles): 250 bodies
0.56 ms (2.3 µs per `Move`), 1,000 1.80 ms (1.8 µs), 2,000 5.58 ms (2.8 µs), 4,000 19.8 ms (5.0 µs). The cost per
move grows with the crowd because every other box near a mover is a candidate obstacle; limit 3,250 to 3,500.
Box2D was **not run** (its native library is not built in this checkout).

## 6. End to end and sustained runs

Measured; visible window, real time, VSync on the 100 Hz display, 600 updates after 120.

| Scenario | Interval p50 / p95 / p99 / p99.9 / max, ms | Hitches | Work p50 / p99, ms | Present p50, ms |
| --- | --- | ---: | --- | ---: |
| sprites, 20,000 | 10.00 / 10.84 / 11.55 / 19.98 / 19.98 | 1 | 1.946 / 3.027 | 8.042 |
| batch-breaks, 20,000 | 10.10 / 11.72 / 13.11 / 14.79 / 14.79 | 0 | 1.461 / 2.117 | 8.563 |
| particles, 20,000 | 10.00 / 10.69 / 11.40 / 12.65 / 12.65 | 0 | 1.175 / 1.973 | 8.817 |
| asset-churn, 4 | 10.01 / 10.80 / 11.19 / 12.01 / 12.01 | 0 | 2.931 / 4.933 | 7.213 |
| integrated, 5,000 | 10.01 / 10.81 / 11.15 / 14.13 / 14.13 | 0 | 4.512 / 6.612 | 5.713 |

At the default loads every scenario holds the display's 10 ms period; present is the VSync wait. One frame in 600
missed a refresh in one scenario. **Batch-breaks is the one at its edge**: its unpaced present costs 8.1 ms
(section 3), so its interval median is already 10.10 ms and its p99 13.1 ms.

Sustained, one minute of game time (3,600 updates): the integrated scene at 5,000 held 10.00 ms at p50, 11.78 at
p99, 18.05 at p99.9, 27.81 at worst, with 10 hitches in 6,000 frames, **no collection and 433 B allocated per
frame**; hidden-window work p50 4.92 ms, p99 7.07, worst 14.81. Asset churn for a minute: 978/627/627 collections,
129 ms of pauses in total, 0.65 ms in the worst frame, no growth in memory (9.3 MB managed at the end). The pauses
are short because the heap is small; the cost of churn is the 2.2 ms of decoding per frame, not the collector.

## 7. The bottleneck fixed: the renderer's garbage

Found by the first render-mode run: with the real renderer every drawing scenario allocated megabytes per frame
(headless: zero), and ran a full collection every few frames.

Cause (source and measured): `GraphicsDeviceBase.BeginCommands` created a new `CommandList` per submission whose
data buffer started at 256 bytes and doubled as the frame's vertices were copied in (10 MB of them at 80,000
sprites), and `Renderer2D.Submit` built its run list, its sorted enumeration and a six-index array per sprite anew
every time. Above 85 KB those arrays are large objects, collected only by a full collection.

Fix (commits `058f3bd`, `de7514d`, `52c0134`; no public signature changed): the device hands the storage of the list
it has just executed to the next list; the batcher keeps its lists; storage larger than 1 MB is given up only after
64 lists in a row filled less than an eighth of it, so a one-off upload does not pin its size and the small lists of
a frame do not throw the large one's away (the first two attempts got exactly those two cases wrong, and the limit
sweep and the baseline caught them: 30 MB per frame from 80,000 sprites, then 7.7 MB per frame at 20,000). Behaviour
change for code that drives a device directly: once `Submit` returns, a submitted list holds nothing (`Commands`
empty, `Payload` throws).

Before (`f65d201`) and after (`52c0134`), measured the same way in the same idle window: hidden SDL window, default
loads, the whole suite in one process, best of 3.

| Scenario | Allocated B/frame | Collections 0/1/2 in 600 frames | GC pause total / worst frame, ms | Work p50, ms | Work p99, ms |
| --- | --- | --- | --- | --- | --- |
| sprites, 20,000 | 8,490,673 → 432 | 210/180/180 → 0/0/0 | 67.7 / 2.37 → 0 / 0 | 1.472 → 1.329 | 2.695 → 2.108 |
| batch-breaks, 20,000 | 28,409,148 → 432 | 489/451/450 → 0/0/0 | **186.6 / 5.52** → 0 / 0 | 1.809 → 1.813 | **4.966 → 2.700** |
| shapes, 10,000 draws | 19,931,026 → 432 | 192/192/192 → 0/0/0 | 114.5 / 3.12 → 0 / 0 | 1.624 → 1.469 | 2.609 → 2.442 |
| text, 500 lines | 4,257,550 → 432 | 205/190/190 → 0/0/0 | 47.3 / 0.72 → 0 / 0 | 0.778 → 0.737 | 1.259 → 1.238 |
| particles, 20,000 | 8,331,660 → 432 | 212/183/183 → 0/0/0 | 77.4 / 3.02 → 0 / 0 | 1.186 → 0.924 | 2.026 → 1.440 |
| world-iterate, 20,000 | 8,490,940 → 432 | 188/159/159 → 0/0/0 | 67.3 / 2.48 → 0 / 0 | 1.619 → 1.447 | 2.551 → 2.349 |
| physics, 2,000 | 859,573 → 439 | 124/121/120 → 0/0/0 | 18.6 / 0.49 → 0 / 0 | 6.109 → 6.248 | 7.469 → 7.498 |
| integrated, 5,000 | 7,101,330 → 440 | 230/197/197 → 0/0/0 | 74.2 / 1.02 → 0 / 0 | 4.326 → 4.132 | 5.460 → 5.107 |
| entity-churn (draws nothing) | 6,240 → 432 | 0 → 0 | 0 → 0 | 0.210 → 0.244 | 0.407 → 0.587 |

The allocation and collection counts are properties of the workload and repeat exactly; the times carry run-to-run
noise of the size of the entity-churn row, which draws nothing and "changed" by as much as most rows. What the fix
demonstrably removes is the garbage, every collection and every pause (up to 5.5 ms inside one frame, 187 ms in ten
seconds); the median barely moves and the tail improves where collections were densest (batch-breaks p99 4.97 to
2.70 ms). At 160,000 sprites a frame allocated 61 MB before the last two commits of the fix and allocates 432 B
now (allocation counts do not depend on what else the machine does).

Would a player notice? At the demos' loads, no: a demo draws a few hundred sprites, which was tens of KB per frame
and a young-generation collection now and then (PERF-2 measured 300 to 1,000 B per frame in them before this device
layer existed). At thousands of sprites, yes: a pause of 2 to 5 ms inside a 10 ms frame is a missed refresh on a
100 Hz display. Regression tests: `tests/MicroEngine.Graphics.Tests/FrameGarbageTests.cs` (five tests: a megabyte of
vertices per frame, 13 MB per frame, a large and two small lists per frame, small frames after a large one, 2,000
sprites in each sort mode, the submitted list's contract, and two frames not leaking into each other).

The 432 B that remain in a window are the `CommandList` objects of a frame (several submissions).

## 8. Optimization opportunities, by priority

Each is backed by a measurement above; none is implemented. "Perceptible" is judged against a 10 or 16.7 ms frame.

1. **Dense kinematic physics: the cost per `Move` grows with the crowd** (1.8 µs at 1,000 bodies, 5.0 µs at 4,000;
   limit 3,250; 93% of the integrated scene's update). Demonstrated: the scaling and its share. Hypothesis: the
   candidate list sorted on every query (`ShapeGrid.Query`), `List.Remove` when a moved shape is refiled, and other
   movers gathered as obstacles. Perceptible: yes, it is the first wall a game with thousands of colliding actors
   meets; a game with a few hundred does not see it (0.56 ms at 250).
2. **Draw calls in a window: 0.4 µs of present per unbatched draw** (20,000 texture switches: present 8.1 ms against
   1.2 ms for one batch; shapes: 4.8 ms for 10,000 draws). Demonstrated: the present times. Hypothesis: the SDL
   device sets three texture states and calls `SDL_RenderGeometryRaw` per run, and circles are sent as geometry
   that SDL cannot merge. Options to measure: `SortMode.Texture` where order allows, skipping unchanged state per
   draw (PERF-1 did this in the previous renderer), atlases in games. Perceptible: yes at tens of thousands of
   unbatched draws; batch-breaks at 20,000 is already at the edge of a 100 Hz frame.
3. **Releasing destroyed entities costs a pass over the whole world** (12 to 14 ns per live entity in any step with a
   destruction; 4.3 of 6.7 ms at 300,000). Demonstrated. Perceptible: only in worlds of 100,000 entities or more;
   belongs to WORLD-1's design.
4. **Text in a window has a tail five times its median** (500 lines, 10,000 glyphs: 0.526 ms at p50, 2.44 ms at
   p95, 2.79 ms at p99; sprites at twice the draws: 1.315 / 1.625 / 1.866). Per glyph the median cost is a
   sprite's (53 against 66 ns), and the limit is the same order (about 160,000 glyphs against 180,000 sprites).
   Demonstrated: the times. The tail is unexplained. Perceptible: no at HUD sizes.
5. **Loading a texture allocates 7.4 times its pixels** (480 KB per 128x128 PNG whose pixels are 64 KB; 1.92 MB and
   a full collection every six frames at four per frame). Demonstrated: bytes and collections. The pauses are short
   (0.5 to 1 ms) and the decode time (0.46 ms per texture) dominates. Perceptible: only for games that stream
   assets during play, as a steady cost rather than a hitch; `AssetCache` already offers background decoding.
6. **NativeAOT allocates per physics move and runs the simulation 43 to 77% slower** (section 4). Demonstrated.
   Relevant only if NativeAOT becomes a claimed target.
7. **Spatial queries grow with the world at constant density** (0.59 to 1.41 µs per overlap). Demonstrated; cause
   hypothesis. Perceptible: no below tens of thousands of bodies.
8. **Particles in a window allocate about 1.1 B per live particle per frame at very high counts** (319 KB per frame
   at 280,000; 432 B at 20,000). Demonstrated in the sweep; cause not looked for. Not perceptible.
9. **A rotated parent makes `DrawPosition2D` 38 times slower** (102 ns against 2.7 ns). Demonstrated by WorldBench.
   Perceptible: only with thousands of children under rotating parents.

Not opportunities, measured: the collector's mode (section 4), allocation in the engine's steady state (zero
headless in ten of eleven scenarios), entity iteration (linear, 4 to 5 ns).

## 9. Regression detection

The baseline is [`perf-3/baseline.json`](perf-3/baseline.json) (22 runs: 11 scenarios, headless and render). To
compare a later build on the same machine:

```
docs/evaluations/perf-3/campaign.sh /path/outside/the/repo idle publish baseline
dotnet run -c Release --project benchmarks/StressBench -- compare docs/evaluations/perf-3/baseline.json /path/outside/the/repo/baseline.json
```

`compare` matches runs by scenario, mode and load and exits with 1 when a run's median CPU work is more than 25%
slower, its 99th percentile more than 50% slower, or its allocation per frame more than 25% larger (each beyond a
noise floor of 0.05 ms or 256 B; `--tolerance` changes the 25). It warns, and does not judge, when the CPU, runtime,
compilation or collector differ or a workload's checksum changed. Checked in this session (run): the baseline
against a copy of itself reports no regression and exits with 0; with the fixed build's results as the baseline and
the pre-fix build's as the current run it reports 12 regressed measures (the allocation of every drawing scenario,
batch-breaks' p99, particles' median) and exits with 1.

The whole campaign of this evaluation is [`perf-3/campaign.sh`](perf-3/campaign.sh) (parts: `idle publish baseline
limits modes e2e sustained`); it took about half an hour here. Two cautions learned the hard way: check idleness
before and never stop a campaign half way without killing its script, and publish NativeAOT on Windows from a shell
with a short `PATH` (`vcvarsall` fails under Git Bash's).

**Proposal, not done (workflows are outside this task):** the times are machine-bound and hosted runners are too
noisy for a 25% gate, but **allocation per frame and the checksums are exact on any machine**. A CI job running
`suite --mode headless --frames 120 --warmup 30 --no-isolate` and comparing only allocations and checksums would
have caught the renderer's garbage (in a render-mode variant under the software driver) at a cost of seconds. The
existing `FrameGarbageTests` already cover the device and batcher paths in the normal test run.

## 10. Limitations

- One machine, one system, one GPU, one runtime version. Nothing is claimed for Linux, macOS, phones or browsers
  (WebAssembly's collector and interpreter would change sections 4 and 7 the most).
- GPU time is not measured; the render figures are the CPU side.
- The scenarios are synthetic and isolate engine paths; a game's own logic comes on top of every figure.
- Box2D and the raylib and SDL_GPU devices were not run through the harness (the modes use the SDL renderer device).
- A desktop game was open and idle during the campaign (section 1).

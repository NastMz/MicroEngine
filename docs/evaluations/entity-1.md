# ENTITY-1: an optional first-party Entity Component System, evaluated

Issue NastMz/MicroEngine#151. Record: [`odd/tasks/entity-1.md`](../../odd/tasks/entity-1.md). Decision:
[ADR 0059](../decisions/0059-no-first-party-ecs.md). Date: 2026-11-18/19. Builds on [PERF-3](perf-3.md) (the stress
harness, the limits of the engine's paths) and [WORLD-1](world-1.md) (the scene world at scale, `SpatialIndex2D`).

Evidence words are used strictly, as in PERF-3. **Measured** means run on the machine of section 1 and read from its
output. **Hypothesis** means a cause proposed for a measured effect and not isolated by an experiment. **Not run**
means exactly that. No number is a frame-rate promise: each is "this workload, this machine, this build".

**The result in one paragraph.** The scene world is not what limits a game until it holds a few hundred thousand
objects: under about 75,000 the choice of store moves a frame by less than a millisecond. Beyond that a component
store does the same work in a quarter of the time and memory, and two maintained libraries (Arch, Friflo.Engine.ECS)
and a first-party prototype all reach the same speed. A scene already has every hook a store needs. Decision:
**defer** a first-party module; a game adopts a library beside the scene world, which is documented, sampled and
tested; the engine may not reference one (ADR 0059). The decision is provisional: what was evaluated is speed,
memory and the integration of a store, not what an ECS gives as a way to model a game (section 11). The libraries differ where the figures do not show it: in what
they need to publish trimmed or with NativeAOT (section 6).

## 1. Conditions

| | |
| --- | --- |
| CPU / RAM | AMD Ryzen 5 7600X (6 cores, 12 threads), 31.2 GB |
| GPU / display | NVIDIA GeForce RTX 4070 Ti SUPER, 1920x1080 at 100 Hz (the `window` runs only) |
| System | Windows 11 Pro 10.0.26300, x64 |
| Runtime | .NET 10.0.15 (SDK 10.0.404); JIT with tiered compilation and dynamic PGO; workstation concurrent GC |
| Engine | 0.6.0-alpha.1 at commit `95ab306` (`feat/entity-1`); Release, published once and run from the published folder |
| Libraries | Arch 2.1.0 (2025-05-27), Friflo.Engine.ECS 3.6.0 (2026-03-23), both from nuget.org, pinned |
| Canvas | 320x180 at scale 3, 60 updates per second, one fixed step per frame |

**How idle the machine was.** `docs/evaluations/entity-1/campaign.sh` takes ten one-second samples of the total CPU
load before every group and adds the CPU time used meanwhile by every `dotnet`, `testhost`, `MSBuild`,
`VBCSCompiler`, `StressBench` and `EcsBench` process. The desktop's own load (peripheral software, this session's
clients) was 3 to 38% of one sample and is not this session's to close.

| Group | Idle check (UTC, 2026-11-19) | CPU load %, ten samples | CPU seconds the build, test and benchmark processes used meanwhile |
| --- | --- | --- | ---: |
| loads (section 4) | 00:37:34 | 8 14 26 21 13 23 13 4 23 15 | 0 |
| limits, `move` | 00:15:02 | 19 8 13 5 8 13 3 16 6 9 | 0.02 |
| limits, `churn` | 00:19:10 | 17 9 26 17 9 7 12 19 20 6 | 0.05 |
| limits, `mixed` | 00:22:11 | 18 6 12 12 20 12 12 7 8 11 | 0.03 |
| window | 00:26:14 | 17 19 22 20 49 25 12 18 21 14 | 0 |
| NativeAOT | 00:56:13 | 19 12 9 4 5 9 5 10 2 28 | 0.05 |

**One group was not clean and is not the record.** The first run of the loads group (00:03 UTC) started right after
the publish, while the build servers it leaves behind were still working: 2.42 CPU seconds in the ten-second check.
The group was run again at 00:37 after `dotnet build-server shutdown` (the script now does that after publishing),
and section 4 is that second run. The two agree: every checksum is equal, the medians differ by 1.9% at the median
of the 54 runs, and by more than 10% in four of them (the scene world at `churn` 100,000, 2.14 ms then 1.73, and at
the 375,000-object game step, 26.8 ms then 21.9; the unpooled world and Arch at `churn` 10,000). The loads, limits
and window groups ran the build of commit `95ab306`; the NativeAOT group ran a build of the tree that became
`7730a80` (the same workloads, plus Friflo's registration of section 6).

## 2. The harness and the workloads

`benchmarks/EcsBench` is the stress harness of PERF-3 (`benchmarks/StressBench`) with scenarios added: the harness now
takes scenarios from another program (`Scenarios.Add`, `StressProgram.Run`), so the libraries are dependencies of
EcsBench only and the harness, its baseline and its suite are unchanged. Every scenario is a scene in a real `Game`
run by `GameRunner` with the engine's `FrameProfiler`: the scene world's own step, the frame schedule and the
renderer's submission are inside every measurement, and a run records what PERF-3 records (CPU work per frame as mean,
p50, p95, p99, p99.9 and max; its update and draw parts; the scenario's own sections; bytes allocated per frame;
collections of each generation and the pauses; managed heap and private bytes; a checksum of the final state).

```
dotnet run -c Release --project benchmarks/EcsBench -- list
dotnet run -c Release --project benchmarks/EcsBench -- run --scenario ecs-mixed-simple,ecs-mixed-friflo --load 50000 --isolate
dotnet run -c Release --project benchmarks/EcsBench -- sweep --scenario ecs-churn-simple
docs/evaluations/entity-1/campaign.sh OUT          # every run of this evaluation
```

Scenarios are named `ecs-<workload>-<store>`.

| Workload | What a step does | The load counts | Why it is representative |
| --- | --- | --- | --- |
| `move` | every object keeps its previous position, moves and bounces off the canvas edges; every object is drawn interpolated | objects | projectiles, particles of the game's own, a swarm: many objects with the same few numbers |
| `churn` | the same objects with a lifetime of ten steps: a tenth is destroyed and created again every step (sections: update, destroy, create) | live objects | spawning and despawning as a way of life: bullets, sparks, pickups |
| `query` | objects with four component sets (all have a position; half a velocity; a third health; a fifth burn), asked for three combinations: position+velocity (move), health+burning (damage), position+velocity+health (count those on the left half) | objects | systems that each want a different slice of the objects |
| `mixed` | a swarm chases a moving point and loses health near it; half as many shots fly with a lifetime of 30 to 59 steps; the dead are destroyed and replaced; everything is drawn interpolated (sections: agents, shots, churn) | swarm members (objects: 1.5 times that) | a game step: two kinds of object, structural changes and drawing together |

| Store | What it is |
| --- | --- |
| `simple` | the scene world as a game would write it: one attachment class per kind (`Mover`, `Chaser`, `Shot`...), the position in the entity's `Transform` (interpolated by the world), `All<T>` to walk, `Local` to move, `TryGet<T>` for a second attachment; attachments of destroyed objects are reused from a `Stack<T>` |
| `simple-alloc` | the same with a new attachment per spawn (`churn` and `mixed` only: it differs only where objects spawn) |
| `arch` | Arch 2.1.0: `World`, `QueryDescription`, chunks walked over component spans (its fastest documented path without code generation), structural changes after the walk |
| `friflo` | Friflo.Engine.ECS 3.6.0: `EntityStore`, `ArchetypeQuery<...>.Chunks` over component spans, structural changes after the walk |
| `proto` | a first-party archetype prototype (`benchmarks/EcsBench/Prototype/MiniWorld.cs`, about 220 lines): one array per component per archetype, generation-checked ids, queries by component mask. It has what the workloads need and nothing else (section 7) |

The three component stores keep `Position`, `Previous` (for interpolation), `Velocity`, `Life`, `Health` and
`Burning` structs, and draw with `Objects.Blend`.

**The stores do the same work, and a test says so.** What an object starts with comes from its serial number, never
from the order a store walks in; the per-object arithmetic is one shared function per rule; a scenario's checksum is
the sum of a hash of each live object (position, lifetime, health), the object and spawn counts and what the queries
found. `tests/Benchmarks.Tests/EcsScenarioTests.cs` runs every workload on every store and requires the same
checksum, the same object counts and the same number of draws (53 tests in the project, 10 of them ENTITY-1's:
the four workloads on every store, what each workload does, allocation-free walks, and the prototype's id and row
rules).

**What it cannot see.** GPU time; the cost of a library's features the workloads do not use (relations, events,
command buffers, parallel walks); a browser or a phone.

## 3. Survey of .NET ECS libraries

Read on 2026-11-18 from NuGet's registration data (`api.nuget.org/v3/registration5-gz-semver2/<id>/index.json`, each
package's `.nuspec`) and each repository's GitHub record and README. Claims about NativeAOT, WebAssembly and
allocation are the projects' own statements unless section 6 says measured.

| Library | Latest stable | Licence | Repository (stars, last push) | Design | Dependencies | NativeAOT, trimming, WASM | Allocation | API fit |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| [Arch](https://github.com/genaray/Arch) | 2.1.0, 2025-05-27 | Apache-2.0 | 1,882; 2026-11-09 | archetypes in 16 KB chunks | six (Arch.LowLevel, Collections.Pooled preview, CommunityToolkit.HighPerformance, Microsoft.Extensions.ObjectPool, ZeroAllocJobScheduler, Unsafe) | README: "AOT friendly"; uses unsafe code. Measured: publishes trimmed and with NativeAOT without warnings; a NativeAOT build needs `ArrayRegistry.Add<T>()` per component (section 6) | measured: none per frame once warm | structs, spans per chunk; entity = id + version; queries are descriptions built once |
| [Friflo.Engine.ECS](https://github.com/friflo/Friflo.Engine.ECS) | 3.6.0, 2026-03-23 (4.0 previews in 2026) | MIT | 818; 2026-10-31 | archetypes | one (Friflo.Json.Fliox) plus Unsafe | README: "Native AOT", "WASM / WebAssembly", "Fully Managed C#. No unsafe code". Measured: 13 trim warnings and 19 in its dependency; a NativeAOT build needs `NativeAOT.CreateSchema()` (section 6) | README: "Zero allocations after buffers are large enough"; measured: none per frame once warm | structs with a marker interface, spans per chunk; `Entity` handle answers `IsNull` when deleted; typed queries |
| [fennecs](https://github.com/outfox/fennecs) | 0.7.3, 2026-08-05 | MIT | 483; 2026-10-20 | archetypes | none; targets `net10.0` only | README: "No code generation and no reflection required" | not measured | streams with delegates; pre-1.0 API |
| [DefaultEcs](https://github.com/Doraku/DefaultEcs) | 0.17.2, 2022-02-20 (a 0.18 beta in 2023) | MIT-0 | 765; 2024-03-01 | sparse sets | none on `netstandard2.1` | README: "not compatible with Xamarin.iOS, AOT platforms (uses the `System.Reflection.Emit` namespace)" | not measured | excluded: no release in four years, not AOT |
| [Flecs.NET](https://github.com/BeanCheeseBurrito/Flecs.NET) | none stable (4.0.4 builds, 2025-03-12) | MIT | 256; 2025-05-25 | bindings to the C library flecs | a native library per platform | static libraries for `PublishAot`, a WASM build | not measured | excluded: prerelease only, and a native library the engine's browser and phone builds would have to carry |
| [Svelto.ECS](https://github.com/sebas77/Svelto.ECS) | 3.5.2, 2024-07-14 | MIT (repository) | 1,366; 2025-05-01 | groups of entity descriptors | - | Unity-centred | not measured | a different model (engines and groups), not compared |
| [TinyEcs](https://github.com/andreakarasho/TinyEcs), [Frent](https://github.com/itsBuggingMe/Frent) | 2.0.0 (2024-12-13); betas | MIT | 155, 187; both pushed in the last month | archetypes | - | not read | not measured | young, small user base; not compared |

**Measured candidates: Arch and Friflo.Engine.ECS.** They are the two that are maintained, released as stable,
permissively licensed, managed code and archetype stores (the design that makes a walk a loop over arrays). The
others were excluded for the reason in their row, not measured and found slower.

**Adaptation cost**, measured as the code that joins a store to the engine: `ArchScene.cs` and `FrifloScene.cs` in
the benchmark (about 285 lines each for four workloads) and `samples/EcsSwarm` (about 300 lines for a playable
scene, on Arch) use only public engine API (`Scene.Enter`, `Exit`, `Update`, `Draw`, `HashState`, `Objects.Blend`,
`IRenderer.Draw`, `IDestroyedCallback`). Nothing in the engine was changed for them. One rule had to be learned: a scene that draws
with `Objects.Blend` must read `Objects` before its first draw (the engine refuses to create a world while drawing,
so the state hash never depends on the display).

## 4. Results at three loads

Measured; `campaign.sh loads` (the second, clean run of section 1): each scenario in a process of its own, 600 frames
after 120 of warm-up, the best of three runs (lowest median). Headless: update plus draw submission to the counting renderer, no graphics device.
Times in ms.

**move, load 10,000 (10,000 objects), headless**

| Store | Work p50 | p99 | p99.9 | max | Update p50 | Draw p50 | move p50 | Alloc B/frame | GC 0/1/2 | Pause total / max | Managed MB | Private MB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: | ---: |
| simple | 0.197 | 0.358 | 4.191 | 4.191 | 0.066 | 0.130 | 0.052 | 0 | 0/0/0 | none | 5.3 | 15.0 |
| arch | 0.136 | 0.200 | 0.307 | 0.307 | 0.026 | 0.110 | 0.020 | 0 | 0/0/0 | none | 1.6 | 13.0 |
| friflo | 0.132 | 0.236 | 0.345 | 0.345 | 0.026 | 0.106 | 0.020 | 0 | 0/0/0 | none | 2.7 | 15.6 |
| proto | 0.143 | 0.209 | 0.292 | 0.292 | 0.027 | 0.116 | 0.019 | 0 | 0/0/0 | none | 2.5 | 12.1 |

**move, load 100,000 (100,000 objects), headless**

| Store | Work p50 | p99 | p99.9 | max | Update p50 | Draw p50 | move p50 | Alloc B/frame | GC 0/1/2 | Pause total / max | Managed MB | Private MB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: | ---: |
| simple | 1.615 | 2.101 | 2.202 | 2.202 | 0.448 | 1.161 | 0.316 | 0 | 0/0/0 | none | 29.2 | 44.2 |
| arch | 1.205 | 1.532 | 1.733 | 1.733 | 0.152 | 1.050 | 0.150 | 0 | 0/0/0 | none | 7.2 | 24.4 |
| friflo | 1.206 | 1.589 | 1.901 | 1.901 | 0.151 | 1.052 | 0.150 | 0 | 0/0/0 | none | 12.1 | 33.2 |
| proto | 1.197 | 1.488 | 1.740 | 1.740 | 0.144 | 1.050 | 0.143 | 0 | 0/0/0 | none | 9.2 | 26.4 |

**move, load 1,000,000 (1,000,000 objects), headless**

| Store | Work p50 | p99 | p99.9 | max | Update p50 | Draw p50 | move p50 | Alloc B/frame | GC 0/1/2 | Pause total / max | Managed MB | Private MB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: | ---: |
| simple | 18.404 | 20.415 | 21.173 | 21.173 | 6.529 | 11.836 | 4.331 | 0 | 0/0/0 | none | 238.3 | 275.0 |
| arch | 12.982 | 14.341 | 15.590 | 15.590 | 2.143 | 10.805 | 2.134 | 0 | 0/0/0 | none | 62.8 | 92.3 |
| friflo | 12.310 | 14.144 | 15.331 | 15.331 | 1.547 | 10.711 | 1.536 | 0 | 0/0/0 | none | 65.6 | 120.3 |
| proto | 12.218 | 14.159 | 15.242 | 15.242 | 1.452 | 10.735 | 1.440 | 0 | 0/0/0 | none | 56.9 | 82.3 |

**churn, load 10,000 (10,000 objects), headless**

| Store | Work p50 | p99 | p99.9 | max | Update p50 | Draw p50 | create p50 | destroy p50 | update p50 | Alloc B/frame | GC 0/1/2 | Pause total / max | Managed MB | Private MB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: | ---: |
| simple | 0.187 | 0.331 | 0.434 | 0.434 | 0.186 | 0.001 | 0.029 | 0.006 | 0.070 | 0 | 0/0/0 | none | 5.4 | 15.6 |
| simple-alloc | 0.278 | 0.454 | 0.806 | 0.806 | 0.277 | 0.001 | 0.088 | 0.026 | 0.079 | 40,000 | 1/0/0 | 0.5 / 0.45 | 18.0 | 32.5 |
| arch | 0.230 | 0.471 | 0.681 | 0.681 | 0.227 | 0.002 | 0.091 | 0.043 | 0.071 | 0 | 0/0/0 | none | 1.8 | 13.6 |
| friflo | 0.138 | 0.204 | 0.336 | 0.336 | 0.138 | 0.000 | 0.036 | 0.056 | 0.035 | 0 | 0/0/0 | none | 2.9 | 17.2 |
| proto | 0.097 | 0.167 | 0.364 | 0.364 | 0.096 | 0.000 | 0.022 | 0.024 | 0.040 | 0 | 0/0/0 | none | 2.7 | 14.5 |

**churn, load 100,000 (100,000 objects), headless**

| Store | Work p50 | p99 | p99.9 | max | Update p50 | Draw p50 | create p50 | destroy p50 | update p50 | Alloc B/frame | GC 0/1/2 | Pause total / max | Managed MB | Private MB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: | ---: |
| simple | 1.732 | 2.933 | 3.537 | 3.537 | 1.730 | 0.001 | 0.475 | 0.066 | 0.619 | 0 | 0/0/0 | none | 30.0 | 45.4 |
| simple-alloc | 1.571 | 4.202 | 5.025 | 5.025 | 1.571 | 0.001 | 0.515 | 0.039 | 0.416 | 400,004 | 15/15/1 | 36.9 / 3.06 | 30.3 | 65.2 |
| arch | 0.915 | 1.191 | 1.812 | 1.812 | 0.915 | 0.000 | 0.406 | 0.327 | 0.180 | 0 | 0/0/0 | none | 8.2 | 26.1 |
| friflo | 0.655 | 1.038 | 1.173 | 1.173 | 0.654 | 0.000 | 0.216 | 0.239 | 0.196 | 0 | 0/0/0 | none | 13.3 | 33.7 |
| proto | 0.529 | 0.781 | 1.223 | 1.223 | 0.528 | 0.000 | 0.127 | 0.191 | 0.207 | 0 | 0/0/0 | none | 10.1 | 29.4 |

**churn, load 1,000,000 (1,000,000 objects), headless**

| Store | Work p50 | p99 | p99.9 | max | Update p50 | Draw p50 | create p50 | destroy p50 | update p50 | Alloc B/frame | GC 0/1/2 | Pause total / max | Managed MB | Private MB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: | ---: |
| simple | 42.605 | 54.872 | 56.782 | 56.782 | 42.600 | 0.004 | 8.725 | 2.018 | 21.137 | 0 | 0/0/0 | none | 319.0 | 448.8 |
| simple-alloc | 27.236 | 51.496 | 56.097 | 56.097 | 27.231 | 0.004 | 8.836 | 0.568 | 9.612 | 4,000,086 | 215/215/72 | 1603.6 / 21.35 | 308.4 | 504.4 |
| arch | 15.338 | 19.806 | 21.230 | 21.230 | 15.334 | 0.004 | 5.276 | 6.951 | 3.081 | 0 | 0/0/0 | none | 73.3 | 93.7 |
| friflo | 8.489 | 11.270 | 13.533 | 13.533 | 8.486 | 0.004 | 2.756 | 3.633 | 2.005 | 0 | 0/0/0 | none | 73.7 | 121.2 |
| proto | 6.346 | 8.014 | 9.652 | 9.652 | 6.343 | 0.003 | 1.423 | 2.657 | 2.127 | 0 | 0/0/0 | none | 66.0 | 104.0 |

**query, load 10,000 (10,000 objects), headless**

| Store | Work p50 | p99 | p99.9 | max | Update p50 | Draw p50 | health-burning p50 | pos-vel p50 | pos-vel-health p50 | Alloc B/frame | GC 0/1/2 | Pause total / max | Managed MB | Private MB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: | ---: |
| simple | 0.106 | 0.172 | 0.287 | 0.287 | 0.105 | 0.001 | 0.019 | 0.033 | 0.038 | 0 | 0/0/0 | none | 5.5 | 16.2 |
| arch | 0.031 | 0.049 | 0.073 | 0.073 | 0.031 | 0.000 | 0.000 | 0.020 | 0.001 | 0 | 0/0/0 | none | 1.7 | 12.3 |
| friflo | 0.031 | 0.051 | 0.191 | 0.191 | 0.031 | 0.000 | 0.001 | 0.019 | 0.001 | 0 | 0/0/0 | none | 2.3 | 16.9 |
| proto | 0.031 | 0.052 | 0.185 | 0.185 | 0.031 | 0.000 | 0.000 | 0.019 | 0.002 | 0 | 0/0/0 | none | 2.2 | 16.0 |

**query, load 100,000 (100,000 objects), headless**

| Store | Work p50 | p99 | p99.9 | max | Update p50 | Draw p50 | health-burning p50 | pos-vel p50 | pos-vel-health p50 | Alloc B/frame | GC 0/1/2 | Pause total / max | Managed MB | Private MB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: | ---: |
| simple | 0.956 | 1.518 | 4.777 | 4.777 | 0.955 | 0.001 | 0.200 | 0.183 | 0.427 | 0 | 0/0/0 | none | 30.5 | 44.7 |
| arch | 0.121 | 0.250 | 0.316 | 0.316 | 0.121 | 0.000 | 0.003 | 0.087 | 0.020 | 0 | 0/0/0 | none | 6.3 | 18.3 |
| friflo | 0.116 | 0.238 | 0.390 | 0.390 | 0.116 | 0.000 | 0.004 | 0.084 | 0.018 | 0 | 0/0/0 | none | 8.7 | 24.7 |
| proto | 0.120 | 0.249 | 0.349 | 0.349 | 0.119 | 0.000 | 0.003 | 0.084 | 0.022 | 0 | 0/0/0 | none | 7.4 | 22.6 |

**query, load 1,000,000 (1,000,000 objects), headless**

| Store | Work p50 | p99 | p99.9 | max | Update p50 | Draw p50 | health-burning p50 | pos-vel p50 | pos-vel-health p50 | Alloc B/frame | GC 0/1/2 | Pause total / max | Managed MB | Private MB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: | ---: |
| simple | 14.190 | 17.025 | 18.149 | 18.149 | 14.185 | 0.004 | 2.828 | 3.447 | 5.518 | 0 | 0/0/0 | none | 177.9 | 278.2 |
| arch | 1.343 | 1.972 | 2.064 | 2.064 | 1.342 | 0.000 | 0.042 | 0.754 | 0.540 | 0 | 0/0/0 | none | 53.3 | 81.0 |
| friflo | 1.308 | 1.784 | 5.944 | 5.944 | 1.307 | 0.001 | 0.038 | 0.722 | 0.541 | 0 | 0/0/0 | none | 96.1 | 119.2 |
| proto | 1.279 | 1.702 | 2.052 | 2.052 | 1.279 | 0.000 | 0.029 | 0.707 | 0.540 | 0 | 0/0/0 | none | 68.6 | 115.4 |

**mixed, load 10,000 (15,000 objects), headless**

| Store | Work p50 | p99 | p99.9 | max | Update p50 | Draw p50 | agents p50 | churn p50 | shots p50 | Alloc B/frame | GC 0/1/2 | Pause total / max | Managed MB | Private MB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: | ---: |
| simple | 0.314 | 0.575 | 0.677 | 0.677 | 0.125 | 0.189 | 0.077 | 0.005 | 0.027 | 7 | 0/0/0 | none | 6.0 | 19.5 |
| simple-alloc | 0.317 | 0.710 | 0.848 | 0.848 | 0.123 | 0.186 | 0.078 | 0.006 | 0.021 | 4,825 | 0/0/0 | none | 9.2 | 24.4 |
| arch | 0.228 | 0.344 | 0.398 | 0.398 | 0.073 | 0.154 | 0.056 | 0.012 | 0.004 | 5 | 0/0/0 | none | 2.1 | 17.4 |
| friflo | 0.231 | 0.399 | 0.453 | 0.453 | 0.076 | 0.154 | 0.059 | 0.006 | 0.006 | 2 | 0/0/0 | none | 3.3 | 19.0 |
| proto | 0.238 | 0.399 | 0.594 | 0.594 | 0.081 | 0.156 | 0.064 | 0.004 | 0.006 | 4 | 0/0/0 | none | 3.2 | 16.4 |

**mixed, load 50,000 (75,000 objects), headless**

| Store | Work p50 | p99 | p99.9 | max | Update p50 | Draw p50 | agents p50 | churn p50 | shots p50 | Alloc B/frame | GC 0/1/2 | Pause total / max | Managed MB | Private MB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: | ---: |
| simple | 1.901 | 2.772 | 3.115 | 3.115 | 0.838 | 1.055 | 0.433 | 0.073 | 0.166 | 902 | 0/0/0 | none | 28.2 | 42.7 |
| simple-alloc | 1.691 | 2.506 | 4.885 | 4.885 | 0.755 | 0.917 | 0.412 | 0.073 | 0.114 | 25,007 | 1/0/0 | 3.1 / 3.08 | 33.5 | 57.1 |
| arch | 1.225 | 1.718 | 1.848 | 1.848 | 0.409 | 0.801 | 0.315 | 0.060 | 0.034 | 21 | 0/0/0 | none | 6.0 | 21.1 |
| friflo | 1.177 | 1.568 | 1.796 | 1.796 | 0.398 | 0.772 | 0.309 | 0.057 | 0.024 | 7 | 0/0/0 | none | 11.1 | 31.4 |
| proto | 1.159 | 1.468 | 1.622 | 1.622 | 0.377 | 0.778 | 0.303 | 0.038 | 0.030 | 14 | 0/0/0 | none | 7.2 | 25.3 |

**mixed, load 250,000 (375,000 objects), headless**

| Store | Work p50 | p99 | p99.9 | max | Update p50 | Draw p50 | agents p50 | churn p50 | shots p50 | Alloc B/frame | GC 0/1/2 | Pause total / max | Managed MB | Private MB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: | ---: |
| simple | 21.929 | 41.716 | 45.897 | 45.897 | 9.167 | 12.624 | 3.047 | 0.544 | 4.045 | 14,091 | 0/0/0 | none | 123.4 | 143.6 |
| simple-alloc | 15.259 | 27.662 | 31.228 | 31.228 | 6.800 | 8.602 | 2.629 | 0.606 | 2.345 | 134,583 | 6/5/1 | 27.1 / 6.53 | 87.1 | 155.0 |
| arch | 6.430 | 8.080 | 8.655 | 8.655 | 2.470 | 3.900 | 1.644 | 0.457 | 0.323 | 82 | 0/0/0 | none | 27.2 | 43.3 |
| friflo | 6.166 | 7.490 | 7.790 | 7.790 | 2.238 | 3.880 | 1.638 | 0.402 | 0.160 | 28 | 0/0/0 | none | 42.8 | 62.7 |
| proto | 6.416 | 8.243 | 8.884 | 8.884 | 2.354 | 3.998 | 1.662 | 0.366 | 0.285 | 55 | 0/0/0 | none | 39.9 | 51.5 |

Private MB is the process's private bytes (the engine, the runtime and the store). The 902 to 14,091 bytes a frame of
the pooled world's game step are its own lists and the pools growing during the measured frames (no collection ran);
single frames stand out in several runs (the world's 4.2 ms at 10,000 `move`, Friflo's 5.9 ms at a million `query`):
one frame of 600 each.

Reading them:

- **Walking and moving** (`move`): the scene world spends 4.5 to 6.6 ns per object in the update (3.2 to 5.2 in the
  walk, the rest in the world's own step, which keeps every transform as the step's start), a component store 1.4
  to 2.7 ns, the copy of the previous position included. Drawing is the same loop on every store (10.5 to 13 ns per
  object submitted) and is 64 to 88% of the frame, so the whole frame differs by 1.3 to 1.5 times, not by 3.
- **Combinations** (`query`): the world answers a combination with a walk over one attachment and `TryGet<T>` for
  the others: 0.96 ms for the three questions over 100,000 objects against 0.12 ms, eight times; eleven times
  at a million.
- **Creation and destruction** (`churn`, 10,000 of 100,000 per step): creating costs 48 ns per object in the world
  (an entity, an attachment, a transform write) against 13 (prototype), 22 (Friflo) and 41 ns (Arch); destroying
  costs 7 ns in the step plus the release at the step's end (about 40 ns, in the part of the update outside the sections),
  against 19, 24 and 33 ns. Arch is the slowest of the three stores at both.
- **Garbage**: with a new attachment per spawn the world allocates 40 bytes per spawn: 400,000 bytes a frame at
  10,000 spawns a step, 15 collections of generations 0 and 1 and one of generation 2 in 600 frames, 36.9 ms of
  pauses in total and 3.1 ms in the worst frame. With pooled attachments, and on every component store, a frame
  allocates nothing and no collection runs.
- **Locality decays with churn in the world** (the surprise of the campaign). At a million objects the walk of
  `churn` takes 21.1 ms where the same walk in `move` takes 4.3: after a few hundred steps of replacement the
  attachments and the entity slots are no longer in the order they are walked. The unpooled variant walks faster
  (9.6 ms) because new attachments are allocated next to each other, and pays for it in pauses (1.6 s in 600
  frames, 21 ms in the worst one). **Hypothesis**: cache misses on the attachment objects and the transforms, reached in an order unrelated
  to their addresses; not isolated with a hardware counter. A component store moves the last row into the hole, so
  its rows stay packed: its `churn` walk costs 1.3 to 1.5 times its `move` walk, and it also counts down a lifetime.
- **A game step** (`mixed`): at 15,000 objects 0.31 ms against 0.23 to 0.24; at 75,000, 1.90 against 1.16 to 1.23;
  at 375,000 the world takes 21.9 ms at the median and 41.7 at the 99th percentile, and the stores 6.2 to 6.4 ms and
  7.5 to 8.2. The world's draw part alone is 12.6 ms there (34 ns per object against 10.5): `DrawPosition2D` reads
  two transforms per entity in the same scattered order.
- **Memory**: a million moving objects are 238 MB of managed heap in the world (two 40-byte transforms, four
  hierarchy links, the creation order, an attachment object of 40 bytes and its place in the attachment store) and
  57 to 66 MB in a component store (three 8-byte components and an entity record).
- **The three component stores are the same speed** on every walk, within the run-to-run spread. They differ in
  creation and destruction: the prototype is the fastest, Friflo second, Arch third.

## 5. Limits

Measured; `campaign.sh limits`: the load doubles until the 99th percentile of the CPU work of a frame exceeds
16.667 ms, then the gap is halved three times; 240 frames after 60, best of two, each run in its own process;
headless. As in PERF-3 these are ceilings of a path on this machine, with nothing left for a game's own logic and
with drawing submitted to a counting renderer, not loads to plan for.

| Workload | Scene world (within / over) | Arch | Friflo | Prototype | Work at the limit, p50 / p99 (world; Friflo) |
| --- | --- | --- | --- | --- | --- |
| move | 750,000 / 800,000 | 1,000,000 / 1,100,000 | 1,100,000 / 1,200,000 | 1,200,000 / 1,300,000 | 13.425 / 15.291; 13.562 / 15.114 |
| churn | 250,000 / 275,000 | 750,000 / 800,000 | 1,100,000 / 1,200,000 | 1,500,000 / 1,600,000 | 9.282 / 14.911; 9.894 / 15.721 |
| mixed | 187,500 / 200,000 | 400,000 / 450,000 | 450,000 / 500,000 | 450,000 / 500,000 | 9.182 / 14.488; 11.387 / 16.498 |

For `mixed` the load is the swarm; the objects are 1.5 times that: the world's limit is 281,000 to 300,000 objects,
Arch's 600,000 to 675,000, Friflo's and the prototype's 675,000 to 750,000. `move` is bounded on every store by
drawing (10.7 ns per object submitted is 10.7 ms per million), which is why a store only reaches 1.0 to 1.3 million
there; `churn` draws nothing and shows the stores apart: the world stops at 250,000 live objects with 25,000
replaced per step, Arch at 750,000, Friflo at 1.1 million, the prototype at 1.5 million.

### 5.1 In a window

Measured; `campaign.sh window`: the same scenarios in a hidden SDL window with the real renderer (`direct3d11`),
unpaced, best of three. "Present" is the CPU side of presenting, on top of the work.

**move, load 20,000 (20,000 objects), render**

| Store | Work p50 | p99 | p99.9 | max | Update p50 | Draw p50 | move p50 | Alloc B/frame | GC 0/1/2 | Pause total / max | Managed MB | Private MB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: | ---: |
| simple | 1.763 | 2.744 | 2.921 | 2.921 | 0.135 | 1.609 (present 1.261) | 0.095 | 432 | 0/0/0 | none | 31.1 | 140.8 |
| arch | 1.633 | 2.513 | 3.604 | 3.604 | 0.055 | 1.567 (present 1.255) | 0.048 | 432 | 0/0/0 | none | 27.9 | 134.2 |
| friflo | 1.613 | 2.512 | 2.752 | 2.752 | 0.054 | 1.554 (present 1.246) | 0.049 | 432 | 0/0/0 | none | 23.1 | 138.6 |
| proto | 1.626 | 2.496 | 2.609 | 2.609 | 0.059 | 1.562 (present 1.233) | 0.053 | 432 | 0/0/0 | none | 30.1 | 134.6 |

**move, load 100,000 (100,000 objects), render**

| Store | Work p50 | p99 | p99.9 | max | Update p50 | Draw p50 | move p50 | Alloc B/frame | GC 0/1/2 | Pause total / max | Managed MB | Private MB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: | ---: |
| simple | 8.802 | 12.159 | 19.354 | 19.354 | 0.849 | 7.924 (present 8.068) | 0.478 | 432 | 0/0/0 | none | 128.4 | 361.4 |
| arch | 8.068 | 13.237 | 25.770 | 25.770 | 0.287 | 7.751 (present 10.368) | 0.273 | 432 | 0/0/0 | none | 99.6 | 333.0 |
| friflo | 7.871 | 9.393 | 10.956 | 10.956 | 0.176 | 7.669 (present 15.480) | 0.162 | 432 | 0/0/0 | none | 113.3 | 337.4 |
| proto | 7.846 | 9.502 | 10.378 | 10.378 | 0.173 | 7.651 (present 7.980) | 0.159 | 432 | 0/0/0 | none | 99.7 | 335.6 |

**mixed, load 20,000 (30,000 objects), render**

| Store | Work p50 | p99 | p99.9 | max | Update p50 | Draw p50 | agents p50 | churn p50 | shots p50 | Alloc B/frame | GC 0/1/2 | Pause total / max | Managed MB | Private MB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: | ---: |
| simple | 3.308 | 5.290 | 5.705 | 5.705 | 0.633 | 2.629 (present 2.457) | 0.212 | 0.044 | 0.212 | 439 | 0/0/0 | none | 39.9 | 160.6 |
| arch | 2.683 | 3.993 | 4.111 | 4.111 | 0.260 | 2.423 (present 2.294) | 0.167 | 0.043 | 0.036 | 437 | 0/0/0 | none | 34.2 | 155.9 |
| friflo | 2.572 | 4.049 | 4.142 | 4.142 | 0.234 | 2.314 (present 3.421) | 0.160 | 0.039 | 0.020 | 434 | 0/0/0 | none | 29.5 | 169.5 |
| proto | 2.812 | 4.096 | 4.649 | 4.649 | 0.239 | 2.565 (present 2.368) | 0.158 | 0.034 | 0.029 | 436 | 0/0/0 | none | 28.9 | 156.1 |

**mixed, load 100,000 (150,000 objects), render**

| Store | Work p50 | p99 | p99.9 | max | Update p50 | Draw p50 | agents p50 | churn p50 | shots p50 | Alloc B/frame | GC 0/1/2 | Pause total / max | Managed MB | Private MB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: | ---: |
| simple | 22.535 | 32.699 | 36.605 | 36.605 | 4.172 | 18.345 (present 13.151) | 1.173 | 0.227 | 2.027 | 2,235 | 1/1/1 | 0.4 / 0.41 | 176.7 | 531.2 |
| arch | 12.828 | 15.232 | 17.122 | 17.122 | 1.256 | 11.538 (present 20.151) | 0.769 | 0.244 | 0.199 | 473 | 0/0/0 | none | 207.0 | 495.1 |
| friflo | 12.468 | 14.880 | 16.569 | 16.569 | 0.996 | 11.435 (present 26.771) | 0.682 | 0.188 | 0.099 | 446 | 0/0/0 | none | 169.7 | 478.4 |
| proto | 12.426 | 14.679 | 15.717 | 15.717 | 1.023 | 11.367 (present 25.985) | 0.678 | 0.171 | 0.141 | 460 | 0/0/0 | none | 168.4 | 488.8 |

Once frames are drawn the renderer is most of the frame on every store: 100,000 moving objects cost 7.8 to 8.8 ms
of work, of which the store's part is 0.2 to 0.8 ms, and presenting costs as much again or more (8 to 15 ms; it
varied between runs, with the desktop's other programs on the same GPU). The game step of 150,000 objects is over a
60 Hz update on every store once presenting is added (12.4 to 22.5 ms of work plus 13 to 27 ms). A store moves the
point where the *step* fills a frame; it does not move the point where *drawing* does, which PERF-3 placed at
180,000 sprites.

## 6. NativeAOT and trimming

Measured; `campaign.sh aot`: EcsBench published with `-p:PublishAot=true` (10.8 MB), each scenario at its default
load, best of three; and the sample published the same way and trimmed.

**Publishing** (`TrimmerSingleWarn=false`, so every warning is listed):

| Assembly | Trim and AOT analysis warnings |
| --- | --- |
| The engine, the harness, the prototype | 0 |
| Arch and its six dependencies | 0 |
| Friflo.Engine.ECS | 13 (IL2071 x10, IL2066 x3) |
| Friflo.Json.Fliox (Friflo's dependency) | 19 (IL3050 x9, IL2070 x3, IL2075 x2, IL3000 x2, IL2067, IL2055, IL2026) |

The repository builds with warnings as errors, so a NativeAOT or trimmed publish of a project that references
Friflo.Engine.ECS fails here unless its warnings are allowed (the campaign publishes with
`-p:TreatWarningsAsErrors=false`).

**Running.**

- **Friflo.Engine.ECS** failed every scenario at first ("A type initializer threw an exception") and printed its own
  instructions ("NativeAOT requires schema creation on startup"). With its six component types registered before the
  first store (`new NativeAOT()`, `RegisterComponent<T>()`, `CreateSchema()`: `FrifloScene`'s static constructor)
  every workload runs.
- **Arch** ran every scenario of the benchmark without any registration, and then failed in the sample:
  `NotSupportedException: 'EcsSwarm.Position[]' is missing native code or metadata`, from `Array.CreateInstance` in
  `Arch.Core.Chunk`. The benchmark program happens to make arrays of the same component types itself (the
  prototype's columns), the sample does not; the publish had given no warning. With `ArrayRegistry.Add<T>()` for
  each component (the sample's static constructor) the sample's NativeAOT build (0 warnings, 9.9 MB) plays its tour
  to the end, and so does a trimmed self-contained publish (`PublishTrimmed`, 0 warnings). Both libraries, then,
  need every component type named before the first row in a NativeAOT build; Friflo says so, Arch throws.
- **The scene world and the prototype** need nothing.

| Workload (load) | Store | JIT work p50 / p99 | NativeAOT work p50 / p99 | NativeAOT / JIT (p50) | Same checksum |
| --- | --- | --- | --- | ---: | --- |
| move (100,000) | simple | 1.615 / 2.101 | 2.640 / 3.563 | 1.63 | yes |
| move (100,000) | arch | 1.205 / 1.532 | 1.406 / 2.107 | 1.17 | yes |
| move (100,000) | friflo | 1.206 / 1.589 | 1.399 / 1.937 | 1.16 | yes |
| move (100,000) | proto | 1.197 / 1.488 | 1.406 / 2.024 | 1.17 | yes |
| churn (100,000) | simple | 1.732 / 2.933 | 2.251 / 3.515 | 1.30 | yes |
| churn (100,000) | arch | 0.915 / 1.191 | 1.387 / 2.023 | 1.52 | yes |
| churn (100,000) | friflo | 0.655 / 1.038 | 1.220 / 1.996 | 1.86 | yes |
| churn (100,000) | proto | 0.529 / 0.781 | 0.997 / 1.508 | 1.89 | yes |
| query (100,000) | simple | 0.956 / 1.518 | 1.316 / 1.929 | 1.38 | yes |
| query (100,000) | arch | 0.121 / 0.250 | 0.121 / 0.286 | 1.00 | yes |
| query (100,000) | friflo | 0.116 / 0.238 | 0.120 / 0.270 | 1.03 | yes |
| query (100,000) | proto | 0.120 / 0.249 | 0.120 / 0.267 | 1.00 | yes |
| mixed (50,000) | simple | 1.901 / 2.772 | 2.739 / 4.200 | 1.44 | yes |
| mixed (50,000) | arch | 1.225 / 1.718 | 1.372 / 1.984 | 1.12 | yes |
| mixed (50,000) | friflo | 1.177 / 1.568 | 1.422 / 2.025 | 1.21 | yes |
| mixed (50,000) | proto | 1.159 / 1.468 | 1.342 / 2.015 | 1.16 | yes |

NativeAOT is slower than the JIT here, as in PERF-3 (no dynamic PGO): the world by 1.3 to 1.6 times, the stores by
1.0 to 1.2 on walks and 1.5 to 1.9 on `churn`. Every run ends in the JIT run's checksum.

**Start-up, observed once each and not benchmarked**: Friflo.Engine.ECS looks through the loaded assemblies for
component types the first time a store is made and writes a line to standard output ("Assemblies loaded: 86,
duration: 463 ms" under a cold `dotnet run`, 16 ms from the built executable).

## 7. The first-party prototype: what it shows and what it lacks

`MiniWorld` is about 220 lines: archetypes keyed by a 64-bit component mask, one typed array per component, rows
kept packed by moving the last row into a hole, entity records (archetype, row, generation) with a free list,
queries that return the archetypes whose mask contains the query's, cached until an archetype is added. It
allocates only when it grows, uses no reflection and no unsafe code beyond `Unsafe.As` on its own arrays.

It shows that the libraries' speed on these workloads is the speed of the design: arrays of structs walked in
order. It does not show that the SDK could ship it. Missing, and each one is work with design decisions:

- adding and removing a component of a live entity (a move between archetypes), with the edges that make it cheap;
- deferred structural changes (a command buffer), which every non-trivial system needs to change rows it walks;
- more than 64 component types, tags, optional and excluded components in a query, queries over more than four types;
- relations and hierarchy between rows, events, serialization and the state hash, clone and bulk operations;
- chunked or parallel walks; a debugger view; documentation; an API reviewed by people who use it.

Arch and Friflo.Engine.ECS have each of these, tests for them and years of users.

## 8. Thresholds and trade-offs

Derived from sections 4 and 5; "objects" are objects simulated every step on a desktop processor like the reference
one.

| Situation | Measured | Use |
| --- | --- | --- |
| Up to about 15,000 objects | every store finishes a game step in 0.23 to 0.31 ms; the world costs 0.1 ms more | the scene world; a store buys nothing a player could notice |
| 15,000 to about 75,000 objects | the world costs 0.4 to 0.7 ms more per frame (2 to 4% of a 60 Hz update) | the scene world, with pooled attachments; it leaves 14 ms of the update |
| Thousands of spawns per step | a new attachment per spawn: a collection every 40 frames and pauses of up to 3.1 ms at 10,000 spawns a step | the scene world with pooled attachments (no garbage), or a store |
| Many systems that each ask for a combination of components over 100,000 objects or more | 0.3 ms per combination in the world, 0.04 ms in a store | a store, if those questions are most of the step |
| 100,000 to 250,000 objects with replacement | the world's frame grows faster than the count (section 4); within one 60 Hz update up to the limits of section 5 | a store when the step, not drawing, is what fills the frame |
| More than about 250,000 objects, or memory that matters (a browser tab) | the world's limit on a game step is 281,000 objects, and at 375,000 it takes 21.9 ms; a store takes 6.3 ms and a quarter of the memory | a store |
| Objects that need a child, a physics body, a mesh, an attachment of any class, a stable order | the world gives each of these; a store gives none | the scene world, or a row linked to an entity |

Trade-offs of adopting a store, beyond speed:

- **Two models in one game.** Rows are not entities: the world's hierarchy, interpolation, clock, destroy-at-step-end
  rule and state hash do not reach them. The scene supplies each by hand (the sample's five places).
- **Structural changes are deferred by the game.** The world lets a walk destroy what it visits; a store does not.
- **A dependency the game owns**: its licence, its releases, and what it needs to publish (section 6: both libraries
  need their component types named for NativeAOT, and one of them brings trimming warnings).
- **Determinism rests on the library** walking its rows in an order that depends only on the calls made. The sample's
  two runs with the same seed end in the same state hash (tested); a library that walks in parallel would not.

## 9. What was delivered

- `benchmarks/EcsBench` and its tests; the harness change that lets a program add scenarios; this evaluation's
  campaign script.
- `samples/EcsSwarm`: 20,000 swarm members and their sparks as rows of an Arch world, a beacon with a child and
  six carriers with a flag each as entities of the scene world, each carrier one row and one entity that remove
  each other. It was first written on Friflo.Engine.ECS and moved to Arch when section 6 was measured: the sample is
  the path the guide recommends, so it uses the candidate that publishes without warnings. The move changed the
  scene's loops and nothing else, which is the replacement path, done once. `tests/Samples.Tests/EcsSwarmTests.cs` plays it headlessly: the store and the world hold what they
  should, the swarm chases and is replaced, a carrier's row moves its entity and the hierarchy carries the flag,
  removal from either side removes both and a stale handle never reaches a reused row, the store is part of the
  state hash and two runs agree, a run draws every member, and playing allocates nothing once warm. Its captures
  (`crossing.png`, `closing.png`, a hidden window) were looked at: the swarm trails the beacon, sparks where members
  were caught, six carriers with their flags and then five.
- The architecture test refuses an ECS package reference anywhere but the benchmark and the sample, and an ECS
  assembly reference in any compiled engine assembly.
- The guide page [Many alike objects and Entity Component Systems](../guide/entity-component-systems.md), ADR 0059,
  the release notes.

Nothing under `src/` changed: no public API was added, changed or removed.

## 10. What a developer and a player would perceive

- **A player** of a game with a few thousand objects perceives nothing either way: the store's share of a frame is
  under a tenth of a millisecond. With a new attachment per spawn and thousands of spawns a step, a player on a
  60 Hz display could see a dropped frame when a 3.1 ms pause lands on a frame that was already near its budget
  (**hypothesis**: no end-to-end run with a display was made for this).
- **A developer** with a bullet-heavy game keeps the scene world and pools attachments. A developer with a
  simulation of hundreds of thousands of creatures adds a package reference and writes the scene the sample shows;
  the engine's renderer, input, audio, assets and scenes are used exactly as before.
- **A developer who expected `MicroEngine.Ecs`** finds a guide page instead of a package, and the reason: a module
  would be no faster than the libraries and would be a second entity model to learn and maintain.

## 11. Not run, and limitations

- **Modelling was not evaluated.** Every workload is a swarm: one behaviour over very many alike objects. Nothing
  here compares the two models for a game with many kinds of object and interacting mechanics: composition of an
  object at run time, the organization and ordering of systems, queries as a way to select what a behaviour works
  on, reuse of behaviour across kinds of object, or how the external integration reads when rows and entities are
  linked in many places. The decision is provisional for that reason; the check is a criterion of the substantial
  demo game (NastMz/MicroEngine#114), and ADR 0059 lists what it would take to reopen it.
- **No browser run**: neither library was built or run for WebAssembly here; their fit there is their own
  statement, and a trimmed desktop publish of the sample is the nearest thing measured.
- **No phone, no ARM, no second machine.** One x64 desktop processor with a large cache; the point where locality
  starts to cost will be lower on smaller caches (**hypothesis**).
- **Fennecs, TinyEcs, Frent, Flecs.NET, DefaultEcs, Svelto** were surveyed, not measured.
- **Multithreaded walks** were not measured: the engine's step is single-threaded and deterministic, and a parallel
  walk changes both.
- **The world's "simple" scenes are one idiom.** A game could keep its objects in arrays of its own and use no
  store at all (as `ParticleSystem` does); that is the prototype's design without ids, and was not measured apart.
- The loads above 250,000 in a window were not swept; PERF-3's render limits (180,000 sprites) apply to every store.

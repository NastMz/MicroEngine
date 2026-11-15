# PERF-3: extreme engine stress benchmarks and .NET GC profiling

Objective: a repeatable stress harness, the conditions it ran under, the limits it measured and a prioritized list of
optimization opportunities, each backed by a measurement. Issue NastMz/MicroEngine#118. Branch `feat/perf-3`, from
`main` at 8029c29. Delivery: local commits per work unit; no push, pull request or release without the owner.
Test runner: `dotnet test MicroEngine.slnx -c Release`. Evaluation:
[`docs/evaluations/perf-3.md`](../../docs/evaluations/perf-3.md). Builds on PERF-1 and PERF-2
([`perf-1.md`](perf-1.md), [`perf-2.md`](perf-2.md)).

## Specs

Quoted fragments are the issue's text, verbatim (L1).

- S1 Why: "The existing demos do not systematically stress MicroEngine. Establish the limits of the 2D engine and
  distinguish .NET/GC overhead from avoidable game/engine inefficiencies, GPU bottlenecks and native interop."
- S2 Workloads: "Reproducible, configurable extreme workloads: entity creation/destruction, dense simulation/physics,
  render batches and state changes, particles, asset load/unload churn and integrated scenarios."
- S3 Measurements: "Separate headless CPU, rendering and end-to-end measurements. Collect frame time
  p50/p95/p99/p99.9, allocations/frame, GC collections/pauses, CPU per subsystem, draw calls, resource counts and
  managed/native memory."
- S4 Runtime modes: "Benchmark representative sustained runs and compare relevant .NET runtime modes (JIT/AOT, GC
  modes) where actually supported. Use native comparisons only where methodology is comparable."
- S5 Bottlenecks and baseline: "Locate actual bottlenecks before proposing changes. Keep reproducible baseline results
  for regression detection."
- S6 Done when: "A repeatable stress harness, documented hardware/runtime conditions, measurable limits, and
  prioritized evidence-backed optimization opportunities exist. Do not promise or impose a universal FPS target
  without agreed test conditions."
- S7 Status: "Future work; not an order to rewrite the engine."
- S8 The assignment's bounds (2026-11-14, not the issue's text): reuse and extend the existing harnesses under
  `benchmarks/`; an engine change only for a measured bottleneck, small and contained, with no public API change, a
  regression test and before/after numbers; everything else is an opportunity in the evaluation; no edits to
  workflows, `sdk/`, `games/` or `site/`; the measured limits of entity and world iteration and of spatial queries
  must be easy to find (WORLD-1, issue 126, uses them).

## Tasks

| ID | Specs | Route | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 | S2 S3 S8 | inline | done | f65d201: `benchmarks/StressBench` (11 scenarios, three modes, percentiles, allocations, collections and pauses, memory, per-subsystem times, sweep, compare), `PhysicsBench` sharing its level; statistics, options, result files, comparison and limit search written test-first (16 tests failing on stubs, then passing); the scenario tests were written after the scenarios |
| T2 | S5 S8 | inline | done | 058f3bd, de7514d, 52c0134: command list storage and batcher lists reused between frames; `FrameGarbageTests` failing first (1,049,792 B and 2,175,856 B per frame; 37.7 MB per frame for each of the two later cases), then passing; before/after in the evaluation, section 7 |
| T3 | S2 S3 | inline | done | 040cc10: each suite scenario in a process of its own (its option test written with the code) |
| T4 | S3 S4 S5 S6 | inline | done | the final campaign at 52c0134 on the idle machine (`docs/evaluations/perf-3/campaign.sh`): baseline, limits, runtime modes (JIT switches, server and non-concurrent GC, NativeAOT), end to end, sustained; `docs/evaluations/perf-3/baseline.json` |
| T5 | S1 S5 S6 S8 | inline | done | `docs/evaluations/perf-3.md` (conditions, method, results, limits, the WORLD-1 numbers in 5.1, nine opportunities); this record; `AGENTS.md` |

Route: every task inline; no Writer trigger held (one unit of behaviour at a time, each depending on the one before).
The engine change names high-risk item 3 (a contract others consume: what a submitted `CommandList` holds); no
independent verifier ran in this session (a delegated worker does not delegate): left to the owner's review.

## Log

- L1 (2026-11-14) The issue, verbatim:

  > ## Why
  > The existing demos do not systematically stress MicroEngine. Establish the limits of the 2D engine and distinguish .NET/GC overhead from avoidable game/engine inefficiencies, GPU bottlenecks and native interop.
  >
  > ## Investigate and validate
  > - Reproducible, configurable extreme workloads: entity creation/destruction, dense simulation/physics, render batches and state changes, particles, asset load/unload churn and integrated scenarios.
  > - Separate headless CPU, rendering and end-to-end measurements. Collect frame time p50/p95/p99/p99.9, allocations/frame, GC collections/pauses, CPU per subsystem, draw calls, resource counts and managed/native memory.
  > - Benchmark representative sustained runs and compare relevant .NET runtime modes (JIT/AOT, GC modes) where actually supported. Use native comparisons only where methodology is comparable.
  > - Locate actual bottlenecks before proposing changes. Keep reproducible baseline results for regression detection.
  >
  > ## Done when
  > A repeatable stress harness, documented hardware/runtime conditions, measurable limits, and prioritized evidence-backed optimization opportunities exist. Do not promise or impose a universal FPS target without agreed test conditions.
  >
  > **Status:** Future work; not an order to rewrite the engine.

- L2 (2026-11-14) Decision: one new program, `benchmarks/StressBench`, rather than extending `PerfBench`. The existing
  benchmarks time single operations or run one synthetic game through the launcher; none runs whole frames of several
  subsystems at a chosen load with collection and memory figures. StressBench reuses what exists: the engine's
  `GameRunner` and `FrameProfiler` (a scenario is a real scene), the headless counting renderer for draw calls, and
  PhysicsBench's level, moved to `benchmarks/Shared/PhysicsLevel.cs` and used by both. PhysicsBench ran before and
  after the move (200 boxes: median 0.24 ms before, 0.20 to 0.22 ms after, on a busy machine).
- L3 (2026-11-14) Found by the first render-mode run: 8.5 MB allocated per frame at 20,000 sprites (28 MB with a
  texture switch per draw) and a full collection every few frames. Cause read in the source: a `CommandList` per
  submission whose data buffer starts at 256 bytes, and lists built per submission in `Renderer2D`. Fixed in T2
  because it met the assignment's bar (measured, small, contained, no signature change, regression test). The XML
  comments of the public members were left as they are so `docs/api` (generated, outside the edit surfaces) stays in
  step; the behaviour is in the release notes. `docs/migrations.md` is outside the edit surfaces and was not edited.
- L4 (2026-11-14/15) The fix took three commits. The first kept storage up to 16 MB; the limit sweep showed 30.7 MB
  per frame from 80,000 sprites (10 MB of vertices, then the indices doubled the buffer past the limit). The second
  kept any storage a list filled an eighth of; the baseline showed 7.7 MB per frame at 20,000 sprites, because a
  frame's small lists took the large storage and gave it up. The third gives storage up after 64 lists in a row that
  barely used it. Each case has a test that failed first. The harness found both regressions.
- L5 (2026-11-14/15) Measurement validity. The first campaign ran while two other sessions built and tested on the
  machine; the coordinator asked for it to be treated as a dry run. Two later campaigns were contaminated by this
  session's own orphans: stopping a background campaign stops the shell that was asked, not `campaign.sh` or the
  program it started, so stopped campaigns kept running beside the next one. Found through the idle check (12 to 16
  CPU seconds used by build and test processes in ten seconds) and a process list; every campaign script and
  `StressBench` process was then killed by id and the campaign run again from the start, each part to its end. Only
  that campaign's numbers are in the evaluation. The idle check itself was rewritten twice (performance counter
  names are localized on this system; a sum over processes goes negative when one exits).
- L6 (2026-11-15) NativeAOT publish fails under Git Bash on this machine ("input line too long" in `vcvarsall`, then
  GNU `link` is found instead of the linker); it works from `cmd` with a short `PATH`. Published that way, with no
  trim or AOT warning, and run.
- L7 (2026-11-15) Not done, and why: Box2D in the physics scenarios (its native library is not built here); the
  raylib and SDL_GPU devices (the render and e2e modes use the SDL renderer device); other machines and systems; GPU
  time; a CI job (workflows are outside the edit surfaces; proposed in the evaluation, section 9); an ADR (no
  architectural decision was taken: the storage reuse is an implementation detail behind unchanged signatures).
- L8 (2026-11-15) Scratch, outside the repository, not deleted:
  `C:\Users\Usuario\AppData\Local\Temp\claude\05a4b2efc44dca38\315a4405-d503-40fe-a7af-0323aca3776a\scratchpad\perf3`
  (`final3`: the final campaign's builds and results; `before`: the harness built at f65d201; `final`, `final2` and
  the files beside them: the discarded campaigns; `publish-aot.cmd`, `ab.sh` and the edit scripts).

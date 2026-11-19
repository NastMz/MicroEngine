# ENTITY-1: evaluate an optional first-party Entity Component System module

Objective: decide with measurements whether MicroEngine implements, adopts or defers an optional ECS, and deliver what
that decision needs for an external developer. Issue NastMz/MicroEngine#151. Branch `feat/entity-1` (from `6cda74a`).
Delivery strategy: `ask-on-risk` (no push, pull request or release without the owner). Test runner:
`dotnet test MicroEngine.slnx -c Release`. Evaluation: [`docs/evaluations/entity-1.md`](../../docs/evaluations/entity-1.md).

## Specs

- S1 "Establish representative workloads and compare the simple model, candidate third-party ECS options and any
  proposed first-party implementation using repeatable benchmarks."
- S2 "Record throughput, frame-time distribution, allocations/GC, entity creation/destruction cost, query/iteration
  cost and memory usage under documented hardware/runtime conditions."
- S3 "Define evidence-based thresholds and tradeoffs that explain when ECS helps and when the simple model remains
  preferable."
- S4 "Demonstrate one opt-in vertical slice without changing the default path used by existing games and samples."
- S5 "Prove through conformance or integration tests that engine subsystems remain independent of the ECS
  implementation."
- S6 "Document package/API boundaries, lifecycle and identity semantics, interoperability with the simple model, and
  the supported replacement path."
- S7 "Produce an explicit implement/adopt/defer decision; do not add ECS complexity to the engine without benchmark
  evidence."
- S8 Constraints of the issue: "Keep the existing simple architecture as the default. ECS must not become a core
  requirement or a mandatory dependency." "Make ECS explicitly opt-in and consumable as a separate first-party
  module/package." "Integrate through stable engine contracts for scene/world lifecycle, identity, transforms and
  relevant events without making graphics, physics, input, audio or other subsystems depend on ECS." "Allow the simple
  model and ECS-backed model to coexist where useful, and define clear extension points so an application can replace
  the supplied ECS implementation." "Evaluate mature .NET ECS libraries and adaptation costs before choosing to build
  and maintain a custom implementation." "Coordinate benchmark scenarios with PERF-3 and relevant world-scale scenarios
  from WORLD-1."
- S9 Judge by an external developer (the project's rule for research issues, as in NAV-1 S7): could someone outside the
  repository build that game today with the SDK. Research ends in implementation where the evidence supports it.
- S10 Scope limits (this task's brief): the demos (`games/**`) are not changed; the simple model stays the default;
  graphics, physics, input, audio and scenes never reference an ECS; existing public API is not redesigned beyond what
  the decision needs; nothing is pushed, published or released; results separate what was demonstrated, what is a
  hypothesis and what a player or developer would perceive.

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 Map the simple model, the PERF-3 and WORLD-1 measurements, the harness and the package wiring | S1 S8 | inline | done | L2 |
| T2 Survey mature .NET ECS libraries from primary sources | S8 | inline | done | L2; evaluation |
| T3 Workloads in the stress harness for the simple model, the candidates and a first-party prototype; their tests | S1 S2 | inline | pending | |
| T4 Measure on an idle machine; thresholds | S2 S3 | inline | pending | |
| T5 Decision (ADR) and what it needs: slice, tests, independence proof | S4 S5 S7 S9 | inline | pending | |
| T6 Evaluation, guide, release notes, architecture, API reference | S3 S6 | inline | pending | |

## Log

- L1 (2026-11-18) The issue, verbatim:

  > ### The problem it solves
  >
  > MicroEngine's current simple world/entity model is the right default for small and conventional games. However,
  > games with very large numbers of similarly structured entities, projectiles, particles, enemies or simulation
  > objects may need data-oriented storage, efficient component queries and predictable iteration costs.
  >
  > Today, a developer with that need must implement an ECS from scratch or integrate an external one directly with
  > engine subsystems. That can duplicate entity identity and lifecycle rules, create adapters in every game, and
  > accidentally couple graphics, physics, input, audio or scene management to one ECS implementation.
  >
  > [WORLD-1](https://github.com/NastMz/MicroEngine/issues/126) covers world scalability and spatial queries, but it
  > does not evaluate an optional first-party ECS capability.
  >
  > ### What you propose
  >
  > Evaluate and, only when measured evidence justifies it, provide an optional first-party Entity Component System
  > module.
  >
  > The evaluation and design should preserve these constraints:
  >
  > - Keep the existing simple architecture as the default. ECS must not become a core requirement or a mandatory
  >   dependency.
  > - Make ECS explicitly opt-in and consumable as a separate first-party module/package.
  > - Integrate through stable engine contracts for scene/world lifecycle, identity, transforms and relevant events
  >   without making graphics, physics, input, audio or other subsystems depend on ECS.
  > - Allow the simple model and ECS-backed model to coexist where useful, and define clear extension points so an
  >   application can replace the supplied ECS implementation.
  > - Evaluate mature .NET ECS libraries and adaptation costs before choosing to build and maintain a custom
  >   implementation.
  > - Coordinate benchmark scenarios with [PERF-3](https://github.com/NastMz/MicroEngine/issues/118) and relevant
  >   world-scale scenarios from WORLD-1.
  >
  > Acceptance criteria:
  >
  > - [ ] Establish representative workloads and compare the simple model, candidate third-party ECS options and any
  >   proposed first-party implementation using repeatable benchmarks.
  > - [ ] Record throughput, frame-time distribution, allocations/GC, entity creation/destruction cost,
  >   query/iteration cost and memory usage under documented hardware/runtime conditions.
  > - [ ] Define evidence-based thresholds and tradeoffs that explain when ECS helps and when the simple model remains
  >   preferable.
  > - [ ] Demonstrate one opt-in vertical slice without changing the default path used by existing games and samples.
  > - [ ] Prove through conformance or integration tests that engine subsystems remain independent of the ECS
  >   implementation.
  > - [ ] Document package/API boundaries, lifecycle and identity semantics, interoperability with the simple model,
  >   and the supported replacement path.
  > - [ ] Produce an explicit implement/adopt/defer decision; do not add ECS complexity to the engine without benchmark
  >   evidence.
  >
  > ### What you do today instead
  >
  > Use the existing simple world/entity model and add focused optimizations such as spatial indexing when profiling
  > identifies a real bottleneck. Games that already require ECS can integrate a third-party library at the
  > application layer, but they must currently own the adapters, identity mapping, lifecycle coordination and
  > subsystem integration themselves.

  The brief that opened this task adds S9 and S10.
- L2 (2026-11-18) Mapping. The simple model (`IWorld`/`SceneWorld`) stores per entity two `Transform`s (80 bytes), four
  hierarchy links, a creation-order place and a generation; attachments are class instances in one dense array per
  type, walked by `All<T>`; a second type is reached with `TryGet<T>` per element. PERF-3 measured it at 3.4 to 5.3 ns
  per entity iterated and moved up to 1.28 million, churn within one 60 Hz step up to about 300,000 live entities, and
  the renderer (180,000 sprites) and physics (3,250 bodies) as the limits a game meets first. A scene already offers
  the seams an application-level store needs: `Enter`/`Exit`, `Update` at the fixed step, `Draw`, `Objects.Blend`,
  `HashState`. Survey (NuGet and GitHub, read 2026-11-18): Arch 2.1.0 (Apache-2.0, six dependencies), Friflo.Engine.ECS
  3.6.0 (MIT, managed only, declares NativeAOT and WASM), fennecs 0.7.3 (MIT, net10.0 only), DefaultEcs 0.17.2 (MIT-0,
  last stable 2022, `Reflection.Emit`), Flecs.NET (MIT, native library, prerelease builds only). Details in the
  evaluation.

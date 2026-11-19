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
| T3 Workloads in the stress harness for the simple model, the candidates and a first-party prototype; their tests | S1 S2 | inline | done | `95ab306`; `Benchmarks.Tests` 53/53, the ten new ones included (L3) |
| T4 Measure on an idle machine; thresholds | S2 S3 | inline | done | `7730a80` (campaign script); evaluation §1, §4 to §6, §8 (L4, L6) |
| T5 Decision (ADR 0059: defer) and what it needs: the sample, its tests, the independence proof | S4 S5 S7 S9 | inline | done | `98279fd`; `EcsSwarmTests` 9/9, architecture tests 23/23; RED by mutation (L5) |
| T6 Evaluation, ADR, guide, release notes, architecture, decision index | S3 S6 | inline | done | the documentation commit; snippets check, `api-reference.mjs --check`, `version.mjs check` (L7) |

Routes: every task inline. No writer trigger held (one behaviour after another, each depending on the previous
one's result), and the measurements needed the machine to itself, which rules out parallel work.

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
- L3 (2026-11-18) The benchmark. `benchmarks/EcsBench` is the stress harness with scenarios added from outside it
  (`Scenarios.Add`, `StressProgram.Run`; StressBench's own list and baseline are unchanged), so only EcsBench
  references Arch and Friflo.Engine.ECS. Four workloads (move, churn, query, mixed) on five stores (the scene world
  pooled and unpooled, Arch, Friflo, a first-party archetype prototype of about 220 lines). Every store must end a
  workload in the same checksum; the test that says so passed on the first build of the three component stores. One
  expectation of mine was wrong, not the code (shots live 30 to 59 steps, so few agents die in 48). A rule learned:
  a scene that draws with `Objects.Blend` must read `Objects` before its first draw.
- L4 (2026-11-19) Measurements (evaluation §4, §5). The first run of the loads group started while the build servers
  of the publish were still working (2.42 CPU seconds in the idle check): discarded as the record, run again clean,
  the two agreeing within 1.9% at the median. CPU work of a frame at the median: 100,000 objects moved and drawn
  1.62 ms in the scene world against 1.20 to 1.21 in a store; a tenth of 100,000 replaced per step 1.73 against 0.53
  to 0.92; three combination queries 0.96 against 0.12; a game step of 375,000 objects 21.9 ms (41.7 at the 99th
  percentile) against 6.2 to 6.4. Limits within one 60 Hz update: a game step of 281,000 objects in the world,
  600,000 to 675,000 in a store. Memory per million objects 238 MB against 57 to 66. A new attachment per spawn at
  10,000 spawns a step: 15 collections and 36.9 ms of pauses in 600 frames. In a window drawing is most of the
  frame on every store. The prototype is as fast as the libraries on walks and faster on creation.
- L5 (2026-11-19) Decision: defer a first-party module (ADR 0059). The slice is `samples/EcsSwarm`, written on
  Friflo.Engine.ECS first with its tests ahead of the first build (nine tests; three expectations of mine were wrong:
  the mean distance of a swarm whose caught members reappear at the edges, the scene of a finished `GameRunner`
  run, a beacon the scene clamps to the canvas). RED was observed by mutation: without `Forget` and without the
  store in `HashState`, three tests fail; with the sample removed from the allowed consumers, the architecture test
  names it.
- L6 (2026-11-19) NativeAOT and trimming (evaluation §6), which changed the sample's library. Friflo.Engine.ECS:
  13 trimming warnings and 19 in its JSON dependency, a type initializer failure under NativeAOT until its component
  types are registered, a line written to standard output at start-up. Arch: no warning, ran in the benchmark, and
  threw in the sample (`'EcsSwarm.Position[]' is missing native code or metadata`) until `ArrayRegistry.Add<T>()`
  named each component; it had worked in the benchmark only because the prototype makes arrays of the same types.
  The sample moved to Arch (the scene's loops changed, nothing else); its NativeAOT and trimmed publishes give no
  warning and play the tour. The tour's two captures were looked at.
- L7 (2026-11-19) Documentation: the evaluation, ADR 0059 (Accepted: the engine is Alpha, and nothing public
  changed), the guide page "Many alike objects and Entity Component Systems" with its code quoted from the sample,
  the release notes, the architecture page, the decision index, AGENTS. Not done, with reasons: `docs/migrations.md`
  (nothing breaks), `docs/api` (no public API changed; the check passes unchanged), `THIRD-PARTY-NOTICES.md` (outside
  this task's edit surfaces, and the two libraries are not shipped by the SDK or the demos; noted in the ADR). Not
  run: a browser build of either library, the site build, the SDK's external-project checks that need packed
  packages (`sdk/tests/docs.mjs` lists the new sample; it was not run here).
- L8 (2026-11-18) Independent verification (Risk item 5): no blocker. The stress harness prints the same output,
  exit codes and checksums at the base and at the head; the figures quoted match the campaign files; the sample
  builds against the published packages. Corrected after it: the architecture test also reads props and targets
  files, central package versions, package ids that extend a listed one and project references to the two consumers
  (a package given through an imported props file passed before); `SwarmScene` keeps its predicate, so a step
  allocates 0 bytes where it allocated about 65, and the test asserts 0; a new test pins that leaving the scene
  releases the store; the ADR and the guide state the scattered-memory cause as likely, not as fact; the pause the
  evaluation quotes is the recorded run's (3.1 ms). RED by mutation: an empty `Exit` and the capturing lambda fail
  two tests; a props file naming `Arch.Extended` under `src/` fails the architecture test. Left as is: `EcsBench help`
  prints the harness's usage and its `suite` also runs the harness's scenarios; the compiled-assembly test loads the
  18 engine assemblies this test project references, not all of them.
- L9 (2026-11-18) The owner's correction after reading the pull request, verbatim:

  > La PR #170 me parece bien sustentada y podemos integrarla, pero quiero ajustar el alcance de la conclusión del
  > ADR 0059 antes de cerrar ENTITY-1. Mi preocupación es que la evaluación se centró principalmente en rendimiento,
  > memoria y viabilidad técnica de integrar un ECS externo. Son dimensiones válidas, pero ECS también aporta un
  > paradigma de modelado: composición dinámica, organización de sistemas, consultas y reutilización de
  > comportamientos. No estoy proponiendo incorporar ECS al SDK ni desarrollar uno propio. Quiero que la decisión de
  > no ofrecer una integración oficial opcional tenga fundamentos arquitectónicos, no solamente métricas de
  > rendimiento. [...] 2. Revisar el ADR 0059 para dejar explícito qué dimensiones se evaluaron y cuáles quedaron
  > pendientes. La decisión de diferir ECS sigue siendo válida, pero provisional. 3. Añadir entre las condiciones
  > para reconsiderar la decisión la complejidad de modelado, composición, coordinación de sistemas y ergonomía de
  > integración, no únicamente rendimiento o problemas técnicos. 4. Identificar la issue existente de la futura demo
  > compleja (mini Hollow Knight) y añadir allí un criterio de evaluación: comprobar si el modelo actual y la guía de
  > integración externa son suficientes para implementar mecánicas complejas sin duplicación ni complejidad
  > excesiva. 5. Si durante esa demo aparecen limitaciones reales, comparar el modelo actual con una integración
  > externa siguiendo la guía y, si se justifica, con un adaptador oficial experimental. No quiero crear otra issue
  > de ECS por ahora ni implementar funcionalidades adicionales sin evidencia. [...] En resumen: aceptamos la
  > decisión actual, pero no damos por demostrada la ausencia de valor arquitectónico de un ECS opcional.

  Done: ADR 0059 is Accepted and provisional, with a "Scope of the evaluation" section (what was evaluated, the five
  modelling dimensions that were not), reopening conditions 5 (modelling) and 6 (ergonomics of the external
  integration) and where they are looked for; the evaluation says the same in its summary and in section 11; the
  decision index and AGENTS name it provisional. The check is a criterion of the substantial demo game
  (NastMz/MicroEngine#114, DEMO-5; the issue names no genre yet). No new issue, no code. S7 is unchanged: the
  decision is still to defer.

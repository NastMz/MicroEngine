# 0059 — No first-party Entity Component System for now; a game runs one beside the scene world

Status: Accepted (2026-11-18), provisional: it rests on the dimensions listed under "Scope of the evaluation" and is
looked at again with the evidence of the substantial demo game (NastMz/MicroEngine#114). The SDK is in Alpha.
Evidence: [ENTITY-1](../evaluations/entity-1.md). Task record: [`odd/tasks/entity-1.md`](../../odd/tasks/entity-1.md).
Issue NastMz/MicroEngine#151.

## Context

The scene world (`IWorld`, `SceneWorld`) is the SDK's only entity model: entities with a transform, a hierarchy and
attachments that are objects of the game's own classes. The issue asks whether games with very many alike objects
(projectiles, particles, swarms, simulated creatures) need a data-oriented store, and whether the SDK should supply
one as an optional module, adopt an existing library, or leave it.

ENTITY-1 wrote four workloads (alike objects moved and drawn; a tenth of them replaced every step; three questions
about component combinations; a game step with a swarm, shots, deaths and drawing) on five stores (the scene world
with pooled attachments and with a new attachment per spawn, Arch 2.1.0, Friflo.Engine.ECS 3.6.0, and a first-party
archetype prototype of about 220 lines), ran them through the real game loop in the stress harness, and checked that
every store ends each workload in the same state. On the reference machine (Ryzen 5 7600X, .NET 10, JIT), CPU work
of a frame at the median, drawing submitted to a counting renderer:

| Workload | Objects | Scene world | Arch | Friflo | Prototype |
| --- | ---: | ---: | ---: | ---: | ---: |
| move and draw | 100,000 | 1.62 ms | 1.21 | 1.21 | 1.20 |
| move and draw | 1,000,000 | 18.4 ms | 13.0 | 12.3 | 12.2 |
| replace a tenth per step | 100,000 | 1.73 ms | 0.92 | 0.66 | 0.53 |
| three combination queries | 100,000 | 0.96 ms | 0.12 | 0.12 | 0.12 |
| game step | 15,000 | 0.31 ms | 0.23 | 0.23 | 0.24 |
| game step | 75,000 | 1.90 ms | 1.23 | 1.18 | 1.16 |
| game step | 375,000 | 21.9 ms | 6.4 | 6.2 | 6.4 |
| largest game step within 16.7 ms at the 99th percentile | | 281,000 | 600,000 | 675,000 | 675,000 |

What the figures say:

- **Under about 75,000 objects the store is not what a frame spends its time on**: the difference is under a
  millisecond of a 16.7 ms update, and drawing each object costs the same on every store. In a window the renderer
  comes first: 150,000 objects drawn take 12 to 22 ms of CPU work plus 13 to 27 ms of presentation on every store
  (PERF-3 placed the renderer's limit at 180,000 sprites and physics at 3,250 bodies).
- **From a few hundred thousand objects the scene world stops fitting and a component store does not**: with churn
  the world's objects (one heap object per attachment, 80 bytes of transforms per entity) cost more to walk than their count suggests
  (measured; that they end up scattered in memory is the likely cause, not isolated),
  and at 375,000 objects the same game step takes three and a half times longer and misses a 60 Hz update that the
  stores finish in 40% of it. A component store also needs a quarter of the memory (57 to 66 MB per million objects
  against 238).
- **A store of the engine's own would not be faster than the libraries**: the prototype is within a few percent of
  both on every walk and ahead only on creation and destruction, and it is a prototype because it lacks everything
  else an ECS is used for (adding and removing components of live entities, deferred commands, relations, events,
  serialization, parallel walks, tooling).
- **The libraries need nothing from the engine that a scene does not already give**: a store owned by the scene
  (`Enter`, `Exit`), stepped in `Update`, drawn with the world's `Blend`, added to the state hash in `HashState`, and
  linked to an entity (`EntityId` in a component, the row in an attachment with `IDestroyedCallback`) when an object
  needs a child or a physics body. The `EcsSwarm` sample is that integration, in about 300 lines, with its tests.
- **Publishing is where the libraries differ.** Arch publishes trimmed and with NativeAOT without warnings and runs,
  once each component type is named (`ArrayRegistry.Add<T>()`; without it a NativeAOT build throws at the first
  row). Friflo.Engine.ECS produces 13 trimming warnings and its JSON dependency 19 more, and fails in a NativeAOT
  build until its component types are registered (`NativeAOT.CreateSchema()`); it then runs. Neither was run in a
  browser.

Both libraries are maintained and permissively licensed (Arch: Apache-2.0, six dependencies; Friflo.Engine.ECS: MIT,
managed code only, one dependency).

## Scope of the evaluation

An Entity Component System is two things: a way to store many alike objects, and a way to model a game (objects
composed from components that come and go, behaviour organized as systems, queries that select what a system works
on, behaviours reused across kinds of object). ENTITY-1 evaluated the first and not the second.

Evaluated, with measurements or runs:

- CPU work per frame, limits within a 60 Hz update, memory and garbage, on four workloads of alike objects.
- Whether a third-party store can run beside the scene world with the hooks a scene has, and what that costs to
  write (the sample, about 300 lines) and to keep correct (its tests).
- Publishing: trimmed and NativeAOT builds of both libraries.
- Whether a store of the engine's own would be faster (the prototype).

Not evaluated:

- **Modelling.** Whether a game with many kinds of object and interacting mechanics is clearer, shorter or less
  repetitive as components and systems than as entities with attachments of the game's classes.
- **Composition at run time.** Adding and removing capabilities of a live object (a status effect, a carried item, a
  changed control scheme) in each model.
- **Coordination of systems.** Ordering, dependencies between behaviours and the events between them, which the
  scene world leaves to the scene's `Update` and to each attachment.
- **Reuse of behaviour** across kinds of object, and what duplication the attachment model leads to in a large game.
- **Ergonomics of the external integration** in a game where rows and entities are both many and linked in many
  places, rather than in a sample with one link.

The workloads are swarms: one kind of behaviour over very many objects. None of them is a game with complex
mechanics, so they say nothing about these five. The decision below is therefore a decision to defer on the evidence
there is. It does not show that an optional official integration would have no architectural value.

## Decision

**Defer.** MicroEngine does not implement an Entity Component System and does not adopt one as a dependency or as an
SDK package. The scene world stays the only entity model of the SDK and the default of every template, sample and
demo.

- **A game that needs a component store adopts a library itself**, as a package reference of the game, and runs it
  beside the scene world through the scene's existing hooks. That path is documented
  ([Many alike objects and Entity Component Systems](../guide/entity-component-systems.md)), shown by a sample
  (`samples/EcsSwarm`, on Arch: the candidate that publishes trimmed and with NativeAOT without warnings) and tested
  (`tests/Samples.Tests/EcsSwarmTests.cs`).
- **No engine assembly, tool, template or demo game references an ECS library.** The architecture test fails if a project,
  props or targets file names one, if an engine, tool, template or demo project references a consumer, or if a
  compiled engine assembly it loads references one (`No_engine_project_tool_template_or_demo_references_an_entity_component_system`,
  `No_compiled_engine_assembly_references_an_entity_component_system`); the benchmark and the sample are the two
  consumers it allows.
- **No engine contract changes.** `IWorld`, the scene hooks and the physics contract are what they were; nothing
  was added for a store, because the measured integration needed nothing.
- **The comparison stays in the repository** (`benchmarks/EcsBench`, on the stress harness) so the decision can be
  measured again; its test keeps the stores doing the same work.
- **The replacement path is the same as the adoption path**: the engine knows no store, so changing the library, or
  writing one's own, changes the game's scenes in the five places the guide names. EcsBench runs the same four
  workloads on three stores through those places and ends in the same state on each.

**What reopens the decision** (any one):

1. An engine subsystem has to read or write component data itself to stay within a frame: for example a renderer
   path that takes spans of positions, or physics over component arrays. Today every subsystem takes values
   (positions, rectangles, `EntityId`), and the glue is a loop in the game.
2. The limits a game meets first move: once drawing and physics allow several hundred thousand objects in a window
   on the reference machine, the world's store becomes the first limit for ordinary games instead of the last.
3. The maintained libraries stop fitting the SDK's platforms: no library that is managed code and usable trimmed,
   with NativeAOT and in browsers.
4. The integration proves error-prone in practice: games outside the repository that get the link between a row and
   an entity, the interpolation or the state hash wrong in ways a first-party module would prevent.
5. Modelling asks for it: a game with complex mechanics written on the scene world shows recurring problems that
   components and systems would remove. The signs are the same behaviour written again for several kinds of object,
   capabilities of a live object that are awkward to add and remove, an order of updates or a set of dependencies
   between behaviours that the scene keeps by hand and gets wrong, or questions about combinations of capabilities
   answered by walking everything.
6. The external integration is not ergonomic in such a game: following the guide takes glue that grows with the
   number of links between rows and entities, or that each game writes again in the same way.

**Where conditions 5 and 6 are looked for.** The substantial demo game (NastMz/MicroEngine#114) carries the check as
one of its evaluation criteria: whether the scene world and the guide for an external store are enough to build its
mechanics without duplication or excessive complexity. If real limits appear there, the same mechanics are compared
on the scene world and on an external library integrated as the guide says and, if that comparison justifies it, on
an experimental official adapter. If the scene world and the guide prove enough, that is evidence for this decision,
and it is recorded here. No separate issue tracks this, and nothing is implemented ahead of that evidence.

If it is reopened, what is added is optional: a package, or first an experimental adapter over a library, that
references the engine and that nothing in the engine references, like `MicroEngine.Navigation`.

## Alternatives considered

- **Implement a first-party module** (`MicroEngine.Ecs`, an archetype store with `EntityId`-style handles). The
  prototype shows it would match the libraries' speed, not beat it, and the work is not the store but everything
  around it; the SDK would maintain a second entity model, its documentation and its interplay with the first, to
  serve games that already have two good libraries one package reference away. Its one advantage that was measured
  is publishing: the prototype needs no registration and produces no warning.
- **Adopt a library inside an SDK package** (a `MicroEngine.Ecs` that wraps or re-exports Arch or Friflo). The
  library's types are the API of an ECS, so they cannot be hidden as DotTiled's are behind `MicroEngine.Tiled`; the
  package would pin every game to one library and one version, and tie SDK releases to the library's, for glue of
  five places that reads better in the game.
- **Make the scene world data-oriented** (struct attachments, archetypes inside `SceneWorld`). It would change the
  model every game uses, to help the games that are the exception; hierarchy, attachments of any class and stable
  attach order are what the default is for.
- **Small contracts for stores in the engine** (a blend on the scene, a registration for extra hashed state). Both
  exist already as `Objects.Blend` and `Scene.HashState`; a second way to reach them would be API without a need.

## Consequences

- A game with hundreds of thousands of objects has a documented, tested way to run them, and carries a third-party
  dependency it chose; the SDK carries none.
- The guide tells developers when not to bother: under tens of thousands of objects the scene world costs a small
  part of a millisecond more, and attachments of destroyed objects should be reused, since a new attachment per spawn
  is garbage (15 collections and 37 ms of pauses in ten seconds at 10,000 spawns a step).
- Two packages (Arch, Friflo.Engine.ECS) are restored when the solution builds: both by the benchmark, Arch also by
  the sample. Neither is shipped in an SDK package or a demo build, and `THIRD-PARTY-NOTICES.md` (the notices of what
  the SDK and the demos ship) does not list them.
- The sample depends on Arch 2.1.0 keeping its API; a new major version is a change to the sample.
- What the measurements do not cover is listed in the evaluation, first of all a browser.
- The question of modelling stays open until a game with complex mechanics answers it; this record is revised with
  what the substantial demo game finds, whichever way it points.

# 0065 — An optional Entity Component System package over Arch; the scene world stays the default

Status: Accepted (2026-11-19). The SDK is in Alpha: a new package and its public types carry no compatibility
commitment. Supersedes [ADR 0059](0059-no-first-party-ecs.md), whose measurements stand. Evidence:
[ENTITY-1, reopened](../evaluations/entity-1b.md) and [ENTITY-1](../evaluations/entity-1.md). Issue
NastMz/MicroEngine#151.

## Context

ADR 0059 deferred an Entity Component System on two grounds: under about 75,000 objects the store is not what a
frame spends its time on, and the integration is "glue of five places that reads better in the game". It listed
what it had not evaluated: modelling, composition at run time, the coordination of systems, reuse of behaviour, and
the ergonomics of the integration in a game.

Two things changed. The engine gained scene systems (ADR 0060), a place where something that belongs to a scene is
started, stepped, hashed and stopped with it; the glue no longer has to live in the game for lack of anywhere else.
And the modelling comparison was made (`benchmarks/EcsModel`): one slice grown in three steps on the scene world and
on Arch, the two versions equal at every update.

- For a game whose kinds of object are few and fixed, the scene world is shorter, plainer and ordered: adding status
  effects to one kind of enemy took +49 lines there and +66 on the store, which pays for changes that cannot happen
  during a walk and for a walk order that is not the order of creation.
- When a second kind of object shared some behaviours and not others, the store took +20 −7 lines and changed no
  walk; the world took +56 −37, a base class, a double attachment and nine tests of "what kind is this". A question
  about a combination is a query description on the store and a walk over everything on the world.

So an ECS is a modelling choice that pays from the moment kinds share behaviour partially or capabilities come and
go, whatever the number of objects. And a developer who makes that choice wrote, in every scene, the store's
lifetime, a registration per component that fails only in a NativeAOT build, a two-way link with an attachment
class, a copy of every position per step, a hash walk and lists of deferred changes: generic code, easy to get
subtly wrong, and written three times in this repository alone.

## Decision

- **`MicroEngine.Ecs`, an optional package.** It references `MicroEngine` and the Arch library (Apache-2.0, pinned
  to `[2.1.0]`). No engine assembly, tool, template or demo references the package or any ECS library; the
  architecture test allows the one project and the one library and refuses everything else. The scene world
  (`IWorld`) stays the default model of the SDK and of everything it ships.
- **It adopts the library and does not hide it.** `EcsStore.World` is `Arch.Core.World`, `EcsStore.Commands` is
  Arch's `CommandBuffer`, rows are Arch's `Entity`, queries are Arch's. A wrapper would either hide what an ECS is
  used for or duplicate an API that is documented elsewhere. The consequence is accepted: Arch's public types are
  part of this package's surface, the version is pinned, and a new Arch version is a deliberate change of the
  package (a breaking one is a breaking change for games that use it).
- **It owns the integration**, as a scene system (`EcsStore`):
  - *Lifetime.* The store is created when its scene starts and destroyed when it stops. `scene.AddEcs(components)`
    gives a scene one; `Scenes.AddEcs(components)` gives every scene one.
  - *Systems.* `Systems` run before the scene's `Update` and `LateSystems` after it, in the order they were added,
    each with an `Enabled` switch. A system is an `EcsSystem` (a class with `Update` and access to the store) or a
    function. Not Arch's own system type: it has no place in a scene's step and no switch, and brings a source
    generator and a second package.
  - *Deferred changes.* The command buffer is applied after each system and after the scene's `Update`; `Flush`
    applies it between two walks of one system.
  - *Links.* `Link(row, entity)` joins a row to an entity of the scene's world, for an object that needs both.
    Either is found from the other; destroying either (through the world, the library's own `Destroy` or the
    commands) destroys both when the step ends; a stale handle on either side reaches nothing. Rows are not linked
    unless the game links them.
  - *Interpolation.* The store keeps the components marked with `Interpolate<T>` as they were when the step started,
    by row id beside the library's storage, and `Interpolated<T>().Blended(row, now)` reads them at the frame's
    blend: exactly the current value at blend 1, for a row that is new this step, and while the scene is frozen.
  - *The state hash.* Components added with `AddState<T>` and `AddTag<T>`, links, and each system's name, switch and
    own state. Per row and independent of row order; nothing drawing writes is in it.
  - *Transforms.* `SyncTransform<T>` writes a linked row's component to its entity's transform when the step ends
    (or reads it when the step starts, per link), so hierarchy, physics, `SpatialIndex2D` and drawing written for
    the scene world keep working for linked objects.
  - *Registration.* Every component type is declared once in an `EcsComponents`. Declaring names it for the library
    (what NativeAOT needs), and a row with an undeclared component fails the step in every build with a message
    that names the type, so the mistake is found in the first debug run instead of the first NativeAOT build.
- **No events, relations, serialization or parallel walks in the package.** The slice needed none. Arch offers the
  first two as separate packages a game can add; each would be a decision of its own.
- **Samples.** `EcsSwarm` moves to the package and keeps its behaviour (captures identical to the hand-wired
  version). `EcsEffects` shows modelling: the status-effects slice as a playable scene. The comparison stays in
  `benchmarks/EcsModel` with its test.

## Why Arch

Arch and Friflo.Engine.ECS are the two maintained, stable, permissively licensed, managed archetype stores
(ENTITY-1 section 3, read again on 2026-11-19). Friflo is the richer one for modelling: relations, events and
systems are built in. Arch is the one that publishes trimmed and with NativeAOT without a warning (measured again
with the package: both samples, and a WebAssembly build that runs in a browser), while Friflo reports 13 warnings
and its JSON dependency 19. The package is marked AOT-compatible and this repository builds with warnings as
errors; adopting Friflo would fail every trimmed game that references the package or suppress warnings on the
developer's behalf. Arch's costs are recorded in the evaluation: its command buffer allocates when played back, its
walk order is not the order of creation, it has six dependencies (one a preview), and its last release is from May
2025. If Friflo publishes without warnings, or Arch stops being maintained, this choice is looked at again; the
package is about 900 lines with its documentation comments, and what would change is its implementation and the library types games see.

## Alternatives considered

- **Keep ADR 0059: guide and sample only.** It leaves each game writing and testing the same integration. It was
  the right answer while the engine had nowhere to put a store; it is not after ADR 0060.
- **A first-party store** (the 220-line prototype of ENTITY-1, grown). It publishes without registration and would
  give the engine control of row order and of garbage, but what a game uses an ECS for is everything the prototype
  lacks, and two maintained libraries have it.
- **Hide the library behind engine types** (`EcsEntity`, an engine query type). A second API to learn over a
  documented one, always a version behind and narrower than the library.
- **A small contract in the engine that any store implements.** Queries and components are the library's, so the
  contract would cover only what `SceneSystem` already is. A game that wants another library writes a scene system;
  the package is the example.
- **Make the scene world component-based.** Unchanged from ADR 0059: it would change the default model for every
  game to serve the ones that choose otherwise.

## Consequences

- A game that models with components and systems references one package and writes components, systems and rules;
  the engine steps, hashes, blends and ends its store.
- The SDK ships an eleventh package and, with it, a third-party dependency whose types are public. A release of the
  SDK that includes it lists Arch in `THIRD-PARTY-NOTICES.md`; games that do not reference the package carry
  nothing.
- The state hash of `EcsSwarm` changed (it is now the store's: by row and by component type name, and it includes
  each member's pull, which the scene left out). No other game or sample changed its hash.
- Games that wired a store by hand keep working: the scene hooks are unchanged, and nothing was removed.
- Two models are documented. The guide chooses between them by what the game models, not by a count of objects.
- ADR 0059's reopening conditions 5 and 6 are answered here by a constructed slice, not yet by a large game. The
  substantial demo game (NastMz/MicroEngine#114) is still the check: if its mechanics read better on the scene
  world throughout, or the package's systems, links or commands prove awkward at that size, this record is revised
  with what it finds.

## Not done here, and needed for a release

- `sdk/version.mjs` lists the SDK's package ids for publishing (`packageIds`, `shippedWithout`):
  `MicroEngine.Ecs` is not in it yet.
- `site/scripts/api-reference.mjs` lists the assemblies of the generated API reference: `MicroEngine.Ecs` is not in
  it yet, so `docs/api` has no page for the package.
- `samples/Directory.Build.targets` swaps the SDK's packages for the source projects inside the repository; the two
  samples carry that swap for this package in a `Directory.Build.targets` of their own until it moves there.

## Verification

`tests/MicroEngine.Ecs.Tests` (27), `tests/Benchmarks.Tests/EcsModelTests.cs` (29), `tests/Samples.Tests`
(`EcsSwarmTests` 10, `EcsEffectsTests` 9), `ArchitectureTests`. NativeAOT and trimmed publishes of both samples and
a browser run of `EcsEffects`, as the evaluation's section 10 records. Not verified: phones, macOS, Linux, and any
cost in time (section 11).

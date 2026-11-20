# ENTITY-1, reopened: an Entity Component System as a way to model a game, and an optional package for it

Evaluation of 2026-11-19, on `main` at 7023b7d, for NastMz/MicroEngine#151 (reopened). It follows the method of
[CONSOLIDATION-1](consolidation-1.md), section 1. Decision record: [ADR 0065](../decisions/0065-optional-ecs-package.md),
which supersedes [ADR 0059](../decisions/0059-no-first-party-ecs.md).

[ENTITY-1](entity-1.md) measured a component store as a place to keep many alike objects and stands as measured:
nothing here repeats or revises its figures. It said what it did not evaluate: modelling, composition at run time,
the coordination of systems, reuse of behaviour across kinds of object, and how the integration reads in a game.
This document is about those. It is a separate file because it answers a different question with a different kind
of evidence (code written twice and compared, not timings), and the first one is a measurement record that should
stay readable as one.

Machine: the reference desktop of ENTITY-1 (Ryzen 5 7600X, Windows 11, .NET SDK 10.0.400). **No time was measured
for this evaluation**: other work was building on the machine, and no decision below rests on a frame time.

## 1. The need

Some games are easier to write as components and systems than as objects of classes: games where what an object
*is* changes while it lives (a status effect, a carried item, a possession), where several kinds of object share
some behaviours and not others, and where the game keeps asking for "everything that has this and that but not
that". The question was never whether MicroEngine's scene world can express them (it can), but what each model costs
as such a game grows, and whether a developer who chooses components and systems has to build the integration with
the engine alone.

## 2. The modelling comparison

`benchmarks/EcsModel` is one gameplay slice written twice and grown in three steps, each step kept as its own source
file so the change between steps can be read and counted:

1. enemies that move and bounce, and projectiles that hit the nearest one;
2. status effects on live enemies: **burning** (a point of damage every 20 steps for 120, and at each of those the
   fire reaches everything within a radius), **frozen** (does not move; the next hit shatters it for twice the damage)
   and **shielded** (absorbs the next hit), with their interactions (fire on a frozen enemy removes the ice and does
   not catch; ice on a burning one puts the fire out; a shield comes before both);
3. a **crate** that can burn and be damaged but neither moves, freezes nor takes a shield, and a question asked
   late: every burning thing near a point that is not shielded.

`Simple/` is the engine's default model, written as the demos write actors: a `SceneWorld`, one class per kind of
object attached to an entity, the entity's transform as its place. `Ecs/` is Arch 2.1.0 used directly, as its
documentation shows. One script (600 updates, 24 volleys) drives both; `tests/Benchmarks.Tests/EcsModelTests.cs`
asserts that the two show the same things in the same places with the same health and effects after **every**
update of every step, and checks each rule on both versions separately, so they cannot agree by being wrong together
(29 tests). `dotnet run -c Release --project benchmarks/EcsModel` prints the comparison and the line counts below
(lines of code: no blanks, comments or lone braces; changes by longest common subsequence against the step before).

| | Scene world, attachments | Component store (Arch) |
| --- | --- | --- |
| Step 1 | 70 lines | 68 lines |
| Step 2: effects on live enemies | 115 lines; **+49 −4** | 126 lines; **+66 −8** |
| Step 3: a crate, and the late question | 134 lines; **+56 −37** | 139 lines; **+20 −7** |

What the code shows, in both directions:

**Step 1: no difference worth a decision.** The same size. The store needs one thing the world does not: rows
cannot be destroyed during a walk, so they go through a command buffer (three lines). The world's version reads
slightly more directly (`target.Health -= shot.Damage`).

**Step 2: the world is shorter and plainer.** An effect is a counter in the enemy's class; the rule for a hit is an
`if` chain over fields, written where the hit happens. The store pays for the same rule in three ways:

- *Structure cannot change during a walk.* A hit adds and removes components, so hits are collected during the walk
  and settled after it (a list, a sort and a loop: about ten lines the world's version does not have), and effects
  that ended must be flushed between two walks of the same behaviour or the second walk still sees them.
- *The order of a walk is not the order of creation.* Arch walks a chunk from its last row to its first with a
  delegate and fills a removed row's place with the last one (pinned by a test). Two projectiles reaching a shielded
  enemy in the same step must hit in the order they were fired, so the store's version carries a firing number and
  sorts; without the sort the rule's test fails on the store and passes on the world, whose attachments are walked
  in attach order. A game that relies on order has to say so in data.
- *Access is noisier.* `world.Has<Frozen>(target)`, `world.Get<Health>(target).Value` and `world.Remove<Frozen>(target)`
  where the other version reads `target.FrozenLeft > 0`: eighteen such calls in step 2.

What the store gives back in step 2 is one thing: "a frozen enemy does not move" is `WithNone<Frozen>()` on the
movement query, with no test inside the loop. It also showed a trap: movement and bouncing were two walks, and both
had to leave frozen rows out; nothing but the reader connects them.

**Step 3: the store absorbs the change; the world's version is rewritten.** On the store a crate is a row without a
velocity. Three empty components say what a kind can have (`Freezes`, `Shieldable`) or is (`Crate`), one condition
is added to the hit rule, one to the shield command, one to the death count, and the late question is a query
description and five lines. **No walk changed**: movement, hits, burning and spreading are the lines of step 2.
On the world the enemy's class had assumed it was the only thing that burns and is hit. The shared part moved to a
base class, an enemy is now attached twice (attachments are found by the type they were attached under, so
`All<Thing>()` does not see an `Enemy`), most methods changed the type they work on, and nine places ask what kind
of thing they hold (`as Enemy`, `is Enemy { Shielded: true }`). 37 of 115 lines were removed or rewritten. The late
question is a walk over every thing with a test per condition: correct, and linear in everything rather than in
what matches.

**The next change would repeat step 3 on the world.** A base class works for one shared set. A third kind that
moves and freezes but does not burn does not fit under `Thing`, and the usual answer is to split the class into
several attachments (`Burnable`, `Freezable`) found with `TryGet`, which is composition by hand: the world can do
it, at the price of the same rewrite again and a lookup per capability per object.

**State machines do not cover this.** `StateMachine<TState, TEvent>` (ADR 0061) models states of which one holds at
a time. Burning, frozen and shielded hold together and interact pairwise; as one machine they are a product of
states. A machine per enemy for its behaviour and effects as components or counters are complementary.

| | Scene world | Component store |
| --- | --- | --- |
| A new effect on one kind of object | fields and an `if` where it matters: cheapest | a component, deferred changes, more calls |
| A behaviour an effect switches off | a test in each loop it affects | left out of the query |
| A new kind sharing some behaviours | base class or split attachments; most methods touched (−37 lines) | a different set of components; no walk touched (−7) |
| "Everything with A and B but not C" | walk everything, test fields | describe the rows; only they are walked |
| Order between behaviours | the order of calls in one method | the same, plus where deferred changes are applied |
| Order within a behaviour | attach order, stable | not promised; sort when it matters |
| Changing an object while walking | allowed | not allowed: collect, then change |
| Reading the rule for a hit | plainest | the same rule through `Has`, `Get`, `Add`, `Remove` |

**Finding.** Neither model is better outright. The world is the better default: for a game whose kinds of object
are few and fixed it is shorter, plainer and ordered. Components and systems win once kinds share behaviours
partially or capabilities come and go, and they win by a margin that grows with each such change. That is a property
of what the game models, not of how many objects it has: the slice has 13 enemies.

## 3. What a developer writes today to use a store

ADR 0059 answered this with a sample, `samples/EcsSwarm`, and called the result "glue of five places that reads
better in the game". Read again, with `benchmarks/EcsBench/ArchScene.cs` and `FrifloScene.cs` beside it, the same
code is written in each of them and none of it is about a swarm:

| Integration | In the sample (hand-wired) | What goes wrong |
| --- | --- | --- |
| The store's lifetime | `World.Create()` in `Enter`, `World.Destroy` in `Exit` | a forgotten `Exit` leaks the library's world, which it lists globally |
| NativeAOT registration | a static constructor naming each component | a forgotten one throws only in a NativeAOT build, at the first row |
| Row to entity and back | a component with the `EntityId`, an attachment class with the row and `IDestroyedCallback`, `RemoveRow`, `Forget` (about 25 lines) | destroying one side leaves the other; a stale handle reaches the row that reused its place |
| Interpolation | a second position component, initialised at every creation and copied in the move loop; `Vector2.Lerp` with `Objects.Blend` in `Draw` | a creation that forgets it draws a streak from the origin; the wrong blend under an overlay |
| The state hash | a walk over the hashed components, in row order (13 lines) | a component added later and not hashed; a hash that depends on row order |
| Deferred changes | two lists, applied after the walks | changing rows during a walk |
| The order of behaviours | the order of loops in one `Update` | nothing names it |
| Transforms of linked rows | a line in the carrier loop | written before the row moved |

The earlier audit counted 85 to 95 of about 150 integration lines as generic. ADR 0059's sentence was true when it
was written for one reason: the engine had no place to put them. Scene systems (ADR 0060) are that place.

## 4. Libraries

Versions and licences read on 2026-11-19 from nuget.org (`api.nuget.org/v3-flatcontainer/<id>/index.json`, each
`.nuspec`) and the GitHub API. Feature statements are each project's documentation unless marked measured.

| | Arch | Friflo.Engine.ECS |
| --- | --- | --- |
| Latest stable | 2.1.0 (2025-05-27); no prerelease since | 3.6.0 (2026-03-23); 4.0.0-preview.2 published |
| Licence | Apache-2.0 | MIT |
| Repository | 1,883 stars, pushed 2026-11-09, 46 open issues | 818 stars, pushed 2026-10-31, 46 open issues |
| Dependencies | six, one of them a preview (`Collections.Pooled` 2.0.0-preview.27) | one (`Friflo.Json.Fliox`) |
| Queries | descriptions with all, any, none; chunk spans or delegates | typed queries with filters; chunk spans or delegates |
| Deferred changes | `CommandBuffer` (measured: it allocates when played back, about 560 bytes for one command and 84 bytes per command at a hundred) | `CommandBuffer` (not measured here) |
| Relations, events, hierarchy | separate packages (`Arch.Relationships`, `Arch.EventBus`) or a compile-time switch not in the published build | built in: relations, component and entity events, a tree of entities |
| Systems | a separate package with a source generator (`Arch.System`) | built in (`SystemRoot`, groups) |
| Trimming and NativeAOT (measured, ENTITY-1 section 6) | 0 warnings; component types must be named for NativeAOT | 13 warnings and 19 in its dependency; a schema must be created for NativeAOT |
| With the package (measured today) | trimmed and NativeAOT publishes of both samples: 0 warnings, their tours play, captures identical to the JIT's; a WebAssembly package runs in a browser | not built |

Fennecs (0.7.3, pre-1.0), Frent (8.0.0-beta), DefaultEcs (last release 2022, not AOT), Flecs.NET (native library,
prerelease) and Svelto remain as ENTITY-1 section 3 describes them; none became a candidate.

**Arch is adopted.** The deciding fact is publishing: the package is marked AOT-compatible and the repository builds
with warnings as errors, and Friflo.Engine.ECS's 32 trimming and AOT warnings would either fail every trimmed game
that references the package or have to be suppressed for the developer, hiding real ones. Friflo is the richer
library for modelling (relations and events are built in, and the module would not need its own system list), so
this is a choice of what can ship today, not a verdict on the libraries. What Arch costs: its command buffer
allocates, its walk order needs care (section 2), it has a preview dependency, and its release cadence is slow
(eighteen months since 2.1.0, with an active repository): stability now, a risk of stagnation later. Friflo's risk
is the opposite: a 4.0 in preview.

## 5. The target experience

```csharp
public static readonly EcsComponents Components = new EcsComponents()
    .AddState<Position>().AddState<Velocity>().AddState<Burning>().AddTag<Frozen>()
    .Interpolate<Position>(static (in Position a, in Position b, float t) => new Position { Value = Vector2.Lerp(a.Value, b.Value, t) })
    .SyncTransform<Position>(static (in Position p, ref Transform local) => local.Position2D = p.Value);

public override void Enter()
{
    var ecs = this.AddEcs(Components);            // created now, destroyed with the scene
    ecs.Systems.Add(new Movement());              // before the scene's Update, in this order
    ecs.Systems.Add(new Effects());
    ecs.LateSystems.Add(new Deaths());            // after it
    var boss = ecs.World.Create(new Position { Value = start }, new Velocity());   // Arch's own world
    ecs.Link(boss, Objects.Create());             // its place moves the entity; either side destroys both
    _places = ecs.Interpolated<Position>();
}

public override void Draw(IRenderer r)
{
    foreach (ref var chunk in Ecs.World.Query(in Drawn))
    {
        var positions = chunk.GetSpan<Position>();
        for (int i = 0; i < chunk.Count; i++) r.Draw(sprite, _places.Blended(chunk.Entity(i), in positions[i]).Value);
    }
}
```

No `Exit`, no `HashState` for the rows, no second position, no attachment class, no registration call.

## 6. Where the responsibilities end

- **The library** owns rows, components, queries, the command buffer and their semantics. Its types are public in
  the package's API (`EcsStore.World` is `Arch.Core.World`); the package wraps none of them.
- **The package** owns what joins a store to the engine: lifetime with the scene; a list of systems before the
  scene's update and one after, run in the order they were added, each with a switch; applying the command buffer
  after each system and after the scene's update; links between rows and scene entities (found from either side,
  destroyed together, stale handles refused, optional per row); the value of marked components at the start of the
  step and their blended reading; the state hash of the components marked as state or tags, of links and of systems;
  transforms written to linked entities (or read from them); and a check that every component of every row was
  declared, which also names the types for NativeAOT.
- **The game** owns its components, its systems, what it draws and every rule.
- **Not in the package**: events between systems (Arch's are a separate package, and the slice needed none: its
  systems pass work through components and lists), relations, serialization of a store, parallel walks, a system
  scheduler with dependencies. None was needed to write the slice; each is a decision of its own if a game shows
  the need.

A system is a small class of the package (`EcsSystem`: `Update`, `Enabled`, the store) or a function, not the
library's `Arch.System` type: that type knows nothing of a scene's step, has no switch, and comes with a source
generator and another package to pin. A game that prefers it runs its group from one `EcsSystem`.

**Replacing the library.** The package is not a contract others implement: a store for another library is another
`SceneSystem`, written with the same hooks (`Start`, `Update`, `LateUpdate`, `BeginDraw`, `HashState`, `Stop`), and
`src/MicroEngine.Ecs` (about 900 lines with its documentation comments) is the worked example. An interface over "a store" was considered and left
out: queries and components are the library's, so an abstraction over them would either hide what an ECS is used
for or be implemented once.

## 7. Integration with the engine

No engine assembly changed. The package references `MicroEngine` and Arch; nothing references the package but
samples and tests. The architecture test (`ArchitectureTests`) allows exactly one engine project to name an ECS
library and exactly that library, refuses a reference to the package from any engine project, tool, template or
demo, and still fails if any other engine project names a library.

Found while writing it:

- Inside a namespace under `MicroEngine`, `World` alone resolves to the engine's `MicroEngine.World` namespace, not
  to Arch's type. Game code in its own namespace is unaffected; the package and its tests alias the type.
- A test outside the engine cannot draw a frame at a chosen blend (`Game.Render` and the setter of `FrameBlend` are
  internal, and a scripted run always draws at blend 1), so the package's interpolation tests call its own internal
  hook with the blend. The engine's call into it is covered by a run through the real loop.

## 8. Trade-offs

- **A dependency of an SDK package on a third-party library whose types are public.** The package is pinned to
  Arch `[2.1.0]`; a new Arch version is a change of the package, and a breaking one is a breaking change of the SDK
  for games that use it. Games that do not reference the package carry nothing.
- **Garbage from the command buffer.** A game that changes many rows every step keeps them in a list and changes
  them after its walk, inside the system, which is safe because the store runs one system at a time. `EcsSwarm` does
  that and allocates nothing per step; `EcsEffects` uses the commands for the few changes a step has.
- **The hash does not follow row order** (a sum of one hash per row, by row id), so it is the same after a row left
  an archetype and came back. It does depend on the library's row ids, which are the same for the same sequence of
  calls.
- **Two models to document.** The guide says which fits what, by what the game models.
- **Snapshots cost a copy per marked row per step** (20,000 positions in the swarm; the sample did the same copy by
  hand).

## 9. Decision

**Extend.** `MicroEngine.Ecs` is added as an optional package. The scene world stays the SDK's default model, the
model of every template and demo, and the right choice for most games. Detail and alternatives: ADR 0065.

## 10. Validation

- `tests/MicroEngine.Ecs.Tests` (27): the store's lifetime with the scene; one store per scene from one line; the
  order of systems, their switch and removal; commands applied after the system that asked and after the scene's
  update; links from both sides, destruction from the world's side, from the library's own `Destroy` and through
  the commands, stale handles on both sides, refused links; transforms in both directions; interpolation at blend
  0, 0.5 and 1, for a row new this step, for a row that reused an id within the step, for a component that came
  back, and for a frozen scene; the hash equal for equal stores, different for a state value, a tag, a link and a
  switch, unchanged by a component that is not state and by drawing; a component that was not declared fails the
  step and names itself; a scene under a blocking overlay does not step its store; no allocation per step once warm.
- `samples/EcsSwarm` rewritten on the package: its ten tests pass, and the captures of its tour in a hidden window
  are byte for byte those of the hand-wired version built from 7023b7d.
- `samples/EcsEffects`: the slice of section 2 as a playable scene (nine tests; its captures were looked at).
- NativeAOT (`PublishAot`) and trimmed self-contained publishes of both samples on Windows x64: no warning, the
  tours play to the end, captures identical to the JIT build's. The NativeAOT link needed the reduced process
  `PATH` that [AOT-1](aot-1.md) documents for this machine.
- `node sdk/tests/docs.mjs`: both samples, copied outside the repository, build against the packages this commit
  packs (`MicroEngine.Ecs` among them) and play their tours. Against nuget.org they do not build, as expected for a
  package that is not published (a report, like the Tiled and navigation samples).
- Browser: `microengine package web` of `EcsEffects` builds (with the same reduced `PATH`; with the account's
  `PATH` the build does not find `emcc`), and the package served from a local folder runs in a Chromium browser:
  the arena draws, a bolt is fired and absorbed, the next one sets its target alight, with nothing on the console.

## 11. Not verified

- **No time was measured**: whether the package costs anything over the hand-wired sample was not benchmarked (the
  machine was in use). The work per step is the same walks plus one copy per interpolated row, which the sample
  already did by hand; that is a reading of the code, not a measurement.
- **Phones, macOS, Linux, ARM**: not built or run with the package.
- **A browser other than the one above, and the swarm sample in a browser**: not run.
- **Friflo.Engine.ECS's features** are its documentation's; only its publishing behaviour was measured (ENTITY-1).
- **A game larger than the slice.** The comparison is three steps of one mechanic. The substantial demo game
  (NastMz/MicroEngine#114) is where the package and the guide's advice meet real complexity; ADR 0065 says what
  would revise the decision.
- **`sdk/tests/packages.mjs` and the release path.** The package was packed and its dependencies, licence, README
  and files inspected by hand; `packages.mjs` has its entry but was not run against a full feed, and the package is
  not yet in `sdk/version.mjs` nor in the generated API reference (ADR 0065, "Not done here").

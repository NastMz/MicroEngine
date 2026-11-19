# 0060 — Scene systems and module lifecycle hooks

Status: Accepted (2026-11-19). The SDK is in Alpha: new public types carry no compatibility commitment. Evidence:
[CONSOLIDATION-1](../evaluations/consolidation-1.md), section 2.1. Issue NastMz/MicroEngine#173.

## Context

A module could register services, frame systems, capabilities and settings, and subscribe to five window events.
Everything after that was fixed code: a scene ticked its coroutines, tweens, world and widgets in a fixed order,
hashed a closed list and tore down a closed list; the game's state hash and shutdown were closed lists too; and nothing
outside a scene class was told when a scene went onto or off the stack.

So anything with state that belongs to a scene had to be driven by the game. `samples/EcsSwarm` creates its component
store in `Enter`, steps it in `Update`, hashes it in `HashState` and destroys it in `Exit`; a game registers and
removes its physics world by hand; a scheduler was written inside a demo. ADR 0059 judged an ECS integration to be
"glue of five places that reads better in the game", which was true only because the engine offered no place to put
it. State machines, a component store, physics per scene and localization all need the same thing.

## Decision

- **Scene systems.** `SceneSystem` is a part of a scene with `Start`, `Update`, `LateUpdate`, `BeginDraw`,
  `HashState` and `Stop`. `Scene.Systems` holds one per type, in `Order` and then in the order they were added. The
  scene starts them before `Enter`, steps them after its coroutines, tweens and widgets and before `Update`, steps
  them again after `Update` and before the world ends its step, adds them to its state hash before its own
  `HashState`, and stops them in reverse order after `Exit`.
- **A system for every scene.** `SceneManager.AddSystem` takes a factory called for each pushed scene;
  `SceneManager.Entered` and `Exited` report the stack. This is how a module lives in scenes it does not know.
- **Module lifecycle.** `IEngineLifecycle.Started` and `Stopping`; `EngineBuilder.StateSections` for a module's part
  of the state hash. Achievements use it instead of a special case in `Game`.
- **Timers.** `Scene.Timers` (`SceneTimers`) is itself a scene system: `After`, `Every`, `Cancel`, on the scene's
  step, with an unscaled option.

The built-in coroutines, tweens, world and widgets stay fixed members with their fixed place in a step. A scene
without systems steps and hashes exactly as before.

## Alternatives

- **Make the built-ins the first entries of the same list.** Uniform, but it changes nothing a developer can do today
  and puts the order of a step, which existing games and their recorded hashes depend on, behind a mutable list.
  Rejected for now; the list is the extension point, the built-ins are its fixed prelude.
- **Frame systems only (`builder.Schedule`).** They exist and are right for work that belongs to the game. They know
  nothing of scenes: a system there cannot be frozen with a scene under an overlay, follow its world's clock or end
  with it, which is exactly the glue being removed.
- **Events on the scene (`Updating`, `Hashing`, `Exiting`).** Less to learn, but ordering between subscribers is
  implicit and a subscriber cannot be found by type from the scene.
- **Leave it to the game.** The state before this record.

## Consequences

- An optional package can ship a scene system and a one-line registration; FSM-1, ENTITY-1, PHYS-2 and LOC-1 build on
  this.
- `IEngineLifecycle` gains two members: an implementation of the interface outside the engine must add them.
- The state hash of a scene with systems contains their count, type names and state. A scene with none is unchanged,
  and `engine.achievements` keeps its name, content and place.
- A system that prepares drawing gets `BeginDraw` only; drawing itself stays in `Scene.Draw`, where the scene decides
  the order.
- Not done here: physics worlds owned by a scene through this mechanism (PHYS-2), and a Tiled, Navigation or Box2D
  composition entry, which they do not need until they hold per-scene state.

## Verification

`tests/MicroEngine.Tests/SceneSystemTests.cs` (16 tests: order, late addition, removal, a frozen scene, the hash with
and without systems, the manager's factory and events, a failing stop, lifecycle and sections, timers on scaled and
unscaled time). `samples/Examples` has a system and a timer in its tour.

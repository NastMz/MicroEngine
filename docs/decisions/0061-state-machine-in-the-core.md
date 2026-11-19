# 0061 — A gameplay state machine in the core; event workflows use a library directly

Status: Accepted (2026-11-19). The SDK is in Alpha: a new public type carries no compatibility commitment. Evidence:
[FSM-1](../evaluations/fsm-1.md). Issue NastMz/MicroEngine#171. Amends [ADR 0055](0055-animation-tracks-and-timelines.md),
which listed state machines among game rules.

## Context

The SDK had no state machine, and said so in its tutorial. The four demos hold eight, each the same field, timer,
reset method and `switch` written again, none with an exit, several with the entry bypassed or copied inline. ADR 0055
and ANIM-1 had ruled the capability out as "game rules" and "a second way to write `switch`": the rules are the
game's, but the state, its time, its entries and exits and the order of its ways out are machinery that every game
repeats.

Mature libraries exist. Stateless (Apache-2.0, no dependency, a net10.0 target, active) and
Appccelerate.StateMachine (Apache-2.0, netstandard2.0) are event machines: a state changes when the caller fires a
trigger. Gameplay machines are mostly polled every step, and neither library has a step, a time in the state or a
timed way out. Written on Stateless, the comparison enemy keeps its `switch` to turn conditions into triggers, gains
a second vocabulary of six triggers, and allocates when a trigger changes the state.

## Decision

- `StateMachine<TState>` and `StateMachine<TState, TEvent>` in the kernel assembly, namespace `MicroEngine`, with no
  dependency: entries, updates and exits; ways out by condition (`When`), by time (`After`) and by event (`On`,
  `Do`), tried in declaration order, then the parents', then `Any`'s; substates; requests made from a handler applied
  after the change in progress; `Time`; the state hash; `Snapshot` and `Restore`.
- `Scene.Machines`, a scene system, steps and hashes a scene's own machines. An object's machine is stepped by the
  object.
- No adapter package over a third-party machine. The guide names Stateless for event workflows (quests, dialogue,
  menu flows) and shows how it sits in a game: referenced directly, fired from the scene, one line in the hash.

## Alternatives

- **An official adapter over Stateless.** It would add exactly what this machine is (step, time, timed ways out)
  around a core whose trigger vocabulary and per-change allocation do not suit a machine per enemy, or hide the
  library behind a façade and lose what it is chosen for.
- **Guide only.** Leaves every game writing the machinery, which is the state this record changes.
- **An optional package for the machine.** It has no dependency and is about 400 lines; a package would cost a
  reference in every game for something a scene and an actor use as readily as a tween.
- **Behaviour trees, utility AI, a visual editor, a file format.** Not evaluated here and not ruled out; nothing in
  this record depends on them.

## Consequences

- A game declares its states where they are and stops carrying timers, reset methods and forgotten exits.
- One more type in the kernel to keep; it does less than Stateless on purpose.
- The demos' eight machines move to it in DEMO-MODERN-1 (#182).
- The tutorial and ADR 0055's sentence are corrected: the engine has a state machine; which animation a state plays
  is still the game's rule.

## Verification

`StateMachineTests` (16), `FsmCompareTests` (the same enemy by hand, on Stateless 5.20.1 and on this machine, equal at
each of 2,400 steps), the example in the tour. Not verified: browser, phone and NativeAOT builds.

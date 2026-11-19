# FSM-1: gameplay state machines

Evaluation of 2026-11-19 for NastMz/MicroEngine#171, part of
[CONSOLIDATION-1](consolidation-1.md). Decision: [ADR 0061](../decisions/0061-state-machine-in-the-core.md).

## A. Need

A platformer hero (idle, run, jump, dash, attack with interrupts), an enemy (patrol, chase, attack, stunned, dead), a
boss with phases, a turn-based battle, the flow of a level. Every genre has them, and several per game.

## B. What a developer writes today

The SDK has no machine. The four demos hold eight of them, all the same shape written again: a state field, a time
in the state, a method that resets it, a `switch`, and two hash lines.

| Where | What it shows |
| --- | --- |
| `games/CourtyardKey/Entities/Boss.cs:59-63, 71-134` | nine states, one `Enter`, a 60-line `switch` |
| `games/CourtyardKey/Entities/Boss.cs:159-160` | `Enter(Dazed)` then `_stateTime = 0.9f`: the entry is patched from outside |
| `games/MeadowRun/Gameplay/Actors/Boss.cs:154-155, 184-185, 194-195, 314` | twelve modes; `_mode` and `_timer` assigned in three places besides `Next` |
| `games/MeadowRun/Gameplay/Stage.cs:195-204, 289-312` | `Phase = X; _phaseTime = 0;` repeated inline |
| `games/MeadowRun/Gameplay/Player.cs:42-58` | no state at all: eight flags and timers |

None has an exit: what a state must undo is remembered by whoever leaves it. The tutorial says "MicroEngine has no
state-machine type", ADR 0055 says state machines "are game rules", and ANIM-1 called one "a second way to write
`switch`". Those statements confuse the rules, which are the game's, with the machinery that runs them.

## C. Existing libraries

| | Stateless 5.20.1 | Appccelerate.StateMachine 6.0.0 |
| --- | --- | --- |
| Licence | Apache-2.0 | Apache-2.0 |
| Targets | net462, netstandard2.0, net8.0, net9.0, net10.0 | netstandard2.0 |
| Dependencies | none | none |
| Activity | pushed 2026-11-14, about 6,300 stars | last push 2025-03-28, about 550 stars |
| Model | triggers fired by the caller; guards, substates, entry and exit, parameterised triggers, async, graph export | events, hierarchy, history states, persistence, async, extensions |

Read from nuget.org and the GitHub API on 2026-11-19. Automatonymous (the third name usually given) depends on
GreenPipes and has had no release since 2021; it was not taken further.

Both are event machines: a state changes when the caller fires a trigger. Gameplay machines are mostly polled: the
ways out are conditions checked every step ("closer than 80", "0.4 s in this state", "the animation ended"). Neither
library has a step, a time in the state or a timed way out.

## D. The same enemy three ways

`benchmarks/FsmCompare` writes one enemy by hand, on Stateless and on the engine's `StateMachine`, and
`tests/Benchmarks.Tests/FsmCompareTests.cs` checks that the three show the same state, time, hitbox and attack count
at each of 2,400 scripted steps. What the game owns in each:

| | By hand | Stateless | Engine machine |
| --- | --- | --- | --- |
| Time in the state | a field, reset by hand | a field, reset in a transition hook | the machine's |
| Conditions checked every step | the `switch` | the same `switch`, firing triggers | `When` on the state |
| Timed ways out | a comparison in the `switch` | a comparison in the `switch`, a `TimeUp` trigger | `After` on the state |
| Exit of a state | remembered in `Enter` | `OnExit` | `OnExit` |
| A second vocabulary | no | six triggers besides the five states | two events |
| State hash | two lines | two lines | one line |

On Stateless the `switch` stays, because something must turn this step's conditions into a trigger; the library adds
entries, exits and a table of permitted changes around it. That is real value for an event workflow and little for a
polled one.

One measurement, because it decides whether a machine per enemy is reasonable on a phone or in a browser: on the
development machine (Ryzen 5 7600X, .NET 10, Release, one run, one machine stepped a million times after warm-up) the
hand-written and the engine versions allocate 0 bytes per step and Stateless 77 bytes per step on average, all of it
when a trigger changes the state. Step times were 7, 14 and 126 ns; with one run on one machine they are an order of
magnitude, not a ranking, and none of them is a reason for anything here.

## E. Responsibilities

The engine: the state, the time in it, entries and exits, the order in which ways out are tried, events that arrive
during a change, hierarchy, the state hash, a snapshot for saves. The game: the states, the conditions, what each
state does, and when the machine is stepped.

## F. Integration

- The machine takes the step it is given, so it follows a scene's clock (slow motion, hitstop) with no hook.
- `IStateHashed`, so `hash.Add(machine)`; `Snapshot` and `Restore` for saves, two plain values that any serializer
  writes, with nothing to register for trimming or NativeAOT.
- `Scene.Machines` (a scene system, [ADR 0060](../decisions/0060-scene-systems-and-module-lifecycle.md)) steps and
  hashes the scene's own machines.
- No reference from graphics, audio, input or physics: a state's handlers call them.

## G. Trade-offs

- **An adapter over Stateless** would have to add the step, the time and the timed ways out, and keep the trigger
  vocabulary, the `switch` and the allocation. What it added would be this machine; what it kept would be the parts
  that do not fit. A façade over it would hide the features Stateless is chosen for.
- **A machine of the engine's own** is about 400 lines with no dependency, in the kernel. It is one more thing to
  maintain and it does less than Stateless: no parameterised triggers, no async, no export.
- **A guide only** leaves the eight machines as they are.

## H. Decision

Add `StateMachine<TState>` and `StateMachine<TState, TEvent>` to the core package; document Stateless as the library
for event workflows, used directly. No adapter package.

## I. Validation

- `tests/MicroEngine.Tests/Kernel/StateMachineTests.cs`, 16 tests: entry at the first step, order of ways out, timed
  ways out to the step, a frozen clock, guards and `Any`, unhandled events, hierarchy and its borders, a parent's
  time, requests from handlers, re-entry, an update refused from a handler, snapshot and restore, the hash, no
  allocation, and a scene's machines following its clock.
- `FsmCompareTests`: the three enemies agree step by step, and the script visits every state.
- `samples/Examples`, "A guard (state machine)", in the tour.

Not verified: a browser or phone build; a NativeAOT publish (the machine uses no reflection, generics over enums and
delegates only). The demos' machines are migrated in DEMO-MODERN-1 (#182), with their captures and hashes compared;
one finding from reading them: the boss that enters `Dazed` with 0.9 s already spent wants a duration of its own, not
a patched timer.

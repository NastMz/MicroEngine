# GAPS-TIME: time a game can adopt, and the children of a group

Objective: close groups A (time) and E (the scene world) of NastMz/MicroEngine#218, so the demos' hand-written tween
loops, scheduler, float timers and room enumerator have an engine piece to move onto. Branch `feat/gaps-time`; part
of [CONSOLIDATION-1](consolidation-1.md). Evaluation: [`docs/evaluations/gaps-time.md`](../../docs/evaluations/gaps-time.md);
decision: [ADR 0074](../../docs/decisions/0074-time-a-game-can-adopt.md). Test runner:
`dotnet test MicroEngine.slnx -c Release`.

## Specs

Quoted fragments are the issue's text, verbatim (L1).

- S1 "A tween cannot be yielded from a coroutine and has no handle; a cutscene is written as ease loops (24 in Trail
  Duel; fades in Courtyard Key)."
- S2 "`Tweens.Add` first applies a value one update after it is added, a loop on its starting step." The engine's
  behaviour is decided, documented and tested (the brief's wording).
- S3 "`SceneTimers`, machines and systems live and step only inside a `Scene`: a game whose rules are a plain testable
  object writes its own scheduler (Neon Rift's `Stage.Schedule`)."
- S4 "`StateMachine.After` and `Timers.After` measure a double clock and leave at `>=`; a float timer accumulated at
  1/60 and compared with `>` leaves one update later for many durations (Meadow Run: 1.9, 2.0, 2.4, 4.2 s; Neon Rift:
  10 of 16 thresholds; Courtyard Key: 9 of the Warden's 12). The engine's is the exact one, and no page says so, nor
  how to move a game with recorded traces onto it."
- S5 "A machine has no way to set the time already spent in a state."
- S6 "No typed, allocation-free enumeration of a parent's children of a type (`RoomActors<T>`, 55 lines in Courtyard
  Key): `IWorld` has `Children(parent)` and `All<T>()` but not the two together."
- S7 Proof with the demos' numbers without editing the demos (the brief's wording).

## Tasks

Route: inline, in this worktree. Status: `[x]` done with its evidence.

| ID | Task | Specs | Status |
| --- | --- | --- | --- |
| T1 | `IWorld.Children<T>(parent)` | S6 | [x] `bfe6a398`, `WorldRulesTests` |
| T2 | Machines: the rule for seconds at any rate, `AfterSteps`, `Steps`, `SetTime`, `SetSteps`, steps in the snapshot | S4, S5, S7 | [x] `54cbf249`, `MachineTimeTests` |
| T3 | `Timers` in the kernel, `SceneTimers` over it, seconds in double precision, steps | S3, S4, S7 | [x] `0a092c5f`, `TimersTests` |
| T4 | `Tween` handle, yield, starting value, scene order, `scaled`, `Bind`, `AddSteps`, `Wait.Steps`; the example | S1, S2, S7 | [x] `TweenTests`, `CutsceneExampleTests` |
| T5 | Evaluation, decision, release notes, migrations, limits, API pages | all | [x] |

## Log

- L1 (the issue, groups A and E, verbatim): "### A. Time: tweens, timers and machines a game can adopt (GAPS-TIME)
  - A tween cannot be yielded from a coroutine and has no handle; a cutscene is written as ease loops (24 in Trail
  Duel; fades in Courtyard Key). `Tweens.Add` first applies a value one update after it is added, a loop on its
  starting step. - `SceneTimers`, machines and systems live and step only inside a `Scene`: a game whose rules are a
  plain testable object writes its own scheduler (Neon Rift's `Stage.Schedule`). - `StateMachine.After` and
  `Timers.After` measure a double clock and leave at `>=`; a float timer accumulated at 1/60 and compared with `>`
  leaves one update later for many durations (Meadow Run: 1.9, 2.0, 2.4, 4.2 s; Neon Rift: 10 of 16 thresholds;
  Courtyard Key: 9 of the Warden's 12). The engine's is the exact one, and no page says so, nor how to move a game
  with recorded traces onto it. A machine has no way to set the time already spent in a state. ### E. The scene world
  (GAPS-WORLD) - No typed, allocation-free enumeration of a parent's children of a type (`RoomActors<T>`, 55 lines in
  Courtyard Key): `IWorld` has `Children(parent)` and `All<T>()` but not the two together."
- L2 (2026-11-24) Measured before designing: updates until 47 durations are reached at 30, 50, 60, 120 and 144 updates
  a second, six ways (the table of the evaluation). Found: `SceneTimers` counted down in `float` and was late for 4 s
  at 144, 10 s at 120, 60 s at 60; the machine's fixed tolerance was late for 300 s at 50; `Wait.Seconds` has no
  tolerance (1 s: 61 updates). "The engine's is the exact one" held for machines under a few minutes only.
- L3 Decision on tweens: the starting value is applied in `Add`; the scene advances tweens before coroutines. With the
  old order a coroutine saw a finished tween one update late, and a tween added from a coroutine advanced in the
  update it was added. Tweens keep `float` accumulation because it is the loop's arithmetic: the yielded tween and
  Trail Duel's loop apply the same bits on the same updates (27 cases at three rates).
- L4 A repeating timer first failed its ten-minute test with a per-interval tolerance: the error of the summed steps
  grows with the total time, not with the interval. It now counts the time since it was added against the time of its
  next call.
- L5 Not changed, for the owner: `Wait.Seconds` and tween lengths stay in single precision. Making them follow the
  rule changes what the four demos record (35 waits, 2 tweens) and makes a tween differ from the loop it replaces.
- L6 (2026-11-26) Whole solution, before the example was added: every test project passed except one test of
  `MicroEngine.Tests`, `GraphicsCorrectionTests.A_strength_or_brightness_without_limit_is_clamped_and_the_effect_ends`,
  which hit its 10 s limit while other builds ran and passes alone in 68 ms; it touches nothing of this work. The four
  demos' tests passed unchanged (79, 73, 73, 134).
- L7 (2026-11-27) Correction batch after an independent verification, one commit, each test seen failing first
  (`TimeCorrectionTests`). D1: a tween callback that cleared the tweens threw; `Clear` now marks, the update sweeps.
  D2: the tolerance `5e-7 x time` reached a whole update on long times at high rates; timers and machines now count
  updates of a step taken as `1 / R` (`StepClock`), within `min(1e-5 s, step / 100)`. Measured after the fix: 0 updates
  off for 1800, 3600, 7200, 10800, 21600, 43200 and 86400 s at 60, 144, 240, 360 and 1000 updates a second on timers
  (60, 144 and 1000 on machines), and `Every(1f)` on every R-th update for 24 h at 60, 144 and 1000. D3 and D5: a step
  that is not a positive finite number advances nothing; a tween refuses a length that is not a finite number of zero
  or more; a repeating timer is called at most 1000 times in one update. D4: one rule for zero (nothing happens by
  time on an update of zero seconds; a zero length is the next update with time). D6: a timer handle carries its
  owner.
- L8 Counting the step as `1 / R` first changed `StateMachine.Time` too (420 updates at 60 gave exactly 7), and Trail
  Duel's credits, which leave their card on `_part.Time > 7f`, left one update later (`CreditsFlowTests`, 420 against
  421). `Time` is again the plain sum of the steps as given, what it always reported; only the ways out by time
  (`After`) are measured on the counted clock. A machine restored from a snapshot takes the saved time as the saved
  number of updates when the two agree to a float's precision.

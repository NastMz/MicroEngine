# GAPS-TIME: tweens, timers and machines a game can adopt, and the children of a group

Evaluation of 2026-11-26 for NastMz/MicroEngine#218, groups A (time) and E (the scene world). It follows the method
of [CONSOLIDATION-1](consolidation-1.md), section 1. Decision record:
[ADR 0074](../decisions/0074-time-a-game-can-adopt.md). The cases are the demos' own code and the numbers of their
records (`odd/tasks/demo-modern-1/*.md`); no demo was edited.

No time was measured as performance. Every number below is a count of updates, produced by running the arithmetic.

## 1. The need (A) and what a developer writes today (B)

| Gap | Who | Written by hand today |
| --- | --- | --- |
| A coroutine cannot wait for a tween; `Tweens.Add` returns nothing | Trail Duel (`BattleScene.Tween`, 24 uses; `FadeOut`, `FadeIn`, `ShowBubble`), Courtyard Key (fades) | `for (t = 0; t < seconds; t += EngineStep) { apply(ease(t / seconds)); yield return null; } apply(1);` |
| A tween first applies a value on the update after it is added | any game that slides or fades something in | set the starting value by hand before `Add` |
| Timers and scene systems step only inside a `Scene` | Neon Rift (`Stage.Schedule`, 12 lines), Meadow Run (`Stage`, boss) | a list of times and actions, its loop and its hash |
| Moving a float timer to `After` moves the event by one update for many durations, and no page says which is regular | Meadow Run (4 of 9 phases), Neon Rift (10 of 16 thresholds), Courtyard Key (9 of the Warden's 12) | nothing: the three games kept their timers |
| A machine's time in a state cannot be set | Meadow Run's tally (`_phaseTime = 0.5f` while it counts) | a timer beside the machine |
| No typed walk of a parent's children | Courtyard Key (`RoomActors<T>`, 55 lines) | an enumerator over `Children` and `TryGet` |

All confirmed in the code on `main` at a4cbe289: `Tweener.Add` returned `void` and applied nothing until its first
`Update`; `SceneTimers.Update` was protected and read `Scene.EngineStep`; `StateMachine` was already usable without a
scene (public `Update`, `IStateHashed`), which no page said; `Snapshot`/`Restore` already put a machine back
mid-state with its time.

### What the arithmetic does

Updates until a duration is reached, at five rates, for 47 durations from 0.02 s to an hour (the table of
`MachineTimeTests`), six ways:

| Way | Where | Result |
| --- | --- | --- |
| `t += dt` in `float`, `t > seconds` | the demos' timers | at 60: one more than `seconds x 60` for most durations from 0.75 s up (1.9 s: 115), equal for others (0.5 s: 30); hundreds of updates off after an hour |
| `t += dt` in `float`, `t >= seconds` | a hand-written ease loop, `Tweener` | the same pattern (0.9 s: 55) |
| `float` countdown, `<= 1e-5` | `SceneTimers` before this work | regular up to a few seconds; late for 4 s at 144, 10 s at 120, 60 s at 60, 83 s at 50 |
| `float` countdown, `> 0` | `Wait.Seconds` | as the float sum (1 s: 61; 0.1 s: 7) |
| `double` clock, `>= seconds - 1e-5` | `StateMachine` before this work | regular except long times: 300 s at 50 (15,001), 600 s and 3600 s at 50 |
| `double` clock, `>= seconds - max(1e-5, 5e-7 x seconds)` | timers and machines, first version | `ceil(seconds x R)` in all 235 cases, but early on long times at high rates: 3600 s at 1000 updates a second, 43200 s at 60 |
| updates counted, step taken as `1 / R`, within `min(1e-5, step / 100)` | timers and machines now | `ceil(seconds x R)` from 0.02 s to an hour at eight rates (30 to 1000), and from 1800 s to 24 h at 60, 144, 240, 360 and 1000; `Every(1f)` on every R-th update for 24 h at 60, 144 and 1000 |

So "the engine's arithmetic is the exact one" was true of machines for short times only, and not of timers. The
recorded numbers of the three demos (25 thresholds) are reproduced by the first row exactly.

## 2. Alternatives (C)

- **The engine's own pieces**, extended: the gaps are a handle, a rule and an owner, not a missing subsystem.
- **Tween libraries** (DOTween and its kind are Unity's; general .NET ones exist): not evaluated as packages. They
  bring their own update loop and time, which a deterministic scripted run would have to own anyway; their handle
  (complete, kill, link to an object) is the shape adopted here.
- **A float clock option** to reproduce hand-kept timers: declined, see the decision record.
- **`System.Threading.Timer`, `PeriodicTimer`**: wall-clock and threaded; not for a simulation step.

## 3. Target experience (D)

Written before the API, from the demos' code:

```csharp
// Trail Duel, BattleScene.Intro: the helper and its 24 call sites keep their shape, the helper goes away.
yield return Tweens.Add(0.9f, t => _introT = t, Ease.OutCubic);

// Neon Rift, Stage: a field instead of a scheduler.
private readonly Timers _timers = new();
_timers.After(0.9f, () => Sound("alarm", 0.7f));          // or AfterSteps(n, ...) to keep a recorded update
public void Update(float dt, PlayerInput input) { _timers.Update(dt); ... }
hash.Add(_timers);

// Meadow Run, Stage: phases on a machine, in the updates its runs were recorded with.
phase.State(StagePhase.Dying).AfterSteps(145, StagePhase.Done);
phase.State(StagePhase.Tally)
    .OnUpdate(_ => { if (Counting()) phase.SetTime(0.5f); })
    .After(2f, StagePhase.Done);

// Courtyard Key, PlayScene.
foreach (var actor in Objects.Children<Actor>(_room)) actor.Step(dt);
```

## 4. Responsibility (E) and integration (F)

- The engine owns counting, order and the hash; the game owns what happens. A timer's action and a tween's `apply`
  are code, so neither is saved: `Remaining` and `MachineSnapshot` give an owner what it needs.
- Scene order: world step begins, tweens, coroutines, systems (timers, machines), `Update`. Tweens and coroutines keep
  the engine's step; `scaled: true` puts a tween on the world's; timers and machines follow the world's unless
  `unscaled`.
- State hash: timers (count, elapsed, due, interval, calls left, unit, clock), tweens (length, time, delay, started,
  unit, clock), coroutines (depth, seconds and updates left, waiting for a condition or a tween), machines (state,
  time and steps per level). Nothing reads the machine it runs on.
- Lengths in updates never count an update of zero seconds, so a frozen world holds them as it holds seconds.

## 5. Trade-offs (G)

- Two arithmetics for seconds remain: exact on timers and machines, the loop's on `Wait.Seconds` and tweens. Chosen
  because the tween must replace the loop exactly (it does, bit for bit) and because changing `Wait.Seconds` moves
  every cutscene of the demos. Cost: a tween of 0.9 s ends one update after a timer of 0.9 s.
- A length in updates is not a length in seconds at another rate, and slow motion does not stretch it. Stated on
  every member that takes one.
- A tween added from a coroutine now advances one update later than before, and `Add` calls `apply` at once.
- `Timers` is also the name of the property `Scene.Timers`; inside a scene `Timers.After` is the property and
  `new Timers()` the class. It compiles and reads as intended; a different class name would have been safer.

## 6. Decision (H)

**Extend.** Tweener, timers, machines and the world keep their models; each gains what kept a game from adopting it.
Not done, with reasons:

- `Wait.Seconds` and tweens on the exact rule: the owner's call (it changes what the demos record).
- Scene systems for plain objects: a system is a scene's lifecycle (start, stop, draw); a rule object needs a timer
  and a machine, which it now has.
- A coroutine wait on the world's clock: no demo or sample asked; reported as a limit.
- A typed filter on `Children<T>`, a snapshot of timers, a float clock: see the decision record.
- Courtyard Key's heartbeat and ambience (`t -= dt; if (t <= 0) t = period`) drop the remainder on purpose; that is
  `EverySteps` with the count the game measures (11 updates for 0.18 s), not a new policy.

## 7. Validation (I)

| Claim | Test |
| --- | --- |
| `seconds x R` on machines and timers, 5 rates x 47 durations; late in a long run; repeating for ten minutes | `MachineTimeTests`, `TimersTests` |
| Meadow Run 115/121/145/253 and 18/30/36, Neon Rift's and Courtyard Key's thresholds, kept through `AfterSteps` | `MachineTimeTests.The_demos_hand_kept_timers...` |
| Neon Rift's schedule (its own code, 21 delays, six clock positions) kept through `Timers.AfterSteps`; in seconds it differs by at most one update | `TimersTests.Neon_rifts_schedule...` |
| Trail Duel's loop and a yielded tween apply the same bits on the same updates and go on in the same update (27 cases, three rates) | `TweenTests.A_yielded_tween_replaces...` |
| Starting value, order in a scene, cancel, complete, bind, steps, scaled | `TweenTests` |
| Set time, steps, snapshot mid-state | `MachineTimeTests` |
| Typed children: order, rules of the walk, no allocation | `WorldRulesTests` |
| The example | `CutsceneExampleTests` |

## 8. Not verified

- No demo was moved onto these pieces: that the 24 call sites of Trail Duel leave its captures unchanged is shown for
  the values the tween applies, not for the game.
- Courtyard Key's fade lengths other than 0.45 s were not read from its code; the lengths 0.6, 1.2 and 2 s in the
  tween test are further cases, not that game's.
- Variable update steps (a game without a fixed step): the rule is stated and tested for fixed rates only.
- Neon Rift calls actions due on the same update in reverse order of scheduling; `Timers` calls them in the order
  added. A game moving over checks the pairs that fall together.

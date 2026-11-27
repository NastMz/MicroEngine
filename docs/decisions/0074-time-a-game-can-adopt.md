# 0074 — Time a game can adopt: yieldable tweens, timers outside a scene, one rule for seconds, lengths in updates

Status: Accepted (2026-11-26). The SDK is in Alpha: the changes of behaviour are listed in the migration notes.
Evidence: [GAPS-TIME](../evaluations/gaps-time.md). Issue NastMz/MicroEngine#218, groups A and E. Extends
[ADR 0060](0060-scene-systems-and-module-lifecycle.md) (scene timers) and [ADR 0061](0061-state-machine-in-the-core.md) (state machines).

## Context

The four demos were moved onto the capabilities of the consolidation and kept their own time code: 24 ease loops in
Trail Duel and the fades of Courtyard Key, because a coroutine could not wait for a tween; a scheduler in Neon Rift,
because timers lived only in a scene; float timers in three games, because moving them to `After` moved events by one
update and nothing said which of the two was regular; a 55-line enumerator in Courtyard Key for "the actors of this
room".

Measured for this decision (60 updates a second unless said):

- A float timer (`t += dt; if (t > seconds)`) and the machine's clock disagree on most durations of 0.75 s and more;
  the machine's count is `seconds x 60`. The demos' recorded numbers reproduce exactly.
- The machine's fixed tolerance (1e-5 s) was too small for long times: 300 s at 50 updates a second left one update
  late. Scene timers counted down in single precision and were late for 4 s at 144, 10 s at 120, 60 s at 60, and
  drifted by hundreds of updates over an hour as repeating timers.
- `Wait.Seconds` and tweens count in single precision with no tolerance: `Wait.Seconds(1)` lasts 61 updates, a tween
  of 0.9 s lasts 55. That arithmetic is the hand-written loop's, bit for bit.

## Decision

- **A tween is a handle.** `Tweener.Add` returns a `Tween` (`IsFinished`, `Progress`, `Cancel`, `Complete`,
  `Bind(world, entity)`), and a coroutine yields it to wait. No second method, no `Wait.Tween`: the value a game
  already gets is the thing it waits for.
- **A tween applies `ease(0)` when it is added** (a delayed one, on the update its delay ends), and advances from the
  next update. The scene advances tweens before coroutines, so the rule does not depend on where the tween was added
  and a waiting coroutine goes on in the update of the final value.
- **A tween in seconds keeps single-precision accumulation**, the arithmetic of the loop it replaces, so the
  replacement is exact; `AddSteps` gives a length in updates. `scaled: true` puts a tween on the world's clock; the
  default stays the engine's.
- **`Timers` is a kernel class** (namespace `MicroEngine`) with a public `Update`, hashed through `IStateHashed`;
  `SceneTimers` is a scene system that owns one and steps it with the world's step and the engine's. State machines
  already needed no scene; the documentation now says so.
- **Seconds follow one rule on timers and machines.** A step that is exactly the `float` of `1 / R` for a whole R is
  counted as `1 / R`, and the clock is a count of updates times that step while the step stays the same (`StepClock`);
  a time is taken as written to seven digits; the n-th call of a repeating timer is due at `n x interval` from its
  start. A time is reached within `min(1e-5 s, a hundredth of the step)` plus the rounding of the arithmetic. A first
  version used a tolerance of `max(1e-5, 5e-7 x time)` over a running sum: it reached a whole update at about
  `2e6 / R` seconds (one update early at 3600 s and 1000 updates a second, at 43200 s and 60), which an independent
  verification measured; the tolerance may never grow with the time. The rule ("`seconds x R` updates, rounded up")
  is stated in the manual and tested from 0.02 s to an hour at eight rates from 30 to 1000, and from half an hour to
  24 hours at 60, 144, 240, 360 and 1000 updates a second.
- **One rule for zero and for steps that are no time**, on timers, machines and tweens: an update of zero seconds
  makes nothing happen by time; a negative, infinite or not-a-number step is zero; a zero length happens on the next
  update that has time. A repeating timer is called at most 1000 times in one update and drops the rest. A timer
  handle carries its owner.
- **Every time can be given in updates**: `StateMachine.AfterSteps` (with `Steps`, `StepsIn`, `SetSteps`),
  `Timers.AfterSteps` and `EverySteps`, `Wait.Steps`, `Tweener.AddSteps`. An update of zero seconds is not counted. It
  is the way a game that thinks in frames writes its rules and the way a game with recorded runs moves its float
  timers over without changing an update.
- **`StateMachine.SetTime`** sets the time spent in the current state; `MachineSnapshot` carries the steps.
- **`IWorld.Children<T>(parent)`** walks the attachments of a type among an entity's direct children, with the rules
  of `Children(parent)` and no allocation.

## Alternatives

- **Make `Wait.Seconds` and tweens follow the exact rule too.** One rule everywhere, and the better design for an
  engine with no games on it. Not taken now: it moves every cutscene of the four demos by an update per wait (35
  waits, 2 tweens) and makes the tween differ from the loop it is meant to replace, which is what kept Trail Duel off
  it. The manual states both arithmetics; this is the open question of the record.
- **A float clock or a `>` comparison as an option of machines and timers**, to reproduce hand-kept timers. Declined:
  what a float sum does depends on the rate and on where the clock is, so no option reproduces it for a second game;
  steps reproduce any of them exactly.
- **`Tweens.Run`/`Wait.Tween` beside a `void Add`.** Two ways to start the same tween.
- **Apply nothing until the first update** (the previous behaviour), documented. It leaves a frame in which a thing
  that slides or fades in is drawn where it was.
- **A snapshot of timers.** A timer's action is code; a save holds what the owner needs to add it again
  (`Remaining`). A sequence that must be saved as data is a `Timeline`.
- **A typed filter on the walk (`Children<Actor>(room).OfType<Door>()`).** Attachments are keyed by the type they are
  attached under everywhere in the world; a kind among them is an `is` in the loop.
- **A tween library as a package, or sequences in the manner of DOTween.** Not evaluated as packages: what was
  missing was a handle and a stated rule on the tweener the engine has, and the engine's coroutines already are the
  sequence. The shape of the handle (complete, cancel, tie to an object) follows what those libraries offer.

## Consequences

- The Trail Duel loop is `yield return Tweens.Add(seconds, apply, ease)`, identical value for value; Neon Rift's
  scheduler is a `Timers` field; float timers move to `AfterSteps` with the count measured once.
- Behaviour changes for existing games: the starting value at `Add`; tweens added from coroutines advance one update
  later; `done` order; long timers and machine times end on the regular update; `TimerHandle` changed namespace; state
  hashes of scenes with machines, coroutines, tweens or timers change (no demo test stores a hash).
- Two arithmetics remain for seconds: exact on timers and machines, the loop's on `Wait.Seconds` and tweens.

## Verification

`MachineTimeTests`, `TimersTests`, `TweenTests`, `WorldRulesTests` (typed children) in `tests/MicroEngine.Tests`;
`CutsceneExampleTests` in `tests/Samples.Tests`. The demos' numbers are reproduced without editing them: the 25
recorded thresholds of Meadow Run, Neon Rift and Courtyard Key through `AfterSteps`; Neon Rift's 21 delays from six
clock positions through `Timers.AfterSteps`; the 23 fixed lengths and easings of Trail Duel's battle tweens and four
further lengths (Courtyard Key's 0.45 s fade among them) through a yielded tween, compared bit by bit on every update
at 60, 50 and 144 updates a second.

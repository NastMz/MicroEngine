# DEMO-MODERN-1: Meadow Run on the current SDK

Objective: replace Meadow Run's private infrastructure with what the engine provides, without changing anything a
player sees or a script records (issue #182, T14 of [`consolidation-1.md`](../consolidation-1.md)). Branch
`feat/demo-meadow`, base `824fcf44`. Test runner: `dotnet test tests/MeadowRun.Tests -c Release`.

## Decisions

| Private infrastructure | Where | Decision |
| --- | --- | --- |
| `saveDirectory` parameter, `CreateSaves` override, `MEADOW_SAVE_DIR`, a temporary folder per test | `MeadowGame.cs`, `Program.cs`, six test files | **Replaced** with the run's own store, `--saves` and `InitializeForTest(saves:)` (ADR 0073) |
| `(int)(time * rate) % count` at 17 drawing sites | actors, scenes, `StageView` | **Replaced** with `SheetClip`s added to the atlas beside their frames (`SpriteAtlasBuilder.AddClip`) and `AnimationClip.FrameAt` |
| The same expression with a start frame per instance (fire bars, torches, the title's coins), Pip's run, the Mole King's poses, the question blocks and lava | `Props.cs`, `TitleScene.cs`, `StageView.cs`, `Boss.cs` | **Kept**, see gap 9: a clip has no frame offset; the run follows distance; the boss picks one index for two strips |
| Pip's mover over `TileMotion` (`Player.Step`) | `Gameplay/Player.cs` | **Kept**: the controller is not identical. Gaps 1 to 8, measured below |
| Enemy and item movement over `TileMotion.Move` | `Actors/Actor.cs` | **Kept**: the stateless helper is the documented way for a game that moves its own boxes |
| Moving and falling platforms as `IRideable` | `Player.cs`, `Props.cs` | **Kept**: the landing rule (2.5 px after, 0.5 px before, 1 px in from the ends) is the game's; see gap 5 |
| Stage phases (`StagePhase`, `_phaseTime`, a `switch`), the Mole King's modes, enemy timers | `Stage.cs`, `Boss.cs`, `Enemies.cs` | **Kept**, gap 10: `StateMachine` keeps time in double and leaves with `>=`; four of the stage's nine phase times would end one step earlier |
| Hand-kept timers of the simulation (coyote, bumps, popups, throw pose) | `Stage*.cs`, actors | **Kept**: `Stage` is a simulation that tests run without a scene; `Scene.Timers` belong to a scene and call actions, these are hashed values the rules compare |
| Menus: `DemoKit.OptionList` with the game's own drawing | `TitleScene`, `PauseScene`, `GameOverScene`, `Ui.Menu` | **Kept**, gap 11: widgets draw in the UI view at the window's resolution; these menus are on the 320 x 180 canvas |
| Fixed drawing bands for actors (`LayerOf`, four passes) | `StageView.cs` | **Kept**: a fixed order between tile passes, nothing is sorted by position; `SortedRenderer` would record and sort the same draws for the same picture |
| Save versions | `MeadowGame.cs` | Already `SaveSchema<SaveData>(version: 1)` |
| Particles, camera, tweens, transitions, prompts, achievements, light map, synthesized audio, music fades | | Already the engine's |
| `LaunchOptions` and the `MEADOW_START`... variables | `MeadowGame.cs` | **Kept**: the game's developer shortcuts, read through the host |
| `DrawTime.Of(scene)`, `NumberLabel` | `Scenes/` | **Kept**: three lines each; reported as small gaps 12 and 13 |
| Strings in code | scenes | **Kept**: string tables are content files, and the game ships none |

## Lines of game code

| Area | Before | After |
| --- | --- | --- |
| `games/MeadowRun/**/*.cs` | 6992 | 7026 |
| of which saves (`MeadowGame.cs`, `Program.cs`) | 229 | 225 |
| of which animation (the clip table in `Assets.cs` +44, seventeen sites -10) | | +34 |
| `tests/MeadowRun.Tests` without the differential | 1681 | 1652 |
| The differential (`ControllerShadow.cs`, `ControllerDifferentialTests.cs`) | 0 | 510 |

The game did not get shorter: the rates moved from the drawing code into one table, which costs lines.

## Can Pip move with `CharacterController2D`? No, not identically

`tests/MeadowRun.Tests/ControllerShadow.cs` drives the controller beside `Player.Step` with the same input in a tile
physics world of its own (the level's grid as tile geometry, a one-way box per platform, a box per hidden block with
`Solid`, a body whose origin is Pip's top-left corner). Positions, velocities and the grounded flag are compared
exactly every step. Two mappings: **two records** (walk and run, as `samples/PhysicsPlayground/ClassicScene.cs`),
and **per state** (a record chosen each step: ground, braking above the cap, air, air with momentum; and the
vertical speed set to 0 after a hidden block), which is everything the public API lets a game do.

Levels, closed loop (the autopilot plays, the controller is put back on Pip when it leaves him), 94,285 steps in 16
attempts over the five levels:

| Mapping | Steps diverged | With the controller on its own for the half second before | Attempts with a divergence |
| --- | --- | --- | --- |
| Two records | 1921 | 62 | 16 of 16 |
| Per state | 66 | 46 | 7 of 16 |

First divergence, per state: level 1 at step 272 (landing: the game is grounded with speed 0, the controller still
falls at 196.96 px/s, same position); levels 2 and 3 at steps 121 and 115 (a jump at run speed with run released:
-315.57 px/s against -317.33). Two records: step 1 on every level (air acceleration 7.17 against 8 px/s per step).

Rules, open loop (one small stage per rule, the controller left alone), 62 runs:

| Rule | Two records | Per state |
| --- | --- | --- |
| Stand, walk, run, friction, skid, standing jump held, ledge fall, coyote jump | identical | identical |
| 33 approaches to a block's corner (5 nudges by each mover) | identical | identical |
| Run, then walk on | first at step 91, 0.55 px for good | identical |
| Running jump, run released in the air | step 1, 51.4 px at landing | identical |
| Running jump, direction released in the air | step 1, 56.8 px at landing | identical |
| Running jump, turned in the air | step 1, 11.0 px | identical |
| Hidden block | step 14, 10.9 px | identical |
| Walking jump | step 1, 7.02 px higher | step 61 (the jump), 7.02 px higher at the top |
| Standing jump tapped (landing) | step 30, 0.70 px, 4 steps | the same |
| Jump pressed just before landing | step 35, 4.90 px | the same |
| Drop through a plank | step 13, 2.81 px, 18 steps | the same |
| Buffered drop | step 35, 37.1 px (it jumps instead) | the same |
| Jump on a platform moving sideways | step 31, 0.87 px, then 66.8 px: Pip loses the life, the controller lands | the same |
| Platform moving up and down | step 7, 0.17 px; 1.19 px while jumping on it | the same |
| Total identical | 45 of 62 | 52 of 62 |

Beyond the step comparison, a migration also needs what gaps 7 and 8 name.

## Engine gaps

Most important first. Each is what a developer making a classic platform game has to write today.

1. **Landing reach.** Pip is on the ground when a floor is within 1 px under him and he is not rising
   (`TileMotion.IsOnGround`), the controller only on contact (0.01). The game lands one step earlier, 30 steps into a
   tapped jump. Missing: a distance in `CharacterMotion` within which a falling character counts as landed.
2. **`RunJumpBonus` is measured against the current `MoveSpeed`.** The game measures it against the run speed while
   walking: `26 * min(1, |vx| / 152)`. At walking speed the controller gives -317.33 px/s where the game gives
   -307.07, a jump 7 px higher. Missing: the speed the bonus is full at, as its own number.
3. **Air has one share for two numbers.** `AirControl` scales `Acceleration` and `Deceleration` alike; the game has
   air acceleration 430 and air drag 140 against 520 to 600 and 620 on the ground. Missing: air acceleration and air
   deceleration. A game can swap the record when the character leaves the ground, which is the game keeping the rule.
4. **Air momentum and braking above the cap.** In the air the game never slows Pip toward a lower cap while he
   pushes the way he moves; on the ground above the cap it brakes with friction, not acceleration. The controller
   approaches `MoveSpeed` with `Acceleration` in both. Missing: a flag for the first, and the deceleration used above
   the cap. Emulated per state by building a record per air step (an allocation every step).
5. **Order of carry, jump and drop.** The controller carries a rider before it reads the jump, so a jump off a moving
   platform takes that step's movement with it (0.87 px, and a different landing); the game drops the platform when
   the jump starts. On the step of a drop the controller picks the gravity phase from the ground it had (17.5 px/s
   gained against 6.33). Missing: both orders as the classic games have them, or a hook between input and move.
6. **A drop needs the jump's buffer.** The game drops with down held and a jump pressed or still buffered; `Drop` is
   a level, and the controller's buffer cannot be read. Missing: the buffered jump exposed, or a drop that takes it.
7. **A launch that can be cut.** `Player.Launch(speed, cuttable)` bounces off an enemy higher while jump is held and
   cuts the rise when it is released; `CharacterController2D.Launch` always clears `IsJumping`. Missing: a parameter.
8. **State that cannot be copied or set.** The autopilot of the level tests plans jumps on `Player.Clone()`; a
   controller is bound to a body in a world. The stage also writes Pip's position (goal pole, stomp, checkpoint),
   and `Teleport` clears the ground. Missing: a snapshot and restore, as `StateMachine` has.
9. **A clip has no start frame.** `(int)(time * 12 + i) % 2` gives each link of a fire bar its own frame;
   `FrameAt(time + i / 12f)` is not the same number at a frame boundary. Missing: `FrameAt(time, frameOffset)`. And
   `AddFrames(name, frames, origin)` has no rate, so a strip built in code needs its frame names listed to be a clip.
10. **`StateMachine` cannot take over a float timer.** It adds steps in double and leaves at `>= seconds`; the stage
    adds in float and leaves at `> seconds`. At 60 Hz the phases of 1.9, 2.0, 2.4 and 4.2 s end at steps 114, 120,
    144 and 252 instead of 115, 121, 145 and 253 (0.3, 0.5 and 0.6 s agree). The tally also puts its time back to
    0.5 s, which `Time` does not allow. Missing: a way to set the time in a state; the first is a choice to document.
11. **Widgets only in the UI view.** A pixel-art game whose menus are part of its canvas cannot use `Scene.Widgets`
    for them without drawing them at the window's resolution.
12. **The time a frame is drawn at** (`Age - (1 - Objects.Blend) * EngineStep`) is written by the game (`DrawTime`).
13. **A number drawn as text** without formatting it every frame is written by the game (`NumberLabel`).

## Not in the edit surface, to follow up

- `packaging/verify.mjs`, `packaging/linux-distros.mjs`, `web/tests/desktop.mjs`, `media/tools/capture-footage.mjs`
  and two scripts under `experiments/arch-1` still set `MEADOW_SAVE_DIR`. The game ignores it now; their runs have a
  script or `--frames`, so they keep their saves to themselves. They can drop it or pass `--saves`.
- `docs/reference/configuration.md` lists `MEADOW_SAVE_DIR`; `docs/examples/testing.md` and
  `docs/manual/backends.md` quote `new MeadowGame(dir)` and `new MeadowGame(TempDir(), ...)` from the tests.
- `games/Shared/OptionList.cs` stays in use.

## Log

- L1 (2026-11-23): baseline of the nine scripts (44 captures and 9 state traces) recorded twice before any change:
  53 of 53 files identical between the two runs.
- L2: the differential was expected to fail at the collision layer and did not: with the body's origin at Pip's
  top-left corner the tile world's sweep and `TileMotion` gave the same positions in every compared step, corner
  nudges included. What differs is in the controller's rules and their order.
- L3: on a platform that moves sideways, a jump straight up costs Pip a life in the game (the platform leaves from
  under him) and not with the controller. The game's behaviour is what scripts and players have; it stays.
- L4: putting the shadow back on Pip leaves the controller's ground body and timers as they were, which produced
  runs of false divergences on platforms; the report counts separately the ones that follow half a second of the
  controller on its own.
- L5 (2026-11-24): after each of the two refactors, 79 of 79 tests and 53 of 53 files identical to the baseline.
  Not every clip is in a capture (pennant, moths, thornbacks); they compute the expression they replaced.
- L6 (2026-11-27): the answer of "Can Pip move with `CharacterController2D`?" has changed since this record.
  GAPS-CHARACTER (issue #218, [`../gaps-character.md`](../gaps-character.md), ADR 0075) added what gaps 1 to 8 name,
  and the same differential now compares the controller with one motion against `Player.Step` in position,
  velocity, ground, jump state, timers and events: 0 of 94,285 steps apart (about 6,500 different situations) and 62
  of 62 rules identical, with the game unchanged. Pip still moves with his own mover; moving him is a later pass.

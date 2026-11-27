# GAPS-CHARACTER: a character controller a classic platformer can use

Objective: make `CharacterController2D` able to move Meadow Run's Pip exactly, with one motion and without editing
the game, and close what Neon Rift and Courtyard Key found in the controllers and the contract. Issue
NastMz/MicroEngine#218, group B. Branch `feat/gaps-character`, base `a4cbe289`; nothing is pushed from this record's
work. Test runner: `dotnet test MicroEngine.slnx -c Release`. Evaluation:
[`docs/evaluations/gaps-character.md`](../../docs/evaluations/gaps-character.md). Decision:
[ADR 0075](../../docs/decisions/0075-character-feel-in-one-motion.md).

## Specs

Quoted fragments are the issue's text, verbatim (L1).

- S1 "a landing reach (land within a distance of the floor, not only on contact)". Acceptance: a fall that ends a
  step within the reach has landed in that step; it stays grounded while it closes the gap.
- S2 "a reference speed for `RunJumpBonus` (it is normalised by the current `MoveSpeed`)". Acceptance: the same
  sideways speed gives the same jump whether run is held or not.
- S3 "separate air acceleration and air drag".
- S4 "air momentum and friction above the cap".
- S5 "the order of carry, jump and drop on one step".
- S6 "a buffered drop".
- S7 "a `Launch` that can be cut (stomp bounces)".
- S8 "a snapshot and restore of the controller's state (`Teleport` clears ground, so a camera clamp or a planner
  cannot place it)".
- S9 "Neon Rift adds: a released jump that cuts upward speed at once (a factor), not only by gravity scale."
- S10 "`MoveResult` says what was touched, not which side stopped the body this step".
- S11 "`Scene.Physics` writes the box centre to the transform (actors positioned at their feet convert by hand)".
- S12 "`AddTiles` takes one grid (walkers and flyers add the tiles twice in two layers)".
- S13 (the task's statement) The differential of `tests/MeadowRun.Tests` "reports zero diverging steps using only the
  controller's public API, with a mapping a developer could plausibly write". `games/MeadowRun` is not edited.
- S14 (the task's statement) "with every new parameter at its default the controller's behaviour is bit-identical to
  what it is on `main` on BOTH backends"; "everything that affects the future is in the controller's state hash; NaN
  and negative parameters are refused; corner correction never ends inside a solid".
- S15 (the task's statement) Why `TopDownController2D` and `TileMotion.Move` differ in the last bits, "and whether
  exact agreement is reachable".
- S16 (the task's statement) The classic scene of `samples/PhysicsPlayground` on the new API, and the manual's
  character section "rewritten around \"here is how to get the feel of a classic platformer\"".

## Tasks

| Task | Specs | Route | State | Commit |
| --- | --- | --- | --- | --- |
| T1 The controller's rules, the contact's `Stopped`, the shared tests, Meadow Run's differential as a proof | S1-S10, S13, S14 | inline | done | f6cd2dd7 |
| T2 Views of a grid, the origin of a character, agreement with `TileMotion` measured and pinned | S11, S12, S15 | inline | done | 2671d33b |
| T3 The playground's classic room | S16 | inline | done | e51a48e1 |
| T4 Manual, release notes, limits, ADR 0075, evaluation, API reference | S16 | inline | done | (docs commit) |

## Log

- L1 (the issue, group B, verbatim): "Meadow Run's mover against `CharacterController2D`, same input, 94,285 steps:
  66 diverge with the best mapping the public API allows. Missing, in order: a landing reach (land within a distance
  of the floor, not only on contact); a reference speed for `RunJumpBonus` (it is normalised by the current
  `MoveSpeed`); separate air acceleration and air drag; air momentum and friction above the cap; the order of carry,
  jump and drop on one step; a buffered drop; a `Launch` that can be cut (stomp bounces); a snapshot and restore of
  the controller's state (`Teleport` clears ground, so a camera clamp or a planner cannot place it). Neon Rift adds:
  a released jump that cuts upward speed at once (a factor), not only by gravity scale. Courtyard Key adds:
  `MoveResult` says what was touched, not which side stopped the body this step; `Scene.Physics` writes the box
  centre to the transform (actors positioned at their feet convert by hand); `AddTiles` takes one grid (walkers and
  flyers add the tiles twice in two layers)."
- L2 Baseline on `a4cbe289`, reproduced before any change: per state 66 of 94,285 steps (46 after half a second on
  its own), 52 of 62 rules; two records 1921 steps, 45 of 62 rules.
- L3 Red: the shadow was rewritten onto the new API while the API had no behaviour (parameters ignored, `StandOn`
  and `Restore` empty): 94,285 of 94,285 steps diverged and 39 of 62 rules were identical. With the rules written:
  2 steps and 61 rules. The 2 steps were a defect of the first writing: the release cut applied with the default
  share to any rise faster than `JumpSpeed` (a stomp bounce at 330). The 1 rule was the stage, not the mover: the
  open loop compared a step in which the stage ended the play. After both: 0 steps, 62 rules.
- L4 The mapping: one `CharacterMotion` (`ControllerShadow.Pip`); the input passed as it is, with drop being down
  while not on a platform; two rules after the step. The hidden block sets the vertical speed to 0 (the game's
  reaction; other ceilings rebound). The platforms are bodies Pip does not collide with, and the game's catch rule
  (2.5 px after, 0.5 px before, 1 px in from the ends, the platform's previous top when it rose) calls `StandOn`.
  The feet "before the vertical move" of the game's rule are taken before the step: for a rider the rule's first
  condition holds either way, for anyone else the two are the same number.
- L5 The closed loop now gives the shadow Pip's whole state through `Restore` when the stage changed him (a stomp, a
  spring, a hit, the goal), where it could only place him and set his velocity before; a divergence that follows is
  the mover's.
- L6 A landing reach as "ground within reach of a downward `Move`" needed to tell a surface that stopped the probe
  from one it came to touch: the contact's `Stopped`, which S10 asked for on its own account.
- L7 Defaults against `main`: a copy of the old controller (renamed) beside the new one, 40 seeded motions over the
  old parameters, 500 steps each with a one-way platform, a moving platform, launches and teleports: 20,000 of
  20,000 steps identical in position, velocity, events, ground, air time, landing speed and state hash, on tiles, on
  Box2D in metres and on Box2D in pixels. 3508 of the steps on the ground, 596 jumps, 33 drops, 812 steps on the
  moving platform, 6 corner corrections, 310 walls met. The comparison is not committed (it is a copy of old source).
- L8 The native library on this machine was of layer version 1 (11 November) while the assembly needs 7, so every
  Box2D test was inconclusive. It was rebuilt from the cached Box2D sources with `native/build.sh win-x64` into this
  worktree's `artifacts/` (ignored by Git), in a work directory of its own; the C layer was not edited.
- L9 S15. Origin at the centre: 3061 of 6000 steps differ from `TileMotion`, worst 0.0002 px (Courtyard Key measured
  1704 and 0.00037). Origin at the corner: 1 step. The many are arithmetic (the world derives the box's corner from
  the origin around each sweep); the one is the helper pushing a box out of a tile it entered by less than 0.001 when
  it moves up or left, which the world's tolerance leaves as touching. Exact agreement in that case would mean
  copying an asymmetry of the helper into the world: declined in ADR 0075, pinned by a test.
- L10 The playground's tour was run with the new room and its captures looked at: the plank is drawn, the character
  skids, jumps and finds the block as before.

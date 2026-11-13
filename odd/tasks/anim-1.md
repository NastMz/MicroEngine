# ANIM-1: extensible 2D animation beyond sprite-sheet playback

Objective: decide, with evidence, which animation capabilities an external developer already has in the SDK, which are
generic gaps, and implement the gaps the evidence justifies. Issue NastMz/MicroEngine#120. Branch `feat/anim-1`.
Test runner: `dotnet test MicroEngine.slnx -c Release`. Evaluation: [`docs/evaluations/anim-1.md`](../../docs/evaluations/anim-1.md).

## Specs

- S1 Why: "MicroEngine supports sprite atlases, clips, tweening and interpolated world transforms. Determine whether
  developers can express advanced 2D animations idiomatically without implementing generic animation infrastructure in
  each game."
- S2 Capabilities: "Property/keyframe animation, timelines, named events, layered animation, blending/transitions and
  hierarchical rigs where justified."
- S3 Separation: "Separate 2D sprite animations from skeletal animation (Spine-like formats) and game-specific state
  machines."
- S4 Tools: "Review mature reusable tools, formats and libraries (managed or native), their licensing, asset pipeline
  fit and portability."
- S5 Done when: "Document which capabilities already exist, which are genuine generic SDK gaps, and whether to reuse,
  extend or defer each. Demonstrate with a consumer scenario only if warranted."
- S6 Scope limit: "**Status:** Idea / research; no blanket commitment to an animation editor."
- S7 Owner's rule for research issues (delegation brief): they end in implementation; the bar is "could an external
  developer build that game today with the published SDK". Implement the generic gaps as small, genre-neutral, additive
  primitives with tests, a sample region and a guide page; defer, with reasons, what does not belong in the engine.
- S8 Owner gate (delegation brief): no third-party dependency, native library or new package; a worthwhile one is
  described under "Needs owner decision". Existing clip and tween behaviour stays identical.

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 Map clips, tweens, easing, coroutines, world transforms, atlas tags | S1 S2 | inline | done | L2 |
| T2 Review tools, formats and libraries | S3 S4 | inline | pending | |
| T3 `Track<T>`, `Keyframe<T>`, `Track` factories (kernel, next to `Ease`) | S2 S7 | inline | pending | |
| T4 `Timeline` (next to `Tweener`): bound tracks, named events, loop, speed, seek, state hash | S2 S7 | inline | pending | |
| T5 Example scene, guide page, release notes, API reference | S5 S7 | inline | pending | |
| T6 Evaluation `docs/evaluations/anim-1.md`, ADR 0055, architecture table | S1–S6 | inline | pending | |

## Log

- L1 (2026-11-13) Issue #120, verbatim:

  > ## Why
  > MicroEngine supports sprite atlases, clips, tweening and interpolated world transforms. Determine whether developers can express advanced 2D animations idiomatically without implementing generic animation infrastructure in each game.
  >
  > ## Research
  > - Property/keyframe animation, timelines, named events, layered animation, blending/transitions and hierarchical rigs where justified.
  > - Separate 2D sprite animations from skeletal animation (Spine-like formats) and game-specific state machines.
  > - Review mature reusable tools, formats and libraries (managed or native), their licensing, asset pipeline fit and portability.
  >
  > ## Done when
  > Document which capabilities already exist, which are genuine generic SDK gaps, and whether to reuse, extend or defer each. Demonstrate with a consumer scenario only if warranted.
  >
  > **Status:** Idea / research; no blanket commitment to an animation editor.

- L2 (2026-11-13) Mapping. Sprite clips and `Animator` are in `src/MicroEngine.Graphics2D/Legacy/Animation.cs`
  (fixed rate or a duration per frame, loop, speed, `Finished`); Aseprite tags become clips through `AtlasData`.
  `Tweener` (one value from 0 to 1 over a duration, ease, delay, `done`) and `CoroutineRunner` are in
  `src/MicroEngine/Scenes/Coroutines.cs`; `Ease` is in `src/MicroEngine.Kernel/Math/MathX.cs`. The scene world
  (`src/MicroEngine.Scene/IWorld.cs`) has parents, local transforms and interpolated world matrices. Missing: a value
  with more than two keys, a playhead shared by several values, and markers on that playhead. Each game would write
  those three itself; they are the generic gap. Homes: tracks next to `Ease` (kernel, no dependencies), the timeline
  next to `Tweener`.

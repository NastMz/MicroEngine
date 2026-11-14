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

- S9 Site (L6): "Registrar la guía en el sitio."
- S10 Semantics (L6): "Comprobar la semántica de seek, eventos y loops."
- S11 Criterion (L6): "corregir problemas reales dentro del lote cuando el alcance sea razonable. No ampliar
  innecesariamente la arquitectura ni crear issues preventivos. Si algo exige un rediseño considerable, presenta
  evidencia, alcance y alternativas antes de implementarlo. Distingue expresamente los fallos demostrados, las
  hipótesis y las diferencias perceptibles para el usuario."

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 Map clips, tweens, easing, coroutines, world transforms, atlas tags | S1 S2 | inline | done | L2 |
| T2 Review tools, formats and libraries | S3 S4 | inline | done | L5; evaluation §3 |
| T3 `Track<T>`, `Keyframe<T>`, `Track` factories (kernel, next to `Ease`) | S2 S7 | inline | done | `9a55183`; RED: 15 failing against stubs; `TimelineTests` 15/15 |
| T4 `Timeline` (next to `Tweener`): bound tracks, named events, loop, speed, seek, state hash | S2 S7 | inline | done | `9a55183`; same tests |
| T5 Example scene, guide page, release notes, API reference | S5 S7 | inline | done | L4; `TimelineExampleTests` 2/2; captures inspected |
| T6 Evaluation `docs/evaluations/anim-1.md`, ADR 0055, architecture table | S1–S6 | inline | done | this branch |
| T7 Register the guide in the site, link it from the manual and the examples page | S9 | inline | done | `261dd60`; L7 |
| T8 Check seek, event and loop rules through tests; fix what fails; document the rest | S10 S11 | inline | done | `c118cef`; RED: 6 failing of 38; L8 |
| T9 Examples tour: find why captures drift, fix it | S11 | inline | done | `26a2fa6`; RED: `ExamplesTourTests` 25 blocks for 26 entries; L9 |

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
- L3 (2026-11-13) Decision. Three gaps are generic and implemented: keyframe tracks, a timeline, named events.
  Layers, cross-fades and cut-out rigs are compositions of those with what exists and get documentation, not types.
  Skeletal runtimes, state machines, a track file format and an editor are deferred (evaluation §4, §5, §7). `Track`
  factories take `params` keys so keys read as `new(time, value, ease)`; with exactly one key the target type must be
  written (`new Keyframe<float>(0, 1)`), which a one-key track rarely needs.
- L4 (2026-11-13) Example. `TimelineExample` is the last entry of the examples so the tour script's earlier captures
  keep their order; the tour presses down twice before it because, on `main`, the list's focus is one entry behind the
  script by the end (after the 24th capture the focus is on "Text entry and clipboard", seen in a debug capture). Not
  fixed here: the script's earlier blocks belong to other examples. Captures `25-timeline.png` (arm raised, "STRIKE")
  and `25-timeline-rest.png` ("READY HITS 1") were inspected. The sample test was written together with the sample, so
  no failing run was observed for it; the engine tests were seen failing first.
- L5 (2026-11-13) Tools. Spine's runtime licence (text of 2025-04-05) requires each user of a product to hold a Spine
  Editor licence; DragonBonesCSharp (MIT) lists Unity only; SpriterDotNet (zlib) was last pushed 2022-12; rive-sharp
  needs a native `rive.dll`; MonoGame.Extended's tweener and Glide bind members through reflection. No dependency is
  proposed. The guide page `docs/guide/animation.md` is not registered in `site/src/lib/repo-docs.mjs` (outside this
  task's edit surface): owner follow-up.
- L6 (2026-11-13) Owner's request, verbatim:

  ```
  ANIM-1

  * Registrar la guía en el sitio.
  * Comprobar la semántica de seek, eventos y loops.

  Criterio general: corregir problemas reales dentro del lote cuando el alcance sea razonable. No ampliar innecesariamente la arquitectura ni crear issues preventivos. Si algo exige un rediseño considerable, presenta evidencia, alcance y alternativas antes de implementarlo. Distingue expresamente los fallos demostrados, las hipótesis y las diferencias perceptibles para el usuario.
  ```

- L7 (2026-11-13) Site. `docs/guide/animation.md` is the page `docs/animation`, in the manual group after Graphics;
  linked from the manual's Animation and Tweens sections and from a new "Tracks and timelines" section of the focused
  examples. `node site/scripts/snippets.mjs` covers its three quoted regions; `SITE_DEMOS=none npm run build` ended with
  "dist: complete (537 documentation pages ...)".
- L8 (2026-11-13) Semantics. 23 tests added (16 timeline, 7 track); 6 failed before the fix, each a demonstrated
  failure: (1) a looping timeline given a step far longer than its duration never returned (`Update(1e9f)` on a 1 s
  loop: subtracting one pass does not change the number in single precision); (2) `Seek(Duration)` on a timeline that
  does not loop reported `Finished` and the next update still applied the tracks and raised the end event;
  (3) `Seek(NaN)` left a NaN playhead; (4) a negative, NaN or infinite step was ignored without a word (an infinite one
  on a loop hung as in 1); (5) `Track.Keys` returned the track's own array, writable through a cast; (6) a timeline
  played to its end and one moved there hashed differently. Fixes: passes computed by division in double precision and
  at most 1,000 raised per update; `Seek` finishes at the end; `Update`, `Speed` and `Seek` refuse values that are not
  forward or not numbers; `Keys` is a read-only view. Decisions where the documents were silent (now in the XML
  comments and the guide): refusing instead of ignoring a bad step (as `Speed` already did; `Animator.Update` and
  `Tweener.Update` are not changed); the 1,000-pass bound; `Seek` clamps on a loop instead of wrapping; `Track.At(NaN)`
  is the first key. The other rules were already right and are now pinned. `Reached` is an event, not a value that is
  read and cleared: what is pinned is that every handler of one update sees the same playhead and values.
- L9 (2026-11-13) Tour. Cause of the drift noted in L4: "Diagnostics and debug drawing" (11th entry) joined the list
  without a block in `scripts/tour.script`, so `11-particles.png` showed Diagnostics and each later capture the example
  before its name (seen in the captures); the extra key press before the timeline hid it. The block is added in its
  place (`11-diagnostics.png`), every later line moves 120 frames and its capture one number, and the extra press is
  gone. All 27 captures were looked at and show what their names say. `ExamplesTourTests` compares the number of blocks
  with `ExamplesGame.All`. `ExamplesGame.cs` is unchanged.

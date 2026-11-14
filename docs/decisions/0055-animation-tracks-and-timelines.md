# ADR 0055: Animation beyond sprite clips is keyframe tracks and timelines; skeletal runtimes and state machines stay out

## Status

Accepted (2026-11-13). Evidence: [ANIM-1](../evaluations/anim-1.md). Task record:
[`odd/tasks/anim-1.md`](../../odd/tasks/anim-1.md).

## Context

The SDK animates sprite frames (`AnimationClip`, `Animator`, Aseprite tags), single values between two ends
(`Tweener`) and sequences of steps (`CoroutineRunner`), and its scene world has transform hierarchies drawn blended
between steps. A developer outside the project who wants a value with more than two keys, several values moving from
one time, or a moment in an animation the game reacts to (a footstep, the frame a swing lands) has to write the key
search, the playhead and the event bookkeeping first. That code is the same in every game and easy to get wrong
across loops and long frames (ANIM-1 §2).

No engine-neutral .NET library offers it without reflection or a dependency on another framework, and the mature
skeletal tool with a managed runtime (Spine) carries a licence condition on every user of the product (ANIM-1 §3–4).

## Decision

- **Tracks.** `Track<T>` (kernel, beside `Ease`) is an immutable list of `Keyframe<T>`, each with the easing curve of
  the segment that arrives at it, and `At(time)`. It holds no playhead, so it is shareable data. `Track.Float`,
  `Track.Vector2`, `Track.Color` and `Track.Step<T>` cover the usual types; any other type brings its blend function.
- **Timelines.** `Timeline` (beside `Tweener`) is a playhead the game holds and updates, like an `Animator`: it applies
  bound tracks through delegates (no reflection, safe for trimming and NativeAOT), loops, scales its speed, seeks, and
  raises named events once per pass in time order, none lost in a long update. It takes part in the state hash.
- **Playhead rules** (`TimelineTests`, `TrackTests`). Time only goes forwards: `Update` and `Speed` refuse negative,
  infinite and NaN values with an exception instead of ignoring them, as `Animator.Speed` does; `Animator.Update` and
  `Tweener.Update` do not validate their step and are left as they are. `Seek` raises nothing, clamps to
  0..`Duration` in both loop modes, and finishes a timeline that does not loop when it lands on the end, so
  `Finished` and "a finished timeline does nothing" always agree. A looping playhead is in `[0, Duration)`. One update
  raises the events of at most 1,000 passes, which bounds the work of a step far longer than the loop.
- **Composition over features.** Layers are several timelines; a cross-fade is a `Lerp` of two `Track.At` reads; a
  cut-out rig is the scene world's hierarchy with tracks writing local transforms. The engine adds no layer mixer,
  blend tree or rig type.
- **Unchanged.** `AnimationClip`, `Animator`, `Tweener` and `CoroutineRunner` keep their behaviour; a sprite clip gets
  events from a timeline of its length.
- **Out of the engine.** Skeletal runtimes (bones with skins and mesh deformation) are not in the core; one may become
  an optional package only by an owner decision that accepts its licence. State machines and blend trees that choose
  which animation plays are game rules. There is no animation editor and no file format for tracks.

## Consequences

- An external developer writes animation data and reactions, not animation infrastructure.
- Tracks are authored in code. A file format waits for a chosen authoring tool.
- Games that need deforming skeletal characters integrate a runtime themselves, and need textured geometry from the 2D
  façade, which `IRenderer` does not expose today.
- Two small public types and one factory class join the SDK surface (Alpha: listed in the release notes).

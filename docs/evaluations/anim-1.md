# ANIM-1: 2D animation beyond sprite-sheet playback

Evaluation for issue #120 (2026-11-13, branch `feat/anim-1`). Decision: [ADR 0055](../decisions/0055-animation-tracks-and-timelines.md).
Task record: [`odd/tasks/anim-1.md`](../../odd/tasks/anim-1.md).

The question, judged from outside the repository: can a developer with the published SDK animate a 2D game past
"play the frames of a sheet" without first writing generic animation infrastructure? The answer before this work was
"partly"; the missing part was small and is implemented here. Skeletal runtimes and state machines stay out, for the
reasons in sections 4 and 5.

## 1. What the SDK had

| Capability | Where | What it does | Limit |
| --- | --- | --- | --- |
| Sprite clips | `AnimationClip`, `src/MicroEngine.Graphics2D/Legacy/Animation.cs:4` | Frames at a fixed rate or with a duration per frame, looping or one-shot, `FrameAt(time)` | Frames only |
| Clip playback | `Animator`, `Animation.cs:81` | Named clips, `Play`, `Speed`, `Finished` | No events, no notion of "frame changed" |
| Clips from files | `SpriteAtlas.Clip`, `src/MicroEngine.Graphics2D/Legacy/Sprite.cs:244`; Aseprite tags and durations, `AtlasData.cs:22`, `:119` | A tag in Aseprite becomes a clip with its timing | Frames only; Aseprite has no bones |
| Tweens | `Tweener`, `src/MicroEngine/Scenes/Coroutines.cs:122` | One callback driven from 0 to 1 over a duration, with an ease, a delay and `done` | Two ends only; no handle, no shared time, no seeking |
| Easing | `Ease`, `src/MicroEngine.Kernel/Math/MathX.cs:51` | Ten curves, plain `Func<float, float>` | |
| Sequencing | `CoroutineRunner`, `Coroutines.cs:27` | Steps over time, `Wait.Seconds`, `Wait.Until`, nesting | Imperative: cannot be sought, sampled or shared as data |
| Hierarchies | `IWorld`, `src/MicroEngine.Scene/IWorld.cs:49`, `:61`, `:69`, `:71` | Parents, local transforms, world matrices | |
| Drawing between steps | `Transform.Interpolate`, `src/MicroEngine.Scene/Transform.cs:66` | Positions and scales lerp, rotations slerp | |
| Transform per draw | `Renderer2D.PushTransform`, `src/MicroEngine.Graphics2D/Renderer2D.cs:210` | A `Matrix3x2` stack under the `IRenderer` façade | The façade itself takes position, rotation and scale per sprite |

## 2. Capability by capability

"Expressible" means an external developer writes game code only, no reusable infrastructure.

| Capability (issue) | Before | Verdict | After |
| --- | --- | --- | --- |
| Property / keyframe animation | A tween moves one value between two ends. Three keys already need chained tweens with `done` callbacks or a hand-written table and search | **Gap, implemented** | `Track<T>`, `Keyframe<T>`, `Track.Float` / `Vector2` / `Color` / `Step` (`src/MicroEngine.Kernel/Animation/Track.cs:11`, `:19`, `:77`) |
| Timelines | Nothing plays several values from one time. Coroutines sequence, but each game would write the playhead, loop and seek | **Gap, implemented** | `Timeline` (`src/MicroEngine/Scenes/Timeline.cs:10`): bound tracks, loop, speed, seek, state hash |
| Named events | None. A footstep on a frame of a walk means comparing times by hand every update, and a long frame skips it | **Gap, implemented** | `Timeline.At` and `Reached` (`Timeline.cs:71`, `:53`): once per pass, in order, none lost in a long update |
| Layered animation | Expressible once tracks exist: independent players over different values | **Exists (composition)** | Two timelines; documented, shown by the example |
| Blending / transitions | For sprite frames there is nothing to blend. For values, a weight between two sources | **Exists (composition), blend trees deferred** | `Track.At` reads without a playhead, so a cross-fade is one `Lerp`; documented |
| Hierarchical rigs | The scene world already has parents, local transforms and blended world matrices | **Exists (reuse)** | Tracks write `Objects.Local(id)`; documented |
| Sprite animation | Complete for frames, including Aseprite timing | **Exists** | Unchanged; events come from a timeline of the clip's length |
| Skeletal animation (Spine-like) | Not present | **Deferred** (section 4) | |
| Game state machines | Not present, by design | **Out of the engine** (section 5) | |
| Animation editor | Not present | **Out of scope** (the issue excludes it) | |

Why these three are generic and not a game's business: every game that animates more than a slide needs the key
search, the segment easing and the event bookkeeping across loops and long frames; none of it depends on a genre; and
the event bookkeeping is easy to get wrong (events lost when a frame jumps over them, fired twice at a loop seam).
They are also small: two files, about 250 lines with their comments, no dependencies.

Why they are separate types and not more `Tweener`: a tween is a fire-and-forget change owned by a scene; a track is
shareable data and a timeline is a playhead the game holds, seeks and reads. `Tweener` and `Animator` are unchanged.

## 3. Tools, formats and libraries

Repository metadata was read from the GitHub API on 2026-11-13; licence texts from the linked pages on the same day.
Portability is judged for the SDK's targets: desktop, browser (WebAssembly) and NativeAOT (phones, trimmed builds).

| Tool or library | What it is | Licence | Pipeline fit | Portability | Verdict |
| --- | --- | --- | --- | --- | --- |
| Aseprite | Frame animation editor; JSON export with tags and durations | The editor has its own licence; exported sheets and JSON are the author's files | Already imported (`AtlasData`) | Data only | **Reuse (done)**: covers sprite animation |
| Spine (`EsotericSoftware/spine-runtimes`, spine-csharp) | The reference skeletal tool: bones, skins, meshes, IK, events | Spine Runtimes License (text updated 2025-04-05): integration allowed, but "each user of the Products must obtain their own Spine Editor license" | Own JSON/binary skeleton plus atlas; needs textured-mesh drawing | Managed C#, no native code | **Defer; owner decision** (section 4) |
| DragonBones (`DragonBones/DragonBonesCSharp`) | Skeletal tool with a C# runtime | MIT | Own JSON; the C# runtime lists Unity as its only supported engine | Managed; no target other than Unity is listed | **Defer**: no engine-neutral runtime to reuse |
| Spriter (`loodakrawa/SpriterDotNet`) | Cut-out (bone and sprite) animation, `.scml` | zlib | A C# reader and player for `.scml` | Managed; last push 2022-12-07, no releases listed | **Defer**: workable for a game, too stale to publish on |
| Rive (`rive-app/rive-sharp`) | Vector animation with state machines | Not stated on the repository page | `.riv`; vector renderer required | Native `rive.dll` built from a C++ submodule; samples are UWP on Windows; last push 2025-01-22 | **Reject**: native, vector, no browser path |
| Lottie | After Effects vector animation as JSON | Format is open; players vary | Needs a vector renderer the engine does not have | Depends on the player | **Reject**: not a sprite engine's format |
| glTF 2.0 animation | Channels and samplers (linear, step, cubic spline) over node transforms and skins | Royalty-free Khronos standard | The engine's own glTF reader exists and refuses skins and animation (`docs/manual/rendering.md:471`) | Data only; own reader | **Defer**: belongs to the 3D path, not to 2D |
| MonoGame.Extended tweening | Tweens on members | MIT | Code only | Binds through `Expression<Func<…>>` and `System.Reflection` (`Tweening/Tweener.cs`): hostile to trimming and NativeAOT; depends on MonoGame | **Reject** |
| Glide (`jacobalbano/glide`) | Tweens on members by name | MIT | Code only | Reflection (`MemberAccessor.cs`); last push 2019 | **Reject** |
| GTweens (`Guillemsc/GTweens`) | Tweens and sequences with getter/setter delegates | MIT | Code only | Delegates, no reflection in its documented API; last push 2025-08-09 | **Not needed**: overlaps `Tweener`; has no keyframe track or sought timeline to reuse |

Conclusions:

- There is no reusable, engine-neutral, AOT-safe .NET library for keyframe tracks with events; the tween libraries
  either need reflection or duplicate what `Tweener` does. Writing the primitive is cheaper than adopting one.
- For data, the engine already reads the one format that matters for sprite animation (Aseprite). Keyframe tracks are
  written in code, which matches the code-first default (ADR 0002); a file format for them is deferred until a real
  authoring tool is chosen, because a format without an editor is only a worse way to write code.

## 4. Skeletal animation: deferred

A skeletal runtime does not belong in the engine's core now:

- **Licence.** Spine is the only actively developed, engine-neutral skeletal tool with a managed runtime, and its
  runtime licence requires every user of a product that integrates it to hold a Spine Editor licence. That condition
  cannot be attached to an MIT SDK's core package; at most it fits an optional package that only Spine licensees
  install, with the notice the licence requires.
- **Renderer.** Skeletal sprites with mesh deformation need textured triangle lists through the 2D façade.
  `IRenderer2D.DrawGeometry` exists (`src/MicroEngine.Graphics2D/Renderer2D.cs:77`) but the `IRenderer` façade games
  draw with does not expose it; that surface belongs to the renderer work, not to this issue.
- **Need.** Cut-out rigs without deformation (parts that rotate around joints) are already expressible: entities in
  the scene world for the hierarchy, tracks for the joint angles. This evaluation found no demo or sample that needs bones with
  meshes.

What would change this: an owner decision to publish an optional `MicroEngine.Spine` package (see "Needs owner
decision"), or a maintained open format with an engine-neutral managed runtime appearing.

## 5. State machines: the game's

Which animation plays when (idle, run, jump, hurt, with which interruptions) is game rules. The engine stays
genre-neutral (AGENTS.md working rules), the tutorial already teaches the pattern as plain code
(`docs/tutorial/10-states.md`), and a generic state-machine or blend-tree framework would be a second way to write
`switch`. `Animator.Play(name)`, `Timeline.Seek(0)` and `Finished` are the hooks a game's own states call.

## 6. Consumer scenario

Warranted, because the primitives are new public API: `samples/Examples/Examples/TimelineExample.cs` ("Tracks and
timelines" in the examples) plays a strike from one timeline (an eased multi-key angle, a colour, a step track of pose
names, an event at the moment it lands) over a breath that loops on a second timeline. `TimelineExampleTests` plays it
headlessly; the tour script captures it mid-swing and at rest (both captures inspected).

## 7. Needs owner decision

1. **Publishing the guide page.** `docs/guide/animation.md` is written and its quoted code is checked against the
   sample, but pages reach the site through `site/src/lib/repo-docs.mjs`, which this work did not touch. Registering
   it (and linking it from the manual's Animation and Tweens sections and from the focused examples page) is one small
   follow-up.
2. **An optional Spine package.** Feasible technically (spine-csharp is managed code), conditional on the licence
   notice, on exposing textured geometry through the 2D façade, and on someone holding a Spine licence to test with.
   Recommendation: do not start it without a concrete game asking for it.
3. **A data format for tracks.** Deferred; reconsider together with a choice of authoring tool.

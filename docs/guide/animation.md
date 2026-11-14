# Animation

MicroEngine animates in four ways, from the simplest to the most flexible. They are independent: use the one that fits
each thing that moves, and combine them freely.

| To do this | Use | Where |
| --- | --- | --- |
| Play the frames of a sprite sheet | `AnimationClip` and `Animator` | [Graphics](../manual/rendering.md#animation) |
| Move one value from A to B | `Tweens.Add` | [Scenes](../manual/scenes.md#tweens) |
| Run steps one after another over time | `Coroutines.Start` | [Scenes](../manual/scenes.md#coroutines) |
| Move a value through several keys, several values together, with moments the game reacts to | `Track<T>` and `Timeline` | This page |

## Tracks

A `Track<T>` is a value that changes over time through keys. Each key has a time in seconds, a value and, optionally,
the easing curve of the way from the previous key to it (an `Ease` function or your own; linear when omitted).
`track.At(time)` gives the value at any time: the first key's value before it, the last key's value after it.

<!-- code: samples/Examples/Examples/TimelineExample.cs#tracks -->
```csharp
// A track is data: keys in seconds, each with the curve of the way to it. It holds no playhead, so every figure
// that moves this way can share it.
private static readonly Track<float> Swing = Track.Float(
    new(0, 0), new(0.35f, -2.2f, Ease.OutCubic), new(0.5f, 0.35f, Ease.InCubic), new(0.9f, 0, Ease.OutBack));
private static readonly Track<Color> Glow = Track.Color(
    new(0, Rest), new(0.5f, Rest), new(0.52f, Hot), new(0.9f, Rest));
// A step track holds each value until the next key: for sprites, names and flags.
private static readonly Track<string> Poses = Track.Step<string>(
    new(0, "RAISE"), new(0.35f, "STRIKE"), new(0.5f, "READY"));
private static readonly Track<float> Breath = Track.Float(
    new(0, 0), new(0.8f, 3, Ease.InOutSine), new(1.6f, 0, Ease.InOutSine));
```

| Factory | Animates | Between two keys |
| --- | --- | --- |
| `Track.Float` | A number: a coordinate, an angle, a scale, an opacity | Linear blend |
| `Track.Vector2` | A point or a size | Linear blend |
| `Track.Color` | A colour | Blend per channel; a curve that overshoots stops at the key's colour |
| `Track.Step<T>` | Anything with nothing in between: a `Sprite`, a name, a flag | Holds the earlier key's value |
| `new Track<T>(blend, keys)` | Any type, with your own blend function | `blend(from, to, progress)` |

A track is immutable and keeps no time of its own, so one track serves every object that moves the same way, and it
can be read at any time you choose: `Swing.At(scene.Age)`, or `Swing.At(distance / speed)` to follow something other
than a clock. Keys are in increasing order of time, and may start before 0 or after it; a track with no keys, with
keys out of order (they are not sorted for you), with two keys at one time or with a time that is not a finite number
is refused when it is created.

| Time given to `At` | Value |
| --- | --- |
| Before the first key | The first key's value |
| Exactly a key's time | That key's value, whatever its curve; a step track switches exactly there |
| Between two keys | Their blend along the later key's curve |
| After the last key | The last key's value |
| Not a number | The first key's value |

## Timelines

A `Timeline` is a playhead that plays several tracks together. `Add(track, apply)` connects a track to the value it
animates: at each update `apply` receives the track's value at the playhead. `At(time, name)` marks a named moment,
and the `Reached` event fires with that name when the playhead gets there.

<!-- code: samples/Examples/Examples/TimelineExample.cs#timeline -->
```csharp
private readonly Timeline _strike = new(0.9f);
private readonly Timeline _breath = new(1.6f, loop: true);

public override void Enter()
{
    _strike
        .Add(Swing, angle => Arm = angle)        // each update: the track's value at the playhead
        .Add(Glow, colour => _anvil = colour)
        .Add(Poses, pose => Pose = pose)
        .At(0.5f, "hit");                        // a named moment the game reacts to
    _strike.Reached += name => { if (name == "hit") Hits++; };
    _strike.Seek(_strike.Duration);              // starts finished, at rest

    _breath.Add(Breath, y => Bob = y);           // another timeline over another value: a layer
}
```

Like an `Animator`, a timeline advances when you update it, so it follows the time you give it: the scene's step, a
world's scaled clock, or nothing while the game is paused.

<!-- code: samples/Examples/Examples/TimelineExample.cs#update -->
```csharp
public override void Update(float dt)
{
    if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
    if (Input.Pressed("confirm") && _strike.Finished) _strike.Seek(0);   // plays it again from the start
    _breath.Update(dt);
    _strike.Update(dt);
}
```

Source: [`TimelineExample.cs`](../../samples/Examples/Examples/TimelineExample.cs), the "Tracks and timelines" entry of
the examples.

| Member | Meaning |
| --- | --- |
| `new Timeline(duration, loop = false)` | A timeline of `duration` seconds (positive); a looping one starts over at the end |
| `Add(track, apply)` | Plays a track; a track shorter than the timeline holds its last key, a longer one is played up to the timeline's end |
| `At(time, name)` | A named event at `time` (0 to `Duration`) |
| `Reached` | Raised with the event's name, after the tracks were applied for that update |
| `Update(dt)` | Advances by `dt × Speed`, applies the tracks and raises the events reached |
| `Seek(time)` | Moves the playhead and applies the tracks there, raising nothing; `Seek(0)` plays again |
| `Time`, `Duration`, `Loop` | The playhead and the timeline's shape |
| `Speed` | 1 by default; 0 pauses, 2 plays twice as fast |
| `Finished` | Whether a timeline that does not loop is at its end |

The timeline's length is the one you give it, not its tracks': events may lie after the last key of every track.

Rules worth knowing:

- **Events fire once per pass, in time order.** An update that jumps over several events raises all of them, and an
  update longer than a looping timeline raises the events of every pass it covered, so a slow frame never loses a
  footstep. Events at the same time fire in the order they were added.
- **An event at 0 fires in the first update that advances**, and each time a looping timeline starts over. **An event
  at the duration fires when the timeline ends**; on a looping timeline it fires at the end of each pass, just before
  the events at 0 of the next one.
- **A handler sees the values of its update**: tracks are applied before events are raised, so every handler of one
  update reads the same `Time` and the same values, the ones the update ended on. A handler may call `Seek`; the rest
  of that update's events are dropped.
- **A looping playhead never rests on the end.** `Time` goes from 0 up to, but not including, `Duration`: reaching the
  end is being at the start of the next pass, so the values applied are always the tracks' values at `Time`.
- **A finished timeline stops applying**, so the game may change the values afterwards. It stays finished until `Seek`
  moves it back.
- **`Seek` raises nothing**: neither the events it jumps over, in either direction, nor one at the time it moves to.
  The next update that advances plays from there, an event exactly there included, so seeking back plays earlier
  events again. The time is kept within 0 and `Duration`, on a looping timeline too (it does not wrap).
- **Seeking a timeline that does not loop to its end finishes it**: the tracks are applied there and nothing more
  plays, an event at the end included. This is how the example starts at rest.
- **An update of 0 seconds, or at `Speed` 0, applies the tracks and raises nothing.** Time only goes forwards:
  `Update` refuses a step that is negative, infinite or not a number, `Speed` refuses the same, and `Seek` refuses a
  time that is not a number.
- **One update raises the events of at most 1,000 passes** of a looping timeline. A longer one (hours given at once
  to a loop of a fraction of a second) ends at the same place and skips the passes in between.
- **Timelines take part in the state hash** when the scene adds them: call `timeline.HashState(hash)` from the scene's
  `HashState`.

## Recipes

**Events on a sprite animation.** Give a timeline the clip's length and read the clip at the playhead; the timeline
adds the events the clip does not have.

```csharp
var walk = new Timeline(clip.Duration, loop: true).At(0.1f, "step").At(0.4f, "step");
walk.Reached += _ => Audio.Play("step");
// Update: walk.Update(dt);    Draw: r.Draw(clip.FrameAt(walk.Time), position);
```

**Layers.** Each timeline writes the values it was given, so two timelines over different values play at once: a
looping breath on the body and a one-shot strike on the arm, as in the example. When two layers move the same value,
keep each in its own variable and add them when drawing.

**Blending and transitions.** A track can be read without a timeline, so a transition between two movements is one
line: read both and blend with a weight that goes from 0 to 1.

```csharp
float angle = MathX.Lerp(idleArm.At(time), aimArm.At(time), _aim);   // _aim moved by a tween
```

**Rigs made of parts.** An arm that carries a hand that carries a tool is a hierarchy of entities in the
[scene world](../manual/scenes.md#the-scene-world): each part's transform is relative to its parent, and the world
draws them blended between steps. Tracks animate the local transforms.

```csharp
timeline.Add(elbowAngle, a => Objects.Local(forearm).Rotation2D = a);
```

**Which animation plays when.** The rules that choose a movement (idle, run, jump, hurt) belong to the game: a
`switch` over the character's state that calls `animator.Play(name)` or `timeline.Seek(0)` is enough, and
[game states](../tutorial/10-states.md) shows the pattern.

## Limits

- Timelines and animators do not play backwards (`Speed` is 0 or more).
- Tracks are written in code. Sprite frames, their durations and their tags come from files
  ([atlases from packing tools](../manual/assets.md#atlases-from-packing-tools)); the SDK reads no file format for
  keyframes, bones or meshes that deform, and has no animation editor.
- Skeletal animation tools with their own runtime (bones, skins, mesh deformation) are not part of the SDK.

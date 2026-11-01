# 5. Animation

The gardener's sheet has four walking frames. This step plays them while he walks, faster while he runs, and shows a
still frame when he stops.

## Clips and the animator

MicroEngine's animation has two types:

- `AnimationClip(frames, framesPerSecond, loop = true)`: an immutable list of sprites played at a fixed rate.
  `clip.FrameAt(time)` returns the frame for a time in seconds.
- `Animator`: plays one named clip at a time. `Add(name, clip)` registers clips, `Play(name)` switches to one,
  `Update(dt)` advances it, `Frame` is the sprite to draw.

The player's constructor, from the previous step, registers two clips:

<!-- code: samples/GemGarden/part2-hero/World/Player.cs#fields -->
```csharp title="World/Player.cs"
private const float WalkSpeed = 70, RunSpeed = 115;   // pixels per second

private readonly Animator _animator = new();
private RectF _bounds;          // a small box at the feet: what will collide with the world
private bool _facingLeft;

public Player(SpriteAtlas sprites, Vector2 start)
{
    _animator
        .Add("idle", new AnimationClip([sprites["hero0"]], 1))
        .Add("walk", new AnimationClip(sprites.Frames("hero"), 8));
    _animator.Play("idle");
    _bounds = RectF.FromCenter(start, 10, 8);
}

/// <summary>The point between the feet, in pixels: where the sprite's origin is drawn.</summary>
public Vector2 Position => new(_bounds.Center.X, _bounds.Bottom);
public RectF Bounds => _bounds;
```

- `sprites.Frames("hero")` returns `hero0`, `hero1`, `hero2`, `hero3`: the frames `AddGrid` cut in step 3, in order.
  `walk` plays them at 8 frames per second, looping.
- `idle` is a clip of one frame: standing still.

## Playing the right clip

The end of `Player.Update` chooses the clip every step:

<!-- code: samples/GemGarden/part2-hero/World/Player.cs#animate -->
```csharp title="World/Player.cs"
if (direction.X != 0) _facingLeft = direction.X < 0;
_animator.Play(direction == Vector2.Zero ? "idle" : "walk");
_animator.Speed = speed / WalkSpeed;
_animator.Update(dt);
```

- Calling `Play` with the clip that is already playing does nothing, so calling it every step is safe: the walk does
  not restart from its first frame each step. `Play(name, restart: true)` forces a restart.
- `Speed` scales time: running is 115/70 times faster than walking, so the legs move 1.6 times faster too.
- `_facingLeft` remembers the last horizontal direction, and `Draw` mirrors the frame with `flipX` (step 3), so one
  set of frames serves both directions.

Clips that should play once (an attack, a jump) use `loop: false`; the animator's `Finished` becomes true on their last
frame, and the game decides what plays next. The [animation example](../examples/focused.md#animation) shows one.

## Run part 2

Part 2 is complete: your project matches [`part2-hero`](../../samples/GemGarden/part2-hero). Run it:

```bash
microengine run
```

Press confirm on the title. The gardener stands on the lawn; arrows walk him with his legs moving, Shift or X runs,
Escape goes back to the title.

## Not every animation needs an animator

For something that simply loops, a clip and a clock are enough: the slimes of step 7 call `clip.FrameAt(time)`
themselves. Choose the animator when one object switches between several clips.

## Next

[Part 3](06-garden.md) builds the garden: a tile map larger than the screen, a camera that follows the gardener, and
hedges he cannot walk through.

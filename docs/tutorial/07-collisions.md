# 7. Gems, slimes and collisions

Games need two kinds of collision, and MicroEngine handles them differently:

- **Against the world** (walls, floors, platforms): the engine moves a box through the tile grid and stops it at solid
  tiles, with `TileMotion.Move`.
- **Between objects** (the gardener and a gem, the gardener and a slime): the game compares rectangles with
  `RectF.Intersects` and decides what happens.

There is no physics engine (no forces, masses or rigid bodies): movement is code you write, which keeps it exact and
easy to tune. This step uses both kinds.

## Hedges stop the gardener

In part 3, `Player.Update` takes the level and moves through its grid:

<!-- code: samples/GemGarden/part3-garden/World/Player.cs#update -->
```csharp title="World/Player.cs"
public void Update(float dt, InputMap input, Level level)
{
    // Keys give unit directions (diagonals normalised); a stick or the touch joystick also gives how far it is pushed.
    var direction = input.Vector("left", "right", "up", "down");
    float speed = input.Down("run") ? RunSpeed : WalkSpeed;
    var velocity = direction * speed + _knockback;
    _knockback = MathX.Approach(_knockback, Vector2.Zero, 400 * dt);

    // Move through the tile grid: X then Y, stopping at solid tiles (the hedges).
    _bounds = TileMotion.Move(_bounds, velocity * dt, level.Grid).Bounds;

    if (direction.X != 0) _facingLeft = direction.X < 0;
    _animator.Play(direction == Vector2.Zero ? "idle" : "walk");
    _animator.Speed = speed / WalkSpeed;
    _animator.Update(dt);
    _hurtTime = MathF.Max(0, _hurtTime - dt);
}
```

`TileMotion.Move(box, delta, grid)` moves the box by `delta`, first horizontally then vertically, in sub-steps of at
most half a tile, so a fast object never jumps through a thin wall. It returns a `MoveResult` (a `TileMoveResult` in the
next SDK version, not published yet): the new `Bounds`, which
sides hit something (`HitLeft`, `HitRight`, `HitCeiling`, `HitFloor`, `HitWall`), and the flags of the tiles the box
ends up touching (`Touched`, to detect water or hazards). Moving X and Y separately is what lets the gardener slide
along a hedge when he walks diagonally into it.

The collision box is the 10x8 box at the feet, not the 16x16 sprite: the head can overlap a hedge drawn above, as in
most top-down games. Moving `_bounds` is the only movement code: the sprite follows because it is drawn at `Position`.

The player also gains a knock-back and a hurt timer, used by the slimes below. Its fields in part 3:

<!-- code: samples/GemGarden/part3-garden/World/Player.cs#fields -->
```csharp title="World/Player.cs"
private const float WalkSpeed = 70, RunSpeed = 115;   // pixels per second

private readonly Animator _animator = new();
private RectF _bounds;          // a small box at the feet: what collides with hedges, gems and slimes
private bool _facingLeft;
private float _hurtTime;        // seconds of blinking after a slime touched the hero
private Vector2 _knockback;

public Player(SpriteAtlas sprites, Vector2 start)
{
    _animator
        .Add("idle", new AnimationClip([sprites["hero0"]], 1))
        .Add("walk", new AnimationClip(sprites.Frames("hero"), 8));
    _animator.Play("idle");
    _bounds = RectF.FromCenter(start, 10, 8);
}

/// <summary>The point between the feet, in world pixels: where the sprite's origin is drawn.</summary>
public Vector2 Position => new(_bounds.Center.X, _bounds.Bottom);
public RectF Bounds => _bounds;
public bool CanBeHit => _hurtTime <= 0;
```

## Slimes

A slime patrols left and right and turns around at hedges. It is the same technique with a different rule: the game
reads `HitWall` and reverses. Create `World/Slime.cs`:

<!-- code: samples/GemGarden/part3-garden/World/Slime.cs -->
```csharp title="World/Slime.cs"
using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.World;

namespace GemGarden.World;

/// <summary>A slime that patrols left and right, turning around when it bumps into a hedge.</summary>
public sealed class Slime(SpriteAtlas sprites, Vector2 start)
{
    private const float Speed = 30;
    private readonly AnimationClip _wobble = new(sprites.Frames("slime"), 4);
    private RectF _bounds = RectF.FromCenter(start, 12, 7);
    private float _direction = 1, _time;

    public RectF Bounds => _bounds;
    public Vector2 Position => new(_bounds.Center.X, _bounds.Bottom);

    public void Update(float dt, Level level)
    {
        _time += dt;
        var move = TileMotion.Move(_bounds, new Vector2(_direction * Speed * dt, 0), level.Grid);
        _bounds = move.Bounds;
        if (move.HitWall) _direction = -_direction;
    }

    public void Draw(IRenderer r) => r.Draw(_wobble.FrameAt(_time), Position, flipX: _direction < 0);
}
```

Enemy behaviour is game code: MicroEngine provides movement against tiles, not AI. A slime animates with a clip and its
own clock (`FrameAt`), without an animator, because it only ever plays one clip.

## Collecting gems, touching slimes

The play scene's `Update` advances everything, then checks the objects against each other:

<!-- code: samples/GemGarden/part3-garden/Scenes/PlayScene.cs#update -->
```csharp title="Scenes/PlayScene.cs"
public override void Update(float dt)
{
    if (Input.Pressed("pause"))
    {
        Game.Scenes.Replace(new TitleScene(_game), Transition.Fade());
        return;
    }

    _player.Update(dt, Input, _level);
    foreach (var slime in _slimes) slime.Update(dt, _level);
    CollectGems();
    CheckSlimes();

    _sparkles.Update(dt);
    _camera.Follow(_player.Position, dt);
    _camera.Update(dt);
}
```

<!-- code: samples/GemGarden/part3-garden/Scenes/PlayScene.cs#collisions -->
```csharp title="Scenes/PlayScene.cs"
private void CollectGems()
{
    for (int i = _gems.Count - 1; i >= 0; i--)
    {
        var gem = RectF.FromCenter(_gems[i], 8, 8);
        if (!gem.Intersects(_player.Bounds)) continue;
        _gems.RemoveAt(i);
        Gems++;
        Audio.Play("pickup", pitchJitter: 0.05f);
        _sparkles.Emit(gem.Center, Sparkle, 12);
    }
}

private void CheckSlimes()
{
    foreach (var slime in _slimes)
    {
        if (!_player.CanBeHit || !slime.Bounds.Intersects(_player.Bounds)) continue;
        _player.Hit(slime.Position);
        _camera.Shake(0.5f);
        Audio.Play("hit");
    }
}
```

- A gem is a position; its 8x8 rectangle is built when needed. `Intersects` is true when two rectangles overlap (sharing
  only an edge does not count). The loop runs backwards so removing a gem does not skip the next one.
- With a few dozen objects, comparing every pair each step costs nothing. MicroEngine has no spatial index; a game
  with thousands of objects would add its own grid of buckets.
- `_sparkles.Emit(position, spec, count)` starts particles; `Sparkle` (in the fields, step 6) describes their colours,
  speeds and life. The particle system is updated and drawn by the scene like everything else.
- `_camera.Shake(0.5f)` adds "trauma" that decays by itself; `Audio.Play` plays the sounds registered in
  [step 8](08-sound.md).

Touching a slime calls `Player.Hit`:

<!-- code: samples/GemGarden/part3-garden/World/Player.cs#hit -->
```csharp title="World/Player.cs"
/// <summary>A slime touched the hero: push him away from it and blink for a moment, during which nothing hurts.</summary>
public void Hit(Vector2 from)
{
    _hurtTime = 1.2f;
    var away = Position - from;
    _knockback = (away == Vector2.Zero ? Vector2.UnitY : Vector2.Normalize(away)) * 160;
}
```

The knock-back is added to the velocity in `Update` and fades with `MathX.Approach`; because it goes through
`TileMotion.Move` too, it can never push the gardener into a hedge. While `_hurtTime` runs, `CanBeHit` is false and the
gardener blinks:

<!-- code: samples/GemGarden/part3-garden/World/Player.cs#draw -->
```csharp title="World/Player.cs"
public void Draw(IRenderer r)
{
    r.FillEllipse(Position, 6, 2, Color.Black.WithAlpha(0.3f));   // a soft shadow under the feet
    bool blinkOff = _hurtTime > 0 && (int)(_hurtTime * 12) % 2 == 0;
    if (!blinkOff) r.Draw(_animator.Frame, Position, flipX: _facingLeft);
}
```

More about tiles, one-way platforms and extra solid
rectangles (moving platforms, doors): [Tile maps and collision](../manual/world.md).

## Next

Picking up a gem calls `Audio.Play("pickup")`, a sound the game has not registered yet. [Step 8](08-sound.md) adds the
sounds and the music.

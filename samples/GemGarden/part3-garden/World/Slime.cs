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

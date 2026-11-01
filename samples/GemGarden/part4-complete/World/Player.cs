using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.World;

namespace GemGarden.World;

/// <summary>The hero: reads the movement actions, walks through the garden without crossing hedges, and animates.</summary>
public sealed class Player
{
    #region fields
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
    #endregion

    #region update
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
    #endregion

    #region hit
    /// <summary>A slime touched the hero: push him away from it and blink for a moment, during which nothing hurts.</summary>
    public void Hit(Vector2 from)
    {
        _hurtTime = 1.2f;
        var away = Position - from;
        _knockback = (away == Vector2.Zero ? Vector2.UnitY : Vector2.Normalize(away)) * 160;
    }
    #endregion

    #region draw
    public void Draw(IRenderer r)
    {
        r.FillEllipse(Position, 6, 2, Color.Black.WithAlpha(0.3f));   // a soft shadow under the feet
        bool blinkOff = _hurtTime > 0 && (int)(_hurtTime * 12) % 2 == 0;
        if (!blinkOff) r.Draw(_animator.Frame, Position, flipX: _facingLeft);
    }
    #endregion
}

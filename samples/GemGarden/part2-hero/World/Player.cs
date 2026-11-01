using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Input;

namespace GemGarden.World;

/// <summary>The hero: reads the movement actions, walks inside the screen, and animates.</summary>
public sealed class Player
{
    #region fields
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
    #endregion

    #region update
    public void Update(float dt, InputMap input, RectF area)
    {
        // Keys give unit directions (diagonals normalised); a stick or the touch joystick also gives how far it is pushed.
        var direction = input.Vector("left", "right", "up", "down");
        float speed = input.Down("run") ? RunSpeed : WalkSpeed;
        var moved = _bounds.Offset(direction * speed * dt);

        // Stay inside the area: the next part replaces this with collisions against the garden's hedges.
        _bounds = moved with
        {
            X = Math.Clamp(moved.X, area.Left, area.Right - moved.Width),
            Y = Math.Clamp(moved.Y, area.Top, area.Bottom - moved.Height),
        };

        #region animate
        if (direction.X != 0) _facingLeft = direction.X < 0;
        _animator.Play(direction == Vector2.Zero ? "idle" : "walk");
        _animator.Speed = speed / WalkSpeed;
        _animator.Update(dt);
        #endregion
    }
    #endregion

    #region draw
    public void Draw(IRenderer r)
    {
        r.FillEllipse(Position, 6, 2, Color.Black.WithAlpha(0.3f));   // a soft shadow under the feet
        r.Draw(_animator.Frame, Position, flipX: _facingLeft);
    }
    #endregion
}

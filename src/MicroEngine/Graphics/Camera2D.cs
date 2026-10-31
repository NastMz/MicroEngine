using System.Numerics;

namespace MicroEngine.Graphics;

/// <summary>
/// A 2D camera whose <see cref="Position"/> is the world point at the top-left of the view.
/// Supports smooth following with a dead zone, world bounds and trauma-based shake.
/// </summary>
public sealed class Camera2D
{
    private float _trauma;
    private float _shakeTime;
    private readonly Random _random = new(1234);

    public Vector2 Position { get; set; }
    public float ViewWidth { get; }
    public float ViewHeight { get; }
    /// <summary>Optional world limits; the view is kept inside when the world is larger than the view.</summary>
    public RectF? Bounds { get; set; }
    /// <summary>Per-second follow sharpness; 0 snaps immediately.</summary>
    public float FollowSharpness { get; set; } = 10f;
    /// <summary>Half-size of the dead zone around the view centre in which the target can move freely.</summary>
    public Vector2 DeadZone { get; set; } = Vector2.Zero;
    /// <summary>Maximum shake offset in pixels at full trauma.</summary>
    public float MaxShake { get; set; } = 6f;
    /// <summary>Shake offset applied on top of <see cref="Position"/> when rendering.</summary>
    public Vector2 ShakeOffset { get; private set; }
    /// <summary>Rounds the render offset to whole pixels for crisp pixel art.</summary>
    public bool PixelSnap { get; set; } = true;

    public Camera2D(float viewWidth, float viewHeight) => (ViewWidth, ViewHeight) = (viewWidth, viewHeight);

    public RectF View => new(Position.X, Position.Y, ViewWidth, ViewHeight);
    public Vector2 Center => Position + new Vector2(ViewWidth, ViewHeight) / 2f;

    /// <summary>The translation the renderer should apply (negative position plus shake), snapped if requested.</summary>
    public Vector2 RenderOffset
    {
        get
        {
            var p = -(Position + ShakeOffset);
            return PixelSnap ? new Vector2(MathF.Round(p.X), MathF.Round(p.Y)) : p;
        }
    }

    public void CenterOn(Vector2 target)
    {
        Position = target - new Vector2(ViewWidth, ViewHeight) / 2f;
        Clamp();
    }

    /// <summary>Moves toward keeping <paramref name="target"/> inside the dead zone.</summary>
    public void Follow(Vector2 target, float dt)
    {
        var center = Center;
        var desired = center;
        if (target.X > center.X + DeadZone.X) desired.X = target.X - DeadZone.X;
        else if (target.X < center.X - DeadZone.X) desired.X = target.X + DeadZone.X;
        if (target.Y > center.Y + DeadZone.Y) desired.Y = target.Y - DeadZone.Y;
        else if (target.Y < center.Y - DeadZone.Y) desired.Y = target.Y + DeadZone.Y;
        var next = FollowSharpness <= 0 ? desired : MathX.Damp(center, desired, FollowSharpness, dt);
        Position = next - new Vector2(ViewWidth, ViewHeight) / 2f;
        Clamp();
    }

    public void Clamp()
    {
        if (Bounds is not { } b) return;
        float x = b.Width <= ViewWidth ? b.X + (b.Width - ViewWidth) / 2f : Math.Clamp(Position.X, b.X, b.Right - ViewWidth);
        float y = b.Height <= ViewHeight ? b.Y + (b.Height - ViewHeight) / 2f : Math.Clamp(Position.Y, b.Y, b.Bottom - ViewHeight);
        Position = new Vector2(x, y);
    }

    /// <summary>Adds shake trauma (0..1). Shake strength is trauma squared, decaying over time.</summary>
    public void Shake(float trauma) => _trauma = Math.Clamp(_trauma + trauma, 0f, 1f);

    public void Update(float dt)
    {
        _trauma = MathF.Max(0, _trauma - dt * 1.6f);
        _shakeTime += dt;
        if (_trauma <= 0) { ShakeOffset = Vector2.Zero; return; }
        float power = _trauma * _trauma * MaxShake;
        ShakeOffset = new Vector2((float)(_random.NextDouble() * 2 - 1) * power, (float)(_random.NextDouble() * 2 - 1) * power);
    }

    public Vector2 WorldToScreen(Vector2 world) => world + RenderOffset;
    public Vector2 ScreenToWorld(Vector2 screen) => screen - RenderOffset;
    public bool IsVisible(RectF world, float margin = 0) => View.Inflate(margin + MaxShake).Intersects(world);
}

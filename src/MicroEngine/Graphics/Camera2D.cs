using System.Numerics;

namespace MicroEngine.Graphics;

/// <summary>
/// A 2D camera whose <see cref="Position"/> is the world point at the top-left of the view.
/// Supports smooth following with a dead zone, world bounds and trauma-based shake.
/// </summary>
/// <remarks>
/// The camera has no zoom or rotation: one world pixel is one virtual pixel. Draw the world between
/// <see cref="IRenderer.PushCamera"/> and <see cref="IRenderer.PopCamera"/>, and the interface after the pop.
/// </remarks>
/// <example>
/// <code>
/// var camera = new Camera2D(Game.Width, Game.Height) { Bounds = new RectF(0, 0, 1280, 360), DeadZone = new Vector2(24, 16) };
///
/// // Update:
/// camera.Follow(player.Center, dt);
/// camera.Update(dt);
///
/// // Draw:
/// renderer.PushCamera(camera);
/// DrawWorld(renderer);
/// renderer.PopCamera();
/// </code>
/// </example>
public sealed class Camera2D
{
    private float _trauma;
    private float _shakeTime;
    private readonly Random _random = new(1234);

    /// <summary>The world point at the top-left of the view, in pixels. Setting it directly does not apply <see cref="Bounds"/>; call <see cref="Clamp"/> for that.</summary>
    public Vector2 Position { get; set; }
    /// <summary>Width of the view in pixels, usually the virtual canvas width.</summary>
    public float ViewWidth { get; }
    /// <summary>Height of the view in pixels, usually the virtual canvas height.</summary>
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

    /// <summary>Creates a camera at the world origin.</summary>
    /// <param name="viewWidth">Width of the view in pixels.</param>
    /// <param name="viewHeight">Height of the view in pixels.</param>
    public Camera2D(float viewWidth, float viewHeight) => (ViewWidth, ViewHeight) = (viewWidth, viewHeight);

    /// <summary>The world rectangle the camera shows, without shake.</summary>
    public RectF View => new(Position.X, Position.Y, ViewWidth, ViewHeight);
    /// <summary>The world point at the centre of the view.</summary>
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

    /// <summary>Moves the view so <paramref name="target"/> is at its centre at once, then applies <see cref="Bounds"/>.</summary>
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

    /// <summary>Keeps the view inside <see cref="Bounds"/>; on an axis where the bounds are smaller than the view, centres the bounds instead. Does nothing without bounds.</summary>
    public void Clamp()
    {
        if (Bounds is not { } b) return;
        float x = b.Width <= ViewWidth ? b.X + (b.Width - ViewWidth) / 2f : Math.Clamp(Position.X, b.X, b.Right - ViewWidth);
        float y = b.Height <= ViewHeight ? b.Y + (b.Height - ViewHeight) / 2f : Math.Clamp(Position.Y, b.Y, b.Bottom - ViewHeight);
        Position = new Vector2(x, y);
    }

    /// <summary>Adds shake trauma (0..1). Shake strength is trauma squared, decaying over time.</summary>
    public void Shake(float trauma) => _trauma = Math.Clamp(_trauma + trauma, 0f, 1f);

    /// <summary>Decays shake trauma (fully in about 0.6 seconds from 1) and picks a new <see cref="ShakeOffset"/>. Call once per update.</summary>
    /// <param name="dt">Elapsed time in seconds.</param>
    public void Update(float dt)
    {
        _trauma = MathF.Max(0, _trauma - dt * 1.6f);
        _shakeTime += dt;
        if (_trauma <= 0) { ShakeOffset = Vector2.Zero; return; }
        float power = _trauma * _trauma * MaxShake;
        ShakeOffset = new Vector2((float)(_random.NextDouble() * 2 - 1) * power, (float)(_random.NextDouble() * 2 - 1) * power);
    }

    /// <summary>Converts a world point to virtual-canvas pixels, including shake.</summary>
    public Vector2 WorldToScreen(Vector2 world) => world + RenderOffset;
    /// <summary>Converts a virtual-canvas point (for example a pointer position) to world pixels, including shake.</summary>
    public Vector2 ScreenToWorld(Vector2 screen) => screen - RenderOffset;
    /// <summary>Whether <paramref name="world"/> overlaps the view, grown by <paramref name="margin"/> plus <see cref="MaxShake"/> pixels. Use it to skip drawing off-screen objects.</summary>
    public bool IsVisible(RectF world, float margin = 0) => View.Inflate(margin + MaxShake).Intersects(world);
}

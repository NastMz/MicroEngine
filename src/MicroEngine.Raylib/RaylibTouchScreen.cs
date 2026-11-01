using System.Numerics;
using MicroEngine.Input;
using Rl = Raylib_cs.Raylib;
using RlColor = Raylib_cs.Color;

namespace MicroEngine.Raylib;

/// <summary>
/// Touches for the raylib platform. raylib's own touch state is a per-frame list of points indexed by position (no
/// cancel, no reliable identity across frames), so it is not used: in a browser the page forwards Pointer Events
/// (<see cref="BrowserTouch"/>), with their pointer ids, the page's safe-area insets and its density. A desktop raylib
/// window reports no touches; its controls still lay out for scripted touches.
/// </summary>
public sealed class RaylibTouchScreen : ITouchScreen
{
    internal static readonly TouchFeed Feed = new();
    private readonly RaylibOverlayCanvas _canvas = new();

    /// <inheritdoc/>
    public Vector2 Size => new(Rl.GetRenderWidth(), Rl.GetRenderHeight());

    /// <inheritdoc/>
    public RectF SafeArea
    {
        get
        {
            var size = Size;
            var (l, t, r, b) = Feed.Insets;
            var area = new RectF(l, t, size.X - l - r, size.Y - t - b);
            return area.Width > 0 && area.Height > 0 ? area : new RectF(0, 0, size.X, size.Y);
        }
    }

    /// <inheritdoc/>
    public float PixelsPerDp => Feed.PixelsPerDp > 0 ? Feed.PixelsPerDp : OperatingSystem.IsBrowser() ? 1 : Math.Max(1, Rl.GetWindowScaleDPI().X);
    /// <inheritdoc/>
    public bool TouchFirst => Feed.TouchFirst;
    /// <inheritdoc/>
    public ITouchOverlay? Overlay { get; set; }
    /// <inheritdoc/>
    public bool CanVibrate => Feed.Vibrator is not null && Feed.TouchFirst;
    /// <inheritdoc/>
    public bool Vibrate(float strength, int milliseconds) => CanVibrate && Feed.Vibrator!(Math.Max(1, milliseconds));
    /// <inheritdoc/>
    public void ReadTouches(List<TouchEvent> events) => Feed.Drain(events);
    internal void DrawOverlay() => Overlay?.Draw(_canvas);
}

/// <summary>Touch events and screen facts handed to the raylib platform by its host (the browser page).</summary>
internal sealed class TouchFeed
{
    private readonly List<TouchEvent> _pending = [];
    private readonly Lock _lock = new();
    public (float Left, float Top, float Right, float Bottom) Insets;
    public float PixelsPerDp;
    public bool TouchFirst;
    public Func<int, bool>? Vibrator;

    public void Push(TouchEvent e)
    {
        lock (_lock) _pending.Add(e);
    }

    public void Drain(List<TouchEvent> into)
    {
        lock (_lock)
        {
            into.AddRange(_pending);
            _pending.Clear();
        }
    }

    public void Clear()
    {
        lock (_lock) _pending.Clear();
    }
}

/// <summary>Overlay drawing with raylib's shapes and default font, in render pixels.</summary>
internal sealed class RaylibOverlayCanvas : IOverlayCanvas
{
    private static RlColor C(Color c) => new(c.R, c.G, c.B, c.A);

    public void FillCircle(Vector2 center, float radius, Color color)
    {
        if (color.A > 0 && radius > 0) Rl.DrawCircleSector(center, radius, 0, 360, 40, C(color));
    }

    public void Ring(Vector2 center, float radius, float thickness, Color color)
    {
        if (color.A > 0 && radius > 0) Rl.DrawRing(center, Math.Max(0, radius - thickness), radius, 0, 360, 40, C(color));
    }

    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        if (color.A == 0) return;
        // raylib only draws counter-clockwise triangles.
        float cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        if (cross > 0) (b, c) = (c, b);
        Rl.DrawTriangle(a, b, c, C(color));
    }

    public void FillRect(RectF rect, Color color)
    {
        if (color.A > 0) Rl.DrawRectangleRec(new Raylib_cs.Rectangle(rect.X, rect.Y, rect.Width, rect.Height), C(color));
    }

    public void Text(string text, Vector2 center, float height, Color color)
    {
        if (string.IsNullOrEmpty(text) || color.A == 0) return;
        // raylib's default font is 10 pixels tall at size 10; whole multiples (never above the asked height) keep it crisp.
        int size = Math.Max(10, (int)MathF.Floor(height / 10) * 10);
        int width = Rl.MeasureText(text, size);
        Rl.DrawText(text, (int)(center.X - width / 2f), (int)(center.Y - size / 2f), size, C(color));
    }
}

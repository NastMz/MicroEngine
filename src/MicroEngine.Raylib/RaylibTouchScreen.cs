using System.Numerics;
using MicroEngine.Input;
using Rl = Raylib_cs.Raylib;
using RlColor = Raylib_cs.Color;

namespace MicroEngine.Raylib;

/// <summary>
/// Touches for the raylib platform. raylib's own touch state is a per-frame list of points indexed by position (no
/// cancel, no reliable identity across frames), so it is not used: a raylib window reports no touches; its controls
/// still lay out for scripted touches.
/// </summary>
public sealed class RaylibTouchScreen : ITouchScreen
{
    private readonly TouchFeed Feed;
    private readonly RaylibOverlayCanvas _canvas = new();

    /// <summary>A touch screen with no page feeding it (a desktop window): no touches, default insets and density.</summary>
    public RaylibTouchScreen() : this(new TouchFeed()) { }

    internal RaylibTouchScreen(TouchFeed feed) => Feed = feed;

    /// <summary>Drops touches not yet read (a new run on the same page starts with none).</summary>
    internal void Clear() => Feed.Clear();

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
    private readonly Dictionary<MicroEngine.Graphics.PixelImage, Raylib_cs.Texture2D> _images = new(ReferenceEqualityComparer.Instance);
    private static RlColor C(Color c) => new(c.R, c.G, c.B, c.A);

    public void Image(MicroEngine.Graphics.PixelImage image, RectF destination, Color tint)
    {
        if (tint.A == 0 || destination.Width <= 0 || destination.Height <= 0) return;
        if (!_images.TryGetValue(image, out var texture))
        {
            var blank = Rl.GenImageColor(image.Width, image.Height, new RlColor(0, 0, 0, 0));
            texture = Rl.LoadTextureFromImage(blank);
            Rl.UnloadImage(blank);
            Rl.UpdateTexture(texture, image.ToRgba());
            Rl.SetTextureFilter(texture, Raylib_cs.TextureFilter.Point);
            _images[image] = texture;
        }
        Rl.DrawTexturePro(texture, new Raylib_cs.Rectangle(0, 0, image.Width, image.Height),
            new Raylib_cs.Rectangle(destination.X, destination.Y, destination.Width, destination.Height), Vector2.Zero, 0, C(tint));
    }

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

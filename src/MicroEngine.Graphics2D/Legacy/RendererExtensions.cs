using System.Numerics;

namespace MicroEngine.Graphics;

/// <summary>Drawing helpers built on <see cref="IRenderer"/>: sprites, outlines, ellipses, gradients and panels.</summary>
public static class RendererExtensions
{
    /// <summary>Draws a sprite so its <see cref="Sprite.Origin"/> lands on <paramref name="position"/>.</summary>
    /// <param name="r">The renderer.</param>
    /// <param name="sprite">The sprite.</param>
    /// <param name="position">Where the sprite's origin goes, in virtual (or camera world) pixels.</param>
    /// <param name="tint">Colour multiplier; null for white (unchanged).</param>
    /// <param name="flipX">Mirrors the sprite horizontally; the origin is mirrored too, so the sprite turns around its anchor.</param>
    public static void Draw(this IRenderer r, Sprite sprite, Vector2 position, Color? tint = null, bool flipX = false)
        => r.Draw(sprite.Texture, sprite.Source, position - (flipX ? new Vector2(sprite.Source.Width - sprite.Origin.X, sprite.Origin.Y) : sprite.Origin),
            tint ?? Color.White, new DrawOptions(FlipX: flipX));

    /// <summary>
    /// Draws a sprite so its origin lands on <paramref name="pivot"/>, rotating and scaling around that
    /// pivot. Use it for squash/stretch, spinning items and anything that must stay anchored at the feet.
    /// </summary>
    public static void DrawPivoted(this IRenderer r, Sprite sprite, Vector2 pivot, float rotation = 0f, Vector2? scale = null, Color? tint = null, bool flipX = false)
    {
        var s = scale ?? Vector2.One;
        var origin = flipX ? new Vector2(sprite.Source.Width - sprite.Origin.X, sprite.Origin.Y) : sprite.Origin;
        r.Draw(sprite.Texture, sprite.Source, pivot - origin * s, tint ?? Color.White, new DrawOptions(FlipX: flipX, Rotation: rotation, Origin: origin, Scale: s));
    }

    /// <summary>Filled ellipse built from horizontal spans; handy for soft drop shadows.</summary>
    public static void FillEllipse(this IRenderer r, Vector2 center, float radiusX, float radiusY, Color color)
    {
        if (radiusX <= 0 || radiusY <= 0) return;
        for (float y = -radiusY + 0.5f; y < radiusY; y += 1f)
        {
            float half = radiusX * MathF.Sqrt(MathF.Max(0, 1 - y * y / (radiusY * radiusY)));
            if (half < 0.5f) continue;
            r.FillRect(new RectF(MathF.Round(center.X - half), MathF.Floor(center.Y + y), MathF.Round(half * 2), 1), color);
        }
    }

    /// <summary>Draws a whole texture with its top-left at <paramref name="position"/>.</summary>
    public static void Draw(this IRenderer r, Texture texture, Vector2 position, Color? tint = null)
        => r.Draw(texture, texture.Bounds, position, tint ?? Color.White);

    /// <summary>Draws the outline of <paramref name="rect"/>, <paramref name="thickness"/> pixels wide, inside the rectangle.</summary>
    public static void DrawRect(this IRenderer r, RectF rect, Color color, float thickness = 1f)
    {
        r.FillRect(new RectF(rect.X, rect.Y, rect.Width, thickness), color);
        r.FillRect(new RectF(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        r.FillRect(new RectF(rect.X, rect.Y + thickness, thickness, rect.Height - thickness * 2), color);
        r.FillRect(new RectF(rect.Right - thickness, rect.Y + thickness, thickness, rect.Height - thickness * 2), color);
    }

    /// <summary>Vertical gradient approximated with horizontal bands (cheap and pixel-crisp).</summary>
    public static void FillGradient(this IRenderer r, RectF rect, Color top, Color bottom, int bands = 0)
    {
        int steps = bands > 0 ? bands : Math.Max(1, (int)rect.Height);
        float h = rect.Height / steps;
        for (int i = 0; i < steps; i++)
            r.FillRect(new RectF(rect.X, MathF.Floor(rect.Y + i * h), rect.Width, MathF.Ceiling(h) + 1), Color.Lerp(top, bottom, steps == 1 ? 0 : i / (float)(steps - 1)));
    }

    /// <summary>Draws a rounded "pixel" panel: border, fill and an inner highlight line.</summary>
    public static void Panel(this IRenderer r, RectF rect, PanelStyle style)
    {
        var (x, y, w, h) = (rect.X, rect.Y, rect.Width, rect.Height);
        if (style.Shadow.A > 0) r.FillRect(new RectF(x + 2, y + 2, w, h), style.Shadow);
        r.FillRect(new RectF(x + 1, y, w - 2, h), style.Border);
        r.FillRect(new RectF(x, y + 1, w, h - 2), style.Border);
        r.FillRect(new RectF(x + 1, y + 1, w - 2, h - 2), style.Fill);
        if (style.Highlight.A > 0) r.FillRect(new RectF(x + 2, y + 2, w - 4, 1), style.Highlight);
        if (style.InnerBorder.A > 0) r.DrawRect(new RectF(x + 2, y + 2, w - 4, h - 4), style.InnerBorder);
    }
}

/// <summary>Colours of a panel drawn with <see cref="RendererExtensions.Panel"/>.</summary>
/// <param name="Fill">Inside colour.</param>
/// <param name="Border">One-pixel outer border with cut corners.</param>
/// <param name="Highlight">A one-pixel line along the top inside edge; transparent (the default) draws none.</param>
/// <param name="Shadow">A drop shadow offset by 2 pixels; transparent draws none.</param>
/// <param name="InnerBorder">A second outline 2 pixels inside the border; transparent draws none.</param>
public readonly record struct PanelStyle(Color Fill, Color Border, Color Highlight = default, Color Shadow = default, Color InnerBorder = default);

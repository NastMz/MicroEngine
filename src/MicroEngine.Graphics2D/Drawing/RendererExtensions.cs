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

    /// <summary>
    /// Draws <paramref name="slice"/> over <paramref name="destination"/>: its four corners at their size (times
    /// <see cref="NineSlice.Scale"/>), its edges stretched along the sides and its centre stretched to fill the rest. A
    /// destination smaller than the corners shrinks them to fit.
    /// </summary>
    /// <param name="r">The renderer.</param>
    /// <param name="slice">The image and its borders.</param>
    /// <param name="destination">Where it goes, in the renderer's pixels or UI units.</param>
    /// <param name="tint">Colour multiplier; null for white (unchanged).</param>
    public static void DrawNineSlice(this IRenderer r, NineSlice slice, RectF destination, Color? tint = null)
    {
        ArgumentNullException.ThrowIfNull(slice.Texture);
        var color = tint ?? Color.White;
        var s = slice.Source;
        float l = slice.Left * slice.Scale, t = slice.Top * slice.Scale, rr = slice.Right * slice.Scale, b = slice.Bottom * slice.Scale;
        // Corners that do not fit shrink together, keeping their proportions.
        if (l + rr > destination.Width && l + rr > 0) { float k = destination.Width / (l + rr); l *= k; rr *= k; }
        if (t + b > destination.Height && t + b > 0) { float k = destination.Height / (t + b); t *= k; b *= k; }
        float[] srcX = [s.X, s.X + slice.Left, s.Right - slice.Right, s.Right];
        float[] srcY = [s.Y, s.Y + slice.Top, s.Bottom - slice.Bottom, s.Bottom];
        float[] dstX = [destination.X, destination.X + l, destination.Right - rr, destination.Right];
        float[] dstY = [destination.Y, destination.Y + t, destination.Bottom - b, destination.Bottom];
        for (int row = 0; row < 3; row++)
            for (int column = 0; column < 3; column++)
            {
                var source = new RectF(srcX[column], srcY[row], srcX[column + 1] - srcX[column], srcY[row + 1] - srcY[row]);
                var target = new RectF(dstX[column], dstY[row], dstX[column + 1] - dstX[column], dstY[row + 1] - dstY[row]);
                if (source.Width <= 0 || source.Height <= 0 || target.Width <= 0 || target.Height <= 0) continue;
                r.DrawStretched(slice.Texture, source, target, color);
            }
    }

    /// <summary>
    /// Draws a panel: with <see cref="PanelStyle.Image"/>, that image as a nine-slice tinted by
    /// <see cref="PanelStyle.ImageTint"/> (its shadow, when set, is the image in the shadow colour 2 pixels down and
    /// right); otherwise a rounded "pixel" box of border, fill and an inner highlight line.
    /// </summary>
    public static void Panel(this IRenderer r, RectF rect, PanelStyle style)
    {
        if (style.Image is { } image)
        {
            if (style.Shadow.A > 0) r.DrawNineSlice(image, new RectF(rect.X + 2, rect.Y + 2, rect.Width, rect.Height), style.Shadow);
            r.DrawNineSlice(image, rect, style.ImageTint ?? Color.White);
            return;
        }
        var (x, y, w, h) = (rect.X, rect.Y, rect.Width, rect.Height);
        if (style.Shadow.A > 0) r.FillRect(new RectF(x + 2, y + 2, w, h), style.Shadow);
        r.FillRect(new RectF(x + 1, y, w - 2, h), style.Border);
        r.FillRect(new RectF(x, y + 1, w, h - 2), style.Border);
        r.FillRect(new RectF(x + 1, y + 1, w - 2, h - 2), style.Fill);
        if (style.Highlight.A > 0) r.FillRect(new RectF(x + 2, y + 2, w - 4, 1), style.Highlight);
        if (style.InnerBorder.A > 0) r.DrawRect(new RectF(x + 2, y + 2, w - 4, h - 4), style.InnerBorder);
    }
}

/// <summary>
/// The look of a panel drawn with <see cref="RendererExtensions.Panel"/>: a box of colours, or an image of the game's
/// own (<see cref="Image"/>).
/// </summary>
/// <param name="Fill">Inside colour.</param>
/// <param name="Border">One-pixel outer border with cut corners.</param>
/// <param name="Highlight">A one-pixel line along the top inside edge; transparent (the default) draws none.</param>
/// <param name="Shadow">A drop shadow offset by 2 pixels; transparent draws none.</param>
/// <param name="InnerBorder">A second outline 2 pixels inside the border; transparent draws none.</param>
public readonly record struct PanelStyle(Color Fill, Color Border, Color Highlight = default, Color Shadow = default, Color InnerBorder = default)
{
    /// <summary>
    /// An image drawn as the panel instead of the box of colours, stretched as a nine-slice (null, the default, draws
    /// the box). <see cref="Fill"/>, <see cref="Border"/>, <see cref="Highlight"/> and <see cref="InnerBorder"/> are
    /// then not drawn; <see cref="Shadow"/> still is, in the image's shape.
    /// </summary>
    public NineSlice? Image { get; init; }

    /// <summary>The colour <see cref="Image"/> is multiplied by; null (the default) draws it unchanged.</summary>
    public Color? ImageTint { get; init; }
}

/// <summary>
/// An image that stretches to any size without distorting its frame: the part of <see cref="Source"/> within
/// <see cref="Left"/>, <see cref="Top"/>, <see cref="Right"/> and <see cref="Bottom"/> pixels of its sides is the
/// border, drawn at its size at the corners and stretched along the edges; the rest is the centre, stretched to fill.
/// Draw one with <see cref="RendererExtensions.DrawNineSlice"/>, or set it as a <see cref="PanelStyle.Image"/>.
/// </summary>
/// <param name="Texture">The texture that holds the image.</param>
/// <param name="Source">The image's rectangle in the texture, in pixels.</param>
/// <param name="Left">Width of the left border, in source pixels.</param>
/// <param name="Top">Height of the top border, in source pixels.</param>
/// <param name="Right">Width of the right border, in source pixels.</param>
/// <param name="Bottom">Height of the bottom border, in source pixels.</param>
public readonly record struct NineSlice(Texture Texture, RectF Source, float Left, float Top, float Right, float Bottom)
{
    /// <summary>An image with a border of <paramref name="border"/> pixels on every side.</summary>
    public NineSlice(Texture texture, RectF source, float border) : this(texture, source, border, border, border, border) { }

    /// <summary>How many destination units one source pixel of the border takes (1 by default); 2 draws the frame twice as thick.</summary>
    public float Scale { get; init; } = 1;
}

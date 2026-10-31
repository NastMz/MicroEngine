using MicroEngine;
using MicroEngine.Graphics;

namespace MeadowRun.Art;

/// <summary>
/// Shading primitives for procedural pixel art. Light always comes from the top-left so
/// creatures, tiles and props share one consistent lighting model.
/// </summary>
internal static class Brush
{
    /// <summary>Fills an ellipse shaded with a ramp (dark..light), lit from the top-left, with dithered steps.</summary>
    public static void Ellipse(PixelImage img, float cx, float cy, float rx, float ry, IReadOnlyList<Color> ramp, float bias = 0f)
    {
        for (int y = (int)MathF.Floor(cy - ry); y <= (int)MathF.Ceiling(cy + ry); y++)
        for (int x = (int)MathF.Floor(cx - rx); x <= (int)MathF.Ceiling(cx + rx); x++)
        {
            float nx = (x + 0.5f - cx) / rx, ny = (y + 0.5f - cy) / ry;
            float d = nx * nx + ny * ny;
            if (d > 1f) continue;
            float light = 0.62f - 0.32f * (nx * 0.6f + ny * 0.8f) - 0.18f * d + bias;
            img[x, y] = MicroEngine.Graphics.Art.Ramp(ramp, light, x, y);
        }
    }

    /// <summary>Flat ellipse (no shading).</summary>
    public static void Flat(PixelImage img, float cx, float cy, float rx, float ry, Color color) => img.FillEllipse(cx, cy, rx, ry, color);

    /// <summary>Rectangle shaded top (light) to bottom (dark) with a lit left edge.</summary>
    public static void Box(PixelImage img, int x, int y, int w, int h, IReadOnlyList<Color> ramp, float top = 0.85f, float bottom = 0.3f)
    {
        for (int j = 0; j < h; j++)
        for (int i = 0; i < w; i++)
        {
            float t = h <= 1 ? 0 : j / (float)(h - 1);
            float v = MathX.Lerp(top, bottom, t) + (i == 0 ? 0.1f : 0) - (i == w - 1 ? 0.12f : 0);
            img[x + i, y + j] = MicroEngine.Graphics.Art.Ramp(ramp, v, x + i, y + j);
        }
    }

    /// <summary>Darkens every opaque pixel along the bottom-right silhouette edge (cheap rim shadow).</summary>
    public static void RimShade(PixelImage img, Color shade)
    {
        var copy = img.Clone();
        for (int y = 0; y < img.Height; y++)
        for (int x = 0; x < img.Width; x++)
        {
            if (copy[x, y].A == 0) continue;
            if (copy[x + 1, y].A == 0 || copy[x, y + 1].A == 0) img[x, y] = Color.Lerp(copy[x, y], shade, 0.45f);
        }
    }

    /// <summary>Outlines only against transparency using a darker version of the neighbour (selective outline).</summary>
    public static void SelOutline(PixelImage img, Color ink)
    {
        var copy = img.Clone();
        for (int y = 0; y < img.Height; y++)
        for (int x = 0; x < img.Width; x++)
        {
            if (copy[x, y].A != 0) continue;
            Color? n = copy[x - 1, y].A > 0 ? copy[x - 1, y] : copy[x + 1, y].A > 0 ? copy[x + 1, y]
                : copy[x, y - 1].A > 0 ? copy[x, y - 1] : copy[x, y + 1].A > 0 ? copy[x, y + 1] : null;
            if (n is { } c) img[x, y] = Color.Lerp(c, ink, 0.78f);
        }
    }

    public static void Px(PixelImage img, int x, int y, Color c) => img[x, y] = c;

    /// <summary>Thick-ish line by stamping single pixels.</summary>
    public static void Line(PixelImage img, int x0, int y0, int x1, int y1, Color c) => img.Line(x0, y0, x1, y1, c);

    /// <summary>Recolours an image through a colour map (palette swap for forms and variants).</summary>
    public static PixelImage Swap(PixelImage source, IReadOnlyDictionary<Color, Color> map)
    {
        var copy = source.Clone();
        copy.Map((_, _, c) => map.TryGetValue(c, out var to) ? to : c);
        return copy;
    }
}

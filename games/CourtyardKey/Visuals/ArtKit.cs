using MicroEngine;
using MicroEngine.Graphics;

namespace CourtyardKey.Visuals;

/// <summary>Game-specific procedural pixel-art helpers (noise fills, shaded blobs, glows).</summary>
public static class ArtKit
{
    /// <summary>Fills a rectangle with dithered value noise across a colour ramp.</summary>
    public static void Noise(PixelImage img, int x0, int y0, int w, int h, IReadOnlyList<Color> ramp, float cell, int seed, float lo = 0f, float hi = 1f)
    {
        for (int y = y0; y < y0 + h; y++)
            for (int x = x0; x < x0 + w; x++)
            {
                float n = Art.ValueNoise(x, y, cell, seed) * 0.7f + Art.Hash(x, y, seed + 9) * 0.3f;
                img[x, y] = Art.Ramp(ramp, lo + (hi - lo) * n, x, y);
            }
    }

    /// <summary>A lit sphere-ish blob: brighter toward the top-left light, darker at the rim.</summary>
    public static void Blob(PixelImage img, float cx, float cy, float rx, float ry, IReadOnlyList<Color> ramp, int seed, float speckle = 0.15f)
    {
        for (int y = (int)(cy - ry - 1); y <= (int)(cy + ry + 1); y++)
            for (int x = (int)(cx - rx - 1); x <= (int)(cx + rx + 1); x++)
            {
                float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                float d = dx * dx + dy * dy;
                if (d > 1f) continue;
                float light = 0.62f - dx * 0.28f - dy * 0.34f - d * 0.25f;
                light += (Art.Hash(x, y, seed) - 0.5f) * speckle;
                img[x, y] = Art.Ramp(ramp, Math.Clamp(light, 0, 1), x, y);
            }
    }

    /// <summary>A soft drop shadow ellipse.</summary>
    public static void Shadow(PixelImage img, float cx, float cy, float rx, float ry, byte alpha = 90)
        => img.FillEllipse(cx, cy, rx, ry, Pal.Ink.WithAlpha(alpha));

    /// <summary>Selective outline: darkens transparent pixels next to the shape with <paramref name="color"/>.</summary>
    public static PixelImage Outlined(PixelImage img, Color color) => img.Outline(color);

    /// <summary>
    /// Rounds a flat-coloured figure with the world's light (upper left): colour pixels just inside the
    /// <paramref name="ink"/> outline are lifted on the upper-left edges and shaded on the lower-right ones.
    /// </summary>
    public static PixelImage Volume(PixelImage img, Color ink, float light = 0.14f, float shade = 0.22f)
    {
        var src = img.Clone();
        bool Edge(int x, int y) => !src.InBounds(x, y) || src[x, y].A == 0 || src[x, y] == ink;
        for (int y = 0; y < src.Height; y++)
            for (int x = 0; x < src.Width; x++)
            {
                var c = src[x, y];
                if (c.A == 0 || c == ink) continue;
                bool lit = Edge(x - 1, y) || Edge(x, y - 1), dark = Edge(x + 1, y) || Edge(x, y + 1);
                if (dark && !lit) img[x, y] = Color.Lerp(c, ink, shade);
                else if (lit && !dark) img[x, y] = Color.Lerp(c, Color.White, light);
            }
        return img;
    }

    /// <summary>Radial glow image (white with alpha falloff), used additively for lights.</summary>
    public static PixelImage Glow(int radius, Color color, float power = 1.6f)
    {
        int size = radius * 2;
        var img = new PixelImage(size, size);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - radius, dy = y + 0.5f - radius;
                float d = MathF.Sqrt(dx * dx + dy * dy) / radius;
                if (d >= 1) continue;
                float a = MathF.Pow(1 - d, power);
                // Quantize to a few bands so the glow keeps a pixel-art feel.
                a = MathF.Round(a * 6) / 6f;
                img[x, y] = color.WithAlpha(a);
            }
        return img;
    }

    public static PixelImage Map(string palette, params string[] rows) => PixelImage.FromMap(ParsePalette(palette), rows);

    /// <summary>Palette string "a=#rrggbb b=#rrggbb ..." (space separated).</summary>
    public static Dictionary<char, Color> ParsePalette(string spec)
        => Art.Palette(spec.Split(' ', StringSplitOptions.RemoveEmptyEntries));

    public static string Hex(Color c) => $"#{c.R:x2}{c.G:x2}{c.B:x2}";
}

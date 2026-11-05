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

    /// <summary>Value noise that tiles every <paramref name="period"/> pixels (smoothstep between hashed lattice points).</summary>
    public static float PeriodicNoise(int x, int y, int cell, int period, int seed)
    {
        int n = period / cell;
        int ix = x / cell, iy = y / cell;
        float fx = (x % cell + 0.5f) / cell, fy = (y % cell + 0.5f) / cell;
        float L(int i, int j) => Art.Hash(((i % n) + n) % n, ((j % n) + n) % n, seed);
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        float a = L(ix, iy) + (L(ix + 1, iy) - L(ix, iy)) * fx;
        float c = L(ix, iy + 1) + (L(ix + 1, iy + 1) - L(ix, iy + 1)) * fx;
        return a + (c - a) * fy;
    }

    /// <summary>Flat colour bands with a narrow dithered seam between steps (no all-over checkerboard).</summary>
    public static Color Band(IReadOnlyList<Color> ramp, float t, int x, int y, float seam = 0.18f)
    {
        float s = Math.Clamp(t, 0, 0.9999f) * (ramp.Count - 1);
        int i = (int)s;
        float f = s - i;
        if (i + 1 < ramp.Count && f > 1 - seam && Art.Dither(x, y, (f - (1 - seam)) / seam)) return ramp[i + 1];
        return ramp[i];
    }

    /// <summary>Blends <paramref name="c"/> over a pixel (or sets it translucent on an empty pixel).</summary>
    public static void Soft(PixelImage img, int x, int y, Color c, float a)
    {
        if (!img.InBounds(x, y)) return;
        var under = img[x, y];
        img[x, y] = under.A == 0 ? c.WithAlpha(a) : Color.Lerp(under, c, a);
    }

    /// <summary>
    /// A cast shadow in <see cref="Pal.CastShadow"/>: a dense core and a dithered penumbra. Callers place it to the lower
    /// right of the object, away from the upper-left light.
    /// </summary>
    public static void CastShadow(PixelImage img, float cx, float cy, float rx, float ry, float alpha)
    {
        for (int y = (int)(cy - ry - 1); y <= cy + ry + 1; y++)
            for (int x = (int)(cx - rx - 1); x <= cx + rx + 1; x++)
            {
                float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                float d = dx * dx + dy * dy;
                if (d > 1 || !img.InBounds(x, y)) continue;
                if (d > 0.62f && !Art.Dither(x, y, 0.5f)) continue;
                Soft(img, x, y, Pal.CastShadow, alpha * (d > 0.62f ? 0.7f : 1f));
            }
    }

    /// <summary>Copies the opaque pixels of <paramref name="src"/> over <paramref name="dst"/> (same size).</summary>
    public static void Over(PixelImage dst, PixelImage src)
    {
        for (int y = 0; y < src.Height; y++)
            for (int x = 0; x < src.Width; x++)
                if (src[x, y].A > 0) dst[x, y] = src[x, y];
    }

    /// <summary>
    /// Selective, coloured outline: every <paramref name="ink"/> pixel takes the hue of the darkest material it borders.
    /// Silhouette edges facing the light (up, left) stay softer, edges facing away (down, right) stay near-black, and
    /// interior lines are the lightest so the parts read as one shape.
    /// </summary>
    public static PixelImage ColorOutline(PixelImage img, Color ink)
    {
        var src = img.Clone();
        bool Empty(int x, int y) => !src.InBounds(x, y) || src[x, y].A == 0;
        static float Lum(Color c) => c.R * 0.3f + c.G * 0.59f + c.B * 0.11f;
        ReadOnlySpan<(int, int)> around = [(1, 0), (-1, 0), (0, 1), (0, -1)];
        for (int y = 0; y < src.Height; y++)
            for (int x = 0; x < src.Width; x++)
            {
                if (src[x, y] != ink) continue;
                Color? best = null;
                foreach (var (dx, dy) in around)
                {
                    int nx = x + dx, ny = y + dy;
                    if (Empty(nx, ny) || src[nx, ny] == ink) continue;
                    var c = src[nx, ny];
                    if (best is null || Lum(c) < Lum(best.Value)) best = c;
                }
                if (best is null) continue;
                bool outside = Empty(x - 1, y) || Empty(x + 1, y) || Empty(x, y - 1) || Empty(x, y + 1);
                bool lit = Empty(x - 1, y) || Empty(x, y - 1);
                bool away = Empty(x + 1, y) || Empty(x, y + 1);
                float k = !outside ? 0.58f : away && !lit ? 0.86f : lit && !away ? 0.66f : 0.78f;
                img[x, y] = Color.Lerp(best.Value, ink, k);
            }
        return img;
    }

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

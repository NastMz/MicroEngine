namespace MicroEngine.Graphics;

/// <summary>
/// Deterministic helpers for procedural pixel art: hash noise, ordered dithering and palette
/// ramps. Every sample game builds its sprites and tiles from these at load time.
/// </summary>
public static class Art
{
    /// <summary>Stable pseudo-random value in 0..1 for integer coordinates and a seed.</summary>
    public static float Hash(int x, int y, int seed = 0)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }

    /// <summary>Smooth value noise in 0..1 with the given cell size.</summary>
    public static float ValueNoise(float x, float y, float cell, int seed = 0)
    {
        float fx = x / cell, fy = y / cell;
        int ix = (int)MathF.Floor(fx), iy = (int)MathF.Floor(fy);
        float tx = fx - ix, ty = fy - iy;
        tx = tx * tx * (3 - 2 * tx);
        ty = ty * ty * (3 - 2 * ty);
        float a = Hash(ix, iy, seed), b = Hash(ix + 1, iy, seed), c = Hash(ix, iy + 1, seed), d = Hash(ix + 1, iy + 1, seed);
        return MathX.Lerp(MathX.Lerp(a, b, tx), MathX.Lerp(c, d, tx), ty);
    }

    private static readonly int[,] Bayer4 = { { 0, 8, 2, 10 }, { 12, 4, 14, 6 }, { 3, 11, 1, 9 }, { 15, 7, 13, 5 } };

    /// <summary>Ordered (Bayer 4x4) dither: true when <paramref name="level"/> (0..1) exceeds the threshold at x,y.</summary>
    public static bool Dither(int x, int y, float level) => level * 16f > Bayer4[y & 3, x & 3] + 0.5f;

    /// <summary>Picks a ramp entry for a 0..1 value with dithered transitions between steps.</summary>
    public static Color Ramp(IReadOnlyList<Color> ramp, float value, int x, int y)
    {
        ArgumentNullException.ThrowIfNull(ramp);
        float scaled = Math.Clamp(value, 0, 0.9999f) * (ramp.Count - 1);
        int i = (int)scaled;
        return i + 1 < ramp.Count && Dither(x, y, scaled - i) ? ramp[i + 1] : ramp[i];
    }

    /// <summary>Builds a palette dictionary from "char=#hex" pairs, e.g. Palette("a=#000000", "b=#ffffff").</summary>
    public static Dictionary<char, Color> Palette(params string[] entries)
    {
        var palette = new Dictionary<char, Color>();
        foreach (var entry in entries)
        {
            if (entry.Length < 3 || entry[1] != '=') throw new FormatException($"Palette entry '{entry}' must look like 'k=#rrggbb'.");
            palette[entry[0]] = Color.Hex(entry[2..]);
        }
        return palette;
    }
}

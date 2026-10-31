using MeadowRun.Content;
using MicroEngine;
using MicroEngine.Graphics;
using Gfx = MicroEngine.Graphics.Art;

namespace MeadowRun.Art;

/// <summary>One horizontally tiling parallax strip. Bottom is anchored to <see cref="Bottom"/> (screen y at the lowest camera).</summary>
internal sealed record BackdropLayer(string Sprite, float FactorX, float FactorY, float Bottom, float Drift = 0, bool Additive = false);

/// <summary>Generates every backdrop image. Strips are seamless: all shapes are periodic in the strip width.</summary>
internal static class BackdropArt
{
    public const int StripWidth = 384;

    public static IReadOnlyList<BackdropLayer> Layers(Biome biome) => biome switch
    {
        Biome.Meadow =>
        [
            new("bg_meadow_clouds", 0.08f, 0.02f, 92, Drift: 4),
            new("bg_meadow_far", 0.18f, 0.08f, 150),
            new("bg_meadow_mid", 0.38f, 0.18f, 168),
            new("bg_meadow_near", 0.62f, 0.3f, 192),
        ],
        Biome.Caves =>
        [
            new("bg_caves_far", 0.15f, 0.1f, 186),
            new("bg_caves_mid", 0.35f, 0.2f, 190),
            new("bg_caves_glow", 0.5f, 0.3f, 176, Additive: true),
        ],
        Biome.Sky =>
        [
            new("bg_sky_far", 0.1f, 0.05f, 186),
            new("bg_sky_mid", 0.3f, 0.15f, 172, Drift: 3),
            new("bg_sky_near", 0.55f, 0.3f, 200, Drift: 6),
        ],
        _ =>
        [
            new("bg_castle_far", 0.15f, 0.08f, 170),
        ],
    };

    public static IEnumerable<(string Name, PixelImage Image)> All()
    {
        foreach (var biome in Enum.GetValues<Biome>()) yield return ("sky_" + biome.ToString().ToLowerInvariant(), Sky(biome));
        yield return ("bg_meadow_clouds", Clouds(70, [Pal.Air, Pal.Cloud, Pal.Cream, Pal.Cream], 11, 5));
        yield return ("bg_meadow_far", Hills(90, [Pal.Sky, Pal.Air, Pal.Cloud, Pal.Cloud], 3, 0.55f, 0, peaks: true));
        yield return ("bg_meadow_mid", TreeHills());
        yield return ("bg_meadow_near", Bushes(48, [Pal.Pine, Pal.Fern, Pal.Leaf, Pal.Lime]));
        yield return ("bg_caves_far", CaveColumns(180, [Pal.Ink, Pal.Night, Pal.Plum, Pal.Slate], 3));
        yield return ("bg_caves_mid", CaveColumns(180, [Pal.Ink, Pal.Ink, Pal.Night, Pal.Plum], 9));
        yield return ("bg_caves_glow", CrystalGlow());
        yield return ("bg_sky_far", CloudSea());
        yield return ("bg_sky_mid", Clouds(90, [Pal.Sky, Pal.Air, Pal.Cloud, Pal.Cloud], 7, 9));
        yield return ("bg_sky_near", Clouds(60, [Pal.Cloud, Pal.Cream, Pal.Cream, Pal.Cream], 5, 13));
        yield return ("bg_castle_far", CastleTowers());
    }

    private static float Periodic(float x, int seed, params (int K, float A)[] waves)
    {
        float v = 0;
        for (int i = 0; i < waves.Length; i++)
            v += MathF.Sin(x / StripWidth * MathF.Tau * waves[i].K + seed * 1.7f + i * 2.3f) * waves[i].A;
        return v;
    }

    private static PixelImage Sky(Biome biome)
    {
        var img = new PixelImage(320, 180);
        Color[] ramp = biome switch
        {
            Biome.Meadow => [Pal.Ocean, Pal.Sky, Pal.Air, Pal.Cloud],
            Biome.Caves => [Pal.Ink, Pal.Ink, Pal.Night, Pal.Plum],
            Biome.Sky => [Pal.Ocean, Pal.Sky, Pal.Air, Pal.Butter],
            _ => [Pal.Ink, Pal.Bark, Pal.Wine, Pal.Rust],
        };
        for (int y = 0; y < 180; y++)
        for (int x = 0; x < 320; x++)
            img[x, y] = Gfx.Ramp(ramp, y / 180f * 0.95f + 0.02f, x, y);
        if (biome is Biome.Meadow or Biome.Sky)
        {
            float sx = biome == Biome.Meadow ? 250 : 70, sy = biome == Biome.Meadow ? 38 : 120;
            for (int y = 0; y < 180; y++)
            for (int x = 0; x < 320; x++)
            {
                float d = MathF.Sqrt((x - sx) * (x - sx) + (y - sy) * (y - sy));
                if (d < 13) img[x, y] = d < 11 ? Pal.Cream : Pal.Butter;
                else if (d < 40 && Gfx.Dither(x, y, (40 - d) / 60f)) img[x, y] = Color.Lerp(img[x, y], Pal.Butter, 0.5f);
            }
        }
        if (biome == Biome.Caves)
            for (int i = 0; i < 70; i++)
            {
                int x = (int)(Gfx.Hash(i, 1, 9) * 320), y = (int)(Gfx.Hash(i, 2, 9) * 180);
                img[x, y] = i % 3 == 0 ? Pal.Violet : Pal.Plum;
            }
        if (biome == Biome.Castle)
        {
            for (int i = 0; i < 40; i++)
            {
                int x = (int)(Gfx.Hash(i, 3, 5) * 320), y = 60 + (int)(Gfx.Hash(i, 4, 5) * 120);
                img[x, y] = Pal.Flame;
            }
            // Stars and a smoky blood moon, glimpsed through the castle windows.
            for (int i = 0; i < 30; i++)
            {
                int x = (int)(Gfx.Hash(i, 6, 5) * 320), y = 8 + (int)(Gfx.Hash(i, 7, 5) * 70);
                img[x, y] = i % 4 == 0 ? Pal.Butter : Pal.Blush;
            }
            const float mx = 236, my = 34;
            for (int y = 0; y < 180; y++)
            for (int x = 0; x < 320; x++)
            {
                float d = MathF.Sqrt((x - mx) * (x - mx) + (y - my) * (y - my));
                if (d < 12) img[x, y] = d < 10 ? (Gfx.Hash(x / 3, y / 3, 8) < 0.22f ? Pal.Coral : Pal.Blush) : Pal.Coral;
                else if (d < 30 && Gfx.Dither(x, y, (30 - d) / 50f)) img[x, y] = Color.Lerp(img[x, y], Pal.Coral, 0.35f);
            }
        }
        return img;
    }

    private static PixelImage Hills(int height, Color[] ramp, int seed, float amp, int extra, bool peaks)
    {
        var img = new PixelImage(StripWidth, height);
        for (int x = 0; x < StripWidth; x++)
        {
            float h = height * (0.55f + Periodic(x, seed, (2, 0.18f), (5, 0.08f), (11, 0.03f)) * amp * 1.6f);
            if (peaks) h += MathF.Max(0, Periodic(x, seed + 4, (3, 1f))) * 18 - 6;
            int top = Math.Clamp(height - (int)h, 0, height - 1);
            for (int y = top; y < height; y++)
            {
                float v = 0.75f - (y - top) / (float)height * 0.9f + (y - top < 2 ? 0.25f : 0);
                // Lit left slopes, shaded right slopes.
                float slope = Periodic(x + 2, seed, (2, 0.18f), (5, 0.08f)) - Periodic(x - 2, seed, (2, 0.18f), (5, 0.08f));
                v += slope * 2.5f;
                img[x, y] = Gfx.Ramp(ramp, v, x, y);
            }
        }
        return img;
    }

    private static PixelImage TreeHills()
    {
        const int height = 110;
        var img = Hills(height, [Pal.Fern, Pal.Leaf, Pal.Lime, Pal.Sprout], 7, 0.35f, 0, peaks: false);
        // Round trees along the ridge, periodic placement.
        for (int i = 0; i < 9; i++)
        {
            float x = i * StripWidth / 9f + Gfx.Hash(i, 0, 3) * 20;
            int ridge = 0;
            while (ridge < height - 1 && img[(int)x % StripWidth, ridge].A == 0) ridge++;
            float r = 7 + Gfx.Hash(i, 1, 3) * 5;
            for (int dx = -1; dx <= 1; dx++)
            for (int y = ridge - 4; y < ridge + 6; y++) img[((int)x + dx + StripWidth) % StripWidth, y] = Pal.Soil;
            for (int y = (int)(ridge - r * 2 - 4); y < ridge; y++)
            for (int dx = (int)-r - 1; dx <= r + 1; dx++)
            {
                float cy = ridge - r - 4, nx = dx / r, ny = (y - cy) / r;
                float d = nx * nx + ny * ny;
                if (d > 1) continue;
                float v = 0.65f - 0.35f * (nx * 0.6f + ny * 0.8f) - d * 0.15f;
                int px = ((int)x + dx + StripWidth) % StripWidth;
                img[px, y] = Gfx.Ramp([Pal.Pine, Pal.Fern, Pal.Leaf, Pal.Lime], v, px, y);
            }
        }
        return img;
    }

    private static PixelImage Bushes(int height, Color[] ramp)
    {
        var img = new PixelImage(StripWidth, height);
        for (int i = 0; i < 14; i++)
        {
            float cx = i * StripWidth / 14f, r = 10 + Gfx.Hash(i, 0, 21) * 9;
            for (int y = 0; y < height; y++)
            for (int dx = -24; dx <= 24; dx++)
            {
                float nx = dx / r, ny = (y - (height - r * 0.7f)) / r;
                float d = nx * nx + ny * ny;
                if (d > 1) continue;
                int px = ((int)cx + dx + StripWidth) % StripWidth;
                float v = 0.55f - 0.35f * (nx * 0.6f + ny * 0.8f) - d * 0.25f;
                if (img[px, y].A == 0 || v > 0.5f) img[px, y] = Gfx.Ramp(ramp, v, px, y);
            }
        }
        for (int x = 0; x < StripWidth; x++)
        for (int y = height - 6; y < height; y++) img[x, y] = Gfx.Ramp(ramp, 0.2f, x, y);
        // A few flowers dotted on the bushes.
        for (int i = 0; i < 30; i++)
        {
            int x = (int)(Gfx.Hash(i, 5, 2) * StripWidth), y = height - 6 - (int)(Gfx.Hash(i, 6, 2) * 14);
            if (img[x, y].A > 0) img[x, y] = i % 2 == 0 ? Pal.Blush : Pal.Butter;
        }
        return img;
    }

    private static PixelImage Clouds(int height, Color[] ramp, int count, int seed)
    {
        var img = new PixelImage(StripWidth, height);
        for (int i = 0; i < count; i++)
        {
            float cx = (i + Gfx.Hash(i, 0, seed) * 0.5f) * StripWidth / count;
            float cy = height * (0.35f + Gfx.Hash(i, 1, seed) * 0.4f);
            float w = 14 + Gfx.Hash(i, 2, seed) * 16;
            for (int b = 0; b < 4; b++)
            {
                float bx = cx + (b - 1.5f) * w * 0.5f, by = cy - (b is 1 or 2 ? w * 0.25f : 0), r = w * (b is 1 or 2 ? 0.55f : 0.4f);
                for (int y = (int)(by - r); y <= by + r; y++)
                for (int x = (int)(bx - r); x <= bx + r; x++)
                {
                    float nx = (x - bx) / r, ny = (y - by) / r;
                    if (nx * nx + ny * ny > 1 || y < 0 || y >= height || y > cy + w * 0.2f) continue;
                    int px = (x % StripWidth + StripWidth) % StripWidth;
                    float v = 0.9f - ny * 0.35f - (y > cy ? 0.3f : 0);
                    img[px, y] = Gfx.Ramp(ramp, v, px, y);
                }
            }
        }
        return img;
    }

    private static PixelImage CloudSea()
    {
        const int height = 120;
        var img = new PixelImage(StripWidth, height);
        for (int x = 0; x < StripWidth; x++)
        {
            float h = 46 + Periodic(x, 2, (4, 6f), (9, 3f), (17, 1.5f));
            for (int y = height - (int)h; y < height; y++)
            {
                float v = 0.95f - (y - (height - h)) / h * 0.8f;
                img[x, y] = Gfx.Ramp([Pal.Ocean, Pal.Sky, Pal.Air, Pal.Cloud], v - 0.1f, x, y);
            }
        }
        // Distant floating islands.
        foreach (var (cx, cy, w) in new[] { (60f, 30f, 26f), (230f, 44f, 18f), (330f, 22f, 12f) })
            for (int y = 0; y < 24; y++)
            for (int x = (int)(cx - w); x <= cx + w; x++)
            {
                float half = w * (1 - y / 14f);
                if (MathF.Abs(x - cx) > half) continue;
                int py = (int)cy + y;
                img[(x + StripWidth) % StripWidth, py] = y < 3 ? Pal.Leaf : Gfx.Ramp([Pal.Slate, Pal.Dusk, Pal.Haze, Pal.Mist], 0.7f - y * 0.04f, x, py);
            }
        return img;
    }

    private static PixelImage CaveColumns(int height, Color[] ramp, int seed)
    {
        var img = new PixelImage(StripWidth, height);
        for (int x = 0; x < StripWidth; x++)
        {
            float top = 30 + Periodic(x, seed, (6, 14f), (13, 8f), (29, 4f));
            float bottom = 46 + Periodic(x, seed + 3, (5, 16f), (11, 7f), (23, 4f));
            for (int y = 0; y < height; y++)
            {
                bool rock = y < top || y > height - bottom;
                if (!rock) continue;
                float edge = y < top ? (top - y) / 20f : (y - (height - bottom)) / 20f;
                img[x, y] = Gfx.Ramp(ramp, 0.75f - MathF.Min(edge, 1) * 0.6f, x, y);
            }
        }
        return img;
    }

    private static PixelImage CrystalGlow()
    {
        const int height = 140;
        var img = new PixelImage(StripWidth, height);
        for (int i = 0; i < 7; i++)
        {
            float cx = i * StripWidth / 7f + Gfx.Hash(i, 0, 31) * 30;
            float cy = height - 10 - Gfx.Hash(i, 1, 31) * 60;
            for (int y = (int)cy - 34; y < cy + 34; y++)
            for (int x = (int)cx - 34; x < cx + 34; x++)
            {
                float d = MathF.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / 34f;
                if (d >= 1 || y < 0 || y >= height) continue;
                int px = (x + StripWidth) % StripWidth;
                var c = (i % 2 == 0 ? Pal.Violet : Pal.Teal).WithAlpha((1 - d) * (1 - d) * 0.35f);
                img.Blend(px, y, c);
            }
            // Shard silhouettes at the glow centre.
            for (int s = -1; s <= 1; s++)
            {
                int bx = (int)cx + s * 5, len = 10 + (int)(Gfx.Hash(i, s + 2, 7) * 8);
                for (int k = 0; k < len; k++)
                for (int w = -2 + k / 4; w <= 2 - k / 4; w++)
                    img[(bx + w + StripWidth) % StripWidth, (int)cy + 8 - k] = (s + k) % 3 == 0 ? Pal.Lilac : Pal.Violet;
            }
        }
        return img;
    }

    private static PixelImage CastleTowers()
    {
        const int height = 150;
        var img = new PixelImage(StripWidth, height);
        var ramp = new[] { Pal.Ink, Pal.Ink, Pal.Bark, Pal.Night };
        for (int i = 0; i < 6; i++)
        {
            int cx = i * 64 + (int)(Gfx.Hash(i, 0, 4) * 20);
            int w = 18 + (int)(Gfx.Hash(i, 1, 4) * 14), top = 20 + (int)(Gfx.Hash(i, 2, 4) * 50);
            for (int y = top; y < height; y++)
            for (int x = cx - w / 2; x < cx + w / 2; x++)
                img[(x + StripWidth) % StripWidth, y] = Gfx.Ramp(ramp, 0.4f + (x - cx) * -0.02f, x, y);
            for (int x = cx - w / 2; x < cx + w / 2; x += 4)
                img.FillRect((x + StripWidth) % StripWidth, top - 3, 2, 3, Pal.Ink);
            // Lit windows.
            for (int wy = top + 10; wy < height - 20; wy += 18)
            {
                int wx = (cx - 2 + StripWidth) % StripWidth;
                img.FillRect(wx, wy, 3, 5, Pal.Amber);
                img[wx + 1, wy + 1] = Pal.Gold;
            }
            // Spire roof.
            for (int k = 0; k < w / 2 + 4; k++)
                img.FillRect((cx - (w / 2 + 2) + k + StripWidth) % StripWidth, top - 4 - k * 2 / 3, (w / 2 + 2 - k) * 2, 2, Pal.Night);
        }
        for (int x = 0; x < StripWidth; x++)
        for (int y = height - 30; y < height; y++) img[x, y] = Pal.Ink;
        return img;
    }
}

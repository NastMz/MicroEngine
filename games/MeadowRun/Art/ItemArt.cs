using MeadowRun.Content;
using MicroEngine;
using MicroEngine.Graphics;
using Gfx = MicroEngine.Graphics.Art;

namespace MeadowRun.Art;

/// <summary>Pickups, props and projectiles.</summary>
internal static class ItemArt
{
    /// <summary>Spinning sun-coin: frame 0 is face-on, widths shrink to a sliver and back.</summary>
    public static IEnumerable<PixelImage> Coin()
    {
        float[] widths = [4.6f, 3.6f, 1.6f, 0.7f, 1.6f, 3.6f];
        for (int f = 0; f < widths.Length; f++)
        {
            var img = new PixelImage(12, 14);
            float rx = widths[f];
            Brush.Ellipse(img, 6, 7, rx, 5.8f, Pal.GoldRamp, 0.05f);
            if (rx > 2.5f)
            {
                Brush.Flat(img, 6, 7, rx * 0.55f, 3.4f, Pal.Amber);
                img.Line(6, 5, 6, 9, Pal.Butter);
                img[(int)(6 - rx * 0.5f), 4] = Pal.Cream;
            }
            Brush.SelOutline(img, Pal.Ink);
            yield return img;
        }
    }

    /// <summary>Sunpetal: the Bloom power-up, a pink flower on a stem.</summary>
    public static PixelImage Sunpetal(int frame)
    {
        var img = new PixelImage(16, 16);
        img.Line(8, 9, 8, 15, Pal.Fern);
        Brush.Ellipse(img, 5, 13, 2.6f, 1.3f, Pal.GrassRamp);
        Brush.Ellipse(img, 11, 12, 2.6f, 1.3f, Pal.GrassRamp, 0.1f);
        float spin = frame * 0.3f;
        for (int i = 0; i < 6; i++)
        {
            float a = spin + i * MathF.Tau / 6;
            Brush.Ellipse(img, 8 + MathF.Cos(a) * 3.4f, 6 + MathF.Sin(a) * 3.2f, 2.1f, 2.1f, [Pal.Wine, Pal.Berry, Pal.Coral, Pal.Blush]);
        }
        Brush.Ellipse(img, 8, 6, 2.1f, 2.1f, Pal.GoldRamp);
        img[7, 5] = Pal.Ink; img[9, 5] = Pal.Ink;
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    /// <summary>Ember seed: the throwing power-up, a glowing acorn-like seed.</summary>
    public static PixelImage EmberSeed(int frame)
    {
        var img = new PixelImage(16, 16);
        float bob = frame % 2 == 0 ? 0 : 0.5f;
        Brush.Ellipse(img, 8, 9.5f + bob, 5, 5.5f, Pal.LavaRamp, 0.05f);
        Brush.Ellipse(img, 8, 5 + bob, 5.4f, 2.4f, [Pal.Bark, Pal.Soil, Pal.Clay, Pal.Sand]);
        img.Line(8, 1, 9, 3, Pal.Soil);
        img[6, 8] = Pal.Ink; img[10, 8] = Pal.Ink;
        img[6, 7] = Pal.Butter; img[10, 7] = Pal.Butter;
        img[5, 11] = Pal.Butter;
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    /// <summary>Life sprout: a little Pip head, the 1-up.</summary>
    public static PixelImage LifeSprout()
    {
        var img = new PixelImage(16, 16);
        Brush.Ellipse(img, 8, 10, 6, 5, [Pal.Ocean, Pal.Sky, Pal.Air, Pal.Cloud]);
        img.Line(8, 5, 8, 2, Pal.Fern);
        Brush.Ellipse(img, 5.6f, 2.6f, 2.6f, 1.4f, Pal.GrassRamp);
        Brush.Ellipse(img, 10.6f, 2, 2.6f, 1.4f, Pal.GrassRamp, 0.1f);
        img.FillRect(6, 8, 2, 3, Pal.Ink); img.FillRect(9, 8, 2, 3, Pal.Ink);
        img[6, 8] = Pal.Cream; img[9, 8] = Pal.Cream;
        img[4, 11] = Pal.Coral; img[12, 11] = Pal.Coral;
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    /// <summary>Spring: frame 0 relaxed, 1 compressed.</summary>
    public static PixelImage Spring(int frame)
    {
        var img = new PixelImage(16, 16);
        int top = frame == 0 ? 4 : 9;
        for (int y = top + 3; y < 14; y += 2)
        {
            img.Line(3, y, 12, y, Pal.Haze);
            img.Line(3, y + 1, 12, y + 1, Pal.Slate);
        }
        Brush.Box(img, 1, top, 14, 3, [Pal.Wine, Pal.Berry, Pal.Coral, Pal.Blush]);
        Brush.Box(img, 2, 14, 12, 2, Pal.StoneRamp);
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    /// <summary>Checkpoint lantern post: frame 0 unlit, 1..2 lit flicker.</summary>
    public static PixelImage Checkpoint(int frame)
    {
        var img = new PixelImage(12, 32);
        img.FillRect(5, 8, 2, 24, Pal.Soil);
        img.Line(5, 8, 5, 31, Pal.Clay);
        Brush.Box(img, 2, 28, 8, 4, Pal.StoneRamp);
        Brush.Box(img, 2, 2, 8, 8, frame == 0 ? Pal.StoneRamp : Pal.GoldRamp, 0.9f, 0.5f);
        img.FillRect(4, 4, 4, 4, frame == 0 ? Pal.Night : frame == 1 ? Pal.Butter : Pal.Gold);
        img.Line(1, 1, 10, 1, Pal.Wine);
        img.Line(2, 0, 9, 0, Pal.Berry);
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    /// <summary>Goal pole (with orb) as one tall image; the pennant is separate so it can slide.</summary>
    public static PixelImage GoalPole(int height)
    {
        var img = new PixelImage(8, height);
        for (int y = 6; y < height; y++) { img[3, y] = Pal.Cream; img[4, y] = Pal.Haze; }
        Brush.Ellipse(img, 4, 3.5f, 3.4f, 3.4f, Pal.GoldRamp, 0.1f);
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    public static PixelImage Pennant(int frame)
    {
        var img = new PixelImage(16, 12);
        for (int x = 0; x < 14; x++)
        {
            float wave = MathF.Sin((x + frame * 3) * 0.55f) * 1.2f * x / 14f;
            int half = 5 - x * 5 / 14;
            for (int y = -half; y <= half; y++)
                img[x, (int)MathF.Round(6 + y + wave)] = Gfx.Ramp([Pal.Pine, Pal.Fern, Pal.Leaf, Pal.Lime], 0.8f - x * 0.03f - (y > 0 ? 0.25f : 0), x, y);
        }
        img[3, 5] = Pal.Butter; img[4, 6] = Pal.Butter; img[3, 7] = Pal.Butter; img[2, 6] = Pal.Butter;
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    /// <summary>Thrown ember seed projectile, 4 rotation frames.</summary>
    public static IEnumerable<PixelImage> SeedShot()
    {
        for (int f = 0; f < 4; f++)
        {
            var img = new PixelImage(8, 8);
            Brush.Ellipse(img, 4, 4, 3, 3, Pal.LavaRamp, 0.1f);
            float a = f * MathF.PI / 2;
            img[(int)(4 + MathF.Cos(a) * 2), (int)(4 + MathF.Sin(a) * 2)] = Pal.Cream;
            Brush.SelOutline(img, Pal.Ink);
            yield return img;
        }
    }

    /// <summary>Firebar / lava fireball, 2 flicker frames.</summary>
    public static PixelImage Fireball(int frame)
    {
        var img = new PixelImage(10, 10);
        Brush.Ellipse(img, 5, 5, 4, 4, Pal.LavaRamp, frame == 0 ? 0.1f : 0.25f);
        Brush.Flat(img, 4, 4, 1.8f, 1.8f, frame == 0 ? Pal.Butter : Pal.Cream);
        Brush.SelOutline(img, Pal.Wine);
        return img;
    }

    /// <summary>Enemy thorn spit.</summary>
    public static PixelImage Thorn()
    {
        var img = new PixelImage(8, 8);
        Brush.Ellipse(img, 4, 4, 2.8f, 2.8f, [Pal.Plum, Pal.Violet, Pal.Lilac, Pal.Mist]);
        img[1, 4] = Pal.Lilac; img[7, 4] = Pal.Lilac; img[4, 1] = Pal.Lilac; img[4, 7] = Pal.Lilac;
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    /// <summary>Boulder dropped during the boss fight.</summary>
    public static PixelImage Rock()
    {
        var img = new PixelImage(14, 14);
        Brush.Ellipse(img, 7, 7, 6, 5.5f, Pal.StoneRamp);
        img[5, 6] = Pal.Night; img[6, 7] = Pal.Night; img[9, 9] = Pal.Night;
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    /// <summary>Moving platform segment (left cap, middle, right cap) for a biome.</summary>
    public static PixelImage Platform(Biome biome, int part)
    {
        var img = new PixelImage(16, 8);
        Color[] ramp = biome switch
        {
            Biome.Sky => Pal.CloudRamp,
            Biome.Caves => Pal.CrystalRamp,
            Biome.Castle => Pal.StoneRamp,
            _ => [Pal.Bark, Pal.Clay, Pal.Sand, Pal.Wheat],
        };
        if (biome == Biome.Sky)
        {
            for (int x = 0; x < 16; x++)
            {
                int top = part == 0 ? Math.Max(0, 3 - x) : part == 2 ? Math.Max(0, x - 12) : 0;
                int bottom = 7 - (int)(Gfx.Hash(x, part, 4) * 2);
                for (int y = top; y <= bottom; y++) img[x, y] = Gfx.Ramp(ramp, 0.95f - y * 0.1f, x, y);
            }
            Brush.SelOutline(img, Pal.Dusk);
            return img;
        }
        Brush.Box(img, 0, 0, 16, 7, ramp, 0.95f, 0.35f);
        img.Line(0, 0, 15, 0, ramp[3]);
        img.Line(0, 7, 15, 7, Pal.Ink);
        if (part == 0) img.Line(0, 0, 0, 7, Pal.Ink);
        if (part == 2) img.Line(15, 0, 15, 7, Pal.Ink);
        img[4, 3] = ramp[0]; img[11, 3] = ramp[0];
        if (biome == Biome.Castle) { img[2, 3] = Pal.Rust; img[13, 3] = Pal.Rust; }
        return img;
    }

    /// <summary>Square brick shard used for breaking blocks.</summary>
    public static PixelImage Shard(Biome biome)
    {
        var ramp = TileArt.Style(biome).Brick;
        var img = new PixelImage(6, 6);
        Brush.Box(img, 0, 0, 6, 6, ramp, 0.9f, 0.4f);
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    /// <summary>The Sunstone, the stolen heart of the meadow and the final goal.</summary>
    public static PixelImage Sunstone(int frame)
    {
        var img = new PixelImage(20, 20);
        float r = 7 + (frame % 2) * 0.4f;
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8 + frame * 0.2f;
            img.Line(10, 10, (int)(10 + MathF.Cos(a) * 9.5f), (int)(10 + MathF.Sin(a) * 9.5f), i % 2 == 0 ? Pal.Gold : Pal.Amber);
        }
        Brush.Ellipse(img, 10, 10, r * 0.75f, r * 0.75f, Pal.GoldRamp, 0.15f);
        img[8, 7] = Pal.Cream; img[7, 8] = Pal.Cream;
        Brush.SelOutline(img, Pal.Rust);
        return img;
    }
}

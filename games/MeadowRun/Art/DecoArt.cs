using MeadowRun.Content;
using MicroEngine;
using MicroEngine.Graphics;

namespace MeadowRun.Art;

/// <summary>Small non-colliding props scattered on ground tops so surfaces never look bare.</summary>
internal static class DecoArt
{
    /// <summary>Sprite names usable as surface decorations per biome (16 px wide, bottom-anchored).</summary>
    public static string[] For(Biome biome) => biome switch
    {
        Biome.Meadow => ["deco_tuft", "deco_tuft", "deco_flower_pink", "deco_flower_gold", "deco_mushroom", "deco_tuft2"],
        Biome.Caves => ["deco_crystal", "deco_crystal_small", "deco_moss", "deco_glowcap"],
        Biome.Sky => ["deco_puff", "deco_skyflower", "deco_puff"],
        _ => ["deco_chain_post", "deco_skull", "deco_rubble"],
    };

    public static IEnumerable<(string Name, PixelImage Image)> All()
    {
        yield return ("deco_tuft", Tuft(Pal.GrassRamp, 0));
        yield return ("deco_tuft2", Tuft(Pal.GrassRamp, 1));
        yield return ("deco_flower_pink", Flower([Pal.Wine, Pal.Berry, Pal.Coral, Pal.Blush]));
        yield return ("deco_flower_gold", Flower(Pal.GoldRamp));
        yield return ("deco_mushroom", Mushroom());
        yield return ("deco_crystal", Crystal(12, 3));
        yield return ("deco_crystal_small", Crystal(7, 2));
        yield return ("deco_moss", Tuft([Pal.Pine, Pal.Teal, Pal.Aqua, Pal.Cream], 1));
        yield return ("deco_glowcap", GlowCap());
        yield return ("deco_puff", Puff());
        yield return ("deco_skyflower", Flower([Pal.Ocean, Pal.Sky, Pal.Air, Pal.Cloud]));
        yield return ("deco_chain_post", ChainPost());
        yield return ("deco_skull", Skull());
        yield return ("deco_rubble", Rubble());
        for (int f = 0; f < 3; f++) yield return ("torch" + f, Torch(f));
        yield return ("glow", Glow(24));
        yield return ("glow_small", Glow(10));
    }

    private static PixelImage Tuft(Color[] ramp, int variant)
    {
        var img = new PixelImage(16, 10);
        int[] heights = variant == 0 ? [4, 7, 5, 8, 4] : [3, 5, 6, 4];
        for (int i = 0; i < heights.Length; i++)
        {
            int x = 3 + i * 2 + variant;
            int lean = i % 2 == 0 ? -1 : 1;
            for (int y = 0; y < heights[i]; y++)
                img[x + (y > heights[i] / 2 ? lean : 0), 9 - y] = y == heights[i] - 1 ? ramp[3] : ramp[y < 2 ? 1 : 2];
        }
        return img;
    }

    private static PixelImage Flower(Color[] petals)
    {
        var img = new PixelImage(16, 12);
        img.Line(8, 6, 8, 11, Pal.Fern);
        img[7, 9] = Pal.Leaf; img[6, 8] = Pal.Leaf; img[9, 10] = Pal.Leaf;
        foreach (var (dx, dy) in new[] { (0, -2), (2, 0), (0, 2), (-2, 0) }) Brush.Ellipse(img, 8.5f + dx, 4.5f + dy, 1.4f, 1.4f, petals);
        img[8, 4] = Pal.Gold;
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    private static PixelImage Mushroom()
    {
        var img = new PixelImage(16, 12);
        img.FillRect(7, 6, 3, 6, Pal.Wheat);
        img.Line(9, 6, 9, 11, Pal.Sand);
        Brush.Ellipse(img, 8.5f, 5, 5, 3, [Pal.Wine, Pal.Berry, Pal.Coral, Pal.Blush]);
        img[6, 4] = Pal.Cream; img[10, 3] = Pal.Cream; img[8, 6] = Pal.Cream;
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    private static PixelImage Crystal(int height, int halfWidth)
    {
        var img = new PixelImage(16, 16);
        void Shard(int bx, int h, int hw, bool lit)
        {
            for (int k = 0; k < h; k++)
            {
                int w = Math.Max(0, hw - k * hw / Math.Max(h, 1));
                for (int x = -w; x <= w; x++)
                    img[bx + x, 15 - k] = x < 0 == lit ? Pal.Lilac : x == 0 ? Pal.Mist : Pal.Violet;
            }
        }
        Shard(5, height - 4, halfWidth - 1, false);
        Shard(10, height - 2, halfWidth - 1, true);
        Shard(8, height, halfWidth, true);
        Brush.SelOutline(img, Pal.Plum);
        return img;
    }

    private static PixelImage GlowCap()
    {
        var img = new PixelImage(16, 12);
        img.Line(6, 7, 6, 11, Pal.Mist);
        img.Line(11, 8, 11, 11, Pal.Mist);
        Brush.Ellipse(img, 6, 6, 3, 2, [Pal.Teal, Pal.Teal, Pal.Aqua, Pal.Cream]);
        Brush.Ellipse(img, 11, 7.5f, 2.2f, 1.5f, [Pal.Teal, Pal.Aqua, Pal.Aqua, Pal.Cream]);
        return img;
    }

    private static PixelImage Puff()
    {
        var img = new PixelImage(16, 8);
        Brush.Ellipse(img, 5, 5, 4, 3, Pal.CloudRamp, 0.2f);
        Brush.Ellipse(img, 10, 4.5f, 4.5f, 3.5f, Pal.CloudRamp, 0.2f);
        return img;
    }

    private static PixelImage ChainPost()
    {
        var img = new PixelImage(16, 14);
        Brush.Box(img, 6, 2, 4, 12, Pal.StoneRamp);
        img.FillRect(5, 1, 6, 2, Pal.Haze);
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    private static PixelImage Skull()
    {
        var img = new PixelImage(16, 8);
        Brush.Ellipse(img, 8, 4.5f, 3.5f, 3, [Pal.Dusk, Pal.Haze, Pal.Mist, Pal.Cream]);
        img[6, 4] = Pal.Ink; img[9, 4] = Pal.Ink; img[7, 7] = Pal.Ink;
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    private static PixelImage Rubble()
    {
        var img = new PixelImage(16, 6);
        Brush.Ellipse(img, 5, 4, 3, 2, Pal.StoneRamp);
        Brush.Ellipse(img, 10, 4.5f, 2.2f, 1.5f, Pal.StoneRamp);
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    private static PixelImage Torch(int frame)
    {
        var img = new PixelImage(10, 20);
        img.FillRect(4, 9, 2, 11, Pal.Soil);
        Brush.Box(img, 2, 8, 6, 3, Pal.StoneRamp);
        float h = 5 + frame % 2;
        Brush.Ellipse(img, 5 + (frame == 2 ? 0.5f : 0), 8 - h * 0.6f, 2.6f, h * 0.7f, Pal.LavaRamp, 0.2f);
        img[5, 5] = Pal.Butter;
        return img;
    }

    /// <summary>Soft radial glow drawn additively behind crystals, torches and lava.</summary>
    internal static PixelImage Glow(int radius)
    {
        int size = radius * 2;
        var img = new PixelImage(size, size);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = MathF.Sqrt((x + 0.5f - radius) * (x + 0.5f - radius) + (y + 0.5f - radius) * (y + 0.5f - radius)) / radius;
            if (d >= 1) continue;
            float a = (1 - d) * (1 - d);
            // Quantise into bands so the glow stays pixel-art rather than airbrushed.
            a = MathF.Round(a * 5) / 5f;
            img[x, y] = Color.White.WithAlpha(a * 0.8f);
        }
        return img;
    }
}

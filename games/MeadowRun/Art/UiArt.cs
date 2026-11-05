using MicroEngine;
using MicroEngine.Graphics;
using Gfx = MicroEngine.Graphics.Art;

namespace MeadowRun.Art;

/// <summary>Logo, HUD icons and world-map art.</summary>
internal static class UiArt
{
    private static readonly Dictionary<char, string[]> Letters = new()
    {
        ['M'] = ["##...##", "###.###", "#######", "##.#.##", "##...##", "##...##", "##...##"],
        ['E'] = ["######", "##....", "##....", "#####.", "##....", "##....", "######"],
        ['A'] = [".####.", "##..##", "##..##", "######", "##..##", "##..##", "##..##"],
        ['D'] = ["#####.", "##..##", "##..##", "##..##", "##..##", "##..##", "#####."],
        ['O'] = [".####.", "##..##", "##..##", "##..##", "##..##", "##..##", ".####."],
        ['W'] = ["##...##", "##...##", "##...##", "##.#.##", "#######", "###.###", "##...##"],
        ['R'] = ["#####.", "##..##", "##..##", "#####.", "##.##.", "##..##", "##..##"],
        ['U'] = ["##..##", "##..##", "##..##", "##..##", "##..##", "##..##", ".####."],
        ['N'] = ["##..##", "###.##", "######", "##.###", "##..##", "##..##", "##..##"],
        [' '] = ["...", "...", "...", "...", "...", "...", "..."],
    };

    /// <summary>"MEADOW RUN" in chunky bevelled letters with a gold gradient and a thick ink outline.</summary>
    public static PixelImage Logo()
    {
        const string text = "MEADOW RUN";
        const int scale = 3;
        int width = text.Sum(c => (Letters[c][0].Length + 1) * scale) + 8;
        var mask = new PixelImage(width, 7 * scale + 8);
        int cursor = 4;
        foreach (var c in text)
        {
            var rows = Letters[c];
            // A gentle wave baseline gives the logo some bounce.
            int lift = (int)MathF.Round(MathF.Sin(cursor * 0.05f) * 1.5f);
            for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
                if (rows[y][x] == '#') mask.FillRect(cursor + x * scale, 4 + lift + y * scale, scale, scale, Color.White);
            cursor += (rows[0].Length + 1) * scale;
        }
        var img = new PixelImage(width, mask.Height + 4);
        // Drop shadow, then outline, then gradient face with a top highlight band.
        for (int y = 0; y < mask.Height; y++)
        for (int x = 0; x < width; x++)
        {
            if (mask[x, y].A == 0) continue;
            for (int dy = -2; dy <= 4; dy++)
            for (int dx = -2; dx <= 3; dx++)
                if (img[x + dx, y + dy].A == 0) img[x + dx, y + dy] = dy > 2 ? Pal.Plum : Pal.Ink;
        }
        for (int y = 0; y < mask.Height; y++)
        for (int x = 0; x < width; x++)
        {
            if (mask[x, y].A == 0) continue;
            float t = (y - 4) / (7f * scale);
            var face = Gfx.Ramp(Pal.GoldRamp, 1f - t * 0.95f, x, y);
            if (mask[x, y - 1].A == 0) face = Pal.Cream;
            else if (mask[x, y + 1].A == 0) face = Pal.Rust;
            img[x, y] = face;
        }
        return img;
    }

    public static PixelImage CoinIcon()
    {
        var img = new PixelImage(8, 9);
        Brush.Ellipse(img, 4, 4.5f, 3, 3.8f, Pal.GoldRamp, 0.1f);
        img.Line(4, 3, 4, 6, Pal.Butter);
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    public static PixelImage LifeIcon()
    {
        var img = new PixelImage(11, 11);
        Brush.Ellipse(img, 5.5f, 6.5f, 4.5f, 3.8f, Pal.GrassRamp);
        img.Line(5, 3, 5, 1, Pal.Fern);
        Brush.Ellipse(img, 3.5f, 1.5f, 1.8f, 1, Pal.GrassRamp);
        Brush.Ellipse(img, 7.5f, 1.2f, 1.8f, 1, Pal.GrassRamp);
        img[6, 5] = Pal.Ink; img[6, 6] = Pal.Ink; img[8, 5] = Pal.Ink; img[8, 6] = Pal.Ink;
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    /// <summary>
    /// The HUD's badge for Pip's form, drawn at the HUD's own size: Pip's round body in the form's colours under its
    /// crown (the sprout's two leaves, or the bloom's and the ember's flower).
    /// </summary>
    public static PixelImage FormIcon(PipForm form)
    {
        var img = new PixelImage(11, 13);
        bool ember = form == PipForm.Ember;
        // Pip's own skin (HeroArt), belly and cheek, so the badge reads as the Pip on screen.
        Color[] body = ember ? [Pal.Wine, Pal.Rust, Pal.Flame, Pal.Gold] : [Pal.Fern, Pal.Leaf, Pal.Lime, Pal.Sprout];
        Brush.Ellipse(img, 5.5f, 8.5f, 4.5f, 3.6f, body);
        Brush.Flat(img, 6.5f, 10.2f, 2.2f, 1.3f, Color.Lerp(body[3], Pal.Butter, 0.6f));
        if (form == PipForm.Sprout)
        {
            img.Line(5, 5, 5, 3, Pal.Fern);
            Brush.Ellipse(img, 3.5f, 3.5f, 1.8f, 1, Pal.GrassRamp);
            Brush.Ellipse(img, 7.5f, 3.2f, 1.8f, 1, Pal.GrassRamp);
        }
        else
        {
            Color[] petals = form == PipForm.Bloom ? [Pal.Wine, Pal.Berry, Pal.Coral, Pal.Blush] : [Pal.Rust, Pal.Flame, Pal.Gold, Pal.Butter];
            img.Paint(3, 1, new Dictionary<char, Color> { ['l'] = petals[3], ['p'] = petals[2], ['d'] = petals[1], ['c'] = form == PipForm.Bloom ? Pal.Gold : Pal.Cream, ['s'] = Pal.Fern },
                ".l.l.", "ppcpp", ".dpd.", "..s..");
        }
        img[6, 7] = Pal.Ink; img[6, 8] = Pal.Ink; img[8, 7] = Pal.Ink; img[8, 8] = Pal.Ink;
        img[5, 9] = ember ? Pal.Blush : Pal.Coral;
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    public static PixelImage ClockIcon()
    {
        var img = new PixelImage(9, 9);
        Brush.Ellipse(img, 4.5f, 4.5f, 3.6f, 3.6f, [Pal.Dusk, Pal.Haze, Pal.Mist, Pal.Cream]);
        img.Line(4, 2, 4, 4, Pal.Ink);
        img.Line(4, 4, 6, 4, Pal.Ink);
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    /// <summary>World map node: 0 locked, 1 open, 2 cleared, 3 boss.</summary>
    public static PixelImage MapNode(int state)
    {
        var img = new PixelImage(16, 12);
        Color[] ramp = state switch
        {
            0 => Pal.StoneRamp,
            1 => [Pal.Wine, Pal.Berry, Pal.Coral, Pal.Blush],
            2 => Pal.GoldRamp,
            _ => [Pal.Ink, Pal.Wine, Pal.Rust, Pal.Flame],
        };
        Brush.Ellipse(img, 8, 7, 7, 4.5f, ramp);
        Brush.Flat(img, 8, 6.5f, 4, 2.2f, ramp[3]);
        Brush.Flat(img, 8, 6.8f, 3, 1.4f, ramp[1]);
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    /// <summary>The stylised world map island, painted once.</summary>
    public static PixelImage MapBackdrop()
    {
        var img = new PixelImage(320, 180);
        for (int y = 0; y < 180; y++)
        for (int x = 0; x < 320; x++)
        {
            float wave = Gfx.ValueNoise(x, y, 10, 4) * 0.25f;
            img[x, y] = Gfx.Ramp([Pal.Ocean, Pal.Ocean, Pal.Sky, Pal.Air], 0.25f + wave + (y / 180f) * 0.1f, x, y);
        }
        // Island: noisy blob, biomes split left to right.
        for (int y = 0; y < 180; y++)
        for (int x = 0; x < 320; x++)
        {
            float nx = (x - 160) / 150f, ny = (y - 98) / 72f;
            float d = nx * nx + ny * ny + (Gfx.ValueNoise(x, y, 18, 8) - 0.5f) * 0.5f;
            if (d > 1) { if (d < 1.08f) img[x, y] = Gfx.Dither(x, y, 0.6f) ? Pal.Cloud : Pal.Air; continue; }
            float shade = 0.75f - d * 0.35f + (Gfx.ValueNoise(x, y, 6, 2) - 0.5f) * 0.3f;
            // Wavy, slightly diagonal borders so regions blend like a painted map.
            float bx = x + (Gfx.ValueNoise(x, y, 14, 21) - 0.5f) * 34 + (y - 90) * 0.35f;
            Color[] ramp = bx switch
            {
                < 105 => Pal.GrassRamp,
                < 165 => [Pal.Night, Pal.Plum, Pal.Violet, Pal.Lilac],
                < 225 => [Pal.Haze, Pal.Air, Pal.Cloud, Pal.Cream],
                _ => [Pal.Bark, Pal.Wine, Pal.Rust, Pal.Flame],
            };
            if (d > 0.9f) ramp = [Pal.Clay, Pal.Sand, Pal.Wheat, Pal.Wheat];
            img[x, y] = Gfx.Ramp(ramp, shade, x, y);
        }
        // Little landmarks: trees in the meadow, crystals, clouds, a castle.
        for (int i = 0; i < 18; i++)
        {
            int tx = 30 + (int)(Gfx.Hash(i, 1, 6) * 70), ty = 50 + (int)(Gfx.Hash(i, 2, 6) * 90);
            if (img[tx, ty + 3] == Pal.Ocean) continue;
            Brush.Ellipse(img, tx, ty, 3, 3, [Pal.Pine, Pal.Fern, Pal.Leaf, Pal.Lime]);
            img[tx, ty + 3] = Pal.Bark; img[tx, ty + 4] = Pal.Bark;
        }
        for (int i = 0; i < 10; i++)
        {
            int cx = 112 + (int)(Gfx.Hash(i, 3, 6) * 46), cy = 60 + (int)(Gfx.Hash(i, 4, 6) * 80);
            img.Line(cx, cy, cx, cy - 5, Pal.Aqua); img[cx + 1, cy] = Pal.Teal; img[cx - 1, cy - 1] = Pal.Lilac;
        }
        foreach (var (cx, cy) in new[] { (180, 52), (205, 120), (190, 150) })
        {
            Brush.Ellipse(img, cx, cy, 8, 4, Pal.CloudRamp, 0.3f);
            Brush.Ellipse(img, cx + 6, cy - 2, 6, 4, Pal.CloudRamp, 0.3f);
        }
        // Ember castle on the east cliff.
        int kx = 272, ky = 78;
        img.FillRect(kx - 12, ky - 6, 24, 18, Pal.Night);
        img.FillRect(kx - 15, ky - 14, 6, 26, Pal.Slate);
        img.FillRect(kx + 9, ky - 14, 6, 26, Pal.Slate);
        img.FillRect(kx - 3, ky - 18, 6, 12, Pal.Slate);
        for (int x = kx - 15; x < kx + 15; x += 3) img[x, ky - 15] = Pal.Ink;
        img.FillRect(kx - 2, ky + 4, 4, 8, Pal.Ink);
        img[kx - 1, ky - 12] = Pal.Gold; img[kx - 13, ky - 8] = Pal.Amber; img[kx + 11, ky - 8] = Pal.Amber;
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }
}

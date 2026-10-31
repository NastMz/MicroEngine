using MicroEngine;
using MicroEngine.Graphics;

namespace CourtyardKey.Visuals;

/// <summary>
/// Procedural 16x16 terrain. Autotiled terrains (path, water, carpet, channel) are generated for all
/// sixteen 4-neighbour masks (1 = up, 2 = right, 4 = down, 8 = left match), animated ones per frame.
/// </summary>
public static class TileArt
{
    public const int S = 16;
    public const int WaterFrames = 4;

    public static void AddAll(SpriteAtlasBuilder b)
    {
        for (int v = 0; v < 4; v++) b.Add($"grass{v}", Grass(v));
        for (int f = 0; f < 2; f++)
        {
            for (int v = 0; v < 2; v++) b.Add($"flowers{v}_{f}", Flowers(v, f));
            b.Add($"tallgrass{f}", TallGrass(f));
        }
        for (int m = 0; m < 16; m++)
        {
            b.Add($"path{m}", Path(m));
            b.Add($"carpet{m}", Carpet(m));
            for (int f = 0; f < WaterFrames; f++)
            {
                b.Add($"water{m}_{f}", Water(m, f));
                b.Add($"channel{m}_{f}", Channel(m, f));
            }
        }
        for (int f = 0; f < WaterFrames; f++) b.Add($"falls{f}", Falls(f));
        b.Add("bridge_top", Bridge(top: true));
        b.Add("bridge_bottom", Bridge(top: false));
        b.Add("rock", Rock());
        for (int m = 0; m < 4; m++) b.Add($"cliff{m}", Cliff((m & 1) != 0, (m & 2) != 0));
        b.Add("bush", Bush());
        for (int m = 0; m < 4; m++) b.Add($"fence{m}", Fence((m & 1) != 0, (m & 2) != 0));
        for (int v = 0; v < 3; v++) b.Add($"ruin{v}", RuinFloor(v));
        b.Add("stairs", Stairs(true, true));
        b.Add("stairs_l", Stairs(true, false));
        b.Add("stairs_m", Stairs(false, false));
        b.Add("stairs_r", Stairs(false, true));
        for (int v = 0; v < 4; v++) b.Add($"floor{v}", Floor(v));
        b.Add("cracked", Cracked());
        b.Add("plate_up", Plate(false));
        b.Add("plate_down", Plate(true));
        b.Add("statue", Statue());
        b.Add("brazier", Brazier());
        foreach (var (name, img) in Walls()) b.Add(name, img);
    }

    // ---------------------------------------------------------------- overworld ground

    public static PixelImage Grass(int variant)
    {
        var img = new PixelImage(S, S);
        ArtKit.Noise(img, 0, 0, S, S, [Pal.Grass[1], Pal.Grass[2], Pal.Grass[2], Pal.Grass[3]], 7f, 11 + variant, 0.15f, 0.8f);
        // Tufts: a light tip over a darker stem, randomly placed per variant.
        for (int k = 0; k < 5 + variant; k++)
        {
            int x = (int)(Art.Hash(k, variant, 3) * 14) + 1, y = (int)(Art.Hash(variant, k, 5) * 13) + 1;
            img[x, y] = Pal.Grass[4];
            img[x, y + 1] = Pal.Grass[3];
            img[x - 1, y + 2] = Pal.Grass[1];
            img[x + 1, y + 2] = Pal.Grass[1];
        }
        return img;
    }

    private static PixelImage Flowers(int variant, int frame)
    {
        var img = Grass(variant + 1);
        Color[] petals = variant == 0 ? [Pal.White, Pal.Gold[3]] : [Pal.Heart[2], Pal.Heart[3]];
        (int X, int Y)[] spots = variant == 0 ? [(3, 4), (11, 3), (7, 11), (13, 12)] : [(4, 10), (10, 6), (2, 2), (12, 13)];
        int i = 0;
        foreach (var (sx, sy) in spots)
        {
            int x = sx + (frame == 1 && i % 2 == 0 ? 1 : 0);
            img[sx, sy + 2] = Pal.Grass[1];
            img[x, sy - 1] = petals[0];
            img[x - 1, sy] = petals[0];
            img[x + 1, sy] = petals[0];
            img[x, sy + 1] = petals[1];
            img[x, sy] = Pal.Gold[2];
            i++;
        }
        return img;
    }

    private static PixelImage TallGrass(int frame)
    {
        var img = Grass(2);
        for (int k = 0; k < 7; k++)
        {
            int x = 1 + k * 2 + (k % 2), baseY = 14 - (k % 3);
            int height = 6 + (int)(Art.Hash(k, 1, 77) * 4);
            int lean = frame == 0 ? (k % 2 == 0 ? 1 : 0) : (k % 2 == 0 ? 0 : -1);
            for (int h = 0; h < height; h++)
            {
                int xx = x + (h > height / 2 ? lean : 0);
                img[xx, baseY - h] = h < 2 ? Pal.Grass[0] : h < height - 2 ? Pal.Grass[2] : Pal.Grass[4];
                if (h < height - 1) img[xx + 1, baseY - h] = h < 3 ? Pal.Grass[1] : Pal.Grass[3];
            }
        }
        return img;
    }

    private static PixelImage Path(int mask)
    {
        var img = new PixelImage(S, S);
        ArtKit.Noise(img, 0, 0, S, S, [Pal.Sand[1], Pal.Sand[2], Pal.Sand[2], Pal.Sand[3]], 5f, 21, 0.2f, 0.8f);
        for (int k = 0; k < 6; k++)
        {
            int x = (int)(Art.Hash(k, mask, 8) * 14) + 1, y = (int)(Art.Hash(mask, k, 9) * 14) + 1;
            img[x, y] = Pal.Sand[4];
            img[x, y + 1] = Pal.Sand[0];
        }
        var grass = Grass(mask % 4);
        // Grass fringe of 2-4 px on every side whose neighbour is not path.
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int edge = 99;
                if ((mask & 1) == 0) edge = Math.Min(edge, y);
                if ((mask & 4) == 0) edge = Math.Min(edge, S - 1 - y);
                if ((mask & 8) == 0) edge = Math.Min(edge, x);
                if ((mask & 2) == 0) edge = Math.Min(edge, S - 1 - x);
                int depth = 2 + (int)(Art.Hash(x / 2, y / 2, 4) * 2.5f);
                if (edge < depth) img[x, y] = grass[x, y];
                else if (edge == depth) img[x, y] = Pal.Sand[0];
                else if (edge == depth + 1 && Art.Dither(x, y, 0.5f)) img[x, y] = Pal.Sand[1];
            }
        return img;
    }

    private static PixelImage Water(int mask, int frame)
    {
        var img = new PixelImage(S, S);
        ArtKit.Noise(img, 0, 0, S, S, [Pal.Water[1], Pal.Water[2], Pal.Water[2]], 6f, 31, 0.1f, 0.9f);
        // Drifting ripple highlights.
        for (int k = 0; k < 4; k++)
        {
            int y = 2 + k * 4;
            int x = ((int)(Art.Hash(k, 0, 17) * 16) + frame * 2 + k) % S;
            for (int i = 0; i < 3 + k % 2; i++) img[(x + i) % S, y] = Pal.Water[3];
            img[(x + 1) % S, y - 1] = Pal.Water[4].WithAlpha((byte)160);
        }
        Shore(img, mask, frame, Pal.Water, Pal.Sand[3], Pal.Sand[1]);
        return img;
    }

    /// <summary>Wet bank and foam on every side that touches land.</summary>
    private static void Shore(PixelImage img, int mask, int frame, Color[] ramp, Color bank, Color bankShade)
    {
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int edge = 99;
                if ((mask & 1) == 0) edge = Math.Min(edge, y);
                if ((mask & 4) == 0) edge = Math.Min(edge, S - 1 - y);
                if ((mask & 8) == 0) edge = Math.Min(edge, x);
                if ((mask & 2) == 0) edge = Math.Min(edge, S - 1 - x);
                if (edge == 0) img[x, y] = bank;
                else if (edge == 1) img[x, y] = bankShade;
                else if (edge == 2) img[x, y] = (x + y + frame) % 4 < 2 ? ramp[4] : ramp[3];
                else if (edge == 3 && (x + y * 3 + frame * 2) % 5 == 0) img[x, y] = ramp[3];
                else if (edge == 4) img[x, y] = Color.Lerp(img[x, y], ramp[0], 0.35f);
            }
    }

    private static PixelImage Falls(int frame)
    {
        var img = new PixelImage(S, S);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float streak = Art.Hash(x, 0, 41);
                int phase = (y - frame * 4 + (int)(streak * 16) + 64) % 16;
                img[x, y] = phase < 2 ? Pal.Water[4] : phase < 6 ? Pal.Water[3] : streak > 0.6f ? Pal.Water[2] : Pal.Water[1];
            }
        return img;
    }

    private static PixelImage Bridge(bool top)
    {
        var img = new PixelImage(S, S);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int plank = x % 4;
                img[x, y] = plank == 3 ? Pal.Wood[0] : plank == 0 ? Pal.Wood[3] : Art.Hash(x, y, 51) > 0.85f ? Pal.Wood[1] : Pal.Wood[2];
            }
        // Nails and a rail with posts along the open side.
        for (int x = 1; x < S; x += 4) { img[x, 3] = Pal.Rock[3]; img[x, 12] = Pal.Rock[3]; }
        int railY = top ? 0 : S - 4;
        img.FillRect(0, railY, S, 3, Pal.Wood[1]);
        img.FillRect(0, railY, S, 1, Pal.Wood[3]);
        img.FillRect(0, railY + 3, S, 1, Pal.Ink.WithAlpha((byte)140));
        img.FillRect(1, railY - (top ? 0 : 2), 2, 5, Pal.Wood[0]);
        img.FillRect(13, railY - (top ? 0 : 2), 2, 5, Pal.Wood[0]);
        return img;
    }

    private static PixelImage Rock()
    {
        var img = Grass(1);
        ArtKit.Shadow(img, 8.5f, 12.5f, 7f, 3.5f, 110);
        var boulder = new PixelImage(S, S);
        ArtKit.Blob(boulder, 8f, 8.5f, 6.5f, 5.8f, Pal.Rock, 61, 0.2f);
        boulder[5, 6] = Pal.Rock[4];
        boulder[6, 5] = Pal.Rock[4];
        boulder.Line(9, 9, 11, 11, Pal.Rock[0]);
        boulder.Outline(Pal.Ink);
        img.Blit(boulder, 0, 0);
        return img;
    }

    private static PixelImage Cliff(bool lipOnTop, bool footAtBottom)
    {
        var img = new PixelImage(S, S);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float strata = Art.ValueNoise(x * 0.6f, y * 2.2f, 4f, 71);
                img[x, y] = Art.Ramp([Pal.Rock[0], Pal.Rock[1], Pal.Rock[2], Pal.Rock[1]], strata, x, y);
                if ((y + (int)(Art.Hash(x / 4, 0, 3) * 4)) % 7 == 0) img[x, y] = Pal.Rock[0];
                if ((x + (int)(Art.Hash(0, y / 5, 4) * 6)) % 6 == 0 && y % 7 != 0) img[x, y] = Color.Lerp(img[x, y], Pal.Rock[3], 0.4f);
            }
        if (lipOnTop)
        {
            for (int x = 0; x < S; x++)
            {
                int depth = 3 + (int)(Art.Hash(x / 3, 1, 72) * 2);
                for (int y = 0; y < depth; y++) img[x, y] = y == 0 ? Pal.Grass[4] : Pal.Grass[3 - Math.Min(2, y / 2)];
                img[x, depth] = Pal.Grass[0];
            }
        }
        if (footAtBottom) img.FillRect(0, S - 2, S, 2, Pal.Ink.WithAlpha((byte)120));
        return img;
    }

    private static PixelImage Bush()
    {
        var img = Grass(3);
        ArtKit.Shadow(img, 8.5f, 13f, 7f, 3f, 100);
        var leaves = new PixelImage(S, S);
        ArtKit.Blob(leaves, 8f, 8f, 6.8f, 6.2f, Pal.Leaf, 81, 0.35f);
        foreach (var (x, y) in new[] { (4, 5), (7, 3), (10, 5), (5, 8), (9, 9), (12, 8) })
        {
            leaves[x, y] = Pal.Leaf[4];
            leaves[x + 1, y + 1] = Pal.Leaf[1];
        }
        leaves.Outline(Pal.Leaf[0]);
        img.Blit(leaves, 0, 0);
        return img;
    }

    private static PixelImage Fence(bool left, bool right)
    {
        var img = Grass(0);
        int x0 = left ? 0 : 3, x1 = right ? S : 13;
        foreach (int ry in new[] { 6, 10 })
        {
            img.FillRect(x0, ry, x1 - x0, 2, Pal.Wood[2]);
            img.FillRect(x0, ry, x1 - x0, 1, Pal.Wood[3]);
            img.FillRect(x0, ry + 2, x1 - x0, 1, Pal.Ink.WithAlpha((byte)120));
        }
        foreach (int px in new[] { 3, 11 })
        {
            img.FillRect(px, 3, 2, 11, Pal.Wood[1]);
            img.FillRect(px, 3, 1, 11, Pal.Wood[2]);
            img[px, 3] = Pal.Wood[3];
            img.FillRect(px, 14, 3, 1, Pal.Ink.WithAlpha((byte)110));
        }
        return img;
    }

    public static PixelImage RuinFloor(int variant)
    {
        var img = new PixelImage(S, S);
        Color[] stone = [Color.Hex("5f5866"), Color.Hex("7d7584"), Color.Hex("9a92a0"), Color.Hex("b8b0b8")];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int sx = (x + (y / 8 % 2) * 4) % 8, sy = y % 8;
                bool gap = sx == 7 || sy == 7;
                float n = Art.ValueNoise(x, y, 4, 91 + variant);
                img[x, y] = gap ? Pal.Grass[1] : sx == 0 || sy == 0 ? stone[3] : Art.Ramp(stone, 0.25f + n * 0.5f, x, y);
                if (gap && Art.Hash(x, y, variant) > 0.6f) img[x, y] = Pal.Grass[2];
            }
        if (variant > 0)
            for (int k = 0; k < 4 * variant; k++)
            {
                int x = (int)(Art.Hash(k, variant, 92) * 15), y = (int)(Art.Hash(variant, k, 93) * 15);
                img[x, y] = Pal.Moss[1 + k % 2];
                img[x + 1, y] = Pal.Moss[k % 3];
            }
        return img;
    }

    /// <summary>
    /// One tile of a stairwell cut into the ruin floor: a stone lintel across the top, cheek stones on
    /// the outer sides only (<paramref name="left"/>/<paramref name="right"/>), and four steps that catch
    /// the daylight at the bottom and sink into darkness toward the back.
    /// </summary>
    private static PixelImage Stairs(bool left, bool right)
    {
        var img = RuinFloor(0);
        Color[] stone = [Color.Hex("5f5866"), Color.Hex("7d7584"), Color.Hex("9a92a0"), Color.Hex("b8b0b8"), Color.Hex("d4ccd0")];
        int x0 = left ? 2 : 0, x1 = right ? 14 : 16;
        // The well itself: darkest at the back.
        for (int y = 3; y < S; y++)
            for (int x = x0; x < x1; x++)
                img[x, y] = Color.Lerp(Pal.Ink, Pal.Night, (y - 3) / 13f);
        // Steps from the back (dim, shallow) to the front (lit, deep): each is a tread and a shadowed riser.
        int[] tops = [4, 6, 9, 12];
        for (int step = 0; step < 4; step++)
        {
            int top = tops[step], bottom = step < 3 ? tops[step + 1] : S;
            float light = MathF.Pow((step + 1) / 4f, 1.4f);
            var tread = Color.Lerp(Pal.Ink, stone[2], light);
            var nosing = Color.Lerp(Pal.Ink, stone[3], light);
            var riser = Color.Lerp(Pal.Ink, stone[0], light * 0.7f);
            for (int x = x0; x < x1; x++)
                for (int y = top; y < bottom; y++)
                {
                    var c = y == top ? nosing : y == bottom - 1 && step < 3 ? riser : tread;
                    if (y > top && y < bottom - 1 && Art.Dither(x, y, 0.25f)) c = Color.Lerp(c, stone[1], 0.35f * light);
                    img[x, y] = c;
                }
            // Worn chips along the nosing.
            int cx = x0 + 1 + (int)(Art.Hash(step, 1, 77) * (x1 - x0 - 2));
            img[cx, top] = tread;
        }
        // Cheek stones and their inner shadow.
        foreach (var (on, cx, inner) in new[] { (left, 0, 2), (right, 14, 13) })
        {
            if (!on) continue;
            for (int y = 0; y < S; y++)
            {
                img[cx, y] = stone[3];
                img[cx + 1, y] = stone[1];
                if (y > 2) img[inner, y] = Color.Lerp(img[inner, y], Pal.Ink, 0.5f);
            }
            img[cx, 0] = stone[4];
        }
        // Lintel across the top with a hard shadow beneath it.
        for (int x = 0; x < S; x++)
        {
            img[x, 0] = stone[4];
            img[x, 1] = stone[3];
            img[x, 2] = stone[1];
            if (x >= x0 && x < x1) img[x, 3] = Pal.Ink;
        }
        if (left) img[0, 0] = stone[3];
        if (right) img[15, 0] = stone[3];
        return img;
    }

    // ---------------------------------------------------------------- dungeon

    public static PixelImage Floor(int variant)
    {
        var img = new PixelImage(S, S);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float n = Art.ValueNoise(x + variant * 16, y, 6, 101);
                var c = n > 0.62f && Art.Dither(x, y, 0.5f) ? Pal.Stone[3] : Pal.Stone[2];
                if (x == 15 || y == 15) c = Pal.Stone[1];
                else if (x == 0 || y == 0) c = Color.Lerp(Pal.Stone[2], Pal.Stone[4], 0.5f);
                img[x, y] = c;
            }
        if (variant == 1) { img.Line(3, 3, 6, 6, Pal.Stone[1]); img.Line(6, 6, 6, 9, Pal.Stone[1]); }
        if (variant == 2)
            for (int k = 0; k < 6; k++) img[(int)(Art.Hash(k, 2, 7) * 6) + 9, (int)(Art.Hash(2, k, 8) * 6) + 9] = Pal.Moss[k % 3];
        if (variant == 3) { img[11, 3] = Pal.Stone[5]; img[4, 12] = Pal.Stone[4]; }
        return img;
    }

    private static PixelImage Cracked()
    {
        var img = Floor(0);
        img.Line(2, 3, 7, 8, Pal.Stone[0]);
        img.Line(7, 8, 6, 13, Pal.Stone[0]);
        img.Line(7, 8, 13, 10, Pal.Stone[0]);
        img.Line(3, 3, 8, 8, Pal.Stone[3]);
        return img;
    }

    private static PixelImage Carpet(int mask)
    {
        var img = Floor(mask % 4);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                bool weave = (x + y) % 4 == 0 || (x - y + 16) % 4 == 0;
                img[x, y] = weave ? Pal.Carpet[2] : Pal.Carpet[1];
            }
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int edge = 99;
                if ((mask & 1) == 0) edge = Math.Min(edge, y);
                if ((mask & 4) == 0) edge = Math.Min(edge, S - 1 - y);
                if ((mask & 8) == 0) edge = Math.Min(edge, x);
                if ((mask & 2) == 0) edge = Math.Min(edge, S - 1 - x);
                if (edge == 0) img[x, y] = Pal.Carpet[0];
                else if (edge == 1) img[x, y] = Pal.Gold[1];
                else if (edge == 2) img[x, y] = (x + y) % 2 == 0 ? Pal.Gold[2] : Pal.Gold[1];
                else if (edge == 3) img[x, y] = Pal.Carpet[0];
            }
        return img;
    }

    private static PixelImage Channel(int mask, int frame)
    {
        var img = new PixelImage(S, S);
        ArtKit.Noise(img, 0, 0, S, S, [Pal.DeepWater[0], Pal.DeepWater[1], Pal.DeepWater[2]], 6f, 111, 0.1f, 0.85f);
        for (int k = 0; k < 3; k++)
        {
            int y = 3 + k * 5;
            int x = ((int)(Art.Hash(k, 1, 113) * 16) + frame * 2) % S;
            for (int i = 0; i < 3; i++) img[(x + i) % S, y] = Pal.DeepWater[3];
        }
        // Stone rim with an inner shadow where the channel meets the floor.
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int edge = 99;
                if ((mask & 1) == 0) edge = Math.Min(edge, y);
                if ((mask & 4) == 0) edge = Math.Min(edge, S - 1 - y);
                if ((mask & 8) == 0) edge = Math.Min(edge, x);
                if ((mask & 2) == 0) edge = Math.Min(edge, S - 1 - x);
                if (edge == 0) img[x, y] = Pal.Stone[4];
                else if (edge == 1) img[x, y] = Pal.Stone[2];
                else if (edge == 2) img[x, y] = Pal.Ink;
                else if (edge == 3 && (x + y + frame) % 3 == 0) img[x, y] = Pal.DeepWater[4];
            }
        return img;
    }

    private static PixelImage Plate(bool pressed)
    {
        var img = Floor(0);
        img.FillRect(2, 2, 12, 12, Pal.Stone[0]);
        if (pressed)
        {
            img.FillRect(3, 3, 10, 10, Pal.Stone[1]);
            img.FillRect(5, 5, 6, 6, Pal.Spirit[1]);
        }
        else
        {
            img.FillRect(3, 2, 10, 10, Pal.Stone[4]);
            img.FillRect(3, 2, 10, 1, Pal.Stone[5]);
            img.FillRect(3, 12, 10, 2, Pal.Stone[2]);
            img.FillRect(6, 5, 4, 4, Pal.Stone[3]);
            img.DrawRect(5, 4, 6, 6, Pal.Stone[2]);
        }
        return img;
    }

    private static PixelImage Statue()
    {
        var img = Floor(3);
        ArtKit.Shadow(img, 8.5f, 14f, 7f, 2.5f, 120);
        var s = ArtKit.Map($"o={ArtKit.Hex(Pal.Ink)} a={ArtKit.Hex(Pal.Stone[1])} b={ArtKit.Hex(Pal.Stone[2])} c={ArtKit.Hex(Pal.Stone[3])} d={ArtKit.Hex(Pal.Stone[4])} e={ArtKit.Hex(Pal.Stone[5])} m={ArtKit.Hex(Pal.Moss[1])} r={ArtKit.Hex(Pal.Ember[1])}",
            "..oo........oo..",
            ".odco......ocdo.",
            ".odcco....occbo.",
            "..occcooooccbo..",
            "...ocddddccbo...",
            "..ocdeddccccbo..",
            "..ocdrcccrcbbo..",
            "..ocdccccccbbo..",
            "...ocdcmmcbbo...",
            "..ocddccccbbbo..",
            ".ocddccccccbbbo.",
            ".ocdcccmccccbbo.",
            ".occccccccccbbo.",
            ".obbbbbbbbbbbao.",
            ".oaaaaaaaaaaaao.",
            "..oooooooooooo..");
        img.Blit(s, 0, 0);
        return img;
    }

    private static PixelImage Brazier()
    {
        var img = Floor(1);
        ArtKit.Shadow(img, 8.5f, 14f, 6f, 2f, 120);
        var b = ArtKit.Map($"o={ArtKit.Hex(Pal.Ink)} a={ArtKit.Hex(Pal.Iron[0])} b={ArtKit.Hex(Pal.Iron[1])} c={ArtKit.Hex(Pal.Iron[2])} d={ArtKit.Hex(Pal.Iron[3])} e={ArtKit.Hex(Pal.Ember[0])} f={ArtKit.Hex(Pal.Ember[1])}",
            "................",
            "................",
            "................",
            "................",
            "................",
            ".oooooooooooooo.",
            ".odccccccccccbo.",
            ".obfefffeffefao.",
            "..obbbbbbbbbao..",
            "...oabbbbbbao...",
            ".....obbbbo.....",
            "......obbo......",
            "......obco......",
            ".....obbbbo.....",
            "....obbbbbbo....",
            "....oooooooo....");
        img.Blit(b, 0, 0);
        return img;
    }

    /// <summary>Wall pieces named wall_n/s/w/e/nw/ne/sw/se plus door frames and the torch sconce.</summary>
    private static IEnumerable<(string, PixelImage)> Walls()
    {
        var brickFace = BrickFace();
        // North wall: cap on top, brick face below, shadow at the floor line.
        var n = new PixelImage(S, S);
        n.Blit(brickFace, 0, 0);
        CapRows(n, 0, 4);
        n.FillRect(0, S - 1, S, 1, Pal.Ink);
        yield return ("wall_n", n);
        var torch = n.Clone();
        torch.FillRect(6, 9, 4, 5, Pal.Iron[1]);
        torch.FillRect(6, 9, 4, 1, Pal.Iron[3]);
        torch.FillRect(7, 14, 2, 1, Pal.Iron[0]);
        torch.FillRect(5, 8, 6, 1, Pal.Iron[2]);
        yield return ("wall_torch", torch);
        yield return ("wall_s", CapTile().Rotate90().Rotate90());
        yield return ("wall_w", CapTile().Rotate90().Rotate90().Rotate90());
        yield return ("wall_e", CapTile().Rotate90());
        var corner = new PixelImage(S, S);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
                corner[x, y] = Art.Ramp([Pal.Stone[0], Pal.Stone[1], Pal.Stone[1]], Art.ValueNoise(x, y, 5, 121), x, y);
        corner.DrawRect(1, 1, 14, 14, Pal.Stone[2]);
        corner.FillRect(4, 4, 8, 8, Pal.Stone[1]);
        corner.DrawRect(4, 4, 8, 8, Pal.Stone[3]);
        corner[5, 5] = Pal.Stone[4];
        foreach (var dir in new[] { "nw", "ne", "sw", "se" }) yield return ($"wall_{dir}", corner);
        // Door frames: an arch around the opening; the door panel itself is drawn by the door entity.
        // "a" is the first cell of the two-cell opening in reading order (left or top).
        PixelImage a = DoorFrame(pillarLeft: true), b = DoorFrame(pillarLeft: false);
        yield return ("frame_n_a", a);
        yield return ("frame_n_b", b);
        yield return ("frame_e_a", a.Rotate90());
        yield return ("frame_e_b", b.Rotate90());
        yield return ("frame_s_a", b.Rotate90().Rotate90());
        yield return ("frame_s_b", a.Rotate90().Rotate90());
        yield return ("frame_w_a", b.Rotate90().Rotate90().Rotate90());
        yield return ("frame_w_b", a.Rotate90().Rotate90().Rotate90());
    }

    private static PixelImage BrickFace()
    {
        var img = new PixelImage(S, S);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int row = y / 4, bx = (x + (row % 2) * 4) % 8;
                bool mortar = y % 4 == 3 || bx == 7;
                float n = Art.ValueNoise(x, y, 3, 131 + row);
                var c = mortar ? Pal.Stone[0] : Art.Ramp([Pal.Stone[2], Pal.Stone[3], Pal.Stone[3]], n, x, y);
                if (!mortar && y % 4 == 0) c = Pal.Stone[4];
                img[x, y] = c;
            }
        // Darker toward the floor for depth.
        for (int y = 10; y < S; y++)
            for (int x = 0; x < S; x++) img[x, y] = Color.Lerp(img[x, y], Pal.Stone[0], (y - 9) / 9f);
        return img;
    }

    /// <summary>The flat top of a wall seen from above, light edge toward the room (down).</summary>
    private static PixelImage CapTile()
    {
        var img = new PixelImage(S, S);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
                img[x, y] = Art.Ramp([Pal.Stone[0], Pal.Stone[1], Pal.Stone[1], Pal.Stone[2]], Art.ValueNoise(x, y, 4, 141), x, y);
        for (int x = 0; x < S; x++)
        {
            img[x, 0] = Pal.Stone[4];
            img[x, 1] = Pal.Stone[3];
            img[x, 2] = Pal.Stone[2];
            if (x % 8 == 3) img.FillRect(x, 4, 1, 8, Pal.Stone[0]);
        }
        img.FillRect(0, S - 1, S, 1, Pal.Ink);
        return img.Rotate90().Rotate90();
    }

    private static void CapRows(PixelImage img, int y0, int rows)
    {
        for (int y = y0; y < y0 + rows; y++)
            for (int x = 0; x < S; x++)
                img[x, y] = y == y0 + rows - 1 ? Pal.Stone[4] : Art.Ramp([Pal.Stone[0], Pal.Stone[1], Pal.Stone[1]], Art.ValueNoise(x, y, 4, 151), x, y);
    }

    private static PixelImage DoorFrame(bool pillarLeft)
    {
        var img = Floor(0);
        // The passage darkens toward the outside of the room.
        for (int y = 0; y < 12; y++)
            img.FillRect(0, y, S, 1, Color.Lerp(Pal.Ink, Pal.Stone[1], y / 14f));
        int px = pillarLeft ? 0 : S - 4;
        img.FillRect(px, 0, 4, S, Pal.Stone[3]);
        img.FillRect(px + (pillarLeft ? 0 : 3), 0, 1, S, pillarLeft ? Pal.Stone[5] : Pal.Stone[1]);
        img.FillRect(px + (pillarLeft ? 3 : 0), 0, 1, S, Pal.Ink);
        for (int y = 3; y < S; y += 4) img.FillRect(px, y, 4, 1, Pal.Stone[2]);
        return img;
    }
}

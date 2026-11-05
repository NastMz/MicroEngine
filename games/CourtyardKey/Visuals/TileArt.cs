using MicroEngine;
using MicroEngine.Graphics;

namespace CourtyardKey.Visuals;

/// <summary>
/// Procedural 16x16 terrain drawn whole (the layered grass, paths, river, ruin paving and flagstones are
/// <see cref="GroundArt"/> and <see cref="DungeonArt"/>). Autotiled terrains (carpet, channel) are generated for all sixteen
/// 4-neighbour masks (1 = up, 2 = right, 4 = down, 8 = left match), animated ones per frame. Everything draws from
/// the world ramps of <see cref="Pal"/> so the river, the cliffs and the ruins sit in the same light as the grass.
/// </summary>
public static class TileArt
{
    public const int S = 16;
    public const int WaterFrames = GroundArt.WaterFrames;

    public static void AddAll(SpriteAtlasBuilder b)
    {
        for (int m = 0; m < 16; m++)
        {
            b.Add($"carpet{m}", Carpet(m));
            for (int f = 0; f < WaterFrames; f++)
            {
                b.Add($"channel{m}_{f}", Channel(m, f));
            }
        }
        for (int f = 0; f < WaterFrames; f++) b.Add($"falls{f}", Falls(f));
        b.Add("bridge_top", Bridge(top: true));
        b.Add("bridge_bottom", Bridge(top: false));
        for (int m = 0; m < 4; m++)
            for (int v = 0; v < 4; v++) b.Add($"cliff{m}_{v}", Cliff((m & 1) != 0, (m & 2) != 0, v % 2, v / 2));
        b.Add("stairs", Stairs(true, true));
        b.Add("stairs_l", Stairs(true, false));
        b.Add("stairs_m", Stairs(false, false));
        b.Add("stairs_r", Stairs(false, true));
    }

    // ---------------------------------------------------------------- overworld

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
        var w = Pal.Timber;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int plank = x % 4;
                img[x, y] = plank == 3 ? w[1] : plank == 0 ? w[4] : Art.Hash(x, y, 51) > 0.85f ? w[2] : w[3];
            }
        // Nails and a rail with posts along the open side.
        for (int x = 1; x < S; x += 4) { img[x, 3] = Pal.Boulder[4]; img[x, 12] = Pal.Boulder[4]; }
        int railY = top ? 0 : S - 4;
        img.FillRect(0, railY, S, 3, w[2]);
        img.FillRect(0, railY, S, 1, w[4]);
        img.FillRect(0, railY + 3, S, 1, Pal.CastShadow.WithAlpha((byte)150));
        img.FillRect(1, railY - (top ? 0 : 2), 2, 5, w[1]);
        img.FillRect(13, railY - (top ? 0 : 2), 2, 5, w[1]);
        return img;
    }

    /// <summary>
    /// A cliff face of upright rock slabs, each lit on its left edge and broken by ledges, keyed to world position
    /// (period 32: <paramref name="vx"/>, <paramref name="vy"/>). A grassy lip crowns the top row and the foot sinks
    /// into the cool cast shadow.
    /// </summary>
    private static PixelImage Cliff(bool lipOnTop, bool footAtBottom, int vx, int vy)
    {
        var img = new PixelImage(S, S);
        var r = Pal.Boulder;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int wx = vx * S + x, wy = vy * S + y;
                // Rock clumps: each pixel belongs to its nearest seed (wrapping every 32 px); a pixel near the
                // clump's edge toward the upper left catches the light, one toward the lower right is in shade.
                var (cell, edge, away) = Clump(wx, wy);
                float n = ArtKit.PeriodicNoise(wx, wy, 4, 32, 74) * 0.35f + Art.Hash(cell, 1, 73) * 0.3f + 0.2f;
                Color c = ArtKit.Band([r[2], r[3], r[3], r[4]], n, wx, wy, 0.25f);
                if (edge < 1.2f) c = away ? r[0] : r[1];
                else if (edge < 2.4f) c = away ? r[1] : r[5];
                else if (edge < 3.6f && !away) c = r[4];
                // The face darkens toward its foot.
                if (vy == 1) c = Color.Lerp(c, r[0], y / 40f);
                img[x, y] = c;
            }
        if (lipOnTop)
        {
            var g = Pal.Turf;
            for (int x = 0; x < S; x++)
            {
                int wx = vx * S + x;
                int depth = 3 + (int)(Art.Hash(wx / 3, 1, 72) * 3);
                for (int y = 0; y < depth; y++) img[x, y] = y == 0 ? g[5] : g[4 - Math.Min(2, y / 2)];
                img[x, depth] = g[1];
                if (Art.Hash(wx, 2, 72) > 0.7f) img[x, depth + 1] = g[2];
                img[x, depth + 2] = Color.Lerp(img[x, depth + 2], r[0], 0.6f);
            }
        }
        if (footAtBottom)
            for (int x = 0; x < S; x++)
            {
                img[x, S - 1] = Pal.Ink;
                img[x, S - 2] = Color.Lerp(img[x, S - 2], Pal.CastShadow, 0.6f);
            }
        return img;
    }

    private static readonly (float X, float Y)[] ClumpSeeds = [(4, 3), (17, 5), (27, 1), (10, 13), (24, 15), (3, 23), (15, 24), (28, 27), (20, 31)];

    /// <summary>The rock clump a cliff pixel belongs to, its distance to the clump's edge, and whether that edge faces away from the light.</summary>
    private static (int Cell, float Edge, bool Away) Clump(int x, int y)
    {
        float best = float.MaxValue, second = float.MaxValue;
        int cell = 0;
        (float X, float Y) bp = default, op = default;
        for (int i = 0; i < ClumpSeeds.Length; i++)
            for (int ox = -32; ox <= 32; ox += 32)
                for (int oy = -32; oy <= 32; oy += 32)
                {
                    float sx = ClumpSeeds[i].X + ox, sy = ClumpSeeds[i].Y + oy;
                    // Clumps are wider than tall, like weathered strata.
                    float d = (x + 0.5f - sx) * (x + 0.5f - sx) * 0.55f + (y + 0.5f - sy) * (y + 0.5f - sy);
                    if (d < best) { second = best; op = bp; best = d; cell = i; bp = (sx, sy); }
                    else if (d < second) { second = d; op = (sx, sy); }
                }
        float edge = MathF.Sqrt(second) - MathF.Sqrt(best);
        // The neighbouring clump lies to the lower right: this pixel is on its own clump's shaded side.
        bool away = (op.X - bp.X) + (op.Y - bp.Y) > 0;
        return (cell, edge, away);
    }

    private static readonly Color[] RuinStone = [Pal.Boulder[2], Pal.Boulder[3], Pal.Boulder[4], Pal.Boulder[5], Pal.Boulder[6]];

    /// <summary>
    /// One tile of a stairwell cut into the ruin floor: a stone lintel across the top, cheek stones on
    /// the outer sides only (<paramref name="left"/>/<paramref name="right"/>), and four steps that catch
    /// the daylight at the bottom and sink into darkness toward the back.
    /// </summary>
    private static PixelImage Stairs(bool left, bool right)
    {
        var img = GroundArt.RuinStones().Crop(0, 0, S, S);
        var stone = RuinStone;
        int x0 = left ? 2 : 0, x1 = right ? 14 : 16;
        // The well itself: darkest at the back.
        for (int y = 3; y < S; y++)
            for (int x = x0; x < x1; x++)
                img[x, y] = Color.Lerp(Pal.DeepShadow, Pal.Night, (y - 3) / 13f);
        // Steps from the back (dim, shallow) to the front (lit, deep): each is a tread and a shadowed riser.
        int[] tops = [4, 6, 9, 12];
        for (int step = 0; step < 4; step++)
        {
            int top = tops[step], bottom = step < 3 ? tops[step + 1] : S;
            float light = MathF.Pow((step + 1) / 4f, 1.4f);
            var tread = Color.Lerp(Pal.DeepShadow, stone[2], light);
            var nosing = Color.Lerp(Pal.DeepShadow, stone[3], light);
            var riser = Color.Lerp(Pal.DeepShadow, stone[0], light * 0.7f);
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
                if (y > 2) img[inner, y] = Color.Lerp(img[inner, y], Pal.DeepShadow, 0.5f);
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

    private static PixelImage Carpet(int mask)
    {
        var img = new PixelImage(S, S);
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
        var st = Pal.Flagstone;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int edge = 99;
                if ((mask & 1) == 0) edge = Math.Min(edge, y);
                if ((mask & 4) == 0) edge = Math.Min(edge, S - 1 - y);
                if ((mask & 8) == 0) edge = Math.Min(edge, x);
                if ((mask & 2) == 0) edge = Math.Min(edge, S - 1 - x);
                if (edge == 0) img[x, y] = st[5];
                else if (edge == 1) img[x, y] = st[3];
                else if (edge == 2) img[x, y] = Pal.DeepShadow;
                else if (edge == 3 && (x + y + frame) % 3 == 0) img[x, y] = Pal.DeepWater[4];
            }
        return img;
    }
}

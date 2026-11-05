using MicroEngine;
using MicroEngine.Graphics;

namespace CourtyardKey.Visuals;

/// <summary>
/// The vale's layered ground. A macro-textured grass that only repeats every 8x8 tiles is drawn under every
/// ground tile (<c>ground0</c>..<c>ground63</c>, picked by world position); paths, flowers, tall grass, rocks,
/// bushes and fences are transparent overlays on top, so nothing re-bakes its own patch of grass and the 16 px
/// grid stops showing. The river and the ruin paving are overlays too, with the same ragged grassy edge as the paths.
/// Light comes from the upper left; shadows fall to the lower right in <see cref="Pal.CastShadow"/>.
/// </summary>
public static class GroundArt
{
    public const int S = 16, Period = 8, Macro = S * Period;
    public const int WaterFrames = 4;

    public static void AddAll(SpriteAtlasBuilder b)
    {
        var grass = MacroGrass();
        for (int ty = 0; ty < Period; ty++)
            for (int tx = 0; tx < Period; tx++)
                b.Add($"ground{tx + ty * Period}", grass.Crop(tx * S, ty * S, S, S));
        // Paths: 16 neighbour masks (1 = up, 2 = right, 4 = down, 8 = left) times four world-keyed variants.
        for (int m = 0; m < 16; m++)
            for (int v = 0; v < 4; v++) b.Add($"path{m}_{v}", PathOverlay(m, v % 2, v / 2));
        for (int f = 0; f < 2; f++)
        {
            for (int v = 0; v < 2; v++) b.Add($"flowers{v}_{f}", FlowerOverlay(v, f));
            b.Add($"tallgrass{f}", TallOverlay(f));
        }
        for (int m = 0; m < 16; m++)
            for (int v = 0; v < 4; v++)
            {
                b.Add($"ruin{m}_{v}", RuinOverlay(m, v % 2, v / 2));
                for (int f = 0; f < WaterFrames; f++) b.Add($"water{m}_{v}_{f}", WaterOverlay(m, v % 2, v / 2, f));
            }
        b.Add("rock", RockOverlay());
        b.Add("bush", BushOverlay());
        for (int m = 0; m < 4; m++) b.Add($"fence{m}", FenceOverlay((m & 1) != 0, (m & 2) != 0));
    }

    private static int Wrap(int v) => ((v % Macro) + Macro) % Macro;

    private static void Put(PixelImage img, int x, int y, Color c) => img[Wrap(x), Wrap(y)] = c;

    /// <summary>The 128x128 grass the vale tiles with: broad light patches, blade grain, tufts, clover, pebbles and wildflowers.</summary>
    public static PixelImage MacroGrass()
    {
        var img = new PixelImage(Macro, Macro);
        var g = Pal.Turf;
        Color[] body = [g[1], g[2], g[3], g[4], g[5]];
        for (int y = 0; y < Macro; y++)
            for (int x = 0; x < Macro; x++)
            {
                float broad = ArtKit.PeriodicNoise(x, y, 32, Macro, 501), mid = ArtKit.PeriodicNoise(x, y, 8, Macro, 502);
                float v = broad * 0.78f + mid * 0.22f;
                img[x, y] = ArtKit.Band(body, Math.Clamp((v - 0.18f) * 1.45f, 0, 1), x, y);
            }
        // Grain: short vertical blade strokes, lit tip over a shaded foot.
        for (int y = 0; y < Macro; y++)
            for (int x = 0; x < Macro; x++)
            {
                if (Art.Hash(x, y, 503) < 0.95f) continue;
                int i = Array.IndexOf(g, img[x, y]);
                if (i < 0) continue;
                img[x, y] = g[Math.Min(6, i + 1)];
                img[x, Wrap(y + 1)] = g[Math.Max(0, i - 1)];
            }
        // Tufts on a jittered 8 px lattice: a V of blades with lit tips and a shaded foot to the lower right.
        for (int cy = 0; cy < Macro / 8; cy++)
            for (int cx = 0; cx < Macro / 8; cx++)
            {
                float h = Art.Hash(cx, cy, 511);
                int x = cx * 8 + (int)(Art.Hash(cx, cy, 512) * 6) + 1, y = cy * 8 + (int)(Art.Hash(cx, cy, 513) * 6) + 1;
                if (h < 0.3f)
                {
                    Put(img, x + 1, y + 3, g[1]); Put(img, x + 2, y + 3, g[0]); Put(img, x + 3, y + 2, g[1]);
                    Put(img, x, y + 2, g[3]); Put(img, x + 1, y + 2, g[4]); Put(img, x + 2, y + 2, g[2]);
                    Put(img, x - 1, y + 1, g[4]); Put(img, x + 1, y + 1, g[5]); Put(img, x + 3, y + 1, g[4]);
                    Put(img, x - 1, y, g[6]); Put(img, x + 1, y, g[6]);
                    if (h < 0.1f) { Put(img, x + 1, y - 1, g[6]); Put(img, x + 3, y, g[5]); Put(img, x + 4, y + 2, g[1]); }
                }
                else if (h < 0.34f)
                {
                    // Clover patch.
                    foreach (var (dx, dy) in new[] { (0, 0), (2, 0), (1, 1), (3, 1), (0, 2), (2, 2) })
                    {
                        Put(img, x + dx, y + dy, g[5]);
                        Put(img, x + dx + 1, y + dy, g[4]);
                        Put(img, x + dx, y + dy + 1, g[2]);
                    }
                }
                else if (h < 0.38f)
                {
                    // A pebble sitting in the grass.
                    var r = Pal.Boulder;
                    Put(img, x, y, r[5]); Put(img, x + 1, y, r[4]); Put(img, x, y + 1, r[3]); Put(img, x + 1, y + 1, r[2]);
                    Put(img, x + 2, y + 1, g[0]); Put(img, x + 1, y + 2, g[0]); Put(img, x + 2, y + 2, g[1]);
                }
                else if (h < 0.43f)
                {
                    // A lone wildflower.
                    var petal = Art.Hash(cx, cy, 514) > 0.5f ? Pal.White : Color.Hex("f2d65a");
                    Put(img, x, y, petal); Put(img, x, y + 1, g[2]); Put(img, x + 1, y + 1, g[1]);
                }
            }
        return img;
    }

    /// <summary>
    /// Distance of a pixel inside a ragged grass margin on the sides <paramref name="mask"/> leaves open (negative:
    /// in the grass). The margin is <paramref name="least"/> to <paramref name="least"/> + <paramref name="spread"/>
    /// pixels deep, keyed to world position (period 32) so neighbouring tiles agree.
    /// </summary>
    private static (int D, bool Shaded) Margin(int mask, int x, int y, int wx, int wy, int least = 2, float spread = 3.2f)
    {
        int Rag(int along) => least + (int)(ArtKit.PeriodicNoise(along, 0, 4, 32, 521) * spread);
        int dTop = (mask & 1) == 0 ? y - Rag(wx) : 99;
        int dBot = (mask & 4) == 0 ? (S - 1 - y) - Rag(wx + 7) : 99;
        int dLeft = (mask & 8) == 0 ? x - Rag(wy + 3) : 99;
        int dRight = (mask & 2) == 0 ? (S - 1 - x) - Rag(wy + 11) : 99;
        int d = Math.Min(Math.Min(dTop, dBot), Math.Min(dLeft, dRight));
        // The upper and left banks face away from the light and throw their shade inward.
        return (d, d == dTop || d == dLeft);
    }

    /// <summary>Grass blades leaning over the edge of an overlay onto the grass below it.</summary>
    private static void Blades(PixelImage img, int ox, int oy)
    {
        var src = img.Clone();
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                if (src[x, y].A == 0) continue;
                bool nearGrass = (x > 0 && src[x - 1, y].A == 0) || (x < S - 1 && src[x + 1, y].A == 0) || (y > 0 && src[x, y - 1].A == 0) || (y < S - 1 && src[x, y + 1].A == 0);
                if (nearGrass && Art.Hash(ox + x, oy + y, 541) > 0.55f) img[x, y] = Art.Hash(ox + x, oy + y, 542) > 0.5f ? Pal.Turf[4] : Pal.Turf[3];
            }
    }

    /// <summary>A sunken dirt path with a ragged grass margin; <paramref name="vx"/>, <paramref name="vy"/> pick the world-keyed variant.</summary>
    private static PixelImage PathOverlay(int mask, int vx, int vy)
    {
        var img = new PixelImage(S, S);
        var sand = Pal.Earth;
        Color[] body = [sand[2], sand[3], sand[4]];
        int ox = vx * S, oy = vy * S;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int wx = ox + x, wy = oy + y;
                var (d, shaded) = Margin(mask, x, y, wx, wy);
                if (d < 0) continue;
                float n = ArtKit.PeriodicNoise(wx, wy, 8, 32, 522) * 0.6f + Art.Hash(wx, wy, 523) * 0.4f;
                var c = ArtKit.Band(body, n, wx, wy, 0.3f);
                // Sunken path: the upper/left bank throws shade in, the lower/right lip catches light.
                if (d == 0) c = shaded ? sand[1] : sand[4];
                else if (d == 1 && shaded) c = sand[2];
                img[x, y] = c;
            }
        // Pebbles (world keyed, period 32).
        for (int k = 0; k < 6; k++)
        {
            int px = (int)(Art.Hash(k, 0, 531) * 32), py = (int)(Art.Hash(k, 1, 531) * 32);
            int x = px - ox, y = py - oy;
            if (x < 1 || y < 1 || x > S - 3 || y > S - 3 || img[x, y].A == 0 || img[x + 1, y + 1].A == 0) continue;
            img[x, y] = sand[5]; img[x + 1, y] = sand[4]; img[x, y + 1] = sand[3];
            img[x + 1, y + 1] = sand[1];
            if (img[x + 2, y + 1].A > 0) img[x + 2, y + 1] = sand[1];
        }
        Blades(img, ox, oy);
        return img;
    }

    /// <summary>
    /// River water inside a ragged grassy bank: a muddy lip, the bank's shade on the upper and left sides, foam that
    /// laps the shore, and ripples drifting downstream. Body and ripples are keyed to world position (period 32).
    /// </summary>
    public static PixelImage WaterOverlay(int mask, int vx, int vy, int frame)
    {
        var img = new PixelImage(S, S);
        var w = Pal.Water;
        int ox = vx * S, oy = vy * S;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int wx = ox + x, wy = oy + y;
                var (d, shaded) = Margin(mask, x, y, wx, wy);
                if (d < 0) continue;
                // Body: broad swirls of the two mid blues; ripples are short light dashes that drift downstream.
                float n = ArtKit.PeriodicNoise(wx, wy, 8, 32, 561) * 0.75f + Art.Hash(wx, wy, 562) * 0.25f;
                var c = ArtKit.Band([w[1], w[2], w[2]], n, wx, wy, 0.25f);
                int ry = Wrap32(wy - frame * 2);
                if (Art.Hash(wx / 4, ry, 563) > 0.9f) c = w[3];
                else if (Art.Hash(wx / 4, Wrap32(ry + 1), 563) > 0.9f && Art.Dither(wx, ry, 0.5f)) c = Color.Lerp(w[2], w[3], 0.5f);
                if (d == 0) c = shaded ? Pal.Earth[1] : Pal.Earth[2];
                else if (d == 1) c = shaded ? w[0] : (wx + wy + frame) % 3 == 0 ? w[4] : w[3];
                else if (d == 2) c = shaded ? Color.Lerp(c, w[0], 0.45f) : (wx * 3 + wy + frame) % 4 == 0 ? w[3] : c;
                else if (d == 3 && !shaded && (wx + wy * 3 + frame * 2) % 7 == 0) c = w[4];
                img[x, y] = c;
            }
        Blades(img, ox, oy);
        return img;
    }

    private static int Wrap32(int v) => ((v % 32) + 32) % 32;

    /// <summary>
    /// Worn paving of the Old Ruins, laid into the grass: weathered stones (world keyed, period 32) with grass and lichen
    /// in the joints, and a ragged edge where the turf has crept over the paving.
    /// </summary>
    public static PixelImage RuinOverlay(int mask, int vx, int vy)
    {
        var img = new PixelImage(S, S);
        int ox = vx * S, oy = vy * S;
        var stones = RuinStones();
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int wx = ox + x, wy = oy + y;
                var (d, shaded) = Margin(mask, x, y, wx, wy, least: 0, spread: 2.2f);
                if (d < 0) continue;
                var c = stones[wx, wy];
                if (d == 0 && c != Pal.Turf[1] && c != Pal.Turf[2]) c = shaded ? Pal.Boulder[1] : Color.Lerp(c, Pal.Boulder[5], 0.4f);
                img[x, y] = c;
            }
        Blades(img, ox, oy);
        return img;
    }

    private static PixelImage? _ruinStones;

    /// <summary>A 32x32 block of ruin paving that tiles seamlessly: rectangular stones of a few sizes, lit from the upper left.</summary>
    public static PixelImage RuinStones()
    {
        if (_ruinStones is not null) return _ruinStones;
        const int P = 32;
        var img = new PixelImage(P, P);
        var r = Pal.Boulder;
        Color[] body = [r[2], r[3], r[4]];
        // Courses of stones: heights 8, 6, 10, 8 (sum 32); joints offset per course.
        int[] heights = [8, 6, 10, 8];
        int[][] widths = [[10, 12, 10], [8, 14, 10], [12, 9, 11], [9, 11, 12]];
        int top = 0, id = 0;
        for (int course = 0; course < heights.Length; course++)
        {
            int h = heights[course], left = (course * 5) % P;
            foreach (int w in widths[course])
            {
                id++;
                float tone = Art.Hash(id, 0, 571) * 0.3f;
                for (int ly = 0; ly < h; ly++)
                    for (int lx = 0; lx < w; lx++)
                    {
                        int x = (left + lx) % P, y = top + ly;
                        Color c;
                        if (lx == w - 1 || ly == h - 1) c = Art.Hash(x, y, 572) > 0.45f ? Pal.Turf[1] : Pal.Turf[2];
                        else if (lx == 0 || ly == 0) c = r[5];
                        else if (lx == w - 2 || ly == h - 2) c = r[2];
                        else c = ArtKit.Band(body, ArtKit.PeriodicNoise(x, y, 8, P, 573) * 0.6f + Art.Hash(x, y, 574) * 0.2f + tone, x, y, 0.3f);
                        img[x, y] = c;
                    }
                // Lichen on some stones, chipped corners on others.
                if (Art.Hash(id, 1, 571) > 0.6f)
                    for (int k = 0; k < 3; k++)
                    {
                        int x = (left + 2 + (int)(Art.Hash(id, k, 575) * (w - 4))) % P, y = top + 1 + (int)(Art.Hash(k, id, 575) * (h - 3));
                        img[x, y] = Pal.Lichen[2]; img[(x + 1) % P, y] = Pal.Lichen[1];
                    }
                left = (left + w) % P;
            }
            top += h;
        }
        return _ruinStones = img;
    }

    private static PixelImage FlowerOverlay(int variant, int frame)
    {
        var img = new PixelImage(S, S);
        Color[] petals = variant == 0 ? [Color.Hex("fff6e0"), Color.Hex("d8d0e8"), Color.Hex("a89cc8")] : [Color.Hex("ffb0b8"), Color.Hex("f0607a"), Color.Hex("a82a50")];
        var heart = variant == 0 ? Color.Hex("f2c23a") : Color.Hex("ffe080");
        (int X, int Y)[] spots = variant == 0 ? [(3, 4), (11, 3), (7, 10), (13, 12), (2, 13)] : [(4, 10), (10, 6), (3, 3), (12, 13), (8, 2)];
        int i = 0;
        foreach (var (sx, sy) in spots)
        {
            // Every other flower nods in the breeze on the second frame.
            int x = sx + (frame == 1 && i % 2 == 0 ? 1 : 0);
            ArtKit.Soft(img, sx + 1, sy + 2, Pal.CastShadow, 0.45f);
            img[sx, sy + 2] = Pal.Turf[1];
            img[sx, sy + 1] = Pal.Turf[3];
            img[x, sy - 1] = petals[0];
            img[x - 1, sy] = petals[0];
            img[x + 1, sy] = petals[1];
            img[x, sy + 1] = petals[2];
            img[x, sy] = heart;
            if (i % 2 == 1) img[sx + 1, sy + 1] = Pal.Turf[4];
            i++;
        }
        return img;
    }

    private static PixelImage TallOverlay(int frame)
    {
        var img = new PixelImage(S, S);
        var g = Pal.Turf;
        for (int k = 0; k < 8; k++)
        {
            int x = 1 + k * 2 - (k % 2), baseY = 15 - (k % 3);
            int height = 7 + (int)(Art.Hash(k, 1, 77) * 5);
            int lean = frame == 0 ? (k % 2 == 0 ? 1 : 0) : (k % 2 == 0 ? 0 : -1);
            ArtKit.Soft(img, x + 1, baseY, Pal.CastShadow, 0.5f);
            for (int h = 0; h < height; h++)
            {
                int xx = x + (h > height / 2 ? lean : 0);
                if (xx < 0 || xx > 15) continue;
                img[xx, baseY - h] = h < 2 ? g[1] : h < height - 3 ? g[3] : h < height - 1 ? g[5] : g[6];
                if (h < height - 2 && xx + 1 < S) img[xx + 1, baseY - h] = h < 3 ? g[0] : g[2];
            }
        }
        return img;
    }

    private static PixelImage RockOverlay()
    {
        var img = new PixelImage(S, S);
        ArtKit.CastShadow(img, 9.5f, 12.5f, 7.5f, 3.5f, 0.55f);
        var boulder = new PixelImage(S, S);
        var r = Pal.Boulder;
        Color[] body = [r[1], r[2], r[3], r[4], r[5], r[6]];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = (x + 0.5f - 8f) / 6.8f, dy = (y + 0.5f - 8.4f) / 6f;
                float d = dx * dx + dy * dy;
                if (d > 1) continue;
                float light = 0.66f - dx * 0.3f - dy * 0.36f - d * 0.22f + (Art.Hash(x, y, 61) - 0.5f) * 0.12f;
                // Facets: a flat lit top plane and a darker front face.
                if (y > 9 + (x - 8) / 4) light -= 0.14f;
                boulder[x, y] = Art.Ramp(body, Math.Clamp(light, 0, 1), x, y);
            }
        boulder.Line(8, 10, 12, 9, r[1]); boulder.Line(8, 11, 12, 10, r[4]);
        // Moss cap on the lit shoulder.
        foreach (var (x, y) in new[] { (4, 4), (5, 4), (6, 3), (7, 3), (5, 5), (3, 5), (8, 3) }) boulder[x, y] = x % 2 == 0 ? Pal.Lichen[3] : Pal.Lichen[2];
        boulder[4, 6] = Pal.Lichen[1]; boulder[6, 4] = Pal.Lichen[2];
        ArtKit.ColorOutline(boulder.Outline(Pal.Ink), Pal.Ink);
        ArtKit.Over(img, boulder);
        return img;
    }

    private static PixelImage BushOverlay()
    {
        var img = new PixelImage(S, S);
        ArtKit.CastShadow(img, 9.5f, 12.5f, 7.5f, 3.2f, 0.55f);
        var leaves = new PixelImage(S, S);
        var l = Pal.Foliage;
        Color[] body = [l[1], l[2], l[3], l[4], l[5]];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = (x + 0.5f - 8f) / 7f, dy = (y + 0.5f - 8.2f) / 6.4f;
                float d = dx * dx + dy * dy;
                // Scalloped rim: lobes along the top.
                float lobe = MathF.Abs(MathF.Sin((x + 0.5f) * 0.62f)) * 0.18f;
                if (d > 1 - (dy < 0 ? lobe : 0)) continue;
                float light = 0.62f - dx * 0.3f - dy * 0.38f - d * 0.2f;
                leaves[x, y] = Art.Ramp(body, Math.Clamp(light, 0, 1), x, y);
            }
        // Leaf clusters: each a lit crescent over its own shadow.
        foreach (var (x, y) in new[] { (4, 5), (8, 3), (11, 6), (5, 9), (9, 8), (12, 10), (7, 12) })
        {
            if (leaves[x, y].A == 0) continue;
            leaves[x, y] = l[5];
            if (leaves[x + 1, y].A > 0) leaves[x + 1, y] = l[4];
            if (leaves[x, y + 1].A > 0) leaves[x, y + 1] = l[2];
            if (leaves[x + 1, y + 1].A > 0) leaves[x + 1, y + 1] = l[1];
        }
        // A few berries.
        leaves[6, 6] = Color.Hex("e0505a"); leaves[10, 11] = Color.Hex("e0505a"); leaves[6, 7] = Color.Hex("8a2040");
        ArtKit.ColorOutline(leaves.Outline(Pal.Ink), Pal.Ink);
        ArtKit.Over(img, leaves);
        return img;
    }

    private static PixelImage FenceOverlay(bool left, bool right)
    {
        var img = new PixelImage(S, S);
        var w = Pal.Timber;
        int x0 = left ? 0 : 3, x1 = right ? S : 13;
        // Shadow of the rails on the grass, offset down-right.
        for (int x = x0 + 1; x < Math.Min(S, x1 + 1); x++) { ArtKit.Soft(img, x, 13, Pal.CastShadow, 0.45f); ArtKit.Soft(img, x, 14, Pal.CastShadow, 0.3f); }
        foreach (int ry in new[] { 6, 10 })
            for (int x = x0; x < x1; x++)
            {
                img[x, ry] = w[3];
                img[x, ry + 1] = x % 5 == 2 ? w[1] : w[2];
                img[x, ry + 2] = w[0];
            }
        foreach (int px in new[] { 3, 11 })
        {
            ArtKit.Soft(img, px + 2, 14, Pal.CastShadow, 0.55f); ArtKit.Soft(img, px + 3, 14, Pal.CastShadow, 0.4f);
            for (int y = 3; y < 15; y++) { img[px, y] = w[3]; img[px + 1, y] = w[2]; img[px + 2, y] = w[1]; }
            img[px, 2] = w[4]; img[px + 1, 2] = w[3]; img[px + 2, 2] = w[2];
            img[px, 1] = w[0]; img[px + 1, 1] = w[0]; img[px + 2, 1] = w[0];
            img[px, 15] = w[0]; img[px + 1, 15] = w[0]; img[px + 2, 15] = w[0];
            img[px + 1, 7] = w[1]; img[px + 1, 11] = w[1];
        }
        return img;
    }
}

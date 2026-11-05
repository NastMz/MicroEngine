using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using TrailDuel.World;

namespace TrailDuel.Visuals;

/// <summary>
/// Procedural 16x16 overworld tiles: grass, autotiled paths and water, swaying flowers and tall
/// grass, trees, fences, ledges, rocks and interior furniture. Everything is lit from the top-left.
/// </summary>
internal static partial class TileArt
{
    public const int T = MapDef.TileSize;
    public const int TreeVariants = 3, PineVariants = 4;

    public static void AddAll(SpriteAtlasBuilder b)
    {
        for (int v = 0; v < 4; v++) b.Add($"t:grass{v}", Grass(v));
        for (int f = 0; f < 2; f++) b.Add($"t:flowers{f}", Flowers(f));
        for (int f = 0; f < 2; f++) b.Add($"t:tall{f}", TallGrass(f, overlay: false));
        b.Add("t:tallover", TallGrass(0, overlay: true));
        for (int f = 0; f < 3; f++) b.Add($"t:rustle{f}", Rustle(f));
        for (int m = 0; m < 16; m++) b.Add($"t:path{m}", PathTile(m));
        for (int m = 0; m < 16; m++)
        for (int f = 0; f < 3; f++) b.Add($"t:water{m}_{f}", WaterTile(m, f));
        for (int v = 0; v < TreeVariants; v++) b.Add($"t:tree{v}", Tree(v), new Vector2(0, 10));
        for (int v = 0; v < PineVariants; v++) b.Add($"t:pine{v}", Pine(v), new Vector2(0, 12));
        for (int m = 0; m < 4; m++) b.Add($"t:fence{m}", Fence(m));
        for (int m = 0; m < 4; m++) b.Add($"t:ledge{m}", Ledge(m));
        for (int c = 0; c < 4; c++) b.Add($"t:waterin{c}", WaterInnerCorner(c));
        b.Add("t:rock", Rock());
        b.Add("t:bush", Bush());
        b.Add("t:sign", Sign());
        b.Add("t:shadow", Shadow());
        // Interiors
        for (int v = 0; v < 2; v++) b.Add($"t:floor{v}", Floor(v));
        b.Add("t:stone", StoneFloor());
        b.Add("t:walltop", Wall(top: true, window: false));
        b.Add("t:wall", Wall(top: false, window: false));
        b.Add("t:wallwin", Wall(top: false, window: true));
        b.Add("t:mat", Mat());
        for (int m = 0; m < 16; m++) b.Add($"t:carpet{m}", Carpet(m));
        b.Add("t:bookshelf", Bookshelf(), new Vector2(0, 8));
        b.Add("t:bedtop", BedTop());
        b.Add("t:bedbottom", BedBottom());
        for (int m = 0; m < 4; m++) b.Add($"t:table{m}", Table(m));
        b.Add("t:plant", Plant(), new Vector2(0, 6));
        for (int m = 0; m < 16; m++) b.Add($"t:counter{m}", Counter(m));
        b.Add("t:tv", Tv(), new Vector2(0, 4));
        b.Add("t:kitchen", Kitchen(), new Vector2(0, 6));
        b.Add("t:altar", Altar(), new Vector2(0, 10));
        b.Add("t:pedestal", Pedestal());
        b.Add("t:shelf", ShopShelf(), new Vector2(0, 8));
        b.Add("t:machine", Machine(), new Vector2(0, 8));
        b.Add("t:statue", Statue(), new Vector2(0, 12));
        b.Add("t:rug", Rug());
    }

    private static PixelImage New() => new(T, T);

    // ------------------------------------------------------------------ outdoor ground

    /// <summary>
    /// Short meadow grass: one calm base colour with a few outlined tufts and specks, so wide fields read
    /// as soft ground instead of noise. Variants move the tufts so neighbouring tiles never match.
    /// </summary>
    public static PixelImage Grass(int variant)
    {
        var img = New();
        var g = Pal.Grass;
        img.Clear(g[2]);
        // Tufts: a little fan of blades, dark at the root and bright at the tips.
        (int X, int Y)[][] layouts =
        [
            [(3, 5), (11, 11)],
            [(10, 4), (4, 12)],
            [(6, 8), (13, 14)],
            [(2, 13), (12, 6)],
        ];
        foreach (var (x, y) in layouts[variant % 4])
        {
            img[x - 1, y] = g[0]; img[x, y] = g[0]; img[x + 1, y] = g[0]; img[x + 2, y] = g[0];
            img[x - 1, y - 1] = g[3]; img[x + 1, y - 1] = g[3]; img[x + 2, y - 2] = g[3];
            img[x, y - 1] = g[1];
        }
        for (int i = 0; i < 2; i++)
        {
            int x = (int)(Art.Hash(i, variant, 11) * 14) + 1, y = (int)(Art.Hash(variant, i, 12) * 14) + 1;
            if (img[x, y] == g[2]) img[x, y] = g[3];
        }
        return img;
    }

    private static PixelImage Flowers(int frame)
    {
        var img = Grass(1);
        (int X, int Y, int C)[] spots = [(3, 4, 0), (11, 3, 1), (7, 10, 2), (13, 12, 0), (2, 13, 3)];
        foreach (var (sx, sy, ci) in spots)
        {
            int x = sx + (frame == 1 && ci % 2 == 0 ? 1 : 0);
            var petal = Pal.FlowerColors[ci];
            img[sx, sy + 2] = Pal.Grass[0];
            img[sx, sy + 1] = Pal.Grass[1];
            img[x - 1, sy] = petal; img[x + 1, sy] = petal; img[x, sy - 1] = petal; img[x, sy + 1] = petal.Shade(0.8f);
            img[x, sy] = Pal.Gold;
        }
        return img;
    }

    /// <summary>
    /// Encounter grass: two staggered rows of tall, outlined blade clumps on dark ground, unmistakable
    /// from the short grass around it. The overlay holds only the front clumps (drawn over feet).
    /// </summary>
    private static PixelImage TallGrass(int frame, bool overlay)
    {
        var img = New();
        var c = Pal.TallGrass;
        if (!overlay) img.Clear(c[1]);
        var clumps = new PixelImage(T, T);
        (int X, int Base)[] spots = overlay ? [(0, 15), (8, 15)] : [(4, 8), (12, 8), (0, 15), (8, 15)];
        foreach (var (cx, baseY) in spots)
        {
            int sway = frame == 1 ? ((cx / 4 + baseY) % 2 == 0 ? 1 : -1) : 0;
            // Three pointed blades per clump: left, tall centre, right.
            foreach (var (dx, h) in new[] { (1, 5), (3, 7), (5, 5) })
                for (int k = 0; k < h; k++)
                {
                    int lean = k >= h - 2 ? sway : 0;
                    int splay = k > 2 ? (dx == 1 ? -1 : dx == 5 ? 1 : 0) : 0;
                    int x = (cx + dx + lean + splay + T) % T, y = baseY - k;
                    if (!clumps.InBounds(x, y)) continue;
                    clumps[x, y] = k == h - 1 ? c[3] : k < 2 ? c[1] : c[2];
                    int x2 = (x + 1) % T;
                    if (k < h - 2) clumps[x2, y] = k < 2 ? c[1] : c[2];
                }
        }
        img.Blit(clumps.Outline(c[0]), 0, 0);
        return img;
    }

    private static PixelImage Rustle(int frame)
    {
        var img = New();
        var c = Pal.TallGrass;
        // Blades bend outward from the centre as something pushes through.
        int spread = 2 + frame * 2;
        for (int i = -1; i <= 1; i += 2)
        for (int h = 0; h < 6 - frame; h++)
        {
            int x = 8 + i * (spread + h / 2), y = 15 - h;
            img[x, y] = h < 2 ? c[1] : c[3];
            img[x + i, y] = c[2];
        }
        if (frame < 2) img[8, 9 - frame * 2] = Pal.Grass[3];
        return img;
    }

    /// <summary>Path with soft, pebbly edges; mask bits: 1 up, 2 right, 4 down, 8 left neighbour is path.</summary>
    /// <summary>
    /// Packed dirt with a clean border where it meets the grass;
    /// mask bits: 1 up, 2 right, 4 down, 8 left neighbour is path.
    /// </summary>
    private static PixelImage PathTile(int mask)
    {
        var img = Grass(3);
        var p = Pal.Path;
        var g = Pal.Grass;
        bool up = (mask & 1) != 0, right = (mask & 2) != 0, down = (mask & 4) != 0, left = (mask & 8) != 0;
        for (int y = 0; y < T; y++)
        for (int x = 0; x < T; x++)
        {
            float edge = 99;
            if (!up) edge = MathF.Min(edge, y);
            if (!down) edge = MathF.Min(edge, T - 1 - y);
            if (!left) edge = MathF.Min(edge, x);
            if (!right) edge = MathF.Min(edge, T - 1 - x);
            if (!up && !left) edge = MathF.Min(edge, RoundCorner(x, y, 4, 4));
            if (!up && !right) edge = MathF.Min(edge, RoundCorner(x, y, T - 5, 4));
            if (!down && !left) edge = MathF.Min(edge, RoundCorner(x, y, 4, T - 5));
            if (!down && !right) edge = MathF.Min(edge, RoundCorner(x, y, T - 5, T - 5));
            if (edge < 1) continue;
            // The rim: dark on the far (upper/left) edges, a lit lip on the near ones, as if slightly sunken.
            bool rim = edge < 2;
            bool farSide = (!up && y < 3) || (!left && x < 3);
            img[x, y] = rim ? (farSide ? p[0] : p[1]) : p[2];
        }
        // Sparse pebbles and wheel-worn light patches.
        for (int i = 0; i < 4; i++)
        {
            int x = 3 + (int)(Art.Hash(i, mask, 5) * 10), y = 3 + (int)(Art.Hash(mask, i, 6) * 10);
            if (img[x, y] != p[2]) continue;
            if (i % 2 == 0) { img[x, y] = p[1]; if (img[x + 1, y] == p[2]) img[x + 1, y] = p[3]; }
            else img[x, y] = p[3];
        }
        _ = g;
        return img;
    }

    /// <summary>Distance inside a rounded corner centred at (cx, cy) with radius 4; large when outside the corner square.</summary>
    private static float RoundCorner(int x, int y, int cx, int cy)
    {
        bool inX = cx < 8 ? x < cx : x > cx, inY = cy < 8 ? y < cy : y > cy;
        if (!inX || !inY) return 99;
        float dx = x - cx, dy = y - cy;
        return 4 - MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Pond water: a deep base with drifting ripple bands and a few glints, edged by a dark waterline,
    /// a band of wet sand and the grass rim; mask bits: 1 up, 2 right, 4 down, 8 left neighbour is water.
    /// </summary>
    private static PixelImage WaterTile(int mask, int frame)
    {
        var img = New();
        var w = Pal.Water;
        img.Clear(w[2]);
        for (int y = 0; y < T; y++)
        for (int x = 0; x < T; x++)
        {
            // Ripple bands: a darker trough and a light crest on each wavy line.
            float phase = (x + frame * 5.33f) * 0.39f;
            int line = (int)MathF.Round(y + MathF.Sin(phase) * 1.2f);
            if (line % 6 == 0) img[x, y] = w[1];
            else if (line % 6 == 1 && (x + frame) % 5 < 3) img[x, y] = w[3];
        }
        for (int i = 0; i < 2; i++)
        {
            int sx = (int)(Art.Hash(i, frame, 3) * 12) + 1, sy = 2 + i * 7 + frame % 2;
            img[sx, sy] = w[4]; img[sx + 1, sy] = w[4];
        }
        bool up = (mask & 1) != 0, right = (mask & 2) != 0, down = (mask & 4) != 0, left = (mask & 8) != 0;
        for (int y = 0; y < T; y++)
        for (int x = 0; x < T; x++)
        {
            int d = 99;
            if (!up) d = Math.Min(d, y);
            if (!down) d = Math.Min(d, T - 1 - y);
            if (!left) d = Math.Min(d, x);
            if (!right) d = Math.Min(d, T - 1 - x);
            float corner = 99;
            if (!up && !left) corner = MathF.Min(corner, ShoreCorner(x, y, 7, 7));
            if (!up && !right) corner = MathF.Min(corner, ShoreCorner(x, y, T - 8, 7));
            if (!down && !left) corner = MathF.Min(corner, ShoreCorner(x, y, 7, T - 8));
            if (!down && !right) corner = MathF.Min(corner, ShoreCorner(x, y, T - 8, T - 8));
            if (corner < 99) d = Math.Min(d, (int)MathF.Floor(corner));
            if (d == 0) img[x, y] = Pal.Grass[1];
            else if (d == 1) img[x, y] = Pal.Path[2];
            else if (d == 2) img[x, y] = Pal.Ink;
            else if (d == 3) img[x, y] = (frame + x + y) % 4 == 0 ? w[4] : w[0];
        }
        return img;
    }

    /// <summary>
    /// A broadleaf tree, 16x26 with its base on the tile; the canopy overhangs the tile above. Variants
    /// cluster the crown differently so rows of trees never read as one stamp repeated.
    /// </summary>
    private static PixelImage Tree(int variant)
    {
        var c = new PropCanvas(26);
        var canopy = new Material(Pal.Canopy[1..5], Pal.Ink);
        var bark = new Material(Pal.Bark, Pal.Ink);
        c.Capsule(8, 18, 8, 24.5f, 2.2f, 3.2f, bark);
        c.Ellipse(8, 24.6f, 4.2f, 1.2f, bark, contour: false);
        switch (variant)
        {
            case 0:
                c.Ellipse(8, 12.5f, 7.6f, 7.2f, canopy);
                c.Ellipse(4, 15, 4, 3.6f, canopy, contour: false);
                c.Ellipse(12, 15.5f, 4, 3.4f, canopy, contour: false);
                c.Ellipse(6.5f, 7.5f, 4.6f, 3.8f, canopy, contour: false);
                c.Ellipse(11, 9, 3.6f, 3.2f, canopy, contour: false);
                break;
            case 1:
                c.Ellipse(8, 11.5f, 7, 8.2f, canopy);
                c.Ellipse(4.5f, 14.5f, 3.6f, 4, canopy, contour: false);
                c.Ellipse(11.5f, 13.5f, 3.8f, 4.4f, canopy, contour: false);
                c.Ellipse(7.5f, 5.5f, 4.2f, 3.6f, canopy, contour: false);
                break;
            default:
                c.Ellipse(8, 13, 7.7f, 6.6f, canopy);
                c.Ellipse(3.8f, 13.5f, 3.8f, 4, canopy, contour: false);
                c.Ellipse(12.2f, 12, 3.6f, 4.2f, canopy, contour: false);
                c.Ellipse(5.5f, 8.5f, 3.8f, 3.4f, canopy, contour: false);
                c.Ellipse(10.5f, 7.5f, 3.6f, 3.2f, canopy, contour: false);
                break;
        }
        var src = c.Render();
        var img = new PixelImage(16, 26);
        img.Blit(src, 0, 0, 16, 26, 0, 0);
        // Leaf clusters: small bright crescents in the lit upper-left, dark notches in the lower-right.
        for (int i = 0; i < 12; i++)
        {
            int x = 2 + (int)(Art.Hash(i, variant, 77) * 12), y = 4 + (int)(Art.Hash(variant, i, 78) * 13);
            if (img[x, y].A == 0 || img[x, y] == Pal.Canopy[0] || img[x + 1, y].A == 0) continue;
            bool lit = x + y < 18;
            img[x, y] = lit ? Pal.Canopy[4] : Pal.Canopy[1];
            img[x + 1, y] = lit ? Pal.Canopy[3] : Pal.Canopy[1];
            if (img[x, y + 1].A > 0 && img[x, y + 1] != Pal.Canopy[0]) img[x, y + 1] = lit ? Pal.Canopy[2] : Pal.Canopy[0];
        }
        // Shade where the crown hangs over the trunk.
        for (int x = 5; x < 11; x++)
            if (img[x, 19].A > 0 && img[x, 19] != Pal.Ink) img[x, 19] = Pal.Bark[0];
        return img;
    }

    /// <summary>
    /// The shore of a concave pond corner (land only on the diagonal), drawn over a water tile:
    /// corner 0 top-left, 1 top-right, 2 bottom-left, 3 bottom-right. Matches the straight shore's bands.
    /// </summary>
    private static PixelImage WaterInnerCorner(int corner)
    {
        var img = New();
        float cx = corner % 2 == 0 ? -0.5f : T - 0.5f, cy = corner < 2 ? -0.5f : T - 0.5f;
        for (int y = 0; y < T; y++)
        for (int x = 0; x < T; x++)
        {
            float d = MathF.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            if (d < 1.5f) img[x, y] = Pal.Grass[1];
            else if (d < 2.5f) img[x, y] = Pal.Path[2];
            else if (d < 3.5f) img[x, y] = Pal.Ink;
            else if (d < 4.5f) img[x, y] = Pal.Water[0];
        }
        return img;
    }

    private static float ShoreCorner(int x, int y, int cx, int cy)
    {
        bool inX = cx < 8 ? x < cx : x > cx, inY = cy < 8 ? y < cy : y > cy;
        if (!inX || !inY) return 99;
        float dx = x - cx, dy = y - cy;
        return MathF.Max(0, 7.5f - MathF.Sqrt(dx * dx + dy * dy));
    }

    /// <summary>
    /// A conifer, 16x28 with its base on the tile. Variants differ in height, tiers, breadth and hue
    /// (a darker blue-green fir), so a dense wood reads as many trees rather than one stamp.
    /// </summary>
    private static PixelImage Pine(int variant)
    {
        var img = new PixelImage(16, 28);
        Color[] cn = variant == 3
            ? [Color.Hex("143428"), Color.Hex("1c4c3c"), Color.Hex("286a4c"), Color.Hex("3c8a5c"), Color.Hex("62aa72")]
            : Pal.Canopy;
        img.FillRect(7, 22, 3, 5, Pal.Bark[1]);
        img.FillRect(7, 22, 1, 5, Pal.Bark[2]);
        img.FillRect(6, 26, 5, 1, Pal.Bark[0]);
        (int Tiers, int Top, int Step, int Half) shape = variant switch
        {
            1 => (3, 5, 5, 5),
            2 => (4, 0, 5, 3),
            _ => (3, 2, 6, 4),
        };
        for (int tier = 0; tier < shape.Tiers; tier++)
        {
            int top = shape.Top + tier * shape.Step, height = 10, half = shape.Half + tier;
            for (int y = 0; y < height; y++)
            {
                if (top + y >= 24) break;
                // Saw-toothed branch tips along each tier's flanks.
                int w = (int)(half * (y + 1) / (float)height) + 1 - (y % 3 == 0 ? 1 : 0);
                w = Math.Min(w, 7);
                for (int x = -w; x <= w; x++)
                {
                    float light = 0.8f - (x + w) / (float)(2 * w + 1) * 0.6f - y / (float)height * 0.25f;
                    if (y == height - 1) light -= 0.3f;
                    int idx = Math.Clamp((int)MathF.Round(light * 3), 0, 3);
                    img[8 + x, top + y] = cn[idx + 1];
                }
                // Lit needle tips on the sunny flank.
                if (y % 3 == 1 && w > 1) img[8 - w + 1, top + y] = cn[4];
            }
        }
        img[8, Math.Max(0, shape.Top - 1)] = cn[4];
        return img.Outline(Pal.Ink);
    }

    private static PixelImage Fence(int mask)
    {
        var img = New();
        var w = Pal.Wood;
        bool left = (mask & 1) != 0, right = (mask & 2) != 0;
        int x0 = left ? 0 : 6, x1 = right ? 15 : 9;
        for (int x = x0; x <= x1; x++)
        {
            img[x, 5] = w[3]; img[x, 6] = w[2]; img[x, 7] = w[1];
            img[x, 10] = w[3]; img[x, 11] = w[2]; img[x, 12] = w[1];
        }
        img.FillRect(6, 2, 4, 13, w[2]);
        img.FillRect(6, 2, 1, 13, w[3]);
        img.FillRect(9, 2, 1, 13, w[1]);
        img[7, 1] = w[2]; img[8, 1] = w[2];
        img.FillRect(5, 15, 6, 1, Pal.Grass[0].WithAlpha(0.6f));
        return img.Outline(Pal.Ink);
    }

    /// <summary>A one-way ledge lip; mask bit 1 = ledge continues left, bit 2 = continues right.</summary>
    /// <summary>
    /// A one-way ledge, read as a drop rather than a wall: the upper meadow runs uninterrupted, in its
    /// own grass colour, to a scalloped edge over a short earth face. The only dark edge is
    /// at the bottom, followed by a soft shadow on the lower grass, so all the weight points the way you
    /// can jump. Mask bit 1 = ledge continues left, bit 2 = continues right.
    /// </summary>
    private static PixelImage Ledge(int mask)
    {
        var img = Grass(mask);
        bool left = (mask & 1) != 0, right = (mask & 2) != 0;
        var g = Pal.Grass;
        var p = Pal.Path;
        for (int x = 0; x < T; x++)
        {
            // Ends curl back up into the meadow so a ledge never looks like a cut-off rail.
            int endLift = !left && x < 3 ? 3 - x : !right && x > 12 ? x - 12 : 0;
            // Scallops: each 4px bump hangs one pixel lower in the middle.
            int bump = (x % 4) is 1 or 2 ? 1 : 0;
            int lip = 6 + endLift, face = 8 + bump - endLift / 2, edge = 13 + bump - endLift;
            if (edge <= lip) continue;
            for (int y = lip; y <= edge + 2 && y < T; y++)
            {
                Color c;
                // The upper ground runs straight into the lip in its own colour: no seam on top.
                if (y < face) c = img[x, y];
                // The exposed earth face: lighter near the top, a few roots and pebbles in it.
                else if (y < edge) c = (x % 3 == 0 && y == face) ? g[1] : (x * 7 + y * 3) % 11 == 0 ? p[2] : y - face < 2 ? p[1] : p[0];
                else if (y == edge) c = Pal.Ink;
                else if (y == edge + 1) c = Color.Lerp(g[2], Pal.Ink, 0.38f);
                else c = Color.Lerp(g[2], Pal.Ink, 0.16f);
                img[x, y] = c;
            }
            // Grass blades hanging over the face.
            if (x % 4 == 0 && face < edge) img[x, face] = g[1];
        }
        return img;
    }

    private static PixelImage Rock()
    {
        var c = new PropCanvas(16);
        var m = new Material(Pal.Stone, Pal.Ink);
        c.Ellipse(8, 9.5f, 6.8f, 5.6f, m, bulge: 0.7f);
        c.Ellipse(5, 11, 3, 2.5f, m, contour: true);
        c.Stroke(9, 6, 11, 9, Pal.Stone[0]);
        return c.Render();
    }

    private static PixelImage Bush()
    {
        var c = new PropCanvas(16);
        var m = new Material(Pal.Canopy[1..5], Pal.Ink);
        c.Circle(5, 10, 4.5f, m).Circle(11, 10, 4.5f, m).Circle(8, 7, 5, m, contour: false);
        c.Dot(6, 6, 0.8f, Pal.FlowerColors[0]).Dot(11, 8, 0.8f, Pal.FlowerColors[2]);
        return c.Render();
    }

    private static PixelImage Sign()
    {
        var img = New();
        var w = Pal.Wood;
        img.FillRect(7, 9, 2, 6, w[1]);
        img.FillRect(2, 2, 12, 8, w[2]);
        img.FillRect(2, 2, 12, 1, w[3]);
        img.FillRect(2, 9, 12, 1, w[1]);
        for (int i = 0; i < 3; i++) img.FillRect(4, 4 + i * 2, 8 - i * 2, 1, w[0].WithAlpha(0.5f));
        img.FillRect(4, 15, 8, 1, Pal.Grass[0].WithAlpha(0.6f));
        return img.Outline(Pal.Ink);
    }

    private static PixelImage Shadow()
    {
        var img = New();
        img.FillEllipse(8, 14, 6, 2.2f, Pal.Ink.WithAlpha(0.28f));
        return img;
    }

}

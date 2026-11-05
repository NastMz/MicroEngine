using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using TrailDuel.World;

namespace TrailDuel.Visuals;

/// <summary>
/// The 16x16 overworld tiles. Grass, paths, ponds, flowers, trees, rocks, bushes and signs are Serene Village pieces
/// (<see cref="Village"/>); tall grass, ledges, fences and interiors are drawn in code with the pack's colours and
/// outline so they sit in the same world.
/// </summary>
internal static partial class TileArt
{
    public const int T = MapDef.TileSize;
    /// <summary>Flower clumps of the pack (red, blue, yellow, white, sky) and the two frames each sways through.</summary>
    public const int FlowerKinds = 5, FlowerFrames = 2;
    /// <summary>Frames of the pond's wave loop the tiles use (every other frame of the pack's loop).</summary>
    public const int WaterFrames = Village.WaveFrames / 2;

    public static void AddAll(SpriteAtlasBuilder b, Village v)
    {
        for (int i = 0; i < 4; i++) Add(b, $"t:grass{i}", Grass(v, i));
        for (int k = 0; k < FlowerKinds; k++)
        for (int f = 0; f < FlowerFrames; f++) Add(b, $"t:flowers{k * FlowerFrames + f}", Flowers(v, k, f));
        for (int f = 0; f < 2; f++) Add(b, $"t:tall{f}", TallGrass(f, overlay: false));
        Add(b, "t:tallover", TallGrass(0, overlay: true));
        for (int f = 0; f < 3; f++) Add(b, $"t:rustle{f}", Rustle(f));
        for (int m = 0; m < 16; m++) Add(b, $"t:path{m}", PathTile(v, m));
        var profiles = new ShoreProfiles(v);
        for (int m = 0; m < 16; m++)
        for (int f = 0; f < WaterFrames; f++) Add(b, $"t:water{m}_{f}", WaterTile(v, m, f));
        for (int c = 0; c < 4; c++) Add(b, $"t:waterin{c}", WaterInnerCorner(profiles, c));
        TreeArt.AddAll(b, v);
        for (int m = 0; m < 4; m++) Add(b, $"t:fence{m}", Fence(m));
        for (int m = 0; m < 4; m++) Add(b, $"t:ledge{m}", Ledge(v, m));
        // A grey boulder, the round bush and the plain signpost of the pack, each cut along its outline.
        Add(b, "t:rock", OnTile(v.Piece(18, 242, 13, 12, 6, 6), 1));
        Add(b, "t:bush", OnTile(v.Piece(113, 194, 14, 14, 7, 7), 0));
        Add(b, "t:sign", OnTile(v.Piece(129, 209, 14, 15, 7, 4), 0));
        b.Add("t:shadow", Shadow());
        // Interiors
        for (int i = 0; i < 2; i++) b.Add($"t:floor{i}", Floor(i));
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

    /// <summary>Adds an outdoor sprite in the overworld's grade (<see cref="Village.Grade(PixelImage)"/>).</summary>
    private static void Add(SpriteAtlasBuilder b, string key, PixelImage img) => b.Add(key, Village.Grade(img));

    private static PixelImage New() => new(T, T);

    /// <summary>A piece centred on a tile, its base on the tile's bottom row less <paramref name="lift"/> pixels.</summary>
    private static PixelImage OnTile(PixelImage piece, int lift)
    {
        var img = New();
        img.Blit(piece, (T - piece.Width) / 2, T - lift - piece.Height);
        return img;
    }

    /// <summary>Copies a rectangle of the pack sheet onto <paramref name="img"/> without blending.</summary>
    private static void Copy(Village v, PixelImage img, int sx, int sy, int w, int h, int dx, int dy)
    {
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            img[dx + x, dy + y] = v.Sheet[sx + x, sy + y];
    }

    // ------------------------------------------------------------------ outdoor ground

    /// <summary>
    /// The pack's meadow: two greens in a fine dither. Variants add a small tuft in the pack's rim green so a field never
    /// shows one stamp repeated.
    /// </summary>
    public static PixelImage Grass(Village v, int variant)
    {
        var img = v.Cell(4, 0);
        // Small blade tufts (rim green with a deep root) and a few sunlit blade tips, so short grass has a little of the
        // texture of the tall grass beside it. Positions are fixed per variant.
        (int X, int Y)[][] tufts = [[(11, 9)], [(3, 6), (12, 13)], [(10, 11), (2, 3)], [(12, 4), (4, 12)]];
        foreach (var (x, y) in tufts[variant % 4])
        {
            img[x, y] = VillagePal.GrassRim; img[x + 2, y] = VillagePal.GrassRim;
            img[x + 1, y - 1] = VillagePal.GrassRim; img[x + 1, y] = VillagePal.GrassDeep;
            img[x + 1, y - 2] = SunBlade;
        }
        for (int i = 0; i < 3; i++)
        {
            int x = 1 + (int)(Art.Hash(i, variant, 141) * 14), y = 1 + (int)(Art.Hash(variant, i, 142) * 14);
            if (img[x, y] == VillagePal.GrassA || img[x, y] == VillagePal.GrassB) img[x, y] = i == 0 ? VillagePal.GrassRim : SunBlade;
        }
        return img;
    }

    /// <summary>The lit tip of a blade, between the pack's grass and its bright leaf.</summary>
    private static readonly Color SunBlade = Color.Hex("8fd474");

    /// <summary>One of the pack's flower clumps on grass; the second frame leans the blooms a pixel in the breeze.</summary>
    private static PixelImage Flowers(Village v, int kind, int frame)
    {
        var img = Grass(v, 0);
        var clump = v.Piece(33 + kind * 16, 194, 14, 13, 7, 6);
        if (frame == 1)
        {
            // The blooms (upper rows) lean right; stems and leaves at the base stay planted.
            var lean = new PixelImage(clump.Width + 1, clump.Height);
            lean.Blit(clump, 0, 7, clump.Width, clump.Height - 7, 0, 7);
            lean.Blit(clump, 0, 0, clump.Width, 7, 1, 0);
            clump = lean;
        }
        img.Blit(clump, 1, 2);
        return img;
    }

    /// <summary>
    /// Encounter grass, drawn in code (the pack has none): two staggered rows of tall blade clumps on darker ground, in
    /// the pack's greens with its slate outline. The overlay holds only the front clumps (drawn over feet).
    /// </summary>
    private static PixelImage TallGrass(int frame, bool overlay)
    {
        var img = New();
        Color[] c = [VillagePal.Leaf[0], VillagePal.Leaf[2], H("62b04e"), VillagePal.Leaf[3]];
        if (!overlay)
            for (int y = 0; y < T; y++)
            for (int x = 0; x < T; x++)
                img[x, y] = (x + y) % 2 == 0 ? H("5ea84f") : H("5aa24c");
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

    private static Color H(string hex) => Color.Hex(hex);

    private static PixelImage Rustle(int frame)
    {
        var img = New();
        Color[] c = [VillagePal.Outline, VillagePal.Leaf[2], H("62b04e"), VillagePal.Leaf[3]];
        // Blades bend outward from the centre as something pushes through.
        int spread = 2 + frame * 2;
        for (int i = -1; i <= 1; i += 2)
        for (int h = 0; h < 6 - frame; h++)
        {
            int x = 8 + i * (spread + h / 2), y = 15 - h;
            img[x, y] = h < 2 ? c[1] : c[3];
            img[x + i, y] = c[2];
        }
        if (frame < 2) img[8, 9 - frame * 2] = VillagePal.Leaf[4];
        return img;
    }

    /// <summary>
    /// A dirt path in the pack's own edges: on its dirt, the grass of each side that is not path rises over the rim
    /// (bumps along a bottom edge, the earth face under a top edge, a straight rim on the sides). Strips are taken from
    /// the pack's path ring and shifted by even steps so its dither stays in phase. Mask bits: 1 up, 2 right, 4 down,
    /// 8 left neighbour is path.
    /// </summary>
    private static PixelImage PathTile(Village v, int mask)
    {
        bool up = (mask & 1) != 0, right = (mask & 2) != 0, down = (mask & 4) != 0, left = (mask & 8) != 0;
        var img = v.Cell(10, 2);
        // Wear on the dirt: two pebbles (the pack's pebble colour over a dark grain), a darker grain and a sun-worn spot,
        // placed per tile shape and only on open dirt, so the edges stay the pack's.
        var worn = Color.Hex("dcae62");
        for (int i = 0; i < 4; i++)
        {
            int x = 4 + (int)(Art.Hash(i, mask, 151) * 8), y = 4 + (int)(Art.Hash(mask, i, 152) * 7);
            switch (i)
            {
                case 0 or 1:
                    img[x, y] = VillagePal.Pebble; img[x + 1, y] = VillagePal.Pebble; img[x, y + 1] = VillagePal.DirtRim;
                    break;
                case 2:
                    img[x, y] = VillagePal.DirtRim;
                    break;
                default:
                    img[x, y] = worn; img[x + 1, y] = worn; img[x + 1, y + 1] = worn;
                    break;
            }
        }
        if (!left) Copy(v, img, 144, 32, 7, 16, 0, 0);
        if (!right) Copy(v, img, 103, 32, 7, 16, 9, 0);
        if (!down) Copy(v, img, 112, 23, 16, 3, 0, 13);
        if (!up) Copy(v, img, 112, 54, 16, 5, 0, 0);
        // Where two grass sides meet, the corner is grass rounded off along the rim, as on the pack's path ends.
        var grass = v.Cell(4, 0);
        void Corner(bool fx, bool fy, int depthY)
        {
            for (int y = 0; y < depthY; y++)
            for (int x = 0; x < 6; x++)
            {
                float dx = 6 - x, dy = depthY - y;
                if (dx * dx / 36f + dy * dy / (float)(depthY * depthY) < 1) continue;
                int px = fx ? T - 1 - x : x, py = fy ? T - 1 - y : y;
                img[px, py] = grass[px, py];
            }
        }
        if (!up && !left) Corner(false, false, 5);
        if (!up && !right) Corner(true, false, 5);
        if (!down && !left) Corner(false, true, 4);
        if (!down && !right) Corner(true, true, 4);
        return img;
    }

    /// <summary>
    /// Pond water: the pack's wave loop under the shore of its pond, assembled per quarter from the pond's corners and
    /// edges (a quarter whose two sides are water stays open water). Mask bits: 1 up, 2 right, 4 down, 8 left neighbour
    /// is water.
    /// </summary>
    private static PixelImage WaterTile(Village v, int mask, int frame)
    {
        // The loop's dark wave bands are softened halfway to the open water: a calm pond, still moving.
        var open = Color.Hex("50a7e8");
        var img = v.Waves.Crop(frame * 2 * T, 0, T, T).Map((_, _, c) => c == open ? c : Color.Lerp(c, open, 0.45f));
        bool up = (mask & 1) != 0, right = (mask & 2) != 0, down = (mask & 4) != 0, left = (mask & 8) != 0;
        for (int q = 0; q < 4; q++)
        {
            int qx = q % 2, qy = q / 2;
            bool side = qx == 0 ? left : right, end = qy == 0 ? up : down;
            if (side && end) continue;
            int col = side ? 12 : qx == 0 ? 11 : 13, row = end ? 1 : qy == 0 ? 0 : 2;
            for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
            {
                int px = qx * 8 + x, py = qy * 8 + y;
                var c = v.Sheet[col * T + px, row * T + py];
                if (c.A > 0 && c != open) img[px, py] = c;
            }
        }
        return img;
    }

    /// <summary>The colours from land to open water along the pond's top, side and bottom shores.</summary>
    private sealed class ShoreProfiles
    {
        public readonly Color[] Top, Side, Bottom;

        public ShoreProfiles(Village v)
        {
            var open = Color.Hex("50a7e8");
            Color[] Run(Func<int, Color> at)
            {
                var list = new List<Color>();
                for (int i = 0; i < T; i++)
                {
                    var c = at(i);
                    if (c == open) break;
                    list.Add(c);
                }
                return [.. list];
            }
            Top = Run(i => v.Sheet[12 * T + 8, i]);
            Side = Run(i => v.Sheet[11 * T + i, T + 8]);
            Bottom = Run(i => v.Sheet[12 * T + 8, 3 * T - 1 - i]);
        }
    }

    /// <summary>
    /// The shore of a concave pond corner (land only on the diagonal), drawn over a water tile: corner 0 top-left,
    /// 1 top-right, 2 bottom-left, 3 bottom-right. The pond's thick top bank and its thin side shore meet in a quarter
    /// ellipse, banded with the pond's own colours.
    /// </summary>
    private static PixelImage WaterInnerCorner(ShoreProfiles p, int corner)
    {
        var img = New();
        var vertical = corner < 2 ? p.Top : p.Bottom;
        for (int y = 0; y < T; y++)
        for (int x = 0; x < T; x++)
        {
            float lx = (corner % 2 == 0 ? x : T - 1 - x) + 0.5f, ly = (corner < 2 ? y : T - 1 - y) + 0.5f;
            float ex = lx / p.Side.Length, ey = ly / vertical.Length;
            float e = MathF.Sqrt(ex * ex + ey * ey);
            if (e >= 1) continue;
            img[x, y] = ex > ey ? p.Side[(int)(e * p.Side.Length)] : vertical[(int)(e * vertical.Length)];
        }
        return img;
    }

    private static PixelImage Fence(int mask)
    {
        var img = New();
        Color[] w = [H("6b4c2c"), H("a86b3c"), H("c78c59"), H("e0ad70")];
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
        return img.Outline(VillagePal.Outline);
    }

    /// <summary>
    /// A one-way ledge in the pack's vocabulary for raised grass: the upper meadow runs to the pack's earth face (the
    /// strip under a grass edge on its paths), which drops onto the lower meadow with a soft shadow. Mask bit 1 = ledge
    /// continues left, bit 2 = continues right; the ends curl back up into the meadow.
    /// </summary>
    private static PixelImage Ledge(Village v, int mask)
    {
        var img = Grass(v, mask);
        bool left = (mask & 1) != 0, right = (mask & 2) != 0;
        // The pack's grass-over-dirt face, rows 4..10 of its path ring's bottom edge: grass, rim, earth face, soft rim.
        // Rows of the pack's grass-over-dirt edge (its path ring's bottom edge): grass, rim, the earth face (deepened by
        // repeating its middle row so the drop reads at a glance), then the soft rim on the ground below.
        int[] faceRows = [52, 53, 54, 55, 56, 56, 57, 58];
        var grass = v.Cell(4, 0);
        for (int x = 0; x < T; x++)
        {
            int lift = !left && x < 3 ? 3 - x : !right && x > 12 ? x - 12 : 0;
            for (int y = 0; y < faceRows.Length; y++)
            {
                int py = 5 + y - lift;
                if (py < 0 || py >= T) continue;
                var c = v.Sheet[112 + x, faceRows[y]];
                // The dirt below the face is the lower meadow here: grass in the shadow of the drop.
                bool dirt = c == VillagePal.DirtA || c == VillagePal.DirtB || c == VillagePal.Pebble || c == VillagePal.DirtRim;
                img[x, py] = dirt ? Color.Lerp(grass[x, py], VillagePal.Outline, 0.24f) : c;
            }
            for (int py = 5 + faceRows.Length - lift; py < Math.Min(T, 5 + faceRows.Length - lift + 2); py++)
                img[x, py] = Color.Lerp(grass[x, py], VillagePal.Outline, 0.12f);
        }
        return img;
    }

    private static PixelImage Shadow()
    {
        var img = New();
        img.FillEllipse(8, 14, 6, 2.2f, Pal.Ink.WithAlpha(0.28f));
        return img;
    }
}

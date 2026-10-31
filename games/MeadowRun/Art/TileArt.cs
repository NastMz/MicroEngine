using MeadowRun.Content;
using MicroEngine;
using MicroEngine.Graphics;
using Gfx = MicroEngine.Graphics.Art;

namespace MeadowRun.Art;

/// <summary>How a biome's ground looks: body material, cap material and cap shape.</summary>
internal sealed record GroundStyle(Color[] Body, Color[] Cap, GroundCap CapKind, Color[] Brick, Color[] Plank, Color[] Spike);

internal enum GroundCap { Grass, Moss, Fluff, Slab }

/// <summary>Builds every 16x16 tile image for each biome.</summary>
internal static class TileArt
{
    public const int Size = 16;

    public static GroundStyle Style(Biome biome) => biome switch
    {
        Biome.Meadow => new(Pal.SoilRamp, Pal.GrassRamp, GroundCap.Grass, [Pal.Bark, Pal.Clay, Pal.Sand, Pal.Wheat], [Pal.Bark, Pal.Soil, Pal.Clay, Pal.Sand], Pal.StoneRamp),
        Biome.Caves => new([Pal.Ink, Pal.Night, Pal.Slate, Pal.Dusk], [Pal.Pine, Pal.Teal, Pal.Aqua, Pal.Cream], GroundCap.Moss, [Pal.Ink, Pal.Plum, Pal.Violet, Pal.Lilac], [Pal.Plum, Pal.Violet, Pal.Lilac, Pal.Mist], [Pal.Plum, Pal.Violet, Pal.Lilac, Pal.Aqua]),
        Biome.Sky => new(Pal.CloudRamp, [Pal.Air, Pal.Cloud, Pal.Cream, Pal.Cream], GroundCap.Fluff, [Pal.Rust, Pal.Amber, Pal.Gold, Pal.Butter], [Pal.Haze, Pal.Air, Pal.Cloud, Pal.Cream], Pal.StoneRamp),
        _ => new([Pal.Ink, Pal.Night, Pal.Slate, Pal.Dusk], [Pal.Bark, Pal.Wine, Pal.Rust, Pal.Flame], GroundCap.Slab, [Pal.Bark, Pal.Wine, Pal.Berry, Pal.Coral], [Pal.Night, Pal.Slate, Pal.Dusk, Pal.Haze], Pal.StoneRamp),
    };

    /// <summary>Ground tile for a 4-neighbour mask (1 up, 2 right, 4 down, 8 left) and a variant.</summary>
    public static PixelImage Ground(Biome biome, int mask, int variant)
    {
        var s = Style(biome);
        var img = new PixelImage(Size, Size);
        bool up = (mask & 1) != 0, right = (mask & 2) != 0, down = (mask & 4) != 0, left = (mask & 8) != 0;
        int seed = (int)biome * 31 + variant * 7;

        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float n = Gfx.ValueNoise(x + variant * 16, y, 5, seed) * 0.5f + Gfx.Hash(x, y, seed) * 0.18f;
            float v = 0.42f + n * 0.5f - (up ? 0 : 0.0f);
            if (!left && x < 2) v -= 0.1f;
            if (!right && x > 12) v -= 0.22f;
            if (!down && y > 12) v -= 0.25f;
            img[x, y] = Gfx.Ramp(s.Body, v, x, y);
        }
        if (s.CapKind == GroundCap.Slab) AddMasonry(img, s, variant);
        else AddPebbles(img, s, variant, seed);
        if (!up) DrawCap(img, s, left, right, seed);
        if (s.CapKind == GroundCap.Fluff) return Fluffy(img, up, right, down, left);
        // Dark silhouette edges against open space keep the playfield readable on any backdrop.
        for (int i = 0; i < Size; i++)
        {
            if (!left) img[0, i] = img[0, i].A == 0 ? img[0, i] : Pal.Ink;
            if (!right) img[Size - 1, i] = img[Size - 1, i].A == 0 ? img[Size - 1, i] : Pal.Ink;
            if (!down) img[i, Size - 1] = img[i, Size - 1].A == 0 ? img[i, Size - 1] : Pal.Ink;
        }
        if (!up && !left) { img[0, 0] = Color.Transparent; img[1, 0] = Color.Transparent; img[0, 1] = Color.Transparent; img[1, 1] = Pal.Ink; }
        if (!up && !right) { img[15, 0] = Color.Transparent; img[14, 0] = Color.Transparent; img[15, 1] = Color.Transparent; img[14, 1] = Pal.Ink; }
        if (!down && !left) { img[0, 15] = Color.Transparent; img[1, 15] = Pal.Ink; img[0, 14] = Pal.Ink; }
        if (!down && !right) { img[15, 15] = Color.Transparent; img[14, 15] = Pal.Ink; img[15, 14] = Pal.Ink; }
        return img;
    }

    /// <summary>Cut-stone blocks for castle floors: mortar lines with a lit top edge on each stone.</summary>
    private static void AddMasonry(PixelImage img, GroundStyle s, int variant)
    {
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            int row = y / 8, off = row % 2 == 0 ? 0 : 8;
            bool mortarH = y % 8 == 7, mortarV = (x + off) % 16 == 15;
            if (mortarH || mortarV) img[x, y] = s.Body[0];
            else if (y % 8 == 0) img[x, y] = s.Body[3];
        }
        if (variant == 2)
        {
            img.Line(4, 9, 7, 12, Pal.Rust);
            img.Line(7, 12, 10, 11, Pal.Flame);
            img[7, 12] = Pal.Gold;
        }
    }

    /// <summary>Clouds as ground: rounded puffy silhouette on open sides, soft shading, a light outline.</summary>
    private static PixelImage Fluffy(PixelImage img, bool up, bool right, bool down, bool left)
    {
        var shape = new PixelImage(Size, Size);
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            bool inside = true;
            // Scalloped edges: bumps of radius 4 along every open side.
            if (!up && y < 4) inside &= Bump(x, y);
            if (!down && y > 11) inside &= Bump(x, 15 - y);
            if (!left && x < 4) inside &= Bump(y, x);
            if (!right && x > 11) inside &= Bump(y, 15 - x);
            if (!inside) continue;
            float v = 0.9f - (up ? 0.15f : y / 40f) - (down ? 0 : MathF.Max(0, y - 9) / 14f) - (right ? 0 : MathF.Max(0, x - 11) / 20f);
            shape[x, y] = Gfx.Ramp(Pal.CloudRamp, v, x, y);
        }
        Brush.SelOutline(shape, Pal.Dusk);
        return shape;

        static bool Bump(int along, int depth)
        {
            float c = along % 8 - 3.5f;
            return depth >= 4 - MathF.Sqrt(MathF.Max(0, 16 - c * c));
        }
    }

    private static void AddPebbles(PixelImage img, GroundStyle s, int variant, int seed)
    {
        int count = 1 + variant;
        for (int i = 0; i < count; i++)
        {
            int px = 2 + (int)(Gfx.Hash(i, variant, seed + 3) * 11), py = 6 + (int)(Gfx.Hash(variant, i, seed + 5) * 7);
            img[px, py] = s.Body[0];
            img[px + 1, py] = s.Body[0];
            img[px, py - 1] = s.Body[3];
        }
        if (variant == 2 && s.CapKind == GroundCap.Slab)
        {
            // Glowing ember crack in castle basalt.
            img.Line(4, 8, 7, 11, Pal.Rust);
            img.Line(7, 11, 10, 10, Pal.Flame);
            img[7, 11] = Pal.Gold;
        }
        if (variant == 2 && s.CapKind == GroundCap.Moss)
        {
            img[9, 9] = Pal.Aqua; img[10, 10] = Pal.Teal; img[8, 10] = Pal.Teal;
        }
    }

    private static void DrawCap(PixelImage img, GroundStyle s, bool left, bool right, int seed)
    {
        switch (s.CapKind)
        {
            case GroundCap.Grass:
            case GroundCap.Moss:
                for (int x = 0; x < Size; x++)
                {
                    int depth = 4 + (int)(Gfx.Hash(x, 1, seed) * 3) + (x % 5 == 2 ? 1 : 0);
                    for (int y = 0; y < depth; y++)
                    {
                        float v = y == 0 ? 0.95f : y == 1 ? 0.75f : 0.55f - (y - 2) * 0.12f;
                        img[x, y] = Gfx.Ramp(s.Cap, v, x, y);
                    }
                    img[x, depth] = Color.Lerp(img[x, depth], s.Body[0], 0.6f);
                }
                if (!left) for (int y = 0; y < 6; y++) img[1, y] = Gfx.Ramp(s.Cap, 0.4f, 1, y);
                if (!right) for (int y = 0; y < 6; y++) img[14, y] = Gfx.Ramp(s.Cap, 0.25f, 14, y);
                break;
            case GroundCap.Fluff:
                for (int x = 0; x < Size; x++)
                for (int y = 0; y < 5; y++)
                    img[x, y] = Gfx.Ramp(s.Cap, 0.9f - y * 0.12f, x, y);
                for (int x = 0; x < Size; x++) img[x, 0] = Pal.Cream;
                break;
            case GroundCap.Slab:
                for (int x = 0; x < Size; x++)
                {
                    img[x, 0] = Pal.Haze;
                    img[x, 1] = Pal.Dusk;
                    img[x, 2] = Pal.Slate;
                    img[x, 3] = Gfx.Dither(x, 3, 0.5f) ? Pal.Night : Pal.Slate;
                }
                for (int x = 3; x < Size; x += 7) { img[x, 1] = Pal.Night; img[x, 2] = Pal.Night; }
                break;
        }
    }

    public static PixelImage Brick(Biome biome)
    {
        var ramp = Style(biome).Brick;
        var img = new PixelImage(Size, Size).Clear(ramp[0]);
        for (int row = 0; row < 2; row++)
        {
            int off = row == 0 ? 0 : 4;
            for (int b = -1; b < 2; b++)
            {
                int x0 = b * 8 + off, y0 = row * 8;
                for (int y = 1; y < 7; y++)
                for (int x = 1; x < 8; x++)
                {
                    int px = x0 + x;
                    if (px < 0 || px >= Size) continue;
                    float v = 0.72f - (y - 1) * 0.06f + (x == 1 ? 0.15f : 0) - (x == 7 ? 0.15f : 0);
                    img[px, y0 + y] = Gfx.Ramp(ramp, v, px, y0 + y);
                }
                if (x0 + 1 >= 0 && x0 + 1 < Size) img[x0 + 1, y0 + 1] = ramp[3];
            }
        }
        img.DrawRect(0, 0, Size, Size, Pal.Ink);
        return img;
    }

    private static readonly string[] QuestionGlyph =
    [
        ".####.",
        "##..##",
        "....##",
        "...##.",
        "..##..",
        "......",
        "..##..",
    ];

    /// <summary>The "?" block. Frame 0..3 moves a sheen across; frame 4 is the spent block.</summary>
    public static PixelImage Question(int frame)
    {
        bool used = frame >= 4;
        var ramp = used ? new[] { Pal.Bark, Pal.Soil, Pal.Clay, Pal.Sand } : Pal.GoldRamp;
        var img = new PixelImage(Size, Size);
        Brush.Box(img, 1, 1, 14, 14, ramp, 0.85f, 0.35f);
        img.DrawRect(0, 0, Size, Size, Pal.Ink);
        img.Line(1, 1, 14, 1, ramp[3]);
        img.Line(1, 1, 1, 14, ramp[3]);
        img.Line(2, 14, 14, 14, ramp[0]);
        img.Line(14, 2, 14, 14, ramp[0]);
        foreach (var (rx, ry) in new[] { (3, 3), (12, 3), (3, 12), (12, 12) }) { img[rx, ry] = ramp[0]; img[rx - 1, ry - 1] = ramp[3]; }
        if (used) return img;
        var glyph = new Dictionary<char, Color> { ['#'] = Pal.Rust };
        img.Paint(6, 5, glyph, QuestionGlyph);
        img.Paint(5, 4, new Dictionary<char, Color> { ['#'] = Pal.Cream }, QuestionGlyph);
        int sheen = frame * 6 - 4;
        for (int y = 1; y < 15; y++)
        for (int k = 0; k < 2; k++)
        {
            int x = sheen + k + (15 - y) / 2;
            if (x >= 1 && x <= 14) img[x, y] = Color.Lerp(img[x, y], Pal.Cream, 0.55f);
        }
        return img;
    }

    public static PixelImage Hard(Biome biome)
    {
        var ramp = biome == Biome.Sky ? new[] { Pal.Haze, Pal.Mist, Pal.Cream, Pal.Cream } : Pal.StoneRamp;
        var img = new PixelImage(Size, Size);
        Brush.Box(img, 0, 0, Size, Size, ramp, 0.8f, 0.4f);
        for (int i = 0; i < Size; i++)
        {
            img[i, 0] = ramp[3]; img[0, i] = ramp[3];
            img[i, 15] = ramp[0]; img[15, i] = ramp[0];
        }
        img.FillRect(4, 4, 8, 8, ramp[1]);
        img.Line(4, 4, 11, 4, ramp[0]);
        img.Line(4, 4, 4, 11, ramp[0]);
        img.Line(5, 11, 11, 11, ramp[2]);
        img.Line(11, 5, 11, 11, ramp[2]);
        img.DrawRect(0, 0, Size, Size, Pal.Ink);
        return img;
    }

    public static PixelImage Plank(Biome biome)
    {
        var ramp = Style(biome).Plank;
        var img = new PixelImage(Size, Size);
        if (biome == Biome.Sky)
        {
            for (int x = 0; x < Size; x++)
            {
                int h = 5 + (int)(MathF.Sin(x * 0.8f) * 1.2f);
                for (int y = 0; y < h; y++) img[x, y] = Gfx.Ramp(Pal.CloudRamp, 0.95f - y * 0.14f, x, y);
            }
            Brush.SelOutline(img, Pal.Dusk);
            return img;
        }
        Brush.Box(img, 0, 0, Size, 5, ramp, 0.9f, 0.4f);
        img.Line(0, 0, 15, 0, ramp[3]);
        img.Line(0, 5, 15, 5, Pal.Ink);
        if (biome == Biome.Meadow)
        {
            img[4, 2] = ramp[0]; img[11, 3] = ramp[0];
            img.FillRect(2, 6, 2, 3, ramp[0]);
            img.FillRect(12, 6, 2, 3, ramp[0]);
        }
        else if (biome == Biome.Caves)
        {
            for (int x = 1; x < 15; x += 3) { img[x, 6] = Pal.Lilac; img[x, 7] = Pal.Violet; if (x % 2 == 1) img[x, 8] = Pal.Plum; }
        }
        else
        {
            for (int x = 1; x < 15; x += 4) { img[x, 2] = Pal.Night; img[x + 1, 2] = Pal.Haze; }
            img.Line(3, 6, 3, 9, Pal.Slate); img.Line(12, 6, 12, 9, Pal.Slate);
        }
        return img;
    }

    public static PixelImage Spikes(Biome biome)
    {
        var ramp = Style(biome).Spike;
        var img = new PixelImage(Size, Size);
        for (int i = 0; i < 4; i++)
        {
            int bx = i * 4;
            for (int y = 0; y < 8; y++)
            {
                int half = (y + 1) / 4 + 0;
                for (int x = -half; x <= half + 1; x++)
                {
                    int px = bx + 1 + x;
                    float v = x <= 0 ? 0.85f : 0.35f;
                    img[px, 8 + y] = Gfx.Ramp(ramp, v - y * 0.04f, px, 8 + y);
                }
            }
            img[bx + 1, 8] = Pal.Cream;
        }
        img.FillRect(0, 14, Size, 2, ramp[0]);
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    /// <summary>Lava surface (frames 0..3) and body (frame 4..5).</summary>
    public static PixelImage Lava(int frame, bool surface)
    {
        var img = new PixelImage(Size, Size);
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float wave = surface ? 3 + MathF.Sin((x + frame * 4) * MathF.Tau / 16f) * 1.5f : -1;
            if (y < wave) continue;
            float glow = Gfx.ValueNoise(x + frame * 2, y + frame * 3, 4, 7) ;
            float v = surface && y < wave + 2 ? 0.98f : 0.25f + glow * 0.55f;
            img[x, y] = Gfx.Ramp(Pal.LavaRamp, v, x, y);
        }
        if (surface)
        {
            int bx = (frame * 5 + 3) % 14 + 1;
            img[bx, 6] = Pal.Gold; img[bx + 1, 6] = Pal.Butter; img[bx, 5] = Pal.Gold;
        }
        return img;
    }

    /// <summary>
    /// Non-solid castle wall drawn behind the playfield. Variants keep long walls from tiling visibly:
    /// 0 plain, 1 a paler stone, 2 a cracked stone, 3 moss creeping along the mortar.
    /// </summary>
    public static PixelImage BackWall(int variant = 0)
    {
        var img = new PixelImage(Size, Size).Clear(Pal.Ink);
        for (int row = 0; row < 4; row++)
        {
            int off = row % 2 == 0 ? 0 : 4;
            for (int b = -1; b < 2; b++)
            {
                // One stone per variant tile is picked out a shade lighter.
                bool pale = variant == 1 && row == 1 && b == 0;
                for (int y = 0; y < 3; y++)
                for (int x = 0; x < 7; x++)
                {
                    int px = b * 8 + off + x, py = row * 4 + y;
                    if (px is < 0 or >= Size) continue;
                    float level = 0.3f + (y == 0 ? 0.3f : 0) + (pale ? 0.3f : 0);
                    img[px, py] = Gfx.Dither(px, py, level) ? (pale && y == 0 ? Pal.Dusk : Pal.Slate) : Pal.Night;
                }
            }
        }
        if (variant == 2)
        {
            img.Line(9, 4, 11, 6, Pal.Ink);
            img.Line(11, 6, 10, 9, Pal.Ink);
            img[12, 5] = Pal.Ink;
        }
        if (variant == 3)
            foreach (var (x, y) in new[] { (2, 3), (3, 3), (4, 3), (3, 4), (9, 11), (10, 11), (10, 12), (11, 11), (6, 15), (7, 15) })
                img[x, y] = Gfx.Hash(x, y, 3) < 0.5f ? Pal.Pine : Pal.Fern;
        return img;
    }

    /// <summary>
    /// Carved moulding drawn over a back-wall tile on the side that faces an opening (1 up, 2 right,
    /// 4 down, 8 left), so windows read as framed arches with a sill instead of holes in the wall.
    /// </summary>
    public static PixelImage WallFrame(int side)
    {
        var img = new PixelImage(Size, Size);
        switch (side)
        {
            case 4: // opening below: the arch's keystone edge
                img.FillRect(0, 13, 16, 3, Pal.Slate);
                img.FillRect(0, 15, 16, 1, Pal.Dusk);
                img.FillRect(7, 12, 2, 4, Pal.Dusk);
                break;
            case 1: // opening above: the sill
                img.FillRect(0, 0, 16, 3, Pal.Dusk);
                img.FillRect(0, 0, 16, 1, Pal.Haze);
                img.FillRect(0, 3, 16, 1, Pal.Ink);
                break;
            case 2: // opening to the right: a jamb
                img.FillRect(13, 0, 3, 16, Pal.Slate);
                img.FillRect(15, 0, 1, 16, Pal.Dusk);
                break;
            case 8: // opening to the left
                img.FillRect(0, 0, 3, 16, Pal.Slate);
                img.FillRect(0, 0, 1, 16, Pal.Haze);
                break;
        }
        return img;
    }
}

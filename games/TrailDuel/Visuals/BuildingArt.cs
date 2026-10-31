using MicroEngine;
using MicroEngine.Graphics;

namespace TrailDuel.Visuals;

public enum BuildingStyle { House, HouseBlue, Lab, Shrine, Shop, Hall }

/// <summary>
/// Procedural buildings sized to their tile footprint. The roof overhangs inset walls and casts an eave
/// shadow; a stone foundation, a doorstep and grass tufts reaching over the base anchor the building to
/// the ground (its cast shadow lives in <see cref="GroundArt"/>). Light comes from the top-left.
/// </summary>
internal static class BuildingArt
{
    /// <summary>Rows of roof drawn above the footprint.</summary>
    public const int RoofOverhang = 6;
    /// <summary>Rows drawn below the footprint: the doorstep and grass tufts in front of the foundation.</summary>
    public const int Skirt = 3;

    public static PixelImage Build(BuildingStyle style, int w, int h, int doorX)
    {
        int pw = w * 16, body = h * 16 + RoofOverhang, ph = body + Skirt;
        var img = new PixelImage(pw, ph);
        var roof = style switch
        {
            BuildingStyle.House => Pal.RoofRed,
            BuildingStyle.HouseBlue => Pal.RoofBlue,
            BuildingStyle.Lab => Pal.RoofTeal,
            BuildingStyle.Shrine => Pal.RoofPink,
            BuildingStyle.Shop => Pal.RoofBlue,
            _ => Pal.RoofGold,
        };
        int roofBottom = (int)(body * (style == BuildingStyle.Hall ? 0.5f : 0.56f));
        int dx = doorX * 16 + 3, dy = body - 16;

        Walls(img, style, pw, roofBottom, body);
        Foundation(img, pw, body, style == BuildingStyle.Hall);
        for (int tx = 0; tx < w; tx++)
        {
            if (tx == doorX || style == BuildingStyle.Hall && tx % 2 == 0) continue;
            Window(img, style, tx * 16 + 4, roofBottom + 6);
        }
        Door(img, style, dx, dy, body);
        Roof(img, roof, pw, roofBottom);
        if (style is BuildingStyle.House or BuildingStyle.HouseBlue) Chimney(img, (int)(pw * 0.7f), roofBottom, style == BuildingStyle.House);
        Emblem(img, style, pw, roofBottom);
        if (style == BuildingStyle.Shop) Awning(img, dx - 3, dy - 5, 16);
        img.Outline(Pal.Ink);

        // Ground contact, painted after the outline so it blends into the grass around the footprint.
        Doorstep(img, dx, body, style is BuildingStyle.Hall or BuildingStyle.Shrine);
        if (style is not BuildingStyle.Hall and not BuildingStyle.Lab)
        {
            Shrub(img, 4, body - 3, 0);
            Shrub(img, pw - 5, body - 3, 1);
        }
        Tufts(img, pw, body, dx, (int)style);
        return img;
    }

    // ------------------------------------------------------------------ walls

    private static void Walls(PixelImage img, BuildingStyle style, int pw, int roofBottom, int body)
    {
        var wall = style switch
        {
            BuildingStyle.Hall => new[] { Color.Hex("b89c78"), Color.Hex("d8c29c"), Color.Hex("ecdcb8"), Color.Hex("f8ecd0") },
            BuildingStyle.Lab => new[] { Color.Hex("b4bcb8"), Color.Hex("d4dcd6"), Color.Hex("eaf0ea"), Color.Hex("fafcf6") },
            BuildingStyle.HouseBlue => new[] { Color.Hex("c4a47c"), Color.Hex("dcc090"), Color.Hex("ecd6a8"), Color.Hex("f8e8c4") },
            _ => new[] { Color.Hex("c9b08a"), Color.Hex("e3d0aa"), Color.Hex("f0e2c0"), Color.Hex("f8eed6") },
        };
        int x0 = 3, x1 = pw - 4, bottom = body - 4;
        for (int y = roofBottom; y < bottom; y++)
        for (int x = x0; x <= x1; x++)
        {
            float light = 0.78f - (x - x0) / (float)(x1 - x0) * 0.22f;
            // Siding boards on the blue house, block courses on the hall, plaster elsewhere.
            if (style == BuildingStyle.HouseBlue && (y - roofBottom) % 4 == 3) light -= 0.3f;
            if (style == BuildingStyle.HouseBlue && (y - roofBottom) % 4 == 0) light += 0.12f;
            if (style == BuildingStyle.Hall && ((y - roofBottom) % 6 == 5 || (x + (y - roofBottom) / 6 * 7) % 14 == 0)) light -= 0.28f;
            int under = y - roofBottom;
            if (under < 3) light -= 0.42f - under * 0.1f; // eave shadow
            if (x == x0) light += 0.15f;
            if (x == x1) light -= 0.25f;
            // Lab walls are smooth panels with a seam every few rows.
            if (style == BuildingStyle.Lab && (y - roofBottom) % 8 == 7) light -= 0.25f;
            // Flat steps of the ramp, like the roofs: painted walls, not grain.
            img[x, y] = wall[Math.Clamp((int)MathF.Round(light * (wall.Length - 1)), 0, wall.Length - 1)];
        }
        switch (style)
        {
            case BuildingStyle.House:
                // Timber frame: corner posts and a sill beam.
                Beam(img, x0, roofBottom, 2, bottom - roofBottom, vertical: true);
                Beam(img, x1 - 1, roofBottom, 2, bottom - roofBottom, vertical: true);
                Beam(img, x0, roofBottom + 3, x1 - x0 + 1, 2, vertical: false);
                break;
            case BuildingStyle.Lab:
                for (int x = x0; x <= x1; x++)
                {
                    img[x, roofBottom + 4] = Pal.RoofTeal[2];
                    img[x, roofBottom + 5] = Pal.RoofTeal[1];
                }
                break;
            case BuildingStyle.Hall:
                for (int x = 8; x < pw - 8; x += 16) Pillar(img, x, roofBottom, bottom - roofBottom, Pal.Plaster);
                break;
            case BuildingStyle.Shrine:
                Beam(img, x0, roofBottom, 2, bottom - roofBottom, vertical: true);
                Beam(img, x1 - 1, roofBottom, 2, bottom - roofBottom, vertical: true);
                break;
        }
    }

    private static void Beam(PixelImage img, int x, int y, int w, int h, bool vertical)
    {
        var wood = Pal.Wood;
        for (int j = 0; j < h; j++)
        for (int i = 0; i < w; i++)
        {
            int along = vertical ? i : j;
            img[x + i, y + j] = along == 0 ? wood[2] : wood[1];
        }
    }

    private static void Pillar(PixelImage img, int x, int y, int h, Color[] ramp)
    {
        for (int j = 0; j < h; j++)
        {
            img[x, y + j] = ramp[1];
            img[x + 1, y + j] = ramp[2];
            img[x + 2, y + j] = ramp[2];
            img[x + 3, y + j] = ramp[0];
        }
        for (int i = -1; i < 5; i++) { img[x + i, y + 3] = ramp[2]; img[x + i, y + h - 1] = ramp[1]; }
    }

    /// <summary>A course of uneven stones, one pixel wider than the walls on each side.</summary>
    private static void Foundation(PixelImage img, int pw, int body, bool grand)
    {
        var s = Pal.Stone;
        int top = body - 4;
        for (int course = 0; course < 2; course++)
        {
            int y0 = top + course * 2;
            int x = 2 - course * 3;
            int n = course * 7 + 1;
            while (x < pw - 2)
            {
                int width = 4 + (int)(Art.Hash(n++, course, grand ? 9 : 3) * 4);
                for (int i = 0; i < width && x + i < pw - 2; i++)
                for (int j = 0; j < 2; j++)
                {
                    int px = x + i;
                    if (px < 2) continue;
                    bool joint = i == width - 1;
                    var c = joint ? s[0] : j == 0 ? (i == 0 ? s[3] : s[2]) : s[1];
                    img[px, y0 + j] = c;
                }
                x += width;
            }
        }
    }

    private static void Window(PixelImage img, BuildingStyle style, int x, int y)
    {
        const int w = 8, h = 9;
        var frame = style == BuildingStyle.Lab ? Pal.Stone : Pal.Wood;
        img.FillRect(x - 1, y - 1, w + 2, h + 2, frame[0]);
        for (int j = 0; j < h; j++)
        for (int i = 0; i < w; i++)
        {
            // Sky reflection: light at the top, a diagonal glint, the room darker below.
            var c = Color.Lerp(Pal.Water[3], Pal.Water[1], j / (float)h);
            if (i + j is 5 or 6) c = Color.Lerp(c, Pal.White, 0.55f);
            img[x + i, y + j] = c;
        }
        img.FillRect(x + w / 2, y, 1, h, frame[2]);
        img.FillRect(x, y + h / 2, w, 1, frame[2]);
        img.FillRect(x - 1, y + h + 1, w + 2, 1, frame[3]);
        if (style is BuildingStyle.House or BuildingStyle.HouseBlue or BuildingStyle.Shrine)
        {
            // Shutters and a flower box.
            var shutter = style == BuildingStyle.HouseBlue ? Pal.RoofBlue : style == BuildingStyle.Shrine ? Pal.RoofPink : Pal.RoofRed;
            for (int j = 0; j < h; j++)
            {
                img[x - 2, y + j] = j % 3 == 2 ? shutter[0] : shutter[1];
                img[x + w + 1, y + j] = j % 3 == 2 ? shutter[0] : shutter[1];
            }
            img.FillRect(x - 1, y + h + 2, w + 2, 2, Pal.Wood[1]);
            img.FillRect(x - 1, y + h + 2, w + 2, 1, Pal.Wood[2]);
            for (int i = 0; i < w + 2; i++)
            {
                bool bloom = (i + x) % 3 == 0;
                img[x - 1 + i, y + h + 1] = bloom ? Pal.FlowerColors[(i + x / 16) % 4] : Pal.Grass[i % 2 == 0 ? 1 : 2];
            }
        }
    }

    private static void Door(PixelImage img, BuildingStyle style, int dx, int dy, int body)
    {
        var wd = Pal.Wood;
        bool glass = style is BuildingStyle.Shop or BuildingStyle.Shrine or BuildingStyle.Lab;
        // Frame and lintel; the opening runs through the foundation down to the step.
        img.FillRect(dx - 2, dy - 2, 14, body - dy + 2, wd[0]);
        img.FillRect(dx - 2, dy - 2, 14, 1, wd[2]);
        if (glass)
        {
            for (int y = dy; y < body; y++)
            for (int x = dx; x < dx + 10; x++)
                img[x, y] = Color.Lerp(Pal.Sky, Pal.Water[1], (y - dy) / 16f);
            img.FillRect(dx + 4, dy, 2, body - dy, wd[1]);
            for (int i = 0; i < 4; i++) img[dx + 1 + i, dy + 4 - i] = Pal.White;
        }
        else
        {
            for (int y = dy; y < body; y++)
            for (int x = dx; x < dx + 10; x++)
                img[x, y] = x == dx ? wd[1] : (x - dx) % 3 == 0 ? wd[1] : wd[2];
            img.FillRect(dx, dy, 10, 1, wd[3]);
            img.FillRect(dx + 3, dy + 3, 4, 3, Pal.Water[2]);
            img[dx + 3, dy + 3] = Pal.Water[4];
            img[dx + 8, dy + 9] = Pal.Gold;
            img[dx + 8, dy + 10] = Pal.GoldDark;
        }
        // Recess shadow along the top and left of the opening.
        for (int x = dx; x < dx + 10; x++) img[x, dy] = img[x, dy].Shade(0.75f);
        for (int y = dy; y < body; y++) img[dx, y] = img[dx, y].Shade(0.8f);
        if (style is BuildingStyle.House or BuildingStyle.HouseBlue or BuildingStyle.Shop)
        {
            // A small wall lantern beside the door.
            int lx = dx + 12, ly = dy + 1;
            img.FillRect(lx, ly, 3, 1, Pal.Ink);
            img.FillRect(lx, ly + 1, 3, 3, Pal.Gold);
            img[lx, ly + 1] = Pal.White;
            img.FillRect(lx, ly + 4, 3, 1, Pal.GoldDark);
        }
    }

    private static void Awning(PixelImage img, int x, int y, int w)
    {
        for (int i = 0; i < w; i++)
        {
            bool stripe = i / 2 % 2 == 0;
            var c = stripe ? Pal.Coral : Pal.Cream;
            var d = stripe ? Pal.CoralDark : Pal.CreamShade;
            img[x + i, y] = d;
            img[x + i, y + 1] = c;
            img[x + i, y + 2] = c;
            if (i % 4 != 3) img[x + i, y + 3] = d;
        }
    }

    // ------------------------------------------------------------------ roof

    /// <summary>
    /// A hipped, shingled roof: staggered tiles with a shadowed lower lip, per-tile tone variation,
    /// lit hips on the left and shaded hips on the right, a ridge cap and a dark fascia over the eave.
    /// </summary>
    private static void Roof(PixelImage img, Color[] roof, int pw, int roofBottom)
    {
        int left = 1, right = pw - 2;
        int hip = Math.Min(12, roofBottom / 3);
        float slope = roofBottom * 0.55f;
        for (int y = 0; y < roofBottom; y++)
        {
            int inset = (int)MathF.Max(0, hip * (1 - y / slope));
            int xa = left + inset, xb = right - inset;
            for (int x = xa; x <= xb; x++)
            {
                int row = y / 4, ly = y % 4;
                int off = row % 2 * 3, col = (x + off) / 6, lx = (x + off) % 6;
                float light = 0.88f - y / (float)roofBottom * 0.3f - (x - left) / (float)(right - left) * 0.32f;
                light += (Art.Hash(col, row, 17) - 0.5f) * 0.08f;
                if (ly == 3) light -= 0.34f;
                else if (ly == 0) light += 0.1f;
                else if (lx == 0) light -= 0.22f;
                if (inset > 0 && x - xa < 2) light += 0.25f;
                if (inset > 0 && xb - x < 2) light -= 0.25f;
                // Each shingle takes one flat step of the ramp: rows read as tiles, not as noise.
                img[x, y] = roof[Math.Clamp((int)MathF.Round(light * (roof.Length - 1)), 0, roof.Length - 1)];
            }
        }
        // Ridge cap and fascia.
        img.FillRect(left + hip, 0, right - left - hip * 2 + 1, 1, roof[3]);
        img.FillRect(left + hip, 1, right - left - hip * 2 + 1, 1, roof[2]);
        img.FillRect(left, roofBottom - 2, right - left + 1, 1, roof[1]);
        img.FillRect(left, roofBottom - 1, right - left + 1, 1, roof[0]);
    }

    private static void Chimney(PixelImage img, int x, int roofBottom, bool brick)
    {
        var ramp = brick ? new[] { Color.Hex("6a3a3a"), Color.Hex("9a5248"), Color.Hex("bc6e5a"), Color.Hex("d89078") } : Pal.Stone;
        int top = 1, bottom = Math.Max(8, roofBottom / 3);
        for (int y = top; y < bottom; y++)
        for (int i = 0; i < 6; i++)
        {
            bool mortar = (y - top) % 3 == 2 || (i + (y - top) / 3 * 3) % 6 == 0;
            img[x + i, y] = mortar ? ramp[0] : i == 0 ? ramp[3] : i == 5 ? ramp[1] : ramp[2];
        }
        img.FillRect(x - 1, top - 1, 8, 2, ramp[1]);
        img.FillRect(x - 1, top - 1, 8, 1, ramp[3]);
        // Shadow the chimney casts down the roof.
        for (int y = bottom; y < bottom + 2; y++)
        for (int i = 1; i < 8; i++)
            if (img[x + i, y].A > 0) img[x + i, y] = img[x + i, y].Shade(0.72f);
    }

    private static void Emblem(PixelImage img, BuildingStyle style, int pw, int roofBottom)
    {
        int cx = pw / 2, cy = roofBottom / 2 + 1;
        switch (style)
        {
            case BuildingStyle.Shop:
                img.FillRect(cx - 9, cy - 5, 18, 10, Pal.Cream);
                img.FillRect(cx - 9, cy + 4, 18, 1, Pal.CreamShade);
                img.DrawRect(cx - 10, cy - 6, 20, 12, Pal.Ink);
                OrbIcon(img, cx, cy);
                break;
            case BuildingStyle.Shrine:
                img.FillCircle(cx, cy, 6, Pal.Ink);
                img.FillCircle(cx, cy, 5.2f, Pal.Cream);
                img.FillCircle(cx - 1.5f, cy - 0.5f, 2.2f, Pal.Coral);
                img.FillCircle(cx + 1.5f, cy - 0.5f, 2.2f, Pal.Coral);
                img.FillRect(cx - 2, cy, 4, 2, Pal.Coral);
                img[cx, cy + 3] = Pal.Coral;
                break;
            case BuildingStyle.Hall:
                img.FillCircle(cx, cy, 9, Pal.Ink);
                img.FillCircle(cx, cy, 8, Pal.Cream);
                img.FillCircle(cx, cy, 6, Pal.Gold);
                for (int i = 0; i < 5; i++)
                {
                    float a = -MathF.PI / 2 + i * MathF.Tau / 5;
                    img.Line(cx, cy, cx + (int)MathF.Round(MathF.Cos(a) * 5), cy + (int)MathF.Round(MathF.Sin(a) * 5), Pal.Cream);
                }
                break;
            case BuildingStyle.Lab:
                img.FillRect(pw - 16, 2, 2, 7, Pal.Stone[1]);
                img.FillEllipse(pw - 15, 2, 4, 2, Pal.Stone[3]);
                img.FillRect(pw - 17, 2, 6, 1, Pal.Stone[2]);
                break;
        }
    }

    public static void OrbIcon(PixelImage img, int cx, int cy)
    {
        for (int y = -4; y <= 4; y++)
        for (int x = -4; x <= 4; x++)
        {
            if (x * x + y * y > 13) continue;
            img[cx + x, cy + y] = y < 0 ? Pal.Coral : y == 0 ? Pal.Ink : Pal.White;
        }
        img[cx, cy] = Pal.White;
        img[cx - 1, cy] = Pal.Ink;
        img[cx + 1, cy] = Pal.Ink;
    }

    // ------------------------------------------------------------------ ground contact

    private static void Doorstep(PixelImage img, int dx, int body, bool wide)
    {
        int x0 = dx - (wide ? 3 : 2), x1 = dx + (wide ? 12 : 11);
        for (int x = x0; x <= x1; x++)
        {
            bool end = x == x0 || x == x1;
            img[x, body] = end ? Pal.Stone[1] : Pal.Stone[3];
            img[x, body + 1] = end ? Pal.Stone[0] : Pal.Stone[1];
            img[x, body + 2] = Pal.Ink.WithAlpha(0.35f);
        }
    }

    /// <summary>A small round shrub hugging a front corner, overlapping the foundation.</summary>
    private static void Shrub(PixelImage img, int cx, int cy, int seed)
    {
        var c = new CreatureCanvas(12);
        var m = new Material(Pal.Canopy[1..5], Pal.Canopy[0]);
        c.Circle(3.5f, 7, 3.4f, m).Circle(8, 7, 3.4f, m).Circle(6, 4.5f, 3.6f, m, contour: false);
        c.Dot(4 + seed * 3, 4, 0.6f, Pal.FlowerColors[seed == 0 ? 0 : 1]);
        img.Blit(c.Render(), cx - 6, cy - 8);
    }

    /// <summary>Blades of grass growing up in front of the foundation so the base line never reads as a cut.</summary>
    private static void Tufts(PixelImage img, int pw, int body, int dx, int seed)
    {
        var g = Pal.Grass;
        int x = 3 + (int)(Art.Hash(seed, 1, 50) * 4);
        int n = 0;
        while (x < pw - 3)
        {
            if (x < dx - 3 || x > dx + 13)
            {
                int height = 2 + (int)(Art.Hash(n, seed, 51) * 3);
                for (int h = 0; h < height; h++)
                {
                    img[x, body + 1 - h] = h == 0 ? g[0] : h == height - 1 ? g[3] : g[1];
                    if (h < height - 1) img[x + 1, body + 1 - h] = h == 0 ? g[1] : g[2];
                }
                img[x - 1, body + 1] = g[1];
            }
            x += 4 + (int)(Art.Hash(seed, n++, 52) * 6);
        }
    }
}

using MicroEngine;
using MicroEngine.Graphics;
using TrailDuel.World;

namespace TrailDuel.Visuals;

public enum BuildingStyle { House, HouseBlue, Lab, Shrine, Shop, Hall, HouseGreen }

/// <summary>
/// Buildings fitted to their tile footprint, never reaching above it, so a roof never hides someone walking behind.
/// Homes are the pack's houses (red, green or blue roofs; a gabled or flat-roofed shape by footprint), placed so their
/// door stands on the footprint's door tile. The lab, shrine, shop and hall have no drawing in the pack: their facades
/// are assembled from the pack's flat-roofed house (its roof, plank walls, foundation, door bay, windows and steps),
/// stretched to the footprint, recoloured, and given a porch roof and an emblem in code.
/// </summary>
internal static class BuildingArt
{
    /// <summary>Rows drawn below the footprint: the steps in front of the door reach onto the tile before it.</summary>
    public const int Skirt = 7;

    /// <summary>A house of the pack: its rectangle on the sheet (red roof), the door's centre column and the foundation's last row.</summary>
    private sealed record PackHouse(int X, int Y, int W, int H, int DoorCenter, int Base);

    // The green and blue houses sit 128 and 256 rows below the red ones on the sheet.
    private static readonly PackHouse WideGabled = new(166, 336, 69, 59, 34, 51);
    private static readonly PackHouse SmallGabled = new(99, 336, 56, 59, 21, 53);
    private static readonly PackHouse WideFlat = new(7, 405, 67, 54, 33, 46);

    private static (PackHouse Shape, int Colour)? HouseFor(BuildingStyle style, int w) => style switch
    {
        BuildingStyle.House => (w >= 5 ? WideGabled : SmallGabled, 0),
        BuildingStyle.HouseGreen => (w >= 5 ? WideFlat : SmallGabled, 1),
        BuildingStyle.HouseBlue => (w >= 5 ? WideFlat : SmallGabled, 2),
        _ => null,
    };

    /// <summary>
    /// Where the drawing sits in the footprint, in pixels from its top-left: left and top edge of the building and
    /// the column after its right edge (shadows follow the drawing, not the footprint).
    /// </summary>
    public static (int Left, int Top, int Right) Bounds(BuildingDef def)
    {
        if (HouseFor(def.Style, def.W) is not { } house) return (0, 0, def.W * 16);
        var (ox, oy) = Offset(house.Shape, def);
        return (ox, oy, ox + house.Shape.W);
    }

    private static (int X, int Y) Offset(PackHouse shape, BuildingDef def)
        => (def.DoorX * 16 + 8 - shape.DoorCenter, def.H * 16 - 1 - shape.Base);

    /// <summary>Whether the drawing fits inside the footprint (the steps may reach <see cref="Skirt"/> rows below it).</summary>
    public static bool Fits(BuildingDef def)
    {
        var (left, top, right) = Bounds(def);
        return left >= 0 && top >= 0 && right <= def.W * 16;
    }

    public static PixelImage Build(Village v, BuildingDef def)
    {
        var img = new PixelImage(def.W * 16, def.H * 16 + Skirt);
        if (HouseFor(def.Style, def.W) is { } house)
        {
            var (ox, oy) = Offset(house.Shape, def);
            img.Blit(v.Cut(house.Shape.X, house.Shape.Y + house.Colour * 128, house.Shape.W, house.Shape.H), ox, oy);
            return img;
        }
        return Facade(v, def, img);
    }

    // ------------------------------------------------------------------ facades

    // The flat-roofed red house the facades are built from (sheet rectangle) and its parts, in its own rows and columns.
    private const int SrcX = 7, SrcY = 405, SrcW = 67, SrcH = 54;
    private const int RoofRows = 24, WallTop = 24, PlankCycle = 28, Foundation = 46, StepsEnd = 53;
    private const int Cap = 6, RoofPeriod = 12, WallFill = 17, WallPeriod = 4;
    private const int BayX = 21, BayW = 25, BayDoorCenter = 12;

    private static PixelImage Facade(Village v, BuildingDef def, PixelImage img)
    {
        var src = v.Cut(SrcX, SrcY, SrcW, SrcH);
        int w = def.W * 16, h = def.H * 16;
        int extra = h - (Foundation + 1), roofExtra = extra / 4, wallExtra = extra - roofExtra;
        // Source row for each row of the facade: the roof's stripes and the walls' plank courses repeat to fill the height.
        var rows = new List<int>();
        for (int r = 0; r < 10; r++) rows.Add(r);
        for (int i = 0; i < roofExtra; i++) rows.Add(9);
        for (int r = 10; r < RoofRows; r++) rows.Add(r);
        int roofEnd = rows.Count;
        for (int r = WallTop; r < PlankCycle; r++) rows.Add(r);
        for (int i = 0; i < wallExtra; i++) rows.Add(PlankCycle + i % 6);
        for (int r = PlankCycle; r <= Foundation; r++) rows.Add(r);

        var roof = RoofColours(v, def.Style);
        var wall = WallTint(def.Style);
        for (int y = 0; y < h; y++)
        {
            int sr = rows[y];
            bool isRoof = y < roofEnd;
            for (int x = 0; x < w; x++)
            {
                int sx = x < Cap ? x : x >= w - Cap ? SrcW - (w - x)
                    : isRoof ? Cap + (x - Cap) % RoofPeriod : WallFill + (x - Cap) % WallPeriod;
                var c = src[sx, sr];
                if (c.A == 0) continue;
                img[x, y] = isRoof ? roof.GetValueOrDefault(c, c) : wall(c);
            }
        }

        // The door bay (door, frame, foundation and steps) with a porch roof over it.
        int bx = def.DoorX * 16 + 8 - BayDoorCenter, bottom = h - 1;
        for (int sr = WallTop + 2; sr <= StepsEnd; sr++)
        for (int sx = 0; sx < BayW; sx++)
        {
            var c = src[BayX + sx, sr];
            if (c.A == 0) continue;
            img[bx + sx, bottom - (Foundation - sr)] = sr <= Foundation && !IsDoor(c) ? wall(c) : c;
        }
        int porch = bottom - (Foundation - WallTop - 2) - 4;
        Porch(img, def.Style, roof, bx - 1, porch, BayW + 2);

        // Windows on each tile column clear of the bay, a second row on two-storey facades.
        int windowTop = bottom - (Foundation - PlankCycle);
        for (int floor = 0; floor < (wallExtra >= 12 ? 2 : 1); floor++)
            for (int tx = 0; tx < def.W; tx++)
            {
                int wx = tx * 16 + 4;
                if (floor == 0 && wx + 8 > bx - 2 && wx < bx + BayW + 2) continue;
                if (wx < Cap || wx + 8 > w - Cap) continue;
                for (int j = 0; j < 7; j++)
                for (int i = 0; i < 8; i++)
                    img[wx + i, windowTop - floor * 12 + j] = wall(src[9 + i, PlankCycle + j]);
            }
        Emblem(img, def.Style, w, roofEnd);
        return img;
    }

    private static bool IsDoor(Color c) => c == Color.Hex("ab4a36") || c == Color.Hex("c26a4b") || c == Color.Hex("b35e3f")
        || c == Color.Hex("f8d239") || c == Color.Hex("f2b22b") || c == Color.Hex("ed931e");

    /// <summary>
    /// The red roof's colours mapped to the building's: the pack's blue for the shop, its green turned to teal for the
    /// lab, the red turned to pink for the shrine and to gold for the hall.
    /// </summary>
    private static Dictionary<Color, Color> RoofColours(Village v, BuildingStyle style)
    {
        Color[] red = [Color.Hex("ba2d2f"), Color.Hex("d93232"), Color.Hex("e63f38"), Color.Hex("fc5c46")];
        Dictionary<Color, Color> Learned(int rows)
        {
            var learned = Village.Recolouring(v.Cut(SrcX, SrcY, SrcW, SrcH), v.Cut(SrcX, SrcY + rows, SrcW, SrcH));
            return red.ToDictionary(c => c, c => learned.GetValueOrDefault(c, c));
        }
        return style switch
        {
            BuildingStyle.Shop => Learned(256),
            BuildingStyle.Lab => Learned(128).ToDictionary(kv => kv.Key, kv => Village.WithHue(kv.Value, 2.75f)),
            BuildingStyle.Shrine => red.ToDictionary(c => c, c => Village.WithHue(c, 5.55f, 0.72f)),
            _ => red.ToDictionary(c => c, c => Village.WithHue(c, 0.62f, 0.95f)), // the hall's gold
        };
    }

    /// <summary>The wall planks of each building: plain wood, pale panels for the lab, stone-washed for the hall.</summary>
    private static Func<Color, Color> WallTint(BuildingStyle style) => style switch
    {
        BuildingStyle.Lab => c => Plank(c) ? Color.Lerp(Grey(c), Color.Hex("f6f4ec"), 0.55f) : c,
        BuildingStyle.Hall => c => Plank(c) ? Color.Lerp(c, Grey(c), 0.55f) : c,
        _ => c => c,
    };

    private static readonly HashSet<Color> Planks =
        [.. new[] { "a85f46", "c78159", "e0ad70", "e8c37e", "e0b870", "c78c59", "b5754d", "f1ce8e", "916662" }.Select(Color.Hex)];

    private static bool Plank(Color c) => Planks.Contains(c);

    private static Color Grey(Color c)
    {
        byte l = (byte)((c.R * 3 + c.G * 5 + c.B * 2) / 10);
        return new Color(l, l, l, c.A);
    }

    /// <summary>A short porch roof over the door bay: striped awning on the shop, the building's roof colour elsewhere.</summary>
    private static void Porch(PixelImage img, BuildingStyle style, Dictionary<Color, Color> roof, int x, int y, int w)
    {
        Color dark = roof[Color.Hex("ba2d2f")], mid = roof[Color.Hex("d93232")], light = roof[Color.Hex("fc5c46")];
        for (int i = 0; i < w; i++)
        {
            bool stripe = i / 3 % 2 == 0;
            var top = style == BuildingStyle.Shop ? (stripe ? Pal.Coral : Pal.Cream) : light;
            var low = style == BuildingStyle.Shop ? (stripe ? Pal.CoralDark : Pal.CreamShade) : (i % 6 < 2 ? dark : mid);
            img[x + i, y] = VillagePal.Outline;
            img[x + i, y + 1] = top;
            img[x + i, y + 2] = low;
            img[x + i, y + 3] = i % 3 == 2 && style == BuildingStyle.Shop ? low : VillagePal.Outline;
        }
        img[x - 1, y + 1] = VillagePal.Outline; img[x - 1, y + 2] = VillagePal.Outline;
        img[x + w, y + 1] = VillagePal.Outline; img[x + w, y + 2] = VillagePal.Outline;
    }

    private static void Emblem(PixelImage img, BuildingStyle style, int pw, int roofEnd)
    {
        int cx = pw / 2, cy = roofEnd / 2 + 2;
        var ink = VillagePal.Outline;
        switch (style)
        {
            case BuildingStyle.Shop:
                img.FillRect(cx - 9, cy - 5, 18, 10, Pal.Cream);
                img.FillRect(cx - 9, cy + 4, 18, 1, Pal.CreamShade);
                img.DrawRect(cx - 10, cy - 6, 20, 12, ink);
                OrbIcon(img, cx, cy);
                break;
            case BuildingStyle.Shrine:
                img.FillCircle(cx, cy, 6, ink);
                img.FillCircle(cx, cy, 5.2f, Pal.Cream);
                img.FillCircle(cx - 1.5f, cy - 0.5f, 2.2f, Pal.Coral);
                img.FillCircle(cx + 1.5f, cy - 0.5f, 2.2f, Pal.Coral);
                img.FillRect(cx - 2, cy, 4, 2, Pal.Coral);
                img[cx, cy + 3] = Pal.Coral;
                break;
            case BuildingStyle.Hall:
                img.FillCircle(cx, cy, 9, ink);
                img.FillCircle(cx, cy, 8, Pal.Cream);
                img.FillCircle(cx, cy, 6, Pal.Gold);
                for (int i = 0; i < 5; i++)
                {
                    float a = -MathF.PI / 2 + i * MathF.Tau / 5;
                    img.Line(cx, cy, cx + (int)MathF.Round(MathF.Cos(a) * 5), cy + (int)MathF.Round(MathF.Sin(a) * 5), Pal.Cream);
                }
                break;
            case BuildingStyle.Lab:
                // A weather vane and a round sign with the lab's leaf.
                img.FillCircle(cx, cy, 6, ink);
                img.FillCircle(cx, cy, 5.2f, Pal.Cream);
                img.FillEllipse(cx, cy, 2.2f, 3.4f, VillagePal.Leaf[3]);
                img.Line(cx, cy - 3, cx, cy + 3, VillagePal.Leaf[1]);
                break;
        }
    }

    private static void OrbIcon(PixelImage img, int cx, int cy)
    {
        for (int y = -4; y <= 4; y++)
        for (int x = -4; x <= 4; x++)
        {
            if (x * x + y * y > 13) continue;
            img[cx + x, cy + y] = y < 0 ? Pal.Coral : y == 0 ? VillagePal.Outline : Pal.White;
        }
        img[cx, cy] = Pal.White;
        img[cx - 1, cy] = VillagePal.Outline;
        img[cx + 1, cy] = VillagePal.Outline;
    }
}

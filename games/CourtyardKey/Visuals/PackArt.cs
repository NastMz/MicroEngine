using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;

namespace CourtyardKey.Visuals;

/// <summary>
/// The vale's houses and trees: hand-drawn pieces of ArMM1998's "Zelda-like tilesets and sprites" (CC0, see
/// CREDITS.md) in <c>Content/vale</c>, cut from the sheet along their own silhouettes and repainted at load in the
/// world ramps of <see cref="Pal"/> (timber, foliage, the coloured ink outline and the one cool cast shadow), so they
/// stand in the same light as the procedural ground.
/// </summary>
public static class PackArt
{
    /// <summary>The sheet, as copied unedited from the pack.</summary>
    public const string Sheet = "vale/overworld.png";

    // The pack's own colours, by role.
    private static readonly Color PackInk = Color.Hex("201729");
    private static readonly Color[] PackWood = [Color.Hex("563a3f"), Color.Hex("79584f"), Color.Hex("94785c"), Color.Hex("a89080")];
    private static readonly Color PackGlass = Color.Hex("e0caca");
    private static readonly Color TreeInk = Color.Hex("2b2f35");
    private static readonly Color TreeShade = Color.Hex("1c1d26");
    private static readonly Color[] PackLeaf = [Color.Hex("2d5b43"), Color.Hex("2b7d4a"), Color.Hex("328e53"), Color.Hex("299750"), Color.Hex("2eca5c"), Color.Hex("50eb6c")];
    private static readonly Color[] PackRoot = [Color.Hex("484744"), Color.Hex("65675a"), Color.Hex("79856b")];

    // Roofs: a weathered terracotta for the elder's house, slate for Marla's shop (dark to light, like PackWood).
    private static readonly Color[] HomeRoof = [Color.Hex("4a1e26"), Color.Hex("7a3330"), Color.Hex("a8503c"), Color.Hex("cf7a52")];
    private static readonly Color[] ShopRoof = [Color.Hex("1f2a48"), Color.Hex("31476e"), Color.Hex("4a6c96"), Color.Hex("7898bc")];

    /// <summary>Rows of the house's upright roof planks left out so the house fits its 4x3 footprint under the room's top edge.</summary>
    private const int RoofCutFrom = 20, RoofCut = 14;
    /// <summary>The bottom of the house walls in the cropped picture (its cast shadow runs below it).</summary>
    private const int HouseWallBottom = 74 - RoofCut;
    /// <summary>The left edge of the house walls in the cropped picture; its eaves overhang the footprint by this much.</summary>
    private const int HouseLeft = 6;

    public static void AddAll(SpriteAtlasBuilder b, Func<string, PixelImage> image)
    {
        foreach (var (name, img, origin) in Pieces(image)) b.Add(name, img, origin);
    }

    /// <summary>Every repainted pack piece with its pivot: the bottom-left corner of the solid footprint it stands on.</summary>
    /// <param name="image">Reads a content image by path.</param>
    public static IEnumerable<(string Name, PixelImage Image, Vector2 Origin)> Pieces(Func<string, PixelImage> image)
    {
        var sheet = image(Sheet);
        // A tree fills its 2x2 footprint; its cast shadow falls inside it, to the lower right.
        var tree = sheet.Crop(80, 256, 32, 32);
        // Three tones so a row of trees does not read as one stamp: the vale's green, a deeper one, a sunlit one.
        var l = Pal.Foliage;
        yield return ("tree0", Tree(tree, [l[1], l[2], l[3], l[3], l[4], l[5]]), new Vector2(0, 32));
        yield return ("tree1", Tree(tree, [l[0], l[1], l[2], l[2], l[3], l[4]]), new Vector2(0, 32));
        yield return ("tree2", Tree(tree, [l[1], l[2], l[3], l[4], l[5], Pal.Turf[5]]), new Vector2(0, 32));
        // Houses stand on a 4x3 footprint; the roof rises above it and the eaves overhang it a little.
        var house = sheet.Crop(96, 0, 80, 80);
        yield return ("house_home", House(house, HomeRoof, shop: false), new Vector2(HouseLeft, HouseWallBottom));
        yield return ("house_shop", House(house, ShopRoof, shop: true), new Vector2(HouseLeft, HouseWallBottom));
    }

    /// <param name="leaf">The six greens that replace the pack's, dark to light.</param>
    private static PixelImage Tree(PixelImage crop, Color[] leaf)
    {
        var img = crop.Clone();
        for (int i = 0; i < PackLeaf.Length; i++) img.Recolor(PackLeaf[i], leaf[i]);
        for (int i = 0; i < PackRoot.Length; i++) img.Recolor(PackRoot[i], Pal.Timber[i + 1]);
        img.Recolor(TreeShade.WithAlpha((byte)102), Pal.CastShadow.WithAlpha((byte)120));
        img.Recolor(TreeInk, Pal.Ink);
        return ArtKit.ColorOutline(img, Pal.Ink);
    }

    private static PixelImage House(PixelImage crop, Color[] roofRamp, bool shop)
    {
        // The roof is everything the outline encloses around the two roof seeds (left and right of the ridge).
        var roof = Enclosed(crop, PackInk, (40, 8), (55, 20));
        var img = crop.Clone().Map((x, y, c) =>
        {
            if (c.A == 0) return c;
            if (c.A < 255) return Pal.CastShadow.WithAlpha((byte)120);
            if (c == PackInk) return Pal.Ink;
            if (c == PackGlass) return Pal.Gold[3];
            int i = Array.IndexOf(PackWood, c);
            if (i < 0) return c;
            return roof[x, y] ? roofRamp[i] : Pal.Timber[i + 1];
        });
        if (shop) HangSign(img);
        img = WithoutRows(img, RoofCutFrom, RoofCut);
        // Seat the house on the grass: a few blades lean against the plinth.
        for (int x = HouseLeft; x < img.Width - 8; x++)
            if (img[x, HouseWallBottom - 1].A == 255 && Art.Hash(x, 3, 551) > 0.55f)
                img[x, HouseWallBottom - 1] = Art.Hash(x, 4, 551) > 0.5f ? Pal.Turf[4] : Pal.Turf[3];
        return ArtKit.ColorOutline(img, Pal.Ink);
    }

    /// <summary>Marla's sign: a gem on a plank hung from the eave beside the door.</summary>
    private static void HangSign(PixelImage img)
    {
        var w = Pal.Timber;
        int x = 50, y = 56;
        img.FillRect(x + 2, y - 4, 1, 4, Pal.Ink);
        img.FillRect(x + 11, y - 4, 1, 4, Pal.Ink);
        img.FillRect(x, y, 14, 10, Pal.Ink);
        img.FillRect(x + 1, y + 1, 12, 8, w[3]);
        img.FillRect(x + 1, y + 1, 12, 1, w[4]);
        img.FillRect(x + 1, y + 8, 12, 1, w[2]);
        img.Blit(ItemArt.Gem(Pal.Gem), x + 3, y + 1);
        ArtKit.Soft(img, x + 14, y + 2, Pal.CastShadow, 0.5f);
        for (int k = 1; k < 10; k++) ArtKit.Soft(img, x + 14, y + k, Pal.CastShadow, 0.45f);
    }

    /// <summary>The pixels reachable from the seeds without crossing <paramref name="wall"/> or transparency.</summary>
    private static bool[,] Enclosed(PixelImage img, Color wall, params (int X, int Y)[] seeds)
    {
        var inside = new bool[img.Width, img.Height];
        var stack = new Stack<(int X, int Y)>(seeds);
        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();
            if (!img.InBounds(x, y) || inside[x, y] || img[x, y].A < 255 || img[x, y] == wall) continue;
            inside[x, y] = true;
            stack.Push((x + 1, y)); stack.Push((x - 1, y)); stack.Push((x, y + 1)); stack.Push((x, y - 1));
        }
        return inside;
    }

    private static PixelImage WithoutRows(PixelImage img, int from, int count)
    {
        var cut = new PixelImage(img.Width, img.Height - count);
        cut.Blit(img, 0, 0, img.Width, from, 0, 0);
        cut.Blit(img, 0, from + count, img.Width, img.Height - from - count, 0, from);
        return cut;
    }
}

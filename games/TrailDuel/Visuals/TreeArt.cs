using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using TrailDuel.World;

namespace TrailDuel.Visuals;

/// <summary>
/// The pack's trees, each cut whole along its silhouette: four 32x38 broadleaf trees (yellow-green, green, grey-teal,
/// teal) drawn as they are or mirrored, and its small 26x28 tree recoloured to each of the four. A tree stands on its
/// tile with the trunk at the bottom; <see cref="TreeLayout"/> picks which one and where its crown may reach.
/// </summary>
internal static class TreeArt
{
    public const int Colours = 4;
    /// <summary>Variants: each colour as drawn and mirrored.</summary>
    public const int Variants = Colours * 2;
    public const int TallWidth = 32, TallHeight = 38, LowWidth = 26, LowHeight = 28;
    /// <summary>Pixels a tall crown rises above its tile; a low tree rises <see cref="LowRise"/>.</summary>
    public const int TallRise = 20, LowRise = 8;

    public static void AddAll(SpriteAtlasBuilder b, Village v)
    {
        var tall = new PixelImage[Colours];
        for (int c = 0; c < Colours; c++) tall[c] = v.Piece(144 + c * TallWidth, 202, TallWidth, TallHeight, 16, 32);
        var low = v.Piece(274, 212, LowWidth, LowHeight, 13, 24);
        for (int c = 0; c < Colours; c++)
        {
            var lowC = c == 0 ? low : Village.Recolour(low, Village.Recolouring(tall[0], tall[c]));
            for (int f = 0; f < 2; f++)
            {
                int variant = c * 2 + f;
                var t = Village.Grade(f == 0 ? tall[c] : tall[c].FlipX());
                var l = Village.Grade(f == 0 ? lowC : lowC.FlipX());
                // The trunk's foot (row 35 of the piece) stands on the tile's bottom row.
                b.Add(TreeLayout.TallKey(variant, 0), t, new Vector2(8, TallRise));
                b.Add(TreeLayout.TallKey(variant, -1), t, new Vector2(16, TallRise));
                b.Add(TreeLayout.TallKey(variant, 1), t, new Vector2(0, TallRise));
                b.Add(TreeLayout.LowKey(variant), l, new Vector2(5, LowRise));
            }
        }
    }
}

/// <summary>Where a tree tile's tree reaches: its variant, a sideways shift of its crown (-1 left, 1 right) or a low tree.</summary>
internal readonly record struct TreePlacement(int Variant, int Shift, bool Low)
{
    public string Key => Low ? TreeLayout.LowKey(Variant) : TreeLayout.TallKey(Variant, Shift);
}

/// <summary>
/// Chooses the tree drawn on each tree tile. A tall crown rises 20 pixels into the row above and half a tile to each
/// side; it is drawn after that row's characters, so it would cover anyone walking just above it. A tall tree therefore
/// stands only where the three tiles above it are not walkable; with walkable ground on one diagonal its crown leans the
/// other way, and with walkable ground straight above (or on both diagonals) the tile gets the pack's small tree, whose
/// crown rises only 8 pixels. Woods are tall trees overlapping row after row, as the pack draws its own woods.
/// </summary>
internal static class TreeLayout
{
    private static readonly string[] Tall = Keys("c"), TallLeft = Keys("l"), TallRight = Keys("r"), LowKeys = SpriteKeys.Numbered("t:treelow", TreeArt.Variants);

    private static string[] Keys(string suffix)
    {
        var keys = new string[TreeArt.Variants];
        for (int i = 0; i < keys.Length; i++) keys[i] = $"t:tree{i}{suffix}";
        return keys;
    }

    public static string TallKey(int variant, int shift) => (shift < 0 ? TallLeft : shift > 0 ? TallRight : Tall)[variant];
    public static string LowKey(int variant) => LowKeys[variant];

    /// <summary>The tree on (<paramref name="x"/>, <paramref name="y"/>), or null when the tile holds none.</summary>
    public static TreePlacement? At(MapDef map, int x, int y)
    {
        var kind = map.KindAt(x, y);
        if (kind is not (TileKind.Tree or TileKind.Pine)) return null;
        // Route and town trees are the pack's greens, the Hollow Wood's pines its teals.
        int colour = (kind == TileKind.Pine ? 2 : 0) + (Art.Hash(x, y, 61) < 0.5f ? 0 : 1);
        int variant = colour * 2 + (Art.Hash(x, y, 62) < 0.5f ? 0 : 1);
        if (Gap(map, x, y)) return null;
        bool above = Walkable(map, x, y - 1), aboveLeft = Walkable(map, x - 1, y - 1), aboveRight = Walkable(map, x + 1, y - 1);
        if (above || aboveLeft && aboveRight) return new TreePlacement(variant, 0, Low: true);
        return new TreePlacement(variant, aboveLeft ? 1 : aboveRight ? -1 : 0, Low: false);
    }

    private static bool TreeOrEdge(MapDef map, int x, int y)
        => x < 0 || y < 0 || x >= map.Width || y >= map.Height || map.KindAt(x, y) is TileKind.Tree or TileKind.Pine;

    /// <summary>Whether a tree tile is surrounded by trees (or the map's edge) on all four sides.</summary>
    public static bool InWood(MapDef map, int x, int y)
        => TreeOrEdge(map, x, y) && TreeOrEdge(map, x - 1, y) && TreeOrEdge(map, x + 1, y) && TreeOrEdge(map, x, y - 1) && TreeOrEdge(map, x, y + 1);

    /// <summary>
    /// Whether a tree tile is left empty: within a wood (trees above, below and on at least one side), trees stand on
    /// every other tile in staggered rows, as in the pack's own woods, and the crowns around close over the gap.
    /// </summary>
    public static bool Gap(MapDef map, int x, int y)
        => (x + y) % 2 == 1 && TreeOrEdge(map, x, y) && TreeOrEdge(map, x, y - 1) && TreeOrEdge(map, x, y + 1)
            && (TreeOrEdge(map, x - 1, y) || TreeOrEdge(map, x + 1, y));

    /// <summary>Whether someone can stand on a tile: inside the map, not solid, not a building or a sign.</summary>
    public static bool Walkable(MapDef map, int x, int y)
    {
        if (x < 0 || y < 0 || x >= map.Width || y >= map.Height) return false;
        var kind = map.KindAt(x, y);
        if (kind == TileKind.Void || Tiles.IsSolid(kind)) return false;
        foreach (var b in map.Buildings)
            if (x >= b.X && x < b.X + b.W && y >= b.Y && y < b.Y + b.H) return false;
        foreach (var s in map.Signs)
            if (s.X == x && s.Y == y) return false;
        return true;
    }
}

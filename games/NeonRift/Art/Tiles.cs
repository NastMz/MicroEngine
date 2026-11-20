using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.World;

namespace NeonRift.Art;

/// <summary>Collision ids used in stage layers.</summary>
internal static class TileId
{
    public const int Empty = 0, Solid = 1, OneWay = 2, Lava = 3;
}

/// <summary>
/// Chooses which 16×16 tile a cell shows from its neighbours: surface rows get the ledge/brick/deck top, the row
/// below gets a face, the rest fill. <c>Content/sprites.sheet</c> cuts the tiles out of each stage's tileset.
/// </summary>
internal static class Tiles
{
    public const int Size = 16;

    /// <summary>Picks the sprite for cell (x, y) of a stage layer, or null for empty cells.</summary>
    public static Sprite? For(Assets art, string theme, TileLayer layer, int x, int y)
    {
        int id = layer[x, y];
        if (id == TileId.Empty) return null;
        int hash = (x * 7349 + y * 1931) & 0x7fffffff;
        if (id == TileId.Lava) return art.Frames("tile_grotto_lava")[(layer[x, y - 1] == TileId.Lava ? 1 : 0)];
        bool solidAbove = layer.Get(x, y - 1) is TileId.Solid;
        bool solidLeft = layer.Get(x - 1, y, TileId.Solid) != TileId.Empty;
        bool solidRight = layer.Get(x + 1, y, TileId.Solid) != TileId.Empty;
        if (id == TileId.OneWay)
            return theme switch
            {
                "city" => art.Frames("tile_city_top")[Edge(solidLeft, solidRight, x)],
                "grotto" => art.Frames("tile_grotto_ledge")[(!solidLeft ? 0 : !solidRight ? 2 : 1)],
                _ => art.Frames("tile_lab_top")[x & 3],
            };
        if (!solidAbove)
            return theme switch
            {
                "city" => art.Frames("tile_city_top")[Edge(solidLeft, solidRight, x)],
                "grotto" => art.Frames("tile_grotto_top")[x & 3],
                _ => art.Frames("tile_lab_top")[x & 3],
            };
        bool surfaceAbove = layer.Get(x, y - 2) is not TileId.Solid;
        return theme switch
        {
            "city" => CityWall(art, layer, x, y, hash),
            "grotto" when surfaceAbove => art.Frames("tile_grotto_face")[x & 3],
            "grotto" => art.Frames("tile_grotto_fill")[(x & 1) + (y & 1) * 2],
            _ when surfaceAbove => art.Frames("tile_lab_face")[x & 3],
            _ => art.Frames("tile_lab_fill")[(x & 1) + (y & 1) * 2],
        };
    }

    /// <summary>The visual under a one-way deck in the lab (the deck's front face hangs below it).</summary>
    public static Sprite? Underside(Assets art, string theme, TileLayer layer, int x, int y)
        => theme == "lab" && layer.Get(x, y - 1) == TileId.OneWay && layer[x, y] == TileId.Empty ? art.Frames("tile_lab_face")[x & 3] : null;

    private static int Edge(bool left, bool right, int x) => !left ? 0 : !right ? 3 : 1 + (x & 1);

    /// <summary>Building walls: mostly dark panels with lit window columns every few cells.</summary>
    private static Sprite CityWall(Assets art, TileLayer layer, int x, int y, int hash)
    {
        int depth = 0;
        while (depth < 6 && layer.Get(x, y - depth - 1) == TileId.Solid) depth++;
        int column = ((x % 6) + 6) % 6;
        if (depth >= 1 && depth <= 4 && column is >= 1 and <= 3 && layer.Get(x - column, y) == TileId.Solid && layer.Get(x - column + 4, y) == TileId.Solid)
            return art.Frames("tile_city_window")[(depth - 1) * 3 + column - 1];
        return art.Frames("tile_city_fill")[hash % 4];
    }
}

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
/// Cuts 16×16 tiles out of each stage's tileset and chooses which one a cell shows from its
/// neighbours: surface rows get the ledge/brick/deck top, the row below gets a face, the rest fill.
/// </summary>
internal static class Tiles
{
    public const int Size = 16;

    private static readonly Dictionary<string, (string File, (int X, int Y)[] Cells)> Sets = new()
    {
        // Warped City rooftop: balcony ledge (L, M, M, R), dark wall, lit window column.
        ["city_top"] = ("stages/city/tileset.png", [(96, 176), (112, 176), (128, 176), (144, 176)]),
        ["city_fill"] = ("stages/city/tileset.png", [(64, 16), (64, 16), (64, 16), (16, 16)]),
        ["city_window"] = ("stages/city/tileset.png", [(48, 16), (64, 16), (80, 16), (48, 32), (64, 32), (80, 32), (48, 48), (64, 48), (80, 48), (48, 64), (64, 64), (80, 64)]),
        // Super Grotto Escape: brick crust, crust underside, mossy fill, small bricks, lava.
        ["grotto_top"] = ("stages/grotto/tileset.png", [(16, 64), (32, 64), (48, 64), (64, 64)]),
        ["grotto_face"] = ("stages/grotto/tileset.png", [(16, 80), (32, 80), (48, 80), (64, 80)]),
        ["grotto_fill"] = ("stages/grotto/tileset.png", [(16, 112), (32, 112), (16, 128), (32, 128)]),
        ["grotto_ledge"] = ("stages/grotto/tileset.png", [(64, 112), (80, 112), (96, 112)]),
        ["grotto_lava"] = ("stages/grotto/tileset.png", [(96, 64), (96, 80)]),
        // Warped City 2 deck and block pieces for the lab.
        ["lab_top"] = ("stages/lab/tileset.png", [(0, 16), (16, 16), (32, 16), (48, 16)]),
        ["lab_face"] = ("stages/lab/tileset.png", [(0, 32), (16, 32), (32, 32), (48, 32)]),
        ["lab_fill"] = ("stages/lab/tileset.png", [(504, 80), (520, 80), (504, 96), (520, 96)]),
    };

    public static void AddAll(SpriteAtlasBuilder b, ContentFolder content)
    {
        var cache = new Dictionary<string, PixelImage>();
        foreach (var (name, (file, cells)) in Sets)
        {
            if (!cache.TryGetValue(file, out var sheet)) cache[file] = sheet = content.Image(file);
            for (int i = 0; i < cells.Length; i++) b.Add($"tile_{name}{i}", sheet.Crop(cells[i].X, cells[i].Y, Size, Size));
        }
    }

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

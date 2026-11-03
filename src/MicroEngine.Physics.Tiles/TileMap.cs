namespace MicroEngine.World;

/// <summary>Collision semantics of a tile. Games may use the Custom bits for their own meaning.</summary>
[Flags]
public enum TileFlags
{
    /// <summary>No collision meaning: the tile is empty space.</summary>
    None = 0,
    /// <summary>Blocks movement from every side.</summary>
    Solid = 1,
    /// <summary>Blocks only bodies moving down onto its top edge (jump-through platform).</summary>
    OneWay = 2,
    /// <summary>Hurts on contact; does not block.</summary>
    Hazard = 4,
    /// <summary>Liquid; does not block.</summary>
    Water = 8,
    /// <summary>Free for the game's own meaning (ladders, ice, triggers...).</summary>
    Custom1 = 16,
    /// <summary>Free for the game's own meaning.</summary>
    Custom2 = 32,
    /// <summary>Free for the game's own meaning.</summary>
    Custom3 = 64,
    /// <summary>Free for the game's own meaning.</summary>
    Custom4 = 128,
}

/// <summary>A rectangular grid of integer tile ids (0 conventionally means empty).</summary>
public sealed class TileLayer
{
    private readonly int[] _tiles;
    /// <summary>Width in tiles.</summary>
    public int Width { get; }
    /// <summary>Height in tiles.</summary>
    public int Height { get; }

    /// <summary>Creates a layer filled with 0.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> or <paramref name="height"/> is not positive.</exception>
    public TileLayer(int width, int height)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        Width = width;
        Height = height;
        _tiles = new int[width * height];
    }

    /// <summary>Whether tile (<paramref name="x"/>, <paramref name="y"/>) is inside the layer.</summary>
    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    /// <summary>Out-of-bounds reads return <paramref name="outside"/> so edges can act as walls.</summary>
    public int Get(int x, int y, int outside = 0) => InBounds(x, y) ? _tiles[y * Width + x] : outside;
    /// <summary>The tile id at (<paramref name="x"/>, <paramref name="y"/>), in tiles. Reading outside returns 0; writing outside does nothing.</summary>
    public int this[int x, int y]
    {
        get => Get(x, y);
        set { if (InBounds(x, y)) _tiles[y * Width + x] = value; }
    }

    /// <summary>
    /// Builds a layer from text rows. Each character is mapped through <paramref name="legend"/>;
    /// characters not in the legend become 0. Rows may have different lengths.
    /// </summary>
    public static TileLayer Parse(IReadOnlyList<string> rows, IReadOnlyDictionary<char, int> legend)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(legend);
        var layer = new TileLayer(rows.Max(r => r.Length), rows.Count);
        for (int y = 0; y < rows.Count; y++)
        for (int x = 0; x < rows[y].Length; x++)
            if (legend.TryGetValue(rows[y][x], out var id)) layer[x, y] = id;
        return layer;
    }

    /// <summary>Finds every cell whose source character is <paramref name="marker"/> (for spawning entities).</summary>
    public static IEnumerable<(int X, int Y)> Find(IReadOnlyList<string> rows, char marker)
    {
        for (int y = 0; y < rows.Count; y++)
        for (int x = 0; x < rows[y].Length; x++)
            if (rows[y][x] == marker) yield return (x, y);
    }
}

/// <summary>Answers collision queries for <see cref="TileMotion"/>.</summary>
public interface ICollisionGrid
{
    /// <summary>Size of a square tile in pixels.</summary>
    int TileSize { get; }
    /// <summary>The flags of the tile at (<paramref name="tileX"/>, <paramref name="tileY"/>), in tiles; any coordinate, inside the map or not.</summary>
    TileFlags FlagsAt(int tileX, int tileY);
}

/// <summary>
/// A collision grid backed by a <see cref="TileLayer"/> and a per-id flag table.
/// Cells outside the map use <see cref="OutsideFlags"/> (solid by default, so levels are closed).
/// </summary>
public sealed class TileGrid : ICollisionGrid
{
    private readonly Dictionary<int, TileFlags> _flags;
    /// <summary>The tile ids the grid reads. Changing a tile in the layer changes collisions at once.</summary>
    public TileLayer Layer { get; }
    /// <summary>Size of a square tile in pixels.</summary>
    public int TileSize { get; }
    /// <summary>Flags of every cell outside the map when <see cref="OutsideRule"/> gives none (solid by default).</summary>
    public TileFlags OutsideFlags { get; set; } = TileFlags.Solid;
    /// <summary>Optional override for open edges, e.g. a platformer's bottom pit must not be solid.</summary>
    public Func<int, int, TileFlags?>? OutsideRule { get; set; }

    /// <summary>Creates a grid; ids missing from <paramref name="flags"/> have no flags (empty).</summary>
    /// <param name="layer">The tile ids.</param>
    /// <param name="tileSize">Tile size in pixels; must be positive.</param>
    /// <param name="flags">Flags per tile id; copied.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="tileSize"/> is not positive.</exception>
    public TileGrid(TileLayer layer, int tileSize, IReadOnlyDictionary<int, TileFlags> flags)
    {
        Layer = layer ?? throw new ArgumentNullException(nameof(layer));
        if (tileSize <= 0) throw new ArgumentOutOfRangeException(nameof(tileSize));
        TileSize = tileSize;
        _flags = new Dictionary<int, TileFlags>(flags);
    }

    /// <summary>Width of the map in pixels.</summary>
    public int PixelWidth => Layer.Width * TileSize;
    /// <summary>Height of the map in pixels.</summary>
    public int PixelHeight => Layer.Height * TileSize;

    /// <summary>The flags of the tile at (<paramref name="tileX"/>, <paramref name="tileY"/>); outside the map, <see cref="OutsideRule"/> or <see cref="OutsideFlags"/>.</summary>
    public TileFlags FlagsAt(int tileX, int tileY)
    {
        if (!Layer.InBounds(tileX, tileY)) return OutsideRule?.Invoke(tileX, tileY) ?? OutsideFlags;
        return _flags.TryGetValue(Layer[tileX, tileY], out var f) ? f : TileFlags.None;
    }

    /// <summary>Sets the flags of every tile with id <paramref name="id"/>.</summary>
    public void SetFlags(int id, TileFlags flags) => _flags[id] = flags;
}

/// <summary>Helpers for picking tile sprites from their neighbours.</summary>
public static class Autotile
{
    /// <summary>4-bit mask: 1 = up, 2 = right, 4 = down, 8 = left neighbour matches.</summary>
    public static int Mask4(TileLayer layer, int x, int y, Func<int, bool> same, bool outsideMatches = true)
    {
        bool S(int tx, int ty) => layer.InBounds(tx, ty) ? same(layer[tx, ty]) : outsideMatches;
        return (S(x, y - 1) ? 1 : 0) | (S(x + 1, y) ? 2 : 0) | (S(x, y + 1) ? 4 : 0) | (S(x - 1, y) ? 8 : 0);
    }
}

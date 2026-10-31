using MicroEngine.Graphics;

namespace MicroEngine.World;

/// <summary>Collision semantics of a tile. Games may use the Custom bits for their own meaning.</summary>
[Flags]
public enum TileFlags
{
    None = 0,
    /// <summary>Blocks movement from every side.</summary>
    Solid = 1,
    /// <summary>Blocks only bodies moving down onto its top edge (jump-through platform).</summary>
    OneWay = 2,
    /// <summary>Hurts on contact; does not block.</summary>
    Hazard = 4,
    /// <summary>Liquid; does not block.</summary>
    Water = 8,
    Custom1 = 16,
    Custom2 = 32,
    Custom3 = 64,
    Custom4 = 128,
}

/// <summary>A rectangular grid of integer tile ids (0 conventionally means empty).</summary>
public sealed class TileLayer
{
    private readonly int[] _tiles;
    public int Width { get; }
    public int Height { get; }

    public TileLayer(int width, int height)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        Width = width;
        Height = height;
        _tiles = new int[width * height];
    }

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    /// <summary>Out-of-bounds reads return <paramref name="outside"/> so edges can act as walls.</summary>
    public int Get(int x, int y, int outside = 0) => InBounds(x, y) ? _tiles[y * Width + x] : outside;
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
    int TileSize { get; }
    TileFlags FlagsAt(int tileX, int tileY);
}

/// <summary>
/// A collision grid backed by a <see cref="TileLayer"/> and a per-id flag table.
/// Cells outside the map use <see cref="OutsideFlags"/> (solid by default, so levels are closed).
/// </summary>
public sealed class TileGrid : ICollisionGrid
{
    private readonly Dictionary<int, TileFlags> _flags;
    public TileLayer Layer { get; }
    public int TileSize { get; }
    public TileFlags OutsideFlags { get; set; } = TileFlags.Solid;
    /// <summary>Optional override for open edges, e.g. a platformer's bottom pit must not be solid.</summary>
    public Func<int, int, TileFlags?>? OutsideRule { get; set; }

    public TileGrid(TileLayer layer, int tileSize, IReadOnlyDictionary<int, TileFlags> flags)
    {
        Layer = layer ?? throw new ArgumentNullException(nameof(layer));
        if (tileSize <= 0) throw new ArgumentOutOfRangeException(nameof(tileSize));
        TileSize = tileSize;
        _flags = new Dictionary<int, TileFlags>(flags);
    }

    public int PixelWidth => Layer.Width * TileSize;
    public int PixelHeight => Layer.Height * TileSize;

    public TileFlags FlagsAt(int tileX, int tileY)
    {
        if (!Layer.InBounds(tileX, tileY)) return OutsideRule?.Invoke(tileX, tileY) ?? OutsideFlags;
        return _flags.TryGetValue(Layer[tileX, tileY], out var f) ? f : TileFlags.None;
    }

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

/// <summary>Draws the visible part of a layer, asking the game which sprite each tile uses.</summary>
public static class TileRenderer
{
    public static void Draw(IRenderer renderer, TileLayer layer, int tileSize, Camera2D? camera, Func<int, int, int, Sprite?> spriteFor)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(layer);
        var view = camera?.View.Inflate(tileSize) ?? new RectF(0, 0, renderer.Width, renderer.Height);
        int x0 = Math.Max(0, (int)MathF.Floor(view.X / tileSize)), y0 = Math.Max(0, (int)MathF.Floor(view.Y / tileSize));
        int x1 = Math.Min(layer.Width - 1, (int)MathF.Ceiling(view.Right / tileSize)), y1 = Math.Min(layer.Height - 1, (int)MathF.Ceiling(view.Bottom / tileSize));
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            int id = layer[x, y];
            if (id == 0) continue;
            if (spriteFor(id, x, y) is { } sprite)
                renderer.Draw(sprite.Texture, sprite.Source, new System.Numerics.Vector2(x * tileSize, y * tileSize) - sprite.Origin, Color.White);
        }
    }
}

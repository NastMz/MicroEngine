using NeonRift.Art;
using MicroEngine.World;

namespace NeonRift.Content;

public enum SpawnKind { Trooper, TrooperGate, Gunner, Drones, Turret, Egg, Skiffs, Carrier, Checkpoint }

public enum BossKind { Gunship, Interceptor, Dreadnought }

/// <summary>An entity placed in a stage. X/Y are the world pixel position of its feet (or centre for flyers).</summary>
public sealed record Spawn(SpawnKind Kind, float X, float Y, int Count = 1, string Param = "");

/// <summary>A decoration drawn behind the action (palms, tanks).</summary>
public sealed record Prop(string Sprite, float X, float Y);

/// <summary>A complete stage: collision layer, spawns, decoration and presentation.</summary>
public sealed record StageDef(
    string Name,
    string Subtitle,
    string Theme,
    string Music,
    TileLayer Layer,
    IReadOnlyList<Spawn> Spawns,
    IReadOnlyList<Prop> Props,
    BossKind Boss,
    float ArenaX)
{
    public int PixelWidth => Layer.Width * Tiles.Size;
    public int PixelHeight => Layer.Height * Tiles.Size;
    public IEnumerable<float> Checkpoints => Spawns.Where(s => s.Kind == SpawnKind.Checkpoint).Select(s => s.X);
}

/// <summary>
/// Authors stages left to right with a cursor, in tiles. Heights are counted from the bottom row,
/// so <c>Floor(10, 3)</c> is ten columns of ground three tiles thick. Spawns are placed relative to
/// the cursor and stand on the given height.
/// </summary>
public sealed class StageBuilder(int rows = StageBuilder.Rows)
{
    public const int Rows = 17;
    /// <summary>Every stage ends in a locked arena exactly one screen (30 columns) wide.</summary>
    public const int ArenaColumns = 30;
    private readonly List<int[]> _columns = [];
    private readonly List<Spawn> _spawns = [];
    private readonly List<Prop> _props = [];

    public int Cursor => _columns.Count;
    /// <summary>World Y (pixels) of the surface of ground <paramref name="height"/> tiles thick.</summary>
    public static float Surface(int height) => (Rows - height) * Tiles.Size;

    public StageBuilder Floor(int length, int height)
    {
        for (int i = 0; i < length; i++)
        {
            var c = new int[rows];
            for (int y = rows - height; y < rows; y++) c[y] = TileId.Solid;
            _columns.Add(c);
        }
        return this;
    }

    public StageBuilder Pit(int length)
    {
        for (int i = 0; i < length; i++) _columns.Add(new int[rows]);
        return this;
    }

    /// <summary>A lava pool: hazard surface <paramref name="depth"/> tiles deep over a solid bed.</summary>
    public StageBuilder Lava(int length, int height, int depth = 1)
    {
        for (int i = 0; i < length; i++)
        {
            var c = new int[rows];
            for (int y = rows - height; y < rows; y++) c[y] = y < rows - height + depth ? TileId.Lava : TileId.Solid;
            _columns.Add(c);
        }
        return this;
    }

    /// <summary>A jump-through platform at an absolute column, its surface <paramref name="height"/> tiles up.</summary>
    public StageBuilder PlatformAt(int x, int height, int length) => Paint(x, height, length, 1, TileId.OneWay);

    /// <summary>A solid block (crate, pillar, wall) at an absolute column, standing on <paramref name="height"/>.</summary>
    public StageBuilder BlockAt(int x, int height, int width, int tall) => Paint(x, height + tall, width, tall, TileId.Solid);

    private StageBuilder Paint(int x, int top, int width, int tall, int id)
    {
        for (int i = x; i < x + width; i++)
        {
            while (i >= _columns.Count) _columns.Add(new int[rows]);
            for (int y = rows - top; y < rows - top + tall; y++) if (y >= 0 && y < rows) _columns[i][y] = id;
        }
        return this;
    }

    /// <summary>Places a spawn at absolute column <paramref name="x"/> standing on ground <paramref name="height"/> tiles thick.</summary>
    public StageBuilder At(int x, int height, SpawnKind kind, int count = 1, string param = "")
    {
        _spawns.Add(new Spawn(kind, x * Tiles.Size + Tiles.Size / 2f, Surface(height), count, param));
        return this;
    }

    public StageBuilder Decor(int x, int height, string sprite)
    {
        _props.Add(new Prop(sprite, x * Tiles.Size + Tiles.Size / 2f, Surface(height)));
        return this;
    }

    public StageDef Build(string name, string subtitle, string theme, string music, BossKind boss)
    {
        var layer = new TileLayer(_columns.Count, rows);
        for (int x = 0; x < _columns.Count; x++)
        for (int y = 0; y < rows; y++)
            layer[x, y] = _columns[x][y];
        return new StageDef(name, subtitle, theme, music, layer, _spawns.OrderBy(s => s.X).ToList(), _props, boss, (_columns.Count - ArenaColumns) * Tiles.Size);
    }
}

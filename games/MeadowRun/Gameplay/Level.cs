using System.Numerics;
using MeadowRun.Content;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.World;

namespace MeadowRun.Gameplay;

/// <summary>A spawn marker found in the level text.</summary>
public readonly record struct Spawn(char Kind, int X, int Y)
{
    public Vector2 TileTopLeft => new(X * Level.TileSize, Y * Level.TileSize);
}

/// <summary>A parsed, playable level: tiles, collision grid, spawn list and decoration placement.</summary>
public sealed class Level
{
    public const int TileSize = 16;

    public LevelDef Def { get; }
    public TileLayer Tiles { get; }
    public TileGrid Grid { get; }
    public IReadOnlyList<Spawn> Spawns { get; }
    public Spawn Start { get; }
    public Spawn? Goal { get; }
    public int Width => Tiles.Width;
    public int Height => Tiles.Height;
    public int PixelWidth => Width * TileSize;
    public int PixelHeight => Height * TileSize;
    public Biome Biome => Def.Biome;
    /// <summary>Surface decorations: sprite index into the biome's deco list and the tile above which it sits.</summary>
    public IReadOnlyList<(int Deco, int X, int Y)> Decorations { get; }

    public Level(LevelDef def)
    {
        Def = def ?? throw new ArgumentNullException(nameof(def));
        int width = def.Rows.Max(r => r.Length);
        var rows = def.Rows.Select(r => r.PadRight(width)).ToArray();
        Tiles = TileLayer.Parse(rows, Legend.Tiles);
        Grid = new TileGrid(Tiles, TileSize, FlagTable)
        {
            // Sides are walls; the sky above and the pits below are open.
            OutsideRule = (x, y) => y >= Tiles.Height || y < 0 ? TileFlags.None : null,
        };
        var spawns = new List<Spawn>();
        for (int y = 0; y < rows.Length; y++)
        for (int x = 0; x < rows[y].Length; x++)
            if (Legend.Entities.Contains(rows[y][x])) spawns.Add(new Spawn(rows[y][x], x, y));
        Spawns = spawns;
        var starts = spawns.Where(s => s.Kind == 'S').ToList();
        if (starts.Count != 1) throw new FormatException($"Level '{def.Name}' needs exactly one start (S), found {starts.Count}.");
        Start = starts[0];
        Goal = spawns.Where(s => s.Kind == 'G').Select(s => (Spawn?)s).FirstOrDefault();
        Decorations = PlaceDecorations();
    }

    public static readonly IReadOnlyDictionary<int, TileFlags> FlagTable = new Dictionary<int, TileFlags>
    {
        [Tile.Ground] = TileFlags.Solid,
        [Tile.Brick] = TileFlags.Solid,
        [Tile.CoinBlock] = TileFlags.Solid,
        [Tile.PowerBlock] = TileFlags.Solid,
        [Tile.LifeBlock] = TileFlags.Solid,
        [Tile.MultiCoinBrick] = TileFlags.Solid,
        [Tile.UsedBlock] = TileFlags.Solid,
        [Tile.HardBlock] = TileFlags.Solid,
        [Tile.OneWay] = TileFlags.OneWay,
        [Tile.Spikes] = TileFlags.Hazard,
        [Tile.Lava] = TileFlags.Hazard | TileFlags.Custom1,
    };

    public bool IsSolid(int x, int y) => (Grid.FlagsAt(x, y) & TileFlags.Solid) != 0;

    /// <summary>Changes a tile at runtime (bumped blocks, broken bricks, revealed hidden blocks).</summary>
    public void SetTile(int x, int y, int id) => Tiles[x, y] = id;

    /// <summary>Where Pip stands at the start marker: feet on the bottom of that tile.</summary>
    public Vector2 StartPosition(Spawn spawn) => new(spawn.X * TileSize + 2, spawn.Y * TileSize + TileSize - Player.Height);

    public RectF Bounds => new(0, 0, PixelWidth, PixelHeight);

    private List<(int, int, int)> PlaceDecorations()
    {
        var list = new List<(int, int, int)>();
        int kinds = Art.DecoArt.For(Biome).Length;
        for (int y = 1; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            if (Tiles[x, y] != Tile.Ground || Tiles[x, y - 1] != Tile.Empty) continue;
            if (Spawns.Any(s => s.X == x && s.Y == y - 1)) continue;
            float h = MicroEngine.Graphics.Art.Hash(x, y, Def.Number * 13);
            if (h < 0.45f) list.Add(((int)(MicroEngine.Graphics.Art.Hash(y, x, 77) * kinds) % kinds, x, y - 1));
        }
        return list;
    }
}

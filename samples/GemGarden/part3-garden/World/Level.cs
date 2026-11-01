using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.World;

namespace GemGarden.World;

/// <summary>The garden: a tile map written as text, its collision grid and where things start.</summary>
public sealed class Level
{
    #region map
    public const int TileSize = 16;
    private const int Grass = 1, Hedge = 2;

    // '#' hedge, '.' grass, 'P' the hero's start, '*' a gem, 's' a slime. 40 x 20 tiles: 640 x 320 pixels.
    private static readonly string[] Map =
    [
        "########################################",
        "#P....*........#...........*...........#",
        "#..........*...#.....s.........*.......#",
        "#...####.......#..........######.......#",
        "#...#..*.......#..*.......#....#...*...#",
        "#...#...............*.....#..*.#.......#",
        "#...####...s..........s...#....#.......#",
        "#.........................##..##...s...#",
        "#..*.......######..............*.......#",
        "#..........#....#.....*................#",
        "#####..*...#.*..#..........#######.....#",
        "#..........#....#....s.....#.....#..*..#",
        "#...s......##.###..........#..*..#.....#",
        "#..................*.......#.....#.....#",
        "#..*.......*...............###.###.....#",
        "#.......................s.........*....#",
        "#...######.....*.....#######...........#",
        "#...#....#...........#.....#.....*.....#",
        "#.*......#.....s.....#..*..#...........#",
        "########################################",
    ];
    #endregion

    #region build
    private readonly SpriteAtlas _sprites;

    public Level(SpriteAtlas sprites)
    {
        _sprites = sprites;
        Layer = TileLayer.Parse(Map, new Dictionary<char, int>
        {
            ['#'] = Hedge, ['.'] = Grass, ['P'] = Grass, ['*'] = Grass, ['s'] = Grass,
        });
        Grid = new TileGrid(Layer, TileSize, new Dictionary<int, TileFlags> { [Hedge] = TileFlags.Solid });
        PlayerStart = CellCenter(TileLayer.Find(Map, 'P').Single());
        GemSpots = [.. TileLayer.Find(Map, '*').Select(CellCenter)];
        SlimeStarts = [.. TileLayer.Find(Map, 's').Select(CellCenter)];
    }

    public TileLayer Layer { get; }
    /// <summary>What blocks movement: hedges are solid, and so is everything outside the map.</summary>
    public TileGrid Grid { get; }
    /// <summary>The whole garden in world pixels, for the camera's bounds.</summary>
    public RectF Area => new(0, 0, Grid.PixelWidth, Grid.PixelHeight);
    public Vector2 PlayerStart { get; }
    public IReadOnlyList<Vector2> GemSpots { get; }
    public IReadOnlyList<Vector2> SlimeStarts { get; }

    /// <summary>World position of the centre of a map cell.</summary>
    private static Vector2 CellCenter((int X, int Y) cell) => new((cell.X + 0.5f) * TileSize, (cell.Y + 0.5f) * TileSize);
    #endregion

    #region draw
    /// <summary>Draws the tiles the camera sees. Call it between PushCamera and PopCamera.</summary>
    public void Draw(IRenderer r, Camera2D camera) =>
        TileRenderer.Draw(r, Layer, TileSize, camera, (id, _, _) => id == Hedge ? _sprites["hedge"] : _sprites["grass"]);
    #endregion
}

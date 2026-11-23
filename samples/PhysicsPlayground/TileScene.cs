using System.Numerics;
using System.Runtime.InteropServices;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Physics;
using MicroEngine.Physics.Box2D;
using MicroEngine.Scenes;
using MicroEngine.World;

namespace PhysicsPlayground;

/// <summary>
/// A tile level in a Box2D world: solid tiles, slopes and one-way platforms from one grid, with balls and crates that
/// roll and slide over them, and an explosion made with Box2D's own function, which the physics contract does not have.
/// </summary>
public sealed class TileScene : Scene
{
    #region tiles
    private const int Tile = 16, Solid = 1, Ledge = 2, Up = 3, Down = 4;

    // '#' solid, '=' one-way platform, '/' a slope that rises to the right, '\' one that rises to the left.
    private static readonly string[] Rows =
    [
        "####################",
        "#..................#",
        "#..................#",
        "#....====..........#",
        "#..................#",
        "#...........\\......#",
        "#...........#\\.....#",
        "#......../####\\....#",
        "#......./######\\...#",
        "#....../########\\..#",
        "####################",
    ];

    private readonly TileGrid _grid = new(
        TileLayer.Parse(Rows, new Dictionary<char, int> { ['#'] = Solid, ['='] = Ledge, ['/'] = Up, ['\\'] = Down }),
        Tile,
        new Dictionary<int, TileFlags>
        {
            [Solid] = TileFlags.Solid, [Ledge] = TileFlags.OneWay, [Up] = TileFlags.SlopeRight, [Down] = TileFlags.SlopeLeft,
        });
    #endregion

    private readonly List<BodyId> _balls = [];
    private Box2DPhysicsWorld2D _world = null!;
    private float _flash;

    /// <summary>The balls and crates.</summary>
    public IReadOnlyList<BodyId> Balls => _balls;
    /// <summary>The tile shape.</summary>
    public ShapeId Level { get; private set; }
    /// <summary>How many explosions went off.</summary>
    public int Explosions { get; private set; }

    public override void Enter()
    {
        #region world
        // This scene makes its own world instead of the game's: one whose Box2D ids the game may read.
        _world = new Box2DPhysicsWorld2D(new Box2DWorldOptions { UnitsPerMeter = 16, YDown = true, NativeAccess = true });
        Systems.Add(new ScenePhysics(_world) { SyncTransforms = false });

        // The whole level is one shape of one static body: the outline of its tiles.
        var level = _world.CreateBody(default, BodyDescription.Static());
        Level = _world.AddTiles(level, _grid, _grid.Layer.Width, _grid.Layer.Height);
        #endregion

        for (int i = 0; i < 6; i++)
        {
            var body = _world.CreateBody(default, BodyDescription.Dynamic(new Vector2(60 + i * 36, 28)));
            if (i % 2 == 0) _world.AddShape(body, ShapeDescription.Circle(5) with { Friction = 0.6f });
            else _world.AddShape(body, ShapeDescription.Box(new Vector2(10, 10)) with { Friction = 0.3f });
            _balls.Add(body);
        }
    }

    #region native
    // Box2D's own explosion: no member of the physics contract does this. The game declares the function it calls.
    [StructLayout(LayoutKind.Sequential)]
    private struct ExplosionDef
    {
        public ulong MaskBits;
        public Vector2 Position;                     // metres, y up: Box2D's units, not the game's
        public float Radius, Falloff, ImpulsePerLength;
    }

    [DllImport(Box2DNative.Library)]
    private static extern void b2World_Explode(B2WorldId world, in ExplosionDef explosion);

    private void Explode(Vector2 at)
    {
        var explosion = new ExplosionDef
        {
            MaskBits = ulong.MaxValue,
            Position = new Vector2(at.X / 16, -at.Y / 16),
            Radius = 3, Falloff = 2, ImpulsePerLength = 4,
        };
        b2World_Explode(_world.NativeIds.World, explosion);
    }
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        if (Input.Pressed("confirm"))
        {
            Explode(new Vector2(160, 150));
            Explosions++;
            _flash = 0.2f;
        }
        _flash = MathF.Max(0, _flash - dt);
    }

    /// <summary>The scene's own state (the world hashes itself).</summary>
    public override void HashState(StateHasher hash)
    {
        hash.Add(Explosions);
        hash.Add(_flash);
    }

    public override void Draw(IRenderer r)
    {
        for (int y = 0; y < _grid.Layer.Height; y++)
        for (int x = 0; x < _grid.Layer.Width; x++)
        {
            var cell = new RectF(x * Tile, y * Tile, Tile, Tile);
            var stone = Color.Hex("566c86");
            switch (_grid.Layer[x, y])
            {
                case Solid: r.FillRect(cell, stone); break;
                case Ledge: r.FillRect(cell with { Height = 4 }, Color.Hex("a7f070")); break;
                case Up: r.FillTriangle(new Vector2(cell.Left, cell.Bottom), new Vector2(cell.Right, cell.Bottom), new Vector2(cell.Right, cell.Top), stone); break;
                case Down: r.FillTriangle(new Vector2(cell.Left, cell.Top), new Vector2(cell.Left, cell.Bottom), new Vector2(cell.Right, cell.Bottom), stone); break;
            }
        }
        Span<ShapeId> shapes = stackalloc ShapeId[1];
        foreach (var body in _balls)
        {
            var at = _world.PositionOf(body);
            var turn = Matrix3x2.CreateRotation(_world.RotationOf(body));
            _world.ShapesOf(body, shapes);
            var d = _world.DescriptionOf(shapes[0]);
            if (d.Kind == ShapeKind.Circle)
            {
                r.FillCircle(at, d.Radius, Color.Hex("ffcd75"));
                r.DrawLine(at, at + Vector2.Transform(new Vector2(d.Radius, 0), turn), 1, Color.Hex("1a1c2c"));
            }
            else
            {
                Vector2 P(float x, float y) => at + Vector2.Transform(new Vector2(x, y), turn);
                r.FillTriangle(P(-5, -5), P(5, -5), P(5, 5), Color.Hex("41a6f6"));
                r.FillTriangle(P(-5, -5), P(5, 5), P(-5, 5), Color.Hex("41a6f6"));
            }
        }
        if (_flash > 0) r.FillCircle(new Vector2(160, 150), 48 * (1 - _flash * 4), Color.Hex("ef7d57"));
        Font.Draw(r, "EXPLOSION: Z", new Vector2(20, 20), Color.White);
    }
}

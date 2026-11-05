using System.Numerics;
using MicroEngine;
using MicroEngine.Physics;
using MicroEngine.Scenes;

namespace PhysicsSubstitution;

/// <summary>
/// The gameplay code of the sample: a platformer hero over a tile level, written against the common contract of
/// <see cref="IPhysicsWorld2D"/> only (static and kinematic bodies, boxes, layers, one-way platforms, collide-and-slide,
/// sensors). It never asks which backend it runs on: that is decided where the world is made.
/// </summary>
public sealed class Platformer
{
    /// <summary>
    /// The level, 16-pixel tiles: '#' solid, '=' one-way platform, 'C' a coin, 'G' the goal (both sensors), 'P' where
    /// the hero starts.
    /// </summary>
    public static readonly string[] Map =
    [
        "########################################",
        "#......................................#",
        "#......................................#",
        "#..........C.................C.........#",
        "#........=====..........=========......#",
        "#......................................#",
        "#.................####.................#",
        "#..P..............#..#...........C...G.#",
        "########################################",
    ];

    public const int Tile = 16;
    public const float Gravity = 900, MaxFall = 600, RunSpeed = 90, JumpSpeed = 400;
    public const ulong LevelLayer = 1, HeroLayer = 2, PickupLayer = 4;
    public static readonly Vector2 HeroSize = new(12, 14);
    public static readonly EntityId LevelOwner = new(1, 1), HeroOwner = new(2, 1), CoinOwner = new(3, 1), GoalOwner = new(4, 1);

    private readonly IPhysicsWorld2D _world;
    private Vector2 _velocity;

    /// <summary>Builds the level and the hero in <paramref name="world"/>.</summary>
    public Platformer(IPhysicsWorld2D world)
    {
        _world = world;
        var level = world.CreateBody(LevelOwner, BodyDescription.Static());
        foreach (var (rect, oneWay) in LevelBoxes())
            Level.Add(world.AddShape(level, ShapeDescription.Box(rect.Size, rect.Center).InLayers(LevelLayer, HeroLayer) with { OneWay = oneWay }));
        for (int y = 0; y < Map.Length; y++)
        for (int x = 0; x < Map[y].Length; x++)
        {
            var centre = new Vector2((x + 0.5f) * Tile, (y + 0.5f) * Tile);
            switch (Map[y][x])
            {
                case 'P':
                    Start = centre + new Vector2(0, Tile / 2f - HeroSize.Y / 2);
                    break;
                case 'C' or 'G':
                    bool goal = Map[y][x] == 'G';
                    var body = world.CreateBody(goal ? GoalOwner : CoinOwner, BodyDescription.Static(centre));
                    var sensor = world.AddShape(body, ShapeDescription.Sensor(new Vector2(Tile, Tile)).InLayers(PickupLayer, HeroLayer));
                    if (goal) Goal = sensor;
                    else Coins.Add(sensor);
                    break;
            }
        }
        Hero = world.CreateBody(HeroOwner, BodyDescription.Kinematic(Start));
        HeroShape = world.AddShape(Hero, ShapeDescription.Box(HeroSize).InLayers(HeroLayer, LevelLayer | PickupLayer));
    }

    /// <summary>
    /// The level's cells as boxes: each row's runs of solid or one-way cells merged, and solid runs of the same span on
    /// consecutive rows stacked into one box (one-way runs are never stacked: each is a platform).
    /// </summary>
    public static List<(RectF Rect, bool OneWay)> LevelBoxes()
    {
        var boxes = new List<(RectF, bool)>();
        var open = new Dictionary<(int, int), int>();
        for (int y = 0; y < Map.Length; y++)
        {
            var next = new Dictionary<(int, int), int>();
            for (int x = 0; x < Map[y].Length;)
            {
                char c = Map[y][x];
                if (c is not ('#' or '=')) { x++; continue; }
                int start = x;
                while (x < Map[y].Length && Map[y][x] == c) x++;
                var rect = new RectF(start * Tile, y * Tile, (x - start) * Tile, Tile);
                if (c == '#' && open.TryGetValue((start, x), out int at))
                {
                    var r = boxes[at].Item1;
                    boxes[at] = (r with { Height = r.Height + Tile }, false);
                    next[(start, x)] = at;
                    continue;
                }
                boxes.Add((rect, c == '='));
                if (c == '#') next[(start, x)] = boxes.Count - 1;
            }
            open = next;
        }
        return boxes;
    }

    public Vector2 Start { get; }
    public BodyId Hero { get; }
    public ShapeId HeroShape { get; }
    public List<ShapeId> Level { get; } = [];
    public List<ShapeId> Coins { get; } = [];
    public ShapeId Goal { get; }
    public MoveResult Last { get; private set; }
    public List<(ShapeId Coin, long Step)> Collected { get; } = [];
    public long GoalStep { get; private set; } = -1;
    public Vector2 Position => _world.PositionOf(Hero);

    /// <summary>One fixed step of play: run (-1, 0 or 1), jump when on ground, drop through one-way platforms.</summary>
    public void Step(float dt, int run, bool jump = false, bool drop = false)
    {
        var up = _world.Up;
        float rise = MathF.Max(Vector2.Dot(_velocity, up) - Gravity * dt, -MaxFall);
        if (jump && Last.OnGround) rise = JumpSpeed;
        _velocity = new Vector2(run * RunSpeed, 0) + up * rise;
        Last = _world.Move(Hero, _velocity * dt, new QueryFilter(HeroLayer, LevelLayer) { PassOneWay = drop });
        float vertical = Vector2.Dot(_velocity, up);
        if ((Last.OnGround && vertical < 0) || (Last.HitCeiling && vertical > 0)) _velocity = new Vector2(_velocity.X, 0);
        _world.Step(dt);
        foreach (var e in _world.SensorEvents)
        {
            if (e.Kind != SensorEventKind.Enter || e.Visitor != HeroShape) continue;
            if (e.Sensor == Goal) GoalStep = _world.StepCount;
            else if (!Collected.Exists(c => c.Coin == e.Sensor)) Collected.Add((e.Sensor, _world.StepCount));
        }
    }

    /// <summary>What the hero did in each part of <see cref="Play"/>: where it ended and its last move.</summary>
    public List<(string Part, Vector2 Position, MoveResult Move)> Parts { get; } = [];

    /// <summary>
    /// The sample's run: land, walk right into the pillar, back off under the one-way platform, jump up through it and
    /// land on it, drop back down through it, jump over the pillar and walk to the goal. Returns the hero's position
    /// after every step.
    /// </summary>
    public List<Vector2> Play()
    {
        const float dt = 1 / 60f;
        var trace = new List<Vector2>();
        void Do(string part, int steps, int run, bool jump = false, bool drop = false)
        {
            for (int i = 0; i < steps; i++)
            {
                Step(dt, run, jump && i == 0, drop);
                trace.Add(Position);
            }
            Parts.Add((part, Position, Last));
        }
        Do("land", 30, 0);
        Do("walk into the pillar", 170, 1);
        Do("back off under the platform", 60, -1);
        Do("jump onto the platform", 50, 0, jump: true);
        Do("walk to its coin", 20, -1);
        Do("drop through it", 40, 0, drop: true);
        Do("walk back", 50, 1);
        Do("jump over the pillar", 60, 1, jump: true);
        Do("walk to the goal", 260, 1);
        return trace;
    }
}

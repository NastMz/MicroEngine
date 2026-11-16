using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Physics;
using MicroEngine.Physics.Tiles;
using MicroEngine.Scenes;

namespace StressBench;

// The consumer scenarios of WORLD-1 (docs/evaluations/world-1.md): what a game with many moving objects or a world
// much larger than its view does every step, written each way a game can write it, so the ways can be compared.

/// <summary>How a scenario finds the objects near a point.</summary>
internal enum Lookup
{
    /// <summary>A walk over every object (<c>All&lt;T&gt;</c> and <c>Local</c>).</summary>
    Brute,
    /// <summary>A body per object in the tile physics world, asked with <c>Overlap</c>.</summary>
    Physics,
    /// <summary>A place per object in a <c>SpatialIndex2D</c>, asked with <c>QueryCircle</c>.</summary>
    Index,
}

internal sealed class Agent
{
    public EntityId Id;
    public Vector2 Velocity;
    public BodyId Body;
    public SpatialHandle Place;
    public int Near;
}

/// <summary>
/// Neighbours in a swarm: the load's agents wander through a square world at a constant density (one per 24×24
/// pixels, so the world grows with the load) and every step each one counts the others within 48 pixels of it (about
/// twelve). The strategy is how it finds them; all strategies count the same pairs.
/// </summary>
internal sealed class NeighboursScene(int load, Lookup lookup) : StressScene(load, "move", "sync", "query")
{
    public const float Radius = 48, Spacing = 24;
    private TilePhysicsWorld2D? _physics;
    private SpatialIndex2D<Agent>? _index;
    private readonly Agent[] _found = new Agent[512];
    private float _side;
    private long _pairs;

    protected override void Prepare()
    {
        var world = Objects;
        _side = MathF.Sqrt(Load) * Spacing;
        if (lookup == Lookup.Physics) _physics = new TilePhysicsWorld2D();
        if (lookup == Lookup.Index) _index = new SpatialIndex2D<Agent>(Radius);
        for (int i = 0; i < Load; i++)
        {
            var id = world.Create();
            var at = new Vector2(Random.NextSingle() * _side, Random.NextSingle() * _side);
            world.Local(id).Position2D = at;
            var agent = world.Attach(id, new Agent { Id = id, Velocity = new Vector2(Random.Next(-60, 60), Random.Next(-60, 60)) });
            if (_physics is { } physics)
            {
                agent.Body = physics.CreateBody(id, BodyDescription.Kinematic(at));
                physics.AddShape(agent.Body, ShapeDescription.Box(new Vector2(8, 8)));
            }
            if (_index is { } index) agent.Place = index.Add(agent, at);
        }
    }

    protected override void Step(float dt)
    {
        var world = Objects;
        float side = _side;
        long t = Now;
        foreach (var a in world.All<Agent>())
        {
            ref var local = ref world.Local(a.Id);
            var p = local.Position2D + a.Velocity * dt;
            if (p.X < 0 || p.X > side) a.Velocity.X = -a.Velocity.X;
            if (p.Y < 0 || p.Y > side) a.Velocity.Y = -a.Velocity.Y;
            local.Position2D = Vector2.Clamp(p, Vector2.Zero, new Vector2(side, side));
        }
        t = Lap(0, t);
        if (_physics is { } physics)
            foreach (var a in world.All<Agent>()) physics.SetPosition(a.Body, world.Local(a.Id).Position2D);
        if (_index is { } index)
            foreach (var a in world.All<Agent>()) index.Move(a.Place, world.Local(a.Id).Position2D);
        t = Lap(1, t);
        long pairs = lookup switch
        {
            Lookup.Brute => CountBrute(world),
            Lookup.Physics => CountPhysics(world, _physics!),
            _ => CountIndex(world, _index!, _found),
        };
        _pairs += pairs;
        Lap(2, t);
    }

    private static long CountBrute(IWorld world)
    {
        long pairs = 0;
        foreach (var a in world.All<Agent>())
        {
            var at = world.Local(a.Id).Position2D;
            int near = 0;
            foreach (var b in world.All<Agent>())
                if (b != a && Vector2.DistanceSquared(world.Local(b.Id).Position2D, at) <= Radius * Radius) near++;
            a.Near = near;
            pairs += near;
        }
        return pairs;
    }

    private static long CountPhysics(IWorld world, TilePhysicsWorld2D physics)
    {
        Span<ShapeId> found = stackalloc ShapeId[512];
        long pairs = 0;
        foreach (var a in world.All<Agent>())
        {
            var at = world.Local(a.Id).Position2D;
            int count = Math.Min(found.Length, physics.Overlap(new RectF(at.X - Radius, at.Y - Radius, Radius * 2, Radius * 2), default, found));
            int near = 0;
            for (int i = 0; i < count; i++)
            {
                var body = physics.BodyOf(found[i]);
                if (body != a.Body && Vector2.DistanceSquared(physics.PositionOf(body), at) <= Radius * Radius) near++;
            }
            a.Near = near;
            pairs += near;
        }
        return pairs;
    }

    private static long CountIndex(IWorld world, SpatialIndex2D<Agent> index, Agent[] found)
    {
        long pairs = 0;
        foreach (var a in world.All<Agent>())
        {
            int count = Math.Min(found.Length, index.QueryCircle(world.Local(a.Id).Position2D, Radius, found));
            int near = 0;
            for (int i = 0; i < count; i++)
                if (found[i] != a) near++;
            a.Near = near;
            pairs += near;
        }
        return pairs;
    }

    public override void Render(IRenderer r) { }

    public override long Checksum()
    {
        long hash = Mix(Seed, _pairs);
        foreach (var a in Objects.All<Agent>())
        {
            var p = Objects.Local(a.Id).Position2D;
            hash = Mix(Mix(Mix(hash, p.X), p.Y), a.Near);
        }
        return hash;
    }

    public override void Count(IDictionary<string, double> resources)
    {
        resources["entities"] = Objects.Count;
        resources["pairs"] = _pairs;
        if (_physics is not null) resources["bodies"] = _physics.Bodies.Length;
    }
}

/// <summary>What a large world updates and draws each step.</summary>
internal enum Scope
{
    /// <summary>Every entity is moved and every entity is drawn.</summary>
    Everything,
    /// <summary>Every entity is moved; one is drawn only when the camera says it is visible.</summary>
    Culled,
    /// <summary>
    /// Entities are filed in a <c>SpatialIndex2D</c>: only those within 96 pixels of the view are moved (the rest
    /// sleep) and only those in view are drawn.
    /// </summary>
    Region,
}

/// <summary>
/// A world much larger than the view: the load's entities at a constant density (2,000 in a 320×180 view, so 200,000
/// cover 2400×2400 pixels), a camera that pans over it, and a strategy for what is updated and drawn.
/// </summary>
internal sealed class LargeWorldScene(int load, Scope scope) : StressScene(load, "update")
{
    public const float PerEntity = 320f * 180 / 2000;
    public const float Margin = 96;
    private readonly Camera2D _camera = new(320, 180);
    private readonly List<Mover> _near = [];
    private SpatialIndex2D<Mover>? _index;
    private float _side, _time;
    private long _drawn, _moved;

    protected override void Prepare()
    {
        var world = Objects;
        _side = MathF.Max(400, MathF.Sqrt(Load * PerEntity));
        if (scope == Scope.Region) _index = new SpatialIndex2D<Mover>(64);
        for (int i = 0; i < Load; i++)
        {
            var id = world.Create();
            var at = new Vector2(Random.NextSingle() * _side, Random.NextSingle() * _side);
            world.Local(id).Position2D = at;
            var mover = world.Attach(id, new Mover { Id = id, Velocity = new Vector2(Random.Next(-30, 30), Random.Next(-30, 30)) });
            if (_index is not null) mover.Place = _index.Add(mover, at);
        }
        WorldCamera = _camera;
        Pan();
    }

    // The camera travels a closed curve over the whole world, so the view keeps meeting other entities.
    private void Pan()
    {
        var reach = new Vector2(_side - 320, _side - 180) / 2;
        _camera.Position = reach + reach * new Vector2(MathF.Cos(_time * 0.2f), MathF.Sin(_time * 0.13f));
    }

    protected override void Step(float dt)
    {
        var world = Objects;
        float side = _side;
        _time += dt;
        Pan();
        long t = Now;
        if (_index is { } index)
        {
            // The active region: the view and a margin around it. What is outside keeps its place and its state.
            _near.Clear();
            index.Query(_camera.VisibleBounds.Inflate(Margin), _near);
            foreach (var m in _near)
            {
                ref var local = ref world.Local(m.Id);
                var p = local.Position2D + m.Velocity * dt;
                if (p.X < 0 || p.X > side) m.Velocity.X = -m.Velocity.X;
                if (p.Y < 0 || p.Y > side) m.Velocity.Y = -m.Velocity.Y;
                local.Position2D = p = Vector2.Clamp(p, Vector2.Zero, new Vector2(side, side));
                index.Move(m.Place, p);
            }
            _moved = _near.Count;
            Lap(0, t);
            return;
        }
        foreach (var m in world.All<Mover>())
        {
            ref var local = ref world.Local(m.Id);
            var p = local.Position2D + m.Velocity * dt;
            if (p.X < 0 || p.X > side) m.Velocity.X = -m.Velocity.X;
            if (p.Y < 0 || p.Y > side) m.Velocity.Y = -m.Velocity.Y;
            local.Position2D = Vector2.Clamp(p, Vector2.Zero, new Vector2(side, side));
        }
        _moved = Load;
        Lap(0, t);
    }

    public override void Render(IRenderer r)
    {
        var world = Objects;
        var ball = Bench.Ball;
        long drawn = 0;
        if (_index is { } index)
        {
            // What the view shows, by a query: the sprite is 8 pixels, so a point 4 pixels outside still shows.
            _near.Clear();
            index.Query(_camera.VisibleBounds.Inflate(4 + _camera.MaxShake), _near);
            foreach (var m in _near) { r.Draw(ball, world.DrawPosition2D(m.Id)); drawn++; }
        }
        else if (scope == Scope.Everything)
            foreach (var m in world.All<Mover>()) { r.Draw(ball, world.DrawPosition2D(m.Id)); drawn++; }
        else
            foreach (var m in world.All<Mover>())
            {
                var p = world.DrawPosition2D(m.Id);
                if (!_camera.IsVisible(new RectF(p.X - 4, p.Y - 4, 8, 8))) continue;
                r.Draw(ball, p);
                drawn++;
            }
        _drawn = drawn;
    }

    public override long Checksum()
    {
        long hash = Mix(Seed, _time);
        foreach (var m in Objects.All<Mover>())
        {
            var p = Objects.Local(m.Id).Position2D;
            hash = Mix(Mix(hash, p.X), p.Y);
        }
        return hash;
    }

    public override void Count(IDictionary<string, double> resources)
    {
        resources["entities"] = Objects.Count;
        resources["drawnLastFrame"] = _drawn;
        resources["movedLastStep"] = _moved;
    }
}

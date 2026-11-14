using System.Numerics;
using Benchmarks;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Physics;
using MicroEngine.Physics.Tiles;
using MicroEngine.Scenes;

namespace StressBench;

/// <summary>The workloads by name: what each one's load counts and how much of it a default run uses.</summary>
public static class Scenarios
{
    private static readonly (string Name, int Load, string What, Func<int, string, StressScene> Create)[] All =
    [
        ("entity-churn", 10_000, "live entities with two attachments; a tenth destroyed and created again every step", (n, _) => new EntityChurnScene(n)),
        ("world-iterate", 20_000, "entities walked through All<T>, moved through Local and drawn through DrawPosition2D", (n, _) => new WorldIterateScene(n)),
        ("spatial-query", 8_000, "static boxes in the tile physics world; a quarter ask Overlap and a quarter RayCast every step", (n, _) => new SpatialQueryScene(n)),
        ("physics", 2_000, "kinematic boxes moved through the tile level (PhysicsBench's world), one Move each and one Step", (n, backend) => new PhysicsScene(n, backend)),
        ("sprites", 20_000, "moving sprites of one texture: one batch", (n, _) => new SpritesScene(n, breakBatches: false)),
        ("batch-breaks", 20_000, "the same sprites over 16 textures, a texture switch every draw and a blend switch every other", (n, _) => new SpritesScene(n, breakBatches: true)),
        ("shapes", 5_000, "filled circles, plus half as many lines and half as many triangles", (n, _) => new ShapesScene(n)),
        ("text", 500, "lines of 24 characters in the built-in bitmap font", (n, _) => new TextScene(n)),
        ("particles", 20_000, "live particles of one additive system, emitted to stay at the load", (n, _) => new ParticlesScene(n)),
        ("asset-churn", 4, "128x128 PNG textures decoded, uploaded, drawn once and unloaded every step", (n, _) => new AssetChurnScene(n)),
        ("integrated", 5_000, "entities moved and drawn, a quarter as many physics bodies, twice as many particles, 2% churn, a HUD", (n, backend) => new IntegratedScene(n, backend)),
    ];

    /// <summary>Every scenario, in the order a suite runs them.</summary>
    public static IReadOnlyList<string> Names { get; } = [.. All.Select(s => s.Name)];

    /// <summary>The load a run uses when none is given.</summary>
    public static int DefaultLoad(string name) => Find(name).Load;

    /// <summary>What the scenario's load counts.</summary>
    public static string Describe(string name) => Find(name).What;

    internal static StressScene Create(string name, int load, string backend) => Find(name).Create(load, backend);

    private static (string Name, int Load, string What, Func<int, string, StressScene> Create) Find(string name)
    {
        foreach (var s in All)
            if (s.Name == name) return s;
        throw new ArgumentException($"Unknown scenario '{name}': {string.Join(", ", Names)}.", nameof(name));
    }
}

/// <summary>The game every scenario runs in: the engine's real loop, a 320×180 canvas, 60 updates per second.</summary>
internal sealed class StressGame(StressScene scene) : Game
{
    public override GameSettings Settings { get; } = new("MicroEngine StressBench", 320, 180, 3)
    {
        UpdateRate = 60,
        Background = Color.Hex("101820"),
    };

    /// <summary>An 8×8 sprite in an atlas of its own.</summary>
    public Sprite Ball { get; private set; } = null!;
    /// <summary>Sixteen separate 8×8 textures, for draws that cannot share a batch.</summary>
    public Texture[] Textures { get; private set; } = [];

    protected override void Load()
    {
        var palette = Art.Palette("o=#1a1c2c", "y=#ffcd75", "w=#f4f4f4");
        var ball = PixelImage.FromMap(palette,
            "..oooo..",
            ".oyyyyo.",
            "oyywyyyo",
            "oyyyyyyo",
            "oyyyyyyo",
            "oyyyyyyo",
            ".oyyyyo.",
            "..oooo..");
        Ball = new SpriteAtlasBuilder().Add("ball", ball, new Vector2(4, 4)).Build(Graphics)["ball"];
        Textures = new Texture[16];
        for (int i = 0; i < Textures.Length; i++)
        {
            var image = new PixelImage(8, 8).Clear(new Color((byte)(40 + i * 13), (byte)(220 - i * 11), (byte)(90 + i * 7)));
            Textures[i] = Graphics.CreateTexture(image);
        }
        Scenes.Push(scene);
    }
}

/// <summary>
/// A workload: what one fixed step does and what one frame draws. The base class reports each step to the
/// <see cref="Probe"/>; a scenario times its own subsystems with <see cref="Lap"/>.
/// </summary>
internal abstract class StressScene(int load, params string[] sections) : Scene
{
    /// <summary>The scenario's size: entities, bodies, sprites...</summary>
    public int Load { get; } = load;
    /// <summary>The subsystems the scenario times inside its step.</summary>
    public string[] SectionNames { get; } = sections;
    /// <summary>Where the run's per-step measurements go; set by the harness before the game starts.</summary>
    public Probe Probe { get; set; } = new(1, []);
    /// <summary>Texture and blend-mode switches the last frame asked for.</summary>
    public int StateChanges { get; protected set; }

    protected StressGame Bench => (StressGame)Game;
    protected Random Random { get; private set; } = new(1);

    public sealed override void Enter()
    {
        Random = new Random(Game.Random.Next());
        _counter = new HeadlessRenderer(Game.Width, Game.Height);
        Prepare();
    }

    public sealed override void Update(float dt)
    {
        Probe.BeginStep();
        Step(dt);
    }

    /// <summary>Draw calls of one frame: counted once, on the last warm-up frame, by drawing it again on a counting renderer.</summary>
    public int Draws { get; private set; } = -1;
    private HeadlessRenderer? _counter;

    public sealed override void Draw(IRenderer renderer)
    {
        Render(renderer);
        if (Draws >= 0 || Probe.Step < Probe.Warmup - 1) return;
        _counter!.Reset();
        Render(_counter);
        Draws = _counter.DrawCalls;
    }

    /// <summary>Ends a timed part of the step: adds the time since <paramref name="since"/> to <paramref name="section"/> and returns now.</summary>
    protected long Lap(int section, long since) => Probe.Lap(section, since);
    protected static long Now => System.Diagnostics.Stopwatch.GetTimestamp();

    /// <summary>Builds the scenario's state; the game and its graphics exist.</summary>
    protected abstract void Prepare();
    protected abstract void Step(float dt);
    /// <summary>Draws a frame; it must draw the same when called twice (the draw calls are counted by a second call).</summary>
    public abstract void Render(IRenderer r);
    /// <summary>A hash of the simulation's state: equal for equal scenario, load, seed and steps.</summary>
    public abstract long Checksum();
    /// <summary>Adds the scenario's resource counts (entities, bodies, particles, textures...).</summary>
    public abstract void Count(IDictionary<string, double> resources);
    /// <summary>Releases what the scenario holds outside the game (temporary files, native worlds).</summary>
    public virtual void Cleanup() { }

    protected static long Mix(long hash, float value) => Mix(hash, BitConverter.SingleToInt32Bits(value));
    protected static long Mix(long hash, long value) => unchecked((hash ^ value) * 1099511628211L);
    protected const long Seed = unchecked((long)14695981039346656037UL);
}

internal sealed class Mover
{
    public EntityId Id;
    public Vector2 Velocity;
    public int Texture;
}

internal sealed class Tag
{
    public int Value;
}

/// <summary>Entity creation and destruction: a ring of live entities, the oldest tenth replaced every step.</summary>
internal sealed class EntityChurnScene(int load) : StressScene(load, "destroy", "create")
{
    private readonly EntityId[] _ring = new EntityId[load];
    private readonly Stack<Mover> _movers = new(load);
    private readonly Stack<Tag> _tags = new(load);
    private int _head, _created;

    private EntityId Spawn()
    {
        var world = Objects;
        var id = world.Create();
        var mover = _movers.Count > 0 ? _movers.Pop() : new Mover();
        mover.Id = id;
        mover.Velocity = new Vector2(Random.Next(-60, 60), Random.Next(-60, 60));
        world.Attach(id, mover);
        var tag = _tags.Count > 0 ? _tags.Pop() : new Tag();
        tag.Value = _created++;
        world.Attach(id, tag);
        world.Local(id).Position2D = new Vector2(Random.Next(Game.Width), Random.Next(Game.Height));
        return id;
    }

    protected override void Prepare()
    {
        for (int i = 0; i < _ring.Length; i++) _ring[i] = Spawn();
    }

    protected override void Step(float dt)
    {
        var world = Objects;
        int batch = Math.Max(1, Load / 10);
        long t = Now;
        for (int j = 0; j < batch; j++)
        {
            var id = _ring[(_head + j) % Load];
            // The attachments go back to the pool, so what the step allocates is the engine's, not the scenario's.
            if (world.TryGet<Mover>(id, out var mover)) _movers.Push(mover);
            if (world.TryGet<Tag>(id, out var tag)) _tags.Push(tag);
            world.Destroy(id);
        }
        t = Lap(0, t);
        for (int j = 0; j < batch; j++) _ring[(_head + j) % Load] = Spawn();
        Lap(1, t);
        _head = (_head + batch) % Load;
    }

    public override void Render(IRenderer r) { }

    public override long Checksum()
    {
        long hash = Mix(Seed, Objects.Count);
        foreach (var id in _ring) hash = Mix(Mix(hash, id.Index), id.Generation);
        return hash;
    }

    public override void Count(IDictionary<string, double> resources)
    {
        resources["entities"] = Objects.Count;
        resources["entitiesCreated"] = _created;
    }
}

/// <summary>World iteration: every entity walked, moved and drawn through the world's own access paths.</summary>
internal sealed class WorldIterateScene(int load) : StressScene(load, "iterate")
{
    protected override void Prepare()
    {
        var world = Objects;
        for (int i = 0; i < Load; i++)
        {
            var id = world.Create();
            world.Attach(id, new Mover { Id = id, Velocity = new Vector2(Random.Next(-90, 90), Random.Next(-90, 90)) });
            world.Attach(id, new Tag { Value = i });
            world.Local(id).Position2D = new Vector2(Random.Next(Game.Width), Random.Next(Game.Height));
        }
    }

    protected override void Step(float dt)
    {
        var world = Objects;
        float width = Game.Width, height = Game.Height;
        long t = Now;
        foreach (var m in world.All<Mover>())
        {
            ref var local = ref world.Local(m.Id);
            var p = local.Position2D + m.Velocity * dt;
            if (p.X < 0 || p.X > width) m.Velocity.X = -m.Velocity.X;
            if (p.Y < 0 || p.Y > height) m.Velocity.Y = -m.Velocity.Y;
            local.Position2D = Vector2.Clamp(p, Vector2.Zero, new Vector2(width, height));
        }
        Lap(0, t);
    }

    public override void Render(IRenderer r)
    {
        var world = Objects;
        var ball = Bench.Ball;
        foreach (var m in world.All<Mover>()) r.Draw(ball, world.DrawPosition2D(m.Id));
    }

    public override long Checksum()
    {
        long hash = Seed;
        foreach (var m in Objects.All<Mover>())
        {
            var p = Objects.Local(m.Id).Position2D;
            hash = Mix(Mix(hash, p.X), p.Y);
        }
        return hash;
    }

    public override void Count(IDictionary<string, double> resources) => resources["entities"] = Objects.Count;
}

/// <summary>
/// Spatial queries: static boxes at a constant density (one 16×16 box per 40×40 pixels on average, so the world grows
/// with the load), and every step a quarter of them ask what overlaps the 64×64 pixels around them and another
/// quarter cast a 128-pixel ray.
/// </summary>
internal sealed class SpatialQueryScene(int load) : StressScene(load, "overlap", "raycast")
{
    private TilePhysicsWorld2D _world = null!;
    private Vector2[] _position = [];
    private long _found, _hits, _step;

    protected override void Prepare()
    {
        _world = new TilePhysicsWorld2D();
        float side = MathF.Sqrt(Load) * 40;
        _position = new Vector2[Load];
        for (int i = 0; i < Load; i++)
        {
            _position[i] = new Vector2(Random.NextSingle() * side, Random.NextSingle() * side);
            var body = _world.CreateBody(new EntityId((uint)i, 1), BodyDescription.Static(_position[i]));
            _world.AddShape(body, ShapeDescription.Box(new Vector2(16, 16)));
        }
    }

    protected override void Step(float dt)
    {
        Span<ShapeId> results = stackalloc ShapeId[32];
        int queries = Math.Max(1, Load / 4);
        // A different quarter every step, so the queries cover the whole world over a run.
        int first = (int)(_step++ * 7919 % Load);
        long t = Now;
        for (int q = 0; q < queries; q++)
        {
            var at = _position[(first + q * 4) % Load];
            _found += _world.Overlap(new RectF(at.X - 32, at.Y - 32, 64, 64), default, results);
        }
        t = Lap(0, t);
        for (int q = 0; q < queries; q++)
        {
            var from = _position[(first + q * 4 + 2) % Load] + new Vector2(12, 12);
            float angle = q * 0.61f;
            if (_world.RayCast(from, from + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 128, default, out _)) _hits++;
        }
        Lap(1, t);
    }

    public override void Render(IRenderer r) { }
    public override long Checksum() => Mix(Mix(Seed, _found), _hits);

    public override void Count(IDictionary<string, double> resources)
    {
        resources["bodies"] = _world.Bodies.Length;
        resources["queriesPerStep"] = Math.Max(1, Load / 4) * 2;
        resources["overlapsFound"] = _found;
        resources["rayHits"] = _hits;
    }
}

/// <summary>Dense physics: PhysicsBench's level with the load's kinematic boxes, each drawn as a rectangle.</summary>
internal sealed class PhysicsScene(int load, string backend) : StressScene(load, "physics")
{
    private IPhysicsWorld2D _world = null!;
    private BodyId[] _bodies = [];
    private Vector2[] _velocity = [];

    protected override void Prepare() => (_world, _bodies, _velocity) = PhysicsLevel.Build(backend, Load);

    protected override void Step(float dt)
    {
        long t = Now;
        PhysicsLevel.Step(_world, _bodies, _velocity, Game.UpdateCount);
        Lap(0, t);
    }

    public override void Render(IRenderer r)
    {
        var color = Color.Hex("94b0c2");
        // The level is 1024×576 and the canvas 320×180: a third of the size, the whole level in view.
        foreach (var body in _bodies)
        {
            var p = _world.PositionOf(body) * (320f / 1024);
            r.FillRect(new RectF(p.X - 2, p.Y - 2, 4, 4), color);
        }
    }

    public override long Checksum()
    {
        long hash = Seed;
        foreach (var body in _bodies)
        {
            var p = _world.PositionOf(body);
            hash = Mix(Mix(hash, p.X), p.Y);
        }
        return hash;
    }

    public override void Count(IDictionary<string, double> resources) => resources["bodies"] = _world.Bodies.Length;
    public override void Cleanup() => (_world as IDisposable)?.Dispose();
}

/// <summary>
/// Render batches: moving sprites, either all of one texture (one batch) or over sixteen textures with a texture
/// switch every draw and a blend-mode switch every other draw (the worst case for a batching renderer).
/// </summary>
internal sealed class SpritesScene(int load, bool breakBatches) : StressScene(load, "move")
{
    private readonly Vector2[] _position = new Vector2[load];
    private readonly Vector2[] _velocity = new Vector2[load];

    protected override void Prepare()
    {
        for (int i = 0; i < Load; i++)
        {
            _position[i] = new Vector2(Random.Next(Game.Width), Random.Next(Game.Height));
            _velocity[i] = new Vector2(Random.Next(-90, 90), Random.Next(-90, 90));
        }
    }

    protected override void Step(float dt)
    {
        var bounds = new Vector2(Game.Width, Game.Height);
        long t = Now;
        for (int i = 0; i < _position.Length; i++)
        {
            var p = _position[i] + _velocity[i] * dt;
            if (p.X < 0 || p.X > bounds.X) _velocity[i].X = -_velocity[i].X;
            if (p.Y < 0 || p.Y > bounds.Y) _velocity[i].Y = -_velocity[i].Y;
            _position[i] = Vector2.Clamp(p, Vector2.Zero, bounds);
        }
        Lap(0, t);
    }

    public override void Render(IRenderer r)
    {
        if (!breakBatches)
        {
            var ball = Bench.Ball;
            for (int i = 0; i < _position.Length; i++) r.Draw(ball, _position[i]);
            StateChanges = 0;
            return;
        }
        var textures = Bench.Textures;
        for (int i = 0; i < _position.Length; i++)
        {
            if ((i & 1) == 0) r.SetBlendMode((i & 2) == 0 ? BlendMode.Alpha : BlendMode.Additive);
            r.Draw(textures[i & 15], _position[i]);
        }
        r.SetBlendMode(BlendMode.Alpha);
        StateChanges = _position.Length + (_position.Length + 1) / 2;
    }

    public override long Checksum()
    {
        long hash = Seed;
        foreach (var p in _position) hash = Mix(Mix(hash, p.X), p.Y);
        return hash;
    }

    public override void Count(IDictionary<string, double> resources)
    {
        resources["sprites"] = Load;
        resources["textures"] = breakBatches ? Bench.Textures.Length : 1;
    }
}

/// <summary>Geometry: filled circles, lines and triangles, the draws SDL submits as triangles rather than copies.</summary>
internal sealed class ShapesScene(int load) : StressScene(load)
{
    private readonly Vector2[] _at = new Vector2[load];
    private float _phase;

    protected override void Prepare()
    {
        for (int i = 0; i < Load; i++) _at[i] = new Vector2(Random.Next(Game.Width), Random.Next(Game.Height));
    }

    protected override void Step(float dt) => _phase += dt;

    public override void Render(IRenderer r)
    {
        var circle = Color.Hex("29366f").WithAlpha(0.8f);
        var line = Color.Hex("ffcd75");
        var triangle = Color.Hex("b13e53").WithAlpha(0.6f);
        var drift = new Vector2(MathF.Cos(_phase), MathF.Sin(_phase)) * 4;
        for (int i = 0; i < _at.Length; i++)
        {
            var p = _at[i] + drift;
            r.FillCircle(p, 3 + i % 5, circle);
            if ((i & 1) == 0) r.DrawLine(p, p + new Vector2(12, 5), 1, line);
            else r.FillTriangle(p, p + new Vector2(8, 0), p + new Vector2(4, 7), triangle);
        }
        StateChanges = 0;
    }

    public override long Checksum() => Mix(Seed, _phase);
    public override void Count(IDictionary<string, double> resources) => resources["shapes"] = Load * 2;
}

/// <summary>Text: lines of 24 characters in the built-in bitmap font, the strings built once.</summary>
internal sealed class TextScene(int load) : StressScene(load)
{
    private readonly string[] _lines = new string[load];
    private long _steps;

    protected override void Prepare()
    {
        for (int i = 0; i < Load; i++) _lines[i] = $"LINE {i:00000} SCORE {i * 7919 % 100000:00000} X".PadRight(24)[..24];
    }

    protected override void Step(float dt) => _steps++;

    public override void Render(IRenderer r)
    {
        var color = Color.Hex("94b0c2");
        var font = Game.Font;
        for (int i = 0; i < _lines.Length; i++)
            font.Draw(r, _lines[i], new Vector2(4 + i * 3 % 120, 4 + i * 8 % (Game.Height - 8)), color);
        StateChanges = 0;
    }

    public override long Checksum() => Mix(Seed, _steps);

    public override void Count(IDictionary<string, double> resources)
    {
        resources["lines"] = Load;
        resources["characters"] = Load * 24;
    }
}

/// <summary>Particles: one additive system kept at the load by emitting as many as die.</summary>
internal sealed class ParticlesScene(int load) : StressScene(load, "particles")
{
    private readonly ParticleSystem _particles = new(Math.Max(1, load)) { Additive = true };
    private readonly ParticleSpec _spark = new() { StartColor = Color.Hex("ffcd75"), Life = (0.8f, 1.2f), Speed = (20, 70), Spread = 60, Size = (2, 1) };
    private float _emit;

    protected override void Prepare() { }

    protected override void Step(float dt)
    {
        long t = Now;
        _emit += Load * dt;   // mean life 1 s: the load per second keeps the load alive
        int count = (int)_emit;
        _emit -= count;
        if (count > 0) _particles.Emit(new Vector2(Game.Width / 2f, Game.Height / 2f), _spark, count);
        _particles.Update(dt);
        Lap(0, t);
    }

    public override void Render(IRenderer r)
    {
        _particles.Draw(r);
        StateChanges = 2;
    }

    public override long Checksum() => Mix(Seed, _particles.Count);
    public override void Count(IDictionary<string, double> resources) => resources["particles"] = _particles.Count;
}

/// <summary>
/// Asset churn: every step the load's PNG files are decoded and uploaded through the asset cache, drawn once, and
/// unloaded at the start of the next step. The files are written to a temporary folder first.
/// </summary>
internal sealed class AssetChurnScene(int load) : StressScene(load, "unload", "load")
{
    private const int Size = 128;
    private DirectoryInfo? _folder;
    private AssetCache? _assets;
    private readonly string[] _paths = new string[load];
    private readonly Texture?[] _live = new Texture?[load];
    private long _loads;

    protected override void Prepare()
    {
        _folder = Directory.CreateTempSubdirectory("microengine-stressbench");
        for (int i = 0; i < Load; i++)
        {
            var image = new PixelImage(Size, Size);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++) image[x, y] = new Color((byte)(x * 2 + i), (byte)(y * 2), (byte)((x ^ y) + Random.Next(8)));
            _paths[i] = $"churn{i}.png";
            File.WriteAllBytes(Path.Combine(_folder.FullName, _paths[i]), PngEncoder.Encode(image.ToRgba(), Size, Size));
        }
        _assets = new AssetCache(Game.Graphics, _folder.FullName);
    }

    protected override void Step(float dt)
    {
        var assets = _assets!;
        long t = Now;
        for (int i = 0; i < Load; i++)
            if (_live[i] is not null) { assets.Unload(_paths[i]); _live[i] = null; }
        t = Lap(0, t);
        for (int i = 0; i < Load; i++) { _live[i] = assets.Texture(_paths[i]); _loads++; }
        Lap(1, t);
    }

    public override void Render(IRenderer r)
    {
        for (int i = 0; i < Load; i++)
            if (_live[i] is { } texture) r.Draw(texture, new Vector2(i * 9 % Game.Width, i * 5 % Game.Height));
        StateChanges = Load;
    }

    public override long Checksum() => Mix(Mix(Seed, _loads), _assets!.Count);

    public override void Count(IDictionary<string, double> resources)
    {
        resources["texturesLive"] = _assets!.Count;
        resources["texturesLoaded"] = _loads;
        resources["textureBytesLive"] = _assets.Statistics.TextureBytes;
    }

    public override void Cleanup()
    {
        _assets?.Dispose();
        _folder?.Delete(recursive: true);
    }
}

/// <summary>
/// Everything at once, in the proportions of a busy 2D game: the load's entities moved and drawn over four textures,
/// a quarter as many physics bodies, twice as many particles, 2% of the entities replaced every step and a HUD.
/// </summary>
internal sealed class IntegratedScene(int load, string backend) : StressScene(load, "world", "physics", "particles", "churn")
{
    private readonly ParticleSystem _particles = new(Math.Max(1, load * 2)) { Additive = true };
    private readonly ParticleSpec _spark = new() { StartColor = Color.Hex("ffcd75"), Life = (0.8f, 1.2f), Speed = (20, 70), Spread = 80, Size = (2, 1) };
    private readonly EntityId[] _ring = new EntityId[load];
    private readonly Stack<Mover> _movers = new();
    private readonly string[] _hud = ["SCORE 0012345", "LIVES 3", "WAVE 17", "COMBO X4", "TIME 01:23", "FPS 60"];
    private IPhysicsWorld2D _physics = null!;
    private BodyId[] _bodies = [];
    private Vector2[] _velocity = [];
    private float _emit;
    private int _head;

    private EntityId Spawn(int index)
    {
        var world = Objects;
        var id = world.Create();
        var mover = _movers.Count > 0 ? _movers.Pop() : new Mover();
        mover.Id = id;
        mover.Velocity = new Vector2(Random.Next(-90, 90), Random.Next(-90, 90));
        mover.Texture = index * 4 / Load;
        world.Attach(id, mover);
        world.Local(id).Position2D = new Vector2(Random.Next(Game.Width), Random.Next(Game.Height));
        return id;
    }

    protected override void Prepare()
    {
        for (int i = 0; i < Load; i++) _ring[i] = Spawn(i);
        (_physics, _bodies, _velocity) = PhysicsLevel.Build(backend, Math.Max(1, Load / 4));
    }

    protected override void Step(float dt)
    {
        var world = Objects;
        float width = Game.Width, height = Game.Height;
        long t = Now;
        foreach (var m in world.All<Mover>())
        {
            ref var local = ref world.Local(m.Id);
            var p = local.Position2D + m.Velocity * dt;
            if (p.X < 0 || p.X > width) m.Velocity.X = -m.Velocity.X;
            if (p.Y < 0 || p.Y > height) m.Velocity.Y = -m.Velocity.Y;
            local.Position2D = Vector2.Clamp(p, Vector2.Zero, new Vector2(width, height));
        }
        t = Lap(0, t);
        PhysicsLevel.Step(_physics, _bodies, _velocity, Game.UpdateCount);
        t = Lap(1, t);
        _emit += Load * 2 * dt;
        int count = (int)_emit;
        _emit -= count;
        if (count > 0) _particles.Emit(new Vector2(width / 2, height / 2), _spark, count);
        _particles.Update(dt);
        t = Lap(2, t);
        int batch = Math.Max(1, Load / 50);
        for (int j = 0; j < batch; j++)
        {
            int index = (_head + j) % Load;
            if (world.TryGet<Mover>(_ring[index], out var mover)) _movers.Push(mover);
            world.Destroy(_ring[index]);
            _ring[index] = Spawn(index);
        }
        _head = (_head + batch) % Load;
        Lap(3, t);
    }

    public override void Render(IRenderer r)
    {
        var world = Objects;
        var textures = Bench.Textures;
        int switches = 0, last = -1;
        foreach (var m in world.All<Mover>())
        {
            if (m.Texture != last) { last = m.Texture; switches++; }
            r.Draw(textures[m.Texture], world.DrawPosition2D(m.Id));
        }
        var color = Color.Hex("94b0c2");
        foreach (var body in _bodies)
        {
            var p = _physics.PositionOf(body) * (320f / 1024);
            r.FillRect(new RectF(p.X - 2, p.Y - 2, 4, 4), color);
        }
        _particles.Draw(r, Game.FrameBlend);
        for (int i = 0; i < _hud.Length; i++) Game.Font.Draw(r, _hud[i], new Vector2(4, 4 + i * 8), Color.White);
        StateChanges = switches + 2;
    }

    public override long Checksum()
    {
        long hash = Mix(Seed, _particles.Count);
        foreach (var m in Objects.All<Mover>())
        {
            var p = Objects.Local(m.Id).Position2D;
            hash = Mix(Mix(hash, p.X), p.Y);
        }
        foreach (var body in _bodies)
        {
            var p = _physics.PositionOf(body);
            hash = Mix(Mix(hash, p.X), p.Y);
        }
        return hash;
    }

    public override void Count(IDictionary<string, double> resources)
    {
        resources["entities"] = Objects.Count;
        resources["bodies"] = _physics.Bodies.Length;
        resources["particles"] = _particles.Count;
    }

    public override void Cleanup() => (_physics as IDisposable)?.Dispose();
}

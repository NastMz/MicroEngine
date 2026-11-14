using System.Numerics;
using MicroEngine;
using MicroEngine.Physics;
using MicroEngine.Physics.Box2D;
using MicroEngine.Physics.Tiles;
using MicroEngine.Scenes;
using MicroEngine.World;

namespace Benchmarks;

/// <summary>
/// The physics world PhysicsBench and StressBench time: N kinematic 12×14 boxes falling and walking through a 64×36
/// tile level with one-way platforms, 32 static boxes and 16 sensors, one Move per box and one Step per fixed step
/// (60 Hz). Box2D has no tile geometry, so its level is the same grid as boxes: one per horizontal run of solid or
/// one-way cells.
/// </summary>
internal static class PhysicsLevel
{
    public const int Tile = 16, Width = 64, Height = 36;
    public const float Dt = 1 / 60f;

    private static readonly TileLayer Layer = BuildLayer();

    /// <summary>The level's tiles: solid borders and twelve one-way platforms.</summary>
    public static TileGrid Grid { get; } = new(Layer, Tile, new Dictionary<int, TileFlags> { [1] = TileFlags.Solid, [2] = TileFlags.OneWay });

    private static TileLayer BuildLayer()
    {
        var layer = new TileLayer(Width, Height);
        for (int x = 0; x < Width; x++) { layer[x, Height - 1] = 1; layer[x, 0] = 1; }
        for (int y = 0; y < Height; y++) { layer[0, y] = 1; layer[Width - 1, y] = 1; }
        for (int p = 0; p < 12; p++)
            for (int x = 0; x < 6; x++) layer[4 + p * 5 % 54 + x, 6 + p * 7 % 26] = 2;
        return layer;
    }

    // The level as boxes, for a backend without tile geometry: one box per horizontal run of cells with the same flag.
    private static void AddLevelBoxes(IPhysicsWorld2D world)
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width;)
            {
                int id = Layer[x, y];
                if (id == 0) { x++; continue; }
                int start = x;
                while (x < Width && Layer[x, y] == id) x++;
                var center = new Vector2((start + x) * Tile / 2f, (y + 0.5f) * Tile);
                var body = world.CreateBody(default, BodyDescription.Static(center));
                world.AddShape(body, ShapeDescription.Box(new Vector2((x - start) * Tile, Tile)) with { OneWay = id == 2 });
            }
    }

    /// <summary>A new world of <paramref name="backend"/> ("tiles" or "box2d") with the level and <paramref name="bodies"/> kinematic boxes.</summary>
    public static (IPhysicsWorld2D World, BodyId[] Bodies, Vector2[] Velocity) Build(string backend, int bodies)
    {
        IPhysicsWorld2D world;
        if (backend == "tiles")
        {
            var tiles = new TilePhysicsWorld2D();
            tiles.AddTiles(tiles.CreateBody(default, BodyDescription.Static()), Grid);
            world = tiles;
        }
        else
        {
            // A tile to the metre, y down: the engine's pixel space, as the PhysicsSubstitution sample sets it.
            world = new Box2DPhysicsWorld2D(new Box2DWorldOptions { UnitsPerMeter = Tile, YDown = true });
            AddLevelBoxes(world);
        }
        for (int i = 0; i < 32; i++)
        {
            var box = world.CreateBody(default, BodyDescription.Static(new Vector2(64 + i * 29 % 900, 120 + i * 53 % 400)));
            world.AddShape(box, ShapeDescription.Box(new Vector2(24, 8)));
        }
        for (int i = 0; i < 16; i++)
        {
            var sensor = world.CreateBody(default, BodyDescription.Static(new Vector2(80 + i * 61 % 880, 100 + i * 37 % 420)));
            world.AddShape(sensor, ShapeDescription.Sensor(new Vector2(32, 32)));
        }
        var ids = new BodyId[bodies];
        var velocity = new Vector2[bodies];
        for (int i = 0; i < bodies; i++)
        {
            ids[i] = world.CreateBody(new EntityId((uint)i, 1), BodyDescription.Kinematic(new Vector2(32 + i * 37 % 960, 32 + i * 13 % 480)));
            world.AddShape(ids[i], ShapeDescription.Box(new Vector2(12, 14)));
            velocity[i] = new Vector2(i % 2 == 0 ? 60 : -60, 0);
        }
        return (world, ids, velocity);
    }

    /// <summary>Fixed step <paramref name="step"/>: gravity, one Move per box (a jump now and then), then the world's Step.</summary>
    public static void Step(IPhysicsWorld2D world, BodyId[] ids, Vector2[] velocity, long step)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            ref var v = ref velocity[i];
            v.Y = MathF.Min(v.Y + 900 * Dt, 420);
            var r = world.Move(ids[i], v * Dt);
            if (r.OnGround || r.HitCeiling) v.Y = r.OnGround && step % 90 == i % 90 ? -300 : 0;
            if (r.HitWall) v.X = -v.X;
        }
        world.Step(Dt);
    }
}

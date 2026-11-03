using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using MicroEngine;
using MicroEngine.Physics;
using MicroEngine.Physics.Tiles;
using MicroEngine.Scenes;
using MicroEngine.World;

// Times the tile physics backend: N kinematic 12×14 boxes falling and walking through a 64×36 tile level with
// one-way platforms, 32 static boxes and 16 sensors, one Move per box and one Step per fixed step (60 Hz).
//
//   PhysicsBench [--bodies N] [--steps N] [--runs N]
//
// Prints the mean, median and 95th percentile time per step of each run (after one warm-up run), the TileMotion
// façade's time for the same moves, and the bytes allocated per step on this thread.
int bodies = 200, steps = 600, runs = 3;
for (int i = 0; i < args.Length; i++)
    switch (args[i])
    {
        case "--bodies": bodies = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
        case "--steps": steps = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
        case "--runs": runs = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
    }

const int Tile = 16, Width = 64, Height = 36;
var layer = new TileLayer(Width, Height);
for (int x = 0; x < Width; x++) { layer[x, Height - 1] = 1; layer[x, 0] = 1; }
for (int y = 0; y < Height; y++) { layer[0, y] = 1; layer[Width - 1, y] = 1; }
for (int p = 0; p < 12; p++)
for (int x = 0; x < 6; x++) layer[4 + p * 5 % 54 + x, 6 + p * 7 % 26] = 2;
var grid = new TileGrid(layer, Tile, new Dictionary<int, TileFlags> { [1] = TileFlags.Solid, [2] = TileFlags.OneWay });

(TilePhysicsWorld2D World, BodyId[] Bodies, Vector2[] Velocity) Build()
{
    var world = new TilePhysicsWorld2D();
    var level = world.CreateBody(default, BodyDescription.Static());
    world.AddTiles(level, grid);
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

const float Dt = 1 / 60f;
void Simulate(TilePhysicsWorld2D world, BodyId[] ids, Vector2[] velocity, double[] times)
{
    var clock = new Stopwatch();
    for (int s = 0; s < steps; s++)
    {
        clock.Restart();
        for (int i = 0; i < ids.Length; i++)
        {
            ref var v = ref velocity[i];
            v.Y = MathF.Min(v.Y + 900 * Dt, 420);
            var r = world.Move(ids[i], v * Dt);
            if (r.OnGround || r.HitCeiling) v.Y = r.OnGround && s % 90 == i % 90 ? -300 : 0;
            if (r.HitWall) v.X = -v.X;
        }
        world.Step(Dt);
        times[s] = clock.Elapsed.TotalMilliseconds;
    }
}

void Facade(RectF[] boxes, Vector2[] velocity, double[] times)
{
    var clock = new Stopwatch();
    for (int s = 0; s < steps; s++)
    {
        clock.Restart();
        for (int i = 0; i < boxes.Length; i++)
        {
            ref var v = ref velocity[i];
            v.Y = MathF.Min(v.Y + 900 * Dt, 420);
            var r = TileMotion.Move(boxes[i], v * Dt, grid);
            boxes[i] = r.Bounds;
            if (r.HitFloor || r.HitCeiling) v.Y = r.HitFloor && s % 90 == i % 90 ? -300 : 0;
            if (r.HitWall) v.X = -v.X;
        }
        times[s] = clock.Elapsed.TotalMilliseconds;
    }
}

static string Stats(double[] t)
{
    var sorted = t.Order().ToArray();
    return string.Create(CultureInfo.InvariantCulture, $"mean {t.Average():0.000} ms, median {sorted[sorted.Length / 2]:0.000} ms, p95 {sorted[(int)(sorted.Length * 0.95)]:0.000} ms");
}

Console.WriteLine($"PhysicsBench: {bodies} kinematic boxes, {steps} steps at 60 Hz, {runs} runs after a warm-up");
var times = new double[steps];
for (int run = 0; run <= runs; run++)
{
    var (world, ids, velocity) = Build();
    long before = GC.GetAllocatedBytesForCurrentThread();
    Simulate(world, ids, velocity, times);
    long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    if (run == 0) continue;
    Console.WriteLine($"world  run {run}: {Stats(times)}, {allocated / (double)steps:0} B/step");
}
for (int run = 0; run <= runs; run++)
{
    var (world, ids, velocity) = Build();
    var boxes = ids.Select(id => RectF.FromCenter(world.PositionOf(id), 12, 14)).ToArray();
    long before = GC.GetAllocatedBytesForCurrentThread();
    Facade(boxes, velocity, times);
    long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    if (run == 0) continue;
    Console.WriteLine($"façade run {run}: {Stats(times)}, {allocated / (double)steps:0} B/step");
}

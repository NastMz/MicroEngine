using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using Benchmarks;
using MicroEngine;
using MicroEngine.Physics;
using MicroEngine.Physics.Box2D;
using MicroEngine.Physics.Tiles;
using MicroEngine.Scenes;
using MicroEngine.World;

// Times a 2D physics backend: N kinematic 12×14 boxes falling and walking through a 64×36 tile level with
// one-way platforms, 32 static boxes and 16 sensors, one Move per box and one Step per fixed step (60 Hz).
//
//   PhysicsBench [--backend tiles|box2d] [--bodies N] [--steps N] [--runs N]
//
// Prints the mean, median and 95th percentile time per step of each run (after one warm-up run) and the bytes
// allocated per step on this thread; for the tile backend, also the TileMotion façade's time for the same moves.
// Box2D has no tile geometry, so its level is the same grid as boxes: one per horizontal run of solid or one-way
// cells (benchmarks/Shared/PhysicsLevel.cs, the world StressBench also runs). It needs the native library of this
// machine (src/MicroEngine.Physics.Box2D/native/build.sh).
int bodies = 200, steps = 600, runs = 3;
string backend = "tiles";
for (int i = 0; i < args.Length; i++)
    switch (args[i])
    {
        case "--bodies": bodies = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
        case "--steps": steps = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
        case "--runs": runs = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
        case "--backend": backend = args[++i]; break;
    }
if (backend is not ("tiles" or "box2d")) throw new ArgumentException($"Unknown backend '{backend}': tiles or box2d.");
if (backend == "box2d" && !Box2DPhysicsWorld2D.IsSupported)
{
    Console.WriteLine("box2d: the native library of this machine is missing (src/MicroEngine.Physics.Box2D/native/build.sh).");
    return 1;
}

const float Dt = PhysicsLevel.Dt;
var grid = PhysicsLevel.Grid;
(IPhysicsWorld2D World, BodyId[] Bodies, Vector2[] Velocity) Build() => PhysicsLevel.Build(backend, bodies);

void Simulate(IPhysicsWorld2D world, BodyId[] ids, Vector2[] velocity, double[] times)
{
    var clock = new Stopwatch();
    for (int s = 0; s < steps; s++)
    {
        clock.Restart();
        PhysicsLevel.Step(world, ids, velocity, s);
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

Console.WriteLine($"PhysicsBench ({backend}): {bodies} kinematic boxes, {steps} steps at 60 Hz, {runs} runs after a warm-up");
var times = new double[steps];
for (int run = 0; run <= runs; run++)
{
    var (world, ids, velocity) = Build();
    long before = GC.GetAllocatedBytesForCurrentThread();
    Simulate(world, ids, velocity, times);
    long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    (world as IDisposable)?.Dispose();
    if (run == 0) continue;
    Console.WriteLine($"world  run {run}: {Stats(times)}, {allocated / (double)steps:0} B/step");
}
if (backend != "tiles") return 0;
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
return 0;

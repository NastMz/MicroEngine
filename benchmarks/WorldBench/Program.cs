using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using MicroEngine;
using MicroEngine.Scenes;

// The scene world's hot paths, ns per operation and bytes allocated per operation (best of 9 runs after warm-up, one
// thread, 1000 entities). Run in Release. A change to SceneWorld that makes a row several times slower (Local is the
// one games feel: every position read and write goes through it) should be explained in its pull request.
System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
Console.WriteLine($"# SceneWorld ({DateTime.UtcNow:yyyy-MM-dd}, {RuntimeInformation.FrameworkDescription}, {RuntimeInformation.OSDescription}, {RuntimeInformation.ProcessArchitecture})");
Console.WriteLine();
Console.WriteLine("| Operation | ns | B |");
Console.WriteLine("| --- | ---: | ---: |");
Bench.Run();

internal sealed class Payload { public int Value; }
internal sealed class Other { public int Value = 1; }
internal sealed class Third { public int Value = 1; }
internal sealed class Fourth { public int Value = 1; }

internal static class Bench
{
    private const int Count = 1000, Runs = 9, Repeat = 200;
    private static long _sink;

    private static void Row(string what, int ops, Action body)
    {
        for (int i = 0; i < 3; i++) body();
        double best = double.MaxValue;
        long bytes = 0;
        for (int r = 0; r < Runs; r++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            var sw = Stopwatch.StartNew();
            body();
            sw.Stop();
            bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            best = Math.Min(best, sw.Elapsed.TotalNanoseconds / ops);
        }
        Console.WriteLine($"| {what} | {best:0.00} | {(double)bytes / ops:0.###} |");
    }

    private static SceneWorld Populated(out EntityId[] ids, int payloadEvery = 1)
    {
        var world = new SceneWorld();
        ids = new EntityId[Count];
        for (int i = 0; i < Count; i++)
        {
            ids[i] = world.Create();
            world.Local(ids[i]).Position2D = new Vector2(i, i * 0.5f);
            if (i % payloadEvery == 0) world.Attach(ids[i], new Payload { Value = i });
            world.Attach(ids[i], new Other());
            world.Attach(ids[i], new Third());
            world.Attach(ids[i], new Fourth());
        }
        return world;
    }

    public static void Run()
    {
        var world = Populated(out var ids);
        var order = ids.OrderBy(id => (id.Index * 7919u) % Count).ToArray();   // a fixed shuffle

        Row("`TryGet<class>` hit, 4 attachment types, shuffled order", Count * Repeat, () =>
        {
            long s = 0;
            for (int r = 0; r < Repeat; r++)
                foreach (var id in order) if (world.TryGet<Payload>(id, out var p)) s += p.Value;
            _sink += s;
        });
        var sparse = Populated(out var sparseIds, payloadEvery: 10);
        Row("`TryGet<class>` miss", Count / 10 * Repeat, () =>
        {
            long s = 0;
            for (int r = 0; r < Repeat; r++)
                for (int i = 1; i < Count; i += 10) s += sparse.TryGet<Payload>(sparseIds[i], out _) ? 1 : 0;
            _sink += s;
        });
        Row("`All<class>` walk, every entity has it, per element", Count * Repeat, () =>
        {
            long s = 0;
            for (int r = 0; r < Repeat; r++) foreach (var p in world.All<Payload>()) s += p.Value;
            _sink += s;
        });
        Row("`All<class>` walk, 1 in 10 has it, per element found", Count / 10 * Repeat, () =>
        {
            long s = 0;
            for (int r = 0; r < Repeat; r++) foreach (var p in sparse.All<Payload>()) s += p.Value;
            _sink += s;
        });
        Row("`Local(id).Position2D` read", Count * Repeat, () =>
        {
            float s = 0;
            for (int r = 0; r < Repeat; r++) foreach (var id in ids) s += world.Local(id).Position2D.X;
            _sink += (long)s;
        });
        Row("`Local(id).Position2D += delta`", Count * Repeat, () =>
        {
            for (int r = 0; r < Repeat; r++) foreach (var id in ids) world.Local(id).Position2D += new Vector2(0.001f, 0);
        });
        world.BeginStep(1, 1 / 60f);
        foreach (var id in ids) world.Local(id).Position2D += Vector2.One;
        world.EndStep();
        world.BeginDraw(1, 0.5f);
        Row("`DrawPosition2D(id)` at blend 0.5, root entity", Count * Repeat, () =>
        {
            float s = 0;
            for (int r = 0; r < Repeat; r++) foreach (var id in ids) s += world.DrawPosition2D(id).X;
            _sink += (long)s;
        });

        var tree = new SceneWorld();
        var room = tree.Create();
        var rotated = tree.Create();
        tree.Local(rotated).Rotation2D = 0.3f;
        var translated = new EntityId[Count];
        var turned = new EntityId[Count];
        for (int i = 0; i < Count; i++)
        {
            translated[i] = tree.Create(room);
            turned[i] = tree.Create(rotated);
            tree.Local(translated[i]).Position2D = tree.Local(turned[i]).Position2D = new Vector2(i, 0);
        }
        tree.BeginStep(1, 1 / 60f);
        tree.Local(room).Position2D = tree.Local(rotated).Position2D = new Vector2(5, 5);
        tree.EndStep();
        tree.BeginDraw(1, 0.5f);
        Row("`DrawPosition2D(id)`, child of a translation-only parent", Count * Repeat / 10, () =>
        {
            float s = 0;
            for (int r = 0; r < Repeat / 10; r++) foreach (var id in translated) s += tree.DrawPosition2D(id).X;
            _sink += (long)s;
        });
        Row("`DrawPosition2D(id)`, child of a rotated parent (matrix path)", Count * Repeat / 10, () =>
        {
            float s = 0;
            for (int r = 0; r < Repeat / 10; r++) foreach (var id in turned) s += tree.DrawPosition2D(id).X;
            _sink += (long)s;
        });
        Row("`Children(parent)` walk, per child", Count * Repeat, () =>
        {
            long s = 0;
            for (int r = 0; r < Repeat; r++) foreach (var c in tree.Children(room)) s += c.Index;
            _sink += s;
        });

        var churn = new SceneWorld();
        var batch = new EntityId[100];
        Row("Create + `Attach<class>` + `Destroy` + release, per entity", batch.Length * Repeat, () =>
        {
            var payload = new Payload();
            for (int r = 0; r < Repeat; r++)
            {
                for (int i = 0; i < batch.Length; i++) churn.Attach(batch[i] = churn.Create(), payload);
                foreach (var id in batch) churn.Destroy(id);
                churn.EndStep();
            }
        });
        Row("`BeginStep` (keeps step-start transforms), per entity", Count * Repeat, () =>
        {
            for (int r = 0; r < Repeat; r++) world.BeginStep(r, 1 / 60f);
        });
        Large();
        Spatial();
        Console.WriteLine();
        Console.WriteLine($"(checksum {_sink})");
    }

    // WORLD-1: what a step costs a large world that does little (docs/evaluations/world-1.md).
    private static void Large()
    {
        const int big = 200_000;
        var world = new SceneWorld();
        var ids = new EntityId[big];
        for (int i = 0; i < big; i++)
        {
            ids[i] = world.Create();
            world.Attach(ids[i], new Payload { Value = i });
            world.Attach(ids[i], new Other());
        }
        var spare = new Payload();
        var other = new Other();
        Row("`BeginStep`, 200,000 entities, per entity", big * 20, () =>
        {
            for (int r = 0; r < 20; r++) world.BeginStep(r, 1 / 60f);
        });
        int next = 0;
        Row("A step that destroys and creates one entity among 200,000 (`Destroy`, `EndStep`, `Create`, two `Attach`), per step", 200, () =>
        {
            for (int r = 0; r < 200; r++)
            {
                next = (next + 7919) % big;
                world.Destroy(ids[next]);
                world.EndStep();
                ids[next] = world.Create();
                world.Attach(ids[next], spare);
                world.Attach(ids[next], other);
            }
        });
        Row("`All<class>` walk of 200,000 after those steps, per element", big * 5, () =>
        {
            long s = 0;
            for (int r = 0; r < 5; r++) foreach (var p in world.All<Payload>()) s += p.Value;
            _sink += s;
        });
    }

    // WORLD-1: SpatialIndex2D, 20,000 points at one per 24x24 units (a query of radius 48 finds about thirteen).
    private static void Spatial()
    {
        const int points = 20_000;
        float side = MathF.Sqrt(points) * 24;
        var random = new Random(1);
        var index = new SpatialIndex2D<int>(48);
        var at = new Vector2[points];
        var handles = new SpatialHandle[points];
        Row("`SpatialIndex2D.Add` (a point), per item", points, () =>
        {
            index.Clear();
            random = new Random(1);
            for (int i = 0; i < points; i++)
            {
                at[i] = new Vector2(random.NextSingle() * side, random.NextSingle() * side);
                handles[i] = index.Add(i, at[i]);
            }
        });
        Row("`SpatialIndex2D.Move` within the item's cell", points * 10, () =>
        {
            for (int r = 0; r < 10; r++)
                for (int i = 0; i < points; i++) index.Move(handles[i], at[i]);
        });
        Row("`SpatialIndex2D.Move` to another cell and back", points * 2, () =>
        {
            for (int i = 0; i < points; i++) index.Move(handles[i], at[i] + new Vector2(96, 0));
            for (int i = 0; i < points; i++) index.Move(handles[i], at[i]);
        });
        var found = new int[256];
        Row("`SpatialIndex2D.QueryCircle`, radius 48 (about 13 found), per query", points, () =>
        {
            long s = 0;
            for (int i = 0; i < points; i++) s += index.QueryCircle(at[i], 48, found);
            _sink += s;
        });
        Row("`SpatialIndex2D.Query`, 96x96 (about 16 found), per query", points, () =>
        {
            long s = 0;
            for (int i = 0; i < points; i++) s += index.Query(new RectF(at[i].X - 48, at[i].Y - 48, 96, 96), found);
            _sink += s;
        });
        var view = new List<int>(4096);
        Row("`SpatialIndex2D.Query`, 1431x805 into a list (about 2,000 found), per query", 200, () =>
        {
            long s = 0;
            for (int i = 0; i < 200; i++)
            {
                view.Clear();
                s += index.Query(new RectF(at[i].X * 0.5f, at[i].Y * 0.5f, 1431, 805), view);
            }
            _sink += s;
        });
        Row("Walk of 20,000 positions for one circle (the walk an index replaces), per query", 200, () =>
        {
            long s = 0;
            for (int q = 0; q < 200; q++)
            {
                var c = at[q];
                for (int i = 0; i < points; i++)
                    if (Vector2.DistanceSquared(at[i], c) <= 48 * 48) s++;
            }
            _sink += s;
        });
    }
}

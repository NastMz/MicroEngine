using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
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
        Console.WriteLine();
        Console.WriteLine($"(checksum {_sink})");
    }
}

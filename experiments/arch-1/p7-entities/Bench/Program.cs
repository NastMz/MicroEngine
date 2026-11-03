extern alias original;

using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Arch1.Scene;
using CourtyardKey.Scenes;
using MicroEngine.Core;
using MicroEngine.Platform;

// ARCH-1 P7 measurements. Release build, one thread, best of several runs after warm-up; ns per operation.
System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
string mode = args.Length > 0 ? args[0] : "all";
Console.WriteLine($"# P7 measurements ({DateTime.UtcNow:yyyy-MM-dd}, {RuntimeInformation.FrameworkDescription}, {RuntimeInformation.OSDescription}, {RuntimeInformation.ProcessArchitecture}, {Environment.ProcessorCount} logical CPUs)");
if (mode is "micro" or "all") Micro.Run();
if (mode is "game" or "all") GameRuns.Run(args.Length > 1 ? args[1] : null);

internal sealed class Payload { public int Value; }
internal sealed class Other { public int Value = 1; }
internal sealed class Third { public int Value = 1; }
internal sealed class Fourth { public int Value = 1; }
internal struct Velocity { public Vector2 Value; }

internal static class Micro
{
    private const int Runs = 9;
    private static long _sink;

    /// <summary>Best-of-runs nanoseconds per inner operation, and bytes allocated per operation in the last run.</summary>
    private static (double Ns, double Bytes) Measure(int ops, Action body)
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
        return (best, (double)bytes / ops);
    }

    private static void Row(string what, int ops, Action body)
    {
        var (ns, bytes) = Measure(ops, body);
        Console.WriteLine($"| {what} | {ns:0.00} | {bytes:0.###} |");
    }

    private static World Populated(AttachmentStorage storage, int count, out EntityId[] ids, bool allTypes = false, int payloadEvery = 1)
    {
        var world = new World(storage);
        ids = new EntityId[count];
        for (int i = 0; i < count; i++)
        {
            ids[i] = world.Create();
            world.Local(ids[i]).Position2D = new Vector2(i, i * 0.5f);
            if (i % payloadEvery == 0) world.Attach(ids[i], new Payload { Value = i });
            if (allTypes)
            {
                world.Attach(ids[i], new Other());
                world.Attach(ids[i], new Third());
                world.AttachValue(ids[i], new Velocity { Value = Vector2.One });
            }
        }
        // Shuffle the access order (deterministically) so lookups are not sequential.
        var rng = new Random(7);
        for (int i = count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (ids[i], ids[j]) = (ids[j], ids[i]);
        }
        return world;
    }

    public static void Run()
    {
        const int N = 1000, Rounds = 200;
        var storages = new[] { AttachmentStorage.DenseStable, AttachmentStorage.PerEntityDictionary };

        Console.WriteLine();
        Console.WriteLine($"## Attachments ({N} entities, random access order)");
        Console.WriteLine();
        Console.WriteLine("| Operation | ns/op | bytes/op |");
        Console.WriteLine("| --- | ---: | ---: |");
        foreach (var storage in storages)
        {
            var world = Populated(storage, N, out var ids, allTypes: true);
            Row($"{storage}: TryGet<class> hit (4 types attached)", N * Rounds, () =>
            {
                long s = 0;
                for (int r = 0; r < Rounds; r++)
                    foreach (var id in ids)
                        if (world.TryGet<Payload>(id, out var p)) s += p.Value;
                _sink += s;
            });
            Row($"{storage}: TryGet<class> miss (type not attached)", N * Rounds, () =>
            {
                long s = 0;
                for (int r = 0; r < Rounds; r++)
                    foreach (var id in ids)
                        if (world.TryGet<Fourth>(id, out var p)) s += p.Value;
                _sink += s;
            });
            Row($"{storage}: Value<struct> by ref, read+write", N * Rounds, () =>
            {
                for (int r = 0; r < Rounds; r++)
                    foreach (var id in ids)
                        world.Value<Velocity>(id).Value.X += 1;
            });
            Row($"{storage}: All<class> iteration, every entity has it (per element)", N * Rounds, () =>
            {
                long s = 0;
                for (int r = 0; r < Rounds; r++)
                    foreach (var p in world.All<Payload>()) s += p.Value;
                _sink += s;
            });
            var sparse = Populated(storage, N, out _, allTypes: true, payloadEvery: 10);
            Row($"{storage}: All<class> iteration, 1 in 10 has it (per element found)", N / 10 * Rounds, () =>
            {
                long s = 0;
                for (int r = 0; r < Rounds; r++)
                    foreach (var p in sparse.All<Payload>()) s += p.Value;
                _sink += s;
            });
        }
        {
            var direct = new Payload[N];
            for (int i = 0; i < N; i++) direct[i] = new Payload { Value = i };
            Row("Baseline: a List-like array of class references (per element)", N * Rounds, () =>
            {
                long s = 0;
                for (int r = 0; r < Rounds; r++)
                    foreach (var p in direct) s += p.Value;
                _sink += s;
            });
        }

        Console.WriteLine();
        Console.WriteLine($"## Transforms, interpolation and hierarchy ({N} entities)");
        Console.WriteLine();
        Console.WriteLine("| Operation | ns/op | bytes/op |");
        Console.WriteLine("| --- | ---: | ---: |");
        {
            var world = Populated(AttachmentStorage.DenseStable, N, out var ids);
            var positions = new Vector2[N];
            var previous = new Vector2[N];
            for (int i = 0; i < N; i++) positions[i] = previous[i] = new Vector2(i, i);
            var inOrder = ids.OrderBy(id => id.Index).ToArray();
            Row("Local(id).Position2D read, ids in creation order", N * Rounds, () =>
            {
                float s = 0;
                for (int r = 0; r < Rounds; r++)
                    foreach (var id in inOrder) s += world.Local(id).Position2D.X;
                _sink += (long)s;
            });
            Row("Local(id).Position2D read, random order (Actor.Position get)", N * Rounds, () =>
            {
                float s = 0;
                for (int r = 0; r < Rounds; r++)
                    foreach (var id in ids) s += world.Local(id).Position2D.X;
                _sink += (long)s;
            });
            Row("Local(id).Position2D += delta (Actor.Position set)", N * Rounds, () =>
            {
                for (int r = 0; r < Rounds; r++)
                    foreach (var id in ids) world.Local(id).Position2D += new Vector2(0.001f, 0);
            });
            Row("Baseline: Vector2 field read from an array", N * Rounds, () =>
            {
                float s = 0;
                for (int r = 0; r < Rounds; r++)
                    for (int i = 0; i < N; i++) s += positions[i].X;
                _sink += (long)s;
            });
            world.BeginStep(1);
            foreach (var id in ids) world.Local(id).Position2D += new Vector2(1, 2);
            Row("DrawPosition2D(id, 0.5), root entity", N * Rounds, () =>
            {
                float s = 0;
                for (int r = 0; r < Rounds; r++)
                    foreach (var id in ids) s += world.DrawPosition2D(id, 0.5f).X;
                _sink += (long)s;
            });
            Row("Baseline: the game's own lerp (Actor.Blend over two Vector2 arrays)", N * Rounds, () =>
            {
                float s = 0;
                for (int r = 0; r < Rounds; r++)
                    for (int i = 0; i < N; i++)
                        s += (previous[i] == positions[i] ? positions[i] : Vector2.Lerp(previous[i], positions[i], 0.5f)).X;
                _sink += (long)s;
            });
            Row("WorldMatrix(id, 0.5), root entity", N * Rounds, () =>
            {
                float s = 0;
                for (int r = 0; r < Rounds; r++)
                    foreach (var id in ids) s += world.WorldMatrix(id, 0.5f).M41;
                _sink += (long)s;
            });
            Row("BeginStep (keep the step-start transforms), per entity", N * Rounds, () =>
            {
                for (int r = 0; r < Rounds; r++) world.BeginStep(r);
            });
            Row("Baseline: copying Vector2 positions, per entity", N * Rounds, () =>
            {
                for (int r = 0; r < Rounds; r++) Array.Copy(positions, previous, N);
            });
        }
        foreach (int depth in new[] { 1, 3 })
        {
            var world = new World();
            var ids = new EntityId[N];
            for (int i = 0; i < N; i++)
            {
                var parent = EntityId.None;
                for (int d = 0; d < depth; d++)
                {
                    var p = world.Create(parent);
                    world.Local(p).Position2D = new Vector2(d, d);
                    parent = p;
                }
                ids[i] = world.Create(parent);
            }
            world.BeginStep(1);
            foreach (var id in ids) world.Local(id).Position2D += Vector2.One;
            Row($"DrawPosition2D(id, 0.5), child at depth {depth}", N * Rounds, () =>
            {
                float s = 0;
                for (int r = 0; r < Rounds; r++)
                    foreach (var id in ids) s += world.DrawPosition2D(id, 0.5f).X;
                _sink += (long)s;
            });
        }

        Console.WriteLine();
        Console.WriteLine("## Entity churn (particle-like use)");
        Console.WriteLine();
        Console.WriteLine("| Operation | ns/op | bytes/op |");
        Console.WriteLine("| --- | ---: | ---: |");
        foreach (var storage in storages)
        {
            var world = new World(storage);
            var batch = new EntityId[N];
            var payload = new Payload();
            Row($"{storage}: Create + Attach<class> + Destroy + release, per entity (steady state)", N * 20, () =>
            {
                for (int r = 0; r < 20; r++)
                {
                    for (int i = 0; i < N; i++)
                    {
                        batch[i] = world.Create();
                        world.Attach(batch[i], payload);
                    }
                    for (int i = 0; i < N; i++) world.Destroy(batch[i]);
                    world.EndStep();
                }
            });
        }
        Console.WriteLine();
        Console.WriteLine($"(sink {_sink & 1})");
    }
}

internal static class GameRuns
{
    private static readonly (string Script, string? Start)[] Scripts =
    [
        ("fight.script", "road"), ("boss.script", "boss"), ("showcase.script", "blocks"), ("gameover.script", "ruins"),
        ("opening.script", null),
    ];

    public static void Run(string? only)
    {
        Console.WriteLine();
        Console.WriteLine("## Courtyard Key, whole frames (headless, deterministic, muted, captures removed from the scripts; update + draw per frame; one warm-up pass, then best of 7 interleaved rounds)");
        Console.WriteLine();
        Console.WriteLine("| Script | Frames | Original µs/frame | Port (dense) µs/frame | Port (dictionary) µs/frame | Original B/frame | Port (dense) B/frame | Port (dictionary) B/frame |");
        Console.WriteLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
        var configs = new (Func<string, string?, Game> Create, AttachmentStorage? Storage)[]
        {
            ((saves, start) => new original::CourtyardKey.CourtyardKeyGame(saves, start), null),
            ((saves, start) => new CourtyardKey.CourtyardKeyGame(saves, start), AttachmentStorage.DenseStable),
            ((saves, start) => new CourtyardKey.CourtyardKeyGame(saves, start), AttachmentStorage.PerEntityDictionary),
        };
        var selected = Scripts.Where(s => only is null || only == s.Script).ToArray();
        var best = new (long Frames, double Us, double Bytes)[selected.Length, configs.Length];
        for (int round = 0; round < 8; round++)
            for (int s = 0; s < selected.Length; s++)
                for (int c = 0; c < configs.Length; c++)
                {
                    var (frames, us, bytes) = Best(saves => configs[c].Create(saves, selected[s].Start), selected[s].Script, configs[c].Storage, 1);
                    if (round == 0) { best[s, c] = (frames, double.MaxValue, double.MaxValue); continue; }   // warm-up
                    best[s, c] = (frames, Math.Min(best[s, c].Us, us), Math.Min(best[s, c].Bytes, bytes));
                }
        for (int s = 0; s < selected.Length; s++)
        {
            var (o, d, p) = (best[s, 0], best[s, 1], best[s, 2]);
            Console.WriteLine($"| {selected[s].Script} | {o.Frames} | {o.Us:0.0} | {d.Us:0.0} | {p.Us:0.0} | {o.Bytes:0} | {d.Bytes:0} | {p.Bytes:0} |");
        }
    }

    private static string Saves() => Path.Combine(Path.GetTempPath(), "courtyard-p7-bench", Guid.NewGuid().ToString("N"));

    private static (long Frames, double Us, double Bytes) Best(Func<string, Game> create, string script, AttachmentStorage? storage, int runs)
    {
        StorageChoice.Override = storage;
        (long, double, double) best = (0, double.MaxValue, double.MaxValue);
        for (int run = 0; run < runs; run++)
        {
            string saves = Saves();
            var game = create(saves);
            string captures = Path.Combine(Path.GetTempPath(), "courtyard-p7-bench-captures", Guid.NewGuid().ToString("N"));
            using var platform = HeadlessPlatform.For(game.Settings);
            // Captures (PNG encoding) are left out: they dominate a frame's allocations and say nothing about the world.
            var text = string.Join('\n', File.ReadAllLines(Path.Combine(Root(), "games", "CourtyardKey", "scripts", script))
                .Where(line => !line.Contains(" capture ", StringComparison.Ordinal)));
            long before = GC.GetAllocatedBytesForCurrentThread();
            var sw = Stopwatch.StartNew();
            var result = GameRunner.Run(game, platform, new RunOptions
            {
                Script = InputScript.Parse(text), Seed = 1, Mute = true, CaptureDirectory = captures,
            });
            sw.Stop();
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            if (result.Error is not null) throw result.Error;
            if (Directory.Exists(captures)) Directory.Delete(captures, true);
            if (Directory.Exists(saves)) Directory.Delete(saves, true);
            double us = sw.Elapsed.TotalMicroseconds / result.Frames;
            if (us < best.Item2) best = (result.Frames, us, best.Item3);
            best.Item3 = Math.Min(best.Item3, (double)bytes / result.Frames);
        }
        StorageChoice.Override = null;
        return best;
    }

    private static string Root()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "MicroEngine.slnx"))) return dir.FullName;
        throw new InvalidOperationException("Repository root not found.");
    }
}

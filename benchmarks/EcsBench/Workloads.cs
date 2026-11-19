using System.Numerics;
using System.Runtime.CompilerServices;
using StressBench;

namespace EcsBench;

/// <summary>What a scenario does every step. Every store runs the same four.</summary>
internal enum Workload
{
    /// <summary>Many alike objects (projectiles, particles, a swarm): each one moved and bounced, drawn interpolated.</summary>
    Move,
    /// <summary>The same objects with a lifetime of ten steps: a tenth destroyed and created again every step.</summary>
    Churn,
    /// <summary>Objects with different component sets, asked for three combinations every step.</summary>
    Query,
    /// <summary>A game step: a swarm chasing a point and dying near it, half as many shots with a lifetime, both drawn.</summary>
    Mixed,
}

/// <summary>The stores compared.</summary>
internal enum Store
{
    /// <summary>The engine's world: entities with class attachments, pooled where the scenario respawns.</summary>
    Simple,
    /// <summary>The engine's world with a new attachment per spawn (no pool): what the simplest game code does.</summary>
    SimpleAlloc,
    /// <summary>Arch 2.1.0.</summary>
    Arch,
    /// <summary>Friflo.Engine.ECS 3.6.0.</summary>
    Friflo,
    /// <summary>The first-party archetype prototype (<see cref="Prototype.MiniWorld"/>).</summary>
    Proto,
}

/// <summary>
/// The rules of the workloads, shared by every store so they all do the same arithmetic: what an object starts with
/// (from its serial number, never from the order a store walks in), what a step does to one object, and the hash of
/// one object. A scenario's checksum is the sum of its objects' hashes, so stores that walk in different orders end in
/// the same checksum exactly when they did the same work.
/// </summary>
internal static class Rules
{
    public const float Width = 320, Height = 180;
    public const int Lifetime = 10, ShotLife = 30, DangerRadius = 20;
    public const float MaxSpeed = 90, Pull = 120;

    public static uint Hash(uint x)
    {
        x ^= x >> 16; x *= 0x7feb352d;
        x ^= x >> 15; x *= 0x846ca68b;
        return x ^ (x >> 16);
    }

    public static Vector2 Place(uint serial) => new(Hash(serial * 2 + 1) % 3200 / 10f, Hash(serial * 2 + 2) % 1800 / 10f);
    public static Vector2 Speed(uint serial) => new((int)(Hash(serial * 3 + 7) % 181) - 90, (int)(Hash(serial * 5 + 11) % 181) - 90);
    public static int AgentHealth(uint serial) => 20 + (int)(serial % 20);
    public static int ShotLifeOf(uint serial) => ShotLife + (int)(serial % ShotLife);

    /// <summary>The point the swarm chases at step <paramref name="step"/>.</summary>
    public static Vector2 Target(long step) => new(Width / 2 + MathF.Cos(step * 0.05f) * 60, Height / 2 + MathF.Sin(step * 0.05f) * 60);

    /// <summary>Moves and bounces off the canvas edges.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Move(ref Vector2 position, ref Vector2 velocity, float dt)
    {
        var p = position + velocity * dt;
        if (p.X < 0 || p.X > Width) velocity.X = -velocity.X;
        if (p.Y < 0 || p.Y > Height) velocity.Y = -velocity.Y;
        position = Vector2.Clamp(p, Vector2.Zero, new Vector2(Width, Height));
    }

    /// <summary>Steers towards <paramref name="target"/> and moves; true when the object is within the danger radius.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Chase(ref Vector2 position, ref Vector2 velocity, Vector2 target, float dt)
    {
        var d = target - position;
        float distance2 = d.LengthSquared();
        if (distance2 > 1) velocity += d / MathF.Sqrt(distance2) * (Pull * dt);
        float speed2 = velocity.LengthSquared();
        if (speed2 > MaxSpeed * MaxSpeed) velocity *= MaxSpeed / MathF.Sqrt(speed2);
        position += velocity * dt;
        return distance2 < DangerRadius * DangerRadius;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long HashOf(Vector2 position, int extra)
    {
        long h = unchecked((long)14695981039346656037UL);
        h = unchecked((h ^ BitConverter.SingleToInt32Bits(position.X)) * 1099511628211L);
        h = unchecked((h ^ BitConverter.SingleToInt32Bits(position.Y)) * 1099511628211L);
        return unchecked((h ^ extra) * 1099511628211L);
    }

    /// <summary>The position drawn at <paramref name="blend"/> between the step's start and its end.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Drawn(Vector2 previous, Vector2 current, float blend) =>
        blend >= 1 || previous == current ? current : Vector2.Lerp(previous, current, blend);

    // The component sets of the query workload, by serial: everything has a position.
    public static bool HasVelocity(int i) => i % 2 == 0;
    public static bool HasHealth(int i) => i % 3 == 0;
    public static bool HasBurning(int i) => i % 5 == 0;
    public static int StartHealth(int i) => 100 - i % 50;
    public static int BurnOf(int i) => 1 + i % 3;
}

/// <summary>A scenario of this program: one workload on one store. The sections are the same for every store.</summary>
internal abstract class EcsScene(int load, Workload workload) : StressScene(load, SectionsOf(workload))
{
    protected Workload Workload { get; } = workload;
    /// <summary>The next serial number a respawn uses.</summary>
    protected uint Next;
    /// <summary>The steps run.</summary>
    protected long Steps;
    /// <summary>What the query workload found, summed over the steps.</summary>
    protected long Found;

    private static string[] SectionsOf(Workload workload) => workload switch
    {
        Workload.Move => ["move"],
        Workload.Churn => ["update", "destroy", "create"],
        Workload.Query => ["pos-vel", "health-burning", "pos-vel-health"],
        _ => ["agents", "shots", "churn"],
    };

    /// <summary>Objects alive now.</summary>
    protected abstract int Live { get; }
    /// <summary>The sum of <see cref="Rules.HashOf"/> over every live object.</summary>
    protected abstract long Sum();

    public sealed override long Checksum() => Mix(Mix(Mix(Mix(Seed, Live), Sum()), Next), Found);

    public sealed override void Count(IDictionary<string, double> resources)
    {
        resources["entities"] = Live;
        resources["entitiesCreated"] = Next;
    }
}

/// <summary>The scenarios this program adds to the harness: <c>ecs-&lt;workload&gt;-&lt;store&gt;</c>.</summary>
public static class EcsScenarios
{
    private static readonly (Workload Workload, string Name, int Load, string What)[] Workloads =
    [
        (Workload.Move, "move", 100_000, "alike objects, each moved, bounced and drawn interpolated"),
        (Workload.Churn, "churn", 100_000, "the same objects with a lifetime of ten steps: a tenth destroyed and created again every step"),
        (Workload.Query, "query", 100_000, "objects with four different component sets, asked for three combinations every step"),
        (Workload.Mixed, "mixed", 50_000, "a swarm chasing a point and dying near it, half as many shots with a lifetime, all drawn"),
    ];

    private static readonly (Store Store, string Name, string What)[] Stores =
    [
        (Store.Simple, "simple", "the engine's world, attachments pooled"),
        (Store.SimpleAlloc, "simple-alloc", "the engine's world, a new attachment per spawn"),
        (Store.Arch, "arch", "Arch 2.1.0"),
        (Store.Friflo, "friflo", "Friflo.Engine.ECS 3.6.0"),
        (Store.Proto, "proto", "the first-party archetype prototype"),
    ];

    /// <summary>The names of the scenarios of one workload (<c>move</c>, <c>churn</c>, <c>query</c>, <c>mixed</c>), one per store.</summary>
    public static IReadOnlyList<string> Of(string workload) =>
        [.. Stores.Where(s => Applies(s.Store, workload)).Select(s => $"ecs-{workload}-{s.Name}")];

    /// <summary>Every workload's name.</summary>
    public static IReadOnlyList<string> WorkloadNames { get; } = [.. Workloads.Select(w => w.Name)];

    // A new attachment per spawn differs from the pooled world only where objects are spawned during the run.
    private static bool Applies(Store store, string workload) => store != Store.SimpleAlloc || workload is "churn" or "mixed";

    /// <summary>Adds the scenarios to the harness; calling it again adds nothing.</summary>
    public static void Register()
    {
        foreach (var w in Workloads)
            foreach (var s in Stores)
            {
                if (!Applies(s.Store, w.Name)) continue;
                var (workload, store) = (w.Workload, s.Store);
                Scenarios.Add($"ecs-{w.Name}-{s.Name}", w.Load, $"{w.What}; on {s.What}", load => store switch
                {
                    Store.Simple => new SimpleScene(load, workload, pooled: true),
                    Store.SimpleAlloc => new SimpleScene(load, workload, pooled: false),
                    Store.Arch => new ArchScene(load, workload),
                    Store.Friflo => new FrifloScene(load, workload),
                    _ => new ProtoScene(load, workload),
                });
            }
    }
}

using System.Diagnostics;

namespace FsmCompare;

/// <summary>
/// Runs the three enemies through one script and reports whether they behaved the same, what each allocates per step
/// and how long a step takes. <c>dotnet run -c Release --project benchmarks/FsmCompare</c>.
/// </summary>
public static class Compare
{
    public const float Dt = 1f / 60f;

    /// <summary>What the world tells the enemy at <paramref name="step"/>: the player wanders in and out of reach, hits now and then, and the enemy is killed at <paramref name="killAt"/>.</summary>
    public static Sense SenseAt(int step, int killAt)
    {
        float distance = 150f + 145f * MathF.Cos(step * 0.021f);
        return new Sense(distance, Hit: step % 97 == 96, Kill: step == killAt);
    }

    /// <summary>One line per step of what a game would observe of the enemy.</summary>
    public static List<string> Trace(IEnemy enemy, int steps, int killAt)
    {
        var trace = new List<string>(steps);
        for (int i = 0; i < steps; i++)
        {
            enemy.Step(SenseAt(i, killAt), Dt);
            // Time to the millisecond: one version sums floats and another keeps a double clock.
            trace.Add($"{i} {enemy.Mode} {MathF.Round(enemy.TimeInMode * 1000f)} {(enemy.HitboxLive ? "live" : "-")} {enemy.Attacks}");
        }
        return trace;
    }

    public static (double BytesPerStep, double NanosecondsPerStep) Measure(IEnemy enemy, int steps)
    {
        // Twice, with a pause between: the runtime compiles the optimized code in the background.
        for (int i = 0; i < 200_000; i++) enemy.Step(SenseAt(i, -1), Dt);
        Thread.Sleep(300);
        for (int i = 0; i < 200_000; i++) enemy.Step(SenseAt(i, -1), Dt);
        long before = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < steps; i++) enemy.Step(SenseAt(i, -1), Dt);
        watch.Stop();
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        return ((double)bytes / steps, watch.Elapsed.TotalNanoseconds / steps);
    }

    public static int Main()
    {
        const int steps = 2400, killAt = 2000;
        var hand = Trace(new HandEnemy(), steps, killAt);
        var stateless = Trace(new StatelessEnemy(), steps, killAt);
        var engine = Trace(new EngineEnemy(), steps, killAt);
        bool same = hand.SequenceEqual(stateless) && hand.SequenceEqual(engine);
        Console.WriteLine($"same behaviour over {steps} steps: {same}");
        if (!same)
        {
            int at = Enumerable.Range(0, steps).First(i => hand[i] != stateless[i] || hand[i] != engine[i]);
            Console.WriteLine($"first difference at step {at}:\n  hand      {hand[at]}\n  stateless {stateless[at]}\n  engine    {engine[at]}");
        }
        foreach (var (name, enemy) in new (string, IEnemy)[] { ("by hand", new HandEnemy()), ("Stateless", new StatelessEnemy()), ("engine", new EngineEnemy()) })
        {
            var (bytes, ns) = Measure(enemy, 1_000_000);
            Console.WriteLine($"{name,-10} {bytes,8:0.0} bytes/step {ns,8:0.0} ns/step");
        }
        return same ? 0 : 1;
    }
}

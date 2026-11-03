using P6;

// ARCH-1 P6 desktop harness.
//   p6 bench <native|managed> [boxes=500] [steps=600] [runs=3]   one warm-up run, then <runs> measured runs
//   p6 contract <native|managed>                                  the common-contract checks
//   p6 state <native|managed> [boxes=500] [steps=600]              final state hash only (cross-process determinism)
//   p6 raw                                                        Box2D's own C API called with by-value structs
string mode = args.Length > 0 ? args[0] : "contract";
string which = args.Length > 1 ? args[1] : "native";
Func<IBackend> factory = which == "managed" ? () => new ManagedBackend() : () => new NativeBackend();
int Arg(int i, int fallback) => args.Length > i ? int.Parse(args[i]) : fallback;

Console.WriteLine($"# {Environment.OSVersion} | {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription} | " +
                  $"{System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture} | cores {Environment.ProcessorCount}");
switch (mode)
{
    case "bench":
    {
        int boxes = Arg(2, 500), steps = Arg(3, 600), runs = Arg(4, 3);
        for (int run = 0; run <= runs; run++)
        {
            var r = Scenarios.Bench(factory, boxes, steps);
            Console.WriteLine($"{(run == 0 ? "warmup" : "run" + run)} {r.Backend} box2d={r.Version} boxes={r.Boxes} steps={r.Steps} " +
                              $"mean={r.MeanMs:F3}ms median={r.MedianMs:F3}ms p95={r.P95Ms:F3}ms max={r.MaxMs:F3}ms " +
                              $"total={r.TotalMs:F0}ms hash={r.FinalHash}");
            if (run == runs) Console.WriteLine($"end state {r.Counters}; checkpoints {r.Checkpoints}");
        }
        break;
    }
    case "state":
    {
        var r = Scenarios.Bench(factory, Arg(2, 500), Arg(3, 600));
        Console.WriteLine($"hash {r.Backend} box2d={r.Version} {r.FinalHash} checkpoints {r.Checkpoints}");
        break;
    }
    case "raw":
        Console.WriteLine(RawProbe.Run());
        break;
    default:
    {
        var report = Scenarios.Contract(factory);
        foreach (string line in report.Lines) Console.WriteLine(line);
        Console.WriteLine($"contract {which}: {report.Passed} passed, {report.Failed} failed");
        break;
    }
}

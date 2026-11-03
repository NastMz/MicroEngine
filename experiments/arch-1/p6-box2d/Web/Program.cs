using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using P6;

[assembly: SupportedOSPlatform("browser")]

// Main only brings the runtime up; main.js calls the exports one by one (each returns text) so a crash in one
// still leaves the earlier results on the page.
Console.WriteLine("p6web: ready");

public static partial class P6Web
{
    private static Func<IBackend> Factory(string which) =>
        which == "managed" ? () => new ManagedBackend() : () => new NativeBackend();

    [JSExport]
    public static string Info()
    {
        using var n = new NativeBackend();
        using var m = new ManagedBackend();
        return $"{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription} " +
               $"{System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture} " +
#if P6_AOT
               "managed code: AOT " +
#else
               "managed code: interpreter " +
#endif
               $"native box2d={n.Version} managed box2d={m.Version}";
    }

    [JSExport]
    public static string Contract(string which)
    {
        var report = Scenarios.Contract(Factory(which));
        return string.Join("\n", report.Lines) + $"\ncontract {which}: {report.Passed} passed, {report.Failed} failed";
    }

    [JSExport]
    public static string Bench(string which, int boxes, int steps, int run)
    {
        var r = Scenarios.Bench(Factory(which), boxes, steps);
        return $"{(run == 0 ? "warmup" : "run" + run)} {r.Backend} box2d={r.Version} boxes={r.Boxes} steps={r.Steps} " +
               $"mean={r.MeanMs:F3}ms median={r.MedianMs:F3}ms p95={r.P95Ms:F3}ms max={r.MaxMs:F3}ms total={r.TotalMs:F0}ms " +
               $"hash={r.FinalHash}\nend state {r.Counters}; checkpoints {r.Checkpoints}";
    }

    [JSExport]
    public static string Raw() => RawProbe.Run(step => Console.WriteLine("raw: calling " + step));
}

using Android.App;
using Android.OS;
using Android.Widget;

namespace P6;

/// <summary>Runs the contract checks and the benchmark on both backends; every line goes to logcat (tag P6).</summary>
[Activity(Label = "P6 Box2D", MainLauncher = true)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var text = new TextView(this) { Text = "running…" };
        var scroll = new ScrollView(this);
        scroll.AddView(text);
        SetContentView(scroll);
        var lines = new List<string>();
        void Out(string s)
        {
            foreach (string l in s.Split('\n')) Android.Util.Log.Info("P6", l);
            lines.Add(s);
            RunOnUiThread(() => text.Text = string.Join("\n", lines));
        }
        new Thread(() =>
        {
            try
            {
                Out($"P6 start {Build.Model} {Build.SupportedAbis?.FirstOrDefault()} Android {Build.VERSION.Release} " +
                    $"{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription} " +
                    $"{System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
                foreach (string which in new[] { "native", "managed" })
                {
                    Func<IBackend> f = which == "managed" ? () => new ManagedBackend() : () => new NativeBackend();
                    var report = Scenarios.Contract(f);
                    Out(string.Join("\n", report.Lines) + $"\ncontract {which}: {report.Passed} passed, {report.Failed} failed");
                    for (int run = 0; run <= 3; run++)
                    {
                        var r = Scenarios.Bench(f);
                        Out($"{(run == 0 ? "warmup" : "run" + run)} {r.Backend} box2d={r.Version} boxes={r.Boxes} steps={r.Steps} " +
                            $"mean={r.MeanMs:F3}ms median={r.MedianMs:F3}ms p95={r.P95Ms:F3}ms max={r.MaxMs:F3}ms " +
                            $"total={r.TotalMs:F0}ms hash={r.FinalHash}\nend state {r.Counters}; checkpoints {r.Checkpoints}");
                    }
                }
                Out(RawProbe.Run());
                Out("P6 done");
            }
            catch (Exception e)
            {
                Out("P6 error " + e);
            }
        }).Start();
    }
}

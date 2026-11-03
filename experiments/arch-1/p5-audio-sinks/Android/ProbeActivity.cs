using Android.App;
using Android.Util;
using Org.Libsdl.App;
using P5.Audio;

namespace P5;

/// <summary>
/// SDL's activity, as in the repository's Android host: SDL owns the surface and audio, and runs <see cref="Main"/> on its
/// own thread. Extras: <c>mode</c> (pull, push, feeder, offline, cost), <c>target</c> (ms). Writes one JSON line to logcat
/// (tag P5) and exits.
/// </summary>
[Activity(Name = "dev.microengine.p5.ProbeActivity", Label = "P5 audio", MainLauncher = true, Exported = true)]
public sealed class ProbeActivity : SDLActivity
{
    protected override string[] GetLibraries() => ["SDL3"];

    protected override void Main()
    {
        string mode = Intent?.GetStringExtra("mode") ?? "pull";
        int target = Intent?.GetIntExtra("target", 50) ?? 50;
        string report;
        string release = global::Android.OS.Build.VERSION.Release ?? "?", model = global::Android.OS.Build.Model ?? "?";
        int api = (int)global::Android.OS.Build.VERSION.SdkInt;
        string runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        try
        {
            report = mode switch
            {
                "offline" => Determinism.Run(),
                "cost" => Cost.Run(new AudioFormat(48000, 2), target, 1024),
                _ => Runner.Run(mode, target, $"android {release} api {api} {model} {runtime}"),
            };
        }
        catch (Exception e)
        {
            report = $"{{\"error\":\"{e.GetType().Name}: {e.Message.Replace("\"", "'")}\"}}";
        }
        for (int i = 0; i < report.Length; i += 3000)
            Log.Info("P5", (i == 0 ? "BEGIN " : "MORE ") + report.Substring(i, Math.Min(3000, report.Length - i)));
        Log.Info("P5", "END");
    }
}

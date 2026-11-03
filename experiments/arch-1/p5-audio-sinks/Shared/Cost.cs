using System.Diagnostics;
using System.Globalization;

namespace P5.Audio;

/// <summary>What one render call costs: the bed plus <c>voices</c> continuous tones (Math.Sin per sample), <c>frames</c> per call.</summary>
public static class Cost
{
    public static string Run(AudioFormat format, int voices, int frames, int calls = 200)
    {
        var mixer = new ProbeMixer(format) { LoadVoices = voices };
        var buffer = new float[frames * format.Channels];
        for (int i = 0; i < 10; i++) mixer.Render(buffer, 0); // warm up
        var times = new double[calls];
        for (int i = 0; i < calls; i++)
        {
            long t = Stopwatch.GetTimestamp();
            mixer.Render(buffer, 0);
            times[i] = (Stopwatch.GetTimestamp() - t) * 1000.0 / Stopwatch.Frequency;
        }
        Array.Sort(times);
        double audioMs = format.FramesToMs(frames);
        string F(double d) => Math.Round(d, 3).ToString(CultureInfo.InvariantCulture);
        return $"{{\"voices\":{voices},\"frames\":{frames},\"audioMs\":{F(audioMs)},\"renderMsMedian\":{F(times[calls / 2])},\"renderMsMax\":{F(times[^1])},\"cpuShare\":{F(times[calls / 2] / audioMs)}}}";
    }
}

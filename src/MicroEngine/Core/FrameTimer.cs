using System.Diagnostics;

namespace MicroEngine.Core;

/// <summary>The default precise wait of <see cref="IPlatform.Wait"/>.</summary>
internal static class FrameTimer
{
    /// <summary>
    /// Sleeps while more than a coarse timer tick remains (a sleep can last up to ~16 ms on Windows without a raised
    /// timer resolution), then yields until the time is up: precise, at the cost of a busy core for the last tick. For
    /// platforms without a precise timer of their own; SDL and raylib use theirs.
    /// </summary>
    public static void Delay(double seconds)
    {
        if (seconds <= 0) return;
        long due = Stopwatch.GetTimestamp() + (long)(seconds * Stopwatch.Frequency);
        const double Coarse = 0.017;
        while (true)
        {
            double left = (due - Stopwatch.GetTimestamp()) / (double)Stopwatch.Frequency;
            if (left <= 0) return;
            if (left > Coarse) Thread.Sleep(TimeSpan.FromSeconds(left - Coarse));
            else Thread.Yield();
        }
    }
}

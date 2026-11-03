using System.Diagnostics;
using static SDL.SDL3;

namespace P5.Audio;

/// <summary>The blocking 60 Hz game loop of desktop and Android, around one <see cref="Scenario"/>.</summary>
public static class Runner
{
    /// <summary>
    /// <paramref name="mode"/>: <c>pull</c>, <c>push</c> (main loop tops up to <paramref name="targetMs"/>) or
    /// <c>feeder</c> (a sink-owned thread tops up to <paramref name="targetMs"/>). Returns one JSON object.
    /// </summary>
    public static string Run(string mode, int targetMs, string platform)
    {
        var format = new AudioFormat(48000, 2);
        var mixer = new ProbeMixer(format);
        int target = format.SampleRate * targetMs / 1000;
        SdlSink sdl = mode switch
        {
            "pull" => SdlPullSink.Create(format),
            "push" or "feeder" => SdlPushSink.Create(format, target),
            _ => throw new ArgumentException($"unknown mode {mode}"),
        };
        if (sdl.Error is not null) return $"{{\"error\":\"{sdl.Error}\"}}";
        IAudioSink sink = mode == "feeder" ? new FeederSink((SdlPushSink)sdl) : sdl;
        var run = new Scenario(sink, mixer);
        run.Start();
        var clock = Stopwatch.StartNew();
        long frame = 0;
        while (run.Frame())
        {
            frame++;
            double wait = frame * 1000.0 / 60 - clock.Elapsed.TotalMilliseconds;
            if (wait > 0) Thread.Sleep(TimeSpan.FromMilliseconds(wait));
        }
        string report = run.Report(platform, $"\"sdlCallbacks\":{sdl.Callbacks}");
        sink.Dispose();
        SDL_QuitSubSystem(SDL.SDL_InitFlags.SDL_INIT_AUDIO);
        return report;
    }
}

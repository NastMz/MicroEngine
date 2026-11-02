using System.Diagnostics;
using MicroEngine.Audio;

namespace MicroEngine.NativeChecks;

/// <summary>
/// Plays a 440 Hz tone through an engine audio device and records the monitor of a PulseAudio sink with
/// <c>parec</c>, then checks that the sink was silent before and carried that tone after: the signal path from the
/// engine to the sound server, not what a listener hears. Shared by the raylib and SDL tests (linked into
/// MicroEngine.Desktop.Tests); the native CI job names the monitor in MICROENGINE_AUDIO_MONITOR.
/// </summary>
internal static class SinkTone
{
    private const int Rate = 44100;
    private const float Frequency = 440;

    /// <summary>Plays a 0.5 sine as a sound effect or as music streamed from its samples, and asserts the sink hears it.</summary>
    public static void AssertReachesTheSink(IAudioDevice device, string monitor, bool music)
    {
        var tone = new float[Rate];
        for (int i = 0; i < tone.Length; i++) tone[i] = 0.5f * MathF.Sin(2 * MathF.PI * Frequency * i / Rate);
        var clip = new AudioClip(tone);
        var sound = music ? null : device.LoadSound("tone", clip);
        var song = music ? device.LoadMusic("tone", clip, loop: false) : null;

        using var recorder = Process.Start(new ProcessStartInfo("parec", $"--device={monitor} --raw --format=s16le --channels=1 --rate={Rate}")
        {
            RedirectStandardOutput = true, RedirectStandardError = true,
        }) ?? throw new InvalidOperationException("parec did not start");
        var recorded = new MemoryStream();
        var reading = recorder.StandardOutput.BaseStream.CopyToAsync(recorded);
        // The recorder connects asynchronously: play only once a quarter second of the sink's output has arrived.
        for (var wait = Stopwatch.StartNew(); recorded.Length < Rate / 2 && wait.ElapsedMilliseconds < 5000;) Thread.Sleep(20);
        long silenceEnds = recorded.Length;
        Assert.IsTrue(silenceEnds >= Rate / 2, $"The sink monitor delivers audio before the tone ({silenceEnds} bytes in 5 s).");
        if (sound is not null) device.Play(sound);
        else device.PlayMusic(song!);
        for (var watch = Stopwatch.StartNew(); watch.ElapsedMilliseconds < 1500;) { device.Update(); Thread.Sleep(10); }
        recorder.Kill();
        reading.Wait(2000);

        var samples = recorded.ToArray();
        Assert.IsTrue(samples.Length > Rate, $"parec recorded {samples.Length} bytes: {recorder.StandardError.ReadToEnd()}");
        double before = Rms(samples, 0, silenceEnds);
        var (loudest, at) = Enumerable.Range(0, (int)((samples.Length - silenceEnds) / 8820))
            .Select(i => (Rms: Rms(samples, silenceEnds + i * 8820L, silenceEnds + (i + 1) * 8820L), Start: silenceEnds + i * 8820L))
            .MaxBy(w => w.Rms);
        Assert.IsTrue(before < 0.01, $"Silence before the tone (RMS {before:F4}).");
        Assert.IsTrue(loudest > 0.1, $"The tone is heard on the sink (loudest 100 ms RMS {loudest:F4}, a 0.5 sine is about 0.35).");
        double frequency = ZeroCrossings(samples, at, at + 8820) / 2.0 / 0.1;
        Assert.AreEqual(Frequency, frequency, Frequency * 0.05, "And it is the tone that was played.");
    }

    private static short Sample(byte[] pcm, long i) => (short)(pcm[i] | pcm[i + 1] << 8);

    private static double Rms(byte[] pcm, long from, long to)
    {
        double sum = 0;
        long count = 0;
        for (long i = from & ~1L; i + 1 < Math.Min(to, pcm.Length); i += 2, count++) sum += Math.Pow(Sample(pcm, i) / 32768.0, 2);
        return count == 0 ? 0 : Math.Sqrt(sum / count);
    }

    private static int ZeroCrossings(byte[] pcm, long from, long to)
    {
        int crossings = 0;
        for (long i = (from & ~1L) + 2; i + 1 < Math.Min(to, pcm.Length); i += 2)
            if (Sample(pcm, i - 2) < 0 != Sample(pcm, i) < 0) crossings++;
        return crossings;
    }
}

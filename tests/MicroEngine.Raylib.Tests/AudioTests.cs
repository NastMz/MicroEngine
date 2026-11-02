using System.Diagnostics;
using MicroEngine.Audio;

namespace MicroEngine.Raylib.Tests;

/// <summary>
/// Two separate claims: the raylib audio device opens on this system's sound server, and a sound played through it
/// actually comes out. The second records the monitor of a virtual PulseAudio sink (MICROENGINE_AUDIO_MONITOR, set up
/// by the native CI job) and looks for the tone that was played; it proves the signal path, not what a listener hears.
/// </summary>
[TestClass]
[TestCategory("Native")]
[DoNotParallelize]
public sealed class AudioTests
{
    private const int Rate = 44100;
    private const float Frequency = 440;

    [TestMethod]
    public void The_raylib_audio_device_opens()
    {
        var device = RaylibAudioDevice.TryCreate();
        try { Native.Require(device.IsAvailable, "No audio output device (the engine fell back to silence)."); }
        finally { (device as IDisposable)?.Dispose(); }
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Linux, IgnoreMessage = "Signal capture uses a PulseAudio sink monitor.")]
    public void A_played_tone_reaches_the_sound_server() => ToneReachesTheSoundServer(music: false);

    [TestMethod]
    [OSCondition(OperatingSystems.Linux, IgnoreMessage = "Signal capture uses a PulseAudio sink monitor.")]
    public void A_tone_streamed_as_music_reaches_the_sound_server() => ToneReachesTheSoundServer(music: true);

    /// <summary>Plays a 0.5 sine as a sound effect or as music streamed from its samples, and checks the sink hears it.</summary>
    private static void ToneReachesTheSoundServer(bool music)
    {
        var monitor = Environment.GetEnvironmentVariable("MICROENGINE_AUDIO_MONITOR");
        Native.Require(!string.IsNullOrEmpty(monitor), "MICROENGINE_AUDIO_MONITOR names no PulseAudio monitor source.");
        var device = RaylibAudioDevice.TryCreate();
        try
        {
            Native.Require(device.IsAvailable, "No audio output device.");
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
        finally { (device as IDisposable)?.Dispose(); }
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

using MicroEngine.Audio;
using MicroEngine.Platform;

namespace MicroEngine.Tests.Audio;

/// <summary>
/// A mix that uses no effect, no ramp and no fade-in renders the same bits as the mixer did before buses had submixes:
/// the hashes below were recorded from that mixer (voices added straight into the output with the product of their bus
/// volumes), over nested buses whose volumes change, pans, pitches, loops, stops with fades, pauses and a stream, for a
/// stereo and a mono sink and several block sizes.
/// </summary>
[TestClass]
public sealed class AudioGraphBaselineTests
{
    private const int Rate = 48000, Steps = 60;

    /// <summary>Recorded from the mixer without submixes (commit d99ce8c).</summary>
    private const string StereoReference = "b0b529ac29b66037", MonoReference = "4deb7b9708c708d7";

    [TestMethod]
    [DataRow(2, 64)]
    [DataRow(2, 1024)]
    [DataRow(2, 4096)]
    [DataRow(1, 441)]
    [DataRow(1, 2048)]
    public void A_graph_without_effects_renders_the_same_bits_as_before_submixes(int channels, int block)
    {
        var sink = new OfflineAudioSink(new AudioFormat(Rate, channels), Steps, block);
        using var engine = new AudioEngine(sink, new AudioEngineSettings { MaxVoices = 12, UpdateRate = Steps, DelayFrames = 300 });
        Scenario(engine);
        Assert.AreEqual(channels == 2 ? StereoReference : MonoReference, Hash(sink.Samples));
    }

    /// <summary>The same calls every time: what a game without effects does to the mixer.</summary>
    internal static void Scenario(AudioEngine engine)
    {
        var ambience = engine.AddBus("ambience", AudioEngine.SfxBus);
        var wind = engine.AddBus("wind", "ambience");
        var ui = engine.AddBus("ui");
        string[] buses = [AudioEngine.SfxBus, "ambience", "wind", "ui", AudioEngine.MusicBus];
        var rng = new Random(11);
        var voices = new List<VoiceHandle>();
        engine.Play(new Ramp(Rate * 3), new PlayParameters { Volume = 0.3f, Bus = AudioEngine.MusicBus, Loop = true, Priority = 100 });
        for (int step = 0; step < 150; step++)
        {
            if (step % 3 == 0)
            {
                var clip = Tone(40 + rng.Next(900), 500 + rng.Next(9000), rng.Next(2) == 0 ? Rate : 22050, 1 + rng.Next(2));
                voices.Add(engine.Play(clip, new PlayParameters
                {
                    Volume = 0.2f + (float)rng.NextDouble() * 0.8f, Pitch = 0.5f + (float)rng.NextDouble() * 1.5f, Pan = (float)rng.NextDouble(),
                    Loop = rng.Next(5) == 0, Bus = buses[rng.Next(buses.Length)], Priority = rng.Next(3),
                }));
            }
            if (step % 7 == 0) engine.Bus(buses[rng.Next(buses.Length)]).Volume = (float)rng.NextDouble() * 1.5f;
            if (step % 11 == 0) engine.Bus(AudioEngine.MasterBus).Volume = 0.5f + (float)rng.NextDouble() * 0.5f;
            if (step % 13 == 0) wind.Volume = ambience.Volume * 0.5f + ui.Volume * 0.25f;
            if (voices.Count > 0)
            {
                var voice = voices[rng.Next(voices.Count)];
                switch (rng.Next(6))
                {
                    case 0: engine.Stop(voice, rng.NextDouble() * 0.2); break;
                    case 1: engine.Set(voice, volume: (float)rng.NextDouble(), pan: (float)rng.NextDouble()); break;
                    case 2: engine.SetPitch(voice, 0.5f + (float)rng.NextDouble()); break;
                    case 3: engine.Pause(voice, rng.Next(2) == 0); break;
                }
            }
            engine.Advance();
            engine.Pump();
        }
    }

    private static AudioClip Tone(double frequency, int frames, int rate, int channels)
    {
        var samples = new float[frames * channels];
        for (int f = 0; f < frames; f++)
            for (int c = 0; c < channels; c++)
                samples[f * channels + c] = 0.04f * MathF.Sin((float)(2 * Math.PI * frequency * f / rate) + c);
        return new AudioClip(samples, rate, channels);
    }

    internal static string Hash(IReadOnlyList<float> samples)
    {
        ulong h = 14695981039346656037;
        foreach (var s in samples) h = (h ^ BitConverter.SingleToUInt32Bits(s)) * 1099511628211;
        return h.ToString("x16");
    }

    /// <summary>A stereo stream at another rate than the sink's: a slow ramp, so resampling shows in the bits.</summary>
    private sealed class Ramp(int frames) : IAudioStream
    {
        private int _at;
        public int Channels => 2;
        public int SampleRate => 32000;
        public int Read(Span<float> buffer)
        {
            int n = Math.Min(buffer.Length / 2, frames - _at);
            for (int i = 0; i < n; i++, _at++)
            {
                buffer[i * 2] = 0.05f * (_at % 500) / 500f;
                buffer[i * 2 + 1] = -0.05f * (_at % 300) / 300f;
            }
            return n * 2;
        }
        public void Rewind() => _at = 0;
        public void Dispose() { }
    }
}

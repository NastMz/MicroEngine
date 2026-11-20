using MicroEngine.Audio;
using MicroEngine.Audio.Effects;
using MicroEngine.Platform;

namespace MicroEngine.Tests.Audio;

/// <summary>
/// The built-in effects, each rendered through the mixer and compared with a reference computed here: the response of
/// the filter's design at a frequency, the compressor's curve, the echoes of an impulse, the decay of the room.
/// </summary>
/// <remarks>
/// Declared tolerances: 0.5 dB for filter responses (measured over whole periods after the filter settled), 2 % for
/// compressor levels after ten time constants, 1e-6 for echoes (products of exact gains).
/// </remarks>
[TestClass]
public sealed class AudioEffectsTests
{
    private const int Rate = 48000, Steps = 60, StepFrames = Rate / Steps;

    private static AudioClip Sine(double frequency, double seconds, float amplitude = 0.5f)
    {
        var samples = new float[(int)(seconds * Rate)];
        for (int i = 0; i < samples.Length; i++) samples[i] = amplitude * (float)Math.Sin(2 * Math.PI * frequency * i / Rate);
        return new AudioClip(samples, Rate);
    }

    private static AudioClip Constant(float value, double seconds) => new([.. Enumerable.Repeat(value, (int)(seconds * Rate))], Rate);

    private static AudioClip Impulse(float value = 1) => new([value], Rate);

    private sealed class Rig : IDisposable
    {
        public readonly OfflineAudioSink Sink = new(new AudioFormat(Rate, 2), Steps);
        public readonly AudioEngine Engine;
        public Rig() => Engine = new AudioEngine(Sink, new AudioEngineSettings { UpdateRate = Steps });
        public void Step(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                Engine.Advance();
                Engine.Pump();
            }
        }
        public void Seconds(double seconds) => Step((int)Math.Ceiling(seconds * Steps));
        public float Left(long frame) => Sink.Samples[(int)frame * 2];
        public float Right(long frame) => Sink.Samples[(int)frame * 2 + 1];
        /// <summary>Root mean square of the left (or right) channel over frames from..to.</summary>
        public double Level(double fromSeconds, double toSeconds, bool right = false)
        {
            int from = (int)(fromSeconds * Rate), to = (int)(toSeconds * Rate);
            double sum = 0;
            for (int f = from; f < to; f++)
            {
                double s = Sink.Samples[f * 2 + (right ? 1 : 0)];
                sum += s * s;
            }
            return Math.Sqrt(sum / (to - from));
        }
        public void Dispose() => Engine.Dispose();
    }

    private static double Decibels(double ratio) => 20 * Math.Log10(ratio);

    /// <summary>
    /// The gain of a sine through <paramref name="filter"/> on the "sfx" bus, measured over 0.2 seconds (whole periods of
    /// the frequencies used here) after 0.2 seconds for the filter to settle.
    /// </summary>
    private static double Gain(AudioEffect filter, double frequency)
    {
        using var rig = new Rig();
        rig.Engine.Bus(AudioEngine.SfxBus).Add(filter);
        rig.Engine.Play(Sine(frequency, 0.5), new PlayParameters());
        rig.Seconds(0.5);
        return rig.Level(0.2, 0.4) / (0.5 / Math.Sqrt(2));
    }

    /// <summary>
    /// The frequency of the analog filter the design maps <paramref name="frequency"/> to, as a multiple of its cutoff:
    /// the bilinear transform warps frequencies by the tangent, and the design pre-warps the cutoff.
    /// </summary>
    private static double Warped(double frequency, double cutoff) => Math.Tan(Math.PI * frequency / Rate) / Math.Tan(Math.PI * cutoff / Rate);

    [TestMethod]
    [DataRow(1000, 100)]
    [DataRow(1000, 1000)]
    [DataRow(1000, 4000)]
    [DataRow(1000, 10000)]
    [DataRow(300, 2400)]
    public void A_low_pass_has_the_response_of_a_second_order_filter(int cutoff, int frequency)
    {
        double w = Warped(frequency, cutoff), expected = 1 / Math.Sqrt(1 + Math.Pow(w, 4));
        double measured = Gain(new LowPass { Cutoff = cutoff }, frequency);
        Assert.AreEqual(Decibels(expected), Decibels(measured), 0.5, $"{frequency} Hz through a low-pass at {cutoff} Hz");
    }

    [TestMethod]
    [DataRow(1000, 100)]
    [DataRow(1000, 1000)]
    [DataRow(1000, 4000)]
    [DataRow(200, 50)]
    public void A_high_pass_has_the_response_of_a_second_order_filter(int cutoff, int frequency)
    {
        double w = Warped(frequency, cutoff), expected = w * w / Math.Sqrt(1 + Math.Pow(w, 4));
        double measured = Gain(new HighPass { Cutoff = cutoff }, frequency);
        Assert.AreEqual(Decibels(expected), Decibels(measured), 0.5, $"{frequency} Hz through a high-pass at {cutoff} Hz");
    }

    [TestMethod]
    public void Resonance_raises_the_cutoff_frequency_by_its_factor()
    {
        // At the cutoff a second-order filter's gain is its Q.
        Assert.AreEqual(Decibels(4), Decibels(Gain(new LowPass { Cutoff = 1000, Resonance = 4 }, 1000)), 0.5);
    }

    [TestMethod]
    public void A_cutoff_change_glides_instead_of_clicking()
    {
        using var rig = new Rig();
        var filter = new LowPass { Cutoff = 8000 };
        rig.Engine.Bus(AudioEngine.SfxBus).Add(filter);
        rig.Engine.Play(Sine(1000, 1), new PlayParameters());
        rig.Seconds(0.3);
        filter.Cutoff = 100;
        rig.Seconds(0.3);
        // A 1 kHz sine of amplitude 0.5 never moves faster than this between two frames; a jump in the filter would.
        double steepest = 0.5 * 2 * Math.PI * 1000 / Rate;
        for (int f = (int)(0.25 * Rate); f < (int)(0.55 * Rate); f++)
            Assert.IsTrue(Math.Abs(rig.Left(f) - rig.Left(f - 1)) <= steepest * 1.25, $"frame {f}: no step in the output");
        double before = rig.Level(0.2, 0.3), during = rig.Level(0.3, 0.31), after = rig.Level(0.5, 0.6);
        Assert.IsTrue(during > after * 5 && during < before * 1.01, $"it gets there over time: {before:F4}, {during:F4}, {after:F4}");
        Assert.AreEqual(Decibels(1 / Math.Sqrt(1 + Math.Pow(Warped(1000, 100), 4))), Decibels(after / before), 1.0, "and ends at the new cutoff's response");
    }

    [TestMethod]
    public void A_bypassed_filter_changes_nothing_and_starts_clean_when_switched_in()
    {
        using var rig = new Rig();
        var filter = new LowPass { Cutoff = 200, Bypass = true };
        rig.Engine.Bus(AudioEngine.SfxBus).Add(filter);
        rig.Engine.Play(Constant(0.5f, 1), new PlayParameters());
        rig.Step(3);
        Assert.AreEqual(0.5f, rig.Left(StepFrames), "bypassed: the samples as mixed");
        filter.Bypass = false;
        rig.Step(30);
        Assert.IsTrue(rig.Left(3 * StepFrames) < 0.01f, "it starts from silence, not from what it heard before");
        Assert.AreEqual(0.5f, rig.Left(30 * StepFrames), 1e-3f, "and settles on the constant, which a low-pass lets through");
    }

    [TestMethod]
    public void A_compressor_gives_one_decibel_out_for_each_ratio_decibels_over_its_threshold()
    {
        using var rig = new Rig();
        rig.Engine.Bus(AudioEngine.SfxBus).Add(new Compressor { Threshold = -20, Ratio = 4, Attack = 0.005f, Release = 0.05f, Makeup = 6 });
        rig.Engine.Play(Constant(1f, 0.5), new PlayParameters());      // 0 dB: 20 over the threshold
        rig.Seconds(0.5);
        rig.Engine.Play(Constant(0.05f, 0.5), new PlayParameters());   // -26 dB: under it
        rig.Seconds(1.2);
        // -20 + 20 / 4 = -15 dB, plus 6 of makeup.
        Assert.AreEqual(Math.Pow(10, (-15 + 6) / 20.0), rig.Left((int)(0.4 * Rate)), 0.02 * Math.Pow(10, -9 / 20.0), "over the threshold");
        Assert.AreEqual(0.05 * Math.Pow(10, 6 / 20.0), rig.Left((int)(0.95 * Rate)), 0.02 * 0.1, "under it only the makeup applies, once it let go");
        Assert.IsTrue(rig.Left(2) > 1.9f, "the attack takes time: the first frames pass almost whole, with the makeup");
    }

    [TestMethod]
    public void Ducking_turns_the_music_down_while_the_voice_bus_sounds_and_brings_it_back()
    {
        using var rig = new Rig();
        rig.Engine.AddBus("voice");
        rig.Engine.Bus(AudioEngine.MusicBus).Add(Compressor.Duck("voice", decibels: 12, attack: 0.02f, release: 0.1f));
        // The music on the left only, the voice on the right only, so each is read alone.
        rig.Engine.Play(Constant(0.5f, 3), new PlayParameters { Bus = AudioEngine.MusicBus, Pan = 0 });
        rig.Seconds(0.5);
        rig.Engine.Play(Sine(300, 0.5, 0.4f), new PlayParameters { Bus = "voice", Pan = 1 });
        rig.Seconds(2);
        Assert.AreEqual(0.5f, rig.Left((int)(0.45 * Rate)), "before the voice: untouched, bit for bit");
        Assert.AreEqual(0.5 * Math.Pow(10, -12 / 20.0), rig.Left((int)(0.9 * Rate)), 0.02 * 0.125, "under the voice: 12 dB down");
        Assert.AreEqual(0.5f, rig.Left((int)(2.2 * Rate)), 1e-3f, "after the voice: back");
        Assert.AreEqual(0.4 / Math.Sqrt(2), rig.Level(0.6, 0.9, right: true), 0.01, "the voice itself is not changed");
        double during = rig.Left((int)(0.5 * Rate) + (int)(0.01 * Rate));
        Assert.IsTrue(during is > 0.15 and < 0.45, $"half way through the attack it is part of the way down: {during:F3}");
    }

    [TestMethod]
    public void A_delay_repeats_the_sound_each_time_weaker_by_its_feedback()
    {
        using var rig = new Rig();
        rig.Engine.Bus(AudioEngine.SfxBus).Add(new Delay { Time = 0.25f, Feedback = 0.5f, Wet = 0.8f, Dry = 1 });
        rig.Engine.Play(Impulse(0.5f), new PlayParameters());
        rig.Seconds(1.1);
        int period = (int)(0.25f * Rate);
        Assert.AreEqual(0.5f, rig.Left(0), "the sound itself");
        Assert.AreEqual(0.5f * 0.8f, rig.Left(period), 1e-6f, "the first echo, at the wet gain");
        Assert.AreEqual(0.5f * 0.8f * 0.5f, rig.Left(2 * period), 1e-6f, "the second, times the feedback");
        Assert.AreEqual(0.5f * 0.8f * 0.25f, rig.Left(3 * period), 1e-6f);
        Assert.AreEqual(0f, rig.Left(period - 1));
        Assert.AreEqual(0f, rig.Left(period + 1));
        Assert.AreEqual(rig.Left(period), rig.Right(period), "both channels");
    }

    [TestMethod]
    public void A_reverb_rings_as_long_as_its_room_size_says_and_keeps_the_dry_sound()
    {
        double Tail(float roomSize, double from, double to)
        {
            using var rig = new Rig();
            rig.Engine.Bus(AudioEngine.SfxBus).Add(new Reverb { RoomSize = roomSize, Damping = 0, Wet = 1, Dry = 1 });
            rig.Engine.Play(Impulse(), new PlayParameters());
            rig.Seconds(to);
            Assert.AreEqual(1f, rig.Left(0), 1e-6f, "the dry sound passes at its gain; no reflection arrives in the first frame");
            return rig.Level(from, to);
        }
        // Without damping each comb filter decays by its feedback once per pass of its line, so over a second the tail
        // falls by the feedback to the power of the passes in a second: between the longest line's and the shortest's.
        foreach (float room in new[] { 0.2f, 0.8f })
        {
            double feedback = Reverb.FeedbackFor(room);
            double slowest = Math.Pow(feedback, (double)Rate / Reverb.CombLength(7, Rate)), fastest = Math.Pow(feedback, (double)Rate / Reverb.CombLength(0, Rate));
            double early = Tail(room, 0.5, 0.6), late = Tail(room, 1.5, 1.6);
            Assert.IsTrue(early > 0, "there is a tail");
            Assert.IsTrue(late / early <= slowest * 1.5 && late / early >= fastest / 1.5,
                $"room {room}: a second later the tail is {late / early:E2} of what it was; the design says between {fastest:E2} and {slowest:E2}");
        }
        Assert.IsTrue(Tail(0.8f, 1.0, 1.2) > 20 * Tail(0.2f, 1.0, 1.2), "a larger room rings longer");
    }

    [TestMethod]
    public void A_reverb_spreads_a_centred_sound_to_both_sides_differently()
    {
        using var rig = new Rig();
        rig.Engine.Bus(AudioEngine.SfxBus).Add(new Reverb { Wet = 1, Dry = 0 });
        rig.Engine.Play(Impulse(), new PlayParameters());
        rig.Seconds(0.5);
        Assert.AreEqual(0f, rig.Left(0), "no dry sound");
        bool differ = false;
        for (int f = 0; f < Rate / 2; f++) differ |= rig.Left(f) != rig.Right(f);
        Assert.IsTrue(differ, "width: the two sides' reflections are not the same");
        Assert.IsTrue(rig.Level(0.03, 0.2) > 1e-4 && rig.Level(0.03, 0.2, right: true) > 1e-4);
    }

    [TestMethod]
    public void Effect_parameters_are_clamped_and_ignore_values_that_are_not_numbers()
    {
        var lowPass = new LowPass { Cutoff = float.NaN, Resonance = 100 };
        Assert.AreEqual(1000f, lowPass.Cutoff);
        Assert.AreEqual(20f, lowPass.Resonance);
        lowPass.Cutoff = 1;
        Assert.AreEqual(10f, lowPass.Cutoff);
        var delay = new Delay { Time = 10, Feedback = 2 };
        Assert.AreEqual(Delay.MaxTime, delay.Time);
        Assert.AreEqual(0.95f, delay.Feedback, "feedback below 1: an echo always dies");
        var compressor = new Compressor { Ratio = 0, Threshold = 10 };
        Assert.AreEqual(1f, compressor.Ratio);
        Assert.AreEqual(0f, compressor.Threshold);
        var duck = Compressor.Duck("voice", 9);
        Assert.AreEqual("voice", duck.Sidechain);
        Assert.AreEqual(9f, duck.Range);
        Assert.AreEqual(0.3f, new Reverb { Wet = float.NaN }.Wet);
        Assert.AreEqual(4f, new Limiter { Threshold = 9 }.Threshold);
    }
}

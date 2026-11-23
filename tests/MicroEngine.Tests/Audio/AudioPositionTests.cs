using MicroEngine.Audio;
using MicroEngine.Platform;

namespace MicroEngine.Tests.Audio;

/// <summary>
/// Time in a sound: where a voice is (on the mixer's timeline, and as the player hears it), moving it, starting it
/// elsewhere than its beginning or later than its step, and the region a loop repeats. Rendered offline: every clip
/// and stream here holds its own frame numbers as samples, so each output frame says which source frame it played.
/// </summary>
[TestClass]
public sealed class AudioPositionTests
{
    private const int Rate = 48000, Steps = 60, StepFrames = Rate / Steps;
    private static readonly int[] Blocks = [64, 441, 1024, 4096];

    /// <summary>A clip whose sample k is k: what plays at an output frame is the source frame it came from.</summary>
    private static AudioClip Numbered(int frames, int from = 0) => new([.. Enumerable.Range(from, frames).Select(i => (float)i)], Rate);

    private static (AudioEngine Engine, OfflineAudioSink Sink) Open(int blockFrames = 1024, int delayFrames = 0)
    {
        var sink = new OfflineAudioSink(new AudioFormat(Rate, 2), Steps, blockFrames);
        return (new AudioEngine(sink, new AudioEngineSettings { UpdateRate = Steps, DelayFrames = delayFrames }), sink);
    }

    private static void Step(AudioEngine engine, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            engine.Advance();
            engine.Pump();
        }
    }

    private static float At(OfflineAudioSink sink, long frame) => sink.Samples[(int)frame * 2];

    /// <summary>A stream of numbered frames that can seek; it counts its reads after the last seek.</summary>
    private sealed class NumberedStream(int frames, LoopRegion? loop = null) : ISeekableAudioStream
    {
        private long _position;
        public int Seeks;
        public bool Disposed;
        public int Channels => 1;
        public int SampleRate => Rate;
        public long Frames => frames;
        public LoopRegion? LoopRegion => loop;
        public int Read(Span<float> buffer)
        {
            int n = (int)Math.Min(buffer.Length, frames - _position);
            for (int i = 0; i < n; i++) buffer[i] = _position + i;
            _position += n;
            return n;
        }
        public void Rewind() => _position = 0;
        public void Seek(long frame) { _position = frame; Seeks++; }
        public void Dispose() => Disposed = true;
    }

    /// <summary>A stream that can only be read forward and rewound.</summary>
    private sealed class ForwardStream(int frames) : IAudioStream
    {
        private int _position;
        public int Channels => 1;
        public int SampleRate => Rate;
        public int Read(Span<float> buffer)
        {
            int n = Math.Min(buffer.Length, frames - _position);
            for (int i = 0; i < n; i++) buffer[i] = _position + i;
            _position += n;
            return n;
        }
        public void Rewind() => _position = 0;
        public void Dispose() { }
    }

    // ---- Position

    [TestMethod]
    public void A_voice_position_follows_the_steps_and_the_heard_position_trails_it_by_the_output_delay()
    {
        var (engine, _) = Open(delayFrames: 480);
        var voice = engine.Play(Numbered(Rate), new PlayParameters());
        Assert.AreEqual(0, engine.Position(voice), "as it starts");
        Assert.AreEqual(-0.01, engine.HeardPosition(voice), 1e-12, "its first frame is heard 480 frames from now");
        Step(engine, 3);
        Assert.AreEqual(3 / 60.0, engine.Position(voice), 1e-12);
        Assert.AreEqual(3 / 60.0 - 0.01, engine.HeardPosition(voice), 1e-12);
        Assert.AreEqual(1.0, engine.Duration(voice), 1e-12);

        engine.Pause(voice, true);
        Step(engine, 5);
        Assert.AreEqual(3 / 60.0, engine.Position(voice), 1e-12, "a paused sound stays where it is");
        engine.Pause(voice, false);
        Step(engine, 1);
        Assert.AreEqual(4 / 60.0, engine.Position(voice), 1e-12);

        engine.SetPitch(voice, 2);
        Step(engine, 3);
        Assert.AreEqual(10 / 60.0, engine.Position(voice), 1e-12, "at twice the speed a step is worth two");

        engine.Stop(voice);
        Assert.AreEqual(0, engine.Position(voice), "a handle that is not playing");
        Assert.AreEqual(0, engine.HeardPosition(voice));
        Assert.AreEqual(0, engine.Duration(voice));
        Assert.AreEqual(0, engine.Position(VoiceHandle.None));
    }

    [TestMethod]
    public void The_position_of_a_clip_at_another_rate_is_in_its_own_seconds()
    {
        var (engine, _) = Open();
        var clip = new AudioClip(new float[22050], 22050);
        var voice = engine.Play(clip, new PlayParameters());
        Step(engine, 30);
        Assert.AreEqual(0.5, engine.Position(voice), 1e-9, "half a second of steps is half a second of the clip, whatever its rate");
        Assert.AreEqual(1.0, engine.Duration(voice), 1e-12);
    }

    // ---- Starting elsewhere, and later

    [TestMethod]
    public void A_sound_starts_at_the_time_asked_and_ends_that_much_sooner()
    {
        foreach (int block in Blocks)
        {
            var (engine, sink) = Open(block, delayFrames: 100);
            var voice = engine.Play(Numbered(4800), new PlayParameters { StartAt = 0.05 });
            Assert.AreEqual(0.05, engine.Position(voice), 1e-12);
            Step(engine, 4);
            Assert.AreEqual(2400f, At(sink, 100), $"block {block}: its first frame is the clip's frame 2400");
            Assert.AreEqual(2400f + 2399, At(sink, 100 + 2399), $"block {block}: the clip's last frame");
            Assert.AreEqual(0f, At(sink, 100 + 2400), $"block {block}: over");
            Assert.IsFalse(engine.IsPlaying(voice), $"block {block}: the timeline knows it ended (2400 frames, 3 steps)");
        }
    }

    [TestMethod]
    public void A_sound_scheduled_later_starts_at_that_exact_frame()
    {
        foreach (int block in Blocks)
        {
            var (engine, sink) = Open(block, delayFrames: 64);
            Step(engine);
            var voice = engine.Play(Numbered(2000, from: 1), new PlayParameters { Delay = 0.0125 });
            Assert.IsTrue(engine.IsPlaying(voice), "it holds its voice while it waits");
            Assert.AreEqual(-0.0125, engine.Position(voice), 1e-12, "the time to its start, counted down");
            Step(engine, 3);
            int start = StepFrames + 64 + 600;
            Assert.AreEqual(0f, At(sink, start - 1), $"block {block}: nothing before its frame");
            Assert.AreEqual(1f, At(sink, start), $"block {block}: its first frame, 600 frames after its step's");
            Assert.AreEqual(700f, At(sink, start + 699), $"block {block}");
            Assert.AreEqual((3 * StepFrames - 600) / (double)Rate, engine.Position(voice), 1e-12, $"block {block}: counted from its own start");
        }
    }

    // ---- Seek

    [TestMethod]
    public void A_seek_moves_a_clip_at_the_frame_of_its_step_whatever_the_block_size()
    {
        foreach (int block in Blocks)
        {
            var (engine, sink) = Open(block, delayFrames: 300);
            var voice = engine.Play(Numbered(Rate), new PlayParameters());
            Step(engine, 2);
            Assert.IsTrue(engine.Seek(voice, 0.5));
            Assert.AreEqual(0.5, engine.Position(voice), 1e-12, "the position is the one asked for at once");
            Step(engine, 2);
            int at = 2 * StepFrames + 300;
            Assert.AreEqual(at - 300 - 1f, At(sink, at - 1), $"block {block}: playing on until the seek's frame");
            Assert.AreEqual(24000f, At(sink, at), $"block {block}: the frame asked for, at the step's frame");
            Assert.AreEqual(24000f + 500, At(sink, at + 500), $"block {block}");
            Assert.AreEqual(0.5 + 2 / 60.0, engine.Position(voice), 1e-12);
        }
    }

    [TestMethod]
    public void A_seek_moves_the_end_of_a_sound_that_does_not_loop()
    {
        var (engine, sink) = Open();
        var voice = engine.Play(Numbered(Rate), new PlayParameters());
        Step(engine);
        Assert.IsTrue(engine.Seek(voice, 0.95));
        Step(engine, 3);
        Assert.IsFalse(engine.IsPlaying(voice), "0.05 s were left: three steps");
        Step(engine);
        Assert.AreEqual(47999f, At(sink, StepFrames + 2399), "its last frame");
        Assert.AreEqual(0f, At(sink, StepFrames + 2400));

        var back = engine.Play(Numbered(Rate), new PlayParameters());
        Step(engine, 50);
        Assert.IsTrue(engine.Seek(back, 0), "back to the start: a whole second is left again");
        Step(engine, 55);
        Assert.IsTrue(engine.IsPlaying(back));
        Step(engine, 6);
        Assert.IsFalse(engine.IsPlaying(back));

        var end = engine.Play(Numbered(Rate), new PlayParameters());
        Assert.IsTrue(engine.Seek(end, 30), "past the end: clamped to it, so the sound is over");
        Step(engine);
        Assert.IsFalse(engine.IsPlaying(end));
    }

    [TestMethod]
    public void A_seek_while_paused_resumes_from_the_new_place()
    {
        var (engine, sink) = Open();
        var voice = engine.Play(Numbered(Rate), new PlayParameters());
        Step(engine);
        engine.Pause(voice, true);
        Step(engine);
        Assert.IsTrue(engine.Seek(voice, 0.25));
        Step(engine, 2);
        Assert.AreEqual(0.25, engine.Position(voice), 1e-12, "paused: it waits at the new place");
        engine.Pause(voice, false);
        Step(engine, 2);
        Assert.AreEqual(12000f, At(sink, 4 * StepFrames), "resumed from the frame asked for");
        Assert.AreEqual(0.25 + 2 / 60.0, engine.Position(voice), 1e-12);
        Step(engine, 42);
        Assert.IsTrue(engine.IsPlaying(voice), "0.75 s from the resume: 45 steps");
        Step(engine);
        Assert.IsFalse(engine.IsPlaying(voice));
    }

    [TestMethod]
    public void Seeks_of_several_steps_wait_each_for_its_frame()
    {
        var (engine, sink) = Open(blockFrames: 4096, delayFrames: 3 * StepFrames);
        var voice = engine.Play(Numbered(Rate), new PlayParameters());
        engine.Advance();
        engine.Seek(voice, 0.1);
        engine.Advance();
        engine.Seek(voice, 0.2);
        engine.Advance();
        engine.Seek(voice, 0.3);
        for (int i = 0; i < 8; i++) engine.Pump();
        int first = 3 * StepFrames;
        Assert.AreEqual(StepFrames - 1f, At(sink, first + StepFrames - 1));
        Assert.AreEqual(4800f, At(sink, first + StepFrames));
        Assert.AreEqual(9600f, At(sink, first + 2 * StepFrames));
        Assert.AreEqual(14400f, At(sink, first + 3 * StepFrames));
        Assert.AreEqual(14400f + StepFrames, At(sink, first + 4 * StepFrames));
    }

    // ---- Loop regions

    [TestMethod]
    public void A_loop_region_plays_the_intro_once_and_repeats_the_region()
    {
        string? reference = null;
        foreach (int block in Blocks)
        {
            var (engine, sink) = Open(block, delayFrames: 50);
            var voice = engine.Play(Numbered(1000), new PlayParameters { Loop = true, LoopRegion = new LoopRegion(400, 700) });
            Step(engine, 3);
            for (int f = 0; f < 700; f += 33) Assert.AreEqual(f, At(sink, 50 + f), $"block {block}: the intro and the first pass");
            Assert.AreEqual(699f, At(sink, 50 + 699));
            Assert.AreEqual(400f, At(sink, 50 + 700), $"block {block}: back to the region's start, not the clip's");
            Assert.AreEqual(699f, At(sink, 50 + 999));
            Assert.AreEqual(400f, At(sink, 50 + 1000));
            Assert.AreEqual(400f + 123, At(sink, 50 + 1300 + 123));
            // Three steps are 2400 frames: 700, then 300 at a time.
            Assert.AreEqual((400 + (2400 - 700) % 300) / (double)Rate, engine.Position(voice), 1e-12, $"block {block}: the timeline is in the region too");
            string rendered = string.Join(",", sink.Samples.Take(2 * 2400));
            reference ??= rendered;
            Assert.AreEqual(reference, rendered, $"block {block}: the same samples at every block size");
        }
    }

    [TestMethod]
    public void A_clip_carries_its_loop_region_and_a_play_can_replace_it()
    {
        var (engine, sink) = Open();
        var clip = new AudioClip([.. Enumerable.Range(0, 1000).Select(i => (float)i)], Rate) { LoopRegion = new LoopRegion(900) };
        engine.Play(clip, new PlayParameters { Loop = true, Pan = 0 });
        engine.Play(clip, new PlayParameters { Loop = true, Pan = 1, LoopRegion = new LoopRegion(0, 10) });
        engine.Play(clip, new PlayParameters { Pan = 0.5f, Volume = 0 });
        Step(engine, 2);
        Assert.AreEqual(900f, sink.Samples[1000 * 2], "left: the clip's own region, from frame 900 to its end");
        Assert.AreEqual(905f, sink.Samples[1105 * 2]);
        Assert.AreEqual(0f, sink.Samples[10 * 2 + 1], "right: the play's region");
        Assert.AreEqual(3f, sink.Samples[1003 * 2 + 1]);
    }

    [TestMethod]
    public void A_region_that_makes_no_sense_is_the_whole_sound()
    {
        foreach (var region in new[] { new LoopRegion(-5, -1), new LoopRegion(5000, 9000), new LoopRegion(0, long.MaxValue), new LoopRegion(long.MinValue, long.MinValue), new LoopRegion(1000, 1000) })
        {
            var (engine, sink) = Open();
            engine.Play(Numbered(1000), new PlayParameters { Loop = true, LoopRegion = region });
            Step(engine, 2);
            Assert.AreEqual(999f, At(sink, 999), $"{region}");
            Assert.AreEqual(0f, At(sink, 1000), $"{region}: loops over the whole clip");
        }
        var (open, heard) = Open();
        open.Play(Numbered(1000), new PlayParameters { Loop = true, LoopRegion = new LoopRegion(700, 400) });
        Step(open, 2);
        Assert.AreEqual(700f, At(heard, 1000), "an end that is not after the start is the sound's end");
        var (empty, _) = Open();
        empty.Play(new AudioClip([], Rate), new PlayParameters { Loop = true, LoopRegion = new LoopRegion(10, 20) });
        Step(empty, 2);
        Assert.AreEqual(0, empty.FaultedEffects);
    }

    [TestMethod]
    public void A_seek_past_the_region_of_a_loop_lands_inside_it()
    {
        var (engine, sink) = Open();
        var voice = engine.Play(Numbered(1000), new PlayParameters { Loop = true, LoopRegion = new LoopRegion(400, 700) });
        Step(engine);
        Assert.IsTrue(engine.Seek(voice, 850.0 / Rate));
        Assert.AreEqual(550.0 / Rate, engine.Position(voice), 1e-12);
        Step(engine);
        Assert.AreEqual(550f, At(sink, StepFrames));
        Assert.IsTrue(engine.Seek(voice, 100.0 / Rate), "the intro can be played again");
        Step(engine);
        Assert.AreEqual(100f, At(sink, 2 * StepFrames));
        Assert.AreEqual(400f, At(sink, 2 * StepFrames + 600));
    }

    // ---- Streams

    [TestMethod]
    public void A_stream_that_can_seek_starts_anywhere_moves_and_tells_its_length()
    {
        foreach (int block in Blocks)
        {
            var (engine, sink) = Open(block, delayFrames: 200);
            var stream = new NumberedStream(Rate);
            var voice = engine.Play(stream, new PlayParameters { StartAt = 0.25 });
            Assert.AreEqual(1.0, engine.Duration(voice), 1e-12);
            Assert.AreEqual(0.25, engine.Position(voice), 1e-12);
            Step(engine, 2);
            Assert.AreEqual(12000f, At(sink, 200), $"block {block}: from the frame asked for");
            Assert.IsTrue(engine.Seek(voice, 0.5));
            Assert.AreEqual(0.5, engine.Position(voice), 1e-12);
            Step(engine, 3);
            int at = 2 * StepFrames + 200;
            Assert.AreEqual(12000f + at - 200 - 1, At(sink, at - 1), $"block {block}: the old place until the seek's frame, without a gap");
            Assert.AreEqual(24000f, At(sink, at), $"block {block}: the new place from it");
            Assert.AreEqual(24000f + 1500, At(sink, at + 1500), $"block {block}");
            Assert.AreEqual(0.5 + 3 / 60.0, engine.Position(voice), 1e-12);
            Assert.IsFalse(stream.Disposed);
            engine.Stop(voice);
            Step(engine, 2);
            Assert.IsTrue(stream.Disposed, "the stream is released once, with its voice");
        }
    }

    [TestMethod]
    public void A_stream_ends_on_the_timeline_where_it_ends_in_the_output_after_a_seek_and_a_pause()
    {
        var (engine, sink) = Open();
        var voice = engine.Play(new NumberedStream(Rate), new PlayParameters());
        Step(engine, 6);
        engine.Pause(voice, true);
        Step(engine, 30);
        engine.Pause(voice, false);
        Step(engine, 6);
        Assert.IsTrue(engine.Seek(voice, 0.9));
        Step(engine, 5);
        Assert.IsTrue(engine.IsPlaying(voice), "0.1 s were left at the seek: six steps");
        Step(engine, 1);
        Assert.IsFalse(engine.IsPlaying(voice));
        int seek = 42 * StepFrames;
        Step(engine);
        Assert.AreEqual(9599f, At(sink, seek - 1), "the pause held it for thirty steps");
        Assert.AreEqual(47999f, At(sink, seek + 4799));

        Assert.AreEqual(0f, At(sink, seek + 4800));
    }

    [TestMethod]
    public void A_long_stream_paused_for_a_while_ends_that_much_later_on_the_timeline()
    {
        var (engine, sink) = Open();
        var voice = engine.Play(new NumberedStream(Rate), new PlayParameters());
        Step(engine, 6);
        engine.Pause(voice, true);
        Step(engine, 30);
        engine.Pause(voice, false);
        Step(engine, 53);
        Assert.IsTrue(engine.IsPlaying(voice), "a second of sound and half a second of pause: 90 steps");
        Step(engine, 1);
        Assert.IsFalse(engine.IsPlaying(voice));
        Assert.AreEqual(47999f, At(sink, 90 * StepFrames - 1));
    }

    [TestMethod]
    public void A_stream_plays_its_last_frame()

    {
        var (engine, sink) = Open();
        var voice = engine.Play(new NumberedStream(5000, null), new PlayParameters());
        Step(engine, 7);
        Assert.AreEqual(4999f, At(sink, 4999));
        Assert.AreEqual(0f, At(sink, 5000));
        Assert.IsFalse(engine.IsPlaying(voice));
    }

    [TestMethod]
    public void A_stream_loops_over_the_region_its_file_names()
    {
        var (engine, sink) = Open(delayFrames: 10);
        var voice = engine.Play(new NumberedStream(12000, new LoopRegion(2000, 8000)), new PlayParameters { Loop = true });
        Step(engine, 24);
        Assert.AreEqual(7999f, At(sink, 10 + 7999));
        Assert.AreEqual(2000f, At(sink, 10 + 8000), "back to the region's start; the tail after the region is never played");
        Assert.AreEqual(7999f, At(sink, 10 + 13999));
        Assert.AreEqual(2000f, At(sink, 10 + 14000));
        Assert.AreEqual((2000 + (24 * StepFrames - 8000) % 6000) / (double)Rate, engine.Position(voice), 1e-12);
        Assert.IsTrue(engine.Seek(voice, 9500.0 / Rate), "past the region: brought into it");
        Assert.AreEqual(3500.0 / Rate, engine.Position(voice), 1e-12);
    }

    [TestMethod]
    public void A_stream_that_cannot_seek_plays_from_its_start_and_refuses_a_seek()
    {
        var (engine, sink) = Open();
        var voice = engine.Play(new ForwardStream(3000), new PlayParameters { StartAt = 0.01, Loop = true, LoopRegion = new LoopRegion(1000, 2000) });
        Step(engine, 5);
        Assert.AreEqual(1f, At(sink, 1), "from its beginning");
        Assert.AreEqual(2999f, At(sink, 2999), "the whole stream");
        Assert.AreEqual(0f, At(sink, 3000), "rewound");
        Assert.IsFalse(engine.Seek(voice, 0.02));
        Assert.AreEqual(0, engine.Duration(voice));
        Assert.AreEqual(5 / 60.0, engine.Position(voice), 1e-12, "its position counts on through its loops");
    }

    // ---- Arguments

    [TestMethod]
    public void Times_that_are_not_usable_numbers_do_not_break_the_mixer()
    {
        double[] odd = [double.NaN, -1, 0, double.PositiveInfinity, double.NegativeInfinity, 1e300, -1e300, double.MaxValue, double.Epsilon];
        foreach (double value in odd)
        {
            var (engine, sink) = Open();
            var clip = Numbered(4800);
            var started = engine.Play(clip, new PlayParameters { StartAt = value });
            var delayed = engine.Play(clip, new PlayParameters { Delay = value });
            var looped = engine.Play(clip, new PlayParameters { Loop = true, LoopRegion = LoopRegion.FromSeconds(value, value, Rate) });
            var stream = engine.Play(new NumberedStream(4800), new PlayParameters { StartAt = value, Loop = true, LoopRegion = LoopRegion.FromSeconds(value, value, Rate) });
            var sought = engine.Play(clip, new PlayParameters());
            var soughtStream = engine.Play(new NumberedStream(4800), new PlayParameters());
            Step(engine);
            Assert.IsTrue(engine.Seek(sought, value), $"{value}");
            Assert.IsTrue(engine.Seek(soughtStream, value), $"{value}");
            Assert.IsTrue(engine.Seek(looped, value), $"{value}");
            Step(engine, 8);
            foreach (var voice in new[] { started, delayed, looped, stream, sought, soughtStream })
            {
                double position = engine.Position(voice), heard = engine.HeardPosition(voice);
                Assert.IsTrue(double.IsFinite(position) && position <= 0.1 + 1e-9 && (position >= 0 || voice == delayed), $"{value}: position {position}");
                Assert.IsTrue(double.IsFinite(heard), $"{value}: heard {heard}");
            }
            Assert.IsTrue(sink.Samples.All(float.IsFinite), $"{value}: every sample rendered is a number");
            Assert.AreEqual(9 * StepFrames, sink.Frames, $"{value}: every block was rendered");
        }
        Assert.AreEqual(new LoopRegion(0, 0), LoopRegion.FromSeconds(double.NaN, double.NaN, Rate));
        Assert.AreEqual(new LoopRegion(48000, 96000), LoopRegion.FromSeconds(1, 2, Rate));
        Assert.AreEqual(new LoopRegion(0, 0), LoopRegion.FromSeconds(1, 2, 0));
    }

    [TestMethod]
    public void A_seek_with_a_time_that_is_not_a_number_changes_nothing()
    {
        var (engine, sink) = Open();
        var voice = engine.Play(Numbered(Rate), new PlayParameters());
        Step(engine, 2);
        Assert.IsTrue(engine.Seek(voice, double.NaN));
        Step(engine, 2);
        Assert.AreEqual(3 * StepFrames + 5f, At(sink, 3 * StepFrames + 5));
        Assert.AreEqual(4 / 60.0, engine.Position(voice), 1e-12);
        Assert.IsFalse(engine.Seek(VoiceHandle.None, 1));
    }

    // ---- The audio thread

    [TestMethod]
    public void Rendering_seeks_loop_regions_and_scheduled_sounds_allocates_nothing()
    {
        var (engine, _) = Open(blockFrames: 256, delayFrames: 100);
        var clip = Numbered(Rate);
        var looped = engine.Play(clip, new PlayParameters { Loop = true, LoopRegion = new LoopRegion(1000, 3000), Volume = 0.1f });
        var stream = engine.Play(new NumberedStream(Rate * 60), new PlayParameters { Loop = true, LoopRegion = new LoopRegion(500, 40000), Volume = 0.1f });
        int round = 0;
        void Round()
        {
            for (int i = 0; i < 20; i++)
            {
                round++;
                engine.Seek(looped, round % 7 * 0.01);
                if (round % 5 == 0) engine.Seek(stream, round % 11 * 0.05);
                engine.Play(clip, new PlayParameters { Delay = 0.004, StartAt = 0.9, Volume = 0.1f });
                Step(engine);
            }
        }
        Round();
        var bytes = new long[5];
        for (int i = 0; i < bytes.Length; i++)
        {
            long before = engine.RenderAllocations.Bytes;
            Round();
            bytes[i] = engine.RenderAllocations.Bytes - before;
        }
        Assert.AreEqual(0, bytes.Min(), $"bytes the render function allocated per round: {string.Join(", ", bytes)}");
        Assert.AreEqual(0, engine.DroppedCommands);
    }

    // ---- Loop points in files

    [TestMethod]
    public void A_wav_file_gives_its_sampler_loop_as_the_clip_region()
    {
        var plain = new AudioClip([.. Enumerable.Range(0, 100).Select(i => i / 200f)], Rate).ToWav();
        Assert.IsNull(AudioClip.FromWav(plain).LoopRegion);

        // The same file with a sampler chunk after its data: one loop, frames 20 to 79 inclusive.
        var smpl = new byte[8 + 36 + 24];
        "smpl"u8.CopyTo(smpl);
        BitConverter.TryWriteBytes(smpl.AsSpan(4), 36 + 24);
        BitConverter.TryWriteBytes(smpl.AsSpan(8 + 28), 1);
        BitConverter.TryWriteBytes(smpl.AsSpan(8 + 36 + 8), 20);
        BitConverter.TryWriteBytes(smpl.AsSpan(8 + 36 + 12), 79);
        byte[] looped = [.. plain, .. smpl];
        BitConverter.TryWriteBytes(looped.AsSpan(4), looped.Length - 8);
        var clip = AudioClip.FromWav(looped);
        Assert.AreEqual(new LoopRegion(20, 80), clip.LoopRegion, "the end of a sampler loop is its last frame; a region's is the one after");
        Assert.AreEqual(100, clip.Samples.Length);

        // Before the data, and cut short: read or ignored, never an error.
        byte[] first = [.. plain.AsSpan(0, 12), .. smpl, .. plain.AsSpan(12)];
        Assert.AreEqual(new LoopRegion(20, 80), AudioClip.FromWav(first).LoopRegion);
        Assert.IsNull(AudioClip.FromWav([.. plain, .. smpl.AsSpan(0, 30)]).LoopRegion);
    }

    // ---- The game-facing mixer

    [TestMethod]
    public void The_game_mixer_tells_where_the_music_is_and_moves_it()
    {
        var device = new CaptureAudioDevice(60, Rate);
        var mixer = new AudioMixer(device);
        Assert.AreEqual(0, mixer.MusicPosition, "no music");
        Assert.IsFalse(mixer.SeekMusic(1));
        mixer.AddMusic("theme", new AudioClip([.. Enumerable.Range(0, Rate).Select(i => i / 65536f)], Rate) { LoopRegion = new LoopRegion(24000) });
        mixer.PlayMusic("theme", fadeSeconds: 0);
        for (int i = 0; i < 6; i++) mixer.Update(1 / 60f);
        Assert.AreEqual(0.1, mixer.MusicPosition, 1e-12);
        Assert.AreEqual(mixer.MusicPosition - mixer.OutputLatency, mixer.MusicHeardPosition, 1e-12);
        Assert.AreEqual(1.0, mixer.MusicDuration, 1e-12);
        Assert.IsTrue(mixer.SeekMusic(0.99));
        mixer.Update(1 / 60f);
        Assert.AreEqual(0.5 + (0.99 + 1 / 60.0 - 1), mixer.MusicPosition, 1e-9, "past its end it is back in its loop region, which starts half way");
        Assert.AreEqual(47520 / 65536f, device.Output.Samples[6 * StepFrames * 2], "the frame asked for, at the step's frame");
        Assert.AreEqual(24000 / 65536f, device.Output.Samples[(6 * StepFrames + 480) * 2], "then the region's start");


        mixer.AddSound("tick", new AudioClip(new float[Rate], Rate));
        var voice = mixer.PlayVoice("tick", new PlayParameters());
        mixer.Update(1 / 60f);
        Assert.AreEqual(1 / 60.0, mixer.Position(voice), 1e-12);
        Assert.AreEqual(1.0, mixer.Duration(voice), 1e-12);
        Assert.IsTrue(mixer.Seek(voice, 0.5));
        Assert.AreEqual(0.5, mixer.Position(voice), 1e-12);
        Assert.AreEqual(0.5 - mixer.OutputLatency, mixer.HeardPosition(voice), 1e-12);
    }

    [TestMethod]
    public void A_silent_device_counts_the_time_the_music_has_played()
    {
        var mixer = new AudioMixer(new SilentAudioDevice());
        mixer.AddMusic("theme", new AudioClip(new float[Rate], Rate));
        Assert.AreEqual(0, mixer.MusicPosition);
        mixer.PlayMusic("theme", fadeSeconds: 0);
        for (int i = 0; i < 30; i++) mixer.Update(1 / 60f);
        Assert.AreEqual(0.5, mixer.MusicPosition, 1e-6);
        Assert.AreEqual(0.5, mixer.MusicHeardPosition, 1e-6);
        mixer.PauseMusic(true);
        for (int i = 0; i < 30; i++) mixer.Update(1 / 60f);
        Assert.AreEqual(0.5, mixer.MusicPosition, 1e-6, "paused");
        mixer.PauseMusic(false);
        Assert.IsTrue(mixer.SeekMusic(3));
        Assert.IsTrue(mixer.SeekMusic(double.NaN));
        Assert.AreEqual(3, mixer.MusicPosition, 1e-6);
        Assert.AreEqual(0, mixer.MusicDuration);
        Assert.AreEqual(0, mixer.OutputLatency);
        Assert.AreEqual(0, mixer.Position(VoiceHandle.None));
        Assert.IsFalse(mixer.Seek(VoiceHandle.None, 1));
        mixer.StopMusic(0);
        Assert.AreEqual(0, mixer.MusicPosition);
    }
}

using System.Diagnostics;
using MicroEngine.Audio;
using MicroEngine.Platform;

namespace MicroEngine.Tests.Audio;

/// <summary>
/// What an independent verification of time, formats and spatial sound found, each as the smallest case that showed
/// it: damaged and hostile files, sounds scheduled one after another, loops with long intros and tiny regions, the
/// heard position across pauses, pitch changes and seeks.
/// </summary>
[TestClass]
public sealed class AudioCorrectionTests
{
    private const int Rate = 48000, Steps = 60, StepFrames = Rate / Steps;

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

    /// <summary>Runs <paramref name="action"/> on this thread and tells the bytes it allocated and the time it took.</summary>
    private static (long Bytes, double Milliseconds) Measure(Action action)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        action();
        return (GC.GetAllocatedBytesForCurrentThread() - before, watch.Elapsed.TotalMilliseconds);
    }

    // ---- D1: a damaged music file

    /// <summary>A stream that gives numbered frames and then throws, as a decoder does on a damaged packet.</summary>
    private sealed class BreakingStream(int goodFrames, Exception error, bool seekThrows = false) : ISeekableAudioStream
    {
        private int _position;
        public bool Disposed;
        public int Channels => 1;
        public int SampleRate => Rate;
        public long Frames => 10 * Rate;
        public int Read(Span<float> buffer)
        {
            if (_position >= goodFrames) throw error;
            int n = Math.Min(buffer.Length, goodFrames - _position);
            for (int i = 0; i < n; i++) buffer[i] = _position + i;
            _position += n;
            return n;
        }
        public void Rewind() => _position = 0;
        public void Seek(long frame)
        {
            if (seekThrows) throw error;
            _position = (int)frame;
        }
        public void Dispose() => Disposed = true;
    }

    [TestMethod]
    public void A_stream_whose_decoder_throws_ends_instead_of_ending_the_game()
    {
        foreach (var error in new Exception[] { new ArgumentOutOfRangeException("index"), new IndexOutOfRangeException(), new InvalidDataException("corrupt packet"), new NullReferenceException() })
        {
            var (engine, sink) = Open();
            var stream = new BreakingStream(3000, error);
            var failures = new List<string>();
            engine.StreamFailed += (voice, problem) => failures.Add(problem.GetType().Name);
            var voice = engine.Play(stream, new PlayParameters { Loop = true });
            Step(engine, 30);
            Assert.AreEqual(2999f, At(sink, 2999), $"{error.GetType().Name}: what it decoded before the damage is played");
            Assert.AreEqual(0f, At(sink, 3000), "then silence");
            Assert.IsFalse(engine.IsPlaying(voice), "the sound is over");
            Assert.AreEqual(1, engine.FailedStreams, "counted once, not once per frame");
            CollectionAssert.AreEqual(new[] { error.GetType().Name }, failures, "and reported once");
            Step(engine, 5);
            Assert.IsTrue(stream.Disposed, "the stream is released");
            Assert.AreEqual(1, engine.FailedStreams);
        }
    }

    [TestMethod]
    public void A_seek_the_decoder_refuses_ends_the_stream_and_is_reported()
    {
        var (engine, _) = Open();
        var voice = engine.Play(new BreakingStream(100000, new InvalidDataException("no such page"), seekThrows: true), new PlayParameters());
        Step(engine, 3);
        engine.Seek(voice, 1.0);
        Step(engine, 5);
        Assert.IsFalse(engine.IsPlaying(voice));
        Assert.AreEqual(1, engine.FailedStreams);
    }

    [TestMethod]
    public void Music_files_damaged_after_their_headers_never_throw_while_they_play()
    {
        foreach (var (format, good) in new[] { ("mp3", AudioFixtures.Mp3), ("ogg", AudioFixtures.Ogg) })
        {
            var random = new Random(4242);
            int played = 0, failed = 0;
            for (int i = 0; i < 400; i++)
            {
                var file = (byte[])good.Clone();
                int flips = 1 + random.Next(6);
                for (int k = 0; k < flips; k++) file[file.Length / 2 + random.Next(file.Length / 2)] = (byte)random.Next(256);
                if (i % 5 == 0) file = file[..(file.Length / 2 + random.Next(file.Length / 2))];
                using var device = new MixerAudioDevice(new OfflineAudioSink(new AudioFormat(22050, 1), 60));
                device.Engine.Deterministic = true;
                MusicHandle handle;
                try { handle = device.LoadMusicStream("m", file, format, loop: i % 2 == 0); }
                catch (InvalidDataException) { continue; }
                played++;
                device.PlayMusic(handle);
                for (int step = 0; step < 90; step++)
                {
                    device.Update();
                    device.Pump();
                }
                failed += device.Engine.FailedStreams;
            }
            Assert.IsTrue(played > 10, $"{format}: {played} damaged files were accepted and played");
            Console.WriteLine($"{format}: {played} played, {failed} ended by their decoder");
        }
    }

    [TestMethod]
    public void The_game_mixer_reports_a_damaged_music_file_once()
    {
        AudioDecoders.Add("breaks-test", _ => new BreakingStream(2000, new IndexOutOfRangeException()));
        try
        {
            var device = new CaptureAudioDevice(60, Rate);
            var mixer = new AudioMixer(device);
            var log = new RecordingLog();
            mixer.Log = log;
            mixer.AddMusic("theme", new EncodedAudio([1, 2, 3], "breaks-test"));
            mixer.PlayMusic("theme", fadeSeconds: 0);
            for (int i = 0; i < 30; i++)
            {
                mixer.Pump();
                mixer.Update(1 / 60f);
            }
            Assert.IsNull(mixer.CurrentMusic, "the music is over");
            Assert.AreEqual(1, mixer.FailedStreams);
            Assert.AreEqual(1, log.Lines.Count, string.Join("\n", log.Lines));
            StringAssert.Contains(log.Lines[0], "theme");
            StringAssert.Contains(log.Lines[0], "IndexOutOfRangeException");
        }
        finally
        {
            AudioDecoders.Remove("breaks-test");
        }
    }

    private sealed class RecordingLog : MicroEngine.Kernel.ILog
    {
        public List<string> Lines { get; } = [];
        public void Write(MicroEngine.Kernel.LogLevel level, string category, string message) => Lines.Add($"{level} {category}: {message}");
    }

    // ---- D2: sounds scheduled one after another

    [TestMethod]
    public void Sounds_scheduled_one_after_another_each_get_a_voice_and_keep_their_handles()
    {
        var (engine, sink) = Open();
        var click = Numbered(4800, from: 1);
        var handles = new[] { 0.5, 1.0, 1.5, 2.0 }.Select(delay => engine.Play(click, new PlayParameters { Delay = delay })).ToList();
        Assert.AreEqual(4, handles.Select(h => h.Index).Distinct().Count(), "four sounds, four voices");
        Assert.AreEqual(4, engine.ActiveVoices);
        for (int i = 0; i < 4; i++)
        {
            Step(engine, 30);
            // Half a second later the next one has just started; the ones before it have ended.
            for (int k = 0; k < 4; k++)
                Assert.AreEqual(k >= i, engine.IsPlaying(handles[k]), $"after {(i + 1) * 0.5} s, sound {k}");
        }
        Step(engine, 30);
        Assert.AreEqual(0, engine.ActiveVoices);
        for (int i = 0; i < 4; i++)
        {
            Assert.AreEqual(0f, At(sink, (i + 1) * Rate / 2 - 1), $"nothing before sound {i}");
            Assert.AreEqual(1f, At(sink, (i + 1) * Rate / 2), $"sound {i} starts at its frame");
            Assert.AreEqual(4800f, At(sink, (i + 1) * Rate / 2 + 4799), $"sound {i} plays whole");
        }
    }

    [TestMethod]
    public void A_sound_playing_keeps_its_handle_when_another_is_scheduled_after_it()
    {
        var (engine, sink) = Open();
        var first = engine.Play(Numbered(Rate, from: 1), new PlayParameters());
        Step(engine, 3);
        var later = engine.Play(Numbered(4800, from: 1), new PlayParameters { Delay = 3 });
        Assert.AreNotEqual(first.Index, later.Index);
        Assert.IsTrue(engine.IsPlaying(first), "the first is still playing, and its handle still names it");
        Step(engine, 10);
        Assert.IsTrue(engine.Stop(first), "so it can be stopped");
        Step(engine, 2);
        Assert.AreEqual(0f, At(sink, 14 * StepFrames + 10));
        Assert.IsTrue(engine.IsPlaying(later));
    }

    [TestMethod]
    public void A_scheduled_sound_that_steals_a_voice_silences_the_sound_it_took_at_once()
    {
        var sink = new OfflineAudioSink(new AudioFormat(Rate, 2), Steps, 1024);
        var engine = new AudioEngine(sink, new AudioEngineSettings { UpdateRate = Steps, MaxVoices = 1 });
        var victim = engine.Play(Numbered(Rate, from: 1), new PlayParameters());
        Step(engine, 3);
        var scheduled = engine.Play(Numbered(4800, from: 1), new PlayParameters { Delay = 0.5 });
        Assert.IsFalse(engine.IsPlaying(victim), "its voice was taken");
        Step(engine, 3);
        Assert.AreEqual(0f, At(sink, 3 * StepFrames + 5), "and it is not heard from that step on: a handle that is gone names nothing that sounds");
        Step(engine, 30);
        Assert.AreEqual(1f, At(sink, 3 * StepFrames + Rate / 2));
        Assert.IsTrue(engine.IsPlaying(scheduled));
    }

    // ---- D5: changes before a scheduled start

    [TestMethod]
    public void A_seek_or_a_pitch_change_before_a_scheduled_start_does_not_end_the_sound_early()
    {
        var (engine, sink) = Open();
        var sought = engine.Play(Numbered(2 * Rate), new PlayParameters { Delay = 1, Pan = 0 });
        Assert.IsTrue(engine.Seek(sought, 0.5));
        var pitched = engine.Play(Numbered(2 * Rate), new PlayParameters { Delay = 1, Pan = 0, Volume = 0 });
        Assert.IsTrue(engine.SetPitch(pitched, 2));
        Step(engine, 119);
        Assert.IsTrue(engine.IsPlaying(pitched), "a second of waiting and a second at twice the speed");
        Step(engine, 1);
        Assert.IsFalse(engine.IsPlaying(pitched));
        Step(engine, 29);
        Assert.IsTrue(engine.IsPlaying(sought), "a second of waiting and the 1.5 s left after the seek");
        Step(engine, 1);
        Assert.IsFalse(engine.IsPlaying(sought));
        Assert.AreEqual(24000f, At(sink, Rate), "it started at its frame, from the place sought");
        Assert.AreEqual(2 * Rate - 1f, At(sink, Rate + 3 * Rate / 2 - 1), "and played to its end");
    }

    // ---- D6: the heard position

    [TestMethod]
    public void The_heard_position_is_what_the_output_held_a_delay_ago_and_never_goes_back()
    {
        var (engine, sink) = Open(delayFrames: 2048);
        var voice = engine.Play(Numbered(4 * Rate), new PlayParameters());
        double previous = double.NegativeInfinity;
        void Check(string when, bool sounding = true)
        {
            double heard = engine.HeardPosition(voice) * Rate;
            Assert.IsTrue(heard >= previous - 1e-6, $"{when}: the heard position went back from {previous} to {heard}");
            previous = heard;
            // What the device plays now is the frame the mixer rendered for this step: the last one it has rendered
            // (each sample of the clip is its own frame number).
            long frame = engine.Now - engine.DelayFrames - 1;
            if (sounding && frame >= 0 && frame < sink.Frames && heard > 2 && At(sink, frame) != 0)
                Assert.AreEqual(At(sink, frame), heard, 2.5, $"{when}: the heard position against the frame the output holds for that moment");
        }
        for (int i = 0; i < 10; i++) { Step(engine); Check("playing"); }
        engine.Pause(voice, true);
        for (int i = 0; i < 6; i++) { Step(engine); Check("paused"); }
        engine.Pause(voice, false);
        for (int i = 0; i < 6; i++) { Step(engine); Check("resumed"); }
        engine.SetPitch(voice, 2);
        for (int i = 0; i < 6; i++) { Step(engine); Check("twice as fast"); }
        engine.SetPitch(voice, 0.5f, seconds: 0.05);
        for (int i = 0; i < 8; i++) { Step(engine); Check("gliding down"); }
        engine.SetPitch(voice, 1);
        Step(engine, 4);
        Check("before the seek");

        // Across a seek: the old place until the seek is heard, then the new one.
        double before = engine.HeardPosition(voice) * Rate;
        engine.Seek(voice, 3.0);
        Assert.AreEqual(3.0, engine.Position(voice), 1e-9, "the position is the new place at once");
        Assert.AreEqual(before, engine.HeardPosition(voice) * Rate, 1e-6, "what is heard has not changed yet");
        Step(engine, 2);
        Assert.AreEqual(before + 2 * StepFrames, engine.HeardPosition(voice) * Rate, 1e-6, "the old place goes on for the length of the delay (2048 frames)");
        Step(engine, 1);
        Assert.AreEqual(3.0 * Rate + (3 * StepFrames - 2048), engine.HeardPosition(voice) * Rate, 1e-6, "then the new one, where it now is");
        previous = double.NegativeInfinity;
        for (int i = 0; i < 6; i++) { Step(engine); Check("after the seek"); }
    }

    [TestMethod]
    public void The_heard_position_of_a_loop_wraps_a_delay_after_the_position_does()
    {
        var (engine, _) = Open(delayFrames: 1000);
        var voice = engine.Play(Numbered(4000), new PlayParameters { Loop = true, LoopRegion = new LoopRegion(1000, 3000) });
        Step(engine, 4);
        Assert.AreEqual(1200.0, engine.Position(voice) * Rate, 1e-6, "3200 frames: past the end of the region once");
        Assert.AreEqual(2200.0, engine.HeardPosition(voice) * Rate, 1e-6, "a thousand frames behind: still before the seam");
        Step(engine, 2);
        Assert.AreEqual(1800.0, engine.HeardPosition(voice) * Rate, 1e-6);
    }

    // ---- D3 and the cost of seeking

    /// <summary>A decoder of numbered frames that counts how many of it were opened and how many frames each call decoded.</summary>
    private sealed class CountingDecoder(int frames, CountingDecoder.Counts counts) : IAudioStream
    {
        public sealed class Counts
        {
            public int Opened;
            public long Decoded;
        }
        private int _position;
        public int Channels => 1;
        public int SampleRate => Rate;
        public int Read(Span<float> buffer)
        {
            int n = Math.Min(buffer.Length, frames - _position);
            for (int i = 0; i < n; i++) buffer[i] = _position + i;
            _position += n;
            counts.Decoded += n;
            return n;
        }
        public void Rewind() => _position = 0;
        public void Dispose() { }
    }

    private static (ExactSeekStream Stream, CountingDecoder.Counts Counts) Counting(int frames)
    {
        var counts = new CountingDecoder.Counts();
        IAudioStream Make()
        {
            counts.Opened++;
            return new CountingDecoder(frames, counts);
        }
        return (new ExactSeekStream(Make(), Make, frames, null), counts);
    }

    [TestMethod]
    public void A_loop_after_a_long_intro_never_decodes_the_intro_at_the_seam()
    {
        // A 55 s intro and a loop of 3.7 s, in frames at 48 kHz.
        const int intro = 55 * Rate, loop = 37 * Rate / 10;
        var (stream, counts) = Counting(intro + loop);
        var (engine, sink) = Open();
        var voice = engine.Play(stream, new PlayParameters { Loop = true, LoopRegion = new LoopRegion(intro, intro + loop), StartAt = 54.0 });
        // It starts 54 s in, which is decoded over the first frames; then the seam comes 4.7 s later and every 3.7 s.
        // No single frame's work may decode the intro again.
        long worst = 0, started = -1;
        int wraps = 0;
        double last = engine.Position(voice);
        Assert.IsTrue(engine.IsSeeking(voice), "far into the stream: on its way");
        for (int step = 0; step < 60 * 24; step++)
        {
            long before = counts.Decoded;
            engine.Advance();
            engine.Pump();
            worst = Math.Max(worst, counts.Decoded - before);
            if (started < 0 && !engine.IsSeeking(voice)) started = engine.Now;
            double position = engine.Position(voice);
            if (position < last) wraps++;
            last = position;
        }
        Assert.IsTrue(started > 0 && started < 6 * Rate, $"it started at frame {started}");
        Assert.IsTrue(wraps >= 4, $"it wrapped {wraps} times");
        Assert.IsTrue(worst < 2 * Rate, $"the most decoded in one frame: {worst} frames ({worst / (double)Rate:F2} s of sound); the intro is {intro}");
        // And the seams are exact.
        Assert.AreEqual(0f, At(sink, started - 1), "silent while on its way");
        Assert.AreEqual(54f * Rate, At(sink, started), "then from the place asked for");
        int seam = (int)started + (int)((1 + 3.7) * Rate);
        Assert.AreEqual(intro + loop - 1f, At(sink, seam - 1));
        Assert.AreEqual((float)intro, At(sink, seam));
        Assert.AreEqual((float)intro, At(sink, seam + loop));
        Assert.AreEqual((float)intro, At(sink, seam + 2 * loop));
    }

    [TestMethod]
    public void A_region_too_short_to_stream_is_the_whole_stream()
    {
        foreach (var region in new[] { new LoopRegion(0, 1), new LoopRegion(11000, 11001), new LoopRegion(500, 600) })
        {
            var (stream, counts) = Counting(Rate / 2);
            var (engine, sink) = Open();
            var voice = engine.Play(stream, new PlayParameters { Loop = true, LoopRegion = region });
            var (_, milliseconds) = Measure(() => Step(engine, 60));
            Assert.IsTrue(counts.Opened < 12, $"{region}: {counts.Opened} decoders were opened in a second of play");
            Assert.IsTrue(counts.Decoded < 6 * Rate, $"{region}: {counts.Decoded} frames were decoded for a second of play");
            Assert.AreEqual(Rate / 2 - 1f, At(sink, Rate / 2 - 1), $"{region}: it plays to its end");
            Assert.AreEqual(0f, At(sink, Rate / 2), $"{region}: and loops over the whole of itself");
            Assert.IsTrue(engine.IsPlaying(voice));
            Assert.IsTrue(milliseconds < 2000, $"{region}: {milliseconds:F0} ms for a second of play");
        }
    }

    [TestMethod]
    public void A_wav_sampler_loop_without_a_length_is_no_loop()
    {
        var plain = new AudioClip([.. Enumerable.Range(0, 100).Select(i => i / 200f)], Rate).ToWav();
        byte[] Smpl(uint start, uint end)
        {
            var chunk = new byte[8 + 36 + 24];
            "smpl"u8.CopyTo(chunk);
            BitConverter.TryWriteBytes(chunk.AsSpan(4), 36 + 24);
            BitConverter.TryWriteBytes(chunk.AsSpan(8 + 28), 1);
            BitConverter.TryWriteBytes(chunk.AsSpan(8 + 36 + 8), start);
            BitConverter.TryWriteBytes(chunk.AsSpan(8 + 36 + 12), end);
            byte[] file = [.. plain, .. chunk];
            BitConverter.TryWriteBytes(file.AsSpan(4), file.Length - 8);
            return file;
        }
        Assert.IsNull(AudioClip.FromWav(Smpl(40, 40)).LoopRegion, "a loop that starts and ends on one frame is a marker, not a loop");
        Assert.IsNull(AudioClip.FromWav(Smpl(40, 10)).LoopRegion);
        Assert.AreEqual(new LoopRegion(40, 42), AudioClip.FromWav(Smpl(40, 41)).LoopRegion);
        Assert.IsNull(AudioClip.FromWav(Smpl(4_000_000_000, 4_100_000_000)).LoopRegion, "a loop past the end of the sound is none");
        Assert.IsNull(AudioClip.FromWav(Smpl(100, 150)).LoopRegion);
        Assert.AreEqual(new LoopRegion(40, 100), AudioClip.FromWav(Smpl(40, 5000)).LoopRegion, "an end past the sound's is the sound's");
    }

    // ---- D4: hostile files

    private static void FixCrc(byte[] file, int page)
    {
        int segments = file[page + 26], length = 27 + segments;
        for (int i = 0; i < segments; i++) length += file[page + 27 + i];
        file[page + 22] = file[page + 23] = file[page + 24] = file[page + 25] = 0;
        uint crc = 0;
        for (int i = 0; i < length; i++)
        {
            crc ^= (uint)file[page + i] << 24;
            for (int b = 0; b < 8; b++) crc = (crc & 0x80000000) != 0 ? (crc << 1) ^ 0x04C11DB7 : crc << 1;
        }
        BitConverter.TryWriteBytes(file.AsSpan(page + 22), crc);
    }

    private static byte[] OggWithLastGranule(long granule)
    {
        var file = AudioFixtures.Ogg;
        int last = -1;
        for (int i = 0; i + 4 <= file.Length; i++)
            if (file[i] == 'O' && file[i + 1] == 'g' && file[i + 2] == 'g' && file[i + 3] == 'S') last = i;
        BitConverter.TryWriteBytes(file.AsSpan(last + 6), granule);
        FixCrc(file, last);
        return file;
    }

    [TestMethod]
    [Timeout(60000)]
    public void An_ogg_file_that_lies_about_its_length_is_refused_at_once()
    {
        foreach (long granule in new[] { long.MaxValue, 8589934588, 1L << 40, -1, 0, 1L << 31, 50_000_000 })
        {
            var file = OggWithLastGranule(granule);
            foreach (var open in new Action[] { () => AudioDecoders.Decode(file, "ogg"), () => AudioDecoders.Open(file, "ogg").Dispose() })
            {
                Exception? thrown = null;
                var (bytes, milliseconds) = Measure(() =>
                {
                    try { open(); }
                    catch (Exception e) { thrown = e; }
                });
                Assert.IsInstanceOfType<InvalidDataException>(thrown, $"granule {granule}: {thrown?.GetType().Name ?? "decoded"}");
                Assert.IsTrue(bytes < 16 << 20, $"granule {granule}: {bytes >> 20} MB allocated");
                Assert.IsTrue(milliseconds < 2000, $"granule {granule}: {milliseconds:F0} ms");
            }
        }
        Assert.AreEqual(11025, AudioDecoders.Decode(OggWithLastGranule(11025), "ogg").Samples.Length, "the file as it was made still decodes");
        Assert.AreEqual(11025, AudioDecoders.Decode(AudioFixtures.Ogg, "ogg").Samples.Length);
    }

    [TestMethod]
    [Timeout(120000)]
    public void An_mp3_file_with_a_tag_larger_than_the_file_or_damaged_bytes_allocates_little_and_returns()
    {
        // An ID3v2 tag that claims 256 MB in front of the file.
        byte[] tagged = [.. "ID3"u8, 3, 0, 0, 0x7F, 0x7F, 0x7F, 0x7F, .. AudioFixtures.Mp3];
        var (tagBytes, _) = Measure(() => Assert.ThrowsExactly<InvalidDataException>(() => AudioDecoders.Decode(tagged, "mp3")));
        Assert.IsTrue(tagBytes < 16 << 20, $"a tag larger than the file: {tagBytes >> 20} MB allocated");
        // A tag of an honest size is skipped.
        byte[] honest = [.. "ID3"u8, 3, 0, 0, 0, 0, 0, 20, .. new byte[20], .. AudioFixtures.Mp3];
        Assert.AreEqual(AudioDecoders.Decode(AudioFixtures.Mp3, "mp3").Samples.Length, AudioDecoders.Decode(honest, "mp3").Samples.Length);
        Assert.ThrowsExactly<InvalidDataException>(() => AudioDecoders.Decode([.. "ID3"u8, 3, 0, 0, 0, 0, 0, 20, .. new byte[20]], "mp3"), "a tag and nothing else");

        // Every byte of the file changed in turn, to several values: an error or a decode, never much memory or time.
        var good = AudioFixtures.Mp3;
        long worstBytes = 0;
        double worstMilliseconds = 0;
        string worst = "";
        foreach (byte value in new byte[] { 0x00, 0xFF, 0x7F, 0x80 })
            for (int at = 0; at < good.Length; at++)
            {
                var file = (byte[])good.Clone();
                if (file[at] == value) continue;
                file[at] = value;
                var (bytes, milliseconds) = Measure(() =>
                {
                    try { AudioDecoders.Decode(file, "mp3"); }
                    catch (InvalidDataException) { }
                });
                if (bytes > worstBytes) (worstBytes, worst) = (bytes, $"byte {at} = {value}");
                worstMilliseconds = Math.Max(worstMilliseconds, milliseconds);
            }
        Assert.IsTrue(worstBytes < 32 << 20, $"the most allocated for one damaged file: {worstBytes >> 20} MB ({worst})");
        Assert.IsTrue(worstMilliseconds < 5000, $"the slowest: {worstMilliseconds:F0} ms");
    }

    [TestMethod]
    [Timeout(120000)]
    public void Ogg_and_wav_files_with_a_changed_byte_allocate_little_and_return()
    {
        foreach (var (format, good) in new[] { ("ogg", AudioFixtures.Ogg), ("wav", new AudioClip(new float[2000], Rate).ToWav()) })
        {
            long worstBytes = 0;
            string worst = "";
            foreach (byte value in new byte[] { 0x00, 0xFF, 0x7F })
                for (int at = 0; at < Math.Min(good.Length, 4100); at += format == "ogg" ? 1 : 1)
                {
                    var file = (byte[])good.Clone();
                    if (file[at] == value) continue;
                    file[at] = value;
                    var (bytes, _) = Measure(() =>
                    {
                        try { AudioDecoders.Decode(file, format); }
                        catch (Exception e) when (e is InvalidDataException or NotSupportedException) { }
                    });
                    if (bytes > worstBytes) (worstBytes, worst) = (bytes, $"byte {at} = {value}");
                }
            Assert.IsTrue(worstBytes < 32 << 20, $"{format}: the most allocated for one damaged file: {worstBytes >> 20} MB ({worst})");
        }
        // A fmt chunk cut short.
        var wav = new AudioClip(new float[100], Rate).ToWav();
        BitConverter.TryWriteBytes(wav.AsSpan(16), 4);
        Assert.ThrowsExactly<InvalidDataException>(() => AudioClip.FromWav(wav.AsSpan(0, 28)));
    }

    // ---- A real compressed file: the way back to a loop, and a seek spread over frames

    private const int LongRate = 11025;

    private static (AudioEngine Engine, OfflineAudioSink Sink) OpenLong()
    {
        var sink = new OfflineAudioSink(new AudioFormat(LongRate, 1), Steps, 512);
        return (new AudioEngine(sink, new AudioEngineSettings { UpdateRate = Steps }) { Deterministic = true }, sink);
    }

    [TestMethod]
    public void A_streamed_ogg_loop_far_into_the_file_is_exact_and_is_reached_by_the_decoders_own_seek()
    {
        var whole = AudioDecoders.Decode(AudioFixtures.OggLong, "ogg");
        Assert.AreEqual(new LoopRegion(30000, 38000), whole.LoopRegion);
        var stream = (ExactSeekStream)AudioDecoders.Open(AudioFixtures.OggLong, "ogg");
        var (engine, sink) = OpenLong();
        engine.Play(stream, new PlayParameters { Loop = true });
        Step(engine, 60 * 9);
        // Nine seconds: to frame 38000, then the 8000 frames of the region seven times over.
        for (int f = 0; f < sink.Frames; f += 7)
        {
            int source = f < 38000 ? f : 30000 + (f - 30000) % 8000;
            Assert.AreEqual(whole.Samples[source], sink.Samples[f], 1e-6f, $"output frame {f} is the file's frame {source}");
        }
        Assert.IsTrue(stream.Calibrated >= 5, $"after the first time round, the decoder for the way back was put at the loop's start by its own seek and the sound found there ({stream.Calibrated} times), not by decoding the intro again");
        Assert.AreEqual(0, engine.FailedStreams);
    }

    [TestMethod]
    public void A_far_seek_in_a_streamed_file_is_spread_over_frames_and_lands_on_the_exact_frame()
    {
        var whole = AudioDecoders.Decode(AudioFixtures.OggLong, "ogg");
        string? first = null;
        for (int run = 0; run < 2; run++)
        {
            var (engine, sink) = OpenLong();
            var voice = engine.Play(AudioDecoders.Open(AudioFixtures.OggLong, "ogg"), new PlayParameters());
            Step(engine, 30);
            Assert.IsFalse(engine.IsSeeking(voice));
            // Near: decoded at once.
            Assert.IsTrue(engine.Seek(voice, 0.1));
            Assert.IsFalse(engine.IsSeeking(voice), "a tenth of a second from the file's start is reached at once");
            Assert.AreEqual(0.1, engine.Position(voice), 1e-4);
            Step(engine, 6);
            // Far: three seconds in, twelve quarter seconds of decoding.
            double before = engine.Position(voice);
            long asked = engine.Now;
            Assert.IsTrue(engine.Seek(voice, 3.0));
            Assert.IsTrue(engine.IsSeeking(voice), "on its way");
            Assert.AreEqual(before, engine.Position(voice), 1e-9, "the sound goes on where it was until it gets there");
            int steps = 0;
            while (engine.IsSeeking(voice) && steps < 100)
            {
                Step(engine);
                steps++;
            }
            Assert.IsTrue(steps is > 3 and < 20, $"it arrived after {steps} frames");
            long arrival = engine.Now;
            Assert.AreEqual(3.0, engine.Position(voice), 1e-4, "there now");
            Step(engine, 20);
            int target = (int)(3.0 * LongRate);
            Assert.AreEqual(whole.Samples[(int)Math.Round(before * LongRate) + (int)(arrival - asked) - 1], sink.Samples[(int)arrival - 1], 1e-6f, "the old place played on, without a gap, to the frame before");
            for (int i = 0; i < 2000; i += 13)
                Assert.AreEqual(whole.Samples[target + i], sink.Samples[(int)arrival + i], 1e-6f, $"frame {i} after the arrival is the file's frame {target + i}");
            string run1 = $"{steps} {arrival}";
            first ??= run1;
            Assert.AreEqual(first, run1, "the same number of frames in every run: the way is counted in frames of sound, not in time");
        }
    }

    [TestMethod]
    public void A_stream_started_far_into_its_file_is_silent_until_it_gets_there_and_then_exact()
    {
        var whole = AudioDecoders.Decode(AudioFixtures.OggLong, "ogg");
        var (engine, sink) = OpenLong();
        var voice = engine.Play(AudioDecoders.Open(AudioFixtures.OggLong, "ogg"), new PlayParameters { StartAt = 2.5 });
        Assert.IsTrue(engine.IsSeeking(voice));
        Assert.AreEqual(2.5, engine.Position(voice), 1e-4, "it stands at the place asked for");
        int steps = 0;
        while (engine.IsSeeking(voice) && steps < 100)
        {
            Step(engine);
            Assert.AreEqual(2.5, engine.Position(voice), 1e-4);
            steps++;
        }
        long arrival = engine.Now;
        Step(engine, 30);
        Assert.IsTrue(sink.Samples.Take((int)arrival).All(s => s == 0), "silent on its way");
        int target = (int)(2.5 * LongRate);
        for (int i = 0; i < 4000; i += 13)
            Assert.AreEqual(whole.Samples[target + i], sink.Samples[(int)arrival + i], 1e-6f, $"frame {i} after it began");
        Assert.AreEqual(2.5 + 0.5, engine.Position(voice), 1e-4, "half a second later, half a second further");
        // To the end: 1.5 s of sound were left.
        Step(engine, 59);
        Assert.IsTrue(engine.IsPlaying(voice));
        Step(engine, 2);
        Assert.IsFalse(engine.IsPlaying(voice));
    }

    // ---- D9: a WAV that is not PCM


    [TestMethod]
    public void A_wav_that_is_not_pcm_is_not_supported_whichever_way_it_is_loaded()
    {
        var wav = new AudioClip(new float[100], Rate).ToWav();
        wav[20] = 2;   // ADPCM
        Assert.ThrowsExactly<NotSupportedException>(() => AudioClip.FromWav(wav));
        Assert.ThrowsExactly<NotSupportedException>(() => AudioDecoders.Decode(wav, "wav"));
        Assert.ThrowsExactly<NotSupportedException>(() => AudioDecoders.Open(wav, "wav"));
        var mixer = new AudioMixer(new CaptureAudioDevice());
        Assert.ThrowsExactly<NotSupportedException>(() => mixer.AddSound("s", new EncodedAudio(wav, "wav")));
        Assert.ThrowsExactly<NotSupportedException>(() => mixer.AddMusic("m", new EncodedAudio(wav, "wav")));
        string path = Path.Combine(Directory.CreateTempSubdirectory("audio-wav-").FullName, "adpcm.wav");
        File.WriteAllBytes(path, wav);
        Assert.ThrowsExactly<NotSupportedException>(() => mixer.AddSoundFile("s", path));
        Assert.ThrowsExactly<NotSupportedException>(() => mixer.AddMusicFile("m", path));
    }
}

using System.Runtime.CompilerServices;
using MicroEngine.Audio;
using MicroEngine.Content;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace MicroEngine.Tests.Audio;

/// <summary>
/// Formats and memory: one list of decoders for sound effects, music and the asset kinds, the same on every device;
/// OGG Vorbis and MP3 decoded in managed code; music streamed or decoded whole; and sounds and music released when
/// they are unregistered or when nothing holds a clip that was played without a name.
/// </summary>
[TestClass]
public sealed class AudioDecoderTests
{
    /// <summary>The amplitude of the tone at <paramref name="frequency"/> in a mono or the left channel of a clip, between two frames.</summary>
    private static double Tone(AudioClip clip, double frequency, int first, int last)
    {
        double real = 0, imaginary = 0;
        for (int f = first; f < last; f++)
        {
            double angle = 2 * Math.PI * frequency * f / clip.SampleRate, sample = clip.Samples[f * clip.Channels];
            real += sample * Math.Cos(angle);
            imaginary += sample * Math.Sin(angle);
        }
        return 2 * Math.Sqrt(real * real + imaginary * imaginary) / (last - first);
    }

    private static (AudioMixer Mixer, CaptureAudioDevice Device) Open()
    {
        var device = new CaptureAudioDevice(60, 22050);
        return (new AudioMixer(device), device);
    }

    private static void Step(AudioMixer mixer, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            mixer.Pump();
            mixer.Update(1 / 60f);
        }
    }

    // ---- The built-in formats

    [TestMethod]
    public void Wav_ogg_and_mp3_are_registered_and_named_without_regard_to_case_or_dot()
    {
        CollectionAssert.IsSubsetOf(new[] { "mp3", "ogg", "wav" }, AudioDecoders.Formats.ToList());
        Assert.IsTrue(AudioDecoders.Supports(".OGG"));
        Assert.IsTrue(AudioDecoders.Supports("Mp3"));
        Assert.IsFalse(AudioDecoders.Supports("flac"));
        Assert.IsFalse(AudioDecoders.Supports(null));
        Assert.IsFalse(AudioDecoders.Supports(""));
        using var device = new CaptureAudioDevice();
        CollectionAssert.IsSubsetOf(new[] { "mp3", "ogg", "wav" }, device.MusicFormats.ToList(), "every device streams them");
    }

    [TestMethod]
    public void An_ogg_file_decodes_whole_with_the_loop_its_comments_name()
    {
        var clip = AudioDecoders.Decode(AudioFixtures.Ogg, "ogg");
        Assert.AreEqual((22050, 1), (clip.SampleRate, clip.Channels));
        Assert.AreEqual(11025, clip.Samples.Length, "half a second");
        Assert.AreEqual(0.125, Tone(clip, 441, 2000, 9000), 0.01, "the tone it was made from, at its level");
        Assert.AreEqual(new LoopRegion(4410, 8820), clip.LoopRegion, "LOOPSTART and LOOPLENGTH");
    }

    [TestMethod]
    public void An_mp3_file_decodes_whole()
    {
        var clip = AudioDecoders.Decode(AudioFixtures.Mp3, ".MP3");
        Assert.AreEqual((22050, 1), (clip.SampleRate, clip.Channels));
        Assert.IsTrue(clip.Samples.Length is >= 11025 and < 11025 + 3 * 1152, $"half a second and the encoder's padding: {clip.Samples.Length}");
        Assert.AreEqual(0.125, Tone(clip, 441, 3000, 9000), 0.02, "the tone it was made from, at its level");
        Assert.IsNull(clip.LoopRegion);
    }

    [TestMethod]
    public void A_stream_of_each_format_seeks_to_the_frame_asked()
    {
        foreach (var (bytes, format, tolerance) in new[] { (AudioFixtures.Ogg, "ogg", 1e-6f), (AudioFixtures.Mp3, "mp3", 1e-6f) })
        {
            var whole = AudioDecoders.Decode(bytes, format);
            using var stream = (ISeekableAudioStream)AudioDecoders.Open(bytes, format);
            Assert.AreEqual(whole.Samples.Length, stream.Frames, $"{format}: its length");
            var read = new float[1000];
            foreach (int frame in new[] { 5000, 0, 9000, 5000 })
            {
                stream.Seek(frame);
                int count = 0;
                while (count < read.Length)
                {
                    int n = stream.Read(read.AsSpan(count));
                    if (n == 0) break;
                    count += n;
                }
                Assert.AreEqual(read.Length, count, $"{format}, from {frame}");
                for (int i = 0; i < read.Length; i += 7)
                    Assert.AreEqual(whole.Samples[frame + i], read[i], tolerance, $"{format}: frame {frame + i} after a seek to {frame}");
            }
            stream.Rewind();
            stream.Read(read);
            Assert.AreEqual(whole.Samples[10], read[10], tolerance, $"{format}: rewound");
        }
    }

    [TestMethod]
    public void Bytes_that_are_not_the_format_are_refused_and_an_unknown_format_is_named()
    {
        byte[][] bad = [[], [1, 2, 3], [.. Enumerable.Range(0, 4000).Select(i => (byte)(i * 31))]];
        foreach (string format in new[] { "wav", "ogg", "mp3" })
            foreach (var bytes in bad)
            {
                Assert.ThrowsExactly<InvalidDataException>(() => AudioDecoders.Decode(bytes, format), $"{format}, {bytes.Length} bytes");
                Assert.ThrowsExactly<InvalidDataException>(() => AudioDecoders.Open(bytes, format), $"{format}, {bytes.Length} bytes");
            }
        // One format's file under another's name.
        Assert.ThrowsExactly<InvalidDataException>(() => AudioDecoders.Decode(AudioFixtures.Ogg, "mp3"));
        Assert.ThrowsExactly<InvalidDataException>(() => AudioDecoders.Decode(AudioFixtures.Mp3, "ogg"));
        var unknown = Assert.ThrowsExactly<NotSupportedException>(() => AudioDecoders.Decode(AudioFixtures.Ogg, "flac"));
        StringAssert.Contains(unknown.Message, "flac");
        StringAssert.Contains(unknown.Message, "ogg");

        var (mixer, _) = Open();
        Assert.ThrowsExactly<InvalidDataException>(() => mixer.AddMusic("bad", new EncodedAudio([1, 2, 3], "mp3")));
        Assert.ThrowsExactly<InvalidDataException>(() => mixer.AddSound("bad", new EncodedAudio([1, 2, 3], "ogg")));
        Assert.ThrowsExactly<NotSupportedException>(() => mixer.AddMusic("odd", new EncodedAudio([1, 2, 3], "flac")));
        Assert.ThrowsExactly<NotSupportedException>(() => mixer.AddMusic("odd", new EncodedAudio([1, 2, 3], "flac"), mode: AudioLoadMode.Decoded));
        Assert.IsFalse(mixer.HasMusic("bad") || mixer.HasMusic("odd") || mixer.HasSound("bad"));
    }

    // ---- Through the mixer

    [TestMethod]
    public void Compressed_files_are_sound_effects()
    {
        var (mixer, device) = Open();
        mixer.SfxVolume = 1;
        mixer.AddSound("ogg", new EncodedAudio(AudioFixtures.Ogg, "ogg"));
        string path = Path.Combine(Directory.CreateTempSubdirectory("audio-decoders-").FullName, "tone.mp3");
        File.WriteAllBytes(path, AudioFixtures.Mp3);
        mixer.AddSoundFile("mp3", path);
        mixer.Play("ogg", pan: 0);
        mixer.Play("mp3", pan: 1);
        Step(mixer, 30);
        var heard = device.Output;
        Assert.AreEqual(0.125, Tone(heard, 441, 2000, 9000), 0.01, "the OGG effect on the left");
        var right = new AudioClip([.. heard.Samples.Where((_, i) => i % 2 == 1)], heard.SampleRate);
        Assert.AreEqual(0.125, Tone(right, 441, 3000, 9000), 0.02, "the MP3 effect on the right");
    }

    [TestMethod]
    public void Music_is_streamed_or_decoded_whole_and_sounds_the_same()
    {
        foreach (var (bytes, format) in new[] { (AudioFixtures.Ogg, "ogg"), (AudioFixtures.Mp3, "mp3") })
        {
            var outputs = new List<float[]>();
            foreach (var mode in new[] { AudioLoadMode.Streamed, AudioLoadMode.Decoded })
            {
                var (mixer, device) = Open();
                mixer.AddMusic("tune", new EncodedAudio(bytes, format), loop: false, mode);
                mixer.PlayMusic("tune", fadeSeconds: 0);
                Assert.AreEqual(mode == AudioLoadMode.Decoded, device.Engine.SetPitch(device.MusicVoice, 1), $"{format}, {mode}: only decoded music has a pitch to change");
                Step(mixer, 20);
                Assert.AreEqual("tune", mixer.CurrentMusic);
                Assert.IsTrue(mixer.MusicDuration is > 0.49 and < 0.7, $"{format}, {mode}: its length is known ({mixer.MusicDuration})");
                Step(mixer, 30);
                Assert.IsNull(mixer.CurrentMusic, $"{format}, {mode}: it ended");
                outputs.Add(device.Output.Samples);
                Assert.AreEqual(0.125, Tone(device.Output, 441, 3000, 9000), 0.02, $"{format}, {mode}");
            }
            for (int i = 0; i < 2 * 11000; i += 2)
                Assert.AreEqual(outputs[1][i], outputs[0][i], 1e-6f, $"{format}: sample {i / 2} is the same streamed and decoded");
        }
    }

    [TestMethod]
    public void Streamed_ogg_music_loops_over_the_region_its_file_names_and_seeks()
    {
        var (mixer, device) = Open();
        mixer.AddMusic("tune", new EncodedAudio(AudioFixtures.Ogg, "ogg"));
        mixer.PlayMusic("tune", fadeSeconds: 0);
        Step(mixer, 60);
        Assert.AreEqual("tune", mixer.CurrentMusic);
        // A second of steps: the first 0.4 s, then the region from 0.2 to 0.4 three times over.
        Assert.AreEqual(0.2, mixer.MusicPosition, 1e-9);
        var clip = AudioDecoders.Decode(AudioFixtures.Ogg, "ogg");
        var heard = device.Output.Samples;
        for (int f = 0; f < 22000; f += 13)
        {
            int source = f < 8820 ? f : 4410 + (f - 4410) % 4410;
            Assert.AreEqual(clip.Samples[source], heard[f * 2], 1e-6f, $"output frame {f} is the file's frame {source}: the seam is exact");
        }

        Assert.IsTrue(mixer.SeekMusic(0.05));
        Step(mixer, 6);
        Assert.AreEqual(0.15, mixer.MusicPosition, 1e-4, "sought into the intro, to a whole sample of the file");
    }

    [TestMethod]
    public void A_format_registered_by_the_game_loads_as_a_sound_and_as_music()
    {
        // A "format" of raw bytes, each a sample from -1 to 1 at 22050 Hz.
        AudioDecoders.Add(".Bytes-Test", bytes => new ByteStream(bytes));
        try
        {
            Assert.IsTrue(AudioDecoders.Supports("bytes-test"));
            byte[] file = [.. Enumerable.Range(0, 22050).Select(i => (byte)(i % 100 < 50 ? 191 : 64))];
            var clip = AudioDecoders.Decode(file, "bytes-test");
            Assert.AreEqual(22050, clip.Samples.Length);
            Assert.AreEqual(0.5f, clip.Samples[0], 0.01f);

            var (mixer, device) = Open();
            mixer.SfxVolume = 1;
            string path = Path.Combine(Directory.CreateTempSubdirectory("audio-decoders-").FullName, "square.bytes-test");
            File.WriteAllBytes(path, file);
            mixer.AddSoundFile("square", path);
            mixer.AddMusicFile("square-music", path);
            mixer.Play("square");
            Step(mixer, 3);
            Assert.AreEqual(0.5f, device.Output.Samples[20], 0.01f, "as a sound effect");
            mixer.PlayMusic("square-music", fadeSeconds: 0);
            Step(mixer, 70);
            Assert.AreEqual("square-music", mixer.CurrentMusic, "as streamed music, looping");
            CollectionAssert.Contains(device.MusicFormats.ToList(), "bytes-test");

            AudioDecoders.Add("bytes-test", _ => throw new FormatException("not today"));
            StringAssert.Contains(Assert.ThrowsExactly<InvalidDataException>(() => AudioDecoders.Decode(file, "bytes-test")).Message, "not today");
            AudioDecoders.Add("bytes-test", bytes => new ByteStream(bytes, channels: 6));
            StringAssert.Contains(Assert.ThrowsExactly<InvalidDataException>(() => AudioDecoders.Open(file, "bytes-test")).Message, "6 channels");
            AudioDecoders.Add("bytes-test", _ => null!);
            Assert.ThrowsExactly<InvalidDataException>(() => AudioDecoders.Open(file, "bytes-test"));
        }
        finally
        {
            Assert.IsTrue(AudioDecoders.Remove("BYTES-TEST"));
        }
        Assert.IsFalse(AudioDecoders.Remove("bytes-test"));
        Assert.IsFalse(AudioDecoders.Supports("bytes-test"));
        Assert.ThrowsExactly<ArgumentException>(() => AudioDecoders.Add(" ", bytes => new ByteStream(bytes)));
        Assert.ThrowsExactly<ArgumentNullException>(() => AudioDecoders.Add("x", null!));
    }

    private sealed class ByteStream(byte[] bytes, int channels = 1) : IAudioStream
    {
        private int _position;
        public int Channels => channels;
        public int SampleRate => 22050;
        public int Read(Span<float> buffer)
        {
            int n = Math.Min(buffer.Length, bytes.Length - _position);
            for (int i = 0; i < n; i++) buffer[i] = (bytes[_position + i] - 127.5f) / 127.5f;
            _position += n;
            return n;
        }
        public void Rewind() => _position = 0;
        public void Dispose() { }
    }

    // ---- Memory

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference Register(AudioMixer mixer, string name, bool music = false)
    {
        var clip = new AudioClip(new float[22050], 22050);
        if (music) mixer.AddMusic(name, clip);
        else mixer.AddSound(name, clip);
        return new WeakReference(clip);
    }

    private static bool Collected(WeakReference reference)
    {
        for (int i = 0; i < 5 && reference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        return !reference.IsAlive;
    }

    [TestMethod]
    public void A_sound_removed_from_the_mixer_is_released_and_its_place_reused()
    {
        var (mixer, device) = Open();
        var first = Register(mixer, "first");
        var second = Register(mixer, "second");
        Assert.AreEqual((2, 0), device.Loaded);
        Assert.IsFalse(Collected(first), "the mixer holds a registered sound");

        Assert.IsTrue(mixer.RemoveSound("first"));
        Assert.IsFalse(mixer.HasSound("first"));
        Assert.IsFalse(mixer.RemoveSound("first"), "already gone");
        Assert.AreEqual((1, 0), device.Loaded);
        Assert.IsTrue(Collected(first), "nothing holds it any more");
        Assert.ThrowsExactly<KeyNotFoundException>(() => mixer.Play("first"));

        var third = Register(mixer, "third");
        Assert.AreEqual((2, 0), device.Loaded, "its place is taken by the next sound");
        mixer.Play("third");
        mixer.Play("second");
        Step(mixer);
        Assert.AreEqual(2, device.Engine.ActiveVoices);

        // Registering a name again releases the sound that had it.
        var replaced = Register(mixer, "second");
        Assert.AreEqual((2, 0), device.Loaded);
        Step(mixer, 70);
        Assert.IsTrue(Collected(second), "replaced, and its voice has ended");
        Assert.IsFalse(Collected(third) || Collected(replaced));
    }

    [TestMethod]
    public void A_sound_removed_while_it_plays_plays_on_to_its_end()
    {
        var (mixer, device) = Open();
        mixer.SfxVolume = 1;
        mixer.AddSound("held", new AudioClip([.. Enumerable.Repeat(0.5f, 22050)], 22050));
        var voice = mixer.PlayVoice("held", new PlayParameters());
        Step(mixer, 3);
        mixer.RemoveSound("held");
        Step(mixer, 30);
        Assert.IsTrue(mixer.IsPlaying(voice));
        Assert.AreEqual(0.5f, device.Output.Samples[2 * 10000], "still heard after it was removed");
    }

    [TestMethod]
    public void Music_removed_from_the_mixer_stops_and_is_released()
    {
        foreach (bool silent in new[] { false, true })
        {
            var device = silent ? null : new CaptureAudioDevice(60, 22050);
            var mixer = new AudioMixer(device ?? (IAudioDevice)new SilentAudioDevice());
            var clip = Register(mixer, "theme", music: true);
            mixer.AddMusic("stream", new EncodedAudio(AudioFixtures.Ogg, "ogg"));
            if (device is not null) Assert.AreEqual((0, 2), device.Loaded);
            mixer.PlayMusic("theme", fadeSeconds: 0);
            Step(mixer, 5);
            Assert.AreEqual("theme", mixer.CurrentMusic);
            Assert.IsTrue(mixer.RemoveMusic("theme"));
            Assert.IsNull(mixer.CurrentMusic, $"silent {silent}: the music that was playing stops");
            Assert.IsFalse(mixer.HasMusic("theme"));
            Assert.IsFalse(mixer.RemoveMusic("theme"));
            Assert.ThrowsExactly<KeyNotFoundException>(() => mixer.PlayMusic("theme"));
            Step(mixer, 3);
            if (device is not null)
            {
                Assert.AreEqual((0, 1), device.Loaded);
                Assert.IsTrue(Collected(clip), "the device no longer holds its samples");
            }
            Assert.IsTrue(mixer.RemoveMusic("stream"));
            mixer.PlayMusic(RegisterAgain(mixer), fadeSeconds: 0.2f);
            Assert.IsTrue(mixer.RemoveMusic("again"), "also while it waits behind a fade or plays");
            Step(mixer, 30);
            Assert.IsNull(mixer.CurrentMusic);
        }
    }

    private static string RegisterAgain(AudioMixer mixer)
    {
        mixer.AddMusic("again", new AudioClip(new float[22050], 22050));
        return "again";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference PlayUnregistered(AudioMixer mixer, string name)
    {
        var clip = new AudioClip([.. Enumerable.Repeat(0.25f, 2205)], 22050) { Name = name };
        mixer.Play(clip, pan: 0);
        mixer.Play(clip, pan: 0);
        mixer.PlayVoice(clip, new PlayParameters { Pan = 1, Volume = 1 });
        return new WeakReference(clip);
    }

    [TestMethod]
    public void A_clip_played_without_a_name_is_held_only_while_it_plays()
    {
        var (mixer, device) = Open();
        mixer.SfxVolume = 1;
        var clip = PlayUnregistered(mixer, "sfx/step.wav");
        Assert.AreEqual((0, 0), device.Loaded, "nothing was registered");
        Step(mixer, 3);
        Assert.AreEqual((0.25f, 0.25f), (device.Output.Samples[2 * 100], device.Output.Samples[2 * 100 + 1]), "played once as an effect (not twice in one update) and once as a voice");
        CollectionAssert.AreEqual(new[] { "sfx/step.wav", "sfx/step.wav" }, device.Played.Select(p => p.Name).ToList());
        Step(mixer, 10);
        Assert.IsTrue(Collected(clip), "its voices ended: nothing in the mixer holds it");
        Assert.ThrowsExactly<KeyNotFoundException>(() => mixer.PlayVoice(new AudioClip(new float[10]), new PlayParameters { Bus = "none" }));

        var silent = new SilentAudioDevice();
        var headless = new AudioMixer(silent);
        Assert.IsTrue(headless.PlayVoice(new AudioClip(new float[10]) { Name = "a" }, new PlayParameters()).IsNone);
        headless.Play(new AudioClip(new float[10]));
        CollectionAssert.AreEqual(new[] { "a", "" }, silent.Played, "a silent device lists them by their names");
    }

    // ---- Assets

    [TestMethod]
    public void The_sound_asset_kind_decodes_every_registered_format_and_a_scope_releases_it()
    {
        string content = Directory.CreateTempSubdirectory("audio-content-").FullName;
        Directory.CreateDirectory(Path.Combine(content, "sfx"));
        File.WriteAllBytes(Path.Combine(content, "sfx", "tone.ogg"), AudioFixtures.Ogg);
        File.WriteAllBytes(Path.Combine(content, "sfx", "tone.mp3"), AudioFixtures.Mp3);
        File.WriteAllBytes(Path.Combine(content, "sfx", "bad.ogg"), [1, 2, 3]);
        using var assets = new AssetCache(new HeadlessGraphicsDevice(), content);
        var level = assets.CreateScope("level");
        var ogg = level.Sound("sfx/tone.ogg");
        Assert.AreEqual(11025, ogg.Samples.Length);
        Assert.AreEqual("sfx/tone.ogg", ogg.Name);
        Assert.AreEqual(new LoopRegion(4410, 8820), ogg.LoopRegion);
        Assert.AreEqual(22050, level.Sound("sfx/tone.mp3").SampleRate);
        Assert.AreEqual("sound", Assert.ThrowsExactly<AssetLoadException>(() => level.Sound("sfx/bad.ogg")).Kind);
        Assert.AreEqual("music", assets.Loaders.KindOf("sfx/tone.ogg"), "a file named without a kind is still music by its extension");
        Assert.AreEqual("sound", assets.Loaders.KindOf("sfx/tone.wav"));

        var (mixer, device) = Open();
        mixer.SfxVolume = 1;
        mixer.Play(ogg);
        Step(mixer, 30);
        Assert.AreEqual(0.125, Tone(device.Output, 441, 2000, 9000), 0.01);
        Assert.AreEqual("sfx/tone.ogg", device.Played[0].Name);
        level.Dispose();
        Assert.IsFalse(assets.IsLoaded("sfx/tone.ogg"), "the scope was the only holder");
    }
}

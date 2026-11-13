using MicroEngine.Audio;
using MicroEngine.Platform;

namespace MicroEngine.Tests.Audio;

/// <summary>
/// The game-facing mixer (<c>Game.Audio</c>) over the engine's voices: sounds played for a handle, changed while they
/// play, buses, and music voices beside the music track. Heard through <see cref="CaptureAudioDevice"/>, which renders
/// exactly what a device would play; the same calls on a device without the mixer do nothing and do not fail.
/// </summary>
[TestClass]
public sealed class AudioMixerVoiceTests
{
    private const int Rate = 48000, StepFrames = Rate / 60;

    private static AudioClip Constant(float value, int frames) => new([.. Enumerable.Repeat(value, frames)], Rate);
    private static AudioClip Ramp(int frames) => new([.. Enumerable.Range(0, frames).Select(i => i / (float)frames)], Rate);

    private static (AudioMixer Mixer, CaptureAudioDevice Device) Open()
    {
        var device = new CaptureAudioDevice(60, Rate);
        return (new AudioMixer(device), device);
    }

    private static void Step(AudioMixer mixer, int count = 1)
    {
        for (int i = 0; i < count; i++) mixer.Update(1 / 60f);
    }

    private static float Left(CaptureAudioDevice device, int frame) => device.Output.Samples[frame * 2];
    private static float Right(CaptureAudioDevice device, int frame) => device.Output.Samples[frame * 2 + 1];

    [TestMethod]
    public void A_voice_plays_exactly_its_parameters_and_is_controlled_through_its_handle()
    {
        var (mixer, device) = Open();
        mixer.AddSound("hum", Constant(0.5f, Rate));
        var voice = mixer.PlayVoice("hum", new PlayParameters { Loop = true, Volume = 0.5f, Pan = 0.25f });
        Assert.IsFalse(voice.IsNone);
        Assert.IsTrue(mixer.IsPlaying(voice));
        Step(mixer);
        Assert.AreEqual((0.25f, 0.125f), (Left(device, 5), Right(device, 5)), "its own volume (not scaled by SfxVolume) and pan");
        Assert.IsTrue(mixer.Set(voice, volume: 1f, pan: 1f));
        Step(mixer);
        Assert.AreEqual((0f, 0.5f), (Left(device, StepFrames + 5), Right(device, StepFrames + 5)), "changed from the step's frame");
        Assert.IsTrue(mixer.Pause(voice, true));
        Step(mixer);
        Assert.AreEqual(0f, Right(device, 2 * StepFrames + 5), "paused");
        Assert.IsTrue(mixer.IsPlaying(voice), "a paused sound keeps its voice");
        mixer.Pause(voice, false);
        Assert.IsTrue(mixer.Stop(voice, fadeSeconds: 1 / 60f));
        Step(mixer);
        Assert.AreEqual(0.25f, Right(device, 3 * StepFrames + StepFrames / 2), 1e-3f, "fading out");
        Step(mixer);
        Assert.AreEqual(0f, Right(device, 4 * StepFrames + 5));
        Assert.IsFalse(mixer.IsPlaying(voice));
        Assert.IsFalse(mixer.Set(voice, volume: 1f), "a stale handle does nothing");
        Assert.IsFalse(mixer.Stop(voice));
        Assert.IsFalse(mixer.Pause(voice, true));
        Assert.AreEqual("hum", device.Played[0].Name, "the capture lists it like any sound");
    }

    [TestMethod]
    public void A_voice_changes_pitch_while_it_plays()
    {
        var (mixer, device) = Open();
        int frames = 4 * StepFrames;
        mixer.AddSound("ramp", Ramp(frames));
        var voice = mixer.PlayVoice("ramp", new PlayParameters());
        Step(mixer);
        Assert.IsTrue(mixer.Set(voice, pitch: 2f));
        Step(mixer);
        Assert.AreEqual((StepFrames + 2 * 10) / (float)frames, Left(device, StepFrames + 10), 1e-6f, "twice as fast from the step's frame");
        Step(mixer);
        Assert.IsFalse(mixer.IsPlaying(voice), "and it ends sooner");
    }

    [TestMethod]
    public void Voices_of_one_sound_overlap_while_Play_still_merges_a_frames_repeats()
    {
        var (mixer, device) = Open();
        mixer.AddSound("tick", Constant(0.25f, StepFrames));
        mixer.SfxVolume = 1;
        var a = mixer.PlayVoice("tick", new PlayParameters());
        var b = mixer.PlayVoice("tick", new PlayParameters());
        Assert.AreNotEqual(a, b);
        Step(mixer);
        Assert.AreEqual(0.5f, Left(device, 5), "two voices of the same sound");
        mixer.Play("tick");
        mixer.Play("tick");
        Step(mixer);
        Assert.AreEqual(0.25f, Left(device, StepFrames + 5), "Play still plays a sound once per frame");
        Assert.AreEqual(32, mixer.Statistics.MaxVoices);
    }

    [TestMethod]
    public void Play_by_name_sounds_as_before()
    {
        var (mixer, device) = Open();
        mixer.AddSound("ding", Constant(0.5f, 10));
        mixer.Play("ding", volume: 0.5f, pan: 0.25f);
        Step(mixer);
        Assert.AreEqual((0.5f * 0.5f * 0.8f, 0.5f * 0.5f * 0.8f * 0.5f), (Left(device, 0), Right(device, 0)), "volume times SfxVolume (0.8), balance pan");
        Assert.AreEqual(0f, Left(device, 10), "once");
        Assert.AreEqual(("ding", 0.4f, 1f, 0.25f), (device.Played[0].Name, device.Played[0].Volume, device.Played[0].Pitch, device.Played[0].Pan));
    }

    [TestMethod]
    public void Buses_scale_the_voices_played_through_them()
    {
        var (mixer, device) = Open();
        mixer.AddSound("wind", Constant(0.5f, Rate));
        Assert.IsTrue(mixer.HasBus("sfx") && mixer.HasBus("music") && mixer.HasBus("master"));
        Assert.IsFalse(mixer.HasBus("ambience"));
        mixer.AddBus("ambience");
        mixer.AddBus("wind", parent: "ambience");
        mixer.SetBusVolume("ambience", 0.5f);
        Assert.AreEqual(0.5f, mixer.BusVolume("ambience"));
        Assert.AreEqual(1f, mixer.BusVolume("wind"));
        mixer.PlayVoice("wind", new PlayParameters { Loop = true, Bus = "wind" });
        Step(mixer);
        Assert.AreEqual(0.25f, Left(device, 5), "half through its parent");
        mixer.SetBusVolume("master", 0.5f);
        Step(mixer);
        Assert.AreEqual(0.125f, Left(device, StepFrames + 5), "the master scales everything, from the step's frame");
        mixer.SetBusVolume("wind", 9f);
        Assert.AreEqual(4f, mixer.BusVolume("wind"), "clamped to 0..4");
        Assert.ThrowsExactly<ArgumentException>(() => mixer.AddBus("wind"), "names are unique");
        Assert.ThrowsExactly<KeyNotFoundException>(() => mixer.AddBus("x", "none"));
        Assert.ThrowsExactly<KeyNotFoundException>(() => mixer.SetBusVolume("none", 1f));
        Assert.ThrowsExactly<KeyNotFoundException>(() => mixer.BusVolume("none"));
        Assert.ThrowsExactly<KeyNotFoundException>(() => mixer.PlayVoice("wind", new PlayParameters { Bus = "none" }));
        Assert.ThrowsExactly<KeyNotFoundException>(() => mixer.PlayVoice("missing", new PlayParameters()));
    }

    [TestMethod]
    public void Music_voices_play_beside_the_music_track()
    {
        var (mixer, device) = Open();
        mixer.AddMusic("theme", Constant(0.25f, Rate));
        mixer.AddMusic("rain", Constant(0.125f, Rate));
        mixer.PlayMusic("theme", fadeSeconds: 0);
        var rain = mixer.PlayMusicVoice("rain", new PlayParameters { Loop = true, Bus = "music" });
        Assert.IsFalse(rain.IsNone);
        Step(mixer);
        Assert.AreEqual("theme", mixer.CurrentMusic, "the music track is still the track");
        Assert.AreEqual(0.375f, Left(device, 5), "both heard");
        mixer.StopMusic(fadeSeconds: 0);
        Step(mixer);
        Assert.AreEqual(0.125f, Left(device, StepFrames + 5), "stopping the music leaves the voice");
        Assert.IsTrue(mixer.IsPlaying(rain));
        mixer.PlayMusic("theme", fadeSeconds: 0);
        Assert.IsTrue(mixer.Stop(rain));
        Step(mixer);
        Assert.AreEqual(0.25f, Left(device, 2 * StepFrames + 5), "and stopping the voice leaves the music");
        Assert.ThrowsExactly<KeyNotFoundException>(() => mixer.PlayMusicVoice("missing", new PlayParameters()));
    }

    /// <summary>A device with a format of its own: constant samples, as a decoder would stream them.</summary>
    private sealed class StreamingDevice : MixerAudioDevice
    {
        public StreamingDevice(IAudioSink sink) : base(sink, new AudioEngineSettings { UpdateRate = 60 }) => AddDecoder("raw", bytes => new ConstantStream(bytes[0] / 256f));

        private sealed class ConstantStream(float value) : IAudioStream
        {
            public int Channels => 1;
            public int SampleRate => Rate;
            public int Read(Span<float> buffer) { buffer.Fill(value); return buffer.Length; }
            public void Rewind() { }
            public void Dispose() { }
        }
    }

    [TestMethod]
    public void Streamed_music_plays_as_voices_side_by_side()
    {
        var sink = new OfflineAudioSink(new AudioFormat(Rate, 2), 60);
        var mixer = new AudioMixer(new StreamingDevice(sink));
        mixer.AddMusic("city", new EncodedAudio([64], "raw"));
        mixer.AddMusic("crowd", new EncodedAudio([32], "raw"));
        mixer.PlayMusic("city", fadeSeconds: 0);
        var crowd = mixer.PlayMusicVoice("crowd", new PlayParameters { Bus = "music", Volume = 0.5f });
        for (int i = 0; i < 3; i++) { mixer.Update(1 / 60f); mixer.Pump(); }
        Assert.AreEqual(0.25f + 0.0625f, sink.Samples[2 * (2 * StepFrames + 5)], "two streams decoded and mixed");
        Assert.IsTrue(mixer.IsPlaying(crowd));
        Assert.IsTrue(mixer.Set(crowd, pitch: 2f), "the voice plays; a stream keeps its own pitch");
    }

    [TestMethod]
    public void Without_the_engines_mixer_the_same_calls_work_and_nothing_plays()
    {
        var device = new SilentAudioDevice();
        var mixer = new AudioMixer(device);
        mixer.AddSound("hum", Constant(0.5f, 10));
        mixer.AddMusic("rain", Constant(0.5f, 10));
        var voice = mixer.PlayVoice("hum", new PlayParameters { Loop = true });
        Assert.IsTrue(voice.IsNone);
        CollectionAssert.AreEqual(new[] { "hum" }, device.Played, "tests still see the cue");
        Assert.IsTrue(mixer.PlayMusicVoice("rain", new PlayParameters()).IsNone);
        Assert.IsFalse(mixer.IsPlaying(voice));
        Assert.IsFalse(mixer.Set(voice, volume: 0.5f, pan: 0f, pitch: 2f));
        Assert.IsFalse(mixer.Stop(voice));
        Assert.IsFalse(mixer.Pause(voice, true));
        mixer.AddBus("ambience");
        mixer.SetBusVolume("ambience", 0.5f);
        Assert.AreEqual(0.5f, mixer.BusVolume("ambience"), "bus volumes are kept, so a settings screen reads them back");
        Assert.IsTrue(mixer.HasBus("ambience"));
        Assert.ThrowsExactly<ArgumentException>(() => mixer.AddBus("ambience"));
        Assert.ThrowsExactly<KeyNotFoundException>(() => mixer.AddBus("x", "none"));
        Assert.ThrowsExactly<KeyNotFoundException>(() => mixer.SetBusVolume("none", 1f));
        Assert.ThrowsExactly<KeyNotFoundException>(() => mixer.PlayVoice("hum", new PlayParameters { Bus = "none" }));
        Assert.ThrowsExactly<KeyNotFoundException>(() => mixer.PlayVoice("missing", new PlayParameters()));
    }
}

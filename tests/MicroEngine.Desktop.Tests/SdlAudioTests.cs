using MicroEngine.Audio;
using MicroEngine.NativeChecks;
using MicroEngine.Sdl;
using MicroEngine.Testing;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// The SDL audio device: through SDL's dummy driver (real streams and mixing, nothing heard), and on this system's
/// sound server, where a tone played through it is recorded from the monitor of a virtual PulseAudio sink
/// (MICROENGINE_AUDIO_MONITOR, set up by the native CI job), as the raylib device is checked (AudioTests).
/// </summary>
[TestClass]
[TestCategory("Native")]
public sealed class SdlAudioTests
{
    [TestMethod]
    [OSCondition(OperatingSystems.Linux, IgnoreMessage = "Signal capture uses a PulseAudio sink monitor.")]
    public void A_played_tone_reaches_the_sound_server() => ToneReachesTheSoundServer(music: false);

    [TestMethod]
    [OSCondition(OperatingSystems.Linux, IgnoreMessage = "Signal capture uses a PulseAudio sink monitor.")]
    public void A_tone_streamed_as_music_reaches_the_sound_server() => ToneReachesTheSoundServer(music: true);

    /// <summary>The default SDL device, as a game opens it, plays a tone the sink hears (SinkTone).</summary>
    private static void ToneReachesTheSoundServer(bool music)
    {
        var monitor = Environment.GetEnvironmentVariable("MICROENGINE_AUDIO_MONITOR");
        Native.Require(!string.IsNullOrEmpty(monitor), "MICROENGINE_AUDIO_MONITOR names no PulseAudio monitor source.");
        var device = SdlAudioDevice.TryCreate();
        try
        {
            Native.Require(device is SdlAudioDevice, "SDL opened no audio output device (the engine fell back to silence).");
            SinkTone.AssertReachesTheSink(device, monitor!, music);
        }
        finally { (device as IDisposable)?.Dispose(); }
    }

    [TestMethod]
    public void Streaming_music_and_playing_sounds_allocate_nothing_per_update()
    {
        var device = SdlAudioDevice.TryCreate("dummy");
        Native.Require(device is SdlAudioDevice, "SDL's dummy audio driver is unavailable here");
        try
        {
            var tone = new float[44100 * 2];
            for (int i = 0; i < tone.Length; i++) tone[i] = 0.3f * MathF.Sin(i * 0.05f);
            var music = device.LoadMusic("tone", new AudioClip(tone, 44100, 2));
            var shot = device.LoadSound("shot", new AudioClip(tone.AsSpan(0, 8820).ToArray()));
            device.PlayMusic(music);
            // Warm up: the first calls create SDL's streams and the reused buffers.
            for (int i = 0; i < 30; i++) { device.Update(); device.Play(shot, pan: i % 3 * 0.5f); Thread.Sleep(5); }

            // The game thread's side: updates and plays. The feeder thread renders; the mixer's render is checked on the
            // test thread by the engine's tests.
            int update = 0;
            AllocationAssert.None(() =>
            {
                device.Update();
                if (update % 6 == 0) device.Play(shot, volume: 0.5f, pitch: 1.2f, pan: update % 4 * 0.33f);
                update++;
                Thread.Sleep(2);
            }, warmup: 0, calls: 24, because: "updates streaming music and playing a sound every sixth update");
            Assert.AreEqual(5 * 24, update);
        }
        finally { (device as IDisposable)?.Dispose(); }
    }
}

using MicroEngine.Audio;
using MicroEngine.NativeChecks;

namespace MicroEngine.Raylib.Tests;

/// <summary>
/// Two separate claims: the raylib audio device opens on this system's sound server, and a sound played through it
/// actually comes out. The second records the monitor of a virtual PulseAudio sink (MICROENGINE_AUDIO_MONITOR, set up
/// by the native CI job) and looks for the tone that was played (<see cref="SinkTone"/>); it proves the signal path,
/// not what a listener hears.
/// </summary>
[TestClass]
[TestCategory("Native")]
[DoNotParallelize]
public sealed class AudioTests
{
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

    private static void ToneReachesTheSoundServer(bool music)
    {
        var monitor = Environment.GetEnvironmentVariable("MICROENGINE_AUDIO_MONITOR");
        Native.Require(!string.IsNullOrEmpty(monitor), "MICROENGINE_AUDIO_MONITOR names no PulseAudio monitor source.");
        var device = RaylibAudioDevice.TryCreate();
        try
        {
            Native.Require(device.IsAvailable, "No audio output device.");
            SinkTone.AssertReachesTheSink(device, monitor!, music);
        }
        finally { (device as IDisposable)?.Dispose(); }
    }
}

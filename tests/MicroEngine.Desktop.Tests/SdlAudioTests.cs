using MicroEngine.Audio;
using MicroEngine.Sdl;

namespace MicroEngine.Desktop.Tests;

/// <summary>The SDL audio device through SDL's dummy driver: real streams and mixing, nothing heard.</summary>
[TestClass]
[TestCategory("Native")]
public sealed class SdlAudioTests
{
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

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 120; i++)
            {
                device.Update();
                if (i % 6 == 0) device.Play(shot, volume: 0.5f, pitch: 1.2f, pan: i % 4 * 0.33f);
                Thread.Sleep(2);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.AreEqual(0, allocated, "120 updates streaming music and 20 sounds allocate nothing");
        }
        finally { (device as IDisposable)?.Dispose(); }
    }
}

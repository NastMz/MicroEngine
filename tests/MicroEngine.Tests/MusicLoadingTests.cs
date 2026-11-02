using MicroEngine.Audio;

namespace MicroEngine.Tests;

/// <summary>PERF-1: songs registered with <see cref="AudioMixer.AddMusic(string, Song, bool)"/> render off the loading path.</summary>
[TestClass]
public class MusicLoadingTests
{
    private static Song Tune() => new Song(120, 1).Track(new Instrument(Wave.Square, 0.3f), "C4 E4 G4 C5");

    [TestMethod]
    public void Songs_render_in_the_background_and_play_the_same_samples()
    {
        var device = new CaptureAudioDevice();
        var mixer = new AudioMixer(device);
        mixer.AddMusic("tune", Tune(), loop: false);
        Assert.IsTrue(mixer.HasMusic("tune"), "registered before it finished rendering");
        Assert.IsNull(device.CurrentMusic, "nothing reaches the device until it plays");
        mixer.PlayMusic("tune", fadeSeconds: 0);
        Assert.AreEqual("tune", mixer.CurrentMusic);
        for (int i = 0; i < 30; i++) mixer.Update(1f / 60);

        var expected = new CaptureAudioDevice();
        var reference = new AudioMixer(expected);
        reference.AddMusic("tune", Tune().Render(), loop: false);
        reference.PlayMusic("tune", fadeSeconds: 0);
        for (int i = 0; i < 30; i++) reference.Update(1f / 60);
        CollectionAssert.AreEqual(expected.Output.Samples, device.Output.Samples);

        Assert.ThrowsExactly<KeyNotFoundException>(() => mixer.PlayMusic("missing"));
        mixer.AddMusic("tune", Tune().Render());
        Assert.IsTrue(mixer.HasMusic("tune"), "a clip registered later replaces the song");
    }
}

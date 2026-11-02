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

    [TestMethod]
    public void A_song_that_cannot_be_rendered_fails_when_registered_not_when_it_plays()
    {
        var mixer = new AudioMixer(new CaptureAudioDevice());
        var badNote = new Song(120).Track(new Instrument(Wave.Square, 0.3f), "C4 H9 G4");
        var badDrum = new Song(120).Drums("k . x .");
        var empty = new Song(120);
        Assert.ThrowsExactly<FormatException>(() => mixer.AddMusic("note", badNote));
        Assert.ThrowsExactly<FormatException>(() => mixer.AddMusic("drum", badDrum));
        Assert.ThrowsExactly<InvalidOperationException>(() => mixer.AddMusic("empty", empty));
        Assert.IsFalse(mixer.HasMusic("note") || mixer.HasMusic("drum") || mixer.HasMusic("empty"), "nothing registered");
        // The same errors Song.Render reports.
        Assert.ThrowsExactly<FormatException>(() => badNote.Render());
        Assert.ThrowsExactly<FormatException>(() => badDrum.Render());
        Assert.ThrowsExactly<InvalidOperationException>(() => empty.Render());
    }
}

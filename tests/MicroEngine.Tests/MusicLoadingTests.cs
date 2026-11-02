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

    private static Song Long() => new Song(140).Track(Instrument.Lead, "C4 E4 G4 C5 . G4 E4 -", "D4 F4 A4 D5 . A4 F4 -")
        .Track(Instrument.Bass, "C2 - . C2 G2 - . G2").Drums("k . s . k k s h");

    [TestMethod]
    public void Rendering_in_slices_gives_the_same_samples_as_rendering_at_once()
    {
        var slices = Long().RenderInSlices();
        int count = 0;
        AudioClip? clip = null;
        while (clip is null && slices.MoveNext()) { clip = slices.Current; count++; }
        Assert.IsTrue(count > 20, $"many small slices ({count})");
        CollectionAssert.AreEqual(Long().Render().Samples, clip!.Samples);
    }

    [TestMethod]
    [DoNotParallelize]
    public void Without_threads_songs_render_a_few_milliseconds_per_update_and_play_unchanged()
    {
        bool threads = AudioMixer.RenderSongsOnThreads;
        var budget = AudioMixer.SliceBudget;
        AudioMixer.RenderSongsOnThreads = false;
        AudioMixer.SliceBudget = TimeSpan.FromTicks(1); // one slice per update, whatever the machine
        try
        {
            var device = new CaptureAudioDevice();
            var mixer = new AudioMixer(device);
            mixer.AddMusic("first", Long(), loop: false);
            mixer.AddMusic("second", Tune(), loop: false);
            Assert.IsTrue(mixer.HasMusic("first") && mixer.HasMusic("second"));
            mixer.Update(1f / 60);
            mixer.Update(1f / 60);
            Assert.IsNull(device.CurrentMusic, "rendering does not play anything");

            // Asked for before its turn: the rest renders at once, and it sounds as if rendered in Load.
            mixer.PlayMusic("second", fadeSeconds: 0);
            for (int i = 0; i < 30; i++) mixer.Update(1f / 60);
            var expected = new CaptureAudioDevice();
            var reference = new AudioMixer(expected);
            reference.AddMusic("second", Tune().Render(), loop: false);
            reference.Update(1f / 60);
            reference.Update(1f / 60);
            reference.PlayMusic("second", fadeSeconds: 0);
            for (int i = 0; i < 30; i++) reference.Update(1f / 60);
            CollectionAssert.AreEqual(expected.Output.Samples, device.Output.Samples);

            // The first song keeps rendering a slice per update until it is ready.
            Assert.AreEqual(1, mixer.SlicedSongs);
            int updates = 0;
            while (mixer.SlicedSongs > 0 && updates < 100_000) { mixer.Update(1f / 60); updates++; }
            Assert.AreEqual(0, mixer.SlicedSongs, "rendered by updates alone");
            Assert.IsTrue(updates > 20, $"over many updates ({updates})");
            mixer.PlayMusic("first", fadeSeconds: 0);
            Assert.AreEqual("first", mixer.CurrentMusic);
        }
        finally
        {
            AudioMixer.RenderSongsOnThreads = threads;
            AudioMixer.SliceBudget = budget;
        }
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

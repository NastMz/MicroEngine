using MicroEngine.Audio;
using MicroEngine.Content;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace MicroEngine.Tests.Audio;

/// <summary>
/// Sounds and music as asset kinds: loaded through <c>Game.Assets</c> from the content source (any source, a zip
/// archive included), shared and released by scopes like any asset, and played by <c>Game.Audio</c>.
/// </summary>
[TestClass]
public sealed class AudioAssetTests
{
    private static string Repository()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "MicroEngine.slnx"))) return dir.FullName;
        throw new InvalidOperationException("The repository root was not found.");
    }

    private static string NeonRiftContent => Path.Combine(Repository(), "games", "NeonRift", "Content");

    [TestMethod]
    public void A_sound_from_content_is_shared_by_scopes_and_played_by_the_mixer()
    {
        var assets = new AssetCache(new HeadlessGraphicsDevice(), NeonRiftContent);
        var wav = assets.Content.Files("sfx").First(f => f.EndsWith(".wav", StringComparison.Ordinal));
        var level = assets.CreateScope("level");
        var clip = level.Sound(wav);
        Assert.AreSame(clip, assets.Sound(wav), "one decoded clip for every scope");
        Assert.AreEqual(1, assets.Count);

        var device = new CaptureAudioDevice();
        var mixer = new AudioMixer(device);
        mixer.AddSound("shot", clip);
        mixer.Play("shot", volume: 1);
        mixer.Update(1f / 60);
        Assert.IsTrue(device.Output.Peak > 0, "the content's sound is heard");
        level.Dispose();
        Assert.IsTrue(assets.IsLoaded(wav), "the root still holds it");
        Assert.AreEqual("sound", Assert.ThrowsExactly<AssetLoadException>(() => assets.Sound("sfx/none.wav")).Kind);
    }

    [TestMethod]
    public void Music_from_content_streams_through_the_mixer_on_every_backend()
    {
        var assets = new AssetCache(new HeadlessGraphicsDevice(), NeonRiftContent);
        var music = assets.Music("music/city.ogg");
        Assert.AreEqual("ogg", music.Format);
        Assert.AreSame(music, assets.Music("music/city.ogg"));

        var device = new CaptureAudioDevice();
        var mixer = new AudioMixer(device);
        mixer.AddMusic("city", music);
        Assert.IsTrue(mixer.HasMusic("city"));
        mixer.PlayMusic("city", fadeSeconds: 0);
        for (int i = 0; i < 30; i++)
        {
            mixer.Update(1f / 60);
            mixer.Pump();
        }
        Assert.AreEqual("city", mixer.CurrentMusic);
        Assert.IsTrue(device.Output.Peak > 0.01f, $"OGG music is decoded by the engine's mixer (peak {device.Output.Peak})");
        Assert.ThrowsExactly<NotSupportedException>(() => mixer.AddMusic("odd", new EncodedAudio([1, 2, 3], "flac")));
    }
}

using System.Numerics;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.UI;

namespace MicroEngine.Tests;

[TestClass]
public sealed class GraphicsTests
{
    [TestMethod]
    public void Color_parses_hex_and_interpolates()
    {
        Assert.AreEqual(new Color(255, 128, 0), Color.Hex("#ff8000"));
        Assert.AreEqual(new Color(1, 2, 3, 4), Color.Hex("01020304"));
        Assert.AreEqual(new Color(128, 128, 128), Color.Lerp(Color.Black, Color.White, 0.5f));
        Assert.ThrowsExactly<FormatException>(() => Color.Hex("#12345"));
    }

    [TestMethod]
    public void Pixel_image_paints_char_maps_and_outlines()
    {
        var palette = Art.Palette("x=#ff0000");
        var image = new PixelImage(5, 5).Paint(2, 2, palette, "x");
        image.Outline(Color.Black);
        Assert.AreEqual(Color.Hex("ff0000"), image[2, 2]);
        Assert.AreEqual(Color.Black, image[1, 2]);
        Assert.AreEqual(Color.Black, image[2, 3]);
        Assert.AreEqual(Color.Transparent, image[1, 1], "4-neighbour outline leaves diagonals empty.");
    }

    [TestMethod]
    public void Pixel_image_rotates_flips_and_silhouettes()
    {
        var image = new PixelImage(3, 2);
        image[0, 0] = Color.White;
        var rotated = image.Rotate90();
        Assert.AreEqual((2, 3), (rotated.Width, rotated.Height));
        Assert.AreEqual(Color.White, rotated[1, 0], "Top-left moves to top-right after a clockwise turn.");
        Assert.AreEqual(Color.White, image.FlipY()[0, 1]);
        var shadow = image.Silhouette(Color.Hex("ff000080"));
        Assert.AreEqual(new Color(255, 0, 0, 128), shadow[0, 0]);
        Assert.AreEqual(Color.Transparent, shadow[1, 1]);
    }

    [TestMethod]
    public void Font_has_a_copyright_glyph()
    {
        var font = BitmapFont.CreateDefault(null);
        Assert.AreEqual(font.Advance('O'), font.Advance('©'));
    }

    [TestMethod]
    public void Atlas_packs_every_sprite_without_overlap()
    {
        var builder = new SpriteAtlasBuilder();
        for (int i = 0; i < 40; i++) builder.Add($"s{i}", new PixelImage(8 + i % 9, 6 + i % 13));
        builder.AddFrames("walk", [new PixelImage(16, 16), new PixelImage(16, 16)]);
        var atlas = builder.Build(new HeadlessGraphicsDevice());
        var rects = atlas.Names.Select(n => atlas[n].Source).ToList();
        for (int a = 0; a < rects.Count; a++)
        {
            Assert.IsTrue(rects[a].Right <= atlas.Texture.Width && rects[a].Bottom <= atlas.Texture.Height);
            for (int b = a + 1; b < rects.Count; b++) Assert.IsFalse(rects[a].Intersects(rects[b]));
        }
        Assert.AreEqual(2, atlas.Frames("walk").Length);
        Assert.ThrowsExactly<ArgumentException>(() => new SpriteAtlasBuilder().Add("a", new PixelImage(1, 1)).Add("a", new PixelImage(1, 1)));
    }

    [TestMethod]
    public void Font_is_proportional_and_wraps_on_word_boundaries()
    {
        var font = BitmapFont.CreateDefault(null);
        Assert.IsTrue(font.MeasureWidth("i") < font.MeasureWidth("m"));
        Assert.AreEqual(font.MeasureWidth("ab"), font.Advance('a') + font.Advance('b') - font.Spacing);
        var lines = font.Wrap("the quick brown fox jumps over the lazy dog", 60);
        Assert.IsTrue(lines.Count > 1);
        Assert.IsTrue(lines.All(l => font.MeasureWidth(l) <= 60));
        Assert.AreEqual("the quick brown fox jumps over the lazy dog", string.Join(" ", lines));
    }

    [TestMethod]
    public void Camera_follows_within_bounds_and_snaps_to_pixels()
    {
        var camera = new Camera2D(320, 180) { Bounds = new RectF(0, 0, 1000, 180), FollowSharpness = 0 };
        camera.Follow(new Vector2(900, 90), 1 / 60f);
        Assert.AreEqual(1000 - 320, camera.Position.X, 0.001f, "Clamped to the right edge.");
        camera.Follow(new Vector2(10, 90), 1 / 60f);
        Assert.AreEqual(0, camera.Position.X, 0.001f);
        camera.Position = new Vector2(10.6f, 0);
        Assert.AreEqual(-11, camera.RenderOffset.X);
    }

    [TestMethod]
    public void Camera_dead_zone_ignores_small_target_motion()
    {
        var camera = new Camera2D(320, 180) { DeadZone = new Vector2(20, 10), FollowSharpness = 0 };
        camera.CenterOn(new Vector2(500, 300));
        var before = camera.Position;
        camera.Follow(new Vector2(515, 305), 1 / 60f);
        Assert.AreEqual(before, camera.Position);
        camera.Follow(new Vector2(560, 300), 1 / 60f);
        Assert.AreEqual(before.X + 40, camera.Position.X, 0.001f);
    }

    [TestMethod]
    public void Camera_shake_decays_to_rest()
    {
        var camera = new Camera2D(320, 180);
        camera.Shake(1);
        camera.Update(1 / 60f);
        Assert.AreNotEqual(Vector2.Zero, camera.ShakeOffset);
        for (int i = 0; i < 120; i++) camera.Update(1 / 60f);
        Assert.AreEqual(Vector2.Zero, camera.ShakeOffset);
    }

    [TestMethod]
    public void Animator_loops_and_one_shot_clips_finish()
    {
        var atlas = new SpriteAtlasBuilder().AddFrames("f", Enumerable.Range(0, 4).Select(_ => new PixelImage(2, 2))).Build(new HeadlessGraphicsDevice());
        var frames = atlas.Frames("f");
        var animator = new Animator().Add("loop", new AnimationClip(frames, 10)).Add("once", new AnimationClip(frames, 10, loop: false));
        animator.Update(0.45f);
        Assert.AreSame(frames[0], animator.Frame, "0.45 s at 10 fps wraps to frame 0 of 4.");
        animator.Play("once");
        animator.Update(1f);
        Assert.IsTrue(animator.Finished);
        Assert.AreSame(frames[3], animator.Frame);
    }

    [TestMethod]
    public void Particles_expire_after_their_life()
    {
        var particles = new ParticleSystem(16);
        particles.Emit(Vector2.Zero, new ParticleSpec { Life = (0.1f, 0.1f) }, 40);
        Assert.AreEqual(16, particles.Count, "Capacity bounds the pool.");
        for (int i = 0; i < 10; i++) particles.Update(0.02f);
        Assert.AreEqual(0, particles.Count);
    }

    [TestMethod]
    public void Typewriter_reveals_progressively_and_reports_characters()
    {
        var typewriter = new Typewriter { CharactersPerSecond = 10 };
        var revealed = new List<char>();
        typewriter.Revealed += revealed.Add;
        typewriter.Start("hi you");
        typewriter.Update(0.25f);
        Assert.AreEqual("hi", typewriter.Visible);
        typewriter.Finish();
        Assert.IsTrue(typewriter.Complete);
        CollectionAssert.AreEqual(new[] { 'h', 'i' }, revealed);
    }
}

[TestClass]
public sealed class AudioTests
{
    [TestMethod]
    public void Note_frequencies_follow_equal_temperament()
    {
        Assert.AreEqual(440f, Synth.NoteFrequency("A4"), 0.01f);
        Assert.AreEqual(261.63f, Synth.NoteFrequency("C4"), 0.01f);
        Assert.AreEqual(Synth.NoteFrequency("C#4"), Synth.NoteFrequency("Db4"), 0.001f);
        Assert.ThrowsExactly<FormatException>(() => Synth.NoteFrequency("H2"));
    }

    [TestMethod]
    public void Sfx_has_the_requested_length_and_stays_in_range()
    {
        var clip = Synth.Render(new SfxParams { Wave = Wave.Noise, Attack = 0.01f, Sustain = 0.1f, Decay = 0.09f, Volume = 1 });
        Assert.AreEqual(0.2, clip.Duration, 0.001);
        Assert.IsTrue(clip.Peak is > 0.1f and <= 1f);
    }

    [TestMethod]
    public void Song_length_is_steps_times_step_duration_and_loops_shorter_tracks()
    {
        var song = new Song(120, 4).Track(Instrument.Lead, "C4 - E4 . G4 - - .").Drums("k h");
        Assert.AreEqual(8, song.LengthInSteps);
        var clip = song.Render();
        Assert.AreEqual(2, clip.Channels);
        Assert.AreEqual(8 * 0.125, clip.Duration, 0.001);
        Assert.IsTrue(clip.Samples.All(s => s is >= -1 and <= 1));
    }

    [TestMethod]
    public void Wav_encoding_has_a_valid_header()
    {
        var wav = new AudioClip(new float[100], 22050, 1).ToWav();
        Assert.AreEqual("RIFF", System.Text.Encoding.ASCII.GetString(wav, 0, 4));
        Assert.AreEqual(44 + 200, wav.Length);
        Assert.AreEqual(22050, BitConverter.ToInt32(wav, 24));
    }

    [TestMethod]
    public void Mixer_plays_named_sounds_once_per_frame_and_crossfades_music()
    {
        var device = new SilentAudioDevice();
        var mixer = new AudioMixer(device);
        mixer.AddSound("coin", new AudioClip(new float[10]));
        mixer.AddMusic("a", new AudioClip(new float[10]));
        mixer.AddMusic("b", new AudioClip(new float[10]));
        mixer.Play("coin");
        mixer.Play("coin");
        Assert.AreEqual(1, device.Played.Count, "Duplicate triggers in one frame are merged.");
        mixer.Update(0.016f);
        mixer.Play("coin");
        Assert.AreEqual(2, device.Played.Count);

        mixer.PlayMusic("a");
        Assert.AreEqual("a", mixer.CurrentMusic);
        mixer.PlayMusic("b", fadeSeconds: 0.2f);
        Assert.AreEqual("a", mixer.CurrentMusic, "Old track keeps playing while fading out.");
        for (int i = 0; i < 20; i++) mixer.Update(0.016f);
        Assert.AreEqual("b", mixer.CurrentMusic);
        Assert.ThrowsExactly<KeyNotFoundException>(() => mixer.Play("missing"));
        Assert.IsFalse(mixer.TryPlay("missing"));
    }

    /// <summary>Counts how often music starts from the beginning.</summary>
    private sealed class MusicStarts : IAudioDevice
    {
        private readonly SilentAudioDevice _inner = new();
        public int Starts { get; private set; }
        public float Volume { get; private set; } = 1;
        public bool IsAvailable => false;
        public SoundHandle LoadSound(string name, AudioClip clip) => _inner.LoadSound(name, clip);
        public MusicHandle LoadMusic(string name, AudioClip clip, bool loop = true) => _inner.LoadMusic(name, clip, loop);
        public MusicHandle LoadMusicStream(string name, byte[] encoded, string format, bool loop = true) => _inner.LoadMusicStream(name, encoded, format, loop);
        public void Play(SoundHandle sound, float volume = 1f, float pitch = 1f, float pan = 0.5f) => _inner.Play(sound, volume, pitch, pan);
        public void PlayMusic(MusicHandle music, float volume = 1f) { Starts++; Volume = volume; _inner.PlayMusic(music, volume); }
        public void StopMusic() => _inner.StopMusic();
        public void SetMusicVolume(float volume) => Volume = volume;
        public void PauseMusic(bool paused) => _inner.PauseMusic(paused);
        public MusicHandle? CurrentMusic => _inner.CurrentMusic;
        public void Update() { }
    }

    [TestMethod]
    public void Asking_again_for_the_track_that_is_fading_out_keeps_it_playing()
    {
        // A scene that starts a track switch and is left before the fade ends (a menu opened and closed quickly)
        // asks for the playing track again: it must fade back in, not restart from the beginning.
        var device = new MusicStarts();
        var mixer = new AudioMixer(device);
        mixer.AddMusic("a", new AudioClip(new float[10]));
        mixer.AddMusic("b", new AudioClip(new float[10]));
        mixer.PlayMusic("a", fadeSeconds: 0);
        mixer.PlayMusic("b", fadeSeconds: 0.4f);
        for (int i = 0; i < 6; i++) mixer.Update(0.016f);
        mixer.PlayMusic("a", fadeSeconds: 0.4f);
        for (int i = 0; i < 60; i++) mixer.Update(0.016f);
        Assert.AreEqual("a", mixer.CurrentMusic);
        Assert.AreEqual(1, device.Starts, "track a was never restarted");
        Assert.AreEqual(1f, device.Volume, 0.001f, "back to full volume");
        // The same holds after a fade to silence.
        mixer.StopMusic(0.4f);
        mixer.Update(0.016f);
        mixer.PlayMusic("a");
        for (int i = 0; i < 60; i++) mixer.Update(0.016f);
        Assert.AreEqual(1, device.Starts);
        Assert.AreEqual("a", mixer.CurrentMusic);
    }

    [TestMethod]
    public void Save_store_round_trips_and_survives_corruption()
    {
        var dir = Path.Combine(Path.GetTempPath(), "microengine-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new SaveStore("test", dir);
            Assert.IsNull(store.Load<Progress>("slot"));
            store.Save("slot", new Progress { Level = 3, Name = "Pip" });
            var loaded = store.Load<Progress>("slot");
            Assert.AreEqual(3, loaded!.Level);
            Assert.AreEqual("Pip", loaded.Name);
            File.WriteAllText(Path.Combine(dir, "slot.json"), "{ not json");
            Assert.IsNull(store.Load<Progress>("slot"));
            Assert.AreEqual("{ not json", File.ReadAllText(Path.Combine(dir, "slot.corrupt.json")), "Corrupt data is kept aside, not lost on the next save.");
            Assert.IsFalse(store.Exists("slot"));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    /// <summary>A non-file medium, like the browser's localStorage.</summary>
    private sealed class MemoryStorage : ISaveStorage
    {
        public Dictionary<string, string> Slots { get; } = [];
        public string Location => "memory";
        public string? Read(string slot) => Slots.GetValueOrDefault(slot);
        public void Write(string slot, string text) => Slots[slot] = text;
        public bool Exists(string slot) => Slots.ContainsKey(slot);
        public void Delete(string slot) => Slots.Remove(slot);
        public void SetAside(string slot)
        {
            if (Slots.Remove(slot, out var text)) Slots[slot + ".corrupt"] = text;
        }
    }

    [TestMethod]
    public void Save_store_round_trips_and_survives_corruption_on_any_storage()
    {
        var storage = new MemoryStorage();
        var store = new SaveStore(storage);
        Assert.IsNull(store.Load<Progress>("slot"));
        store.Save("slot", new Progress { Level = 3, Name = "Pip" });
        Assert.IsTrue(store.Exists("slot"));
        Assert.AreEqual("Pip", store.Load<Progress>("slot")!.Name);
        storage.Slots["slot"] = "{ not json";
        Assert.IsNull(store.Load<Progress>("slot"));
        Assert.AreEqual("{ not json", storage.Slots["slot.corrupt"], "Corrupt data is kept aside, not lost on the next save.");
        Assert.IsFalse(store.Exists("slot"));
        store.Save("slot", new Progress { Level = 1 });
        store.Delete("slot");
        Assert.IsFalse(store.Exists("slot"));
        Assert.AreEqual("memory", store.Directory);
    }

    /// <summary>Storage that fails like a full or blocked medium (browser quota, read-only disk).</summary>
    private sealed class FailingStorage : ISaveStorage
    {
        public Dictionary<string, string> Slots { get; } = [];
        public bool FailReads, FailWrites;
        public int Writes;
        public string Location => "failing";
        public string? Read(string slot) => FailReads ? throw new IOException("read refused") : Slots.GetValueOrDefault(slot);
        public void Write(string slot, string text)
        {
            if (FailWrites) throw new IOException("quota exceeded");
            Writes++;
            Slots[slot] = text;
        }
        public bool Exists(string slot) => Slots.ContainsKey(slot);
        public void Delete(string slot) => Slots.Remove(slot);
        public void SetAside(string slot) { }
    }

    [TestMethod]
    public void A_refused_write_is_reported_instead_of_crashing_the_game()
    {
        var storage = new FailingStorage { FailWrites = true };
        var store = new SaveStore(storage);
        Assert.IsFalse(store.Save("slot", new Progress { Level = 2 }));
        storage.FailWrites = false;
        Assert.IsTrue(store.Save("slot", new Progress { Level = 2 }), "the next save tries again");
        Assert.AreEqual(2, store.Load<Progress>("slot")!.Level);
    }

    [TestMethod]
    public void A_slot_that_could_not_be_read_is_never_overwritten()
    {
        var storage = new FailingStorage();
        storage.Slots["slot"] = """{ "Level": 9, "Name": "Valid" }""";
        storage.Slots["other"] = """{ "Level": 1 }""";
        var store = new SaveStore(storage);
        storage.FailReads = true;
        Assert.IsNull(store.Load<Progress>("slot"), "the game starts fresh");
        storage.FailReads = false;
        Assert.IsFalse(store.Save("slot", new Progress { Level = 1 }), "fresh progress must not replace data that exists but was unreadable");
        Assert.AreEqual("""{ "Level": 9, "Name": "Valid" }""", storage.Slots["slot"]);
        Assert.AreEqual(0, storage.Writes);
        Assert.IsTrue(store.Save("other", new Progress { Level = 2 }), "other slots still save");
        Assert.IsTrue(new SaveStore(storage).Save("slot", new Progress { Level = 1 }), "a later session that reads the slot normally may write it again");
        Assert.AreEqual(2, storage.Writes);
    }

    [TestMethod]
    [DoNotParallelize]
    public void Launchers_choose_the_default_storage_for_stores_without_a_directory()
    {
        var storage = new MemoryStorage();
        SaveStore.DefaultStorage = gameId => gameId == "game" ? storage : throw new InvalidOperationException(gameId);
        try
        {
            new SaveStore("game").Save("slot", new Progress { Level = 2 });
            Assert.IsTrue(storage.Exists("slot"), "A store without a directory uses the launcher's storage.");
            var dir = Path.Combine(Path.GetTempPath(), "microengine-tests-" + Guid.NewGuid().ToString("N"));
            Assert.AreEqual(dir, new SaveStore("game", dir).Directory, "An explicit directory still means files.");
        }
        finally { SaveStore.DefaultStorage = null; }
        StringAssert.EndsWith(new SaveStore("game").Directory, Path.Combine("MicroEngine", "game"), "Without a launcher choice saves go to local app data.");
    }

    [TestMethod]
    public void The_default_save_folder_is_absolute_on_every_system()
    {
        var dir = SaveStore.DefaultDirectory("game");
        Assert.IsTrue(Path.IsPathFullyQualified(dir), $"Saves must never depend on the working directory: {dir}");
        StringAssert.EndsWith(dir, Path.Combine("MicroEngine", "game"));
        Assert.AreEqual(dir, new SaveStore("game").Directory);
    }

    [TestMethod]
    public void Linux_user_data_follows_the_XDG_base_directory_rule()
    {
        Assert.AreEqual("/data/me", SaveStore.XdgDataHome("/data/me/", "/home/me"), "An absolute XDG_DATA_HOME wins.");
        Assert.AreEqual("/home/me/.local/share", SaveStore.XdgDataHome(null, "/home/me"), "Unset: the XDG default.");
        Assert.AreEqual("/home/me/.local/share", SaveStore.XdgDataHome("", "/home/me/"), "Empty counts as unset.");
        Assert.AreEqual("/home/me/.local/share", SaveStore.XdgDataHome("relative/data", "/home/me"), "A relative value is invalid and ignored.");
        Assert.IsNull(SaveStore.XdgDataHome(null, null), "No home: the caller picks a fallback, never the working directory.");
        Assert.IsNull(SaveStore.XdgDataHome("data", "home"));
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Linux)]
    [DoNotParallelize]
    public void On_Linux_saves_reach_a_data_folder_that_does_not_exist_yet()
    {
        var root = Path.Combine(Path.GetTempPath(), "microengine-tests-" + Guid.NewGuid().ToString("N"));
        var (home, cwd) = (Path.Combine(root, "home"), Path.Combine(root, "cwd"));
        Directory.CreateDirectory(home);
        Directory.CreateDirectory(cwd);
        var saved = (Xdg: Environment.GetEnvironmentVariable("XDG_DATA_HOME"), Home: Environment.GetEnvironmentVariable("HOME"), Cwd: Environment.CurrentDirectory);
        try
        {
            Environment.CurrentDirectory = cwd;
            Environment.SetEnvironmentVariable("HOME", home);
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", Path.Combine(root, "xdg", "data"));
            Assert.IsTrue(new SaveStore("game").Save("slot", new Progress { Level = 4 }));
            Assert.IsTrue(File.Exists(Path.Combine(root, "xdg", "data", "MicroEngine", "game", "slot.json")), "Missing parent folders are created.");
            Assert.AreEqual(4, new SaveStore("game").Load<Progress>("slot")!.Level, "The next session finds the save.");

            Environment.SetEnvironmentVariable("XDG_DATA_HOME", "relative-data");
            Assert.IsTrue(new SaveStore("game").Save("slot", new Progress { Level = 5 }));
            Assert.IsTrue(File.Exists(Path.Combine(home, ".local", "share", "MicroEngine", "game", "slot.json")), "~/.local/share is created when absent.");
            Assert.AreEqual(0, Directory.GetFileSystemEntries(cwd).Length, "Nothing is written to the working directory.");
        }
        finally
        {
            Environment.CurrentDirectory = saved.Cwd;
            Environment.SetEnvironmentVariable("HOME", saved.Home);
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", saved.Xdg);
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public void A_save_folder_blocked_by_a_file_fails_softly_and_recovers()
    {
        var root = Path.Combine(Path.GetTempPath(), "microengine-tests-" + Guid.NewGuid().ToString("N"));
        var blocker = Path.Combine(root, "saves");
        Directory.CreateDirectory(root);
        File.WriteAllText(blocker, "not a folder");
        try
        {
            var store = new SaveStore("game", Path.Combine(blocker, "game"));
            Assert.IsFalse(store.Save("slot", new Progress { Level = 2 }), "The game keeps running with progress in memory.");
            Assert.IsNull(store.Load<Progress>("slot"));
            Assert.IsFalse(store.Exists("slot"));
            File.Delete(blocker);
            Assert.IsTrue(store.Save("slot", new Progress { Level = 2 }), "Once the path is usable the next save succeeds.");
            Assert.AreEqual(2, new SaveStore("game", Path.Combine(blocker, "game")).Load<Progress>("slot")!.Level);
        }
        finally { Directory.Delete(root, true); }
    }

    public sealed class Progress { public int Level { get; set; } public string Name { get; set; } = ""; }
}

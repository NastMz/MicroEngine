#pragma warning disable CS0618 // SelectionList, deprecated, is tested until it is removed
using System.Collections;
using MicroEngine.Audio;
using MicroEngine.Audio.Retro;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace MicroEngine.Tests;

/// <summary>Behaviours that were defects in 0.4.0-alpha.1, and the neighbouring behaviour that must not change.</summary>
[TestClass]
public sealed class BehaviourFixTests
{
    private sealed class TestGame(Scene first) : Game
    {
        public override GameSettings Settings { get; } = new("fixes");
        protected override void Load() => Scenes.Push(first);
    }

    private sealed class Named(string name) : Scene
    {
        public string Name { get; } = name;
        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer) { }
    }

    private static TestGame Start(Scene first)
    {
        var game = new TestGame(first);
        game.InitializeForTest(new HeadlessPlatform());
        return game;
    }

    private static string Stack(Game game) => string.Join(",", game.Scenes.Stack.Select(s => ((Named)s).Name));

    [TestMethod]
    public void Changes_requested_during_a_transition_apply_after_its_swap_in_order()
    {
        var game = Start(new Named("title"));
        game.Scenes.Push(new Named("a"), Transition.Fade(0.2f));
        game.Scenes.Push(new Named("b"), Transition.Fade(0.2f));   // its own transition is dropped, its order is kept
        game.Scenes.Push(new Named("c"));
        game.StepForTest();
        Assert.AreEqual("title", Stack(game), "nothing changes while the old scene is being covered");
        for (int i = 0; i < 30; i++) game.StepForTest();
        Assert.AreEqual("title,a,b,c", Stack(game));
    }

    [TestMethod]
    public void A_change_without_transition_still_applies_before_the_next_update()
    {
        var game = Start(new Named("title"));
        game.Scenes.Push(new Named("a"));
        game.Scenes.Pop();
        game.Scenes.Push(new Named("b"));
        game.StepForTest();
        Assert.AreEqual("title,b", Stack(game));
    }

    [TestMethod]
    public void StopAll_finishes_coroutines_that_have_not_run_yet()
    {
        var runner = new CoroutineRunner();
        static IEnumerator Forever() { while (true) yield return null; }
        var started = runner.Start(Forever());
        runner.Update(0.016f);
        var pending = runner.Start(Forever());
        runner.StopAll();
        Assert.IsTrue(started.IsFinished);
        Assert.IsTrue(pending.IsFinished, "a coroutine started but not yet run is stopped too");
        Assert.AreEqual(0, runner.Count);
    }

    [TestMethod]
    public void Animator_refuses_a_negative_speed_and_keeps_its_frame()
    {
        var sprite = new Sprite(new HeadlessGraphicsDevice().CreateTexture(new PixelImage(2, 2)), new RectF(0, 0, 2, 2), default);
        var animator = new Animator().Add("walk", new AnimationClip([sprite, sprite], 8));
        animator.Play("walk");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => animator.Speed = -1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => animator.Speed = float.NaN);
        animator.Speed = 0;
        animator.Update(1);
        Assert.AreSame(sprite, animator.Frame);
        Assert.AreEqual(0, animator.Time, "speed 0 pauses the clip");
    }

    [TestMethod]
    public void A_sound_with_no_length_has_no_NaN_samples()
    {
        var clip = Synth.Render(new SfxParams { Wave = Wave.Sine, Frequency = 440, EndFrequency = 880, Attack = 0, Sustain = 0, Decay = 0 });
        Assert.IsFalse(clip.Samples.Any(float.IsNaN));
    }

    [TestMethod]
    public void Layer_refuses_clips_it_would_mix_wrongly()
    {
        var mono = Synth.Render(new SfxParams { Frequency = 440, Sustain = 0.05f });
        var stereo = new AudioClip(new float[200], 44100, channels: 2);
        var slow = new AudioClip(new float[100], 22050);
        Assert.ThrowsExactly<ArgumentException>(() => Synth.Layer((mono, 0), (stereo, 0)));
        Assert.ThrowsExactly<ArgumentException>(() => Synth.Layer((mono, 0), (slow, 0)));
        var layered = Synth.Layer((mono, 0), (mono, 0.01f));
        Assert.AreEqual(1, layered.Channels, "mono clips at the default rate still layer");
    }

    private sealed class VolumeProbe : IAudioDevice
    {
        public float Volume = -1;
        public bool IsAvailable => true;
        public MusicHandle? CurrentMusic { get; private set; }
        public SoundHandle LoadSound(string name, AudioClip clip) => new(0, name);
        public MusicHandle LoadMusic(string name, AudioClip clip, bool loop = true) => new(0, name);
        public MusicHandle LoadMusicStream(string name, byte[] encoded, string format, bool loop = true) => new(0, name);
        public void Play(SoundHandle sound, float volume = 1, float pitch = 1, float pan = 0.5f) { }
        public void PlayMusic(MusicHandle music, float volume = 1) { CurrentMusic = music; Volume = volume; }
        public void StopMusic() => CurrentMusic = null;
        public void SetMusicVolume(float volume) => Volume = volume;
        public void PauseMusic(bool paused) { }
        public void Update() { }
    }

    [TestMethod]
    public void PlayMusic_without_a_fade_starts_at_full_volume()
    {
        var device = new VolumeProbe();
        var mixer = new AudioMixer(device);
        mixer.AddMusic("a", new AudioClip(new float[10]));
        mixer.AddMusic("b", new AudioClip(new float[10]));
        mixer.PlayMusic("a", fadeSeconds: 0);
        Assert.AreEqual(1f, device.Volume, "no fade asked: full volume at once");
        mixer.PlayMusic("b");                      // the default fade is unchanged: out, then in
        mixer.Update(0.5f);
        Assert.AreEqual("b", device.CurrentMusic?.Name);
        Assert.IsLessThan(1f, device.Volume, "a faded switch still rises from silence");
    }

    [TestMethod]
    public void ContentFolder_never_reaches_a_sibling_folder_with_the_same_prefix()
    {
        var parent = Directory.CreateTempSubdirectory("content-").FullName;
        Directory.CreateDirectory(Path.Combine(parent, "Content"));
        Directory.CreateDirectory(Path.Combine(parent, "ContentX"));
        File.WriteAllText(Path.Combine(parent, "ContentX", "secret.txt"), "x");
        File.WriteAllText(Path.Combine(parent, "Content", "ok.txt"), "x");
        var content = new ContentFolder(Path.Combine(parent, "Content"));
        Assert.ThrowsExactly<ArgumentException>(() => content.PathOf("../ContentX/secret.txt"));
        Assert.IsFalse(content.Exists("../ContentX/secret.txt"), "Exists answers only for files inside the folder");
        Assert.IsTrue(content.Exists("ok.txt"));
        Assert.AreEqual(Path.Combine(parent, "Content", "ok.txt"), content.PathOf("ok.txt"));
        Directory.Delete(parent, true);
    }

    private sealed class LockedStorage : ISaveStorage
    {
        public bool Locked = true;
        public string? Text;
        public string Location => "test";
        public string? Read(string slot) => Locked ? throw new IOException("locked") : Text;
        public void Write(string slot, string text) => Text = text;
        public bool Exists(string slot) => Text is not null;
        public void Delete(string slot) => Text = null;
        public void SetAside(string slot) { }
    }

    [TestMethod]
    public void Deleting_an_unreadable_slot_lets_the_game_save_it_again()
    {
        var storage = new LockedStorage { Text = "{}" };
        var store = new SaveStore(storage);
        Assert.IsNull(store.Load<SaveData>("slot"));
        Assert.IsFalse(store.Save("slot", new SaveData()), "an unreadable slot is never overwritten");
        store.Delete("slot");
        storage.Locked = false;
        Assert.IsTrue(store.Save("slot", new SaveData()), "after an explicit delete, a new save is a choice, not an accident");
    }

    private sealed class SaveData { public int Score { get; set; } }

    [TestMethod]
    public void A_wrong_number_names_its_option()
    {
        var error = Assert.ThrowsExactly<FormatException>(() => LaunchArguments.Parse(["--seed", "abc"]));
        StringAssert.Contains(error.Message, "--seed");
        Assert.AreEqual(5, LaunchArguments.Parse(["--frames", "5"]).Frames);
    }

    [TestMethod]
    public void A_tap_within_one_frame_is_released_on_the_next_update()
    {
        var map = new InputMap().Bind("jump", Key.Z);
        var keys = new HeadlessInput();
        keys.Set(Key.Z, true);
        map.Sample(keys);
        keys.Set(Key.Z, false);
        map.Sample(keys);
        Assert.IsTrue(map.Pressed("jump"));
        Assert.IsFalse(map.Released("jump"));
        map.EndUpdate();
        map.Sample(keys);
        Assert.IsTrue(map.Released("jump"), "the release a tap implies is reported once");
        Assert.IsFalse(map.Pressed("jump"));
        map.EndUpdate();
        map.Sample(keys);
        Assert.IsFalse(map.Released("jump"));
    }

    [TestMethod]
    public void SelectionList_keeps_its_index_valid()
    {
        var list = new SelectionList("a", "b", "c");
        Assert.ThrowsExactly<ArgumentException>(() => list.SetOptions([]));
        Assert.HasCount(3, list.Options, "a refused replacement keeps the options");
        list.Index = 7;
        Assert.AreEqual(2, list.Index);
        list.Index = -3;
        Assert.AreEqual(0, list.Index);
        list.SetOptions(["only"]);
        Assert.AreEqual(0, list.Index);
    }
}

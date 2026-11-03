using System.Numerics;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;

namespace MicroEngine.Tests;

[TestClass]
public sealed class PlatformCompositionTests
{
    private static readonly GameSettings Settings = new("composition");

    /// <summary>A headless profile that records the request its backend received.</summary>
    private static PlatformProfile Headless(string name, List<PlatformRequest>? seen = null, PlatformParts replaceable = PlatformParts.Audio | PlatformParts.Input, string? unsupported = null) =>
        new(name, $"{name} test profile", request => { seen?.Add(request); return HeadlessPlatform.For(request); })
        {
            Replaceable = replaceable,
            CheckSupport = () => unsupported,
        };

    private sealed class RecordingAudio : IAudioDevice, IDisposable
    {
        private readonly SilentAudioDevice _inner = new();
        public List<bool> Pauses { get; } = [];
        public bool Disposed { get; private set; }
        public bool IsAvailable => true;
        public MusicHandle? CurrentMusic => _inner.CurrentMusic;
        public SoundHandle LoadSound(string name, AudioClip clip) => _inner.LoadSound(name, clip);
        public MusicHandle LoadMusic(string name, AudioClip clip, bool loop = true) => _inner.LoadMusic(name, clip, loop);
        public MusicHandle LoadMusicStream(string name, byte[] encoded, string format, bool loop = true) => _inner.LoadMusicStream(name, encoded, format, loop);
        public void Play(SoundHandle sound, float volume = 1, float pitch = 1, float pan = 0.5f) => _inner.Play(sound, volume, pitch, pan);
        public void PlayMusic(MusicHandle music, float volume = 1) => _inner.PlayMusic(music, volume);
        public void StopMusic() => _inner.StopMusic();
        public void SetMusicVolume(float volume) { }
        public void PauseMusic(bool paused) => Pauses.Add(paused);
        public void Update() { }
        public void Dispose() => Disposed = true;
    }

    private sealed class KeySource(Key key) : IInputSource
    {
        public bool IsKeyDown(Key k) => k == key;
        public bool IsPadDown(PadButton button) => button == PadButton.A;
        public Vector2 LeftStick => new(0.5f, 0);
    }

    [TestMethod]
    public void DefaultIsTheFirstSupportedProfileInRegistrationOrder()
    {
        var catalog = new PlatformCatalog()
            .Add(Headless("metal", unsupported: "needs a GPU"))
            .Add(Headless("sdl"))
            .Add(Headless("raylib"));
        Assert.AreEqual("sdl", catalog.Default.Name);
        Assert.AreEqual("sdl", catalog.Resolve(null).Name);
        Assert.AreEqual("raylib", catalog.Resolve("RayLib").Name);
    }

    [TestMethod]
    public void UnknownAndUnsupportedProfilesFailWithTheReason()
    {
        var catalog = new PlatformCatalog().Add(Headless("sdl")).Add(Headless("raylib", unsupported: "raylib has no iOS platform"));
        var unknown = Assert.ThrowsExactly<PlatformConfigurationException>(() => catalog.Resolve("vulkan"));
        Assert.AreEqual("Unknown platform 'vulkan'. Available platforms: sdl, raylib.", unknown.Message);
        var unsupported = Assert.ThrowsExactly<PlatformConfigurationException>(() => catalog.Resolve("raylib"));
        Assert.AreEqual("Platform 'raylib' cannot run here: raylib has no iOS platform.", unsupported.Message);
        var empty = Assert.ThrowsExactly<PlatformConfigurationException>(() => new PlatformCatalog().Add(Headless("x", unsupported: "no")).Default);
        Assert.AreEqual("No platform can run here. x: no.", empty.Message);
    }

    [TestMethod]
    public void ProfileNamesAreUnique()
    {
        var catalog = new PlatformCatalog().Add(Headless("sdl"));
        var e = Assert.ThrowsExactly<ArgumentException>(() => catalog.Add(Headless("SDL")));
        StringAssert.Contains(e.Message, "'SDL' is already registered");
    }

    [TestMethod]
    public void AnAudioOverrideReplacesTheBackendDeviceWhichIsNotOpened()
    {
        var seen = new List<PlatformRequest>();
        var audio = new RecordingAudio();
        var platform = Headless("sdl", seen).Create(new PlatformRequest(Settings) { Audio = () => audio });
        Assert.AreSame(audio, platform.Audio);
        Assert.IsFalse(seen.Single().OpensOwnAudio, "the backend must not open its own device when audio is replaced");
        platform.Dispose();
        Assert.IsTrue(audio.Disposed, "the platform owns the replacement device");
    }

    [TestMethod]
    public void WithoutOverridesTheBackendPlatformIsReturnedUnchanged()
    {
        var seen = new List<PlatformRequest>();
        using var platform = Headless("sdl", seen).Create(new PlatformRequest(Settings));
        Assert.IsInstanceOfType<HeadlessPlatform>(platform);
        Assert.IsTrue(seen.Single().OpensOwnAudio);
        Assert.IsFalse(new PlatformRequest(Settings) { Mute = true }.OpensOwnAudio);
    }

    [TestMethod]
    public void ExtraInputSourcesAreCombinedWithTheBackendInput()
    {
        using var platform = Headless("sdl").Create(new PlatformRequest(Settings) { ExtraInput = [new KeySource(Key.Z)] });
        Assert.IsTrue(platform.Input.IsKeyDown(Key.Z));
        Assert.IsFalse(platform.Input.IsKeyDown(Key.X));
        Assert.IsTrue(platform.Input.IsPadDown(PadButton.A));
        Assert.AreEqual(0.5f, platform.Input.LeftStick.X);
    }

    [TestMethod]
    public void ReplacingAPartTheProfileFixesIsAConfigurationError()
    {
        var browser = Headless("browser", replaceable: PlatformParts.Input);
        var e = Assert.ThrowsExactly<PlatformConfigurationException>(() => browser.Create(new PlatformRequest(Settings) { Audio = () => new RecordingAudio() }));
        Assert.AreEqual("Platform 'browser' does not allow replacing its audio device.", e.Message);
        var fixedInput = Headless("console", replaceable: PlatformParts.None);
        e = Assert.ThrowsExactly<PlatformConfigurationException>(() => fixedInput.Create(new PlatformRequest(Settings) { ExtraInput = [new KeySource(Key.Z)] }));
        Assert.AreEqual("Platform 'console' does not allow adding input sources.", e.Message);
    }

    [TestMethod]
    public void AnUnsupportedProfileRefusesToCreate()
    {
        var e = Assert.ThrowsExactly<PlatformConfigurationException>(() => Headless("raylib", unsupported: "no iOS platform").Create(new PlatformRequest(Settings)));
        Assert.AreEqual("Platform 'raylib' cannot run here: no iOS platform.", e.Message);
    }

    private sealed class CountingGame : Game
    {
        public override GameSettings Settings { get; } = new("lifecycle");
        public List<string> Events { get; } = [];
        protected override void Load()
        {
            Audio.AddMusic("theme", new AudioClip(new float[8]));
            Audio.PlayMusic("theme", 0);
            Scenes.Push(new IdleScene());
        }
        protected override void OnSuspended() => Events.Add($"suspended at {UpdateCount}");
        protected override void OnResumed() => Events.Add($"resumed at {UpdateCount}");
    }

    private sealed class IdleScene : Scene
    {
        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer) { }
    }

    private sealed class SuspendablePlatform(RecordingAudio audio) : TestPlatform
    {
        public override IAudioDevice Audio => audio;
        public override void EndFrame() { }
    }

    [TestMethod]
    public void ASuspendedPlatformStopsSimulationAndMusicUntilItResumes()
    {
        var audio = new RecordingAudio();
        var platform = new SuspendablePlatform(audio);
        var game = new CountingGame();
        var loop = new GameLoop(game, platform, new RunOptions { TimeMode = MicroEngine.Kernel.TimeMode.Deterministic });
        Assert.IsTrue(loop.Start());
        for (int i = 0; i < 10; i++) loop.Tick();
        Assert.AreEqual(10, game.UpdateCount);

        platform.Suspended = true;
        for (int i = 0; i < 30; i++) Assert.IsTrue(loop.Tick(), "a suspended game keeps running");
        Assert.AreEqual(10, game.UpdateCount, "no simulation while suspended");
        Assert.AreEqual(10, loop.Frames, "no rendering while suspended");

        platform.Suspended = false;
        loop.Tick();
        Assert.AreEqual(11, game.UpdateCount, "resuming does not catch up on the suspended time");
        CollectionAssert.AreEqual(new[] { "suspended at 10", "resumed at 10" }, game.Events);
        CollectionAssert.AreEqual(new[] { true, false }, audio.Pauses);
        loop.Finish();
    }

    [TestMethod]
    public void ResumingKeepsMusicTheGamePausedItself()
    {
        var audio = new RecordingAudio();
        var platform = new SuspendablePlatform(audio);
        var game = new CountingGame();
        var loop = new GameLoop(game, platform, new RunOptions { TimeMode = MicroEngine.Kernel.TimeMode.Deterministic });
        Assert.IsTrue(loop.Start());
        loop.Tick();
        game.Audio.PauseMusic(true);
        platform.Suspended = true;
        loop.Tick();
        platform.Suspended = false;
        loop.Tick();
        CollectionAssert.AreEqual(new[] { true }, audio.Pauses, "the game's own pause stands after resuming");
        Assert.IsTrue(game.Audio.MusicPaused);
        loop.Finish();
    }

    [TestMethod]
    public void PlatformsAreActiveByDefault()
    {
        IPlatform platform = new HeadlessPlatform();
        Assert.IsTrue(platform.PumpEvents(new IgnoredEvents()), "a frame may run");
    }
}

using System.Diagnostics.CodeAnalysis;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Kernel;
using MicroEngine.Platform;
using MicroEngine.Scenes;

namespace MicroEngine.Tests.Hosts;

/// <summary>
/// The engine reaches a platform only through the host contract: events from the host's pump become engine lifecycle
/// events, optional features are found with <see cref="IPlatformHost.TryGetCapability{T}"/> (never by casting), a host
/// that replaces parts is the backend itself (no wrapper), and nothing about the platform depends on the time mode.
/// </summary>
[TestClass]
public sealed class PlatformHostTests
{
    private static readonly GameSettings Settings = new("host", 64, 36);

    private sealed class Idle : Scene
    {
        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer) { }
    }

    /// <summary>Records the engine events and each fixed step, in order.</summary>
    private sealed class RecordingGame(Action<EngineBuilder>? configure = null) : Game
    {
        public List<string> Seen { get; } = [];
        public override GameSettings Settings => PlatformHostTests.Settings;
        public override void Configure(EngineBuilder builder)
        {
            builder.Lifecycle.Resized += size => Seen.Add($"resized {size.Width}x{size.Height}");
            builder.Lifecycle.FocusChanged += focused => Seen.Add(focused ? "focus" : "blur");
            builder.Lifecycle.Suspending += () => Seen.Add("suspending");
            builder.Lifecycle.Resumed += () => Seen.Add("resumed");
            builder.Schedule.Add(FramePhase.FixedUpdate, "test.step", step => Seen.Add($"step {step.FixedStep}"));
            configure?.Invoke(builder);
        }
        protected override void Load() => Scenes.Push(new Idle());
    }

    private static GameLoop Started(Game game, IPlatform platform)
    {
        var loop = new GameLoop(game, platform, new RunOptions { TimeMode = TimeMode.Deterministic });
        Assert.IsTrue(loop.Start());
        return loop;
    }

    // ---- Events: the host's pump drives the engine's lifecycle -------------------------------------------------------

    [TestMethod]
    public void Resize_and_focus_events_reach_the_game_before_the_frame_that_follows_them()
    {
        var game = new RecordingGame();
        var platform = HeadlessPlatform.For(Settings);
        var loop = Started(game, platform);
        loop.Tick();
        platform.Resize(128, 72);
        platform.Focus(false);
        loop.Tick();
        platform.Focus(true);
        loop.Tick();
        CollectionAssert.AreEqual(new[] { "step 0", "resized 128x72", "blur", "step 1", "focus", "step 2" }, game.Seen);
        Assert.AreEqual((128, 72), platform.MainSurface!.PixelSize, "the surface reports the new size");
        loop.Finish();
    }

    [TestMethod]
    public void A_suspended_host_runs_no_frame_and_the_game_hears_both_edges()
    {
        var game = new RecordingGame();
        var platform = HeadlessPlatform.For(Settings);
        var loop = Started(game, platform);
        loop.Tick();
        platform.Suspend();
        Assert.IsTrue(loop.Tick(), "a suspended game keeps running, without frames");
        Assert.IsTrue(loop.Tick());
        platform.Resume();
        loop.Tick();
        CollectionAssert.AreEqual(new[] { "step 0", "suspending", "resumed", "step 1" }, game.Seen);
        Assert.AreEqual(2, game.UpdateCount, "nothing was simulated while suspended");
        loop.Finish();
    }

    [TestMethod]
    public void Every_quit_request_is_asked_once_and_a_veto_keeps_the_game_running()
    {
        bool veto = true;
        int asked = 0;
        var game = new RecordingGame(builder => builder.Lifecycle.QuitRequested += request => { asked++; if (veto) request.Veto(); });
        var platform = HeadlessPlatform.For(Settings);
        var loop = Started(game, platform);
        platform.RequestQuit();
        Assert.IsTrue(loop.Tick(), "vetoed: the frame runs");
        Assert.IsTrue(loop.Tick(), "one request is asked once");
        Assert.AreEqual(1, asked);
        veto = false;
        platform.RequestQuit();
        Assert.IsFalse(loop.Tick(), "a later request nobody vetoes ends the run");
        Assert.AreEqual(2, asked);
        loop.Finish();
    }

    [TestMethod]
    public void A_quit_requested_while_the_game_loads_is_asked_once_it_has_loaded()
    {
        int asked = 0;
        var game = new RecordingGame(builder => builder.Lifecycle.QuitRequested += _ => asked++);
        var platform = HeadlessPlatform.For(Settings);
        platform.RequestQuit();
        var loop = new GameLoop(game, platform, new RunOptions { TimeMode = TimeMode.RealTime, StartScreen = true });
        Assert.IsTrue(loop.Start());
        Assert.IsFalse(loop.Tick(), "the request made before the game existed ends the run");
        Assert.AreEqual(1, asked);
        loop.Finish();
    }

    // ---- Capabilities: found by query, never by casting ---------------------------------------------------------------

    /// <summary>
    /// A host that offers its optional features only through <see cref="TryGetCapability{T}"/>: the class implements none
    /// of them, so an engine that cast the platform would find nothing.
    /// </summary>
    private sealed class QueryOnlyHost(HeadlessPlatform inner, CapabilityRegistry capabilities, bool canBlock = true) : IPlatform
    {
        public IGraphicsDevice Graphics => inner.Graphics;
        public IAudioDevice Audio => inner.Audio;
        public IInputSource Input => inner.Input;
        public IMonotonicClock Clock => inner.Clock;
        public bool PumpEvents(IEngineEventSink sink) => inner.PumpEvents(sink);
        public ISurface? MainSurface => inner.MainSurface;
        public bool CanBlock => canBlock;
        public bool TryGetCapability<T>([NotNullWhen(true)] out T? capability) where T : class => capabilities.TryGet(out capability);
        public IRenderer BeginFrame() => inner.BeginFrame();
        public void EndFrame() => inner.EndFrame();
        public void SaveScreenshot(string path) => inner.SaveScreenshot(path);
        public void ToggleFullscreen() { }
        public Color Letterbox { get; set; }
        public void Dispose() => inner.Dispose();
    }

    private sealed class Display : IDisplayControl, IDisplayPacing, IPlatformDiagnostics
    {
        public IReadOnlyList<DisplayInfo> Displays { get; } = [new DisplayInfo(0, "test", 1920, 1080, 144, 1, true)];
        public int CurrentDisplay => 0;
        public bool VSync { get; private set; } = true;
        public bool SetVSync(bool on) { VSync = on; return true; }
        public WindowMode WindowMode { get; private set; }
        public bool SetWindowMode(WindowMode mode) { WindowMode = mode; return true; }
        public bool MoveToDisplay(int index) => index == 0;
        public double DisplayRate => 144;
        public IReadOnlyDictionary<string, string> Diagnostics { get; } = new Dictionary<string, string> { ["renderer"] = "test-api" };
    }

    [TestMethod]
    public void The_game_finds_every_host_capability_by_query()
    {
        var display = new Display();
        var capabilities = new CapabilityRegistry();
        capabilities.Add<IDisplayControl>(display);
        capabilities.Add<IDisplayPacing>(display);
        capabilities.Add<IPlatformDiagnostics>(display);
        var game = new RecordingGame();
        var loop = Started(game, new QueryOnlyHost(HeadlessPlatform.For(Settings), capabilities));

        Assert.IsTrue(game.Screen.CanChange, "the display control was found by query");
        Assert.AreEqual(144.0, game.Screen.RefreshRate, "the display pacing was found by query");
        Assert.AreEqual("test-api", game.Screen.Renderer, "the diagnostics were found by query");
        game.Screen.WindowMode = WindowMode.Fullscreen;
        Assert.AreEqual(WindowMode.Fullscreen, display.WindowMode, "changes go to the host's capability");
        Assert.IsTrue(game.TryGetCapability<IDisplayControl>(out var found));
        Assert.AreSame(display, found, "the game forwards any capability of its host");
        Assert.IsFalse(game.TryGetCapability<IPreciseDelay>(out _), "an undeclared capability stays absent");
        loop.Finish();
    }

    [TestMethod]
    public void A_host_without_capabilities_leaves_the_screen_unchangeable_and_unpaced()
    {
        var game = new RecordingGame();
        var loop = Started(game, new QueryOnlyHost(HeadlessPlatform.For(Settings), new CapabilityRegistry()));
        Assert.IsFalse(game.Screen.CanChange);
        Assert.AreEqual(0.0, game.Screen.RefreshRate);
        Assert.IsNull(game.Screen.Renderer);
        Assert.IsFalse(game.TryGetCapability<ITouchScreen>(out _));
        loop.Finish();
    }

    [TestMethod]
    public void The_headless_touch_screen_is_a_capability_the_game_finds()
    {
        var platform = HeadlessPlatform.For(Settings);
        Assert.IsTrue(platform.TryGetCapability<ITouchScreen>(out var touch));
        Assert.AreSame(platform.TouchScreen, touch);
        var game = new RecordingGame();
        var loop = Started(game, platform);
        Assert.IsTrue(game.TryGetCapability<ITouchScreen>(out var seen));
        Assert.AreSame(platform.TouchScreen, seen);
        Assert.AreSame(game.Touch, platform.TouchScreen.Overlay, "the engine draws its controls over the host's touch screen");
        loop.Finish();
    }

    // ---- Composition: parts are composed into the backend, never wrapped ----------------------------------------------

    private sealed class Recording : IAudioDevice, IDisposable
    {
        private readonly SilentAudioDevice _inner = new();
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
        public void PauseMusic(bool paused) { }
        public void Update() { }
        public void Dispose() => Disposed = true;
    }

    private sealed class KeyDown(Key key) : IInputSource
    {
        public bool IsKeyDown(Key k) => k == key;
        public bool IsPadDown(PadButton button) => false;
        public System.Numerics.Vector2 LeftStick => default;
    }

    [TestMethod]
    public void Replacing_audio_and_adding_input_returns_the_backend_itself_with_every_capability()
    {
        var audio = new Recording();
        var extra = new KeyDown(Key.Z);
        var platform = HeadlessPlatform.Profile.Create(new PlatformRequest(Settings) { Audio = () => audio, ExtraInput = [extra] });
        Assert.IsInstanceOfType<HeadlessPlatform>(platform, "no wrapper: the request's parts are composed into the backend");
        Assert.AreSame(audio, platform.Audio, "the host's audio device");
        Assert.IsTrue(platform.Input.IsKeyDown(Key.Z), "the extra source is read with the backend's");
        ((HeadlessPlatform)platform).Keys.Set(Key.X, true);
        Assert.IsTrue(platform.Input.IsKeyDown(Key.X), "the backend's own source is still read");
        Assert.IsTrue(platform.TryGetCapability<ITouchScreen>(out _), "the backend's capabilities are all there");
        platform.Dispose();
        Assert.IsTrue(audio.Disposed, "the backend owns the device the request supplied");
    }

    [TestMethod]
    public void A_backend_that_ignores_the_supplied_audio_device_is_refused()
    {
        var careless = new PlatformProfile("careless", "ignores the request's parts", request => HeadlessPlatform.For(request.Settings));
        var audio = new Recording();
        var e = Assert.ThrowsExactly<PlatformConfigurationException>(() => careless.Create(new PlatformRequest(Settings) { Audio = () => audio }));
        StringAssert.Contains(e.Message, "PlatformRequest.CreateAudio");
        Assert.IsFalse(audio.Disposed, "the backend never took the device, so it is still the host's");
        e = Assert.ThrowsExactly<PlatformConfigurationException>(() => careless.Create(new PlatformRequest(Settings) { ExtraInput = [new KeyDown(Key.Z)] }));
        StringAssert.Contains(e.Message, "PlatformRequest.CombineInput");
    }

    // ---- No hidden coupling between time and the platform ------------------------------------------------------------

    [TestMethod]
    public void Platform_requests_carry_no_time_mode()
    {
        Assert.IsNull(typeof(PlatformRequest).GetProperty("FixedStep"), "deterministic time is the loop's business only");
        Assert.IsNull(typeof(IPlatform).GetProperty("RealTime"), "a platform has no time-mode preference");
        var request = new PlatformRequest(Settings);
        Assert.IsTrue(request.WaitForDisplay, "frames of a visible window wait for the display by default");
        Assert.IsFalse(request.StartGamepadsAfterFirstFrame, "gamepads start with the platform by default");
        Assert.IsTrue(request.OpensOwnAudio, "a device by default, whatever the run's time");
    }

    [TestMethod]
    public void A_run_that_chooses_no_time_mode_runs_in_real_time_on_any_host()
    {
        var game = new RecordingGame();
        var loop = new GameLoop(game, HeadlessPlatform.For(Settings));
        Assert.IsTrue(loop.Start());
        Assert.AreEqual(TimeMode.RealTime, game.TimeSettings.Mode, "the headless host does not choose the time mode");
        loop.Finish();
    }

    [TestMethod]
    public void A_test_initializes_a_game_in_the_time_mode_a_run_would_choose()
    {
        var deterministic = new RecordingGame();
        deterministic.InitializeForTest(HeadlessPlatform.For(Settings), 1, TimeMode.Deterministic);
        Assert.AreEqual(TimeMode.Deterministic, deterministic.TimeSettings.Mode);
        var unchosen = new RecordingGame();
        unchosen.InitializeForTest(HeadlessPlatform.For(Settings));
        Assert.AreEqual(TimeMode.RealTime, unchosen.TimeSettings.Mode, "no choice: the default, real time");
    }
}

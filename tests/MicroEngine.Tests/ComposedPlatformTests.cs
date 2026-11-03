using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Kernel;
using MicroEngine.Platform;
using MicroEngine.Scenes;

namespace MicroEngine.Tests;

/// <summary>
/// A platform whose audio is replaced or whose input is extended (<c>--audio-capture</c>, a host's extra input) keeps
/// every capability of its backend: display pacing, its timer, run-time display control and diagnostics. The parts are
/// composed into the backend when it is built, so there is no wrapper that could hide a capability.
/// </summary>
[TestClass]
public sealed class ComposedPlatformTests
{
    private static readonly GameSettings Settings = new("composed");

    private static PlatformRequest Composing(GameSettings settings) => new(settings)
    {
        Audio = () => new SilentAudioDevice(),
        ExtraInput = [new NoInput()],
    };

    private sealed class NoInput : IInputSource
    {
        public bool IsKeyDown(Key key) => false;
        public bool IsPadDown(PadButton button) => false;
        public Vector2 LeftStick => Vector2.Zero;
    }

    /// <summary>A backend with every optional capability, each with a value no default has; it honours the request's parts.</summary>
    private sealed class FullPlatform : IPlatform, IDisplayControl, IPlatformDiagnostics, IDisplayPacing, IPreciseDelay
    {
        private readonly HeadlessPlatform _inner = new();
        private readonly CapabilityRegistry _capabilities = new();
        private readonly IAudioDevice _audio;
        private readonly IInputSource _input;

        public FullPlatform(PlatformRequest? request = null)
        {
            _audio = request?.CreateAudio(() => _inner.Audio) ?? _inner.Audio;
            _input = request?.CombineInput(_inner.Input) ?? _inner.Input;
            _capabilities.Add<IDisplayControl>(this);
            _capabilities.Add<IPlatformDiagnostics>(this);
            _capabilities.Add<IDisplayPacing>(this);
            _capabilities.Add<IPreciseDelay>(this);
            _capabilities.Add<ITouchScreen>(_inner.TouchScreen);
        }

        public List<double> Waits { get; } = [];
        public List<string> Calls { get; } = [];
        public IGraphicsDevice Graphics => _inner.Graphics;
        public IAudioDevice Audio => _audio;
        public IInputSource Input => _input;
        public IMonotonicClock Clock => _inner.Clock;
        public bool PumpEvents(IEngineEventSink sink) => true;
        public ISurface? MainSurface => _inner.MainSurface;
        public bool CanBlock => true;
        public bool TryGetCapability<T>([NotNullWhen(true)] out T? capability) where T : class => _capabilities.TryGet(out capability);
        public double DisplayRate => 144;
        public void Delay(double seconds) => Waits.Add(seconds);
        public ITouchScreen Touch => _inner.Touch;
        public IRenderer BeginFrame() => _inner.BeginFrame();
        public void EndFrame() { }
        public void SaveScreenshot(string path) { }
        public void ToggleFullscreen() { }
        public Color Letterbox { get; set; } = Color.Hex("123456");
        public void Dispose() { }

        public IReadOnlyList<DisplayInfo> Displays { get; } = [new(0, "left", 2560, 1440, 144, 1, true), new(1, "right", 1920, 1080, 60, 1, false)];
        public int CurrentDisplay => 1;
        public bool VSync { get; private set; } = true;
        public bool SetVSync(bool on) { Calls.Add($"vsync {on}"); VSync = on; return true; }
        public WindowMode WindowMode { get; private set; } = WindowMode.Fullscreen;
        public bool SetWindowMode(WindowMode mode) { Calls.Add($"mode {mode}"); WindowMode = mode; return true; }
        public bool MoveToDisplay(int index) { Calls.Add($"move {index}"); return true; }
        public IReadOnlyDictionary<string, string> Diagnostics { get; } = new Dictionary<string, string> { ["renderer"] = "vulkan", ["adapter"] = "test gpu" };
    }

    private static PlatformProfile Full() => new("full", "test profile", request => new FullPlatform(request));

    [TestMethod]
    public void A_composed_platform_is_the_backend_with_every_optional_capability()
    {
        using var composed = Full().Create(Composing(Settings));
        var inner = composed as FullPlatform;
        Assert.IsNotNull(inner, "the request replaces audio and adds input, and the backend is still not wrapped");

        Assert.IsTrue(composed.TryGetCapability<IDisplayPacing>(out var pacing));
        Assert.AreEqual(144, pacing.DisplayRate, "display pacing");
        Assert.IsTrue(composed.TryGetCapability<IPreciseDelay>(out var delay));
        delay.Delay(0.004);
        CollectionAssert.AreEqual(new[] { 0.004 }, inner.Waits, "the backend's own timer answers");
        Assert.IsTrue(composed.TryGetCapability<ITouchScreen>(out var touch));
        Assert.AreSame(inner.Touch, touch);
        Assert.AreEqual(Color.Hex("123456"), composed.Letterbox);

        Assert.IsTrue(composed.TryGetCapability<IDisplayControl>(out var control), "run-time display control");
        Assert.AreSame(inner.Displays, control.Displays);
        Assert.AreEqual(1, control.CurrentDisplay);
        Assert.IsTrue(control.VSync);
        Assert.IsTrue(control.SetVSync(false));
        Assert.IsFalse(inner.VSync);
        Assert.AreEqual(WindowMode.Fullscreen, control.WindowMode);
        Assert.IsTrue(control.SetWindowMode(WindowMode.Windowed));
        Assert.IsTrue(control.MoveToDisplay(0));
        CollectionAssert.AreEqual(new[] { "vsync False", "mode Windowed", "move 0" }, inner.Calls);

        Assert.IsTrue(composed.TryGetCapability<IPlatformDiagnostics>(out var diagnostics), "diagnostics");
        Assert.AreSame(inner.Diagnostics, diagnostics.Diagnostics);
    }

    private sealed class EmptyGame : Game
    {
        public override GameSettings Settings { get; } = new("composed game");
        protected override void Load() => Scenes.Push(new Idle());
        private sealed class Idle : Scene
        {
            public override void Update(float dt) { }
            public override void Draw(IRenderer renderer) { }
        }
    }

    [TestMethod]
    public void The_game_screen_sees_the_backend_through_a_composed_request()
    {
        var direct = new EmptyGame();
        direct.InitializeForTest(new FullPlatform());
        var composed = new EmptyGame();
        composed.InitializeForTest(Full().Create(Composing(composed.Settings)));
        foreach (var game in new[] { direct, composed })
        {
            Assert.IsTrue(game.Screen.CanChange);
            Assert.AreEqual(144, game.Screen.RefreshRate);
            Assert.AreEqual("right", game.Screen.Display?.Name);
            Assert.AreEqual(2, game.Screen.Displays.Count);
            Assert.AreEqual("vulkan", game.Screen.Renderer);
            Assert.AreEqual("test gpu", game.Screen.Adapter);
        }
    }

    private interface IAbsent;

    [TestMethod]
    public void A_composed_headless_platform_has_exactly_the_headless_capabilities()
    {
        var settings = Settings with { UpdateRate = 120 };
        using var direct = HeadlessPlatform.Profile.Create(new PlatformRequest(settings));
        using var composed = HeadlessPlatform.Profile.Create(Composing(settings));
        Assert.IsInstanceOfType<HeadlessPlatform>(direct);
        Assert.IsInstanceOfType<HeadlessPlatform>(composed, "no wrapper");
        Assert.AreEqual(direct.TryGetCapability<IDisplayControl>(out _), composed.TryGetCapability<IDisplayControl>(out _), "no run-time display control appears");
        Assert.AreEqual(direct.TryGetCapability<IPlatformDiagnostics>(out _), composed.TryGetCapability<IPlatformDiagnostics>(out _), "no diagnostics appear");
        Assert.AreEqual(direct.TryGetCapability<IDisplayPacing>(out _), composed.TryGetCapability<IDisplayPacing>(out _));
        Assert.AreEqual(direct.TryGetCapability<IPreciseDelay>(out _), composed.TryGetCapability<IPreciseDelay>(out _));
        Assert.AreEqual(direct.TryGetCapability<ITouchScreen>(out _), composed.TryGetCapability<ITouchScreen>(out _));
        Assert.IsFalse(composed.TryGetCapability<IAbsent>(out _));
        Assert.AreEqual(direct.CanBlock, composed.CanBlock);
        Assert.AreEqual(direct.Letterbox, composed.Letterbox);
        Assert.AreEqual(direct.Clock.Now, composed.Clock.Now, 0.5, "both are headless monotonic clocks started moments apart");
    }

    [TestMethod]
    public void The_platform_contract_has_no_default_members_a_wrapper_could_miss()
    {
        // A member left to an interface's default would silently replace the wrapped platform's answer.
        foreach (var contract in new[] { typeof(IPlatform), typeof(IPlatformHost) })
        {
            var defaulted = contract.GetMethods().Where(m => !m.IsAbstract).Select(m => m.Name).ToList();
            Assert.AreEqual(0, defaulted.Count, $"{contract.Name} defaults: {string.Join(", ", defaulted)}");
        }
    }

    [TestMethod]
    public void A_composed_platform_still_replaces_audio_and_combines_input()
    {
        // The behaviour composition exists for holds with the capabilities intact.
        var audio = new SilentAudioDevice();
        using var composed = Full().Create(new PlatformRequest(Settings) { Audio = () => audio, ExtraInput = [new KeyDown(Key.Z)] });
        Assert.AreSame(audio, composed.Audio);
        Assert.IsTrue(composed.Input.IsKeyDown(Key.Z));
    }

    private sealed class KeyDown(Key key) : IInputSource
    {
        public bool IsKeyDown(Key k) => k == key;
        public bool IsPadDown(PadButton button) => false;
        public Vector2 LeftStick => Vector2.Zero;
    }
}

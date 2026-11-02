using System.Numerics;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;

namespace MicroEngine.Tests;

/// <summary>
/// A platform whose audio is replaced or whose input is extended (<c>--audio-capture</c>, a host's extra input) keeps
/// every capability of the backend it wraps: display pacing, its timer, run-time display control and diagnostics.
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

    /// <summary>A backend with every optional capability, each with a value no default has.</summary>
    private sealed class FullPlatform : IPlatform, IDisplayControl, IPlatformDiagnostics
    {
        private readonly HeadlessPlatform _inner = new();
        public List<double> Waits { get; } = [];
        public List<string> Calls { get; } = [];
        public IGraphicsDevice Graphics => _inner.Graphics;
        public IAudioDevice Audio => _inner.Audio;
        public IInputSource Input => _inner.Input;
        public bool CloseRequested => false;
        public double Now => 12.5;
        public bool RealTime => true;
        public double DisplayRate => 144;
        public bool Suspended => true;
        public bool Wait(double seconds) { Waits.Add(seconds); return false; }
        public ITouchScreen? Touch => _inner.Touch;
        public void PollEvents() { }
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

    private static PlatformProfile Wrapping(Func<PlatformRequest, IPlatform> create) => new("wrapped", "test profile", create);

    [TestMethod]
    public void A_composed_platform_forwards_every_optional_capability()
    {
        var inner = new FullPlatform();
        using var composed = Wrapping(_ => inner).Create(Composing(Settings));
        Assert.AreNotSame(inner, composed, "the request replaces audio and adds input, so the backend is wrapped");

        Assert.AreEqual(144, composed.DisplayRate, "display pacing");
        Assert.IsFalse(composed.Wait(0.004), "the backend's own timer answers");
        CollectionAssert.AreEqual(new[] { 0.004 }, inner.Waits);
        Assert.IsTrue(composed.Suspended);
        Assert.AreEqual(12.5, composed.Now);
        Assert.IsTrue(composed.RealTime);
        Assert.AreSame(inner.Touch, composed.Touch);
        Assert.AreEqual(Color.Hex("123456"), composed.Letterbox);

        var control = composed as IDisplayControl;
        Assert.IsNotNull(control, "run-time display control");
        Assert.AreSame(inner.Displays, control.Displays);
        Assert.AreEqual(1, control.CurrentDisplay);
        Assert.IsTrue(control.VSync);
        Assert.IsTrue(control.SetVSync(false));
        Assert.IsFalse(inner.VSync);
        Assert.AreEqual(WindowMode.Fullscreen, control.WindowMode);
        Assert.IsTrue(control.SetWindowMode(WindowMode.Windowed));
        Assert.IsTrue(control.MoveToDisplay(0));
        CollectionAssert.AreEqual(new[] { "vsync False", "mode Windowed", "move 0" }, inner.Calls);

        var diagnostics = composed as IPlatformDiagnostics;
        Assert.IsNotNull(diagnostics, "diagnostics");
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
    public void The_game_screen_sees_the_backend_through_a_composed_platform()
    {
        var direct = new EmptyGame();
        direct.InitializeForTest(new FullPlatform());
        var composed = new EmptyGame();
        composed.InitializeForTest(Wrapping(_ => new FullPlatform()).Create(Composing(composed.Settings)));
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

    [TestMethod]
    public void A_composed_headless_platform_has_exactly_the_headless_capabilities()
    {
        var settings = Settings with { UpdateRate = 120 };
        using var direct = HeadlessPlatform.Profile.Create(new PlatformRequest(settings));
        using var composed = HeadlessPlatform.Profile.Create(Composing(settings));
        Assert.IsInstanceOfType<HeadlessPlatform>(direct);
        Assert.IsNotInstanceOfType<HeadlessPlatform>(composed);
        Assert.IsNotInstanceOfType<IDisplayControl>(direct);
        Assert.IsNotInstanceOfType<IDisplayControl>(composed, "no run-time display control appears");
        Assert.IsNotInstanceOfType<IPlatformDiagnostics>(direct);
        Assert.IsNotInstanceOfType<IPlatformDiagnostics>(composed, "no diagnostics appear");
        Assert.AreEqual(direct.DisplayRate, composed.DisplayRate);
        Assert.AreEqual(direct.RealTime, composed.RealTime);
        Assert.AreEqual(direct.Suspended, composed.Suspended);
        Assert.AreEqual(direct.Touch is null, composed.Touch is null);
        Assert.AreEqual(direct.Letterbox, composed.Letterbox);
        for (int i = 0; i < 6; i++) { direct.PollEvents(); composed.PollEvents(); }
        Assert.AreEqual(direct.Now, composed.Now);
        Assert.AreEqual(direct.Wait(0), composed.Wait(0));
    }

    [TestMethod]
    public void A_composed_platform_implements_every_platform_member_itself()
    {
        // A member left to the interface's default would silently replace the backend's answer.
        using var composed = Wrapping(_ => new FullPlatform()).Create(Composing(Settings));
        var map = composed.GetType().GetInterfaceMap(typeof(IPlatform));
        var defaulted = map.TargetMethods.Where(m => m.DeclaringType == typeof(IPlatform)).Select(m => m.Name).ToList();
        Assert.AreEqual(0, defaulted.Count, $"left to IPlatform's defaults: {string.Join(", ", defaulted)}");
    }

    [TestMethod]
    public void A_composed_platform_still_replaces_audio_and_combines_input()
    {
        // The behaviour composition exists for holds with the capabilities forwarded.
        var audio = new SilentAudioDevice();
        using var composed = Wrapping(_ => new FullPlatform()).Create(new PlatformRequest(Settings) { Audio = () => audio, ExtraInput = [new KeyDown(Key.Z)] });
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

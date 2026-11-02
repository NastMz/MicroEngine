// ARCH-1 core experiments (throwaway). Run: dotnet run -- <experiment>
using System.Numerics;
using System.Reflection;
using MicroEngine;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;

switch (args.FirstOrDefault())
{
    case "compose": Compose(); break;
    case "stub": RunStub(); break;
    case "surface": Surface(); break;
    case "headless-rate": HeadlessRate(); break;
    case "assets": Assets(); break;
    default: Compose(); RunStub(); HeadlessRate(); Assets(); Surface(); break;
}

// E1: a profile that replaces audio or adds input wraps the backend in ComposedPlatform. Which optional
// capabilities survive the wrapping?
static void Compose()
{
    Console.WriteLine("== E1 composition keeps optional capabilities?");
    var settings = new GameSettings("t");
    var profile = new PlatformProfile("stub", "stub backend", r => new StubPlatform(r.Settings));
    IPlatform direct = profile.Create(new PlatformRequest(settings));
    IPlatform composed = profile.Create(new PlatformRequest(settings) { ExtraInput = [new StubInput()] });
    foreach (var (label, p) in new[] { ("direct", direct), ("composed (ExtraInput)", composed) })
    {
        bool waited = p.Wait(0.0005);
        Console.WriteLine($"{label,-24} type={p.GetType().Name,-18} DisplayRate={p.DisplayRate,5} IDisplayControl={p is IDisplayControl,-5} IPlatformDiagnostics={p is IPlatformDiagnostics,-5} Wait->backend={StubPlatform.WaitCalls > 0}");
        StubPlatform.WaitCalls = 0;
    }
    var game = new EmptyGame();
    game.InitializeForTest(composed);
    Console.WriteLine($"Game.Screen.CanChange through composed platform: {game.Screen.CanChange}; Displays: {game.Screen.Displays.Count}");
}

// E2: the smallest backend: run a game on it for 10 updates.
static void RunStub()
{
    Console.WriteLine("== E2 stub backend runs a game");
    var r = GameRunner.Run(new EmptyGame(), new StubPlatform(new GameSettings("t")), new RunOptions { MaxUpdates = 10 });
    Console.WriteLine($"updates={r.Updates} frames={r.Frames} error={r.Error?.GetType().Name ?? "none"} {r.Error?.Message}");
    int Count(Type t) => t.GetMembers(BindingFlags.Public | BindingFlags.Instance).Count(m => m is MethodInfo { IsSpecialName: false } or PropertyInfo or EventInfo);
    int Abstract(Type t) => t.GetMethods(BindingFlags.Public | BindingFlags.Instance).Count(m => m.IsAbstract && !m.IsSpecialName)
        + t.GetProperties().Count(p => (p.GetMethod?.IsAbstract ?? false) || (p.SetMethod?.IsAbstract ?? false));
    foreach (var t in new[] { typeof(IPlatform), typeof(IGraphicsDevice), typeof(IRenderer), typeof(IInputSource), typeof(IAudioDevice), typeof(ITouchScreen), typeof(IOverlayCanvas), typeof(IDisplayControl), typeof(IPlatformDiagnostics), typeof(ISaveStorage) })
        Console.WriteLine($"{t.Name,-22} members={Count(t),3} required(no default)={Abstract(t),3}");
    var texAbstract = typeof(Texture).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Count(m => m.IsAbstract);
    Console.WriteLine($"Texture (abstract class) protected abstract methods={texAbstract}");
}

// E3: HeadlessPlatform.Now with an update rate other than 60.
static void HeadlessRate()
{
    Console.WriteLine("== E3 headless clock vs UpdateRate");
    var game = new RateGame();
    var platform = new HeadlessPlatform();
    var r = GameRunner.Run(game, platform, new RunOptions { MaxUpdates = 120 });
    Console.WriteLine($"UpdateRate=120: updates={r.Updates} Game.Time={game.Time:0.000}s platform.Now={platform.Now:0.000}s (frames/60)");
}

// E4: asset cache identity: sibling scopes and Unload while a reference is held.
static void Assets()
{
    Console.WriteLine("== E4 asset identity and lifetime");
    string root = Path.Combine(Path.GetTempPath(), "arch1-assets-" + Environment.ProcessId);
    Directory.CreateDirectory(root);
    var rgba = Enumerable.Repeat((byte)255, 64).ToArray();
    File.WriteAllBytes(Path.Combine(root, "a.png"), PngEncoder.Encode(rgba, 4, 4));
    var device = new HeadlessGraphicsDevice();
    var cache = new AssetCache(device, root);
    using (var level1 = cache.CreateScope("level1"))
    using (var level2 = cache.CreateScope("level2"))
    {
        var t1 = level1.Texture("a.png"); var t2 = level2.Texture("a.png");
        Console.WriteLine($"sibling scopes share one texture: {ReferenceEquals(t1, t2)} (device textures alive: {device.GetType().GetProperty("Created")?.GetValue(device) ?? "n/a"})");
        var t3 = level1.Texture("a.png", TextureFilter.Linear);
        Console.WriteLine($"same file, other filter -> other texture: {!ReferenceEquals(t1, t3)}; cache count={cache.Count}");
    }
    var held = cache.Texture("a.png");
    cache.Unload("a.png");
    Console.WriteLine($"held texture after Unload: IsDisposed={held.IsDisposed}");
    Directory.Delete(root, true);
}

// E5: public API surface of the core assembly.
static void Surface()
{
    Console.WriteLine("== E5 public surface of MicroEngine.dll");
    var asm = typeof(Game).Assembly;
    var types = asm.GetExportedTypes();
    foreach (var g in types.GroupBy(t => t.Namespace).OrderBy(g => g.Key))
    {
        int members = g.Sum(t => t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Count(m => m is not MethodInfo { IsSpecialName: true }));
        Console.WriteLine($"{g.Key,-22} types={g.Count(),3} public members={members,4}");
    }
    Console.WriteLine($"total exported types={types.Length}");
    var settable = types.SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => p.SetMethod?.IsPublic == true).Select(p => $"{t.Name}.{p.Name}"));
    Console.WriteLine("public static settable: " + string.Join(", ", settable));
    var hooks = types.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => m.Name.EndsWith("ForTest")).Select(m => $"{t.Name}.{m.Name}"));
    Console.WriteLine("public test hooks: " + string.Join(", ", hooks));
}

sealed class EmptyGame : Game
{
    public override GameSettings Settings { get; } = new("empty");
    protected override void Load() => Scenes.Push(new EmptyScene());
}
sealed class RateGame : Game
{
    public override GameSettings Settings { get; } = new("rate") { UpdateRate = 120 };
    protected override void Load() => Scenes.Push(new EmptyScene());
}
sealed class EmptyScene : Scene
{
    public override void Update(float dt) { }
    public override void Draw(IRenderer r) { }
}

// ---- The smallest backend the core accepts.
sealed class StubPlatform(GameSettings settings) : IPlatform, IDisplayControl, IPlatformDiagnostics
{
    public static int WaitCalls;
    private readonly StubRenderer _renderer = new(settings.VirtualWidth, settings.VirtualHeight);
    private int _frames;
    public IGraphicsDevice Graphics { get; } = new StubDevice();
    public IAudioDevice Audio { get; } = new SilentAudioDevice();
    public IInputSource Input { get; } = new StubInput();
    public bool CloseRequested => false;
    public double Now => _frames / 60.0;
    public bool RealTime => false;
    public double DisplayRate => 144;
    public bool Wait(double seconds) { WaitCalls++; return true; }
    public void PollEvents() => _frames++;
    public IRenderer BeginFrame() => _renderer;
    public void EndFrame() { }
    public void SaveScreenshot(string path) { }
    public void ToggleFullscreen() { }
    public void Dispose() { }
    // IDisplayControl
    public IReadOnlyList<DisplayInfo> Displays => [];
    public int CurrentDisplay => -1;
    public bool VSync => true;
    public bool SetVSync(bool on) => on;
    public WindowMode WindowMode => WindowMode.Windowed;
    public bool SetWindowMode(WindowMode mode) => false;
    public bool MoveToDisplay(int index) => false;
    // IPlatformDiagnostics
    public IReadOnlyDictionary<string, string> Diagnostics => new Dictionary<string, string> { ["renderer"] = "stub" };
}
sealed class StubDevice : IGraphicsDevice
{
    public TextureFilter DefaultFilter { get; set; }
    public Texture CreateTexture(PixelImage image, TextureFilter filter) => new StubTexture(image.Width, image.Height, filter);
}
sealed class StubTexture(int w, int h, TextureFilter f) : Texture(w, h, f)
{
    protected override void Upload(int x, int y, int width, int height, ReadOnlySpan<Color> pixels) { }
    protected override void ApplyFilter(TextureFilter filter) { }
    protected override void Release() { }
}
sealed class StubRenderer(int w, int h) : IRenderer
{
    public int Width => w;
    public int Height => h;
    public void Clear(Color color) { }
    public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default) { }
    public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint) { }
    public void FillRect(RectF rect, Color color) { }
    public void FillCircle(Vector2 center, float radius, Color color) { }
    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color) { }
    public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color) { }
    public void PushCamera(Camera2D camera) { }
    public void PopCamera() { }
    public void SetBlendMode(BlendMode mode) { }
    public void SetClip(RectF? screenRect) { }
}
sealed class StubInput : IInputSource
{
    public bool IsKeyDown(Key key) => false;
    public bool IsPadDown(PadButton button) => false;
    public Vector2 LeftStick => Vector2.Zero;
}

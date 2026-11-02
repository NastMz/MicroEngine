using System.Numerics;
using MicroEngine;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;

// Substitution-cost probe: a "custom renderer" (a command recorder standing in for a retained/batched or GPU renderer)
// plugged into the engine. The only seam is IPlatform, so the stub must supply a whole platform; it borrows the headless
// platform's input/audio/time to keep the count to what the renderer itself costs.
var stub = new RecordingPlatform(new HeadlessPlatform(320, 180));
var result = GameRunner.Run(new ProbeGame(), stub, new RunOptions { MaxUpdates = 3, StartScreen = false });
Console.WriteLine($"updates={result.Updates} frames={result.Frames} error={result.Error?.GetType().Name}: {result.Error?.Message}");
foreach (var (frame, cmds) in stub.Frames.Select((c, i) => (i, c)))
    Console.WriteLine($"frame {frame}: {cmds.Count} commands: " + string.Join(", ", cmds.GroupBy(c => c.Kind).Select(g => $"{g.Key}x{g.Count()}")));
Console.WriteLine($"textures created={stub.Device.Created} filters=[{string.Join(",", stub.Device.Filters)}]");

internal sealed class ProbeGame : Game
{
    public override GameSettings Settings { get; } = new("stub") { Scaling = CanvasScaling.Smooth, VirtualWidth = 1280, VirtualHeight = 720 };
    protected override void Load() => Scenes.Push(new ProbeScene(this));
}

internal sealed class ProbeScene(ProbeGame game) : Scene
{
    private readonly Camera2D _camera = new(1280, 720);
    private SpriteAtlas? _atlas;
    public override void Enter() => _atlas = new SpriteAtlasBuilder().Add("dot", new PixelImage(4, 4).Clear(Color.White)).Build(game.Graphics);
    public override void Update(float dt) { }
    public override void Draw(IRenderer r)
    {
        r.PushCamera(_camera);
        r.Draw(_atlas!["dot"], new Vector2(10, 10));
        r.PopCamera();
        game.Font.DrawCentered(r, "Hello", new Vector2(640, 360), Color.White, 2);
        r.SetBlendMode(BlendMode.Additive);
        r.FillCircle(new Vector2(100, 100), 20, Color.White);
        r.SetBlendMode(BlendMode.Alpha);
    }
}

public readonly record struct Cmd(string Kind, Texture? Texture, RectF A, RectF B, Color Color);

public sealed class RecordingDevice : IGraphicsDevice
{
    public int Created;
    public List<TextureFilter> Filters { get; } = [];
    public TextureFilter DefaultFilter { get; set; }
    public Texture CreateTexture(PixelImage image, TextureFilter filter) { Created++; Filters.Add(filter); return new Tex(image.Width, image.Height, filter); }
    private sealed class Tex(int w, int h, TextureFilter f) : Texture(w, h, f)
    {
        protected override void Upload(int x, int y, int width, int height, ReadOnlySpan<Color> pixels) { }
        protected override void ApplyFilter(TextureFilter filter) { }
        protected override void Release() { }
    }
}

public sealed class RecordingRenderer(int w, int h) : IRenderer
{
    private readonly Stack<Vector2> _cams = new();
    private Vector2 _offset;
    public List<Cmd> Commands { get; private set; } = [];
    public int Width => w;
    public int Height => h;
    public void Begin() { Commands = []; _cams.Clear(); _offset = Vector2.Zero; }
    public void Clear(Color color) => Commands.Add(new("clear", null, default, default, color));
    public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default)
        => Commands.Add(new("sprite", texture, source, new RectF(position.X + _offset.X, position.Y + _offset.Y, source.Width, source.Height), tint));
    public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint) => Commands.Add(new("stretched", texture, source, destination, tint));
    public void FillRect(RectF rect, Color color) => Commands.Add(new("rect", null, rect, default, color));
    public void FillCircle(Vector2 center, float radius, Color color) => Commands.Add(new("circle", null, default, default, color));
    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color) => Commands.Add(new("tri", null, default, default, color));
    public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color) => Commands.Add(new("line", null, default, default, color));
    public void PushCamera(Camera2D camera) { _cams.Push(_offset); _offset = camera.RenderOffset; }
    public void PopCamera() => _offset = _cams.Pop();
    public void SetBlendMode(BlendMode mode) => Commands.Add(new($"blend:{mode}", null, default, default, default));
    public void SetClip(RectF? screenRect) => Commands.Add(new("clip", null, screenRect ?? default, default, default));
}

public sealed class RecordingPlatform(HeadlessPlatform inner) : IPlatform
{
    private readonly RecordingRenderer _renderer = new(320, 180);
    public RecordingDevice Device { get; } = new();
    public List<List<Cmd>> Frames { get; } = [];
    public IGraphicsDevice Graphics => Device;
    public IAudioDevice Audio => inner.Audio;
    public IInputSource Input => inner.Input;
    public bool CloseRequested => inner.CloseRequested;
    public double Now => inner.Now;
    public bool RealTime => false;
    public void PollEvents() => inner.PollEvents();
    public IRenderer BeginFrame() { _renderer.Begin(); return _renderer; }
    public void EndFrame() => Frames.Add(_renderer.Commands);
    public void SaveScreenshot(string path) { }
    public void ToggleFullscreen() { }
    public void Dispose() => inner.Dispose();
}

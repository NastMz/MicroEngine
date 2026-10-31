using System.Numerics;
using MicroEngine.Audio;
using MicroEngine.Graphics;
using MicroEngine.Input;

namespace MicroEngine.Core;

/// <summary>
/// A windowless platform: deterministic time, no devices, and a renderer that only counts
/// calls. Used by automated tests and CI to play whole games through scripts.
/// </summary>
public sealed class HeadlessPlatform : IPlatform
{
    private readonly HeadlessRenderer _renderer;
    private int _frames;
    public HeadlessPlatform(int width = 320, int height = 180) => _renderer = new HeadlessRenderer(width, height);

    public IGraphicsDevice Graphics { get; } = new HeadlessGraphicsDevice();
    public SilentAudioDevice SilentAudio { get; } = new();
    public IAudioDevice Audio => SilentAudio;
    public HeadlessInput Keys { get; } = new();
    public IInputSource Input => Keys;
    public bool CloseRequested { get; set; }
    public double Now => _frames / 60.0;
    public bool RealTime => false;
    public HeadlessRenderer Renderer => _renderer;
    public List<string> Screenshots { get; } = [];

    public static HeadlessPlatform For(GameSettings settings) => new(settings.VirtualWidth, settings.VirtualHeight);

    /// <summary>The headless platform as a catalog entry (<c>--platform headless</c>, <c>--headless</c>).</summary>
    public static PlatformProfile Profile { get; } = new("headless", "no window, devices or sound; one step per frame, for tests and CI", request => For(request.Settings));

    public void PollEvents() => _frames++;
    public IRenderer BeginFrame() { _renderer.Reset(); return _renderer; }
    public void EndFrame() { }
    public void SaveScreenshot(string path) => Screenshots.Add(path);
    public void ToggleFullscreen() { }
    public void Dispose() { }
}

public sealed class HeadlessInput : IInputSource
{
    private readonly HashSet<Key> _keys = [];
    public bool IsKeyDown(Key key) => _keys.Contains(key);
    public bool IsPadDown(PadButton button) => false;
    public Vector2 LeftStick => Vector2.Zero;
    public void Set(Key key, bool down) { if (down) _keys.Add(key); else _keys.Remove(key); }
}

public sealed class HeadlessGraphicsDevice : IGraphicsDevice
{
    public int TexturesCreated { get; private set; }
    public Texture CreateTexture(PixelImage image)
    {
        TexturesCreated++;
        return new HeadlessTexture(image.Width, image.Height);
    }

    private sealed class HeadlessTexture(int width, int height) : Texture(width, height)
    {
        protected override void Release() { }
    }
}

/// <summary>Counts draw calls and tracks camera balance so tests can assert rendering ran correctly.</summary>
public sealed class HeadlessRenderer(int width, int height) : IRenderer
{
    private int _cameraDepth;
    public int Width { get; } = width;
    public int Height { get; } = height;
    public int DrawCalls { get; private set; }
    public int CameraDepth => _cameraDepth;

    public void Reset()
    {
        if (_cameraDepth != 0) throw new InvalidOperationException("Unbalanced PushCamera/PopCamera in the previous frame.");
        DrawCalls = 0;
    }
    public void Clear(Color color) => DrawCalls++;
    public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default)
    {
        ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
        DrawCalls++;
    }
    public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint) => DrawCalls++;
    public void FillRect(RectF rect, Color color) => DrawCalls++;
    public void FillCircle(Vector2 center, float radius, Color color) => DrawCalls++;
    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color) => DrawCalls++;
    public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color) => DrawCalls++;
    public void PushCamera(Camera2D camera) => _cameraDepth++;
    public void PopCamera()
    {
        if (_cameraDepth == 0) throw new InvalidOperationException("PopCamera without PushCamera.");
        _cameraDepth--;
    }
    public void SetBlendMode(BlendMode mode) { }
    public void SetClip(RectF? screenRect) { }
}

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
    public HeadlessPlatform(int width = 320, int height = 180)
    {
        _renderer = new HeadlessRenderer(width, height);
        TouchScreen = new HeadlessTouchScreen(new Vector2(width * 4, height * 4));
    }

    public IGraphicsDevice Graphics { get; } = new HeadlessGraphicsDevice();
    public SilentAudioDevice SilentAudio { get; } = new();
    public IAudioDevice Audio => SilentAudio;
    public HeadlessInput Keys { get; } = new();
    public IInputSource Input => Keys;
    /// <summary>A touch screen tests can touch; by default 4× the canvas, not touch-first, no safe-area insets.</summary>
    public HeadlessTouchScreen TouchScreen { get; }
    public ITouchScreen Touch => TouchScreen;
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

/// <summary>Keys, a gamepad and a pointer that tests set by hand.</summary>
public sealed class HeadlessInput : IInputSource
{
    private readonly HashSet<Key> _keys = [];
    private readonly HashSet<PadButton> _buttons = [];
    private readonly Dictionary<PadAxis, float> _axes = [];
    public bool IsKeyDown(Key key) => _keys.Contains(key);
    public bool IsPadDown(PadButton button) => _buttons.Contains(button);
    public Vector2 LeftStick => new(GetPadAxis(PadAxis.LeftX), GetPadAxis(PadAxis.LeftY));
    public float GetPadAxis(PadAxis axis) => _axes.GetValueOrDefault(axis);
    public bool PadConnected { get; set; }
    public PointerState Pointer { get; set; }
    public void Set(Key key, bool down) { if (down) _keys.Add(key); else _keys.Remove(key); }
    public void Set(PadButton button, bool down) { if (down) _buttons.Add(button); else _buttons.Remove(button); }
    public void Set(PadAxis axis, float value) => _axes[axis] = value;
    public void SetLeftStick(Vector2 value) { _axes[PadAxis.LeftX] = value.X; _axes[PadAxis.LeftY] = value.Y; }
}

/// <summary>
/// A touch screen for tests: fingers go down, move and lift by hand, the size, safe area and density can change (a
/// rotation, a notch), vibrations are counted, and <see cref="DrawOverlay"/> records what the overlay draws.
/// </summary>
public sealed class HeadlessTouchScreen(Vector2 size) : ITouchScreen
{
    private readonly List<TouchEvent> _pending = [];
    private RectF? _safe;

    public Vector2 Size { get; set; } = size;
    /// <summary>Defaults to the whole screen.</summary>
    public RectF SafeArea { get => _safe ?? new RectF(0, 0, Size.X, Size.Y); set => _safe = value; }
    public float PixelsPerDp { get; set; } = 1;
    public bool TouchFirst { get; set; }
    public ITouchOverlay? Overlay { get; set; }
    public bool CanVibrate { get; set; } = true;
    public int Vibrations { get; private set; }

    public void Down(long finger, float x, float y) => _pending.Add(new TouchEvent(finger, TouchPhase.Began, new Vector2(x, y)));
    public void Down(long finger, Vector2 at) => Down(finger, at.X, at.Y);
    public void Move(long finger, Vector2 at) => _pending.Add(new TouchEvent(finger, TouchPhase.Moved, at));
    public void Up(long finger, Vector2 at = default) => _pending.Add(new TouchEvent(finger, TouchPhase.Ended, at));
    public void Cancel(long finger) => _pending.Add(new TouchEvent(finger, TouchPhase.Canceled, default));

    public void ReadTouches(List<TouchEvent> events)
    {
        events.AddRange(_pending);
        _pending.Clear();
    }

    public bool Vibrate(float strength, int milliseconds)
    {
        if (!CanVibrate) return false;
        Vibrations++;
        return true;
    }

    /// <summary>Draws the overlay into a recorder and returns what it drew.</summary>
    public RecordingOverlayCanvas DrawOverlay()
    {
        var canvas = new RecordingOverlayCanvas();
        Overlay?.Draw(canvas);
        return canvas;
    }
}

/// <summary>An <see cref="IOverlayCanvas"/> that keeps a list of the shapes drawn, for tests.</summary>
public sealed class RecordingOverlayCanvas : IOverlayCanvas
{
    public readonly record struct Shape(string Kind, Vector2 Center, float Size, Color Color, string? Text = null);
    public List<Shape> Shapes { get; } = [];
    public void FillCircle(Vector2 center, float radius, Color color) => Shapes.Add(new("circle", center, radius, color));
    public void Ring(Vector2 center, float radius, float thickness, Color color) => Shapes.Add(new("ring", center, radius, color));
    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color) => Shapes.Add(new("triangle", (a + b + c) / 3, Vector2.Distance(a, b), color));
    public void FillRect(RectF rect, Color color) => Shapes.Add(new("rect", rect.Center, rect.Width, color));
    public void Text(string text, Vector2 center, float height, Color color) => Shapes.Add(new("text", center, height, color, text));
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

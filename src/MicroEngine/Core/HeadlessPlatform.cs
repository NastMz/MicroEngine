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
    /// <summary>Creates a headless platform whose renderer reports a <paramref name="width"/> × <paramref name="height"/> canvas.</summary>
    public HeadlessPlatform(int width = 320, int height = 180)
    {
        _renderer = new HeadlessRenderer(width, height);
        TouchScreen = new HeadlessTouchScreen(new Vector2(width * 4, height * 4));
    }

    /// <summary>A <see cref="HeadlessGraphicsDevice"/>: textures have a size but no pixels.</summary>
    public IGraphicsDevice Graphics { get; } = new HeadlessGraphicsDevice();
    /// <summary>The silent audio device, typed so tests can read <see cref="SilentAudioDevice.Played"/>.</summary>
    public SilentAudioDevice SilentAudio { get; } = new();
    /// <summary>The same device as <see cref="SilentAudio"/>.</summary>
    public IAudioDevice Audio => SilentAudio;
    /// <summary>The input tests set by hand (keys, gamepad, pointer).</summary>
    public HeadlessInput Keys { get; } = new();
    /// <summary>The same source as <see cref="Keys"/>.</summary>
    public IInputSource Input => Keys;
    /// <summary>A touch screen tests can touch; by default 4× the canvas, not touch-first, no safe-area insets.</summary>
    public HeadlessTouchScreen TouchScreen { get; }
    /// <summary>The same screen as <see cref="TouchScreen"/>.</summary>
    public ITouchScreen Touch => TouchScreen;
    /// <summary>Set it to true to stop the loop at the next frame, as if the window closed.</summary>
    public bool CloseRequested { get; set; }
    /// <summary>Simulated time: 1/60 second per frame polled.</summary>
    public double Now => _frames / 60.0;
    /// <summary>Always false: the loop advances exactly one update per frame.</summary>
    public bool RealTime => false;
    /// <summary>The renderer, typed so tests can read its counters.</summary>
    public HeadlessRenderer Renderer => _renderer;
    /// <summary>Paths of the screenshots requested, in order; no file is written.</summary>
    public List<string> Screenshots { get; } = [];

    /// <summary>A headless platform with the virtual size of <paramref name="settings"/>.</summary>
    public static HeadlessPlatform For(GameSettings settings) => new(settings.VirtualWidth, settings.VirtualHeight);

    /// <summary>The headless platform as a catalog entry (<c>--platform headless</c>, <c>--headless</c>).</summary>
    public static PlatformProfile Profile { get; } = new("headless", "no window, devices or sound; one step per frame, for tests and CI", request => For(request.Settings));

    /// <summary>Advances simulated time by one frame.</summary>
    public void PollEvents() => _frames++;
    /// <inheritdoc/>
    public IRenderer BeginFrame() { _renderer.Reset(); return _renderer; }
    /// <inheritdoc/>
    public void EndFrame() { }
    /// <summary>Records <paramref name="path"/> in <see cref="Screenshots"/> without writing a file.</summary>
    public void SaveScreenshot(string path) => Screenshots.Add(path);
    /// <summary>Does nothing.</summary>
    public void ToggleFullscreen() { }
    /// <summary>Does nothing; there is nothing to release.</summary>
    public void Dispose() { }
}

/// <summary>Keys, a gamepad and a pointer that tests set by hand.</summary>
public sealed class HeadlessInput : IInputSource
{
    private readonly HashSet<Key> _keys = [];
    private readonly HashSet<PadButton> _buttons = [];
    private readonly Dictionary<PadAxis, float> _axes = [];
    /// <inheritdoc/>
    public bool IsKeyDown(Key key) => _keys.Contains(key);
    /// <inheritdoc/>
    public bool IsPadDown(PadButton button) => _buttons.Contains(button);
    /// <inheritdoc/>
    public Vector2 LeftStick => new(GetPadAxis(PadAxis.LeftX), GetPadAxis(PadAxis.LeftY));
    /// <inheritdoc/>
    public float GetPadAxis(PadAxis axis) => _axes.GetValueOrDefault(axis);
    /// <inheritdoc/>
    public bool PadConnected { get; set; }
    /// <inheritdoc/>
    public GamepadStyle PadStyle { get; set; }
    /// <inheritdoc/>
    public PointerState Pointer { get; set; }
    /// <summary>Presses or releases a key.</summary>
    public void Set(Key key, bool down) { if (down) _keys.Add(key); else _keys.Remove(key); }
    /// <summary>Presses or releases a gamepad button.</summary>
    public void Set(PadButton button, bool down) { if (down) _buttons.Add(button); else _buttons.Remove(button); }
    /// <summary>Sets a gamepad axis (sticks -1..1, triggers 0..1).</summary>
    public void Set(PadAxis axis, float value) => _axes[axis] = value;
    /// <summary>Sets both axes of the left stick.</summary>
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

    /// <inheritdoc/>
    public Vector2 Size { get; set; } = size;
    /// <summary>Defaults to the whole screen.</summary>
    public RectF SafeArea { get => _safe ?? new RectF(0, 0, Size.X, Size.Y); set => _safe = value; }
    /// <inheritdoc/>
    public float PixelsPerDp { get; set; } = 1;
    /// <inheritdoc/>
    public bool TouchFirst { get; set; }
    /// <inheritdoc/>
    public ITouchOverlay? Overlay { get; set; }
    /// <inheritdoc/>
    public bool CanVibrate { get; set; } = true;
    /// <summary>Successful <see cref="Vibrate"/> calls so far.</summary>
    public int Vibrations { get; private set; }

    /// <summary>Puts <paramref name="finger"/> down at (<paramref name="x"/>, <paramref name="y"/>) in screen pixels; delivered at the next read.</summary>
    public void Down(long finger, float x, float y) => _pending.Add(new TouchEvent(finger, TouchPhase.Began, new Vector2(x, y)));
    /// <summary>Puts <paramref name="finger"/> down at <paramref name="at"/> in screen pixels.</summary>
    public void Down(long finger, Vector2 at) => Down(finger, at.X, at.Y);
    /// <summary>Moves <paramref name="finger"/> to <paramref name="at"/>.</summary>
    public void Move(long finger, Vector2 at) => _pending.Add(new TouchEvent(finger, TouchPhase.Moved, at));
    /// <summary>Lifts <paramref name="finger"/>.</summary>
    public void Up(long finger, Vector2 at = default) => _pending.Add(new TouchEvent(finger, TouchPhase.Ended, at));
    /// <summary>Cancels <paramref name="finger"/>, as the system does when the game loses the screen.</summary>
    public void Cancel(long finger) => _pending.Add(new TouchEvent(finger, TouchPhase.Canceled, default));

    /// <inheritdoc/>
    public void ReadTouches(List<TouchEvent> events)
    {
        events.AddRange(_pending);
        _pending.Clear();
    }

    /// <summary>Counts the vibration in <see cref="Vibrations"/>; false when <see cref="CanVibrate"/> is false.</summary>
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
    /// <summary>One recorded drawing call.</summary>
    /// <param name="Kind">"circle", "ring", "triangle", "rect", "text" or "image".</param>
    /// <param name="Center">Centre of the shape (the centroid for a triangle).</param>
    /// <param name="Size">Radius for circles and rings, width for rectangles, the length of the first side for triangles, height for text.</param>
    /// <param name="Color">The colour drawn.</param>
    /// <param name="Text">The text, for "text" shapes; otherwise null.</param>
    public readonly record struct Shape(string Kind, Vector2 Center, float Size, Color Color, string? Text = null);
    /// <summary>The shapes drawn, in order.</summary>
    public List<Shape> Shapes { get; } = [];
    /// <inheritdoc/>
    public void FillCircle(Vector2 center, float radius, Color color) => Shapes.Add(new("circle", center, radius, color));
    /// <inheritdoc/>
    public void Ring(Vector2 center, float radius, float thickness, Color color) => Shapes.Add(new("ring", center, radius, color));
    /// <inheritdoc/>
    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color) => Shapes.Add(new("triangle", (a + b + c) / 3, Vector2.Distance(a, b), color));
    /// <inheritdoc/>
    public void FillRect(RectF rect, Color color) => Shapes.Add(new("rect", rect.Center, rect.Width, color));
    /// <inheritdoc/>
    public void Text(string text, Vector2 center, float height, Color color) => Shapes.Add(new("text", center, height, color, text));
    /// <inheritdoc/>
    public void Image(PixelImage image, RectF destination, Color tint) => Shapes.Add(new("image", destination.Center, destination.Width, tint));
}

/// <summary>A graphics device for tests: textures keep their size but hold no pixels, and creations are counted.</summary>
public sealed class HeadlessGraphicsDevice : IGraphicsDevice
{
    /// <summary>Number of textures created so far.</summary>
    public int TexturesCreated { get; private set; }
    /// <inheritdoc/>
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
    /// <inheritdoc/>
    public int Width { get; } = width;
    /// <inheritdoc/>
    public int Height { get; } = height;
    /// <summary>Draw calls since the frame began (cleared by <see cref="Reset"/>).</summary>
    public int DrawCalls { get; private set; }
    /// <summary>Cameras pushed and not yet popped.</summary>
    public int CameraDepth => _cameraDepth;

    /// <summary>Starts a new frame: clears <see cref="DrawCalls"/>.</summary>
    /// <exception cref="InvalidOperationException">The previous frame pushed a camera it did not pop.</exception>
    public void Reset()
    {
        if (_cameraDepth != 0) throw new InvalidOperationException("Unbalanced PushCamera/PopCamera in the previous frame.");
        DrawCalls = 0;
    }
    /// <inheritdoc/>
    public void Clear(Color color) => DrawCalls++;
    /// <summary>Counts the call; throws <see cref="ObjectDisposedException"/> for a disposed texture, as real renderers fail.</summary>
    public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default)
    {
        ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
        DrawCalls++;
    }
    /// <inheritdoc/>
    public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint) => DrawCalls++;
    /// <inheritdoc/>
    public void FillRect(RectF rect, Color color) => DrawCalls++;
    /// <inheritdoc/>
    public void FillCircle(Vector2 center, float radius, Color color) => DrawCalls++;
    /// <inheritdoc/>
    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color) => DrawCalls++;
    /// <inheritdoc/>
    public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color) => DrawCalls++;
    /// <inheritdoc/>
    public void PushCamera(Camera2D camera) => _cameraDepth++;
    /// <inheritdoc/>
    public void PopCamera()
    {
        if (_cameraDepth == 0) throw new InvalidOperationException("PopCamera without PushCamera.");
        _cameraDepth--;
    }
    /// <inheritdoc/>
    public void SetBlendMode(BlendMode mode) { }
    /// <inheritdoc/>
    public void SetClip(RectF? screenRect) { }
}

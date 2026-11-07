using System.Numerics;
using MicroEngine.Audio;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Platform;

namespace MicroEngine.Core;

/// <summary>
/// A windowless platform: no devices, and a renderer that counts calls (or, with <see cref="Rasterize"/>, also records
/// them and draws them with the CPU reference rasterizer). Used by automated tests and CI to play whole games through
/// scripts. Its surface has the canvas's size; tests drive its events by hand (<see cref="Resize"/>, <see cref="Focus"/>,
/// <see cref="Suspend"/>, <see cref="Resume"/>, <see cref="RequestQuit"/>), which the next event pump delivers in order.
/// Its run preset (<see cref="Preset"/>) is deterministic time: a stated default of this testing composition that a
/// run or a game overrides. Nothing the platform does depends on the time mode.
/// </summary>
public sealed class HeadlessPlatform : IPlatform, IScreenView, IViewCapture
{
    private readonly int _width, _height;
    private HeadlessRenderer _renderer;
    private HeadlessGraphicsDevice _graphics = new();
    private (int Width, int Height) _surfaceSize;
    private readonly HostSurface _surface;
    private readonly Kernel.CapabilityRegistry _capabilities = new();
    private readonly Queue<Action<IEngineEventSink>> _pending = new();
    private readonly IAudioDevice? _suppliedAudio;
    private readonly IInputSource _input;
    private bool _suspended, _quitAsked, _disposed;
    private bool? _focused;

    /// <summary>The clipboard (the <see cref="IClipboard"/> capability): tests put text on it or read what was copied.</summary>
    public MemoryClipboard Clipboard { get; } = new();

    /// <summary>Creates a headless platform whose renderer reports a <paramref name="width"/> × <paramref name="height"/> canvas.</summary>
    public HeadlessPlatform(int width = 320, int height = 180) : this(width, height, null) { }

    private HeadlessPlatform(int width, int height, PlatformRequest? request)
    {
        (_width, _height) = (width, height);
        _surfaceSize = (width, height);
        _renderer = new HeadlessRenderer(width, height);
        TouchScreen = new HeadlessTouchScreen(new Vector2(width * 4, height * 4));
        _surface = new HostSurface(0, () => _surfaceSize, () => _density, () => false);
        _capabilities.Add<ITouchScreen>(TouchScreen);
        _capabilities.Add<IInputBackend>(Devices);
        _capabilities.Add<IKeyLabels>(Devices);
        _capabilities.Add<IGamepadRumble>(Devices);
        _capabilities.Add<ITextInputControl>(Devices);
        _capabilities.Add<IClipboard>(Clipboard);
        _capabilities.Add(_preset);
        _capabilities.Add<IScreenView>(this);
        _capabilities.Add<IViewCapture>(this);
        if (request?.Audio is not null) _suppliedAudio = request.CreateAudio(() => SilentAudio);
        _input = request?.CombineInput(Keys) ?? Keys;
    }

    /// <summary>
    /// When true, textures keep their pixels and the renderer records each frame's draws
    /// (<see cref="HeadlessRenderer.Commands"/>) and draws them on <see cref="HeadlessRenderer.Frame"/> with the CPU
    /// reference rasterizer, following the engine's rendering rules; <see cref="SaveScreenshot"/> then writes that frame
    /// as a PNG (at scale 1). False by default: the renderer only counts. Set it when creating the platform.
    /// </summary>
    public bool Rasterize
    {
        get => _renderer.Rasterizes;
        init
        {
            if (value == _renderer.Rasterizes) return;
            _graphics = new HeadlessGraphicsDevice { KeepsPixels = value };
            _renderer = value ? new HeadlessRenderer(_width, _height, _graphics) : new HeadlessRenderer(_width, _height);
        }
    }

    /// <summary>
    /// The run defaults this testing composition states (<see cref="RunPreset"/>, offered as a capability):
    /// <see cref="RunPreset.Testing"/> by default, so a game run on it through <see cref="GameRunner"/>,
    /// <see cref="GameLoop"/>, <see cref="GameSession"/> or <see cref="Game.InitializeForTest"/> advances one fixed step
    /// per frame with no option. The run's <see cref="RunOptions.TimeMode"/> and the game's <see cref="Game.Configure"/>
    /// win over it; <c>Preset = new RunPreset()</c> when creating the platform makes runs that choose nothing real-time.
    /// The platform behaves the same either way.
    /// </summary>
    public RunPreset Preset
    {
        get => _preset;
        init => _capabilities.Replace(_preset = value ?? throw new ArgumentNullException(nameof(value)));
    }
    private RunPreset _preset = RunPreset.Testing;

    /// <summary>A <see cref="HeadlessGraphicsDevice"/>: textures have a size but no pixels, unless <see cref="Rasterize"/> is on.</summary>
    public IGraphicsDevice Graphics => _graphics;
    /// <summary>The silent audio device, typed so tests can read <see cref="SilentAudioDevice.Played"/>.</summary>
    public SilentAudioDevice SilentAudio { get; } = new();
    /// <summary>The device the request supplied (<see cref="PlatformRequest.Audio"/>), otherwise <see cref="SilentAudio"/>.</summary>
    public IAudioDevice Audio => _suppliedAudio ?? SilentAudio;
    /// <summary>The input tests set by hand (keys, gamepad, pointer), read every update as one polled device.</summary>
    public HeadlessInput Keys { get; } = new();
    /// <summary>
    /// The platform's input backend (also its <see cref="IInputBackend"/>, <see cref="IKeyLabels"/>,
    /// <see cref="IGamepadRumble"/> and <see cref="ITextInputControl"/> capabilities): tests send device events with it,
    /// which the next frame routes like a real backend's.
    /// </summary>
    public HeadlessInputBackend Devices { get; } = new();
    /// <summary><see cref="Keys"/>, read together with the request's extra sources (<see cref="PlatformRequest.ExtraInput"/>) when there are any.</summary>
    public IInputSource Input => _input;
    /// <summary>A touch screen tests can touch; by default 4× the canvas, not touch-first, no safe-area insets. Also the host's <see cref="ITouchScreen"/> capability.</summary>
    public HeadlessTouchScreen TouchScreen { get; }
    /// <summary>The same screen as <see cref="TouchScreen"/>.</summary>
    public ITouchScreen Touch => TouchScreen;
    /// <summary>
    /// Set it to true to ask the game to quit at the next event pump, as if the window's close button was pressed; the
    /// request is delivered once while it stays true. Setting it back to false allows a later request.
    /// </summary>
    public bool CloseRequested
    {
        get => _quitAsked;
        set
        {
            if (value && !_quitAsked) _pending.Enqueue(sink => sink.QuitRequested());
            _quitAsked = value;
        }
    }
    /// <summary>
    /// A monotonic clock in seconds since the platform was created. The loop owns time: in deterministic time it never
    /// reads this clock to decide how many steps a frame runs.
    /// </summary>
    public IMonotonicClock Clock { get; } = new StopwatchClock();
    /// <summary>The surface, of the canvas's size until <see cref="Resize"/> changes it; it has no native window.</summary>
    public ISurface MainSurface => _surface;
    ISurface? IPlatformHost.MainSurface => _surface;
    /// <summary>True: tests and CI may block.</summary>
    public bool CanBlock => true;
    /// <summary>Whether <see cref="Suspend"/> sent the platform to the background (until <see cref="Resume"/>).</summary>
    public bool Suspended => _suspended;
    /// <summary>The renderer, typed so tests can read its counters.</summary>
    public HeadlessRenderer Renderer => _renderer;
    /// <summary>
    /// The UI view's renderer of the current frame (<see cref="IScreenView"/>): the surface's size in UI units (pixels
    /// divided by the UI scale, <see cref="Graphics.Presentation.UiScaleFor"/> of <see cref="PixelDensity"/>). It counts
    /// draw calls; with <see cref="Rasterize"/> on it also draws them, and a frame that drew a UI is captured with it
    /// composited over the world (<see cref="IViewCapture"/>), each UI unit repeated over the window pixels it covers.
    /// </summary>
    public HeadlessRenderer UiRenderer => _ui ??= NewUiRenderer();
    private HeadlessRenderer? _ui;
    private bool _frameHasUi;

    private (int Width, int Height) UiUnits
    {
        get
        {
            float scale = ((IScreenView)this).UiScale;
            return (Math.Max(1, (int)MathF.Floor(_surfaceSize.Width / scale + 1e-4f)), Math.Max(1, (int)MathF.Floor(_surfaceSize.Height / scale + 1e-4f)));
        }
    }

    private HeadlessRenderer NewUiRenderer()
    {
        var (w, h) = UiUnits;
        return Rasterize ? new HeadlessRenderer(w, h, _graphics) : new HeadlessRenderer(w, h);
    }

    /// <summary>
    /// Pixels per logical unit of the surface (<see cref="ISurface.PixelDensity"/>): 1 by default, as a headless run has no
    /// screen. Set it when creating the platform to stand for a high-density screen: with the presentation's
    /// <see cref="Graphics.Presentation.UiScale"/> unset, the UI scale follows it.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a positive finite number.</exception>
    public float PixelDensity
    {
        get => _density;
        init => _density = float.IsFinite(value) && value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "The pixel density must be a positive number.");
    }
    private float _density = 1f;

    (int Width, int Height) IScreenView.ScreenPixels => _surfaceSize;
    (int Width, int Height) IScreenView.CanvasSize => (_width, _height);
    RectF IScreenView.WorldFrame => Presentation.Fit(new System.Numerics.Vector2(_surfaceSize.Width, _surfaceSize.Height), default, _width, _height);

    /// <summary>
    /// How the canvas is placed in the surface for the UI view's mapping (<see cref="IScreenView.WorldFrame"/>): the
    /// game's presentation when the platform was made for its settings, else <see cref="Graphics.Presentation.PixelArt"/>.
    /// </summary>
    public Presentation Presentation { get; init; } = Presentation.PixelArt;
    float IScreenView.UiScale => Presentation.UiScaleFor(_density);
    IRenderer IScreenView.BeginUi()
    {
        var (w, h) = UiUnits;
        if (_ui is null || _ui.Width != w || _ui.Height != h) _ui = NewUiRenderer();
        _ui.Reset();
        _ui.Raster?.Clear(Color.Transparent);
        _frameHasUi = true;
        return _ui;
    }
    /// <summary>Paths of the screenshots requested, in order; a file is written only when <see cref="Rasterize"/> is on.</summary>
    public List<string> Screenshots { get; } = [];
    /// <summary>The colour around the world in a capture of a frame that drew a UI (<see cref="IViewCapture.CaptureScreen"/>); there is no window to paint.</summary>
    public Color Letterbox { get; set; }

    /// <summary>A headless platform with the virtual size of <paramref name="settings"/>.</summary>
    public static HeadlessPlatform For(GameSettings settings) => new(settings.VirtualWidth, settings.VirtualHeight) { Presentation = settings.Presentation };

    /// <summary>A headless platform with the virtual size of <paramref name="settings"/>, rasterizing when <paramref name="rasterize"/> is true (see <see cref="Rasterize"/>).</summary>
    public static HeadlessPlatform For(GameSettings settings, bool rasterize) =>
        new(settings.VirtualWidth, settings.VirtualHeight) { Rasterize = rasterize, Presentation = settings.Presentation };

    /// <summary>
    /// A headless platform for <paramref name="request"/>: the virtual size of its settings, the audio device it supplies
    /// (<see cref="PlatformRequest.Audio"/>) and its extra input sources read with <see cref="Keys"/>.
    /// </summary>
    public static HeadlessPlatform For(PlatformRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(request.Settings.VirtualWidth, request.Settings.VirtualHeight, request) { Presentation = request.Settings.Presentation };
    }

    /// <summary>The headless platform as a catalog entry (<c>--platform headless</c>, <c>--headless</c>).</summary>
    public static PlatformProfile Profile { get; } = new("headless", "no window, devices or sound; one step per frame, for tests and CI", For);

    /// <summary>Changes the surface's size; the next pump delivers the resize (once, when the size changed).</summary>
    public void Resize(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);
        _pending.Enqueue(sink =>
        {
            _surfaceSize = (width, height);
            _surface.Pump(sink);
        });
    }

    /// <summary>Gives (true) or takes (false) the input focus; the next pump delivers it when the focus changes.</summary>
    public void Focus(bool focused) => _pending.Enqueue(sink =>
    {
        if (_focused == focused) return;
        _focused = focused;
        sink.FocusChanged(focused);
    });

    /// <summary>Sends the platform to the background, as a phone does; the next pump delivers <c>Suspending</c>.</summary>
    public void Suspend() => _pending.Enqueue(sink =>
    {
        if (_suspended) return;
        _suspended = true;
        sink.Suspending();
    });

    /// <summary>Brings the platform back; the next pump delivers <c>Resumed</c> after a <c>Suspending</c>.</summary>
    public void Resume() => _pending.Enqueue(sink =>
    {
        if (!_suspended) return;
        _suspended = false;
        sink.Resumed();
    });

    /// <summary>Asks the game to quit, as a window's close button does; the next pump delivers the request.</summary>
    public void RequestQuit() => _pending.Enqueue(sink => sink.QuitRequested());

    /// <summary>Delivers the events the test hooks queued, in order; false while suspended.</summary>
    public bool PumpEvents(IEngineEventSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        while (_pending.TryDequeue(out var deliver)) deliver(sink);
        return !_suspended;
    }

    /// <inheritdoc/>
    public bool TryGetCapability<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? capability) where T : class => _capabilities.TryGet(out capability);

    /// <inheritdoc/>
    public IRenderer BeginFrame()
    {
        _frameHasUi = false;
        _renderer.Reset();
        return _renderer;
    }
    /// <inheritdoc/>
    public void EndFrame() { }
    /// <summary>
    /// Records <paramref name="path"/> in <see cref="Screenshots"/>; with <see cref="Rasterize"/> on, also writes what the
    /// player would see there as an opaque PNG at scale 1 (<see cref="IViewCapture.CaptureScreen"/>): the canvas, or, for
    /// a frame that drew a UI, the surface with the UI over the world.
    /// </summary>
    public void SaveScreenshot(string path)
    {
        Screenshots.Add(path);
        if (_renderer.Rasterizes) Write(path, ((IViewCapture)this).CaptureScreen());
    }

    private static void Write(string path, PixelImage image) => File.WriteAllBytes(path, PngEncoder.Encode(image.ToRgba(), image.Width, image.Height));

    bool IViewCapture.FrameHasUi => _frameHasUi;

    /// <exception cref="InvalidOperationException">The platform does not rasterize (<see cref="Rasterize"/>).</exception>
    PixelImage IViewCapture.CaptureWorld() => _renderer.Frame ?? throw new InvalidOperationException("Captures need a rasterizing platform (Rasterize = true).");

    PixelImage? IViewCapture.CaptureUi()
    {
        if (!_frameHasUi || _ui?.Raster is not { } raster) return null;
        // As stored (premultiplied, transparent where nothing was drawn), at the window's pixels: each UI unit covers the
        // window pixels its scale gives it.
        var (w, h) = _surfaceSize;
        var image = new PixelImage(Math.Max(1, w), Math.Max(1, h));
        float scale = ((IScreenView)this).UiScale;
        for (int y = 0; y < image.Height; y++)
            for (int x = 0; x < image.Width; x++)
            {
                int ux = (int)(x / scale), uy = (int)(y / scale);
                if (ux < raster.Width && uy < raster.Height) image[x, y] = raster[ux, uy];
            }
        return image;
    }

    PixelImage IViewCapture.CaptureScreen()
    {
        var capture = (IViewCapture)this;
        var world = capture.CaptureWorld();
        if (capture.CaptureUi() is not { } ui) return world;
        var view = (IScreenView)this;
        return ScreenCapture.Compose(world, ui, view.ScreenPixels, view.WorldFrame, Letterbox, Presentation.Upscale);
    }

    void IViewCapture.SaveWorldScreenshot(string path) => Write(path, ((IViewCapture)this).CaptureWorld());
    /// <summary>Counts the request in <see cref="FullscreenToggles"/>: there is no window to switch.</summary>
    public void ToggleFullscreen() => FullscreenToggles++;
    /// <summary>How many times the game asked to switch between a window and full screen (there is no window to switch).</summary>
    public int FullscreenToggles { get; private set; }
    /// <summary>Disposes the audio device the request supplied, if any; nothing else holds resources.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        (_suppliedAudio as IDisposable)?.Dispose();
    }

    private sealed class StopwatchClock : IMonotonicClock
    {
        private readonly System.Diagnostics.Stopwatch _watch = System.Diagnostics.Stopwatch.StartNew();
        public double Now => _watch.Elapsed.TotalSeconds;
    }
}

/// <summary>
/// An input backend tests drive by hand: device events sent with <see cref="Send"/> are delivered at the next drain, in
/// order; key labels can be set as a keyboard layout would print them; rumbles and text-input switches are recorded.
/// </summary>
public sealed class HeadlessInputBackend : IInputBackend, IKeyLabels, IGamepadRumble, ITextInputControl
{
    private readonly List<InputEvent> _pending = [];

    /// <summary>The labels <see cref="KeyLabel"/> reports, as a layout would print them; keys not in it report null.</summary>
    public Dictionary<Key, string> Labels { get; } = [];
    /// <summary>Every <see cref="Rumble"/> call, in order.</summary>
    public List<(DeviceId Pad, float Low, float High, double Seconds)> Rumbles { get; } = [];
    /// <summary>Whether text input is on, and the caret it was turned on with.</summary>
    public (bool Active, RectF Caret) TextInput { get; private set; }

    /// <summary>Queues an event for the next drain.</summary>
    public HeadlessInputBackend Send(InputEvent e)
    {
        _pending.Add(e);
        return this;
    }

    /// <inheritdoc/>
    public void Drain(List<InputEvent> events)
    {
        events.AddRange(_pending);
        _pending.Clear();
    }

    /// <inheritdoc/>
    public string? KeyLabel(Key key) => Labels.TryGetValue(key, out var label) ? label : null;

    /// <summary>Records the call; every pad can rumble here.</summary>
    public bool Rumble(DeviceId pad, float low, float high, double seconds)
    {
        Rumbles.Add((pad, low, high, seconds));
        return true;
    }

    /// <inheritdoc/>
    public void Start(RectF caret) => TextInput = (true, caret);
    /// <inheritdoc/>
    public void Stop() => TextInput = (false, default);
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

/// <summary>
/// A graphics device for tests: textures keep their size and filter but hold no pixels (unless <see cref="KeepsPixels"/>);
/// creations, uploads and live textures are counted.
/// </summary>
public sealed class HeadlessGraphicsDevice : IGraphicsDevice
{
    /// <summary>
    /// Whether textures keep a copy of their pixels (creations and updates), which a rasterizing
    /// <see cref="HeadlessRenderer"/> needs to draw them. False by default.
    /// </summary>
    public bool KeepsPixels { get; init; }
    /// <summary>Number of textures created so far.</summary>
    public int TexturesCreated { get; private set; }
    /// <summary>Number of textures created and not disposed yet.</summary>
    public int LiveTextures { get; private set; }
    /// <summary>Number of region updates (<see cref="Texture.Update(int, int, int, int, ReadOnlySpan{Color})"/>) so far.</summary>
    public int Updates { get; private set; }
    /// <summary>Pixels sent by creations and updates so far.</summary>
    public long PixelsUploaded { get; private set; }
    /// <inheritdoc/>
    public TextureFilter DefaultFilter { get; set; }
    /// <inheritdoc/>
    public Texture CreateTexture(PixelImage image) => CreateTexture(image, DefaultFilter);
    /// <inheritdoc/>
    public Texture CreateTexture(PixelImage image, TextureFilter filter)
    {
        ArgumentNullException.ThrowIfNull(image);
        TexturesCreated++;
        LiveTextures++;
        PixelsUploaded += (long)image.Width * image.Height;
        return new HeadlessTexture(this, TexturesCreated, image.Width, image.Height, filter) { Pixels = KeepsPixels ? image.Clone() : null };
    }

    internal sealed class HeadlessTexture(HeadlessGraphicsDevice device, int id, int width, int height, TextureFilter filter) : Texture(device, width, height, filter)
    {
        /// <summary>The creation number on its device, used to name the texture in recorded commands.</summary>
        public int Id { get; } = id;
        /// <summary>A copy of the pixels when the device keeps them.</summary>
        public PixelImage? Pixels { get; init; }

        protected override void Upload(int x, int y, int width, int height, ReadOnlySpan<Color> pixels)
        {
            device.Updates++;
            device.PixelsUploaded += pixels.Length;
            if (Pixels is null) return;
            for (int j = 0; j < height; j++)
                for (int i = 0; i < width; i++) Pixels[x + i, y + j] = pixels[j * width + i];
        }
        protected override void ApplyFilter(TextureFilter filter) { }
        protected override void Release() => device.LiveTextures--;
    }
}

/// <summary>
/// Counts draw calls and tracks camera balance so tests can assert rendering ran correctly. Created with a graphics
/// device, it also rasterizes: it records each draw as a line of text (<see cref="Commands"/>) and draws it on
/// <see cref="Frame"/> with the CPU reference rasterizer, which follows the engine's rendering rules exactly.
/// </summary>
public sealed class HeadlessRenderer : IRenderer
{
    private readonly HeadlessGraphicsDevice? _device;
    private readonly ReferenceRasterizer? _raster;
    private readonly List<string> _commands = [];
    private readonly Stack<Vector2> _offsets = new();
    private Vector2 _offset;
    private int _cameraDepth;

    /// <summary>A counting renderer: nothing is drawn, every texture of any headless device is accepted.</summary>
    public HeadlessRenderer(int width, int height) => (Width, Height) = (width, height);

    /// <summary>
    /// A rasterizing renderer for <paramref name="device"/>: it draws only that device's textures, which must keep
    /// their pixels (<see cref="HeadlessGraphicsDevice.KeepsPixels"/>).
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="device"/> does not keep pixels.</exception>
    public HeadlessRenderer(int width, int height, HeadlessGraphicsDevice device) : this(width, height)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (!device.KeepsPixels) throw new ArgumentException("A rasterizing renderer needs a device that keeps pixels (KeepsPixels = true).", nameof(device));
        _device = device;
        _raster = new ReferenceRasterizer(width, height);
    }

    /// <inheritdoc/>
    public int Width { get; }
    /// <inheritdoc/>
    public int Height { get; }
    /// <summary>Draw calls since the frame began (cleared by <see cref="Reset"/>).</summary>
    public int DrawCalls { get; private set; }
    /// <summary>Cameras pushed and not yet popped.</summary>
    public int CameraDepth => _cameraDepth;
    /// <summary>Whether this renderer rasterizes (it was created with a graphics device).</summary>
    public bool Rasterizes => _raster is not null;
    /// <summary>The rasterizer this renderer draws on, or null when it only counts.</summary>
    internal ReferenceRasterizer? Raster => _raster;

    /// <summary>
    /// This frame's draws as text, one line per call, in screen pixels after the camera offset (a backend-neutral form
    /// tests can compare exactly): <c>clear</c>, <c>rect</c>, <c>sprite</c>, <c>circle</c>, <c>triangle</c>,
    /// <c>line</c>, <c>blend</c>, <c>clip</c>, <c>camera</c>. Empty when the renderer does not rasterize.
    /// </summary>
    public IReadOnlyList<string> Commands => _commands;

    /// <summary>
    /// A copy of the canvas as a capture stores it (opaque), or null when the renderer does not rasterize. The canvas
    /// keeps its pixels between frames, as a backend's does.
    /// </summary>
    public PixelImage? Frame => _raster?.Capture();

    /// <summary>Starts a new frame: clears <see cref="DrawCalls"/> and <see cref="Commands"/>, blending returns to <see cref="BlendMode.Alpha"/> and the clip is removed.</summary>
    /// <exception cref="InvalidOperationException">The previous frame pushed a camera it did not pop.</exception>
    public void Reset()
    {
        if (_cameraDepth != 0) throw new InvalidOperationException("Unbalanced PushCamera/PopCamera in the previous frame.");
        DrawCalls = 0;
        _commands.Clear();
        _offsets.Clear();
        _offset = Vector2.Zero;
        if (_raster is null) return;
        _raster.Blend = BlendMode.Alpha;
        _raster.SetClip(null);
    }

    private static string N(float v) => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    private static string P(Vector2 v) => $"{N(v.X)},{N(v.Y)}";
    private static string R(RectF r) => $"{N(r.X)},{N(r.Y)} {N(r.Width)}x{N(r.Height)}";
    private static string C(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}";

    private void Record(string command)
    {
        DrawCalls++;
        _commands.Add(command);
    }

    /// <summary>Counts a draw and returns true when this renderer only counts, so no command text is formatted.</summary>
    private bool Counted()
    {
        if (_raster is not null) return false;
        DrawCalls++;
        return true;
    }

    /// <inheritdoc/>
    public void Clear(Color color)
    {
        if (Counted()) return;
        Record($"clear {C(color)}");
        _raster!.Clear(color);
    }

    /// <summary>Counts the call (and rasterizes it); throws <see cref="ObjectDisposedException"/> for a disposed texture and, when rasterizing, <see cref="ArgumentException"/> for another device's texture.</summary>
    public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default)
    {
        var own = Check(texture);
        var scale = options.Scale ?? Vector2.One;
        var p = position + _offset;
        if (own is null) { DrawCalls++; return; }
        string extra = (options.FlipX ? " flipx" : "") + (options.FlipY ? " flipy" : "")
            + (options.Rotation != 0 ? $" rotate {N(options.Rotation)} origin {P(options.Origin)}" : "");
        Record($"sprite #{own.Id} {own.Width}x{own.Height} {own.Filter.ToString().ToLowerInvariant()} src {R(source)} dst {R(new RectF(p.X, p.Y, source.Width * scale.X, source.Height * scale.Y))} tint {C(tint)}{extra}");
        _raster!.DrawSprite(own.Pixels!, own.Filter, source, p, scale, options.Rotation, options.Origin, options.FlipX, options.FlipY, tint);
    }

    /// <summary>Counts the call (and rasterizes it), with the checks of <see cref="Draw"/>.</summary>
    public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint)
    {
        var own = Check(texture);
        if (own is null) { DrawCalls++; return; }
        var dst = new RectF(destination.X + _offset.X, destination.Y + _offset.Y, destination.Width, destination.Height);
        Record($"sprite #{own.Id} {own.Width}x{own.Height} {own.Filter.ToString().ToLowerInvariant()} src {R(source)} dst {R(dst)} tint {C(tint)}");
        if (source.Width == 0 || source.Height == 0) return;
        _raster!.DrawSprite(own.Pixels!, own.Filter, source, dst.Position, new Vector2(dst.Width / source.Width, dst.Height / source.Height), 0, Vector2.Zero, false, false, tint);
    }

    /// <summary>The texture as this device's own when rasterizing (null when only counting), after the checks every renderer makes.</summary>
    private HeadlessGraphicsDevice.HeadlessTexture? Check(Texture texture)
    {
        if (_device is null)
        {
            ArgumentNullException.ThrowIfNull(texture);
            ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
            return null;
        }
        return Texture.CheckDrawable<HeadlessGraphicsDevice.HeadlessTexture>(texture, _device);
    }

    /// <inheritdoc/>
    public void FillRect(RectF rect, Color color)
    {
        if (Counted()) return;
        var r = new RectF(rect.X + _offset.X, rect.Y + _offset.Y, rect.Width, rect.Height);
        Record($"rect {R(r)} {C(color)}");
        _raster!.FillRect(r, color);
    }

    /// <inheritdoc/>
    public void FillCircle(Vector2 center, float radius, Color color)
    {
        if (Counted()) return;
        Record($"circle {P(center + _offset)} r {N(radius)} {C(color)}");
        _raster!.FillCircle(center + _offset, radius, color);
    }

    /// <inheritdoc/>
    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        if (Counted()) return;
        Record($"triangle {P(a + _offset)} {P(b + _offset)} {P(c + _offset)} {C(color)}");
        _raster!.FillTriangle(a + _offset, b + _offset, c + _offset, color);
    }

    /// <inheritdoc/>
    public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color)
    {
        if (Counted()) return;
        Record($"line {P(from + _offset)} {P(to + _offset)} w {N(thickness)} {C(color)}");
        _raster!.DrawLine(from + _offset, to + _offset, thickness, color);
    }

    /// <inheritdoc/>
    public void PushCamera(Camera2D camera)
    {
        _cameraDepth++;
        _offsets.Push(_offset);
        _offset = camera.RenderOffset;
        if (_raster is not null) _commands.Add($"camera {P(_offset)}");
    }

    /// <inheritdoc/>
    public void PopCamera()
    {
        if (_cameraDepth == 0) throw new InvalidOperationException("PopCamera without PushCamera.");
        _cameraDepth--;
        _offset = _offsets.Pop();
        if (_raster is not null) _commands.Add($"camera {P(_offset)}");
    }

    /// <inheritdoc/>
    public void SetBlendMode(BlendMode mode)
    {
        if (_raster is null) return;
        _raster.Blend = mode;
        _commands.Add($"blend {mode.ToString().ToLowerInvariant()}");
    }

    /// <inheritdoc/>
    public void SetClip(RectF? screenRect)
    {
        if (_raster is null) return;
        _raster.SetClip(screenRect);
        _commands.Add(screenRect is { } r ? $"clip {R(r)}" : "clip none");
    }
}

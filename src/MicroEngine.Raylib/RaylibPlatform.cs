using System.Numerics;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Platform;
using Rl = Raylib_cs.Raylib;
using RlColor = Raylib_cs.Color;
using RlRect = Raylib_cs.Rectangle;

namespace MicroEngine.Raylib;

/// <summary>
/// Window options of a raylib platform created directly (browser hosts); hosts with a <see cref="PlatformRequest"/> use
/// <see cref="RaylibPlatform(PlatformRequest)"/>. Every option is set by name
/// (<c>new DesktopOptions { Hidden = true, Mute = true }</c>): there is no positional constructor, so a call written for
/// the earlier positional shape (whose third argument was a fixed-step flag) fails to compile instead of meaning
/// something else.
/// </summary>
public sealed record DesktopOptions
{
    /// <summary>Window scale; null (the default) uses <see cref="GameSettings.Scale"/>.</summary>
    public int? Scale { get; init; }
    /// <summary>Renders into a hidden window, which never waits for the display (false by default).</summary>
    public bool Hidden { get; init; }
    /// <summary>Opens no audio device (false by default).</summary>
    public bool Mute { get; init; }
    /// <summary>Size factor of screenshots (3 by default).</summary>
    public int CaptureScale { get; init; } = 3;
    /// <summary>The window's icon (the game's <see cref="AppIdentity.Icon"/>); ignored in browsers.</summary>
    public PixelImage? Icon { get; init; }
    /// <summary>Whether a visible desktop window's frames may wait for the display (true by default; see <see cref="PlatformRequest.WaitForDisplay"/>).</summary>
    public bool WaitForDisplay { get; init; } = true;
}

/// <summary>
/// raylib-backed platform. Games draw into a virtual-resolution render texture which is
/// scaled to the window by the largest integer factor that fits (letterboxed), keeping pixel art crisp. A desktop
/// platform (the second implementation of the platform contract beside SDL); browsers run the SDL platform. Its
/// capabilities (<see cref="TryGetCapability{T}"/>): <see cref="IDisplayControl"/>, <see cref="IDisplayPacing"/>,
/// <see cref="ITouchScreen"/> and <see cref="IPreciseDelay"/>.
/// </summary>
public sealed class RaylibPlatform : IPlatform, IDisplayControl, IDisplayPacing, IPreciseDelay, IMonotonicClock, IScreenView, IViewCapture
{
    private readonly GameSettings _settings;
    // A visible desktop window: the only kind whose window and monitor the platform changes. Browsers and Android leave
    // those to the page and the system.
    private readonly bool _desktop;
    // A visible desktop window whose request lets it wait for the display: the only kind whose VSync the platform sets.
    private readonly bool _paced;
    private readonly HostSurface _surface;
    private readonly Kernel.CapabilityRegistry _capabilities = new();
    private readonly IInputSource _combinedInput;
    private bool _suspended, _closeSent, _focused;
    private double _displayRate;
    private bool _vsync;
    private int _monitor = -1;
    private readonly int _captureScale;
    private readonly RaylibDevice _device;
    private readonly DeviceCanvas _canvas;
    private readonly RaylibInput _input;
    private readonly IAudioDevice _audio;
    private readonly RaylibTouchScreen _touch;
    private long _frames;
    private bool _disposed;

    /// <summary>
    /// The raylib platform as a catalog entry. raylib has no iOS platform; its own audio device is not opened when the
    /// request supplies another one.
    /// </summary>
    public static PlatformProfile Profile { get; } = new("raylib", "raylib 6.0: GLFW window, OpenGL renderer, input and miniaudio sound",
        request => new RaylibPlatform(request))
    {
        CheckSupport = () => OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() ? "raylib 6.0 has no iOS platform"
            : OperatingSystem.IsBrowser() ? "raylib is not built for browsers; the browser host runs SDL (SdlPlatform.BrowserProfile)" : null,
    };

    /// <summary>Opens the raylib window (and audio device unless the options say otherwise) for <paramref name="settings"/>. Hosts usually go through <see cref="Profile"/> instead.</summary>
    public RaylibPlatform(GameSettings settings, DesktopOptions options) : this(settings, options, null) { }

    /// <summary>
    /// Opens the raylib window for <paramref name="request"/>: its window options, its audio device or raylib's own
    /// (unless muted or silent), and its extra input sources read with raylib's.
    /// </summary>
    public RaylibPlatform(PlatformRequest request) : this(
        (request ?? throw new ArgumentNullException(nameof(request))).Settings,
        new DesktopOptions { Scale = request.Scale, Hidden = request.Hidden, Mute = !request.OpensOwnAudio, CaptureScale = request.CaptureScale, Icon = request.Identity?.Icon, WaitForDisplay = request.WaitForDisplay },
        request)
    { }

    private RaylibPlatform(GameSettings settings, DesktopOptions options, PlatformRequest? request)
    {
        _touch = new RaylibTouchScreen();
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Letterbox = _settings.Letterbox;
        _input = new RaylibInput { CanvasWidth = settings.VirtualWidth, CanvasHeight = settings.VirtualHeight };
        _combinedInput = request?.CombineInput(_input) ?? _input;
        ArgumentNullException.ThrowIfNull(options);
        _captureScale = Math.Max(1, options.CaptureScale);
        var graphics = settings.Graphics;
        _desktop = !options.Hidden && !OperatingSystem.IsBrowser() && !OperatingSystem.IsAndroid();
        _paced = _desktop && options.WaitForDisplay;
        var flags = Raylib_cs.ConfigFlags.ResizableWindow;
        // A hidden window is never looked at: waiting for the display there only slows scripted checks down.
        if (options.Hidden) flags |= Raylib_cs.ConfigFlags.HiddenWindow;
        else if (graphics.VSync && options.WaitForDisplay) flags |= Raylib_cs.ConfigFlags.VSyncHint;
        Rl.SetTraceLogLevel(Raylib_cs.TraceLogLevel.Warning);
        Rl.SetConfigFlags(flags);
        int scale = options.Scale ?? settings.Scale;
        Rl.InitWindow(settings.VirtualWidth * scale, settings.VirtualHeight * scale, settings.Title);
        // Without a window raylib keeps going, and the next window call aborts the process from inside GLFW.
        if (!Rl.IsWindowReady())
            throw new InvalidOperationException("raylib could not open a window: no display, or no usable OpenGL (raylib's warnings above give the reason).");
        // A page may be smaller than the virtual canvas (a small iframe); the canvas then scales down instead of being cropped.
        if (!OperatingSystem.IsBrowser() && !OperatingSystem.IsAndroid())
        {
            var (minWidth, minHeight) = WindowSizing.Minimum(settings);
            Rl.SetWindowMinSize(minWidth, minHeight);
            // GLFW knows the monitors only once the window exists: a window larger than its display shrinks to fit it.
            int monitor = Rl.GetCurrentMonitor();
            var (width, height) = WindowSizing.Initial(settings, scale, Rl.GetMonitorWidth(monitor), Rl.GetMonitorHeight(monitor));
            if (width != settings.VirtualWidth * scale || height != settings.VirtualHeight * scale) Rl.SetWindowSize(width, height);
        }
        Rl.SetExitKey(Raylib_cs.KeyboardKey.Null);
        if (options.Icon is { } icon && !OperatingSystem.IsBrowser()) SetWindowIcon(icon);
        // On Android raylib's GetCurrentMonitor detaches the calling thread from the JVM, which aborts a thread that is
        // running .NET code (the experimental host, platforms/android); the display's vsync paces frames there instead.
        if (!options.Hidden && !OperatingSystem.IsBrowser() && !OperatingSystem.IsAndroid())
        {
            if (_desktop && graphics.Display is { } display) MoveToDisplay(display);
            if (_desktop && graphics.WindowMode != WindowMode.Windowed) SetWindowMode(graphics.WindowMode);
            _vsync = graphics.VSync && options.WaitForDisplay;
            Pace();
        }
        _audio = request?.CreateAudio(() => RaylibAudioDevice.TryCreate(request.Log)) ?? (options.Mute ? new SilentAudioDevice() : RaylibAudioDevice.TryCreate());
        _surface = new HostSurface(GetWindowHandle(), () => (Rl.GetRenderWidth(), Rl.GetRenderHeight()),
            () => Rl.GetWindowScaleDPI().X is > 0 and var d ? d : 1, () => !OperatingSystem.IsBrowser() && Rl.IsWindowMinimized());
        // The graphics device on raylib's window; the game draws into a canvas render target through the 2D batcher and
        // EndFrame composes it into the window. raylib's EndDrawing swaps the buffers, so the device does not.
        var present = _paced && graphics.VSync ? MicroEngine.Graphics.Devices.PresentMode.VSync : MicroEngine.Graphics.Devices.PresentMode.Immediate;
        _device = new RaylibDevice(_surface, new MicroEngine.Graphics.Devices.GraphicsDeviceOptions(present)) { SwapOnPresent = false };
        _canvas = new DeviceCanvas(_device, settings.VirtualWidth, settings.VirtualHeight);
        _canvas.Graphics.DefaultFilter = settings.DefaultTextureFilter;
        _focused = Rl.IsWindowFocused();
        _capabilities.Add<IDisplayControl>(this);
        _capabilities.Add<IDisplayPacing>(this);
        _capabilities.Add<IScreenView>(this);
        _capabilities.Add<IViewCapture>(this);
        _capabilities.Add<ITouchScreen>(_touch);
        _capabilities.Add<IInputBackend>(_input);
        _capabilities.Add<IGamepadRumble>(_input);
        _capabilities.Add<ITextInputControl>(_input);
        _capabilities.Add<IClipboard>(new RaylibClipboard());
        if (CanBlock) _capabilities.Add<IPreciseDelay>(this);
    }

    private static unsafe nint GetWindowHandle() => (nint)Rl.GetWindowHandle();

    /// <inheritdoc/>
    public IGraphicsDevice Graphics => _canvas.Graphics;
    /// <summary>The graphics device the platform draws with (raylib's OpenGL layer behind the device contract).</summary>
    public RaylibDevice Device => _device;
    /// <summary>The canvas the game draws into, with the 2D batcher's counts for the last frame.</summary>
    public DeviceCanvas Canvas => _canvas;
    /// <inheritdoc/>
    public IAudioDevice Audio => _audio;
    /// <summary>
    /// raylib's keyboard, gamepads and mouse as a polled source, read together with the request's extra sources. The
    /// engine reads raylib's input as events instead (the <see cref="IInputBackend"/> capability).
    /// </summary>
    public IInputSource Input => _combinedInput;
    /// <summary>raylib's timer (the platform is its own clock).</summary>
    public IMonotonicClock Clock => this;
    /// <inheritdoc/>
    public double Now => Rl.GetTime();
    /// <summary>The window, or the page's canvas in a browser: its size is the canvas's as it is now, never the size requested at creation.</summary>
    public ISurface MainSurface => _surface;
    ISurface? IPlatformHost.MainSurface => _surface;
    /// <summary>False in a browser, whose page drives the frames; true on desktop.</summary>
    public bool CanBlock => !OperatingSystem.IsBrowser();
    /// <inheritdoc/>
    public bool TryGetCapability<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? capability) where T : class => _capabilities.TryGet(out capability);
    /// <summary>The monitor's refresh rate when frames are paced to it (a visible real-time desktop window with VSync); 0 otherwise.</summary>
    public double DisplayRate => _displayRate;

    /// <summary>
    /// With VSync, raylib's own limiter paces frames to the monitor's rate (the steadiest wake-ups measured); without
    /// it, frames run free and the engine's frame limit, if any, paces them.
    /// </summary>
    private void Pace()
    {
        _monitor = Rl.GetCurrentMonitor();
        int refresh = Rl.GetMonitorRefreshRate(_monitor);
        Rl.SetTargetFPS(_vsync ? Math.Max(60, refresh) : 0);
        _displayRate = _vsync && refresh >= 60 ? refresh : 0;
    }
    /// <summary>The touch screen (also the <see cref="ITouchScreen"/> capability).</summary>
    public ITouchScreen Touch => _touch;
    /// <summary>False: a desktop window is never suspended.</summary>
    public bool Suspended => false;

    /// <summary>
    /// Reads what raylib collected at the end of the last frame and delivers the engine's events: a hidden page's
    /// suspension (browsers), the window's close request (desktop, once while it stays asked), focus changes and size
    /// changes of the window or canvas.
    /// </summary>
    public bool PumpEvents(IEngineEventSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        _frames++;
        _input.Frame = FrameRect();
        _input.Collect(Now);
        if (Suspended != _suspended)
        {
            _suspended = !_suspended;
            if (_suspended) sink.Suspending();
            else sink.Resumed();
        }
        if (!OperatingSystem.IsBrowser())
        {
            // The page never asks to close (closing the tab ends everything); GLFW keeps the request until it is read.
            bool close = Rl.WindowShouldClose();
            if (close && !_closeSent) sink.QuitRequested();
            _closeSent = close;
        }
        bool focused = Rl.IsWindowFocused();
        // Keys held while another window takes over would never be released to the game.
        if (!focused && _focused) _input.ReleaseAll(Now);
        if (focused != _focused) sink.FocusChanged(_focused = focused);
        _surface.Pump(sink);
        if (OperatingSystem.IsBrowser()) return !_suspended;
        // A window moved to another monitor is paced to that monitor's rate (checked twice a second: the query walks the monitors).
        if (_desktop && _frames % 30 == 0 && Rl.GetCurrentMonitor() != _monitor) Pace();
        return !_suspended;
    }

    /// <inheritdoc/>
    public IRenderer BeginFrame()
    {
        // An expanding presentation grows the canvas with the window.
        var size = _touch.Size;
        var area = _touch.SafeArea is { Width: > 0, Height: > 0 } safe ? safe : new RectF(0, 0, size.X, size.Y);
        var (cw, ch) = _settings.Presentation.CanvasSize(area, _settings.VirtualWidth, _settings.VirtualHeight);
        if (_canvas.EnsureSize(cw, ch)) (_input.CanvasWidth, _input.CanvasHeight) = (cw, ch);
        _frameHasUi = false;
        return _canvas.BeginFrame();
    }

    (int Width, int Height) IScreenView.ScreenPixels => (Math.Max(1, (int)_touch.Size.X), Math.Max(1, (int)_touch.Size.Y));
    (int Width, int Height) IScreenView.CanvasSize => (_canvas.Target.Width, _canvas.Target.Height);
    RectF IScreenView.WorldFrame => FrameRect();
    float IScreenView.UiScale => _settings.Presentation.UiScaleFor(_surface.PixelDensity);
    IRenderer IScreenView.BeginUi()
    {
        var (width, height) = ((IScreenView)this).ScreenPixels;
        _frameHasUi = true;
        return _canvas.BeginUi(width, height, _settings.Presentation.UiScaleFor(_surface.PixelDensity));
    }

    private bool _frameHasUi;

    /// <inheritdoc/>
    public Color Letterbox { get; set; }

    public void EndFrame()
    {
        _canvas.EndFrame();
        Rl.BeginDrawing();
        // The canvas into the window as the presentation says, prescale included (copied opaque: its alpha never darkens it).
        if (_canvas.Compose(_settings.Presentation, FrameRect(), Letterbox))
        {
            // The on-screen controls, over the letterboxed frame in render pixels (never in captures).
            _touch.DrawOverlay();
            _device.Present();
        }
        Rl.EndDrawing();
    }

    private RectF FrameRect()
    {
        var presentation = _settings.Presentation;
        return _touch.Overlay?.Frame(_touch.Size, _touch.SafeArea, _settings.VirtualWidth, _settings.VirtualHeight, presentation)
            ?? presentation.Fit(_touch.Size, _touch.SafeArea, _settings.VirtualWidth, _settings.VirtualHeight);
    }

    /// <summary>Writes the canvas as a PNG, opaque (alpha 255 everywhere) as on every backend: the frame is shown on an opaque backdrop.</summary>
    /// <summary>
    /// Writes what the player saw as a PNG: the canvas at the capture scale, or, for a frame that drew a UI, the window with
    /// the UI view over the world (<see cref="IViewCapture.CaptureScreen"/>) at the window's size.
    /// </summary>
    public void SaveScreenshot(string path)
    {
        if (!_frameHasUi) _canvas.SaveCapture(path, _captureScale);
        else
        {
            var screen = ((IViewCapture)this).CaptureScreen();
            File.WriteAllBytes(path, PngEncoder.Encode(screen.ToRgba(), screen.Width, screen.Height));
        }
    }

    bool IViewCapture.FrameHasUi => _frameHasUi;
    PixelImage IViewCapture.CaptureWorld() => _canvas.Capture();
    PixelImage? IViewCapture.CaptureUi() => _frameHasUi ? _canvas.CaptureUi() : null;
    PixelImage IViewCapture.CaptureScreen()
    {
        var world = _canvas.Capture();
        if (!_frameHasUi || _canvas.CaptureUi() is not { } ui) return world;
        return ScreenCapture.Compose(world, ui, ((IScreenView)this).ScreenPixels, FrameRect(), Letterbox, _settings.Presentation, _device.Capabilities.Limits.MaxRenderTargetSize);
    }
    void IViewCapture.SaveWorldScreenshot(string path) => _canvas.SaveCapture(path, _captureScale);

    /// <summary>Switches between the window and a borderless full-screen window (exclusive full screen goes back to the window).</summary>
    public void ToggleFullscreen() => SetWindowMode(WindowMode == WindowMode.Windowed ? WindowMode.Fullscreen : WindowMode.Windowed);

    /// <summary>Waits with raylib's timer (a sleep, then a short spin); never offered in a browser, whose page cannot be blocked.</summary>
    public void Delay(double seconds)
    {
        if (OperatingSystem.IsBrowser()) throw new InvalidOperationException("A browser page cannot be blocked: the page paces the frames.");
        if (seconds > 0) Rl.WaitTime(seconds);
    }

    /// <summary>The monitors GLFW reports (none in a browser or on Android); GLFW lists the primary one first.</summary>
    public IReadOnlyList<DisplayInfo> Displays
    {
        get
        {
            if (OperatingSystem.IsBrowser() || OperatingSystem.IsAndroid()) return [];
            var list = new List<DisplayInfo>();
            int current = Rl.GetCurrentMonitor();
            float scale = Rl.GetWindowScaleDPI().X;
            for (int i = 0, n = Rl.GetMonitorCount(); i < n; i++)
                list.Add(new DisplayInfo(i, Rl.GetMonitorName_(i), Rl.GetMonitorWidth(i), Rl.GetMonitorHeight(i),
                    Rl.GetMonitorRefreshRate(i), i == current ? scale : 1, i == 0));
            return list;
        }
    }

    /// <inheritdoc/>
    public int CurrentDisplay => OperatingSystem.IsBrowser() || OperatingSystem.IsAndroid() ? -1 : Rl.GetCurrentMonitor();

    /// <inheritdoc/>
    public bool VSync => _vsync;

    /// <summary>Turns VSync on or off on a visible desktop window that may wait for the display; browsers and Android follow the display.</summary>
    public bool SetVSync(bool on)
    {
        if (!_paced) return on == _vsync;
        if (on) Rl.SetWindowState(Raylib_cs.ConfigFlags.VSyncHint);
        else Rl.ClearWindowState(Raylib_cs.ConfigFlags.VSyncHint);
        _vsync = on;
        Pace();
        return true;
    }

    /// <inheritdoc/>
    public WindowMode WindowMode => Rl.IsWindowFullscreen() ? WindowMode.ExclusiveFullscreen
        : Rl.IsWindowState(Raylib_cs.ConfigFlags.BorderlessWindowMode) ? WindowMode.Fullscreen : WindowMode.Windowed;

    /// <summary>Changes the window's mode on desktop (raylib's full screen is exclusive, its borderless mode is <see cref="WindowMode.Fullscreen"/>).</summary>
    public bool SetWindowMode(WindowMode mode)
    {
        if (OperatingSystem.IsBrowser() || OperatingSystem.IsAndroid()) return false;
        var current = WindowMode;
        if (current == mode) return true;
        // raylib toggles: leave the current full-screen mode first.
        if (current == WindowMode.ExclusiveFullscreen) Rl.ToggleFullscreen();
        if (current == WindowMode.Fullscreen) Rl.ToggleBorderlessWindowed();
        if (mode == WindowMode.ExclusiveFullscreen) Rl.ToggleFullscreen();
        if (mode == WindowMode.Fullscreen) Rl.ToggleBorderlessWindowed();
        if (_desktop) Pace();
        return WindowMode == mode;
    }

    /// <summary>Moves the window to monitor <paramref name="index"/> (GLFW's order) on desktop.</summary>
    public bool MoveToDisplay(int index)
    {
        if (OperatingSystem.IsBrowser() || OperatingSystem.IsAndroid() || index < 0 || index >= Rl.GetMonitorCount()) return false;
        Rl.SetWindowMonitor(index);
        if (_desktop) Pace();
        return true;
    }

    private static unsafe void SetWindowIcon(PixelImage icon)
    {
        byte[] rgba = icon.ToRgba();
        fixed (byte* pixels = rgba)
        {
            // raylib copies the pixels into GLFW's icon; the image only borrows the managed buffer.
            Rl.SetWindowIcon(new Raylib_cs.Image { Data = pixels, Width = icon.Width, Height = icon.Height, Mipmaps = 1, Format = Raylib_cs.PixelFormat.UncompressedR8G8B8A8 });
        }
    }

    /// <summary>Closes the window and the audio device the platform opened.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        (_audio as IDisposable)?.Dispose();
        _canvas.Dispose();
        _device.Dispose();
        _touch.Clear();
        Rl.CloseWindow();
    }
}

/// <summary>
/// raylib's input as device events (<see cref="IInputBackend"/>). raylib polls its devices once per frame, so the
/// backend compares each frame with the one before: keys come from raylib's queue of presses (in order, so a key pressed
/// and released within one frame is a press then a release) and its key states; text from its character queue while
/// text input is on; the mouse and wheel from its state; gamepads 0 to 3 from their state, connecting and disconnecting
/// with ids of their own. Also the <see cref="IGamepadRumble"/> and <see cref="ITextInputControl"/> capabilities; raylib
/// cannot tell the keyboard layout or compose text with an input method.
/// </summary>
internal sealed class RaylibInput : BufferedInputBackend, IGamepadRumble, ITextInputControl
{
    public static readonly DeviceId Keyboard = new(1), Mouse = new(2);
    private const int Pads = 4, PadBase = 100;
    private static readonly (Key Key, Raylib_cs.KeyboardKey Raylib)[] Keys = BuildKeys();
    private static readonly (PadButton Button, Raylib_cs.GamepadButton Raylib)[] Buttons =
    [
        (PadButton.A, Raylib_cs.GamepadButton.RightFaceDown), (PadButton.B, Raylib_cs.GamepadButton.RightFaceRight),
        (PadButton.X, Raylib_cs.GamepadButton.RightFaceLeft), (PadButton.Y, Raylib_cs.GamepadButton.RightFaceUp),
        (PadButton.Start, Raylib_cs.GamepadButton.MiddleRight), (PadButton.Back, Raylib_cs.GamepadButton.MiddleLeft),
        (PadButton.Guide, Raylib_cs.GamepadButton.Middle),
        (PadButton.LeftShoulder, Raylib_cs.GamepadButton.LeftTrigger1), (PadButton.RightShoulder, Raylib_cs.GamepadButton.RightTrigger1),
        (PadButton.DPadUp, Raylib_cs.GamepadButton.LeftFaceUp), (PadButton.DPadDown, Raylib_cs.GamepadButton.LeftFaceDown),
        (PadButton.DPadLeft, Raylib_cs.GamepadButton.LeftFaceLeft), (PadButton.DPadRight, Raylib_cs.GamepadButton.LeftFaceRight),
        (PadButton.LeftStick, Raylib_cs.GamepadButton.LeftThumb), (PadButton.RightStick, Raylib_cs.GamepadButton.RightThumb),
    ];
    private readonly HashSet<Key> _held = [];
    private readonly bool[] _padOn = new bool[Pads];
    private readonly HashSet<PadButton>[] _padHeld = [[], [], [], []];
    private readonly float[,] _axes = new float[Pads, 6];
    private readonly bool[] _mouseHeld = new bool[3];
    private Vector2 _mouse = new(float.NaN);
    private bool _mouseInside, _text;

    public RaylibInput()
    {
        Emit(InputEvent.Added(new InputDeviceInfo(Keyboard, DeviceKind.Keyboard, "keyboard")));
        Emit(InputEvent.Added(new InputDeviceInfo(Mouse, DeviceKind.Mouse, "mouse")));
    }

    /// <summary>Where the frame was drawn, set by the platform each frame to map the mouse onto the canvas.</summary>
    public RectF Frame { get; set; }
    public int CanvasWidth { get; set; } = 320;
    public int CanvasHeight { get; set; } = 180;

    public static DeviceId PadId(int pad) => new(PadBase + pad);

    private static (Key, Raylib_cs.KeyboardKey)[] BuildKeys()
    {
        var map = new Dictionary<Key, Raylib_cs.KeyboardKey>
        {
            [Key.Space] = Raylib_cs.KeyboardKey.Space, [Key.Enter] = Raylib_cs.KeyboardKey.Enter,
            [Key.Escape] = Raylib_cs.KeyboardKey.Escape, [Key.Backspace] = Raylib_cs.KeyboardKey.Backspace,
            [Key.Tab] = Raylib_cs.KeyboardKey.Tab, [Key.LeftShift] = Raylib_cs.KeyboardKey.LeftShift,
            [Key.RightShift] = Raylib_cs.KeyboardKey.RightShift, [Key.LeftControl] = Raylib_cs.KeyboardKey.LeftControl,
            [Key.RightControl] = Raylib_cs.KeyboardKey.RightControl, [Key.LeftAlt] = Raylib_cs.KeyboardKey.LeftAlt,
            [Key.RightAlt] = Raylib_cs.KeyboardKey.RightAlt, [Key.LeftSuper] = Raylib_cs.KeyboardKey.LeftSuper,
            [Key.RightSuper] = Raylib_cs.KeyboardKey.RightSuper, [Key.Menu] = Raylib_cs.KeyboardKey.KeyboardMenu,
            [Key.Left] = Raylib_cs.KeyboardKey.Left, [Key.Right] = Raylib_cs.KeyboardKey.Right,
            [Key.Up] = Raylib_cs.KeyboardKey.Up, [Key.Down] = Raylib_cs.KeyboardKey.Down,
            [Key.Minus] = Raylib_cs.KeyboardKey.Minus, [Key.Equals] = Raylib_cs.KeyboardKey.Equal,
            [Key.LeftBracket] = Raylib_cs.KeyboardKey.LeftBracket, [Key.RightBracket] = Raylib_cs.KeyboardKey.RightBracket,
            [Key.Backslash] = Raylib_cs.KeyboardKey.Backslash, [Key.Semicolon] = Raylib_cs.KeyboardKey.Semicolon,
            [Key.Apostrophe] = Raylib_cs.KeyboardKey.Apostrophe, [Key.Grave] = Raylib_cs.KeyboardKey.Grave,
            [Key.Comma] = Raylib_cs.KeyboardKey.Comma, [Key.Period] = Raylib_cs.KeyboardKey.Period, [Key.Slash] = Raylib_cs.KeyboardKey.Slash,
            // GLFW's "world 1" key is the extra key of ISO keyboards.
            [Key.NonUsBackslash] = (Raylib_cs.KeyboardKey)161,
            [Key.CapsLock] = Raylib_cs.KeyboardKey.CapsLock, [Key.ScrollLock] = Raylib_cs.KeyboardKey.ScrollLock,
            [Key.NumLock] = Raylib_cs.KeyboardKey.NumLock, [Key.PrintScreen] = Raylib_cs.KeyboardKey.PrintScreen,
            [Key.Pause] = Raylib_cs.KeyboardKey.Pause, [Key.Insert] = Raylib_cs.KeyboardKey.Insert,
            [Key.Delete] = Raylib_cs.KeyboardKey.Delete, [Key.Home] = Raylib_cs.KeyboardKey.Home, [Key.End] = Raylib_cs.KeyboardKey.End,
            [Key.PageUp] = Raylib_cs.KeyboardKey.PageUp, [Key.PageDown] = Raylib_cs.KeyboardKey.PageDown,
            [Key.KeypadDivide] = Raylib_cs.KeyboardKey.KpDivide, [Key.KeypadMultiply] = Raylib_cs.KeyboardKey.KpMultiply,
            [Key.KeypadMinus] = Raylib_cs.KeyboardKey.KpSubtract, [Key.KeypadPlus] = Raylib_cs.KeyboardKey.KpAdd,
            [Key.KeypadEnter] = Raylib_cs.KeyboardKey.KpEnter, [Key.KeypadPeriod] = Raylib_cs.KeyboardKey.KpDecimal,
            [Key.KeypadEquals] = Raylib_cs.KeyboardKey.KpEqual,
        };
        for (int i = 0; i < 26; i++) map[Key.A + i] = Raylib_cs.KeyboardKey.A + i;
        for (int i = 0; i < 10; i++) map[Key.D0 + i] = Raylib_cs.KeyboardKey.Zero + i;
        for (int i = 0; i < 10; i++) map[Key.Keypad0 + i] = Raylib_cs.KeyboardKey.Kp0 + i;
        Key[] f1to12 = [Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6, Key.F7, Key.F8, Key.F9, Key.F10, Key.F11, Key.F12];
        for (int i = 0; i < f1to12.Length; i++) map[f1to12[i]] = Raylib_cs.KeyboardKey.F1 + i;
        // GLFW numbers F13-F24 right after F12.
        for (int i = 0; i < 12; i++) map[Key.F13 + i] = (Raylib_cs.KeyboardKey)(302 + i);
        return [.. map.Select(p => (p.Key, p.Value))];
    }

    private static readonly Dictionary<Raylib_cs.KeyboardKey, Key> ByRaylib = Keys.ToDictionary(k => k.Raylib, k => k.Key);

    /// <summary>
    /// Reads what raylib collected at the end of the last frame and emits it as events, in this order: the keyboard
    /// (presses in raylib's order, then releases), text, the mouse and wheel, then each gamepad. Called once per frame by
    /// the platform's event pump.
    /// </summary>
    public void Collect(double now)
    {
        BeginFrame();
        // Presses since the last frame, in order; a key already released again is pressed and released.
        for (int code; (code = Rl.GetKeyPressed()) != 0;)
            if (ByRaylib.TryGetValue((Raylib_cs.KeyboardKey)code, out var key))
            {
                // raylib queues real presses only (never repeats). A key it still reports down from an earlier press
                // (a release it missed, as a page can while it loads) was released in between: say so, then press it.
                if (!_held.Add(key)) Emit(InputEvent.KeyUp(Keyboard, key, now));
                Emit(InputEvent.KeyDown(Keyboard, key, now));
            }
        foreach (var (key, raylib) in Keys)
            if (_held.Contains(key) && !Rl.IsKeyDown(raylib))
            {
                _held.Remove(key);
                Emit(InputEvent.KeyUp(Keyboard, key, now));
            }
        for (int c; (c = Rl.GetCharPressed()) != 0;)
            if (_text && c is > 0 and <= 0x10FFFF and not (>= 0xD800 and <= 0xDFFF)) Emit(InputEvent.TextInput(Keyboard, char.ConvertFromUtf32(c), now));
        CollectMouse(now);
        for (int pad = 0; pad < Pads; pad++) CollectPad(pad, now);
    }

    private void CollectMouse(double now)
    {
        bool inside = Rl.IsCursorOnScreen();
        var screen = Rl.GetMousePosition();
        var at = FrameLayout.ToCanvas(screen, Frame, CanvasWidth, CanvasHeight);
        if (inside && (at != _mouse || !_mouseInside)) Emit(InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, at, now) with { ScreenPosition = screen });
        _mouse = at;
        (Raylib_cs.MouseButton Raylib, PointerButton Button)[] buttons =
            [(Raylib_cs.MouseButton.Left, PointerButton.Primary), (Raylib_cs.MouseButton.Right, PointerButton.Secondary), (Raylib_cs.MouseButton.Middle, PointerButton.Middle)];
        for (int i = 0; i < buttons.Length; i++)
        {
            var (raylib, button) = buttons[i];
            bool pressed = Rl.IsMouseButtonPressed(raylib), down = Rl.IsMouseButtonDown(raylib);
            if (pressed || down && !_mouseHeld[i]) Emit(InputEvent.PointerDown(Mouse, 0, PointerKind.Mouse, at, button, now) with { ScreenPosition = screen });
            if (!down && (_mouseHeld[i] || pressed)) Emit(InputEvent.PointerUp(Mouse, 0, PointerKind.Mouse, at, button, now) with { ScreenPosition = screen });
            _mouseHeld[i] = down;
        }
        var wheel = Rl.GetMouseWheelMoveV();
        if (wheel != Vector2.Zero) Emit(InputEvent.WheelMoved(Mouse, wheel, at, now));
        if (!inside && _mouseInside) Emit(new InputEvent { Kind = InputEventKind.PointerLeave, Device = Mouse, PointerKind = PointerKind.Mouse, Time = now });
        _mouseInside = inside;
    }

    private void CollectPad(int pad, double now)
    {
        var id = PadId(pad);
        bool on = Rl.IsGamepadAvailable(pad);
        if (on && !_padOn[pad])
            Emit(InputEvent.Added(new InputDeviceInfo(id, DeviceKind.Gamepad, Rl.GetGamepadName_(pad) ?? "gamepad")
            {
                Style = ActionPrompts.StyleFromName(Rl.GetGamepadName_(pad)),
                PlayerIndex = pad,
                CanRumble = OperatingSystem.IsBrowser(),
            }, now));
        if (!on)
        {
            if (_padOn[pad])
            {
                foreach (var release in State.HeldReleases(id, now).ToList()) Emit(release);
                Emit(InputEvent.Removed(id, now));
                _padHeld[pad].Clear();
                for (int a = 0; a < 6; a++) _axes[pad, a] = 0;
            }
            _padOn[pad] = false;
            return;
        }
        _padOn[pad] = true;
        foreach (var (button, raylib) in Buttons)
        {
            bool down = Rl.IsGamepadButtonDown(pad, raylib);
            if (down && _padHeld[pad].Add(button)) Emit(InputEvent.ButtonDown(id, button, now));
            else if (!down && _padHeld[pad].Remove(button)) Emit(InputEvent.ButtonUp(id, button, now));
        }
        for (int a = 0; a < 6; a++)
        {
            var axis = (PadAxis)a;
            float v = Rl.GetGamepadAxisMovement(pad, axis switch
            {
                PadAxis.LeftX => Raylib_cs.GamepadAxis.LeftX,
                PadAxis.LeftY => Raylib_cs.GamepadAxis.LeftY,
                PadAxis.RightX => Raylib_cs.GamepadAxis.RightX,
                PadAxis.RightY => Raylib_cs.GamepadAxis.RightY,
                PadAxis.LeftTrigger => Raylib_cs.GamepadAxis.LeftTrigger,
                _ => Raylib_cs.GamepadAxis.RightTrigger,
            });
            // raylib reports triggers as -1 (released) .. 1 (pulled).
            v = axis is PadAxis.LeftTrigger or PadAxis.RightTrigger ? (v + 1) / 2 : Math.Clamp(v, -1, 1);
            if (v != _axes[pad, a])
            {
                _axes[pad, a] = v;
                Emit(InputEvent.AxisMoved(id, axis, v, now));
            }
        }
    }

    /// <summary>Releases every key, mouse button and pad button held, as the window loses the focus.</summary>
    public void ReleaseAll(double now)
    {
        ReleaseHeld(now);
        _held.Clear();
        Array.Clear(_mouseHeld);
        foreach (var held in _padHeld) held.Clear();
    }

    /// <summary>
    /// Passes the rumble to raylib's gamepad vibration in browsers (the Gamepad API's vibration, where the browser and
    /// pad offer it); desktop raylib has none, so it returns false there.
    /// </summary>
    public bool Rumble(DeviceId pad, float low, float high, double seconds)
    {
        int index = pad.Value - PadBase;
        if (!OperatingSystem.IsBrowser() || index is < 0 or >= Pads || !_padOn[index]) return false;
        Rl.SetGamepadVibration(index, Math.Clamp(low, 0, 1), Math.Clamp(high, 0, 1), (float)Math.Max(0, seconds));
        return true;
    }

    /// <summary>raylib has no input methods: characters arrive as typed, wherever the caret is.</summary>
    public void Start(RectF caret) => _text = true;

    /// <inheritdoc/>
    public void Stop() => _text = false;
}

/// <summary>The system clipboard through raylib (GLFW).</summary>
internal sealed class RaylibClipboard : IClipboard
{
    public string? GetText() => Rl.GetClipboardText_() is { Length: > 0 } text ? text : null;

    public bool SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Rl.SetClipboardText(text);
        return true;
    }
}

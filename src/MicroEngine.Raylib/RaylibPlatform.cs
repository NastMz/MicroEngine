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
/// scaled to the window by the largest integer factor that fits (letterboxed), keeping pixel art crisp.
/// In a browser (raylib built for emscripten) the page paces frames: the window never asks to close and
/// raylib's frame limiter is left off, because both would block the only thread; the page also owns
/// fullscreen, and the canvas has no minimum size: it is a page-driven host (<see cref="CanBlock"/> is false), whose
/// surface size is the canvas's, read after start. Its capabilities (<see cref="TryGetCapability{T}"/>):
/// <see cref="IDisplayControl"/>, <see cref="IDisplayPacing"/>, <see cref="ITouchScreen"/> and, outside browsers,
/// <see cref="IPreciseDelay"/>.
/// </summary>
public sealed class RaylibPlatform : IPlatform, IDisplayControl, IDisplayPacing, IPreciseDelay, IMonotonicClock
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
    private readonly BrowserPage? _page;
    private long _frames;
    private bool _disposed;

    /// <summary>
    /// The raylib platform as a catalog entry. raylib has no iOS platform; its own audio device is not opened when the
    /// request supplies another one.
    /// </summary>
    public static PlatformProfile Profile { get; } = new("raylib", "raylib 6.0: GLFW window, OpenGL renderer, input and miniaudio sound",
        request => new RaylibPlatform(request))
    {
        CheckSupport = () => OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() ? "raylib 6.0 has no iOS platform" : null,
    };

    /// <summary>
    /// The raylib platform of a browser page as a catalog entry (named <c>raylib</c>, like <see cref="Profile"/>): every
    /// platform it builds reads touches, insets, visibility and vibration from <paramref name="page"/> only. The browser
    /// host offers this profile; a game's composition may offer others.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="page"/> is null.</exception>
    public static PlatformProfile BrowserProfile(BrowserPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return new("raylib", "raylib 6.0 in a browser page: WebGL canvas, page input and Web Audio sound", request => new RaylibPlatform(request, page))
        {
            CheckSupport = () => OperatingSystem.IsBrowser() ? null : "a browser page profile runs only in a browser",
        };
    }

    /// <summary>Opens the raylib window (and audio device unless the options say otherwise) for <paramref name="settings"/>. Hosts usually go through <see cref="Profile"/> instead.</summary>
    public RaylibPlatform(GameSettings settings, DesktopOptions options) : this(settings, options, null, null) { }

    /// <summary>
    /// Opens the raylib window for <paramref name="request"/>: its window options, its audio device or raylib's own
    /// (unless muted or silent), and its extra input sources read with raylib's.
    /// </summary>
    public RaylibPlatform(PlatformRequest request) : this(request, null) { }

    private RaylibPlatform(PlatformRequest request, BrowserPage? page) : this(
        (request ?? throw new ArgumentNullException(nameof(request))).Settings,
        new DesktopOptions { Scale = request.Scale, Hidden = request.Hidden, Mute = !request.OpensOwnAudio, CaptureScale = request.CaptureScale, Icon = request.Identity?.Icon, WaitForDisplay = request.WaitForDisplay },
        request, page)
    { }

    private RaylibPlatform(GameSettings settings, DesktopOptions options, PlatformRequest? request, BrowserPage? page)
    {
        _page = page;
        _touch = page is null ? new RaylibTouchScreen() : new RaylibTouchScreen(page.Touch);
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
        _audio = request?.CreateAudio(() => RaylibAudioDevice.TryCreate()) ?? (options.Mute ? new SilentAudioDevice() : RaylibAudioDevice.TryCreate());
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
        _capabilities.Add<ITouchScreen>(_touch);
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
    /// <summary>raylib's keyboard, gamepad and pointer, read together with the request's extra sources.</summary>
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
    /// <summary>In a browser, a hidden page (<see cref="BrowserPage.Hidden"/> of the page the platform was built for); a desktop window never suspends.</summary>
    public bool Suspended => OperatingSystem.IsBrowser() && _page is { Hidden: true };

    /// <summary>
    /// Reads what raylib collected at the end of the last frame and delivers the engine's events: a hidden page's
    /// suspension (browsers), the window's close request (desktop, once while it stays asked), focus changes and size
    /// changes of the window or canvas.
    /// </summary>
    public bool PumpEvents(IEngineEventSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        _frames++;
        _input.CollectTaps();
        _input.Frame = FrameRect();
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
        if (focused != _focused) sink.FocusChanged(_focused = focused);
        _surface.Pump(sink);
        // In a browser, fullscreen needs a user gesture, so the page handles it (F11 belongs to the browser).
        if (OperatingSystem.IsBrowser()) return !_suspended;
        // A window moved to another monitor is paced to that monitor's rate (checked twice a second: the query walks the monitors).
        if (_desktop && _frames % 30 == 0 && Rl.GetCurrentMonitor() != _monitor) Pace();
        if (Rl.IsKeyPressed(Raylib_cs.KeyboardKey.F11) || (Rl.IsKeyDown(Raylib_cs.KeyboardKey.LeftAlt) && Rl.IsKeyPressed(Raylib_cs.KeyboardKey.Enter)))
            ToggleFullscreen();
        return !_suspended;
    }

    /// <inheritdoc/>
    public IRenderer BeginFrame()
    {
        return _canvas.BeginFrame();
    }

    /// <inheritdoc/>
    public Color Letterbox { get; set; }

    public void EndFrame()
    {
        _canvas.EndFrame();
        Rl.BeginDrawing();
        // The canvas into the window as the presentation says (copied opaque: its alpha never darkens it).
        if (_canvas.Compose(_settings.EffectivePresentation, FrameRect(), Letterbox))
        {
            // The on-screen controls, over the letterboxed frame in render pixels (never in captures).
            _touch.DrawOverlay();
            _device.Present();
        }
        Rl.EndDrawing();
    }

    private RectF FrameRect()
    {
        var presentation = _settings.EffectivePresentation;
        var overlayScaling = presentation.Rounding == ScaleRounding.Fractional ? CanvasScaling.Smooth : CanvasScaling.Pixel;
        return _touch.Overlay?.Frame(_touch.Size, _touch.SafeArea, _settings.VirtualWidth, _settings.VirtualHeight, overlayScaling)
            ?? presentation.Fit(_touch.Size, _touch.SafeArea, _settings.VirtualWidth, _settings.VirtualHeight);
    }

    /// <summary>Writes the canvas as a PNG, opaque (alpha 255 everywhere) as on every backend: the frame is shown on an opaque backdrop.</summary>
    public void SaveScreenshot(string path) => _canvas.SaveCapture(path, _captureScale);

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

internal sealed class RaylibInput : IInputSource
{
    private static readonly Dictionary<Key, Raylib_cs.KeyboardKey> Keys = BuildKeys();
    private readonly Dictionary<Raylib_cs.KeyboardKey, int> _taps = [];
    private readonly HashSet<Raylib_cs.KeyboardKey> _sampled = [];

    /// <summary>
    /// raylib reports whether a key is down when the frame starts, so a key pressed and released within one
    /// frame (a quick tap during a long frame, such as the first frames after loading) would never be seen.
    /// Keys pressed since the last frame count as down until a simulation step has sampled them; frames with
    /// no step (a display faster than the update rate) keep them, and unread taps expire after a few frames.
    /// </summary>
    public void CollectTaps()
    {
        foreach (var key in _sampled) _taps.Remove(key);
        _sampled.Clear();
        foreach (var key in _taps.Keys.ToList())
            if (--_taps[key] <= 0) _taps.Remove(key);
        for (int key; (key = Rl.GetKeyPressed()) != 0;) _taps[(Raylib_cs.KeyboardKey)key] = 8;
    }

    private static Dictionary<Key, Raylib_cs.KeyboardKey> BuildKeys()
    {
        var map = new Dictionary<Key, Raylib_cs.KeyboardKey>
        {
            [Key.Space] = Raylib_cs.KeyboardKey.Space, [Key.Enter] = Raylib_cs.KeyboardKey.Enter,
            [Key.Escape] = Raylib_cs.KeyboardKey.Escape, [Key.Backspace] = Raylib_cs.KeyboardKey.Backspace,
            [Key.Tab] = Raylib_cs.KeyboardKey.Tab, [Key.LeftShift] = Raylib_cs.KeyboardKey.LeftShift,
            [Key.RightShift] = Raylib_cs.KeyboardKey.RightShift, [Key.LeftControl] = Raylib_cs.KeyboardKey.LeftControl,
            [Key.Left] = Raylib_cs.KeyboardKey.Left, [Key.Right] = Raylib_cs.KeyboardKey.Right,
            [Key.Up] = Raylib_cs.KeyboardKey.Up, [Key.Down] = Raylib_cs.KeyboardKey.Down,
            [Key.F1] = Raylib_cs.KeyboardKey.F1, [Key.F2] = Raylib_cs.KeyboardKey.F2, [Key.F3] = Raylib_cs.KeyboardKey.F3,
            [Key.F4] = Raylib_cs.KeyboardKey.F4, [Key.F5] = Raylib_cs.KeyboardKey.F5,
            [Key.F11] = Raylib_cs.KeyboardKey.F11, [Key.F12] = Raylib_cs.KeyboardKey.F12,
        };
        for (int i = 0; i < 26; i++) map[Key.A + i] = Raylib_cs.KeyboardKey.A + i;
        for (int i = 0; i < 10; i++) map[Key.D0 + i] = Raylib_cs.KeyboardKey.Zero + i;
        return map;
    }

    public bool IsKeyDown(Key key)
    {
        if (!Keys.TryGetValue(key, out var k)) return false;
        if (!_taps.ContainsKey(k)) return Rl.IsKeyDown(k);
        _sampled.Add(k);
        return true;
    }

    public bool IsPadDown(PadButton button)
    {
        if (!Rl.IsGamepadAvailable(0)) return false;
        var b = button switch
        {
            PadButton.A => Raylib_cs.GamepadButton.RightFaceDown,
            PadButton.B => Raylib_cs.GamepadButton.RightFaceRight,
            PadButton.X => Raylib_cs.GamepadButton.RightFaceLeft,
            PadButton.Y => Raylib_cs.GamepadButton.RightFaceUp,
            PadButton.Start => Raylib_cs.GamepadButton.MiddleRight,
            PadButton.Back => Raylib_cs.GamepadButton.MiddleLeft,
            PadButton.LeftShoulder => Raylib_cs.GamepadButton.LeftTrigger1,
            PadButton.RightShoulder => Raylib_cs.GamepadButton.RightTrigger1,
            PadButton.DPadUp => Raylib_cs.GamepadButton.LeftFaceUp,
            PadButton.DPadDown => Raylib_cs.GamepadButton.LeftFaceDown,
            PadButton.DPadLeft => Raylib_cs.GamepadButton.LeftFaceLeft,
            PadButton.DPadRight => Raylib_cs.GamepadButton.LeftFaceRight,
            PadButton.LeftStick => Raylib_cs.GamepadButton.LeftThumb,
            PadButton.RightStick => Raylib_cs.GamepadButton.RightThumb,
            _ => Raylib_cs.GamepadButton.Unknown,
        };
        return b != Raylib_cs.GamepadButton.Unknown && Rl.IsGamepadButtonDown(0, b);
    }

    public Vector2 LeftStick => new(GetPadAxis(PadAxis.LeftX), GetPadAxis(PadAxis.LeftY));

    public float GetPadAxis(PadAxis axis)
    {
        if (!Rl.IsGamepadAvailable(0)) return 0;
        float v = Rl.GetGamepadAxisMovement(0, axis switch
        {
            PadAxis.LeftX => Raylib_cs.GamepadAxis.LeftX,
            PadAxis.LeftY => Raylib_cs.GamepadAxis.LeftY,
            PadAxis.RightX => Raylib_cs.GamepadAxis.RightX,
            PadAxis.RightY => Raylib_cs.GamepadAxis.RightY,
            PadAxis.LeftTrigger => Raylib_cs.GamepadAxis.LeftTrigger,
            _ => Raylib_cs.GamepadAxis.RightTrigger,
        });
        // raylib reports triggers as -1 (released) .. 1 (pulled).
        return axis is PadAxis.LeftTrigger or PadAxis.RightTrigger ? (v + 1) / 2 : Math.Clamp(v, -1, 1);
    }

    public bool PadConnected => Rl.IsGamepadAvailable(0);

    public GamepadStyle PadStyle => PadConnected ? ActionPrompts.StyleFromName(Rl.GetGamepadName_(0)) : GamepadStyle.Xbox;

    /// <summary>Where the frame was drawn, set by the platform each frame to map the mouse onto the canvas.</summary>
    public RectF Frame { get; set; }
    public int CanvasWidth { get; init; } = 320;
    public int CanvasHeight { get; init; } = 180;

    public PointerState Pointer => new(Rl.IsCursorOnScreen(), FrameLayout.ToCanvas(Rl.GetMousePosition(), Frame, CanvasWidth, CanvasHeight),
        Rl.IsMouseButtonDown(Raylib_cs.MouseButton.Left), Rl.IsMouseButtonDown(Raylib_cs.MouseButton.Right), Rl.IsMouseButtonDown(Raylib_cs.MouseButton.Middle));
}

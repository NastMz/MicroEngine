using System.Numerics;
using System.Runtime.InteropServices;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using GraphicsDeviceOptions = MicroEngine.Graphics.Devices.GraphicsDeviceOptions;
using PresentMode = MicroEngine.Graphics.Devices.PresentMode;
using MicroEngine.Input;
using MicroEngine.Platform;
using SDL;
using static SDL.SDL3;

namespace MicroEngine.Sdl;

/// <summary>
/// SDL3-backed platform. Games draw through SDL's 2D renderer into a texture of the virtual size, which is scaled to
/// the window by the largest integer factor that fits (letterboxed), as on the raylib platform. SDL chooses the
/// graphics API per system (Direct3D, Metal, OpenGL, OpenGL ES, Vulkan, or its software renderer); the
/// <c>SDL_RENDER_DRIVER</c> environment variable forces one (for example <c>software</c> on a machine without a GPU).
/// Fingers go to the game's on-screen controls through <see cref="ITouchScreen"/> (<see cref="SdlTouchScreen"/>), which
/// also reports the window's safe area and density; a system that sends the app to the background suspends it
/// (<see cref="IEngineEventSink.Suspending"/>). Its capabilities (<see cref="TryGetCapability{T}"/>):
/// <see cref="IDisplayControl"/>, <see cref="IPlatformDiagnostics"/>, <see cref="IDisplayPacing"/>,
/// <see cref="IPreciseDelay"/> and <see cref="ITouchScreen"/>.
/// </summary>
public sealed unsafe class SdlPlatform : IPlatform, IPlatformDiagnostics, IDisplayControl, IDisplayPacing, IPreciseDelay, IMonotonicClock, IScreenView, IViewCapture
{
    private readonly GameSettings _settings;
    private readonly int _captureScale;
    private readonly SDL_Window* _window;
    private readonly SDL_Renderer* _sdl;
    private readonly SdlRendererDevice _device;
    private readonly DeviceCanvas _canvas;
    private readonly SdlInput _input;
    private readonly IInputSource _combinedInput;
    private readonly IAudioDevice _audio;
    private readonly HostSurface _surface;
    private readonly Kernel.CapabilityRegistry _capabilities = new();
    private readonly SdlTouchScreen _touch;
    private readonly ulong _start;
    private readonly bool _paced; // a visible real-time window: the only kind VSync applies to
    private readonly bool _mobile;
    private bool _vsync;
    private bool _suspended, _focused, _disposed;
    // Written by the lifecycle watch on the thread the system uses to send the event.
    private volatile bool _background;
    private int _wentToBackground; // 1 after a trip to the background the loop has not seen; exchanged atomically
    private GCHandle _self;

    /// <summary>The SDL3 platform as a catalog entry. SDL3 is packaged for Windows, Linux, macOS, Android and iOS, not for browsers.</summary>
    public static PlatformProfile Profile { get; } = CreateProfile();

    /// <summary>
    /// The SDL3 platform with a fixed render driver ("direct3d11", "direct3d12", "metal", "opengl", "opengles2",
    /// "vulkan", "gpu", "software"), named <c>sdl-&lt;driver&gt;</c>; null lets SDL choose (the <c>sdl</c> profile).
    /// A driver this system lacks fails when the platform is created, naming the drivers it has.
    /// </summary>
    public static PlatformProfile CreateProfile(string? renderer = null) => new(
        renderer is null ? "sdl" : $"sdl-{renderer}",
        renderer is null
            ? "SDL 3: window, 2D renderer (Direct3D, Metal, OpenGL, Vulkan or software), input, touch and audio"
            : $"SDL 3 with its {renderer} renderer",
        request => new SdlPlatform(request, renderer))
    {
        CheckSupport = () => OperatingSystem.IsBrowser() ? "SDL3 is not packaged for browsers; use raylib" : null,
    };

    /// <summary>The render drivers this SDL build offers on this system, in SDL's order of preference.</summary>
    public static IReadOnlyList<string> RenderDrivers
    {
        get
        {
            var names = new List<string>();
            for (int i = 0, n = SDL_GetNumRenderDrivers(); i < n; i++)
                if (SDL_GetRenderDriver(i) is { } name) names.Add(name);
            return names;
        }
    }

    /// <summary>
    /// Opens the SDL window, renderer and (unless the request says otherwise) audio for <paramref name="request"/>, with
    /// the audio device and extra input sources the request supplies. Hosts usually go through <see cref="Profile"/> or
    /// <see cref="CreateProfile"/> instead.
    /// </summary>
    /// <param name="request">The game's settings and window options.</param>
    /// <param name="renderer">An SDL render driver name (see <see cref="RenderDrivers"/>), or null to let SDL choose.</param>
    public SdlPlatform(PlatformRequest request, string? renderer = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        _settings = request.Settings;
        Letterbox = _settings.Letterbox;
        _captureScale = Math.Max(1, request.CaptureScale);
        SDL_SetHint(SDL_HINT_APP_NAME, _settings.Title);
        // The game's identity (its project's ApplicationTitle and ApplicationId): the name and id desktops group
        // windows by (Wayland's app id, X11's class), so a packaged game is not shown under a generic name.
        if (request.Identity is { } identity) SDL_SetAppMetadata(identity.Title, identity.Version, identity.Id);
        // Mobile systems deliver input to the game's thread through the event queue; keep mouse and touch apart.
        SDL_SetHint(SDL_HINT_TOUCH_MOUSE_EVENTS, "0");
        SDL_SetHint(SDL_HINT_MOUSE_TOUCH_EVENTS, "0");
        // By default SDL on Android blocks inside event polling while the app is paused and sends the background and
        // foreground events together on resume, so the loop would never see the suspension (music on, progress unsaved).
        SDL_SetHint(SDL_HINT_ANDROID_BLOCK_ON_PAUSE, "0");
        // Diagnostics: SDL logs every event it delivers (1 = without motion, 2 = all). Read through .NET, because on
        // mobile hosts .NET's environment variables never reach the native environment SDL reads its hints from.
        if (Environment.GetEnvironmentVariable("MICROENGINE_SDL_EVENT_LOGGING") is { Length: > 0 } logging)
            SDL_SetHint(SDL_HINT_EVENT_LOGGING, logging);
        bool mobile = _mobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
        var graphics = _settings.Graphics;
        // Pixel-art games are wide; without this SDL lets a phone turn the window to portrait.
        if (mobile) SDL_SetHint(SDL_HINT_ORIENTATIONS, (request.Identity?.Orientation ?? AppOrientation.Landscape) switch
        {
            AppOrientation.Portrait => "Portrait PortraitUpsideDown",
            AppOrientation.Any => "LandscapeLeft LandscapeRight Portrait PortraitUpsideDown",
            _ => "LandscapeLeft LandscapeRight",
        });
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO))
            throw new InvalidOperationException($"SDL could not start its video subsystem: {SDL_GetError()}");
        Mark("video", ref started);
        int scale = request.Scale ?? _settings.Scale;
        var flags = SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY;
        // A window placed on a chosen display or opened full screen is shown once it is there, so it never appears
        // first where the system would have put it.
        bool placed = !mobile && (graphics.Display is not null || graphics.WindowMode != WindowMode.Windowed);
        if (request.Hidden || placed) flags |= SDL_WindowFlags.SDL_WINDOW_HIDDEN;
        if (mobile) flags |= SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;
        var (windowWidth, windowHeight) = mobile ? (_settings.VirtualWidth * scale, _settings.VirtualHeight * scale) : InitialWindowSize(scale);
        _window = SDL_CreateWindow(_settings.Title, windowWidth, windowHeight, flags);
        if (_window == null) throw Fail("SDL could not open a window");
        if (placed)
        {
            if (graphics.Display is { } index) MoveToDisplay(index);
            if (graphics.WindowMode != WindowMode.Windowed) SetWindowMode(graphics.WindowMode);
            if (!request.Hidden) SDL_ShowWindow(_window);
        }
        if (!mobile && request.Identity?.Icon is { } icon) SetWindowIcon(icon);
        Mark("window", ref started);
        // SDL can choose a GPU only for its Metal and GPU renderers; elsewhere the system decides (GpuPreference).
        if (graphics.Gpu != GpuPreference.Automatic)
        {
            string lowPower = graphics.Gpu == GpuPreference.PowerSaving ? "1" : "0";
            SDL_SetHint(SDL_HINT_RENDER_GPU_LOW_POWER, lowPower);
            SDL_SetHint(SDL_HINT_RENDER_METAL_PREFER_LOW_POWER_DEVICE, lowPower);
        }
        _surface = new HostSurface((nint)_window, PixelSize, () => SDL_GetWindowPixelDensity(_window) is > 0 and var d ? d : 1,
            () => (SDL_GetWindowFlags(_window) & SDL_WindowFlags.SDL_WINDOW_MINIMIZED) != 0);
        _paced = !request.Hidden && request.WaitForDisplay;
        // The graphics device on SDL's renderer: the game's preferred APIs first (SDL tries a comma-separated list in
        // order), then the profile's driver or SDL's own choice.
        var present = _paced && graphics.VSync ? PresentMode.VSync : PresentMode.Immediate;
        SdlRendererDevice? device = null;
        if (renderer is null && graphics.Renderers.Count > 0)
        {
            try { device = new SdlRendererDevice(_surface, new GraphicsDeviceOptions(present, string.Join(',', graphics.Renderers))); }
            catch (InvalidOperationException e)
            {
                Console.Error.WriteLine($"platform: none of the preferred renderers ({string.Join(", ", graphics.Renderers)}) could start here ({e.Message}); SDL chooses (drivers here: {string.Join(", ", RenderDrivers)})");
            }
        }
        try { device ??= new SdlRendererDevice(_surface, new GraphicsDeviceOptions(present, renderer)); }
        catch (InvalidOperationException e)
        {
            SDL_DestroyWindow(_window);
            SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_VIDEO);
            throw new InvalidOperationException(renderer is null
                ? $"SDL could not create a renderer: {e.Message}"
                : $"SDL could not create its '{renderer}' renderer (drivers here: {string.Join(", ", RenderDrivers)}): {e.Message}", e);
        }
        _device = device;
        _sdl = device.Renderer;
        Mark("renderer", ref started);
        var (minWidth, minHeight) = WindowSizing.Minimum(_settings);
        SDL_SetWindowMinimumSize(_window, minWidth, minHeight);
        _vsync = _paced && graphics.VSync && SDL_SetRenderVSync(_sdl, 1);
        if (_paced && !graphics.VSync) SDL_SetRenderVSync(_sdl, 0);
        // The game draws into a canvas render target through the 2D batcher; EndFrame composes it into the window.
        _canvas = new DeviceCanvas(_device, _settings.VirtualWidth, _settings.VirtualHeight);
        _canvas.Graphics.DefaultFilter = _settings.DefaultTextureFilter;
        _touch = new SdlTouchScreen(_window, _sdl, mobile);
        _input = new SdlInput(this);
        _combinedInput = request.CombineInput(_input);
        Mark("frame", ref started);
        _audio = request.CreateAudio(() => SdlAudioDevice.TryCreate());
        Mark("audio", ref started);
        // A host that shows a start screen asks for the gamepad subsystem to start after it (StartGamepads); without one,
        // a window that showed the game's first frame and then froze would be worse than one that appears a moment later.
        if (!request.StartGamepadsAfterFirstFrame) StartGamepads();
        _focused = (SDL_GetWindowFlags(_window) & SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS) != 0;
        _capabilities.Add<IDisplayControl>(this);
        _capabilities.Add<IPlatformDiagnostics>(this);
        _capabilities.Add<IDisplayPacing>(this);
        _capabilities.Add<IScreenView>(this);
        _capabilities.Add<IViewCapture>(this);
        _capabilities.Add<IPreciseDelay>(this);
        _capabilities.Add<ITouchScreen>(_touch);
        _capabilities.Add<IInputBackend>(_input);
        _capabilities.Add<IKeyLabels>(_input);
        _capabilities.Add<IGamepadRumble>(_input);
        _capabilities.Add<ITextInputControl>(_input);
        _self = GCHandle.Alloc(this);
        SDL_AddEventWatch(&WatchLifecycle, GCHandle.ToIntPtr(_self));
        _start = SDL_GetTicksNS();
        RendererName = SDL_GetRendererName(_sdl) ?? "unknown";
    }

    /// <summary>The graphics API SDL chose ("direct3d11", "metal", "opengl", "software"...), for logs and tests.</summary>
    public string RendererName { get; }
    /// <summary>"SDL renderer" followed by the name of the render driver in use.</summary>
    public override string ToString() => $"SDL renderer {RendererName}";

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Diagnostics
    {
        get
        {
            var facts = new Dictionary<string, string>
            {
                ["sdl"] = $"{SDL_GetVersion() / 1000000}.{SDL_GetVersion() / 1000 % 1000}.{SDL_GetVersion() % 1000}",
                ["renderer"] = RendererName,
                ["videoDriver"] = SDL_GetCurrentVideoDriver() ?? "unknown",
            };
            int vsync;
            if (SDL_GetRenderVSync(_sdl, &vsync)) facts["vsync"] = vsync.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (CurrentDisplay is >= 0 and var current && Displays is var displays && current < displays.Count)
            {
                var d = displays[current];
                facts["display"] = string.Create(System.Globalization.CultureInfo.InvariantCulture,
                    $"{d.Width}x{d.Height} @ {d.RefreshRate:0.##} Hz, #{d.Index}{(d.Primary ? " primary" : "")} \"{d.Name}\", scale {d.Scale:0.##}");
                facts["displays"] = displays.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            facts["windowMode"] = WindowMode.ToString();
            int w, h;
            if (SDL_GetCurrentRenderOutputSize(_sdl, &w, &h)) facts["output"] = $"{w}x{h}";
            if (Adapter() is { } adapter) facts["adapter"] = adapter;
            foreach (var (step, ms) in _startSteps)
                facts[$"start.{step}"] = ms.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " ms";
            return facts;
        }
    }
    /// <inheritdoc/>
    public IGraphicsDevice Graphics => _canvas.Graphics;
    /// <summary>The graphics device the platform draws with (SDL's renderer behind the device contract).</summary>
    public SdlRendererDevice Device => _device;
    /// <summary>The canvas the game draws into, with the 2D batcher's counts for the last frame.</summary>
    public DeviceCanvas Canvas => _canvas;
    /// <inheritdoc/>
    public IAudioDevice Audio => _audio;
    /// <summary>
    /// The keyboard, gamepads and mouse as a polled source, read together with the request's extra sources. The engine
    /// reads SDL's input as events instead (the <see cref="IInputBackend"/> capability).
    /// </summary>
    public IInputSource Input => _combinedInput;
    /// <summary>SDL's nanosecond ticks since the platform opened, in seconds (the platform is its own clock).</summary>
    public IMonotonicClock Clock => this;
    /// <inheritdoc/>
    public double Now => (SDL_GetTicksNS() - _start) / 1e9;
    /// <summary>The window: an <c>SDL_Window*</c> as <see cref="ISurface.NativeWindow"/>, its size in pixels and density.</summary>
    public ISurface MainSurface => _surface;
    ISurface? IPlatformHost.MainSurface => _surface;
    /// <summary>True: SDL's native hosts own their thread.</summary>
    public bool CanBlock => true;
    /// <inheritdoc/>
    public bool TryGetCapability<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? capability) where T : class => _capabilities.TryGet(out capability);

    private (int Width, int Height) PixelSize()
    {
        int w, h;
        return SDL_GetWindowSizeInPixels(_window, &w, &h) ? (w, h) : (0, 0);
    }
    /// <summary>The refresh rate of the display the window is on while VSync paces the frames; 0 otherwise.</summary>
    public double DisplayRate
    {
        get
        {
            if (!_vsync) return 0;
            var mode = SDL_GetCurrentDisplayMode(SDL_GetDisplayForWindow(_window));
            return mode == null ? 0 : mode->refresh_rate;
        }
    }
    /// <summary>Whether the system took the app away (a phone app in the background); no frame runs meanwhile.</summary>
    public bool Suspended => _suspended;
    /// <summary>The touch screen (also the <see cref="ITouchScreen"/> capability).</summary>
    public ITouchScreen Touch => _touch;

    /// <summary>
    /// Phones announce backgrounding from their own thread and may stop the game thread soon after, so SDL asks for
    /// these events to be handled as they are sent (an event watch) rather than when the loop next polls. A trip to
    /// the background is latched until the loop has seen it, even if the app came back before it polled.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static SDLBool WatchLifecycle(IntPtr self, SDL_Event* e)
    {
        if (GCHandle.FromIntPtr(self).Target is not SdlPlatform platform) return true;
        switch ((SDL_EventType)e->type)
        {
            case SDL_EventType.SDL_EVENT_WILL_ENTER_BACKGROUND:
            case SDL_EventType.SDL_EVENT_DID_ENTER_BACKGROUND:
                platform._background = true;
                Interlocked.Exchange(ref platform._wentToBackground, 1);
                break;
            case SDL_EventType.SDL_EVENT_DID_ENTER_FOREGROUND:
                platform._background = false;
                break;
        }
        return true;
    }

    private readonly List<(string Step, double Milliseconds)> _startSteps = [];

    /// <summary>The GPU the renderer draws with, when its graphics API can name it (Direct3D 11, SDL's GPU renderer); null otherwise.</summary>
    private string? Adapter()
    {
        nint gpu = SDL_GetPointerProperty(SDL_GetRendererProperties(_sdl), SDL_PROP_RENDERER_GPU_DEVICE_POINTER, 0);
        if (gpu != 0)
        {
            var props = SDL_GetGPUDeviceProperties((SDL_GPUDevice*)gpu);
            string? name = SDL_GetStringProperty(props, SDL_PROP_GPU_DEVICE_NAME_STRING, null);
            string? driver = SDL_GetStringProperty(props, SDL_PROP_GPU_DEVICE_DRIVER_NAME_STRING, null);
            return name is null ? null : driver is null ? name : $"{name} ({driver})";
        }
        return OperatingSystem.IsWindows() ? Direct3DAdapter() : null;
    }

    /// <summary>
    /// The graphics adapter SDL's Direct3D 11 renderer draws with, by its DXGI description (on a machine with two GPUs,
    /// which one the game got); null with another renderer or when it cannot be read.
    /// </summary>
    private string? Direct3DAdapter()
    {
        nint device = SDL_GetPointerProperty(SDL_GetRendererProperties(_sdl), SDL_PROP_RENDERER_D3D11_DEVICE_POINTER, 0);
        if (device == 0) return null;
        nint dxgi = 0, adapter = 0;
        try
        {
            // ID3D11Device → IDXGIDevice (IUnknown::QueryInterface, slot 0) → IDXGIDevice::GetAdapter (slot 7)
            // → IDXGIAdapter::GetDesc (slot 8): DXGI_ADAPTER_DESC starts with the adapter's name in 128 UTF-16 units.
            var iid = new Guid("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
            if (((delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)(*(nint**)device)[0])(device, &iid, &dxgi) != 0) return null;
            if (((delegate* unmanaged[Stdcall]<nint, nint*, int>)(*(nint**)dxgi)[7])(dxgi, &adapter) != 0) return null;
            var desc = stackalloc byte[512];
            if (((delegate* unmanaged[Stdcall]<nint, byte*, int>)(*(nint**)adapter)[8])(adapter, desc) != 0) return null;
            return new string((char*)desc, 0, 128).TrimEnd('\0');
        }
        finally
        {
            if (adapter != 0) ((delegate* unmanaged[Stdcall]<nint, uint>)(*(nint**)adapter)[2])(adapter);
            if (dxgi != 0) ((delegate* unmanaged[Stdcall]<nint, uint>)(*(nint**)dxgi)[2])(dxgi);
        }
    }

    /// <summary>The window size for <paramref name="scale"/>, kept inside the display the window opens on (<see cref="WindowSizing.Initial"/>).</summary>
    private (int Width, int Height) InitialWindowSize(int scale)
    {
        var display = SDL_GetPrimaryDisplay();
        int count;
        var ids = SDL_GetDisplays(&count);
        if (ids != null)
        {
            if (_settings.Graphics.Display is { } index && index >= 0 && index < count) display = ids[index];
            SDL_free(ids);
        }
        SDL_Rect usable;
        bool known = SDL_GetDisplayUsableBounds(display, &usable);
        return WindowSizing.Initial(_settings, scale, known ? usable.w : 0, known ? usable.h : 0);
    }

    /// <summary>Records how long a step of the constructor took, for <see cref="Diagnostics"/>.</summary>
    private void Mark(string step, ref long since)
    {
        long now = System.Diagnostics.Stopwatch.GetTimestamp();
        _startSteps.Add((step, System.Diagnostics.Stopwatch.GetElapsedTime(since, now).TotalMilliseconds));
        since = now;
    }

    private static InvalidOperationException Fail(string what) => new($"{what}: {SDL_GetError()}");

    /// <summary>The game's icon on its window and taskbar entry (Windows also shows the executable's own icon).</summary>
    private void SetWindowIcon(PixelImage icon)
    {
        byte[] rgba = icon.ToRgba();
        fixed (byte* pixels = rgba)
        {
            var surface = SDL_CreateSurfaceFrom(icon.Width, icon.Height, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888, (IntPtr)pixels, icon.Width * 4);
            if (surface == null) return;
            SDL_SetWindowIcon(_window, surface);
            SDL_DestroySurface(surface);
        }
    }

    /// <summary>
    /// Processes SDL's pending events and delivers the engine's: a trip to the background (latched by the lifecycle
    /// watch, so one that ended before this pump is still delivered as <c>Suspending</c> then <c>Resumed</c>), one quit
    /// request however many close and quit events arrived together, focus changes and size changes of the window.
    /// While suspended it waits 50 ms instead of spinning.
    /// </summary>
    public bool PumpEvents(IEngineEventSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        _input.BeginFrame();
        if (Interlocked.Exchange(ref _wentToBackground, 0) == 1 && !_suspended)
        {
            // Fingers and keys held when the app left are never released by the system.
            ReleaseAll();
            _suspended = true;
            sink.Suspending();
        }
        if (_suspended && !_background)
        {
            _suspended = false;
            sink.Resumed();
        }
        bool quit = false;
        SDL_Event e;
        while (SDL_PollEvent(&e))
        {
            switch ((SDL_EventType)e.type)
            {
                case SDL_EventType.SDL_EVENT_QUIT:
                case SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED:
                case SDL_EventType.SDL_EVENT_TERMINATING:
                    // A window's close button sends a close request and a quit: one request for the game.
                    if (!quit) sink.QuitRequested();
                    quit = true;
                    break;
                case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:
                    if (!_focused) sink.FocusChanged(_focused = true);
                    break;
                case SDL_EventType.SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED:
                    _surface.Pump(sink);
                    break;
                case SDL_EventType.SDL_EVENT_FINGER_DOWN:
                    _touch.Finger(e.tfinger, TouchPhase.Began);
                    break;
                case SDL_EventType.SDL_EVENT_FINGER_MOTION:
                    _touch.Finger(e.tfinger, TouchPhase.Moved);
                    break;
                case SDL_EventType.SDL_EVENT_FINGER_UP:
                    _touch.Finger(e.tfinger, TouchPhase.Ended);
                    break;
                case SDL_EventType.SDL_EVENT_FINGER_CANCELED:
                    _touch.Finger(e.tfinger, TouchPhase.Canceled);
                    break;
                case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
                    // Keys and fingers held while another window takes over would never be released to the game.
                    ReleaseAll();
                    if (_focused) sink.FocusChanged(_focused = false);
                    break;
                default:
                    // Keys, text, mouse, wheel and gamepads become the input backend's events.
                    _input.Handle(e);
                    break;
            }
        }
        // A size change SDL reported without its own event (or with one that came before the size settled).
        _surface.Pump(sink);
        // A suspended app draws nothing (a phone may have taken its surface away); wait instead of spinning.
        if (_suspended) SDL_Delay(50);
        return !_suspended;
    }

    private void ReleaseAll()
    {
        _input.ReleaseAll();
        _touch.CancelAll();
    }

    /// <summary>Where the frame is drawn in the window, in render pixels (the shared letterbox rule).</summary>
    internal RectF FrameRect()
    {
        int sw, sh;
        SDL_GetRenderOutputSize(_sdl, &sw, &sh); // the window's size, whatever target is bound
        var window = new Vector2(sw, sh);
        var presentation = _settings.EffectivePresentation;
        return _touch.Overlay?.Frame(window, _touch.SafeArea, _settings.VirtualWidth, _settings.VirtualHeight, OverlayScaling(presentation))
            ?? presentation.Fit(window, _touch.SafeArea, _settings.VirtualWidth, _settings.VirtualHeight);
    }

    /// <summary>How the on-screen controls lay the frame out around themselves: whole factors unless the presentation is fractional.</summary>
    internal static CanvasScaling OverlayScaling(Presentation presentation) => presentation.Rounding == ScaleRounding.Fractional ? CanvasScaling.Smooth : CanvasScaling.Pixel;

    /// <summary>A window position in SDL's window coordinates as virtual canvas pixels.</summary>
    internal Vector2 ToCanvas(float x, float y) =>
        FrameLayout.ToCanvas(ToPixels(x, y), FrameRect(), _canvas.Target.Width, _canvas.Target.Height);

    /// <summary>A window position in SDL's window coordinates as render pixels (the touch screen's and overlay's pixels).</summary>
    internal Vector2 ToPixels(float x, float y)
    {
        float density = SDL_GetWindowPixelDensity(_window);
        return new Vector2(x, y) * (density > 0 ? density : 1);
    }

    /// <summary>A rectangle in virtual canvas pixels as a rectangle in SDL's window coordinates (where an input method places its candidates).</summary>
    internal SDL_Rect WindowRect(RectF canvas)
    {
        var frame = FrameRect();
        float density = SDL_GetWindowPixelDensity(_window);
        if (density <= 0) density = 1;
        float sx = frame.Width / _settings.VirtualWidth / density, sy = frame.Height / _settings.VirtualHeight / density;
        return new SDL_Rect
        {
            x = (int)MathF.Floor((frame.X / density) + canvas.X * sx),
            y = (int)MathF.Floor((frame.Y / density) + canvas.Y * sy),
            w = Math.Max(1, (int)MathF.Ceiling(canvas.Width * sx)),
            h = Math.Max(1, (int)MathF.Ceiling(canvas.Height * sy)),
        };
    }

    /// <summary>An SDL event timestamp (nanoseconds since SDL started) on this platform's clock, in seconds.</summary>
    internal double Seconds(ulong timestamp) => timestamp >= _start ? (timestamp - _start) / 1e9 : 0;

    /// <inheritdoc/>
    public IRenderer BeginFrame()
    {
        // An expanding presentation grows the canvas with the window.
        var (width, height) = WindowPixels();
        var area = _touch.SafeArea is { Width: > 0, Height: > 0 } safe ? safe : new RectF(0, 0, width, height);
        var (cw, ch) = _settings.EffectivePresentation.CanvasSize(area, _settings.VirtualWidth, _settings.VirtualHeight);
        _canvas.EnsureSize(cw, ch);
        _frameHasUi = false;
        return _canvas.BeginFrame();
    }

    private (int Width, int Height) WindowPixels()
    {
        int w, h;
        SDL_GetRenderOutputSize(_sdl, &w, &h);
        return (Math.Max(1, w), Math.Max(1, h));
    }

    (int Width, int Height) IScreenView.ScreenPixels => WindowPixels();
    (int Width, int Height) IScreenView.CanvasSize => (_canvas.Target.Width, _canvas.Target.Height);
    RectF IScreenView.WorldFrame => FrameRect();
    float IScreenView.UiScale => _settings.EffectivePresentation.UiScaleFor(_surface.PixelDensity);
    IRenderer IScreenView.BeginUi()
    {
        var (width, height) = WindowPixels();
        _frameHasUi = true;
        return _canvas.BeginUi(width, height, _settings.EffectivePresentation.UiScaleFor(_surface.PixelDensity));
    }

    private bool _frameHasUi;

    /// <inheritdoc/>
    public Color Letterbox { get; set; }

    /// <inheritdoc/>
    public void EndFrame()
    {
        _canvas.EndFrame();
        // The canvas into the window as the presentation says; nothing to present while minimised.
        if (_canvas.Compose(_settings.EffectivePresentation, FrameRect(), Letterbox))
        {
            // The on-screen controls, over the letterboxed frame in window pixels (never in captures).
            _touch.Overlay?.Draw(_touch.Canvas);
            _device.Present();
        }
        if (!_gamepadsStarted) StartGamepads();
    }

    private bool _gamepadsStarted;

    /// <summary>
    /// Starts SDL's gamepad subsystem. A game with a start screen does it once that screen is shown, before the game
    /// loads: on some Windows machines it takes about a second (measured in PERF-1, with every joystick driver of SDL
    /// turned off as well), which would otherwise keep the window from appearing. SDL reports the pads already connected
    /// as added, so none is missed.
    /// </summary>
    private void StartGamepads()
    {
        _gamepadsStarted = true;
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        if (!SDL_InitSubSystem(SDL_InitFlags.SDL_INIT_GAMEPAD))
            Console.Error.WriteLine($"input: gamepads unavailable ({SDL_GetError()})");
        Mark("gamepad", ref started);
    }

    /// <inheritdoc/>
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
        return ScreenCapture.Compose(world, ui, ((IScreenView)this).ScreenPixels, FrameRect(), Letterbox, _settings.EffectivePresentation.Upscale);
    }
    void IViewCapture.SaveWorldScreenshot(string path) => _canvas.SaveCapture(path, _captureScale);

    /// <summary>Switches between the window and borderless full screen (exclusive full screen goes back to the window).</summary>
    public void ToggleFullscreen() => SetWindowMode(WindowMode == WindowMode.Windowed ? WindowMode.Fullscreen : WindowMode.Windowed);

    /// <summary>Waits with SDL's precise delay (a high-resolution timer, then a short spin).</summary>
    public void Delay(double seconds)
    {
        if (seconds > 0) SDL_DelayPrecise((ulong)(seconds * 1e9));
    }

    /// <inheritdoc/>
    public IReadOnlyList<DisplayInfo> Displays
    {
        get
        {
            var list = new List<DisplayInfo>();
            int count;
            var ids = SDL_GetDisplays(&count);
            if (ids == null) return list;
            try
            {
                var primary = SDL_GetPrimaryDisplay();
                for (int i = 0; i < count; i++)
                {
                    var mode = SDL_GetCurrentDisplayMode(ids[i]);
                    list.Add(new DisplayInfo(i, SDL_GetDisplayName(ids[i]) ?? "", mode == null ? 0 : mode->w, mode == null ? 0 : mode->h,
                        mode == null ? 0 : mode->refresh_rate, SDL_GetDisplayContentScale(ids[i]), ids[i] == primary));
                }
            }
            finally { SDL_free(ids); }
            return list;
        }
    }

    /// <inheritdoc/>
    public int CurrentDisplay
    {
        get
        {
            var current = SDL_GetDisplayForWindow(_window);
            int count;
            var ids = SDL_GetDisplays(&count);
            if (ids == null) return -1;
            try
            {
                for (int i = 0; i < count; i++) if (ids[i] == current) return i;
                return -1;
            }
            finally { SDL_free(ids); }
        }
    }

    /// <inheritdoc/>
    public bool VSync => _vsync;

    /// <summary>Turns VSync on or off; hidden windows and requests that do not wait for the display keep it off.</summary>
    public bool SetVSync(bool on)
    {
        if (!_paced) return !on;
        if (SDL_SetRenderVSync(_sdl, on ? 1 : 0)) _vsync = on;
        return _vsync == on;
    }

    /// <inheritdoc/>
    public WindowMode WindowMode
    {
        get
        {
            if ((SDL_GetWindowFlags(_window) & SDL_WindowFlags.SDL_WINDOW_FULLSCREEN) == 0) return WindowMode.Windowed;
            return SDL_GetWindowFullscreenMode(_window) == null ? WindowMode.Fullscreen : WindowMode.ExclusiveFullscreen;
        }
    }

    /// <summary>Changes the window's mode; phones keep theirs (always full screen).</summary>
    public bool SetWindowMode(WindowMode mode)
    {
        if (_mobile) return mode != WindowMode.Windowed;
        if (mode == WindowMode.Windowed) return SDL_SetWindowFullscreen(_window, false);
        // Exclusive: the display's desktop mode (its native resolution and refresh rate); null: borderless.
        var exclusive = mode == WindowMode.ExclusiveFullscreen ? SDL_GetDesktopDisplayMode(SDL_GetDisplayForWindow(_window)) : null;
        return SDL_SetWindowFullscreenMode(_window, exclusive) && SDL_SetWindowFullscreen(_window, true);
    }

    /// <summary>Moves the window to the centre of display <paramref name="index"/>; a full-screen window follows to that display.</summary>
    public bool MoveToDisplay(int index)
    {
        if (_mobile) return false;
        int count;
        var ids = SDL_GetDisplays(&count);
        if (ids == null) return false;
        try
        {
            if (index < 0 || index >= count) return false;
            var mode = WindowMode;
            if (mode != WindowMode.Windowed) SDL_SetWindowFullscreen(_window, false);
            int position = (int)SDL_WINDOWPOS_CENTERED_DISPLAY(ids[index]);
            bool moved = SDL_SetWindowPosition(_window, position, position);
            SDL_SyncWindow(_window);
            if (mode != WindowMode.Windowed) SetWindowMode(mode);
            return moved;
        }
        finally { SDL_free(ids); }
    }

    /// <summary>Closes the window, the renderer and the audio device the platform opened.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_self.IsAllocated)
        {
            SDL_RemoveEventWatch(&WatchLifecycle, GCHandle.ToIntPtr(_self));
            _self.Free();
        }
        (_audio as IDisposable)?.Dispose();
        _canvas?.Dispose();
        _input?.Dispose();
        _touch?.Dispose();
        _device?.Dispose(); // destroys SDL's renderer
        if (_window != null) SDL_DestroyWindow(_window);
        if (_gamepadsStarted) SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_GAMEPAD);
        SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_VIDEO);
    }

    internal SDL_Window* Window => _window;
}

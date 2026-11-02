using System.Numerics;
using System.Runtime.InteropServices;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
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
/// (<see cref="IPlatform.Suspended"/>).
/// </summary>
public sealed unsafe class SdlPlatform : IPlatform, IPlatformDiagnostics, IDisplayControl
{
    private readonly GameSettings _settings;
    private readonly bool _fixedStep;
    private readonly int _captureScale;
    private readonly SDL_Window* _window;
    private readonly SDL_Renderer* _sdl;
    private readonly SDL_Texture* _target;
    private readonly SdlRenderer _renderer;
    private readonly SdlGraphicsDevice _graphics;
    private readonly SdlInput _input;
    private readonly IAudioDevice _audio;
    private readonly SdlTouchScreen _touch;
    private readonly ulong _start;
    private long _frames;
    private readonly bool _paced; // a visible real-time window: the only kind VSync applies to
    private readonly bool _mobile;
    private bool _vsync;
    private bool _closeRequested, _suspended, _disposed;
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

    /// <summary>Opens the SDL window, renderer and (unless the request says otherwise) audio for <paramref name="request"/>. Hosts usually go through <see cref="Profile"/> or <see cref="CreateProfile"/> instead.</summary>
    /// <param name="request">The game's settings and window options.</param>
    /// <param name="renderer">An SDL render driver name (see <see cref="RenderDrivers"/>), or null to let SDL choose.</param>
    public SdlPlatform(PlatformRequest request, string? renderer = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        _settings = request.Settings;
        Letterbox = _settings.Letterbox;
        _fixedStep = request.FixedStep;
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
        // The game's preferred APIs first (SDL tries a comma-separated list in order), then SDL's own choice.
        if (renderer is null && graphics.Renderers.Count > 0)
        {
            _sdl = SDL_CreateRenderer(_window, string.Join(',', graphics.Renderers));
            if (_sdl == null)
                Console.Error.WriteLine($"platform: none of the preferred renderers ({string.Join(", ", graphics.Renderers)}) could start here ({SDL_GetError()}); SDL chooses (drivers here: {string.Join(", ", RenderDrivers)})");
        }
        if (_sdl == null) _sdl = renderer is null ? SDL_CreateRenderer(_window, (byte*)null) : SDL_CreateRenderer(_window, renderer);
        if (_sdl == null)
        {
            var error = Fail(renderer is null
                ? "SDL could not create a renderer"
                : $"SDL could not create its '{renderer}' renderer (drivers here: {string.Join(", ", RenderDrivers)})");
            SDL_DestroyWindow(_window);
            SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_VIDEO);
            throw error;
        }
        Mark("renderer", ref started);
        var (minWidth, minHeight) = WindowSizing.Minimum(_settings);
        SDL_SetWindowMinimumSize(_window, minWidth, minHeight);
        _paced = !request.Hidden && !_fixedStep;
        _vsync = _paced && graphics.VSync && SDL_SetRenderVSync(_sdl, 1);
        if (_paced && !graphics.VSync) SDL_SetRenderVSync(_sdl, 0);
        _target = SDL_CreateTexture(_sdl, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888, SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, _settings.VirtualWidth, _settings.VirtualHeight);
        if (_target == null) { Dispose(); throw Fail("SDL could not create the frame texture"); }
        SDL_SetTextureScaleMode(_target, _settings.Scaling == CanvasScaling.Smooth ? SDL_ScaleMode.SDL_SCALEMODE_LINEAR : SDL_ScaleMode.SDL_SCALEMODE_NEAREST);
        _graphics = new SdlGraphicsDevice(_sdl) { DefaultFilter = _settings.DefaultTextureFilter };
        _renderer = new SdlRenderer(_sdl, _graphics, _settings.VirtualWidth, _settings.VirtualHeight);
        _touch = new SdlTouchScreen(_window, _sdl, mobile);
        _input = new SdlInput(this);
        Mark("frame", ref started);
        _audio = request.OpensOwnAudio ? SdlAudioDevice.TryCreate() : new SilentAudioDevice();
        Mark("audio", ref started);
        // Only a start screen hides the gamepad subsystem's start (StartGamepads); without one, a window that showed the
        // game's first frame and then froze would be worse than a window that appears a moment later.
        if (!(request.Identity is { Splash: true } && !request.Hidden && !_fixedStep)) StartGamepads();
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
    public IGraphicsDevice Graphics => _graphics;
    /// <inheritdoc/>
    public IAudioDevice Audio => _audio;
    /// <inheritdoc/>
    public IInputSource Input => _input;
    /// <inheritdoc/>
    public bool CloseRequested => _closeRequested;
    /// <inheritdoc/>
    public double Now => _fixedStep ? _frames / (double)_settings.UpdateRate : (SDL_GetTicksNS() - _start) / 1e9;
    /// <inheritdoc/>
    public bool RealTime => !_fixedStep;
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
    /// <inheritdoc/>
    public bool Suspended => _suspended;
    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public void PollEvents()
    {
        _frames++;
        _input.BeginFrame();
        bool suspended = Interlocked.Exchange(ref _wentToBackground, 0) == 1 || _background;
        // Fingers and keys held when the app left are never released by the system.
        if (suspended && !_suspended) ReleaseAll();
        _suspended = suspended;
        SDL_Event e;
        while (SDL_PollEvent(&e))
        {
            switch ((SDL_EventType)e.type)
            {
                case SDL_EventType.SDL_EVENT_QUIT:
                case SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED:
                case SDL_EventType.SDL_EVENT_TERMINATING:
                    _closeRequested = true;
                    break;
                case SDL_EventType.SDL_EVENT_KEY_DOWN:
                    if (!e.key.repeat && (e.key.scancode == SDL_Scancode.SDL_SCANCODE_F11
                        || e.key.scancode == SDL_Scancode.SDL_SCANCODE_RETURN && (e.key.mod & SDL_Keymod.SDL_KMOD_ALT) != 0))
                        ToggleFullscreen();
                    _input.KeyDown(e.key.scancode);
                    break;
                case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
                    _input.GamepadAdded(e.gdevice.which);
                    break;
                case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:
                    _input.GamepadRemoved(e.gdevice.which);
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
                    break;
            }
        }
        // A suspended app draws nothing (a phone may have taken its surface away); wait instead of spinning.
        if (_suspended) SDL_Delay(50);
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
        SDL_GetCurrentRenderOutputSize(_sdl, &sw, &sh);
        var window = new Vector2(sw, sh);
        return _touch.Overlay?.Frame(window, _touch.SafeArea, _settings.VirtualWidth, _settings.VirtualHeight, _settings.Scaling)
            ?? FrameLayout.Fit(window, _touch.SafeArea, _settings.VirtualWidth, _settings.VirtualHeight, _settings.Scaling);
    }

    /// <summary>A window position in SDL's window coordinates as virtual canvas pixels.</summary>
    internal Vector2 ToCanvas(float x, float y)
    {
        float density = SDL_GetWindowPixelDensity(_window);
        return FrameLayout.ToCanvas(new Vector2(x, y) * (density > 0 ? density : 1), FrameRect(), _settings.VirtualWidth, _settings.VirtualHeight);
    }

    /// <inheritdoc/>
    public IRenderer BeginFrame()
    {
        SDL_SetRenderTarget(_sdl, _target);
        _renderer.Begin();
        return _renderer;
    }

    /// <inheritdoc/>
    public Color Letterbox { get; set; }

    /// <inheritdoc/>
    public void EndFrame()
    {
        _renderer.End();
        SDL_SetRenderTarget(_sdl, null);
        SDL_SetRenderDrawColor(_sdl, Letterbox.R, Letterbox.G, Letterbox.B, 255);
        SDL_RenderClear(_sdl);
        var frame = FrameRect();
        var dst = new SDL_FRect { x = frame.X, y = frame.Y, w = frame.Width, h = frame.Height };
        SDL_RenderTexture(_sdl, _target, null, &dst);
        // The on-screen controls, over the letterboxed frame in window pixels (never in captures).
        _touch.Overlay?.Draw(_touch.Canvas);
        SDL_RenderPresent(_sdl);
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
    public void SaveScreenshot(string path)
    {
        SDL_SetRenderTarget(_sdl, _target);
        var surface = SDL_RenderReadPixels(_sdl, null);
        SDL_SetRenderTarget(_sdl, null);
        if (surface == null) throw Fail("SDL could not read the frame back");
        var rgba = SDL_ConvertSurface(surface, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888);
        SDL_DestroySurface(surface);
        if (rgba == null) throw Fail("SDL could not convert the frame");
        try
        {
            int w = rgba->w, h = rgba->h;
            var pixels = new byte[w * h * 4];
            for (int y = 0; y < h; y++)
                new ReadOnlySpan<byte>((byte*)rgba->pixels + y * rgba->pitch, w * 4).CopyTo(pixels.AsSpan(y * w * 4));
            // The frame is shown on an opaque black backdrop; store it as seen.
            for (int i = 3; i < pixels.Length; i += 4) pixels[i] = 255;
            File.WriteAllBytes(path, PngEncoder.Encode(pixels, w, h, _captureScale));
        }
        finally { SDL_DestroySurface(rgba); }
    }

    /// <summary>Switches between the window and borderless full screen (exclusive full screen goes back to the window).</summary>
    public void ToggleFullscreen() => SetWindowMode(WindowMode == WindowMode.Windowed ? WindowMode.Fullscreen : WindowMode.Windowed);

    /// <summary>Waits with SDL's precise delay (a high-resolution timer, then a short spin).</summary>
    public bool Wait(double seconds)
    {
        if (seconds > 0) SDL_DelayPrecise((ulong)(seconds * 1e9));
        return true;
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

    /// <summary>Turns VSync on or off; hidden and fixed-step (scripted) runs keep it off.</summary>
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
        _graphics?.Dispose();
        _input?.Dispose();
        _touch?.Dispose();
        if (_target != null) SDL_DestroyTexture(_target);
        if (_sdl != null) SDL_DestroyRenderer(_sdl);
        if (_window != null) SDL_DestroyWindow(_window);
        if (_gamepadsStarted) SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_GAMEPAD);
        SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_VIDEO);
    }

    internal SDL_Window* Window => _window;
}

internal sealed unsafe class SdlGraphicsDevice(SDL_Renderer* renderer) : IGraphicsDevice, IDisposable
{
    // Textures not disposed yet, released with the renderer; a texture leaves the set when it is disposed, so games that
    // load and release assets for every level do not keep their wrappers alive.
    private readonly HashSet<SdlTexture> _textures = [];

    public TextureFilter DefaultFilter { get; set; }

    public Texture CreateTexture(PixelImage image) => CreateTexture(image, DefaultFilter);

    public Texture CreateTexture(PixelImage image, TextureFilter filter)
    {
        ArgumentNullException.ThrowIfNull(image);
        var native = SDL_CreateTexture(renderer, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888, SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC, image.Width, image.Height);
        if (native == null) throw new InvalidOperationException($"SDL could not create a {image.Width}x{image.Height} texture: {SDL_GetError()}");
        // Color is laid out as R, G, B, A bytes, which is ABGR8888 on little-endian machines: no conversion copy.
        fixed (Color* p = image.Pixels) SDL_UpdateTexture(native, null, (nint)p, image.Width * 4);
        SDL_SetTextureScaleMode(native, SdlTexture.ScaleMode(filter));
        var texture = new SdlTexture(this, native, image.Width, image.Height, filter);
        _textures.Add(texture);
        return texture;
    }

    internal void Forget(SdlTexture texture) => _textures.Remove(texture);

    public void Dispose()
    {
        foreach (var t in _textures.ToList()) t.Dispose();
        _textures.Clear();
    }
}

internal sealed unsafe class SdlTexture(SdlGraphicsDevice device, SDL_Texture* native, int width, int height, TextureFilter filter) : Texture(device, width, height, filter)
{
    public SDL_Texture* Native { get; } = native;
    /// <summary>The frame (<see cref="SdlRenderer"/>'s count) in which <see cref="Tint"/> and <see cref="Blend"/> were last applied.</summary>
    public long StateFrame { get; set; } = -1;
    public Color Tint { get; set; }
    public SDL_BlendMode Blend { get; set; }

    public static SDL_ScaleMode ScaleMode(TextureFilter filter) => filter == TextureFilter.Linear ? SDL_ScaleMode.SDL_SCALEMODE_LINEAR : SDL_ScaleMode.SDL_SCALEMODE_NEAREST;

    protected override void Upload(int x, int y, int width, int height, ReadOnlySpan<Color> pixels)
    {
        var rect = new SDL_Rect { x = x, y = y, w = width, h = height };
        fixed (Color* p = pixels)
            if (!SDL_UpdateTexture(Native, &rect, (nint)p, width * 4))
                throw new InvalidOperationException($"SDL could not update a {width}x{height} region of a texture: {SDL_GetError()}");
    }

    protected override void ApplyFilter(TextureFilter filter) => SDL_SetTextureScaleMode(Native, ScaleMode(filter));

    protected override void Release()
    {
        device.Forget(this);
        SDL_DestroyTexture(Native);
    }
}

/// <summary>
/// Immediate-mode drawing through SDL's renderer, with the raylib renderer's conventions (pivot, winding, blend modes).
/// SDL already follows the engine's rendering rules (pixel centres at +0.5 with the top-left rule, Clear replacing the
/// whole target whatever the clip); textures go through <see cref="Texture.CheckDrawable"/> first.
/// </summary>
internal sealed unsafe class SdlRenderer(SDL_Renderer* sdl, SdlGraphicsDevice device, int width, int height) : IRenderer
{
    private const int CircleSegments = 36;
    /// <summary>The circle's rim as unit vectors, computed once.</summary>
    private static readonly Vector2[] Rim = [.. Enumerable.Range(0, CircleSegments + 1).Select(i => new Vector2(MathF.Cos(i * MathF.Tau / CircleSegments), MathF.Sin(i * MathF.Tau / CircleSegments)))];
    private readonly Stack<Vector2> _offsets = new();
    // State already set on the SDL renderer in this frame, so unchanged state is not set again for every draw. Anything
    // else that draws with the renderer (the letterbox, the touch overlay) does so between frames, so each frame starts
    // without assumptions.
    private long _frame;
    private SDL_BlendMode? _drawBlend;
    private Color? _drawColor;
    private readonly SDL_Vertex[] _vertices = new SDL_Vertex[CircleSegments * 3];
    private Vector2 _offset;
    private SDL_BlendMode _blend = SDL_BlendMode.SDL_BLENDMODE_BLEND;

    public int Width { get; } = width;
    public int Height { get; } = height;

    public void Begin()
    {
        _frame++;
        _drawBlend = null;
        _drawColor = null;
        _offsets.Clear();
        _offset = Vector2.Zero;
        _blend = SDL_BlendMode.SDL_BLENDMODE_BLEND;
        SDL_SetRenderClipRect(sdl, null);
    }

    public void End()
    {
        SDL_SetRenderClipRect(sdl, null);
        if (_offsets.Count != 0) throw new InvalidOperationException("Unbalanced PushCamera/PopCamera in this frame.");
    }

    private static SDL_FColor F(Color c) => new() { r = c.R / 255f, g = c.G / 255f, b = c.B / 255f, a = c.A / 255f };

    public void Clear(Color color)
    {
        DrawColor(color);
        SDL_RenderClear(sdl);
    }

    private void DrawColor(Color color)
    {
        if (_drawColor == color) return;
        _drawColor = color;
        SDL_SetRenderDrawColor(sdl, color.R, color.G, color.B, color.A);
    }

    private void DrawBlend()
    {
        if (_drawBlend == _blend) return;
        _drawBlend = _blend;
        SDL_SetRenderDrawBlendMode(sdl, _blend);
    }

    private SDL_Texture* Tint(SdlTexture texture, Color tint)
    {
        var native = texture.Native;
        bool fresh = texture.StateFrame != _frame;
        if (fresh || texture.Tint != tint)
        {
            SDL_SetTextureColorMod(native, tint.R, tint.G, tint.B);
            SDL_SetTextureAlphaMod(native, tint.A);
            texture.Tint = tint;
        }
        if (fresh || texture.Blend != _blend)
        {
            SDL_SetTextureBlendMode(native, _blend);
            texture.Blend = _blend;
        }
        texture.StateFrame = _frame;
        return native;
    }

    public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default)
    {
        var native = Tint(Texture.CheckDrawable<SdlTexture>(texture, device), tint);
        var scale = options.Scale ?? Vector2.One;
        var p = position + _offset;
        var src = new SDL_FRect { x = source.X, y = source.Y, w = source.Width, h = source.Height };
        var dst = new SDL_FRect { x = p.X, y = p.Y, w = source.Width * scale.X, h = source.Height * scale.Y };
        if (options.Rotation == 0 && !options.FlipX && !options.FlipY)
        {
            // The origin only matters when rotating; without rotation or flips the plain copy draws the same pixels.
            SDL_RenderTexture(sdl, native, &src, &dst);
            return;
        }
        var center = new SDL_FPoint { x = options.Origin.X * scale.X, y = options.Origin.Y * scale.Y };
        var flip = (options.FlipX ? SDL_FlipMode.SDL_FLIP_HORIZONTAL : 0) | (options.FlipY ? SDL_FlipMode.SDL_FLIP_VERTICAL : 0);
        SDL_RenderTextureRotated(sdl, native, &src, &dst, options.Rotation * 180.0 / Math.PI, &center, flip);
    }

    public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint)
    {
        var native = Tint(Texture.CheckDrawable<SdlTexture>(texture, device), tint);
        var src = new SDL_FRect { x = source.X, y = source.Y, w = source.Width, h = source.Height };
        var dst = new SDL_FRect { x = destination.X + _offset.X, y = destination.Y + _offset.Y, w = destination.Width, h = destination.Height };
        SDL_RenderTexture(sdl, native, &src, &dst);
    }

    public void FillRect(RectF rect, Color color)
    {
        if (color.A == 0) return;
        DrawBlend();
        DrawColor(color);
        var r = new SDL_FRect { x = rect.X + _offset.X, y = rect.Y + _offset.Y, w = rect.Width, h = rect.Height };
        SDL_RenderFillRect(sdl, &r);
    }

    public void FillCircle(Vector2 center, float radius, Color color)
    {
        DrawBlend();
        FillCircle(sdl, center + _offset, radius, F(color), _vertices);
    }

    internal static void FillCircle(SDL_Renderer* sdl, Vector2 center, float radius, SDL_FColor color, SDL_Vertex[]? buffer = null)
    {
        var v = buffer ?? new SDL_Vertex[CircleSegments * 3];
        for (int i = 0; i < CircleSegments; i++)
        {
            v[i * 3] = Vertex(center, color);
            v[i * 3 + 1] = Vertex(center + Rim[i] * radius, color);
            v[i * 3 + 2] = Vertex(center + Rim[i + 1] * radius, color);
        }
        fixed (SDL_Vertex* p = v) SDL_RenderGeometry(sdl, null, p, CircleSegments * 3, null, 0);
    }

    private static SDL_Vertex Vertex(Vector2 p, SDL_FColor c) => new() { position = new SDL_FPoint { x = p.X, y = p.Y }, color = c };

    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        DrawBlend();
        var f = F(color);
        var v = stackalloc SDL_Vertex[3] { Vertex(a + _offset, f), Vertex(b + _offset, f), Vertex(c + _offset, f) };
        SDL_RenderGeometry(sdl, null, v, 3, null, 0);
    }

    public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color)
    {
        var d = to - from;
        if (d.LengthSquared() < 1e-12f) return;
        var n = Vector2.Normalize(new Vector2(-d.Y, d.X)) * (thickness / 2);
        var f = F(color);
        Vector2 a = from + _offset + n, b = from + _offset - n, c = to + _offset - n, e = to + _offset + n;
        DrawBlend();
        var v = stackalloc SDL_Vertex[6] { Vertex(a, f), Vertex(b, f), Vertex(c, f), Vertex(a, f), Vertex(c, f), Vertex(e, f) };
        SDL_RenderGeometry(sdl, null, v, 6, null, 0);
    }

    public void PushCamera(Camera2D camera)
    {
        _offsets.Push(_offset);
        _offset = camera.RenderOffset;
    }

    public void PopCamera() => _offset = _offsets.Count > 0 ? _offsets.Pop() : throw new InvalidOperationException("PopCamera without PushCamera.");

    public void SetBlendMode(BlendMode mode) => _blend = mode switch
    {
        BlendMode.Additive => SDL_BlendMode.SDL_BLENDMODE_ADD,
        // raylib's BLEND_MULTIPLIED: src × dst + dst × (1 − src alpha), which is SDL's MUL.
        BlendMode.Multiply => SDL_BlendMode.SDL_BLENDMODE_MUL,
        _ => SDL_BlendMode.SDL_BLENDMODE_BLEND,
    };

    public void SetClip(RectF? screenRect)
    {
        if (screenRect is { } r)
        {
            var rect = new SDL_Rect { x = (int)r.X, y = (int)r.Y, w = (int)r.Width, h = (int)r.Height };
            SDL_SetRenderClipRect(sdl, &rect);
        }
        else SDL_SetRenderClipRect(sdl, null);
    }
}


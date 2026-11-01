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
/// Touch screens get on-screen controls (<see cref="TouchPad"/>); a system that sends the app to the background
/// suspends it (<see cref="IPlatform.Suspended"/>).
/// </summary>
public sealed unsafe class SdlPlatform : IPlatform
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
    private readonly TouchPad _touch = new();
    private readonly ulong _start;
    private long _frames;
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

    public SdlPlatform(PlatformRequest request, string? renderer = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        _settings = request.Settings;
        _fixedStep = request.FixedStep;
        _captureScale = Math.Max(1, request.CaptureScale);
        SDL_SetHint(SDL_HINT_APP_NAME, _settings.Title);
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
        _touch.Visible = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
        // Pixel-art games are wide; without this SDL lets a phone turn the window to portrait.
        if (_touch.Visible) SDL_SetHint(SDL_HINT_ORIENTATIONS, "LandscapeLeft LandscapeRight");
        if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_GAMEPAD))
            throw new InvalidOperationException($"SDL could not start its video subsystem: {SDL_GetError()}");
        int scale = request.Scale ?? _settings.Scale;
        var flags = SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY;
        if (request.Hidden) flags |= SDL_WindowFlags.SDL_WINDOW_HIDDEN;
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS()) flags |= SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;
        _window = SDL_CreateWindow(_settings.Title, _settings.VirtualWidth * scale, _settings.VirtualHeight * scale, flags);
        if (_window == null) throw Fail("SDL could not open a window");
        _sdl = renderer is null ? SDL_CreateRenderer(_window, (byte*)null) : SDL_CreateRenderer(_window, renderer);
        if (_sdl == null)
        {
            var error = Fail(renderer is null
                ? "SDL could not create a renderer"
                : $"SDL could not create its '{renderer}' renderer (drivers here: {string.Join(", ", RenderDrivers)})");
            SDL_DestroyWindow(_window);
            SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_GAMEPAD);
            throw error;
        }
        SDL_SetWindowMinimumSize(_window, _settings.VirtualWidth, _settings.VirtualHeight);
        if (!request.Hidden && !_fixedStep) SDL_SetRenderVSync(_sdl, 1);
        _target = SDL_CreateTexture(_sdl, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888, SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, _settings.VirtualWidth, _settings.VirtualHeight);
        if (_target == null) { Dispose(); throw Fail("SDL could not create the frame texture"); }
        SDL_SetTextureScaleMode(_target, SDL_ScaleMode.SDL_SCALEMODE_NEAREST);
        _graphics = new SdlGraphicsDevice(_sdl);
        _renderer = new SdlRenderer(_sdl, _settings.VirtualWidth, _settings.VirtualHeight);
        _input = new SdlInput(_touch);
        _audio = request.OpensOwnAudio ? SdlAudioDevice.TryCreate() : new SilentAudioDevice();
        _self = GCHandle.Alloc(this);
        SDL_AddEventWatch(&WatchLifecycle, GCHandle.ToIntPtr(_self));
        _start = SDL_GetTicksNS();
        RendererName = SDL_GetRendererName(_sdl) ?? "unknown";
    }

    /// <summary>The graphics API SDL chose ("direct3d11", "metal", "opengl", "software"...), for logs and tests.</summary>
    public string RendererName { get; }
    public override string ToString() => $"SDL renderer {RendererName}";
    public IGraphicsDevice Graphics => _graphics;
    public IAudioDevice Audio => _audio;
    public IInputSource Input => _input;
    public bool CloseRequested => _closeRequested;
    public double Now => _fixedStep ? _frames / (double)_settings.UpdateRate : (SDL_GetTicksNS() - _start) / 1e9;
    public bool RealTime => !_fixedStep;
    public bool Suspended => _suspended;

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

    private static InvalidOperationException Fail(string what) => new($"{what}: {SDL_GetError()}");

    public void PollEvents()
    {
        _frames++;
        _input.BeginFrame();
        _touch.BeginFrame();
        bool suspended = Interlocked.Exchange(ref _wentToBackground, 0) == 1 || _background;
        // Fingers and keys held when the app left are never released by the system.
        if (suspended && !_suspended) _input.ReleaseAll();
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
                case SDL_EventType.SDL_EVENT_FINGER_MOTION:
                    _touch.Set((ulong)e.tfinger.fingerID, new Vector2(e.tfinger.x, e.tfinger.y), OutputAspect());
                    break;
                case SDL_EventType.SDL_EVENT_FINGER_UP:
                case SDL_EventType.SDL_EVENT_FINGER_CANCELED:
                    _touch.Release((ulong)e.tfinger.fingerID);
                    break;
            }
        }
        // A suspended app draws nothing (a phone may have taken its surface away); wait instead of spinning.
        if (_suspended) SDL_Delay(50);
    }

    private float OutputAspect()
    {
        int w, h;
        SDL_GetCurrentRenderOutputSize(_sdl, &w, &h);
        return h > 0 ? w / (float)h : 16f / 9f;
    }

    public IRenderer BeginFrame()
    {
        SDL_SetRenderTarget(_sdl, _target);
        _renderer.Begin();
        return _renderer;
    }

    public void EndFrame()
    {
        _renderer.End();
        SDL_SetRenderTarget(_sdl, null);
        SDL_SetRenderDrawColor(_sdl, 0, 0, 0, 255);
        SDL_RenderClear(_sdl);
        int sw, sh;
        SDL_GetCurrentRenderOutputSize(_sdl, &sw, &sh);
        float scale = MathF.Min(sw / (float)_settings.VirtualWidth, sh / (float)_settings.VirtualHeight);
        if (scale >= 1) scale = MathF.Floor(scale);
        float w = _settings.VirtualWidth * scale, h = _settings.VirtualHeight * scale;
        var dst = new SDL_FRect { x = MathF.Floor((sw - w) / 2), y = MathF.Floor((sh - h) / 2), w = w, h = h };
        SDL_RenderTexture(_sdl, _target, null, &dst);
        if (_touch.Visible) DrawTouchPad(sw, sh);
        SDL_RenderPresent(_sdl);
    }

    /// <summary>The on-screen controls, drawn over the letterboxed frame in window pixels (never in captures).</summary>
    private void DrawTouchPad(int width, int height)
    {
        SDL_SetRenderDrawBlendMode(_sdl, SDL_BlendMode.SDL_BLENDMODE_BLEND);
        foreach (var control in _touch.Controls(width / (float)height))
        {
            byte alpha = (byte)(_touch.IsDown(control.Button) ? 150 : 70);
            var center = new Vector2(control.Center.X * width, control.Center.Y * height);
            SdlRenderer.FillCircle(_sdl, center, control.Radius * height, new SDL_FColor { r = 1, g = 1, b = 1, a = alpha / 255f });
        }
    }

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

    public void ToggleFullscreen()
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS()) return;
        bool full = (SDL_GetWindowFlags(_window) & SDL_WindowFlags.SDL_WINDOW_FULLSCREEN) != 0;
        SDL_SetWindowFullscreen(_window, !full);
    }

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
        if (_target != null) SDL_DestroyTexture(_target);
        if (_sdl != null) SDL_DestroyRenderer(_sdl);
        if (_window != null) SDL_DestroyWindow(_window);
        SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_GAMEPAD);
    }
}

internal sealed unsafe class SdlGraphicsDevice(SDL_Renderer* renderer) : IGraphicsDevice, IDisposable
{
    private readonly List<SdlTexture> _textures = [];

    public Texture CreateTexture(PixelImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        var native = SDL_CreateTexture(renderer, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888, SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC, image.Width, image.Height);
        if (native == null) throw new InvalidOperationException($"SDL could not create a {image.Width}x{image.Height} texture: {SDL_GetError()}");
        var rgba = image.ToRgba();
        fixed (byte* p = rgba) SDL_UpdateTexture(native, null, (nint)p, image.Width * 4);
        SDL_SetTextureScaleMode(native, SDL_ScaleMode.SDL_SCALEMODE_NEAREST);
        var texture = new SdlTexture(native, image.Width, image.Height);
        _textures.Add(texture);
        return texture;
    }

    public void Dispose()
    {
        foreach (var t in _textures) t.Dispose();
        _textures.Clear();
    }
}

internal sealed unsafe class SdlTexture(SDL_Texture* native, int width, int height) : Texture(width, height)
{
    public SDL_Texture* Native { get; } = native;
    protected override void Release() => SDL_DestroyTexture(Native);
}

/// <summary>Immediate-mode drawing through SDL's renderer, with the raylib renderer's conventions (pivot, winding, blend modes).</summary>
internal sealed unsafe class SdlRenderer(SDL_Renderer* sdl, int width, int height) : IRenderer
{
    private const int CircleSegments = 36;
    private readonly Stack<Vector2> _offsets = new();
    private readonly SDL_Vertex[] _vertices = new SDL_Vertex[CircleSegments * 3];
    private Vector2 _offset;
    private SDL_BlendMode _blend = SDL_BlendMode.SDL_BLENDMODE_BLEND;

    public int Width { get; } = width;
    public int Height { get; } = height;

    public void Begin()
    {
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
        SDL_SetRenderDrawColor(sdl, color.R, color.G, color.B, color.A);
        SDL_RenderClear(sdl);
    }

    private void Tint(SDL_Texture* texture, Color tint)
    {
        SDL_SetTextureColorMod(texture, tint.R, tint.G, tint.B);
        SDL_SetTextureAlphaMod(texture, tint.A);
        SDL_SetTextureBlendMode(texture, _blend);
    }

    public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default)
    {
        var native = ((SdlTexture)texture).Native;
        Tint(native, tint);
        var scale = options.Scale ?? Vector2.One;
        var p = position + _offset;
        var src = new SDL_FRect { x = source.X, y = source.Y, w = source.Width, h = source.Height };
        var dst = new SDL_FRect { x = p.X, y = p.Y, w = source.Width * scale.X, h = source.Height * scale.Y };
        var center = new SDL_FPoint { x = options.Origin.X * scale.X, y = options.Origin.Y * scale.Y };
        var flip = (options.FlipX ? SDL_FlipMode.SDL_FLIP_HORIZONTAL : 0) | (options.FlipY ? SDL_FlipMode.SDL_FLIP_VERTICAL : 0);
        SDL_RenderTextureRotated(sdl, native, &src, &dst, options.Rotation * 180.0 / Math.PI, &center, flip);
    }

    public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint)
    {
        var native = ((SdlTexture)texture).Native;
        Tint(native, tint);
        var src = new SDL_FRect { x = source.X, y = source.Y, w = source.Width, h = source.Height };
        var dst = new SDL_FRect { x = destination.X + _offset.X, y = destination.Y + _offset.Y, w = destination.Width, h = destination.Height };
        SDL_RenderTexture(sdl, native, &src, &dst);
    }

    public void FillRect(RectF rect, Color color)
    {
        if (color.A == 0) return;
        SDL_SetRenderDrawBlendMode(sdl, _blend);
        SDL_SetRenderDrawColor(sdl, color.R, color.G, color.B, color.A);
        var r = new SDL_FRect { x = rect.X + _offset.X, y = rect.Y + _offset.Y, w = rect.Width, h = rect.Height };
        SDL_RenderFillRect(sdl, &r);
    }

    public void FillCircle(Vector2 center, float radius, Color color)
    {
        SDL_SetRenderDrawBlendMode(sdl, _blend);
        FillCircle(sdl, center + _offset, radius, F(color), _vertices);
    }

    internal static void FillCircle(SDL_Renderer* sdl, Vector2 center, float radius, SDL_FColor color, SDL_Vertex[]? buffer = null)
    {
        var v = buffer ?? new SDL_Vertex[CircleSegments * 3];
        for (int i = 0; i < CircleSegments; i++)
        {
            float a0 = i * MathF.Tau / CircleSegments, a1 = (i + 1) * MathF.Tau / CircleSegments;
            v[i * 3] = Vertex(center, color);
            v[i * 3 + 1] = Vertex(center + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * radius, color);
            v[i * 3 + 2] = Vertex(center + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * radius, color);
        }
        fixed (SDL_Vertex* p = v) SDL_RenderGeometry(sdl, null, p, CircleSegments * 3, null, 0);
    }

    private static SDL_Vertex Vertex(Vector2 p, SDL_FColor c) => new() { position = new SDL_FPoint { x = p.X, y = p.Y }, color = c };

    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        SDL_SetRenderDrawBlendMode(sdl, _blend);
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
        SDL_SetRenderDrawBlendMode(sdl, _blend);
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

/// <summary>Keyboard, the first gamepad and the on-screen touch controls.</summary>
internal sealed unsafe class SdlInput(TouchPad touch) : IInputSource, IDisposable
{
    private static readonly Dictionary<Key, SDL_Scancode> Keys = BuildKeys();
    private readonly Dictionary<SDL_Scancode, int> _taps = [];
    private readonly HashSet<SDL_Scancode> _sampled = [];
    private SDL_Gamepad* _pad;
    private SDL_JoystickID _padId;

    /// <summary>
    /// Like the raylib input: a key pressed and released between two frames still counts as down until a simulation
    /// step has sampled it, so a quick tap during a long frame is not lost; unread taps expire after a few frames.
    /// </summary>
    public void BeginFrame()
    {
        foreach (var key in _sampled) _taps.Remove(key);
        _sampled.Clear();
        foreach (var key in _taps.Keys.ToList())
            if (--_taps[key] <= 0) _taps.Remove(key);
    }

    public void KeyDown(SDL_Scancode code) => _taps[code] = 8;

    public void ReleaseAll()
    {
        _taps.Clear();
        touch.ReleaseAll();
    }

    public void GamepadAdded(SDL_JoystickID id)
    {
        if (_pad != null) return;
        _pad = SDL_OpenGamepad(id);
        _padId = id;
    }

    public void GamepadRemoved(SDL_JoystickID id)
    {
        if (_pad == null || id != _padId) return;
        SDL_CloseGamepad(_pad);
        _pad = null;
    }

    private static Dictionary<Key, SDL_Scancode> BuildKeys()
    {
        var map = new Dictionary<Key, SDL_Scancode>
        {
            [Key.Space] = SDL_Scancode.SDL_SCANCODE_SPACE, [Key.Enter] = SDL_Scancode.SDL_SCANCODE_RETURN,
            [Key.Escape] = SDL_Scancode.SDL_SCANCODE_ESCAPE, [Key.Backspace] = SDL_Scancode.SDL_SCANCODE_BACKSPACE,
            [Key.Tab] = SDL_Scancode.SDL_SCANCODE_TAB, [Key.LeftShift] = SDL_Scancode.SDL_SCANCODE_LSHIFT,
            [Key.RightShift] = SDL_Scancode.SDL_SCANCODE_RSHIFT, [Key.LeftControl] = SDL_Scancode.SDL_SCANCODE_LCTRL,
            [Key.Left] = SDL_Scancode.SDL_SCANCODE_LEFT, [Key.Right] = SDL_Scancode.SDL_SCANCODE_RIGHT,
            [Key.Up] = SDL_Scancode.SDL_SCANCODE_UP, [Key.Down] = SDL_Scancode.SDL_SCANCODE_DOWN,
            [Key.F1] = SDL_Scancode.SDL_SCANCODE_F1, [Key.F2] = SDL_Scancode.SDL_SCANCODE_F2, [Key.F3] = SDL_Scancode.SDL_SCANCODE_F3,
            [Key.F4] = SDL_Scancode.SDL_SCANCODE_F4, [Key.F5] = SDL_Scancode.SDL_SCANCODE_F5,
            [Key.F11] = SDL_Scancode.SDL_SCANCODE_F11, [Key.F12] = SDL_Scancode.SDL_SCANCODE_F12,
            [Key.D0] = SDL_Scancode.SDL_SCANCODE_0,
        };
        for (int i = 0; i < 26; i++) map[Key.A + i] = SDL_Scancode.SDL_SCANCODE_A + i;
        for (int i = 1; i < 10; i++) map[Key.D0 + i] = SDL_Scancode.SDL_SCANCODE_1 + (i - 1);
        return map;
    }

    public bool IsKeyDown(Key key)
    {
        if (!Keys.TryGetValue(key, out var code)) return false;
        if (_taps.ContainsKey(code))
        {
            _sampled.Add(code);
            return true;
        }
        int count;
        var state = SDL_GetKeyboardState(&count);
        return (int)code < count && state[(int)code];
    }

    public bool IsPadDown(PadButton button)
    {
        if (touch.IsDown(button)) return true;
        if (_pad == null) return false;
        var b = button switch
        {
            PadButton.A => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH,
            PadButton.B => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST,
            PadButton.X => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_WEST,
            PadButton.Y => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_NORTH,
            PadButton.Start => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_START,
            PadButton.Back => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK,
            PadButton.LeftShoulder => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_SHOULDER,
            PadButton.RightShoulder => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER,
            PadButton.DPadUp => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP,
            PadButton.DPadDown => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN,
            PadButton.DPadLeft => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_LEFT,
            PadButton.DPadRight => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_RIGHT,
            _ => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_INVALID,
        };
        return b != SDL_GamepadButton.SDL_GAMEPAD_BUTTON_INVALID && SDL_GetGamepadButton(_pad, b);
    }

    public Vector2 LeftStick => _pad == null ? Vector2.Zero : new Vector2(
        Math.Max(-1f, SDL_GetGamepadAxis(_pad, SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTX) / 32767f),
        Math.Max(-1f, SDL_GetGamepadAxis(_pad, SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTY) / 32767f));

    public void Dispose()
    {
        if (_pad != null) SDL_CloseGamepad(_pad);
        _pad = null;
    }
}

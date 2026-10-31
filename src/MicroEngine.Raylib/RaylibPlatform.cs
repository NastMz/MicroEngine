using System.Numerics;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using Rl = Raylib_cs.Raylib;
using RlColor = Raylib_cs.Color;
using RlRect = Raylib_cs.Rectangle;

namespace MicroEngine.Raylib;

/// <summary>Desktop window options decided by the launcher.</summary>
public sealed record DesktopOptions(int? Scale = null, bool Hidden = false, bool FixedStep = false, bool Mute = false, int CaptureScale = 3);

/// <summary>
/// raylib-backed platform. Games draw into a virtual-resolution render texture which is
/// scaled to the window by the largest integer factor that fits (letterboxed), keeping pixel art crisp.
/// In a browser (raylib built for emscripten) the page paces frames: the window never asks to close and
/// raylib's frame limiter is left off, because both would block the only thread; the page also owns
/// fullscreen, and the canvas has no minimum size.
/// </summary>
public sealed class RaylibPlatform : IPlatform
{
    private readonly GameSettings _settings;
    private readonly bool _fixedStep;
    private readonly int _captureScale;
    private readonly Raylib_cs.RenderTexture2D _target;
    private readonly RaylibRenderer _renderer;
    private readonly RaylibInput _input = new();
    private readonly RaylibGraphicsDevice _graphics = new();
    private readonly IAudioDevice _audio;
    private long _frames;
    private bool _disposed;

    public RaylibPlatform(GameSettings settings, DesktopOptions options)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        ArgumentNullException.ThrowIfNull(options);
        _fixedStep = options.FixedStep;
        _captureScale = Math.Max(1, options.CaptureScale);
        var flags = Raylib_cs.ConfigFlags.ResizableWindow | Raylib_cs.ConfigFlags.VSyncHint;
        if (options.Hidden) flags |= Raylib_cs.ConfigFlags.HiddenWindow;
        Rl.SetTraceLogLevel(Raylib_cs.TraceLogLevel.Warning);
        Rl.SetConfigFlags(flags);
        int scale = options.Scale ?? settings.Scale;
        Rl.InitWindow(settings.VirtualWidth * scale, settings.VirtualHeight * scale, settings.Title);
        // Without a window raylib keeps going, and the next window call aborts the process from inside GLFW.
        if (!Rl.IsWindowReady())
            throw new InvalidOperationException("raylib could not open a window: no display, or no usable OpenGL (raylib's warnings above give the reason).");
        // A page may be smaller than the virtual canvas (a small iframe); the canvas then scales down instead of being cropped.
        if (!OperatingSystem.IsBrowser()) Rl.SetWindowMinSize(settings.VirtualWidth, settings.VirtualHeight);
        Rl.SetExitKey(Raylib_cs.KeyboardKey.Null);
        // On Android raylib's GetCurrentMonitor detaches the calling thread from the JVM, which aborts a thread that is
        // running .NET code (the experimental host, platforms/android); the display's vsync paces frames there instead.
        if (!options.Hidden && !OperatingSystem.IsBrowser() && !OperatingSystem.IsAndroid()) Rl.SetTargetFPS(Math.Max(60, Rl.GetMonitorRefreshRate(Rl.GetCurrentMonitor())));
        _target = Rl.LoadRenderTexture(settings.VirtualWidth, settings.VirtualHeight);
        Rl.SetTextureFilter(_target.Texture, Raylib_cs.TextureFilter.Point);
        _renderer = new RaylibRenderer(settings.VirtualWidth, settings.VirtualHeight);
        _audio = options.Mute ? new SilentAudioDevice() : RaylibAudioDevice.TryCreate();
    }

    public IGraphicsDevice Graphics => _graphics;
    public IAudioDevice Audio => _audio;
    public IInputSource Input => _input;
    public bool CloseRequested => !OperatingSystem.IsBrowser() && Rl.WindowShouldClose();
    public double Now => _fixedStep ? _frames / (double)_settings.UpdateRate : Rl.GetTime();
    public bool RealTime => !_fixedStep;

    public void PollEvents()
    {
        _frames++;
        _input.CollectTaps();
        // In a browser, fullscreen needs a user gesture, so the page handles it (F11 belongs to the browser).
        if (OperatingSystem.IsBrowser()) return;
        if (Rl.IsKeyPressed(Raylib_cs.KeyboardKey.F11) || (Rl.IsKeyDown(Raylib_cs.KeyboardKey.LeftAlt) && Rl.IsKeyPressed(Raylib_cs.KeyboardKey.Enter)))
            ToggleFullscreen();
    }

    public IRenderer BeginFrame()
    {
        Rl.BeginTextureMode(_target);
        _renderer.Begin();
        return _renderer;
    }

    public void EndFrame()
    {
        _renderer.End();
        Rl.EndTextureMode();
        Rl.BeginDrawing();
        Rl.ClearBackground(new RlColor(0, 0, 0, 255));
        int sw = Rl.GetRenderWidth(), sh = Rl.GetRenderHeight();
        float scale = MathF.Min(sw / (float)_settings.VirtualWidth, sh / (float)_settings.VirtualHeight);
        if (scale >= 1) scale = MathF.Floor(scale);
        float w = _settings.VirtualWidth * scale, h = _settings.VirtualHeight * scale;
        var src = new RlRect(0, 0, _settings.VirtualWidth, -_settings.VirtualHeight);
        var dst = new RlRect(MathF.Floor((sw - w) / 2), MathF.Floor((sh - h) / 2), w, h);
        Rl.DrawTexturePro(_target.Texture, src, dst, Vector2.Zero, 0, new RlColor(255, 255, 255, 255));
        Rl.EndDrawing();
    }

    public void SaveScreenshot(string path)
    {
        var image = Rl.LoadImageFromTexture(_target.Texture);
        Rl.ImageFlipVertical(ref image);
        if (_captureScale > 1) Rl.ImageResizeNN(ref image, image.Width * _captureScale, image.Height * _captureScale);
        Rl.ExportImage(image, path);
        Rl.UnloadImage(image);
    }

    public void ToggleFullscreen() => Rl.ToggleBorderlessWindowed();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        (_audio as IDisposable)?.Dispose();
        _graphics.Dispose();
        Rl.UnloadRenderTexture(_target);
        Rl.CloseWindow();
    }
}

internal sealed class RaylibGraphicsDevice : IGraphicsDevice, IDisposable
{
    private readonly List<RaylibTexture> _textures = [];

    public Texture CreateTexture(PixelImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        var blank = Rl.GenImageColor(image.Width, image.Height, new RlColor(0, 0, 0, 0));
        var native = Rl.LoadTextureFromImage(blank);
        Rl.UnloadImage(blank);
        Rl.UpdateTexture(native, image.ToRgba());
        Rl.SetTextureFilter(native, Raylib_cs.TextureFilter.Point);
        var texture = new RaylibTexture(native);
        _textures.Add(texture);
        return texture;
    }

    public void Dispose()
    {
        foreach (var t in _textures) t.Dispose();
        _textures.Clear();
    }
}

internal sealed class RaylibTexture(Raylib_cs.Texture2D native) : Texture(native.Width, native.Height)
{
    public Raylib_cs.Texture2D Native { get; } = native;
    protected override void Release() => Rl.UnloadTexture(Native);
}

internal sealed class RaylibRenderer(int width, int height) : IRenderer
{
    private readonly Stack<Vector2> _offsets = new();
    private Vector2 _offset;
    private BlendMode _blend = BlendMode.Alpha;
    private bool _clipping;

    public int Width { get; } = width;
    public int Height { get; } = height;

    public void Begin()
    {
        _offsets.Clear();
        _offset = Vector2.Zero;
        _blend = BlendMode.Alpha;
    }

    public void End()
    {
        if (_blend != BlendMode.Alpha) Rl.EndBlendMode();
        if (_clipping) { Rl.EndScissorMode(); _clipping = false; }
        if (_offsets.Count != 0) throw new InvalidOperationException("Unbalanced PushCamera/PopCamera in this frame.");
    }

    private static RlColor C(Color c) => new(c.R, c.G, c.B, c.A);

    public void Clear(Color color) => Rl.ClearBackground(C(color));

    public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default)
    {
        var native = ((RaylibTexture)texture).Native;
        var scale = options.Scale ?? Vector2.One;
        var src = new RlRect(source.X, source.Y, options.FlipX ? -source.Width : source.Width, options.FlipY ? -source.Height : source.Height);
        var origin = options.Origin * scale;
        var p = position + _offset;
        var dst = new RlRect(p.X + origin.X, p.Y + origin.Y, source.Width * scale.X, source.Height * scale.Y);
        Rl.DrawTexturePro(native, src, dst, origin, options.Rotation * 180f / MathF.PI, C(tint));
    }

    public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint)
    {
        var native = ((RaylibTexture)texture).Native;
        Rl.DrawTexturePro(native, new RlRect(source.X, source.Y, source.Width, source.Height),
            new RlRect(destination.X + _offset.X, destination.Y + _offset.Y, destination.Width, destination.Height), Vector2.Zero, 0, C(tint));
    }

    public void FillRect(RectF rect, Color color)
    {
        if (color.A == 0) return;
        Rl.DrawRectangleRec(new RlRect(rect.X + _offset.X, rect.Y + _offset.Y, rect.Width, rect.Height), C(color));
    }

    public void FillCircle(Vector2 center, float radius, Color color) => Rl.DrawCircleV(center + _offset, radius, C(color));

    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        // raylib only draws counter-clockwise triangles; normalise the winding for callers.
        float cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        if (cross > 0) (b, c) = (c, b);
        Rl.DrawTriangle(a + _offset, b + _offset, c + _offset, C(color));
    }

    public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color) => Rl.DrawLineEx(from + _offset, to + _offset, thickness, C(color));

    public void PushCamera(Camera2D camera)
    {
        _offsets.Push(_offset);
        _offset = camera.RenderOffset;
    }

    public void PopCamera() => _offset = _offsets.Count > 0 ? _offsets.Pop() : throw new InvalidOperationException("PopCamera without PushCamera.");

    public void SetBlendMode(BlendMode mode)
    {
        if (mode == _blend) return;
        if (_blend != BlendMode.Alpha) Rl.EndBlendMode();
        _blend = mode;
        if (mode == BlendMode.Additive) Rl.BeginBlendMode(Raylib_cs.BlendMode.Additive);
        else if (mode == BlendMode.Multiply) Rl.BeginBlendMode(Raylib_cs.BlendMode.Multiplied);
    }

    public void SetClip(RectF? screenRect)
    {
        if (_clipping) { Rl.EndScissorMode(); _clipping = false; }
        if (screenRect is { } r)
        {
            Rl.BeginScissorMode((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
            _clipping = true;
        }
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
            _ => Raylib_cs.GamepadButton.Unknown,
        };
        return b != Raylib_cs.GamepadButton.Unknown && Rl.IsGamepadButtonDown(0, b);
    }

    public Vector2 LeftStick => Rl.IsGamepadAvailable(0)
        ? new Vector2(Rl.GetGamepadAxisMovement(0, Raylib_cs.GamepadAxis.LeftX), Rl.GetGamepadAxisMovement(0, Raylib_cs.GamepadAxis.LeftY))
        : Vector2.Zero;
}

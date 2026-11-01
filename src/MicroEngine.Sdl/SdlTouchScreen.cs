using System.Numerics;
using MicroEngine.Input;
using SDL;
using static SDL.SDL3;

namespace MicroEngine.Sdl;

/// <summary>
/// SDL's touch input as an <see cref="ITouchScreen"/>: finger events in render pixels, the window's safe area
/// (<c>SDL_GetWindowSafeArea</c>: notches, rounded corners, system bars), its display scale as the density, and the
/// device vibrator on Android (SDL's haptic API). Fingers still down when the window loses focus or the app is
/// suspended are cancelled.
/// </summary>
internal sealed unsafe class SdlTouchScreen : ITouchScreen, IDisposable
{
    private readonly SDL_Window* _window;
    private readonly SDL_Renderer* _sdl;
    private readonly List<TouchEvent> _pending = [];
    private readonly Dictionary<long, Vector2> _down = [];
    private SDL_Haptic* _haptic;
    private bool _hapticTried;

    public SdlTouchScreen(SDL_Window* window, SDL_Renderer* sdl, bool touchFirst)
    {
        _window = window;
        _sdl = sdl;
        TouchFirst = touchFirst;
        Canvas = new SdlOverlayCanvas(sdl);
    }

    public bool TouchFirst { get; }
    public ITouchOverlay? Overlay { get; set; }
    internal SdlOverlayCanvas Canvas { get; }

    public Vector2 Size
    {
        get
        {
            int w, h;
            SDL_GetCurrentRenderOutputSize(_sdl, &w, &h);
            return new Vector2(w, h);
        }
    }

    public RectF SafeArea
    {
        get
        {
            SDL_Rect r;
            var size = Size;
            if (!SDL_GetWindowSafeArea(_window, &r) || r.w <= 0 || r.h <= 0) return new RectF(0, 0, size.X, size.Y);
            // The safe area is in window coordinates; the renderer draws in pixels.
            int ww, wh;
            SDL_GetWindowSize(_window, &ww, &wh);
            float sx = ww > 0 ? size.X / ww : 1, sy = wh > 0 ? size.Y / wh : 1;
            return new RectF(r.x * sx, r.y * sy, r.w * sx, r.h * sy);
        }
    }

    public float PixelsPerDp
    {
        get
        {
            // Display scale is the content scale times the pixel density: pixels per 1/96-ish inch on desktops, the
            // system's density on phones (Android reports dpi / 160).
            float scale = SDL_GetWindowDisplayScale(_window);
            return scale > 0 ? scale : 1;
        }
    }

    public void Finger(SDL_TouchFingerEvent e, TouchPhase phase)
    {
        var at = new Vector2(e.x, e.y) * Size;
        long id = (long)(ulong)e.fingerID;
        if (phase == TouchPhase.Began) _down[id] = at;
        else if (phase is TouchPhase.Ended or TouchPhase.Canceled) _down.Remove(id);
        else if (_down.ContainsKey(id)) _down[id] = at;
        _pending.Add(new TouchEvent(id, phase, at));
    }

    /// <summary>Cancels every finger that is down (focus lost, suspension).</summary>
    public void CancelAll()
    {
        foreach (var (id, at) in _down) _pending.Add(new TouchEvent(id, TouchPhase.Canceled, at));
        _down.Clear();
    }

    public void ReadTouches(List<TouchEvent> events)
    {
        events.AddRange(_pending);
        _pending.Clear();
    }

    public bool CanVibrate => Haptic() != null;

    public bool Vibrate(float strength, int milliseconds)
    {
        var haptic = Haptic();
        return haptic != null && SDL_PlayHapticRumble(haptic, Math.Clamp(strength, 0, 1), (uint)Math.Max(1, milliseconds));
    }

    /// <summary>
    /// The phone's vibrator. SDL exposes it as a haptic device on Android (with the VIBRATE permission); on iOS SDL only
    /// drives game controllers, and desktops have no vibrating screen, so this stays null there.
    /// </summary>
    private SDL_Haptic* Haptic()
    {
        if (_hapticTried) return _haptic;
        _hapticTried = true;
        if (!OperatingSystem.IsAndroid() || !SDL_InitSubSystem(SDL_InitFlags.SDL_INIT_HAPTIC)) return null;
        using var haptics = SDL_GetHaptics();
        if (haptics is null || haptics.Count == 0) return null;
        var h = SDL_OpenHaptic(haptics[0]);
        if (h == null) return null;
        if (!SDL_InitHapticRumble(h)) { SDL_CloseHaptic(h); return null; }
        return _haptic = h;
    }

    public void Dispose()
    {
        if (_haptic != null) SDL_CloseHaptic(_haptic);
        _haptic = null;
    }
}

/// <summary>Overlay drawing with SDL's renderer in window pixels: geometry for shapes, SDL's 8×8 debug font for labels.</summary>
internal sealed unsafe class SdlOverlayCanvas(SDL_Renderer* sdl) : IOverlayCanvas
{
    private const int Segments = 40;
    private readonly SDL_Vertex[] _vertices = new SDL_Vertex[Segments * 6];

    private static SDL_FColor F(Color c) => new() { r = c.R / 255f, g = c.G / 255f, b = c.B / 255f, a = c.A / 255f };
    private static SDL_Vertex V(Vector2 p, SDL_FColor c) => new() { position = new SDL_FPoint { x = p.X, y = p.Y }, color = c };

    private void Draw(int count)
    {
        SDL_SetRenderDrawBlendMode(sdl, SDL_BlendMode.SDL_BLENDMODE_BLEND);
        fixed (SDL_Vertex* p = _vertices) SDL_RenderGeometry(sdl, null, p, count, null, 0);
    }

    public void FillCircle(Vector2 center, float radius, Color color)
    {
        if (color.A == 0 || radius <= 0) return;
        var c = F(color);
        for (int i = 0; i < Segments; i++)
        {
            float a0 = i * MathF.Tau / Segments, a1 = (i + 1) * MathF.Tau / Segments;
            _vertices[i * 3] = V(center, c);
            _vertices[i * 3 + 1] = V(center + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * radius, c);
            _vertices[i * 3 + 2] = V(center + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * radius, c);
        }
        Draw(Segments * 3);
    }

    public void Ring(Vector2 center, float radius, float thickness, Color color)
    {
        if (color.A == 0 || radius <= 0) return;
        var c = F(color);
        float inner = Math.Max(0, radius - thickness);
        for (int i = 0; i < Segments; i++)
        {
            var d0 = new Vector2(MathF.Cos(i * MathF.Tau / Segments), MathF.Sin(i * MathF.Tau / Segments));
            var d1 = new Vector2(MathF.Cos((i + 1) * MathF.Tau / Segments), MathF.Sin((i + 1) * MathF.Tau / Segments));
            Vector2 o0 = center + d0 * radius, o1 = center + d1 * radius, i0 = center + d0 * inner, i1 = center + d1 * inner;
            int k = i * 6;
            _vertices[k] = V(o0, c); _vertices[k + 1] = V(o1, c); _vertices[k + 2] = V(i1, c);
            _vertices[k + 3] = V(o0, c); _vertices[k + 4] = V(i1, c); _vertices[k + 5] = V(i0, c);
        }
        Draw(Segments * 6);
    }

    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        if (color.A == 0) return;
        var f = F(color);
        _vertices[0] = V(a, f); _vertices[1] = V(b, f); _vertices[2] = V(c, f);
        Draw(3);
    }

    public void FillRect(RectF rect, Color color)
    {
        if (color.A == 0) return;
        SDL_SetRenderDrawBlendMode(sdl, SDL_BlendMode.SDL_BLENDMODE_BLEND);
        SDL_SetRenderDrawColor(sdl, color.R, color.G, color.B, color.A);
        var r = new SDL_FRect { x = rect.X, y = rect.Y, w = rect.Width, h = rect.Height };
        SDL_RenderFillRect(sdl, &r);
    }

    public void Text(string text, Vector2 center, float height, Color color)
    {
        if (string.IsNullOrEmpty(text) || color.A == 0) return;
        // SDL's debug font has 8×8 glyphs; drawing it under a render scale keeps the pixels square.
        float scale = Math.Max(1, MathF.Floor(height / 8));
        SDL_SetRenderScale(sdl, scale, scale);
        SDL_SetRenderDrawColor(sdl, color.R, color.G, color.B, color.A);
        float x = center.X / scale - text.Length * 4, y = center.Y / scale - 4;
        SDL_RenderDebugText(sdl, x, y, text);
        SDL_SetRenderScale(sdl, 1, 1);
    }
}

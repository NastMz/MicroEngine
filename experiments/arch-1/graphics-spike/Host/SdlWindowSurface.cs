using SDL;
using static SDL.SDL3;

namespace Arch1.Graphics.Sdl;

/// <summary>An <see cref="ISurface"/> over an SDL window (what the platform host would produce).</summary>
public sealed unsafe class SdlWindowSurface : ISurface, IDisposable
{
    private SDL_Window* _window;

    /// <summary>Opens a window of <paramref name="width"/>×<paramref name="height"/> pixels.</summary>
    public SdlWindowSurface(string title, int width, int height, bool hidden = true)
    {
        SdlRuntime.EnsureVideo();
        var flags = hidden ? SDL_WindowFlags.SDL_WINDOW_HIDDEN : 0;
        _window = SDL_CreateWindow(title, width, height, flags);
        if (_window == null) throw new InvalidOperationException($"SDL_CreateWindow: {SDL_GetError()}");
    }

    /// <inheritdoc />
    public nint NativeWindow => (nint)_window;

    /// <inheritdoc />
    public (int Width, int Height) PixelSize
    {
        get
        {
            int w, h;
            SDL_GetWindowSizeInPixels(_window, &w, &h);
            return (w, h);
        }
    }

    /// <inheritdoc />
    public float PixelDensity => SDL_GetWindowPixelDensity(_window);

    /// <inheritdoc />
    public bool IsMinimized
    {
        get
        {
            var (w, h) = PixelSize;
            return (SDL_GetWindowFlags(_window) & SDL_WindowFlags.SDL_WINDOW_MINIMIZED) != 0 || w == 0 || h == 0;
        }
    }

    /// <inheritdoc />
    public event Action? Resized { add { } remove { } }

    /// <inheritdoc />
    public event Action? Lost { add { } remove { } }

    /// <summary>Minimises (or restores) the window and waits until the window manager applied it.</summary>
    public void SetMinimized(bool minimized)
    {
        if (minimized) SDL_MinimizeWindow(_window); else SDL_RestoreWindow(_window);
        SDL_SyncWindow(_window);
        SdlRuntime.Pump();
    }

    /// <summary>Switches desktop full screen on or off.</summary>
    public void SetFullscreen(bool fullscreen)
    {
        SDL_SetWindowFullscreen(_window, fullscreen);
        SDL_SyncWindow(_window);
        SdlRuntime.Pump();
    }

    /// <summary>Shows the window.</summary>
    public void Show()
    {
        SDL_ShowWindow(_window);
        SDL_SyncWindow(_window);
        SdlRuntime.Pump();
    }

    /// <summary>Destroys the window (after the devices that use it).</summary>
    public void Dispose()
    {
        if (_window == null) return;
        SDL_DestroyWindow(_window);
        _window = null;
    }
}

/// <summary>Process-wide SDL initialisation for the spike.</summary>
public static class SdlRuntime
{
    private static bool _video;

    /// <summary>SDL version string of the loaded native library, e.g. "3.5.0 (SDL-3.5.0-...)".</summary>
    public static string Version
    {
        get
        {
            var v = SDL_GetVersion();
            return $"{v / 1000000}.{v / 1000 % 1000}.{v % 1000} ({SDL_GetRevision()})";
        }
    }

    /// <summary>Initialises the SDL video subsystem once.</summary>
    public static void EnsureVideo()
    {
        if (_video) return;
        if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO)) throw new InvalidOperationException($"SDL_Init: {SDL_GetError()}");
        _video = true;
    }

    /// <summary>Drains the event queue; returns false when a quit was requested.</summary>
    public static unsafe bool Pump()
    {
        SDL_Event e;
        var running = true;
        while (SDL_PollEvent(&e))
            if (e.Type == SDL_EventType.SDL_EVENT_QUIT) running = false;
        return running;
    }
}

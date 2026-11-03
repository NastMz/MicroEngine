using MicroEngine.Platform;
using SDL;
using static SDL.SDL3;

namespace Slice3D;

/// <summary>An SDL window as the device's surface (what a host gives a device), with the event pump the loop needs.</summary>
internal sealed unsafe class SdlWindow : ISurface, IDisposable
{
    private SDL_Window* _window;

    public SdlWindow(string title, int width, int height, bool hidden)
    {
        if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO)) throw new InvalidOperationException($"SDL video: {SDL_GetError()}");
        var flags = SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY;
        if (hidden) flags |= SDL_WindowFlags.SDL_WINDOW_HIDDEN;
        _window = SDL_CreateWindow(title, width, height, flags);
        if (_window == null) throw new InvalidOperationException($"SDL window: {SDL_GetError()}");
    }

    public nint NativeWindow => (nint)_window;

    public (int Width, int Height) PixelSize
    {
        get
        {
            int w, h;
            SDL_GetWindowSizeInPixels(_window, &w, &h);
            return (w, h);
        }
    }

    public float PixelDensity => SDL_GetWindowPixelDensity(_window);

    public bool IsMinimized => (SDL_GetWindowFlags(_window) & SDL_WindowFlags.SDL_WINDOW_MINIMIZED) != 0;

    public event Action? Resized { add { } remove { } }
    public event Action? Lost { add { } remove { } }

    /// <summary>Handles pending events; false once the window was closed or Escape pressed.</summary>
    public bool Pump()
    {
        SDL_Event e;
        while (SDL_PollEvent(&e))
        {
            if (e.Type is SDL_EventType.SDL_EVENT_QUIT or SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED) return false;
            if (e.Type == SDL_EventType.SDL_EVENT_KEY_DOWN && e.key.key == SDL_Keycode.SDLK_ESCAPE) return false;
        }
        return true;
    }

    public void Dispose()
    {
        if (_window == null) return;
        SDL_DestroyWindow(_window);
        _window = null;
        SDL_Quit();
    }
}

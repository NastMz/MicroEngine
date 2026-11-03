namespace MicroEngine.Platform;

/// <summary>
/// A surface a host produces and a graphics device presents to: a window, a browser canvas, a phone's screen. This is
/// the graphics contract's surface (version 1); only backends read <see cref="NativeWindow"/>.
/// </summary>
public interface ISurface
{
    /// <summary>Opaque native window (an <c>SDL_Window*</c> for SDL hosts, raylib's window handle for raylib); 0 when there is none (headless). Only backends read it.</summary>
    nint NativeWindow { get; }

    /// <summary>Drawable size in pixels.</summary>
    (int Width, int Height) PixelSize { get; }

    /// <summary>Pixels per logical unit.</summary>
    float PixelDensity { get; }

    /// <summary>True while the surface has nothing to present to (minimised, zero size).</summary>
    bool IsMinimized { get; }

    /// <summary>Raised when <see cref="PixelSize"/> changes, during the host's event pump.</summary>
    event Action? Resized;

    /// <summary>Raised when a mobile surface is destroyed.</summary>
    event Action? Lost;
}

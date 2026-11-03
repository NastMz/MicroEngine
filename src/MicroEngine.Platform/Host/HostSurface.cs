using MicroEngine.Kernel;

namespace MicroEngine.Platform;

/// <summary>
/// An <see cref="ISurface"/> for hosts: it reads its size, density and state from the host's window when asked, and
/// <see cref="Pump"/> reports a size change the way the platform contract requires (<see cref="Resized"/> and the
/// sink's <see cref="IEngineEventSink.Resized"/>, once per change).
/// </summary>
public sealed class HostSurface : ISurface
{
    private readonly Func<(int Width, int Height)> _pixelSize;
    private readonly Func<float> _density;
    private readonly Func<bool> _minimized;
    private (int Width, int Height) _reported;

    /// <summary>Creates a surface over a host's window.</summary>
    /// <param name="nativeWindow">The opaque native window, or 0 when there is none.</param>
    /// <param name="pixelSize">Reads the drawable size in pixels, as it is now.</param>
    /// <param name="density">Reads the pixels per logical unit, as it is now.</param>
    /// <param name="minimized">Reads whether the window is minimised; a zero size counts as minimised anyway.</param>
    /// <exception cref="ArgumentNullException">A reader is null.</exception>
    public HostSurface(nint nativeWindow, Func<(int Width, int Height)> pixelSize, Func<float> density, Func<bool> minimized)
    {
        NativeWindow = nativeWindow;
        _pixelSize = pixelSize ?? throw new ArgumentNullException(nameof(pixelSize));
        _density = density ?? throw new ArgumentNullException(nameof(density));
        _minimized = minimized ?? throw new ArgumentNullException(nameof(minimized));
        _reported = pixelSize();
    }

    /// <inheritdoc/>
    public nint NativeWindow { get; }
    /// <inheritdoc/>
    public (int Width, int Height) PixelSize => _pixelSize();
    /// <inheritdoc/>
    public float PixelDensity => _density();
    /// <inheritdoc/>
    public bool IsMinimized => _minimized() || PixelSize.Width <= 0 || PixelSize.Height <= 0;

    /// <inheritdoc/>
    public event Action? Resized;
    /// <inheritdoc/>
    public event Action? Lost;

    /// <summary>
    /// Compares the size with the one last reported; when it changed, raises <see cref="Resized"/> and delivers
    /// <see cref="IEngineEventSink.Resized"/> to <paramref name="sink"/>. Hosts call it from their event pump.
    /// </summary>
    /// <returns>Whether the size changed.</returns>
    public bool Pump(IEngineEventSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        var size = _pixelSize();
        if (size == _reported) return false;
        _reported = size;
        Resized?.Invoke();
        sink.Resized(new SurfaceResized(size.Width, size.Height));
        return true;
    }

    /// <summary>Raises <see cref="Lost"/>: the system destroyed the surface.</summary>
    public void RaiseLost() => Lost?.Invoke();
}

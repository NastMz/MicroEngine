namespace MicroEngine.Core.Graphics;

/// <summary>
/// Defines anti-aliasing modes for rendering.
/// Anti-aliasing smooths jagged edges by sampling multiple points per pixel.
/// Available modes depend on the backend implementation.
/// </summary>
public enum AntiAliasingMode
{
    /// <summary>
    /// No anti-aliasing (fastest, aliased edges).
    /// </summary>
    None = 0,

    /// <summary>
    /// 4x Multi-Sample Anti-Aliasing (4 samples per pixel).
    /// Good balance between quality and performance.
    /// Currently the only MSAA mode supported by Raylib backend.
    /// </summary>
    MSAA4X = 4
}

namespace MicroEngine.Core.Graphics;

/// <summary>
/// Texture filtering mode for scaling and rendering.
/// </summary>
public enum TextureFilter
{
    /// <summary>
    /// Point (nearest neighbor) filtering - sharp, pixelated look.
    /// Best for pixel art and retro-style graphics.
    /// </summary>
    Point = 0,

    /// <summary>
    /// Bilinear filtering - smooth interpolation between pixels.
    /// Reduces aliasing but may appear blurry for pixel art.
    /// </summary>
    Bilinear = 1,

    /// <summary>
    /// Trilinear filtering - bilinear with mipmap interpolation.
    /// Smoother rendering at various distances/scales.
    /// Requires mipmaps to be generated.
    /// </summary>
    Trilinear = 2,

    /// <summary>
    /// Anisotropic filtering - highest quality for oblique viewing angles.
    /// Most expensive but provides best visual quality for 3D.
    /// </summary>
    Anisotropic4X = 3,

    /// <summary>
    /// Anisotropic filtering with 8x quality - highest quality for oblique viewing angles.
    /// </summary>
    Anisotropic8X = 4,

    /// <summary>
    /// Anisotropic filtering with 16x quality - highest quality for oblique viewing angles.
    /// </summary>
    Anisotropic16X = 5
}

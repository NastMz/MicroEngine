namespace MicroEngine.Core.Graphics;

/// <summary>
/// Defines the sorting mode for sprite batching.
/// </summary>
public enum SpriteSortMode
{
    /// <summary>
    /// No sorting. Sprites are drawn in submission order.
    /// Fastest option but may cause visual artifacts.
    /// </summary>
    Deferred,

    /// <summary>
    /// Sort by texture to minimize texture switches.
    /// Good for performance with many different textures.
    /// </summary>
    Texture,

    /// <summary>
    /// Sort back-to-front by layer depth.
    /// Use for correct alpha blending.
    /// </summary>
    BackToFront,

    /// <summary>
    /// Sort front-to-back by layer depth.
    /// Good for opaque sprites with depth testing.
    /// </summary>
    FrontToBack,

    /// <summary>
    /// Immediate mode - draw each sprite immediately without batching.
    /// Useful for debugging or when batching is not desired.
    /// </summary>
    Immediate
}

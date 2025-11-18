namespace MicroEngine.Core.Resources;

/// <summary>
/// Texture resource representing a 2D image.
/// </summary>
public interface ITexture : IResource
{
    /// <summary>
    /// Gets the texture width in pixels.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the texture height in pixels.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets the texture format (e.g., "RGBA32", "RGB24").
    /// </summary>
    string Format { get; }
}

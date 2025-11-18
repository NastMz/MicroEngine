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

    /// <summary>
    /// Gets or sets the texture filtering mode.
    /// </summary>
    Graphics.TextureFilter Filter { get; set; }

    /// <summary>
    /// Gets whether this texture has mipmaps generated.
    /// Mipmaps are required for Trilinear filtering.
    /// This is true if the texture was loaded with mipmaps or if GenerateMipmaps() was called.
    /// </summary>
    bool HasMipmaps { get; }

    /// <summary>
    /// Gets the number of mipmap levels in this texture.
    /// Returns 1 if no mipmaps are present (base level only).
    /// For a 256x256 texture with mipmaps, this would be 9 (256→128→64→32→16→8→4→2→1).
    /// </summary>
    int MipmapCount { get; }

    /// <summary>
    /// Generates mipmaps for this texture if they don't already exist.
    /// Mipmaps improve visual quality and performance when textures are scaled down.
    /// Required for Trilinear and Anisotropic filtering modes.
    /// If the texture was loaded with mipmaps already present, this method does nothing.
    /// </summary>
    void GenerateMipmaps();
}

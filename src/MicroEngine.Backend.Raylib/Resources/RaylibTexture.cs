using MicroEngine.Core.Graphics;
using MicroEngine.Core.Resources;
using System.Runtime.InteropServices;

namespace MicroEngine.Backend.Raylib.Resources;

/// <summary>
/// Raylib implementation of ITexture.
/// </summary>
internal sealed class RaylibTexture : ITexture
{
    private Raylib_cs.Texture2D _texture;
    private bool _disposed;
    private TextureFilter _filter;
    private bool _hasMipmaps;

    // OpenGL constants for anisotropic filtering
    private const uint GL_TEXTURE_2D = 0x0DE1;
    private const uint GL_TEXTURE_MAX_ANISOTROPY_EXT = 0x84FE;

    // P/Invoke for OpenGL functions
    [DllImport("opengl32.dll", EntryPoint = "glBindTexture")]
    private static extern void GlBindTexture(uint target, uint texture);

    [DllImport("opengl32.dll", EntryPoint = "glTexParameterf")]
    private static extern void GlTexParameterf(uint target, uint pname, float param);

    /// <summary>
    /// Initializes a new instance of the <see cref="RaylibTexture"/> class.
    /// </summary>
    public RaylibTexture(ResourceId id, string path, Raylib_cs.Texture2D texture, ResourceMetadata? metadata = null)
    {
        Id = id;
        Path = path;
        _texture = texture;
        IsLoaded = true;
        Metadata = metadata;
        _filter = TextureFilter.Point; // Default to Point filtering for pixel art
        
        // Detect if texture was loaded with mipmaps (raylib sets mipmaps > 1 if present)
        _hasMipmaps = texture.Mipmaps > 1;
    }

    /// <inheritdoc/>
    public ResourceId Id { get; }

    /// <inheritdoc/>
    public string Path { get; }

    /// <inheritdoc/>
    public bool IsLoaded { get; private set; }

    /// <inheritdoc/>
    public long SizeInBytes => Width * Height * 4; // Assuming RGBA32

    /// <inheritdoc/>
    public ResourceMetadata? Metadata { get; }

    /// <inheritdoc/>
    public int Width => _texture.Width;

    /// <inheritdoc/>
    public int Height => _texture.Height;

    /// <inheritdoc/>
    public string Format => "RGBA32";

    /// <inheritdoc/>
    public TextureFilter Filter
    {
        get => _filter;
        set
        {
            if (_filter == value)
            {
                return;
            }

            _filter = value;
            ApplyFilter();
        }
    }

    /// <inheritdoc/>
    public bool HasMipmaps => _hasMipmaps;

    /// <inheritdoc/>
    public int MipmapCount => _texture.Mipmaps;

    /// <summary>
    /// Gets the underlying Raylib texture.
    /// </summary>
    internal Raylib_cs.Texture2D NativeTexture => _texture;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (IsLoaded)
        {
            Raylib_cs.Raylib.UnloadTexture(_texture);
            IsLoaded = false;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public void GenerateMipmaps()
    {
        if (!IsLoaded)
        {
            return;
        }

        if (_hasMipmaps)
        {
            return; // Already has mipmaps (either loaded or previously generated)
        }

        unsafe
        {
            Raylib_cs.Raylib.GenTextureMipmaps(ref _texture);
        }

        // Update mipmap count from the modified texture
        _hasMipmaps = _texture.Mipmaps > 1;

        // Re-apply the current filter after generating mipmaps
        // (Raylib may reset the filter during mipmap generation)
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (!IsLoaded)
        {
            return;
        }

        // For anisotropic filtering, we need to manually set the anisotropy level
        // because Raylib's TextureFilter enum doesn't actually configure the GL parameter
        if (_filter is TextureFilter.Anisotropic4X or TextureFilter.Anisotropic8X or TextureFilter.Anisotropic16X)
        {
            // Set base filter to Trilinear (required for anisotropic)
            Raylib_cs.Raylib.SetTextureFilter(_texture, Raylib_cs.TextureFilter.Trilinear);
            
            // Manually set anisotropy level using direct OpenGL calls
            var anisotropyLevel = _filter switch
            {
                TextureFilter.Anisotropic4X => 4.0f,
                TextureFilter.Anisotropic8X => 8.0f,
                TextureFilter.Anisotropic16X => 16.0f,
                _ => 1.0f
            };

            // Bind the texture and set anisotropy parameter
            GlBindTexture(GL_TEXTURE_2D, _texture.Id);
            GlTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MAX_ANISOTROPY_EXT, anisotropyLevel);
        }
        else
        {
            // For non-anisotropic filters, use the standard Raylib method
            var raylibFilter = _filter switch
            {
                TextureFilter.Point => Raylib_cs.TextureFilter.Point,
                TextureFilter.Bilinear => Raylib_cs.TextureFilter.Bilinear,
                TextureFilter.Trilinear => Raylib_cs.TextureFilter.Trilinear,
                _ => Raylib_cs.TextureFilter.Point
            };

            Raylib_cs.Raylib.SetTextureFilter(_texture, raylibFilter);
        }
    }
}

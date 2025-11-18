using MicroEngine.Core.Graphics;
using MicroEngine.Core.Resources;

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
    }

    private void ApplyFilter()
    {
        if (!IsLoaded)
        {
            return;
        }

        var raylibFilter = _filter switch
        {
            TextureFilter.Point => Raylib_cs.TextureFilter.Point,
            TextureFilter.Bilinear => Raylib_cs.TextureFilter.Bilinear,
            TextureFilter.Trilinear => Raylib_cs.TextureFilter.Trilinear,
            TextureFilter.Anisotropic4X => Raylib_cs.TextureFilter.Anisotropic4X,
            TextureFilter.Anisotropic8X => Raylib_cs.TextureFilter.Anisotropic8X,
            TextureFilter.Anisotropic16X => Raylib_cs.TextureFilter.Anisotropic16X,
            _ => Raylib_cs.TextureFilter.Point
        };

        Raylib_cs.Raylib.SetTextureFilter(_texture, raylibFilter);
    }
}

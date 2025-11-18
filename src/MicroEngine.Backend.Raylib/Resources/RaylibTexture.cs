using MicroEngine.Core.Resources;

namespace MicroEngine.Backend.Raylib.Resources;

/// <summary>
/// Raylib implementation of ITexture.
/// </summary>
internal sealed class RaylibTexture : ITexture
{
    private readonly Raylib_cs.Texture2D _texture;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RaylibTexture"/> class.
    /// </summary>
    public RaylibTexture(ResourceId id, string path, Raylib_cs.Texture2D texture)
    {
        Id = id;
        Path = path;
        _texture = texture;
        IsLoaded = true;
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
    public int Width => _texture.Width;

    /// <inheritdoc/>
    public int Height => _texture.Height;

    /// <inheritdoc/>
    public string Format => "RGBA32";

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
}

using MicroEngine.Core.Resources;

namespace MicroEngine.Backend.Raylib.Resources;

/// <summary>
/// Raylib implementation of IFont.
/// </summary>
internal sealed class RaylibFont : IFont
{
    private readonly Raylib_cs.Font _font;
    private readonly int _size;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RaylibFont"/> class.
    /// </summary>
    public RaylibFont(ResourceId id, string path, Raylib_cs.Font font, int size)
    {
        Id = id;
        Path = path;
        _font = font;
        _size = size;
        IsLoaded = true;
    }

    /// <inheritdoc/>
    public ResourceId Id { get; }

    /// <inheritdoc/>
    public string Path { get; }

    /// <inheritdoc/>
    public bool IsLoaded { get; private set; }

    /// <inheritdoc/>
    public long SizeInBytes => _font.GlyphCount * 64; // Rough estimate

    /// <inheritdoc/>
    public int Size => _size;

    /// <inheritdoc/>
    public string Family => System.IO.Path.GetFileNameWithoutExtension(Path);

    /// <summary>
    /// Gets the underlying Raylib font.
    /// </summary>
    internal Raylib_cs.Font NativeFont => _font;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (IsLoaded)
        {
            Raylib_cs.Raylib.UnloadFont(_font);
            IsLoaded = false;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

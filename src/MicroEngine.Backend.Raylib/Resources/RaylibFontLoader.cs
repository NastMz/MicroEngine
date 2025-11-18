using MicroEngine.Core.Resources;

namespace MicroEngine.Backend.Raylib.Resources;

/// <summary>
/// Raylib implementation of IResourceLoader for fonts.
/// Supports TTF, OTF font formats.
/// </summary>
public sealed class RaylibFontLoader : IResourceLoader<IFont>
{
    private static readonly string[] EXTENSIONS = [".ttf", ".otf"];
    private readonly int _defaultSize;
    private uint _nextId = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="RaylibFontLoader"/> class.
    /// </summary>
    /// <param name="defaultSize">Default font size in pixels (default: 32).</param>
    public RaylibFontLoader(int defaultSize = 32)
    {
        _defaultSize = defaultSize;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> SupportedExtensions => EXTENSIONS;

    /// <inheritdoc/>
    public IFont Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Font file not found: {path}", path);
        }

        var font = Raylib_cs.Raylib.LoadFontEx(path, _defaultSize, null, 0);

        if (font.GlyphCount == 0)
        {
            throw new InvalidDataException($"Failed to load font: {path}");
        }

        var id = new ResourceId(_nextId++);
        return new RaylibFont(id, path, font, _defaultSize);
    }

    /// <inheritdoc/>
    public void Unload(IFont resource)
    {
        resource.Dispose();
    }
}

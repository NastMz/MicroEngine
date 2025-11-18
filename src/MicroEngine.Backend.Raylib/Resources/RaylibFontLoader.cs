using MicroEngine.Core.Resources;

namespace MicroEngine.Backend.Raylib.Resources;

/// <summary>
/// Raylib implementation of IResourceLoader for fonts.
/// Supports TTF, OTF font formats.
/// </summary>
public sealed class RaylibFontLoader : IResourceLoader<IFont>
{
    private static readonly string[] EXTENSIONS = [".ttf", ".otf"];
    private readonly ResourceValidator _validator;
    private readonly int _defaultSize;
    private uint _nextId = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="RaylibFontLoader"/> class.
    /// </summary>
    /// <param name="defaultSize">Default font size in pixels (default: 32).</param>
    /// <param name="validator">Resource validator instance.</param>
    public RaylibFontLoader(int defaultSize = 32, ResourceValidator? validator = null)
    {
        _defaultSize = defaultSize;
        _validator = validator ?? new ResourceValidator();
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> SupportedExtensions => EXTENSIONS;

    /// <inheritdoc/>
    public ResourceValidationResult Validate(string path)
    {
        return _validator.Validate(path, SupportedExtensions);
    }

    /// <inheritdoc/>
    public IFont Load(string path, ResourceMetadata? metadata = null)
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
        
        var enrichedMetadata = metadata ?? ResourceMetadata.FromFile(path);
        enrichedMetadata = enrichedMetadata.WithCustomMetadata(new Dictionary<string, string>
        {
            ["Size"] = _defaultSize.ToString(),
            ["GlyphCount"] = font.GlyphCount.ToString(),
            ["BaseSize"] = font.BaseSize.ToString()
        });

        return new RaylibFont(id, path, font, _defaultSize, enrichedMetadata);
    }

    /// <inheritdoc/>
    public void Unload(IFont resource)
    {
        resource.Dispose();
    }
}

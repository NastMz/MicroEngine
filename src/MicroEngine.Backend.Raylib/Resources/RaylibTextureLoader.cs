using MicroEngine.Core.Resources;

namespace MicroEngine.Backend.Raylib.Resources;

/// <summary>
/// Raylib implementation of IResourceLoader for textures.
/// Supports PNG, JPG, BMP, TGA, GIF formats.
/// </summary>
public sealed class RaylibTextureLoader : IResourceLoader<ITexture>
{
    private static readonly string[] EXTENSIONS = [".png", ".jpg", ".jpeg", ".bmp", ".tga", ".gif"];
    private readonly ResourceValidator _validator;
    private uint _nextId = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="RaylibTextureLoader"/> class.
    /// </summary>
    public RaylibTextureLoader(ResourceValidator? validator = null)
    {
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
    public ITexture Load(string path, ResourceMetadata? metadata = null)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Texture file not found: {path}", path);
        }

        var texture = Raylib_cs.Raylib.LoadTexture(path);

        if (texture.Id == 0)
        {
            throw new InvalidDataException($"Failed to load texture: {path}");
        }

        var id = new ResourceId(_nextId++);
        
        var enrichedMetadata = metadata ?? ResourceMetadata.FromFile(path);
        enrichedMetadata = enrichedMetadata.WithCustomMetadata(new Dictionary<string, string>
        {
            ["Width"] = texture.Width.ToString(),
            ["Height"] = texture.Height.ToString(),
            ["Format"] = "RGBA32"
        });

        return new RaylibTexture(id, path, texture, enrichedMetadata);
    }

    /// <inheritdoc/>
    public void Unload(ITexture resource)
    {
        // Dispose handles Raylib unloading
        resource.Dispose();
    }
}

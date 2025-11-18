using MicroEngine.Core.Resources;

namespace MicroEngine.Backend.Raylib.Resources;

/// <summary>
/// Raylib implementation of IResourceLoader for textures.
/// Supports PNG, JPG, BMP, TGA, GIF formats.
/// </summary>
public sealed class RaylibTextureLoader : IResourceLoader<ITexture>
{
    private static readonly string[] EXTENSIONS = [".png", ".jpg", ".jpeg", ".bmp", ".tga", ".gif"];
    private uint _nextId = 1;

    /// <inheritdoc/>
    public IReadOnlyList<string> SupportedExtensions => EXTENSIONS;

    /// <inheritdoc/>
    public ITexture Load(string path)
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
        return new RaylibTexture(id, path, texture);
    }

    /// <inheritdoc/>
    public void Unload(ITexture resource)
    {
        // Dispose handles Raylib unloading
        resource.Dispose();
    }
}

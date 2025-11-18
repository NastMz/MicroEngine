using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Graphics;

/// <summary>
/// Represents a texture atlas containing multiple sprite regions.
/// Allows multiple sprites to share a single texture for efficient batching.
/// </summary>
public class SpriteAtlas
{
    private readonly ITexture _texture;
    private readonly Dictionary<string, SpriteRegion> _regions;

    /// <summary>
    /// Gets the underlying texture.
    /// </summary>
    public ITexture Texture => _texture;

    /// <summary>
    /// Gets the number of regions in this atlas.
    /// </summary>
    public int RegionCount => _regions.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpriteAtlas"/> class.
    /// </summary>
    /// <param name="texture">The atlas texture.</param>
    public SpriteAtlas(ITexture texture)
    {
        _texture = texture ?? throw new ArgumentNullException(nameof(texture));
        _regions = new Dictionary<string, SpriteRegion>();
    }

    /// <summary>
    /// Adds a sprite region to the atlas.
    /// </summary>
    /// <param name="name">Unique name for this region.</param>
    /// <param name="region">The sprite region.</param>
    public void AddRegion(string name, SpriteRegion region)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Region name cannot be null or empty", nameof(name));
        }

        _regions[name] = region;
    }

    /// <summary>
    /// Adds a sprite region from bounds.
    /// </summary>
    /// <param name="name">Unique name for this region.</param>
    /// <param name="x">X position in atlas.</param>
    /// <param name="y">Y position in atlas.</param>
    /// <param name="width">Width of region.</param>
    /// <param name="height">Height of region.</param>
    public void AddRegion(string name, float x, float y, float width, float height)
    {
        var region = SpriteRegion.FromBounds(x, y, width, height, name);
        AddRegion(name, region);
    }

    /// <summary>
    /// Gets a sprite region by name.
    /// </summary>
    /// <param name="name">Name of the region.</param>
    /// <returns>The sprite region.</returns>
    /// <exception cref="KeyNotFoundException">If region not found.</exception>
    public SpriteRegion GetRegion(string name)
    {
        if (!_regions.TryGetValue(name, out var region))
        {
            throw new KeyNotFoundException($"Sprite region '{name}' not found in atlas");
        }

        return region;
    }

    /// <summary>
    /// Tries to get a sprite region by name.
    /// </summary>
    /// <param name="name">Name of the region.</param>
    /// <param name="region">The sprite region if found.</param>
    /// <returns>True if region was found.</returns>
    public bool TryGetRegion(string name, out SpriteRegion region)
    {
        return _regions.TryGetValue(name, out region);
    }

    /// <summary>
    /// Checks if a region exists in the atlas.
    /// </summary>
    /// <param name="name">Name of the region.</param>
    /// <returns>True if region exists.</returns>
    public bool HasRegion(string name)
    {
        return _regions.ContainsKey(name);
    }

    /// <summary>
    /// Gets all region names in the atlas.
    /// </summary>
    /// <returns>Collection of region names.</returns>
    public IEnumerable<string> GetRegionNames()
    {
        return _regions.Keys;
    }

    /// <summary>
    /// Removes a region from the atlas.
    /// </summary>
    /// <param name="name">Name of the region to remove.</param>
    /// <returns>True if region was removed.</returns>
    public bool RemoveRegion(string name)
    {
        return _regions.Remove(name);
    }

    /// <summary>
    /// Clears all regions from the atlas.
    /// </summary>
    public void Clear()
    {
        _regions.Clear();
    }

    /// <summary>
    /// Creates a grid-based atlas (useful for spritesheets with uniform frames).
    /// </summary>
    /// <param name="texture">The texture.</param>
    /// <param name="frameWidth">Width of each frame.</param>
    /// <param name="frameHeight">Height of each frame.</param>
    /// <param name="spacing">Spacing between frames.</param>
    /// <param name="margin">Margin around the spritesheet.</param>
    /// <param name="namePrefix">Prefix for generated frame names.</param>
    /// <returns>A sprite atlas with grid regions.</returns>
    public static SpriteAtlas CreateGrid(
        ITexture texture,
        int frameWidth,
        int frameHeight,
        int spacing = 0,
        int margin = 0,
        string namePrefix = "frame")
    {
        var atlas = new SpriteAtlas(texture);

        var columns = (texture.Width - margin * 2 + spacing) / (frameWidth + spacing);
        var rows = (texture.Height - margin * 2 + spacing) / (frameHeight + spacing);

        var frameIndex = 0;
        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < columns; col++)
            {
                var x = margin + col * (frameWidth + spacing);
                var y = margin + row * (frameHeight + spacing);

                var name = $"{namePrefix}_{frameIndex}";
                atlas.AddRegion(name, x, y, frameWidth, frameHeight);

                frameIndex++;
            }
        }

        return atlas;
    }
}

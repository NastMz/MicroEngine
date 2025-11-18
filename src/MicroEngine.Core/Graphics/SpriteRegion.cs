using MicroEngine.Core.Math;

namespace MicroEngine.Core.Graphics;

/// <summary>
/// Represents a rectangular region within a texture (sprite atlas).
/// Used to define sprite frames within a larger texture.
/// </summary>
public readonly struct SpriteRegion : IEquatable<SpriteRegion>
{
    /// <summary>
    /// Gets the source rectangle in texture coordinates (pixels).
    /// </summary>
    public Rectangle Source { get; init; }

    /// <summary>
    /// Gets the pivot point for rotation (normalized 0-1).
    /// Default is (0.5, 0.5) for center pivot.
    /// </summary>
    public Vector2 Pivot { get; init; }

    /// <summary>
    /// Gets the name of this sprite region (optional).
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpriteRegion"/> struct.
    /// </summary>
    /// <param name="source">Source rectangle in texture coordinates.</param>
    /// <param name="pivot">Pivot point (normalized 0-1). Default is center (0.5, 0.5).</param>
    /// <param name="name">Optional name for this region.</param>
    public SpriteRegion(Rectangle source, Vector2 pivot, string name = "")
    {
        Source = source;
        Pivot = pivot;
        Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpriteRegion"/> struct with center pivot.
    /// </summary>
    /// <param name="source">Source rectangle in texture coordinates.</param>
    /// <param name="name">Optional name for this region.</param>
    public SpriteRegion(Rectangle source, string name = "")
        : this(source, new Vector2(0.5f, 0.5f), name)
    {
    }

    /// <summary>
    /// Creates a sprite region from position and size.
    /// </summary>
    /// <param name="x">X position in texture.</param>
    /// <param name="y">Y position in texture.</param>
    /// <param name="width">Width of the region.</param>
    /// <param name="height">Height of the region.</param>
    /// <param name="name">Optional name for this region.</param>
    /// <returns>A new sprite region.</returns>
    public static SpriteRegion FromBounds(float x, float y, float width, float height, string name = "")
    {
        return new SpriteRegion(new Rectangle(x, y, width, height), name);
    }

    /// <summary>
    /// Creates a sprite region that covers the entire texture.
    /// </summary>
    /// <param name="textureWidth">Texture width in pixels.</param>
    /// <param name="textureHeight">Texture height in pixels.</param>
    /// <returns>A sprite region covering the full texture.</returns>
    public static SpriteRegion FullTexture(int textureWidth, int textureHeight)
    {
        return new SpriteRegion(new Rectangle(0, 0, textureWidth, textureHeight), "Full");
    }

    /// <inheritdoc/>
    public bool Equals(SpriteRegion other)
    {
        return Source.Equals(other.Source) && 
               Pivot.Equals(other.Pivot) && 
               Name == other.Name;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is SpriteRegion other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Source, Pivot, Name);
    }

    /// <summary>
    /// Checks if two sprite regions are equal.
    /// </summary>
    public static bool operator ==(SpriteRegion left, SpriteRegion right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Checks if two sprite regions are not equal.
    /// </summary>
    public static bool operator !=(SpriteRegion left, SpriteRegion right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var nameStr = string.IsNullOrEmpty(Name) ? "Unnamed" : Name;
        return $"SpriteRegion({nameStr}, {Source}, Pivot={Pivot})";
    }
}

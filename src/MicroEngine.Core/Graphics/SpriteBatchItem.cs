using MicroEngine.Core.Math;
using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Graphics;

/// <summary>
/// Represents a single sprite to be rendered in a batch.
/// Contains all rendering parameters for deferred rendering.
/// </summary>
public readonly struct SpriteBatchItem
{
    /// <summary>
    /// Gets the texture to render.
    /// </summary>
    public ITexture Texture { get; init; }

    /// <summary>
    /// Gets the position in world/screen space.
    /// </summary>
    public Vector2 Position { get; init; }

    /// <summary>
    /// Gets the source region in the texture.
    /// </summary>
    public SpriteRegion Region { get; init; }

    /// <summary>
    /// Gets the rotation in degrees.
    /// </summary>
    public float Rotation { get; init; }

    /// <summary>
    /// Gets the scale factor.
    /// </summary>
    public Vector2 Scale { get; init; }

    /// <summary>
    /// Gets the color tint.
    /// </summary>
    public Color Tint { get; init; }

    /// <summary>
    /// Gets the layer depth for sorting (0 = back, 1 = front).
    /// </summary>
    public float LayerDepth { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpriteBatchItem"/> struct.
    /// </summary>
    /// <param name="texture">Texture to render.</param>
    /// <param name="position">Position in world/screen space.</param>
    /// <param name="region">Source region in texture.</param>
    /// <param name="rotation">Rotation in degrees.</param>
    /// <param name="scale">Scale factor.</param>
    /// <param name="tint">Color tint.</param>
    /// <param name="layerDepth">Layer depth for sorting (0-1).</param>
    public SpriteBatchItem(
        ITexture texture,
        Vector2 position,
        SpriteRegion region,
        float rotation = 0f,
        Vector2 scale = default,
        Color tint = default,
        float layerDepth = 0f)
    {
        Texture = texture;
        Position = position;
        Region = region;
        Rotation = rotation;
        Scale = scale.Equals(Vector2.Zero) ? Vector2.One : scale;
        Tint = tint.Equals(default(Color)) ? Color.White : tint;
        LayerDepth = System.Math.Clamp(layerDepth, 0f, 1f);
    }
}

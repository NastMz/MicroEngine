using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;
using MicroEngine.Core.Resources;

namespace MicroEngine.Core.ECS.Components;

/// <summary>
/// Component for rendering 2D sprites with support for texture atlases.
/// Pure data component - contains no logic.
/// Rendering logic is handled by RenderSystem.
/// </summary>
public struct SpriteComponent : IComponent
{
    /// <summary>
    /// Gets or sets the texture to render.
    /// </summary>
    public ITexture? Texture { get; set; }

    /// <summary>
    /// Gets or sets the sprite region from a texture atlas.
    /// If null, the entire texture is used.
    /// </summary>
    public SpriteRegion? Region { get; set; }

    /// <summary>
    /// Gets or sets the tint color applied to the sprite.
    /// White means no tinting.
    /// </summary>
    public Color Tint { get; set; }

    /// <summary>
    /// Gets or sets whether the sprite is visible.
    /// </summary>
    public bool Visible { get; set; }

    /// <summary>
    /// Gets or sets the rendering layer/depth.
    /// Higher values render on top.
    /// </summary>
    public int Layer { get; set; }

    /// <summary>
    /// Gets or sets whether to flip the sprite horizontally.
    /// </summary>
    public bool FlipX { get; set; }

    /// <summary>
    /// Gets or sets whether to flip the sprite vertically.
    /// </summary>
    public bool FlipY { get; set; }
}

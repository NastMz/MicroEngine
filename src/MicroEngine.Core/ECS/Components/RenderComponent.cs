using MicroEngine.Core.Graphics;

namespace MicroEngine.Core.ECS.Components;

/// <summary>
/// Defines the shape to render for a RenderComponent.
/// </summary>
public enum RenderShape
{
    /// <summary>
    /// Rectangle shape.
    /// </summary>
    Rectangle,

    /// <summary>
    /// Circle shape.
    /// </summary>
    Circle,

    /// <summary>
    /// Line shape.
    /// </summary>
    Line
}

/// <summary>
/// Component for basic rendering data.
/// Pure data component - contains no logic.
/// </summary>
public struct RenderComponent : IComponent
{
    /// <summary>
    /// Gets or sets the color of the rendered shape.
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    /// Gets or sets the shape to render.
    /// </summary>
    public RenderShape Shape { get; set; }

    /// <summary>
    /// Gets or sets whether the component is enabled.
    /// </summary>
    public bool Enabled { get; set; }
}

using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;

namespace MicroEngine.Game.Utils;

/// <summary>
/// Represents a simple procedural sprite shape for rendering.
/// Used for demos when sprite assets are not available.
/// </summary>
public abstract class ProceduralShape
{
    /// <summary>
    /// Gets the size of this shape.
    /// </summary>
    public Vector2 Size { get; protected set; }

    /// <summary>
    /// Gets the color of this shape.
    /// </summary>
    public Color Color { get; protected set; }

    /// <summary>
    /// Renders this shape at the specified position.
    /// </summary>
    /// <param name="renderBackend">Render backend to draw with.</param>
    /// <param name="position">World position to draw at.</param>
    /// <param name="rotation">Rotation in degrees (optional).</param>
    /// <param name="scale">Scale factor (optional).</param>
    public abstract void Render(IRenderer2D renderBackend, Vector2 position, float rotation = 0f, float scale = 1f);
}

/// <summary>
/// A simple colored rectangle shape.
/// </summary>
public sealed class RectangleShape : ProceduralShape
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RectangleShape"/> class.
    /// </summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="color">Fill color.</param>
    public RectangleShape(float width, float height, Color color)
    {
        Size = new Vector2(width, height);
        Color = color;
    }

    /// <inheritdoc/>
    public override void Render(IRenderer2D renderBackend, Vector2 position, float rotation = 0f, float scale = 1f)
    {
        var scaledSize = new Vector2(Size.X * scale, Size.Y * scale);
        renderBackend.DrawRectangle(position, scaledSize, Color);
    }
}

/// <summary>
/// A simple colored circle shape.
/// </summary>
public sealed class CircleShape : ProceduralShape
{
    private readonly float _radius;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircleShape"/> class.
    /// </summary>
    /// <param name="radius">Radius in pixels.</param>
    /// <param name="color">Fill color.</param>
    public CircleShape(float radius, Color color)
    {
        _radius = radius;
        Size = new Vector2(radius * 2, radius * 2);
        Color = color;
    }

    /// <inheritdoc/>
    public override void Render(IRenderer2D renderBackend, Vector2 position, float rotation = 0f, float scale = 1f)
    {
        float scaledRadius = _radius * scale;
        var center = new Vector2(position.X + scaledRadius, position.Y + scaledRadius);
        renderBackend.DrawCircle(center, scaledRadius, Color);
    }
}

/// <summary>
/// A rectangle with a border.
/// </summary>
public sealed class BorderedRectangleShape : ProceduralShape
{
    private readonly Color _borderColor;
    private readonly float _borderWidth;

    /// <summary>
    /// Initializes a new instance of the <see cref="BorderedRectangleShape"/> class.
    /// </summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="fillColor">Interior color.</param>
    /// <param name="borderColor">Border color.</param>
    /// <param name="borderWidth">Border width in pixels.</param>
    public BorderedRectangleShape(float width, float height, Color fillColor, Color borderColor, float borderWidth = 2f)
    {
        Size = new Vector2(width, height);
        Color = fillColor;
        _borderColor = borderColor;
        _borderWidth = borderWidth;
    }

    /// <inheritdoc/>
    public override void Render(IRenderer2D renderBackend, Vector2 position, float rotation = 0f, float scale = 1f)
    {
        var scaledSize = new Vector2(Size.X * scale, Size.Y * scale);
        var scaledBorder = _borderWidth * scale;

        // Draw border
        renderBackend.DrawRectangle(position, scaledSize, _borderColor);

        // Draw fill (inner rectangle)
        var innerPos = new Vector2(position.X + scaledBorder, position.Y + scaledBorder);
        var innerSize = new Vector2(scaledSize.X - scaledBorder * 2, scaledSize.Y - scaledBorder * 2);
        renderBackend.DrawRectangle(innerPos, innerSize, Color);
    }
}

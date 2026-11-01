using System.Numerics;

namespace MicroEngine;

/// <summary>Axis-aligned rectangle in world or screen units (Y grows downward).</summary>
/// <param name="X">Left edge.</param>
/// <param name="Y">Top edge.</param>
/// <param name="Width">Width; rectangles with zero or negative size contain nothing.</param>
/// <param name="Height">Height.</param>
public readonly record struct RectF(float X, float Y, float Width, float Height)
{
    /// <summary>The left edge (<see cref="X"/>).</summary>
    public float Left => X;
    /// <summary>The top edge (<see cref="Y"/>).</summary>
    public float Top => Y;
    /// <summary>The right edge, <c>X + Width</c> (exclusive).</summary>
    public float Right => X + Width;
    /// <summary>The bottom edge, <c>Y + Height</c> (exclusive).</summary>
    public float Bottom => Y + Height;
    /// <summary>The top-left corner.</summary>
    public Vector2 Position => new(X, Y);
    /// <summary>Width and height as a vector.</summary>
    public Vector2 Size => new(Width, Height);
    /// <summary>The centre point.</summary>
    public Vector2 Center => new(X + Width / 2f, Y + Height / 2f);

    /// <summary>A rectangle of the given size centred on <paramref name="center"/>.</summary>
    public static RectF FromCenter(Vector2 center, float width, float height)
        => new(center.X - width / 2f, center.Y - height / 2f, width, height);

    /// <summary>Strict overlap: rectangles that only share an edge do not intersect.</summary>
    public bool Intersects(RectF other)
        => X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;

    /// <summary>Whether <paramref name="point"/> is inside; the left and top edges are inside, the right and bottom edges are not.</summary>
    public bool Contains(Vector2 point)
        => point.X >= X && point.X < Right && point.Y >= Y && point.Y < Bottom;

    /// <summary>A copy moved by (<paramref name="dx"/>, <paramref name="dy"/>).</summary>
    public RectF Offset(float dx, float dy) => this with { X = X + dx, Y = Y + dy };
    /// <summary>A copy moved by <paramref name="delta"/>.</summary>
    public RectF Offset(Vector2 delta) => Offset(delta.X, delta.Y);
    /// <summary>A copy grown by <paramref name="amount"/> on every side (shrunk when negative).</summary>
    public RectF Inflate(float amount) => new(X - amount, Y - amount, Width + amount * 2, Height + amount * 2);

    /// <summary>The overlapping area of both rectangles, or <c>default</c> (all zeros) when they do not overlap.</summary>
    public RectF Intersection(RectF other)
    {
        float x = MathF.Max(X, other.X), y = MathF.Max(Y, other.Y);
        float r = MathF.Min(Right, other.Right), b = MathF.Min(Bottom, other.Bottom);
        return r <= x || b <= y ? default : new RectF(x, y, r - x, b - y);
    }
}

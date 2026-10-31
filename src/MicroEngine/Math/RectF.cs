using System.Numerics;

namespace MicroEngine;

/// <summary>Axis-aligned rectangle in world or screen units (Y grows downward).</summary>
public readonly record struct RectF(float X, float Y, float Width, float Height)
{
    public float Left => X;
    public float Top => Y;
    public float Right => X + Width;
    public float Bottom => Y + Height;
    public Vector2 Position => new(X, Y);
    public Vector2 Size => new(Width, Height);
    public Vector2 Center => new(X + Width / 2f, Y + Height / 2f);

    public static RectF FromCenter(Vector2 center, float width, float height)
        => new(center.X - width / 2f, center.Y - height / 2f, width, height);

    /// <summary>Strict overlap: rectangles that only share an edge do not intersect.</summary>
    public bool Intersects(RectF other)
        => X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;

    public bool Contains(Vector2 point)
        => point.X >= X && point.X < Right && point.Y >= Y && point.Y < Bottom;

    public RectF Offset(float dx, float dy) => this with { X = X + dx, Y = Y + dy };
    public RectF Offset(Vector2 delta) => Offset(delta.X, delta.Y);
    public RectF Inflate(float amount) => new(X - amount, Y - amount, Width + amount * 2, Height + amount * 2);

    public RectF Intersection(RectF other)
    {
        float x = MathF.Max(X, other.X), y = MathF.Max(Y, other.Y);
        float r = MathF.Min(Right, other.Right), b = MathF.Min(Bottom, other.Bottom);
        return r <= x || b <= y ? default : new RectF(x, y, r - x, b - y);
    }
}

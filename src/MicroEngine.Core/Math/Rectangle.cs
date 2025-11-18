namespace MicroEngine.Core.Math;

/// <summary>
/// Represents a rectangle with position and size.
/// </summary>
public readonly struct Rectangle : IEquatable<Rectangle>
{
    /// <summary>
    /// X coordinate of the top-left corner.
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// Y coordinate of the top-left corner.
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// Width of the rectangle.
    /// </summary>
    public float Width { get; init; }

    /// <summary>
    /// Height of the rectangle.
    /// </summary>
    public float Height { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rectangle"/> struct.
    /// </summary>
    public Rectangle(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets the center point of the rectangle.
    /// </summary>
    public Vector2 Center => new(X + Width / 2f, Y + Height / 2f);

    /// <summary>
    /// Gets the top-left corner.
    /// </summary>
    public Vector2 Position => new(X, Y);

    /// <summary>
    /// Gets the size of the rectangle.
    /// </summary>
    public Vector2 Size => new(Width, Height);

    /// <summary>
    /// Gets the right edge X coordinate.
    /// </summary>
    public float Right => X + Width;

    /// <summary>
    /// Gets the bottom edge Y coordinate.
    /// </summary>
    public float Bottom => Y + Height;

    /// <summary>
    /// Checks if this rectangle contains a point.
    /// </summary>
    public bool Contains(Vector2 point)
    {
        return point.X >= X && point.X <= Right &&
               point.Y >= Y && point.Y <= Bottom;
    }

    /// <summary>
    /// Checks if this rectangle intersects with another rectangle.
    /// </summary>
    public bool Intersects(Rectangle other)
    {
        return X < other.Right && Right > other.X &&
               Y < other.Bottom && Bottom > other.Y;
    }

    /// <inheritdoc/>
    public bool Equals(Rectangle other)
    {
        return MathHelper.ApproximatelyEqual(X, other.X) && 
               MathHelper.ApproximatelyEqual(Y, other.Y) &&
               MathHelper.ApproximatelyEqual(Width, other.Width) && 
               MathHelper.ApproximatelyEqual(Height, other.Height);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Rectangle other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

    /// <inheritdoc/>
    public override string ToString() => $"Rectangle({X}, {Y}, {Width}, {Height})";

    /// <summary>Equality operator.</summary>
    public static bool operator ==(Rectangle left, Rectangle right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(Rectangle left, Rectangle right) => !left.Equals(right);

    /// <summary>Zero rectangle.</summary>
    public static readonly Rectangle Zero = new(0, 0, 0, 0);
}

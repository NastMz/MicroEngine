namespace MicroEngine.Core.Math;

/// <summary>
/// Represents a two-dimensional vector with single-precision floating-point components.
/// </summary>
public readonly struct Vector2 : IEquatable<Vector2>
{
    /// <summary>
    /// Gets the X component of the vector.
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// Gets the Y component of the vector.
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Vector2"/> struct.
    /// </summary>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Vector2"/> struct with the same value for all components.
    /// </summary>
    /// <param name="value">The value for all components.</param>
    public Vector2(float value)
    {
        X = value;
        Y = value;
    }

    /// <summary>
    /// Gets a vector with all components set to zero.
    /// </summary>
    public static Vector2 Zero => new(0f, 0f);

    /// <summary>
    /// Gets a vector with all components set to one.
    /// </summary>
    public static Vector2 One => new(1f, 1f);

    /// <summary>
    /// Gets the unit vector for the X axis (1, 0).
    /// </summary>
    public static Vector2 UnitX => new(1f, 0f);

    /// <summary>
    /// Gets the unit vector for the Y axis (0, 1).
    /// </summary>
    public static Vector2 UnitY => new(0f, 1f);

    /// <summary>
    /// Gets the magnitude (length) of the vector.
    /// </summary>
    public float Magnitude => MathF.Sqrt(X * X + Y * Y);

    /// <summary>
    /// Gets the squared magnitude of the vector.
    /// Useful for comparing distances without the cost of a square root.
    /// </summary>
    public float SqrMagnitude => X * X + Y * Y;

    /// <summary>
    /// Returns a normalized version of this vector (magnitude = 1).
    /// </summary>
    public Vector2 Normalized
    {
        get
        {
            float mag = Magnitude;
            return mag > MathConstants.EPSILON ? this / mag : Zero;
        }
    }

    /// <summary>
    /// Adds two vectors component-wise.
    /// </summary>
    public static Vector2 operator +(Vector2 left, Vector2 right) =>
        new(left.X + right.X, left.Y + right.Y);

    /// <summary>
    /// Subtracts one vector from another component-wise.
    /// </summary>
    public static Vector2 operator -(Vector2 left, Vector2 right) =>
        new(left.X - right.X, left.Y - right.Y);

    /// <summary>
    /// Negates a vector.
    /// </summary>
    public static Vector2 operator -(Vector2 value) =>
        new(-value.X, -value.Y);

    /// <summary>
    /// Multiplies a vector by a scalar.
    /// </summary>
    public static Vector2 operator *(Vector2 vector, float scalar) =>
        new(vector.X * scalar, vector.Y * scalar);

    /// <summary>
    /// Multiplies a scalar by a vector.
    /// </summary>
    public static Vector2 operator *(float scalar, Vector2 vector) =>
        new(vector.X * scalar, vector.Y * scalar);

    /// <summary>
    /// Multiplies two vectors component-wise.
    /// </summary>
    public static Vector2 operator *(Vector2 left, Vector2 right) =>
        new(left.X * right.X, left.Y * right.Y);

    /// <summary>
    /// Divides a vector by a scalar.
    /// </summary>
    public static Vector2 operator /(Vector2 vector, float scalar) =>
        new(vector.X / scalar, vector.Y / scalar);

    /// <summary>
    /// Divides two vectors component-wise.
    /// </summary>
    public static Vector2 operator /(Vector2 left, Vector2 right) =>
        new(left.X / right.X, left.Y / right.Y);

    /// <summary>
    /// Determines whether two vectors are equal.
    /// </summary>
    public static bool operator ==(Vector2 left, Vector2 right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two vectors are not equal.
    /// </summary>
    public static bool operator !=(Vector2 left, Vector2 right) =>
        !left.Equals(right);

    /// <summary>
    /// Calculates the dot product of two vectors.
    /// </summary>
    public static float Dot(Vector2 left, Vector2 right) =>
        left.X * right.X + left.Y * right.Y;

    /// <summary>
    /// Calculates the distance between two vectors.
    /// </summary>
    public static float Distance(Vector2 a, Vector2 b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Calculates the squared distance between two vectors.
    /// </summary>
    public static float DistanceSquared(Vector2 a, Vector2 b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// Linearly interpolates between two vectors.
    /// </summary>
    /// <param name="a">The start vector.</param>
    /// <param name="b">The end vector.</param>
    /// <param name="t">The interpolation factor (0 to 1).</param>
    public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
    {
        t = System.Math.Clamp(t, 0f, 1f);
        return new Vector2(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t
        );
    }

    /// <summary>
    /// Reflects a vector off a surface with the given normal.
    /// </summary>
    public static Vector2 Reflect(Vector2 vector, Vector2 normal)
    {
        float dot = Dot(vector, normal);
        return vector - 2f * dot * normal;
    }

    /// <summary>
    /// Returns a vector with the minimum components of two vectors.
    /// </summary>
    public static Vector2 Min(Vector2 a, Vector2 b) =>
        new(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y));

    /// <summary>
    /// Returns a vector with the maximum components of two vectors.
    /// </summary>
    public static Vector2 Max(Vector2 a, Vector2 b) =>
        new(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y));

    /// <summary>
    /// Clamps a vector's components between minimum and maximum values.
    /// </summary>
    public static Vector2 Clamp(Vector2 value, Vector2 min, Vector2 max) =>
        new(
            System.Math.Clamp(value.X, min.X, max.X),
            System.Math.Clamp(value.Y, min.Y, max.Y)
        );

    /// <inheritdoc/>
    public bool Equals(Vector2 other)
    {
        return MathF.Abs(X - other.X) < MathConstants.EPSILON &&
               MathF.Abs(Y - other.Y) < MathConstants.EPSILON;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is Vector2 vector && Equals(vector);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(X, Y);

    /// <inheritdoc/>
    public override string ToString() =>
        $"({X:F2}, {Y:F2})";
}

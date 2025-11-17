namespace MicroEngine.Core.Math;

/// <summary>
/// Represents a three-dimensional vector with single-precision floating-point components.
/// </summary>
public readonly struct Vector3 : IEquatable<Vector3>
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
    /// Gets the Z component of the vector.
    /// </summary>
    public float Z { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Vector3"/> struct.
    /// </summary>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <param name="z">The Z component.</param>
    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Vector3"/> struct with the same value for all components.
    /// </summary>
    /// <param name="value">The value for all components.</param>
    public Vector3(float value)
    {
        X = value;
        Y = value;
        Z = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Vector3"/> struct from a Vector2 and Z component.
    /// </summary>
    /// <param name="xy">The XY components.</param>
    /// <param name="z">The Z component.</param>
    public Vector3(Vector2 xy, float z)
    {
        X = xy.X;
        Y = xy.Y;
        Z = z;
    }

    /// <summary>
    /// Gets a vector with all components set to zero.
    /// </summary>
    public static Vector3 Zero => new(0f, 0f, 0f);

    /// <summary>
    /// Gets a vector with all components set to one.
    /// </summary>
    public static Vector3 One => new(1f, 1f, 1f);

    /// <summary>
    /// Gets the unit vector for the X axis (1, 0, 0).
    /// </summary>
    public static Vector3 UnitX => new(1f, 0f, 0f);

    /// <summary>
    /// Gets the unit vector for the Y axis (0, 1, 0).
    /// </summary>
    public static Vector3 UnitY => new(0f, 1f, 0f);

    /// <summary>
    /// Gets the unit vector for the Z axis (0, 0, 1).
    /// </summary>
    public static Vector3 UnitZ => new(0f, 0f, 1f);

    /// <summary>
    /// Gets the forward direction vector (0, 0, 1).
    /// </summary>
    public static Vector3 Forward => new(0f, 0f, 1f);

    /// <summary>
    /// Gets the backward direction vector (0, 0, -1).
    /// </summary>
    public static Vector3 Backward => new(0f, 0f, -1f);

    /// <summary>
    /// Gets the up direction vector (0, 1, 0).
    /// </summary>
    public static Vector3 Up => new(0f, 1f, 0f);

    /// <summary>
    /// Gets the down direction vector (0, -1, 0).
    /// </summary>
    public static Vector3 Down => new(0f, -1f, 0f);

    /// <summary>
    /// Gets the right direction vector (1, 0, 0).
    /// </summary>
    public static Vector3 Right => new(1f, 0f, 0f);

    /// <summary>
    /// Gets the left direction vector (-1, 0, 0).
    /// </summary>
    public static Vector3 Left => new(-1f, 0f, 0f);

    /// <summary>
    /// Gets the magnitude (length) of the vector.
    /// </summary>
    public float Magnitude => MathF.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>
    /// Gets the squared magnitude of the vector.
    /// </summary>
    public float SqrMagnitude => X * X + Y * Y + Z * Z;

    /// <summary>
    /// Returns a normalized version of this vector (magnitude = 1).
    /// </summary>
    public Vector3 Normalized
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
    public static Vector3 operator +(Vector3 left, Vector3 right) =>
        new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    /// <summary>
    /// Subtracts one vector from another component-wise.
    /// </summary>
    public static Vector3 operator -(Vector3 left, Vector3 right) =>
        new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    /// <summary>
    /// Negates a vector.
    /// </summary>
    public static Vector3 operator -(Vector3 value) =>
        new(-value.X, -value.Y, -value.Z);

    /// <summary>
    /// Multiplies a vector by a scalar.
    /// </summary>
    public static Vector3 operator *(Vector3 vector, float scalar) =>
        new(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);

    /// <summary>
    /// Multiplies a scalar by a vector.
    /// </summary>
    public static Vector3 operator *(float scalar, Vector3 vector) =>
        new(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);

    /// <summary>
    /// Multiplies two vectors component-wise.
    /// </summary>
    public static Vector3 operator *(Vector3 left, Vector3 right) =>
        new(left.X * right.X, left.Y * right.Y, left.Z * right.Z);

    /// <summary>
    /// Divides a vector by a scalar.
    /// </summary>
    public static Vector3 operator /(Vector3 vector, float scalar) =>
        new(vector.X / scalar, vector.Y / scalar, vector.Z / scalar);

    /// <summary>
    /// Divides two vectors component-wise.
    /// </summary>
    public static Vector3 operator /(Vector3 left, Vector3 right) =>
        new(left.X / right.X, left.Y / right.Y, left.Z / right.Z);

    /// <summary>
    /// Determines whether two vectors are equal.
    /// </summary>
    public static bool operator ==(Vector3 left, Vector3 right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two vectors are not equal.
    /// </summary>
    public static bool operator !=(Vector3 left, Vector3 right) =>
        !left.Equals(right);

    /// <summary>
    /// Calculates the dot product of two vectors.
    /// </summary>
    public static float Dot(Vector3 left, Vector3 right) =>
        left.X * right.X + left.Y * right.Y + left.Z * right.Z;

    /// <summary>
    /// Calculates the cross product of two vectors.
    /// </summary>
    public static Vector3 Cross(Vector3 left, Vector3 right) =>
        new(
            left.Y * right.Z - left.Z * right.Y,
            left.Z * right.X - left.X * right.Z,
            left.X * right.Y - left.Y * right.X
        );

    /// <summary>
    /// Calculates the distance between two vectors.
    /// </summary>
    public static float Distance(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        float dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// Calculates the squared distance between two vectors.
    /// </summary>
    public static float DistanceSquared(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        float dz = a.Z - b.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    /// <summary>
    /// Linearly interpolates between two vectors.
    /// </summary>
    /// <param name="a">The start vector.</param>
    /// <param name="b">The end vector.</param>
    /// <param name="t">The interpolation factor (0 to 1).</param>
    public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
    {
        t = System.Math.Clamp(t, 0f, 1f);
        return new Vector3(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t
        );
    }

    /// <summary>
    /// Reflects a vector off a surface with the given normal.
    /// </summary>
    public static Vector3 Reflect(Vector3 vector, Vector3 normal)
    {
        float dot = Dot(vector, normal);
        return vector - 2f * dot * normal;
    }

    /// <summary>
    /// Returns a vector with the minimum components of two vectors.
    /// </summary>
    public static Vector3 Min(Vector3 a, Vector3 b) =>
        new(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y), MathF.Min(a.Z, b.Z));

    /// <summary>
    /// Returns a vector with the maximum components of two vectors.
    /// </summary>
    public static Vector3 Max(Vector3 a, Vector3 b) =>
        new(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y), MathF.Max(a.Z, b.Z));

    /// <summary>
    /// Clamps a vector's components between minimum and maximum values.
    /// </summary>
    public static Vector3 Clamp(Vector3 value, Vector3 min, Vector3 max) =>
        new(
            System.Math.Clamp(value.X, min.X, max.X),
            System.Math.Clamp(value.Y, min.Y, max.Y),
            System.Math.Clamp(value.Z, min.Z, max.Z)
        );

    /// <inheritdoc/>
    public bool Equals(Vector3 other)
    {
        return MathF.Abs(X - other.X) < MathConstants.EPSILON &&
               MathF.Abs(Y - other.Y) < MathConstants.EPSILON &&
               MathF.Abs(Z - other.Z) < MathConstants.EPSILON;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is Vector3 vector && Equals(vector);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(X, Y, Z);

    /// <inheritdoc/>
    public override string ToString() =>
        $"({X:F2}, {Y:F2}, {Z:F2})";
}

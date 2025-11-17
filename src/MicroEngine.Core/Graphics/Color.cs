using MicroEngine.Core.Math;

namespace MicroEngine.Core.Graphics;

/// <summary>
/// Color structure with RGBA components.
/// </summary>
public readonly struct Color : IEquatable<Color>
{
    /// <summary>
    /// Red component (0-255).
    /// </summary>
    public byte R { get; init; }

    /// <summary>
    /// Green component (0-255).
    /// </summary>
    public byte G { get; init; }

    /// <summary>
    /// Blue component (0-255).
    /// </summary>
    public byte B { get; init; }

    /// <summary>
    /// Alpha component (0-255).
    /// </summary>
    public byte A { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Color"/> struct.
    /// </summary>
    public Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>White color (255, 255, 255, 255).</summary>
    public static readonly Color White = new(255, 255, 255);
    /// <summary>Black color (0, 0, 0, 255).</summary>
    public static readonly Color Black = new(0, 0, 0);
    /// <summary>Red color (255, 0, 0, 255).</summary>
    public static readonly Color Red = new(255, 0, 0);
    /// <summary>Green color (0, 255, 0, 255).</summary>
    public static readonly Color Green = new(0, 255, 0);
    /// <summary>Blue color (0, 0, 255, 255).</summary>
    public static readonly Color Blue = new(0, 0, 255);
    /// <summary>Yellow color (255, 255, 0, 255).</summary>
    public static readonly Color Yellow = new(255, 255, 0);
    /// <summary>Magenta color (255, 0, 255, 255).</summary>
    public static readonly Color Magenta = new(255, 0, 255);
    /// <summary>Cyan color (0, 255, 255, 255).</summary>
    public static readonly Color Cyan = new(0, 255, 255);
    /// <summary>Gray color (128, 128, 128, 255).</summary>
    public static readonly Color Gray = new(128, 128, 128);
    /// <summary>Dark gray color (64, 64, 64, 255).</summary>
    public static readonly Color DarkGray = new(64, 64, 64);
    /// <summary>Light gray color (192, 192, 192, 255).</summary>
    public static readonly Color LightGray = new(192, 192, 192);
    /// <summary>Transparent color (0, 0, 0, 0).</summary>
    public static readonly Color Transparent = new(0, 0, 0, 0);

    /// <inheritdoc/>
    public bool Equals(Color other)
    {
        return R == other.R && G == other.G && B == other.B && A == other.A;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is Color other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B, A);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Color({R}, {G}, {B}, {A})";
    }

    /// <summary>Equality operator.</summary>
    public static bool operator ==(Color left, Color right)
    {
        return left.Equals(right);
    }

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(Color left, Color right)
    {
        return !left.Equals(right);
    }
}

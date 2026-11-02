using System.Numerics;
using System.Runtime.InteropServices;

namespace Arch1.Graphics;

/// <summary>
/// A colour with float channels in 0..1. Everywhere in the graphics contract colours are <b>premultiplied</b>
/// (R, G and B already multiplied by A) unless a description says <see cref="AlphaMode.Straight"/>.
/// </summary>
/// <param name="R">Red, premultiplied.</param>
/// <param name="G">Green, premultiplied.</param>
/// <param name="B">Blue, premultiplied.</param>
/// <param name="A">Alpha (coverage).</param>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct ColorF(float R, float G, float B, float A)
{
    /// <summary>Opaque white: the neutral tint.</summary>
    public static readonly ColorF White = new(1, 1, 1, 1);

    /// <summary>All channels zero.</summary>
    public static readonly ColorF Transparent = new(0, 0, 0, 0);

    /// <summary>A colour from 8-bit channels (already premultiplied): each channel is <c>byte / 255</c>.</summary>
    public static ColorF FromBytes(byte r, byte g, byte b, byte a = 255) => new(r / 255f, g / 255f, b / 255f, a / 255f);
}

/// <summary>An integer rectangle in pixels: origin top-left, x to the right, y down; right and bottom exclusive.</summary>
/// <param name="X">Left edge (inclusive).</param>
/// <param name="Y">Top edge (inclusive).</param>
/// <param name="Width">Width in pixels (never negative).</param>
/// <param name="Height">Height in pixels (never negative).</param>
public readonly record struct RectI(int X, int Y, int Width, int Height)
{
    /// <summary>Right edge (exclusive).</summary>
    public int Right => X + Width;

    /// <summary>Bottom edge (exclusive).</summary>
    public int Bottom => Y + Height;

    /// <summary>True when the rectangle covers no pixel.</summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>True when <paramref name="other"/> lies entirely inside this rectangle.</summary>
    public bool Contains(RectI other) => other.X >= X && other.Y >= Y && other.Right <= Right && other.Bottom <= Bottom;

    /// <summary>The overlap of two rectangles (empty when they do not overlap).</summary>
    public RectI Intersect(RectI other)
    {
        var x = Math.Max(X, other.X);
        var y = Math.Max(Y, other.Y);
        var r = Math.Min(Right, other.Right);
        var b = Math.Min(Bottom, other.Bottom);
        return r <= x || b <= y ? new RectI(x, y, 0, 0) : new RectI(x, y, r - x, b - y);
    }
}

/// <summary>A float rectangle (texel or world units): origin top-left, y down.</summary>
/// <param name="X">Left edge.</param>
/// <param name="Y">Top edge.</param>
/// <param name="Width">Width.</param>
/// <param name="Height">Height.</param>
public readonly record struct RectF(float X, float Y, float Width, float Height);

/// <summary>
/// The vertex of the built-in pipelines (<see cref="BuiltInPipeline"/>), 32 bytes: position in target pixels relative
/// to the viewport origin (y down), texture coordinate normalised to 0..1 over the bound texture, premultiplied colour
/// that multiplies the texel. The layout is fixed and identical on every tier: <see cref="VertexLayout.Vertex2D"/>.
/// </summary>
/// <param name="Position">Pixels, relative to the viewport origin, y down. Pixel (x, y) has its centre at (x + 0.5, y + 0.5).</param>
/// <param name="TexCoord">Normalised texture coordinate; (0, 0) is the top-left corner of texel (0, 0).</param>
/// <param name="Color">Premultiplied colour multiplied with the sampled texel.</param>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Vertex2D(Vector2 Position, Vector2 TexCoord, ColorF Color)
{
    /// <summary>Size of one vertex in bytes (the stride of every built-in vertex buffer).</summary>
    public const int SizeInBytes = 32;
}

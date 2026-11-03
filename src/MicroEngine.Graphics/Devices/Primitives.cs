using System.Numerics;
using System.Runtime.InteropServices;

namespace MicroEngine.Graphics.Devices;

/// <summary>
/// The graphics device contract this assembly implements. The contract text is the XML documentation of the types in
/// <c>MicroEngine.Graphics.Devices</c>; every rule it states is checked by the device conformance suite. A change to a
/// rule or to the shape of a type is a contract revision: it raises <see cref="Version"/> and records its evidence.
/// </summary>
/// <remarks>
/// Written rules (each is repeated on the member it governs):
/// <list type="bullet">
/// <item>Colours are premultiplied (<see cref="ColorF"/> channels already multiplied by alpha) and stored as written:
/// the device applies no transfer function to <c>Rgba8Unorm</c> data.</item>
/// <item>Built-in vertices (<see cref="Vertex2D"/>) are target pixels relative to the viewport origin, y down; pixel
/// (x, y) is the square [x, x + 1) × [y, y + 1) with its centre at (x + 0.5, y + 0.5).</item>
/// <item>Coverage: a triangle covers exactly the pixels whose centre lies inside it; a centre on an edge belongs to the
/// triangle when the edge is a top or a left edge (the top-left rule of Direct3D, Vulkan, Metal and WebGPU).</item>
/// <item>Clears (<see cref="LoadOp.Clear"/>) ignore viewport and scissor; scissor is absolute in target pixels.</item>
/// <item>Bindings, pipeline, viewport and scissor are pass-scoped; uniform data persist for the rest of the list.</item>
/// <item>Command lists execute in <see cref="IGraphicsDevice.Submit"/> order and, within a list, in recording order
/// (updates included); <see cref="IGraphicsDevice.ReadPixels"/> observes every list submitted before it.</item>
/// <item>Surface targets expire at <see cref="IGraphicsDevice.Present"/> and cannot be sampled or read back.</item>
/// <item>Numeric conventions: System.Numerics right-handed matrices uploaded as-is, <c>mul(M, v)</c> in HLSL,
/// counter-clockwise front faces, depth 0 (near) .. 1 (far), NDC y up, row 0 of a target at the top.</item>
/// </list>
/// </remarks>
public static class GraphicsContract
{
    /// <summary>The contract version: 1 (frozen; the built-in pipeline is <see cref="BuiltInPipeline.Textured"/>).</summary>
    public const int Version = 1;
}

/// <summary>An integer rectangle in pixels: origin top-left, x to the right, y down; right and bottom exclusive.</summary>
/// <param name="X">Left edge (inclusive).</param>
/// <param name="Y">Top edge (inclusive).</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
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

/// <summary>
/// The vertex of the built-in pipeline (<see cref="BuiltInPipeline.Textured"/>), 32 bytes: position in target pixels
/// relative to the viewport origin (y down), texture coordinate normalised to 0..1 over the bound texture, premultiplied
/// colour that multiplies the texel. The layout is fixed and identical on every tier: <see cref="VertexLayout.Vertex2D"/>.
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

/// <summary>Conversions between 8-bit colours and the premultiplied float colours of the device contract.</summary>
public static class PremultipliedColor
{
    /// <summary>A colour from 8-bit channels that are already premultiplied: each channel is <c>byte / 255</c>.</summary>
    public static ColorF FromBytes(byte r, byte g, byte b, byte a = 255) => new(r / 255f, g / 255f, b / 255f, a / 255f);

    /// <summary>
    /// The premultiplied form of a straight (not premultiplied) 8-bit colour, as the 2D layer passes tints to the device:
    /// each colour channel <c>c / 255 × a / 255</c>.
    /// </summary>
    public static ColorF FromStraight(Color color)
    {
        float a = color.A / 255f;
        return new(color.R / 255f * a, color.G / 255f * a, color.B / 255f * a, a);
    }

    /// <summary>
    /// Premultiplies straight 8-bit RGBA texels in place, as an upload of straight-alpha image data does (each colour
    /// channel becomes <c>round(c × a / 255)</c>). Opaque and fully transparent texels are exact.
    /// </summary>
    /// <param name="rgba">Tightly packed R, G, B, A bytes.</param>
    public static void PremultiplyInPlace(Span<byte> rgba)
    {
        for (int i = 0; i + 3 < rgba.Length; i += 4)
        {
            int a = rgba[i + 3];
            if (a == 255) continue;
            rgba[i] = Mul(rgba[i], a);
            rgba[i + 1] = Mul(rgba[i + 1], a);
            rgba[i + 2] = Mul(rgba[i + 2], a);
        }
    }

    private static byte Mul(int c, int a) => (byte)((c * a + 127) / 255);
}

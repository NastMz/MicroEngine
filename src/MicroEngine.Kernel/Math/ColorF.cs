namespace MicroEngine;

/// <summary>
/// A floating-point RGBA colour in linear light, for blending, lighting and anything that adds or scales colours.
/// Channels are not clamped (values above 1 are brighter than white). <see cref="Color"/> is the 8-bit sRGB form used
/// to store and name colours; convert with <see cref="FromSrgb"/> and <see cref="ToSrgb"/>.
/// </summary>
/// <remarks>
/// The colour is straight (not premultiplied) unless it came from <see cref="Premultiply"/>; nothing in the type records
/// which, so keep premultiplied values in variables that say so. Premultiplied colours blend and filter without dark
/// fringes: a red-to-transparent edge sampled halfway over white stays red.
/// </remarks>
/// <param name="R">Red in linear light, 0..1 for displayable colours.</param>
/// <param name="G">Green in linear light.</param>
/// <param name="B">Blue in linear light.</param>
/// <param name="A">Opacity, 0 (transparent) to 1 (opaque, the default).</param>
public readonly record struct ColorF(float R, float G, float B, float A = 1f)
{
    /// <summary>Fully transparent black (0, 0, 0, 0).</summary>
    public static readonly ColorF Transparent = new(0, 0, 0, 0);
    /// <summary>Opaque white (1, 1, 1, 1).</summary>
    public static readonly ColorF White = new(1, 1, 1);
    /// <summary>Opaque black (0, 0, 0, 1).</summary>
    public static readonly ColorF Black = new(0, 0, 0);

    /// <summary>
    /// The linear-light value of an 8-bit sRGB colour: each colour channel through the sRGB transfer function (IEC
    /// 61966-2-1), opacity divided by 255.
    /// </summary>
    public static ColorF FromSrgb(Color color) =>
        new(SrgbToLinear(color.R / 255f), SrgbToLinear(color.G / 255f), SrgbToLinear(color.B / 255f), color.A / 255f);

    /// <summary>
    /// The nearest 8-bit sRGB colour: each channel clamped to 0..1, encoded with the sRGB transfer function and rounded.
    /// <c>FromSrgb(c).ToSrgb()</c> returns <c>c</c> for every 8-bit colour.
    /// </summary>
    public Color ToSrgb() => new(Encode(LinearToSrgb(R)), Encode(LinearToSrgb(G)), Encode(LinearToSrgb(B)), Encode(A));

    /// <summary>The colour with its colour channels multiplied by its opacity: (R·A, G·A, B·A, A).</summary>
    public ColorF Premultiply() => new(R * A, G * A, B * A, A);

    /// <summary>
    /// The straight colour of a premultiplied one: colour channels divided by opacity. A fully transparent colour has no
    /// colour left to recover and gives <see cref="Transparent"/>.
    /// </summary>
    public ColorF Unpremultiply() => A == 0 ? Transparent : new(R / A, G / A, B / A, A);

    /// <summary>Linear interpolation of every channel; <paramref name="t"/> is clamped to 0..1.</summary>
    public static ColorF Lerp(ColorF a, ColorF b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t, a.A + (b.A - a.A) * t);
    }

    /// <summary>Every channel multiplied by <paramref name="factor"/>, opacity included.</summary>
    public static ColorF operator *(ColorF color, float factor) => new(color.R * factor, color.G * factor, color.B * factor, color.A * factor);

    /// <summary>Channel-by-channel product (modulation): a tint applied to a colour.</summary>
    public static ColorF operator *(ColorF a, ColorF b) => new(a.R * b.R, a.G * b.G, a.B * b.B, a.A * b.A);

    /// <summary>Channel-by-channel sum (additive light).</summary>
    public static ColorF operator +(ColorF a, ColorF b) => new(a.R + b.R, a.G + b.G, a.B + b.B, a.A + b.A);

    /// <summary>The sRGB transfer function decoded: an sRGB-encoded value (0..1) to linear light.</summary>
    public static float SrgbToLinear(float value) =>
        value <= 0.04045f ? value / 12.92f : MathF.Pow((value + 0.055f) / 1.055f, 2.4f);

    /// <summary>The sRGB transfer function: linear light (0..1) to an sRGB-encoded value.</summary>
    public static float LinearToSrgb(float value) =>
        value <= 0.0031308f ? value * 12.92f : 1.055f * MathF.Pow(value, 1f / 2.4f) - 0.055f;

    private static byte Encode(float value) => (byte)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f);
}

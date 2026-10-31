using System.Globalization;

namespace MicroEngine;

/// <summary>An 8-bit-per-channel, non-premultiplied RGBA color.</summary>
public readonly record struct Color(byte R, byte G, byte B, byte A = 255)
{
    public static readonly Color Transparent = new(0, 0, 0, 0);
    public static readonly Color White = new(255, 255, 255);
    public static readonly Color Black = new(0, 0, 0);

    /// <summary>Parses "#RRGGBB" or "#RRGGBBAA" (the leading '#' is optional).</summary>
    public static Color Hex(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);
        var digits = hex.TrimStart('#');
        if (digits.Length is not (6 or 8)) throw new FormatException($"Invalid color '{hex}'.");
        byte Part(int i) => byte.Parse(digits.AsSpan(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return new Color(Part(0), Part(2), Part(4), digits.Length == 8 ? Part(6) : (byte)255);
    }

    public Color WithAlpha(byte alpha) => this with { A = alpha };
    public Color WithAlpha(float alpha) => this with { A = (byte)Math.Clamp(alpha * 255f, 0f, 255f) };

    /// <summary>Linear interpolation of every channel.</summary>
    public static Color Lerp(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        static byte Mix(byte x, byte y, float t) => (byte)MathF.Round(x + (y - x) * t);
        return new Color(Mix(a.R, b.R, t), Mix(a.G, b.G, t), Mix(a.B, b.B, t), Mix(a.A, b.A, t));
    }

    /// <summary>Multiplies RGB by <paramref name="factor"/>; values above 1 brighten.</summary>
    public Color Shade(float factor)
    {
        static byte S(byte c, float f) => (byte)Math.Clamp(MathF.Round(c * f), 0f, 255f);
        return new Color(S(R, factor), S(G, factor), S(B, factor), A);
    }

    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";
}

using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace TrailDuel.Visuals;

/// <summary>
/// The fourteen species: hand-drawn 64x64 battle sprites from the Openmon collection (CC0, see
/// CREDITS.md) in <c>Content/creatures</c>. Every drawing faces left, so the opponent (top right) shows
/// it as drawn and your own creature (bottom left) is always mirrored to face right; even the species
/// that look head-on are turned slightly left. Bouldox and Fizzbit are recoloured to match Pebblit and
/// Voltiger, their evolution-line relatives.
/// </summary>
internal static class CreatureArt
{
    public const int Size = 64;

    /// <summary>The opponent-side sprite: the original drawing, facing left, recoloured where an evolution line asks for it.</summary>
    public static PixelImage Front(ContentFolder content, string speciesId)
    {
        var img = content.Image($"creatures/{speciesId}.png");
        return speciesId switch
        {
            "bouldox" => img.Map((_, _, c) => PebblitHide(c)),
            "fizzbit" => img.Map((_, _, c) => c.A > 0 && VoltigerCoat.TryGetValue(c with { A = 255 }, out var to) ? to with { A = c.A } : c),
            _ => img,
        };
    }

    // Pebblit's browns, dark to light, as (luminance, colour) stops.
    private static readonly (float L, Color C)[] PebblitRamp =
    [
        (20, Color.Hex("241a16")), (60, Color.Hex("463831")), (110, Color.Hex("806759")),
        (150, Color.Hex("af927e")), (190, Color.Hex("d7b69b")), (225, Color.Hex("ecd8c2")),
    ];

    /// <summary>
    /// Bouldox wears Pebblit's coffee browns in place of its blue-green plates and cool grey hide; the
    /// outline, white horn, red eye and saturated moss stay.
    /// </summary>
    private static Color PebblitHide(Color c)
    {
        if (c.A == 0) return c;
        var (hue, sat, lum) = Hsl(c);
        bool plate = hue is >= 2.9f and <= 3.5f && lum > 12;
        bool greyHide = hue is >= 1.5f and <= 3.5f && sat < 0.3f && lum > 12;
        if (!plate && !greyHide) return c;
        if (lum <= PebblitRamp[0].L) return PebblitRamp[0].C with { A = c.A };
        for (int i = 1; i < PebblitRamp.Length; i++)
            if (lum <= PebblitRamp[i].L)
            {
                var (l0, c0) = PebblitRamp[i - 1];
                var (l1, c1) = PebblitRamp[i];
                return Color.Lerp(c0, c1, (lum - l0) / (l1 - l0)) with { A = c.A };
            }
        return PebblitRamp[^1].C with { A = c.A };
    }

    /// <summary>Fizzbit in Voltiger's coat: purple quills turn charcoal, green tips storm yellow, cream fur grey with a tan face.</summary>
    private static readonly Dictionary<Color, Color> VoltigerCoat = new()
    {
        [Color.Hex("422950")] = Color.Hex("1f1c1d"), [Color.Hex("8038a8")] = Color.Hex("3a3537"),
        [Color.Hex("b060e0")] = Color.Hex("504b4d"), [Color.Hex("c485f1")] = Color.Hex("6e696b"),
        [Color.Hex("385640")] = Color.Hex("a88a00"), [Color.Hex("389e50")] = Color.Hex("e0b800"),
        [Color.Hex("4ed877")] = Color.Hex("fed800"),
        [Color.Hex("665346")] = Color.Hex("504b4d"), [Color.Hex("b89070")] = Color.Hex("877a7a"),
        [Color.Hex("d8b870")] = Color.Hex("9e9e9e"), [Color.Hex("f8e8a0")] = Color.Hex("e2d0b4"),
    };

    /// <summary>Hue (0..6), saturation (0..1) and perceived luminance (0..255) of a colour.</summary>
    private static (float Hue, float Sat, float Lum) Hsl(Color c)
    {
        float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f;
        float max = MathF.Max(r, MathF.Max(g, b)), min = MathF.Min(r, MathF.Min(g, b)), d = max - min;
        float lum = (c.R * 3 + c.G * 6 + c.B) / 10f;
        if (d <= 0 || max <= 0) return (0, 0, lum);
        float h = max == r ? (g - b) / d : max == g ? (b - r) / d + 2 : (r - g) / d + 4;
        return (h < 0 ? h + 6 : h, d / max, lum);
    }

    /// <summary>The player-side sprite: the original mirrored to face right.</summary>
    public static PixelImage Back(PixelImage front) => front.FlipX();

    /// <summary>Drawn height of each species' sprite in pixels, filled when the atlas is built (effects aim at the middle).</summary>
    public static Dictionary<string, int> Heights { get; } = new(StringComparer.Ordinal);

    /// <summary>Where a sprite stands: the middle of its feet (bottom of its opaque bounds).</summary>
    public static Vector2 Feet(PixelImage img)
    {
        var b = img.OpaqueBounds() ?? (0, 0, img.Width, img.Height);
        return new Vector2(b.X + b.Width / 2f, b.Y + b.Height);
    }
}

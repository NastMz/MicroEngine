using MicroEngine.Graphics;

namespace MicroEngine.UI;

/// <summary>Visual theme for the shared widgets so each game can keep its own identity.</summary>
/// <param name="Panel">Colours of panels and boxes.</param>
/// <param name="Text">Normal text.</param>
/// <param name="TextShadow">The 1px shadow under text.</param>
/// <param name="Accent">Highlights: the selected option, name tags, headings.</param>
/// <param name="Muted">Secondary or disabled text.</param>
public sealed record UiTheme(PanelStyle Panel, Color Text, Color TextShadow, Color Accent, Color Muted)
{
    /// <summary>The built-in dark blue theme with a warm yellow accent.</summary>
    public static readonly UiTheme Default = new(
        new PanelStyle(Color.Hex("1c1b2e"), Color.Hex("f4f1de"), Color.Hex("3a3a5c"), Color.Hex("00000080")),
        Color.Hex("f4f1de"), Color.Hex("0a0a14"), Color.Hex("f2cc60"), Color.Hex("8a8aa8"));

    /// <summary>The widgets' layout unit for <paramref name="font"/>: 1 for the built-in pixel font (10-pixel lines), proportionally more for larger fonts, never less than 1.</summary>
    internal static float Unit(IFont font) => MathF.Max(1, MathF.Round(font.LineHeight / 10f));
}

using System.Numerics;
using MicroEngine.Graphics;
using MicroEngine.Input;

namespace MicroEngine.UI;

/// <summary>Colours of prompt glyphs (<see cref="PromptText"/>): key caps, gamepad buttons and touch controls.</summary>
/// <param name="Cap">Fill of key caps and of gamepad buttons without a colour of their own.</param>
/// <param name="CapBorder">Outline of caps, pills and touch rings.</param>
/// <param name="CapText">Text on caps and buttons.</param>
public sealed record PromptGlyphStyle(Color Cap, Color CapBorder, Color CapText)
{
    /// <summary>Light caps with dark text, readable on most backgrounds.</summary>
    public static readonly PromptGlyphStyle Default = new(Color.Hex("f4f1de"), Color.Hex("0a0a14"), Color.Hex("1c1b2e"));

    /// <summary>Whether Xbox and Nintendo face buttons are filled with their usual colours (A green, B red, X blue, Y yellow).</summary>
    public bool ColoredFaceButtons { get; init; } = true;

    /// <summary>Caps in a theme's text colour, outlined with its shadow, labelled in its panel colour.</summary>
    public static PromptGlyphStyle From(UiTheme theme) => new(theme.Text, theme.TextShadow, theme.Panel.Fill);
}

/// <summary>
/// Draws text with action prompts as glyphs: <c>"Press {attack} to swing"</c> shows a key cap with Z on a keyboard, a
/// green A on an Xbox pad, a × button on a PlayStation pad and the touch button's label in a ring on a phone. Text that
/// does not name an action is drawn as with <see cref="BitmapFont.Draw(IRenderer, string, Vector2, Color, float)"/>. Glyphs are as tall as a line of the font.
/// The overloads taking a <see cref="BitmapFont"/> draw the built-in font's nine-pixel glyphs; those taking any
/// <see cref="IFont"/> size the glyphs by the font's line height, for scalable and shaped fonts.
/// </summary>
public static class PromptText
{
    /// <summary>Width of <paramref name="template"/> drawn by <c>Draw</c>, in pixels (one line).</summary>
    public static int Measure(BitmapFont font, ActionPrompts prompts, string template)
    {
        int width = 0;
        foreach (var (text, prompt) in prompts.Split(template))
            width += prompt is { } p ? GlyphWidth(font, p) + 2 : font.MeasureWidth(text) + font.Spacing;
        return Math.Max(0, width - font.Spacing);
    }

    /// <summary>Draws one line of <paramref name="template"/> with its top-left at <paramref name="position"/>.</summary>
    /// <param name="r">The renderer.</param>
    /// <param name="font">The font of the text and of the glyph labels.</param>
    /// <param name="prompts">Resolves the actions (<see cref="Core.Game.Prompts"/>).</param>
    /// <param name="template">Text with <c>{action}</c> names (see <see cref="ActionPrompts.Format"/>).</param>
    /// <param name="position">Top-left of the line, in virtual pixels.</param>
    /// <param name="color">Colour of the plain text.</param>
    /// <param name="shadow">A 1px shadow under the plain text; null draws none.</param>
    /// <param name="style">Glyph colours (<see cref="PromptGlyphStyle.Default"/> when null).</param>
    public static void Draw(IRenderer r, BitmapFont font, ActionPrompts prompts, string template, Vector2 position, Color color, Color? shadow = null, PromptGlyphStyle? style = null)
    {
        float x = MathF.Round(position.X), y = MathF.Round(position.Y);
        foreach (var (text, prompt) in prompts.Split(template))
        {
            if (prompt is { } p)
            {
                x += 1;
                DrawGlyph(r, font, p, new Vector2(x, y), style);
                x += GlyphWidth(font, p) + 1;
                continue;
            }
            if (shadow is { } s) font.DrawShadowed(r, text, new Vector2(x, y), color, s);
            else font.Draw(r, text, new Vector2(x, y), color);
            x += font.MeasureWidth(text) + font.Spacing;
        }
    }

    /// <summary>Draws <paramref name="template"/> centred horizontally on <paramref name="centerX"/>.</summary>
    public static void DrawCentered(IRenderer r, BitmapFont font, ActionPrompts prompts, string template, float centerX, float y, Color color, Color? shadow = null, PromptGlyphStyle? style = null)
        => Draw(r, font, prompts, template, new Vector2(MathF.Round(centerX - Measure(font, prompts, template) / 2f), y), color, shadow, style);

    /// <summary>Width of one prompt's glyph in pixels.</summary>
    public static int GlyphWidth(BitmapFont font, ActionPrompt prompt) => prompt.Kind switch
    {
        PromptKind.Label => font.MeasureWidth(prompt.Text),
        PromptKind.PadButton when IsRound(prompt) => 9,
        _ => Math.Max(9, font.MeasureWidth(prompt.Text) + 5),
    };

    /// <summary>
    /// Draws one prompt with its top-left at <paramref name="position"/>: a key cap for a key, a round button for a
    /// gamepad face button (in its colour on Xbox and Nintendo pads), a pill for other gamepad controls, a ring for a
    /// touch control, plain text for a label.
    /// </summary>
    public static void DrawGlyph(IRenderer r, BitmapFont font, ActionPrompt prompt, Vector2 position, PromptGlyphStyle? style = null)
    {
        var s = style ?? PromptGlyphStyle.Default;
        int w = GlyphWidth(font, prompt), h = 9;
        var box = new RectF(position.X, position.Y - 1, w, h);
        var textPos = new Vector2(MathF.Round(box.X + (w - font.MeasureWidth(prompt.Text)) / 2f), position.Y);
        switch (prompt.Kind)
        {
            case PromptKind.Key or PromptKind.Pointer:
                r.FillRect(box, s.CapBorder);
                r.FillRect(new RectF(box.X + 1, box.Y, w - 2, h - 1), s.Cap);
                r.FillRect(new RectF(box.X + 1, box.Bottom - 2, w - 2, 1), Color.Lerp(s.Cap, s.CapBorder, 0.35f));
                font.Draw(r, prompt.Text, textPos - new Vector2(0, 0), s.CapText);
                break;
            case PromptKind.PadButton when IsRound(prompt):
            {
                var center = new Vector2(box.X + 4.5f, box.Y + 4.5f);
                var fill = s.ColoredFaceButtons ? FaceColor(prompt) ?? s.Cap : s.Cap;
                r.FillCircle(center, 5f, s.CapBorder);
                r.FillCircle(center, 4f, fill);
                font.Draw(r, prompt.Text, textPos, fill == s.Cap ? s.CapText : Color.White);
                break;
            }
            case PromptKind.PadButton or PromptKind.PadAxis:
                Pill(r, box, s.CapBorder, s.Cap);
                font.Draw(r, prompt.Text, textPos, s.CapText);
                break;
            case PromptKind.Touch:
                Pill(r, box, s.Cap, s.CapBorder);
                font.Draw(r, prompt.Text, textPos, s.Cap);
                break;
            default:
                font.Draw(r, prompt.Text, position, s.Cap);
                break;
        }
    }

    private static bool IsRound(ActionPrompt p) => p.Kind == PromptKind.PadButton && p.Button is PadButton.A or PadButton.B or PadButton.X or PadButton.Y;

    private static Color? FaceColor(ActionPrompt p) => p.PadStyle switch
    {
        GamepadStyle.Xbox => p.Button switch
        {
            PadButton.A => Color.Hex("3f9a3c"),
            PadButton.B => Color.Hex("c8423a"),
            PadButton.X => Color.Hex("3a6fc8"),
            _ => Color.Hex("c9a227"),
        },
        _ => null,
    };

    private static void Pill(IRenderer r, RectF box, Color outline, Color fill)
    {
        float radius = box.Height / 2f;
        r.FillCircle(new Vector2(box.X + radius, box.Y + radius), radius, outline);
        r.FillCircle(new Vector2(box.Right - radius, box.Y + radius), radius, outline);
        r.FillRect(new RectF(box.X + radius, box.Y, box.Width - 2 * radius, box.Height), outline);
        r.FillCircle(new Vector2(box.X + radius, box.Y + radius), radius - 1, fill);
        r.FillCircle(new Vector2(box.Right - radius, box.Y + radius), radius - 1, fill);
        r.FillRect(new RectF(box.X + radius, box.Y + 1, box.Width - 2 * radius, box.Height - 2), fill);
    }

    // ---- any font

    /// <summary>
    /// Width of <paramref name="template"/> drawn by <see cref="Draw(IRenderer, IFont, ActionPrompts, string, Vector2, Color, Color?, PromptGlyphStyle?, bool)"/>
    /// with any font (a scalable or shaped font at a size), in the font's units (one line).
    /// </summary>
    public static float Measure(IFont font, ActionPrompts prompts, string template)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(prompts);
        float width = 0, gap = Unit(font);
        foreach (var (text, prompt) in prompts.Split(template))
            width += prompt is { } p ? GlyphWidth(font, p) + 2 * gap : font.MeasureWidth(text);
        return width;
    }

    /// <summary>
    /// Draws one line of <paramref name="template"/> with any font, its top-left at <paramref name="position"/>: the
    /// text and the labels of the glyphs in <paramref name="font"/>, the glyphs as tall as a line of it. Use it with
    /// the font of the game's language (a scalable font, or a shaped one for Arabic and Hebrew).
    /// </summary>
    /// <param name="r">The renderer.</param>
    /// <param name="font">The font of the text and of the glyph labels.</param>
    /// <param name="prompts">Resolves the actions (<see cref="Core.Game.Prompts"/>).</param>
    /// <param name="template">Text with <c>{action}</c> names (see <see cref="ActionPrompts.Format"/>).</param>
    /// <param name="position">Top-left of the line.</param>
    /// <param name="color">Colour of the plain text.</param>
    /// <param name="shadow">A shadow under the plain text; null draws none.</param>
    /// <param name="style">Glyph colours (<see cref="PromptGlyphStyle.Default"/> when null).</param>
    /// <param name="rightToLeft">
    /// Whether the pieces of the line follow each other from the right (a right-to-left language,
    /// <c>Game.Localization.RightToLeft</c>): the first piece ends at the line's right edge. The line takes the same
    /// width either way.
    /// </param>
    public static void Draw(IRenderer r, IFont font, ActionPrompts prompts, string template, Vector2 position, Color color, Color? shadow = null, PromptGlyphStyle? style = null, bool rightToLeft = false)
    {
        ArgumentNullException.ThrowIfNull(r);
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(prompts);
        float gap = Unit(font), y = MathF.Round(position.Y);
        float x = MathF.Round(position.X) + (rightToLeft ? Measure(font, prompts, template) : 0);
        foreach (var (text, prompt) in prompts.Split(template))
        {
            float width = prompt is { } glyph ? GlyphWidth(font, glyph) + 2 * gap : font.MeasureWidth(text);
            float left = rightToLeft ? x - width : x;
            if (prompt is { } p) DrawGlyph(r, font, p, new Vector2(left + gap, y), style);
            else if (shadow is { } s) font.DrawShadowed(r, text, new Vector2(left, y), color, s, new Vector2(gap));
            else font.Draw(r, text, new Vector2(left, y), color);
            x += rightToLeft ? -width : width;
        }
    }

    /// <summary>Draws <paramref name="template"/> with any font, centred horizontally on <paramref name="centerX"/>.</summary>
    public static void DrawCentered(IRenderer r, IFont font, ActionPrompts prompts, string template, float centerX, float y, Color color, Color? shadow = null, PromptGlyphStyle? style = null, bool rightToLeft = false)
        => Draw(r, font, prompts, template, new Vector2(MathF.Round(centerX - Measure(font, prompts, template) / 2f), y), color, shadow, style, rightToLeft);

    /// <summary>A tenth of the font's line: the outline of a glyph and the space around it.</summary>
    private static float Unit(IFont font) => MathF.Max(1, MathF.Round(font.LineHeight / 10f));

    /// <summary>Width of one prompt's glyph with any font: a label's text, a round button or a one-letter cap as wide as a line is tall, else the text with a margin.</summary>
    public static float GlyphWidth(IFont font, ActionPrompt prompt)
    {
        ArgumentNullException.ThrowIfNull(font);
        float height = MathF.Round(font.LineHeight);
        return prompt.Kind switch
        {
            PromptKind.Label => font.MeasureWidth(prompt.Text),
            PromptKind.PadButton when IsRound(prompt) => height,
            _ => MathF.Max(height, MathF.Ceiling(font.MeasureWidth(prompt.Text) + height / 2)),
        };
    }

    /// <summary>
    /// Draws one prompt with any font, its top-left at <paramref name="position"/>, as tall as a line of the font: a key
    /// cap, a round gamepad button, a pill, a ring for a touch control, plain text for a label.
    /// </summary>
    public static void DrawGlyph(IRenderer r, IFont font, ActionPrompt prompt, Vector2 position, PromptGlyphStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(r);
        ArgumentNullException.ThrowIfNull(font);
        var s = style ?? PromptGlyphStyle.Default;
        float w = GlyphWidth(font, prompt), h = MathF.Round(font.LineHeight), u = Unit(font);
        var box = new RectF(position.X, position.Y, w, h);
        var textPos = new Vector2(MathF.Round(box.X + (w - font.MeasureWidth(prompt.Text)) / 2f), position.Y);
        switch (prompt.Kind)
        {
            case PromptKind.Key or PromptKind.Pointer:
                r.FillRect(box, s.CapBorder);
                r.FillRect(new RectF(box.X + u, box.Y + u, w - 2 * u, h - 2 * u), s.Cap);
                r.FillRect(new RectF(box.X + u, box.Bottom - 2 * u, w - 2 * u, u), Color.Lerp(s.Cap, s.CapBorder, 0.35f));
                font.Draw(r, prompt.Text, textPos, s.CapText);
                break;
            case PromptKind.PadButton when IsRound(prompt):
            {
                var center = new Vector2(box.X + h / 2, box.Y + h / 2);
                var fill = s.ColoredFaceButtons ? FaceColor(prompt) ?? s.Cap : s.Cap;
                r.FillCircle(center, h / 2, s.CapBorder);
                r.FillCircle(center, h / 2 - u, fill);
                font.Draw(r, prompt.Text, textPos, fill == s.Cap ? s.CapText : Color.White);
                break;
            }
            case PromptKind.PadButton or PromptKind.PadAxis:
                Pill(r, box, s.CapBorder, s.Cap, u);
                font.Draw(r, prompt.Text, textPos, s.CapText);
                break;
            case PromptKind.Touch:
                Pill(r, box, s.Cap, s.CapBorder, u);
                font.Draw(r, prompt.Text, textPos, s.Cap);
                break;
            default:
                font.Draw(r, prompt.Text, position, s.Cap);
                break;
        }
    }

    private static void Pill(IRenderer r, RectF box, Color outline, Color fill, float border)
    {
        float radius = box.Height / 2f;
        r.FillCircle(new Vector2(box.X + radius, box.Y + radius), radius, outline);
        r.FillCircle(new Vector2(box.Right - radius, box.Y + radius), radius, outline);
        r.FillRect(new RectF(box.X + radius, box.Y, box.Width - 2 * radius, box.Height), outline);
        r.FillCircle(new Vector2(box.X + radius, box.Y + radius), radius - border, fill);
        r.FillCircle(new Vector2(box.Right - radius, box.Y + radius), radius - border, fill);
        r.FillRect(new RectF(box.X + radius, box.Y + border, box.Width - 2 * radius, box.Height - 2 * border), fill);
    }
}

using System.Numerics;
using System.Text;

namespace MicroEngine.Graphics;

/// <summary>
/// Anything that measures and draws text at one size: the built-in <see cref="BitmapFont"/> (at scale 1) and a
/// <see cref="ScalableFont"/> at a size (<see cref="ScalableFont.At"/>). UI widgets and layout code take an
/// <see cref="IFont"/>, so the same code serves pixel-art and HD text.
/// </summary>
public interface IFont
{
    /// <summary>Distance between the tops of two lines, in canvas pixels.</summary>
    float LineHeight { get; }
    /// <summary>Size of <paramref name="text"/> in canvas pixels: the widest line by the height of its lines. '\n' starts a new line.</summary>
    Vector2 Measure(string text);
    /// <summary>Draws <paramref name="text"/> with its top-left at <paramref name="position"/>, in <paramref name="color"/>.</summary>
    void Draw(IRenderer renderer, string text, Vector2 position, Color color);

    /// <summary>
    /// Size of <paramref name="text"/>, as <see cref="Measure(string)"/>, for text that is not a string (a buffer a
    /// number was written into). The engine's fonts measure it without allocating; a font of another kind that does not
    /// write this member measures a copy of the text as a string.
    /// </summary>
    Vector2 Measure(ReadOnlySpan<char> text) => Measure(text.ToString());

    /// <summary>
    /// Draws <paramref name="text"/>, as <see cref="Draw(IRenderer, string, Vector2, Color)"/>, for text that is not a
    /// string. The engine's fonts draw it without allocating; a font of another kind that does not write this member
    /// draws a copy of the text as a string.
    /// </summary>
    void Draw(IRenderer renderer, ReadOnlySpan<char> text, Vector2 position, Color color) => Draw(renderer, text.ToString(), position, color);

    /// <summary>Size of an interpolated string (<c>$"{score}"</c>), which is written into a reused buffer instead of a string.</summary>
    Vector2 Measure(InterpolatedText text)
    {
        try { return Measure(text.Text); }
        finally { text.Dispose(); }
    }

    /// <summary>
    /// Draws an interpolated string (<c>$"{hp}/{max}"</c>), which is written into a reused buffer instead of a string:
    /// a number drawn every frame makes no garbage.
    /// </summary>
    void Draw(IRenderer renderer, InterpolatedText text, Vector2 position, Color color)
    {
        try { Draw(renderer, text.Text, position, color); }
        finally { text.Dispose(); }
    }
}

/// <summary>Layout and drawing helpers that work with any <see cref="IFont"/>.</summary>
public static class FontExtensions
{
    /// <summary>Width of the widest line of <paramref name="text"/> in canvas pixels.</summary>
    public static float MeasureWidth(this IFont font, string text) => font.Measure(text).X;

    /// <inheritdoc cref="MeasureWidth(IFont, string)"/>
    public static float MeasureWidth(this IFont font, ReadOnlySpan<char> text) => font.Measure(text).X;

    /// <summary>Width of an interpolated string (<c>$"{score}"</c>), measured without making a string.</summary>
    public static float MeasureWidth(this IFont font, InterpolatedText text)
    {
        try { return font.Measure(text.Text).X; }
        finally { text.Dispose(); }
    }

    /// <summary>
    /// Greedy word wrap that respects explicit newlines: no line is wider than <paramref name="maxWidth"/>, and a word wider
    /// than that on its own is broken between characters.
    /// </summary>
    public static IReadOnlyList<string> Wrap(this IFont font, string text, float maxWidth)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(text);
        var result = new List<string>();
        foreach (var paragraph in text.Split('\n'))
        {
            var line = new StringBuilder();
            foreach (var word in paragraph.Split(' '))
            {
                var candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && font.Measure(candidate).X > maxWidth)
                {
                    result.Add(line.ToString());
                    line.Clear().Append(word);
                }
                else line.Clear().Append(candidate);
                // Only a single word can be too wide here: cut it where it stops fitting.
                while (line.Length > 1 && font.Measure(line.ToString()).X > maxWidth)
                {
                    int fit = 1;
                    while (fit < line.Length && font.Measure(line.ToString(0, fit + 1)).X <= maxWidth) fit++;
                    result.Add(line.ToString(0, fit));
                    line.Remove(0, fit);
                }
            }
            result.Add(line.ToString());
        }
        return result;
    }

    /// <summary>Draws <paramref name="text"/> centred on <paramref name="center"/>, rounded to whole pixels.</summary>
    public static void DrawCentered(this IFont font, IRenderer renderer, string text, Vector2 center, Color color)
    {
        var size = font.Measure(text);
        font.Draw(renderer, text, new Vector2(MathF.Round(center.X - size.X / 2), MathF.Round(center.Y - size.Y / 2)), color);
    }

    /// <inheritdoc cref="DrawCentered(IFont, IRenderer, string, Vector2, Color)"/>
    public static void DrawCentered(this IFont font, IRenderer renderer, ReadOnlySpan<char> text, Vector2 center, Color color)
    {
        var size = font.Measure(text);
        font.Draw(renderer, text, new Vector2(MathF.Round(center.X - size.X / 2), MathF.Round(center.Y - size.Y / 2)), color);
    }

    /// <summary>Draws an interpolated string (<c>$"{minutes}:{seconds:00}"</c>) centred on <paramref name="center"/>, without making a string.</summary>
    public static void DrawCentered(this IFont font, IRenderer renderer, InterpolatedText text, Vector2 center, Color color)
    {
        try { font.DrawCentered(renderer, text.Text, center, color); }
        finally { text.Dispose(); }
    }

    /// <summary>
    /// Draws <paramref name="text"/> so that it ends at <paramref name="topRight"/>: a score or a price against the
    /// right edge of a panel, whatever its number of digits. Rounded to whole pixels.
    /// </summary>
    public static void DrawRight(this IFont font, IRenderer renderer, string text, Vector2 topRight, Color color)
    {
        ArgumentNullException.ThrowIfNull(text);
        font.DrawRight(renderer, text.AsSpan(), topRight, color);
    }

    /// <inheritdoc cref="DrawRight(IFont, IRenderer, string, Vector2, Color)"/>
    public static void DrawRight(this IFont font, IRenderer renderer, ReadOnlySpan<char> text, Vector2 topRight, Color color)
        => font.Draw(renderer, text, new Vector2(MathF.Round(topRight.X - font.Measure(text).X), MathF.Round(topRight.Y)), color);

    /// <summary>Draws an interpolated string (<c>$"{score}"</c>) so that it ends at <paramref name="topRight"/>, without making a string.</summary>
    public static void DrawRight(this IFont font, IRenderer renderer, InterpolatedText text, Vector2 topRight, Color color)
    {
        try { font.DrawRight(renderer, text.Text, topRight, color); }
        finally { text.Dispose(); }
    }

    /// <summary>Draws <paramref name="text"/> over a copy of itself in <paramref name="shadow"/>, moved by <paramref name="offset"/> (one pixel down and right by default).</summary>
    public static void DrawShadowed(this IFont font, IRenderer renderer, string text, Vector2 position, Color color, Color shadow, Vector2? offset = null)
    {
        font.Draw(renderer, text, position + (offset ?? Vector2.One), shadow);
        font.Draw(renderer, text, position, color);
    }

    /// <inheritdoc cref="DrawShadowed(IFont, IRenderer, string, Vector2, Color, Color, Vector2?)"/>
    public static void DrawShadowed(this IFont font, IRenderer renderer, ReadOnlySpan<char> text, Vector2 position, Color color, Color shadow, Vector2? offset = null)
    {
        font.Draw(renderer, text, position + (offset ?? Vector2.One), shadow);
        font.Draw(renderer, text, position, color);
    }

    /// <summary>Draws an interpolated string (<c>$"{coins}"</c>) over a copy of itself in <paramref name="shadow"/>, without making a string.</summary>
    public static void DrawShadowed(this IFont font, IRenderer renderer, InterpolatedText text, Vector2 position, Color color, Color shadow, Vector2? offset = null)
    {
        try { font.DrawShadowed(renderer, text.Text, position, color, shadow, offset); }
        finally { text.Dispose(); }
    }
}

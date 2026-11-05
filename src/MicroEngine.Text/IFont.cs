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
}

/// <summary>Layout and drawing helpers that work with any <see cref="IFont"/>.</summary>
public static class FontExtensions
{
    /// <summary>Width of the widest line of <paramref name="text"/> in canvas pixels.</summary>
    public static float MeasureWidth(this IFont font, string text) => font.Measure(text).X;

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

    /// <summary>Draws <paramref name="text"/> over a copy of itself in <paramref name="shadow"/>, moved by <paramref name="offset"/> (one pixel down and right by default).</summary>
    public static void DrawShadowed(this IFont font, IRenderer renderer, string text, Vector2 position, Color color, Color shadow, Vector2? offset = null)
    {
        font.Draw(renderer, text, position + (offset ?? Vector2.One), shadow);
        font.Draw(renderer, text, position, color);
    }
}

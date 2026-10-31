using System.Numerics;
using System.Text;

namespace MicroEngine.Graphics;

/// <summary>
/// A proportional pixel font. The built-in face ("Micro 5x7") is authored in code so every game
/// has crisp, license-free text without loading files. Glyph widths are derived by trimming
/// empty columns, so narrow letters like 'i' take less space.
/// </summary>
public sealed class BitmapFont : IDisposable
{
    private readonly Dictionary<char, Glyph> _glyphs;
    private readonly Texture? _texture;

    public int LineHeight { get; }
    public int Spacing { get; } = 1;
    /// <summary>Height of capital letters above the baseline.</summary>
    public int CapHeight { get; } = 7;

    private readonly record struct Glyph(RectF Source, int Advance);

    private BitmapFont(Dictionary<char, Glyph> glyphs, Texture? texture, int lineHeight)
        => (_glyphs, _texture, LineHeight) = (glyphs, texture, lineHeight);

    /// <summary>Creates the built-in face. Pass null for a measurement-only font (tests, layout).</summary>
    public static BitmapFont CreateDefault(IGraphicsDevice? device)
    {
        var defs = MicroFontData.Glyphs;
        const int cell = 6, rows = 8;
        int columns = 16, lines = (defs.Count + columns - 1) / columns;
        var sheet = new PixelImage(columns * cell, lines * (rows + 1));
        var glyphs = new Dictionary<char, Glyph>();
        int index = 0;
        foreach (var (ch, pattern) in defs)
        {
            var rowsData = pattern.Split('|');
            int min = 5, max = -1;
            foreach (var row in rowsData)
                for (int x = 0; x < row.Length; x++)
                    if (row[x] == '#') { min = Math.Min(min, x); max = Math.Max(max, x); }
            int ox = index % columns * cell, oy = index / columns * (rows + 1);
            for (int y = 0; y < rowsData.Length; y++)
                for (int x = 0; x < rowsData[y].Length; x++)
                    if (rowsData[y][x] == '#') sheet[ox + x - Math.Max(min, 0), oy + y] = Color.White;
            int width = max < 0 ? 2 : max - min + 1;
            glyphs[ch] = new Glyph(new RectF(ox, oy, Math.Max(width, 1), rows), width);
            index++;
        }
        var texture = device?.CreateTexture(sheet);
        return new BitmapFont(glyphs, texture, 10);
    }

    public int Advance(char c) => (_glyphs.TryGetValue(c, out var g) ? g.Advance : _glyphs['?'].Advance) + Spacing;

    /// <summary>Width in pixels of the widest line (unscaled).</summary>
    public int MeasureWidth(string text)
    {
        int best = 0, current = 0;
        foreach (var c in text)
        {
            if (c == '\n') { best = Math.Max(best, current); current = 0; continue; }
            current += Advance(c);
        }
        return Math.Max(best, current) - (text.Length > 0 ? Spacing : 0);
    }

    public Vector2 Measure(string text, float scale = 1f)
    {
        int lines = 1 + text.Count(c => c == '\n');
        return new Vector2(MeasureWidth(text) * scale, (lines * LineHeight - (LineHeight - 8)) * scale);
    }

    /// <summary>Greedy word wrap that respects explicit newlines.</summary>
    public IReadOnlyList<string> Wrap(string text, int maxWidth)
    {
        var result = new List<string>();
        foreach (var paragraph in text.Split('\n'))
        {
            var line = new StringBuilder();
            foreach (var word in paragraph.Split(' '))
            {
                var candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && MeasureWidth(candidate) > maxWidth)
                {
                    result.Add(line.ToString());
                    line.Clear().Append(word);
                }
                else line.Clear().Append(candidate);
            }
            result.Add(line.ToString());
        }
        return result;
    }

    public void Draw(IRenderer renderer, string text, Vector2 position, Color color, float scale = 1f)
    {
        if (_texture is null) throw new InvalidOperationException("This font was created without a graphics device.");
        float x = position.X, y = position.Y;
        var options = new DrawOptions(Scale: new Vector2(scale));
        foreach (var c in text)
        {
            if (c == '\n') { x = position.X; y += LineHeight * scale; continue; }
            if (!_glyphs.TryGetValue(c, out var g)) g = _glyphs['?'];
            if (c != ' ') renderer.Draw(_texture, g.Source, new Vector2(x, y), color, options);
            x += (g.Advance + Spacing) * scale;
        }
    }

    /// <summary>Draws text with a 1px drop shadow, the most legible style over busy pixel art.</summary>
    public void DrawShadowed(IRenderer renderer, string text, Vector2 position, Color color, Color shadow, float scale = 1f)
    {
        Draw(renderer, text, position + new Vector2(scale, scale), shadow, scale);
        Draw(renderer, text, position, color, scale);
    }

    /// <summary>Draws text with a full 1px outline (8 directions).</summary>
    public void DrawOutlined(IRenderer renderer, string text, Vector2 position, Color color, Color outline, float scale = 1f)
    {
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
            if (dx != 0 || dy != 0) Draw(renderer, text, position + new Vector2(dx, dy) * scale, outline, scale);
        Draw(renderer, text, position, color, scale);
    }

    public void DrawCentered(IRenderer renderer, string text, Vector2 center, Color color, float scale = 1f, Color? shadow = null)
    {
        var size = Measure(text, scale);
        var pos = new Vector2(MathF.Round(center.X - size.X / 2), MathF.Round(center.Y - size.Y / 2));
        if (shadow is { } s) DrawShadowed(renderer, text, pos, color, s, scale);
        else Draw(renderer, text, pos, color, scale);
    }

    public void Dispose() => _texture?.Dispose();
}

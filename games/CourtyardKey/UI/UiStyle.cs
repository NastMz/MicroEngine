using System.Numerics;
using CourtyardKey.Visuals;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.UI;

namespace CourtyardKey.UI;

/// <summary>The game's UI look: deep night-violet panels with a gilded double border and corner studs.</summary>
public static class UiStyle
{
    public static readonly Color PanelFill = Color.Hex("1d1830f0");
    public static readonly PanelStyle Panel = new(PanelFill, Pal.Gold[1], Color.Hex("3a3058"), Pal.Ink.WithAlpha((byte)150), Pal.Gold[0]);
    public static readonly UiTheme Theme = new(Panel, Pal.White, Pal.Ink, Pal.Gold[2], Pal.Stone[4]);

    /// <summary>A framed panel with tiny studs at the corners.</summary>
    public static void Frame(IRenderer r, RectF rect)
    {
        r.Panel(rect, Panel);
        Stud(r, rect.X, rect.Y);
        Stud(r, rect.Right - 3, rect.Y);
        Stud(r, rect.X, rect.Bottom - 3);
        Stud(r, rect.Right - 3, rect.Bottom - 3);
    }

    private static void Stud(IRenderer r, float x, float y)
    {
        r.FillRect(new RectF(x, y, 3, 3), Pal.Gold[2]);
        r.FillRect(new RectF(x + 1, y + 1, 1, 1), Pal.Gold[3]);
    }

    /// <summary>
    /// The rows of a menu from <paramref name="position"/> down: the one under the cursor gilded behind an arrow that
    /// bobs, one that cannot be chosen muted.
    /// </summary>
    public static void Menu(IRenderer r, BitmapFont font, ReadOnlySpan<string> options, MenuCursor cursor, Vector2 position, float time, float lineHeight)
    {
        for (int i = 0; i < options.Length; i++)
        {
            bool selected = i == cursor.Index;
            var color = !cursor.IsEnabled(i) ? Theme.Muted : selected ? Theme.Accent : Theme.Text;
            var at = position + new Vector2(selected ? 10 : 8, i * lineHeight);
            if (selected) font.Draw(r, "▶", new Vector2(position.X + MathF.Round(MathX.Wave(time, 2f, 1f)), at.Y), Theme.Accent);
            font.DrawShadowed(r, options[i], at, color, Theme.TextShadow);
        }
    }

    /// <summary>A small key-cap label like "Z" for control hints.</summary>
    public static void KeyCap(IRenderer r, BitmapFont font, string key, Vector2 at)
    {
        int w = font.MeasureWidth(key) + 5;
        r.FillRect(new RectF(at.X, at.Y + 1, w, 9), Pal.Ink);
        r.FillRect(new RectF(at.X, at.Y, w, 9), Pal.Cream);
        r.FillRect(new RectF(at.X, at.Y + 8, w, 1), Pal.Stone[3]);
        font.Draw(r, key, at + new Vector2(3, 1), Pal.Ink);
    }

    public static int KeyCapWidth(BitmapFont font, string key) => font.MeasureWidth(key) + 5;
}

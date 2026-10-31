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
        foreach (var (x, y) in new[] { (rect.X, rect.Y), (rect.Right - 3, rect.Y), (rect.X, rect.Bottom - 3), (rect.Right - 3, rect.Bottom - 3) })
        {
            r.FillRect(new RectF(x, y, 3, 3), Pal.Gold[2]);
            r.FillRect(new RectF(x + 1, y + 1, 1, 1), Pal.Gold[3]);
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

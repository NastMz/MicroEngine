using System.Numerics;
using MicroEngine;
using MicroEngine.Audio;
using MicroEngine.Graphics;
using MicroEngine.UI;
using TrailDuel.Rules;
using TrailDuel.Visuals;

namespace TrailDuel.Ui;

/// <summary>Shared drawing helpers so every menu, box and bar has the same warm storybook look.</summary>
internal static class UiKit
{
    public static readonly Color Border = Color.Hex("3d3456");
    public static readonly Color Fill = Pal.Cream;
    public static readonly Color FillShade = Color.Hex("efe4cc");
    public static readonly Color Accent = Pal.Coral;
    public static readonly Color TextColor = Pal.TextDark;
    public static readonly Color TextShade = Pal.TextShadow;
    public static readonly Color Muted = Color.Hex("9a8e82");
    /// <summary>The same look for the engine's shared widgets (achievement list and banners).</summary>
    public static readonly UiTheme Theme = new(new PanelStyle(Fill, Border, FillShade, Pal.Ink.WithAlpha(0.35f), Accent.WithAlpha(0.55f)), TextColor, TextShade, Pal.CoralDark, Muted);

    /// <summary>A rounded cream box with a double plum border and a soft drop shadow.</summary>
    public static void Box(IRenderer r, RectF rect, Color? fill = null)
    {
        var (x, y, w, h) = (MathF.Round(rect.X), MathF.Round(rect.Y), MathF.Round(rect.Width), MathF.Round(rect.Height));
        r.FillRect(new RectF(x + 2, y + h, w - 2, 2), Pal.Ink.WithAlpha(0.35f));
        r.FillRect(new RectF(x + w, y + 2, 2, h - 1), Pal.Ink.WithAlpha(0.35f));
        r.FillRect(new RectF(x + 1, y, w - 2, h), Border);
        r.FillRect(new RectF(x, y + 1, w, h - 2), Border);
        r.FillRect(new RectF(x + 1, y + 1, w - 2, h - 2), fill ?? Fill);
        r.DrawRect(new RectF(x + 2, y + 2, w - 4, h - 4), Accent.WithAlpha(0.55f));
        r.FillRect(new RectF(x + 3, y + h - 4, w - 6, 1), FillShade);
    }

    public static void Text(IRenderer r, BitmapFont font, string text, float x, float y, Color? color = null)
        => font.DrawShadowed(r, text, new Vector2(MathF.Round(x), MathF.Round(y)), color ?? TextColor, TextShade);

    public static void TextRight(IRenderer r, BitmapFont font, string text, float right, float y, Color? color = null)
        => Text(r, font, text, right - font.MeasureWidth(text), y, color);

    /// <summary>White text with a dark outline: legible over the battle scene and overworld.</summary>
    public static void Title(IRenderer r, BitmapFont font, string text, Vector2 center, float scale = 1f, Color? color = null)
    {
        var size = font.Measure(text, scale);
        var pos = new Vector2(MathF.Round(center.X - size.X / 2), MathF.Round(center.Y - size.Y / 2));
        font.DrawOutlined(r, text, pos, color ?? Pal.White, Pal.Ink, scale);
    }

    /// <summary>
    /// The engine's list navigation (up and down move and wrap, confirm chooses) with the game's cursor sound. Screens draw
    /// their lists themselves, with <see cref="Cursor"/> beside the highlighted row.
    /// </summary>
    public static SelectionList List(AudioMixer audio, params string[] options)
    {
        var list = new SelectionList(options);
        list.Moved += () => audio.Play("move", 0.6f);
        return list;
    }

    public static void Cursor(IRenderer r, BitmapFont font, float x, float y, float time)
        => font.Draw(r, "▶", new Vector2(MathF.Round(x + MathX.Wave(time, 2.5f, 1f)), MathF.Round(y)), Accent);

    // Bar and badge colours parsed once: they are drawn many times a frame.
    private static readonly Color HpHigh = Color.Hex("58c060"), HpMid = Color.Hex("f2c038"), HpLow = Color.Hex("e8504a"),
        BarTrack = Color.Hex("5a5068"), XpFill = Color.Hex("58b0f0"),
        Burn = Color.Hex("e8603a"), Poison = Color.Hex("a85ac8"), Sleep = Color.Hex("8a8aa8");

    public static Color HpColor(float fraction) => fraction > 0.5f ? HpHigh : fraction > 0.2f ? HpMid : HpLow;

    /// <summary>A segmented HP bar with an "HP" label plate.</summary>
    public static void HpBar(IRenderer r, BitmapFont font, float x, float y, float width, float fraction)
    {
        fraction = Math.Clamp(fraction, 0, 1);
        r.FillRect(new RectF(x, y, 15, 7), Border);
        font.Draw(r, "HP", new Vector2(x + 2, y), Pal.Gold);
        r.FillRect(new RectF(x + 14, y, width + 2, 7), Border);
        r.FillRect(new RectF(x + 15, y + 1, width, 5), BarTrack);
        float fill = MathF.Round(width * fraction);
        if (fraction > 0) fill = MathF.Max(1, fill);
        var c = HpColor(fraction);
        r.FillRect(new RectF(x + 15, y + 1, fill, 5), c);
        r.FillRect(new RectF(x + 15, y + 1, fill, 1), Color.Lerp(c, Color.White, 0.45f));
        r.FillRect(new RectF(x + 15, y + 5, fill, 1), c.Shade(0.75f));
    }

    public static void XpBar(IRenderer r, float x, float y, float width, float fraction)
    {
        r.FillRect(new RectF(x, y, width + 2, 4), Border);
        r.FillRect(new RectF(x + 1, y + 1, width, 2), BarTrack);
        r.FillRect(new RectF(x + 1, y + 1, MathF.Round(width * Math.Clamp(fraction, 0, 1)), 2), XpFill);
    }

    /// <summary>A small coloured type tag, e.g. FLAME.</summary>
    public static void TypeBadge(IRenderer r, BitmapFont font, ElementType type, float x, float y)
    {
        string name = TypeChart.Name(type);
        int w = font.MeasureWidth(name) + 6;
        var c = Pal.TypeColor(type);
        r.FillRect(new RectF(x, y, w, 9), Pal.TypeColorDark(type));
        r.FillRect(new RectF(x + 1, y + 1, w - 2, 7), c);
        font.Draw(r, name, new Vector2(x + 3, y + 1), Pal.White);
    }

    public static int TypeBadgeWidth(BitmapFont font, ElementType type) => font.MeasureWidth(TypeChart.Name(type)) + 6;

    public static void StatusBadge(IRenderer r, BitmapFont font, StatusCondition status, float x, float y)
    {
        if (status == StatusCondition.None) return;
        var (label, color) = status switch
        {
            StatusCondition.Burn => ("BRN", Burn),
            StatusCondition.Poison => ("PSN", Poison),
            _ => ("SLP", Sleep),
        };
        r.FillRect(new RectF(x, y, 19, 9), color.Shade(0.6f));
        r.FillRect(new RectF(x + 1, y + 1, 17, 7), color);
        font.Draw(r, label, new Vector2(x + 2, y + 1), Pal.White);
    }
}

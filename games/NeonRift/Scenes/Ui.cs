using System.Globalization;
using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.UI;

namespace NeonRift.Scenes;

/// <summary>Neon UI: white text with deep-violet outlines, magenta and cyan accents, additive glows.</summary>
internal static class Ui
{
    public static readonly Color Ink = Color.Hex("0d0b1a");
    public static readonly Color Magenta = Color.Hex("ff3fa4");
    public static readonly Color Cyan = Color.Hex("4ef2ff");
    public static readonly Color Gold = Color.Hex("ffd447");
    public static readonly Color Muted = Color.Hex("8a86b8");
    public static readonly Color Text = Color.Hex("f4f0ff");
    /// <summary>The neon look for the engine's shared widgets (achievement list and banners).</summary>
    public static readonly UiTheme Theme = new(new PanelStyle(Ink.WithAlpha(0.92f), Magenta, Magenta.WithAlpha(0.4f)), Text, Ink, Cyan, Muted);

    public static string N(int value, int digits = 0) => value.ToString(digits > 0 ? new string('0', digits) : "0", CultureInfo.InvariantCulture);

    public static void Label(IRenderer r, BitmapFont font, string text, Vector2 at, Color? color = null, float scale = 1)
        => font.DrawOutlined(r, text, Vector2.Round(at), color ?? Text, Ink, scale);

    public static void Centered(IRenderer r, BitmapFont font, string text, float cx, float y, Color? color = null, float scale = 1)
    {
        var size = font.Measure(text, scale);
        Label(r, font, text, new Vector2(MathF.Round(cx - size.X / 2), y), color, scale);
    }

    /// <summary>Large title text with a soft neon bloom made of offset additive copies.</summary>
    public static void Neon(IRenderer r, BitmapFont font, string text, float cx, float y, float scale, Color glow, float time)
    {
        var size = font.Measure(text, scale);
        var at = new Vector2(MathF.Round(cx - size.X / 2), y);
        float flicker = 0.75f + 0.25f * MathF.Sin(time * 7) * MathF.Sin(time * 2.3f);
        r.SetBlendMode(BlendMode.Additive);
        foreach (var o in new[] { new Vector2(-2, 0), new Vector2(2, 0), new Vector2(0, -2), new Vector2(0, 2), new Vector2(-1, -1), new Vector2(1, 1) })
            font.Draw(r, text, at + o, glow.WithAlpha(0.35f * flicker), scale);
        r.SetBlendMode(BlendMode.Alpha);
        font.Draw(r, text, at + new Vector2(MathF.Ceiling(scale / 2), MathF.Ceiling(scale / 2)), Ink, scale);
        font.Draw(r, text, at, Color.Lerp(Text, glow, 0.3f), scale);
    }

    public static void Menu(IRenderer r, BitmapFont font, SelectionList list, float cx, float y, float time, int lineHeight = 16, int width = 140)
    {
        for (int i = 0; i < list.Options.Count; i++)
        {
            bool selected = i == list.Index;
            float ly = y + i * lineHeight;
            if (selected)
            {
                r.FillRect(new RectF(cx - width / 2f, ly - 3, width, 13), Magenta.WithAlpha(0.22f));
                r.FillRect(new RectF(cx - width / 2f, ly + 10, width, 1), Magenta.WithAlpha(0.8f));
                float bob = MathF.Round(MathX.Wave(time, 2f, 1.5f));
                Label(r, font, ">", new Vector2(cx - width / 2f + 6 + bob, ly), Cyan);
            }
            Centered(r, font, list.Options[i], cx, ly, !list.IsEnabled(i) ? Muted : selected ? Gold : Text);
        }
    }

    /// <summary>Thin scanlines over the whole canvas for a CRT-ish finish.</summary>
    public static void Scanlines(IRenderer r)
    {
        for (int y = 0; y < r.Height; y += 2) r.FillRect(new RectF(0, y, r.Width, 1), Color.Black.WithAlpha(0.07f));
    }
}

using System.Globalization;
using System.Numerics;
using MeadowRun.Art;
using MeadowRun.Gameplay;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.UI;

namespace MeadowRun.Scenes;

/// <summary>Shared UI look: cream text with ink outlines, warm panels, gold accents.</summary>
internal static class Ui
{
    public static readonly UiTheme Theme = new(
        new PanelStyle(Pal.Night, Pal.Ink, Pal.Slate, Pal.Ink.WithAlpha(0.5f), Pal.Dusk),
        Pal.Cream, Pal.Ink, Pal.Gold, Pal.Dusk);

    public static readonly PanelStyle Card = new(Pal.Night.WithAlpha((byte)0xf0), Pal.Ink, Pal.Slate, Pal.Ink.WithAlpha(0.45f), Pal.Gold.WithAlpha(0.5f));

    public static string N(int value, int digits = 0) => value.ToString(digits > 0 ? new string('0', digits) : "0", CultureInfo.InvariantCulture);

    public static void Text(IRenderer r, BitmapFont font, string text, Vector2 at, Color? color = null, float scale = 1)
        => font.DrawOutlined(r, text, Snap(at), color ?? Pal.Cream, Pal.Ink, scale);

    public static void Centered(IRenderer r, BitmapFont font, string text, float cx, float y, Color? color = null, float scale = 1)
    {
        var size = font.Measure(text, scale);
        Text(r, font, text, new Vector2(cx - size.X / 2, y), color, scale);
    }

    public static Vector2 Snap(Vector2 v) => new(MathF.Round(v.X), MathF.Round(v.Y));

    /// <summary>A vertical menu with a bobbing cursor and a highlight bar behind the selected item.</summary>
    public static void Menu(IRenderer r, BitmapFont font, SelectionList list, float cx, float y, float time, int lineHeight = 13, int width = 96)
    {
        for (int i = 0; i < list.Options.Count; i++)
        {
            bool selected = i == list.Index;
            float ly = y + i * lineHeight;
            if (selected)
            {
                r.FillRect(new RectF(cx - width / 2f, ly - 2, width, 11), Pal.Gold.WithAlpha(0.22f));
                r.FillRect(new RectF(cx - width / 2f, ly + 9, width, 1), Pal.Gold.WithAlpha(0.6f));
                float bob = MathF.Round(MathX.Wave(time, 2f, 1.5f));
                Text(r, font, "▶", new Vector2(cx - width / 2f + 4 + bob, ly), Pal.Gold);
            }
            var color = !list.IsEnabled(i) ? Pal.Dusk : selected ? Pal.Butter : Pal.Cream;
            Centered(r, font, list.Options[i], cx, ly, color);
        }
    }

    /// <summary>The in-level HUD: lives, coins, score, power and timer along the top edge.</summary>
    public static void Hud(IRenderer r, BitmapFont font, Assets art, Session session, Stage stage, float time, HudLabels labels)
    {
        r.FillGradient(new RectF(0, 0, 320, 16), Pal.Ink.WithAlpha(0.55f), Pal.Ink.WithAlpha(0f), 4);
        r.Draw(art["icon_life"], new Vector2(5, 3));
        Text(r, font, labels.Lives.For(session.Lives), new Vector2(18, 4));
        r.Draw(art["icon_coin"], new Vector2(46, 4));
        Text(r, font, labels.Coins.For(session.Coins), new Vector2(56, 4), Pal.Butter);
        Centered(r, font, labels.Score.For(session.Score), 160, 4);
        // Power state: a small form badge, drawn at its own size so it keeps the pixel grid.
        r.Draw(art.FormIcon(stage.Player.Form), new Vector2(249, 2));
        int t = (int)MathF.Ceiling(stage.TimeLeft);
        bool hurry = t <= 100 && stage.Phase == StagePhase.Playing;
        r.Draw(art["icon_clock"], new Vector2(266, 3));
        var color = hurry && (int)(time * 4) % 2 == 0 ? Pal.Coral : Pal.Cream;
        Text(r, font, labels.Time.For(t), new Vector2(278, 4), color);
    }
}

/// <summary>A number drawn as text every frame, formatted again only when it changes.</summary>
internal sealed class NumberLabel(string prefix = "", int digits = 0)
{
    private int _value;
    private string? _text;

    public string For(int value)
    {
        if (_text is null || value != _value)
        {
            _value = value;
            _text = prefix + Ui.N(value, digits);
        }
        return _text;
    }
}

/// <summary>The HUD's numbers, kept by the scene that draws it.</summary>
internal sealed class HudLabels
{
    public NumberLabel Lives { get; } = new("×", 2);
    public NumberLabel Coins { get; } = new("×", 2);
    public NumberLabel Score { get; } = new(digits: 7);
    public NumberLabel Time { get; } = new(digits: 3);
}

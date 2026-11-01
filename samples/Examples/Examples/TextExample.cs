using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace Examples;

/// <summary>The built-in pixel font: plain, shadowed, outlined, centred, scaled, measured and wrapped text.</summary>
public sealed class TextExample : Scene
{
    private const string Story = "The built-in font is a small proportional pixel face. Wrap splits text into lines that fit a width, at spaces.";

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) Game.Scenes.Pop();
    }

    public override void Draw(IRenderer r)
    {
        #region text
        Font.Draw(r, "Plain text, upper and lower case: 0123456789 !?.,:;()+-*/", new Vector2(8, 20), Color.White);
        Font.DrawShadowed(r, "Shadowed text reads on any background", new Vector2(8, 32), Color.Hex("ffcd75"), Color.Black);
        Font.DrawOutlined(r, "Outlined", new Vector2(8, 44), Color.White, Color.Hex("b13e53"));
        Font.Draw(r, "x2", new Vector2(70, 40), Color.Hex("a7f070"), scale: 2);
        Font.DrawCentered(r, "CENTRED ON A POINT", new Vector2(240, 48), Color.Hex("73eff7"), shadow: Color.Black);

        // Measure: the size of a string, for boxes and right alignment.
        var score = $"SCORE {Game.UpdateCount}";
        Font.Draw(r, score, new Vector2(312 - Font.MeasureWidth(score), 62), Color.White);

        // Wrap: lines that fit a width; LineHeight spaces them.
        var box = new RectF(8, 78, 180, 60);
        r.Panel(box, MicroEngine.UI.UiTheme.Default.Panel);
        var lines = Font.Wrap(Story, (int)box.Width - 12);
        for (int i = 0; i < lines.Count; i++)
            Font.Draw(r, lines[i], new Vector2(box.X + 6, box.Y + 6 + i * (Font.LineHeight + 1)), Color.White);
        #endregion

        Font.Draw(r, "Symbols: ♥ ▶ ▼ ★ ♪ → ← ↑ ↓ × © ·", new Vector2(196, 84), Color.Hex("ffcd75"));
        Font.Draw(r, "Not in the font: ñ é ü", new Vector2(196, 96), Color.Hex("ef7d57"));   // drawn as ?
        Heading.Draw(r, Font, "TEXT");
    }
}

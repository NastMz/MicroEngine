using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// Shaped text (the optional MicroEngine.Text.Shaping package): Arabic letters joined to their neighbours and Hebrew,
/// both running right to left, next to English, from one font with two fallbacks. The right-to-left lines are aligned
/// to the right, and a wrapping label shows the font serving widgets.
/// </summary>
public sealed class ShapingExample : Scene
{
    private ShapedFont? _font;

    /// <summary>The font at the size the lines use, for tests.</summary>
    public ShapedFontSize Text { get; private set; } = null!;

    #region shaping
    public override void Enter()
    {
        // Latin from Inter; the letters it lacks from Noto Sans Arabic, then Noto Sans Hebrew.
        _font = ShapedFont.FromBytes(Game.Graphics, Game.Assets.Bytes("fonts/Inter-Regular.ttf"), name: "Inter")
            .AddFallback(Game.Assets.Bytes("fonts/NotoSansArabic-Regular.ttf"), "Noto Sans Arabic")
            .AddFallback(Game.Assets.Bytes("fonts/NotoSansHebrew-Regular.ttf"), "Noto Sans Hebrew");
        Text = _font.At(14);
        // An IFont like any other: widgets measure, wrap and draw with it.
        Widgets.Font = _font.At(12);
        Widgets.Content = new Panel(new Label("مرحبا بك في المثال. هذه الجملة تلتف داخل اللوحة.") { Wrap = true, Width = 140 });
    }

    public override void DrawUi(IRenderer ui)
    {
        // Inside the game's frame (the UI view also covers any letterbox around it).
        var frame = Game.Views.CanvasToUi(Vector2.Zero);
        var end = Game.Views.CanvasToUi(new Vector2(Game.Settings.VirtualWidth, Game.Settings.VirtualHeight));
        float left = frame.X + 16, right = end.X - 16, y = frame.Y + 24, step = Text.LineHeight + 4;
        Text.Draw(ui, "Hello, world", new Vector2(left, y), Color.White);
        // Right-to-left lines start at the right edge.
        DrawRight(ui, "مرحبا بالعالم", right, y + step, Color.Hex("ffcd75"));
        DrawRight(ui, "שלום עולם", right, y + 2 * step, Color.Hex("73eff7"));
    }

    private void DrawRight(IRenderer ui, string line, float right, float y, Color color) =>
        Text.Draw(ui, line, new Vector2(right - Text.MeasureWidth(line), y), color);
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) Game.Scenes.Pop();
    }

    public override void Exit()
    {
        _font?.Dispose();
        _font = null;
    }

    public override void Draw(IRenderer r) => Heading.Draw(r, Font, "SHAPED TEXT");
}

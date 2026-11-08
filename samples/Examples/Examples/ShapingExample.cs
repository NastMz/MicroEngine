using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// Shaped text (the optional MicroEngine.Text.Shaping package): Arabic letters joined to their neighbours and Hebrew,
/// both running right to left, next to English, from one font with two fallbacks; lines that mix both directions; each
/// line aligned to the side it starts from; and a wrapping label showing the font serving widgets.
/// </summary>
public sealed class ShapingExample : Scene
{
    private ShapedFont? _font;

    /// <summary>The font at the size the lines use, for tests.</summary>
    public ShapedFontSize Text { get; private set; } = null!;

    /// <summary>The lines shown, top to bottom: English, Arabic, Hebrew, then each direction with words of the other.</summary>
    public static readonly string[] Lines =
    [
        "Hello, world",
        "مرحبا بالعالم",
        "שלום עולם",
        "مرحبا Bob، لديك 3 رسائل",   // "Hello Bob, you have 3 messages"
        "Level 2: שלב הבא",           // "next stage"
    ];

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
        var label = new Label("مرحبا بك في المثال. هذه الجملة تلتف داخل اللوحة.") { Wrap = true, Width = 140 };
        Widgets.Content = new Layer().Add(new Panel(label), Anchor.Bottom, new Vector2(0, -12));
    }

    public override void DrawUi(IRenderer ui)
    {
        // Inside the game's frame (the UI view also covers any letterbox around it).
        var frame = Game.Views.CanvasToUi(Vector2.Zero);
        var end = Game.Views.CanvasToUi(new Vector2(Game.Settings.VirtualWidth, Game.Settings.VirtualHeight));
        float left = frame.X + 16, right = end.X - 16, y = frame.Y + 20;
        foreach (var line in Lines)
        {
            // A line that starts with a right-to-left letter is aligned to the right edge.
            float x = Text.IsRightToLeft(line) ? right - Text.MeasureWidth(line) : left;
            Text.Draw(ui, line, new Vector2(x, y), Color.White);
            y += Text.LineHeight + 2;
        }
    }
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

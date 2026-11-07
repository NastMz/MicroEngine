using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// A menu with a look of its own: the buttons are a wooden frame and the panel behind them is parchment, both cut from
/// one small image and stretched as nine-slices to whatever size the widgets measure.
/// </summary>
public sealed class SkinExample : Scene
{
    private Texture _frames = null!;

    /// <summary>The panel behind the menu, for tests.</summary>
    public Panel Menu { get; private set; } = null!;
    /// <summary>The last button pressed, for tests.</summary>
    public string Chosen { get; private set; } = "";

    #region skin
    public override void Enter()
    {
        // Content/frames.png holds two 12x12 frames side by side, each with a 4-pixel border: wood, then parchment.
        _frames = Game.Graphics.CreateTexture(new ContentFolder().Image("frames.png"), TextureFilter.Nearest);
        var wood = new NineSlice(_frames, new RectF(0, 0, 12, 12), 4);
        var parchment = new NineSlice(_frames, new RectF(12, 0, 12, 12), 4);

        // The theme's panel is what buttons draw: here the wooden frame, its text light on the dark wood.
        var theme = UiTheme.Default with { Accent = Color.Hex("ffd66b"), Text = Color.Hex("f4e4c1") };
        Widgets.Theme = theme with { Panel = theme.Panel with { Image = wood } };

        var buttons = new Stack { Spacing = 2 };
        foreach (var name in new[] { "CONTINUE", "OPTIONS", "QUIT TO TITLE" })
            buttons.Add(new Button(name, () => Chosen = name) { Width = 100 });
        Menu = new Panel(new Stack { Spacing = 4 }
            .Add(new Label("PAUSED") { Color = Color.Hex("4a3426"), Shadow = false })
            .Add(buttons))
        {
            Padding = 10,                                       // clear of the 4-pixel border
            Style = theme.Panel with { Image = parchment },     // a panel of its own look
        };
        Widgets.Content = Menu;
    }

    public override void Exit() => _frames.Dispose();
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) Game.Scenes.Pop();
    }

    public override void Draw(IRenderer r) => Heading.Draw(r, Font, "YOUR OWN LOOK");
}

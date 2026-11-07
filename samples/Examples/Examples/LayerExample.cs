using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// Widgets placed on a layer: a speech bubble that follows a character walking in the world, and a banner that slides
/// into the top-right corner and fades away (confirm shows it). Both are plain panels; the layer places them, and tweens
/// move them through their offset and opacity.
/// </summary>
public sealed class LayerExample : Scene
{
    private float _x = 60, _direction = 1;
    private Layer _layer = null!;

    /// <summary>The bubble over the character, for tests.</summary>
    public Panel Bubble { get; private set; } = null!;
    /// <summary>The banner in the corner, for tests.</summary>
    public Panel Banner { get; private set; } = null!;
    /// <summary>The character's position in the world (canvas pixels: this scene has no camera).</summary>
    public Vector2 Character => new(_x, 110);

    #region layer
    public override void Enter()
    {
        Bubble = new Panel(new Label("HELLO! A FINE NIGHT FOR A WALK.") { Width = 70, Wrap = true });
        Banner = new Panel(new Label("ACHIEVEMENT\nFIRST STEPS")) { Opacity = 0 };
        _layer = new Layer()
            .AddAt(Bubble, Head(), Anchor.Bottom)                       // its bottom centre over the head
            .Add(Banner, Anchor.TopRight, new Vector2(-4, 16));         // 4 units from the right, under the heading
        Widgets.Content = _layer;
    }

    /// <summary>The character's head in UI units: from the world to the canvas (no camera here), then to the UI view.</summary>
    private Vector2 Head() => Game.Views.CanvasToUi(Character - new Vector2(0, 2));
    #endregion

    #region follow
    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        _x += _direction * 40 * dt;
        if (_x is > 260 or < 60) _direction = -_direction;
        _layer.Move(Bubble, Head());                                    // every update: the bubble follows
        if (Input.Pressed("confirm")) ShowBanner();
    }
    #endregion

    #region banner
    /// <summary>Slides the banner in from the right as it fades in, then fades it out two seconds later.</summary>
    private void ShowBanner()
    {
        Tweens.Add(0.4f, t =>
        {
            Banner.Offset = new Vector2((1 - t) * 60, 0);
            Banner.Opacity = t;
        }, Ease.OutCubic);
        Tweens.Add(0.4f, t => Banner.Opacity = 1 - t, delay: 2.4f);
    }
    #endregion

    public override void Draw(IRenderer r)
    {
        Heading.Draw(r, Font, "BUBBLES AND BANNERS", "CONFIRM: BANNER");
        r.FillRect(new RectF(0, 130, 320, 2), UiTheme.Default.Muted);                  // the ground
        r.FillRect(new RectF(_x - 6, 110, 12, 20), Color.Hex("b13e53"));               // the character
    }
}

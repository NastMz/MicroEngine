using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// On-screen controls: a d-pad, a joystick and a toggle button for this screen only. They are drawn and read where the
/// platform has a touch screen (phones, tablets, touch laptops, mobile browsers); on other screens nothing appears.
/// </summary>
public sealed class TouchExample : Scene
{
    private TouchLayout _previous = null!;

    #region layout
    public override void Enter()
    {
        // A game normally declares one layout by overriding Game.CreateTouchLayout; a scene may switch to another.
        _previous = Game.Touch.Layout;
        Game.Touch.Layout = TouchLayout.Of(
            new TouchDPad { Offset = new(100, 100), Diagonals = false },
            new TouchStick("aim", "aim-left", "aim-right", "aim-up", "aim-down")
            {
                Placement = StickPlacement.Fixed, Anchor = TouchAnchor.BottomRight, Offset = new(100, 100),
            },
            new TouchButton("shield", "shield", "S") { Anchor = TouchAnchor.TopRight, Offset = new(40, 40), Mode = TouchButtonMode.Toggle },
            new TouchButton("back", "pause", "<") { Anchor = TouchAnchor.TopLeft, Offset = new(30, 30), Radius = 20 })
            with { Visibility = TouchVisibility.Always };
    }

    public override void Exit() => Game.Touch.Layout = _previous;
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) Game.Scenes.Pop();
    }

    public override void Draw(IRenderer r)
    {
        #region read
        // Touch controls press ordinary actions, so the game reads them like keys...
        var walk = new Vector2(Input.Axis("left", "right"), Input.Axis("up", "down"));
        // ...and an action no key is bound to (aim-*, shield) still works through the touch source.
        var aim = Input.Vector("aim-left", "aim-right", "aim-up", "aim-down");
        #endregion

        Font.Draw(r, $"TOUCH.SHOWN: {Game.Touch.Shown}   FINGERS: {Game.Touch.FingerCount}", new Vector2(8, 20), Color.White);
        Font.Draw(r, $"D-PAD {walk.X},{walk.Y}", new Vector2(8, 40), UiTheme.Default.Text);
        Font.Draw(r, $"STICK {aim.X:0.00},{aim.Y:0.00}  ({Game.Touch.ValueOf("aim").Length():0.00})", new Vector2(8, 52), UiTheme.Default.Text);
        Font.Draw(r, $"SHIELD {(Input.Down("shield") ? "ON" : "OFF")}", new Vector2(8, 64), UiTheme.Default.Text);
        Heading.Draw(r, Font, "TOUCH CONTROLS");
    }
}

using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// Effects on the whole window: the game's screen chain runs after the canvas was scaled into the window, at the
/// window's resolution, over the world and the interface alike. Here an old tube: curved glass, the lines of the
/// picture and darker corners, on the scene and on its widgets. Confirm turns it off and on. Where the graphics device
/// runs no shaders the tube is flat and shows its lines only.
/// </summary>
public sealed class ScreenEffectsExample : Scene
{
    private readonly Crt _tube = new() { Curvature = 0.06f, Scanlines = 0.3f, Mask = 0.2f };
    private readonly Vignette _corners = new() { Strength = 0.6f };
    private float _time;

    /// <summary>Whether the tube is on.</summary>
    public bool On => _tube.Enabled;

    #region screen
    public override void Enter()
    {
        // One line of the tube per row of the canvas, whatever the window's size.
        _tube.Lines = Game.Height;
        Game.ScreenPostProcess.Add(_tube).Add(_corners);

        Widgets.Anchor = Anchor.Center;
        Widgets.Content = new Panel(new Label("THE INTERFACE IS PART\nOF THE SCREEN'S PICTURE"));
    }

    // The chain belongs to the game, not to this scene: take out what this scene put in.
    public override void Exit()
    {
        Game.ScreenPostProcess.Remove(_tube);
        Game.ScreenPostProcess.Remove(_corners);
    }
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        if (Input.Pressed("confirm")) _tube.Enabled = _corners.Enabled = !_tube.Enabled;
        _time += dt;
    }

    public override void Draw(IRenderer r)
    {
        r.FillGradient(new RectF(0, 0, 320, 180), Color.Hex("29366f"), Color.Hex("3b5dc9"));
        for (int i = 0; i < 8; i++)
        {
            float x = (i * 44 + _time * 30) % 352 - 16;
            r.FillRect(new RectF(x, 132 - i % 3 * 14, 20, 48 + i % 3 * 14), Color.Hex(i % 2 == 0 ? "257179" : "38b764"));
        }
        r.FillRect(new RectF(0, 172, 320, 8), Color.Hex("1a1c2c"));
    }

    public override void DrawUi(IRenderer ui) =>
        Heading.Draw(ui, Font, "SCREEN EFFECTS", On ? "CONFIRM: OFF" : "CONFIRM: ON");
}

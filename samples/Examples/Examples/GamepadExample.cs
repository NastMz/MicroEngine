using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// Buttons, triggers and sticks through actions: the left stick moves (with intensity), the right trigger is analog,
/// and the bindings of an action can be listed. Keys do the same, so the example works without a gamepad.
/// </summary>
public sealed class GamepadExample : Scene
{
    private Vector2 _position = new(160, 100);

    #region bind
    public override void Enter()
    {
        // Actions can be bound at any time; usually a game does it once in Game.CreateInput.
        Input.Bind("boost", [Key.LeftShift], [PadButton.A, PadButton.RightShoulder])
             .BindAxis("throttle", PadAxis.RightTrigger)                 // 0..1 as the trigger is pressed
             .BindAxis("throttle", PadAxis.RightY, positive: false);     // or the right stick pushed up
    }

    public override void Exit() => Input.Unbind("boost").Unbind("throttle");
    #endregion

    #region read
    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }

        var move = Input.Vector("left", "right", "up", "down");   // length 0..1: a light push is a slow move
        float speed = 60 + 120 * Input.Value("throttle") + (Input.Down("boost") ? 60 : 0);
        _position += move * speed * dt;
        _position = Vector2.Clamp(_position, new Vector2(8, 20), new Vector2(312, 172));
    }
    #endregion

    public override void Draw(IRenderer r)
    {
        r.FillCircle(_position, 6, Input.Down("boost") ? Color.Hex("ffcd75") : Color.Hex("41a6f6"));
        r.FillRect(new RectF(8, 160, 100 * Input.Value("throttle"), 6), Color.Hex("a7f070"));
        r.DrawRect(new RectF(8, 160, 100, 6), Color.White);

        var bindings = Input.GetBindings("boost")!;
        Font.Draw(r, $"GAMEPAD: {(Input.PadConnected ? "CONNECTED" : "NONE")}   LAST DEVICE: {Input.LastDevice}", new Vector2(8, 16), Color.White);
        Font.Draw(r, $"BOOST = {string.Join(", ", bindings.Keys)} / {string.Join(", ", bindings.Buttons)}", new Vector2(8, 26), UiTheme.Default.Muted);
        Font.Draw(r, "THROTTLE (RIGHT TRIGGER)", new Vector2(112, 160), UiTheme.Default.Muted);
        Heading.Draw(r, Font, "GAMEPAD AND ANALOG INPUT");
    }
}

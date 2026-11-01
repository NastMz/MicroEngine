using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

#region source
/// <summary>
/// An action source of your own: anything that is not a key can press actions (an accessibility switch, a network
/// player, a replay). This one presses "confirm" once a second. The game reads it like any key.
/// </summary>
public sealed class Metronome : IActionSource
{
    public float Time;
    public bool IsDown(string action) => action == "confirm" && Time % 1f < 0.1f;
    public InputDevice Device => InputDevice.Other;
}
#endregion

/// <summary>Adds the Metronome while this screen is open.</summary>
public sealed class CustomSourceExample : Scene
{
    private readonly Metronome _metronome = new();
    private int _beats;

    #region use
    public override void Enter() => Input.AddSource(_metronome);
    public override void Exit() => Input.RemoveSource(_metronome);

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        _metronome.Time += dt;
        if (Input.Pressed("confirm")) _beats++;   // the metronome and the keyboard both count
    }
    #endregion

    public override void Draw(IRenderer r)
    {
        bool on = Input.Down("confirm");
        r.FillCircle(new Vector2(160, 90), on ? 24 : 18, on ? Color.Hex("ffcd75") : Color.Hex("566c86"));
        Font.DrawCentered(r, $"BEATS {_beats}   LAST DEVICE: {Input.LastDevice}", new Vector2(160, 130), UiTheme.Default.Text);
        Heading.Draw(r, Font, "CUSTOM INPUT SOURCE");
    }
}

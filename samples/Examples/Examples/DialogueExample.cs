using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>A dialogue box: pages revealed letter by letter with a blip, advanced with confirm, and a menu answer.</summary>
public sealed class DialogueExample : Scene
{
    #region dialogue
    private readonly DialogueBox _box = new() { Lines = 3 };
    private readonly SelectionList _answer = new("YES", "NO");
    private bool _asking;
    private string _result = "";

    public override void Enter()
    {
        _box.Blip += _ => Audio.TryPlay("select", 0.3f);      // a sound per letter, if the game registered one
        _box.Closed += () => _asking = true;
        _box.Show("GARDENER", "Welcome! The gems only grow at night.", "Will you help me collect them before sunrise?");
    }

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        if (_box.Update(dt, Input)) return;                   // true while the box is open: it owns confirm
        if (_asking && _answer.Update(Input) is { } choice)
        {
            _asking = false;
            _result = choice == 0 ? "THANK YOU!" : "OH... MAYBE TOMORROW.";
        }
    }

    public override void Draw(IRenderer r)
    {
        _box.Draw(r, Font, new RectF(10, 120, 300, 50));
        if (_asking) _answer.Draw(r, Font, new Vector2(140, 60), UiTheme.Default, Age);
        Font.DrawCentered(r, _result, new Vector2(160, 90), UiTheme.Default.Accent);
        Heading.Draw(r, Font, "DIALOGUE");
    }
    #endregion
}

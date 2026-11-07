using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.UI;
using CourtyardKey.UI;

namespace CourtyardKey.Scenes;

/// <summary>An overlay that freezes the world and plays paged dialogue with a typewriter blip.</summary>
public sealed class DialogueScene : Scene
{
    private readonly CourtyardKey.UI.DialogueBox _box = new() { Theme = UiStyle.Theme, Lines = 3 };
    private readonly string? _speaker;
    private readonly string[] _pages;
    private readonly Action? _closed;
    private int _blips;
    private bool _done;

    public DialogueScene(string? speaker, string[] pages, Action? closed)
    {
        _speaker = speaker;
        _pages = pages;
        _closed = closed;
    }

    public override bool IsOverlay => true;
    /// <summary>Only A (advance) and B on touch screens: the joystick and the other buttons leave the text.</summary>
    public override IReadOnlyCollection<string>? TouchActions { get; } = ["confirm", "cancel"];
    public bool Active => _box.Active;
    public string? Speaker => _box.Speaker;

    public override void Enter()
    {
        _box.Blip += _ => { if (_blips++ % 2 == 0) Audio.Play("blip", 0.6f, 1f, 0.04f); };
        _box.Closed += () => _done = true;
        // {confirm} and {action} in a page name the player's own controls (key, pad button or touch button).
        _box.Prompts = Game.Prompts;
        _box.Show(_speaker, _pages);
    }

    /// <summary>
    /// State hash: whether the box is open, the current page's text, the blip count (it follows the typewriter) and
    /// whether it closed. The speaker and pages are fixed when the scene is made; the box's line wrapping is made while
    /// drawing and stays out.
    /// </summary>
    public override void HashState(StateHasher hash)
    {
        hash.Add(_box.Active);
        hash.Add(_box.Text);
        hash.Add(_blips);
        hash.Add(_done);
    }

    public override void Update(float dt)
    {
        if (_done) return;
        _box.Update(dt, Input);
        if (_done)
        {
            Game.Scenes.Pop();
            _closed?.Invoke();
        }
    }

    public override void Draw(IRenderer r)
    {
        var area = new RectF(10, 128, 300, 46);
        _box.Draw(r, Font, area);
    }
}

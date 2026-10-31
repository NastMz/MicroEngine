using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;
using CourtyardKey.UI;

namespace CourtyardKey.Scenes;

/// <summary>An overlay that freezes the world and plays paged dialogue with a typewriter blip.</summary>
public sealed class DialogueScene : Scene
{
    private readonly DialogueBox _box = new() { Theme = UiStyle.Theme, Lines = 3 };
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
    public bool Active => _box.Active;
    public string? Speaker => _box.Speaker;

    public override void Enter()
    {
        _box.Blip += _ => { if (_blips++ % 2 == 0) Audio.Play("blip", 0.6f, 1f, 0.04f); };
        _box.Closed += () => _done = true;
        _box.Show(_speaker, _pages);
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

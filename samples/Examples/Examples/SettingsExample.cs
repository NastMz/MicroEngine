using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// A settings screen on the widget tree: sliders for the music and effects volumes, a toggle and a value selector. The
/// three controls are not engine types: each is a small widget of the game's own, built on the widget primitives (the
/// focused widget's left and right actions, the pointer's press and drag, the wheel). Copy them and draw them your way.
/// </summary>
public sealed class SettingsExample : Scene
{
    private Toggle _hints = null!;
    private Choice _difficulty = null!;

    #region screen
    public override void Enter()
    {
        var music = new Slider(Audio.MusicVolume);
        music.Changed += value => Audio.MusicVolume = value;
        var effects = new Slider(Audio.SfxVolume);
        effects.Changed += value =>
        {
            Audio.SfxVolume = value;
            Audio.TryPlay("select");                       // hear the new level, if the game has the sound
        };
        _hints = new Toggle(on: true);
        _difficulty = new Choice("EASY", "NORMAL", "HARD") { Index = 1 };

        Widgets.Content = new Panel(new Stack()
            .Add(Row("MUSIC", music))
            .Add(Row("EFFECTS", effects))
            .Add(Row("HINTS", _hints))
            .Add(Row("DIFFICULTY", _difficulty))
            .Add(new Button("BACK", () => Game.Scenes.Pop())));
        Widgets.Cancelled += () => Game.Scenes.Pop();
    }

    /// <summary>
    /// A label and a control, both of fixed width. Focus moves to the nearest widget in a direction, centre to centre, so
    /// controls of one width in one column make up and down go straight from row to row.
    /// </summary>
    private static Widget Row(string text, Widget control)
    {
        control.Width = 80;
        return new Stack { Orientation = Orientation.Horizontal, Align = Align.Center }
            .Add(new Label(text) { Width = 64 })
            .Add(control);
    }
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) Game.Scenes.Pop();
    }

    public override void Draw(IRenderer r)
    {
        Heading.Draw(r, Font, "SETTINGS");
        Font.Draw(r, $"HINTS {(_hints.On ? "ON" : "OFF")}   DIFFICULTY {_difficulty.Value}", new Vector2(8, 168), UiTheme.Default.Muted);
    }
}

#region slider
/// <summary>
/// A value from 0 to 1. Left and right step it while it has the focus; a press on it and a drag set it from the pointer's
/// position (the slider holds the press, so the drag may leave it); the wheel steps it while it has the focus.
/// </summary>
public sealed class Slider(float value = 0, float step = 0.1f) : Widget
{
    private float _value = Math.Clamp(value, 0, 1);

    /// <summary>The value, 0..1.</summary>
    public float Value
    {
        get => _value;
        set
        {
            float v = Math.Clamp(value, 0, 1);
            if (v == _value) return;
            _value = v;
            Changed?.Invoke(v);
        }
    }

    public float Step { get; set; } = step;
    public event Action<float>? Changed;
    public override bool Focusable => true;

    protected override Vector2 MeasureContent(UiContext context) => new(80 * context.Unit, context.Font.LineHeight + 4 * context.Unit);

    protected override bool OnAction(UiAction action) => action switch
    {
        UiAction.Left => Nudge(-1),
        UiAction.Right => Nudge(1),
        _ => false,
    };

    protected override void OnPointerDown(Vector2 position) => Follow(position);
    protected override void OnPointerMove(Vector2 position) => Follow(position);
    protected override bool OnWheel(Vector2 delta) => IsFocused && delta.Y != 0 && Nudge(MathF.Sign(delta.Y));

    private void Follow(Vector2 position) => Value = (position.X - Bounds.X) / Bounds.Width;

    private bool Nudge(int steps)
    {
        Value = MathF.Round(Value / Step + steps) * Step;   // on the step grid, however the value was set
        return true;
    }

    protected override void DrawContent(IRenderer r, UiContext context)
    {
        var theme = context.Theme;
        float u = context.Unit, mid = MathF.Round(Bounds.Y + Bounds.Height / 2);
        r.FillRect(new RectF(Bounds.X, mid - u, Bounds.Width, 2 * u), theme.Muted);
        r.FillRect(new RectF(Bounds.X, mid - u, MathF.Round(Bounds.Width * Value), 2 * u), theme.Accent);
        float x = MathF.Round(Bounds.X + Bounds.Width * Value);
        r.FillRect(new RectF(x - 2 * u, Bounds.Y + u, 4 * u, Bounds.Height - 2 * u), IsFocused ? theme.Accent : theme.Text);
    }

    protected override void HashState(StateHasher hash) => hash.Add(_value);
}
#endregion

#region toggle
/// <summary>On or off: confirm while it has the focus, or a click, switches it.</summary>
public sealed class Toggle(bool on = false) : Widget
{
    private bool _on = on;

    public bool On
    {
        get => _on;
        set
        {
            if (value == _on) return;
            _on = value;
            Changed?.Invoke(value);
        }
    }

    public event Action<bool>? Changed;
    public override bool Focusable => true;

    protected override Vector2 MeasureContent(UiContext context) => new(context.Font.LineHeight + 4 * context.Unit);
    protected override bool OnAction(UiAction action)
    {
        if (action != UiAction.Confirm) return false;
        On = !On;
        return true;
    }

    protected override void OnClick() => On = !On;

    protected override void DrawContent(IRenderer r, UiContext context)
    {
        var theme = context.Theme;
        var box = new RectF(Bounds.X, Bounds.Y, Bounds.Height, Bounds.Height);   // a square at the start of its bounds
        r.Panel(box, IsFocused ? theme.Panel with { Border = theme.Accent } : theme.Panel);
        float inset = 3 * context.Unit;
        if (On) r.FillRect(new RectF(box.X + inset, box.Y + inset, box.Width - 2 * inset, box.Height - 2 * inset), theme.Accent);
    }

    protected override void HashState(StateHasher hash) => hash.Add(_on);
}
#endregion

#region choice
/// <summary>One of several values: left and right while it has the focus, or a click, change it.</summary>
public sealed class Choice(params string[] options) : Widget
{
    private int _index;

    public IReadOnlyList<string> Options { get; } = options;
    public int Index { get => _index; set => _index = ((value % Options.Count) + Options.Count) % Options.Count; }
    public string Value => Options[_index];
    public override bool Focusable => true;

    protected override Vector2 MeasureContent(UiContext context) =>
        new(Options.Max(o => context.Font.MeasureWidth(o)) + context.Font.MeasureWidth("<  >"), context.Font.LineHeight + 4 * context.Unit);

    protected override bool OnAction(UiAction action)
    {
        if (action is not (UiAction.Left or UiAction.Right)) return false;
        Index += action == UiAction.Right ? 1 : -1;
        return true;
    }

    protected override void OnClick() => Index++;

    protected override void DrawContent(IRenderer r, UiContext context)
    {
        var color = IsFocused ? context.Theme.Accent : context.Theme.Text;
        string text = $"< {Value} >";
        float x = MathF.Round(Bounds.X + (Bounds.Width - context.Font.MeasureWidth(text)) / 2);
        context.Font.Draw(r, text, new Vector2(x, Bounds.Y + 2 * context.Unit), color);
    }

    protected override void HashState(StateHasher hash) => hash.Add(_index);
}
#endregion

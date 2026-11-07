using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Platform;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// A controls screen: choose an action, press the key or gamepad button it should use. A control another gameplay
/// action already had is swapped between them, the defaults come back on request, and the bindings are saved.
/// </summary>
public sealed class RebindExample(string? saveDirectory) : Scene
{
    #region defaults
    /// <summary>The actions the player may rebind: gameplay only. Menu actions keep their keys, so menus always work.</summary>
    public static readonly string[] Rebindable = ["jump", "fire"];

    /// <summary>The game's own defaults for those actions, applied at start and by "reset".</summary>
    public static void BindDefaults(InputMap map) => map
        .Unbind("jump").Bind("jump", [Key.Z, Key.Space], [PadButton.A])
        .Unbind("fire").Bind("fire", [Key.X], [PadButton.X]);
    #endregion

    private readonly Label _status = new();
    private readonly Dictionary<string, Label> _shown = [];
    private IStorage _storage = null!;
    private IDisposable? _listening;

    #region load
    public override void Enter()
    {
        _storage = saveDirectory is null ? SaveStore.DefaultStorage("MicroEngineExamples") : new FileSaveStorage(saveDirectory);
        BindDefaults(Input);
        // Saved bindings replace the defaults; false (nothing saved, or unreadable and set aside) keeps them.
        _status.Text = Input.LoadBindings(_storage) ? "LOADED SAVED CONTROLS" : "DEFAULT CONTROLS";

        var rows = new Stack { Spacing = 3, Align = Align.Start };
        foreach (var action in Rebindable)
        {
            _shown[action] = new Label { Color = UiTheme.Default.Muted };
            rows.Add(new Stack { Orientation = Orientation.Horizontal, Spacing = 8 }
                .Add(new Button(action.ToUpperInvariant(), () => Listen(action)) { Width = 50 })
                .Add(_shown[action]));
        }
        rows.Add(new Button("RESET TO DEFAULTS", Reset));
        Widgets.Content = new Stack { Spacing = 10, Align = Align.Start }.Add(rows).Add(_status);
    }
    #endregion

    public override void Exit()
    {
        _listening?.Dispose();
        foreach (var action in Rebindable) Input.Unbind(action);
    }

    #region listen
    private void Listen(string action)
    {
        if (_listening is not null) return;                       // already waiting for a press
        _status.Text = $"PRESS A KEY OR BUTTON FOR {action.ToUpperInvariant()} (ESC: KEEP)";
        // The next press goes to the listener: not to gameplay, and not to the buttons.
        _listening = Game.InputSystem.ListenForPress(press =>
        {
            _listening = null;
            if (press is { Kind: InputEventKind.KeyDown, Key: Key.Escape }) { _status.Text = "UNCHANGED"; return; }
            Assign(Input, action, press);
            Save($"{action.ToUpperInvariant()} CHANGED");
        });
    }

    private void Reset()
    {
        BindDefaults(Input);
        Save("DEFAULTS RESTORED");
    }

    public override void Update(float dt)
    {
        if (_listening is null && Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        foreach (var (action, label) in _shown)
        {
            var b = Input.GetBindings(action)!;
            label.Text = string.Join(" ", b.Keys.Select(k => k.ToString()).Concat(b.Buttons.Select(p => "PAD " + p))).ToUpperInvariant();
        }
    }
    #endregion

    #region assign
    /// <summary>
    /// Binds <paramref name="press"/> to <paramref name="action"/>, replacing its keys (for a key) or buttons (for a
    /// button). Another rebindable action that used the same control gets the ones <paramref name="action"/> gave up, so
    /// no control presses two gameplay actions and no action is left without one.
    /// </summary>
    public static void Assign(InputMap map, string action, in InputEvent press)
    {
        bool key = press.Kind == InputEventKind.KeyDown;
        string control = key ? press.Key.ToString() : press.Button.ToString();
        var set = map.ExportBindings();                            // every action's bindings as editable data
        List<string> Controls(ActionBindingData a) => key ? a.Keys : a.Buttons;
        var given = Controls(set.Actions.Single(a => a.Action == action));
        if (given.Contains(control)) return;                       // already this action's control: nothing to do

        var changed = new InputBindingSet();
        foreach (var other in set.Actions.Where(a => a.Action != action && Rebindable.Contains(a.Action)))
        {
            var controls = Controls(other);
            if (!controls.Remove(control)) continue;
            controls.AddRange(given.Except(controls));             // the swap
            changed.Actions.Add(other);
        }
        map.ImportBindings(changed);                               // only the actions that lost the control
        map.BindFrom(action, press, replace: true);
    }
    #endregion

    #region save
    private void Save(string done)
    {
        try
        {
            Input.SaveBindings(_storage);
            _status.Text = done;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            _status.Text = "COULD NOT SAVE: THE CHANGE LASTS UNTIL YOU QUIT";
        }
    }
    #endregion

    public override void Draw(IRenderer r)
    {
        Heading.Draw(r, Font, "CONTROLS");
        Font.Draw(r, "TRY THEM:", new Vector2(16, 160), Color.White);
        if (Input.Down("jump")) Font.Draw(r, "JUMP", new Vector2(80, 160), Color.White);
        if (Input.Down("fire")) Font.Draw(r, "FIRE", new Vector2(120, 160), Color.White);
    }
}

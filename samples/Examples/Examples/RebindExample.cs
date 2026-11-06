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

    private SelectionList _list = null!;
    private IStorage _storage = null!;
    private IDisposable? _listening;
    private string _status = "";

    #region load
    public override void Enter()
    {
        _list = new SelectionList([.. Rebindable, "reset to defaults"]);
        _storage = saveDirectory is null ? SaveStore.DefaultStorage("MicroEngineExamples") : new FileSaveStorage(saveDirectory);
        BindDefaults(Input);
        // Saved bindings replace the defaults; false (nothing saved, or unreadable and set aside) keeps them.
        _status = Input.LoadBindings(_storage) ? "LOADED SAVED CONTROLS" : "DEFAULT CONTROLS";
    }
    #endregion

    public override void Exit()
    {
        _listening?.Dispose();
        foreach (var action in Rebindable) Input.Unbind(action);
    }

    #region listen
    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        if (_listening is not null) return;                       // the next press goes to the listener, not here
        if (_list.Update(Input) is not { } choice) return;
        if (choice == Rebindable.Length)
        {
            BindDefaults(Input);
            Save("DEFAULTS RESTORED");
            return;
        }
        string action = Rebindable[choice];
        _status = $"PRESS A KEY OR BUTTON FOR {action.ToUpperInvariant()} (ESC: KEEP)";
        _listening = Game.InputSystem.ListenForPress(press =>
        {
            _listening = null;
            if (press is { Kind: InputEventKind.KeyDown, Key: Key.Escape }) { _status = "UNCHANGED"; return; }
            Assign(Input, action, press);
            Save($"{action.ToUpperInvariant()} CHANGED");
        });
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
            _status = done;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            _status = "COULD NOT SAVE: THE CHANGE LASTS UNTIL YOU QUIT";
        }
    }
    #endregion

    public override void Draw(IRenderer r)
    {
        Heading.Draw(r, Font, "CONTROLS");
        _list.Draw(r, Font, new Vector2(16, 24), UiTheme.Default, Age, lineHeight: 12);
        for (int i = 0; i < Rebindable.Length; i++)
        {
            var b = Input.GetBindings(Rebindable[i])!;
            string text = string.Join(" ", b.Keys.Select(k => k.ToString()).Concat(b.Buttons.Select(p => "PAD " + p))).ToUpperInvariant();
            Font.Draw(r, text, new Vector2(120, 24 + i * 12), UiTheme.Default.Muted);
        }
        Font.Draw(r, $"STATUS: {_status}", new Vector2(16, 80), UiTheme.Default.Accent);
        Font.Draw(r, "TRY THEM:", new Vector2(16, 100), Color.White);
        if (Input.Down("jump")) Font.Draw(r, "JUMP", new Vector2(80, 100), Color.White);
        if (Input.Down("fire")) Font.Draw(r, "FIRE", new Vector2(120, 100), Color.White);
    }
}

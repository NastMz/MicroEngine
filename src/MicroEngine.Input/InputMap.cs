using System.Numerics;
using MicroEngine.Kernel;

namespace MicroEngine.Input;

/// <summary>What an action is bound to, for display or a rebinding screen (<see cref="InputMap.GetBindings"/>).</summary>
/// <param name="Keys">The keys that press the action.</param>
/// <param name="Buttons">The gamepad buttons that press it.</param>
/// <param name="Stick">The left-stick direction that presses it, or <c>None</c>.</param>
/// <param name="Axes">The gamepad axis sides that press it.</param>
public sealed record ActionBindings(IReadOnlyList<Key> Keys, IReadOnlyList<PadButton> Buttons, InputMap.StickDirection Stick, IReadOnlyList<(PadAxis Axis, bool Positive)> Axes)
{
    /// <summary>The key combinations that press the action (Alt+Enter).</summary>
    public IReadOnlyList<KeyChord> Chords { get; init; } = [];
    /// <summary>
    /// Every control bound to the action, mouse buttons, the wheel, pointer motion and the right stick included, in
    /// the order the map reads them: by <see cref="InputControlKind"/>, then in the order they were bound. The other
    /// members are views of this list by kind.
    /// </summary>
    public IReadOnlyList<InputControl> Controls { get; init; } = [];
}

/// <summary>
/// A key pressed while a modifier is held: <c>new KeyChord(Key.Enter, Key.LeftAlt, Key.RightAlt)</c> is Alt+Enter with
/// either Alt key. The chord is down while <see cref="Key"/> and any one of <see cref="Modifiers"/> are down.
/// </summary>
public sealed record KeyChord
{
    /// <summary>A chord of <paramref name="key"/> with any one of <paramref name="modifiers"/> (at least one).</summary>
    /// <exception cref="ArgumentException">No modifier is given.</exception>
    public KeyChord(Key key, params Key[] modifiers)
    {
        if (modifiers is null || modifiers.Length == 0) throw new ArgumentException("A chord needs at least one modifier key.", nameof(modifiers));
        Key = key;
        Modifiers = [.. modifiers];
    }

    /// <summary>The key that is pressed.</summary>
    public Key Key { get; }
    /// <summary>The keys of which one must be held (both Alt keys, both Control keys...).</summary>
    public IReadOnlyList<Key> Modifiers { get; }
}

/// <summary>
/// Named actions bound to controls (<see cref="InputControl"/>: keys, gamepad buttons, stick directions and analog
/// axes, mouse buttons, the wheel and pointer motion), plus any number of action sources (touch controls, custom
/// sources). Games query actions ("jump", "confirm"), never physical keys, so bindings and
/// scripted input stay in one place. Every action is both digital (<see cref="Down"/>, <see cref="Pressed"/>,
/// <see cref="Released"/>) and analog (<see cref="Value"/>, <see cref="Vector"/>): a key is 0 or 1, a stick or a
/// virtual joystick gives the deflection beyond its dead zone.
/// Edge states (pressed/released) are computed per fixed update, so a tap is never lost or doubled.
/// </summary>
/// <example>
/// <code>
/// // In a Game subclass: keep the defaults and add a "dash" action.
/// protected override InputMap CreateInput() => InputMap.CreateDefault()
///     .Bind("dash", [Key.LeftShift, Key.N], [PadButton.RightShoulder]);
///
/// // In a scene's Update:
/// var move = Input.Vector("left", "right", "up", "down");
/// if (Input.Pressed("confirm")) Audio.Play("select");
/// </code>
/// </example>
public sealed partial class InputMap : IStateHashed
{
    private sealed class Binding
    {
        /// <summary>The controls, kept in reading order: by kind, then as bound.</summary>
        public List<InputControl> Controls { get; } = [];

        public bool Add(InputControl control)
        {
            if (control.Kind == InputControlKind.None || Controls.Contains(control)) return false;
            int at = Controls.Count;
            while (at > 0 && Controls[at - 1].Kind > control.Kind) at--;
            Controls.Insert(at, control);
            return true;
        }

        public InputControl? LeftStick()
        {
            foreach (var c in Controls)
                if (c.Kind == InputControlKind.Stick && c.Stick == PadStick.Left) return c;
            return null;
        }
    }

    private readonly Dictionary<string, Binding> _bindings = new(StringComparer.Ordinal);
    private readonly HashSet<string> _down = new(StringComparer.Ordinal);
    private readonly HashSet<string> _previous = new(StringComparer.Ordinal);
    private readonly HashSet<string> _latched = new(StringComparer.Ordinal);
    private readonly HashSet<string> _forced = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _values = new(StringComparer.Ordinal);
    private readonly HashSet<string> _consumed = new(StringComparer.Ordinal);
    // Chord precedence: a key pressed while one of its chords' modifiers was held belongs to the chord until it is let
    // go, so it presses no single-key binding (Alt+Enter is not also Enter). The chord keys down at the previous sample
    // tell a new press from a held key.
    private readonly HashSet<Key> _chorded = [];
    private readonly HashSet<Key> _chordKeysDown = [];
    private readonly List<Key> _released = [];
    private readonly List<IActionSource> _sources = [];
    private readonly List<PointerInfo> _pointers = [];
    private PointerState _pointer;
    // Wheel controls press once per whole notch: what a smooth wheel has turned towards the next one, per direction
    // (indexed by StickDirection), and the directions that completed a notch in this update.
    private readonly float[] _wheelTurned = new float[5];
    private readonly bool[] _wheelNotch = new bool[5];
    private readonly HashSet<string> _impulses = new(StringComparer.Ordinal);
    private PointerButtons _previousButtons;
    // What the bindings use, found again after they change: maps with no wheel or pointer control do none of that work.
    private bool _usesChanged = true, _usesWheel, _usesPointer;

    /// <summary>
    /// The direction of a stick an action is bound to (<c>None</c>, <c>Left</c>, <c>Right</c>, <c>Up</c>,
    /// <c>Down</c>). The action is down when the stick points that way beyond <see cref="StickThreshold"/>, and its
    /// <see cref="Value"/> is the deflection along that direction. The wheel and pointer motion name their directions
    /// with it too (<see cref="InputControl.Direction"/>).
    /// </summary>
    public enum StickDirection { None, Left, Right, Up, Down }

    /// <summary>Radial dead zone of the left stick, as a fraction of full deflection (default 0.2). Values beyond it are rescaled to 0..1.</summary>
    public float StickDeadZone { get; set; } = 0.2f;
    /// <summary>Radial dead zone of the right stick, for the directions bound with <see cref="BindStick"/> (default 0.2). An axis bound with <see cref="BindAxis"/> uses <see cref="AxisDeadZone"/>.</summary>
    public float RightStickDeadZone { get; set; } = 0.2f;
    /// <summary>Deflection (after the dead zone) at which a stick direction counts as down (default 0.35).</summary>
    public float StickThreshold { get; set; } = 0.35f;
    /// <summary>
    /// The <see cref="Value"/> one virtual canvas pixel of pointer motion gives an action bound to it (default 1):
    /// the game's mouse sensitivity.
    /// </summary>
    public float PointerSensitivity { get; set; } = 1f;
    /// <summary>How a stick presses direction actions: eight sectors (default) or four.</summary>
    public StickDirections StickMode { get; set; } = StickDirections.Eight;
    /// <summary>Dead zone of bound triggers and other single axes (default 0.1).</summary>
    public float AxisDeadZone { get; set; } = 0.1f;
    /// <summary>The device that most recently pressed an action. Touch controls use it to hide while a keyboard, mouse or gamepad plays.</summary>
    public InputDevice LastDevice { get; private set; }
    /// <summary>Whether a gamepad was connected at the last sample.</summary>
    public bool PadConnected { get; private set; }
    /// <summary>The family of the gamepad at the last sample (Xbox when unknown), for prompts.</summary>
    public GamepadStyle PadStyle { get; private set; }
    /// <summary>
    /// The pointer at the last sample: the mouse over the virtual canvas, or a finger on the screen that no on-screen
    /// control took (whichever moved last), with every button's state and edges
    /// (<see cref="PointerState.Pressed"/>, <see cref="PointerState.Released"/>) and the wheel's turn and the pointer's
    /// motion since the previous update. Clicks and wheel turns an input layer took (a widget, an on-screen control) are
    /// not in it.
    /// </summary>
    public PointerState Pointer => _pointer;
    /// <summary>
    /// Every pointer at the last sample: the mouse while over the window, and each finger or pen on the screen that no
    /// input layer (such as the on-screen controls) took, in device and id order. Tap-to-move, drag and pinch read it.
    /// </summary>
    public IReadOnlyList<PointerInfo> Pointers => _pointers;
    /// <summary>
    /// The engine's own action names this map uses (menus, dialogs, on-screen controls and prompts read actions by
    /// these names). Rename them here when the game calls its actions differently; the default names are
    /// <see cref="ActionNames.Default"/>.
    /// </summary>
    public ActionNames Names { get; set; } = ActionNames.Default;
    /// <summary>The action sources read with the devices (<see cref="AddSource"/>).</summary>
    public IReadOnlyList<IActionSource> Sources => _sources;

    /// <summary>Binds keys to <paramref name="action"/>, adding to its existing bindings.</summary>
    /// <param name="action">The action name (case-sensitive, not empty).</param>
    /// <param name="keys">Keys that press the action.</param>
    /// <returns>This map, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty.</exception>
    public InputMap Bind(string action, params Key[] keys) => Bind(action, keys, [], StickDirection.None);

    /// <summary>Binds keys, gamepad buttons and optionally a left-stick direction to <paramref name="action"/>, adding to its existing bindings (use <see cref="Unbind(string)"/> first to replace them).</summary>
    /// <param name="action">The action name (case-sensitive, not empty).</param>
    /// <param name="keys">Keys that press the action.</param>
    /// <param name="buttons">Gamepad buttons that press the action.</param>
    /// <param name="stick">A left-stick direction that presses the action; <c>None</c> keeps the current one.</param>
    /// <returns>This map, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty.</exception>
    public InputMap Bind(string action, Key[] keys, PadButton[] buttons, StickDirection stick = StickDirection.None)
    {
        var b = Get(action);
        foreach (var key in keys) b.Add(key);
        foreach (var button in buttons) b.Add(button);
        if (stick != StickDirection.None)
        {
            // An action follows one direction of the left stick: a new one replaces it.
            if (b.LeftStick() is { } old) b.Controls.Remove(old);
            b.Add(InputControl.Of(PadStick.Left, stick));
        }
        _usesChanged = true;
        return this;
    }

    /// <summary>Binds key combinations (<see cref="KeyChord"/>: Alt+Enter, Ctrl+S) to <paramref name="action"/>, adding to its existing bindings.</summary>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty.</exception>
    public InputMap Bind(string action, params KeyChord[] chords)
    {
        var b = Get(action);
        foreach (var chord in chords) b.Add(chord);
        _usesChanged = true;
        return this;
    }

    /// <summary>
    /// Binds any controls to <paramref name="action"/>, adding to its existing bindings: keys, gamepad buttons and
    /// mouse buttons by themselves, the rest by name
    /// (<c>Bind("zoom-in", InputControl.WheelUp, Key.Equals)</c>, <c>Bind("order", PointerButton.Secondary)</c>).
    /// A control the action already has is not added again.
    /// </summary>
    /// <remarks>
    /// A mouse button presses the action like a key. The primary button is also a finger or pen on the screen. A wheel
    /// direction presses it for one update per notch, and the action's <see cref="Value"/> is the notches turned in
    /// that update. Pointer motion gives the action a <see cref="Value"/> (pixels moved in the update times
    /// <see cref="PointerSensitivity"/>) and never presses it; read it with <see cref="AnalogAxis"/> or
    /// <see cref="Motion"/>. Clicks and wheel turns an input layer took (a widget under the pointer, an on-screen
    /// control) press nothing.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty.</exception>
    public InputMap Bind(string action, params InputControl[] controls)
    {
        ArgumentNullException.ThrowIfNull(controls);
        var b = Get(action);
        foreach (var control in controls) b.Add(control);
        _usesChanged = true;
        return this;
    }

    /// <summary>
    /// Binds one side of an analog axis: <c>BindAxis("aim-right", PadAxis.RightX, positive: true)</c>, or a trigger
    /// (<c>BindAxis("fire", PadAxis.RightTrigger)</c>). The action is down beyond half travel and its
    /// <see cref="Value"/> follows the axis.
    /// </summary>
    public InputMap BindAxis(string action, PadAxis axis, bool positive = true) => Bind(action, InputControl.Of(axis, positive));

    /// <summary>
    /// Binds a direction of a stick: <c>BindStick("aim-up", PadStick.Right, StickDirection.Up)</c>. The stick is read
    /// as a whole, with its radial dead zone (<see cref="StickDeadZone"/>, <see cref="RightStickDeadZone"/>): the
    /// action is down when it points that way beyond <see cref="StickThreshold"/> and its <see cref="Value"/> is the
    /// deflection along that direction, so four actions give <see cref="Vector"/> a round aim.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty, or <paramref name="direction"/> is <c>None</c>.</exception>
    public InputMap BindStick(string action, PadStick stick, StickDirection direction) => Bind(action, InputControl.Of(stick, direction));

    /// <summary>
    /// Removes every control bound to the action (the action stays, unbound), to rebind or turn it off. An action the
    /// map does not have yet becomes one with no binding, so the engine adds no default binding to it
    /// (<see cref="ActionNames.Fullscreen"/>).
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty.</exception>
    public InputMap Unbind(string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        _bindings[action] = new Binding();
        _usesChanged = true;
        return this;
    }

    /// <summary>Removes one control from the action's bindings; false when the action does not have it.</summary>
    public bool Unbind(string action, InputControl control)
    {
        if (!_bindings.TryGetValue(action, out var b) || !b.Controls.Remove(control)) return false;
        _usesChanged = true;
        return true;
    }

    /// <summary>The actions <paramref name="control"/> is bound to, for a rebinding screen that must not give one control to two actions.</summary>
    public IEnumerable<string> ActionsBoundTo(InputControl control)
    {
        foreach (var (action, b) in _bindings)
            if (b.Controls.Contains(control)) yield return action;
    }

    /// <summary>What the action is bound to; null for an unknown action.</summary>
    public ActionBindings? GetBindings(string action)
    {
        if (!_bindings.TryGetValue(action, out var b)) return null;
        var keys = new List<Key>();
        var buttons = new List<PadButton>();
        var axes = new List<(PadAxis, bool)>();
        var chords = new List<KeyChord>();
        foreach (var c in b.Controls)
            switch (c.Kind)
            {
                case InputControlKind.Key: keys.Add(c.Key); break;
                case InputControlKind.Chord: chords.Add(c.Chord!); break;
                case InputControlKind.PadButton: buttons.Add(c.Button); break;
                case InputControlKind.PadAxis: axes.Add((c.Axis, c.Positive)); break;
            }
        return new ActionBindings(keys, buttons, b.LeftStick()?.Direction ?? StickDirection.None, axes) { Chords = chords, Controls = [.. b.Controls] };
    }

    private Binding Get(string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        if (!_bindings.TryGetValue(action, out var b)) _bindings[action] = b = new Binding();
        return b;
    }

    /// <summary>The names of every bound action (including actions unbound with <see cref="Unbind(string)"/>).</summary>
    public IEnumerable<string> Actions => _bindings.Keys;

    /// <summary>Adds an action source read with the devices at every sample. Its actions need no binding, but unbound names are listed in <see cref="Actions"/> only once bound.</summary>
    public InputMap AddSource(IActionSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!_sources.Contains(source)) _sources.Add(source);
        return this;
    }

    /// <summary>Stops reading <paramref name="source"/>; returns false when it was not added.</summary>
    public bool RemoveSource(IActionSource source) => _sources.Remove(source);

    /// <summary>Samples devices and action sources. Called by the runner before each fixed update.</summary>
    public void Sample(IInputSource source)
    {
        PadConnected = source.PadConnected;
        PadStyle = source.PadStyle;
        var pointer = source.Pointer;
        var buttons = pointer.Buttons;
        _pointer = pointer with { PressedButtons = buttons & ~_previousButtons, ReleasedButtons = _previousButtons & ~buttons };
        _pointers.Clear();
        _pointers.AddRange(source.Pointers);
        if (_usesChanged) FindUses();
        UpdateChords(source);
        if (_usesWheel) UpdateWheel(pointer.Wheel);
        _impulses.Clear();
        var stick = ApplyDeadZone(source.LeftStick, StickDeadZone);
        var stickDown = Directions(stick, StickThreshold, StickMode);
        // The right stick is read only by the maps that bind a direction of it.
        Vector2 rightStick = default;
        IReadOnlySet<StickDirection>? rightDown = null;
        foreach (var (action, b) in _bindings)
        {
            var device = InputDevice.None;
            float value = 0;
            bool down = false, impulse = false;
            if (_forced.Contains(action)) { down = true; value = 1; device = InputDevice.Other; }
            // One pass over the controls, which are kept in reading order: digital ones first (the first one down
            // decides the device), then the analog ones, whose largest value wins. A plain loop, no LINQ: this runs for
            // every action at every update.
            foreach (var c in b.Controls)
                switch (c.Kind)
                {
                    case InputControlKind.Key:
                        if (!down && source.IsKeyDown(c.Key) && !_chorded.Contains(c.Key)) { down = true; value = 1; device = InputDevice.Keyboard; }
                        break;
                    case InputControlKind.Chord:
                        if (!down && _chorded.Contains(c.Key) && source.IsKeyDown(c.Key) && AnyDown(source, c.Chord!.Modifiers)) { down = true; value = 1; device = InputDevice.Keyboard; }
                        break;
                    case InputControlKind.PadButton:
                        if (!down && source.IsPadDown(c.Button)) { down = true; value = 1; device = InputDevice.Gamepad; }
                        break;
                    case InputControlKind.PointerButton:
                        if (!down && pointer.Down(c.PointerButton)) { down = true; value = 1; device = pointer.Kind == PointerKind.Touch ? InputDevice.Touch : InputDevice.Mouse; }
                        break;
                    case InputControlKind.Wheel:
                    {
                        float v = Component(pointer.Wheel, c.Direction, wheel: true);
                        if (v > value) value = v;
                        if (_wheelNotch[(int)c.Direction])
                        {
                            impulse = true;
                            if (!down) { down = true; device = InputDevice.Mouse; }
                        }
                        break;
                    }
                    case InputControlKind.Stick:
                    {
                        bool pressed;
                        float v;
                        if (c.Stick == PadStick.Left)
                        {
                            v = Component(stick, c.Direction);
                            pressed = stickDown.Contains(c.Direction);
                        }
                        else
                        {
                            if (rightDown is null)
                            {
                                rightStick = ApplyDeadZone(source.RightStick, RightStickDeadZone);
                                rightDown = Directions(rightStick, StickThreshold, StickMode);
                            }
                            v = Component(rightStick, c.Direction);
                            pressed = rightDown.Contains(c.Direction);
                        }
                        if (v > value) value = v;
                        if (!down && pressed) { down = true; device = InputDevice.Gamepad; }
                        break;
                    }
                    case InputControlKind.PadAxis:
                    {
                        float raw = source.GetPadAxis(c.Axis);
                        float v = Rescale(c.Positive ? raw : -raw, AxisDeadZone);
                        if (v > value) value = v;
                        if (!down && v >= 0.5f) { down = true; device = InputDevice.Gamepad; }
                        break;
                    }
                    case InputControlKind.PointerMotion:
                    {
                        float v = Component(pointer.Delta, c.Direction) * PointerSensitivity;
                        if (v > value) value = v;
                        break;
                    }
                }
            foreach (var s in _sources)
            {
                float v = s.Value(action);
                if (v > value) value = v;
                if (!down && s.IsDown(action)) { down = true; value = Math.Max(value, v); device = s.Device; }
            }
            Set(action, down, value, device);
            if (impulse) _impulses.Add(action);
        }
        // Actions only a source knows (not bound) still work, so a touch layout can add "aim" without a key.
        foreach (var s in _sources)
            foreach (var action in s.Actions)
                if (!_bindings.ContainsKey(action))
                {
                    bool down = _forced.Contains(action);
                    float value = 0;
                    foreach (var x in _sources)
                    {
                        down |= x.IsDown(action);
                        value = Math.Max(value, x.Value(action));
                    }
                    Set(action, down, value, down ? s.Device : InputDevice.None);
                }
        foreach (var action in _forced)
            if (!_bindings.ContainsKey(action)) Set(action, true, 1, InputDevice.Other);
    }

    private void FindUses()
    {
        _usesChanged = false;
        _usesWheel = _usesPointer = false;
        foreach (var b in _bindings.Values)
            foreach (var c in b.Controls)
            {
                if (c.Kind == InputControlKind.Wheel) _usesWheel = true;
                if (c.Device == InputDevice.Mouse) _usesPointer = true;
            }
        if (!_usesWheel)
        {
            Array.Clear(_wheelTurned);
            Array.Clear(_wheelNotch);
        }
    }

    /// <summary>
    /// The wheel as presses: a notch presses once, and a smooth wheel or a touchpad, which turns by fractions, presses
    /// once each time those add up to a notch. Turning the other way starts again.
    /// </summary>
    private void UpdateWheel(Vector2 wheel)
    {
        Turn(StickDirection.Up, StickDirection.Down, wheel.Y);
        Turn(StickDirection.Right, StickDirection.Left, wheel.X);

        void Turn(StickDirection positive, StickDirection negative, float amount)
        {
            _wheelNotch[(int)positive] = _wheelNotch[(int)negative] = false;
            if (amount == 0) return;
            var (way, other) = amount > 0 ? (positive, negative) : (negative, positive);
            _wheelTurned[(int)other] = 0;
            float turned = _wheelTurned[(int)way] + MathF.Abs(amount);
            // A whole notch is 1; the small margin keeps ten tenths a notch.
            if (turned >= 1 - 1e-4f)
            {
                _wheelNotch[(int)way] = true;
                turned = Math.Max(0, turned - MathF.Floor(turned + 1e-4f));
            }
            _wheelTurned[(int)way] = turned;
        }
    }

    /// <summary>
    /// Chord precedence: a chord key that went down since the previous sample while a modifier of one of its chords is
    /// held becomes the chord's until it is released; a chord key that went down alone stays a plain key.
    /// </summary>
    private void UpdateChords(IInputSource source)
    {
        if (_chordKeysDown.Count == 0 && _chorded.Count == 0 && !HasChords()) return;
        // Plain loops and a reused list: this runs at every update and must not allocate.
        if (_chorded.Count > 0)
        {
            _released.Clear();
            foreach (var k in _chorded) if (!source.IsKeyDown(k)) _released.Add(k);
            foreach (var k in _released) _chorded.Remove(k);
        }
        foreach (var b in _bindings.Values)
            foreach (var c in b.Controls)
            {
                if (c.Kind != InputControlKind.Chord) continue;
                bool down = source.IsKeyDown(c.Key);
                if (down && !_chordKeysDown.Contains(c.Key) && AnyDown(source, c.Chord!.Modifiers)) _chorded.Add(c.Key);
            }
        _chordKeysDown.Clear();
        foreach (var b in _bindings.Values)
            foreach (var c in b.Controls)
                if (c.Kind == InputControlKind.Chord && source.IsKeyDown(c.Key)) _chordKeysDown.Add(c.Key);
    }

    private bool HasChords()
    {
        foreach (var b in _bindings.Values)
            foreach (var c in b.Controls)
                if (c.Kind == InputControlKind.Chord) return true;
        return false;
    }

    private static bool AnyDown(IInputSource source, IReadOnlyList<Key> keys)
    {
        foreach (var key in keys) if (source.IsKeyDown(key)) return true;
        return false;
    }

    private void Set(string action, bool down, float value, InputDevice device)
    {
        _values[action] = down && value <= 0 ? 1 : value;
        if (down)
        {
            if (_down.Add(action))
            {
                _latched.Add(action);
                if (device != InputDevice.None) LastDevice = device;
            }
        }
        else _down.Remove(action);
    }

    /// <summary>Forces an action down regardless of devices (scripted playback, tests).</summary>
    public void SetForced(string action, bool down)
    {
        if (down) _forced.Add(action); else _forced.Remove(action);
    }

    /// <summary>Advances edge detection. Called by the runner after each fixed update.</summary>
    public void EndUpdate()
    {
        _previous.Clear();
        foreach (var a in _down) _previous.Add(a);
        // A tap latched this update counted as down: the next update reports its release.
        foreach (var a in _latched) _previous.Add(a);
        _latched.Clear();
        _previousButtons = _pointer.Buttons;
        // A consumed action stays taken until it was let go and its release was hidden for one update.
        if (_consumed.Count > 0) _consumed.RemoveWhere(a => !_down.Contains(a) && !_previous.Contains(a));
    }

    /// <summary>Whether the action is held in this update. Also true for a press that started and ended within the last frame, so a quick tap is never missed. Unknown actions are never down.</summary>
    public bool Down(string action) => (_down.Contains(action) || _latched.Contains(action)) && !_consumed.Contains(action);
    /// <summary>True during the first update after the action went down, even if it was released within the same frame.</summary>
    /// <remarks>A wheel notch is a press of its own: an action bound to the wheel is pressed again in every update a notch turns in, even two in a row.</remarks>
    public bool Pressed(string action) => (_latched.Contains(action) || _impulses.Contains(action) || (_down.Contains(action) && !_previous.Contains(action))) && !_consumed.Contains(action);
    /// <summary>True during the first update after the action was let go. A tap that started and ended within one frame reports <see cref="Pressed"/> in its update and <see cref="Released"/> in the next one.</summary>
    public bool Released(string action) => !_down.Contains(action) && _previous.Contains(action) && !_consumed.Contains(action);

    /// <summary>
    /// Takes the action for whoever read it first in this update: from now until it is let go, it reads as up for
    /// everyone (not <see cref="Down"/>, <see cref="Pressed"/> or <see cref="Released"/>, value 0). A menu over a scene
    /// that both read "confirm" consumes it, so the scene below does not act on the same press. Does nothing when the
    /// action is not down.
    /// </summary>
    /// <returns>Whether the action was down and is now consumed.</returns>
    public bool Consume(string action)
    {
        if (!(_down.Contains(action) || _latched.Contains(action)) || _consumed.Contains(action)) return false;
        _consumed.Add(action);
        return true;
    }

    /// <summary>Whether <see cref="Consume"/> took the action and it has not been let go yet.</summary>
    public bool IsConsumed(string action) => _consumed.Contains(action);

    /// <summary>
    /// The action's strength: 1 for a key or button, the deflection beyond the dead zone (0..1) for a stick, trigger
    /// or virtual joystick. A stick that is deflected but below <see cref="StickThreshold"/> has a value without being down.
    /// The wheel and pointer motion are amounts, not positions, and may exceed 1: the notches turned in this update, the
    /// pixels moved in it times <see cref="PointerSensitivity"/>.
    /// </summary>
    public float Value(string action)
    {
        if (_consumed.Contains(action)) return 0;
        float v = _values.GetValueOrDefault(action);
        return Down(action) && v <= 0 ? 1 : v;
    }

    /// <summary>-1, 0 or 1 from two opposing actions.</summary>
    public int Axis(string negative, string positive) => (Down(positive) ? 1 : 0) - (Down(negative) ? 1 : 0);

    /// <summary>-1..1 from two opposing actions, analog when a stick or virtual joystick drives them.</summary>
    public float AnalogAxis(string negative, string positive) => Value(positive) - Value(negative);

    /// <summary>
    /// A 2D direction from four actions, at most 1 long: keys give unit vectors (diagonals normalised), a stick or a
    /// virtual joystick gives its deflection. <c>Input.Vector("left", "right", "up", "down")</c>.
    /// </summary>
    public Vector2 Vector(string left, string right, string up, string down)
    {
        var v = new Vector2(AnalogAxis(left, right), AnalogAxis(up, down));
        float length = v.Length();
        return length > 1 ? v / length : v;
    }

    /// <summary>
    /// A 2D amount from four actions, with no limit on its length: what pointer motion or the wheel moved in this
    /// update (<c>camera.Yaw += Input.Motion("look-left", "look-right", "look-up", "look-down").X</c>). A stick bound
    /// to the same actions adds its deflection, so one line reads the mouse and the gamepad.
    /// </summary>
    public Vector2 Motion(string left, string right, string up, string down) => new(AnalogAxis(left, right), AnalogAxis(up, down));

    /// <summary>
    /// Adds the sampled state to the per-step state hash: the actions down, down in the previous update, latched and
    /// forced (each in ordinal order), every action's value, the last device, the gamepad's connection and style, and the
    /// pointer. Bindings are configuration, not state, and are left out.
    /// </summary>
    public void HashState(StateHasher hash)
    {
        static void Names(StateHasher hash, IEnumerable<string> names)
        {
            var sorted = names.Order(StringComparer.Ordinal).ToList();
            hash.Add(sorted.Count);
            foreach (var name in sorted) hash.Add(name);
        }
        Names(hash, _down);
        Names(hash, _previous);
        Names(hash, _latched);
        Names(hash, _forced);
        var values = _values.Keys.Order(StringComparer.Ordinal).ToList();
        hash.Add(values.Count);
        foreach (var action in values)
        {
            hash.Add(action);
            hash.Add(_values[action]);
        }
        hash.Add((int)LastDevice);
        hash.Add(PadConnected);
        hash.Add((int)PadStyle);
        hash.Add(_pointer.Present);
        hash.Add(_pointer.Position);
        hash.Add(_pointer.Primary);
        hash.Add(_pointer.Secondary);
        hash.Add(_pointer.Middle);
        // State that only exists once a game uses it: absent, it adds nothing, so older traces keep their hashes.
        if (_chorded.Count > 0)
        {
            hash.Add(_chorded.Count);
            foreach (var key in _chorded.Order()) hash.Add((int)key);
        }
        if (_pointer.Wheel != Vector2.Zero || _pointers.Count > 0 || _consumed.Count > 0)
        {
            hash.Add(_pointer.Wheel);
            hash.Add(_pointers.Count);
            foreach (var p in _pointers)
            {
                hash.Add(p.Device.Value);
                hash.Add(p.Id);
                hash.Add((int)p.Kind);
                hash.Add(p.Position);
                hash.Add(p.Down);
            }
            Names(hash, _consumed);
        }
        // The pointer's later state: only for a map that binds a mouse control, or while a side button is held, so
        // every other game's hashes are what they were.
        if (_usesPointer || _pointer.Back || _pointer.Forward)
        {
            hash.Add(_pointer.Back);
            hash.Add(_pointer.Forward);
            hash.Add(_pointer.Delta);
            hash.Add((int)_pointer.Kind);
            hash.Add((int)_previousButtons);
            foreach (float turned in _wheelTurned) hash.Add(turned);
            Names(hash, _impulses);
        }
    }

    /// <summary>Clears every state, e.g. after a scene change, so held keys must be pressed again.</summary>
    public void Reset()
    {
        _previous.Clear();
        foreach (var a in _down) _previous.Add(a);
        _latched.Clear();
    }

    /// <summary>Radial dead zone: inside it the stick is centred, beyond it the deflection is rescaled to 0..1.</summary>
    public static Vector2 ApplyDeadZone(Vector2 stick, float deadZone)
    {
        float length = stick.Length();
        if (length <= deadZone || length < 1e-6f) return Vector2.Zero;
        float scaled = Math.Min(1, (length - deadZone) / Math.Max(1e-6f, 1 - deadZone));
        return stick / length * scaled;
    }

    private static float Rescale(float v, float deadZone) => v <= deadZone ? 0 : Math.Min(1, (v - deadZone) / Math.Max(1e-6f, 1 - deadZone));

    /// <summary>The directions a 2D input presses: none inside <paramref name="threshold"/>, else by 45° or 90° sectors.</summary>
    public static IReadOnlySet<StickDirection> Directions(Vector2 v, float threshold, StickDirections mode)
    {
        // A centred stick, the usual case at every update, needs no new set.
        if (v.Length() < threshold || v.Length() < 1e-6f) return System.Collections.Frozen.FrozenSet<StickDirection>.Empty;
        var result = new HashSet<StickDirection>();
        // Angle in degrees, 0 = right, 90 = down (screen coordinates).
        float angle = MathF.Atan2(v.Y, v.X) * 180 / MathF.PI;
        if (angle < 0) angle += 360;
        if (mode == StickDirections.Four)
        {
            result.Add(angle < 45 || angle >= 315 ? StickDirection.Right : angle < 135 ? StickDirection.Down : angle < 225 ? StickDirection.Left : StickDirection.Up);
            return result;
        }
        int sector = (int)MathF.Floor((angle + 22.5f) / 45) % 8; // 0 right, 1 down-right, 2 down, ...
        if (sector is 7 or 0 or 1) result.Add(StickDirection.Right);
        if (sector is 1 or 2 or 3) result.Add(StickDirection.Down);
        if (sector is 3 or 4 or 5) result.Add(StickDirection.Left);
        if (sector is 5 or 6 or 7) result.Add(StickDirection.Up);
        return result;
    }

    /// <summary>
    /// How far <paramref name="v"/> goes towards <paramref name="d"/> (0 when it goes the other way). Up is negative Y
    /// for sticks and the pointer (screen coordinates) and positive Y for the wheel (away from the player).
    /// </summary>
    private static float Component(Vector2 v, StickDirection d, bool wheel = false) => d switch
    {
        StickDirection.Left => Math.Max(0, -v.X),
        StickDirection.Right => Math.Max(0, v.X),
        StickDirection.Up => Math.Max(0, wheel ? v.Y : -v.Y),
        StickDirection.Down => Math.Max(0, wheel ? -v.Y : v.Y),
        _ => 0,
    };

    /// <summary>
    /// A conventional binding set shared by the sample games: arrows, WASD, the d-pad and the left stick move; confirm,
    /// cancel, action and pause on keys and face buttons. <paramref name="names"/> renames the engine's actions (null
    /// keeps <see cref="ActionNames.Default"/>), and the map keeps them in <see cref="Names"/>.
    /// </summary>
    public static InputMap CreateDefault(ActionNames? names = null)
    {
        var n = names ?? ActionNames.Default;
        return new InputMap { Names = n }
            .Bind(n.Left, [Key.Left, Key.A], [PadButton.DPadLeft], StickDirection.Left)
            .Bind(n.Right, [Key.Right, Key.D], [PadButton.DPadRight], StickDirection.Right)
            .Bind(n.Up, [Key.Up, Key.W], [PadButton.DPadUp], StickDirection.Up)
            .Bind(n.Down, [Key.Down, Key.S], [PadButton.DPadDown], StickDirection.Down)
            .Bind(n.Confirm, [Key.Z, Key.Space, Key.Enter, Key.J], [PadButton.A])
            .Bind(n.Cancel, [Key.X, Key.Backspace, Key.K], [PadButton.B])
            .Bind(n.Action, [Key.C, Key.L], [PadButton.X])
            .Bind(n.Pause, [Key.Escape, Key.P], [PadButton.Start])
            .BindFullscreenDefaults();
    }

    /// <summary>
    /// Binds <see cref="ActionNames.Fullscreen"/> (in <see cref="Names"/>) to F11 and Alt+Enter, the usual desktop
    /// toggle; no gamepad button has that meaning by convention, so none is bound. The engine calls this for a map that
    /// has no such action, so a game that never mentions it keeps the toggle.
    /// </summary>
    public InputMap BindFullscreenDefaults() => Bind(Names.Fullscreen, Key.F11).Bind(Names.Fullscreen, new KeyChord(Key.Enter, Key.LeftAlt, Key.RightAlt));

    /// <summary>
    /// A new map with the same bindings, names and stick, axis and pointer settings, and no sampled state or action sources:
    /// the starting point of another player's map.
    /// </summary>
    public InputMap CopyBindings()
    {
        var copy = new InputMap
        {
            Names = Names,
            StickDeadZone = StickDeadZone,
            RightStickDeadZone = RightStickDeadZone,
            PointerSensitivity = PointerSensitivity,
            StickThreshold = StickThreshold,
            StickMode = StickMode,
            AxisDeadZone = AxisDeadZone,
        };
        foreach (var (action, b) in _bindings)
        {
            copy.Get(action).Controls.AddRange(b.Controls);
        }
        return copy;
    }
}

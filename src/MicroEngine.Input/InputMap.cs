using System.Numerics;
using MicroEngine.Kernel;

namespace MicroEngine.Input;

/// <summary>What an action is bound to, for display or a rebinding screen (<see cref="InputMap.GetBindings"/>).</summary>
public sealed record ActionBindings(IReadOnlyList<Key> Keys, IReadOnlyList<PadButton> Buttons, InputMap.StickDirection Stick, IReadOnlyList<(PadAxis Axis, bool Positive)> Axes);

/// <summary>
/// Named actions bound to keys, gamepad buttons, stick directions and analog axes, plus any number of action sources
/// (touch controls, custom sources). Games query actions ("jump", "confirm"), never physical keys, so bindings and
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
        public List<Key> Keys { get; } = [];
        public List<PadButton> Buttons { get; } = [];
        public StickDirection Stick { get; set; }
        public List<(PadAxis Axis, bool Positive)> Axes { get; } = [];
    }

    private readonly Dictionary<string, Binding> _bindings = new(StringComparer.Ordinal);
    private readonly HashSet<string> _down = new(StringComparer.Ordinal);
    private readonly HashSet<string> _previous = new(StringComparer.Ordinal);
    private readonly HashSet<string> _latched = new(StringComparer.Ordinal);
    private readonly HashSet<string> _forced = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _values = new(StringComparer.Ordinal);
    private readonly HashSet<string> _consumed = new(StringComparer.Ordinal);
    private readonly List<IActionSource> _sources = [];
    private readonly List<PointerInfo> _pointers = [];
    private PointerState _pointer;

    /// <summary>
    /// The direction of the left stick an action is bound to (<c>None</c>, <c>Left</c>, <c>Right</c>, <c>Up</c>,
    /// <c>Down</c>). The action is down when the stick points that way beyond <see cref="StickThreshold"/>, and its
    /// <see cref="Value"/> is the deflection along that direction.
    /// </summary>
    public enum StickDirection { None, Left, Right, Up, Down }

    /// <summary>Radial dead zone of the left stick, as a fraction of full deflection (default 0.2). Values beyond it are rescaled to 0..1.</summary>
    public float StickDeadZone { get; set; } = 0.2f;
    /// <summary>Deflection (after the dead zone) at which a stick direction counts as down (default 0.35).</summary>
    public float StickThreshold { get; set; } = 0.35f;
    /// <summary>How the left stick presses direction actions: eight sectors (default) or four.</summary>
    public StickDirections StickMode { get; set; } = StickDirections.Eight;
    /// <summary>Dead zone of bound triggers and other single axes (default 0.1).</summary>
    public float AxisDeadZone { get; set; } = 0.1f;
    /// <summary>The device that most recently pressed an action. Touch controls use it to hide while a keyboard or gamepad plays.</summary>
    public InputDevice LastDevice { get; private set; }
    /// <summary>Whether a gamepad was connected at the last sample.</summary>
    public bool PadConnected { get; private set; }
    /// <summary>The family of the gamepad at the last sample (Xbox when unknown), for prompts.</summary>
    public GamepadStyle PadStyle { get; private set; }
    /// <summary>
    /// The pointer at the last sample: the mouse over the virtual canvas, or a finger on the screen that no on-screen
    /// control took (whichever moved last), with the wheel's turn since the previous update.
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

    /// <summary>Binds keys, gamepad buttons and optionally a left-stick direction to <paramref name="action"/>, adding to its existing bindings (use <see cref="Unbind"/> first to replace them).</summary>
    /// <param name="action">The action name (case-sensitive, not empty).</param>
    /// <param name="keys">Keys that press the action.</param>
    /// <param name="buttons">Gamepad buttons that press the action.</param>
    /// <param name="stick">A left-stick direction that presses the action; <c>None</c> keeps the current one.</param>
    /// <returns>This map, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty.</exception>
    public InputMap Bind(string action, Key[] keys, PadButton[] buttons, StickDirection stick = StickDirection.None)
    {
        var b = Get(action);
        b.Keys.AddRange(keys);
        b.Buttons.AddRange(buttons);
        if (stick != StickDirection.None) b.Stick = stick;
        return this;
    }

    /// <summary>
    /// Binds one side of an analog axis: <c>BindAxis("aim-right", PadAxis.RightX, positive: true)</c>, or a trigger
    /// (<c>BindAxis("fire", PadAxis.RightTrigger)</c>). The action is down beyond half travel and its
    /// <see cref="Value"/> follows the axis.
    /// </summary>
    public InputMap BindAxis(string action, PadAxis axis, bool positive = true)
    {
        Get(action).Axes.Add((axis, positive));
        return this;
    }

    /// <summary>Removes every key, button, stick and axis bound to the action (the action stays, unbound), to rebind it.</summary>
    public InputMap Unbind(string action)
    {
        if (_bindings.ContainsKey(action)) _bindings[action] = new Binding();
        return this;
    }

    /// <summary>What the action is bound to; null for an unknown action.</summary>
    public ActionBindings? GetBindings(string action) => _bindings.TryGetValue(action, out var b)
        ? new ActionBindings([.. b.Keys], [.. b.Buttons], b.Stick, [.. b.Axes])
        : null;

    private Binding Get(string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        if (!_bindings.TryGetValue(action, out var b)) _bindings[action] = b = new Binding();
        return b;
    }

    /// <summary>The names of every bound action (including actions unbound with <see cref="Unbind"/>).</summary>
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
        _pointer = source.Pointer;
        _pointers.Clear();
        _pointers.AddRange(source.Pointers);
        var stick = ApplyDeadZone(source.LeftStick, StickDeadZone);
        var stickDown = Directions(stick, StickThreshold, StickMode);
        foreach (var (action, b) in _bindings)
        {
            var device = InputDevice.None;
            float value = 0;
            bool down = false;
            if (_forced.Contains(action)) { down = true; value = 1; device = InputDevice.Other; }
            // Plain loops: this runs for every action at every update, and LINQ would allocate each time.
            if (!down)
                foreach (var key in b.Keys)
                    if (source.IsKeyDown(key)) { down = true; value = 1; device = InputDevice.Keyboard; break; }
            if (!down)
                foreach (var button in b.Buttons)
                    if (source.IsPadDown(button)) { down = true; value = 1; device = InputDevice.Gamepad; break; }
            if (b.Stick != StickDirection.None)
            {
                float v = Component(stick, b.Stick);
                if (v > value) value = v;
                if (!down && stickDown.Contains(b.Stick)) { down = true; device = InputDevice.Gamepad; }
            }
            foreach (var (axis, positive) in b.Axes)
            {
                float raw = source.GetPadAxis(axis);
                float v = Rescale(positive ? raw : -raw, AxisDeadZone);
                if (v > value) value = v;
                if (!down && v >= 0.5f) { down = true; device = InputDevice.Gamepad; }
            }
            foreach (var s in _sources)
            {
                float v = s.Value(action);
                if (v > value) value = v;
                if (!down && s.IsDown(action)) { down = true; value = Math.Max(value, v); device = s.Device; }
            }
            Set(action, down, value, device);
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
        // A consumed action stays taken until it was let go and its release was hidden for one update.
        if (_consumed.Count > 0) _consumed.RemoveWhere(a => !_down.Contains(a) && !_previous.Contains(a));
    }

    /// <summary>Whether the action is held in this update. Also true for a press that started and ended within the last frame, so a quick tap is never missed. Unknown actions are never down.</summary>
    public bool Down(string action) => (_down.Contains(action) || _latched.Contains(action)) && !_consumed.Contains(action);
    /// <summary>True during the first update after the action went down, even if it was released within the same frame.</summary>
    public bool Pressed(string action) => (_latched.Contains(action) || (_down.Contains(action) && !_previous.Contains(action))) && !_consumed.Contains(action);
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
    /// The action's strength in 0..1: 1 for a key or button, the deflection beyond the dead zone for a stick, trigger
    /// or virtual joystick. A stick that is deflected but below <see cref="StickThreshold"/> has a value without being down.
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

    private static float Component(Vector2 v, StickDirection d) => d switch
    {
        StickDirection.Left => Math.Max(0, -v.X),
        StickDirection.Right => Math.Max(0, v.X),
        StickDirection.Up => Math.Max(0, -v.Y),
        StickDirection.Down => Math.Max(0, v.Y),
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
            .Bind(n.Pause, [Key.Escape, Key.P], [PadButton.Start]);
    }

    /// <summary>
    /// A new map with the same bindings, names and stick and axis settings, and no sampled state or action sources:
    /// the starting point of another player's map.
    /// </summary>
    public InputMap CopyBindings()
    {
        var copy = new InputMap
        {
            Names = Names,
            StickDeadZone = StickDeadZone,
            StickThreshold = StickThreshold,
            StickMode = StickMode,
            AxisDeadZone = AxisDeadZone,
        };
        foreach (var (action, b) in _bindings)
        {
            var c = copy.Get(action);
            c.Keys.AddRange(b.Keys);
            c.Buttons.AddRange(b.Buttons);
            c.Stick = b.Stick;
            c.Axes.AddRange(b.Axes);
        }
        return copy;
    }
}

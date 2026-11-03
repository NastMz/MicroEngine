using System.Numerics;
using MicroEngine.Kernel;

namespace MicroEngine.Input;

/// <summary>Physical keys the engine understands. Backends map their own codes onto these.</summary>
public enum Key
{
    None, Space, Enter, Escape, Backspace, Tab, LeftShift, RightShift, LeftControl,
    Left, Right, Up, Down,
    A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
    F1, F2, F3, F4, F5, F11, F12,
}

/// <summary>Gamepad buttons using Xbox layout names; backends map their own codes.</summary>
public enum PadButton { None, A, B, X, Y, Start, Back, LeftShoulder, RightShoulder, DPadUp, DPadDown, DPadLeft, DPadRight, LeftStick, RightStick }

/// <summary>Analog gamepad axes. Sticks are -1..1 (right and down positive), triggers 0..1.</summary>
public enum PadAxis { LeftX, LeftY, RightX, RightY, LeftTrigger, RightTrigger }

/// <summary>The kind of device that last pressed an action (see <see cref="InputMap.LastDevice"/>).</summary>
/// <remarks><c>Other</c> covers scripted input and custom <see cref="IActionSource"/>s.</remarks>
public enum InputDevice { None, Keyboard, Gamepad, Touch, Other }

/// <summary>
/// The button names of a gamepad family, for prompts (<see cref="ActionPrompts"/>): <see cref="PadButton"/> uses Xbox
/// names, which PlayStation pads show as shapes and Nintendo pads with A/B and X/Y swapped.
/// </summary>
public enum GamepadStyle
{
    /// <summary>Xbox names (A, B, X, Y), also for pads whose family is unknown.</summary>
    Xbox,
    /// <summary>PlayStation shapes (cross, circle, square, triangle).</summary>
    PlayStation,
    /// <summary>Nintendo names, where the button in <see cref="PadButton.A"/>'s place is labelled B.</summary>
    Nintendo,
}

/// <summary>How a 2D stick turns into the four direction actions.</summary>
public enum StickDirections
{
    /// <summary>Eight 45° sectors: diagonals press two directions.</summary>
    Eight,
    /// <summary>Four 90° sectors: only the dominant direction is pressed (grid movement, menus).</summary>
    Four,
}

/// <summary>The mouse (or another pointer) over the game's virtual canvas.</summary>
/// <param name="Present">Whether the pointer is over the window at all.</param>
/// <param name="Position">Position in virtual canvas pixels (may lie outside the canvas, in the letterbox).</param>
public readonly record struct PointerState(bool Present, Vector2 Position, bool Primary, bool Secondary, bool Middle);

/// <summary>The raw device state a backend reports once per polled frame.</summary>
public interface IInputSource
{
    /// <summary>Whether <paramref name="key"/> is held.</summary>
    bool IsKeyDown(Key key);
    /// <summary>Whether <paramref name="button"/> is held on the gamepad.</summary>
    bool IsPadDown(PadButton button);
    /// <summary>Left stick, each axis in -1..1.</summary>
    Vector2 LeftStick { get; }
    /// <summary>Any gamepad axis. Sources that only know the left stick keep the default.</summary>
    float GetPadAxis(PadAxis axis) => axis switch
    {
        PadAxis.LeftX => LeftStick.X,
        PadAxis.LeftY => LeftStick.Y,
        _ => 0,
    };
    /// <summary>Whether a gamepad is connected. Sources without gamepads keep the default.</summary>
    bool PadConnected => false;
    /// <summary>The family of the connected gamepad, for button names in prompts; sources that cannot tell keep Xbox.</summary>
    GamepadStyle PadStyle => GamepadStyle.Xbox;
    /// <summary>The mouse over the virtual canvas; sources without one keep the default (not present).</summary>
    PointerState Pointer => default;
}

/// <summary>
/// A source of actions rather than of keys: on-screen touch controls (<see cref="VirtualControls"/>), a network peer,
/// an accessibility switch or an AI. Added with <see cref="InputMap.AddSource"/>, it is read every step together with
/// the keyboard and gamepad, so a game cannot tell which produced an action.
/// </summary>
public interface IActionSource
{
    /// <summary>Whether the action is down (digital).</summary>
    bool IsDown(string action);
    /// <summary>The action's strength, 0..1 (analog). Digital sources keep the default.</summary>
    float Value(string action) => IsDown(action) ? 1 : 0;
    /// <summary>The kind of device this source stands for, reported by <see cref="InputMap.LastDevice"/>.</summary>
    InputDevice Device => InputDevice.Other;
    /// <summary>Actions this source may press that need no key binding (read even if the map does not bind them).</summary>
    IEnumerable<string> Actions => [];
}

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
public sealed class InputMap : IStateHashed
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
    private readonly List<IActionSource> _sources = [];
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
    /// <summary>The mouse over the virtual canvas at the last sample.</summary>
    public PointerState Pointer => _pointer;
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
    }

    /// <summary>Whether the action is held in this update. Also true for a press that started and ended within the last frame, so a quick tap is never missed. Unknown actions are never down.</summary>
    public bool Down(string action) => _down.Contains(action) || _latched.Contains(action);
    /// <summary>True during the first update after the action went down, even if it was released within the same frame.</summary>
    public bool Pressed(string action) => _latched.Contains(action) || (_down.Contains(action) && !_previous.Contains(action));
    /// <summary>True during the first update after the action was let go. A tap that started and ended within one frame reports <see cref="Pressed"/> in its update and <see cref="Released"/> in the next one.</summary>
    public bool Released(string action) => !_down.Contains(action) && _previous.Contains(action);

    /// <summary>
    /// The action's strength in 0..1: 1 for a key or button, the deflection beyond the dead zone for a stick, trigger
    /// or virtual joystick. A stick that is deflected but below <see cref="StickThreshold"/> has a value without being down.
    /// </summary>
    public float Value(string action)
    {
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

    /// <summary>A conventional binding set shared by the sample games.</summary>
    public static InputMap CreateDefault() => new InputMap()
        .Bind("left", [Key.Left, Key.A], [PadButton.DPadLeft], StickDirection.Left)
        .Bind("right", [Key.Right, Key.D], [PadButton.DPadRight], StickDirection.Right)
        .Bind("up", [Key.Up, Key.W], [PadButton.DPadUp], StickDirection.Up)
        .Bind("down", [Key.Down, Key.S], [PadButton.DPadDown], StickDirection.Down)
        .Bind("confirm", [Key.Z, Key.Space, Key.Enter, Key.J], [PadButton.A])
        .Bind("cancel", [Key.X, Key.Backspace, Key.K], [PadButton.B])
        .Bind("action", [Key.C, Key.L], [PadButton.X])
        .Bind("pause", [Key.Escape, Key.P], [PadButton.Start]);
}

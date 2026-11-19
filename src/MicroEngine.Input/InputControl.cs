namespace MicroEngine.Input;

/// <summary>
/// What kind of physical control an <see cref="InputControl"/> is. The order is the order a map reads an action's
/// controls in: the digital ones first (a key before a gamepad button before a mouse button), then the analog ones.
/// </summary>
public enum InputControlKind
{
    /// <summary>No control.</summary>
    None,
    /// <summary>A keyboard key (<see cref="InputControl.Key"/>).</summary>
    Key,
    /// <summary>A key pressed while a modifier is held (<see cref="InputControl.Chord"/>).</summary>
    Chord,
    /// <summary>A gamepad button (<see cref="InputControl.Button"/>).</summary>
    PadButton,
    /// <summary>A mouse button (<see cref="InputControl.PointerButton"/>); the primary one is also a finger or pen on the screen.</summary>
    PointerButton,
    /// <summary>The wheel turning one way (<see cref="InputControl.Direction"/>): pressed for one update per notch.</summary>
    Wheel,
    /// <summary>A stick pointing one way (<see cref="InputControl.Stick"/>, <see cref="InputControl.Direction"/>).</summary>
    Stick,
    /// <summary>One side of a gamepad axis (<see cref="InputControl.Axis"/>, <see cref="InputControl.Positive"/>).</summary>
    PadAxis,
    /// <summary>The pointer moving one way (<see cref="InputControl.Direction"/>): a value, never a press.</summary>
    PointerMotion,
}

/// <summary>The two sticks of a gamepad.</summary>
public enum PadStick
{
    /// <summary>The left stick.</summary>
    Left,
    /// <summary>The right stick.</summary>
    Right,
}

/// <summary>
/// One physical control an action can be bound to: a key, a key combination, a gamepad button, one side of a gamepad
/// axis, a stick direction, a mouse button, the wheel turning one way or the pointer moving one way. Bindings, rebinding
/// by listening, saved bindings and prompts all work on this one value, so a mouse button is rebound and saved exactly
/// like a key.
/// </summary>
/// <remarks>
/// A key, a chord, a gamepad button and a mouse button convert to a control by themselves, so
/// <c>map.Bind("select", PointerButton.Primary, Key.Enter)</c> reads as it should. The other kinds have a name:
/// <see cref="WheelUp"/>, <see cref="PointerRight"/>, <see cref="Of(PadStick, InputMap.StickDirection)"/>,
/// <see cref="Of(PadAxis, bool)"/>. As text (<see cref="ToString"/>, <see cref="TryParse"/>) a control is its kind and
/// its name: <c>Key:Space</c>, <c>Mouse:Secondary</c>, <c>Wheel:Up</c>, <c>Stick:Right.Up</c>.
/// </remarks>
/// <example>
/// <code>
/// protected override InputMap CreateInput() =&gt; InputMap.CreateDefault()
///     .Bind("select", PointerButton.Primary)
///     .Bind("order", PointerButton.Secondary, Key.E)
///     .Bind("zoom-in", InputControl.WheelUp, Key.Equals)
///     .Bind("zoom-out", InputControl.WheelDown, Key.Minus);
/// </code>
/// </example>
public readonly struct InputControl : IEquatable<InputControl>
{
    private InputControl(InputControlKind kind, int code, int detail = 0, KeyChord? chord = null)
    {
        Kind = kind;
        _code = code;
        _detail = detail;
        Chord = chord;
    }

    private readonly int _code, _detail;

    /// <summary>What kind of control this is; <see cref="InputControlKind.None"/> for the default value.</summary>
    public InputControlKind Kind { get; }
    /// <summary>The key of a key control, or the key a chord presses.</summary>
    public Key Key => Kind is InputControlKind.Key or InputControlKind.Chord ? (Key)_code : Key.None;
    /// <summary>The combination of a chord control, else null.</summary>
    public KeyChord? Chord { get; }
    /// <summary>The button of a gamepad-button control.</summary>
    public PadButton Button => Kind == InputControlKind.PadButton ? (PadButton)_code : PadButton.None;
    /// <summary>The button of a mouse-button control.</summary>
    public PointerButton PointerButton => Kind == InputControlKind.PointerButton ? (PointerButton)_code : PointerButton.None;
    /// <summary>The axis of a gamepad-axis control.</summary>
    public PadAxis Axis => Kind == InputControlKind.PadAxis ? (PadAxis)_code : default;
    /// <summary>For a gamepad-axis control: whether the positive side (right, down, a trigger's travel) presses.</summary>
    public bool Positive => Kind == InputControlKind.PadAxis && _detail != 0;
    /// <summary>The stick of a stick control.</summary>
    public PadStick Stick => Kind == InputControlKind.Stick ? (PadStick)_code : PadStick.Left;
    /// <summary>The direction of a stick, wheel or pointer-motion control (up is away from the player for the wheel).</summary>
    public InputMap.StickDirection Direction => Kind is InputControlKind.Stick or InputControlKind.Wheel or InputControlKind.PointerMotion
        ? (InputMap.StickDirection)_detail : InputMap.StickDirection.None;

    /// <summary>
    /// The kind of device the control is on: the keyboard, a gamepad, or the mouse (buttons, wheel and motion).
    /// </summary>
    public InputDevice Device => Kind switch
    {
        InputControlKind.Key or InputControlKind.Chord => InputDevice.Keyboard,
        InputControlKind.PadButton or InputControlKind.Stick or InputControlKind.PadAxis => InputDevice.Gamepad,
        InputControlKind.PointerButton or InputControlKind.Wheel or InputControlKind.PointerMotion => InputDevice.Mouse,
        _ => InputDevice.None,
    };

    /// <summary>The wheel turning away from the player (scrolling up).</summary>
    public static InputControl WheelUp { get; } = new(InputControlKind.Wheel, 0, (int)InputMap.StickDirection.Up);
    /// <summary>The wheel turning towards the player (scrolling down).</summary>
    public static InputControl WheelDown { get; } = new(InputControlKind.Wheel, 0, (int)InputMap.StickDirection.Down);
    /// <summary>The wheel turning left (wheels that tilt, touchpads).</summary>
    public static InputControl WheelLeft { get; } = new(InputControlKind.Wheel, 0, (int)InputMap.StickDirection.Left);
    /// <summary>The wheel turning right (wheels that tilt, touchpads).</summary>
    public static InputControl WheelRight { get; } = new(InputControlKind.Wheel, 0, (int)InputMap.StickDirection.Right);
    /// <summary>The pointer moving left.</summary>
    public static InputControl PointerLeft { get; } = new(InputControlKind.PointerMotion, 0, (int)InputMap.StickDirection.Left);
    /// <summary>The pointer moving right.</summary>
    public static InputControl PointerRight { get; } = new(InputControlKind.PointerMotion, 0, (int)InputMap.StickDirection.Right);
    /// <summary>The pointer moving up.</summary>
    public static InputControl PointerUp { get; } = new(InputControlKind.PointerMotion, 0, (int)InputMap.StickDirection.Up);
    /// <summary>The pointer moving down.</summary>
    public static InputControl PointerDown { get; } = new(InputControlKind.PointerMotion, 0, (int)InputMap.StickDirection.Down);

    /// <summary>A keyboard key.</summary>
    /// <exception cref="ArgumentException"><paramref name="key"/> is <see cref="Key.None"/>.</exception>
    public static InputControl Of(Key key) =>
        key == Key.None ? throw new ArgumentException("A control needs a key.", nameof(key)) : new(InputControlKind.Key, (int)key);

    /// <summary>A key combination.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="chord"/> is null.</exception>
    public static InputControl Of(KeyChord chord)
    {
        ArgumentNullException.ThrowIfNull(chord);
        return new(InputControlKind.Chord, (int)chord.Key, 0, chord);
    }

    /// <summary>A gamepad button.</summary>
    /// <exception cref="ArgumentException"><paramref name="button"/> is <see cref="PadButton.None"/>.</exception>
    public static InputControl Of(PadButton button) =>
        button == PadButton.None ? throw new ArgumentException("A control needs a button.", nameof(button)) : new(InputControlKind.PadButton, (int)button);

    /// <summary>A mouse button. The primary button is also a finger or a pen on the screen.</summary>
    /// <exception cref="ArgumentException"><paramref name="button"/> is <see cref="PointerButton.None"/>.</exception>
    public static InputControl Of(PointerButton button) =>
        button == PointerButton.None ? throw new ArgumentException("A control needs a button.", nameof(button)) : new(InputControlKind.PointerButton, (int)button);

    /// <summary>One side of a gamepad axis: a trigger, or half of a stick axis.</summary>
    public static InputControl Of(PadAxis axis, bool positive = true) => new(InputControlKind.PadAxis, (int)axis, positive ? 1 : 0);

    /// <summary>A stick pointing one way.</summary>
    /// <exception cref="ArgumentException"><paramref name="direction"/> is <c>None</c>.</exception>
    public static InputControl Of(PadStick stick, InputMap.StickDirection direction) => direction == InputMap.StickDirection.None
        ? throw new ArgumentException("A stick control needs a direction.", nameof(direction))
        : new(InputControlKind.Stick, (int)stick, (int)direction);

    /// <summary>
    /// The control a press is on, for a rebinding screen: the key of a key-down, the button of a gamepad button-down, the
    /// button of a mouse button-down, or the way a wheel event turned (its larger component). Null for anything else,
    /// and for a finger or pen (a touch is not a control to rebind).
    /// </summary>
    public static InputControl? FromPress(in InputEvent press) => press.Kind switch
    {
        InputEventKind.KeyDown when press.Key != Key.None => (InputControl?)Of(press.Key),
        InputEventKind.ButtonDown when press.Button != PadButton.None => Of(press.Button),
        InputEventKind.PointerDown when press.PointerKind == PointerKind.Mouse && press.PointerButton != PointerButton.None => Of(press.PointerButton),
        InputEventKind.Wheel when press.Wheel != default => MathF.Abs(press.Wheel.X) > MathF.Abs(press.Wheel.Y)
            ? (press.Wheel.X > 0 ? WheelRight : WheelLeft)
            : (press.Wheel.Y > 0 ? WheelUp : WheelDown),
        _ => null,
    };

    /// <summary>A key as a control.</summary>
    public static implicit operator InputControl(Key key) => Of(key);
    /// <summary>A key combination as a control.</summary>
    public static implicit operator InputControl(KeyChord chord) => Of(chord);
    /// <summary>A gamepad button as a control.</summary>
    public static implicit operator InputControl(PadButton button) => Of(button);
    /// <summary>A mouse button as a control.</summary>
    public static implicit operator InputControl(PointerButton button) => Of(button);

    /// <inheritdoc/>
    public bool Equals(InputControl other)
    {
        if (Kind != other.Kind || _code != other._code || _detail != other._detail) return false;
        if (Kind != InputControlKind.Chord) return true;
        return Chord!.Modifiers.SequenceEqual(other.Chord!.Modifiers);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is InputControl other && Equals(other);
    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine((int)Kind, _code, _detail);
    /// <summary>Whether two controls are the same physical control.</summary>
    public static bool operator ==(InputControl left, InputControl right) => left.Equals(right);
    /// <summary>Whether two controls differ.</summary>
    public static bool operator !=(InputControl left, InputControl right) => !left.Equals(right);

    /// <summary>
    /// The control as text, the form saved bindings use: <c>Key:Space</c>, <c>Chord:LeftAlt|RightAlt+Enter</c>,
    /// <c>Pad:A</c>, <c>Mouse:Secondary</c>, <c>Wheel:Up</c>, <c>Stick:Right.Up</c>, <c>Axis:RightTrigger+</c>,
    /// <c>Motion:Left</c>. Names are the enum names, so the text stays valid when the engine adds keys.
    /// </summary>
    public override string ToString() => Kind switch
    {
        InputControlKind.Key => "Key:" + Key,
        InputControlKind.Chord => "Chord:" + string.Join('|', Chord!.Modifiers) + "+" + Chord.Key,
        InputControlKind.PadButton => "Pad:" + Button,
        InputControlKind.PointerButton => "Mouse:" + PointerButton,
        InputControlKind.Wheel => "Wheel:" + Direction,
        InputControlKind.Stick => "Stick:" + Stick + "." + Direction,
        InputControlKind.PadAxis => "Axis:" + Axis + (Positive ? "+" : "-"),
        InputControlKind.PointerMotion => "Motion:" + Direction,
        _ => "None",
    };

    /// <summary>
    /// Reads a control written by <see cref="ToString"/>. False for text that is not a control, or that names a kind,
    /// key, button or axis this version does not know (saved bindings skip those).
    /// </summary>
    public static bool TryParse(string? text, out InputControl control)
    {
        control = default;
        if (text is null) return false;
        int colon = text.IndexOf(':');
        if (colon <= 0 || colon == text.Length - 1) return false;
        string name = text[(colon + 1)..];
        switch (text[..colon])
        {
            case "Key":
                if (!Name<Key>(name, out var key) || key == Key.None) return false;
                control = Of(key);
                return true;
            case "Chord":
            {
                int plus = name.LastIndexOf('+');
                if (plus <= 0 || !Name<Key>(name[(plus + 1)..], out var pressed) || pressed == Key.None) return false;
                var modifiers = new List<Key>();
                foreach (var m in name[..plus].Split('|'))
                    if (Name<Key>(m, out var modifier) && modifier != Key.None) modifiers.Add(modifier);
                if (modifiers.Count == 0) return false;
                control = Of(new KeyChord(pressed, [.. modifiers]));
                return true;
            }
            case "Pad":
                if (!Name<PadButton>(name, out var button) || button == PadButton.None) return false;
                control = Of(button);
                return true;
            case "Mouse":
                if (!Name<PointerButton>(name, out var pointer) || pointer == PointerButton.None) return false;
                control = Of(pointer);
                return true;
            case "Wheel":
                if (!Way(name, out var turn)) return false;
                control = new(InputControlKind.Wheel, 0, (int)turn);
                return true;
            case "Motion":
                if (!Way(name, out var move)) return false;
                control = new(InputControlKind.PointerMotion, 0, (int)move);
                return true;
            case "Stick":
            {
                int dot = name.IndexOf('.');
                if (dot <= 0 || !Name<PadStick>(name[..dot], out var stick) || !Way(name[(dot + 1)..], out var way)) return false;
                control = Of(stick, way);
                return true;
            }
            case "Axis":
                if (name.Length < 2 || name[^1] is not ('+' or '-') || !Name<PadAxis>(name[..^1], out var axis)) return false;
                control = Of(axis, name[^1] == '+');
                return true;
            default:
                return false;
        }

        // Names only: a number would parse as an enum value, and a saved file never holds one.
        static bool Name<T>(string text, out T value) where T : struct, Enum =>
            Enum.TryParse(text, out value) && Enum.IsDefined(value) && text.Length > 0 && !char.IsAsciiDigit(text[0]) && text[0] != '-';

        static bool Way(string text, out InputMap.StickDirection direction) =>
            Name(text, out direction) && direction != InputMap.StickDirection.None;
    }
}

/// <summary>
/// The names prompts show for the mouse's controls, in the same short style as key and gamepad names: <c>"LMB"</c>,
/// <c>"Wheel ↑"</c>, <c>"Mouse →"</c>.
/// </summary>
public static class PointerNames
{
    /// <summary>The name of a mouse button: "LMB", "RMB", "MMB", "Mouse 4" (back) and "Mouse 5" (forward).</summary>
    public static string Button(PointerButton button) => button switch
    {
        PointerButton.Primary => "LMB",
        PointerButton.Secondary => "RMB",
        PointerButton.Middle => "MMB",
        PointerButton.Back => "Mouse 4",
        PointerButton.Forward => "Mouse 5",
        _ => "Mouse",
    };

    /// <summary>
    /// The name of a mouse control: a button (<see cref="Button"/>), the wheel turning one way ("Wheel ↑") or the
    /// pointer moving one way ("Mouse →"). Null for a control that is not on the mouse.
    /// </summary>
    public static string? Of(InputControl control) => control.Kind switch
    {
        InputControlKind.PointerButton => Button(control.PointerButton),
        InputControlKind.Wheel => "Wheel " + Arrow(control.Direction),
        InputControlKind.PointerMotion => "Mouse " + Arrow(control.Direction),
        _ => null,
    };

    private static string Arrow(InputMap.StickDirection direction) => direction switch
    {
        InputMap.StickDirection.Left => "←",
        InputMap.StickDirection.Right => "→",
        InputMap.StickDirection.Up => "↑",
        _ => "↓",
    };
}

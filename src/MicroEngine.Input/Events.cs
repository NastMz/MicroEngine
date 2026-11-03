using System.Numerics;
using MicroEngine.Kernel;

namespace MicroEngine.Input;

/// <summary>
/// Identifies one input device for as long as it is connected: a keyboard, a mouse, a touch screen, one gamepad. Ids
/// are chosen by whoever reports the device (a backend's own ids are positive; the engine's polled sources and touch
/// screen use negative ones) and are never reused while the game runs.
/// </summary>
/// <param name="Value">The id; 0 is <see cref="None"/>.</param>
public readonly record struct DeviceId(int Value)
{
    /// <summary>No device (scripted actions, sources that are not devices).</summary>
    public static DeviceId None => default;
    /// <summary>The touch screen the platform offers (its fingers become pointers of this device).</summary>
    public static DeviceId TouchScreen => new(-1);
    /// <inheritdoc/>
    public override string ToString() => $"device {Value}";
}

/// <summary>The kind of an input device.</summary>
public enum DeviceKind { Keyboard, Mouse, Touch, Gamepad, Other }

/// <summary>A connected device as its backend describes it.</summary>
/// <param name="Id">Its id.</param>
/// <param name="Kind">Keyboard, mouse, touch screen, gamepad or other.</param>
/// <param name="Name">The name the system reports ("Xbox Series X Controller"), or a generic one.</param>
public sealed record InputDeviceInfo(DeviceId Id, DeviceKind Kind, string Name)
{
    /// <summary>For gamepads: the family whose names its buttons carry (Xbox when unknown).</summary>
    public GamepadStyle Style { get; init; }
    /// <summary>For gamepads: the player number the system or the pad shows (its LEDs), 0-based, or -1 when it has none.</summary>
    public int PlayerIndex { get; init; } = -1;
    /// <summary>For gamepads: whether <see cref="IGamepadRumble.Rumble"/> can do anything on it.</summary>
    public bool CanRumble { get; init; }
}

/// <summary>What kind of pointer moved or pressed: the mouse, a finger on a touch screen or a pen.</summary>
public enum PointerKind { Mouse, Touch, Pen }

/// <summary>Pointer buttons. A finger or pen tip is <see cref="Primary"/>.</summary>
public enum PointerButton { None, Primary, Secondary, Middle, Back, Forward }

/// <summary>What an <see cref="InputEvent"/> reports.</summary>
public enum InputEventKind
{
    /// <summary>A device was connected (<see cref="InputEvent.Info"/> describes it). Devices present at start are reported too.</summary>
    DeviceAdded,
    /// <summary>A device was disconnected; every key, button and pointer it held is released by the events before it.</summary>
    DeviceRemoved,
    /// <summary>A key went down (<see cref="InputEvent.Key"/>); <see cref="InputEvent.Repeat"/> for the system's auto-repeat.</summary>
    KeyDown,
    /// <summary>A key went up.</summary>
    KeyUp,
    /// <summary>Text was entered while text input is on (<see cref="InputEvent.Text"/>): characters, never keys.</summary>
    Text,
    /// <summary>The text being composed by an input method changed (<see cref="InputEvent.Text"/>, empty when composition ended).</summary>
    TextComposition,
    /// <summary>A pointer button went down, or a finger or pen touched (<see cref="InputEvent.Pointer"/>, <see cref="InputEvent.Button"/>).</summary>
    PointerDown,
    /// <summary>A pointer button went up, or a finger or pen left the screen.</summary>
    PointerUp,
    /// <summary>A pointer moved (<see cref="InputEvent.Position"/>).</summary>
    PointerMove,
    /// <summary>The system took a finger away (the game lost the screen); handle it as an up that must not act.</summary>
    PointerCancel,
    /// <summary>The mouse left the window.</summary>
    PointerLeave,
    /// <summary>The wheel turned (<see cref="InputEvent.Wheel"/>).</summary>
    Wheel,
    /// <summary>A gamepad button went down (<see cref="InputEvent.Button"/>).</summary>
    ButtonDown,
    /// <summary>A gamepad button went up.</summary>
    ButtonUp,
    /// <summary>A gamepad axis moved (<see cref="InputEvent.Axis"/>, <see cref="InputEvent.Value"/>).</summary>
    AxisMoved,
}

/// <summary>
/// One thing that happened on an input device, in the order it happened, with the time it happened on the host's
/// monotonic clock. Backends deliver them (<see cref="IInputBackend"/>); the engine routes them through the input
/// layers (UI, on-screen controls, then gameplay) and derives the polled state actions read from those no layer took.
/// Build one with the static methods (<see cref="KeyDown"/>, <see cref="PointerDown"/>...).
/// </summary>
public readonly record struct InputEvent
{
    /// <summary>What happened.</summary>
    public InputEventKind Kind { get; init; }
    /// <summary>The device it happened on.</summary>
    public DeviceId Device { get; init; }
    /// <summary>When, in seconds of the host's monotonic clock (0 for scripted and polled events).</summary>
    public double Time { get; init; }
    /// <summary>The key of key events.</summary>
    public Key Key { get; init; }
    /// <summary>Whether a key-down is the system's auto-repeat of a held key (it does not change what is held).</summary>
    public bool Repeat { get; init; }
    /// <summary>The text of text and composition events.</summary>
    public string? Text { get; init; }
    /// <summary>The pointer of pointer events: 0 for the mouse, the finger's id for touches.</summary>
    public long Pointer { get; init; }
    /// <summary>The kind of pointer.</summary>
    public PointerKind PointerKind { get; init; }
    /// <summary>The pointer button of pointer-down and pointer-up events.</summary>
    public PointerButton PointerButton { get; init; }
    /// <summary>The pointer's position in virtual canvas pixels (may lie outside the canvas, in the letterbox).</summary>
    public Vector2 Position { get; init; }
    /// <summary>The pointer's position in the window's pixels, for overlays drawn in screen pixels (touches; the mouse when known).</summary>
    public Vector2 ScreenPosition { get; init; }
    /// <summary>How far the wheel turned: Y positive away from the player, X positive to the right, in notches.</summary>
    public Vector2 Wheel { get; init; }
    /// <summary>The gamepad button of button events.</summary>
    public PadButton Button { get; init; }
    /// <summary>The gamepad axis of axis events.</summary>
    public PadAxis Axis { get; init; }
    /// <summary>The axis value: sticks -1..1 (right and down positive), triggers 0..1.</summary>
    public float Value { get; init; }
    /// <summary>The device of <see cref="InputEventKind.DeviceAdded"/>.</summary>
    public InputDeviceInfo? Info { get; init; }

    /// <summary>A device was connected.</summary>
    public static InputEvent Added(InputDeviceInfo info, double time = 0) =>
        new() { Kind = InputEventKind.DeviceAdded, Device = info.Id, Info = info, Time = time };
    /// <summary>A device was disconnected.</summary>
    public static InputEvent Removed(DeviceId device, double time = 0) => new() { Kind = InputEventKind.DeviceRemoved, Device = device, Time = time };
    /// <summary>A key went down.</summary>
    public static InputEvent KeyDown(DeviceId device, Key key, double time = 0, bool repeat = false) =>
        new() { Kind = InputEventKind.KeyDown, Device = device, Key = key, Time = time, Repeat = repeat };
    /// <summary>A key went up.</summary>
    public static InputEvent KeyUp(DeviceId device, Key key, double time = 0) => new() { Kind = InputEventKind.KeyUp, Device = device, Key = key, Time = time };
    /// <summary>Text was entered.</summary>
    public static InputEvent TextInput(DeviceId device, string text, double time = 0) => new() { Kind = InputEventKind.Text, Device = device, Text = text, Time = time };
    /// <summary>The text being composed changed.</summary>
    public static InputEvent Composition(DeviceId device, string text, double time = 0) =>
        new() { Kind = InputEventKind.TextComposition, Device = device, Text = text, Time = time };
    /// <summary>A pointer button went down or a finger touched.</summary>
    public static InputEvent PointerDown(DeviceId device, long pointer, PointerKind kind, Vector2 position, PointerButton button = PointerButton.Primary, double time = 0) =>
        new() { Kind = InputEventKind.PointerDown, Device = device, Pointer = pointer, PointerKind = kind, Position = position, PointerButton = button, Time = time };
    /// <summary>A pointer button went up or a finger lifted.</summary>
    public static InputEvent PointerUp(DeviceId device, long pointer, PointerKind kind, Vector2 position, PointerButton button = PointerButton.Primary, double time = 0) =>
        new() { Kind = InputEventKind.PointerUp, Device = device, Pointer = pointer, PointerKind = kind, Position = position, PointerButton = button, Time = time };
    /// <summary>A pointer moved.</summary>
    public static InputEvent PointerMove(DeviceId device, long pointer, PointerKind kind, Vector2 position, double time = 0) =>
        new() { Kind = InputEventKind.PointerMove, Device = device, Pointer = pointer, PointerKind = kind, Position = position, Time = time };
    /// <summary>The wheel turned.</summary>
    public static InputEvent WheelMoved(DeviceId device, Vector2 wheel, Vector2 position = default, double time = 0) =>
        new() { Kind = InputEventKind.Wheel, Device = device, Wheel = wheel, Position = position, Time = time };
    /// <summary>A gamepad button went down.</summary>
    public static InputEvent ButtonDown(DeviceId device, PadButton button, double time = 0) => new() { Kind = InputEventKind.ButtonDown, Device = device, Button = button, Time = time };
    /// <summary>A gamepad button went up.</summary>
    public static InputEvent ButtonUp(DeviceId device, PadButton button, double time = 0) => new() { Kind = InputEventKind.ButtonUp, Device = device, Button = button, Time = time };
    /// <summary>A gamepad axis moved.</summary>
    public static InputEvent AxisMoved(DeviceId device, PadAxis axis, float value, double time = 0) =>
        new() { Kind = InputEventKind.AxisMoved, Device = device, Axis = axis, Value = value, Time = time };

    /// <summary>Whether this is a press: a key-down that is not a repeat, a pointer-down or a gamepad button-down.</summary>
    public bool IsPress => Kind is InputEventKind.PointerDown or InputEventKind.ButtonDown || Kind == InputEventKind.KeyDown && !Repeat;
}

/// <summary>
/// The input part of a platform: the devices and what happens on them, delivered as events. A capability of hosts with
/// input (<see cref="Platform.IPlatformHost.TryGetCapability{T}"/>); the engine drains it once per frame, at the start
/// of the frame's input phase.
/// </summary>
/// <remarks>
/// The contract every input backend follows:
/// <list type="number">
/// <item>Events are in the order they happened; a host gathers them in its event pump and hands them over in
/// <see cref="Drain"/>, never between drains.</item>
/// <item>Every connected device is announced with <see cref="InputEventKind.DeviceAdded"/> before its first other
/// event, also those present at start; a disconnected one with <see cref="InputEventKind.DeviceRemoved"/> after events
/// releasing what it held.</item>
/// <item>Keys are positions (<see cref="Key"/>), never characters; characters come as <see cref="InputEventKind.Text"/>
/// only while text input is on (<see cref="ITextInputControl"/>).</item>
/// <item>Pointer positions are in virtual canvas pixels. A backend does not synthesize mouse events from touches or
/// touches from the mouse.</item>
/// <item>When the window loses focus, the backend releases every key and button it reported down.</item>
/// </list>
/// </remarks>
public interface IInputBackend
{
    /// <summary>Moves every event since the previous call into <paramref name="events"/>, oldest first.</summary>
    void Drain(List<InputEvent> events);
}

/// <summary>
/// The labels the player's keyboard layout prints on keys: "Q" for <see cref="Key.A"/> on AZERTY. A capability of input
/// backends that know the layout (<see cref="Platform.IPlatformHost.TryGetCapability{T}"/>).
/// </summary>
public interface IKeyLabels
{
    /// <summary>The key's label in the current layout, or null when the backend cannot tell (the US name is used then).</summary>
    string? KeyLabel(Key key);
}

/// <summary>Gamepad vibration, a capability of input backends whose pads can rumble.</summary>
public interface IGamepadRumble
{
    /// <summary>
    /// Rumbles <paramref name="pad"/>'s low-frequency (left, heavy) and high-frequency (right, light) motors at 0..1 for
    /// <paramref name="seconds"/>; 0, 0 stops it. Returns false when the pad cannot rumble.
    /// </summary>
    bool Rumble(DeviceId pad, float low, float high, double seconds);
}

/// <summary>
/// Turns the system's text input on and off: the on-screen keyboard on phones, input methods (IME) on desktops. A
/// capability of input backends that deliver <see cref="InputEventKind.Text"/>.
/// </summary>
public interface ITextInputControl
{
    /// <summary>
    /// Turns text input on; <paramref name="caret"/> is where the text being edited is, in virtual canvas pixels, so an
    /// input method can place its candidate list next to it.
    /// </summary>
    void Start(RectF caret);
    /// <summary>Turns text input off.</summary>
    void Stop();
}

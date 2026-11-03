using System.Numerics;

namespace MicroEngine.Input;

/// <summary>
/// The physical position of each <see cref="Key"/> as a USB HID keyboard usage (page 7), the code SDL calls a scancode:
/// backends that report HID usages map them with one table.
/// </summary>
public static class KeyUsages
{
    private static readonly Dictionary<Key, int> ToUsage = Build();
    private static readonly Dictionary<int, Key> FromUsage = ToUsage.ToDictionary(p => p.Value, p => p.Key);

    /// <summary>The HID usage of <paramref name="key"/>, or 0 for <see cref="Key.None"/>.</summary>
    public static int Usage(Key key) => ToUsage.GetValueOrDefault(key);

    /// <summary>The key at HID usage <paramref name="usage"/>, or <see cref="Key.None"/> for a usage the engine does not name.</summary>
    public static Key FromHidUsage(int usage) => FromUsage.GetValueOrDefault(usage);

    /// <summary>Every key the engine names, in enum order (no <see cref="Key.None"/>).</summary>
    public static IReadOnlyList<Key> All { get; } = [.. Enum.GetValues<Key>().Where(k => k != Key.None)];

    private static Dictionary<Key, int> Build()
    {
        var map = new Dictionary<Key, int>
        {
            [Key.Enter] = 40, [Key.Escape] = 41, [Key.Backspace] = 42, [Key.Tab] = 43, [Key.Space] = 44,
            [Key.Minus] = 45, [Key.Equals] = 46, [Key.LeftBracket] = 47, [Key.RightBracket] = 48, [Key.Backslash] = 49,
            [Key.Semicolon] = 51, [Key.Apostrophe] = 52, [Key.Grave] = 53, [Key.Comma] = 54, [Key.Period] = 55, [Key.Slash] = 56,
            [Key.CapsLock] = 57, [Key.PrintScreen] = 70, [Key.ScrollLock] = 71, [Key.Pause] = 72, [Key.Insert] = 73,
            [Key.Home] = 74, [Key.PageUp] = 75, [Key.Delete] = 76, [Key.End] = 77, [Key.PageDown] = 78,
            [Key.Right] = 79, [Key.Left] = 80, [Key.Down] = 81, [Key.Up] = 82, [Key.NumLock] = 83,
            [Key.KeypadDivide] = 84, [Key.KeypadMultiply] = 85, [Key.KeypadMinus] = 86, [Key.KeypadPlus] = 87, [Key.KeypadEnter] = 88,
            [Key.Keypad0] = 98, [Key.KeypadPeriod] = 99, [Key.NonUsBackslash] = 100, [Key.Menu] = 101, [Key.KeypadEquals] = 103,
            [Key.LeftControl] = 224, [Key.LeftShift] = 225, [Key.LeftAlt] = 226, [Key.LeftSuper] = 227,
            [Key.RightControl] = 228, [Key.RightShift] = 229, [Key.RightAlt] = 230, [Key.RightSuper] = 231,
            [Key.D0] = 39,
        };
        for (int i = 0; i < 26; i++) map[Key.A + i] = 4 + i;
        for (int i = 1; i < 10; i++) map[Key.D0 + i] = 30 + i - 1;
        for (int i = 1; i < 10; i++) map[Key.Keypad0 + i] = 89 + i - 1;
        Key[] f1to12 = [Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6, Key.F7, Key.F8, Key.F9, Key.F10, Key.F11, Key.F12];
        for (int i = 0; i < f1to12.Length; i++) map[f1to12[i]] = 58 + i;
        for (int i = 0; i < 12; i++) map[Key.F13 + i] = 104 + i;
        return map;
    }
}

/// <summary>
/// A base for input backends of platforms whose system reports input as it happens or as per-frame state: a backend
/// emits events (<see cref="Emit"/>) while it processes the system's, and the engine drains them
/// (<see cref="Drain"/>). It also answers the polled questions (<see cref="IInputSource"/>) from the same events, for
/// code that reads a platform's <c>Input</c> directly; there a press shorter than a frame reads as held until the next
/// <see cref="BeginFrame"/>.
/// </summary>
public abstract class BufferedInputBackend : IInputBackend, IInputSource
{
    // Nobody may drain (a platform used without the engine's input): keep a bounded backlog, not an unbounded one.
    private const int MaxBacklog = 4096;
    private readonly List<InputEvent> _queue = [];
    private readonly InputDevices _state = new();
    private readonly IInputSource _view;

    /// <summary>Creates the backend with an empty queue.</summary>
    protected BufferedInputBackend() => _view = _state.View(_ => true);

    /// <summary>The state of the devices this backend reported, as its polled view reads it.</summary>
    protected InputDevices State => _state;

    /// <summary>Queues <paramref name="e"/> for the engine and applies it to the polled view.</summary>
    protected void Emit(in InputEvent e)
    {
        if (_queue.Count >= MaxBacklog) _queue.RemoveRange(0, MaxBacklog / 2);
        _queue.Add(e);
        _state.Apply(e);
    }

    /// <summary>Starts a frame of the polled view: presses already released stop reading as held. Call before the frame's events.</summary>
    public void BeginFrame() => _state.EndSample();

    /// <inheritdoc/>
    public void Drain(List<InputEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        events.AddRange(_queue);
        _queue.Clear();
    }

    /// <summary>
    /// Emits releases of every key, button and pointer button this backend reported held, as the window loses the
    /// focus (the system will not send them).
    /// </summary>
    protected void ReleaseHeld(double time)
    {
        foreach (var info in _state.Devices.ToList())
            foreach (var e in _state.HeldReleases(info.Id, time).ToList()) Emit(e);
    }

    /// <inheritdoc/>
    public bool IsKeyDown(Key key) => _view.IsKeyDown(key);
    /// <inheritdoc/>
    public bool IsPadDown(PadButton button) => _view.IsPadDown(button);
    /// <inheritdoc/>
    public Vector2 LeftStick => _view.LeftStick;
    /// <inheritdoc/>
    public float GetPadAxis(PadAxis axis) => _view.GetPadAxis(axis);
    /// <inheritdoc/>
    public bool PadConnected => _view.PadConnected;
    /// <inheritdoc/>
    public GamepadStyle PadStyle => _view.PadStyle;
    /// <inheritdoc/>
    public PointerState Pointer => _view.Pointer;
    /// <inheritdoc/>
    public IReadOnlyList<PointerInfo> Pointers => _view.Pointers;
}

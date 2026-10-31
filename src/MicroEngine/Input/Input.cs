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
public enum PadButton { None, A, B, X, Y, Start, Back, LeftShoulder, RightShoulder, DPadUp, DPadDown, DPadLeft, DPadRight }

/// <summary>The raw device state a backend reports once per polled frame.</summary>
public interface IInputSource
{
    bool IsKeyDown(Key key);
    bool IsPadDown(PadButton button);
    /// <summary>Left stick, each axis in -1..1.</summary>
    System.Numerics.Vector2 LeftStick { get; }
}

/// <summary>
/// Named actions bound to keys and gamepad buttons. Games query actions ("jump", "confirm"),
/// never physical keys, so bindings and scripted input stay in one place.
/// Edge states (pressed/released) are computed per fixed update, so a tap is never lost or doubled.
/// </summary>
public sealed class InputMap
{
    private readonly Dictionary<string, (List<Key> Keys, List<PadButton> Buttons, StickDirection Stick)> _bindings = new(StringComparer.Ordinal);
    private readonly HashSet<string> _down = new(StringComparer.Ordinal);
    private readonly HashSet<string> _previous = new(StringComparer.Ordinal);
    private readonly HashSet<string> _latched = new(StringComparer.Ordinal);
    private readonly HashSet<string> _forced = new(StringComparer.Ordinal);

    public enum StickDirection { None, Left, Right, Up, Down }

    public InputMap Bind(string action, params Key[] keys) => Bind(action, keys, [], StickDirection.None);

    public InputMap Bind(string action, Key[] keys, PadButton[] buttons, StickDirection stick = StickDirection.None)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        if (!_bindings.TryGetValue(action, out var b))
            _bindings[action] = b = ([], [], stick);
        b.Keys.AddRange(keys);
        b.Buttons.AddRange(buttons);
        if (stick != StickDirection.None) _bindings[action] = (b.Keys, b.Buttons, stick);
        return this;
    }

    public IEnumerable<string> Actions => _bindings.Keys;

    /// <summary>Samples devices. Called by the runner once per rendered frame.</summary>
    public void Sample(IInputSource source)
    {
        foreach (var (action, b) in _bindings)
        {
            bool down = _forced.Contains(action)
                || b.Keys.Any(source.IsKeyDown)
                || b.Buttons.Any(source.IsPadDown)
                || b.Stick switch
                {
                    StickDirection.Left => source.LeftStick.X < -0.5f,
                    StickDirection.Right => source.LeftStick.X > 0.5f,
                    StickDirection.Up => source.LeftStick.Y < -0.5f,
                    StickDirection.Down => source.LeftStick.Y > 0.5f,
                    _ => false,
                };
            if (down) { if (_down.Add(action)) _latched.Add(action); }
            else _down.Remove(action);
        }
    }

    /// <summary>Forces an action down regardless of devices (scripted playback, tests, touch overlays).</summary>
    public void SetForced(string action, bool down)
    {
        if (down) _forced.Add(action); else _forced.Remove(action);
    }

    /// <summary>Advances edge detection. Called by the runner after each fixed update.</summary>
    public void EndUpdate()
    {
        _previous.Clear();
        foreach (var a in _down) _previous.Add(a);
        _latched.Clear();
    }

    public bool Down(string action) => _down.Contains(action) || _latched.Contains(action);
    /// <summary>True during the first update after the action went down, even if it was released within the same frame.</summary>
    public bool Pressed(string action) => _latched.Contains(action) || (_down.Contains(action) && !_previous.Contains(action));
    public bool Released(string action) => !_down.Contains(action) && _previous.Contains(action);

    /// <summary>-1, 0 or 1 from two opposing actions.</summary>
    public int Axis(string negative, string positive) => (Down(positive) ? 1 : 0) - (Down(negative) ? 1 : 0);

    /// <summary>Clears every state, e.g. after a scene change, so held keys must be pressed again.</summary>
    public void Reset()
    {
        _previous.Clear();
        foreach (var a in _down) _previous.Add(a);
        _latched.Clear();
    }

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

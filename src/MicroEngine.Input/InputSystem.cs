using System.Numerics;
using System.Text;
using MicroEngine.Kernel;

namespace MicroEngine.Input;

/// <summary>
/// The text channel, separate from actions: while it is on, characters typed (and the text an input method is
/// composing) arrive here instead of as keys to remember. Turn it on for name entry, chat or a console; on phones it
/// shows the on-screen keyboard. A layer above gameplay (a text field) may take text events itself.
/// </summary>
public sealed class TextInput
{
    private readonly StringBuilder _pending = new();
    private ITextInputControl? _control;

    /// <summary>Whether text input is on.</summary>
    public bool Active { get; private set; }

    /// <summary>The characters typed since the previous update, in order (empty while off). Backspace and Enter are keys, never text.</summary>
    public string Typed { get; private set; } = "";

    /// <summary>The text an input method is composing and has not committed yet (empty when none).</summary>
    public string Composition { get; private set; } = "";

    /// <summary>
    /// Turns text input on. <paramref name="caret"/> is where the text being edited is, in virtual canvas pixels, so an
    /// input method can show its candidates next to it.
    /// </summary>
    public void Start(RectF caret = default)
    {
        Active = true;
        _control?.Start(caret);
    }

    /// <summary>Turns text input off and drops what was being composed.</summary>
    public void Stop()
    {
        if (!Active) return;
        Active = false;
        Composition = "";
        _pending.Clear();
        _control?.Stop();
    }

    internal void Attach(ITextInputControl? control)
    {
        _control = control;
        if (Active) control?.Start(default);
    }

    internal void Receive(in InputEvent e)
    {
        if (!Active) return;
        if (e.Kind == InputEventKind.Text)
        {
            _pending.Append(e.Text);
            Composition = "";
        }
        else Composition = e.Text ?? "";
    }

    internal void BeginUpdate()
    {
        Typed = _pending.Length == 0 ? "" : _pending.ToString();
        _pending.Clear();
    }

    internal void EndUpdate() => Typed = "";
}

/// <summary>
/// The engine's input: the devices and their events, the routing through input layers, the players and their action
/// maps, and the text channel. Each frame the events of the platform's input backend (and those sent with
/// <see cref="Send"/>) are routed through <see cref="Layers"/>; what no layer takes becomes the devices' polled state
/// and <see cref="Events"/>. Each update samples every player's map from the devices they hear.
/// </summary>
/// <remarks>
/// A game rarely needs this directly: <c>Game.Input</c> is player 1's map and <c>Game.Players</c> the players. Read
/// <see cref="Events"/> for what actions cannot express (a tap's position, a gesture, the order of presses).
/// </remarks>
public sealed class InputSystem : IStateHashed
{
    private readonly List<InputEvent> _drained = [], _sent = [], _pending = [], _events = [];
    private readonly List<RoutedEvent> _routed = [];
    private readonly List<(DeviceId Id, IInputSource Source)> _polled = [];
    private readonly Dictionary<(int Device, InputEventKind Kind, long Code), IInputLayer> _captures = [];
    private readonly Dictionary<PlayerInput, IInputSource> _views = [];
    private IInputBackend? _backend;
    private IGamepadRumble? _rumble;
    private int _nextPolled = -2;

    /// <summary>Creates the input with player 1 reading <paramref name="first"/>.</summary>
    public InputSystem(InputMap first)
    {
        Players = new Players(first);
    }

    /// <summary>The devices and their state.</summary>
    public InputDevices Devices { get; } = new();

    /// <summary>The local players; player 1 owns every device until devices are paired.</summary>
    public Players Players { get; }

    /// <summary>The input layers events pass through before gameplay.</summary>
    public InputLayers Layers { get; } = new();

    /// <summary>The text channel.</summary>
    public TextInput Text { get; } = new();

    /// <summary>
    /// Whether <see cref="Routed"/> records where each event of the frame went. Off by default; the game's diagnostics
    /// turn it on while they measure.
    /// </summary>
    public bool Trace
    {
        get => _trace;
        set
        {
            _trace = value;
            if (!value) _routed.Clear();
        }
    }
    private bool _trace;

    /// <summary>
    /// While <see cref="Trace"/> is on, the events routed this frame (device changes excepted) in order, each with the
    /// layer that took it, or none when it reached the devices and gameplay: the answer to "why did my action not fire?"
    /// when a menu, the on-screen controls or another layer took the press.
    /// </summary>
    public IReadOnlyList<RoutedEvent> Routed => _routed;

    /// <summary>The labels of the player's keyboard layout, when the backend knows them.</summary>
    public IKeyLabels? KeyLabels { get; private set; }

    /// <summary>
    /// The events no layer took, since the previous update, in order: read them in an update for what actions cannot
    /// say. Each event is seen by exactly one update (frames that run no update keep them for the next).
    /// </summary>
    public IReadOnlyList<InputEvent> Events => _events;

    /// <summary>The polled sources read as devices (a host's extra input, a test's hand-set input).</summary>
    public IReadOnlyList<(DeviceId Id, IInputSource Source)> PolledSources => _polled;

    /// <summary>Connects the platform's input: its event backend and the optional features it offers.</summary>
    public void Connect(IInputBackend? backend, IKeyLabels? labels = null, IGamepadRumble? rumble = null, ITextInputControl? text = null)
    {
        _backend = backend;
        KeyLabels = labels;
        _rumble = rumble;
        Text.Attach(text);
    }

    /// <summary>
    /// Reads <paramref name="source"/> as one more device, every update: its keys, gamepad and pointer, merged like one
    /// keyboard and one pad. Returns the id players pair it by.
    /// </summary>
    public DeviceId AddPolled(IInputSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var id = new DeviceId(_nextPolled--);
        _polled.Add((id, source));
        return id;
    }

    /// <summary>
    /// Queues an event as if a device had produced it; it is routed with the next frame's events, after the backend's.
    /// Scripted runs and tests drive input this way, through the same layers and state as real devices.
    /// </summary>
    public void Send(in InputEvent e) => _sent.Add(e);

    /// <summary>Starts a frame: routes the backend's events, then the sent ones. Called by the loop once per frame.</summary>
    public void BeginFrame()
    {
        _routed.Clear();
        _drained.Clear();
        _backend?.Drain(_drained);
        _drained.AddRange(_sent);
        _sent.Clear();
        foreach (var e in _drained) Route(e);
    }

    /// <summary>
    /// Routes one event now: to the layer that took the press it belongs to, else through the layers from the top, else
    /// into the devices' state and <see cref="Events"/>.
    /// </summary>
    public void Route(in InputEvent e)
    {
        switch (e.Kind)
        {
            case InputEventKind.DeviceAdded:
                Devices.Apply(e);
                _pending.Add(e);
                return;
            case InputEventKind.DeviceRemoved:
                int device = e.Device.Value;
                foreach (var key in _captures.Keys.Where(k => k.Device == device).ToList()) _captures.Remove(key);
                Devices.Apply(e);
                _pending.Add(e);
                return;
        }
        if (CaptureKey(e) is { } capture && _captures.TryGetValue(capture, out var owner))
        {
            owner.Handle(e);
            if (Ends(e)) _captures.Remove(capture);
            if (Trace) _routed.Add(new RoutedEvent(e, owner));
            return;
        }
        if (Layers.Offer(e) is { } taker)
        {
            if (Trace) _routed.Add(new RoutedEvent(e, taker));
            if (Starts(e) && CaptureKey(e) is { } start) _captures[start] = taker;
            return;
        }
        if (Trace) _routed.Add(new RoutedEvent(e, null));
        if (e.IsPress && Players.OwnerOf(e.Device) is null) Players.ReportUnpaired(e);
        Devices.Apply(e);
        if (e.Kind is InputEventKind.Text or InputEventKind.TextComposition) Text.Receive(e);
        _pending.Add(e);
    }

    private static (int, InputEventKind, long)? CaptureKey(in InputEvent e) => e.Kind switch
    {
        InputEventKind.KeyDown or InputEventKind.KeyUp => (e.Device.Value, InputEventKind.KeyDown, (long)e.Key),
        InputEventKind.ButtonDown or InputEventKind.ButtonUp => (e.Device.Value, InputEventKind.ButtonDown, (long)e.Button),
        InputEventKind.PointerDown or InputEventKind.PointerUp or InputEventKind.PointerMove or InputEventKind.PointerCancel
            => (e.Device.Value, InputEventKind.PointerDown, e.Pointer),
        _ => null,
    };

    private static bool Starts(in InputEvent e) => e.Kind is InputEventKind.PointerDown or InputEventKind.ButtonDown || e.Kind == InputEventKind.KeyDown && !e.Repeat;

    private static bool Ends(in InputEvent e) => e.Kind is InputEventKind.KeyUp or InputEventKind.ButtonUp or InputEventKind.PointerCancel
        || e.Kind == InputEventKind.PointerUp && (e.PointerKind != PointerKind.Mouse || e.PointerButton == PointerButton.Primary);

    /// <summary>
    /// Samples every player's actions from the devices they hear and makes the pending events this update's
    /// <see cref="Events"/>. Called by the loop before each fixed update.
    /// </summary>
    public void Sample()
    {
        _events.Clear();
        _events.AddRange(_pending);
        _pending.Clear();
        Text.BeginUpdate();
        foreach (var player in Players.InOrder) player.Map.Sample(ViewOf(player));
    }

    /// <summary>
    /// What <paramref name="player"/> hears, read as one polled source: the devices they own and the polled sources
    /// they own, a key or button down when any has it down.
    /// </summary>
    public IInputSource ViewOf(PlayerInput player)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (!_views.TryGetValue(player, out var view)) _views[player] = view = new PlayerView(this, player.Owns, Devices.View(player.Owns));
        return view;
    }

    /// <summary>
    /// Ends an update: edge detection of every map, presses already released stop reading as held, the wheel and the
    /// update's events are cleared. Called by the loop after each fixed update.
    /// </summary>
    public void EndUpdate()
    {
        foreach (var player in Players.InOrder) player.Map.EndUpdate();
        Devices.EndSample();
        _events.Clear();
        Text.EndUpdate();
    }

    /// <summary>
    /// Takes the next key or gamepad-button press of any device away from everything else and hands it to
    /// <paramref name="pressed"/>, once: the "press a key for jump" step of a rebinding screen
    /// (<see cref="InputMap.BindFrom"/>). Dispose the result to stop listening before a press came.
    /// </summary>
    public IDisposable ListenForPress(Action<InputEvent> pressed)
    {
        ArgumentNullException.ThrowIfNull(pressed);
        var listener = new PressListener(Layers, pressed);
        Layers.Add(listener, int.MaxValue);
        return listener;
    }

    private sealed class PressListener(InputLayers layers, Action<InputEvent> pressed) : IInputLayer, IDisposable
    {
        private bool _done;

        public bool Handle(in InputEvent e)
        {
            if (_done || e.Kind is not (InputEventKind.KeyDown or InputEventKind.ButtonDown) || e.Repeat) return false;
            _done = true;
            // Gone at once; the press's release still comes here, as the press's owner, so gameplay never sees it.
            layers.Remove(this);
            pressed(e);
            return true;
        }

        public void Dispose()
        {
            _done = true;
            layers.Remove(this);
        }
    }

    /// <summary>Releases every key, button and pointer and forgets which layer took what (focus lost, suspended).</summary>
    public void ReleaseAll()
    {
        Devices.ReleaseAll();
        _captures.Clear();
    }

    /// <summary>
    /// Rumbles every gamepad <paramref name="player"/> hears: low-frequency (heavy) and high-frequency (light) motors at
    /// 0..1 for <paramref name="seconds"/>. Returns false when none could rumble.
    /// </summary>
    public bool Rumble(PlayerInput player, float low, float high, double seconds)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (_rumble is null) return false;
        bool any = false;
        foreach (var info in Devices.Devices)
            if (info.Kind == DeviceKind.Gamepad && player.Owns(info.Id)) any |= _rumble.Rumble(info.Id, Math.Clamp(low, 0, 1), Math.Clamp(high, 0, 1), Math.Max(0, seconds));
        return any;
    }

    /// <summary>Rumbles one gamepad (see <see cref="Rumble(PlayerInput, float, float, double)"/>).</summary>
    public bool Rumble(DeviceId pad, float low, float high, double seconds) =>
        _rumble?.Rumble(pad, Math.Clamp(low, 0, 1), Math.Clamp(high, 0, 1), Math.Max(0, seconds)) ?? false;

    /// <summary>Adds the players beyond player 1 and the pairings (<see cref="Players.HashState"/>); player 1's map is hashed by its owner.</summary>
    public void HashState(StateHasher hash) => Players.HashState(hash);

    private sealed class PlayerView(InputSystem input, Func<DeviceId, bool> owns, IInputSource devices) : IInputSource
    {
        public bool IsKeyDown(Key key)
        {
            if (devices.IsKeyDown(key)) return true;
            foreach (var (id, s) in input._polled) if (owns(id) && s.IsKeyDown(key)) return true;
            return false;
        }

        public bool IsPadDown(PadButton button)
        {
            if (devices.IsPadDown(button)) return true;
            foreach (var (id, s) in input._polled) if (owns(id) && s.IsPadDown(button)) return true;
            return false;
        }

        public float GetPadAxis(PadAxis axis)
        {
            float best = devices.GetPadAxis(axis);
            foreach (var (id, s) in input._polled)
            {
                if (!owns(id)) continue;
                float v = s.GetPadAxis(axis);
                if (MathF.Abs(v) > MathF.Abs(best)) best = v;
            }
            return best;
        }

        public Vector2 LeftStick
        {
            get
            {
                var best = devices.LeftStick;
                foreach (var (id, s) in input._polled)
                {
                    if (!owns(id)) continue;
                    var v = s.LeftStick;
                    if (v.LengthSquared() > best.LengthSquared()) best = v;
                }
                return best;
            }
        }

        public bool PadConnected
        {
            get
            {
                if (devices.PadConnected) return true;
                foreach (var (id, s) in input._polled) if (owns(id) && s.PadConnected) return true;
                return false;
            }
        }

        public GamepadStyle PadStyle
        {
            get
            {
                if (devices.PadConnected) return devices.PadStyle;
                foreach (var (id, s) in input._polled) if (owns(id) && s.PadConnected) return s.PadStyle;
                return GamepadStyle.Xbox;
            }
        }

        public PointerState Pointer
        {
            get
            {
                var own = devices.Pointer;
                if (own.Present) return own;
                foreach (var (id, s) in input._polled)
                {
                    if (!owns(id)) continue;
                    var p = s.Pointer;
                    if (p.Present) return own.Wheel == Vector2.Zero ? p : p with { Wheel = p.Wheel + own.Wheel };
                }
                return own;
            }
        }

        public IReadOnlyList<PointerInfo> Pointers => devices.Pointers;
    }
}

/// <summary>One event of the frame and where it went (<see cref="InputSystem.Routed"/>).</summary>
/// <param name="Event">The event.</param>
/// <param name="Layer">The input layer that took it (or that took the press it ends), or null when it reached the devices and gameplay.</param>
public readonly record struct RoutedEvent(InputEvent Event, IInputLayer? Layer);

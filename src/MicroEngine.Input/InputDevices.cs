using System.Numerics;

namespace MicroEngine.Input;

/// <summary>A pointer the game can read: the mouse while it is over the window, or a finger or pen on the screen.</summary>
/// <param name="Device">The device it belongs to.</param>
/// <param name="Id">0 for the mouse, the finger's id for touches.</param>
/// <param name="Kind">Mouse, touch or pen.</param>
/// <param name="Position">Where it is, in virtual canvas pixels.</param>
/// <param name="Down">
/// Whether its primary button is held (a finger or pen is always down while it touches). Also true in the update after
/// a press that started and ended within one frame, so a quick tap is never lost.
/// </param>
public readonly record struct PointerInfo(DeviceId Device, long Id, PointerKind Kind, Vector2 Position, bool Down);

/// <summary>
/// The state of every device, derived from the events no input layer took: which keys and buttons are held, where the
/// axes, the pointers and the wheel are, and which devices are connected. A press that started and ended between two
/// updates still reads as held until an update has sampled it (<see cref="EndSample"/>): taps expire by updates, never
/// by drawn frames. Players read it through views limited to their devices (<see cref="View"/>).
/// </summary>
public sealed class InputDevices
{
    private sealed class Device(InputDeviceInfo info, long order)
    {
        public InputDeviceInfo Info = info;
        public readonly long Order = order;
        public readonly HashSet<Key> Keys = [];
        public readonly HashSet<Key> KeyTaps = [];
        public readonly HashSet<PadButton> Buttons = [];
        public readonly HashSet<PadButton> ButtonTaps = [];
        public readonly float[] Axes = new float[6];
        public readonly Dictionary<long, Pointer> Pointers = [];
        public Vector2 Wheel;
        public long Touched; // sequence of the last pointer event, to pick the pointer that moved last
    }

    private sealed class Pointer
    {
        public PointerKind Kind;
        public Vector2 Position;
        public bool Present;
        public PointerButtons Held, Taps;
        public long Order;
    }

    [Flags]
    private enum PointerButtons { None = 0, Primary = 1, Secondary = 2, Middle = 4, Back = 8, Forward = 16 }

    private readonly Dictionary<DeviceId, Device> _devices = [];
    private readonly List<InputDeviceInfo> _connected = [];
    private long _sequence;

    /// <summary>Raised when a device connects (also those present at start), after it was added.</summary>
    public event Action<InputDeviceInfo>? Connected;
    /// <summary>Raised when a device disconnects, after it was removed.</summary>
    public event Action<InputDeviceInfo>? Disconnected;

    /// <summary>The connected devices, in the order they connected.</summary>
    public IReadOnlyList<InputDeviceInfo> Devices => _connected;

    /// <summary>The device's description, or null when it is not connected.</summary>
    public InputDeviceInfo? Find(DeviceId id) => _devices.TryGetValue(id, out var d) ? d.Info : null;

    /// <summary>
    /// Applies one event. An event of a device that was never announced announces it first as a generic device of the
    /// event's kind, so a backend's own ids always work.
    /// </summary>
    public void Apply(in InputEvent e)
    {
        if (e.Kind == InputEventKind.DeviceAdded)
        {
            var info = e.Info ?? new InputDeviceInfo(e.Device, DeviceKind.Other, "device");
            if (_devices.TryGetValue(e.Device, out var known)) known.Info = info;
            else
            {
                _devices[e.Device] = new Device(info, ++_sequence);
                _connected.Add(info);
                Connected?.Invoke(info);
                return;
            }
            var added = e.Device;
            int index = _connected.FindIndex(d => d.Id == added);
            if (index >= 0) _connected[index] = info;
            return;
        }
        if (e.Kind == InputEventKind.DeviceRemoved)
        {
            var removed = e.Device;
            if (!_devices.Remove(removed, out var gone)) return;
            _connected.RemoveAll(d => d.Id == removed);
            Disconnected?.Invoke(gone.Info);
            return;
        }
        var device = Get(e);
        switch (e.Kind)
        {
            case InputEventKind.KeyDown:
                if (!e.Repeat && device.Keys.Add(e.Key)) device.KeyTaps.Add(e.Key);
                break;
            case InputEventKind.KeyUp:
                device.Keys.Remove(e.Key);
                break;
            case InputEventKind.ButtonDown:
                if (device.Buttons.Add(e.Button)) device.ButtonTaps.Add(e.Button);
                break;
            case InputEventKind.ButtonUp:
                device.Buttons.Remove(e.Button);
                break;
            case InputEventKind.AxisMoved:
                device.Axes[(int)e.Axis] = e.Value;
                break;
            case InputEventKind.Wheel:
                device.Wheel += e.Wheel;
                break;
            case InputEventKind.PointerDown:
            {
                var p = Touch(device, e);
                p.Present = true;
                var b = Flag(e.PointerButton);
                if ((p.Held & b) == 0) p.Taps |= b;
                p.Held |= b;
                break;
            }
            case InputEventKind.PointerUp:
            case InputEventKind.PointerCancel:
            {
                var p = Touch(device, e);
                p.Held &= e.Kind == InputEventKind.PointerCancel ? PointerButtons.None : ~Flag(e.PointerButton);
                if (e.Kind == InputEventKind.PointerCancel) p.Taps = PointerButtons.None;
                // A finger or pen that lifted stays readable until an update has seen it, then goes.
                if (e.PointerKind != PointerKind.Mouse) p.Present = false;
                break;
            }
            case InputEventKind.PointerMove:
                Touch(device, e).Present = true;
                break;
            case InputEventKind.PointerLeave:
                Touch(device, e).Present = false;
                break;
        }
    }

    private Device Get(in InputEvent e)
    {
        if (_devices.TryGetValue(e.Device, out var d)) return d;
        var kind = e.Kind switch
        {
            InputEventKind.KeyDown or InputEventKind.KeyUp or InputEventKind.Text or InputEventKind.TextComposition => DeviceKind.Keyboard,
            InputEventKind.ButtonDown or InputEventKind.ButtonUp or InputEventKind.AxisMoved => DeviceKind.Gamepad,
            _ => e.PointerKind == PointerKind.Touch ? DeviceKind.Touch : DeviceKind.Mouse,
        };
        Apply(InputEvent.Added(new InputDeviceInfo(e.Device, kind, kind.ToString().ToLowerInvariant())));
        return _devices[e.Device];
    }

    private Pointer Touch(Device device, in InputEvent e)
    {
        if (!device.Pointers.TryGetValue(e.Pointer, out var p)) device.Pointers[e.Pointer] = p = new Pointer { Kind = e.PointerKind };
        p.Position = e.Position;
        p.Order = device.Touched = ++_sequence;
        return p;
    }

    private static PointerButtons Flag(PointerButton button) => button switch
    {
        PointerButton.Primary => PointerButtons.Primary,
        PointerButton.Secondary => PointerButtons.Secondary,
        PointerButton.Middle => PointerButtons.Middle,
        PointerButton.Back => PointerButtons.Back,
        PointerButton.Forward => PointerButtons.Forward,
        _ => PointerButtons.None,
    };

    /// <summary>
    /// Ends a sample: presses that were already released stop reading as held, the wheel starts again from 0 and
    /// lifted fingers go. Called once after every update has read the devices.
    /// </summary>
    public void EndSample()
    {
        foreach (var d in _devices.Values)
        {
            d.KeyTaps.Clear();
            d.ButtonTaps.Clear();
            d.Wheel = Vector2.Zero;
            List<long>? gone = null;
            foreach (var (id, p) in d.Pointers)
            {
                p.Taps = PointerButtons.None;
                if (!p.Present && p.Kind != PointerKind.Mouse) (gone ??= []).Add(id);
            }
            if (gone is not null) foreach (var id in gone) d.Pointers.Remove(id);
        }
    }

    /// <summary>Releases everything held on every device (the window lost focus, the game was suspended).</summary>
    public void ReleaseAll()
    {
        foreach (var d in _devices.Values)
        {
            d.Keys.Clear();
            d.KeyTaps.Clear();
            d.Buttons.Clear();
            d.ButtonTaps.Clear();
            Array.Clear(d.Axes);
            d.Wheel = Vector2.Zero;
            foreach (var p in d.Pointers.Values) p.Held = p.Taps = PointerButtons.None;
            List<long>? gone = null;
            foreach (var (id, p) in d.Pointers) if (p.Kind != PointerKind.Mouse) (gone ??= []).Add(id);
            if (gone is not null) foreach (var id in gone) d.Pointers.Remove(id);
        }
    }

    /// <summary>
    /// The state of the devices <paramref name="owns"/> accepts, read as one polled source: a key or button is down when
    /// any of them has it down, the axis with the largest deflection wins, the pointer is the one that moved last.
    /// </summary>
    public IInputSource View(Func<DeviceId, bool> owns)
    {
        ArgumentNullException.ThrowIfNull(owns);
        return new DeviceView(this, owns);
    }

    private sealed class DeviceView(InputDevices devices, Func<DeviceId, bool> owns) : IInputSource
    {
        private readonly List<PointerInfo> _pointers = [];

        public bool IsKeyDown(Key key)
        {
            foreach (var (id, d) in devices._devices)
                if ((d.Keys.Contains(key) || d.KeyTaps.Contains(key)) && owns(id)) return true;
            return false;
        }

        public bool IsPadDown(PadButton button)
        {
            foreach (var (id, d) in devices._devices)
                if ((d.Buttons.Contains(button) || d.ButtonTaps.Contains(button)) && owns(id)) return true;
            return false;
        }

        public float GetPadAxis(PadAxis axis)
        {
            float best = 0;
            foreach (var (id, d) in devices._devices)
            {
                float v = d.Axes[(int)axis];
                if (MathF.Abs(v) > MathF.Abs(best) && owns(id)) best = v;
            }
            return best;
        }

        public Vector2 LeftStick
        {
            get
            {
                var best = Vector2.Zero;
                foreach (var (id, d) in devices._devices)
                {
                    var v = new Vector2(d.Axes[(int)PadAxis.LeftX], d.Axes[(int)PadAxis.LeftY]);
                    if (v.LengthSquared() > best.LengthSquared() && owns(id)) best = v;
                }
                return best;
            }
        }

        public bool PadConnected => FirstPad() is not null;

        public GamepadStyle PadStyle => FirstPad()?.Style ?? GamepadStyle.Xbox;

        private InputDeviceInfo? FirstPad()
        {
            foreach (var info in devices._connected)
                if (info.Kind == DeviceKind.Gamepad && owns(info.Id)) return info;
            return null;
        }

        public PointerState Pointer
        {
            get
            {
                Pointer? last = null;
                Device? lastDevice = null;
                foreach (var (id, d) in devices._devices)
                {
                    if (d.Pointers.Count == 0 || !owns(id)) continue;
                    foreach (var p in d.Pointers.Values)
                        if (last is null || p.Order > last.Order) (last, lastDevice) = (p, d);
                }
                var wheel = Vector2.Zero;
                foreach (var (id, d) in devices._devices)
                    if (d.Wheel != Vector2.Zero && owns(id)) wheel += d.Wheel;
                if (last is null) return wheel == Vector2.Zero ? default : new PointerState(false, default, false, false, false) { Wheel = wheel };
                var held = last.Held | last.Taps;
                // A finger that lifted within the frame is still there for the update that reads the tap.
                bool present = last.Present || last.Taps != PointerButtons.None;
                return new PointerState(present, last.Position, (held & PointerButtons.Primary) != 0,
                    (held & PointerButtons.Secondary) != 0, (held & PointerButtons.Middle) != 0) { Wheel = wheel };
            }
        }

        public IReadOnlyList<PointerInfo> Pointers
        {
            get
            {
                _pointers.Clear();
                foreach (var (id, d) in devices._devices)
                {
                    if (d.Pointers.Count == 0 || !owns(id)) continue;
                    foreach (var (pid, p) in d.Pointers)
                    {
                        bool tapped = p.Taps != PointerButtons.None;
                        if (!p.Present && !tapped) continue;
                        _pointers.Add(new PointerInfo(id, pid, p.Kind, p.Position, ((p.Held | p.Taps) & PointerButtons.Primary) != 0));
                    }
                }
                // Device order, then pointer id: the same events give the same list on every run.
                _pointers.Sort(static (a, b) => a.Device.Value != b.Device.Value ? a.Device.Value.CompareTo(b.Device.Value) : a.Id.CompareTo(b.Id));
                return _pointers;
            }
        }
    }
}

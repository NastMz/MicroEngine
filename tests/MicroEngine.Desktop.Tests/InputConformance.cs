using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Input;
using MicroEngine.Raylib;
using MicroEngine.Sdl;
using SDL;
using static SDL.SDL3;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// The input backend contract (<see cref="IInputBackend"/>) as a conformance suite, like the platform's: every host runs
/// the same tests by subclassing this class and saying how a test makes its devices act (a key, the mouse, the wheel, a
/// gamepad plugged in and pressed). What a host cannot be made to do here is reported as skipped with the reason,
/// never passed. Every drained event is also checked against the contract's invariants (devices announced before use,
/// nothing from a removed device, releases only of what was held, time never going back).
/// </summary>
public abstract class InputConformance
{
    protected abstract IPlatform? Open(PlatformRequest request);
    /// <summary>Makes the keyboard press or release <paramref name="key"/> (delivered by the next pump).</summary>
    protected virtual bool Key(IPlatform host, Key key, bool down) => false;
    /// <summary>Moves the mouse over the window to a point in window coordinates and presses or releases its primary button.</summary>
    protected virtual bool Mouse(IPlatform host, Vector2 at, bool? down) => false;
    /// <summary>Turns the wheel.</summary>
    protected virtual bool Wheel(IPlatform host, float notches) => false;
    /// <summary>Plugs a gamepad in; returns a handle the test passes back, or null when this host cannot be made to.</summary>
    protected virtual object? PlugPad(IPlatform host, string name) => null;
    /// <summary>Presses or releases the bottom face button of a pad plugged in by <see cref="PlugPad"/>.</summary>
    protected virtual bool PadButton(IPlatform host, object pad, bool down) => false;
    /// <summary>Unplugs a pad plugged in by <see cref="PlugPad"/>.</summary>
    protected virtual bool UnplugPad(IPlatform host, object pad) => false;
    /// <summary>Makes the window lose the focus.</summary>
    protected virtual bool Blur(IPlatform host) => false;
    /// <summary>What the host does between pumps (a frame, for hosts that read the system when a frame ends).</summary>
    protected virtual void BetweenPumps(IPlatform host) => host.EndFrame();

    private static readonly GameSettings Settings = new("input conformance", 64, 36, 2);
    private static PlatformRequest Request => new(Settings) { Hidden = true, WaitForDisplay = false, Mute = true };

    private sealed class Contract
    {
        private readonly HashSet<DeviceId> _known = [];
        private readonly HashSet<(DeviceId, Key)> _keys = [];
        private readonly HashSet<(DeviceId, PadButton)> _buttons = [];
        private double _time;
        public List<InputEvent> All { get; } = [];

        public void Check(IEnumerable<InputEvent> events)
        {
            foreach (var e in events)
            {
                All.Add(e);
                Assert.IsTrue(e.Time >= _time - 1e-9, $"time went back at {e.Kind} ({e.Time} < {_time})");
                _time = Math.Max(_time, e.Time);
                switch (e.Kind)
                {
                    case InputEventKind.DeviceAdded:
                        Assert.IsNotNull(e.Info, "a connection describes the device");
                        Assert.AreEqual(e.Device, e.Info.Id);
                        _known.Add(e.Device);
                        continue;
                    case InputEventKind.DeviceRemoved:
                        Assert.IsTrue(_known.Remove(e.Device), $"{e.Device} removed without being added");
                        Assert.IsFalse(_keys.Any(k => k.Item1 == e.Device) || _buttons.Any(b => b.Item1 == e.Device), "a device is removed after releasing what it held");
                        continue;
                }
                Assert.IsTrue(_known.Contains(e.Device), $"{e.Kind} of {e.Device}, which was never announced");
                Assert.IsTrue(float.IsFinite(e.Position.X) && float.IsFinite(e.Position.Y), "positions are finite");
                switch (e.Kind)
                {
                    case InputEventKind.KeyDown when !e.Repeat:
                        Assert.IsTrue(_keys.Add((e.Device, e.Key)), $"{e.Key} pressed twice without a release");
                        break;
                    case InputEventKind.KeyUp:
                        Assert.IsTrue(_keys.Remove((e.Device, e.Key)), $"{e.Key} released without a press");
                        break;
                    case InputEventKind.ButtonDown:
                        Assert.IsTrue(_buttons.Add((e.Device, e.Button)), $"{e.Button} pressed twice without a release");
                        break;
                    case InputEventKind.ButtonUp:
                        Assert.IsTrue(_buttons.Remove((e.Device, e.Button)), $"{e.Button} released without a press");
                        break;
                }
            }
        }
    }

    private sealed class Rig : IDisposable
    {
        public IPlatform Host { get; }
        public IInputBackend Backend { get; }
        public Contract Contract { get; } = new();
        private readonly InputConformance _suite;

        public Rig(InputConformance suite)
        {
            _suite = suite;
            Host = suite.Open(Request) ?? throw new AssertInconclusiveException("the host cannot open here");
            Assert.IsTrue(Host.TryGetCapability<IInputBackend>(out var backend), "the host offers its input as an event backend");
            Backend = backend;
        }

        /// <summary>A few frames: pump, drain, check.</summary>
        public List<InputEvent> Frames(int count = 3)
        {
            var seen = new List<InputEvent>();
            for (int i = 0; i < count; i++)
            {
                Host.PumpEvents(NativeChecks.IgnoredEvents.Instance);
                var drained = new List<InputEvent>();
                Backend.Drain(drained);
                Contract.Check(drained);
                seen.AddRange(drained);
                Host.BeginFrame();
                _suite.BetweenPumps(Host);
            }
            return seen;
        }

        public void Dispose() => Host.Dispose();
    }

    private static void Skip(bool driven, string what)
    {
        if (!driven) Assert.Inconclusive($"this host cannot be made to {what} here");
    }

    [TestMethod]
    public void The_keyboard_and_mouse_are_announced_and_a_second_drain_is_empty()
    {
        using var rig = new Rig(this);
        var first = rig.Frames(2);
        Assert.IsTrue(rig.Contract.All.Any(e => e.Kind == InputEventKind.DeviceAdded && e.Info!.Kind == DeviceKind.Keyboard), "a keyboard is announced");
        var again = new List<InputEvent>();
        rig.Backend.Drain(again);
        Assert.AreEqual(0, again.Count, "drained events are gone");
        _ = first;
    }

    [TestMethod]
    public void A_key_press_and_release_arrive_in_order_as_positions()
    {
        using var rig = new Rig(this);
        rig.Frames(1);
        Skip(Key(rig.Host, Input.Key.A, true), "press a key");
        var down = rig.Frames(2);
        Skip(Key(rig.Host, Input.Key.A, false), "release a key");
        var up = rig.Frames(2);
        Assert.AreEqual(1, down.Count(e => e.Kind == InputEventKind.KeyDown && e.Key == Input.Key.A && !e.Repeat), "one press of the key in A's place");
        Assert.AreEqual(1, up.Count(e => e.Kind == InputEventKind.KeyUp && e.Key == Input.Key.A), "one release");
    }

    [TestMethod]
    public void A_tap_within_one_frame_is_a_press_then_a_release()
    {
        using var rig = new Rig(this);
        rig.Frames(1);
        Skip(Key(rig.Host, Input.Key.Space, true) && Key(rig.Host, Input.Key.Space, false), "tap a key");
        var kinds = rig.Frames(2).Where(e => e.Key == Input.Key.Space && e.Kind is InputEventKind.KeyDown or InputEventKind.KeyUp).Select(e => e.Kind).ToArray();
        CollectionAssert.AreEqual(new[] { InputEventKind.KeyDown, InputEventKind.KeyUp }, kinds);
    }

    [TestMethod]
    public void The_clipboard_gives_back_the_text_put_on_it()
    {
        using var rig = new Rig(this);
        rig.Frames(1);
        Assert.IsTrue(rig.Host.TryGetCapability<IClipboard>(out var clipboard), "the host offers the system clipboard");
        string? before = clipboard.GetText();
        try
        {
            string text = $"MicroEngine clipboard check {Guid.NewGuid():N}";
            Assert.IsTrue(clipboard.SetText(text));
            rig.Frames(1);
            Assert.AreEqual(text, clipboard.GetText());
        }
        finally
        {
            if (before is not null) clipboard.SetText(before); // the machine's clipboard as the test found it
        }
    }

    [TestMethod]
    public void Losing_the_focus_releases_held_keys()
    {
        using var rig = new Rig(this);
        rig.Frames(1);
        Skip(Key(rig.Host, Input.Key.Left, true), "press a key");
        rig.Frames(1);
        Skip(Blur(rig.Host), "lose the focus");
        var after = rig.Frames(2);
        Assert.IsTrue(after.Any(e => e.Kind == InputEventKind.KeyUp && e.Key == Input.Key.Left), "the held key is released");
    }

    [TestMethod]
    public void The_mouse_moves_presses_and_turns_its_wheel_in_canvas_pixels()
    {
        using var rig = new Rig(this);
        rig.Frames(1);
        Skip(Mouse(rig.Host, new Vector2(10, 10), null) && Mouse(rig.Host, new Vector2(10, 10), true) && Mouse(rig.Host, new Vector2(10, 10), false), "use the mouse");
        var events = rig.Frames(2);
        Assert.IsTrue(events.Any(e => e.Kind == InputEventKind.PointerMove && e.PointerKind == PointerKind.Mouse));
        var kinds = events.Where(e => e.Kind is InputEventKind.PointerDown or InputEventKind.PointerUp).Select(e => (e.Kind, e.PointerButton)).ToArray();
        CollectionAssert.AreEqual(new[] { (InputEventKind.PointerDown, PointerButton.Primary), (InputEventKind.PointerUp, PointerButton.Primary) }, kinds);
        Skip(Wheel(rig.Host, 1), "turn the wheel");
        var wheel = rig.Frames(2).Where(e => e.Kind == InputEventKind.Wheel).ToList();
        Assert.AreEqual(1, wheel.Count);
        Assert.AreEqual(1, wheel[0].Wheel.Y, 1e-4f, "one notch away from the player");
    }

    [TestMethod]
    public void Every_pad_has_its_own_id_and_is_released_before_it_goes()
    {
        using var rig = new Rig(this);
        rig.Frames(1);
        var first = PlugPad(rig.Host, "Virtual DualSense");
        Skip(first is not null, "plug a gamepad in");
        var second = PlugPad(rig.Host, "Virtual Xbox")!;
        var added = rig.Frames(3).Where(e => e.Kind == InputEventKind.DeviceAdded && e.Info!.Kind == DeviceKind.Gamepad).ToList();
        Assert.AreEqual(2, added.Count, "both pads are announced");
        Assert.AreNotEqual(added[0].Device, added[1].Device);
        Skip(PadButton(rig.Host, second, true), "press a pad button");
        var press = rig.Frames(2).Where(e => e.Kind == InputEventKind.ButtonDown).ToList();
        Assert.AreEqual(1, press.Count);
        Assert.AreEqual(Input.PadButton.A, press[0].Button);
        Assert.AreEqual(added[1].Device, press[0].Device, "the press names the pad that made it");
        Skip(UnplugPad(rig.Host, second), "unplug a pad");
        var gone = rig.Frames(3);
        int release = gone.FindIndex(e => e.Kind == InputEventKind.ButtonUp && e.Device == added[1].Device);
        int removed = gone.FindIndex(e => e.Kind == InputEventKind.DeviceRemoved && e.Device == added[1].Device);
        Assert.IsTrue(release >= 0 && removed > release, "the held button is released, then the pad goes");
        UnplugPad(rig.Host, first!);
        rig.Frames(3);
    }
}

[TestClass]
public sealed class HeadlessInputConformance : InputConformance
{
    private static readonly DeviceId Keyboard = new(1), MouseId = new(2);
    private int _pads;
    private readonly HashSet<DeviceId> _held = [];

    protected override IPlatform? Open(PlatformRequest request)
    {
        var host = HeadlessPlatform.For(request);
        host.Devices.Send(InputEvent.Added(new InputDeviceInfo(Keyboard, DeviceKind.Keyboard, "keyboard")))
            .Send(InputEvent.Added(new InputDeviceInfo(MouseId, DeviceKind.Mouse, "mouse")));
        return host;
    }

    private static HeadlessInputBackend Devices(IPlatform host) => ((HeadlessPlatform)host).Devices;

    protected override bool Key(IPlatform host, Key key, bool down)
    {
        Devices(host).Send(down ? InputEvent.KeyDown(Keyboard, key) : InputEvent.KeyUp(Keyboard, key));
        return true;
    }

    protected override bool Mouse(IPlatform host, Vector2 at, bool? down)
    {
        Devices(host).Send(down switch
        {
            null => InputEvent.PointerMove(MouseId, 0, PointerKind.Mouse, at),
            true => InputEvent.PointerDown(MouseId, 0, PointerKind.Mouse, at),
            false => InputEvent.PointerUp(MouseId, 0, PointerKind.Mouse, at),
        });
        return true;
    }

    protected override bool Wheel(IPlatform host, float notches)
    {
        Devices(host).Send(InputEvent.WheelMoved(MouseId, new Vector2(0, notches)));
        return true;
    }

    protected override object? PlugPad(IPlatform host, string name)
    {
        var id = new DeviceId(100 + ++_pads);
        Devices(host).Send(InputEvent.Added(new InputDeviceInfo(id, DeviceKind.Gamepad, name)));
        return id;
    }

    protected override bool PadButton(IPlatform host, object pad, bool down)
    {
        var id = (DeviceId)pad;
        if (down) _held.Add(id); else _held.Remove(id);
        Devices(host).Send(down ? InputEvent.ButtonDown(id, Input.PadButton.A) : InputEvent.ButtonUp(id, Input.PadButton.A));
        return true;
    }

    protected override bool UnplugPad(IPlatform host, object pad)
    {
        var id = (DeviceId)pad;
        // The headless backend sends exactly what a test gives it, so the test keeps the contract as a backend would.
        if (_held.Remove(id)) Devices(host).Send(InputEvent.ButtonUp(id, Input.PadButton.A));
        Devices(host).Send(InputEvent.Removed(id));
        return true;
    }

    protected override bool Blur(IPlatform host)
    {
        Devices(host).Send(InputEvent.KeyUp(Keyboard, Input.Key.Left));
        return true;
    }

    protected override void BetweenPumps(IPlatform host) { }
}

/// <summary>SDL driven through its own event queue (keys, mouse, wheel, focus) and its virtual gamepads.</summary>
[TestClass]
[TestCategory("Native")]
public sealed unsafe class SdlInputConformance : InputConformance
{
    [TestInitialize]
    public void NotOnMacOS()
    {
        if (OperatingSystem.IsMacOS()) Assert.Inconclusive("windows can only be opened on the main thread on macOS");
        // The hidden window never has focus, and SDL keeps pad input from unfocused windows.
        SDL_SetHint(SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS, "1");
    }

    protected override IPlatform? Open(PlatformRequest request)
    {
        try { return SdlPlatform.Profile.Create(request); }
        catch (InvalidOperationException e)
        {
            Native.Require(false, $"SDL cannot open here: {e.Message}");
            return null;
        }
    }

    private static SDL_Window* Window(IPlatform host) => (SDL_Window*)host.MainSurface!.NativeWindow;

    protected override bool Key(IPlatform host, Key key, bool down)
    {
        var e = new SDL_Event();
        e.key.type = down ? SDL_EventType.SDL_EVENT_KEY_DOWN : SDL_EventType.SDL_EVENT_KEY_UP;
        e.key.timestamp = SDL_GetTicksNS();
        e.key.windowID = SDL_GetWindowID(Window(host));
        e.key.scancode = (SDL_Scancode)KeyUsages.Usage(key);
        e.key.down = down;
        return SDL_PushEvent(&e);
    }

    protected override bool Mouse(IPlatform host, Vector2 at, bool? down)
    {
        var e = new SDL_Event();
        if (down is { } pressed)
        {
            e.button.type = pressed ? SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN : SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP;
            e.button.timestamp = SDL_GetTicksNS();
            e.button.windowID = SDL_GetWindowID(Window(host));
            e.button.which = (SDL_MouseID)1;
            e.button.button = 1;
            e.button.down = pressed;
            e.button.x = at.X;
            e.button.y = at.Y;
        }
        else
        {
            e.motion.type = SDL_EventType.SDL_EVENT_MOUSE_MOTION;
            e.motion.timestamp = SDL_GetTicksNS();
            e.motion.windowID = SDL_GetWindowID(Window(host));
            e.motion.which = (SDL_MouseID)1;
            e.motion.x = at.X;
            e.motion.y = at.Y;
        }
        return SDL_PushEvent(&e);
    }

    protected override bool Wheel(IPlatform host, float notches)
    {
        var e = new SDL_Event();
        e.wheel.type = SDL_EventType.SDL_EVENT_MOUSE_WHEEL;
        e.wheel.timestamp = SDL_GetTicksNS();
        e.wheel.windowID = SDL_GetWindowID(Window(host));
        e.wheel.which = (SDL_MouseID)1;
        e.wheel.y = notches;
        e.wheel.direction = SDL_MouseWheelDirection.SDL_MOUSEWHEEL_NORMAL;
        return SDL_PushEvent(&e);
    }

    protected override bool Blur(IPlatform host)
    {
        var e = new SDL_Event();
        e.window.type = SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST;
        e.window.timestamp = SDL_GetTicksNS();
        e.window.windowID = SDL_GetWindowID(Window(host));
        return SDL_PushEvent(&e);
    }

    protected override object? PlugPad(IPlatform host, string name)
    {
        var desc = new SDL_VirtualJoystickDesc
        {
            version = (uint)sizeof(SDL_VirtualJoystickDesc),
            type = (ushort)SDL_JoystickType.SDL_JOYSTICK_TYPE_GAMEPAD,
            vendor_id = 0x045E,
            product_id = 0x0B12,
            naxes = (ushort)SDL_GamepadAxis.SDL_GAMEPAD_AXIS_COUNT,
            nbuttons = (ushort)SDL_GamepadButton.SDL_GAMEPAD_BUTTON_COUNT,
            button_mask = (1u << (int)SDL_GamepadButton.SDL_GAMEPAD_BUTTON_COUNT) - 1,
            axis_mask = (1u << (int)SDL_GamepadAxis.SDL_GAMEPAD_AXIS_COUNT) - 1,
        };
        var utf8 = System.Text.Encoding.UTF8.GetBytes(name + "\0");
        fixed (byte* text = utf8)
        {
            desc.name = text;
            var id = SDL_AttachVirtualJoystick(&desc);
            if ((uint)id == 0) return null;
            return (id, (nint)SDL_OpenJoystick(id));
        }
    }

    protected override bool PadButton(IPlatform host, object pad, bool down)
    {
        var (_, joystick) = ((SDL_JoystickID, nint))pad;
        bool ok = SDL_SetJoystickVirtualButton((SDL_Joystick*)joystick, (int)SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH, down);
        SDL_UpdateJoysticks();
        return ok;
    }

    protected override bool UnplugPad(IPlatform host, object pad)
    {
        var (id, joystick) = ((SDL_JoystickID, nint))pad;
        SDL_CloseJoystick((SDL_Joystick*)joystick);
        return SDL_DetachVirtualJoystick(id);
    }

    [TestMethod]
    public void F11_and_Alt_Enter_are_keys_like_any_other_and_the_backend_never_switches_the_window_itself()
    {
        using var host = Open(new PlatformRequest(new GameSettings("keys", 64, 36)) { Hidden = true, WaitForDisplay = false, Mute = true })
            ?? throw new AssertInconclusiveException("SDL cannot open here");
        Assert.IsTrue(host.TryGetCapability<MicroEngine.Core.IDisplayControl>(out var display));
        var before = display.WindowMode;
        Assert.IsTrue(Key(host, Input.Key.F11, true) && Key(host, Input.Key.F11, false));
        Assert.IsTrue(Key(host, Input.Key.LeftAlt, true) && Key(host, Input.Key.Enter, true) && Key(host, Input.Key.Enter, false) && Key(host, Input.Key.LeftAlt, false));
        host.PumpEvents(NativeChecks.IgnoredEvents.Instance);
        Assert.IsTrue(host.TryGetCapability<IInputBackend>(out var backend));
        var events = new List<InputEvent>();
        backend.Drain(events);
        Assert.AreEqual(before, display.WindowMode, "switching is the engine's full-screen action, not the backend's");
        CollectionAssert.IsSubsetOf(new[] { Input.Key.F11, Input.Key.LeftAlt, Input.Key.Enter },
            events.Where(e => e.Kind == InputEventKind.KeyDown).Select(e => e.Key).ToArray(), "the keys reach the engine as events");
    }

    [TestMethod]
    public void Character_keys_are_labelled_by_the_layout_and_named_keys_are_left_to_the_engine()
    {
        using var host = Open(new PlatformRequest(new GameSettings("labels", 64, 36)) { Hidden = true, WaitForDisplay = false, Mute = true })
            ?? throw new AssertInconclusiveException("SDL cannot open here");
        Assert.IsTrue(host.TryGetCapability<IKeyLabels>(out var labels));
        var a = labels.KeyLabel(Input.Key.A);
        Assert.IsNotNull(a, "a letter key has the layout's label");
        Assert.AreEqual(1, a.Length, a);
        Assert.IsNull(labels.KeyLabel(Input.Key.Space), "Space is named by the engine");
        Assert.IsNull(labels.KeyLabel(Input.Key.Left));
        Assert.IsTrue(host.TryGetCapability<IGamepadRumble>(out var rumble));
        Assert.IsFalse(rumble.Rumble(new DeviceId(12345), 1, 1, 0.1), "no such pad");
        Assert.IsTrue(host.TryGetCapability<ITextInputControl>(out var text));
        text.Start(new RectF(4, 4, 2, 8));
        text.Stop();
    }
}

/// <summary>
/// raylib on desktop: GLFW gives no way to inject keys, mouse or pads from code, so only what needs no driving runs
/// here; its keyboard path is checked with real key events through the X server (MicroEngine.Raylib.Tests).
/// </summary>
[TestClass]
[TestCategory("Native")]
public sealed class RaylibInputConformance : InputConformance
{
    [TestInitialize]
    public void NotOnMacOS()
    {
        if (OperatingSystem.IsMacOS()) Assert.Inconclusive("windows can only be opened on the main thread on macOS");
    }

    protected override IPlatform? Open(PlatformRequest request)
    {
        try { return RaylibPlatform.Profile.Create(request); }
        catch (InvalidOperationException e)
        {
            Native.Require(false, $"raylib cannot open here: {e.Message}");
            return null;
        }
    }
}

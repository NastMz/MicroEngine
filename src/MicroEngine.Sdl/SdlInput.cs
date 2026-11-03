using System.Numerics;
using MicroEngine.Input;
using SDL;
using static SDL.SDL3;

namespace MicroEngine.Sdl;

/// <summary>
/// SDL's input as device events (<see cref="IInputBackend"/>): the keyboard by scancode (every key of a full-size
/// keyboard), text and input-method composition while text input is on, the mouse with its wheel, and every gamepad
/// with its own id, hot-plugged, with its type, the player number it shows and rumble. Fingers stay with the touch
/// screen (<see cref="SdlTouchScreen"/>), which the engine turns into pointers. Also the platform's
/// <see cref="IKeyLabels"/> (the layout's label for a scancode), <see cref="IGamepadRumble"/> and
/// <see cref="ITextInputControl"/>.
/// </summary>
internal sealed unsafe class SdlInput : BufferedInputBackend, IKeyLabels, IGamepadRumble, ITextInputControl, IDisposable
{
    /// <summary>The keyboard: SDL merges every keyboard into one state, and so does the engine.</summary>
    public static readonly DeviceId Keyboard = new(1);
    /// <summary>The mouse.</summary>
    public static readonly DeviceId Mouse = new(2);
    private const int PadBase = 100; // pad ids: 100 + SDL's joystick id, which SDL never reuses while it runs

    private readonly SdlPlatform _platform;
    private readonly Dictionary<SDL_JoystickID, nint> _pads = [];
    private bool _mouseInside;

    public SdlInput(SdlPlatform platform)
    {
        _platform = platform;
        Emit(InputEvent.Added(new InputDeviceInfo(Keyboard, DeviceKind.Keyboard, "keyboard")));
        Emit(InputEvent.Added(new InputDeviceInfo(Mouse, DeviceKind.Mouse, "mouse")));
        // A mouse already over the window is there before it first moves.
        float x, y;
        SDL_GetMouseState(&x, &y);
        if (SDL_GetMouseFocus() == platform.Window)
        {
            _mouseInside = true;
            Emit(InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, platform.ToCanvas(x, y)) with { ScreenPosition = platform.ToPixels(x, y) });
        }
    }

    /// <summary>The engine's id of SDL's gamepad <paramref name="id"/>.</summary>
    public static DeviceId PadId(SDL_JoystickID id) => new(PadBase + (int)(uint)id);

    /// <summary>Turns one SDL event into the engine's input events; false when it is not an input event.</summary>
    public bool Handle(in SDL_Event e)
    {
        switch ((SDL_EventType)e.type)
        {
            case SDL_EventType.SDL_EVENT_KEY_DOWN:
            case SDL_EventType.SDL_EVENT_KEY_UP:
            {
                var key = KeyUsages.FromHidUsage((int)e.key.scancode);
                if (key == Key.None) return true;
                double t = _platform.Seconds(e.key.timestamp);
                Emit(e.key.down ? InputEvent.KeyDown(Keyboard, key, t, e.key.repeat) : InputEvent.KeyUp(Keyboard, key, t));
                return true;
            }
            case SDL_EventType.SDL_EVENT_TEXT_INPUT:
                if (e.text.GetText() is { Length: > 0 } text) Emit(InputEvent.TextInput(Keyboard, text, _platform.Seconds(e.text.timestamp)));
                return true;
            case SDL_EventType.SDL_EVENT_TEXT_EDITING:
                Emit(InputEvent.Composition(Keyboard, e.edit.GetText() ?? "", _platform.Seconds(e.edit.timestamp)));
                return true;
            case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                if (e.motion.which == SDL_TOUCH_MOUSEID) return true;
                _mouseInside = true;
                Emit(InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, _platform.ToCanvas(e.motion.x, e.motion.y), _platform.Seconds(e.motion.timestamp))
                    with { ScreenPosition = _platform.ToPixels(e.motion.x, e.motion.y) });
                return true;
            case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
            case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
            {
                if (e.button.which == SDL_TOUCH_MOUSEID) return true;
                var button = e.button.button switch
                {
                    1 => PointerButton.Primary, 2 => PointerButton.Middle, 3 => PointerButton.Secondary,
                    4 => PointerButton.Back, 5 => PointerButton.Forward, _ => PointerButton.None,
                };
                if (button == PointerButton.None) return true;
                var at = _platform.ToCanvas(e.button.x, e.button.y);
                double t = _platform.Seconds(e.button.timestamp);
                var ev = e.button.down ? InputEvent.PointerDown(Mouse, 0, PointerKind.Mouse, at, button, t) : InputEvent.PointerUp(Mouse, 0, PointerKind.Mouse, at, button, t);
                Emit(ev with { ScreenPosition = _platform.ToPixels(e.button.x, e.button.y) });
                return true;
            }
            case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
            {
                if (e.wheel.which == SDL_TOUCH_MOUSEID) return true;
                var turn = new Vector2(e.wheel.x, e.wheel.y);
                if (e.wheel.direction == SDL_MouseWheelDirection.SDL_MOUSEWHEEL_FLIPPED) turn = -turn;
                Emit(InputEvent.WheelMoved(Mouse, turn, _platform.ToCanvas(e.wheel.mouse_x, e.wheel.mouse_y), _platform.Seconds(e.wheel.timestamp)));
                return true;
            }
            case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
                if (_mouseInside) Emit(new InputEvent { Kind = InputEventKind.PointerLeave, Device = Mouse, PointerKind = PointerKind.Mouse, Time = _platform.Seconds(e.window.timestamp) });
                _mouseInside = false;
                return true;
            case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
                PadAdded(e.gdevice.which, _platform.Seconds(e.gdevice.timestamp));
                return true;
            case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:
                PadRemoved(e.gdevice.which, _platform.Seconds(e.gdevice.timestamp));
                return true;
            case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
            case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP:
            {
                if (!_pads.ContainsKey(e.gbutton.which)) return true;
                var button = Button(e.gbutton.Button);
                if (button == PadButton.None) return true;
                double t = _platform.Seconds(e.gbutton.timestamp);
                var id = PadId(e.gbutton.which);
                Emit(e.gbutton.down ? InputEvent.ButtonDown(id, button, t) : InputEvent.ButtonUp(id, button, t));
                return true;
            }
            case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION:
            {
                if (!_pads.ContainsKey(e.gaxis.which)) return true;
                var (axis, known) = Axis(e.gaxis.Axis);
                if (!known) return true;
                float value = Math.Clamp(e.gaxis.value / 32767f, -1f, 1f);
                if (axis is PadAxis.LeftTrigger or PadAxis.RightTrigger) value = Math.Max(0, value);
                Emit(InputEvent.AxisMoved(PadId(e.gaxis.which), axis, value, _platform.Seconds(e.gaxis.timestamp)));
                return true;
            }
            default:
                return false;
        }
    }

    private void PadAdded(SDL_JoystickID id, double time)
    {
        if (_pads.ContainsKey(id)) return;
        var pad = SDL_OpenGamepad(id);
        if (pad == null) return;
        _pads[id] = (nint)pad;
        bool rumble;
        fixed (byte* name = SDL_PROP_GAMEPAD_CAP_RUMBLE_BOOLEAN) rumble = SDL_GetBooleanProperty(SDL_GetGamepadProperties(pad), name, false);
        Emit(InputEvent.Added(new InputDeviceInfo(PadId(id), DeviceKind.Gamepad, SDL_GetGamepadName(pad) ?? "gamepad")
        {
            Style = Style(SDL_GetGamepadType(pad)),
            PlayerIndex = SDL_GetGamepadPlayerIndex(pad),
            CanRumble = rumble,
        }, time));
    }

    private void PadRemoved(SDL_JoystickID id, double time)
    {
        if (!_pads.Remove(id, out var pad)) return;
        var device = PadId(id);
        // What the pad held is released before it goes (the contract of IInputBackend).
        foreach (var release in State.HeldReleases(device, time).ToList()) Emit(release);
        Emit(InputEvent.Removed(device, time));
        SDL_CloseGamepad((SDL_Gamepad*)pad);
    }

    /// <summary>Releases every key, mouse button and pad button held, as the window loses the focus.</summary>
    public void ReleaseAll() => ReleaseHeld(_platform.Now);

    public static GamepadStyle Style(SDL_GamepadType type) => type switch
    {
        SDL_GamepadType.SDL_GAMEPAD_TYPE_PS3 or SDL_GamepadType.SDL_GAMEPAD_TYPE_PS4 or SDL_GamepadType.SDL_GAMEPAD_TYPE_PS5 => GamepadStyle.PlayStation,
        SDL_GamepadType.SDL_GAMEPAD_TYPE_NINTENDO_SWITCH_PRO or SDL_GamepadType.SDL_GAMEPAD_TYPE_NINTENDO_SWITCH_JOYCON_LEFT
            or SDL_GamepadType.SDL_GAMEPAD_TYPE_NINTENDO_SWITCH_JOYCON_RIGHT or SDL_GamepadType.SDL_GAMEPAD_TYPE_NINTENDO_SWITCH_JOYCON_PAIR => GamepadStyle.Nintendo,
        _ => GamepadStyle.Xbox,
    };

    public static PadButton Button(SDL_GamepadButton button) => button switch
    {
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH => PadButton.A,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST => PadButton.B,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_WEST => PadButton.X,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_NORTH => PadButton.Y,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_START => PadButton.Start,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK => PadButton.Back,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_GUIDE => PadButton.Guide,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_SHOULDER => PadButton.LeftShoulder,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER => PadButton.RightShoulder,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP => PadButton.DPadUp,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN => PadButton.DPadDown,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_LEFT => PadButton.DPadLeft,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_RIGHT => PadButton.DPadRight,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_STICK => PadButton.LeftStick,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_STICK => PadButton.RightStick,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_MISC1 => PadButton.Misc1,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_PADDLE1 => PadButton.RightPaddle1,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_PADDLE1 => PadButton.LeftPaddle1,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_PADDLE2 => PadButton.RightPaddle2,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_PADDLE2 => PadButton.LeftPaddle2,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_TOUCHPAD => PadButton.Touchpad,
        _ => PadButton.None,
    };

    private static (PadAxis Axis, bool Known) Axis(SDL_GamepadAxis axis) => axis switch
    {
        SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTX => (PadAxis.LeftX, true),
        SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTY => (PadAxis.LeftY, true),
        SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTX => (PadAxis.RightX, true),
        SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTY => (PadAxis.RightY, true),
        SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFT_TRIGGER => (PadAxis.LeftTrigger, true),
        SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHT_TRIGGER => (PadAxis.RightTrigger, true),
        _ => (default, false),
    };

    /// <summary>
    /// The label the current layout prints on <paramref name="key"/> when it is a character key ("Q" for the key in A's
    /// place on AZERTY); null for keys whose name does not depend on the layout (Space, arrows, F1...).
    /// </summary>
    public string? KeyLabel(Key key)
    {
        int usage = KeyUsages.Usage(key);
        if (usage == 0) return null;
        var code = SDL_GetKeyFromScancode((SDL_Scancode)usage, SDL_Keymod.SDL_KMOD_NONE, false);
        // Character keys have their character as keycode; the others have the scancode mask. Space, Enter, Escape, Tab,
        // Backspace and Delete are characters too, but control ones: the engine names them.
        if ((uint)code <= 0x20 || (uint)code == 0x7F || ((uint)code & 0x40000000u) != 0) return null;
        var name = SDL_GetKeyName(code);
        return string.IsNullOrEmpty(name) ? null : name;
    }

    /// <inheritdoc/>
    public bool Rumble(DeviceId pad, float low, float high, double seconds)
    {
        foreach (var (id, handle) in _pads)
            if (PadId(id) == pad)
                return SDL_RumbleGamepad((SDL_Gamepad*)handle, (ushort)(Math.Clamp(low, 0, 1) * 65535), (ushort)(Math.Clamp(high, 0, 1) * 65535),
                    (uint)Math.Clamp(seconds * 1000, 0, uint.MaxValue));
        return false;
    }

    /// <inheritdoc/>
    public void Start(MicroEngine.RectF caret)
    {
        var area = _platform.WindowRect(caret);
        SDL_SetTextInputArea(_platform.Window, &area, 0);
        SDL_StartTextInput(_platform.Window);
    }

    /// <inheritdoc/>
    public void Stop() => SDL_StopTextInput(_platform.Window);

    public void Dispose()
    {
        foreach (var pad in _pads.Values) SDL_CloseGamepad((SDL_Gamepad*)pad);
        _pads.Clear();
    }
}

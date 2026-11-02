using System.Numerics;
using MicroEngine.Input;
using SDL;
using static SDL.SDL3;

namespace MicroEngine.Sdl;

/// <summary>Keyboard, mouse and one gamepad (the first connected; the next one takes over when it is unplugged).</summary>
internal sealed unsafe class SdlInput(SdlPlatform platform) : IInputSource, IDisposable
{
    private static readonly Dictionary<Key, SDL_Scancode> Keys = BuildKeys();
    private readonly Dictionary<SDL_Scancode, int> _taps = [];
    private readonly List<SDL_Scancode> _keys = [];
    private readonly HashSet<SDL_Scancode> _sampled = [];
    private SDL_Gamepad* _pad;
    private SDL_JoystickID _padId;

    /// <summary>
    /// Like the raylib input: a key pressed and released between two frames still counts as down until a simulation
    /// step has sampled it, so a quick tap during a long frame is not lost; unread taps expire after a few frames.
    /// </summary>
    public void BeginFrame()
    {
        foreach (var key in _sampled) _taps.Remove(key);
        _sampled.Clear();
        _keys.Clear();
        _keys.AddRange(_taps.Keys); // reused every frame instead of a new list
        foreach (var key in _keys)
            if (--_taps[key] <= 0) _taps.Remove(key);
    }

    public void KeyDown(SDL_Scancode code) => _taps[code] = 8;

    public void ReleaseAll() => _taps.Clear();

    public void GamepadAdded(SDL_JoystickID id)
    {
        if (_pad != null) return;
        _pad = SDL_OpenGamepad(id);
        _padId = id;
    }

    public void GamepadRemoved(SDL_JoystickID id)
    {
        if (_pad == null || id != _padId) return;
        SDL_CloseGamepad(_pad);
        _pad = null;
        // Another connected pad takes over, so unplugging one of two controllers does not leave the game without one.
        using var pads = SDL_GetGamepads();
        if (pads is null) return;
        for (int i = 0; i < pads.Count; i++)
            if (pads[i] != id) { GamepadAdded(pads[i]); break; }
    }

    private static Dictionary<Key, SDL_Scancode> BuildKeys()
    {
        var map = new Dictionary<Key, SDL_Scancode>
        {
            [Key.Space] = SDL_Scancode.SDL_SCANCODE_SPACE, [Key.Enter] = SDL_Scancode.SDL_SCANCODE_RETURN,
            [Key.Escape] = SDL_Scancode.SDL_SCANCODE_ESCAPE, [Key.Backspace] = SDL_Scancode.SDL_SCANCODE_BACKSPACE,
            [Key.Tab] = SDL_Scancode.SDL_SCANCODE_TAB, [Key.LeftShift] = SDL_Scancode.SDL_SCANCODE_LSHIFT,
            [Key.RightShift] = SDL_Scancode.SDL_SCANCODE_RSHIFT, [Key.LeftControl] = SDL_Scancode.SDL_SCANCODE_LCTRL,
            [Key.Left] = SDL_Scancode.SDL_SCANCODE_LEFT, [Key.Right] = SDL_Scancode.SDL_SCANCODE_RIGHT,
            [Key.Up] = SDL_Scancode.SDL_SCANCODE_UP, [Key.Down] = SDL_Scancode.SDL_SCANCODE_DOWN,
            [Key.F1] = SDL_Scancode.SDL_SCANCODE_F1, [Key.F2] = SDL_Scancode.SDL_SCANCODE_F2, [Key.F3] = SDL_Scancode.SDL_SCANCODE_F3,
            [Key.F4] = SDL_Scancode.SDL_SCANCODE_F4, [Key.F5] = SDL_Scancode.SDL_SCANCODE_F5,
            [Key.F11] = SDL_Scancode.SDL_SCANCODE_F11, [Key.F12] = SDL_Scancode.SDL_SCANCODE_F12,
            [Key.D0] = SDL_Scancode.SDL_SCANCODE_0,
        };
        for (int i = 0; i < 26; i++) map[Key.A + i] = SDL_Scancode.SDL_SCANCODE_A + i;
        for (int i = 1; i < 10; i++) map[Key.D0 + i] = SDL_Scancode.SDL_SCANCODE_1 + (i - 1);
        return map;
    }

    public bool IsKeyDown(Key key)
    {
        if (!Keys.TryGetValue(key, out var code)) return false;
        if (_taps.ContainsKey(code))
        {
            _sampled.Add(code);
            return true;
        }
        int count;
        var state = SDL_GetKeyboardState(&count);
        return (int)code < count && state[(int)code];
    }

    public bool IsPadDown(PadButton button)
    {
        if (_pad == null) return false;
        var b = button switch
        {
            PadButton.A => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH,
            PadButton.B => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST,
            PadButton.X => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_WEST,
            PadButton.Y => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_NORTH,
            PadButton.Start => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_START,
            PadButton.Back => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK,
            PadButton.LeftShoulder => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_SHOULDER,
            PadButton.RightShoulder => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER,
            PadButton.DPadUp => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP,
            PadButton.DPadDown => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN,
            PadButton.DPadLeft => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_LEFT,
            PadButton.DPadRight => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_RIGHT,
            PadButton.LeftStick => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_STICK,
            PadButton.RightStick => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_STICK,
            _ => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_INVALID,
        };
        return b != SDL_GamepadButton.SDL_GAMEPAD_BUTTON_INVALID && SDL_GetGamepadButton(_pad, b);
    }

    public float GetPadAxis(PadAxis axis)
    {
        if (_pad == null) return 0;
        var a = axis switch
        {
            PadAxis.LeftX => SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTX,
            PadAxis.LeftY => SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTY,
            PadAxis.RightX => SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTX,
            PadAxis.RightY => SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTY,
            PadAxis.LeftTrigger => SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFT_TRIGGER,
            _ => SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHT_TRIGGER,
        };
        return Math.Clamp(SDL_GetGamepadAxis(_pad, a) / 32767f, -1f, 1f);
    }

    public Vector2 LeftStick => new(GetPadAxis(PadAxis.LeftX), GetPadAxis(PadAxis.LeftY));

    public bool PadConnected => _pad != null;

    public GamepadStyle PadStyle => _pad == null ? GamepadStyle.Xbox : SDL_GetGamepadType(_pad) switch
    {
        SDL_GamepadType.SDL_GAMEPAD_TYPE_PS3 or SDL_GamepadType.SDL_GAMEPAD_TYPE_PS4 or SDL_GamepadType.SDL_GAMEPAD_TYPE_PS5 => GamepadStyle.PlayStation,
        SDL_GamepadType.SDL_GAMEPAD_TYPE_NINTENDO_SWITCH_PRO or SDL_GamepadType.SDL_GAMEPAD_TYPE_NINTENDO_SWITCH_JOYCON_LEFT
            or SDL_GamepadType.SDL_GAMEPAD_TYPE_NINTENDO_SWITCH_JOYCON_RIGHT or SDL_GamepadType.SDL_GAMEPAD_TYPE_NINTENDO_SWITCH_JOYCON_PAIR => GamepadStyle.Nintendo,
        _ => GamepadStyle.Xbox,
    };

    public PointerState Pointer
    {
        get
        {
            float x, y;
            var buttons = SDL_GetMouseState(&x, &y);
            bool present = SDL_GetMouseFocus() == platform.Window;
            return new PointerState(present, platform.ToCanvas(x, y),
                (buttons & SDL_MouseButtonFlags.SDL_BUTTON_LMASK) != 0,
                (buttons & SDL_MouseButtonFlags.SDL_BUTTON_RMASK) != 0,
                (buttons & SDL_MouseButtonFlags.SDL_BUTTON_MMASK) != 0);
        }
    }

    public void Dispose()
    {
        if (_pad != null) SDL_CloseGamepad(_pad);
        _pad = null;
    }
}

using System.Numerics;

namespace MicroEngine.Input;

/// <summary>
/// Physical keys, named by their position on a US keyboard (a scancode, not a character): <see cref="A"/> is the key
/// left of <see cref="S"/> whatever the layout prints on it, so WASD movement stays in place on AZERTY or Dvorak. Ask
/// <see cref="IKeyLabels"/> (or <c>ActionPrompts</c>) for the label the player's layout shows. Backends map their own
/// codes onto these; the set covers a full-size keyboard.
/// </summary>
public enum Key
{
    None, Space, Enter, Escape, Backspace, Tab, LeftShift, RightShift, LeftControl,
    Left, Right, Up, Down,
    A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
    F1, F2, F3, F4, F5, F11, F12,
    // Added after the first set, so the numbers of the keys above never change.
    F6, F7, F8, F9, F10, F13, F14, F15, F16, F17, F18, F19, F20, F21, F22, F23, F24,
    /// <summary>The key right of 0 (- on US layouts).</summary>
    Minus,
    /// <summary>The key left of Backspace (= on US layouts).</summary>
    Equals,
    LeftBracket, RightBracket, Backslash, Semicolon, Apostrophe,
    /// <summary>The key left of 1 (` on US layouts).</summary>
    Grave,
    Comma, Period, Slash,
    /// <summary>The extra key next to the left Shift of ISO keyboards (&lt; &gt; on many European layouts).</summary>
    NonUsBackslash,
    CapsLock, LeftAlt, RightAlt, RightControl,
    /// <summary>The left Windows, Command or Super key.</summary>
    LeftSuper,
    /// <summary>The right Windows, Command or Super key.</summary>
    RightSuper,
    /// <summary>The context-menu key.</summary>
    Menu,
    Insert, Delete, Home, End, PageUp, PageDown, PrintScreen, ScrollLock, Pause, NumLock,
    Keypad0, Keypad1, Keypad2, Keypad3, Keypad4, Keypad5, Keypad6, Keypad7, Keypad8, Keypad9,
    KeypadDivide, KeypadMultiply, KeypadMinus, KeypadPlus, KeypadEnter, KeypadPeriod, KeypadEquals,
}

/// <summary>
/// Gamepad buttons by position, using Xbox names (<see cref="A"/> is the bottom face button on every pad); backends map
/// their own codes. <see cref="GamepadStyle"/> says what the pad prints there.
/// </summary>
public enum PadButton
{
    None, A, B, X, Y, Start, Back, LeftShoulder, RightShoulder, DPadUp, DPadDown, DPadLeft, DPadRight, LeftStick, RightStick,
    // Added after the first set, so the numbers of the buttons above never change.
    /// <summary>The system button in the middle (Xbox, PS, Home).</summary>
    Guide,
    /// <summary>An extra button some pads have (Xbox Series share, Switch Pro capture, PS5 microphone).</summary>
    Misc1,
    /// <summary>The upper back paddle on the right (Xbox Elite P1).</summary>
    RightPaddle1,
    /// <summary>The upper back paddle on the left (Xbox Elite P3).</summary>
    LeftPaddle1,
    /// <summary>The lower back paddle on the right (Xbox Elite P2).</summary>
    RightPaddle2,
    /// <summary>The lower back paddle on the left (Xbox Elite P4).</summary>
    LeftPaddle2,
    /// <summary>A click of the touchpad (PlayStation pads).</summary>
    Touchpad,
}

/// <summary>Analog gamepad axes. Sticks are -1..1 (right and down positive), triggers 0..1.</summary>
public enum PadAxis { LeftX, LeftY, RightX, RightY, LeftTrigger, RightTrigger }

/// <summary>The kind of device that last pressed an action (see <see cref="InputMap.LastDevice"/>).</summary>
/// <remarks><c>Other</c> covers scripted input and custom <see cref="IActionSource"/>s.</remarks>
public enum InputDevice { None, Keyboard, Gamepad, Touch, Other }

/// <summary>
/// The button names of a gamepad family, for prompts: <see cref="PadButton"/> uses Xbox names, which PlayStation pads
/// show as shapes and Nintendo pads with A/B and X/Y swapped.
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
public readonly record struct PointerState(bool Present, Vector2 Position, bool Primary, bool Secondary, bool Middle)
{
    /// <summary>
    /// How far the wheel turned since the previous update: Y positive away from the player (scroll up), X positive to
    /// the right, in notches (fractions for smooth wheels and touchpads).
    /// </summary>
    public Vector2 Wheel { get; init; }
}

/// <summary>The raw device state a backend reports once per polled frame.</summary>
/// <remarks>
/// Engine backends deliver events instead (<see cref="IInputBackend"/>); a polled source is still read every frame and
/// its changes become the same events, so a host's extra source or a test's hand-set input is one more device.
/// </remarks>
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
    /// <summary>Every pointer: the mouse while over the window, each finger or pen on the screen. Sources without pointers keep the default (none).</summary>
    IReadOnlyList<PointerInfo> Pointers => Array.Empty<PointerInfo>();
}

/// <summary>
/// A source of actions rather than of keys: on-screen touch controls, a network peer, an accessibility switch or an AI.
/// Added with <see cref="InputMap.AddSource"/>, it is read every step together with the keyboard and gamepad, so a game
/// cannot tell which produced an action.
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

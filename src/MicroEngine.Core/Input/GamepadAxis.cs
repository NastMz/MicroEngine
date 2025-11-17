namespace MicroEngine.Core.Input;

/// <summary>
/// Gamepad axis identifiers.
/// </summary>
public enum GamepadAxis
{
    /// <summary>
    /// Left stick horizontal axis (-1 = left, +1 = right).
    /// </summary>
    LeftX = 0,

    /// <summary>
    /// Left stick vertical axis (-1 = up, +1 = down).
    /// </summary>
    LeftY = 1,

    /// <summary>
    /// Right stick horizontal axis (-1 = left, +1 = right).
    /// </summary>
    RightX = 2,

    /// <summary>
    /// Right stick vertical axis (-1 = up, +1 = down).
    /// </summary>
    RightY = 3,

    /// <summary>
    /// Left trigger (0 = not pressed, +1 = fully pressed).
    /// </summary>
    LeftTrigger = 4,

    /// <summary>
    /// Right trigger (0 = not pressed, +1 = fully pressed).
    /// </summary>
    RightTrigger = 5
}

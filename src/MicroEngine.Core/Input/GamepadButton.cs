namespace MicroEngine.Core.Input;

/// <summary>
/// Gamepad button identifiers.
/// Based on standard gamepad layout (Xbox/PlayStation compatible).
/// </summary>
public enum GamepadButton
{
    /// <summary>A button (Xbox) / Cross (PlayStation).</summary>
    A = 0,
    /// <summary>B button (Xbox) / Circle (PlayStation).</summary>
    B = 1,
    /// <summary>X button (Xbox) / Square (PlayStation).</summary>
    X = 2,
    /// <summary>Y button (Xbox) / Triangle (PlayStation).</summary>
    Y = 3,

    /// <summary>Left bumper (LB).</summary>
    LeftBumper = 4,
    /// <summary>Right bumper (RB).</summary>
    RightBumper = 5,

    /// <summary>Back/Select button.</summary>
    Back = 6,
    /// <summary>Start button.</summary>
    Start = 7,
    /// <summary>Guide/Home button.</summary>
    Guide = 8,

    /// <summary>Left thumbstick click (L3).</summary>
    LeftThumb = 9,
    /// <summary>Right thumbstick click (R3).</summary>
    RightThumb = 10,

    /// <summary>D-Pad up.</summary>
    DPadUp = 11,
    /// <summary>D-Pad right.</summary>
    DPadRight = 12,
    /// <summary>D-Pad down.</summary>
    DPadDown = 13,
    /// <summary>D-Pad left.</summary>
    DPadLeft = 14
}

namespace MicroEngine.Core.Input;

/// <summary>
/// Gamepad button identifiers following standard gamepad layout.
/// Values map to backend-specific gamepad button codes.
/// </summary>
public enum GamepadButton
{
    /// <summary>Unknown button (error checking).</summary>
    Unknown = 0,

    /// <summary>D-Pad up.</summary>
    DPadUp = 1,
    /// <summary>D-Pad right.</summary>
    DPadRight = 2,
    /// <summary>D-Pad down.</summary>
    DPadDown = 3,
    /// <summary>D-Pad left.</summary>
    DPadLeft = 4,

    /// <summary>Y button (Xbox) / Triangle (PlayStation).</summary>
    Y = 5,
    /// <summary>B button (Xbox) / Circle (PlayStation).</summary>
    B = 6,
    /// <summary>A button (Xbox) / Cross (PlayStation).</summary>
    A = 7,
    /// <summary>X button (Xbox) / Square (PlayStation).</summary>
    X = 8,

    /// <summary>Left bumper (LB/L1).</summary>
    LeftBumper = 9,
    /// <summary>Left trigger (LT/L2).</summary>
    LeftTrigger = 10,
    /// <summary>Right bumper (RB/R1).</summary>
    RightBumper = 11,
    /// <summary>Right trigger (RT/R2).</summary>
    RightTrigger = 12,

    /// <summary>Back/Select button.</summary>
    Back = 13,
    /// <summary>Guide/Home button.</summary>
    Guide = 14,
    /// <summary>Start button.</summary>
    Start = 15,

    /// <summary>Left thumbstick click (L3).</summary>
    LeftThumb = 16,
    /// <summary>Right thumbstick click (R3).</summary>
    RightThumb = 17
}

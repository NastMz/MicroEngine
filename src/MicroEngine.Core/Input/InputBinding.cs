namespace MicroEngine.Core.Input;

/// <summary>
/// Represents a specific input binding (key, mouse button, or gamepad button).
/// </summary>
public readonly struct InputBinding : IEquatable<InputBinding>
{
    /// <summary>
    /// Gets the type of input binding.
    /// </summary>
    public InputBindingType Type { get; init; }

    /// <summary>
    /// Gets the input code (KeyCode, MouseButton, or GamepadButton cast to int).
    /// </summary>
    public int Code { get; init; }

    /// <summary>
    /// Gets the gamepad index (0-3) for gamepad bindings, -1 for other types.
    /// </summary>
    public int GamepadIndex { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InputBinding"/> struct.
    /// </summary>
    /// <param name="type">The type of input binding.</param>
    /// <param name="code">The input code.</param>
    /// <param name="gamepadIndex">The gamepad index (default -1 for non-gamepad).</param>
    public InputBinding(InputBindingType type, int code, int gamepadIndex = -1)
    {
        Type = type;
        Code = code;
        GamepadIndex = gamepadIndex;
    }

    /// <inheritdoc/>
    public bool Equals(InputBinding other)
    {
        return Type == other.Type &&
               Code == other.Code &&
               GamepadIndex == other.GamepadIndex;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is InputBinding other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Code, GamepadIndex);
    }

    /// <summary>
    /// Checks equality between two input bindings.
    /// </summary>
    public static bool operator ==(InputBinding left, InputBinding right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Checks inequality between two input bindings.
    /// </summary>
    public static bool operator !=(InputBinding left, InputBinding right)
    {
        return !left.Equals(right);
    }

    ///  <inheritdoc/>
    public override string ToString()
    {
        return Type switch
        {
            InputBindingType.Keyboard => $"Key: {(Key)Code}",
            InputBindingType.Mouse => $"Mouse: {(MouseButton)Code}",
            InputBindingType.Gamepad => $"Gamepad{GamepadIndex}: {(GamepadButton)Code}",
            _ => "Unknown"
        };
    }
}

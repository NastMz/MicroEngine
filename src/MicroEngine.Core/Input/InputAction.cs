namespace MicroEngine.Core.Input;

/// <summary>
/// Represents a logical game action that can be triggered by multiple input bindings.
/// Examples: "Jump", "Shoot", "MoveLeft", "Pause".
/// </summary>
public sealed class InputAction
{
    private readonly List<InputBinding> _bindings;

    /// <summary>
    /// Gets the unique name of this action.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the collection of input bindings for this action.
    /// </summary>
    public IReadOnlyList<InputBinding> Bindings => _bindings;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputAction"/> class.
    /// </summary>
    /// <param name="name">The unique name of this action.</param>
    public InputAction(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Action name cannot be null or whitespace.", nameof(name));
        }

        Name = name;
        _bindings = new List<InputBinding>();
    }

    /// <summary>
    /// Adds a keyboard binding to this action.
    /// </summary>
    /// <param name="key">The key to bind.</param>
    /// <returns>This action for chaining.</returns>
    public InputAction AddKeyboardBinding(Key key)
    {
        _bindings.Add(new InputBinding(InputBindingType.Keyboard, (int)key));
        return this;
    }

    /// <summary>
    /// Adds a mouse button binding to this action.
    /// </summary>
    /// <param name="button">The mouse button to bind.</param>
    /// <returns>This action for chaining.</returns>
    public InputAction AddMouseBinding(MouseButton button)
    {
        _bindings.Add(new InputBinding(InputBindingType.Mouse, (int)button));
        return this;
    }

    /// <summary>
    /// Adds a gamepad button binding to this action.
    /// </summary>
    /// <param name="button">The gamepad button to bind.</param>
    /// <param name="gamepadIndex">The gamepad index (0-3).</param>
    /// <returns>This action for chaining.</returns>
    public InputAction AddGamepadBinding(GamepadButton button, int gamepadIndex = 0)
    {
        _bindings.Add(new InputBinding(InputBindingType.Gamepad, (int)button, gamepadIndex));
        return this;
    }

    /// <summary>
    /// Removes all bindings from this action.
    /// </summary>
    public void ClearBindings()
    {
        _bindings.Clear();
    }

    /// <summary>
    /// Removes a specific keyboard binding.
    /// </summary>
    /// <param name="key">The key binding to remove.</param>
    /// <returns>True if the binding was removed.</returns>
    public bool RemoveKeyboardBinding(Key key)
    {
        return _bindings.RemoveAll(b =>
            b.Type == InputBindingType.Keyboard && b.Code == (int)key) > 0;
    }

    /// <summary>
    /// Removes a specific mouse binding.
    /// </summary>
    /// <param name="button">The mouse button binding to remove.</param>
    /// <returns>True if the binding was removed.</returns>
    public bool RemoveMouseBinding(MouseButton button)
    {
        return _bindings.RemoveAll(b =>
            b.Type == InputBindingType.Mouse && b.Code == (int)button) > 0;
    }

    /// <summary>
    /// Removes a specific gamepad binding.
    /// </summary>
    /// <param name="button">The gamepad button binding to remove.</param>
    /// <param name="gamepadIndex">The gamepad index.</param>
    /// <returns>True if the binding was removed.</returns>
    public bool RemoveGamepadBinding(GamepadButton button, int gamepadIndex = 0)
    {
        return _bindings.RemoveAll(b =>
            b.Type == InputBindingType.Gamepad &&
            b.Code == (int)button &&
            b.GamepadIndex == gamepadIndex) > 0;
    }
}

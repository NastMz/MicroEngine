namespace MicroEngine.Core.Input;

/// <summary>
/// Interface for input mapping system that translates raw input into game actions.
/// Allows remapping controls and supporting multiple input devices.
/// </summary>
public interface IInputMap
{
    /// <summary>
    /// Gets all registered actions.
    /// </summary>
    IReadOnlyList<InputAction> Actions { get; }

    /// <summary>
    /// Adds a new action to the input map.
    /// </summary>
    /// <param name="action">The action to add.</param>
    void AddAction(InputAction action);

    /// <summary>
    /// Gets an action by name.
    /// </summary>
    /// <param name="actionName">The name of the action.</param>
    /// <returns>The action, or null if not found.</returns>
    InputAction? GetAction(string actionName);

    /// <summary>
    /// Removes an action from the input map.
    /// </summary>
    /// <param name="actionName">The name of the action to remove.</param>
    /// <returns>True if the action was removed.</returns>
    bool RemoveAction(string actionName);

    /// <summary>
    /// Checks if an action was just pressed this frame.
    /// </summary>
    /// <param name="actionName">The name of the action.</param>
    /// <returns>True if the action was just pressed.</returns>
    bool IsActionPressed(string actionName);

    /// <summary>
    /// Checks if an action was just released this frame.
    /// </summary>
    /// <param name="actionName">The name of the action.</param>
    /// <returns>True if the action was just released.</returns>
    bool IsActionReleased(string actionName);

    /// <summary>
    /// Checks if an action is currently held down.
    /// </summary>
    /// <param name="actionName">The name of the action.</param>
    /// <returns>True if the action is held.</returns>
    bool IsActionHeld(string actionName);

    /// <summary>
    /// Gets the analog value for an action (0-1 for triggers, -1 to 1 for axes).
    /// </summary>
    /// <param name="actionName">The name of the action.</param>
    /// <returns>The analog value, or 0 if digital or not active.</returns>
    float GetActionValue(string actionName);

    /// <summary>
    /// Updates the input map state. Should be called once per frame.
    /// </summary>
    /// <param name="inputBackend">The input backend to query.</param>
    void Update(IInputBackend inputBackend);

    /// <summary>
    /// Clears all actions from the input map.
    /// </summary>
    void Clear();
}

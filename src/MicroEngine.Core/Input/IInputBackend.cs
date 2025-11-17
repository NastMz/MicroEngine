using MicroEngine.Core.Math;

namespace MicroEngine.Core.Input;

/// <summary>
/// Backend-agnostic input interface.
/// Provides unified access to keyboard, mouse, and gamepad input.
/// </summary>
public interface IInputBackend
{
    /// <summary>
    /// Updates the input state for the current frame.
    /// Must be called once per frame before querying input.
    /// </summary>
    void Update();

    #region Keyboard

    /// <summary>
    /// Checks if a key is currently being held down.
    /// </summary>
    bool IsKeyDown(Key key);

    /// <summary>
    /// Checks if a key was just pressed this frame.
    /// </summary>
    bool IsKeyPressed(Key key);

    /// <summary>
    /// Checks if a key was just released this frame.
    /// </summary>
    bool IsKeyReleased(Key key);

    #endregion

    #region Mouse

    /// <summary>
    /// Gets the current mouse position in screen coordinates.
    /// </summary>
    Vector2 GetMousePosition();

    /// <summary>
    /// Gets the mouse movement delta since last frame.
    /// </summary>
    Vector2 GetMouseDelta();

    /// <summary>
    /// Checks if a mouse button is currently being held down.
    /// </summary>
    bool IsMouseButtonDown(MouseButton button);

    /// <summary>
    /// Checks if a mouse button was just pressed this frame.
    /// </summary>
    bool IsMouseButtonPressed(MouseButton button);

    /// <summary>
    /// Checks if a mouse button was just released this frame.
    /// </summary>
    bool IsMouseButtonReleased(MouseButton button);

    /// <summary>
    /// Gets the mouse wheel scroll delta.
    /// Positive = scroll up, Negative = scroll down.
    /// </summary>
    float GetMouseWheelDelta();

    #endregion

    #region Gamepad

    /// <summary>
    /// Checks if a gamepad is currently connected.
    /// </summary>
    /// <param name="gamepad">Gamepad index (0-3).</param>
    bool IsGamepadAvailable(int gamepad);

    /// <summary>
    /// Checks if a gamepad button is currently being held down.
    /// </summary>
    bool IsGamepadButtonDown(int gamepad, GamepadButton button);

    /// <summary>
    /// Checks if a gamepad button was just pressed this frame.
    /// </summary>
    bool IsGamepadButtonPressed(int gamepad, GamepadButton button);

    /// <summary>
    /// Checks if a gamepad button was just released this frame.
    /// </summary>
    bool IsGamepadButtonReleased(int gamepad, GamepadButton button);

    /// <summary>
    /// Gets the current value of a gamepad axis.
    /// Returns value in range [-1, 1] for sticks, [0, 1] for triggers.
    /// </summary>
    float GetGamepadAxis(int gamepad, GamepadAxis axis);

    #endregion
}

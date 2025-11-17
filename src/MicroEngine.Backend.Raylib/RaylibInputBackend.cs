using MicroEngine.Core.Input;
using MicroEngine.Core.Math;
using Raylib_cs;
using RlMouseButton = Raylib_cs.MouseButton;
using RlGamepadButton = Raylib_cs.GamepadButton;
using RlGamepadAxis = Raylib_cs.GamepadAxis;

namespace MicroEngine.Backend.Raylib;

/// <summary>
/// Raylib implementation of the input backend.
/// Provides keyboard, mouse, and gamepad input using Raylib-cs.
/// </summary>
public class RaylibInputBackend : IInputBackend
{
    /// <inheritdoc/>
    public void Update()
    {
        // Raylib processes input internally via PollInputEvents
        // This is called automatically by BeginDrawing
    }

    #region Keyboard

    /// <inheritdoc/>
    public bool IsKeyDown(Key key)
    {
        return Raylib_cs.Raylib.IsKeyDown((KeyboardKey)(int)key);
    }

    /// <inheritdoc/>
    public bool IsKeyPressed(Key key)
    {
        return Raylib_cs.Raylib.IsKeyPressed((KeyboardKey)(int)key);
    }

    /// <inheritdoc/>
    public bool IsKeyReleased(Key key)
    {
        return Raylib_cs.Raylib.IsKeyReleased((KeyboardKey)(int)key);
    }

    #endregion

    #region Mouse

    /// <inheritdoc/>
    public Vector2 GetMousePosition()
    {
        var pos = Raylib_cs.Raylib.GetMousePosition();
        return new Vector2(pos.X, pos.Y);
    }

    /// <inheritdoc/>
    public Vector2 GetMouseDelta()
    {
        var delta = Raylib_cs.Raylib.GetMouseDelta();
        return new Vector2(delta.X, delta.Y);
    }

    /// <inheritdoc/>
    public bool IsMouseButtonDown(Core.Input.MouseButton button)
    {
        return Raylib_cs.Raylib.IsMouseButtonDown((RlMouseButton)(int)button);
    }

    /// <inheritdoc/>
    public bool IsMouseButtonPressed(Core.Input.MouseButton button)
    {
        return Raylib_cs.Raylib.IsMouseButtonPressed((RlMouseButton)(int)button);
    }

    /// <inheritdoc/>
    public bool IsMouseButtonReleased(Core.Input.MouseButton button)
    {
        return Raylib_cs.Raylib.IsMouseButtonReleased((RlMouseButton)(int)button);
    }

    /// <inheritdoc/>
    public float GetMouseWheelDelta()
    {
        return Raylib_cs.Raylib.GetMouseWheelMove();
    }

    #endregion

    #region Gamepad

    /// <inheritdoc/>
    public bool IsGamepadAvailable(int gamepad)
    {
        return Raylib_cs.Raylib.IsGamepadAvailable(gamepad);
    }

    /// <inheritdoc/>
    public bool IsGamepadButtonDown(int gamepad, Core.Input.GamepadButton button)
    {
        return Raylib_cs.Raylib.IsGamepadButtonDown(gamepad, (RlGamepadButton)(int)button);
    }

    /// <inheritdoc/>
    public bool IsGamepadButtonPressed(int gamepad, Core.Input.GamepadButton button)
    {
        return Raylib_cs.Raylib.IsGamepadButtonPressed(gamepad, (RlGamepadButton)(int)button);
    }

    /// <inheritdoc/>
    public bool IsGamepadButtonReleased(int gamepad, Core.Input.GamepadButton button)
    {
        return Raylib_cs.Raylib.IsGamepadButtonReleased(gamepad, (RlGamepadButton)(int)button);
    }

    /// <inheritdoc/>
    public float GetGamepadAxis(int gamepad, Core.Input.GamepadAxis axis)
    {
        return Raylib_cs.Raylib.GetGamepadAxisMovement(gamepad, (RlGamepadAxis)(int)axis);
    }

    #endregion
}

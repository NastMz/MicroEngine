# Input Backend Module

**Module:** Engine.Backend.Input  
**Status:** Active  
**Version:** 1.0  
**Last Updated:** November 2025

---

## Overview

The Input Backend module provides a unified, backend-agnostic interface for handling user input in MicroEngine.

It supports:

- **Keyboard input** (key presses, holds, releases)
- **Mouse input** (position, buttons, wheel)
- **Gamepad input** (buttons, axes, triggers)
- **Touch input** (future support for mobile)
- **Input mapping** and rebinding
- **Input buffering** for frame-perfect inputs
- **Multiple input backends** (Raylib, SDL, etc.)

The input system is designed to be:

- **Platform-independent:** Core logic never depends on specific input libraries
- **Flexible:** Easy to swap backends without changing game code
- **Responsive:** Low-latency input processing
- **Configurable:** Support for custom keybindings and input schemes

---

## Table of Contents

1. [Core Concepts](#core-concepts)
2. [Architecture](#architecture)
3. [IInputBackend Interface](#iinputbackend-interface)
4. [Keyboard Input](#keyboard-input)
5. [Mouse Input](#mouse-input)
6. [Gamepad Input](#gamepad-input)
7. [Input Actions and Mapping](#input-actions-and-mapping)
8. [Input Buffering](#input-buffering)
9. [Backend Implementations](#backend-implementations)
10. [Usage Examples](#usage-examples)
11. [Best Practices](#best-practices)
12. [API Reference](#api-reference)

---

## Core Concepts

### What is an Input Backend?

An input backend is a concrete implementation of the `IInputBackend` interface that translates platform-specific
input events into a unified API.

### Input State vs Input Events

**Input State:** Current state of input devices (key pressed, mouse position)

```csharp
bool isJumping = InputBackend.IsKeyDown(Key.Space);
```

**Input Events:** Discrete occurrences (key just pressed, mouse clicked)

```csharp
if (InputBackend.IsKeyPressed(Key.Space))
{
    player.Jump();
}
```

### Input Lifecycle

```
[Not Pressed] → Pressed → [Down] → Released → [Not Pressed]
                  ↓                     ↓
              OnPressed             OnReleased
```

---

## Architecture

### Class Diagram

```
IInputBackend (interface)
├── Keyboard methods
├── Mouse methods
├── Gamepad methods
└── Update cycle

Implementations:
├── RaylibInputBackend
├── SDLInputBackend
└── CustomInputBackend
```

### Core Interfaces

#### IInputBackend

Main input interface.

```csharp
public interface IInputBackend
{
    void Update();

    // Keyboard
    bool IsKeyDown(Key key);
    bool IsKeyPressed(Key key);
    bool IsKeyReleased(Key key);

    // Mouse
    Vector2 GetMousePosition();
    bool IsMouseButtonDown(MouseButton button);
    bool IsMouseButtonPressed(MouseButton button);
    bool IsMouseButtonReleased(MouseButton button);
    float GetMouseWheelDelta();

    // Gamepad
    bool IsGamepadAvailable(int gamepad);
    bool IsGamepadButtonDown(int gamepad, GamepadButton button);
    float GetGamepadAxis(int gamepad, GamepadAxis axis);
}
```

---

## IInputBackend Interface

### Update Cycle

The input backend must be updated every frame to process new input:

```csharp
public void GameLoop()
{
    while (running)
    {
        inputBackend.Update(); // Process input events

        // Game logic
        world.Update(deltaTime);

        // Rendering
        renderer.Render();
    }
}
```

### Input Polling vs Events

MicroEngine uses **polling** for simplicity:

```csharp
// Polling (MicroEngine approach)
if (inputBackend.IsKeyPressed(Key.Space))
{
    player.Jump();
}
```

Alternative event-based approach (not used):

```csharp
// Event-based (not used in MicroEngine)
inputBackend.OnKeyPressed += (key) =>
{
    if (key == Key.Space)
        player.Jump();
};
```

---

## Keyboard Input

### Key States

#### IsKeyDown

Check if a key is currently held down:

```csharp
if (inputBackend.IsKeyDown(Key.W))
{
    player.MoveForward();
}
```

Use for **continuous actions** (movement, aiming).

#### IsKeyPressed

Check if a key was just pressed this frame:

```csharp
if (inputBackend.IsKeyPressed(Key.Space))
{
    player.Jump();
}
```

Use for **discrete actions** (jump, shoot, interact).

#### IsKeyReleased

Check if a key was just released this frame:

```csharp
if (inputBackend.IsKeyReleased(Key.Space))
{
    player.ChargeAttackRelease();
}
```

Use for **charge mechanics** and **hold-release** actions.

### Key Enum

```csharp
public enum Key
{
    // Letters
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

    // Numbers
    Zero, One, Two, Three, Four, Five, Six, Seven, Eight, Nine,

    // Function keys
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

    // Modifiers
    LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt,

    // Special
    Space, Enter, Escape, Tab, Backspace, Delete,
    Left, Right, Up, Down,

    // Numpad
    Numpad0, Numpad1, Numpad2, Numpad3, Numpad4,
    Numpad5, Numpad6, Numpad7, Numpad8, Numpad9
}
```

### Modifier Keys

```csharp
if (inputBackend.IsKeyDown(Key.LeftControl) && inputBackend.IsKeyPressed(Key.S))
{
    SaveGame();
}
```

### Text Input

For text fields, use character input:

```csharp
char? GetCharPressed()
{
    return inputBackend.GetCharPressed();
}

// Usage
var c = inputBackend.GetCharPressed();
if (c.HasValue)
{
    textField.Append(c.Value);
}
```

---

## Mouse Input

### Mouse Position

```csharp
Vector2 mousePos = inputBackend.GetMousePosition();
Console.WriteLine($"Mouse at: {mousePos.X}, {mousePos.Y}");
```

### Mouse Delta

Get mouse movement since last frame:

```csharp
Vector2 mouseDelta = inputBackend.GetMouseDelta();
camera.Rotate(mouseDelta.X * sensitivity);
```

### Mouse Buttons

```csharp
public enum MouseButton
{
    Left,
    Right,
    Middle
}
```

#### Button States

```csharp
// Click detection
if (inputBackend.IsMouseButtonPressed(MouseButton.Left))
{
    Fire();
}

// Drag detection
if (inputBackend.IsMouseButtonDown(MouseButton.Left))
{
    DragObject();
}

// Release detection
if (inputBackend.IsMouseButtonReleased(MouseButton.Left))
{
    DropObject();
}
```

### Mouse Wheel

```csharp
float wheelDelta = inputBackend.GetMouseWheelDelta();
camera.Zoom += wheelDelta * 0.1f;
```

### Mouse Cursor

```csharp
// Show/hide cursor
inputBackend.SetMouseCursorVisible(false);

// Lock cursor to window center (FPS games)
inputBackend.SetMouseCursorLocked(true);
```

---

## Gamepad Input

### Gamepad Availability

```csharp
for (int i = 0; i < 4; i++)
{
    if (inputBackend.IsGamepadAvailable(i))
    {
        Console.WriteLine($"Gamepad {i} connected");
    }
}
```

### Gamepad Buttons

```csharp
public enum GamepadButton
{
    // Face buttons (Xbox naming)
    A, B, X, Y,

    // Shoulder buttons
    LeftBumper, RightBumper,

    // D-Pad
    DPadUp, DPadDown, DPadLeft, DPadRight,

    // Analog sticks
    LeftStick, RightStick,

    // Menu buttons
    Start, Back
}
```

#### Button Input

```csharp
int gamepadId = 0;

if (inputBackend.IsGamepadButtonPressed(gamepadId, GamepadButton.A))
{
    player.Jump();
}

if (inputBackend.IsGamepadButtonDown(gamepadId, GamepadButton.RightBumper))
{
    player.Aim();
}
```

### Gamepad Axes

```csharp
public enum GamepadAxis
{
    LeftX,        // Left stick horizontal
    LeftY,        // Left stick vertical
    RightX,       // Right stick horizontal
    RightY,       // Right stick vertical
    LeftTrigger,  // Left trigger (0 to 1)
    RightTrigger  // Right trigger (0 to 1)
}
```

#### Axis Input

```csharp
float leftX = inputBackend.GetGamepadAxis(gamepadId, GamepadAxis.LeftX);
float leftY = inputBackend.GetGamepadAxis(gamepadId, GamepadAxis.LeftY);

player.Move(new Vector2(leftX, leftY));
```

### Deadzone Handling

```csharp
public Vector2 GetGamepadStick(int gamepad, GamepadStick stick, float deadzone = 0.15f)
{
    var x = inputBackend.GetGamepadAxis(gamepad,
        stick == GamepadStick.Left ? GamepadAxis.LeftX : GamepadAxis.RightX);
    var y = inputBackend.GetGamepadAxis(gamepad,
        stick == GamepadStick.Left ? GamepadAxis.LeftY : GamepadAxis.RightY);

    var magnitude = new Vector2(x, y).Length();

    if (magnitude < deadzone)
        return Vector2.Zero;

    return new Vector2(x, y);
}
```

### Vibration (Future)

```csharp
inputBackend.SetGamepadVibration(gamepadId, leftMotor: 0.5f, rightMotor: 0.5f, duration: 0.2f);
```

---

## Input Actions and Mapping

### Input Actions

Abstract input into logical actions:

```csharp
public enum InputAction
{
    MoveLeft,
    MoveRight,
    Jump,
    Attack,
    Interact,
    Pause
}
```

### Action Mapping

Map actions to physical inputs:

```csharp
public class InputMapper
{
    private readonly Dictionary<InputAction, List<InputBinding>> _bindings;

    public bool IsActionPressed(InputAction action)
    {
        var bindings = _bindings[action];

        foreach (var binding in bindings)
        {
            if (binding.Type == BindingType.Key)
            {
                if (inputBackend.IsKeyPressed(binding.Key))
                    return true;
            }
            else if (binding.Type == BindingType.GamepadButton)
            {
                if (inputBackend.IsGamepadButtonPressed(binding.GamepadId, binding.Button))
                    return true;
            }
        }

        return false;
    }
}
```

### Rebindable Controls

```csharp
public class InputSettings
{
    public void RebindAction(InputAction action, Key newKey)
    {
        _inputMapper.ClearBindings(action);
        _inputMapper.AddBinding(action, new KeyBinding(newKey));
        SaveSettings();
    }
}

// Usage
inputSettings.RebindAction(InputAction.Jump, Key.W);
```

### Input Schemes

Support multiple control schemes:

```csharp
public enum ControlScheme
{
    KeyboardMouse,
    Gamepad
}

public void SetControlScheme(ControlScheme scheme)
{
    if (scheme == ControlScheme.KeyboardMouse)
    {
        _inputMapper.AddBinding(InputAction.Jump, new KeyBinding(Key.Space));
        _inputMapper.AddBinding(InputAction.Attack, new MouseButtonBinding(MouseButton.Left));
    }
    else if (scheme == ControlScheme.Gamepad)
    {
        _inputMapper.AddBinding(InputAction.Jump, new GamepadButtonBinding(0, GamepadButton.A));
        _inputMapper.AddBinding(InputAction.Attack, new GamepadButtonBinding(0, GamepadButton.X));
    }
}
```

---

## Input Buffering

### Why Buffer Input?

Input buffering allows frame-perfect inputs by storing inputs for a few frames:

```csharp
// Without buffering: player presses jump 1 frame before landing → no jump
// With buffering: jump input is remembered and executed on landing
```

### Buffer Implementation

```csharp
public class InputBuffer
{
    private readonly Dictionary<InputAction, Queue<float>> _buffer;
    private const float BufferTime = 0.1f; // 100ms buffer

    public void Update(float deltaTime)
    {
        foreach (var action in _buffer.Keys)
        {
            if (inputMapper.IsActionPressed(action))
            {
                _buffer[action].Enqueue(BufferTime);
            }

            // Decay buffered inputs
            if (_buffer[action].Count > 0)
            {
                var time = _buffer[action].Peek() - deltaTime;
                if (time <= 0)
                    _buffer[action].Dequeue();
                else
                    _buffer[action] = new Queue<float>(new[] { time });
            }
        }
    }

    public bool ConsumeAction(InputAction action)
    {
        if (_buffer[action].Count > 0)
        {
            _buffer[action].Clear();
            return true;
        }
        return false;
    }
}

// Usage
if (player.IsGrounded && inputBuffer.ConsumeAction(InputAction.Jump))
{
    player.Jump();
}
```

---

## Backend Implementations

### Raylib Input Backend

```csharp
public class RaylibInputBackend : IInputBackend
{
    public void Update()
    {
        // Raylib handles input polling internally
    }

    public bool IsKeyPressed(Key key)
    {
        return Raylib.IsKeyPressed(MapKey(key));
    }

    public Vector2 GetMousePosition()
    {
        var pos = Raylib.GetMousePosition();
        return new Vector2(pos.X, pos.Y);
    }

    private static KeyboardKey MapKey(Key key)
    {
        return key switch
        {
            Key.Space => KeyboardKey.KEY_SPACE,
            Key.W => KeyboardKey.KEY_W,
            // ... other mappings
        };
    }
}
```

### Custom Input Backend

```csharp
public class CustomInputBackend : IInputBackend
{
    private readonly HashSet<Key> _keysDown = new();
    private readonly HashSet<Key> _keysPressed = new();
    private readonly HashSet<Key> _keysReleased = new();

    public void Update()
    {
        _keysPressed.Clear();
        _keysReleased.Clear();

        // Poll platform-specific input
        PollKeyboardEvents();
    }

    public bool IsKeyDown(Key key) => _keysDown.Contains(key);
    public bool IsKeyPressed(Key key) => _keysPressed.Contains(key);
    public bool IsKeyReleased(Key key) => _keysReleased.Contains(key);
}
```

---

## Usage Examples

### Example 1: Player Movement System

```csharp
public class PlayerInputSystem : ISystem
{
    private readonly IInputBackend _input;

    public PlayerInputSystem(IInputBackend input)
    {
        _input = input;
    }

    public void Update(World world, float deltaTime)
    {
        var player = world.Query<PlayerComponent, TransformComponent>().First();
        var transform = player.GetComponent<TransformComponent>();

        var velocity = Vector2.Zero;

        if (_input.IsKeyDown(Key.W)) velocity.Y -= 1;
        if (_input.IsKeyDown(Key.S)) velocity.Y += 1;
        if (_input.IsKeyDown(Key.A)) velocity.X -= 1;
        if (_input.IsKeyDown(Key.D)) velocity.X += 1;

        if (velocity.LengthSquared() > 0)
        {
            velocity = Vector2.Normalize(velocity);
            transform.Position += new Vector3(velocity.X, velocity.Y, 0) * 200 * deltaTime;
        }
    }
}
```

### Example 2: Gamepad Support

```csharp
public class GamepadInputSystem : ISystem
{
    private readonly IInputBackend _input;
    private const int GamepadId = 0;

    public void Update(World world, float deltaTime)
    {
        if (!_input.IsGamepadAvailable(GamepadId))
            return;

        var player = world.Query<PlayerComponent, TransformComponent>().First();
        var transform = player.GetComponent<TransformComponent>();

        // Movement with left stick
        var leftX = _input.GetGamepadAxis(GamepadId, GamepadAxis.LeftX);
        var leftY = _input.GetGamepadAxis(GamepadId, GamepadAxis.LeftY);

        if (Math.Abs(leftX) > 0.15f || Math.Abs(leftY) > 0.15f)
        {
            transform.Position += new Vector3(leftX, leftY, 0) * 200 * deltaTime;
        }

        // Jump with A button
        if (_input.IsGamepadButtonPressed(GamepadId, GamepadButton.A))
        {
            player.GetComponent<VelocityComponent>().Value.Y = -500;
        }
    }
}
```

### Example 3: UI Interaction

```csharp
public class UISystem : ISystem
{
    private readonly IInputBackend _input;
    private readonly List<Button> _buttons;

    public void Update(World world, float deltaTime)
    {
        var mousePos = _input.GetMousePosition();

        foreach (var button in _buttons)
        {
            if (button.Bounds.Contains(mousePos))
            {
                button.IsHovered = true;

                if (_input.IsMouseButtonPressed(MouseButton.Left))
                {
                    button.OnClick?.Invoke();
                }
            }
            else
            {
                button.IsHovered = false;
            }
        }
    }
}
```

### Example 4: Camera Control

```csharp
public class CameraControlSystem : ISystem
{
    private readonly IInputBackend _input;
    private readonly Camera2D _camera;
    private Vector2 _lastMousePos;

    public void Update(World world, float deltaTime)
    {
        var mousePos = _input.GetMousePosition();

        // Pan camera with middle mouse button
        if (_input.IsMouseButtonDown(MouseButton.Middle))
        {
            var delta = mousePos - _lastMousePos;
            _camera.Position -= delta / _camera.Zoom;
        }

        // Zoom with mouse wheel
        var wheelDelta = _input.GetMouseWheelDelta();
        if (Math.Abs(wheelDelta) > 0.01f)
        {
            _camera.Zoom += wheelDelta * 0.1f;
            _camera.Zoom = Math.Clamp(_camera.Zoom, 0.5f, 4.0f);
        }

        _lastMousePos = mousePos;
    }
}
```

---

## Best Practices

### Do's

- ✓ Use `IInputBackend` interface, never concrete implementations in core
- ✓ Poll input at the start of the update loop
- ✓ Use `IsKeyPressed` for discrete actions, `IsKeyDown` for continuous
- ✓ Implement deadzone for analog sticks
- ✓ Support multiple control schemes (keyboard + gamepad)
- ✓ Use input mapping for rebindable controls
- ✓ Buffer inputs for frame-perfect timing
- ✓ Provide visual feedback for button presses

### Don'ts

- ✗ Don't poll input outside the update loop
- ✗ Don't hardcode input keys in gameplay logic (use actions)
- ✗ Don't ignore gamepad support
- ✗ Don't forget to handle edge cases (key held from previous scene)
- ✗ Don't use raw axis values without deadzone
- ✗ Don't assume keyboard/mouse is always available

### Input Architecture

**Good: Action-based**

```csharp
if (inputMapper.IsActionPressed(InputAction.Jump))
{
    player.Jump();
}
```

**Bad: Hardcoded keys**

```csharp
if (inputBackend.IsKeyPressed(Key.Space))
{
    player.Jump();
}
```

---

## API Reference

### IInputBackend

```csharp
public interface IInputBackend
{
    // Lifecycle
    void Update();

    // Keyboard
    bool IsKeyDown(Key key);
    bool IsKeyPressed(Key key);
    bool IsKeyReleased(Key key);
    char? GetCharPressed();

    // Mouse
    Vector2 GetMousePosition();
    Vector2 GetMouseDelta();
    bool IsMouseButtonDown(MouseButton button);
    bool IsMouseButtonPressed(MouseButton button);
    bool IsMouseButtonReleased(MouseButton button);
    float GetMouseWheelDelta();
    void SetMouseCursorVisible(bool visible);
    void SetMouseCursorLocked(bool locked);

    // Gamepad
    bool IsGamepadAvailable(int gamepad);
    string GetGamepadName(int gamepad);
    bool IsGamepadButtonDown(int gamepad, GamepadButton button);
    bool IsGamepadButtonPressed(int gamepad, GamepadButton button);
    bool IsGamepadButtonReleased(int gamepad, GamepadButton button);
    float GetGamepadAxis(int gamepad, GamepadAxis axis);
    void SetGamepadVibration(int gamepad, float leftMotor, float rightMotor, float duration);
}
```

---

## Related Documentation

- [Architecture](../ARCHITECTURE.md)
- [ECS Module](ECS.md)
- [Graphics Backend](GRAPHICS_BACKEND.md)
- [Audio Backend](AUDIO_BACKEND.md)

---

**Last Updated:** November 2025  
**Version:** 1.0

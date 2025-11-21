using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes.Demos;

/// <summary>
/// Demonstrates input handling across keyboard, mouse, and gamepad.
/// Shows real-time input state visualization and parameter passing.
/// </summary>
public sealed class InputDemo : Scene
{
    private IInputBackend _inputBackend = null!;
    private IRenderer2D _renderer = null!;
    private ILogger _logger = null!;
    private string _welcomeMessage;
    private bool _receivedParameters;
    
    private Vector2 _lastMousePosition;
    private readonly List<string> _recentKeys;
    private readonly List<string> _recentButtons;
    private readonly List<string> _recentGamepadButtons;
    private const int MAX_HISTORY = 5;
    private float _scrollAccumulator;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputDemo"/> class.
    /// </summary>
    public InputDemo()
        : base("InputDemo")
    {
        _welcomeMessage = "Input System Demo";
        _lastMousePosition = Vector2.Zero;
        _scrollAccumulator = 0f;
        _recentKeys = new List<string>();
        _recentButtons = new List<string>();
        _recentGamepadButtons = new List<string>();
    }

    /// <inheritdoc/>
    public override void OnLoad(SceneContext context)
    {
        base.OnLoad(context);
        _inputBackend = context.InputBackend;
        _renderer = context.Renderer;
        _logger = context.Logger;
        _logger.Info("InputDemo", "Input demo loaded - showing real-time input state");
    }

    /// <inheritdoc/>
    public override void OnLoad(SceneContext context, SceneParameters parameters)
    {
        base.OnLoad(context, parameters);
        
        // Extract parameters if provided
        if (parameters.TryGet<string>("welcomeMessage", out var message) && message != null)
        {
            _welcomeMessage = message;
            _receivedParameters = true;
            _logger.Info("InputDemo", $"Received parameter: welcomeMessage = {message}");
        }
        
        if (parameters.TryGet<bool>("fromMenu", out var fromMenu))
        {
            _logger.Info("InputDemo", $"Received parameter: fromMenu = {fromMenu}");
        }
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        // Early exit if not loaded yet (can happen during scene preloading)
        if (_inputBackend == null)
        {
            return;
        }

        if (_inputBackend.IsKeyPressed(Key.Escape))
        {
            PopScene();
            return;
        }

        // Track keyboard input
        foreach (Key key in Enum.GetValues<Key>())
        {
            if (_inputBackend.IsKeyPressed(key))
            {
                AddToHistory(_recentKeys, key.ToString());
                _logger.Debug("InputDemo", $"Key pressed: {key}");
            }
        }

        // Track mouse input
        var mousePos = _inputBackend.GetMousePosition();
        if (!mousePos.Equals(_lastMousePosition))
        {
            _lastMousePosition = mousePos;
        }

        foreach (MouseButton button in Enum.GetValues<MouseButton>())
        {
            if (_inputBackend.IsMouseButtonPressed(button))
            {
                AddToHistory(_recentButtons, button.ToString());
                _logger.Debug("InputDemo", $"Mouse button pressed: {button}");
            }
        }

        // Track mouse wheel
        var scroll = _inputBackend.GetMouseWheelDelta();
        _scrollAccumulator += scroll;

        // Track gamepad buttons
        if (_inputBackend.IsGamepadAvailable(0))
        {
            foreach (GamepadButton button in Enum.GetValues<GamepadButton>())
            {
                if (_inputBackend.IsGamepadButtonPressed(0, button))
                {
                    AddToHistory(_recentGamepadButtons, button.ToString());
                    _logger.Debug("InputDemo", $"Gamepad button pressed: {button}");
                }
            }
        }

        // Clear history on Space
        if (_inputBackend.IsKeyPressed(Key.Space))
        {
            _recentKeys.Clear();
            _recentButtons.Clear();
            _recentGamepadButtons.Clear();
            _scrollAccumulator = 0f;
            _logger.Info("InputDemo", "Input history cleared");
        }
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        // Early exit if not loaded yet (can happen during scene preloading)
        if (_renderer == null)
        {
            return;
        }

        _renderer.Clear(new Color(25, 30, 45, 255));

        var layout = new TextLayoutHelper(_renderer, startX: 20, startY: 20, defaultLineHeight: 20);
        var keyboardColor = new Color(200, 200, 255, 255);
        var mouseColor = new Color(255, 200, 200, 255);
        var gamepadColor = new Color(200, 255, 200, 255);
        var dimColor = new Color(180, 180, 180, 255);
        var grayColor = new Color(120, 120, 120, 255);
        var controlsColor = new Color(150, 150, 150, 255);

        // Title
        layout.DrawText(_welcomeMessage, 20, Color.White)
              .AddSpacing(10);
        
        if (_receivedParameters)
        {
            layout.DrawText("✓ Scene parameters received", 12, new Color(100, 255, 100, 255))
                  .AddSpacing(20);
        }
        else
        {
            layout.AddSpacing(20);
        }

        // Keyboard section
        layout.DrawSection("KEYBOARD", 16, keyboardColor, spacingAfter: 10)
              .SetX(40)
              .DrawText("Recent keys:", 14, dimColor)
              .SetX(60);
        
        if (_recentKeys.Count > 0)
        {
            foreach (var key in _recentKeys)
            {
                layout.DrawText($"• {key}", 12, Color.White, customLineHeight: 18);
            }
        }
        else
        {
            layout.DrawText("(none)", 12, grayColor, customLineHeight: 18);
        }

        layout.AddSpacing(20);

        // Mouse section
        layout.SetX(20)
              .DrawSection("MOUSE", 16, mouseColor, spacingAfter: 10)
              .SetX(40)
              .DrawText($"Position: ({_lastMousePosition.X:F0}, {_lastMousePosition.Y:F0})", 12, Color.White)
              .DrawText($"Scroll delta: {_scrollAccumulator:F2}", 12, Color.White)
              .DrawText("Recent buttons:", 14, dimColor)
              .SetX(60);
        
        if (_recentButtons.Count > 0)
        {
            foreach (var btn in _recentButtons)
            {
                layout.DrawText($"• {btn}", 12, Color.White, customLineHeight: 18);
            }
        }
        else
        {
            layout.DrawText("(none)", 12, grayColor, customLineHeight: 18);
        }

        // Gamepad section
        layout.AddSpacing(20)
              .SetX(20)
              .DrawSection("GAMEPAD", 16, gamepadColor, spacingAfter: 10)
              .SetX(40);
        
        if (!_inputBackend.IsGamepadAvailable(0))
        {
            layout.DrawText("No gamepad detected", 12, controlsColor);
        }
        else
        {
            // Show axis values
            var leftX = _inputBackend.GetGamepadAxis(0, GamepadAxis.LeftX);
            var leftY = _inputBackend.GetGamepadAxis(0, GamepadAxis.LeftY);
            var rightX = _inputBackend.GetGamepadAxis(0, GamepadAxis.RightX);
            var rightY = _inputBackend.GetGamepadAxis(0, GamepadAxis.RightY);
            
            bool hasAxisInput = System.Math.Abs(leftX) > 0.1f || System.Math.Abs(leftY) > 0.1f ||
                           System.Math.Abs(rightX) > 0.1f || System.Math.Abs(rightY) > 0.1f;
            
            if (hasAxisInput)
            {
                if (System.Math.Abs(leftX) > 0.1f || System.Math.Abs(leftY) > 0.1f)
                {
                    layout.DrawText($"Left Stick: ({leftX:F2}, {leftY:F2})", 12, Color.White, customLineHeight: 18);
                }
                
                if (System.Math.Abs(rightX) > 0.1f || System.Math.Abs(rightY) > 0.1f)
                {
                    layout.DrawText($"Right Stick: ({rightX:F2}, {rightY:F2})", 12, Color.White, customLineHeight: 18);
                }
            }
            
            // Show button history
            layout.DrawText("Recent buttons:", 14, dimColor)
                  .SetX(60);
            
            if (_recentGamepadButtons.Count > 0)
            {
                foreach (var btn in _recentGamepadButtons)
                {
                    layout.DrawText($"• {btn}", 12, Color.White, customLineHeight: 18);
                }
            }
            else
            {
                layout.DrawText("(none)", 12, grayColor);
            }
        }

        // Instructions
        layout.SetX(20)
              .SetY(510)
              .DrawText("[Press any key/button to record]", 14, dimColor)
              .DrawText("[SPACE] Clear history", 14, dimColor)
              .DrawText("[ESC] Back to Menu", 14, controlsColor);
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        base.OnUnload();
        _logger?.Info("InputDemo", "Input demo unloaded");
    }

    private void AddToHistory(List<string> history, string item)
    {
        history.Insert(0, item);
        if (history.Count > MAX_HISTORY)
        {
            history.RemoveAt(history.Count - 1);
        }
    }
}

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
    private IRenderBackend2D _renderBackend = null!;
    private ILogger _logger = null!;
    private string _welcomeMessage;
    private bool _receivedParameters;
    
    private Vector2 _lastMousePosition;
    private readonly List<string> _recentKeys;
    private readonly List<string> _recentButtons;
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
    }

    /// <inheritdoc/>
    public override void OnLoad(SceneContext context)
    {
        base.OnLoad(context);
        _inputBackend = context.InputBackend;
        _renderBackend = context.RenderBackend;
        _logger = context.Logger;
        _logger.Info("InputDemo", "Input demo loaded - showing real-time input state");
    }

    /// <inheritdoc/>
    public override void OnLoad(SceneContext context, SceneParameters parameters)
    {
        base.OnLoad(context, parameters);
        
        // Extract parameters if provided
        if (parameters.TryGet<string>("welcomeMessage", out var message))
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

        // Clear history on Space
        if (_inputBackend.IsKeyPressed(Key.Space))
        {
            _recentKeys.Clear();
            _recentButtons.Clear();
            _scrollAccumulator = 0f;
            _logger.Info("InputDemo", "Input history cleared");
        }
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        // Early exit if not loaded yet (can happen during scene preloading)
        if (_renderBackend == null)
        {
            return;
        }

        _renderBackend.Clear(new Color(25, 30, 45, 255));

        // Title
        _renderBackend.DrawText(_welcomeMessage, new Vector2(20, 20), 20, Color.White);
        
        if (_receivedParameters)
        {
            _renderBackend.DrawText("✓ Scene parameters received", new Vector2(20, 50), 12, new Color(100, 255, 100, 255));
        }

        var yPos = 90f;

        // Keyboard section
        _renderBackend.DrawText("KEYBOARD", new Vector2(20, yPos), 16, new Color(200, 200, 255, 255));
        yPos += 30;
        
        _renderBackend.DrawText("Recent keys:", new Vector2(40, yPos), 14, new Color(180, 180, 180, 255));
        yPos += 20;
        
        if (_recentKeys.Count > 0)
        {
            foreach (var key in _recentKeys)
            {
                _renderBackend.DrawText($"• {key}", new Vector2(60, yPos), 12, Color.White);
                yPos += 18;
            }
        }
        else
        {
            _renderBackend.DrawText("(none)", new Vector2(60, yPos), 12, new Color(120, 120, 120, 255));
            yPos += 18;
        }

        yPos += 20;

        // Mouse section
        _renderBackend.DrawText("MOUSE", new Vector2(20, yPos), 16, new Color(255, 200, 200, 255));
        yPos += 30;
        
        _renderBackend.DrawText($"Position: ({_lastMousePosition.X:F0}, {_lastMousePosition.Y:F0})", new Vector2(40, yPos), 12, Color.White);
        yPos += 20;
        
        _renderBackend.DrawText($"Scroll delta: {_scrollAccumulator:F2}", new Vector2(40, yPos), 12, Color.White);
        yPos += 20;
        
        _renderBackend.DrawText("Recent buttons:", new Vector2(40, yPos), 14, new Color(180, 180, 180, 255));
        yPos += 20;
        
        if (_recentButtons.Count > 0)
        {
            foreach (var btn in _recentButtons)
            {
                _renderBackend.DrawText($"• {btn}", new Vector2(60, yPos), 12, Color.White);
                yPos += 18;
            }
        }
        else
        {
            _renderBackend.DrawText("(none)", new Vector2(60, yPos), 12, new Color(120, 120, 120, 255));
            yPos += 18;
        }

        // Gamepad section
        yPos += 20;
        _renderBackend.DrawText("GAMEPAD", new Vector2(20, yPos), 16, new Color(200, 255, 200, 255));
        yPos += 30;
        
        // Show axis values
        var leftX = _inputBackend.GetGamepadAxis(0, GamepadAxis.LeftX);
        var leftY = _inputBackend.GetGamepadAxis(0, GamepadAxis.LeftY);
        var rightX = _inputBackend.GetGamepadAxis(0, GamepadAxis.RightX);
        var rightY = _inputBackend.GetGamepadAxis(0, GamepadAxis.RightY);
        
        bool hasInput = System.Math.Abs(leftX) > 0.1f || System.Math.Abs(leftY) > 0.1f ||
                       System.Math.Abs(rightX) > 0.1f || System.Math.Abs(rightY) > 0.1f;
        
        if (hasInput)
        {
            if (System.Math.Abs(leftX) > 0.1f || System.Math.Abs(leftY) > 0.1f)
            {
                _renderBackend.DrawText($"Left Stick: ({leftX:F2}, {leftY:F2})", new Vector2(40, yPos), 12, Color.White);
                yPos += 18;
            }
            
            if (System.Math.Abs(rightX) > 0.1f || System.Math.Abs(rightY) > 0.1f)
            {
                _renderBackend.DrawText($"Right Stick: ({rightX:F2}, {rightY:F2})", new Vector2(40, yPos), 12, Color.White);
                yPos += 18;
            }
        }
        else
        {
            _renderBackend.DrawText("No gamepad input detected", new Vector2(40, yPos), 12, new Color(150, 150, 150, 255));
            yPos += 18;
        }

        // Instructions
        _renderBackend.DrawText("[Press any key/button to record]", new Vector2(20, 510), 14, new Color(180, 180, 180, 255));
        _renderBackend.DrawText("[SPACE] Clear history", new Vector2(20, 535), 14, new Color(180, 180, 180, 255));
        _renderBackend.DrawText("[ESC] Back to Menu", new Vector2(20, 560), 14, new Color(150, 150, 150, 255));
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        base.OnUnload();
        _logger.Info("InputDemo", "Input demo unloaded");
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

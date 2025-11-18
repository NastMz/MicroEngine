using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes;

/// <summary>
/// Demo scene showcasing input mapping system.
/// Demonstrates action binding, remapping, and multi-device support.
/// </summary>
public sealed class InputMapDemoScene : Scene
{
    private readonly IInputBackend _inputBackend;
    private readonly IRenderBackend _renderBackend;
    private readonly ILogger _logger;
    private readonly IInputMap _inputMap;

    private Vector2 _playerPosition;
    private const float PLAYER_SPEED = 200f;
    private const float PLAYER_SIZE = 50f;

    private bool _isJumping;
    private float _jumpVelocity;
    private const float JUMP_STRENGTH = -400f;
    private const float GRAVITY = 980f;
    private const float GROUND_Y = 500f;

    private int _score;
    private bool _showHelp = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputMapDemoScene"/> class.
    /// </summary>
    /// <param name="inputBackend">The input backend.</param>
    /// <param name="renderBackend">The render backend.</param>
    /// <param name="logger">The logger.</param>
    public InputMapDemoScene(
        IInputBackend inputBackend,
        IRenderBackend renderBackend,
        ILogger logger)
        : base("InputMapDemo")
    {
        _inputBackend = inputBackend;
        _renderBackend = renderBackend;
        _logger = logger;
        _inputMap = new InputMap();

        _playerPosition = new Vector2(400, GROUND_Y);
        _jumpVelocity = 0f;
    }

    /// <inheritdoc/>
    public override void OnLoad()
    {
        SetupInputActions();
        _logger.Info(Name, "Input mapping configured with 6 actions");
    }

    private void SetupInputActions()
    {
        // Movement actions
        var moveLeft = new InputAction("MoveLeft")
            .AddKeyboardBinding(Key.Left)
            .AddKeyboardBinding(Key.A)
            .AddGamepadBinding(GamepadButton.DPadLeft);

        var moveRight = new InputAction("MoveRight")
            .AddKeyboardBinding(Key.Right)
            .AddKeyboardBinding(Key.D)
            .AddGamepadBinding(GamepadButton.DPadRight);

        var jump = new InputAction("Jump")
            .AddKeyboardBinding(Key.Space)
            .AddKeyboardBinding(Key.W)
            .AddKeyboardBinding(Key.Up)
            .AddGamepadBinding(GamepadButton.A)
            .AddMouseBinding(MouseButton.Left);

        // UI actions
        var toggleHelp = new InputAction("ToggleHelp")
            .AddKeyboardBinding(Key.H)
            .AddGamepadBinding(GamepadButton.Y);

        var remapControls = new InputAction("RemapControls")
            .AddKeyboardBinding(Key.R);

        var exit = new InputAction("Exit")
            .AddKeyboardBinding(Key.Escape)
            .AddGamepadBinding(GamepadButton.Start);

        _inputMap.AddAction(moveLeft);
        _inputMap.AddAction(moveRight);
        _inputMap.AddAction(jump);
        _inputMap.AddAction(toggleHelp);
        _inputMap.AddAction(remapControls);
        _inputMap.AddAction(exit);
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        _inputMap.Update(_inputBackend);

        HandleMovement(deltaTime);
        HandleJump(deltaTime);
        HandleUI();
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        _renderBackend.Clear(new Color(20, 20, 30, 255));

        RenderPlayer();
        RenderGround();
        RenderUI();

        if (_showHelp)
        {
            RenderHelp();
        }
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        _inputMap.Clear();
    }

    private void HandleMovement(float deltaTime)
    {
        var moveSpeed = 0f;

        if (_inputMap.IsActionHeld("MoveLeft"))
        {
            moveSpeed = -PLAYER_SPEED;
        }
        else if (_inputMap.IsActionHeld("MoveRight"))
        {
            moveSpeed = PLAYER_SPEED;
        }

        var newX = _playerPosition.X + (moveSpeed * deltaTime);

        // Clamp to screen bounds
        newX = System.Math.Clamp(newX, PLAYER_SIZE / 2, 1280 - PLAYER_SIZE / 2);
        _playerPosition = new Vector2(newX, _playerPosition.Y);
    }

    private void HandleJump(float deltaTime)
    {
        if (_inputMap.IsActionPressed("Jump") && !_isJumping)
        {
            _jumpVelocity = JUMP_STRENGTH;
            _isJumping = true;
            _score += 10;
            _logger.Info(Name, $"Jump! Score: {_score}");
        }

        if (_isJumping)
        {
            _jumpVelocity += GRAVITY * deltaTime;
            var newY = _playerPosition.Y + (_jumpVelocity * deltaTime);

            if (newY >= GROUND_Y)
            {
                newY = GROUND_Y;
                _jumpVelocity = 0f;
                _isJumping = false;
            }

            _playerPosition = new Vector2(_playerPosition.X, newY);
        }
    }

    private void HandleUI()
    {
        if (_inputMap.IsActionPressed("ToggleHelp"))
        {
            _showHelp = !_showHelp;
            _logger.Info(Name, $"Help overlay: {(_showHelp ? "ON" : "OFF")}");
        }

        if (_inputMap.IsActionPressed("RemapControls"))
        {
            RemapToAlternativeScheme();
        }

        if (_inputMap.IsActionPressed("Exit"))
        {
            _logger.Info(Name, "Exit requested via input mapping");
        }
    }

    private void RemapToAlternativeScheme()
    {
        _logger.Info(Name, "Remapping controls to WASD scheme");

        var moveLeft = _inputMap.GetAction("MoveLeft");
        if (moveLeft != null)
        {
            moveLeft.ClearBindings();
            moveLeft.AddKeyboardBinding(Key.A)
                .AddGamepadBinding(GamepadButton.DPadLeft);
        }

        var moveRight = _inputMap.GetAction("MoveRight");
        if (moveRight != null)
        {
            moveRight.ClearBindings();
            moveRight.AddKeyboardBinding(Key.D)
                .AddGamepadBinding(GamepadButton.DPadRight);
        }

        var jump = _inputMap.GetAction("Jump");
        if (jump != null)
        {
            jump.ClearBindings();
            jump.AddKeyboardBinding(Key.W)
                .AddGamepadBinding(GamepadButton.A);
        }
    }

    private void RenderPlayer()
    {
        var playerColor = _isJumping ? new Color(100, 200, 255, 255) : new Color(100, 255, 100, 255);

        var playerPos = new Vector2(
            _playerPosition.X - PLAYER_SIZE / 2,
            _playerPosition.Y - PLAYER_SIZE / 2);
        var playerSize = new Vector2(PLAYER_SIZE, PLAYER_SIZE);
        _renderBackend.DrawRectangle(playerPos, playerSize, playerColor);

        // Eyes
        var leftEyePos = new Vector2(_playerPosition.X - 15, _playerPosition.Y - 10);
        var rightEyePos = new Vector2(_playerPosition.X + 7, _playerPosition.Y - 10);
        var eyeSize = new Vector2(8, 8);
        _renderBackend.DrawRectangle(leftEyePos, eyeSize, Color.Black);
        _renderBackend.DrawRectangle(rightEyePos, eyeSize, Color.Black);
    }

    private void RenderGround()
    {
        var groundPos = new Vector2(0, GROUND_Y + PLAYER_SIZE / 2);
        var groundSize = new Vector2(1280, 720 - GROUND_Y - PLAYER_SIZE / 2);
        _renderBackend.DrawRectangle(groundPos, groundSize, new Color(80, 60, 40, 255));
    }

    private void RenderUI()
    {
        _renderBackend.DrawText($"Score: {_score}", new Vector2(10, 10), 24, Color.White);
        _renderBackend.DrawText($"Position: ({_playerPosition.X:F0}, {_playerPosition.Y:F0})", 
            new Vector2(10, 40), 20, Color.Gray);
        _renderBackend.DrawText($"Jumping: {(_isJumping ? "YES" : "NO")}", 
            new Vector2(10, 70), 20, Color.Gray);

        var jumpState = _inputMap.IsActionPressed("Jump") ? "PRESSED" 
            : _inputMap.IsActionHeld("Jump") ? "HELD" : "0";
        var actionStates = $"Actions: " +
            $"Left={(_inputMap.IsActionHeld("MoveLeft") ? "1" : "0")} " +
            $"Right={(_inputMap.IsActionHeld("MoveRight") ? "1" : "0")} " +
            $"Jump={jumpState}";

        _renderBackend.DrawText(actionStates, new Vector2(10, 100), 18, new Color(200, 200, 0, 255));
    }

    private void RenderHelp()
    {
        var helpY = 200;
        var lineHeight = 25;

        var bgPos = new Vector2(400, helpY - 10);
        var bgSize = new Vector2(480, 300);
        _renderBackend.DrawRectangle(bgPos, bgSize, new Color(0, 0, 0, 200));

        _renderBackend.DrawText("INPUT MAPPING DEMO", new Vector2(410, helpY), 24, 
            new Color(255, 255, 0, 255));
        helpY += lineHeight * 2;

        _renderBackend.DrawText("Movement:", new Vector2(410, helpY), 20, Color.White);
        helpY += lineHeight;
        _renderBackend.DrawText("  Left/A or Gamepad D-Pad Left", new Vector2(420, helpY), 18, Color.Gray);
        helpY += lineHeight;
        _renderBackend.DrawText("  Right/D or Gamepad D-Pad Right", new Vector2(420, helpY), 18, Color.Gray);
        helpY += lineHeight * 2;

        _renderBackend.DrawText("Jump:", new Vector2(410, helpY), 20, Color.White);
        helpY += lineHeight;
        _renderBackend.DrawText("  Space/W/Up or Gamepad A or Left Click", new Vector2(420, helpY), 18, Color.Gray);
        helpY += lineHeight * 2;

        _renderBackend.DrawText("Controls:", new Vector2(410, helpY), 20, Color.White);
        helpY += lineHeight;
        _renderBackend.DrawText("  H - Toggle this help", new Vector2(420, helpY), 18, Color.Gray);
        helpY += lineHeight;
        _renderBackend.DrawText("  R - Remap to alternative scheme", new Vector2(420, helpY), 18, Color.Gray);
        helpY += lineHeight;
        _renderBackend.DrawText("  ESC - Exit", new Vector2(420, helpY), 18, Color.Gray);
    }
}

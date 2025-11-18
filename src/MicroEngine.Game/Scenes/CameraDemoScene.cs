using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes;

/// <summary>
/// Demonstrates the 2D camera system with zoom, rotation, and follow.
/// </summary>
public class CameraDemoScene : Scene
{
    private const float PLAYER_SPEED = 200f;
    private const float CAMERA_FOLLOW_SPEED = 0.1f;
    private const float ZOOM_SPEED = 0.1f;
    private const float ROTATION_SPEED = 90f;

    private readonly ILogger _logger;
    private readonly IInputBackend _input;
    private readonly IRenderBackend _render;

    private Camera2D _camera;
    private Vector2 _playerPosition;
    private float _playerRotation;

    private bool _cameraFollowEnabled = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="CameraDemoScene"/> class.
    /// </summary>
    public CameraDemoScene(
        ILogger logger,
        IInputBackend input,
        IRenderBackend render)
        : base("CameraDemo")
    {
        _logger = logger;
        _input = input;
        _render = render;

        _camera = new Camera2D(Vector2.Zero, 1f);
        _playerPosition = Vector2.Zero;
        _playerRotation = 0f;
    }

    /// <inheritdoc/>
    public override void OnLoad()
    {
        _logger.Info(Name, "=== Camera Demo Scene ===");
        _logger.Info(Name, "Controls:");
        _logger.Info(Name, "  WASD - Move player");
        _logger.Info(Name, "  Q/E - Rotate player");
        _logger.Info(Name, "  Arrow Keys - Move camera manually");
        _logger.Info(Name, "  Z/X - Zoom in/out");
        _logger.Info(Name, "  C - Reset camera");
        _logger.Info(Name, "  F - Toggle camera follow");
        _logger.Info(Name, "  R - Rotate camera");
        _logger.Info(Name, "  ESC - Exit");

        // Position player at world origin
        _playerPosition = Vector2.Zero;

        // Center camera offset (so camera target is screen center)
        _camera.Offset = new Vector2(_render.WindowWidth / 2f, _render.WindowHeight / 2f);

        _logger.Info(Name, "Scene loaded - Camera follow enabled");
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        HandlePlayerInput(deltaTime);
        HandleCameraInput(deltaTime);

        if (_cameraFollowEnabled)
        {
            _camera.Follow(_playerPosition, CAMERA_FOLLOW_SPEED);
        }

        if (_input.IsKeyPressed(Key.Escape))
        {
            // Return to previous scene - just exit for now
            _logger.Info(Name, "Exiting camera demo");
        }
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        _render.Clear(Color.DarkGray);

        // Render world with camera
        _render.BeginCamera2D(_camera);
        RenderWorldGrid();
        RenderPlayer();
        _render.EndCamera2D();

        // Render UI (screen space)
        RenderUI();
    }

    private void HandlePlayerInput(float deltaTime)
    {
        var moveX = 0f;
        var moveY = 0f;

        if (_input.IsKeyDown(Key.W)) moveY -= 1;
        if (_input.IsKeyDown(Key.S)) moveY += 1;
        if (_input.IsKeyDown(Key.A)) moveX -= 1;
        if (_input.IsKeyDown(Key.D)) moveX += 1;

        var movement = new Vector2(moveX, moveY);
        if (movement.SqrMagnitude > 0f)
        {
            movement = movement.Normalized * PLAYER_SPEED * deltaTime;
            _playerPosition += movement;
        }

        if (_input.IsKeyDown(Key.Q)) _playerRotation -= ROTATION_SPEED * deltaTime;
        if (_input.IsKeyDown(Key.E)) _playerRotation += ROTATION_SPEED * deltaTime;
    }

    private void HandleCameraInput(float deltaTime)
    {
        // Manual camera movement (when follow is disabled)
        if (!_cameraFollowEnabled)
        {
            var moveX = 0f;
            var moveY = 0f;
            if (_input.IsKeyDown(Key.Up)) moveY -= 1;
            if (_input.IsKeyDown(Key.Down)) moveY += 1;
            if (_input.IsKeyDown(Key.Left)) moveX -= 1;
            if (_input.IsKeyDown(Key.Right)) moveX += 1;

            var cameraMove = new Vector2(moveX, moveY);
            if (cameraMove.SqrMagnitude > 0f)
            {
                cameraMove = cameraMove.Normalized * PLAYER_SPEED * deltaTime;
                _camera.Move(cameraMove);
            }
        }

        // Zoom
        if (_input.IsKeyDown(Key.Z)) _camera.AdjustZoom(ZOOM_SPEED * deltaTime);
        if (_input.IsKeyDown(Key.X)) _camera.AdjustZoom(-ZOOM_SPEED * deltaTime);

        // Camera rotation
        if (_input.IsKeyDown(Key.R)) _camera.Rotate(ROTATION_SPEED * deltaTime);

        // Toggle camera follow
        if (_input.IsKeyPressed(Key.F))
        {
            _cameraFollowEnabled = !_cameraFollowEnabled;
            _logger.Info(Name, $"Camera follow: {(_cameraFollowEnabled ? "ENABLED" : "DISABLED")}");
        }

        // Reset camera
        if (_input.IsKeyPressed(Key.C))
        {
            _camera.Reset();
            _camera.Offset = new Vector2(_render.WindowWidth / 2f, _render.WindowHeight / 2f);
            _cameraFollowEnabled = true;
            _logger.Info(Name, "Camera reset");
        }
    }

    private void RenderWorldGrid()
    {
        const int GRID_SIZE = 50;
        const int GRID_EXTENT = 500;

        // Draw grid lines
        for (var x = -GRID_EXTENT; x <= GRID_EXTENT; x += GRID_SIZE)
        {
            var color = x == 0 ? Color.Red : Color.Gray;
            _render.DrawLine(
                new Vector2(x, -GRID_EXTENT),
                new Vector2(x, GRID_EXTENT),
                color,
                x == 0 ? 2f : 1f);
        }

        for (var y = -GRID_EXTENT; y <= GRID_EXTENT; y += GRID_SIZE)
        {
            var color = y == 0 ? Color.Green : Color.Gray;
            _render.DrawLine(
                new Vector2(-GRID_EXTENT, y),
                new Vector2(GRID_EXTENT, y),
                color,
                y == 0 ? 2f : 1f);
        }

        // Draw world origin marker
        _render.DrawCircle(Vector2.Zero, 10f, Color.Yellow);
    }

    private void RenderPlayer()
    {
        // Draw player as a triangle pointing in rotation direction
        const float PLAYER_SIZE = 20f;

        // Convert rotation to radians
        var rotRad = MathHelper.ToRadians(_playerRotation);
        var cos = MathF.Cos(rotRad);
        var sin = MathF.Sin(rotRad);

        // Define triangle vertices (pointing up)
        var p1 = new Vector2(0, -PLAYER_SIZE); // tip
        var p2 = new Vector2(-PLAYER_SIZE / 2f, PLAYER_SIZE / 2f); // bottom left
        var p3 = new Vector2(PLAYER_SIZE / 2f, PLAYER_SIZE / 2f); // bottom right

        // Rotate and translate vertices
        Vector2 RotatePoint(Vector2 point)
        {
            return new Vector2(
                point.X * cos - point.Y * sin + _playerPosition.X,
                point.X * sin + point.Y * cos + _playerPosition.Y);
        }

        var tp1 = RotatePoint(p1);
        var tp2 = RotatePoint(p2);
        var tp3 = RotatePoint(p3);

        // Draw triangle edges
        _render.DrawLine(tp1, tp2, Color.Blue, 2f);
        _render.DrawLine(tp2, tp3, Color.Blue, 2f);
        _render.DrawLine(tp3, tp1, Color.Blue, 2f);

        // Draw player position marker
        _render.DrawCircle(_playerPosition, 5f, Color.Cyan);
    }

    private void RenderUI()
    {
        const int LINE_HEIGHT = 22;
        var y = 10;

        // Title
        _render.DrawText("MicroEngine - Camera2D Demo", new Vector2(10, y), 20, Color.Yellow);
        y += LINE_HEIGHT + 10;

        // Controls
        _render.DrawText("WASD: Move Player | Q/E: Rotate Player", new Vector2(10, y), 16, Color.White);
        y += LINE_HEIGHT;
        _render.DrawText("Arrows: Move Camera (manual) | Z/X: Zoom", new Vector2(10, y), 16, Color.White);
        y += LINE_HEIGHT;
        _render.DrawText($"F: Camera Follow [{(_cameraFollowEnabled ? "ON" : "OFF")}] | C: Reset | R: Rotate Camera", new Vector2(10, y), 16, Color.White);
        y += LINE_HEIGHT + 10;

        // Camera info
        _render.DrawText($"Camera Position: ({_camera.Position.X:F1}, {_camera.Position.Y:F1})", new Vector2(10, y), 16, Color.LightGray);
        y += LINE_HEIGHT;
        _render.DrawText($"Camera Zoom: {_camera.Zoom:F2}x | Rotation: {_camera.Rotation:F1}°", new Vector2(10, y), 16, Color.LightGray);
        y += LINE_HEIGHT;
        _render.DrawText($"Player Position: ({_playerPosition.X:F1}, {_playerPosition.Y:F1})", new Vector2(10, y), 16, Color.LightGray);
        y += LINE_HEIGHT;

        var visibleBounds = _camera.GetVisibleBounds(_render.WindowWidth, _render.WindowHeight);
        _render.DrawText($"Visible Bounds: {visibleBounds.Width:F0}x{visibleBounds.Height:F0}", new Vector2(10, y), 16, Color.LightGray);

        // FPS
        _render.DrawText($"FPS: {_render.GetFPS()}", new Vector2(_render.WindowWidth - 100, 10), 18, Color.Green);
    }
}

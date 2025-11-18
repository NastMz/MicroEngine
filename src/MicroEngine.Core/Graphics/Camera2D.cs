using MicroEngine.Core.Math;

namespace MicroEngine.Core.Graphics;

/// <summary>
/// 2D camera for viewport and world-space transformations.
/// Provides position, rotation, zoom, and viewport management.
/// </summary>
public sealed class Camera2D
{
    private float _zoom;

    /// <summary>
    /// Gets or sets the camera position in world space.
    /// This is the point the camera is looking at.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Gets or sets the camera offset from the target position.
    /// Useful for screen-shake or parallax effects.
    /// Default is (0, 0).
    /// </summary>
    public Vector2 Offset { get; set; }

    /// <summary>
    /// Gets or sets the camera rotation in degrees.
    /// Positive values rotate clockwise.
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// Gets or sets the camera zoom factor.
    /// 1.0 = normal, 2.0 = 2x zoom in, 0.5 = 2x zoom out.
    /// Must be greater than 0.
    /// </summary>
    public float Zoom
    {
        get => _zoom;
        set => _zoom = System.Math.Max(value, 0.0001f); // Prevent zero/negative zoom
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Camera2D"/> class.
    /// </summary>
    public Camera2D()
    {
        Position = Vector2.Zero;
        Offset = Vector2.Zero;
        Rotation = 0f;
        _zoom = 1f;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Camera2D"/> class with position.
    /// </summary>
    /// <param name="position">Initial camera position in world space.</param>
    public Camera2D(Vector2 position) : this()
    {
        Position = position;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Camera2D"/> class with position and zoom.
    /// </summary>
    /// <param name="position">Initial camera position in world space.</param>
    /// <param name="zoom">Initial zoom factor.</param>
    public Camera2D(Vector2 position, float zoom) : this(position)
    {
        Zoom = zoom; // Use property setter for validation
    }

    /// <summary>
    /// Converts a screen-space position to world-space position.
    /// </summary>
    /// <param name="screenPosition">Position in screen coordinates.</param>
    /// <param name="screenWidth">Screen width in pixels.</param>
    /// <param name="screenHeight">Screen height in pixels.</param>
    /// <returns>Position in world coordinates.</returns>
    public Vector2 ScreenToWorld(Vector2 screenPosition, int screenWidth, int screenHeight)
    {
        // Default offset is screen center if not set
        var actualOffset = Offset.SqrMagnitude > 0f ? Offset : new Vector2(screenWidth / 2f, screenHeight / 2f);

        // Apply inverse transformations
        var rotationRad = MathHelper.ToRadians(-Rotation);
        var cos = MathF.Cos(rotationRad);
        var sin = MathF.Sin(rotationRad);

        // Translate to origin
        var translated = screenPosition - actualOffset;

        // Apply inverse zoom
        var scaled = translated / Zoom;

        // Apply inverse rotation
        var rotated = new Vector2(
            scaled.X * cos - scaled.Y * sin,
            scaled.X * sin + scaled.Y * cos);

        // Translate to world position
        return Position + rotated;
    }

    /// <summary>
    /// Converts a world-space position to screen-space position.
    /// </summary>
    /// <param name="worldPosition">Position in world coordinates.</param>
    /// <param name="screenWidth">Screen width in pixels.</param>
    /// <param name="screenHeight">Screen height in pixels.</param>
    /// <returns>Position in screen coordinates.</returns>
    public Vector2 WorldToScreen(Vector2 worldPosition, int screenWidth, int screenHeight)
    {
        // Default offset is screen center if not set
        var actualOffset = Offset.SqrMagnitude > 0f ? Offset : new Vector2(screenWidth / 2f, screenHeight / 2f);

        // Translate to camera space
        var translated = worldPosition - Position;

        // Apply rotation
        var rotationRad = MathHelper.ToRadians(Rotation);
        var cos = MathF.Cos(rotationRad);
        var sin = MathF.Sin(rotationRad);

        var rotated = new Vector2(
            translated.X * cos - translated.Y * sin,
            translated.X * sin + translated.Y * cos);

        // Apply zoom
        var scaled = rotated * Zoom;

        // Translate to screen space
        return scaled + actualOffset;
    }

    /// <summary>
    /// Moves the camera by the specified offset in world space.
    /// </summary>
    /// <param name="offset">Movement offset.</param>
    public void Move(Vector2 offset)
    {
        Position += offset;
    }

    /// <summary>
    /// Rotates the camera by the specified angle in degrees.
    /// </summary>
    /// <param name="degrees">Rotation angle in degrees.</param>
    public void Rotate(float degrees)
    {
        Rotation += degrees;
    }

    /// <summary>
    /// Adjusts the zoom by the specified factor.
    /// </summary>
    /// <param name="factor">Zoom adjustment (additive).</param>
    public void AdjustZoom(float factor)
    {
        Zoom += factor; // Use property setter for validation
    }

    /// <summary>
    /// Resets the camera to default values.
    /// </summary>
    public void Reset()
    {
        Position = Vector2.Zero;
        Offset = Vector2.Zero;
        Rotation = 0f;
        _zoom = 1f;
    }

    /// <summary>
    /// Smoothly follows a target position using linear interpolation.
    /// </summary>
    /// <param name="targetPosition">Target position to follow.</param>
    /// <param name="speed">Follow speed (0 = instant, 1 = slow).</param>
    public void Follow(Vector2 targetPosition, float speed)
    {
        var t = System.Math.Clamp(speed, 0f, 1f);
        Position = Vector2.Lerp(Position, targetPosition, t);
    }

    /// <summary>
    /// Sets the camera to look at a specific world position.
    /// </summary>
    /// <param name="worldPosition">World position to look at.</param>
    public void LookAt(Vector2 worldPosition)
    {
        Position = worldPosition;
    }

    /// <summary>
    /// Gets the visible world bounds based on screen size and current camera state.
    /// </summary>
    /// <param name="screenWidth">Screen width in pixels.</param>
    /// <param name="screenHeight">Screen height in pixels.</param>
    /// <returns>Rectangle representing visible world bounds.</returns>
    public Rectangle GetVisibleBounds(int screenWidth, int screenHeight)
    {
        var topLeft = ScreenToWorld(Vector2.Zero, screenWidth, screenHeight);
        var bottomRight = ScreenToWorld(new Vector2(screenWidth, screenHeight), screenWidth, screenHeight);

        var width = MathF.Abs(bottomRight.X - topLeft.X);
        var height = MathF.Abs(bottomRight.Y - topLeft.Y);

        return new Rectangle(
            System.Math.Min(topLeft.X, bottomRight.X),
            System.Math.Min(topLeft.Y, bottomRight.Y),
            width,
            height);
    }
}

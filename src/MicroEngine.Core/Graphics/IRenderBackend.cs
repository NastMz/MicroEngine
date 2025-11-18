using MicroEngine.Core.Math;

namespace MicroEngine.Core.Graphics;

/// <summary>
/// Backend-agnostic rendering interface.
/// Provides unified API for 2D and future 3D rendering.
/// </summary>
public interface IRenderBackend
{
    #region Window Management

    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    string WindowTitle { get; set; }

    /// <summary>
    /// Gets the window width in pixels.
    /// </summary>
    int WindowWidth { get; }

    /// <summary>
    /// Gets the window height in pixels.
    /// </summary>
    int WindowHeight { get; }

    /// <summary>
    /// Gets whether the window should close.
    /// </summary>
    bool ShouldClose { get; }

    /// <summary>
    /// Initializes the rendering backend and creates a window.
    /// </summary>
    void Initialize(int width, int height, string title);

    /// <summary>
    /// Shuts down the rendering backend and closes the window.
    /// </summary>
    void Shutdown();

    #endregion

    #region Frame Management

    /// <summary>
    /// Begins a new frame for rendering.
    /// Must be called before any draw commands.
    /// </summary>
    void BeginFrame();

    /// <summary>
    /// Ends the current frame and presents the rendered image.
    /// </summary>
    void EndFrame();

    /// <summary>
    /// Clears the screen with the specified color.
    /// </summary>
    void Clear(Color color);

    #endregion

    #region 2D Rendering

    /// <summary>
    /// Draws a filled rectangle.
    /// </summary>
    void DrawRectangle(Vector2 position, Vector2 size, Color color);

    /// <summary>
    /// Draws a rectangle outline.
    /// </summary>
    void DrawRectangleLines(Vector2 position, Vector2 size, Color color, float thickness = 1f);

    /// <summary>
    /// Draws a filled circle.
    /// </summary>
    void DrawCircle(Vector2 center, float radius, Color color);

    /// <summary>
    /// Draws a circle outline.
    /// </summary>
    void DrawCircleLines(Vector2 center, float radius, Color color, float thickness = 1f);

    /// <summary>
    /// Draws a line between two points.
    /// </summary>
    void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 1f);

    /// <summary>
    /// Draws a texture (sprite) at the specified position.
    /// </summary>
    /// <param name="texture">Texture resource to draw.</param>
    /// <param name="position">Top-left position to draw at.</param>
    /// <param name="tint">Color tint (use White for no tint).</param>
    void DrawTexture(Resources.ITexture texture, Vector2 position, Color tint);

    /// <summary>
    /// Draws a portion of a texture (sprite) with rotation and scale.
    /// </summary>
    /// <param name="texture">Texture resource to draw.</param>
    /// <param name="sourceRect">Source rectangle in texture coordinates (x, y, width, height).</param>
    /// <param name="destRect">Destination rectangle on screen (x, y, width, height).</param>
    /// <param name="origin">Rotation origin point relative to destination.</param>
    /// <param name="rotation">Rotation angle in degrees.</param>
    /// <param name="tint">Color tint (use White for no tint).</param>
    void DrawTexturePro(
        Resources.ITexture texture,
        Math.Rectangle sourceRect,
        Math.Rectangle destRect,
        Vector2 origin,
        float rotation,
        Color tint);

    /// <summary>
    /// Draws text at the specified position.
    /// </summary>
    void DrawText(string text, Vector2 position, int fontSize, Color color);

    /// <summary>
    /// Draws text with a custom font.
    /// </summary>
    void DrawTextEx(Resources.IFont font, string text, Vector2 position, float fontSize, float spacing, Color color);

    #endregion

    #region Camera

    /// <summary>
    /// Begins 2D camera mode with the specified camera.
    /// All subsequent draw calls will be transformed by the camera.
    /// </summary>
    /// <param name="camera">The 2D camera to use for rendering.</param>
    void BeginCamera2D(Camera2D camera);

    /// <summary>
    /// Ends 2D camera mode.
    /// Subsequent draw calls will use screen-space coordinates.
    /// </summary>
    void EndCamera2D();

    #endregion

    #region FPS and Debug

    /// <summary>
    /// Gets the current frames per second.
    /// </summary>
    int GetFPS();

    /// <summary>
    /// Gets the time delta between frames in seconds.
    /// </summary>
    float GetDeltaTime();

    /// <summary>
    /// Sets the target FPS for the rendering backend.
    /// </summary>
    void SetTargetFPS(int fps);

    #endregion

    #region Anti-Aliasing

    /// <summary>
    /// Gets or sets the anti-aliasing mode for rendering.
    /// Anti-aliasing smooths jagged edges by sampling multiple points per pixel.
    /// Changing this setting may require restarting the rendering context in some backends.
    /// </summary>
    AntiAliasingMode AntiAliasing { get; set; }

    #endregion
}

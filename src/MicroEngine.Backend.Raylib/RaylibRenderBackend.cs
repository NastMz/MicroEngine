using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Math;
using Raylib_cs;
using Color = MicroEngine.Core.Graphics.Color;

namespace MicroEngine.Backend.Raylib;

/// <summary>
/// Raylib implementation of the render backend.
/// Provides 2D rendering capabilities using Raylib-cs.
/// </summary>
public class RaylibRenderBackend : IRenderBackend
{
    private bool _isInitialized;

    #region Window Management

    private string _windowTitle = string.Empty;

    /// <inheritdoc/>
    public string WindowTitle
    {
        get => _windowTitle;
        set
        {
            _windowTitle = value;
            if (_isInitialized)
            {
                Raylib_cs.Raylib.SetWindowTitle(value);
            }
        }
    }

    /// <inheritdoc/>
    public int WindowWidth => Raylib_cs.Raylib.GetScreenWidth();

    /// <inheritdoc/>
    public int WindowHeight => Raylib_cs.Raylib.GetScreenHeight();

    /// <inheritdoc/>
    public bool ShouldClose => Raylib_cs.Raylib.WindowShouldClose();

    /// <inheritdoc/>
    public void Initialize(int width, int height, string title)
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("Render backend already initialized");
        }

        Raylib_cs.Raylib.InitWindow(width, height, title);
        _isInitialized = true;
    }

    /// <inheritdoc/>
    public void Shutdown()
    {
        if (!_isInitialized)
        {
            return;
        }

        Raylib_cs.Raylib.CloseWindow();
        _isInitialized = false;
    }

    #endregion

    #region Frame Management

    /// <inheritdoc/>
    public void BeginFrame()
    {
        Raylib_cs.Raylib.BeginDrawing();
    }

    /// <inheritdoc/>
    public void EndFrame()
    {
        Raylib_cs.Raylib.EndDrawing();
    }

    /// <inheritdoc/>
    public void Clear(Color color)
    {
        Raylib_cs.Raylib.ClearBackground(ToRaylibColor(color));
    }

    #endregion

    #region 2D Rendering

    /// <inheritdoc/>
    public void DrawRectangle(Vector2 position, Vector2 size, Color color)
    {
        Raylib_cs.Raylib.DrawRectangle(
            (int)position.X,
            (int)position.Y,
            (int)size.X,
            (int)size.Y,
            ToRaylibColor(color)
        );
    }

    /// <inheritdoc/>
    public void DrawRectangleLines(Vector2 position, Vector2 size, Color color, float thickness = 1f)
    {
        Raylib_cs.Raylib.DrawRectangleLinesEx(
            new Rectangle(position.X, position.Y, size.X, size.Y),
            thickness,
            ToRaylibColor(color)
        );
    }

    /// <inheritdoc/>
    public void DrawCircle(Vector2 center, float radius, Color color)
    {
        Raylib_cs.Raylib.DrawCircle((int)center.X, (int)center.Y, radius, ToRaylibColor(color));
    }

    /// <inheritdoc/>
    public void DrawCircleLines(Vector2 center, float radius, Color color, float thickness = 1f)
    {
        Raylib_cs.Raylib.DrawCircleLines((int)center.X, (int)center.Y, radius, ToRaylibColor(color));
    }

    /// <inheritdoc/>
    public void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 1f)
    {
        Raylib_cs.Raylib.DrawLineEx(
            new System.Numerics.Vector2(start.X, start.Y),
            new System.Numerics.Vector2(end.X, end.Y),
            thickness,
            ToRaylibColor(color)
        );
    }

    /// <inheritdoc/>
    public void DrawText(string text, Vector2 position, int fontSize, Color color)
    {
        Raylib_cs.Raylib.DrawText(text, (int)position.X, (int)position.Y, fontSize, ToRaylibColor(color));
    }

    #endregion

    #region FPS and Debug

    /// <inheritdoc/>
    public int GetFPS()
    {
        return Raylib_cs.Raylib.GetFPS();
    }

    /// <inheritdoc/>
    public float GetDeltaTime()
    {
        return Raylib_cs.Raylib.GetFrameTime();
    }

    /// <inheritdoc/>
    public void SetTargetFPS(int fps)
    {
        Raylib_cs.Raylib.SetTargetFPS(fps);
    }

    #endregion

    #region Helper Methods

    private static Raylib_cs.Color ToRaylibColor(Color color)
    {
        return new Raylib_cs.Color(color.R, color.G, color.B, color.A);
    }

    #endregion
}

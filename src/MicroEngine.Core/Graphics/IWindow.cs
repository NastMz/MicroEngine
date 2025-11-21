namespace MicroEngine.Core.Graphics;

/// <summary>
/// Interface for window management.
/// Handles window creation, properties, and lifecycle.
/// </summary>
public interface IWindow
{
    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    string Title { get; set; }

    /// <summary>
    /// Gets the window width in pixels.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the window height in pixels.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets whether the window should close.
    /// </summary>
    bool ShouldClose { get; }

    /// <summary>
    /// Initializes the window.
    /// </summary>
    void Initialize(int width, int height, string title);

    /// <summary>
    /// Closes the window and releases resources.
    /// </summary>
    void Shutdown();
}

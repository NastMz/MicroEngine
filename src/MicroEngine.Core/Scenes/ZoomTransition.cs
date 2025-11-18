using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;

namespace MicroEngine.Core.Scenes;

/// <summary>
/// Zoom direction for scene transitions.
/// </summary>
public enum ZoomMode
{
    /// <summary>Zoom in (scene expands from center).</summary>
    ZoomIn,

    /// <summary>Zoom out (scene shrinks to center).</summary>
    ZoomOut
}

/// <summary>
/// Implements a zoom transition effect for scene changes.
/// The scene zooms in or out with a fade effect.
/// </summary>
public sealed class ZoomTransition : ISceneTransitionEffect
{
    private readonly IRenderBackend2D _renderBackend;
    private readonly float _duration;
    private readonly ZoomMode _zoomMode;
    private readonly Color _backgroundColor;

    private float _elapsed;
    private bool _isFadeOut;
    private bool _isComplete;

    /// <inheritdoc/>
    public bool IsComplete => _isComplete;

    /// <inheritdoc/>
    public float Progress => System.Math.Clamp(_elapsed / _duration, 0f, 1f);

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoomTransition"/> class.
    /// </summary>
    /// <param name="renderBackend">Render backend for drawing the zoom overlay.</param>
    /// <param name="zoomMode">Zoom mode (in or out).</param>
    /// <param name="duration">Duration of the zoom effect in seconds.</param>
    /// <param name="backgroundColor">Background color visible during zoom (default: black).</param>
    public ZoomTransition(
        IRenderBackend2D renderBackend,
        ZoomMode zoomMode = ZoomMode.ZoomOut,
        float duration = 0.5f,
        Color? backgroundColor = null)
    {
        _renderBackend = renderBackend ?? throw new ArgumentNullException(nameof(renderBackend));
        _duration = duration > 0 ? duration : throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive");
        _zoomMode = zoomMode;
        _backgroundColor = backgroundColor ?? new Color(0, 0, 0, 255);
        _isComplete = true;
    }

    /// <inheritdoc/>
    public void Start(bool fadeOut)
    {
        _isFadeOut = fadeOut;
        _elapsed = 0f;
        _isComplete = false;
    }

    /// <inheritdoc/>
    public void Update(float deltaTime)
    {
        if (_isComplete)
        {
            return;
        }

        _elapsed += deltaTime;

        if (_elapsed >= _duration)
        {
            _elapsed = _duration;
            _isComplete = true;
        }
    }

    /// <inheritdoc/>
    public void Render()
    {
        if (_elapsed <= 0f)
        {
            return;
        }

        float progress = Progress;

        // Calculate alpha based on fade direction
        float alpha = _isFadeOut ? progress : (1f - progress);

        int screenWidth = _renderBackend.WindowWidth;
        int screenHeight = _renderBackend.WindowHeight;

        // Zoom effect: draw borders that expand/contract
        // This creates the illusion of zooming by covering parts of the screen
        float borderSize;

        if (_zoomMode == ZoomMode.ZoomOut)
        {
            // Zoom out: borders expand from edges to center
            borderSize = _isFadeOut
                ? (System.Math.Min(screenWidth, screenHeight) / 2f) * progress
                : (System.Math.Min(screenWidth, screenHeight) / 2f) * (1f - progress);
        }
        else
        {
            // Zoom in: borders contract from center to edges
            borderSize = _isFadeOut
                ? (System.Math.Min(screenWidth, screenHeight) / 2f) * (1f - progress)
                : (System.Math.Min(screenWidth, screenHeight) / 2f) * progress;
        }

        // Draw semi-transparent overlay with borders
        Color overlayColor = new Color(
            _backgroundColor.R,
            _backgroundColor.G,
            _backgroundColor.B,
            (byte)(alpha * 255)
        );

        // Draw fullscreen overlay
        _renderBackend.DrawRectangle(
            Vector2.Zero,
            new Vector2(screenWidth, screenHeight),
            overlayColor
        );

        // For stronger zoom effect, add vignette-like darkening at edges
        if (borderSize > 0)
        {
            Color borderColor = new Color(
                _backgroundColor.R,
                _backgroundColor.G,
                _backgroundColor.B,
                (byte)(alpha * 128) // Half opacity for subtle effect
            );

            float horizontalBorder = (screenWidth / 2f) * (borderSize / (System.Math.Min(screenWidth, screenHeight) / 2f));
            float verticalBorder = (screenHeight / 2f) * (borderSize / (System.Math.Min(screenWidth, screenHeight) / 2f));

            // Left border
            _renderBackend.DrawRectangle(
                Vector2.Zero,
                new Vector2(horizontalBorder, screenHeight),
                borderColor);

            // Right border
            _renderBackend.DrawRectangle(
                new Vector2(screenWidth - horizontalBorder, 0),
                new Vector2(horizontalBorder, screenHeight),
                borderColor);

            // Top border
            _renderBackend.DrawRectangle(
                new Vector2(horizontalBorder, 0),
                new Vector2(screenWidth - 2 * horizontalBorder, verticalBorder),
                borderColor);

            // Bottom border
            _renderBackend.DrawRectangle(
                new Vector2(horizontalBorder, screenHeight - verticalBorder),
                new Vector2(screenWidth - 2 * horizontalBorder, verticalBorder),
                borderColor);
        }
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _elapsed = 0f;
        _isComplete = true;
    }
}

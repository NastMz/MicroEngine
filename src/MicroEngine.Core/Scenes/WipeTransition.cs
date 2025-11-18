using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;

namespace MicroEngine.Core.Scenes;

/// <summary>
/// Wipe direction for scene transitions.
/// </summary>
public enum WipeDirection
{
    /// <summary>Wipe from left to right.</summary>
    LeftToRight,

    /// <summary>Wipe from right to left.</summary>
    RightToLeft,

    /// <summary>Wipe from top to bottom.</summary>
    TopToBottom,

    /// <summary>Wipe from bottom to top.</summary>
    BottomToTop,

    /// <summary>Wipe from center outward (circular).</summary>
    CenterOut,

    /// <summary>Wipe from edges to center (circular).</summary>
    EdgeIn
}

/// <summary>
/// Implements a wipe transition effect for scene changes.
/// A solid color wipes across the screen, revealing the new scene.
/// </summary>
public sealed class WipeTransition : ISceneTransitionEffect
{
    private readonly IRenderBackend2D _renderBackend;
    private readonly float _duration;
    private readonly WipeDirection _direction;
    private readonly Color _wipeColor;

    private float _elapsed;
    private bool _isFadeOut;
    private bool _isComplete;

    /// <inheritdoc/>
    public bool IsComplete => _isComplete;

    /// <inheritdoc/>
    public float Progress => System.Math.Clamp(_elapsed / _duration, 0f, 1f);

    /// <summary>
    /// Initializes a new instance of the <see cref="WipeTransition"/> class.
    /// </summary>
    /// <param name="renderBackend">Render backend for drawing the wipe overlay.</param>
    /// <param name="direction">Direction of the wipe effect.</param>
    /// <param name="duration">Duration of the wipe effect in seconds.</param>
    /// <param name="wipeColor">Color of the wipe overlay (default: black).</param>
    public WipeTransition(
        IRenderBackend2D renderBackend,
        WipeDirection direction = WipeDirection.LeftToRight,
        float duration = 0.4f,
        Color? wipeColor = null)
    {
        _renderBackend = renderBackend ?? throw new ArgumentNullException(nameof(renderBackend));
        _duration = duration > 0 ? duration : throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive");
        _direction = direction;
        _wipeColor = wipeColor ?? new Color(0, 0, 0, 255);
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

        // Fade out: wipe covers screen (0 -> 1)
        // Fade in: wipe uncovers screen (1 -> 0)
        if (!_isFadeOut)
        {
            progress = 1f - progress;
        }

        int screenWidth = _renderBackend.WindowWidth;
        int screenHeight = _renderBackend.WindowHeight;

        switch (_direction)
        {
            case WipeDirection.LeftToRight:
                {
                    float width = screenWidth * progress;
                    _renderBackend.DrawRectangle(
                        Vector2.Zero,
                        new Vector2(width, screenHeight),
                        _wipeColor);
                }
                break;

            case WipeDirection.RightToLeft:
                {
                    float width = screenWidth * progress;
                    float x = screenWidth - width;
                    _renderBackend.DrawRectangle(
                        new Vector2(x, 0),
                        new Vector2(width, screenHeight),
                        _wipeColor);
                }
                break;

            case WipeDirection.TopToBottom:
                {
                    float height = screenHeight * progress;
                    _renderBackend.DrawRectangle(
                        Vector2.Zero,
                        new Vector2(screenWidth, height),
                        _wipeColor);
                }
                break;

            case WipeDirection.BottomToTop:
                {
                    float height = screenHeight * progress;
                    float y = screenHeight - height;
                    _renderBackend.DrawRectangle(
                        new Vector2(0, y),
                        new Vector2(screenWidth, height),
                        _wipeColor);
                }
                break;

            case WipeDirection.CenterOut:
                {
                    // Circular wipe from center
                    float radius = (float)System.Math.Sqrt(screenWidth * screenWidth + screenHeight * screenHeight) / 2f;
                    float currentRadius = radius * progress;
                    Vector2 center = new Vector2(screenWidth / 2f, screenHeight / 2f);
                    
                    // For simplicity, draw expanding rectangles (circular would require more complex rendering)
                    float size = currentRadius * 2f;
                    _renderBackend.DrawRectangle(
                        new Vector2(center.X - currentRadius, center.Y - currentRadius),
                        new Vector2(size, size),
                        _wipeColor);
                }
                break;

            case WipeDirection.EdgeIn:
                {
                    // Wipe from edges to center
                    float margin = (screenWidth / 2f) * (1f - progress);
                    float marginY = (screenHeight / 2f) * (1f - progress);
                    
                    // Draw four rectangles from edges
                    _renderBackend.DrawRectangle(Vector2.Zero, new Vector2(margin, screenHeight), _wipeColor); // Left
                    _renderBackend.DrawRectangle(new Vector2(screenWidth - margin, 0), new Vector2(margin, screenHeight), _wipeColor); // Right
                    _renderBackend.DrawRectangle(new Vector2(margin, 0), new Vector2(screenWidth - 2 * margin, marginY), _wipeColor); // Top
                    _renderBackend.DrawRectangle(new Vector2(margin, screenHeight - marginY), new Vector2(screenWidth - 2 * margin, marginY), _wipeColor); // Bottom
                }
                break;
        }
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _elapsed = 0f;
        _isComplete = true;
    }
}

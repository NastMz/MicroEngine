using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;

namespace MicroEngine.Core.Scenes;

/// <summary>
/// Slide direction for scene transitions.
/// </summary>
public enum SlideDirection
{
    /// <summary>Slide from/to the left.</summary>
    Left,

    /// <summary>Slide from/to the right.</summary>
    Right,

    /// <summary>Slide from/to the top.</summary>
    Up,

    /// <summary>Slide from/to the bottom.</summary>
    Down
}

/// <summary>
/// Implements a slide transition effect for scene changes.
/// The new scene slides in from a specified direction, pushing the old scene out.
/// </summary>
public sealed class SlideTransition : ISceneTransitionEffect
{
    private readonly IRenderBackend2D _renderBackend;
    private readonly float _duration;
    private readonly SlideDirection _direction;
    private readonly Color _backgroundColor;

    private float _elapsed;
    private bool _isFadeOut;
    private bool _isComplete;

    /// <inheritdoc/>
    public bool IsComplete => _isComplete;

    /// <inheritdoc/>
    public float Progress => System.Math.Clamp(_elapsed / _duration, 0f, 1f);

    /// <summary>
    /// Initializes a new instance of the <see cref="SlideTransition"/> class.
    /// </summary>
    /// <param name="renderBackend">Render backend for drawing the slide overlay.</param>
    /// <param name="direction">Direction from which the new scene slides in.</param>
    /// <param name="duration">Duration of the slide effect in seconds.</param>
    /// <param name="backgroundColor">Background color visible during slide (default: black).</param>
    public SlideTransition(
        IRenderBackend2D renderBackend,
        SlideDirection direction = SlideDirection.Left,
        float duration = 0.5f,
        Color? backgroundColor = null)
    {
        _renderBackend = renderBackend ?? throw new ArgumentNullException(nameof(renderBackend));
        _duration = duration > 0 ? duration : throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive");
        _direction = direction;
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
        
        // Fade out: scene slides out (0 -> 1)
        // Fade in: scene slides in (1 -> 0, we invert to get 0 -> 1)
        if (!_isFadeOut)
        {
            progress = 1f - progress;
        }

        int screenWidth = _renderBackend.WindowWidth;
        int screenHeight = _renderBackend.WindowHeight;

        // Calculate slide offset based on direction
        Vector2 offset = _direction switch
        {
            SlideDirection.Left => new Vector2(-screenWidth * progress, 0),
            SlideDirection.Right => new Vector2(screenWidth * progress, 0),
            SlideDirection.Up => new Vector2(0, -screenHeight * progress),
            SlideDirection.Down => new Vector2(0, screenHeight * progress),
            _ => Vector2.Zero
        };

        // Draw background color in the area revealed by the slide
        Vector2 bgPosition = _direction switch
        {
            SlideDirection.Left => new Vector2(screenWidth * (1 - progress), 0),
            SlideDirection.Right => new Vector2(0, 0),
            SlideDirection.Up => new Vector2(0, screenHeight * (1 - progress)),
            SlideDirection.Down => new Vector2(0, 0),
            _ => Vector2.Zero
        };

        Vector2 bgSize = _direction switch
        {
            SlideDirection.Left or SlideDirection.Right => new Vector2(screenWidth * progress, screenHeight),
            SlideDirection.Up or SlideDirection.Down => new Vector2(screenWidth, screenHeight * progress),
            _ => new Vector2(screenWidth, screenHeight)
        };

        _renderBackend.DrawRectangle(bgPosition, bgSize, _backgroundColor);
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _elapsed = 0f;
        _isComplete = true;
    }
}

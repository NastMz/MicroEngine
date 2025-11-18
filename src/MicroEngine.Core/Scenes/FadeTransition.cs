using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;

namespace MicroEngine.Core.Scenes;

/// <summary>
/// Implements a fade transition effect for scene changes.
/// Fades to a solid color (typically black) and back.
/// </summary>
public sealed class FadeTransition : ISceneTransitionEffect
{
    private readonly IRenderBackend2D _renderBackend;
    private readonly float _duration;
    private readonly Color _fadeColor;

    private float _elapsed;
    private bool _isFadeOut;
    private bool _isComplete;

    /// <inheritdoc/>
    public bool IsComplete => _isComplete;

    /// <inheritdoc/>
    public float Progress => System.Math.Clamp(_elapsed / _duration, 0f, 1f);

    /// <summary>
    /// Initializes a new instance of the <see cref="FadeTransition"/> class.
    /// </summary>
    /// <param name="renderBackend">Render backend for drawing the fade overlay.</param>
    /// <param name="duration">Duration of the fade effect in seconds.</param>
    /// <param name="fadeColor">Color to fade to (default: black).</param>
    public FadeTransition(IRenderBackend2D renderBackend, float duration = 0.3f, Color? fadeColor = null)
    {
        _renderBackend = renderBackend ?? throw new ArgumentNullException(nameof(renderBackend));
        _duration = duration > 0 ? duration : throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive");
        _fadeColor = fadeColor ?? new Color(0, 0, 0, 255);
        _isComplete = true; // Initially complete (no transition running)
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

        float alpha = Progress;

        // Fade out: 0 -> 1 (transparent to opaque)
        // Fade in: 1 -> 0 (opaque to transparent)
        if (!_isFadeOut)
        {
            alpha = 1f - alpha;
        }

        // Get screen dimensions
        int screenWidth = _renderBackend.WindowWidth;
        int screenHeight = _renderBackend.WindowHeight;

        // Draw fullscreen rectangle with fade color
        Color fadeWithAlpha = new Color(
            _fadeColor.R,
            _fadeColor.G,
            _fadeColor.B,
            (byte)(alpha * 255)
        );

        _renderBackend.DrawRectangle(
            Vector2.Zero,
            new Vector2(screenWidth, screenHeight),
            fadeWithAlpha
        );
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _elapsed = 0f;
        _isComplete = true; // Reset to idle state (no transition running)
    }
}

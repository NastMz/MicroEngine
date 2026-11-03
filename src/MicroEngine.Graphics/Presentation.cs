using System.Numerics;
using MicroEngine.Graphics.Devices;

namespace MicroEngine.Graphics;

/// <summary>How the canvas's scale to the window is rounded (<see cref="Presentation.Rounding"/>).</summary>
public enum ScaleRounding
{
    /// <summary>The largest scale that fits, whatever its fraction: smooth and HD art.</summary>
    Fractional,
    /// <summary>
    /// Whole factors (2×, 3×...), so every canvas pixel covers the same number of window pixels; a window smaller than the
    /// canvas still shrinks it (never crops).
    /// </summary>
    Integer,
    /// <summary>
    /// Whole factors, except when the largest whole factor would fill less than <see cref="Presentation.WastefulThreshold"/>
    /// of what fits (a small phone browser): then fractional. The pixel-art default.
    /// </summary>
    IntegerUnlessWasteful,
}

/// <summary>What happens when the window's shape differs from the canvas's (<see cref="Presentation.Aspect"/>).</summary>
public enum AspectMode
{
    /// <summary>Keep the canvas's shape; the rest of the window is letterbox.</summary>
    Keep,
    /// <summary>Fill the whole area, stretching the canvas to the window's shape (pixels stop being square).</summary>
    Stretch,
}

/// <summary>
/// How the game's canvas reaches the window, one independent choice per field: how its scale is rounded
/// (<see cref="Rounding"/>), what happens to the window's other shape (<see cref="Aspect"/>), how it is sampled when it is
/// scaled (<see cref="Upscale"/>), the sampler textures get when they choose none (<see cref="DefaultSampler"/>), the size
/// factor of captures (<see cref="CaptureScale"/>) and whether desktop windows open at whole factors
/// (<see cref="WholeFactorWindows"/>). <see cref="PixelArt"/> and <see cref="HighResolution2D"/> are presets: start from
/// one and change any field with <c>with</c>.
/// </summary>
public sealed record Presentation
{
    private readonly float _wastefulThreshold = 0.8f;
    private readonly int _captureScale = 1;

    /// <summary>
    /// Pixel art: whole factors unless they waste more than a fifth of the window, letterboxed, nearest sampling for the
    /// canvas and for textures, captures three times the canvas, windows opened at whole factors.
    /// </summary>
    public static Presentation PixelArt { get; } = new()
    {
        Rounding = ScaleRounding.IntegerUnlessWasteful,
        Upscale = SamplerDescription.PointClamp,
        DefaultSampler = SamplerDescription.PointClamp,
        CaptureScale = 3,
        WholeFactorWindows = true,
    };

    /// <summary>
    /// High-resolution 2D: the largest size of the canvas's shape that fits, letterboxed, linear sampling for the canvas
    /// and for textures, captures at the canvas's size.
    /// </summary>
    public static Presentation HighResolution2D { get; } = new()
    {
        Rounding = ScaleRounding.Fractional,
        Upscale = SamplerDescription.LinearClamp,
        DefaultSampler = SamplerDescription.LinearClamp,
        CaptureScale = 1,
        WholeFactorWindows = false,
    };

    /// <summary>How the scale is rounded.</summary>
    public ScaleRounding Rounding { get; init; } = ScaleRounding.Fractional;

    /// <summary>Keep the canvas's shape (letterbox) or stretch it to the window.</summary>
    public AspectMode Aspect { get; init; } = AspectMode.Keep;

    /// <summary>How the canvas is sampled when it is drawn to the window at another size.</summary>
    public SamplerDescription Upscale { get; init; } = SamplerDescription.LinearClamp;

    /// <summary>
    /// The sampler of textures, atlases and font pages created without a filter of their own: the game's explicit default,
    /// written down instead of implied by another option.
    /// </summary>
    public SamplerDescription DefaultSampler { get; init; } = SamplerDescription.LinearClamp;

    /// <summary>
    /// With <see cref="ScaleRounding.IntegerUnlessWasteful"/>: the share of the fitting size a whole factor must fill (0.8
    /// by default, so a whole factor is kept when it fills at least 80 % of what would fit).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not within 0..1.</exception>
    public float WastefulThreshold
    {
        get => _wastefulThreshold;
        init => _wastefulThreshold = value is >= 0 and <= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value), "The threshold is a share between 0 and 1.");
    }

    /// <summary>Size factor of the captures a run writes (screenshots, scripted captures): 1 is the canvas's own size.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 1.</exception>
    public int CaptureScale
    {
        get => _captureScale;
        init => _captureScale = value >= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value), "Captures are at least the canvas's size (1).");
    }

    /// <summary>Whether desktop windows open at a whole factor of the canvas when the requested size does not fit the display.</summary>
    public bool WholeFactorWindows { get; init; }

    /// <summary>
    /// Where the canvas lands in <paramref name="area"/> (the window, or its safe area on a phone), in window pixels: the
    /// largest scale that fits, rounded by <see cref="Rounding"/>, centred; with <paramref name="portrait"/> it sits at the
    /// top of the area instead (on-screen controls get the space below). With <see cref="AspectMode.Stretch"/> it covers
    /// the whole area.
    /// </summary>
    /// <param name="area">The area available, in window pixels.</param>
    /// <param name="canvasWidth">The canvas's width in pixels.</param>
    /// <param name="canvasHeight">The canvas's height in pixels.</param>
    /// <param name="portrait">Whether the screen is portrait (taller than wide by more than a tenth).</param>
    /// <exception cref="ArgumentOutOfRangeException">The canvas size is not positive.</exception>
    public RectF Fit(RectF area, int canvasWidth, int canvasHeight, bool portrait = false)
    {
        if (canvasWidth <= 0 || canvasHeight <= 0) throw new ArgumentOutOfRangeException(nameof(canvasWidth), "The canvas size must be positive.");
        if (Aspect == AspectMode.Stretch) return area;
        float fit = MathF.Min(area.Width / canvasWidth, area.Height / canvasHeight);
        float scale = Rounding switch
        {
            ScaleRounding.Integer when fit >= 1 => MathF.Floor(fit),
            ScaleRounding.IntegerUnlessWasteful when fit >= 1 && MathF.Floor(fit) >= fit * WastefulThreshold => MathF.Floor(fit),
            _ => fit,
        };
        float w = canvasWidth * scale, h = canvasHeight * scale;
        float x = MathF.Floor(area.X + (area.Width - w) / 2);
        float y = portrait ? area.Y : MathF.Floor(area.Y + (area.Height - h) / 2);
        return new RectF(x, y, w, h);
    }

    /// <summary>
    /// Where the canvas lands in a window of <paramref name="window"/> pixels whose usable part is
    /// <paramref name="safeArea"/> (an empty safe area means the whole window): <see cref="Fit(RectF, int, int, bool)"/>
    /// in the safe area, at its top when the window is portrait (taller than wide by more than a tenth). Every backend
    /// places its frame with this method, so they agree.
    /// </summary>
    public RectF Fit(Vector2 window, RectF safeArea, int canvasWidth, int canvasHeight)
    {
        if (safeArea.Width <= 0 || safeArea.Height <= 0) safeArea = new RectF(0, 0, window.X, window.Y);
        return Fit(safeArea, canvasWidth, canvasHeight, portrait: window.Y > window.X * 1.1f);
    }

    /// <summary>A window position in canvas pixels, for a canvas drawn at <paramref name="frame"/>.</summary>
    public static Vector2 ToCanvas(Vector2 window, RectF frame, int canvasWidth, int canvasHeight) =>
        new((window.X - frame.X) * canvasWidth / Math.Max(1, frame.Width), (window.Y - frame.Y) * canvasHeight / Math.Max(1, frame.Height));
}

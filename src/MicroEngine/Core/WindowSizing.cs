using MicroEngine.Graphics;

namespace MicroEngine.Core;

/// <summary>The window sizes desktop platforms open and allow, shared by every backend so they agree.</summary>
public static class WindowSizing
{
    /// <summary>Share of the display's usable area a window may take when it opens.</summary>
    public const float MaximumDisplayShare = 0.9f;

    /// <summary>
    /// The size a window opens at: the canvas times <paramref name="scale"/>, or, when that does not fit in
    /// <see cref="MaximumDisplayShare"/> of the display's usable area, the largest size that does: a whole factor of the
    /// canvas when the presentation asks for whole-factor windows (<see cref="Presentation.WholeFactorWindows"/>, pixel
    /// art) and one fits, otherwise the canvas's shape scaled to fit (a 1920×1080 canvas on a 1366×768 laptop).
    /// </summary>
    /// <param name="settings">The game's canvas and scaling.</param>
    /// <param name="scale">The requested multiple of the canvas (<see cref="GameSettings.Scale"/> or <c>--scale</c>).</param>
    /// <param name="usableWidth">Width of the display's usable area; 0 or less when unknown, which keeps the requested size.</param>
    /// <param name="usableHeight">Height of the display's usable area; 0 or less when unknown.</param>
    public static (int Width, int Height) Initial(GameSettings settings, int scale, int usableWidth, int usableHeight)
    {
        ArgumentNullException.ThrowIfNull(settings);
        int vw = settings.VirtualWidth, vh = settings.VirtualHeight;
        scale = Math.Max(1, scale);
        if (usableWidth <= 0 || usableHeight <= 0) return (vw * scale, vh * scale);
        float maxW = usableWidth * MaximumDisplayShare, maxH = usableHeight * MaximumDisplayShare;
        if (vw * scale <= maxW && vh * scale <= maxH) return (vw * scale, vh * scale);
        float fit = MathF.Min(maxW / vw, maxH / vh);
        if (settings.Presentation.WholeFactorWindows && fit >= 1) return (vw * (int)fit, vh * (int)fit);
        return (Math.Max(1, (int)(vw * fit)), Math.Max(1, (int)(vh * fit)));
    }

    /// <summary>
    /// The smallest window size: the canvas itself, or a quarter of it for canvases wider than 640 pixels (an HD canvas
    /// would otherwise forbid windows smaller than a laptop screen).
    /// </summary>
    public static (int Width, int Height) Minimum(GameSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.VirtualWidth > 640
            ? (Math.Max(1, settings.VirtualWidth / 4), Math.Max(1, settings.VirtualHeight / 4))
            : (settings.VirtualWidth, settings.VirtualHeight);
    }
}

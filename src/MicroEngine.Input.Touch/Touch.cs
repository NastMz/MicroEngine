using System.Numerics;
using MicroEngine.Graphics;

namespace MicroEngine.Input;

/// <summary>
/// What happened to a finger in a <see cref="TouchEvent"/>: it touched the screen (<c>Began</c>), moved (<c>Moved</c>),
/// was lifted (<c>Ended</c>), or the system took it away, for example when the game left the screen (<c>Canceled</c>).
/// </summary>
public enum TouchPhase { Began, Moved, Ended, Canceled }

/// <summary>One change of one finger, in the touch screen's pixels (<see cref="ITouchScreen.Size"/>).</summary>
/// <param name="Finger">Identifies the finger from <see cref="TouchPhase.Began"/> until it ends or is cancelled.</param>
public readonly record struct TouchEvent(long Finger, TouchPhase Phase, Vector2 Position);

/// <summary>
/// A screen that may be touched, a capability a backend offers (<see cref="Platform.IPlatformHost.TryGetCapability{T}"/>): its drawable size, the
/// area free of notches, rounded corners and system bars, its density, the touches since the last frame, and a layer
/// drawn over the game for on-screen controls. Coordinates are drawable pixels with the origin at the top left.
/// SDL reports phones and touch screens, the browser host reports Pointer Events, the headless platform lets tests
/// touch it (<c>HeadlessTouchScreen</c>).
/// </summary>
public interface ITouchScreen
{
    /// <summary>Drawable size in pixels.</summary>
    Vector2 Size { get; }
    /// <summary>The part of <see cref="Size"/> no notch, rounded corner or system bar covers.</summary>
    RectF SafeArea { get; }
    /// <summary>Pixels per density-independent pixel (1/160 inch): 1 on a classic desktop display, 2-4 on phones.</summary>
    float PixelsPerDp { get; }
    /// <summary>Whether touch is the main way to play here (a phone, a tablet, a touch-only browser).</summary>
    bool TouchFirst { get; }
    /// <summary>Moves every touch event since the previous call into <paramref name="events"/>, oldest first.</summary>
    void ReadTouches(List<TouchEvent> events);
    /// <summary>Drawn over the letterboxed game in screen pixels after every frame (never in captures).</summary>
    ITouchOverlay? Overlay { get; set; }
    /// <summary>Whether <see cref="Vibrate"/> can do anything here.</summary>
    bool CanVibrate => false;
    /// <summary>A short vibration (strength 0..1). Returns false when the device cannot vibrate.</summary>
    bool Vibrate(float strength, int milliseconds) => false;
}

/// <summary>Something drawn over the game on the touch screen, such as <see cref="VirtualControls"/>.</summary>
public interface ITouchOverlay
{
    /// <summary>Draws the overlay in screen pixels.</summary>
    void Draw(IOverlayCanvas canvas);

    /// <summary>
    /// Where the backend should draw the game frame in this window, so the overlay does not cover it. The default is
    /// the plain letterbox rule of the game's presentation (<see cref="FrameLayout.Fit"/>); <see cref="VirtualControls"/>
    /// fits the frame around its controls. Null <paramref name="presentation"/> is <see cref="Presentation.PixelArt"/>.
    /// </summary>
    RectF Frame(Vector2 window, RectF safeArea, int virtualWidth, int virtualHeight, Presentation? presentation = null) => FrameLayout.Fit(window, safeArea, virtualWidth, virtualHeight, presentation);
}

/// <summary>Primitive drawing in screen pixels, implemented by each backend for <see cref="ITouchOverlay"/>.</summary>
public interface IOverlayCanvas
{
    /// <summary>Fills a circle.</summary>
    void FillCircle(Vector2 center, float radius, Color color);
    /// <summary>A circle outline of the given thickness, drawn inward from <paramref name="radius"/>.</summary>
    void Ring(Vector2 center, float radius, float thickness, Color color);
    /// <summary>Fills a triangle.</summary>
    void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color);
    /// <summary>Fills a rectangle.</summary>
    void FillRect(RectF rect, Color color);
    /// <summary>Short text centred on <paramref name="center"/>, about <paramref name="height"/> pixels tall.</summary>
    void Text(string text, Vector2 center, float height, Color color);
    /// <summary>
    /// Draws <paramref name="image"/> stretched over <paramref name="destination"/> without smoothing, multiplied by
    /// <paramref name="tint"/> (white keeps its colours; the alpha fades it). Backends keep one texture per image, so the
    /// same <see cref="Graphics.PixelImage"/> instance should be passed every frame. Canvases that cannot draw images
    /// keep the default, which draws nothing.
    /// </summary>
    void Image(Graphics.PixelImage image, RectF destination, Color tint) { }
}

/// <summary>Where the virtual canvas goes in a window: the shared letterbox rule of every backend.</summary>
public static class FrameLayout
{
    /// <summary>
    /// Where the frame goes in the safe area (the whole window when the safe area is empty), by the presentation's rule
    /// (<see cref="Presentation.Fit(Vector2, RectF, int, int)"/>). With <see cref="Presentation.PixelArt"/> (the default,
    /// also used when <paramref name="presentation"/> is null) it is the largest integer scale of the canvas that fits,
    /// centred, so pixels stay square and even; when that would fill less than 80 % of what fits (a 480×270 game on a
    /// 412-pixel-high phone browser is 1× of a possible 1.5×), the frame scales fractionally instead: on small screens
    /// size matters more than even pixels. With <see cref="Presentation.HighResolution2D"/> the frame always takes the
    /// largest size that fits. On a portrait screen the frame sits at the top of the safe area, so on-screen controls get
    /// the space below.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The canvas size is not positive.</exception>
    public static RectF Fit(Vector2 window, RectF safeArea, int virtualWidth, int virtualHeight, Presentation? presentation = null) =>
        (presentation ?? Presentation.PixelArt).Fit(window, safeArea, virtualWidth, virtualHeight);

    /// <summary>A window position in virtual canvas pixels for the frame <paramref name="frame"/>.</summary>
    public static Vector2 ToCanvas(Vector2 window, RectF frame, int virtualWidth, int virtualHeight) =>
        new((window.X - frame.X) * virtualWidth / Math.Max(1, frame.Width), (window.Y - frame.Y) * virtualHeight / Math.Max(1, frame.Height));
}

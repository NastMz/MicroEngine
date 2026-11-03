using System.Numerics;
using MicroEngine.Graphics;
using MicroEngine.Input;

namespace MicroEngine.Core;

/// <summary>
/// The UI view of a platform that draws at the window's resolution: a capability
/// (<see cref="Platform.IPlatformHost.TryGetCapability{T}"/>) of the SDL and raylib platforms and of the headless
/// platform. The world is drawn into the canvas (<see cref="IPlatform.BeginFrame"/>) and scaled into
/// <see cref="WorldFrame"/>; the UI view is drawn in UI units (window pixels divided by <see cref="UiScale"/>) and
/// composited over it, so its text and shapes have the window's resolution whatever the canvas's.
/// </summary>
public interface IScreenView
{
    /// <summary>The window's drawable size in pixels.</summary>
    (int Width, int Height) ScreenPixels { get; }
    /// <summary>The canvas's current size in pixels (larger than the configured one with an expanding presentation).</summary>
    (int Width, int Height) CanvasSize { get; }
    /// <summary>Where the canvas lands in the window, in window pixels.</summary>
    RectF WorldFrame { get; }
    /// <summary>Window pixels per UI unit (<see cref="Presentation.UiScale"/>).</summary>
    float UiScale { get; }
    /// <summary>Starts this frame's UI view, transparent; called after <see cref="IPlatform.BeginFrame"/>, at most once per frame.</summary>
    IRenderer BeginUi();
}

/// <summary>
/// The game's two views and the mapping between them (<see cref="Game.Views"/>): the world view (the canvas, at the
/// canvas's resolution, placed in the window by the presentation) and the UI view (the window's resolution, in UI units),
/// drawn over it. Positions convert between window pixels, canvas pixels and UI units, and the pointer is given in each
/// view; the UI view gets it first (<see cref="PointerOnUi"/>).
/// </summary>
public sealed class GameViews
{
    private readonly Game _game;

    internal GameViews(Game game) => _game = game;

    private IScreenView? Screen => _game.TryGetCapability(out IScreenView? view) ? view : null;

    /// <summary>The window's size in pixels; the canvas's size where the platform has no UI view.</summary>
    public Vector2 ScreenSize => Screen is { } s ? new(s.ScreenPixels.Width, s.ScreenPixels.Height) : CanvasSize;
    /// <summary>The canvas's size in pixels (the world view's resolution).</summary>
    public Vector2 CanvasSize => Screen is { } s ? new(s.CanvasSize.Width, s.CanvasSize.Height) : new(_game.Width, _game.Height);
    /// <summary>Where the canvas lands in the window, in window pixels.</summary>
    public RectF WorldFrame => Screen?.WorldFrame ?? new RectF(0, 0, CanvasSize.X, CanvasSize.Y);
    /// <summary>Window pixels per UI unit.</summary>
    public float UiScale => Screen?.UiScale ?? 1f;
    /// <summary>The UI view's size in UI units.</summary>
    public Vector2 UiSize => Screen is null ? CanvasSize : ScreenSize / UiScale;
    /// <summary>Whether the platform draws the UI view at the window's resolution (false: the UI view is the canvas).</summary>
    public bool HasScreenView => Screen is not null;

    /// <summary>A window position (pixels) in canvas pixels.</summary>
    public Vector2 ScreenToCanvas(Vector2 screen)
    {
        var f = WorldFrame;
        var size = CanvasSize;
        return new((screen.X - f.X) * size.X / Math.Max(1e-6f, f.Width), (screen.Y - f.Y) * size.Y / Math.Max(1e-6f, f.Height));
    }

    /// <summary>A canvas position in window pixels.</summary>
    public Vector2 CanvasToScreen(Vector2 canvas)
    {
        var f = WorldFrame;
        var size = CanvasSize;
        return new(f.X + canvas.X * f.Width / size.X, f.Y + canvas.Y * f.Height / size.Y);
    }

    /// <summary>A window position (pixels) in UI units.</summary>
    public Vector2 ScreenToUi(Vector2 screen) => Screen is null ? screen : screen / UiScale;
    /// <summary>A UI position in window pixels.</summary>
    public Vector2 UiToScreen(Vector2 ui) => Screen is null ? ui : ui * UiScale;
    /// <summary>A canvas position in UI units (to place a label over a world object).</summary>
    public Vector2 CanvasToUi(Vector2 canvas) => Screen is null ? canvas : ScreenToUi(CanvasToScreen(canvas));
    /// <summary>A UI position in canvas pixels.</summary>
    public Vector2 UiToCanvas(Vector2 ui) => Screen is null ? ui : ScreenToCanvas(UiToScreen(ui));

    /// <summary>The pointer in UI units.</summary>
    public PointerState UiPointer => _game.Input.Pointer with { Position = CanvasToUi(_game.Input.Pointer.Position) };

    /// <summary>
    /// Whether the pointer is over the UI of a visible scene (<see cref="Scenes.Scene.UiContains"/>): the UI view gets the
    /// pointer first, and <see cref="WorldPointer"/> is then not present.
    /// </summary>
    public bool PointerOnUi
    {
        get
        {
            var pointer = UiPointer;
            return pointer.Present && _game.Scenes.VisibleUiContains(pointer.Position);
        }
    }

    /// <summary>
    /// The pointer in the world of <paramref name="camera"/> (canvas pixels without one); not present while it is over the
    /// UI (<see cref="PointerOnUi"/>).
    /// </summary>
    public PointerState WorldPointer(Camera2D? camera = null)
    {
        var pointer = _game.Input.Pointer;
        if (PointerOnUi) return pointer with { Present = false };
        return camera is null ? pointer : pointer with { Position = camera.ScreenToWorld(pointer.Position) };
    }
}

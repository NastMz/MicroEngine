using System.Numerics;
using MicroEngine.Input;

namespace MicroEngine.Raylib;

/// <summary>
/// The browser page's way into the raylib platform's touch screen (<see cref="RaylibTouchScreen"/>): the page listens to
/// Pointer Events on the canvas and forwards touch and pen pointers here, in canvas pixels, with the page's safe-area
/// insets (CSS <c>env(safe-area-inset-*)</c>) and density. The browser host (src/MicroEngine.Cli/Hosts/web) calls these from its JS exports.
/// </summary>
public static class BrowserTouch
{
    /// <summary>Phase: 0 began, 1 moved, 2 ended, 3 cancelled. Position in canvas (render) pixels.</summary>
    public static void Pointer(long id, int phase, float x, float y) =>
        RaylibTouchScreen.Feed.Push(new TouchEvent(id, (TouchPhase)Math.Clamp(phase, 0, 3), new Vector2(x, y)));

    /// <summary>The screen facts the page knows: safe-area insets in canvas pixels, canvas pixels per CSS pixel, and whether touch is the main input.</summary>
    public static void Screen(float left, float top, float right, float bottom, float pixelsPerDp, bool touchFirst)
    {
        var feed = RaylibTouchScreen.Feed;
        feed.Insets = (Math.Max(0, left), Math.Max(0, top), Math.Max(0, right), Math.Max(0, bottom));
        feed.PixelsPerDp = pixelsPerDp;
        feed.TouchFirst = touchFirst;
    }

    /// <summary>How the page vibrates (navigator.vibrate); null where it cannot.</summary>
    public static Func<int, bool>? Vibrator
    {
        get => RaylibTouchScreen.Feed.Vibrator;
        set => RaylibTouchScreen.Feed.Vibrator = value;
    }
}

/// <summary>The page's vibration (navigator.vibrate through the "microengine" module), used for touch-control haptics.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
internal static partial class BrowserHaptics
{
    [System.Runtime.InteropServices.JavaScript.JSImport("haptics.vibrate", "microengine")]
    public static partial bool Vibrate(int milliseconds);
}

/// <summary>The page's visibility: a hidden page (another tab, the phone locked or switched away) suspends the game.</summary>
public static class BrowserLifecycle
{
    private static volatile bool _hidden;

    /// <summary>Set by the page from <c>visibilitychange</c>; the raylib platform reports it as a suspension (<see cref="Platform.IEngineEventSink.Suspending"/>).</summary>
    public static bool Hidden
    {
        get => _hidden;
        set => _hidden = value;
    }
}

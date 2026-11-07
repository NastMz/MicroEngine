using System.Numerics;
using MicroEngine.Input;

namespace MicroEngine.Sdl;

/// <summary>
/// One browser page's way into the SDL platform it hosts. SDL reads the keyboard, the mouse and gamepads from the page
/// itself; what it cannot know comes from here: the page listens to Pointer Events on the canvas and forwards touch and
/// pen pointers, in canvas pixels, with the page's safe-area insets (CSS <c>env(safe-area-inset-*)</c>) and density,
/// its visibility and how it vibrates. The browser host (src/MicroEngine.Cli/Hosts/web) owns one and calls these from
/// its JavaScript exports; every platform it builds (<see cref="SdlPlatform.BrowserProfile"/>) reads this page and no
/// other, so nothing is shared through the process.
/// </summary>
public sealed class BrowserPage
{
    private volatile bool _hidden;

    internal PageFeed Feed { get; } = new();

    /// <summary>A touch or pen pointer. Phase: 0 began, 1 moved, 2 ended, 3 cancelled. Position in canvas (render) pixels.</summary>
    public void Pointer(long id, int phase, float x, float y) =>
        Feed.Push(new TouchEvent(id, (TouchPhase)Math.Clamp(phase, 0, 3), new Vector2(x, y)));

    /// <summary>The screen facts the page knows: safe-area insets in canvas pixels, canvas pixels per CSS pixel, and whether touch is the main input.</summary>
    public void Screen(float left, float top, float right, float bottom, float pixelsPerDp, bool touchFirst)
    {
        Feed.Insets = (Math.Max(0, left), Math.Max(0, top), Math.Max(0, right), Math.Max(0, bottom));
        Feed.PixelsPerDp = pixelsPerDp;
        Feed.TouchFirst = touchFirst;
    }

    /// <summary>
    /// Whether the page is hidden (another tab, the phone locked or switched away), set by the page from
    /// <c>visibilitychange</c>; the SDL platform reports it as a suspension (<see cref="Platform.IEngineEventSink.Suspending"/>).
    /// </summary>
    public bool Hidden
    {
        get => _hidden;
        set => _hidden = value;
    }

    /// <summary>How the page vibrates (navigator.vibrate) for a number of milliseconds; null where it cannot.</summary>
    public Func<int, bool>? Vibrator
    {
        get => Feed.Vibrator;
        set => Feed.Vibrator = value;
    }

    /// <summary>
    /// The text of a paste on the page (its <c>paste</c> event): what the game's clipboard reads from now on, because a
    /// page cannot read the system clipboard without asking. The paste's key press reaches the game after it.
    /// </summary>
    public void Paste(string text) => Feed.Clipboard = text ?? throw new ArgumentNullException(nameof(text));

    /// <summary>How the page writes text to the system clipboard (navigator.clipboard); null where it cannot.</summary>
    public Func<string, bool>? ClipboardWriter
    {
        get => Feed.ClipboardWriter;
        set => Feed.ClipboardWriter = value;
    }
}

/// <summary>
/// A browser page's clipboard: reading gives the text of the page's latest paste, or of the game's latest copy;
/// writing also puts the text on the system clipboard through the page, when the browser allows it.
/// </summary>
internal sealed class PageClipboard(PageFeed feed) : IClipboard
{
    public string? GetText() => feed.Clipboard;

    public bool SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        feed.Clipboard = text;
        return feed.ClipboardWriter?.Invoke(text) ?? true;
    }
}

/// <summary>Touch events and screen facts handed to the SDL platform by its page.</summary>
internal sealed class PageFeed
{
    private readonly List<TouchEvent> _pending = [];
    private readonly Lock _lock = new();
    public (float Left, float Top, float Right, float Bottom) Insets;
    public float PixelsPerDp;
    public bool TouchFirst;
    public Func<int, bool>? Vibrator;
    public volatile string? Clipboard;
    public Func<string, bool>? ClipboardWriter;

    public void Push(TouchEvent e)
    {
        lock (_lock) _pending.Add(e);
    }

    public void Drain(List<TouchEvent> into)
    {
        lock (_lock)
        {
            into.AddRange(_pending);
            _pending.Clear();
        }
    }

    public void Clear()
    {
        lock (_lock) _pending.Clear();
    }
}

/// <summary>The page's vibration (navigator.vibrate through the "microengine" module), used for touch-control haptics.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
internal static partial class BrowserHaptics
{
    [System.Runtime.InteropServices.JavaScript.JSImport("haptics.vibrate", "microengine")]
    public static partial bool Vibrate(int milliseconds);
}

/// <summary>The page's clipboard writing (navigator.clipboard through the "microengine" module).</summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
internal static partial class BrowserClipboard
{
    [System.Runtime.InteropServices.JavaScript.JSImport("clipboard.write", "microengine")]
    public static partial bool Write(string text);
}

using System.Diagnostics.CodeAnalysis;
using MicroEngine.Kernel;

namespace MicroEngine.Platform;

/// <summary>A monotonic clock in seconds: it never goes back and does not depend on frames, steps or rates.</summary>
public interface IMonotonicClock
{
    /// <summary>Seconds since an arbitrary start (the host's creation); never decreases.</summary>
    double Now { get; }
}

/// <summary>
/// Where a host delivers what happens to the game from outside its simulation (<see cref="IPlatformHost.PumpEvents"/>).
/// The engine's loop implements it and raises the matching <see cref="IEngineLifecycle"/> events.
/// </summary>
public interface IEngineEventSink
{
    /// <summary>The main surface changed its size in pixels.</summary>
    void Resized(SurfaceResized size);
    /// <summary>The game's window gained (true) or lost (false) the input focus.</summary>
    void FocusChanged(bool focused);
    /// <summary>The system is taking the game away (a phone app sent to the background, a hidden page).</summary>
    void Suspending();
    /// <summary>A suspended game came back.</summary>
    void Resumed();
    /// <summary>Something outside the game asked it to quit (the window's close button, the system).</summary>
    void QuitRequested();
}

/// <summary>
/// The platform a game runs on, as the loop sees it: a monotonic clock, the events of the window and the system, the
/// main surface, optional features found by type and whether the host may block its thread.
/// </summary>
/// <remarks>
/// <para>The platform contract, which every host follows:</para>
/// <list type="number">
/// <item><b>One thread.</b> The loop calls every member on the game's thread. A host that learns of an event on another
/// thread (a phone's lifecycle callbacks) holds it until the next <see cref="PumpEvents"/>.</item>
/// <item><b>Events are delivered only by <see cref="PumpEvents"/></b>, which the loop calls once at the start of every
/// frame, in the order they happened. Nothing is delivered between pumps.</item>
/// <item><b>Lifecycle order.</b> <see cref="IEngineEventSink.Suspending"/> and <see cref="IEngineEventSink.Resumed"/>
/// alternate, starting with <c>Suspending</c>: a host never sends two in a row, and never <c>Resumed</c> without a
/// <c>Suspending</c> before it. A trip to the background and back that happened between two pumps is delivered as both,
/// in that order, in one pump. <see cref="IEngineEventSink.FocusChanged"/> is sent only when the focus changes.
/// <see cref="IEngineEventSink.QuitRequested"/> is sent once per request; the loop decides (handlers may veto it), and
/// a later request is sent again.</item>
/// <item><b>Resizes.</b> When the main surface's pixel size changes, the same pump raises <see cref="ISurface.Resized"/>
/// and delivers <see cref="IEngineEventSink.Resized"/> with the new <see cref="ISurface.PixelSize"/>; a size that did
/// not change is not delivered.</item>
/// <item><b>Clock.</b> <see cref="Clock"/> is monotonic and nothing else: it is never derived from frames, steps or the
/// update rate. The loop owns time.</item>
/// <item><b>Capabilities.</b> Optional features (changing the display mode, diagnostics, a touch screen, display
/// pacing, a precise timer) are found with <see cref="TryGetCapability{T}"/>, never by casting the host. A host's
/// capabilities do not change while it runs (a capability's values may). A host that wraps another forwards the
/// query.</item>
/// <item><b>Page-driven hosts.</b> A host whose frames are driven by a page (a browser's animation frames) reports
/// <see cref="CanBlock"/> false and then never blocks: no waits, sleeps or precise delays in any member; the page paces
/// the frames. It reads the surface size from the canvas after start, never from the size requested at creation. It
/// opens the audio output inside the user gesture that starts the game.</item>
/// <item><b>No time coupling.</b> Nothing a host does depends on how the loop advances time: display pacing, device
/// start-up and audio output are explicit options of the host's creation.</item>
/// </list>
/// </remarks>
public interface IPlatformHost : IDisposable
{
    /// <summary>The host's monotonic clock.</summary>
    IMonotonicClock Clock { get; }

    /// <summary>
    /// Processes the system's pending events (window, keyboard, gamepad, touch) and delivers those the engine handles
    /// to <paramref name="sink"/>, in the order they happened. Called by the loop once at the start of every frame.
    /// </summary>
    /// <returns>True when the game can run a frame now; false while it is suspended (between <c>Suspending</c> and <c>Resumed</c>).</returns>
    bool PumpEvents(IEngineEventSink sink);

    /// <summary>The surface the game presents to (its window or canvas), or null when the host has none.</summary>
    ISurface? MainSurface { get; }

    /// <summary>The optional feature of type <typeparamref name="T"/> this host offers, if it offers one.</summary>
    bool TryGetCapability<T>([NotNullWhen(true)] out T? capability) where T : class;

    /// <summary>
    /// Whether the host may block its thread to pace frames (a desktop window, a phone, headless runs). False for
    /// page-driven hosts (a browser), which must never block: the loop then skips a frame that comes early instead of
    /// waiting for it.
    /// </summary>
    bool CanBlock { get; }
}

/// <summary>
/// The refresh rate a host's frames are paced to, when a display paces them (VSync). A capability
/// (<see cref="IPlatformHost.TryGetCapability{T}"/>): a host without it never paces frames to a display.
/// </summary>
public interface IDisplayPacing
{
    /// <summary>
    /// The refresh rate in Hz of the display that paces the frames, or 0 when frames are not paced to a display right
    /// now or its rate is unknown. The loop then counts elapsed time in refresh periods.
    /// </summary>
    double DisplayRate { get; }
}

/// <summary>
/// A precise way to wait, used by the loop to hold a frame back until the frame limit allows it. A capability
/// (<see cref="IPlatformHost.TryGetCapability{T}"/>) of hosts that can block (<see cref="IPlatformHost.CanBlock"/>);
/// without it the loop sleeps and then spins for the last milliseconds.
/// </summary>
public interface IPreciseDelay
{
    /// <summary>Blocks for about <paramref name="seconds"/>, precisely enough to pace frames.</summary>
    void Delay(double seconds);
}

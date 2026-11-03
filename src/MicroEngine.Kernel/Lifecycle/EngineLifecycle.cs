namespace MicroEngine.Kernel;

/// <summary>The new size of the drawing surface, in pixels (<see cref="IEngineLifecycle.Resized"/>).</summary>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
public readonly record struct SurfaceResized(int Width, int Height);

/// <summary>
/// A request to quit that came from outside the game (the window's close button, the system), passed to
/// <see cref="IEngineLifecycle.QuitRequested"/>. A handler that must keep the game running (unsaved work, a confirmation
/// dialog) calls <see cref="Veto"/>; the game can quit later on its own.
/// </summary>
public sealed class QuitRequest
{
    /// <summary>Whether a handler vetoed the request.</summary>
    public bool Vetoed { get; private set; }
    /// <summary>Keeps the game running.</summary>
    public void Veto() => Vetoed = true;
}

/// <summary>
/// Engine events: the things that happen to a running game from outside its simulation. Raised on the game's thread,
/// between frames (before the frame that follows them), in the order they happened; handlers run in the order they
/// subscribed.
/// </summary>
public interface IEngineLifecycle
{
    /// <summary>The drawing surface changed size. Raised by hosts that report it.</summary>
    event Action<SurfaceResized>? Resized;
    /// <summary>The game's window gained (true) or lost (false) the input focus. Raised by hosts that report it.</summary>
    event Action<bool>? FocusChanged;
    /// <summary>The system is taking the game away (a phone app sent to the background); nothing runs until <see cref="Resumed"/>.</summary>
    event Action? Suspending;
    /// <summary>A suspended game came back; raised before its next frame.</summary>
    event Action? Resumed;
    /// <summary>Something outside the game asked it to quit; a handler may <see cref="QuitRequest.Veto"/> it.</summary>
    event Action<QuitRequest>? QuitRequested;
}

/// <summary>
/// The engine's <see cref="IEngineLifecycle"/>: subscribers read it through the interface; the loop and hosts raise
/// the events through the <c>Raise</c> methods.
/// </summary>
public sealed class EngineLifecycle : IEngineLifecycle
{
    /// <inheritdoc/>
    public event Action<SurfaceResized>? Resized;
    /// <inheritdoc/>
    public event Action<bool>? FocusChanged;
    /// <inheritdoc/>
    public event Action? Suspending;
    /// <inheritdoc/>
    public event Action? Resumed;
    /// <inheritdoc/>
    public event Action<QuitRequest>? QuitRequested;

    /// <summary>Raises <see cref="Resized"/>.</summary>
    public void RaiseResized(SurfaceResized size) => Resized?.Invoke(size);
    /// <summary>Raises <see cref="FocusChanged"/>.</summary>
    public void RaiseFocusChanged(bool focused) => FocusChanged?.Invoke(focused);
    /// <summary>Raises <see cref="Suspending"/>.</summary>
    public void RaiseSuspending() => Suspending?.Invoke();
    /// <summary>Raises <see cref="Resumed"/>.</summary>
    public void RaiseResumed() => Resumed?.Invoke();

    /// <summary>Raises <see cref="QuitRequested"/>; every handler runs even after a veto.</summary>
    /// <returns>True when the game should quit (no handler vetoed).</returns>
    public bool RaiseQuitRequested()
    {
        var request = new QuitRequest();
        QuitRequested?.Invoke(request);
        return !request.Vetoed;
    }
}

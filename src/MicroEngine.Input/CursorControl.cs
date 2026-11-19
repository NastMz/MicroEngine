namespace MicroEngine.Input;

/// <summary>
/// The mouse cursor over the game's window: shown or hidden, free or locked. A platform capability
/// (<c>Game.TryGetCapability</c>): platforms with no cursor to control (phones, a console) do not offer it, so a game
/// asks and goes on without it.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Visible"/> hides the system's arrow while it is over the window, for a game that draws its own cursor or
/// none. <see cref="SetRelative"/> locks the cursor: it disappears, cannot leave the window, and the mouse reports only
/// how far it moved (<see cref="PointerState.Delta"/>, or an action bound to <see cref="InputControl.PointerRight"/>
/// and its siblings) while <see cref="PointerState.Position"/> stays where it was. That is what looking around with the
/// mouse, or dragging a view without running out of desk, needs.
/// </para>
/// <para>
/// On desktops both take effect at once. In a browser a page may lock the cursor only in answer to a click or a key
/// press, and the player's Escape key always frees it: ask from the update in which the player clicked, and read
/// <see cref="Relative"/> afterwards instead of assuming it stayed on. The window losing the focus frees the cursor
/// everywhere; the lock comes back with the focus on desktops.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Hold the secondary button to look around; the cursor is back where it was when it is let go.
/// if (Game.TryGetCapability&lt;ICursorControl&gt;(out var cursor))
/// {
///     if (Input.Pressed("look")) cursor.SetRelative(true);
///     if (Input.Released("look")) cursor.SetRelative(false);
/// }
/// camera.Yaw += Input.Motion("look-left", "look-right", "look-up", "look-down").X * 0.2f;
/// </code>
/// </example>
public interface ICursorControl
{
    /// <summary>Whether the system's cursor is drawn while it is over the window (true at start). A locked cursor is never drawn.</summary>
    bool Visible { get; set; }
    /// <summary>Whether the cursor is locked to the window and the mouse reports motion only.</summary>
    bool Relative { get; }
    /// <summary>
    /// Locks (<paramref name="relative"/>) or frees the cursor. Returns whether the platform did it: false when the
    /// system refused (a browser outside a click or key press).
    /// </summary>
    bool SetRelative(bool relative);
}

/// <summary>
/// A cursor that only remembers what it was asked: the headless platform's, so a game's cursor handling runs in tests
/// and scripted runs.
/// </summary>
public sealed class MemoryCursor : ICursorControl
{
    /// <inheritdoc/>
    public bool Visible { get; set; } = true;
    /// <inheritdoc/>
    public bool Relative { get; private set; }
    /// <summary>Whether <see cref="SetRelative"/> succeeds (true by default); a test sets it to false to stand for a browser that refuses.</summary>
    public bool AllowsRelative { get; set; } = true;

    /// <inheritdoc/>
    public bool SetRelative(bool relative)
    {
        if (relative && !AllowsRelative) return false;
        Relative = relative;
        return true;
    }
}

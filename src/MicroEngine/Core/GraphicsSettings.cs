using MicroEngine.Platform;

namespace MicroEngine.Core;

/// <summary>How the game's window covers the screen (<see cref="GraphicsSettings.WindowMode"/>, <see cref="GameScreen.WindowMode"/>).</summary>
public enum WindowMode
{
    /// <summary>A window the player can move and resize (the default on desktop).</summary>
    Windowed,
    /// <summary>A borderless window covering its display at the display's current resolution: switching is instant and other windows can show above it.</summary>
    Fullscreen,
    /// <summary>
    /// Exclusive full screen at the display's desktop resolution and refresh rate, where the backend offers it (SDL and
    /// raylib on desktop); elsewhere the same as <see cref="Fullscreen"/>.
    /// </summary>
    ExclusiveFullscreen,
}

/// <summary>A display (monitor) as the system reports it.</summary>
/// <param name="Index">Position in <see cref="GameScreen.Displays"/>, used by <see cref="GraphicsSettings.Display"/> and <see cref="GameScreen.MoveToDisplay"/>.</param>
/// <param name="Name">The system's name for it ("LG FHD"); may be empty.</param>
/// <param name="Width">Width of its current mode in pixels.</param>
/// <param name="Height">Height of its current mode in pixels.</param>
/// <param name="RefreshRate">Refresh rate of its current mode in hertz; 0 when unknown.</param>
/// <param name="Scale">The system's content scale for it (1.5 for 150%).</param>
/// <param name="Primary">Whether it is the system's primary display.</param>
public sealed record DisplayInfo(int Index, string Name, int Width, int Height, double RefreshRate, float Scale, bool Primary);

/// <summary>
/// The graphics options a game starts with (<see cref="GameSettings.Graphics"/>). Every option has a sensible automatic
/// default; the launcher's options (<c>--platform</c>, <c>--renderer</c>, <c>--vsync</c>, <c>--fps-limit</c>, <c>--gpu</c>,
/// <c>--window</c>, <c>--display</c>) override them for one run, and <see cref="Game.Screen"/> changes the ones a platform
/// can change while the game runs. A game that offers a graphics menu saves <see cref="GameScreen.Current"/> and returns
/// it from <see cref="Game.Settings"/> at its next start. VSync, the frame limit and <see cref="GameSettings.UpdateRate"/>
/// are independent: VSync waits for the display, the limit caps frames per second, the update rate is the simulation's.
/// </summary>
public sealed record GraphicsSettings
{
    /// <summary>
    /// The platform to run on first ("sdl", "raylib", "sdl-software"...); null tries the catalog in order. A platform that
    /// cannot start here falls back to the next one, saying why; a platform named on the command line does not.
    /// Read at start only.
    /// </summary>
    public string? Backend { get; init; }
    /// <summary>
    /// Graphics APIs to try in order on platforms that offer several (SDL: "direct3d11", "direct3d12", "vulkan",
    /// "opengl", "metal", "gpu"...); after them, the platform chooses as when the list is empty (the default). Names the
    /// system lacks are skipped. Read at start only.
    /// </summary>
    public IReadOnlyList<string> Renderers { get; init; } = [];
    /// <summary>Whether frames wait for the display's refresh (true by default): no tearing, and frames paced to the display.</summary>
    public bool VSync { get; init; } = true;
    /// <summary>
    /// The most frames per second to draw; 0 (the default) sets no limit. Independent of VSync: with VSync off it paces
    /// frames on its own; with VSync on, a limit below the display's rate skips refreshes (choose a divisor of the
    /// display's rate, or draw with <see cref="Game.FrameBlend"/>). Not applied to fixed-step (scripted) runs.
    /// </summary>
    public int FrameRateLimit { get; init; }
    /// <summary>Which GPU to ask for on a machine with two (<see cref="GpuPreference"/>). Read at start only.</summary>
    public GpuPreference Gpu { get; init; }
    /// <summary>Windowed (the default), borderless full screen, or exclusive full screen. Phones and browsers decide this themselves.</summary>
    public WindowMode WindowMode { get; init; }
    /// <summary>The display to open on (an index of <see cref="GameScreen.Displays"/>); null lets the system choose. An index the system does not have is ignored.</summary>
    public int? Display { get; init; }
}

/// <summary>
/// What a platform can change about its window and presentation while the game runs (<see cref="Game.Screen"/> uses it).
/// A capability (<see cref="IPlatformHost.TryGetCapability{T}"/>): platforms without a window (headless) or whose host
/// owns these choices (browsers, phones) do not offer it, or report that a change did not apply.
/// </summary>
public interface IDisplayControl
{
    /// <summary>The displays the system reports, in its order.</summary>
    IReadOnlyList<DisplayInfo> Displays { get; }
    /// <summary>Index in <see cref="Displays"/> of the display the window is on; -1 when unknown.</summary>
    int CurrentDisplay { get; }
    /// <summary>Whether frames wait for the display's refresh.</summary>
    bool VSync { get; }
    /// <summary>Turns VSync on or off; returns whether <see cref="VSync"/> now equals <paramref name="on"/>.</summary>
    bool SetVSync(bool on);
    /// <summary>The window's current mode.</summary>
    WindowMode WindowMode { get; }
    /// <summary>Changes the window's mode; returns whether it applied.</summary>
    bool SetWindowMode(WindowMode mode);
    /// <summary>Moves the window (and full screen) to display <paramref name="index"/>; returns whether it applied.</summary>
    bool MoveToDisplay(int index);
}

/// <summary>
/// The game's screen while it runs (<see cref="Game.Screen"/>): the displays, which one the game is on, and the options
/// that can change without restarting. Changes apply where the platform allows; read a property back to see what applies
/// (a browser or phone keeps its own window, a headless run has none).
/// </summary>
public sealed class GameScreen
{
    private readonly IDisplayControl? _control;
    private readonly IDisplayPacing? _pacing;
    private readonly IPlatformDiagnostics? _diagnostics;
    private readonly IPlatform _platform;
    private readonly GraphicsSettings _start;
    private int _frameRateLimit;

    internal GameScreen(IPlatform platform, GraphicsSettings start)
    {
        _platform = platform;
        platform.TryGetCapability(out _control);
        platform.TryGetCapability(out _pacing);
        platform.TryGetCapability(out _diagnostics);
        _start = start;
        _frameRateLimit = Math.Max(0, start.FrameRateLimit);
    }

    /// <summary>Whether this platform can change the window and VSync while running (false headless, in browsers and on phones).</summary>
    public bool CanChange => _control is not null;
    /// <summary>The displays the system reports; empty where the platform cannot tell.</summary>
    public IReadOnlyList<DisplayInfo> Displays => _control?.Displays ?? [];
    /// <summary>The display the game is on, or null when unknown.</summary>
    public DisplayInfo? Display => _control is { CurrentDisplay: >= 0 and var i } && i < Displays.Count ? Displays[i] : null;
    /// <summary>
    /// The refresh rate frames are paced to (the display's while VSync is on), in hertz; 0 when nothing paces them to a
    /// display (VSync off, scripted and headless runs).
    /// </summary>
    public double RefreshRate => _pacing?.DisplayRate ?? 0;

    /// <summary>Whether frames wait for the display's refresh. Setting it applies where <see cref="CanChange"/>; read it back.</summary>
    public bool VSync
    {
        get => _control?.VSync ?? RefreshRate > 0;
        set => _control?.SetVSync(value);
    }

    /// <summary>The window's mode. Setting it applies where <see cref="CanChange"/>; read it back.</summary>
    public WindowMode WindowMode
    {
        get => _control?.WindowMode ?? WindowMode.Fullscreen;
        set => _control?.SetWindowMode(value);
    }

    /// <summary>The most frames per second to draw; 0 for no limit. Applies at once on every platform that runs in real time.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int FrameRateLimit
    {
        get => _frameRateLimit;
        set => _frameRateLimit = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "The frame limit cannot be negative.");
    }

    /// <summary>
    /// Switches between a window and full screen: through the platform's display control where it has one, otherwise by
    /// asking the platform (<see cref="IPlatform.ToggleFullscreen"/>; the headless platform counts the request). The
    /// engine calls it when the full-screen action is pressed (<see cref="Input.ActionNames.Fullscreen"/>).
    /// </summary>
    /// <returns>Whether the window's mode changed through the display control.</returns>
    public bool ToggleFullscreen()
    {
        if (_control is { } control) return control.SetWindowMode(control.WindowMode == WindowMode.Windowed ? WindowMode.Fullscreen : WindowMode.Windowed);
        _platform.ToggleFullscreen();
        return false;
    }

    /// <summary>Moves the game to display <paramref name="index"/> of <see cref="Displays"/>; returns whether it applied.</summary>
    public bool MoveToDisplay(int index) => _control?.MoveToDisplay(index) ?? false;

    /// <summary>The graphics API in use ("direct3d11", "opengl"...) where the platform reports it; null otherwise.</summary>
    public string? Renderer => Fact("renderer");
    /// <summary>The graphics adapter (GPU) in use where the platform can identify it; null otherwise.</summary>
    public string? Adapter => Fact("adapter");

    private string? Fact(string key) => _diagnostics is { } d && d.Diagnostics.TryGetValue(key, out var v) ? v : null;

    /// <summary>
    /// The options in effect, to save and return from <see cref="Game.Settings"/> at the next start: the start-only
    /// options as the game started, the others as they are now.
    /// </summary>
    public GraphicsSettings Current => _start with
    {
        VSync = VSync,
        FrameRateLimit = FrameRateLimit,
        WindowMode = CanChange ? WindowMode : _start.WindowMode,
        Display = Display?.Index ?? _start.Display,
    };
}

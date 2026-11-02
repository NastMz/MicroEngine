using MicroEngine.Audio;
using MicroEngine.Graphics;
using MicroEngine.Input;

namespace MicroEngine.Core;

/// <summary>Window/backbuffer settings a game declares. The virtual size is the pixel-art canvas.</summary>
/// <param name="Title">Window title and application name shown by the system.</param>
/// <param name="VirtualWidth">Width of the virtual canvas in pixels (320 by default). Everything is drawn in these pixels and scaled to the window.</param>
/// <param name="VirtualHeight">Height of the virtual canvas in pixels (180 by default).</param>
/// <param name="Scale">Initial window size as a multiple of the virtual size on desktop (4 by default); the launcher's <c>--scale</c> overrides it.</param>
public sealed record GameSettings(string Title, int VirtualWidth = 320, int VirtualHeight = 180, int Scale = 4)
{
    /// <summary>Fixed simulation rate in updates per second.</summary>
    public int UpdateRate { get; init; } = 60;
    /// <summary>Colour the canvas is cleared to before the scenes draw each frame (black by default).</summary>
    public Color Background { get; init; } = Color.Black;
    /// <summary>Colour of the window around the frame when the window's shape differs from the canvas (black by default).</summary>
    public Color Letterbox { get; init; } = Color.Black;
    /// <summary>Graphics options: platform, graphics API, VSync, frame limit, GPU, window mode and display (all automatic by default).</summary>
    public GraphicsSettings Graphics { get; init; } = new();
}

/// <summary>
/// Facts a platform can report about itself for logs and profiles: the graphics API in use, VSync, the graphics adapter
/// when the backend can name it. Keys are short lower-case words; values are free text.
/// </summary>
public interface IPlatformDiagnostics
{
    /// <summary>The facts, read when asked (a value may change, such as the display's refresh rate).</summary>
    IReadOnlyDictionary<string, string> Diagnostics { get; }
}

/// <summary>
/// Everything a backend provides: a window (or nothing, headless), input devices, audio,
/// texture creation and a renderer that draws into the virtual canvas.
/// </summary>
public interface IPlatform : IDisposable
{
    /// <summary>Creates textures.</summary>
    IGraphicsDevice Graphics { get; }
    /// <summary>Plays sound; a silent device when there is no audio.</summary>
    IAudioDevice Audio { get; }
    /// <summary>Keyboard, gamepad and pointer state, read once per update.</summary>
    IInputSource Input { get; }
    /// <summary>Whether the user closed the window; the loop stops at the next frame.</summary>
    bool CloseRequested { get; }
    /// <summary>Monotonic time in seconds.</summary>
    double Now { get; }
    /// <summary>Whether time advances with the wall clock (desktop) or one step per frame (headless).</summary>
    bool RealTime { get; }
    /// <summary>
    /// The refresh rate in Hz of the display that paces the frames (VSync), or 0 when frames are not paced to a display
    /// or its rate is unknown (the default). The loop then counts elapsed time in refresh periods, which removes the
    /// stutter of waking up irregularly around them.
    /// </summary>
    double DisplayRate => 0;
    /// <summary>
    /// Whether the system took the game away (a phone app sent to the background): while true the loop neither
    /// simulates nor renders, music is paused and the game is told through <c>OnSuspended</c>/<c>OnResumed</c>.
    /// Platforms without such a state keep the default.
    /// </summary>
    bool Suspended => false;
    /// <summary>
    /// Blocks for about <paramref name="seconds"/>, precisely enough to pace frames (the frame limit,
    /// <see cref="GraphicsSettings.FrameRateLimit"/>); returns false when the platform cannot block its thread (a browser,
    /// whose page drives the frames), and the loop then skips the frames that come early instead. The default sleeps and
    /// then spins for the last milliseconds; backends override it with their precise timers.
    /// </summary>
    bool Wait(double seconds)
    {
        FrameTimer.Delay(seconds);
        return true;
    }
    /// <summary>
    /// The touch screen, if the platform has one: touches for <see cref="VirtualControls"/>, the safe area and a layer
    /// drawn over the frame. Platforms without touch keep the default (null).
    /// </summary>
    ITouchScreen? Touch => null;
    /// <summary>Processes pending system events (window, keyboard, gamepad, touch). Called at the start of every frame.</summary>
    void PollEvents();
    /// <summary>Starts a frame and returns the renderer that draws into the virtual canvas.</summary>
    IRenderer BeginFrame();
    /// <summary>Finishes the frame and presents it.</summary>
    void EndFrame();
    /// <summary>Writes the most recently completed virtual frame as a PNG.</summary>
    void SaveScreenshot(string path);
    /// <summary>Switches between window and full screen where the platform has a window; does nothing elsewhere.</summary>
    void ToggleFullscreen();
    /// <summary>
    /// Colour of the window around the frame. Starts as <see cref="GameSettings.Letterbox"/>; the start screen sets its
    /// own colour while it shows. Platforms without a window keep the default, which ignores changes.
    /// </summary>
    Color Letterbox { get => Color.Black; set { } }
}

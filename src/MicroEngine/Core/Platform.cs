using MicroEngine.Audio;
using MicroEngine.Graphics;
using MicroEngine.Input;

namespace MicroEngine.Core;

/// <summary>Window/backbuffer settings a game declares. The virtual size is the pixel-art canvas.</summary>
public sealed record GameSettings(string Title, int VirtualWidth = 320, int VirtualHeight = 180, int Scale = 4)
{
    /// <summary>Fixed simulation rate in updates per second.</summary>
    public int UpdateRate { get; init; } = 60;
    public Color Background { get; init; } = Color.Black;
}

/// <summary>
/// Everything a backend provides: a window (or nothing, headless), input devices, audio,
/// texture creation and a renderer that draws into the virtual canvas.
/// </summary>
public interface IPlatform : IDisposable
{
    IGraphicsDevice Graphics { get; }
    IAudioDevice Audio { get; }
    IInputSource Input { get; }
    bool CloseRequested { get; }
    /// <summary>Monotonic time in seconds.</summary>
    double Now { get; }
    /// <summary>Whether time advances with the wall clock (desktop) or one step per frame (headless).</summary>
    bool RealTime { get; }
    /// <summary>
    /// Whether the system took the game away (a phone app sent to the background): while true the loop neither
    /// simulates nor renders, music is paused and the game is told through <c>OnSuspended</c>/<c>OnResumed</c>.
    /// Platforms without such a state keep the default.
    /// </summary>
    bool Suspended => false;
    void PollEvents();
    IRenderer BeginFrame();
    void EndFrame();
    /// <summary>Writes the most recently completed virtual frame as a PNG.</summary>
    void SaveScreenshot(string path);
    void ToggleFullscreen();
}

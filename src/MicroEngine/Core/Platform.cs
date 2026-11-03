using MicroEngine.Audio;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Platform;

namespace MicroEngine.Core;

/// <summary>
/// Window/backbuffer settings a game declares. The virtual size is the canvas everything is drawn on: 320×180 pixel art
/// by default, or a high-resolution canvas such as 1280×720 or 1920×1080 with <see cref="CanvasScaling.Smooth"/>.
/// </summary>
/// <param name="Title">Window title and application name shown by the system.</param>
/// <param name="VirtualWidth">Width of the virtual canvas in pixels (320 by default). Everything is drawn in these pixels and scaled to the window.</param>
/// <param name="VirtualHeight">Height of the virtual canvas in pixels (180 by default).</param>
/// <param name="Scale">
/// Initial window size as a multiple of the virtual size on desktop (4 by default); the launcher's <c>--scale</c> overrides
/// it. A window that would not fit the display opens at the largest size that does.
/// </param>
public sealed record GameSettings(string Title, int VirtualWidth = 320, int VirtualHeight = 180, int Scale = 4)
{
    /// <summary>
    /// How the canvas is scaled to the window: <see cref="CanvasScaling.Pixel"/> (the default: whole factors, nearest
    /// sampling) for pixel art, <see cref="CanvasScaling.Smooth"/> (fills the window, linear sampling) for
    /// high-resolution art.
    /// </summary>
    public CanvasScaling Scaling { get; init; } = CanvasScaling.Pixel;
    /// <summary>
    /// The filter of textures created without one (images, atlases). Null (the default) follows
    /// <see cref="Scaling"/>: nearest for pixel art, linear for smooth scaling. Each texture can still choose its own.
    /// Glyph pages of TTF/OTF fonts do not follow it: their filter is <see cref="FontOptions.Filter"/>, or linear for
    /// antialiased text and nearest otherwise.
    /// </summary>
    public TextureFilter? TextureFilter { get; init; }
    /// <summary>The filter textures get when they do not choose one: <see cref="TextureFilter"/>, or the one <see cref="Scaling"/> implies.</summary>
    public TextureFilter DefaultTextureFilter => TextureFilter ?? (Scaling == CanvasScaling.Smooth ? MicroEngine.Graphics.TextureFilter.Linear : MicroEngine.Graphics.TextureFilter.Nearest);
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
/// when the backend can name it. Keys are short lower-case words; values are free text. A capability
/// (<see cref="IPlatformHost.TryGetCapability{T}"/>).
/// </summary>
public interface IPlatformDiagnostics
{
    /// <summary>The facts, read when asked (a value may change, such as the display's refresh rate).</summary>
    IReadOnlyDictionary<string, string> Diagnostics { get; }
}

/// <summary>
/// Everything a backend provides: the host (<see cref="IPlatformHost"/>: clock, events, main surface, capabilities,
/// whether it may block) plus, until the graphics, input and audio contracts are split from it, the renderer that draws
/// into the virtual canvas and the graphics, input and audio devices.
/// </summary>
/// <remarks>
/// Optional features are capabilities found with <see cref="IPlatformHost.TryGetCapability{T}"/>, never by casting a
/// platform: <see cref="IDisplayControl"/>, <see cref="IPlatformDiagnostics"/>, <see cref="ITouchScreen"/> (with its
/// vibration), <see cref="IDisplayPacing"/> and <see cref="IPreciseDelay"/>. A platform that wraps another forwards
/// every member, so wrapping never hides a capability.
/// </remarks>
public interface IPlatform : IPlatformHost
{
    /// <summary>Creates textures.</summary>
    IGraphicsDevice Graphics { get; }
    /// <summary>Plays sound; a silent device when there is no audio.</summary>
    IAudioDevice Audio { get; }
    /// <summary>Keyboard, gamepad and pointer state, read once per update.</summary>
    IInputSource Input { get; }
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
    /// own colour while it shows. Platforms without a window keep it and ignore it.
    /// </summary>
    Color Letterbox { get; set; }
}

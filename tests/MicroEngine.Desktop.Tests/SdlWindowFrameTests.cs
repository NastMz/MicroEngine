using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Kernel;
using MicroEngine.Platform;
using MicroEngine.Sdl;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// Where a desktop SDL window places the game's frame. The game draws into its canvas (a render target of the canvas's
/// size) and the frame is laid out while that target may still be bound, so the layout must measure the window, never
/// the bound target: measured on the canvas, the frame stays at scale 1 in the window's top-left corner.
/// </summary>
[TestClass]
[TestCategory("Native")]
public sealed class SdlWindowFrameTests
{
    private SdlPlatform? _platform;

    [TestInitialize]
    public void Open()
    {
        try { _platform = new SdlPlatform(new PlatformRequest(new GameSettings("frame test", 160, 90, 3)) { Hidden = true, WaitForDisplay = false, Mute = true }); }
        catch (InvalidOperationException e) { Native.Require(false, $"SDL cannot open a window here: {e.Message}"); }
    }

    [TestCleanup]
    public void Close() => _platform?.Dispose();

    [TestMethod]
    public void The_frame_fills_the_window_while_the_game_draws_into_its_canvas()
    {
        var screen = ((IScreenView)_platform!).ScreenPixels;
        Assert.IsTrue(screen.Width >= 320 && screen.Height >= 180, $"The window is larger than the canvas ({screen}).");
        _platform.BeginFrame(); // the game's canvas (160×90) is the render target until the frame is composed
        Assert.AreEqual(new Vector2(screen.Width, screen.Height), _platform.Touch.Size, "Measured on the window, not on the bound canvas.");
        Assert.AreEqual(new RectF(0, 0, screen.Width, screen.Height), _platform.Touch.SafeArea);
        var frame = ((IScreenView)_platform).WorldFrame;
        Assert.IsTrue(frame.Width >= 320 && frame.X + frame.Width <= screen.Width && frame.Y + frame.Height <= screen.Height,
            $"The frame is scaled to the window ({frame} in {screen}).");
        _platform.EndFrame();
    }
}

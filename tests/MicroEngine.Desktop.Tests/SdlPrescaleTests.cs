using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Graphics.Devices;
using MicroEngine.Sdl;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// A presentation with a prescale on an SDL window: the frame is composed through the enlarged canvas, and the capture of
/// what the player saw samples that same enlargement.
/// </summary>
[TestClass]
[TestCategory("Native")]
public sealed class SdlPrescaleTests
{
    private static readonly Color Red = new(255, 0, 0), Blue = new(0, 0, 255);

    /// <summary>The window of a 160 × 90 canvas, red on its left half and blue on its right, as the player saw it.</summary>
    private static PixelImage Screen(Presentation presentation)
    {
        SdlPlatform? platform = null;
        try { platform = new SdlPlatform(new PlatformRequest(new GameSettings("prescale test", 160, 90, 3) { Presentation = presentation }) { Hidden = true, WaitForDisplay = false, Mute = true }); }
        catch (InvalidOperationException e) { Native.Require(false, $"SDL cannot open a window here: {e.Message}"); }
        using (platform)
        {
            var world = platform!.BeginFrame();
            world.FillRect(new RectF(0, 0, 80, 90), Red);
            world.FillRect(new RectF(80, 0, 80, 90), Blue);
            ((IScreenView)platform).BeginUi(); // a frame with a UI view is captured as the window
            platform.EndFrame();
            return ((IViewCapture)platform).CaptureScreen();
        }
    }

    [TestMethod]
    public void The_capture_samples_the_enlarged_canvas_as_the_window_does()
    {
        var linear = Presentation.PixelArt with { Upscale = SamplerDescription.LinearClamp };
        var smooth = Screen(linear);
        Native.Require(smooth.Width == 480 && smooth.Height == 270, $"The window opened at {smooth.Width}x{smooth.Height}, not at three times the canvas.");
        var soft = Screen(linear with { Prescale = 2 });
        var sharp = Screen(linear with { Prescale = Presentation.AutoPrescale });

        // The edge between the halves is at window pixel 240. Linear alone blends pixel 239 a third of the way to blue;
        // enlarged twice first, a sixth; enlarged three times (the window's scale), not at all: the nearest picture.
        Assert.AreEqual(170, smooth[239, 45].R, 3);
        Assert.AreEqual(212, soft[239, 45].R, 3);
        Assert.AreEqual(Red, sharp[239, 45] with { A = 255 });
        Assert.AreEqual(Blue, sharp[240, 45] with { A = 255 });
        Assert.AreEqual(Red, soft[237, 45] with { A = 255 }, "two window pixels from the edge nothing is blended");
    }
}

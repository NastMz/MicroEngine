using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Graphics.Devices;
using MicroEngine.Graphics.SdlGpu;
using MicroEngine.Sdl;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// The picture an SDL_GPU window presents, read back from the device, against the capture of what the player saw: the
/// same frame whatever the presentation, prescale included.
/// </summary>
[TestClass]
[TestCategory("Native")]
public sealed class SdlGpuPresentedTests
{
    private static readonly Color Red = new(255, 0, 0), Blue = new(0, 0, 255);

    /// <summary>The window of a 160 × 90 canvas, red on its left half and blue on its right: as presented, and as captured.</summary>
    private static (PixelImage Presented, PixelImage Captured) Window(Presentation presentation)
    {
        var settings = new GameSettings("presented test", 160, 90, 3) { Presentation = presentation, Graphics = new GraphicsSettings { Device = SdlPlatform.GpuDevice } };
        SdlPlatform? platform = null;
        try { platform = new SdlPlatform(new PlatformRequest(settings) { Hidden = true, WaitForDisplay = false, Mute = true }); }
        catch (Exception e) when (e is InvalidOperationException or PlatformConfigurationException) { Native.Require(false, $"SDL_GPU cannot open a window here: {e.Message}"); }
        using (platform)
        {
            var world = platform!.BeginFrame();
            world.FillRect(new RectF(0, 0, 80, 90), Red);
            world.FillRect(new RectF(80, 0, 80, 90), Blue);
            ((IScreenView)platform).BeginUi().FillRect(new RectF(4, 4, 6, 6), Color.White);
            platform.EndFrame();
            var request = ((SdlGpuDevice)platform.Device).ReadPresented();
            Assert.IsNotNull(request, "a frame was composed");
            request.Wait();
            var (w, h) = (request.Region.Width, request.Region.Height);
            var pixels = request.Pixels;
            bool bgra = request.Format is TextureFormat.Bgra8Unorm;
            var presented = new PixelImage(w, h);
            for (int i = 0; i < w * h; i++)
                presented[i % w, i / w] = new Color(pixels[i * 4 + (bgra ? 2 : 0)], pixels[i * 4 + 1], pixels[i * 4 + (bgra ? 0 : 2)], 255);
            return (presented, ((IViewCapture)platform).CaptureScreen());
        }
    }

    [TestMethod]
    public void The_presented_window_is_the_capture_of_the_window_with_and_without_a_prescale()
    {
        var smooth = Presentation.PixelArt with { Upscale = SamplerDescription.LinearClamp };
        foreach (var (presentation, name) in new[] { (Presentation.PixelArt, "nearest"), (smooth, "linear"), (smooth with { Prescale = 2 }, "prescale 2"), (smooth with { Prescale = Presentation.AutoPrescale }, "auto prescale") })
        {
            var (presented, captured) = Window(presentation);
            Native.Require(presented.Width == 480 && presented.Height == 270, $"The window opened at {presented.Width}x{presented.Height}, not at three times the canvas.");
            Assert.AreEqual((presented.Width, presented.Height), (captured.Width, captured.Height), name);
            for (int y = 0; y < presented.Height; y++)
                for (int x = 0; x < presented.Width; x++)
                {
                    var (p, c) = (presented[x, y], captured[x, y]);
                    if (Math.Abs(p.R - c.R) > 2 || Math.Abs(p.G - c.G) > 2 || Math.Abs(p.B - c.B) > 2)
                        Assert.Fail($"{name}, pixel {x},{y}: {p} presented, {c} captured");
                }
            Assert.AreEqual(Color.White, presented[5, 5], $"{name}: the UI view is in the presented picture");
        }
        // The edge between the halves is at window pixel 240: pure with nearest, blended a third of the way with linear.
        Assert.AreEqual(Red, Window(Presentation.PixelArt).Presented[239, 45]);
        Assert.AreEqual(170, Window(smooth).Presented[239, 45].R, 3);
        Assert.AreEqual(212, Window(smooth with { Prescale = 2 }).Presented[239, 45].R, 3);
        Assert.AreEqual(Red, Window(smooth with { Prescale = Presentation.AutoPrescale }).Presented[239, 45]);
    }
}

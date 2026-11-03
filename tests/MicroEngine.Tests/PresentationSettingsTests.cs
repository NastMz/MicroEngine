using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Graphics.Devices;
using MicroEngine.Input;

namespace MicroEngine.Tests;

/// <summary>
/// The game's presentation (<see cref="GameSettings.Presentation"/>) replaces the single <see cref="CanvasScaling"/>
/// switch that decided five things at once: each choice is now a field, and <c>Scaling</c> only picks a preset.
/// </summary>
[TestClass]
public sealed class PresentationSettingsTests
{
    [TestMethod]
    public void Without_A_Presentation_Scaling_Picks_The_Preset()
    {
        Assert.AreSame(Presentation.PixelArt, new GameSettings("pixel").EffectivePresentation);
        Assert.AreSame(Presentation.HighResolution2D, new GameSettings("hd", 1280, 720) { Scaling = CanvasScaling.Smooth }.EffectivePresentation);
    }

    [TestMethod]
    public void A_Presentation_Wins_Over_Scaling()
    {
        var settings = new GameSettings("hd", 1280, 720) { Scaling = CanvasScaling.Smooth, Presentation = Presentation.PixelArt };
        Assert.AreSame(Presentation.PixelArt, settings.EffectivePresentation);
        Assert.AreEqual(TextureFilter.Nearest, settings.DefaultTextureFilter);
    }

    [TestMethod]
    public void The_Default_Texture_Filter_Comes_From_The_Default_Sampler_Not_From_The_Rounding()
    {
        // Whole-factor scaling with smooth textures: the old switch could not say it.
        var settings = new GameSettings("pixel") { Presentation = Presentation.PixelArt with { DefaultSampler = SamplerDescription.LinearClamp } };
        Assert.AreEqual(TextureFilter.Linear, settings.DefaultTextureFilter);
        Assert.AreEqual(ScaleRounding.IntegerUnlessWasteful, settings.EffectivePresentation.Rounding, "the rounding is untouched");
        Assert.AreEqual(TextureFilter.Nearest, (settings with { TextureFilter = TextureFilter.Nearest }).DefaultTextureFilter, "an explicit TextureFilter still wins");
    }

    [TestMethod]
    public void The_Capture_Scale_Comes_From_The_Presentation()
    {
        Assert.AreEqual(3, new PlatformRequest(new GameSettings("pixel")).CaptureScale, "pixel art, as before");
        Assert.AreEqual(1, new PlatformRequest(new GameSettings("hd", 1280, 720) { Scaling = CanvasScaling.Smooth }).CaptureScale, "smooth, as before");
        var pixelArtAtOne = new GameSettings("pixel") { Presentation = Presentation.PixelArt with { CaptureScale = 1 } };
        Assert.AreEqual(1, new PlatformRequest(pixelArtAtOne).CaptureScale, "independent of the rounding");
        Assert.AreEqual(2, new PlatformRequest(pixelArtAtOne) { CaptureScale = 2 }.CaptureScale, "a request still chooses its own");
    }

    [TestMethod]
    public void Whole_Factor_Windows_Follow_Their_Own_Field()
    {
        var pixel = new GameSettings("pixel");
        Assert.AreEqual((960, 540), WindowSizing.Initial(pixel, 4, 1366, 728), "whole factors, as before");
        var fractional = pixel with { Presentation = Presentation.PixelArt with { WholeFactorWindows = false } };
        var (w, h) = WindowSizing.Initial(fractional, 4, 1366, 728);
        Assert.AreEqual(1164, w, "the canvas's shape scaled to 90 % of the display's height, no whole factor");
        Assert.AreEqual(655, h);
    }

    [TestMethod]
    public void The_Frame_In_A_Window_Follows_The_Rounding_And_The_Safe_Area()
    {
        var window = new Vector2(1366, 768);
        var none = new RectF(0, 0, 0, 0); // no safe area reported: the whole window
        Assert.AreEqual(FrameLayout.Fit(window, none, 320, 180), Presentation.PixelArt.Fit(window, none, 320, 180), "the pixel-art preset keeps the shared rule");
        Assert.AreEqual(FrameLayout.Fit(window, none, 1280, 720, CanvasScaling.Smooth), Presentation.HighResolution2D.Fit(window, none, 1280, 720), "and so does the smooth one");
        var portrait = new Vector2(1080, 1920);
        var safe = new RectF(0, 80, 1080, 1800);
        Assert.AreEqual(FrameLayout.Fit(portrait, safe, 320, 180), Presentation.PixelArt.Fit(portrait, safe, 320, 180), "portrait screens put the frame at the top of the safe area");
        var integer = Presentation.PixelArt with { Rounding = ScaleRounding.Integer };
        Assert.AreEqual(new RectF(46, 1, 640, 360), integer.Fit(new Vector2(732, 362), none, 320, 180), "never fractional when a whole factor fits");
    }
}

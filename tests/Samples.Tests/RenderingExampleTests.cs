using System.Numerics;
using Examples;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace Samples.Tests;

/// <summary>
/// The examples of the rendering guide whose point is the picture: each is opened in the examples game on the headless
/// platform's rasterizer and checked by the frame the engine draws (the scenes' pictures, then the UI view).
/// </summary>
[TestClass]
public sealed class RenderingExampleTests
{
    private sealed class NoEvents : MicroEngine.Platform.IEngineEventSink
    {
        public void Resized(SurfaceResized size) { }
        public void FocusChanged(bool focused) { }
        public void Suspending() { }
        public void Resumed() { }
        public void QuitRequested() { }
    }

    /// <summary>The examples game on a rasterizing 960×540 headless window with <paramref name="scene"/> on top.</summary>
    internal static (ExamplesGame Game, HeadlessPlatform Platform) Start(Scene scene)
    {
        var game = new ExamplesGame(Directory.CreateTempSubdirectory("examples-").FullName);
        var platform = HeadlessPlatform.For(game.Settings, rasterize: true);
        platform.Resize(960, 540);
        platform.PumpEvents(new NoEvents());
        game.InitializeForTest(platform);
        game.Scenes.Push(scene);
        game.StepForTest();
        return (game, platform);
    }

    /// <summary>Draws a frame as the loop does (render, then present) and returns the canvas.</summary>
    internal static PixelImage Draw(ExamplesGame game, HeadlessPlatform platform)
    {
        var frame = new FrameContext(1 / 60.0, 1 / 60.0, 1f, game.UpdateCount, 0);
        game.Schedule.Run(FramePhase.Render, frame);
        game.Schedule.Run(FramePhase.Present, frame);
        return ((IViewCapture)platform).CaptureWorld();
    }

    internal static void Press(ExamplesGame game, string action)
    {
        game.Input.SetForced(action, true);
        game.StepForTest();
        game.Input.SetForced(action, false);
        game.StepForTest();
    }

    private static readonly Color Trunk = Color.Hex("5d275d"), Crown = Color.Hex("257179"), Grass = Color.Hex("38b764");

    [TestMethod]
    public void TheWalkerGoesBehindTheTreeBelowItAndInFrontOfTheTreeAboveIt()
    {
        var (game, platform) = Start(new DepthExample());
        var scene = (DepthExample)game.Scenes.Current!;
        Assert.AreEqual(new Vector2(150, 92), scene.Walker, "the walker starts just below the tree at (150, 84), over its trunk");

        // Sorted: the walker's feet (y 92) are below the tree's (y 84), so the walker covers the trunk at (150, 80)
        // although the example draws the walker first.
        var sorted = Draw(game, platform);
        Assert.AreNotEqual(Trunk, sorted[150, 80], "sorted: the nearer walker covers the trunk");
        Assert.AreNotEqual(Grass, sorted[150, 80]);

        // The same calls in the order they are made: the tree, drawn after the walker, covers it.
        Press(game, "confirm");
        Assert.IsTrue(scene.Unsorted);
        Assert.AreEqual(Trunk, Draw(game, platform)[150, 80], "call order: the tree drawn after the walker covers it although the walker is nearer");
        Press(game, "confirm");

        // Walked above the tree, the walker is behind it: the crown (centre 150, 62) covers its head.
        game.Input.SetForced("up", true);
        while (scene.Walker.Y > 70) game.StepForTest();
        game.Input.SetForced("up", false);
        game.StepForTest();
        Assert.AreEqual(Crown, Draw(game, platform)[150, 60], "the crown of the tree below covers the walker");

        Press(game, "pause");
        Assert.IsInstanceOfType<MenuScene>(game.Scenes.Current, "pause goes back to the list");
    }

    private static int Brightness(Color c) => c.R + c.G + c.B;

    [TestMethod]
    public void TheScenesChainProcessesItsPictureAndSaysWhatTheDeviceLeavesOut()
    {
        var (game, platform) = Start(new PostProcessExample());
        var scene = (PostProcessExample)game.Scenes.Current!;
        Assert.HasCount(5, scene.PostProcess.Effects);
        var support = scene.PostProcess.SupportOn(game.Graphics).ToDictionary(e => e.Effect.GetType().Name, e => e.Support);
        Assert.AreEqual(PostEffectSupport.Reduced, support["Bloom"], "no shaders on the headless platform: the simpler bloom");
        Assert.AreEqual(PostEffectSupport.None, support["ShaderEffect"], "and the pass through the game's shader is left out");
        Assert.AreEqual(PostEffectSupport.Full, support["Vignette"]);
        Assert.AreEqual(PostEffectSupport.Full, support["Scanlines"]);

        var on = Draw(game, platform);
        Assert.IsTrue(Brightness(on[160, 20]) > Brightness(on[3, 20]) + 12, "the vignette darkens the corners of the scene's picture");
        Assert.IsTrue(Brightness(on[160, 20]) > Brightness(on[160, 21]), "and the scanlines darken every other row");

        Press(game, "action");                      // the effect written in the game: four-pixel blocks
        var pixels = Draw(game, platform);
        Assert.AreEqual(pixels[160, 90], pixels[163, 90], "a block of four pixels is one colour (in the middle, where the vignette changes nothing)");
        Assert.AreEqual(pixels[164, 90], pixels[167, 90]);
        Press(game, "action");

        Press(game, "confirm");                     // the chain off: the picture as the scene draws it
        Assert.IsFalse(scene.PostProcess.Enabled);
        var off = Draw(game, platform);
        Assert.AreEqual(off[160, 20], off[3, 20], "the same row of the gradient, edge to edge");

        int live = ((HeadlessGraphicsDevice)platform.Graphics).LiveTextures;
        Press(game, "confirm");
        Draw(game, platform);
        Press(game, "pause");
        Assert.IsInstanceOfType<MenuScene>(game.Scenes.Current, "pause goes back to the list");
        Assert.IsTrue(((HeadlessGraphicsDevice)platform.Graphics).LiveTextures <= live, "leaving releases what the chain made");
    }
}

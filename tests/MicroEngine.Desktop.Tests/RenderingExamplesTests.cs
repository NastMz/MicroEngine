using Examples;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// The four rendering examples (samples/Examples: light behind walls, masks, water, post-processing), each run as the
/// only scene of a game in an SDL window through the launcher, on both graphics devices: on <c>sdl-gpu</c> every
/// technique shows; on the default device the two made of render targets and blend states show, the water draws its
/// plain picture and the post-processing chain runs the effects that need no shader. Checked by the canvas each scene leaves, read back inside the frame; no input is
/// involved, so nothing another window does can change what is drawn.
/// </summary>
[TestClass]
[TestCategory("Native")]
public sealed class RenderingExamplesTests
{
    private static readonly Color FlatWater = Color.Hex("3b5dc9");
    private static readonly Color[] Stripes = [Color.Hex("ef7d57"), Color.Hex("ffcd75"), Color.Hex("a7f070")];
    private static readonly Dictionary<string, Func<Scene>> Scenes = new()
    {
        ["Light behind walls"] = () => new OccludedLightExample(),
        ["Masks"] = () => new MaskExample(),
        ["Water"] = () => new WaterExample(),
        ["Post-processing"] = () => new PostProcessExample(),
    };

    /// <summary>A game whose only scene is one example; it keeps the canvas of its last frame.</summary>
    private sealed class OneExample(Func<Scene> scene) : Game
    {
        public PixelImage? Canvas;
        public override GameSettings Settings { get; } = new("rendering example");
        protected override void Load() => Scenes.Push(scene());

        protected override void DrawAboveScenes(IRenderer renderer)
        {
            var canvas = (DeviceRenderer)renderer;
            canvas.Flush();
            var request = canvas.Target.Device.ReadPixels(canvas.Target, canvas.Target.Bounds);
            request.Wait();
            var rgba = request.Pixels;
            var image = new PixelImage(canvas.Target.Width, canvas.Target.Height);
            for (int i = 0; i < image.Width * image.Height; i++)
                image[i % image.Width, i / image.Width] = new Color(rgba[i * 4], rgba[i * 4 + 1], rgba[i * 4 + 2], 255);
            Canvas = image;
        }
    }

    /// <summary>Runs the four examples on a graphics device and returns the canvas each one drew.</summary>
    private static Dictionary<string, PixelImage> Play(string graphics)
    {
        if (OperatingSystem.IsMacOS()) Assert.Inconclusive("windows can only be opened on the main thread on macOS");
        var canvases = new Dictionary<string, PixelImage>();
        foreach (var (name, scene) in Scenes)
        {
            Assert.IsTrue(ExamplesGame.All.Any(e => e.Name.StartsWith(name, StringComparison.Ordinal)), $"'{name}' is in the list of examples");
            var game = new OneExample(scene);
            var errors = new StringWriter();
            var (previousError, previousOut) = (Console.Error, Console.Out);
            Console.SetError(errors);
            Console.SetOut(new StringWriter());
            int code;
            try { code = DesktopLauncher.Run(game, ["--platform", "sdl", "--graphics", graphics, "--hidden", "--mute", "--frames", "20"]); }
            finally
            {
                Console.SetError(previousError);
                Console.SetOut(previousOut);
            }
            if (code != 0 && errors.ToString().Contains("cannot start here")) Native.Require(false, errors.ToString().Trim());
            Assert.AreEqual(0, code, errors.ToString());
            canvases[name] = game.Canvas ?? throw new AssertFailedException($"'{name}' drew no frame");
        }
        return canvases;
    }

    private static Color At(PixelImage canvas, int x, int y) => canvas[x, y];
    private static int Brightness(Color c) => c.R + c.G + c.B;

    private static void AssertLightAndMask(Dictionary<string, PixelImage> canvases)
    {
        // The torch is at (120, 92) and reaches 110 pixels: the floor beside it is lit, the floor 60 pixels away behind
        // the wall at x = 150 is not (both points are on tiles of the same colour), nor is the far corner.
        var light = canvases["Light behind walls"];
        int lit = Brightness(At(light, 100, 95)), shadowed = Brightness(At(light, 180, 95)), corner = Brightness(At(light, 10, 170));
        Assert.IsTrue(lit > shadowed * 2, $"the floor in the light ({lit}) is brighter than the floor behind the wall ({shadowed})");
        Assert.IsTrue(lit > corner * 2, $"the floor in the light ({lit}) is brighter than the far corner ({corner})");
        // The circle of the mask is at (230, 120): the stripes show inside it, the scene's background everywhere else.
        var mask = canvases["Masks"];
        CollectionAssert.Contains(Stripes, At(mask, 230, 120), "the picture shows through the mask's circle");
        CollectionAssert.DoesNotContain(Stripes, At(mask, 20, 150), "nothing of the picture outside the mask");
        CollectionAssert.DoesNotContain(Stripes, At(mask, 300, 40));
    }

    [TestMethod]
    [TestCategory("Gpu")]
    public void On_the_programmable_device_every_technique_shows()
    {
        var canvases = Play("sdl-gpu");
        AssertLightAndMask(canvases);
        // Water: the row under the surface mirrors the hills, the sky and the posts instead of being one flat colour.
        var water = canvases["Water"];
        var row = Enumerable.Range(0, 320).Select(x => At(water, x, 126)).ToList();
        Assert.IsTrue(row.Distinct().Count() >= 3, $"the water mirrors what is above it ({row.Distinct().Count()} colours in a row)");
        CollectionAssert.DoesNotContain(row, FlatWater, "the shader's water replaced the flat water");
        // Post-processing: the same row of the gradient is darker at the edge than in the middle (the vignette).
        var post = canvases["Post-processing"];
        Assert.IsTrue(Brightness(At(post, 160, 21)) > Brightness(At(post, 3, 21)) + 12, "the corners are darkened");
    }

    [TestMethod]
    public void On_the_default_device_the_render_target_techniques_show_and_the_shader_ones_draw_what_they_can_without_one()
    {
        var canvases = Play("sdl-renderer");
        AssertLightAndMask(canvases);
        Assert.AreEqual(FlatWater, At(canvases["Water"], 20, 170), "flat water where the device has no shaders");
        // Post-processing: the chain runs here too, with the effects that need no shader (the vignette among them).
        var post = canvases["Post-processing"];
        Assert.IsTrue(Brightness(At(post, 160, 21)) > Brightness(At(post, 3, 21)) + 12, "the corners are darkened without a shader as well");
    }
}

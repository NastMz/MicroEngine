using System.Numerics;
using Examples;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Graphics.SdlGpu;
using MicroEngine.Scenes;
using MicroEngine.Sdl;
using Dev = MicroEngine.Graphics.Devices;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// Texture files (KTX 2.0) on the real graphics devices, with the picture the Examples sample ships both as an image
/// and as BC7 blocks made by <c>microengine textures</c>: the programmable device samples the blocks and what it
/// draws is the image to within the encoding's error; the default device refuses the blocks with the typed error and
/// takes the same picture stored as pixels; and the example draws the same picture on both, from the file where the
/// device samples it and from the image where it does not.
/// </summary>
[TestClass]
[TestCategory("Native")]
public sealed class TextureFileDeviceTests
{
    private static string Content(string file) => Path.Combine(AppContext.BaseDirectory, "Content", file.Replace('/', Path.DirectorySeparatorChar));

    private static byte[] ImagePixels(out int width, out int height)
    {
        var image = PixelImage.Decode(File.ReadAllBytes(Content("pictures/emblem.png")));
        (width, height) = (image.Width, image.Height);
        var rgba = image.ToRgba();
        Dev.PremultipliedColor.PremultiplyInPlace(rgba);
        return rgba;
    }

    private static byte[] DrawAndReadBack(Dev.IGraphicsDevice device, Dev.Texture texture)
    {
        using var batcher = new Renderer2D(device);
        using var target = device.CreateRenderTarget(new Dev.RenderTargetDescription(texture.Width, texture.Height));
        batcher.Begin(new BatchSettings(target, Blend: Dev.BlendState.Opaque, Clear: new ColorF(0, 0, 0, 0)));
        batcher.Draw(texture, new RectF(0, 0, texture.Width, texture.Height), Matrix3x2.Identity, ColorF.White);
        batcher.End();
        var request = device.ReadPixels(target, target.Bounds);
        request.Wait();
        return request.Pixels.ToArray();
    }

    [TestMethod]
    [TestCategory("Gpu")]
    public void The_programmable_device_samples_the_block_compressed_file_close_to_the_image()
    {
        Dev.IGraphicsDevice device;
        try { device = new SdlGpuBackendFactory().Create(null, new Dev.GraphicsDeviceOptions(PreferLowPower: Environment.GetEnvironmentVariable("MICROENGINE_GPU_LOW_POWER") == "1")); }
        catch (Exception e) when (e is InvalidOperationException or NotSupportedException)
        {
            Native.Require(false, $"SDL_GPU cannot start here: {e.Message}");
            throw;
        }
        using (device)
        {
            var file = Dev.TextureFile.Parse(File.ReadAllBytes(Content("textures/emblem.ktx2")));
            Assert.AreEqual((256, 256, Dev.TextureFormat.Bc7, Dev.AlphaMode.Premultiplied), (file.Width, file.Height, file.Format, file.Alpha));
            if (!file.CanCreate(device.Capabilities)) Assert.Inconclusive($"{device.Capabilities.Adapter} ({device.Capabilities.ApiName}) does not sample BC7");
            using var texture = file.Create(device);
            var drawn = DrawAndReadBack(device, texture);
            var expected = ImagePixels(out int width, out int height);
            Assert.AreEqual(width * height * 4, drawn.Length);
            long total = 0;
            int worst = 0;
            for (int i = 0; i < drawn.Length; i++)
            {
                int error = Math.Abs(drawn[i] - expected[i]);
                total += error;
                worst = Math.Max(worst, error);
            }
            double mean = total / (double)drawn.Length;
            Console.WriteLine($"BC7 on {device.Capabilities.Adapter} ({device.Capabilities.ApiName}): mean error {mean:0.000}, worst {worst} of 255");
            Assert.IsLessThan(1.0, mean, "mean error per channel, in 8-bit levels");
            // The worst pixels sit on hard edges between an outline and transparency (52 on the reference machine).
            Assert.IsLessThan(64, worst, "worst channel of any pixel");
            // Not a blank or a shifted picture: transparent corner, the opaque star in the middle.
            Assert.AreEqual(0, drawn[3], "the corner is transparent");
            Assert.AreEqual(255, drawn[(124 * width + 128) * 4 + 3], "the middle is opaque");
        }
    }

    [TestMethod]
    public void The_default_device_refuses_blocks_and_draws_the_same_picture_stored_as_pixels()
    {
        if (OperatingSystem.IsMacOS()) Assert.Inconclusive("windows can only be opened on the main thread on macOS");
        SdlTestSurface surface;
        try { surface = new SdlTestSurface(64, 64); }
        catch (InvalidOperationException e)
        {
            Native.Require(false, e.Message);
            throw;
        }
        using (surface)
        using (var device = new SdlRendererBackendFactory().Create(surface, new Dev.GraphicsDeviceOptions()))
        {
            var blocks = Dev.TextureFile.Parse(File.ReadAllBytes(Content("textures/emblem.ktx2")));
            Assert.IsFalse(blocks.CanCreate(device.Capabilities));
            var error = Assert.ThrowsExactly<Dev.GraphicsCapabilityException>(() => blocks.Create(device));
            Assert.AreEqual("TextureFormat.Bc7 as Sampled", error.Requirement);

            var expected = ImagePixels(out int width, out int height);
            var pixels = Dev.TextureFile.Parse(new Dev.TextureFile(width, height, Dev.TextureFormat.Rgba8Unorm, [expected]).ToBytes());
            Assert.IsTrue(pixels.CanCreate(device.Capabilities));
            using var texture = pixels.Create(device);
            CollectionAssert.AreEqual(expected, DrawAndReadBack(device, texture), "a file of pixels is the image exactly");
        }
    }

    /// <summary>A game whose only scene is the example; it keeps the canvas of its last frame and which path drew it.</summary>
    private sealed class OneExample : Game
    {
        private readonly TextureFileExample _scene = new();
        public PixelImage? Canvas;
        public bool? FromTheFile;
        public Dev.GraphicsCapabilities? Capabilities;
        public override GameSettings Settings { get; } = new("texture file example");
        protected override void Load() => Scenes.Push(_scene);

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
            (Canvas, FromTheFile, Capabilities) = (image, _scene.DrawsTheTextureFile, canvas.Target.Device.Capabilities);
        }
    }

    private static OneExample Play(string graphics)
    {
        if (OperatingSystem.IsMacOS()) Assert.Inconclusive("windows can only be opened on the main thread on macOS");
        var game = new OneExample();
        var errors = new StringWriter();
        var (previousError, previousOut) = (Console.Error, Console.Out);
        Console.SetError(errors);
        Console.SetOut(new StringWriter());
        int code;
        try { code = DesktopLauncher.Run(game, ["--platform", "sdl", "--graphics", graphics, "--hidden", "--mute", "--frames", "10"]); }
        finally
        {
            Console.SetError(previousError);
            Console.SetOut(previousOut);
        }
        if (code != 0 && errors.ToString().Contains("cannot start here")) Native.Require(false, errors.ToString().Trim());
        Assert.AreEqual(0, code, errors.ToString());
        Assert.IsNotNull(game.Canvas, "the example drew a frame");
        return game;
    }

    [TestMethod]
    [TestCategory("Gpu")]
    public void The_example_draws_the_same_picture_from_the_file_and_from_the_image()
    {
        var onGpu = Play("sdl-gpu");
        var onDefault = Play("sdl-renderer");
        Assert.AreEqual(onGpu.Capabilities!.Supports(Dev.TextureFormat.Bc7, Dev.TextureUsage.Sampled), onGpu.FromTheFile, "the file is drawn exactly where the device samples its format");
        Assert.IsFalse(onDefault.FromTheFile!.Value, "the default device draws the image");
        // The emblem is drawn at half size with its top-left at (96, 34): the star's middle, the ring, the disc.
        foreach (var (x, y) in new[] { (160, 96), (160, 46), (130, 80), (190, 110) })
        {
            Color a = onGpu.Canvas![x, y], b = onDefault.Canvas![x, y];
            Assert.IsTrue(Math.Abs(a.R - b.R) <= 12 && Math.Abs(a.G - b.G) <= 12 && Math.Abs(a.B - b.B) <= 12, $"({x}, {y}): {a} from sdl-gpu, {b} from the default device");
        }
        var star = onGpu.Canvas![160, 96];
        Assert.IsTrue(star.R > 200 && star.G > 150 && star.B < 120, $"the star is yellow: {star}");
        Assert.AreNotEqual(onGpu.Canvas[160, 96], onGpu.Canvas[10, 100], "the picture is over the background, not in place of it");
    }
}

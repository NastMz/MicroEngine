using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using Dev = MicroEngine.Graphics.Devices;

namespace MicroEngine.Tests;

/// <summary>
/// <see cref="TextureFilter.LinearMipmap"/> where the device has no mip levels (the headless platform and a tier-0
/// device, the CPU reference device here): the texture has one level and is sampled as linear, everywhere a filter is
/// read. What it draws where levels exist is in the conformance suites (tests/Shared/Graphics/DeviceConformance.cs,
/// tests/MicroEngine.Desktop.Tests/RendererConformance.cs).
/// </summary>
[TestClass]
public sealed class MipLevelTests
{
    private static PixelImage Pair() => new PixelImage(2, 1) { [0, 0] = Color.Black, [1, 0] = Color.White };

    [TestMethod]
    public void On_a_device_without_mip_levels_the_texture_has_one_level_and_is_sampled_as_linear()
    {
        using var device = new Dev.ReferenceGraphicsDevice();
        Assert.IsFalse(device.Capabilities.Has(Dev.GraphicsFeatures.Mipmaps));
        using var canvas = new DeviceCanvas(device, 32, 4);
        var levels = canvas.Graphics.CreateTexture(Pair(), TextureFilter.LinearMipmap);
        var linear = canvas.Graphics.CreateTexture(Pair(), TextureFilter.Linear);
        Assert.AreEqual(TextureFilter.LinearMipmap, levels.Filter, "the filter asked for is kept");
        Assert.AreEqual(1, levels.MipLevels);
        Assert.AreEqual(linear.SizeInBytes, levels.SizeInBytes);
        Assert.AreEqual(Dev.SamplerDescription.LinearClamp, ((DeviceTexture)levels).Sampler);
        var r = canvas.BeginFrame();
        r.DrawStretched(levels, levels.Bounds, new RectF(0, 0, 32, 2), Color.White);
        r.DrawStretched(linear, linear.Bounds, new RectF(0, 2, 32, 2), Color.White);
        canvas.EndFrame();
        var frame = canvas.Capture();
        for (int x = 0; x < 32; x++) Assert.AreEqual(frame[x, 3], frame[x, 1], $"the same as linear at x={x}");
        Assert.IsTrue(frame[16, 1].R is > 100 and < 160, "blended between the two texels");
        levels.Update(Pair().Clear(Color.White));
        Assert.AreEqual(1, levels.MipLevels, "an update has no levels to make again");
    }

    [TestMethod]
    public void The_headless_platform_and_everything_that_reads_a_filter_take_it_as_linear()
    {
        var graphics = new HeadlessGraphicsDevice { KeepsPixels = true };
        var levels = graphics.CreateTexture(Pair(), TextureFilter.LinearMipmap);
        Assert.AreEqual(1, levels.MipLevels);
        var r = new HeadlessRenderer(32, 2, graphics);
        r.DrawStretched(levels, levels.Bounds, new RectF(0, 0, 32, 2), Color.White);
        Assert.IsTrue(r.Frame![16, 0].R is > 100 and < 160, "linear on the headless rasterizer");
        Assert.AreEqual(Dev.SamplerDescription.LinearClamp, DrawState.Alpha.Filtered(TextureFilter.LinearMipmap).Sampler);
        Assert.AreEqual(Dev.SamplerDescription.PointClamp, DrawState.Alpha.Filtered(TextureFilter.Nearest).Sampler);

        // An atlas built for it keeps the one-pixel border linear sampling needs.
        var atlas = new SpriteAtlasBuilder().Add("a", new PixelImage(4, 4).Clear(Color.White)).Build(graphics, TextureFilter.LinearMipmap);
        var smooth = new SpriteAtlasBuilder().Add("a", new PixelImage(4, 4).Clear(Color.White)).Build(graphics, TextureFilter.Linear);
        Assert.AreEqual(TextureFilter.LinearMipmap, atlas.Texture.Filter);
        Assert.AreEqual(smooth["a"].Source, atlas["a"].Source, "packed as for linear sampling");
    }
}

using System.Numerics;
using MicroEngine.Conformance.Graphics;
using MicroEngine.Graphics;
using MicroEngine.Graphics.Devices;
using MicroEngine.Graphics.SdlGpu;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// The 2D batcher drawing with a material (shaders of the caller's own) on the tier-1 SDL_GPU device, by what lands in
/// the target: the fragment shader's result, the parameters each draw was recorded with, a second texture, the order
/// of draws with and without a material, the viewport, the blend state, and the refusals. The shaders are the
/// conformance suite's (a desaturation by a uniform, a palette lookup through a second texture).
/// </summary>
public abstract class Material2DTests(string? api)
{
    private const uint Red = 0xFF0000FF, Green = 0x00FF00FF, Blue = 0x0000FFFF, Black = 0x000000FF;
    private static readonly RectF Texel = new(0, 0, 1, 1);
    private MicroEngine.Graphics.Devices.IGraphicsDevice _device = null!;
    private Renderer2D _batcher = null!;
    private Shader _vertex = null!, _grayscale = null!, _palette = null!;
    private MicroEngine.Graphics.Devices.Texture _red = null!;

    [TestInitialize]
    public void Open()
    {
        try { _device = new SdlGpuBackendFactory().Create(null, new GraphicsDeviceOptions(Api: api, PreferLowPower: Environment.GetEnvironmentVariable("MICROENGINE_GPU_LOW_POWER") == "1")); }
        catch (Exception e) when (e is InvalidOperationException or NotSupportedException)
        {
            string why = $"SDL_GPU '{api ?? "default"}' cannot start here: {e.Message}";
            if (api is null) Native.Require(false, why);
            Assert.Inconclusive(why);
            throw;
        }
        _batcher = new Renderer2D(_device);
        _vertex = ShaderFiles.Compiled("pixel.vert").Create(_device);
        _grayscale = ShaderFiles.Compiled("grayscale.frag").Create(_device);
        _palette = ShaderFiles.Compiled("palette.frag").Create(_device);
        _red = _device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(Red));
    }

    [TestCleanup]
    public void Close()
    {
        _batcher?.Dispose();
        _device?.Dispose();
    }

    private RenderTarget Target(int width = 16, int height = 8) => _device.CreateRenderTarget(new RenderTargetDescription(width, height));

    private Image Read(RenderTarget target)
    {
        var request = _device.ReadPixels(target, target.Bounds);
        request.Wait();
        return new Image(target.Width, target.Height, request.Pixels.ToArray());
    }

    /// <summary>The grayscale material with its amount set: 0 copies the texture, 1 gives its luma.</summary>
    private Material2D Grayscale(float amount)
    {
        var material = new Material2D(_vertex, _grayscale);
        material.SetUniforms(new Vector4(amount, 0, 0, 0));
        return material;
    }

    private static Matrix3x2 Box(float x, float y, float width, float height) => Matrix3x2.CreateScale(width, height) * Matrix3x2.CreateTranslation(x, y);

    private static void AssertGrayOfRed(Image image, int x, int y)
    {
        for (int channel = 0; channel < 3; channel++)
            Assert.IsTrue(Math.Abs(image.Channel(x, y, channel) - 0.299f * 255) <= 1, $"({x},{y}) channel {channel}: the luma of red is 76, got {image.Channel(x, y, channel)}");
        Assert.AreEqual(255, image.Channel(x, y, 3));
    }

    [TestMethod]
    public void A_batch_with_a_material_is_drawn_by_its_fragment_shader()
    {
        var target = Target();
        using var gray = Grayscale(1);
        _batcher.Begin(new BatchSettings(target, Clear: ColorF.Black) { Material = gray });
        _batcher.Draw(_red, Texel, Box(0, 0, 8, 8), ColorF.White);
        _batcher.End();
        var image = Read(target);
        AssertGrayOfRed(image, 3, 3);
        Assert.AreEqual(Black, image[12, 3], "nothing was drawn on the right half");
        Assert.AreEqual(1, _batcher.Statistics.DrawCalls);
    }

    [TestMethod]
    public void Each_draw_keeps_the_parameters_the_material_had_when_it_was_recorded()
    {
        var target = Target();
        using var material = Grayscale(0);
        _batcher.Begin(new BatchSettings(target, Clear: ColorF.Black) { Material = material });
        _batcher.Draw(_red, Texel, Box(0, 0, 8, 8), ColorF.White);       // amount 0: red as it is
        material.SetUniforms(new Vector4(1, 0, 0, 0));
        _batcher.Draw(_red, Texel, Box(8, 0, 8, 8), ColorF.White);       // amount 1: its luma
        _batcher.End();
        var image = Read(target);
        Assert.AreEqual(Red, image[3, 3], "the first draw kept amount 0, set before it was recorded");
        AssertGrayOfRed(image, 12, 3);
        Assert.AreEqual(2, _batcher.Statistics.DrawCalls, "draws with different parameters are separate draw calls");
    }

    [TestMethod]
    public void Draws_with_the_same_parameters_share_a_draw_call()
    {
        var target = Target();
        using var gray = Grayscale(1);
        _batcher.Begin(new BatchSettings(target, Clear: ColorF.Black) { Material = gray });
        for (int i = 0; i < 4; i++) _batcher.Draw(_red, Texel, Box(i * 4, 0, 4, 8), ColorF.White);
        _batcher.End();
        Assert.AreEqual(new Renderer2DStatistics(Sprites: 4, DrawCalls: 1, Flushes: 1), _batcher.Statistics);
        AssertGrayOfRed(Read(target), 14, 6);
    }

    [TestMethod]
    public void Draws_with_and_without_a_material_land_in_the_order_they_were_recorded()
    {
        var target = Target();
        using var gray = Grayscale(1);
        _batcher.Begin(new BatchSettings(target, Clear: ColorF.Black));
        _batcher.Draw(_batcher.White, Texel, Box(0, 0, 16, 8), new ColorF(0, 1, 0, 1));    // built-in: green everywhere
        _batcher.PushState(_batcher.State with { Material = gray });
        _batcher.Draw(_red, Texel, Box(4, 0, 8, 8), ColorF.White);                         // material: gray over the middle
        _batcher.PopState();
        _batcher.Draw(_batcher.White, Texel, Box(6, 0, 2, 8), new ColorF(0, 0, 1, 1));     // built-in again: blue over the gray
        _batcher.End();
        var image = Read(target);
        Assert.AreEqual(Green, image[1, 4]);
        AssertGrayOfRed(image, 5, 4);
        Assert.AreEqual(Blue, image[7, 4], "the built-in draw recorded after the material's lands over it");
        AssertGrayOfRed(image, 10, 4);
        Assert.AreEqual(Green, image[14, 4]);
    }

    [TestMethod]
    public void A_material_gives_its_shader_more_textures_than_the_draw_s()
    {
        // The draw's texture holds indices (in red); the material's second texture is the palette they select from.
        var indexed = _device.CreateTexture(new TextureDescription(4, 1), Texels.Bytes(0x000000FF, 0x010000FF, 0x020000FF, 0x030000FF));
        var colors = new uint[256];
        Array.Fill(colors, Black);
        uint[] first = [Red, Green, Blue, 0xFFFFFFFF];
        first.CopyTo(colors, 0);
        var palette = _device.CreateTexture(new TextureDescription(256, 1), Texels.Bytes(colors));
        using var material = new Material2D(_vertex, _palette);
        material.SetTexture(1, palette);
        var target = Target(16, 4);
        _batcher.Begin(new BatchSettings(target) { Material = material });
        _batcher.Draw(indexed, new RectF(0, 0, 4, 1), Matrix3x2.CreateScale(4, 4), ColorF.White);
        _batcher.End();
        var image = Read(target);
        for (int x = 0; x < 16; x++) Assert.AreEqual(first[x / 4], image[x, 2], $"pixel {x} shows palette entry {x / 4}");
    }

    [TestMethod]
    public void A_material_s_draws_are_placed_by_the_viewport_and_the_view_like_any_draw()
    {
        var target = Target();
        using var copy = Grayscale(0);
        _batcher.Begin(new BatchSettings(target, View: Matrix3x2.CreateTranslation(1, 0), Clear: ColorF.Black, Viewport: new RectI(4, 2, 8, 4)) { Material = copy });
        _batcher.Draw(_red, Texel, Box(0, 0, 2, 2), ColorF.White);
        _batcher.End();
        Assert.AreEqual(new RectI(5, 2, 2, 2), Read(target).BoundsOf(Red), "pixels are relative to the viewport, after the view");
    }

    [TestMethod]
    public void The_state_s_blend_and_scissor_apply_to_a_material_s_draws()
    {
        var target = Target();
        using var copy = Grayscale(0);
        _batcher.Begin(new BatchSettings(target, Clear: new ColorF(0, 1, 0, 1)));
        _batcher.PushState(new RenderState(BlendState.Additive, SamplerDescription.PointClamp, new RectI(0, 0, 8, 8)) { Material = copy });
        _batcher.Draw(_red, Texel, Box(0, 0, 16, 8), ColorF.White);
        _batcher.PopState();
        _batcher.End();
        var image = Read(target);
        Assert.AreEqual(0xFFFF00FFu, image[3, 3], "red added to green");
        Assert.AreEqual(Green, image[12, 3], "outside the scissor");
    }

    [TestMethod]
    public void A_material_whose_parameters_are_incomplete_is_refused_when_a_draw_is_recorded()
    {
        var target = Target();
        using var noUniforms = new Material2D(_vertex, _grayscale);
        _batcher.Begin(new BatchSettings(target) { Material = noUniforms });
        var e = Assert.ThrowsExactly<InvalidOperationException>(() => _batcher.Draw(_red, Texel, Matrix3x2.Identity, ColorF.White));
        StringAssert.Contains(e.Message, "uniform slot 0");
        _batcher.End();

        using var noPalette = new Material2D(_vertex, _palette);
        _batcher.Begin(new BatchSettings(target) { Material = noPalette });
        e = Assert.ThrowsExactly<InvalidOperationException>(() => _batcher.DrawGeometry(_red, [default, default, default], [0, 1, 2]));
        StringAssert.Contains(e.Message, "slot 1");
        _batcher.End();
    }

    [TestMethod]
    public void A_material_refuses_what_its_shaders_do_not_declare()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new Material2D(_grayscale, _vertex), "the stages swapped");
        using var material = new Material2D(_vertex, _grayscale);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => material.SetUniforms(new Vector4(1, 0, 0, 0), slot: 1), "the shader has one uniform slot");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => material.SetUniforms(ReadOnlySpan<byte>.Empty));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => material.SetTexture(0, _red), "slot 0 is the draw's texture");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => material.SetTexture(1, _red), "the shader samples one texture");
    }

    [TestMethod]
    public void A_disposed_material_or_one_of_another_device_cannot_be_drawn_with()
    {
        var target = Target();
        var disposed = Grayscale(1);
        disposed.Dispose();
        Assert.IsTrue(disposed.IsDisposed);
        _batcher.Begin(new BatchSettings(target) { Material = disposed });
        Assert.ThrowsExactly<ObjectDisposedException>(() => _batcher.Draw(_red, Texel, Matrix3x2.Identity, ColorF.White));
        _batcher.End();

        using var reference = new ReferenceGraphicsDevice();
        using var other = new Renderer2D(reference);
        using var gray = Grayscale(1);
        other.Begin(new BatchSettings(reference.CreateRenderTarget(new RenderTargetDescription(4, 4))) { Material = gray });
        Assert.ThrowsExactly<ArgumentException>(() => other.Draw(other.White, Texel, Matrix3x2.Identity, ColorF.White));
        other.End();
    }

    [TestMethod]
    public void Without_a_material_a_batch_is_drawn_as_it_always_was()
    {
        var target = Target();
        _batcher.Begin(new BatchSettings(target, Clear: ColorF.Black));
        Assert.IsNull(_batcher.State.Material);
        Assert.AreEqual(RenderState.Default, _batcher.State);
        _batcher.Draw(_red, Texel, Box(0, 0, 8, 8), ColorF.White);
        _batcher.End();
        Assert.AreEqual(new RectI(0, 0, 8, 8), Read(target).BoundsOf(Red));
    }
}

/// <summary>The material tests on the driver SDL_GPU chooses here.</summary>
[TestClass]
[TestCategory("Native")]
[TestCategory("Gpu")]
public sealed class Material2DDefaultDriverTests() : Material2DTests(null);

/// <summary>The material tests on Vulkan (the driver of the software rasterizer CI runs), skipped where it cannot start.</summary>
[TestClass]
[TestCategory("Native")]
[TestCategory("Gpu")]
public sealed class Material2DVulkanTests() : Material2DTests("vulkan");

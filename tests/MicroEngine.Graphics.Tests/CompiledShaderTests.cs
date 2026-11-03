using MicroEngine.Conformance.Graphics;
using MicroEngine.Graphics.Devices;

namespace MicroEngine.Graphics.Tests;

/// <summary>The compiled shader file: round trip, the format a device is given, and the typed error where shaders are undeclared.</summary>
[TestClass]
public sealed class CompiledShaderTests
{
    private static readonly IReadOnlySet<BlendState> Presets = new HashSet<BlendState>(BlendState.Presets);

    private static GraphicsCapabilities Tier1(string api, ShaderFormat formats) => new("test", GraphicsTier.Tier1, GraphicsFeatures.Shaders, formats,
        new Dictionary<TextureFormat, TextureUsage> { [TextureFormat.Rgba8Unorm] = TextureUsage.Sampled | TextureUsage.RenderTarget }, Presets,
        new HashSet<PresentMode>(), new GraphicsLimits(4096, 4096, 16, 32768, 16), api, null);

    private static CompiledShader AllFormats() => new(ShaderStage.Fragment, new ShaderResources(Samplers: 1, UniformBuffers: 1), new Dictionary<ShaderFormat, ShaderCode>
    {
        [ShaderFormat.SpirV] = new(new byte[] { 3, 2, 35, 7 }, "main"),
        [ShaderFormat.Dxil] = new(new byte[] { 68, 88, 66, 67 }, "main"),
        [ShaderFormat.Msl] = new("#include <metal_stdlib>"u8.ToArray(), "main0"),
    }, "effect.frag.hlsl");

    [TestMethod]
    public void File_RoundTripsEveryField_AndIsByteStable()
    {
        var shader = AllFormats();
        var bytes = shader.ToBytes();
        var read = CompiledShader.Parse(bytes);
        Assert.AreEqual(ShaderStage.Fragment, read.Stage);
        Assert.AreEqual(new ShaderResources(Samplers: 1, UniformBuffers: 1), read.Resources);
        Assert.AreEqual("effect.frag.hlsl", read.Source);
        Assert.AreEqual(ShaderFormat.SpirV | ShaderFormat.Dxil | ShaderFormat.Msl, read.Formats);
        foreach (var format in new[] { ShaderFormat.SpirV, ShaderFormat.Dxil, ShaderFormat.Msl })
        {
            Assert.IsTrue(shader.TryGetCode(format, out var expected));
            Assert.IsTrue(read.TryGetCode(format, out var actual));
            CollectionAssert.AreEqual(expected.Bytes.ToArray(), actual.Bytes.ToArray(), format.ToString());
            Assert.AreEqual(expected.EntryPoint, actual.EntryPoint);
        }
        CollectionAssert.AreEqual(bytes, read.ToBytes(), "the same shader always gives the same file");
    }

    [TestMethod]
    public void DescriptionFor_GivesEachApiItsNativeFormat()
    {
        var shader = AllFormats();
        Assert.AreEqual(ShaderFormat.Dxil, shader.DescriptionFor(Tier1("direct3d12", ShaderFormat.Dxil | ShaderFormat.SpirV)).Format);
        Assert.AreEqual(ShaderFormat.SpirV, shader.DescriptionFor(Tier1("vulkan", ShaderFormat.SpirV | ShaderFormat.Dxil)).Format);
        var metal = shader.DescriptionFor(Tier1("metal", ShaderFormat.Msl));
        Assert.AreEqual(ShaderFormat.Msl, metal.Format);
        Assert.AreEqual("main0", metal.EntryPoint);
        Assert.AreEqual(new ShaderResources(Samplers: 1, UniformBuffers: 1), metal.Resources);
        Assert.AreEqual(ShaderFormat.SpirV, shader.DescriptionFor(Tier1("other", ShaderFormat.SpirV)).Format, "an unknown API takes the first format both have");
    }

    [TestMethod]
    public void DescriptionFor_NoCommonFormat_IsTheTypedErrorNamingBothSides()
    {
        var spirvOnly = new CompiledShader(ShaderStage.Vertex, default, new Dictionary<ShaderFormat, ShaderCode> { [ShaderFormat.SpirV] = new(new byte[] { 1 }, "main") });
        var e = Assert.ThrowsExactly<GraphicsCapabilityException>(() => spirvOnly.DescriptionFor(Tier1("metal", ShaderFormat.Msl)));
        Assert.AreEqual(GraphicsFeatures.None, e.Missing);
        Assert.Contains("Msl", e.Requirement!);
        Assert.Contains("SpirV", e.Requirement!);
    }

    [TestMethod]
    public void Create_OnATier0Device_IsTheTypedErrorNamingShaders()
    {
        using var device = new ReferenceGraphicsDevice();
        var e = Assert.ThrowsExactly<GraphicsCapabilityException>(() => AllFormats().Create(device));
        Assert.AreEqual(GraphicsFeatures.Shaders, e.Missing);
        Assert.AreEqual("reference", e.Device);
    }

    [TestMethod]
    public void TheSuitesCompiledShaders_AreReadable()
    {
        foreach (var name in new[] { "mesh.vert", "color.frag", "pixel.vert", "palette.frag", "grayscale.frag" })
            Assert.AreEqual(ShaderFormat.SpirV | ShaderFormat.Dxil | ShaderFormat.Msl, ShaderFiles.Compiled(name).Formats, name);
    }

    [TestMethod]
    public void Parse_RefusesWhatIsNotACompiledShaderOfThisVersion()
    {
        Assert.ThrowsExactly<InvalidDataException>(() => CompiledShader.Parse("{}"u8));
        Assert.ThrowsExactly<InvalidDataException>(() => CompiledShader.Parse("not json"u8));
        var future = System.Text.Encoding.UTF8.GetString(AllFormats().ToBytes()).Replace("\"version\": 1", "\"version\": 2");
        var e = Assert.ThrowsExactly<InvalidDataException>(() => CompiledShader.Parse(System.Text.Encoding.UTF8.GetBytes(future)));
        Assert.Contains("version 2", e.Message);
    }
}

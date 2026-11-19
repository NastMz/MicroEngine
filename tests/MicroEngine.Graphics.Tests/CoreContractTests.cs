using System.Runtime.CompilerServices;
using MicroEngine.Graphics.Devices;

namespace MicroEngine.Graphics.Tests;

/// <summary>The parts of the contract core owns for every backend: layout, deferred destruction, counters, typed errors.</summary>
[TestClass]
public sealed class CoreContractTests
{
    [TestMethod]
    public void Vertex2D_IsThirtyTwoBytes_AndItsLayoutMatchesTheStruct()
    {
        Assert.AreEqual(Vertex2D.SizeInBytes, Unsafe.SizeOf<Vertex2D>());
        var layout = VertexLayout.Vertex2D;
        Assert.AreEqual(32, layout.Stride);
        CollectionAssert.AreEqual(new[] { (0, VertexFormat.Float2, 0), (1, VertexFormat.Float2, 8), (2, VertexFormat.Float4, 16) },
            layout.Attributes.Select(a => (a.Location, a.Format, a.Offset)).ToArray());
    }

    [TestMethod]
    public void BuiltInPipeline_HasOneKind_Textured()
    {
        CollectionAssert.AreEqual(new[] { BuiltInPipeline.Textured }, Enum.GetValues<BuiltInPipeline>());
        Assert.AreEqual("1", GraphicsContract.Version.ToString(System.Globalization.CultureInfo.InvariantCulture), "the contract text is version 1");
    }

    [TestMethod]
    public void TypedError_NamesTheOperationTheDeviceAndWhatIsMissing()
    {
        var feature = new GraphicsCapabilityException("GetSampler(Mirror)", "sdl-renderer", GraphicsFeatures.AddressMirror);
        Assert.AreEqual("GetSampler(Mirror) on 'sdl-renderer' needs AddressMirror", feature.Message);
        Assert.IsNull(feature.Requirement);
        var format = new GraphicsCapabilityException("CreateTexture", "raylib", GraphicsFeatures.None, "TextureFormat.Bc7 as Sampled");
        Assert.AreEqual("CreateTexture on 'raylib' needs TextureFormat.Bc7 as Sampled", format.Message);
        Assert.AreEqual(GraphicsFeatures.None, format.Missing);
    }

    [TestMethod]
    public void Premultiply_RoundsEachChannelAndKeepsOpaqueAndTransparentTexelsExact()
    {
        byte[] texels = [255, 0, 0, 255, 255, 255, 255, 0, 200, 100, 50, 128, 1, 2, 3, 4];
        PremultipliedColor.PremultiplyInPlace(texels);
        CollectionAssert.AreEqual(new byte[] { 255, 0, 0, 255, 0, 0, 0, 0, 100, 50, 25, 128, 0, 0, 0, 4 }, texels);
        var half = PremultipliedColor.FromStraight(new Color(255, 102, 0, 51));
        Assert.AreEqual(0.2f, half.R, 1e-6f);
        Assert.AreEqual(0.08f, half.G, 1e-6f);
        Assert.AreEqual(0.2f, half.A, 1e-6f);
    }

    [TestMethod]
    public void Premultiply_IsTheNearestValueForEveryChannelAndAlpha()
    {
        // Every colour value against every alpha: round(c × a / 255), written here without the method's own arithmetic
        // (255 is odd, so no product falls exactly between two values).
        var texels = new byte[256 * 4];
        for (int a = 0; a < 256; a++)
        {
            for (int c = 0; c < 256; c++) (texels[c * 4], texels[c * 4 + 1], texels[c * 4 + 2], texels[c * 4 + 3]) = ((byte)c, (byte)(255 - c), (byte)(c ^ 0x55), (byte)a);
            PremultipliedColor.PremultiplyInPlace(texels);
            for (int c = 0; c < 256; c++)
            {
                static byte Nearest(int value, int alpha) => (byte)Math.Round(value * alpha / 255.0, MidpointRounding.AwayFromZero);
                Assert.AreEqual((Nearest(c, a), Nearest(255 - c, a), Nearest(c ^ 0x55, a), (byte)a), (texels[c * 4], texels[c * 4 + 1], texels[c * 4 + 2], texels[c * 4 + 3]), $"colour {c}, alpha {a}");
            }
        }
        // A length that is not a whole number of texels leaves the last bytes alone.
        byte[] odd = [200, 100, 50, 128, 9, 9];
        PremultipliedColor.PremultiplyInPlace(odd);
        CollectionAssert.AreEqual(new byte[] { 100, 50, 25, 128, 9, 9 }, odd);
    }

    [TestMethod]
    public void Disposal_IsDeferredUntilTheBackendCompletesTheSubmissionsThatMayUseTheResource()
    {
        using var device = new LaggingDevice();
        var buffer = (LaggingDevice.Buffer)device.CreateBuffer(new BufferDescription(16, BufferUsage.Vertex));
        device.Submit(device.BeginCommands());
        device.Submit(device.BeginCommands());
        buffer.Dispose();
        Assert.IsTrue(buffer.IsDisposed, "disposed for callers at once");
        Assert.IsFalse(buffer.Released, "but two submissions are still executing");
        Assert.AreEqual(1, device.PendingReleases);
        device.Complete(1);
        Assert.IsFalse(buffer.Released, "the second submission may still use it");
        device.Complete(2);
        Assert.IsTrue(buffer.Released);
        Assert.AreEqual(0, device.PendingReleases);
        buffer.Dispose();
        Assert.AreEqual(1, buffer.Releases, "Dispose is idempotent");
    }

    [TestMethod]
    public void Disposal_AfterTheDevice_DoesNotReleaseAgain()
    {
        var device = new LaggingDevice();
        var buffer = (LaggingDevice.Buffer)device.CreateBuffer(new BufferDescription(16, BufferUsage.Vertex));
        device.Dispose();
        buffer.Dispose();
        Assert.IsTrue(buffer.IsDisposed);
        Assert.AreEqual(0, buffer.Releases, "the device released every backend object when it was disposed");
    }

    [TestMethod]
    public void Counters_SubtractToMeasureASpan()
    {
        var earlier = new GraphicsCounters(Submissions: 2, DrawCalls: 5, LiveResources: 3);
        var later = new GraphicsCounters(Submissions: 5, DrawCalls: 9, LiveResources: 4);
        Assert.AreEqual(new GraphicsCounters(Submissions: 3, DrawCalls: 4, LiveResources: 4), later - earlier);
    }

    [TestMethod]
    public void Counters_CountCreatedAndLiveResources()
    {
        using var device = new ReferenceGraphicsDevice();
        var before = device.Counters;
        var target = device.CreateRenderTarget(new RenderTargetDescription(2, 2));
        Assert.AreEqual(2, (device.Counters - before).ResourcesCreated, "a target and its colour texture");
        Assert.AreEqual(before.LiveResources + 2, device.Counters.LiveResources);
        target.Dispose();
        Assert.AreEqual(before.LiveResources, device.Counters.LiveResources, "the colour texture ends with its target");
        Assert.IsTrue(target.Color!.IsDisposed);
    }

    /// <summary>A device whose backend completes submissions only when told: the deferred-destruction rule made visible.</summary>
    private sealed class LaggingDevice : GraphicsDeviceBase
    {
        private long _completed;

        public override GraphicsCapabilities Capabilities { get; } = new ReferenceGraphicsDevice().Capabilities with { Backend = "lagging" };

        protected override long CompletedSubmissions => _completed;

        public void Complete(long submissions)
        {
            _completed = submissions;
            CollectRetired();
        }

        public sealed class Buffer(LaggingDevice device, in BufferDescription description) : GpuBuffer(device, description)
        {
            public int Releases { get; private set; }
            public bool Released => Releases > 0;
            protected override void Release() => Releases++;
        }

        protected override GpuBuffer CreateBufferCore(in BufferDescription description) => new Buffer(this, description);
        protected override void SubmitCore(CommandList commands) { }
        protected override void DisposeCore(IReadOnlyList<Pipeline> builtIns, IReadOnlyList<Sampler> samplers) { }
        protected override Devices.Texture CreateTextureCore(in TextureDescription description, ReadOnlySpan<byte> initialData) => throw new NotSupportedException();
        protected override RenderTarget CreateRenderTargetCore(in RenderTargetDescription description) => throw new NotSupportedException();
        protected override Sampler CreateSamplerCore(in SamplerDescription description) => throw new NotSupportedException();
        protected override Shader CreateShaderCore(in ShaderDescription description) => throw new NotSupportedException();
        protected override Pipeline CreatePipelineCore(PipelineDescription description) => throw new NotSupportedException();
        protected override Pipeline CreateBuiltInPipelineCore(BuiltInPipeline kind, BlendState blend, TextureFormat colorFormat, TextureFormat? depthFormat) => throw new NotSupportedException();
        protected override ReadbackRequest ReadPixelsCore(RenderTarget target, RectI region) => throw new NotSupportedException();
        protected override RenderTarget? TryAcquireSurfaceTargetCore() => null;
        protected override void PresentCore(RenderTarget surfaceTarget) { }
    }
}

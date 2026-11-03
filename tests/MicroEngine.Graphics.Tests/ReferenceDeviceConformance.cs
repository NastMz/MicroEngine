using MicroEngine.Conformance.Graphics;
using MicroEngine.Graphics.Devices;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace MicroEngine.Graphics.Tests;

/// <summary>The device conformance suite on the CPU reference device: no window, so it runs everywhere.</summary>
[TestClass]
public sealed class ReferenceDeviceConformance : DeviceConformance
{
    protected override Devices.IGraphicsDevice CreateDevice(ITestSurface surface) => new ReferenceGraphicsBackendFactory().Create(surface, new GraphicsDeviceOptions());
    protected override ITestSurface CreateSurface(int width, int height) => new MemorySurface(width, height);
    protected override string ExpectedBackend => "reference";
    protected override string? ExpectedApi => "cpu";
    protected override GraphicsFeatures ExpectedFeatures => ReferenceGraphicsDevice.DeclaredFeatures;

    [TestMethod]
    public void Present_KeepsTheSurfacePixelsForHeadlessCaptures()
    {
        var device = (ReferenceGraphicsDevice)Device;
        Assert.IsNull(device.PresentedFrame, "nothing presented yet");
        Assert.IsTrue(Device.TryAcquireSurfaceTarget(out var surface));
        Painter.Paint(surface, [], clear: Opaque(10, 20, 30));
        Device.Present();
        var frame = device.PresentedFrame!.Value;
        Assert.AreEqual((64, 48), (frame.Width, frame.Height));
        CollectionAssert.AreEqual(new byte[] { 10, 20, 30, 255 }, frame.Rgba[..4], "RGBA order, whatever the surface format");
    }

    [TestMethod]
    public void Offscreen_WithoutASurface_HasNoSurfaceTarget()
    {
        using var offscreen = new ReferenceGraphicsDevice();
        Assert.IsNull(new ReferenceGraphicsBackendFactory().UnsupportedReason(null));
        Assert.IsFalse(offscreen.TryAcquireSurfaceTarget(out var none));
        Assert.IsNull(none);
        offscreen.Present();
        Assert.AreEqual(0, offscreen.Counters.Presents, "a present without a target presents nothing");
    }

    [TestMethod]
    public void Mailbox_IsRefusedAtCreationWithTheTypedError()
    {
        var e = Assert.ThrowsExactly<GraphicsCapabilityException>(() => new ReferenceGraphicsDevice(null, new GraphicsDeviceOptions(PresentMode.Mailbox)));
        Assert.AreEqual(GraphicsFeatures.None, e.Missing);
        Assert.AreEqual("PresentMode.Mailbox", e.Requirement);
    }

    [TestMethod]
    public void IndicesOutsideTheVertexBuffer_FailAtSubmit()
    {
        var target = Target(4, 4);
        var white = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFFFFFFFF));
        var vertices = Device.CreateBuffer(new BufferDescription(3 * Vertex2D.SizeInBytes, BufferUsage.Vertex));
        var indices = Device.CreateBuffer(new BufferDescription(6, BufferUsage.Index));
        var list = Device.BeginCommands();
        list.UpdateBuffer<ushort>(indices, 0, [0, 1, 7]);
        list.BeginPass(new PassDescription(new ColorAttachment(target)));
        list.SetPipeline(Device.GetBuiltInPipeline(BuiltInPipeline.Textured, BlendState.AlphaPremultiplied, target.ColorFormat));
        list.SetTexture(0, white, Device.GetSampler(SamplerDescription.PointClamp));
        list.SetVertices(vertices);
        list.SetIndices(indices);
        list.DrawIndexed(3); // recorded: index values are not validated while recording
        list.EndPass();
        Assert.ThrowsExactly<InvalidOperationException>(() => Device.Submit(list), "the reference device names the undefined behaviour");
    }
}

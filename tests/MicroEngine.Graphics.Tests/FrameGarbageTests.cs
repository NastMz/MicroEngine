using System.Numerics;
using MicroEngine.Conformance.Graphics;
using MicroEngine.Graphics.Devices;
using MicroEngine.Testing;

namespace MicroEngine.Graphics.Tests;

/// <summary>
/// PERF-3: what a frame allocates on the way to the device does not grow with what it draws. A command list used to
/// start with 256 bytes and double as vertices were recorded, every frame: 8.5 MB of garbage per frame at 20,000
/// sprites, a full collection every few frames.
/// </summary>
[TestClass]
public sealed class FrameGarbageTests
{
    private const int Calls = 20;

    /// <summary>Bytes per call in the quietest of several rounds (see <see cref="AllocationAssert"/> for why not every round).</summary>
    private static long BytesPerFrame(Action frame) => AllocationAssert.BytesPerRound(frame, warmup: 5, calls: Calls, rounds: 5).Min() / Calls;

    [TestMethod]
    public void A_frame_of_buffer_updates_reuses_the_previous_frames_command_storage()
    {
        using var device = new ReferenceGraphicsDevice();
        var buffer = device.CreateBuffer(new BufferDescription(1 << 20, BufferUsage.Vertex));
        var target = device.CreateRenderTarget(new RenderTargetDescription(8, 8));
        var data = new byte[1 << 20];
        void Frame()
        {
            var commands = device.BeginCommands();
            commands.UpdateBuffer(buffer, 0, data);
            commands.BeginPass(new PassDescription(new ColorAttachment(target, LoadOp.Load, StoreOp.Store, default)));
            commands.EndPass();
            device.Submit(commands);
        }
        long bytes = BytesPerFrame(Frame);
        Assert.IsTrue(bytes < 1024, $"a megabyte of vertices recorded per frame allocated {bytes:N0} bytes per frame");
    }

    [TestMethod]
    public void A_batch_of_two_thousand_sprites_allocates_next_to_nothing_per_frame()
    {
        using var device = new ReferenceGraphicsDevice();
        using var batcher = new Renderer2D(device);
        Devices.Texture a = device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFF0000FF)), b = device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0x00FF00FF));
        var target = device.CreateRenderTarget(new RenderTargetDescription(64, 64));
        foreach (var sort in new[] { SortMode.Deferred, SortMode.Texture, SortMode.BackToFront })
        {
            void Frame()
            {
                batcher.Begin(new BatchSettings(target, Clear: default(ColorF), Sort: sort));
                // Alternating textures: every sprite its own draw call, the worst case for the batcher's bookkeeping.
                for (int i = 0; i < 2000; i++)
                    batcher.Draw((i & 1) == 0 ? a : b, new RectF(0, 0, 1, 1), Matrix3x2.CreateTranslation(i % 64, i / 64), ColorF.White, depth: i % 7);
                batcher.End();
            }
            long bytes = BytesPerFrame(Frame);
            Assert.IsTrue(bytes < 1024, $"{sort}: 2,000 sprites allocated {bytes:N0} bytes per frame");
        }
    }

    [TestMethod]
    public void A_submitted_list_keeps_refusing_work_and_the_next_list_starts_empty()
    {
        using var device = new ReferenceGraphicsDevice();
        var buffer = device.CreateBuffer(new BufferDescription(64, BufferUsage.Vertex));
        var target = device.CreateRenderTarget(new RenderTargetDescription(8, 8));
        var first = device.BeginCommands();
        first.UpdateBuffer(buffer, 0, new byte[64]);
        first.BeginPass(new PassDescription(new ColorAttachment(target, LoadOp.Load, StoreOp.Store, default)));
        first.EndPass();
        Assert.AreEqual(3, first.Commands.Length);
        device.Submit(first);

        Assert.IsTrue(first.IsSubmitted);
        Assert.AreEqual(0, first.Commands.Length, "the commands of a submitted list are released once Submit returns");
        Assert.ThrowsExactly<InvalidOperationException>(() => first.UpdateBuffer(buffer, 0, new byte[4]));
        Assert.ThrowsExactly<InvalidOperationException>(() => device.Submit(first));

        var second = device.BeginCommands();
        Assert.IsFalse(second.IsSubmitted);
        Assert.AreEqual(0, second.Commands.Length, "a new list holds nothing of the list whose storage it reuses");
        var open = device.BeginCommands();
        second.UpdateBuffer(buffer, 0, new byte[8]);
        open.UpdateBuffer(buffer, 8, new byte[16]);
        Assert.AreEqual(1, second.Commands.Length);
        Assert.AreEqual(8, second.Payload(second.Commands[0]).Length);
        Assert.AreEqual(16, open.Payload(open.Commands[0]).Length, "two lists recorded at once do not share storage");
        device.Submit(second);
        device.Submit(open);
    }

    [TestMethod]
    public void Frames_drawn_one_after_another_do_not_leak_into_each_other()
    {
        using var device = new ReferenceGraphicsDevice();
        using var batcher = new Renderer2D(device);
        Devices.Texture red = device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFF0000FF)), green = device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0x00FF00FF));
        var target = device.CreateRenderTarget(new RenderTargetDescription(8, 8));
        var black = PremultipliedColor.FromBytes(0, 0, 0);

        batcher.Begin(new BatchSettings(target, Clear: black));
        for (int i = 0; i < 8; i++) batcher.Draw(red, new RectF(0, 0, 1, 1), Matrix3x2.CreateTranslation(i, 0), ColorF.White);
        batcher.End();
        batcher.Begin(new BatchSettings(target, Clear: black));
        batcher.Draw(green, new RectF(0, 0, 1, 1), Matrix3x2.CreateTranslation(2, 0), ColorF.White);
        batcher.End();

        var request = device.ReadPixels(target, target.Bounds);
        request.Wait();
        var image = new Image(8, 8, request.Pixels.ToArray());
        Assert.AreEqual(0x00FF00FFu, image[2, 0], "the second frame's sprite");
        Assert.AreEqual(0x000000FFu, image[1, 0], "nothing of the first frame's eight sprites");
        Assert.AreEqual(0x000000FFu, image[3, 0]);
        Assert.AreEqual(new Renderer2DStatistics(Sprites: 1, DrawCalls: 1, Flushes: 1), batcher.Statistics);
    }
}

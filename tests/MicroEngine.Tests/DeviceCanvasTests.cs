using System.Numerics;
using MicroEngine.Graphics;
using Dev = MicroEngine.Graphics.Devices;

namespace MicroEngine.Tests;

/// <summary>
/// The drawing API as a façade over the 2D batcher on a graphics device (what the SDL and raylib platforms use), held
/// to the renderer rules on the CPU reference device.
/// </summary>
[TestClass]
public sealed class DeviceCanvasTests
{
    private static readonly Color Bg = new(10, 20, 30);

    private static PixelImage Frame(int w, int h, Action<IRenderer, IGraphicsDevice> draw, out Renderer2DStatistics statistics)
    {
        using var device = new Dev.ReferenceGraphicsDevice();
        using var canvas = new DeviceCanvas(device, w, h);
        var r = canvas.BeginFrame();
        r.Clear(Bg);
        draw(r, canvas.Graphics);
        canvas.EndFrame();
        statistics = canvas.Renderer.Statistics;
        return canvas.Capture();
    }

    private static PixelImage Frame(int w, int h, Action<IRenderer, IGraphicsDevice> draw) => Frame(w, h, draw, out _);

    [TestMethod]
    public void Rectangles_and_sprites_cover_the_pixels_whose_centres_they_contain()
    {
        var f = Frame(8, 8, (r, g) =>
        {
            r.FillRect(new RectF(2.5f, 2.5f, 1, 1), Color.White);
            var t = g.CreateTexture(new PixelImage(2, 1).Clear(new Color(255, 0, 0)));
            r.Draw(t, t.Bounds, new Vector2(5, 5), Color.White, new DrawOptions(FlipX: true));
        });
        Assert.AreEqual(Color.White, f[2, 2]);
        Assert.AreEqual(Bg, f[3, 3]);
        Assert.AreEqual(new Color(255, 0, 0), f[5, 5]);
        Assert.AreEqual(new Color(255, 0, 0), f[6, 5]);
        Assert.AreEqual(Bg, f[7, 5]);
    }

    [TestMethod]
    public void Blend_modes_follow_the_written_premultiplied_equations()
    {
        var f = Frame(12, 4, (r, _) =>
        {
            r.FillRect(new RectF(0, 0, 12, 4), new Color(128, 128, 128));
            r.FillRect(new RectF(0, 0, 4, 4), new Color(255, 0, 0, 128));
            r.SetBlendMode(BlendMode.Additive);
            r.FillRect(new RectF(4, 0, 4, 4), new Color(100, 0, 0, 128));
            r.SetBlendMode(BlendMode.Multiply);
            r.FillRect(new RectF(8, 0, 4, 4), new Color(255, 0, 0, 128));
        });
        Assert.AreEqual(new Color(192, 64, 64), f[1, 1], "alpha");
        Assert.AreEqual(new Color(178, 128, 128), f[5, 1], "additive");
        Assert.AreEqual(new Color(128, 64, 64), f[9, 1], "multiply: s·a·d + d·(1 − a)");
    }

    [TestMethod]
    public void Clear_ignores_the_clip_which_still_applies_after_it()
    {
        var f = Frame(8, 8, (r, _) =>
        {
            r.FillRect(new RectF(0, 0, 8, 8), Color.White);
            r.SetClip(new RectF(0, 0, 4.9f, 4.9f));
            r.Clear(new Color(255, 0, 0));
            r.FillRect(new RectF(0, 0, 8, 8), new Color(0, 255, 0));
            r.SetClip(null);
        });
        Assert.AreEqual(new Color(0, 255, 0), f[3, 3], "inside the truncated clip");
        Assert.AreEqual(new Color(255, 0, 0), f[4, 4], "the clear covered everything; the clip (4 × 4) kept the green out");
    }

    [TestMethod]
    public void The_camera_moves_draws_and_zoom_scales_them()
    {
        var camera = new Camera2D(8, 8) { Position = new Vector2(2, 0) };
        var f = Frame(8, 8, (r, _) =>
        {
            r.PushCamera(camera);
            r.FillRect(new RectF(3, 1, 1, 1), Color.White);
            r.PopCamera();
        });
        Assert.AreEqual(Color.White, f[1, 1]);
        var zoomed = new Camera2D(8, 8) { Zoom = 2 };
        var z = Frame(8, 8, (r, _) =>
        {
            r.PushCamera(zoomed);
            r.FillRect(new RectF(4, 4, 1, 1), Color.White);
            r.PopCamera();
        });
        Assert.AreEqual(Color.White, z[4, 4]);
        Assert.AreEqual(Color.White, z[5, 5], "one world pixel is two canvas pixels");
    }

    [TestMethod]
    public void Disposed_and_foreign_textures_throw_and_draw_nothing()
    {
        Frame(4, 4, (r, g) =>
        {
            var t = g.CreateTexture(new PixelImage(1, 1));
            t.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => r.Draw(t, t.Bounds, Vector2.Zero, Color.White));
            var foreign = new Core.HeadlessGraphicsDevice().CreateTexture(new PixelImage(1, 1));
            Assert.ThrowsExactly<ArgumentException>(() => r.Draw(foreign, foreign.Bounds, Vector2.Zero, Color.White));
        });
    }

    [TestMethod]
    public void Draws_of_one_texture_share_a_draw_call_and_the_counts_are_exact()
    {
        Frame(16, 16, (r, g) =>
        {
            var atlas = g.CreateTexture(new PixelImage(4, 4).Clear(Color.White));
            for (int i = 0; i < 10; i++) r.Draw(atlas, new RectF(0, 0, 2, 2), new Vector2(i, 0), Color.White);
            r.FillRect(new RectF(0, 8, 4, 4), new Color(255, 0, 0)); // the batcher's white texel: a second texture
            r.Draw(atlas, new RectF(0, 0, 2, 2), new Vector2(0, 12), Color.White);
        }, out var statistics);
        Assert.AreEqual(new Renderer2DStatistics(Sprites: 12, DrawCalls: 3, Flushes: 1), statistics,
            "10 atlas draws, one rectangle (the white texel), one atlas draw; the opening clear joins the frame's one pass");
    }

    [TestMethod]
    public void Captures_are_opaque_and_store_the_canvas_premultiplied()
    {
        var f = Frame(4, 4, (r, _) => r.Clear(new Color(200, 100, 50, 128)));
        Assert.AreEqual(new Color(100, 50, 25), f[0, 0]);
        Assert.IsTrue(f.Pixels.ToArray().All(c => c.A == 255));
    }

    /// <summary>One pass of device work into <paramref name="target"/>: an opaque quad, drawn after whatever was submitted before it.</summary>
    private static void DeviceQuad(Dev.IGraphicsDevice device, Dev.RenderTarget target, RectF rect, ColorF color)
    {
        using var white = device.CreateTexture(new Dev.TextureDescription(1, 1), [255, 255, 255, 255]);
        using var batch = new Renderer2D(device);
        batch.Begin(new BatchSettings(target, Blend: Dev.BlendState.Opaque));
        batch.Draw(white, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(rect.Width, rect.Height) * Matrix3x2.CreateTranslation(rect.X, rect.Y), color);
        batch.End();
    }

    [TestMethod]
    public void Flush_puts_device_work_between_the_2D_drawn_before_and_after_it()
    {
        // Clear, red on the left, Flush, device work (green over the whole canvas), Flush, blue on the right: the green
        // covers the red (it was submitted after it) and the blue covers the green (recorded after it).
        using var device = new Dev.ReferenceGraphicsDevice();
        using var canvas = new DeviceCanvas(device, 12, 4);
        var r = canvas.BeginFrame();
        var renderer = canvas.Renderer;
        Assert.AreSame(canvas.Target, renderer.Target, "during the frame the renderer draws into the canvas");
        r.Clear(Bg);
        r.FillRect(new RectF(0, 0, 4, 4), new Color(255, 0, 0));
        renderer.Flush();
        DeviceQuad(device, renderer.Target, new RectF(0, 0, 8, 4), new ColorF(0, 1, 0, 1));
        renderer.Flush(); // nothing recorded since the last flush: submits nothing
        r.FillRect(new RectF(8, 0, 4, 4), new Color(0, 0, 255));
        canvas.EndFrame();
        var f = canvas.Capture();
        Assert.AreEqual(new Color(0, 255, 0), f[1, 1], "device work after the first flush covers the red drawn before it");
        Assert.AreEqual(new Color(0, 255, 0), f[5, 1]);
        Assert.AreEqual(new Color(0, 0, 255), f[9, 1], "2D recorded after the flush lands after the device work");
        Assert.AreEqual(2, canvas.Renderer.Statistics.Flushes, "one flush with the clear and the red, one at the end with the blue");
    }

    [TestMethod]
    public void Without_Flush_device_work_submitted_mid_frame_lands_before_the_frames_2D()
    {
        // The batch is deferred: what it recorded reaches the device only at Flush or End, so device work submitted in
        // between without a flush is covered by the frame's clear. This is why Flush exists.
        using var device = new Dev.ReferenceGraphicsDevice();
        using var canvas = new DeviceCanvas(device, 4, 4);
        var r = canvas.BeginFrame();
        r.Clear(Bg);
        DeviceQuad(device, canvas.Renderer.Target, new RectF(0, 0, 4, 4), new ColorF(0, 1, 0, 1));
        canvas.EndFrame();
        Assert.AreEqual(Bg, canvas.Capture()[1, 1]);
        Assert.AreEqual(1, canvas.Renderer.Statistics.Flushes, "a frame without Flush still submits once");
    }

    [TestMethod]
    public void Flush_outside_a_frame_does_nothing()
    {
        using var device = new Dev.ReferenceGraphicsDevice();
        using var canvas = new DeviceCanvas(device, 4, 4);
        var before = device.Counters.Submissions;
        canvas.Renderer.Flush();
        Assert.AreEqual(before, device.Counters.Submissions);
    }
}

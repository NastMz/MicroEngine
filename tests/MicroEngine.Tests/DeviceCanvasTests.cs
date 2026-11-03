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
}

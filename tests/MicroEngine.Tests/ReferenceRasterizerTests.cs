using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace MicroEngine.Tests;

/// <summary>
/// The rasterizing headless renderer (the CPU reference) follows each written rendering rule exactly, records its
/// draws as text, and enforces the resource rules every renderer shares. The counting renderer keeps its behaviour.
/// </summary>
[TestClass]
public sealed class ReferenceRasterizerTests
{
    private static readonly Color Bg = new(10, 20, 30);

    private static (HeadlessRenderer Renderer, HeadlessGraphicsDevice Device) Reference(int width = 16, int height = 16)
    {
        var device = new HeadlessGraphicsDevice { KeepsPixels = true };
        var renderer = new HeadlessRenderer(width, height, device);
        renderer.Reset();
        renderer.Clear(Bg);
        return (renderer, device);
    }

    private static List<(int X, int Y)> Lit(PixelImage frame)
    {
        var lit = new List<(int, int)>();
        for (int y = 0; y < frame.Height; y++)
            for (int x = 0; x < frame.Width; x++)
                if (frame[x, y] != Bg) lit.Add((x, y));
        return lit;
    }

    [TestMethod]
    public void Clear_replaces_the_whole_canvas_whatever_the_clip_and_blend_mode()
    {
        var (r, _) = Reference();
        r.FillRect(new RectF(0, 0, 16, 16), Color.White);
        r.SetClip(new RectF(0, 0, 4, 4));
        r.SetBlendMode(BlendMode.Additive);
        r.Clear(new Color(255, 0, 0, 128));
        r.SetBlendMode(BlendMode.Alpha);
        r.FillRect(new RectF(0, 0, 16, 16), new Color(0, 255, 0));
        var frame = r.Frame!;
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                Assert.AreEqual(x < 4 && y < 4 ? new Color(0, 255, 0) : new Color(128, 0, 0), frame[x, y], $"({x},{y}): the clear stored premultiplied");
    }

    [TestMethod]
    public void Rectangles_cover_the_pixels_whose_centres_they_contain_with_the_top_left_rule()
    {
        var (r, _) = Reference(32, 16);
        r.FillRect(new RectF(6.5f, 2.5f, 1, 1), Color.White);
        r.FillRect(new RectF(10.25f, 6.25f, 2, 1), Color.White);
        r.FillRect(new RectF(16.6f, 2.1f, 0.3f, 0.3f), Color.White);
        r.FillRect(new RectF(20, 9.5f, 3, 2), Color.White);
        r.FillRect(new RectF(1, 12, 2, 2), Color.Transparent);
        CollectionAssert.AreEquivalent(new[] { (6, 2), (10, 6), (11, 6), (20, 9), (21, 9), (22, 9), (20, 10), (21, 10), (22, 10) }, Lit(r.Frame!));
    }

    [TestMethod]
    public void Sprites_sample_the_texel_under_each_covered_centre_with_flips_scale_and_rotation()
    {
        var (r, g) = Reference(32, 16);
        var image = new PixelImage(2, 1);
        image[0, 0] = new Color(255, 0, 0);
        image[1, 0] = new Color(0, 255, 0);
        var pair = g.CreateTexture(image, TextureFilter.Nearest);
        r.Draw(pair, pair.Bounds, new Vector2(0.5f, 0), Color.White, new DrawOptions(Scale: new Vector2(2, 1)));
        r.Draw(pair, pair.Bounds, new Vector2(8, 0), Color.White, new DrawOptions(FlipX: true));
        r.Draw(pair, pair.Bounds, new Vector2(12, 4), Color.White, new DrawOptions(Rotation: MathF.PI / 2));
        var f = r.Frame!;
        Assert.AreEqual(new Color(255, 0, 0), f[0, 0]);
        Assert.AreEqual(new Color(255, 0, 0), f[1, 0]);
        Assert.AreEqual(new Color(0, 255, 0), f[2, 0]);
        Assert.AreEqual(new Color(0, 255, 0), f[3, 0]);
        Assert.AreEqual(Bg, f[4, 0]);
        Assert.AreEqual(new Color(0, 255, 0), f[8, 0]);
        Assert.AreEqual(new Color(255, 0, 0), f[9, 0]);
        // A quarter turn clockwise about the top-left: the 2×1 strip becomes 1×2, going down from (11, 4).
        Assert.AreEqual(new Color(255, 0, 0), f[11, 4]);
        Assert.AreEqual(new Color(0, 255, 0), f[11, 5]);
        Assert.HasCount(4 + 2 + 2, Lit(f), "the scaled sprite covers 4 pixels, the flipped one 2, the rotated one 2");
    }

    [TestMethod]
    public void Linear_sampling_mixes_the_four_texels_around_the_centre_and_clamps_at_the_edges()
    {
        var (r, g) = Reference(32, 4);
        var image = new PixelImage(2, 1);
        image[0, 0] = Color.Black;
        image[1, 0] = Color.White;
        var pair = g.CreateTexture(image, TextureFilter.Linear);
        r.Draw(pair, pair.Bounds, Vector2.Zero, Color.White, new DrawOptions(Scale: new Vector2(16, 4)));
        var f = r.Frame!;
        for (int x = 0; x < 32; x++)
        {
            double u = (x + 0.5) / 16 - 0.5;
            Assert.AreEqual((int)Math.Round(Math.Clamp(u, 0, 1) * 255, MidpointRounding.AwayFromZero), f[x, 1].R, $"pixel {x}");
        }
    }

    [TestMethod]
    public void Blending_follows_the_written_premultiplied_alpha_equations_rounded_to_nearest()
    {
        var (r, g) = Reference(32, 4);
        r.FillRect(new RectF(0, 0, 32, 4), new Color(128, 128, 128));
        r.FillRect(new RectF(0, 0, 4, 4), new Color(255, 0, 0, 128));
        r.SetBlendMode(BlendMode.Additive);
        r.FillRect(new RectF(4, 0, 4, 4), new Color(100, 0, 0, 128));
        r.SetBlendMode(BlendMode.Multiply);
        r.FillRect(new RectF(8, 0, 4, 4), new Color(255, 0, 0, 128));
        r.SetBlendMode(BlendMode.Alpha);
        var tex = g.CreateTexture(new PixelImage(2, 2).Clear(new Color(0, 200, 100)));
        r.Draw(tex, tex.Bounds, new Vector2(12, 0), new Color(255, 255, 255, 128));
        var f = r.Frame!;
        Assert.AreEqual(new Color(192, 64, 64), f[1, 1], "alpha: 255·0.502 + 128·0.498");
        Assert.AreEqual(new Color(178, 128, 128), f[5, 1], "additive: 100·0.502 + 128");
        Assert.AreEqual(new Color(128, 64, 64), f[9, 1], "multiply: 255·0.502·128/255 + 128·0.498");
        Assert.AreEqual(new Color(64, 164, 114), f[12, 0], "tinted sprite");
    }

    [TestMethod]
    public void The_clip_is_truncated_to_whole_screen_pixels_and_ignores_the_camera()
    {
        var (r, _) = Reference(32, 16);
        r.PushCamera(new Camera2D(32, 16) { Position = new Vector2(10, 0) });
        r.SetClip(new RectF(2.9f, 3.9f, 2.9f, 1.9f));
        r.FillRect(new RectF(0, 0, 64, 16), Color.White);
        r.SetClip(null);
        r.PopCamera();
        CollectionAssert.AreEquivalent(new[] { (2, 3), (3, 3) }, Lit(r.Frame!));
    }

    [TestMethod]
    public void Circles_cover_the_centres_strictly_inside_the_disc()
    {
        var (r, _) = Reference();
        r.FillCircle(new Vector2(8, 8), 2, Color.White);
        // Centres at distance < 2 from (8, 8): the 4 around the centre and the 8 next to them (distance 1.58).
        var lit = Lit(r.Frame!);
        Assert.HasCount(12, lit);
        Assert.IsTrue(lit.All(p => Vector2.Distance(new Vector2(p.X + 0.5f, p.Y + 0.5f), new Vector2(8, 8)) < 2));
    }

    [TestMethod]
    public void Triangles_and_lines_cover_their_interior_with_either_winding()
    {
        var (a, _) = Reference();
        a.FillTriangle(new Vector2(0, 0), new Vector2(8, 0), new Vector2(0, 8), Color.White);
        var (b, _) = Reference();
        b.FillTriangle(new Vector2(0, 0), new Vector2(0, 8), new Vector2(8, 0), Color.White);
        var lit = Lit(a.Frame!);
        CollectionAssert.AreEquivalent(lit, Lit(b.Frame!), "the winding does not matter");
        // Centres with x + y + 1 < 8; the 8 centres on the hypotenuse (a right edge) are outside by the top-left rule.
        Assert.HasCount(28, lit);
        var (line, _) = Reference();
        line.DrawLine(new Vector2(2, 8), new Vector2(14, 8), 2, Color.White);
        CollectionAssert.AreEquivalent(Enumerable.Range(2, 12).SelectMany(x => new[] { (x, 7), (x, 8) }).ToArray(), Lit(line.Frame!));
    }

    [TestMethod]
    public void Drawing_a_disposed_texture_throws_and_draws_nothing_on_both_renderers()
    {
        var (r, g) = Reference();
        var texture = g.CreateTexture(new PixelImage(2, 2).Clear(Color.White));
        texture.Dispose();
        Assert.ThrowsExactly<ObjectDisposedException>(() => r.Draw(texture, texture.Bounds, Vector2.Zero, Color.White));
        Assert.ThrowsExactly<ObjectDisposedException>(() => r.DrawStretched(texture, texture.Bounds, new RectF(0, 0, 4, 4), Color.White));
        Assert.IsEmpty(Lit(r.Frame!));
        Assert.HasCount(1, r.Commands, "only the clear is recorded");
        var counting = new HeadlessRenderer(16, 16);
        Assert.ThrowsExactly<ObjectDisposedException>(() => counting.Draw(texture, texture.Bounds, Vector2.Zero, Color.White));
        Assert.ThrowsExactly<ObjectDisposedException>(() => counting.DrawStretched(texture, texture.Bounds, new RectF(0, 0, 4, 4), Color.White));
        Assert.AreEqual(0, counting.DrawCalls);
    }

    [TestMethod]
    public void Drawing_another_devices_texture_throws_a_clear_argument_exception()
    {
        var (r, _) = Reference();
        var other = new HeadlessGraphicsDevice { KeepsPixels = true };
        var texture = other.CreateTexture(new PixelImage(2, 2).Clear(Color.White));
        var e = Assert.ThrowsExactly<ArgumentException>(() => r.Draw(texture, texture.Bounds, Vector2.Zero, Color.White));
        StringAssert.Contains(e.Message, "another graphics device");
        Assert.AreEqual("texture", e.ParamName);
        Assert.ThrowsExactly<ArgumentException>(() => r.DrawStretched(texture, texture.Bounds, new RectF(0, 0, 4, 4), Color.White));
        Assert.IsEmpty(Lit(r.Frame!));
    }

    [TestMethod]
    public void The_counting_renderer_still_accepts_any_headless_texture_and_records_nothing()
    {
        var renderer = new HeadlessRenderer(16, 16);
        var texture = new HeadlessGraphicsDevice().CreateTexture(new PixelImage(2, 2));
        renderer.Draw(texture, texture.Bounds, Vector2.Zero, Color.White);
        renderer.DrawStretched(texture, texture.Bounds, new RectF(0, 0, 4, 4), Color.White);
        renderer.SetClip(new RectF(0, 0, 1, 1));
        renderer.SetBlendMode(BlendMode.Additive);
        Assert.AreEqual(2, renderer.DrawCalls);
        Assert.IsFalse(renderer.Rasterizes);
        Assert.IsEmpty(renderer.Commands);
        Assert.IsNull(renderer.Frame);
    }

    [TestMethod]
    public void A_rasterizing_renderer_needs_a_device_that_keeps_pixels()
    {
        var e = Assert.ThrowsExactly<ArgumentException>(() => new HeadlessRenderer(8, 8, new HeadlessGraphicsDevice()));
        StringAssert.Contains(e.Message, "KeepsPixels");
    }

    [TestMethod]
    public void Texture_updates_reach_the_kept_pixels()
    {
        var (r, g) = Reference(8, 8);
        var texture = g.CreateTexture(new PixelImage(4, 4).Clear(new Color(255, 0, 0)));
        texture.Update(1, 1, 2, 2, [Color.White, Color.White, Color.White, Color.White]);
        r.Draw(texture, texture.Bounds, Vector2.Zero, Color.White);
        Assert.AreEqual(new Color(255, 0, 0), r.Frame![0, 0]);
        Assert.AreEqual(Color.White, r.Frame![1, 1]);
        Assert.AreEqual(Color.White, r.Frame![2, 2]);
        Assert.AreEqual(new Color(255, 0, 0), r.Frame![3, 3]);
    }

    [TestMethod]
    public void Commands_record_each_draw_as_backend_neutral_text_in_screen_pixels()
    {
        var (r, g) = Reference(32, 16);
        var texture = g.CreateTexture(new PixelImage(4, 2), TextureFilter.Linear);
        r.PushCamera(new Camera2D(32, 16) { Position = new Vector2(-2, -1) });
        r.FillRect(new RectF(1, 1, 2.5f, 3), new Color(255, 0, 0));
        r.Draw(texture, texture.Bounds, new Vector2(4, 4), Color.White, new DrawOptions(FlipX: true, Scale: new Vector2(2)));
        r.PopCamera();
        r.SetBlendMode(BlendMode.Additive);
        r.FillCircle(new Vector2(8, 8), 1.5f, new Color(0, 0, 255, 128));
        r.SetClip(new RectF(0, 0, 8, 8));
        r.DrawLine(new Vector2(0, 0), new Vector2(4, 4), 1, Color.White);
        r.FillTriangle(new Vector2(0, 0), new Vector2(2, 0), new Vector2(0, 2), Color.Black);
        r.SetClip(null);
        CollectionAssert.AreEqual(new[]
        {
            "clear #0A141EFF",
            "camera 2,1",
            "rect 3,2 2.5x3 #FF0000FF",
            "sprite #1 4x2 linear src 0,0 4x2 dst 6,5 8x4 tint #FFFFFFFF flipx",
            "camera 0,0",
            "blend additive",
            "circle 8,8 r 1.5 #0000FF80",
            "clip 0,0 8x8",
            "line 0,0 4,4 w 1 #FFFFFFFF",
            "triangle 0,0 2,0 0,2 #000000FF",
            "clip none",
        }, r.Commands.ToArray(), string.Join("\n", r.Commands));
        r.Reset();
        Assert.IsEmpty(r.Commands, "each frame starts a new list");
    }

    [TestMethod]
    public void Each_frame_starts_with_alpha_blending_and_no_clip_while_the_canvas_keeps_its_pixels()
    {
        var (r, _) = Reference(8, 8);
        r.FillRect(new RectF(0, 0, 1, 1), Color.White);
        r.SetBlendMode(BlendMode.Additive);
        r.SetClip(new RectF(0, 0, 1, 1));
        r.Reset();
        r.FillRect(new RectF(4, 4, 1, 1), new Color(255, 0, 0, 128));
        Assert.AreEqual(Color.White, r.Frame![0, 0], "the canvas keeps the previous frame");
        Assert.AreEqual(BlendRef(Bg, new Color(255, 0, 0, 128)), r.Frame![4, 4], "alpha blending, outside the old clip");
    }

    private static Color BlendRef(Color d, Color s)
    {
        double a = s.A / 255.0;
        byte Q(double v) => (byte)Math.Round(v * 255, MidpointRounding.AwayFromZero);
        return new Color(Q(s.R / 255.0 * a + d.R / 255.0 * (1 - a)), Q(s.G / 255.0 * a + d.G / 255.0 * (1 - a)), Q(s.B / 255.0 * a + d.B / 255.0 * (1 - a)));
    }

    [TestMethod]
    public void Frames_and_captures_are_opaque()
    {
        var (r, _) = Reference(4, 4);
        r.Clear(new Color(200, 100, 50, 0));
        Assert.IsTrue(r.Frame!.Pixels.ToArray().All(c => c == new Color(0, 0, 0)), "a transparent clear stores premultiplied black; the capture is opaque");
    }

    private sealed class SquareGame : Game
    {
        public override GameSettings Settings { get; } = new("square", 16, 8, 1) { Background = Bg };
        protected override void Load() => Scenes.Push(new SquareScene());
    }

    private sealed class SquareScene : Scene
    {
        public override void Update(float dt) { }
        public override void Draw(IRenderer r) => r.FillRect(new RectF(2, 2, 3, 3), Color.White);
    }

    [TestMethod]
    public void A_rasterizing_headless_platform_writes_script_captures_as_png_files()
    {
        var game = new SquareGame();
        string dir = Path.Combine(Path.GetTempPath(), "microengine-reference-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var platform = HeadlessPlatform.For(game.Settings, rasterize: true);
            Assert.IsTrue(platform.Rasterize);
            var result = GameRunner.Run(game, platform, new RunOptions { Script = new InputScript().Capture(1, "frame.png").QuitAfter(2), CaptureDirectory = dir });
            Assert.IsNull(result.Error, result.Error?.ToString());
            var path = Path.Combine(dir, "frame.png");
            CollectionAssert.AreEqual(new[] { path }, platform.Screenshots);
            var capture = PixelImage.Load(path);
            Assert.AreEqual(16, capture.Width);
            Assert.AreEqual(8, capture.Height);
            Assert.AreEqual(Color.White, capture[2, 2]);
            Assert.AreEqual(Color.White, capture[4, 4]);
            Assert.AreEqual(Bg, capture[5, 5]);
            Assert.AreEqual(Bg, capture[1, 1]);
            Assert.IsTrue(platform.Renderer.Commands.Contains("rect 2,2 3x3 #FFFFFFFF"), string.Join("\n", platform.Renderer.Commands));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    [TestMethod]
    public void A_default_headless_platform_still_writes_no_capture_file()
    {
        var game = new SquareGame();
        string dir = Path.Combine(Path.GetTempPath(), "microengine-reference-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var platform = HeadlessPlatform.For(game.Settings);
            Assert.IsFalse(platform.Rasterize);
            GameRunner.Run(game, platform, new RunOptions { Script = new InputScript().Capture(1, "frame.png").QuitAfter(2), CaptureDirectory = dir });
            Assert.HasCount(1, platform.Screenshots);
            Assert.IsFalse(File.Exists(platform.Screenshots[0]));
            Assert.IsNull(platform.Renderer.Frame);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    [TestMethod]
    public void CheckDrawable_accepts_the_devices_own_textures_and_textures_that_name_no_device()
    {
        var device = new HeadlessGraphicsDevice();
        var own = device.CreateTexture(new PixelImage(1, 1));
        Assert.AreSame(device, own.Device);
        Assert.AreSame(own, Texture.CheckDrawable<Texture>(own, device));
        var anonymous = new AnonymousTexture();
        Assert.IsNull(anonymous.Device);
        Assert.AreSame(anonymous, Texture.CheckDrawable<AnonymousTexture>(anonymous, device));
        Assert.ThrowsExactly<ArgumentException>(() => Texture.CheckDrawable<AnonymousTexture>(own, device));
        Assert.ThrowsExactly<ArgumentException>(() => Texture.CheckDrawable<Texture>(own, new HeadlessGraphicsDevice()));
        Assert.ThrowsExactly<ArgumentNullException>(() => Texture.CheckDrawable<Texture>(null!, device));
    }

    private sealed class AnonymousTexture() : Texture(1, 1)
    {
        protected override void Upload(int x, int y, int width, int height, ReadOnlySpan<Color> pixels) { }
        protected override void ApplyFilter(TextureFilter filter) { }
        protected override void Release() { }
    }
}

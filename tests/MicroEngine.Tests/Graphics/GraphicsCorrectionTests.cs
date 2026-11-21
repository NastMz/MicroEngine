using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Testing;
using MicroEngine.World;
using Dev = MicroEngine.Graphics.Devices;

namespace MicroEngine.Tests;

/// <summary>
/// The edges of post-processing chains and sorted drawing: effect values that are not numbers or are out of range, one
/// name used for two kinds of resource, one chain on pictures of several sizes in a frame, a camera that moves between
/// two sorted draws, the memory a sorted block keeps, and text and tile maps drawn through a sorted renderer.
/// </summary>
[TestClass]
public sealed class GraphicsCorrectionTests
{
    private static readonly Color White = Color.White, Red = new(255, 0, 0), Bg = new(10, 20, 30);

    private static (HeadlessRenderer Renderer, HeadlessGraphicsDevice Graphics) Headless(int width = 16, int height = 8)
    {
        var graphics = new HeadlessGraphicsDevice { KeepsPixels = true };
        return (new HeadlessRenderer(width, height, graphics), graphics);
    }

    // ---- Effect values -------------------------------------------------------------------------------------------------

    [TestMethod]
    [Timeout(10000)]
    public void A_strength_or_brightness_without_limit_is_clamped_and_the_effect_ends()
    {
        var (r, graphics) = Headless();
        foreach (float huge in new[] { float.PositiveInfinity, 1e8f, 16777216f })
        {
            var bloom = new Bloom { Strength = huge };
            var grade = new ColorGrade { Brightness = huge };
            var chain = new PostProcess().Add(bloom).Add(grade);
            chain.Begin(r, graphics, new Color(20, 20, 20));
            chain.End(r, opaque: true);                       // used to add the picture to itself without end
            Assert.AreEqual(White, r.Frame![4, 4], "sixteen times dark grey is white");
            chain.Dispose();
            Assert.AreEqual(Bloom.MaxStrength, bloom.Strength, $"a strength of {huge} is the largest strength");
            Assert.AreEqual(ColorGrade.MaxBrightness, grade.Brightness, $"a brightness of {huge} is the largest brightness");
        }
        Assert.AreEqual(0f, new Bloom { Strength = float.NegativeInfinity }.Strength);
        Assert.AreEqual(0f, new ColorGrade { Brightness = -5 }.Brightness);
    }

    [TestMethod]
    public void A_value_that_is_not_a_number_is_ignored_by_every_effect_and_every_value_stays_in_its_range()
    {
        // Each value: its setter, its getter, and the range it is kept in.
        var grade = new ColorGrade();
        var blur = new Blur();
        var bloom = new Bloom();
        var vignette = new Vignette();
        var lines = new Scanlines();
        var crt = new Crt();
        (string Name, Action<float> Set, Func<float> Get, float Min, float Max)[] values =
        [
            ("ColorGrade.Brightness", v => grade.Brightness = v, () => grade.Brightness, 0, ColorGrade.MaxBrightness),
            ("ColorGrade.Contrast", v => grade.Contrast = v, () => grade.Contrast, 0, 16),
            ("ColorGrade.Saturation", v => grade.Saturation = v, () => grade.Saturation, 0, 16),
            ("Blur.Radius", v => blur.Radius = v, () => blur.Radius, 0, Blur.MaxRadius),
            ("Bloom.Threshold", v => bloom.Threshold = v, () => bloom.Threshold, 0, 1),
            ("Bloom.Strength", v => bloom.Strength = v, () => bloom.Strength, 0, Bloom.MaxStrength),
            ("Bloom.Radius", v => bloom.Radius = v, () => bloom.Radius, 0, Blur.MaxRadius),
            ("Vignette.Strength", v => vignette.Strength = v, () => vignette.Strength, 0, 1),
            ("Vignette.Radius", v => vignette.Radius = v, () => vignette.Radius, 0, 1),
            ("Vignette.Softness", v => vignette.Softness = v, () => vignette.Softness, 0, 4),
            ("Scanlines.Strength", v => lines.Strength = v, () => lines.Strength, 0, 1),
            ("Crt.Curvature", v => crt.Curvature = v, () => crt.Curvature, 0, 1),
            ("Crt.Scanlines", v => crt.Scanlines = v, () => crt.Scanlines, 0, 1),
            ("Crt.Mask", v => crt.Mask = v, () => crt.Mask, 0, 1),
        ];
        foreach (var (name, set, get, min, max) in values)
        {
            float before = get();
            set(float.NaN);
            Assert.AreEqual(before, get(), $"{name}: not a number is ignored, the value stays");
            set(float.PositiveInfinity);
            Assert.AreEqual(max, get(), $"{name}: no more than its largest value");
            set(float.NegativeInfinity);
            Assert.AreEqual(min, get(), $"{name}: no less than its smallest value");
            set(before);
            Assert.AreEqual(before, get(), $"{name}: an ordinary value is kept as given");
        }
        lines.Lines = -3;
        Assert.AreEqual(0, lines.Lines, "a count of lines below zero is zero: the default");
        crt.Lines = int.MaxValue;
        Assert.AreEqual(Scanlines.MaxLines, crt.Lines);

        // The picture: a brightness that is not a number changes nothing, where it used to draw black.
        var (r, graphics) = Headless();
        var chain = new PostProcess().Add(new ColorGrade { Brightness = float.NaN, Tint = new Color(255, 128, 0) });
        chain.Begin(r, graphics, new Color(200, 200, 200));
        chain.End(r, opaque: true);
        var c = r.Frame![4, 4];
        Assert.IsTrue(Math.Abs(c.R - 200) <= 1 && Math.Abs(c.G - 100) <= 1 && c.B == 0, $"the tint alone: {c}");
    }

    // ---- What a chain keeps --------------------------------------------------------------------------------------------

    private sealed class SameName : PostEffect
    {
        public override PostEffectSupport SupportOn(IGraphicsDevice graphics) => PostEffectSupport.Full;
        protected internal override void Apply(PostPass pass)
        {
            pass.Scratch("x", 4, 4);
            pass.Image("x", 1, 0, static _ => new PixelImage(2, 2));
        }
    }

    [TestMethod]
    public void One_name_for_a_target_and_a_texture_of_the_same_effect_is_refused_and_nothing_is_left_alive()
    {
        var (r, graphics) = Headless();
        var chain = new PostProcess().Add(new SameName());
        for (int frame = 0; frame < 20; frame++)
        {
            r.Reset();
            chain.Begin(r, graphics, Bg);
            var error = Assert.ThrowsExactly<InvalidOperationException>(() => chain.End(r, opaque: true));
            StringAssert.Contains(error.Message, "'x'");
            StringAssert.Contains(error.Message, "SameName");
        }
        Assert.IsTrue(graphics.LiveTextures <= 4, $"twenty frames made nothing more than the first: {graphics.LiveTextures}");
        chain.Dispose();
        Assert.AreEqual(0, graphics.LiveTextures, "the chain released everything it made");
    }

    [TestMethod]
    public void One_chain_runs_on_pictures_of_different_sizes_in_one_frame_and_releases_the_sizes_it_stops_using()
    {
        using var device = new Dev.ReferenceGraphicsDevice();
        using var canvas = new DeviceCanvas(device, 32, 16);
        var graphics = canvas.Graphics;
        var chain = new PostProcess().Add(new Blur { Radius = 4 }).Add(new Scanlines { Strength = 1 }).Add(new Vignette());
        var small = graphics.CreateRenderTarget(8, 8, TextureFilter.Nearest);
        var large = graphics.CreateRenderTarget(16, 12, TextureFilter.Nearest);
        void Frame(bool both)
        {
            var r = canvas.BeginFrame();
            foreach (var picture in both ? new[] { small, large } : [small])
            {
                r.PushTarget(picture);
                r.Clear(White);
                r.PopTarget();
            }
            chain.Apply(r, graphics, small.Texture, new RectF(0, 0, 8, 8), opaque: true);
            if (both) chain.Apply(r, graphics, large.Texture, new RectF(16, 0, 16, 12), opaque: true);
            canvas.EndFrame();
        }
        Frame(both: true);
        var frame = canvas.Capture();
        Assert.AreEqual(Color.Black, frame[4, 1], "the small picture went through the chain: its dark rows");
        Assert.AreEqual(Color.Black, frame[24, 1], "and so did the large one, in the same frame");
        int two = graphics.LiveTextures;
        for (int i = 0; i < 50; i++) Frame(both: true);
        Assert.AreEqual(two, graphics.LiveTextures, "fifty more frames of the same two sizes make nothing new");

        for (int i = 0; i < 20; i++) Frame(both: false);
        Assert.IsTrue(graphics.LiveTextures < two, $"what was made for the size no longer used is released: {graphics.LiveTextures} of {two}");
        int one = graphics.LiveTextures;
        for (int i = 0; i < 20; i++) Frame(both: false);
        Assert.AreEqual(one, graphics.LiveTextures);
    }

    // ---- Sorted drawing ------------------------------------------------------------------------------------------------

    [TestMethod]
    public void A_sorted_draw_keeps_the_camera_as_it_was_when_the_draw_was_made()
    {
        PixelImage Picture(bool sort)
        {
            var (canvas, _) = Headless(16, 8);
            canvas.Clear(Color.Black);
            var sorted = new SortedRenderer();
            IRenderer r = sort ? sorted.Begin(canvas) : canvas;
            var camera = new Camera2D(16, 8) { MaxShake = 0 };
            camera.Position = new Vector2(0, 0);
            r.PushCamera(camera);
            r.FillRect(new RectF(0, 0, 2, 2), White);
            r.PopCamera();
            camera.Position = new Vector2(-8, 0);          // the same camera, moved: the world is 8 pixels to the right
            r.PushCamera(camera);
            r.FillRect(new RectF(0, 4, 2, 2), Red);
            r.PopCamera();
            camera.Position = new Vector2(-12, 0);         // and moved again before the block ends
            if (sort) sorted.End();
            return canvas.Frame!;
        }
        var direct = Picture(sort: false);
        Assert.AreEqual(White, direct[0, 0]);
        Assert.AreEqual(Red, direct[8, 4]);
        var viaSorted = Picture(sort: true);
        CollectionAssert.AreEqual(direct.Pixels.ToArray(), viaSorted.Pixels.ToArray(), "sorted drawing places each draw where direct drawing does");
    }

    [TestMethod]
    public void A_sorted_block_allocates_nothing_once_it_has_its_memory()
    {
        var canvas = new HeadlessRenderer(64, 64);                 // counts draws; nothing is rasterized
        var sorted = new SortedRenderer();
        var camera = new Camera2D(64, 64) { MaxShake = 0 };
        var texture = new HeadlessGraphicsDevice().CreateTexture(new PixelImage(2, 2), TextureFilter.Nearest);
        void Frame()
        {
            canvas.Reset();
            var r = sorted.Begin(canvas);
            r.PushState(DrawState.Alpha);                          // one outer state around everything
            for (int i = 0; i < 500; i++)
            {
                r.Depth = (i * 37) % 101;
                r.PushState(DrawState.Additive);                   // each draw in a state of its own
                r.FillRect(new RectF(i % 60, i % 60, 2, 2), White);
                r.PopState();
            }
            r.PushCamera(camera);
            r.Draw(texture, texture.Bounds, new Vector2(3, 3), White);
            r.PopCamera();
            r.PopState();
            sorted.End();
        }
        Frame();
        AllocationAssert.None(Frame, warmup: 5, calls: 20, because: "a sorted renderer keeps its memory between frames");
    }

    [TestMethod]
    public void Text_and_a_tile_map_draw_through_a_sorted_renderer_as_they_do_directly()
    {
        var layer = new TileLayer(8, 4);
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 8; x++) layer[x, y] = (x + y) % 2 + 1;
        PixelImage Picture(bool sort)
        {
            var (canvas, graphics) = Headless(64, 32);
            canvas.Clear(Bg);
            var font = BitmapFont.CreateDefault(graphics);
            var tiles = new[] { new Color(60, 120, 60), new Color(120, 60, 60) }
                .Select(colour => new Sprite(graphics.CreateTexture(new PixelImage(8, 8).Clear(colour), TextureFilter.Nearest), new RectF(0, 0, 8, 8), Vector2.Zero)).ToArray();
            var camera = new Camera2D(64, 32) { MaxShake = 0, Position = new Vector2(4, 2) };
            void Map(IRenderer r) => TileRenderer.Draw(r, layer, 8, camera, (_, x, y) => tiles[layer[x, y] - 1]);
            void Label(IRenderer r) => font.Draw(r, "SORTED", new Vector2(6, 10), White);
            if (!sort)
            {
                canvas.PushCamera(camera);
                Map(canvas);
                canvas.PopCamera();
                canvas.FillRect(new RectF(4, 8, 40, 10), Color.Black.WithAlpha(0.5f));
                Label(canvas);
            }
            else
            {
                // Called in the wrong order for a painter: the text first, then its backing, then the map under both.
                var sorted = new SortedRenderer();
                var r = sorted.Begin(canvas);
                r.At(layer: 2);
                Label(r);
                r.At(layer: 1);
                r.FillRect(new RectF(4, 8, 40, 10), Color.Black.WithAlpha(0.5f));
                r.At(layer: 0);
                r.PushCamera(camera);
                Map(r);
                r.PopCamera();
                Assert.IsTrue(sorted.Count > 8 * 4 / 2, $"the tiles and the glyphs were recorded: {sorted.Count}");
                sorted.End();
            }
            return canvas.Frame!;
        }
        var direct = Picture(sort: false);
        Assert.IsTrue(direct.Pixels.ToArray().Contains(White), "the text is in the picture");
        CollectionAssert.AreEqual(direct.Pixels.ToArray(), Picture(sort: true).Pixels.ToArray());
    }
}

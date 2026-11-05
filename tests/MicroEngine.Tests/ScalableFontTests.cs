using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace MicroEngine.Tests;

/// <summary>TTF and OTF fonts at any size (FontStashSharp, ADR 0025), with the AssetGallery sample's real font files.</summary>
[TestClass]
public sealed class ScalableFontTests
{
    private static string Gallery(string path) => Path.Combine(AppContext.BaseDirectory, "Gallery", path);
    private static ScalableFont Inter(HeadlessGraphicsDevice device, FontOptions? options = null) => ScalableFont.Load(device, Gallery("fonts/Inter-Regular.ttf"), options);

    [TestMethod]
    public void Text_measures_wider_and_taller_at_larger_sizes_and_each_size_is_one_shared_object()
    {
        using var font = Inter(new HeadlessGraphicsDevice());
        var small = font.Measure("Hello, world", 16);
        var large = font.Measure("Hello, world", 48);
        Assert.IsTrue(small.X > 40 && small.X < 140, $"16 px text should be about 90 px wide, got {small.X}");
        Assert.AreEqual(3, large.X / small.X, 0.15, "width scales with the size");
        Assert.IsTrue(font.LineHeight(48) > font.LineHeight(16));
        Assert.AreSame(font.At(24), font.At(24));
        Assert.AreEqual(Vector2.Zero, font.At(24).Measure(""));
        var twoLines = font.At(24).Measure("a\nb");
        Assert.AreEqual(2 * font.LineHeight(24), twoLines.Y, 0.5, "lines count whole line heights");
        Assert.IsTrue(font.Measure("WWW", 24).X > font.Measure("iii", 24).X, "proportional widths");
    }

    [TestMethod]
    public void Otf_files_with_cff_outlines_draw_and_measure()
    {
        var device = new HeadlessGraphicsDevice();
        using var serif = ScalableFont.Load(device, Gallery("fonts/SourceSerif4-Regular.otf"));
        using var bold = ScalableFont.Load(device, Gallery("fonts/Inter-Bold.otf"));
        foreach (var font in new[] { serif, bold })
        {
            Assert.IsTrue(font.Measure("Serif", 32).X > 40, font.Name);
            var renderer = new HeadlessRenderer(640, 360);
            font.Draw(renderer, "Serif", new Vector2(10, 10), 32, Color.White);
            Assert.AreEqual(5, renderer.DrawCalls, $"{font.Name}: one quad per glyph");
            Assert.IsTrue(font.GlyphsRasterized >= 4, font.Name);
        }
    }

    [TestMethod]
    public void Prepared_glyphs_cost_no_font_work_when_drawn()
    {
        var device = new HeadlessGraphicsDevice();
        using var font = Inter(device);
        font.Prepare("Score: 0123456789", 32, 48);
        int rasterized = font.GlyphsRasterized, updates = device.Updates;
        Assert.IsTrue(rasterized >= 2 * 13, $"each distinct glyph at both sizes, got {rasterized}");
        Assert.AreEqual(1, font.Pages, "one page holds them");
        var renderer = new HeadlessRenderer(1280, 720);
        for (int frame = 0; frame < 10; frame++) font.Draw(renderer, $"Score: {frame * 1234}", new Vector2(20, 20), 32, Color.White);
        Assert.AreEqual(rasterized, font.GlyphsRasterized, "drawing prepared glyphs rasterizes nothing");
        Assert.AreEqual(updates, device.Updates, "and uploads nothing");
        font.Draw(renderer, "Q", Vector2.Zero, 32, Color.White);
        Assert.AreEqual(rasterized + 1, font.GlyphsRasterized, "an unprepared glyph is rasterized once, on first use");
    }

    [TestMethod]
    [DataRow(TextureFilter.Nearest)]
    [DataRow(TextureFilter.Linear)]
    public void The_glyph_filter_is_the_game_default_unless_the_font_options_choose_one(TextureFilter gameDefault)
    {
        // As the rendering manual states: FontOptions.Filter, else the game's default (its presentation's default sampler),
        // whether the text is antialiased or not.
        var device = new HeadlessGraphicsDevice { DefaultFilter = gameDefault };
        using var smooth = Inter(device);
        using var pixel = Inter(device, FontOptions.Pixel);
        using var chosen = Inter(device, new FontOptions(Filter: TextureFilter.Nearest));
        Assert.AreEqual(gameDefault, smooth.Filter);
        Assert.AreEqual(TextureFilter.Nearest, pixel.Filter, "the pixel-font options choose nearest");
        Assert.AreEqual(TextureFilter.Nearest, chosen.Filter, "a font's own choice wins");
        var assets = new AssetCache(device, Path.Combine(AppContext.BaseDirectory, "Gallery"));
        Assert.AreEqual(gameDefault, assets.Font("fonts/Inter-Regular.ttf").Filter, "loaded through the cache too");
        assets.Dispose();
    }

    [TestMethod]
    public void Pages_follow_the_font_filter_and_disposing_releases_them()
    {
        var device = new HeadlessGraphicsDevice { DefaultFilter = TextureFilter.Linear };
        var smooth = Inter(device);
        smooth.Prepare("abc", 20);
        Assert.AreEqual(TextureFilter.Linear, smooth.Filter);
        var pixel = Inter(device, FontOptions.Pixel with { PageSize = 256 });
        pixel.Prepare("abc", 16);
        Assert.AreEqual(TextureFilter.Nearest, pixel.Filter);
        Assert.AreEqual(256L * 256 * 4, pixel.TextureBytes);
        pixel.Filter = TextureFilter.Linear;
        Assert.AreEqual(TextureFilter.Linear, pixel.Filter);
        Assert.AreEqual(2, device.LiveTextures);
        var kept = pixel.At(16);
        smooth.Dispose();
        pixel.Dispose();
        Assert.AreEqual(0, device.LiveTextures);
        Assert.ThrowsExactly<ObjectDisposedException>(() => smooth.At(12));
        Assert.ThrowsExactly<ObjectDisposedException>(() => kept.Measure("abc"), "a size kept from before refuses too");
    }

    [TestMethod]
    public void Reset_frees_the_pages_and_glyphs_come_back_when_drawn_again()
    {
        var device = new HeadlessGraphicsDevice();
        using var font = Inter(device);
        font.Prepare(ScalableFont.Ascii, 24);
        Assert.AreEqual(1, device.LiveTextures);
        font.Reset();
        Assert.AreEqual(0, device.LiveTextures);
        Assert.AreEqual(0, font.Pages);
        font.Draw(new HeadlessRenderer(100, 100), "ok", Vector2.Zero, 24, Color.White);
        Assert.AreEqual(1, font.Pages);
    }

    [TestMethod]
    public void A_fallback_face_draws_characters_the_main_face_lacks()
    {
        var device = new HeadlessGraphicsDevice();
        using var serif = ScalableFont.Load(device, Gallery("fonts/SourceSerif4-Regular.otf"));
        // A character Inter has and Source Serif lacks (checked below: without the fallback it measures as the '?' glyph).
        const string symbol = "⌘";
        Assert.AreEqual(serif.Measure("?", 32).X, serif.Measure(symbol, 32).X, "missing characters draw as '?'");
        serif.AddFallback(File.ReadAllBytes(Gallery("fonts/Inter-Regular.ttf")), "Inter");
        Assert.AreNotEqual(serif.Measure("?", 32).X, serif.Measure(symbol, 32).X, "the symbol now comes from the fallback face");
    }

    [TestMethod]
    public void A_file_that_is_not_a_font_fails_with_its_name()
    {
        var device = new HeadlessGraphicsDevice();
        string path = Path.Combine(Path.GetTempPath(), $"me-font-{Guid.NewGuid():N}.ttf");
        try
        {
            File.WriteAllBytes(path, [1, 2, 3, 4, 5, 6, 7, 8]);
            var error = Assert.ThrowsExactly<InvalidDataException>(() => ScalableFont.Load(device, path));
            StringAssert.Contains(error.Message, Path.GetFileName(path));
        }
        finally { File.Delete(path); }
        using var font = Inter(device);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => font.At(0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => font.At(float.NaN));
    }

    /// <summary>
    /// The rasterizer trusts its input and reads out of bounds on a broken file (it hung this suite once in five runs):
    /// broken files must be refused before it sees them.
    /// </summary>
    [TestMethod]
    public void Truncated_or_foreign_files_are_refused_before_the_rasterizer_reads_them()
    {
        var inter = File.ReadAllBytes(Gallery("fonts/Inter-Regular.ttf"));
        Assert.IsNull(ScalableFont.CheckStructure(inter));
        Assert.IsNull(ScalableFont.CheckStructure(File.ReadAllBytes(Gallery("fonts/SourceSerif4-Regular.otf"))));
        StringAssert.Contains(ScalableFont.CheckStructure([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]), "header");
        StringAssert.Contains(ScalableFont.CheckStructure([0, 1, 0, 0]), "too short");
        StringAssert.Contains(ScalableFont.CheckStructure(inter.AsSpan(0, 40)), "cut short");
        StringAssert.Contains(ScalableFont.CheckStructure(inter.AsSpan(0, 4000)), "outside the file");
        var device = new HeadlessGraphicsDevice();
        for (int i = 0; i < 50; i++)
            Assert.ThrowsExactly<InvalidDataException>(() => ScalableFont.FromBytes(device, inter[..(400 + i * 37)]));
    }

    [TestMethod]
    public void Bitmap_and_scalable_fonts_share_layout_through_IFont()
    {
        using var inter = Inter(new HeadlessGraphicsDevice());
        var bitmap = BitmapFont.CreateDefault(null);
        foreach (IFont font in new IFont[] { bitmap, inter.At(20) })
        {
            float limit = font.Measure("the quick brown").X + 1;
            var lines = font.Wrap("the quick brown fox jumps over the lazy dog", limit);
            Assert.IsTrue(lines.Count >= 3, $"{font}: {string.Join("|", lines)}");
            Assert.IsTrue(lines.All(l => font.Measure(l).X <= limit));
        }
        Assert.AreEqual(bitmap.MeasureWidth("Hello"), ((IFont)bitmap).MeasureWidth("Hello"));
        Assert.AreEqual(10f, ((IFont)bitmap).LineHeight);
    }

    [TestMethod]
    public void Text_registers_the_font_loader_with_content_under_the_kind_font()
    {
        var loaders = new MicroEngine.Content.AssetLoaderRegistry();
        FontLoader.Register(loaders, new HeadlessGraphicsDevice());
        CollectionAssert.AreEqual(new[] { "font" }, loaders.Kinds.ToList());
        Assert.AreEqual("font", loaders.KindOf("fonts/Inter-Regular.ttf"));
        Assert.AreEqual("font", loaders.KindOf("fonts/SourceSerif4-Regular.otf"));
        var damaged = Assert.ThrowsExactly<AssetLoadException>(() => FontLoader.Open(new HeadlessGraphicsDevice(), "fonts/bad.ttf", [1, 2, 3, 4], FontOptions.Default));
        Assert.AreEqual("fonts/bad.ttf", damaged.AssetPath);
        Assert.AreEqual("font", damaged.Kind);
    }
}

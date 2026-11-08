using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace MicroEngine.Tests;

/// <summary>Shaped text (MicroEngine.Text.Shaping, HarfBuzz) with the examples' real font files: Inter, Noto Sans Arabic and Hebrew.</summary>
[TestClass]
public sealed class ShapedFontTests
{
    private static string FontPath(string file) => Path.Combine(AppContext.BaseDirectory, "ExampleFonts", file);
    private static byte[] Font(string file) => File.ReadAllBytes(FontPath(file));

    /// <summary>Inter, with Arabic and Hebrew after it for the letters it lacks.</summary>
    private static ShapedFont Mixed(HeadlessGraphicsDevice device) =>
        ShapedFont.FromBytes(device, Font("Inter-Regular.ttf"), name: "Inter")
            .AddFallback(Font("NotoSansArabic-Regular.ttf"), "Arabic")
            .AddFallback(Font("NotoSansHebrew-Regular.ttf"), "Hebrew");

    [TestMethod]
    public void Arabic_letters_take_their_joined_forms_and_run_right_to_left()
    {
        using var font = ShapedFont.FromBytes(new HeadlessGraphicsDevice(), Font("NotoSansArabic-Regular.ttf"));
        var size = font.At(32);
        const string word = "مرحبا"; // "hello": five letters, four of them joined
        var glyphs = size.Shape(word);
        // Noto draws the dot of ب as a mark of its own, with no advance: six glyphs for five letters.
        CollectionAssert.AreEqual(new[] { 4, 3, 2, 1, 0 }, glyphs.Select(g => g.Cluster).Distinct().ToArray(), "drawn left to right, the last letter first");
        for (int i = 1; i < glyphs.Count; i++) Assert.IsTrue(glyphs[i].Pen >= glyphs[i - 1].Pen + glyphs[i - 1].Advance - 0.01f, "each glyph's advance starts where the one before ends");
        Assert.IsTrue(glyphs[^1].Pen > glyphs[0].Pen);
        int joined = 0;
        for (int i = 0; i < word.Length; i++)
            if (size.Shape(word[i].ToString())[0].Glyph != glyphs.First(g => g.Cluster == i).Glyph) joined++;
        Assert.IsTrue(joined >= 3, $"letters in the word use other glyphs than alone (initial, medial, final forms); {joined} did");
    }

    [TestMethod]
    public void Hebrew_runs_right_to_left_and_letters_a_face_lacks_come_from_its_fallbacks()
    {
        using var font = Mixed(new HeadlessGraphicsDevice());
        var glyphs = font.At(24).Shape("שלום");
        CollectionAssert.AreEqual(new[] { 3, 2, 1, 0 }, glyphs.Select(g => g.Cluster).ToArray());
        Assert.IsTrue(glyphs.All(g => g.Face == 2 && g.Glyph != 0), "Hebrew from the third face, none missing");

        var latin = font.At(24).Shape("Hi");
        Assert.IsTrue(latin.All(g => g.Face == 0), "Latin from the main face");
        Assert.IsTrue(font.At(24).Shape("ب").All(g => g.Face == 1 && g.Glyph != 0), "Arabic from the first fallback");
    }

    [TestMethod]
    public void Measure_and_line_height_agree_with_the_scalable_font_of_the_same_file()
    {
        var device = new HeadlessGraphicsDevice();
        using var shaped = ShapedFont.FromBytes(device, Font("Inter-Regular.ttf"));
        using var plain = ScalableFont.FromBytes(device, Font("Inter-Regular.ttf"));
        foreach (float px in new[] { 16f, 32f })
        {
            Assert.AreEqual(plain.LineHeight(px), shaped.At(px).LineHeight, 1, $"line height at {px}");
            Assert.AreEqual(plain.Measure("Hello, world", px).X, shaped.At(px).Measure("Hello, world").X, px / 8, $"width at {px}");
        }
        var size = shaped.At(24);
        Assert.AreSame(size, shaped.At(24));
        Assert.AreEqual(Vector2.Zero, size.Measure(""));
        Assert.AreEqual(2 * size.LineHeight, size.Measure("a\nbb").Y, 0.01, "lines count whole line heights");
        Assert.AreEqual(size.Measure("bb").X, size.Measure("a\nbb").X, 0.01, "the widest line");
    }

    [TestMethod]
    public void Drawing_puts_one_quad_per_inked_glyph_and_rasterizes_each_glyph_once()
    {
        var device = new HeadlessGraphicsDevice();
        using var font = Mixed(device);
        var renderer = new HeadlessRenderer(640, 360);
        font.At(32).Draw(renderer, "Hi مرحبا", new Vector2(10, 10), Color.White);
        Assert.AreEqual(8, renderer.DrawCalls, "H, i and six Arabic glyphs (five letters and a dot); the space draws nothing");
        int rasterized = font.GlyphsRasterized;
        Assert.AreEqual(8, rasterized);
        Assert.AreEqual(1, font.Pages);
        for (int frame = 0; frame < 5; frame++) font.At(32).Draw(renderer, "Hi مرحبا", new Vector2(10, 10), Color.White);
        Assert.AreEqual(rasterized, font.GlyphsRasterized, "later frames only draw quads");
    }

    /// <summary>The text indices a line shows, left to right, each once (a letter drawn as a base and a mark counts once).</summary>
    private static int[] VisualOrder(ShapedFontSize size, string line) => [.. size.Shape(line).Select(g => g.Cluster).Distinct()];

    [TestMethod]
    public void Unicode_bidirectional_classes_come_from_the_character_database()
    {
        Assert.AreEqual(BidiClass.L, Bidi.ClassOf('A'));
        Assert.AreEqual(BidiClass.R, Bidi.ClassOf('א'));
        Assert.AreEqual(BidiClass.AL, Bidi.ClassOf('ب'));
        Assert.AreEqual(BidiClass.EN, Bidi.ClassOf('5'));
        Assert.AreEqual(BidiClass.AN, Bidi.ClassOf('٥'), "Arabic-Indic five");
        Assert.AreEqual(BidiClass.WS, Bidi.ClassOf(' '));
        Assert.AreEqual(BidiClass.ON, Bidi.ClassOf('!'));
        Assert.AreEqual(BidiClass.NSM, Bidi.ClassOf('́'), "combining acute accent");
        Assert.AreEqual(BidiClass.R, Bidi.ClassOf(0x10800), "Cypriot, outside the basic plane");
        Assert.AreEqual(BidiClass.AL, Bidi.ClassOf(0x07BF), "U+07BF, unassigned, takes the AL default of its range");
    }

    [TestMethod]
    public void A_left_to_right_line_reverses_its_right_to_left_run_and_keeps_numbers_in_reading_order()
    {
        using var font = Mixed(new HeadlessGraphicsDevice());
        var size = font.At(24);
        // a b c ␠ ש ל ו ם ␠ 1 2 3: the Hebrew word and the number after it read right to left as a group, the digits
        // left to right inside it, as browsers show it.
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 9, 10, 11, 8, 7, 6, 5, 4 }, VisualOrder(size, "abc שלום 123"));
        Assert.IsFalse(size.IsRightToLeft("abc שלום 123"), "the first strong letter is Latin");
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5 }, VisualOrder(size, "Hello!"), "plain Latin is unchanged");
    }

    [TestMethod]
    public void A_right_to_left_line_keeps_English_words_and_numbers_left_to_right_in_their_place()
    {
        using var font = Mixed(new HeadlessGraphicsDevice());
        var size = font.At(24);
        Assert.IsTrue(size.IsRightToLeft("مرحبا Bob"));
        CollectionAssert.AreEqual(new[] { 6, 7, 8, 5, 4, 3, 2, 1, 0 }, VisualOrder(size, "مرحبا Bob"), "Bob at the left end, after the Arabic in reading order");
        CollectionAssert.AreEqual(new[] { 4, 5, 3, 2, 1, 0 }, VisualOrder(size, "عدد 25"), "\"number 25\": the digits keep their order");
        CollectionAssert.AreEqual(new[] { 4, 3, 2, 1, 0 }, VisualOrder(size, "שלום!"), "the closing mark ends the line on the left");
        Assert.IsFalse(size.IsRightToLeft("123 !"), "no strong letter: left to right");
        var glyphs = size.Shape("مرحبا Bob");
        for (int i = 1; i < glyphs.Count; i++) Assert.IsTrue(glyphs[i].Pen >= glyphs[i - 1].Pen, "pens grow left to right across runs");
    }

    [TestMethod]
    public void A_shaped_font_serves_widgets_and_the_asset_cache_through_IFont()
    {
        using var font = Mixed(new HeadlessGraphicsDevice());
        IFont size = font.At(20);
        var lines = size.Wrap("مرحبا بالعالم and more words", size.MeasureWidth("مرحبا بالعالم") + 1);
        Assert.AreEqual("مرحبا بالعالم", lines[0], "wrapping measures shaped words");
        Assert.ThrowsExactly<InvalidDataException>(() => ShapedFont.FromBytes(new HeadlessGraphicsDevice(), [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13]));
    }
}

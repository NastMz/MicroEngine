using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.UI;

namespace MicroEngine.Tests.Localization;

/// <summary>Prompt glyphs with any font: sized by its line height, labelled with it, and laid from the right when asked.</summary>
[TestClass]
public sealed class PromptTextFontTests
{
    /// <summary>A font ten units wide per character and twenty tall, which records what it draws.</summary>
    private sealed class TenWide : IFont
    {
        public readonly List<(string Text, Vector2 At)> Drawn = [];
        public float LineHeight => 20;
        public Vector2 Measure(string text) => new(text.Length * 10, 20);
        public void Draw(IRenderer renderer, string text, Vector2 position, Color color) => Drawn.Add((text, position));
    }

    private static (ActionPrompts Prompts, IRenderer Renderer) Rig()
    {
        var map = InputMap.CreateDefault().Bind("attack", [Key.Z], [PadButton.A]).Bind("jump", [Key.Space], []);
        var platform = HeadlessPlatform.For(new GameSettings("prompts", 320, 180, 1), rasterize: false);
        return (new ActionPrompts(map), platform.BeginFrame());
    }

    [TestMethod]
    public void GlyphsAreSizedByTheFontsLineHeight()
    {
        var (prompts, _) = Rig();
        var font = new TenWide();
        Assert.AreEqual(20f, PromptText.GlyphWidth(font, prompts.Get("attack")), "a one-letter key cap is as wide as it is tall");
        Assert.AreEqual(60f, PromptText.GlyphWidth(font, prompts.Get("jump")), "'Space' and a margin of a quarter line on each side");
        Assert.AreEqual(60 + 2 + 20 + 2 + 40f, PromptText.Measure(font, prompts, "Press {attack} now"));
        Assert.AreEqual(0f, PromptText.Measure(font, prompts, ""));
    }

    [TestMethod]
    public void TextAndGlyphLabelsAreDrawnWithTheFont()
    {
        var (prompts, renderer) = Rig();
        var font = new TenWide();
        PromptText.Draw(renderer, font, prompts, "Press {attack} now", new Vector2(100, 50), Color.White);
        CollectionAssert.AreEqual(new[] { ("Press ", new Vector2(100, 50)), ("Z", new Vector2(167, 50)), (" now", new Vector2(184, 50)) }, font.Drawn);

        font.Drawn.Clear();
        PromptText.DrawCentered(renderer, font, prompts, "{attack}", 100, 50, Color.White, shadow: Color.Black);
        Assert.AreEqual(("Z", new Vector2(95, 50)), font.Drawn.Single(), "the cap's 24 units centred on 100");
    }

    [TestMethod]
    public void ARightToLeftLineIsLaidFromItsRightEdge()
    {
        var (prompts, renderer) = Rig();
        var font = new TenWide();
        PromptText.Draw(renderer, font, prompts, "Press {attack} now", new Vector2(100, 50), Color.White, rightToLeft: true);
        CollectionAssert.AreEqual(new[] { ("Press ", new Vector2(164, 50)), ("Z", new Vector2(147, 50)), (" now", new Vector2(100, 50)) }, font.Drawn,
            "the first piece ends at the right edge; the line occupies the same width");
    }

    [TestMethod]
    public void TheBuiltInFontKeepsItsPixelGlyphs()
    {
        var (prompts, renderer) = Rig();
        using var platform = HeadlessPlatform.For(new GameSettings("prompts", 320, 180, 1), rasterize: false);
        var font = BitmapFont.CreateDefault(platform.Graphics);
        Assert.AreEqual(Math.Max(9, font.MeasureWidth("Z") + 5), PromptText.GlyphWidth(font, prompts.Get("attack")), "the pixel cap, as before");
        Assert.IsInstanceOfType<int>(PromptText.Measure(font, prompts, "Press {attack}"));
    }
}

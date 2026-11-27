using System.Globalization;
using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Testing;

namespace MicroEngine.Tests;

/// <summary>
/// Text drawn every frame without garbage: an interpolated string a font takes is written into a reused buffer, the
/// fonts draw text that is not a string, and a prompt's text is kept until it may have changed.
/// </summary>
[TestClass]
public sealed class InterpolatedTextTests
{
    /// <summary>Counts what is drawn and folds it into a number, without allocating: two drawings are the same when their sums are.</summary>
    private sealed class Sum : IRenderer
    {
        public int Draws;
        public long Hash;
        public int Width => 320;
        public int Height => 180;
        public void Clear(Color color) { }
        public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default)
        {
            Draws++;
            Hash = Hash * 31 + HashCode.Combine(source, position, tint, options.Scale, options.FlipX);
        }
        public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint) => Draws++;
        public void FillRect(RectF rect, Color color) { }
        public void FillCircle(Vector2 center, float radius, Color color) { }
        public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color) { }
        public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color) { }
        public void PushCamera(Camera2D camera) { }
        public void PopCamera() { }
        public void SetBlendMode(BlendMode mode) { }
        public void SetClip(RectF? screenRect) { }
        public void PushState(in DrawState state) { }
        public void PopState() { }
        public void PushTarget(RenderTarget2D target) { }
        public void PopTarget() { }
        public (int, long) Take()
        {
            var taken = (Draws, Hash);
            (Draws, Hash) = (0, 0);
            return taken;
        }
    }

    private static string Text(InterpolatedText text)
    {
        string s = text.ToString();
        text.Dispose();
        return s;
    }

    private enum Suit { Hearts, Spades }
    private readonly record struct Plain(int Value);

    [TestMethod]
    public void An_interpolated_text_is_the_string_the_same_interpolation_gives()
    {
        int hp = 12, max = 20, negative = -7;
        long big = 12_345_678_901;
        uint level = 9;
        ulong huge = ulong.MaxValue;
        float seconds = 83.456f;
        double ratio = 0.3333333;
        string name = "Pip";
        string? none = null;
        char mark = '×';
        var suit = Suit.Spades;
        var plain = new Plain(4);
        var when = new DateTime(2026, 11, 24);
        Assert.AreEqual($"{hp}/{max}", Text($"{hp}/{max}"));
        Assert.AreEqual($"Lv{level} ${big} {huge} {negative}", Text($"Lv{level} ${big} {huge} {negative}"));
        Assert.AreEqual($"{(int)seconds / 60}:{(int)seconds % 60:00}.{(int)(seconds * 10) % 10}", Text($"{(int)seconds / 60}:{(int)seconds % 60:00}.{(int)(seconds * 10) % 10}"));
        Assert.AreEqual($"{seconds:0.0} {ratio:P1} {seconds} {ratio}", Text($"{seconds:0.0} {ratio:P1} {seconds} {ratio}"));
        Assert.AreEqual($"[{hp,6}] [{hp,-6}] [{name,5}] [{name,-5}] [{hp,1}] [{seconds,8:0.00}]", Text($"[{hp,6}] [{hp,-6}] [{name,5}] [{name,-5}] [{hp,1}] [{seconds,8:0.00}]"));
        Assert.AreEqual($"{name}{none}{mark}{name.AsSpan(1)}", Text($"{name}{none}{mark}{name.AsSpan(1)}"));
        Assert.AreEqual($"{suit} {plain} {when:yyyy-MM-dd} {(object?)null}", Text($"{suit} {plain} {when:yyyy-MM-dd} {(object?)null}"));
        Assert.AreEqual("", Text($""));
        string longText = new('x', 500);
        Assert.AreEqual($"{longText}{hp}{longText}", Text($"{longText}{hp}{longText}"), "a text longer than the first buffer grows it");
    }

    [TestMethod]
    [DoNotParallelize] // changes the thread's culture
    public void Numbers_follow_the_culture_as_string_interpolation_does()
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-ES");
            float value = 1234.5f;
            Assert.AreEqual($"{value:0.0} {value:N1} {-3}", Text($"{value:0.0} {value:N1} {-3}"));
            StringAssert.Contains(Text($"{value:0.0}"), ",", "a comma separates the decimals in Spanish, as in the string");
        }
        finally { CultureInfo.CurrentCulture = before; }
    }

    [TestMethod]
    public void A_font_draws_an_interpolated_text_and_a_span_as_it_draws_the_string()
    {
        var font = BitmapFont.CreateDefault(new HeadlessGraphicsDevice());
        IFont asFont = font;
        var r = new Sum();
        int hp = 12, max = 20;
        var at = new Vector2(8, 9);
        string text = $"HP {hp}/{max}\nLv7";

        font.Draw(r, text, at, Color.White);
        var expected = r.Take();
        Assert.AreEqual(text.Count(c => c is not (' ' or '\n')), expected.Item1, "one quad per visible character");
        font.Draw(r, $"HP {hp}/{max}\nLv7", at, Color.White);
        Assert.AreEqual(expected, r.Take(), "interpolated");
        font.Draw(r, text.AsSpan(), at, Color.White);
        Assert.AreEqual(expected, r.Take(), "span");
        asFont.Draw(r, $"HP {hp}/{max}\nLv7", at, Color.White);
        Assert.AreEqual(expected, r.Take(), "interpolated, through IFont");
        asFont.Draw(r, text.AsSpan(), at, Color.White);
        Assert.AreEqual(expected, r.Take(), "span, through IFont");

        Assert.AreEqual(font.Measure(text), font.Measure($"HP {hp}/{max}\nLv7"));
        Assert.AreEqual(font.Measure(text), asFont.Measure(text.AsSpan()));
        Assert.AreEqual(font.MeasureWidth(text), font.MeasureWidth(text.AsSpan()));
        Assert.AreEqual(asFont.MeasureWidth(text), asFont.MeasureWidth($"HP {hp}/{max}\nLv7"));

        font.DrawShadowed(r, text, at, Color.White, Color.Black, 2);
        expected = r.Take();
        font.DrawShadowed(r, $"HP {hp}/{max}\nLv7", at, Color.White, Color.Black, 2);
        Assert.AreEqual(expected, r.Take(), "shadowed");
        font.DrawOutlined(r, text, at, Color.White, Color.Black);
        expected = r.Take();
        font.DrawOutlined(r, $"HP {hp}/{max}\nLv7", at, Color.White, Color.Black);
        Assert.AreEqual(expected, r.Take(), "outlined");
        font.DrawCentered(r, text, new Vector2(160, 90), Color.White, shadow: Color.Black);
        expected = r.Take();
        font.DrawCentered(r, $"HP {hp}/{max}\nLv7", new Vector2(160, 90), Color.White, shadow: Color.Black);
        Assert.AreEqual(expected, r.Take(), "centred");
        asFont.DrawCentered(r, text, new Vector2(160, 90), Color.White);
        expected = r.Take();
        asFont.DrawCentered(r, $"HP {hp}/{max}\nLv7", new Vector2(160, 90), Color.White);
        Assert.AreEqual(expected, r.Take(), "centred, through IFont");
        asFont.DrawShadowed(r, text, at, Color.White, Color.Black);
        expected = r.Take();
        asFont.DrawShadowed(r, $"HP {hp}/{max}\nLv7", at, Color.White, Color.Black);
        Assert.AreEqual(expected, r.Take(), "shadowed, through IFont");
    }

    [TestMethod]
    public void A_text_drawn_to_the_right_ends_where_it_is_told_whatever_its_length()
    {
        var font = BitmapFont.CreateDefault(new HeadlessGraphicsDevice());
        IFont asFont = font;
        var r = new Sum();
        foreach (int score in new[] { 7, 1250, 987654 })
        {
            string text = score.ToString(CultureInfo.CurrentCulture);
            font.Draw(r, text, new Vector2(300 - font.MeasureWidth(text), 12), Color.White);
            var expected = r.Take();
            font.DrawRight(r, $"{score}", new Vector2(300, 12), Color.White);
            Assert.AreEqual(expected, r.Take(), $"{score}");
            font.DrawRight(r, text, new Vector2(300, 12), Color.White);
            Assert.AreEqual(expected, r.Take(), $"{score} as a string");
            asFont.DrawRight(r, $"{score}", new Vector2(300, 12), Color.White);
            Assert.AreEqual(expected, r.Take(), $"{score} through IFont");
        }
    }

    [TestMethod]
    public void A_frame_that_draws_a_changing_score_a_timer_and_a_prompt_allocates_nothing()
    {
        var font = BitmapFont.CreateDefault(new HeadlessGraphicsDevice());
        IFont asFont = font;
        var input = InputMap.CreateDefault();
        var prompts = new ActionPrompts(input) { Override = InputDevice.Keyboard };
        var r = new Sum();
        int frame = 0;
        void Frame()
        {
            frame++;
            int score = frame * 37;
            float time = frame / 60f;
            font.DrawRight(r, $"{score}", new Vector2(312, 4), Color.White, shadow: Color.Black);
            font.DrawCentered(r, $"{(int)time / 60}:{(int)time % 60:00}.{(int)(time * 10) % 10}", new Vector2(160, 8), Color.White);
            font.Draw(r, $"{time:0.00} s", new Vector2(4, 4), Color.White);
            asFont.Draw(r, $"x{frame % 100,3}", new Vector2(4, 16), Color.White);
            font.Draw(r, prompts.Cached("{confirm} Jump   {pause} Pause"), new Vector2(4, 168), Color.White);
        }
        AllocationAssert.None(Frame, warmup: 100, calls: 600, because: "a HUD with a score, a timer and a hint");
        Assert.IsTrue(r.Draws > 600 * 20, "the frames drew their text");
    }

    [TestMethod]
    public void The_numbers_of_a_battle_screen_draw_without_garbage_and_as_their_strings()
    {
        // Trail Duel keeps 78 lines of caches (Ui/NumberTexts.cs, Ui/CachedText.cs) for exactly these five texts.
        var font = BitmapFont.CreateDefault(new HeadlessGraphicsDevice());
        var r = new Sum();
        int n = 0;
        void Hud()
        {
            n++;
            int hp = n % 250, max = 250, level = 1 + n % 100, count = n % 99, money = n * 13;
            font.Draw(r, $"{hp}", new Vector2(10, 10), Color.White);
            font.Draw(r, $"Lv{level}", new Vector2(10, 20), Color.White);
            font.Draw(r, $"×{count}", new Vector2(10, 30), Color.White);
            font.DrawShadowed(r, $"{hp}/{max}", new Vector2(10, 40), Color.White, Color.Black);
            font.DrawRight(r, $"${money}", new Vector2(310, 50), Color.White);
        }
        AllocationAssert.None(Hud, warmup: 50, calls: 500, because: "HP, level, count, ratio and money every frame");

        r.Take();
        for (int i = 0; i < 300; i++)
        {
            Hud();
            var drawn = r.Take();
            int hp = n % 250, max = 250, level = 1 + n % 100, count = n % 99, money = n * 13;
            font.Draw(r, hp.ToString(), new Vector2(10, 10), Color.White);
            font.Draw(r, "Lv" + level, new Vector2(10, 20), Color.White);
            font.Draw(r, "×" + count, new Vector2(10, 30), Color.White);
            font.DrawShadowed(r, hp + "/" + max, new Vector2(10, 40), Color.White, Color.Black);
            string price = "$" + money;
            font.Draw(r, price, new Vector2(310 - font.MeasureWidth(price), 50), Color.White);
            Assert.AreEqual(r.Take(), drawn, $"frame {n}");
        }
    }

    [TestMethod]
    public void A_scalable_font_draws_a_changing_number_without_garbage_and_as_its_string()
    {
        var device = new HeadlessGraphicsDevice();
        using var family = ScalableFont.Load(device, Path.Combine(AppContext.BaseDirectory, "Gallery", "fonts/Inter-Regular.ttf"));
        var font = family.At(24);
        IFont asFont = font;
        var r = new Sum();
        font.Draw(r, "0123456789:. s", Vector2.Zero, Color.White); // every glyph the frames show is rasterized once
        r.Take();

        int score = 4711;
        font.Draw(r, score.ToString(), new Vector2(10, 10), Color.White);
        var expected = r.Take();
        font.Draw(r, $"{score}", new Vector2(10, 10), Color.White);
        Assert.AreEqual(expected, r.Take(), "interpolated");
        asFont.Draw(r, score.ToString().AsSpan(), new Vector2(10, 10), Color.White);
        Assert.AreEqual(expected, r.Take(), "span");
        family.Draw(r, $"{score}", new Vector2(10, 10), 24, Color.White);
        Assert.AreEqual(expected, r.Take(), "through the family, at a size");
        Assert.AreEqual(font.Measure(score.ToString()), font.Measure($"{score}"));
        Assert.AreEqual(font.Measure("12\n3"), asFont.Measure("12\n3".AsSpan()));

        int frame = 0;
        AllocationAssert.None(() =>
        {
            frame++;
            font.Draw(r, $"{frame * 37}", new Vector2(10, 10), Color.White);
            font.Draw(r, $"{frame / 60}:{frame % 60:00}", new Vector2(10, 40), Color.White);
            _ = asFont.Measure($"{frame} s");
        }, warmup: 100, calls: 300, because: "numbers in a TrueType font");
    }

    [TestMethod]
    public void A_shaped_font_draws_a_span_as_its_string_and_a_text_seen_before_without_garbage()
    {
        var device = new HeadlessGraphicsDevice();
        using var family = ShapedFont.FromBytes(device, File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "ExampleFonts", "Inter-Regular.ttf")), name: "Inter");
        var font = family.At(20);
        IFont asFont = font;
        var r = new Sum();
        font.Draw(r, "Level 12\nfi 7", new Vector2(5, 6), Color.White);
        var expected = r.Take();
        Assert.IsTrue(expected.Item1 > 8);
        asFont.Draw(r, "Level 12\nfi 7".AsSpan(), new Vector2(5, 6), Color.White);
        Assert.AreEqual(expected, r.Take(), "span");
        int level = 12, n = 7;
        font.Draw(r, $"Level {level}\nfi {n}", new Vector2(5, 6), Color.White);
        Assert.AreEqual(expected, r.Take(), "interpolated");
        Assert.AreEqual(font.Measure("Level 12\nfi 7"), font.Measure($"Level {level}\nfi {n}"));
        Assert.AreEqual(font.Measure("Level 12\nfi 7"), asFont.Measure("Level 12\nfi 7".AsSpan()));

        // A clock: sixty texts, each shaped the first time it shows and found afterwards.
        int frame = 0;
        AllocationAssert.None(() => font.Draw(r, $"0:{frame++ / 4 % 60:00}", new Vector2(5, 6), Color.White), warmup: 300, calls: 300, because: "texts a shaped font has drawn before");
    }

    [TestMethod]
    public void An_atlas_finds_a_sprite_by_a_name_built_from_numbers_without_a_string()
    {
        var builder = new SpriteAtlasBuilder();
        for (int mask = 0; mask < 4; mask++)
            for (int f = 0; f < 3; f++) builder.Add($"t:water{mask}_{f}", new PixelImage(4 + mask, 4 + f));
        using var atlas = builder.Build(new HeadlessGraphicsDevice());
        Assert.AreSame(atlas["t:water2_1"], atlas[$"t:water{2}_{1}"]);
        Assert.AreSame(atlas["t:water3_0"], atlas["t:water3_0!".AsSpan(0, 10)]);
        Assert.IsTrue(atlas.TryGet("t:water0_2", out var found));
        Assert.AreSame(atlas["t:water0_2"], found);
        Assert.IsFalse(atlas.TryGet("t:water9_9", out _));
        int missing = 9;
        var error = Assert.ThrowsExactly<KeyNotFoundException>(() => _ = atlas[$"t:water{missing}_0"]);
        StringAssert.Contains(error.Message, "t:water9_0");

        int frame = 0;
        Sprite? last = null;
        AllocationAssert.None(() => { frame++; last = atlas[$"t:water{frame % 4}_{frame % 3}"]; }, because: "a tile looked up by mask and frame");
        Assert.IsNotNull(last);
    }

    [TestMethod]
    public void A_prompt_text_is_kept_until_the_prompts_change()
    {
        var input = InputMap.CreateDefault();
        var prompts = new ActionPrompts(input) { Override = InputDevice.Keyboard };
        const string template = "{confirm} Jump";
        string first = prompts.Cached(template);
        Assert.AreEqual("Z Jump", first);
        Assert.AreEqual(prompts.Format(template), first);
        Assert.AreSame(first, prompts.Cached(template), "the same string while nothing changes");
        Assert.AreEqual(prompts.Text("confirm"), prompts.Cached("{confirm}"), "an action alone");
        Assert.AreEqual("{fly} Fly", prompts.Cached("{fly} Fly"), "a name that is no action stays as written, as in Format");

        prompts.Override = InputDevice.Gamepad;
        string pad = prompts.Cached(template);
        Assert.AreEqual(prompts.Format(template), pad, "another device: built again");
        Assert.AreNotEqual(first, pad);
        Assert.AreSame(pad, prompts.Cached(template));

        prompts.SetLabel("confirm", null, "tap");
        Assert.AreEqual("tap Jump", prompts.Cached(template), "a label: built again");

        prompts.SetLabel("confirm", null, null);
        prompts.Override = InputDevice.Keyboard;
        input.Unbind("confirm").Bind("confirm", Key.K);
        prompts.Invalidate();
        Assert.AreEqual("K Jump", prompts.Cached(template), "after a rebinding the game invalidates");

        for (int i = 0; i < 1000; i++) Assert.AreEqual($"{i} K", prompts.Cached($"{i} {{confirm}}"), "more templates than are kept still give the right text");
        Assert.AreEqual("K Jump", prompts.Cached(template));
    }

    [TestMethod]
    public void A_cached_text_is_built_again_only_when_its_key_changes()
    {
        var text = new CachedText<(int Coins, int Language)>();
        int built = 0;
        string Format((int Coins, int Language) key) { built++; return $"{key.Coins} coins"; }
        string first = text.Get((3, 1), Format);
        Assert.AreEqual("3 coins", first);
        Assert.AreSame(first, text.Get((3, 1), Format));
        Assert.AreEqual(1, built);
        Assert.AreEqual("4 coins", text.Get((4, 1), Format));
        Assert.AreEqual("4 coins", text.Get((4, 2), Format), "another language builds it again");
        Assert.AreEqual(3, built);
        Assert.AreEqual("4 coins", text.Get((4, 2), "x", static (prefix, key) => prefix + key.Coins), "the same key keeps the text whatever the format");
        text.Clear();
        Assert.AreEqual("x4", text.Get((4, 2), "x", static (prefix, key) => prefix + key.Coins));

        var number = new CachedText<int>();
        int value = 0;
        AllocationAssert.None(() => _ = number.Get(value / 50, static v => v.ToString(CultureInfo.InvariantCulture)), calls: 40, rounds: 5,
            because: "one string per change of the value, none while it stays");
        value = 100;
        Assert.AreEqual("2", number.Get(value / 50, static v => v.ToString(CultureInfo.InvariantCulture)));
    }
}

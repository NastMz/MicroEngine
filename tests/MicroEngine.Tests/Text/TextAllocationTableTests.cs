using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Testing;

namespace MicroEngine.Tests;

/// <summary>
/// The table behind "drawing and measuring text allocate nothing": every method of every font that takes text, called
/// with a string, a span and an interpolated string, and every kind of value an interpolation holds.
/// </summary>
[TestClass]
public sealed class TextAllocationTableTests
{
    private static readonly HeadlessGraphicsDevice Device = new();
    private static string ExampleFont(string file) => Path.Combine(AppContext.BaseDirectory, "ExampleFonts", file);

    private static void Free(string what, Action call) => AllocationAssert.None(call, warmup: 60, calls: 200, because: what);

    private enum Suit { Hearts, Spades }
    [Flags] private enum Sides { None = 0, Left = 1, Right = 2 }

    [TestMethod]
    public void Every_text_method_of_the_built_in_font_is_free_for_a_string_a_span_and_an_interpolation()
    {
        var font = BitmapFont.CreateDefault(Device);
        IRenderer r = new HeadlessRenderer(320, 180);
        ((HeadlessRenderer)r).Reset();
        var renderer = (HeadlessRenderer)r;
        const string s = "Score 1250";
        int n = 0;
        var at = new Vector2(4, 4);
        void Row(string method, Action text, Action span, Action interpolated)
        {
            Free($"BitmapFont.{method}(string)", () => { renderer.Reset(); text(); });
            Free($"BitmapFont.{method}(span)", () => { renderer.Reset(); span(); });
            Free($"BitmapFont.{method}(interpolated)", () => { renderer.Reset(); n++; interpolated(); });
        }
        Row("Draw", () => font.Draw(r, s, at, Color.White), () => font.Draw(r, s.AsSpan(2), at, Color.White), () => font.Draw(r, $"Score {n}", at, Color.White));
        Row("DrawShadowed", () => font.DrawShadowed(r, s, at, Color.White, Color.Black), () => font.DrawShadowed(r, s.AsSpan(2), at, Color.White, Color.Black), () => font.DrawShadowed(r, $"Score {n}", at, Color.White, Color.Black));
        Row("DrawOutlined", () => font.DrawOutlined(r, s, at, Color.White, Color.Black), () => font.DrawOutlined(r, s.AsSpan(2), at, Color.White, Color.Black), () => font.DrawOutlined(r, $"Score {n}", at, Color.White, Color.Black));
        Row("DrawCentered", () => font.DrawCentered(r, s, at, Color.White), () => font.DrawCentered(r, s.AsSpan(2), at, Color.White), () => font.DrawCentered(r, $"Score {n}", at, Color.White));
        Row("DrawRight", () => font.DrawRight(r, s, at, Color.White), () => font.DrawRight(r, s.AsSpan(2), at, Color.White), () => font.DrawRight(r, $"Score {n}", at, Color.White));
        Row("Measure", () => _ = font.Measure(s), () => _ = font.Measure(s.AsSpan(2)), () => _ = font.Measure($"Score {n}"));
        Row("MeasureWidth", () => _ = font.MeasureWidth(s), () => _ = font.MeasureWidth(s.AsSpan(2)), () => _ = font.MeasureWidth($"Score {n}"));
        Assert.AreEqual(font.MeasureWidth("Score 7"), font.MeasureWidth($"Score {7}"));
    }

    [TestMethod]
    public void Every_text_method_of_a_font_seen_as_IFont_is_free()
    {
        IFont font = BitmapFont.CreateDefault(Device);
        var r = new HeadlessRenderer(320, 180);
        const string s = "Score 1250";
        int n = 0;
        var at = new Vector2(4, 4);
        void Row(string method, Action text, Action span, Action interpolated)
        {
            Free($"IFont.{method}(string)", () => { r.Reset(); text(); });
            Free($"IFont.{method}(span)", () => { r.Reset(); span(); });
            Free($"IFont.{method}(interpolated)", () => { r.Reset(); n++; interpolated(); });
        }
        Row("Draw", () => font.Draw(r, s, at, Color.White), () => font.Draw(r, s.AsSpan(2), at, Color.White), () => font.Draw(r, $"Score {n}", at, Color.White));
        Row("Measure", () => _ = font.Measure(s), () => _ = font.Measure(s.AsSpan(2)), () => _ = font.Measure($"Score {n}"));
        Row("MeasureWidth", () => _ = font.MeasureWidth(s), () => _ = font.MeasureWidth(s.AsSpan(2)), () => _ = font.MeasureWidth($"Score {n}"));
        Row("DrawCentered", () => font.DrawCentered(r, s, at, Color.White), () => font.DrawCentered(r, s.AsSpan(2), at, Color.White), () => font.DrawCentered(r, $"Score {n}", at, Color.White));
        Row("DrawRight", () => font.DrawRight(r, s, at, Color.White), () => font.DrawRight(r, s.AsSpan(2), at, Color.White), () => font.DrawRight(r, $"Score {n}", at, Color.White));
        Row("DrawShadowed", () => font.DrawShadowed(r, s, at, Color.White, Color.Black), () => font.DrawShadowed(r, s.AsSpan(2), at, Color.White, Color.Black), () => font.DrawShadowed(r, $"Score {n}", at, Color.White, Color.Black));
    }

    [TestMethod]
    public void Every_text_method_of_a_scalable_font_and_of_its_sizes_is_free()
    {
        using var family = ScalableFont.Load(Device, Path.Combine(AppContext.BaseDirectory, "Gallery", "fonts/Inter-Regular.ttf"));
        var sized = family.At(20);
        var r = new HeadlessRenderer(320, 180);
        const string s = "Score 1250";
        int n = 0;
        var at = new Vector2(4, 4);
        sized.Draw(r, "Score 0123456789", at, Color.White);    // the glyphs are rasterized once
        void Row(string method, Action text, Action span, Action interpolated)
        {
            Free($"{method}(string)", () => { r.Reset(); text(); });
            Free($"{method}(span)", () => { r.Reset(); span(); });
            Free($"{method}(interpolated)", () => { r.Reset(); n++; interpolated(); });
        }
        Row("ScalableFont.Draw", () => family.Draw(r, s, at, 20, Color.White), () => family.Draw(r, s.AsSpan(2), at, 20, Color.White), () => family.Draw(r, $"Score {n}", at, 20, Color.White));
        Row("ScalableFont.Measure", () => _ = family.Measure(s, 20), () => _ = family.Measure(s.AsSpan(2), 20), () => _ = family.Measure($"Score {n}", 20));
        Row("SizedFont.Draw", () => sized.Draw(r, s, at, Color.White), () => sized.Draw(r, s.AsSpan(2), at, Color.White), () => sized.Draw(r, $"Score {n}", at, Color.White));
        Row("SizedFont.Measure", () => _ = sized.Measure(s), () => _ = sized.Measure(s.AsSpan(2)), () => _ = sized.Measure($"Score {n}"));
        Row("SizedFont.MeasureWidth", () => _ = sized.MeasureWidth(s), () => _ = sized.MeasureWidth(s.AsSpan(2)), () => _ = sized.MeasureWidth($"Score {n}"));
        Row("SizedFont.DrawCentered", () => sized.DrawCentered(r, s, at, Color.White), () => sized.DrawCentered(r, s.AsSpan(2), at, Color.White), () => sized.DrawCentered(r, $"Score {n}", at, Color.White));
        Row("SizedFont.DrawRight", () => sized.DrawRight(r, s, at, Color.White), () => sized.DrawRight(r, s.AsSpan(2), at, Color.White), () => sized.DrawRight(r, $"Score {n}", at, Color.White));
        Row("SizedFont.DrawShadowed", () => sized.DrawShadowed(r, s, at, Color.White, Color.Black), () => sized.DrawShadowed(r, s.AsSpan(2), at, Color.White, Color.Black), () => sized.DrawShadowed(r, $"Score {n}", at, Color.White, Color.Black));
        Assert.AreEqual(family.Measure("Score 7", 20), family.Measure($"Score {7}", 20));
    }

    [TestMethod]
    public void Every_text_method_of_a_shaped_font_is_free_for_texts_it_has_seen()
    {
        using var family = ShapedFont.FromBytes(Device, File.ReadAllBytes(ExampleFont("Inter-Regular.ttf")), name: "Inter");
        var font = family.At(20);
        var r = new HeadlessRenderer(320, 180);
        const string s = "Score 1250";
        int n = 0;
        var at = new Vector2(4, 4);
        void Row(string method, Action text, Action span, Action interpolated)
        {
            Free($"ShapedFontSize.{method}(string)", () => { r.Reset(); text(); });
            Free($"ShapedFontSize.{method}(span)", () => { r.Reset(); span(); });
            // Sixty texts in turn: each is shaped the first time and found afterwards.
            Free($"ShapedFontSize.{method}(interpolated)", () => { r.Reset(); n = (n + 1) % 60; interpolated(); });
        }
        Row("Draw", () => font.Draw(r, s, at, Color.White), () => font.Draw(r, s.AsSpan(2), at, Color.White), () => font.Draw(r, $"Score {n}", at, Color.White));
        Row("Measure", () => _ = font.Measure(s), () => _ = font.Measure(s.AsSpan(2)), () => _ = font.Measure($"Score {n}"));
        Row("MeasureWidth", () => _ = font.MeasureWidth(s), () => _ = font.MeasureWidth(s.AsSpan(2)), () => _ = font.MeasureWidth($"Score {n}"));
        Row("DrawCentered", () => font.DrawCentered(r, s, at, Color.White), () => font.DrawCentered(r, s.AsSpan(2), at, Color.White), () => font.DrawCentered(r, $"Score {n}", at, Color.White));
    }

    [TestMethod]
    public void Every_kind_of_value_a_game_shows_is_written_without_garbage()
    {
        var font = BitmapFont.CreateDefault(Device);
        var r = new HeadlessRenderer(320, 180);
        int n = 0;
        var at = new Vector2(4, 4);
        void Value(string what, Action draw) => Free(what, () => { r.Reset(); n++; draw(); });

        Value("int", () => font.Draw(r, $"{n}", at, Color.White));
        Value("int with a format and a width", () => font.Draw(r, $"{n:000}|{n,6}|{n,-6:D2}", at, Color.White));
        Value("long, uint, ulong", () => font.Draw(r, $"{(long)n} {(uint)n} {(ulong)n}", at, Color.White));
        Value("short, ushort, byte, sbyte", () => font.Draw(r, $"{(short)n} {(ushort)n} {(byte)n} {(sbyte)(n % 100)}", at, Color.White));
        Value("float and double", () => font.Draw(r, $"{n * 0.5f:0.0} {n * 0.25:0.00} {n * 0.5f}", at, Color.White));
        Value("char, bool, string and span", () => font.Draw(r, $"{(char)('a' + n % 26)} {n % 2 == 0} {"text"} {"spanned".AsSpan(0, 4)} {"wide",6} {"s".AsSpan(),3}", at, Color.White));
        Value("a nullable number with a value", () => { int? hp = n; float? ratio = n * 0.5f; long? big = n; double? d = n; font.Draw(r, $"{hp} {ratio:0.0} {big} {d}", at, Color.White); });
        Value("a nullable number without one", () => { int? none = null; font.Draw(r, $"{none}{n}", at, Color.White); });
        Value("an enum", () => font.Draw(r, $"{(n % 2 == 0 ? Suit.Hearts : Suit.Spades)} {Sides.Left | Sides.Right}", at, Color.White));
        Value("a TimeSpan and a Guid", () => font.Draw(r, $"{TimeSpan.FromSeconds(n):mm\\:ss} {Guid.Empty}", at, Color.White));

        // The text is still what the string interpolation gives.
        int? three = 3, none2 = null;
        Assert.AreEqual($"{three}|{none2}|{Suit.Spades}|{Sides.Left | Sides.Right}|{Suit.Hearts:D}|{(Suit)9}|{three:000}", Read($"{three}|{none2}|{Suit.Spades}|{Sides.Left | Sides.Right}|{Suit.Hearts:D}|{(Suit)9}|{three:000}"));
    }

    private static string Read(InterpolatedText text)
    {
        string s = text.ToString();
        text.Dispose();
        return s;
    }

    private sealed class Sink : IRenderer
    {
        public int Draws;
        public int Width => 320;
        public int Height => 180;
        public void Clear(Color color) { }
        public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default) => Draws++;
        public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint) => Draws++;
        public void FillRect(RectF rect, Color color) => Draws++;
        public void FillCircle(Vector2 center, float radius, Color color) => Draws++;
        public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color) => Draws++;
        public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color) => Draws++;
        public void PushCamera(Camera2D camera) { }
        public void PopCamera() { }
        public void SetBlendMode(BlendMode mode) { }
        public void SetClip(RectF? screenRect) { }
        public void PushState(in DrawState state) { }
        public void PopState() { }
        public void PushTarget(RenderTarget2D target) { }
        public void PopTarget() { }
    }

    [TestMethod]
    public void A_hint_follows_a_rebinding_by_itself_and_costs_nothing_while_nothing_changes()
    {
        var input = InputMap.CreateDefault();
        var prompts = new ActionPrompts(input) { Override = InputDevice.Keyboard };
        var font = BitmapFont.CreateDefault(Device);
        var r = new Sink();
        const string template = "{confirm}: jump   {cancel}: back";
        void Frame() => font.Draw(r, prompts.Cached(template), new Vector2(4, 4), Color.White);
        Frame();
        Assert.AreEqual("Z: jump   X: back", prompts.Cached(template));
        int version = prompts.Version;
        AllocationAssert.None(Frame, warmup: 20, calls: 200, because: "a hint drawn every frame");
        Assert.AreEqual(version, prompts.Version, "nothing changed");

        // The player rebinds while the hint is on screen: no Invalidate, the next frame shows the new key.
        input.Unbind("confirm").Bind("confirm", Key.K);
        Assert.AreNotEqual(version, prompts.Version, "the prompts know the bindings changed");
        Assert.AreEqual("K: jump   X: back", prompts.Cached(template));
        Assert.AreEqual("K: jump   X: back", prompts.Format(template));
        Assert.AreEqual("K", prompts.Text("confirm"));
        Assert.AreEqual("K", prompts.Split(template)[0].Prompt!.Value.Text);

        Assert.IsTrue(input.Unbind("cancel", InputControl.Of(Key.X)));
        Assert.AreEqual("K: jump   Backspace: back", prompts.Cached(template), "one control removed");
        version = prompts.Version;
        Assert.IsFalse(input.Unbind("cancel", InputControl.Of(Key.X)), "nothing to remove");
        Assert.AreEqual(version, prompts.Version, "a call that changes nothing does not move the version");

        // Saved bindings applied to the map are a change like any other.
        Assert.IsTrue(input.ImportBindings(InputMap.CreateDefault().ExportBindings()) > 0);
        Assert.AreEqual("Z: jump   X: back", prompts.Cached(template), "bindings loaded from a save");
        AllocationAssert.None(Frame, warmup: 20, calls: 200, because: "the hint after the changes");
    }
}

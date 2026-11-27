using System.Globalization;
using System.Numerics;
using System.Text;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace MicroEngine.Tests;

/// <summary>A helper a game may have written before the engine's fonts had <c>DrawRight</c>: it must still compile beside the engine's.</summary>
internal static class GamesOwnTextHelpers
{
    public static int Calls;
    public static void DrawRight(this BitmapFont font, IRenderer r, string text, Vector2 at, Color color) => Calls++;
}

/// <summary>
/// Every way games call the fonts with text, in one place: this file compiling is the test. A change to the fonts'
/// overloads or to <see cref="InterpolatedText"/> that stops one of these shapes from compiling breaks games that
/// compiled before. The shapes that cannot bind to a buffer on the stack (an <c>await</c> or a <c>dynamic</c> value
/// inside the interpolated string) are in the migration notes, with their one-line fix, and at the end of this file
/// in that fixed form.
/// </summary>
[TestClass]
public sealed class TextCallShapeTests
{
    private readonly struct Money : IFormattable
    {
        public string ToString(string? format, IFormatProvider? provider) => "M" + format;
    }
    private readonly struct OnlyToString { public override string ToString() => "ots"; }
    private enum Kind { A, B }

    /// <summary>A font of a game's own, written against the interface as it was: two members.</summary>
    private sealed class OwnFont : IFont
    {
        public List<string> Drawn { get; } = [];
        public float LineHeight => 8;
        public Vector2 Measure(string text) => new(text.Length, 8);
        public void Draw(IRenderer renderer, string text, Vector2 position, Color color) => Drawn.Add(text);
    }

    [TestMethod]
    public void Every_call_shape_a_game_writes_compiles_and_runs()
    {
        var font = BitmapFont.CreateDefault(new HeadlessGraphicsDevice());
        IFont ifont = font;
        var mine = new OwnFont();
        IRenderer r = new HeadlessRenderer(320, 180);
        var at = new Vector2(1, 2);
        var c = Color.White;
        int n = 5; float f = 1.5f; string s = "abc"; string? nul = null; char ch = 'x'; bool b = true;
        var d = new DateTime(2026, 1, 2, 3, 4, 5); var ts = TimeSpan.FromSeconds(75.5); Kind k = Kind.B;
        object o = 12; int? ni = 3; decimal dec = 1.5m; Guid g = Guid.Empty;
        ReadOnlySpan<char> span = "span";
        Span<char> wspan = stackalloc char[2];
        wspan.Fill('w');
        var sb = new StringBuilder("sb");
        const string K = "const";

        // Strings, literals and constant interpolations.
        font.Draw(r, s, at, c);
        font.Draw(r, "literal", at, c);
        font.Draw(r, $"{K}-{nameof(Kind)}", at, c);
        font.Draw(r, $"no holes", at, c);
        ifont.Draw(r, s, at, c);
        ifont.Draw(r, "literal", at, c);
        mine.Draw(r, s, at, c);
        mine.Draw(r, $"{n}", at, c);
        ((IFont)mine).Draw(r, $"{n}!", at, c);
        CollectionAssert.AreEqual(new[] { "abc", "5", "5!" }, mine.Drawn, "a font written against the two string members draws all three forms");

        // Interpolations with every kind of value, format and width.
        font.Draw(r, $"{n}", at, c);
        font.Draw(r, $"{n}/{f:0.00} {n,5} {n,-5:D3} {d:HH:mm} {ts} {ts:mm\\:ss} {k} {k:D} {nul} {ch} {b} {o} {ni} {dec:C} {g}", at, c);
        font.Draw(r, $"{s,6}|{s,-6}|{s:x}|{ch,3}|{b,6}", at, c);
        font.Draw(r, $"{new Money():abc} {new OnlyToString()} {$"{n}!"}", at, c);
        font.Draw(r, $"{span} {wspan}", at, c);
        font.Draw(r, $"{span,6}", at, c);
        font.Draw(r, $"{span,-6}|", at, c);
        font.Draw(r, $"{span:x}", at, c);
        font.Draw(r, $"{span,6:x}", at, c);
        font.Draw(r, $"{wspan,4}", at, c);
        font.Draw(r, $"a{n}" + $"b{f}", at, c);
        font.Draw(r, $"a{n}" + "b", at, c);
        font.Draw(r, b ? $"a{n}" : "b", at, c);
        font.Draw(r, ($"a{n}"), at, c);
        font.Draw(r, (string)$"a{n}", at, c);
        font.Draw(r, string.Create(CultureInfo.InvariantCulture, $"a{f}"), at, c);
        font.Draw(r, FormattableString.Invariant($"a{f}"), at, c);
        font.Draw(renderer: r, text: $"a{n}", position: at, color: c);
        font.Draw(r, $"a{n}", at, c, 2f);
        font.Draw(r, $"a{n}", at, c, scale: 2f);
        font.Draw(r, $@"a\{n}", at, c);
        font.Draw(r, $"""raw {n}""", at, c);
        font.Draw(r, $"{(n > 3 ? "big" : "small")}", at, c);
        font.Draw(r, $"{n switch { 1 => "one", _ => "many" }}", at, c);
        font.Draw(r, $"{(object?)null}{(string?)null}{default(int?)}", at, c);
        font.Draw(r, $"{(nint)3} {(sbyte)-3} {(short)3} {(ushort)3} {(byte)3} {3u} {3L} {3ul} {3.0} {(Half)3}", at, c);
        font.Draw(r, $"{ifont}", at, c);
        font.DrawShadowed(r, $"{n}", at, c, Color.Black);
        font.DrawOutlined(r, $"{n}", at, c, Color.Black);
        font.DrawCentered(r, $"{n}", at, c);
        font.DrawCentered(r, $"{n}", at, c, 2f, Color.Black);
        font.DrawCentered(r, s, at, c, shadow: Color.Black);
        _ = font.Measure($"{n}");
        _ = font.Measure($"{n}", 2f);
        _ = font.Measure(s);
        _ = font.MeasureWidth(s);
        _ = font.MeasureWidth($"{n}");
        _ = ifont.Measure($"{n}");
        _ = ifont.MeasureWidth($"{n}");
        _ = ifont.MeasureWidth(s);
        ifont.Draw(r, $"{n}", at, c);
        ifont.DrawCentered(r, $"{n}", at, c);
        ifont.DrawShadowed(r, $"{n}", at, c, Color.Black);
        ifont.DrawShadowed(r, s, at, c, Color.Black, new Vector2(2, 2));
        _ = ifont.Wrap($"{n} words", 100);
        _ = font.Wrap($"{n} words", 100);

        // Delegates and method groups.
        Action<IRenderer, string, Vector2, Color> viaInterface = ifont.Draw;
        Func<string, Vector2> measure = ifont.Measure;
        Action<IRenderer, string, Vector2, Color, float> viaFont = font.Draw;
        Func<string, int> width = font.MeasureWidth;
        var strings = new[] { "a", "b" };
        _ = strings.Select(ifont.Measure).ToArray();
        _ = strings.Select(font.MeasureWidth).ToArray();
        _ = strings.Max(x => ifont.MeasureWidth(x));
        viaInterface(r, s, at, c);
        viaFont(r, s, at, c, 1f);
        Assert.AreEqual(font.Measure(s), measure(s));
        Assert.AreEqual(font.MeasureWidth(s), width(s));

        // A lambda that draws an interpolation.
        Action draw = () => font.Draw(r, $"{n}", at, c);
        draw();

        // A game's own DrawRight helper compiles beside the engine's method, which is the one called now.
        font.DrawRight(r, s, at, c);
        font.DrawRight(r, $"{n}", at, c);
        Assert.AreEqual(0, GamesOwnTextHelpers.Calls, "an instance method is chosen before an extension of the same name");
        GamesOwnTextHelpers.DrawRight(font, r, s, at, c);
        Assert.AreEqual(1, GamesOwnTextHelpers.Calls, "called by its class, the game's helper still runs");

        // Things that give a string.
        font.Draw(r, sb.ToString(), at, c);
        font.Draw(r, string.Concat(s, s), at, c);
        font.Draw(r, s + n, at, c);
        font.Draw(r, n.ToString(), at, c);
        font.Draw(r, s[1..], at, c);
        font.Draw(r, s?.ToUpperInvariant() ?? "", at, c);

        // The text of the shapes whose result can be read back.
        Assert.AreEqual($"{span,6}|{span,-6}|{span:x}|{wspan,4}|{k}|{ni}|{default(int?)}|", Text($"{span,6}|{span,-6}|{span:x}|{wspan,4}|{k}|{ni}|{default(int?)}|"));
    }

    private static string Text(InterpolatedText text)
    {
        string s = text.ToString();
        text.Dispose();
        return s;
    }

    [TestMethod]
    public async Task An_awaited_or_dynamic_value_is_put_in_a_local_first()
    {
        // `font.Draw(r, $"{await task}", …)` and `$"{(dynamic)n}"` do not compile against a buffer on the stack (CS4007,
        // CS9230); the value goes into a local, as the migration notes say.
        var font = BitmapFont.CreateDefault(new HeadlessGraphicsDevice());
        IRenderer r = new HeadlessRenderer(320, 180);
        int loaded = await Task.FromResult(3);
        font.Draw(r, $"{loaded}", default, Color.White);
        await Task.Yield();
        font.Draw(r, $"{3}", default, Color.White);
        dynamic value = 7;
        string shown = $"{value}";
        font.Draw(r, shown, default, Color.White);
        foreach (int _ in Iterator(font, r, 2)) { }
    }

    private static IEnumerable<int> Iterator(BitmapFont font, IRenderer r, int n)
    {
        font.Draw(r, $"{n}", default, Color.White);
        yield return 1;
        font.Draw(r, $"{n}", default, Color.White);
    }

    [TestMethod]
    public void A_null_string_is_refused_by_name()
    {
        var font = BitmapFont.CreateDefault(new HeadlessGraphicsDevice());
        IFont ifont = font;
        IRenderer r = new HeadlessRenderer(320, 180);
        string? nul = null;
        Assert.ThrowsExactly<ArgumentNullException>(() => font.Draw(r, nul!, default, Color.White));
        Assert.ThrowsExactly<ArgumentNullException>(() => font.Draw(r, null!, default, Color.White));
        Assert.ThrowsExactly<ArgumentNullException>(() => ifont.Draw(r, null!, default, Color.White));
        Assert.ThrowsExactly<ArgumentNullException>(() => font.Measure(nul!));
        Assert.ThrowsExactly<ArgumentNullException>(() => font.MeasureWidth(nul!));
        Assert.ThrowsExactly<ArgumentNullException>(() => font.DrawShadowed(r, nul!, default, Color.White, Color.Black));
        Assert.ThrowsExactly<ArgumentNullException>(() => font.DrawOutlined(r, nul!, default, Color.White, Color.Black));
        Assert.ThrowsExactly<ArgumentNullException>(() => font.DrawCentered(r, nul!, default, Color.White));
        Assert.ThrowsExactly<ArgumentNullException>(() => font.DrawRight(r, nul!, default, Color.White));
        // A null value inside an interpolation is nothing, as in a string.
        font.Draw(r, $"{nul}", default, Color.White);
    }

    [TestMethod]
    public void A_text_that_was_drawn_cannot_be_drawn_again()
    {
        var font = BitmapFont.CreateDefault(new HeadlessGraphicsDevice());
        IRenderer r = new HeadlessRenderer(320, 180);
        int n = 4;
        InterpolatedText kept = $"kept {n}";
        font.Draw(r, kept, default, Color.White);
        bool threw = false;
        try { font.Draw(r, kept, default, Color.White); }
        catch (ObjectDisposedException) { threw = true; }
        Assert.IsTrue(threw, "the second use of a kept text is refused: its buffer went back after the first");

        // The refusal took nothing from the next texts: they are written and read as usual.
        for (int i = 0; i < 50; i++) Assert.AreEqual($"after {i}", Text($"after {i}"));
        InterpolatedText twice = $"x{n}";
        twice.Dispose();
        twice.Dispose();
        Assert.AreEqual("y4", Text($"y{n}"), "giving a buffer back twice is harmless");

        // A text built while another is being written (a value that draws) keeps both intact.
        Assert.AreEqual("outer inner 4 end", Text($"outer {Inner(n)} end"));
        static string Inner(int n) => Text($"inner {n}");
    }
}

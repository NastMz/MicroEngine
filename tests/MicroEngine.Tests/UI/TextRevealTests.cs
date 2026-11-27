using System.Globalization;
using System.Numerics;
using System.Text;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.Testing;
using MicroEngine.UI;

namespace MicroEngine.Tests.Interface;

/// <summary>
/// Text revealed a character at a time (<see cref="TextReveal"/>): characters per second on the clock it is updated
/// with, counted as a reader counts them (a letter with its marks is one), a call per revealed character for a sound,
/// the rest at once on demand, no allocation while it runs; and a <see cref="Label"/> that reveals its text with one.
/// </summary>
[TestClass]
public sealed class TextRevealTests
{
    private const float Step = 1f / 60f;

    private const string Page =
        "Welcome! The gems only grow at night, and only where the moonlight\ntouches the soil of the old garden behind the well. My lantern broke.";

    /// <summary>
    /// The arithmetic of the typewriter the four demo games share (<c>games/Shared/Typewriter.cs</c>): a float count
    /// that grows by rate times step, capped at the text's length, shown as its whole part.
    /// </summary>
    private static int[] DemoCounts(string text, float rate, int updates)
    {
        var counts = new int[updates];
        float visible = 0;
        for (int i = 0; i < updates; i++)
        {
            visible = MathF.Min(text.Length, visible + rate * Step);
            counts[i] = Math.Min(text.Length, (int)visible);
        }
        return counts;
    }

    [TestMethod]
    [DataRow(45f, DisplayName = "45 a second: Courtyard Key's dialogue box and the demos' default")]
    [DataRow(60f, DisplayName = "60 a second: Trail Duel's text panel")]
    public void It_reveals_the_characters_the_demos_typewriter_reveals_on_every_update(float rate)
    {
        var expected = DemoCounts(Page, rate, 240);
        var reveal = new TextReveal { CharactersPerSecond = rate };
        var blips = new StringBuilder();
        reveal.CharacterRevealed += rune => blips.Append(rune.ToString());
        reveal.Start(Page);
        Assert.AreEqual(0, reveal.Revealed);
        for (int i = 0; i < expected.Length; i++)
        {

            reveal.Update(Step);
            Assert.AreEqual(expected[i], reveal.Revealed, $"characters shown after update {i + 1} at {rate} a second");
            Assert.AreEqual(Page[..expected[i]], reveal.Visible.ToString());
            Assert.AreEqual(expected[i], reveal.VisibleLength, "plain text: one char per character");
            Assert.AreEqual(expected[i] >= Page.Length, reveal.Complete);

        }
        Assert.IsTrue(reveal.Complete, "the page is shorter than four seconds of text");
        Assert.AreEqual(string.Concat(Page.Where(c => !char.IsWhiteSpace(c))), blips.ToString(),
            "a call for every character that is not a space or a line break, in order, once");
    }

    [TestMethod]
    public void Finish_shows_the_rest_at_once_without_a_call_per_character()
    {
        var reveal = new TextReveal();
        int calls = 0;
        reveal.CharacterRevealed += _ => calls++;
        reveal.Start("HELLO THERE");
        reveal.Update(Step);
        int soFar = calls;
        reveal.Finish();
        Assert.IsTrue(reveal.Complete);
        Assert.AreEqual("HELLO THERE", reveal.Visible.ToString());
        Assert.AreEqual(soFar, calls, "skipping is silent");
        reveal.Update(1);
        Assert.AreEqual(soFar, calls);
    }

    [TestMethod]
    public void Start_can_keep_what_was_already_shown()
    {
        var reveal = new TextReveal();
        reveal.Start("ABCDEF", 4);
        Assert.AreEqual(4, reveal.Revealed);
        Assert.AreEqual("ABCD", reveal.Visible.ToString());
        reveal.Start("AB", 9);
        Assert.AreEqual(2, reveal.Revealed, "never more than the text has");
        Assert.IsTrue(reveal.Complete);
        reveal.Start(null!);
        Assert.AreEqual("", reveal.Text, "null counts as empty");
        Assert.IsTrue(reveal.Complete);
    }

    [TestMethod]
    public void A_letter_with_its_marks_an_emoji_sequence_and_a_line_break_are_one_character_each()
    {
        // e + combining acute; a family joined by zero-width joiners; a flag (two regional indicators); CR LF;
        // an Arabic letter with a vowel mark; a Hebrew letter with a point.
        string[] characters = ["é", "\U0001F468‍\U0001F469‍\U0001F467", "\U0001F1E8\U0001F1F4", "\r\n", "بَ", "שׁ", "z"];
        string text = string.Concat(characters);
        var reveal = new TextReveal { CharactersPerSecond = 60 };
        var seen = new List<string>();
        reveal.CharacterRevealed += rune => seen.Add(rune.ToString());
        reveal.Start(text);
        Assert.AreEqual(characters.Length, reveal.Count, "seven characters as a reader counts them");
        Assert.AreEqual(new StringInfo(text).LengthInTextElements, reveal.Count);
        string shown = "";
        for (int i = 0; i < characters.Length; i++)
        {
            reveal.Update(Step);                                   // one a step at 60 a second
            shown += characters[i];
            Assert.AreEqual(i + 1, reveal.Revealed);
            Assert.AreEqual(shown, reveal.Visible.ToString(), "never half a character");
            Assert.AreEqual(shown.Length, reveal.VisibleLength);
        }
        Assert.IsTrue(reveal.Complete);
        CollectionAssert.AreEqual(new[] { "e", "\U0001F468", "\U0001F1E8", "ب", "ש", "z" }, seen,
            "the call carries the character's first code point; the line break makes no sound");
    }

    [TestMethod]
    public void A_pause_after_a_character_holds_the_reveal_for_that_long()
    {
        // A step of 1/64 s and 64 characters a second: every sum here is exact, so the test counts updates.
        const float step = 1f / 64f;
        var reveal = new TextReveal { CharactersPerSecond = 64, PauseAfter = rune => rune.Value == '.' ? 0.5f : 0 };
        reveal.Start("A.BC.");
        reveal.Update(step);
        reveal.Update(step);
        Assert.AreEqual("A.", reveal.Visible.ToString());
        for (int i = 0; i < 31; i++) reveal.Update(step);
        Assert.AreEqual("A.", reveal.Visible.ToString(), "still held just before half a second");
        reveal.Update(step);
        reveal.Update(step);
        Assert.AreEqual("A.B", reveal.Visible.ToString(), "then it goes on");
        reveal.Update(step);
        reveal.Update(step);
        Assert.IsTrue(reveal.Complete, "no pause after the last character: the text is complete as soon as it all shows");

        reveal.Start("A.B");
        reveal.Update(1);
        Assert.AreEqual("A.", reveal.Visible.ToString(), "a long step stops at the pause too");
        reveal.Finish();
        Assert.IsTrue(reveal.Complete, "finishing ends a pause");
    }

    [TestMethod]
    public void The_lines_show_what_is_revealed_and_keep_whole_lines_as_they_are()
    {
        var reveal = new TextReveal { CharactersPerSecond = 60 };
        reveal.Start("AB\nCD");
        CollectionAssert.AreEqual(new[] { "AB", "CD" }, reveal.Lines.ToArray());
        CollectionAssert.AreEqual(new[] { "", "" }, reveal.VisibleLines.ToArray());
        reveal.Update(Step);
        CollectionAssert.AreEqual(new[] { "A", "" }, reveal.VisibleLines.ToArray());
        reveal.Update(Step);
        reveal.Update(Step);                                        // the line break is a character
        CollectionAssert.AreEqual(new[] { "AB", "" }, reveal.VisibleLines.ToArray());
        Assert.AreSame(reveal.Lines[0], reveal.VisibleLines[0], "a whole line is the line itself, not a copy");
        reveal.Update(Step);
        CollectionAssert.AreEqual(new[] { "AB", "C" }, reveal.VisibleLines.ToArray());
        var list = reveal.VisibleLines;
        Assert.AreSame(list, reveal.VisibleLines, "asked twice without a change: the same list");
    }

    [TestMethod]
    [DoNotParallelize]
    public void Revealing_allocates_nothing()
    {
        var reveal = new TextReveal { CharactersPerSecond = 45 };
        int blips = 0, length = 0;
        reveal.CharacterRevealed += _ => blips++;
        reveal.Start(Page);
        AllocationAssert.None(() =>
        {
            if (reveal.Complete) reveal.Start(Page);               // again and again: starting over allocates nothing either
            reveal.Update(Step);
            length += reveal.Visible.Length;
        }, warmup: 300, calls: 400, rounds: 4, because: "a reveal runs every frame of a dialogue");
        Assert.IsTrue(blips > 0 && length > 0);

        reveal.Finish();
        var lines = reveal.VisibleLines;
        AllocationAssert.None(() =>
        {
            reveal.Update(Step);
            length += reveal.VisibleLines.Count;
        }, calls: 100, rounds: 3, because: "a page that is all shown is drawn every frame until the player goes on");
        Assert.AreSame(lines, reveal.VisibleLines);
    }

    [TestMethod]
    public void Its_state_is_what_shows_and_what_is_left_to_show()
    {
        static ulong Hash(TextReveal r)
        {
            var hash = new StateHasher();
            hash.Add(r);
            return hash.Value;
        }
        var a = new TextReveal();
        var b = new TextReveal();
        a.Start("SOME TEXT");
        b.Start("SOME TEXT");
        Assert.AreEqual(Hash(a), Hash(b));
        a.Update(Step);
        Assert.AreNotEqual(Hash(a), Hash(b), "one is further on");
        b.Update(Step);
        Assert.AreEqual(Hash(a), Hash(b));
        b.Start("OTHER TEXT", a.Revealed);
        Assert.AreNotEqual(Hash(a), Hash(b), "another text");
    }

    // ---- a label that reveals its text

    private sealed class RevealScene : Scene
    {
        public Label Text = null!;
        public int Blips;
        public string? Next;
        public override void Enter()
        {
            Text = new Label("HELLO, TRAVELLER") { Reveal = new TextReveal { CharactersPerSecond = 60 } };
            Text.Reveal!.CharacterRevealed += _ => Blips++;
            Widgets.Content = Text;
        }
        public override void Update(float dt)
        {
            if (Next is null) return;
            Text.Text = Next;
            Next = null;
        }
        public override void Draw(IRenderer renderer) { }
    }

    private sealed class RevealGame(Scene scene) : Game
    {
        public override GameSettings Settings { get; } = new("reveal", 320, 180, 1);
        protected override void Load() => Scenes.Push(scene);
    }

    private static (Game Game, HeadlessPlatform Platform, RevealScene Scene) Start()
    {
        var scene = new RevealScene();
        var game = new RevealGame(scene);
        var platform = HeadlessPlatform.For(game.Settings);
        platform.Resize(320, 180);
        platform.PumpEvents(new IgnoredEvents());
        game.InitializeForTest(platform);
        return (game, platform, scene);
    }

    private static void Frame(Game game, HeadlessPlatform platform)
    {
        game.StepForTest();
        game.Render(platform.BeginFrame());
        platform.EndFrame();
    }

    [TestMethod]
    public void A_label_with_a_reveal_shows_its_text_a_character_at_a_time_on_the_scenes_clock()
    {
        var (game, platform, scene) = Start();
        Frame(game, platform);
        Assert.AreEqual("H", scene.Text.VisibleLines[0], "one character per update at 60 a second");
        Frame(game, platform);
        Frame(game, platform);
        Assert.AreEqual("HEL", scene.Text.VisibleLines[0]);
        Assert.AreEqual(3, scene.Blips);
        Assert.AreEqual("HELLO, TRAVELLER", scene.Text.Lines[0], "the label keeps the lines and the size of its whole text");
        float width = scene.Text.Bounds.Width;
        Assert.AreEqual(game.Font.MeasureWidth("HELLO, TRAVELLER"), width);

        scene.Text.Reveal!.Finish();
        Frame(game, platform);
        Assert.AreEqual("HELLO, TRAVELLER", scene.Text.VisibleLines[0]);
        Assert.IsTrue(scene.Text.Reveal.Complete);
    }

    [TestMethod]
    public void A_new_text_starts_the_reveal_again_and_never_shows_a_piece_of_it_early()
    {
        var (game, platform, scene) = Start();
        for (int i = 0; i < 5; i++) Frame(game, platform);
        Assert.AreEqual("HELLO", scene.Text.VisibleLines[0]);

        scene.Next = "GOODBYE";                                    // set by the scene's update, after the widgets'
        game.StepForTest();
        Assert.AreEqual("", scene.Text.VisibleLines[0], "nothing of the new text shows before its reveal starts");
        game.Render(platform.BeginFrame());
        platform.EndFrame();
        Frame(game, platform);
        Assert.AreEqual("G", scene.Text.VisibleLines[0]);
        Assert.AreEqual("GOODBYE", scene.Text.Reveal!.Text);
    }

    [TestMethod]
    public void A_label_reveals_the_text_of_the_language_in_use_and_a_right_to_left_line_grows_from_its_right_edge()
    {
        var (game, platform, scene) = Start();
        var texts = new MicroEngine.Localization.Localizer();
        // Arabic with vowel marks: three letters, each with a mark, are three characters and six chars.
        texts.Add("ar", new Dictionary<string, string> { ["greeting"] = "بَتَثَ" });
        texts.Add("en", new Dictionary<string, string> { ["greeting"] = "HELLO" });
        texts.Language = "en";
        var root = game.Scenes.Current!.Widgets;
        root.Localization = texts;
        root.Content = null;
        root.Content = new Stack { Width = 200, Align = Align.Stretch }.Add(scene.Text);
        scene.Text.Localized = new MicroEngine.Localization.LocalizedText("greeting");
        Frame(game, platform);
        Frame(game, platform);
        Assert.AreEqual("HE", scene.Text.VisibleLines[0], "the key's text in English, two updates on");
        Assert.AreEqual(scene.Text.Bounds.X, scene.Text.LineStart(0));

        texts.Language = "ar";                                      // the text changes: the reveal starts again
        Frame(game, platform);
        Assert.AreEqual("بَ", scene.Text.VisibleLines[0], "one character: the letter with its mark, never the letter alone");
        float one = scene.Text.LineStart(0);
        Assert.AreEqual(scene.Text.Bounds.Right - game.Font.MeasureWidth(scene.Text.VisibleLines[0]), one, 0.01f, "it ends at the right edge");
        Frame(game, platform);
        Assert.AreEqual("بَتَ", scene.Text.VisibleLines[0]);
        Assert.IsTrue(scene.Text.LineStart(0) < one, "the line grows leftwards");
        Frame(game, platform);
        Assert.IsTrue(scene.Text.Reveal!.Complete);
        Assert.AreEqual(3, scene.Text.Reveal.Count);
    }

    [TestMethod]
    public void A_revealing_label_is_part_of_the_scenes_state()
    {
        var (game, platform, scene) = Start();
        Frame(game, platform);
        static ulong Hash(Game g)
        {
            var hash = new StateHasher();
            g.Scenes.Current!.Widgets.HashState(hash);
            return hash.Value;
        }
        ulong one = Hash(game);
        Frame(game, platform);
        Assert.AreNotEqual(one, Hash(game), "another character shows: what confirm does next depends on it");
    }

    [TestMethod]
    [DoNotParallelize]
    public void A_label_whose_text_is_all_shown_or_not_changing_is_drawn_without_allocating()
    {
        var (game, platform, scene) = Start();
        var label = scene.Text;
        label.Reveal = null;
        label.Text = "ONE LINE\nAND ANOTHER";
        label.VisibleCharacters = 12;
        Frame(game, platform);
        var first = label.VisibleLines;
        CollectionAssert.AreEqual(new[] { "ONE LINE", "AND" }, first.ToArray());
        int n = 0;
        AllocationAssert.None(() => n += label.VisibleLines.Count, calls: 100, rounds: 3, because: "the label is drawn every frame");
        label.VisibleCharacters = 13;
        CollectionAssert.AreEqual(new[] { "ONE LINE", "AND " }, label.VisibleLines.ToArray());
    }
}

using Examples;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Localization;

namespace Samples.Tests;

/// <summary>
/// The localization example: one press changes the language of every text, plurals follow each language's forms,
/// the hint names keys in the language, Arabic reads from the right with its own image, and the choice is saved.
/// </summary>
[TestClass]
public sealed class LocalizationExampleTests
{
    private static (ExamplesGame Game, GameSession Platform, LocalizationExample Scene) Open(MemorySaveStorage? saves = null)
    {
        // A session runs whole frames, drawing included: drawing is what places a label's lines.
        var session = new GameSession(() => new ExamplesGame(), game =>
        {
            var platform = HeadlessPlatform.For(game.Settings, rasterize: false);
            platform.Resize(960, 540);
            return platform;
        });
        Assert.IsTrue(session.Start(new RunOptions { Saves = saves }));
        var examples = (ExamplesGame)session.Game!;
        var scene = new LocalizationExample();
        examples.Scenes.Push(scene);
        Draw(examples, session);
        Draw(examples, session);
        return (examples, session, scene);
    }

    private static void Draw(ExamplesGame game, GameSession session) => Assert.IsTrue(session.Tick());

    private static void Press(ExamplesGame game, GameSession session, string action)
    {
        game.Input.SetForced(action, true);
        Draw(game, session);
        game.Input.SetForced(action, false);
        Draw(game, session);
    }

    [TestMethod]
    public void OnePressChangesTheLanguageOfEveryText()
    {
        var (game, platform, scene) = Open();
        Assert.AreEqual("en", game.Localization.Language);
        Assert.AreEqual("Localization", scene.Title.Text);
        Assert.AreEqual("Language: English", scene.Language.Text);
        Assert.AreEqual("No gems yet", scene.Gems.Text);
        Assert.AreEqual("Next language", scene.Next.Text);
        StringAssert.Contains(scene.Hint.Text, "Press Z / Space / Enter / J for the next language and C to add a gem");

        Press(game, platform, "action");
        Assert.AreEqual("1 gem", scene.Gems.Text);
        Press(game, platform, "action");
        Assert.AreEqual("2 gems", scene.Gems.Text);

        Press(game, platform, "confirm");
        Assert.AreEqual("es", game.Localization.Language);
        Assert.AreEqual("Localización", scene.Title.Text);
        Assert.AreEqual("Idioma: Español", scene.Language.Text);
        Assert.AreEqual("2 gemas", scene.Gems.Text);
        Assert.AreEqual("Siguiente idioma", scene.Next.Text);
        StringAssert.Contains(scene.Hint.Text, "Pulsa Z / Espacio / Intro / J para cambiar de idioma y C para sumar una gema");
        Assert.AreEqual("localization/next.png", scene.ImagePath, "Spanish has no image of its own");
    }

    [TestMethod]
    public void RussianCountsWithItsFourForms()
    {
        var (game, platform, scene) = Open();
        game.Localization.Language = "ru";
        Draw(game, platform);
        Assert.AreEqual("Самоцветов пока нет", scene.Gems.Text);
        var seen = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            Press(game, platform, "action");
            seen.Add(scene.Gems.Text);
        }
        CollectionAssert.AreEqual(new[] { "1 самоцвет", "2 самоцвета", "3 самоцвета", "4 самоцвета", "5 самоцветов" }, seen);
        StringAssert.Contains(scene.Hint.Text, "Пробел");
    }

    [TestMethod]
    public void ArabicReadsFromTheRightWithItsOwnFontAndImage()
    {
        var (game, platform, scene) = Open();
        game.Localization.Language = "ar";
        Draw(game, platform);
        Assert.IsTrue(game.Localization.RightToLeft);
        Assert.AreEqual("التعريب", scene.Title.Text);
        Assert.AreEqual("localization/next.ar.png", scene.ImagePath, "the arrow points the other way");
        var font = (ShapedFontSize)scene.Widgets.Font!;
        Assert.IsTrue(font.Shape(scene.Title.Text).All(g => g.Glyph != 0), "every letter has a glyph in the language's fonts");
        Assert.IsTrue(font.Shape(scene.Title.Text).Any(g => g.Face == 1), "Arabic letters from the second font the table names");
        float width = font.MeasureWidth(scene.Title.Text);
        Assert.IsTrue(width < scene.Title.Bounds.Width - 4, "the title is narrower than its column");
        Assert.AreEqual(scene.Title.Bounds.Right - width, scene.Title.LineStart(0), 1f, "the line ends at the right edge");
        Press(game, platform, "action");
        Press(game, platform, "action");
        Assert.AreEqual("جوهرتان", scene.Gems.Text, "the form for two");
    }

    [TestMethod]
    public void TheChoiceIsSavedAndLeavingRestoresEnglishForTheOtherExamples()
    {
        var saves = new MemorySaveStorage();   // the same saves for the second launch
        var (game, platform, _) = Open(saves);
        Press(game, platform, "confirm");
        Assert.AreEqual("es", game.Localization.Language);
        Assert.AreEqual("es", GameSaves.LoadSettings(game.Saves).Language);
        game.Scenes.Pop();
        Draw(game, platform);
        Assert.AreEqual("en", game.Localization.Language, "the rest of the tour stays in English");
        Assert.AreEqual("Space", game.Prompts.KeyText(MicroEngine.Input.Key.Space));

        var (again, _, scene) = Open(saves);
        Assert.AreEqual("es", again.Localization.Language, "the saved choice comes back");
        Assert.AreEqual("Localización", scene.Title.Text);
    }

    [TestMethod]
    public void TheExamplesTablesAgree()
    {
        var (game, _, _) = Open();
        var text = game.Localization;
        CollectionAssert.AreEqual(new[] { "ar", "en", "es", "ru" }, text.Languages.Select(l => l.Code).ToArray());
        var tables = text.Languages.Select(l => new StringTableData(l.Code, text.Keys(l.Code).ToDictionary(k => k, k => text.Pattern(k, l.Code)!)));
        var issues = StringTableChecks.Check(tables);
        Assert.AreEqual(0, issues.Count, string.Join("\n", issues));
        foreach (var language in text.Languages)
        {
            var fonts = language.Fonts.Select(f => FontCoverage.Read(game.Assets.Bytes(f))).ToArray();
            Assert.AreEqual("", FontCoverage.Missing(text.Characters(language.Code), fonts), $"{language.Code}: its fonts have every character its texts show");
        }
    }
}

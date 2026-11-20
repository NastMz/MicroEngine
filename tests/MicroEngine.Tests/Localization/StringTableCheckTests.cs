using MicroEngine.Core;
using MicroEngine.Localization;

namespace MicroEngine.Tests.Localization;

/// <summary>What is wrong with a game's string tables before a player sees it, and which characters a font has.</summary>
[TestClass]
public sealed class StringTableCheckTests
{
    private static StringTableData Table(string language, params (string Key, string Text)[] entries) =>
        new(language, entries.ToDictionary(e => e.Key, e => e.Text));

    private static IReadOnlyList<LocalizationIssue> Check(params StringTableData[] tables) => StringTableChecks.Check(tables);

    [TestMethod]
    public void TablesThatAgreeHaveNothingToSay()
    {
        var issues = Check(
            Table("en", ("play", "Play"), ("gems", "{n, plural, one {# gem} other {# gems}}"), ("hint", "Press {jump}")),
            // The name of a key is translated where a language has another word for it: no language must.
            Table("es", ("play", "Jugar"), ("gems", "{n, plural, one {# gema} other {# gemas}}"), ("hint", "Pulsa {jump}"), ("prompt.Space", "Espacio")),
            Table("es-MX", ("play", "Jugar ya")),
            Table("ja", ("play", "プレイ"), ("gems", "{n, plural, other {宝石 # 個}}"), ("hint", "{jump} を押す")));
        Assert.AreEqual(0, issues.Count, string.Join("\n", issues));
    }

    [TestMethod]
    public void AKeyALanguageLacksIsAnError()
    {
        var issues = Check(
            Table("en", ("play", "Play"), ("quit", "Quit"), ("new", "New")),
            Table("es", ("play", "Jugar")),
            Table("es-MX", ("quit", "Cerrar")));
        var spanish = issues.Where(i => i.Language == "es").ToList();
        Assert.AreEqual(2, spanish.Count, string.Join("\n", issues));
        Assert.IsTrue(spanish.All(i => i.Error));
        CollectionAssert.AreEquivalent(new[] { "quit", "new" }, spanish.Select(i => i.Key).ToArray());
        StringAssert.Contains(spanish[0].Message, "en");
        // A region takes what it lacks from its language, and lacks only what that lacks too.
        CollectionAssert.AreEqual(new[] { "new" }, issues.Where(i => i.Language == "es-MX").Select(i => i.Key).ToArray());
        Assert.IsFalse(issues.Any(i => i.Language == "en"));
    }

    [TestMethod]
    public void AMessageThatCannotBeReadIsAnError()
    {
        var issue = Check(Table("en", ("gems", "{n, plural, one {# gem}}"))).Single();
        Assert.IsTrue(issue.Error);
        Assert.AreEqual("gems", issue.Key);
        StringAssert.Contains(issue.Message, "other");
    }

    [TestMethod]
    public void ValuesMustAgreeAcrossLanguages()
    {
        var issues = Check(
            Table("en", ("found", "{who} found {n} keys"), ("hint", "Press {jump}")),
            Table("es", ("found", "{quien} encontró {n} llaves"), ("hint", "Pulsa el botón")));
        var typo = issues.Single(i => i.Key == "found" && i.Error);
        Assert.AreEqual("es", typo.Language);
        StringAssert.Contains(typo.Message, "{quien}");
        // A value a translation does not use is allowed, and said.
        var unused = issues.Where(i => !i.Error).ToList();
        Assert.AreEqual(2, unused.Count, string.Join("\n", issues));
        Assert.IsTrue(unused.Any(i => i.Key == "found" && i.Message.Contains("{who}")));
        Assert.IsTrue(unused.Any(i => i.Key == "hint" && i.Message.Contains("{jump}")));
    }

    [TestMethod]
    public void PluralsMustGiveTheFormsOfTheirLanguage()
    {
        var issues = Check(
            Table("en", ("gems", "{n, plural, one {# gem} few {# gemz} other {# gems}}")),
            Table("ru", ("gems", "{n, plural, one {# самоцвет} other {# самоцвета}}")),
            Table("ar", ("gems", "{n, plural, =0 {لا شيء} one {واحد} two {اثنان} few {#} many {#} other {#}}")),
            Table("tlh", ("gems", "{n, plural, one {#} other {#}}")));
        var russian = issues.Single(i => i.Language == "ru");
        Assert.IsFalse(russian.Error);
        StringAssert.Contains(russian.Message, "few, many");
        var english = issues.Single(i => i.Language == "en");
        StringAssert.Contains(english.Message, "few");
        Assert.IsFalse(issues.Any(i => i.Language == "ar"), "=0 stands for the zero form");
        var unknown = issues.Single(i => i.Language == "tlh");
        Assert.IsNull(unknown.Key);
        StringAssert.Contains(unknown.Message, "PluralRules.Register");
    }

    private static byte[] Font(string file) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "ExampleFonts", file));

    [TestMethod]
    public void AFontSaysWhichCharactersItHas()
    {
        var inter = FontCoverage.Read(Font("Inter-Regular.ttf"));
        Assert.IsTrue(inter.Covers('A'));
        Assert.IsTrue(inter.Covers('ñ'));
        Assert.IsFalse(inter.Covers('م'));
        Assert.IsFalse(inter.Covers(0x1F600), "a character beyond the basic plane");
        Assert.AreEqual("", inter.Missing("Jardín: ¿3 gemas?"));
        Assert.AreEqual("مح", inter.Missing("مرحبا"[..1] + "A" + "ح" + "\n "), "each missing character once, in the order met; spaces and line breaks never count");

        var arabic = FontCoverage.Read(Font("NotoSansArabic-Regular.ttf"));
        Assert.IsTrue(arabic.Covers('م'));
        Assert.AreEqual("", FontCoverage.Missing("مرحبا Ana", inter, arabic), "a chain covers what any of its faces has");
        Assert.AreEqual("宝", FontCoverage.Missing("宝 Ana", inter, arabic));

        Assert.ThrowsExactly<InvalidDataException>(() => FontCoverage.Read([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]));
    }
}

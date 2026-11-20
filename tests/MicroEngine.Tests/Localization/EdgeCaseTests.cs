using MicroEngine.Localization;

namespace MicroEngine.Tests.Localization;

/// <summary>Edges of messages, tables and the language in use that content files and saved choices can reach.</summary>
[TestClass]
public sealed class EdgeCaseTests
{
    [TestMethod]
    public void AMessageNestedTooDeepIsRefusedInsteadOfEndingTheProcess()
    {
        string deep = string.Concat(Enumerable.Repeat("{a, select, other {", 5000)) + "x" + string.Concat(Enumerable.Repeat("}}", 5000));
        Assert.IsFalse(Message.TryParse(deep, out var message, out var error));
        Assert.IsNull(message);
        StringAssert.Contains(error, Message.MaxDepth.ToString());
        Assert.ThrowsExactly<FormatException>(() => Message.Parse(deep));

        string allowed = string.Concat(Enumerable.Repeat("{a, select, other {", Message.MaxDepth)) + "x" + string.Concat(Enumerable.Repeat("}}", Message.MaxDepth));
        Assert.AreEqual("x", Message.Parse(allowed).Format("en", [("a", "b")]));
    }

    [TestMethod]
    [DataRow("{a,\tplural, other {x}}")]
    [DataRow("{a,\r\nplural,\r\n\tother {x}}")]
    [DataRow("{a ,\n plural\t,\tother {x}}")]
    public void TabsAndLineBreaksInsideAnArgumentAreSpace(string pattern) =>
        Assert.AreEqual("x", Message.Parse(pattern).Format("en", [("a", 3)]));

    [TestMethod]
    public void ACsvLineOfSpacesIsAnEmptyLine()
    {
        var tables = StringTables.ParseCsv("key,en\na,1\n \n\t,  \nb,2\n");
        Assert.AreEqual(2, tables[0].Entries.Count);
        Assert.AreEqual("2", tables[0].Entries["b"]);
    }

    [TestMethod]
    public void AStringsFileIsATableWhateverItsNameAndItsNameMustBeALanguage()
    {
        Assert.IsTrue(StringTables.IsTable("text/menu.strings.json"), "recognised, so it is reported and never skipped");
        Assert.IsNull(StringTables.LanguageOf("text/menu.strings.json"));
        Assert.AreEqual("en", StringTables.LanguageOf("text/en.strings.json"));
    }

    [TestMethod]
    public void AChoiceMadeBeforeTheTablesIsMatchedWhenTheyArrive()
    {
        var text = new Localizer();
        Assert.IsTrue(text.TrySetLanguage("zz"));
        Assert.IsTrue(text.TrySetLanguage("ES-mx"));
        text.Add("en", new Dictionary<string, string> { ["a"] = "A" });
        Assert.AreEqual("en", text.Language, "no table for the choice yet: the default, a language the game has");
        Assert.IsFalse(text.LanguageChosen);
        text.Add("es", new Dictionary<string, string> { ["a"] = "Á" });
        Assert.AreEqual("es", text.Language, "the choice is taken once its table is there");
        Assert.IsTrue(text.LanguageChosen);
        CollectionAssert.Contains(text.Languages.Select(l => l.Code).ToArray(), text.Language);

        var never = new Localizer();
        never.TrySetLanguage("zz");
        never.Add("en", new Dictionary<string, string> { ["a"] = "A" });
        Assert.AreEqual("en", never.Language);
    }

    [TestMethod]
    public void NumbersAtTheEdges()
    {
        Assert.AreEqual("0 gems", Message.Parse("{n, plural, one {# gem} other {# gems}}").Format("en", [("n", -0.0004)]));
        Assert.AreEqual("0", new TextArgument("n", -0.0).ToString());
        Assert.AreEqual("-0.5", new TextArgument("n", -0.5).ToString());
        Assert.AreEqual(PluralCategory.Many, PluralRules.Select("ru", 1e19));
        Assert.AreEqual(PluralCategory.Many, PluralRules.Select("pl", 9223372036854775808d));
        Assert.AreEqual(PluralCategory.Other, PluralRules.Select("ru", 1.5));
    }
}

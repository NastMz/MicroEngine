using MicroEngine.Localization;

namespace MicroEngine.Tests.Localization;

/// <summary>String tables, the language in use, fallbacks, what is reported, and the files tables are read from.</summary>
[TestClass]
public sealed class LocalizerTests
{
    private static Localizer Tables()
    {
        var text = new Localizer();
        text.Add("en", new Dictionary<string, string>
        {
            ["menu.play"] = "Play",
            ["menu.quit"] = "Quit",
            ["gems"] = "{count, plural, one {# gem} other {# gems}}",
            ["only.english"] = "English only",
        });
        text.Add("es", new Dictionary<string, string>
        {
            ["menu.play"] = "Jugar",
            ["menu.quit"] = "Salir",
            ["gems"] = "{count, plural, one {# gema} other {# gemas}}",
        });
        text.Add("es-MX", new Dictionary<string, string> { ["menu.quit"] = "Cerrar" });
        return text;
    }

    [TestMethod]
    public void TextComesFromTheLanguageInUse()
    {
        var text = Tables();
        Assert.AreEqual("en", text.Language, "the fallback language until another is chosen");
        Assert.AreEqual("Play", text["menu.play"]);
        Assert.AreEqual("3 gems", text.Format("gems", ("count", 3)));

        text.Language = "es";
        Assert.AreEqual("Jugar", text["menu.play"]);
        Assert.AreEqual("1 gema", text.Format("gems", ("count", 1)));
        Assert.AreEqual("2 gemas", text.Format(new LocalizedText("gems", ("count", 2))));
    }

    [TestMethod]
    public void ARegionFallsBackToItsLanguageAndThenToTheFallbackLanguage()
    {
        var text = Tables();
        text.Language = "es-MX";
        Assert.AreEqual("es-MX", text.Language);
        Assert.AreEqual("Cerrar", text["menu.quit"], "the region's own text");
        Assert.AreEqual("Jugar", text["menu.play"], "the language's text");
        Assert.AreEqual("English only", text["only.english"], "the fallback language's text");
        Assert.IsTrue(text.Has("only.english"));
        Assert.IsFalse(text.Has("only.english", "es"), "asked for one language, a key is there or not");
    }

    [TestMethod]
    public void ChangingTheLanguageRaisesChangedAndMovesTheVersion()
    {
        var text = Tables();
        int changes = 0, version = text.Version;
        text.Changed += () => changes++;
        text.Language = "es";
        Assert.AreEqual(1, changes);
        Assert.AreNotEqual(version, text.Version);
        version = text.Version;
        text.Language = "es";
        Assert.AreEqual(1, changes, "the same language again changes nothing");
        Assert.AreEqual(version, text.Version);
        text.Add("es", new Dictionary<string, string> { ["new"] = "Nuevo" });
        Assert.AreEqual(2, changes, "more text for a language is a change too");
    }

    [TestMethod]
    public void ALanguageWithoutATableIsRefusedNamingThoseThatExist()
    {
        var text = Tables();
        var error = Assert.ThrowsExactly<ArgumentException>(() => text.Language = "fr");
        StringAssert.Contains(error.Message, "en, es, es-MX");
        Assert.IsFalse(text.TrySetLanguage("fr"));
        Assert.IsFalse(text.TrySetLanguage(null));
        Assert.AreEqual("en", text.Language);
        Assert.IsTrue(text.TrySetLanguage("ES"));
        Assert.AreEqual("es", text.Language);
    }

    [TestMethod]
    public void ThePreferredLanguagesOfAPlayerAreMatchedToTheTables()
    {
        var text = Tables();
        Assert.AreEqual("es-MX", text.Match(["fr-FR", "es-MX", "en"]));
        Assert.AreEqual("es", text.Match(["es-AR"]), "another region of a language the game has");
        Assert.AreEqual("en", text.Match(["en_US"]));
        Assert.IsNull(text.Match(["fr", "de"]));

        var regional = new Localizer();
        regional.Add("pt-BR", new Dictionary<string, string> { ["a"] = "b" });
        Assert.AreEqual("pt-BR", regional.Match(["pt"]), "the only region of the language asked for");
    }

    [TestMethod]
    public void AMissingKeyShowsItsNameAndIsReportedOnce()
    {
        var text = Tables();
        var reports = new List<string>();
        text.Report = reports.Add;
        text.Language = "es";
        Assert.AreEqual("menu.options", text["menu.options"]);
        Assert.AreEqual("menu.options", text["menu.options"]);
        Assert.AreEqual(1, reports.Count);
        StringAssert.Contains(reports[0], "menu.options");
        StringAssert.Contains(reports[0], "es");
        CollectionAssert.AreEqual(new[] { "menu.options" }, text.MissingKeys.ToArray());

        // A key only the fallback language has is shown from it, and said once too.
        Assert.AreEqual("English only", text["only.english"]);
        Assert.AreEqual(2, reports.Count);
        StringAssert.Contains(reports[1], "only.english");
    }

    [TestMethod]
    public void ABrokenMessageShowsItsTextAndIsReported()
    {
        var text = new Localizer();
        var reports = new List<string>();
        text.Report = reports.Add;
        text.Add("en", new Dictionary<string, string> { ["bad"] = "{n, plural, one {a}}" });
        Assert.AreEqual("{n, plural, one {a}}", text.Format("bad", ("n", 1)));
        Assert.AreEqual(1, reports.Count);
        StringAssert.Contains(reports[0], "bad");
    }

    [TestMethod]
    public void LanguagesCarryTheirNameDirectionAndFonts()
    {
        var text = new Localizer();
        text.Add(StringTables.ParseJson("en", """{ "@name": "English", "hello": "Hello" }"""));
        text.Add(StringTables.ParseJson("ar", """{ "@name": "العربية", "@fonts": ["fonts/a.ttf", "fonts/b.ttf"], "hello": "مرحبا" }"""));
        text.Add(StringTables.ParseJson("ja", """{ "hello": "こんにちは" }"""));
        CollectionAssert.AreEqual(new[] { "en", "ar", "ja" }, text.Languages.Select(l => l.Code).ToArray());
        Assert.AreEqual("English", text.Current.Name);
        Assert.IsFalse(text.Current.RightToLeft);
        text.Language = "ar";
        Assert.AreEqual("العربية", text.Current.Name);
        Assert.IsTrue(text.Current.RightToLeft, "Arabic reads right to left without saying so");
        CollectionAssert.AreEqual(new[] { "fonts/a.ttf", "fonts/b.ttf" }, text.Current.Fonts.ToArray());
        Assert.AreEqual("ja", text.Languages[2].Name, "a language without a name is known by its code");
    }

    [TestMethod]
    public void TheCharactersOfALanguageAreListedForPreparingFonts()
    {
        var text = new Localizer();
        text.Add("ja", new Dictionary<string, string> { ["a"] = "はい", ["b"] = "いいえ {n, plural, other {# 個}}" });
        Assert.AreEqual("いえは個", text.Characters("ja", includeAscii: false));
        StringAssert.Contains(text.Characters("ja"), "0123456789");
    }

    [TestMethod]
    public void JsonTablesNestKeysAndRefuseWhatIsNotText()
    {
        var table = StringTables.ParseJson("es", """
            {
              // comments and trailing commas are fine
              "@name": "Español",
              "@direction": "ltr",
              "menu": { "play": "Jugar", "options": { "sound": "Sonido" } },
              "title": "Jardín",
            }
            """);
        Assert.AreEqual("es", table.Language);
        Assert.AreEqual("Español", table.Name);
        Assert.AreEqual(false, table.RightToLeft);
        Assert.AreEqual("Jugar", table.Entries["menu.play"]);
        Assert.AreEqual("Sonido", table.Entries["menu.options.sound"]);
        Assert.AreEqual(3, table.Entries.Count, "names starting with @ describe the language");

        var number = Assert.ThrowsExactly<FormatException>(() => StringTables.ParseJson("en", "{\n \"lives\": 3\n}"));
        StringAssert.Contains(number.Message, "lives");
        StringAssert.Contains(number.Message, "line 2");
        var twice = Assert.ThrowsExactly<FormatException>(() => StringTables.ParseJson("en", """{ "a": "x", "a": "y" }"""));
        StringAssert.Contains(twice.Message, "twice");
        Assert.ThrowsExactly<FormatException>(() => StringTables.ParseJson("en", "[1, 2]"));
        Assert.ThrowsExactly<FormatException>(() => StringTables.ParseJson("en", "{ \"a\": "));
    }

    [TestMethod]
    public void CsvTablesHoldALanguagePerColumn()
    {
        var tables = StringTables.ParseCsv(""""
            key,en,es,ar
            @name,English,Español,العربية
            @fonts,,,fonts/a.ttf;fonts/b.ttf
            # a comment row
            menu.play,Play,Jugar,العب
            quote,"She said ""hi"", twice","Dijo ""hola""",
            lines,"one
            two",uno,
            """");
        Assert.AreEqual(3, tables.Count);
        var (en, es, ar) = (tables[0], tables[1], tables[2]);
        Assert.AreEqual("en", en.Language);
        Assert.AreEqual("Español", es.Name);
        CollectionAssert.AreEqual(new[] { "fonts/a.ttf", "fonts/b.ttf" }, ar.Fonts.ToArray());
        Assert.AreEqual("She said \"hi\", twice", en.Entries["quote"]);
        Assert.AreEqual("one\ntwo", en.Entries["lines"]);
        Assert.AreEqual("العب", ar.Entries["menu.play"]);
        Assert.IsFalse(ar.Entries.ContainsKey("quote"), "an empty cell is a text the language does not have");
        Assert.AreEqual(3, en.Entries.Count);

        var error = Assert.ThrowsExactly<FormatException>(() => StringTables.ParseCsv("key,en\na,b,c\n"));
        StringAssert.Contains(error.Message, "line 2");
        Assert.ThrowsExactly<FormatException>(() => StringTables.ParseCsv("key,en\na,\"open\n"));
        Assert.ThrowsExactly<FormatException>(() => StringTables.ParseCsv("key\na\n"));
    }

    [TestMethod]
    public void FileNamesSayWhatATableIs()
    {
        Assert.AreEqual("en", StringTables.LanguageOf("text/en.strings.json"));
        Assert.AreEqual("pt-BR", StringTables.LanguageOf("pt-BR.strings.json"));
        Assert.IsNull(StringTables.LanguageOf("atlases/hero.json"));
        Assert.IsTrue(StringTables.IsTable("text/all.strings.csv"));
        Assert.IsTrue(StringTables.IsTable("text/en.strings.json"));
        Assert.IsFalse(StringTables.IsTable("data/levels.csv"));
    }
}

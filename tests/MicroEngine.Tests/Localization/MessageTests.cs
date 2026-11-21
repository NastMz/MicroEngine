using MicroEngine.Localization;

namespace MicroEngine.Tests.Localization;

/// <summary>Messages: arguments, plurals per language, selections, quoting, and what is left as written.</summary>
[TestClass]
public sealed class MessageTests
{
    private static string Format(string pattern, string language, params TextArgument[] arguments) =>
        Message.Parse(pattern).Format(language, arguments);

    [TestMethod]
    public void PlainTextIsItself()
    {
        var message = Message.Parse("Start game");
        Assert.IsTrue(message.IsPlain);
        Assert.AreEqual("Start game", message.Format("en", []));
        Assert.AreEqual(0, message.Arguments.Count);
    }

    [TestMethod]
    public void ArgumentsAreReplacedByNameWhateverTheirOrder()
    {
        Assert.AreEqual("Ana found 3 keys", Format("{who} found {count} keys", "en", ("count", 3), ("who", "Ana")));
        Assert.AreEqual("Half: 0.5, whole: 2, long: 4000000000", Format("Half: {a}, whole: {b}, long: {c}", "en", ("a", 0.5), ("b", 2.0f), ("c", 4_000_000_000L)));
    }

    [TestMethod]
    public void AnArgumentThatWasNotGivenStaysAsWritten()
    {
        // What the prompts replace afterwards: "Press {attack}".
        Assert.AreEqual("Press {attack} to hit 2 times", Format("Press {attack} to hit {n} times", "en", ("n", 2)));
        Assert.AreEqual("{n, plural, one {# gem} other {# gems}} left", Format("{n, plural, one {# gem} other {# gems}} left", "en"));
        Assert.AreEqual("{{literal}}", Format("{{literal}}", "en"), "doubled braces reach the prompts unchanged");
    }

    [TestMethod]
    public void PluralsFollowTheLanguage()
    {
        const string english = "{count, plural, =0 {No gems} one {# gem} other {# gems}}";
        Assert.AreEqual("No gems", Format(english, "en", ("count", 0)));
        Assert.AreEqual("1 gem", Format(english, "en", ("count", 1)));
        Assert.AreEqual("12 gems", Format(english, "en", ("count", 12)));

        const string russian = "{count, plural, one {# самоцвет} few {# самоцвета} many {# самоцветов} other {# самоцвета}}";
        Assert.AreEqual("1 самоцвет", Format(russian, "ru", ("count", 1)));
        Assert.AreEqual("3 самоцвета", Format(russian, "ru", ("count", 3)));
        Assert.AreEqual("5 самоцветов", Format(russian, "ru", ("count", 5)));
        Assert.AreEqual("21 самоцвет", Format(russian, "ru", ("count", 21)));
        Assert.AreEqual("1.5 самоцвета", Format(russian, "ru", ("count", 1.5)));

        Assert.AreEqual("宝石 7 個", Format("{count, plural, other {宝石 # 個}}", "ja", ("count", 7)));
    }

    [TestMethod]
    public void ACategoryWithoutABranchUsesOther()
    {
        // Polish needs "few" for 2; a message that forgot it still shows something.
        Assert.AreEqual("2 x", Format("{n, plural, one {# a} other {# x}}", "pl", ("n", 2)));
    }

    [TestMethod]
    public void SelectChoosesByTextAndNestsInsidePlurals()
    {
        const string pattern = "{who, select, hero {The hero} other {{who}}} took {n, plural, one {{item, select, key {the key} other {one thing}}} other {# things}}";
        Assert.AreEqual("The hero took the key", Format(pattern, "en", ("who", "hero"), ("n", 1), ("item", "key")));
        Assert.AreEqual("Ana took 4 things", Format(pattern, "en", ("who", "Ana"), ("n", 4), ("item", "key")));
    }

    [TestMethod]
    public void ApostrophesQuoteBracesAndStayApostrophesElsewhere()
    {
        Assert.AreEqual("Don't press {here}", Format("Don't press '{here}'", "en", ("here", "x")));
        Assert.AreEqual("It's {a} #1", Format("It''s '{'a'}' #{n}", "en", ("n", 1)));
        Assert.AreEqual("# of 3", Format("{n, plural, other {'#' of #}}", "en", ("n", 3)));
    }

    [TestMethod]
    public void TheArgumentsOfAMessageAreListedForChecks()
    {
        var message = Message.Parse("{who} has {n, plural, one {# {thing}} other {# things}}, {mood, select, sad {sadly} other {}}");
        CollectionAssert.AreEqual(new[] { "who", "n", "thing", "mood" }, message.Arguments.Select(a => a.Name).ToArray());
        Assert.AreEqual(MessageArgumentKind.Plural, message.Arguments[1].Kind);
        CollectionAssert.AreEqual(new[] { "one", "other" }, message.Arguments[1].Branches.ToArray());
        Assert.AreEqual(MessageArgumentKind.Select, message.Arguments[3].Kind);
        Assert.IsFalse(message.IsPlain);
    }

    [TestMethod]
    [DataRow("{n, plural, one {a}}", "other")]
    [DataRow("{n, plural, some {a} other {b}}", "some")]
    [DataRow("{n, plural, one {a} other {b}", "closed")]
    [DataRow("{n, selectordinal, one {#st} other {#th}}", "selectordinal")]
    [DataRow("{d, date, short}", "date")]
    [DataRow("{n, plural, offset:1 one {a} other {b}}", "offset")]
    [DataRow("{n, plural, one {a} one {b} other {c}}", "twice")]
    public void MistakesAreRefusedNamingWhatIsWrong(string pattern, string expected)
    {
        Assert.IsFalse(Message.TryParse(pattern, out var message, out var error));
        Assert.IsNull(message);
        StringAssert.Contains(error, expected);
        Assert.ThrowsExactly<FormatException>(() => Message.Parse(pattern));
    }

    [TestMethod]
    public void FormattingAPlainOrPreparedMessageAllocatesOnlyItsResult()
    {
        var plain = Message.Parse("Start game");
        Assert.AreSame(plain.Format("en", []), plain.Format("en", []), "a plain message is one string");
    }
}

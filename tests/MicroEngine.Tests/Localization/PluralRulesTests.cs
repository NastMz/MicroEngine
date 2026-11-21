using MicroEngine.Localization;
using static MicroEngine.Localization.PluralCategory;

namespace MicroEngine.Tests.Localization;

/// <summary>The plural category of a number in each language family, with no culture data from the runtime.</summary>
[TestClass]
public sealed class PluralRulesTests
{
    private static void Expect(string language, PluralCategory category, params double[] numbers)
    {
        foreach (double n in numbers)
            Assert.AreEqual(category, PluralRules.Select(language, n), $"{language}: {n}");
    }

    [TestMethod]
    public void LanguagesWithOneFormForOne()
    {
        foreach (var language in new[] { "en", "de", "es", "it", "nl", "sv", "tr", "el", "hu", "fi", "en-GB", "es_MX", "pt-PT" })
        {
            Expect(language, One, 1);
            Expect(language, Other, 0, 2, 5, 11, 21, 100, 1.5);
        }
    }

    [TestMethod]
    public void LanguagesWithoutPlurals()
    {
        foreach (var language in new[] { "ja", "zh", "zh-Hans", "ko", "vi", "th", "id" })
            Expect(language, Other, 0, 1, 2, 5, 100, 1.5);
    }

    [TestMethod]
    public void FrenchAndPortugueseCountZeroAsOne()
    {
        Expect("fr", One, 0, 1, 1.5);
        Expect("fr", Other, 2, 10, 100);
        Expect("pt", One, 0, 1);
        Expect("pt-BR", Other, 2, 7);
        Expect("hi", One, 0, 1, 0.5);
        Expect("hi", Other, 2, 1.5);
    }

    [TestMethod]
    public void EastSlavicLanguages()
    {
        foreach (var language in new[] { "ru", "uk" })
        {
            Expect(language, One, 1, 21, 31, 101, 1001);
            Expect(language, Few, 2, 3, 4, 22, 24, 102);
            Expect(language, Many, 0, 5, 10, 11, 12, 14, 19, 20, 25, 100, 111, 112);
            Expect(language, Other, 0.5, 1.5);
        }
    }

    [TestMethod]
    public void Polish()
    {
        Expect("pl", One, 1);
        Expect("pl", Few, 2, 3, 4, 22, 23, 24, 102);
        Expect("pl", Many, 0, 5, 10, 11, 12, 13, 14, 21, 25, 100, 112);
        Expect("pl", Other, 0.5, 1.5);
    }

    [TestMethod]
    public void CzechAndSlovak()
    {
        Expect("cs", One, 1);
        Expect("cs", Few, 2, 3, 4);
        Expect("cs", Many, 0.5, 1.5);
        Expect("sk", Other, 0, 5, 10, 22, 100);
    }

    [TestMethod]
    public void SerbianAndCroatian()
    {
        Expect("sr", One, 1, 21, 101);
        Expect("hr", Few, 2, 3, 4, 22);
        Expect("sr", Other, 0, 5, 11, 12, 14, 25);
    }

    [TestMethod]
    public void Arabic()
    {
        Expect("ar", Zero, 0);
        Expect("ar", One, 1);
        Expect("ar", Two, 2);
        Expect("ar", Few, 3, 10, 103, 110);
        Expect("ar", Many, 11, 26, 99, 111, 199);
        Expect("ar", Other, 100, 101, 102, 200, 1000);
        Expect("ar-EG", Two, 2);
    }

    [TestMethod]
    public void HebrewRomanianLithuanianLatvianAndSlovenian()
    {
        Expect("he", One, 1);
        Expect("he", Two, 2);
        Expect("he", Other, 0, 3, 10, 20, 100);

        Expect("ro", One, 1);
        Expect("ro", Few, 0, 2, 19, 101, 119);
        Expect("ro", Other, 20, 100, 120);

        Expect("lt", One, 1, 21, 31);
        Expect("lt", Few, 2, 9, 22, 29);
        Expect("lt", Many, 0.5, 1.5);
        Expect("lt", Other, 0, 10, 11, 12, 19, 20, 30);

        Expect("lv", Zero, 0, 10, 11, 19, 20, 30);
        Expect("lv", One, 1, 21, 31);
        Expect("lv", Other, 2, 9, 22);

        Expect("sl", One, 1, 101);
        Expect("sl", Two, 2, 102);
        Expect("sl", Few, 3, 4, 103);
        Expect("sl", Other, 0, 5, 100);
    }

    [TestMethod]
    public void TheCategoriesOfALanguageAreListedForChecks()
    {
        CollectionAssert.AreEqual(new[] { One, Other }, PluralRules.Categories("en").ToArray());
        CollectionAssert.AreEqual(new[] { Other }, PluralRules.Categories("ja").ToArray());
        CollectionAssert.AreEqual(new[] { One, Few, Many, Other }, PluralRules.Categories("ru").ToArray());
        CollectionAssert.AreEqual(new[] { Zero, One, Two, Few, Many, Other }, PluralRules.Categories("ar").ToArray());
    }

    [TestMethod]
    public void AnUnknownLanguageCountsLikeEnglishUntilAGameRegistersItsRule()
    {
        Assert.IsFalse(PluralRules.Knows("tlh"));
        Assert.IsTrue(PluralRules.Knows("en-AU"));
        Expect("tlh", One, 1);
        Expect("tlh", Other, 0, 2);

        PluralRules.Register("x-test", n => n == 0 ? Zero : n < 3 ? Few : Other, Zero, Few, Other);
        Assert.IsTrue(PluralRules.Knows("x-test"));
        Expect("x-test", Zero, 0);
        Expect("x-test", Few, 1, 2);
        Expect("x-test", Other, 3);
        CollectionAssert.AreEqual(new[] { Zero, Few, Other }, PluralRules.Categories("x-test").ToArray());
    }
}

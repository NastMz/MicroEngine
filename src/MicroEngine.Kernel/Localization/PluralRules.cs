using System.Collections.Concurrent;

namespace MicroEngine.Localization;

/// <summary>
/// The form a language uses for a number: English has two (<see cref="One"/> for 1, <see cref="Other"/> for the rest),
/// Russian four, Arabic six, Japanese one. A plural message gives a text per category
/// (<c>{count, plural, one {# gem} other {# gems}}</c>).
/// </summary>
public enum PluralCategory
{
    /// <summary>The form for zero, in languages that have one (Arabic, Latvian).</summary>
    Zero,
    /// <summary>The singular form.</summary>
    One,
    /// <summary>The form for two, in languages that have one (Arabic, Hebrew, Slovenian).</summary>
    Two,
    /// <summary>The form for a few (2 to 4 in most Slavic languages).</summary>
    Few,
    /// <summary>The form for many (5 and more in most Slavic languages).</summary>
    Many,
    /// <summary>The general form; every language has it and every plural message must give it.</summary>
    Other,
}

/// <summary>
/// Which plural form a number takes in a language, from rules built into the engine: nothing is read from the
/// operating system or the runtime's culture data, so trimmed, NativeAOT, phone and browser builds count alike.
/// </summary>
/// <remarks>
/// For whole numbers the rules are the cardinal rules of Unicode's CLDR; fractions are approximated (in most languages
/// they take the general form). Built in: Afrikaans, Albanian, Amharic, Arabic, Azerbaijani, Basque, Belarusian,
/// Bengali, Bosnian, Bulgarian, Burmese, Cantonese, Catalan, Chinese, Croatian, Czech, Danish, Dutch, English,
/// Estonian, Finnish, French, Galician, Georgian, German, Greek, Gujarati, Hebrew, Hindi, Hungarian, Indonesian,
/// Italian, Japanese, Kannada, Kazakh, Khmer, Korean, Lao, Latvian, Lithuanian, Malay, Malayalam, Mongolian, Nepali,
/// Norwegian, Persian, Polish, Portuguese, Romanian, Russian, Serbian, Slovak, Slovenian, Spanish, Swahili, Swedish,
/// Tamil, Telugu, Thai, Turkish, Ukrainian, Urdu, Uzbek, Vietnamese and Zulu. French, Italian, Portuguese, Spanish and
/// Catalan leave out CLDR's <see cref="PluralCategory.Many"/> for whole millions. Any other language counts like
/// English until the game gives its rule with <see cref="Register"/>.
/// </remarks>
public static class PluralRules
{
    private sealed record Rule(Func<double, PluralCategory> Select, PluralCategory[] Categories);

    private static readonly PluralCategory[] OnlyOther = [PluralCategory.Other];
    private static readonly PluralCategory[] OneOther = [PluralCategory.One, PluralCategory.Other];
    private static readonly PluralCategory[] OneFewOther = [PluralCategory.One, PluralCategory.Few, PluralCategory.Other];
    private static readonly PluralCategory[] OneFewManyOther = [PluralCategory.One, PluralCategory.Few, PluralCategory.Many, PluralCategory.Other];

    private static readonly Rule English = new(static n => n == 1 ? PluralCategory.One : PluralCategory.Other, OneOther);
    private static readonly ConcurrentDictionary<string, Rule> Rules = new(StringComparer.OrdinalIgnoreCase);

    static PluralRules()
    {
        Add(new Rule(static _ => PluralCategory.Other, OnlyOther), "ja", "zh", "ko", "vi", "th", "id", "ms", "lo", "my", "km", "yue");
        Add(English, "en", "de", "nl", "sv", "da", "nb", "nn", "no", "fi", "et", "el", "hu", "tr", "bg", "es", "it", "ca", "eu", "gl",
            "sq", "af", "sw", "ur", "ta", "te", "ml", "ka", "kk", "az", "uz", "mn", "ne", "pt-PT");
        // 0 and 1, and the fractions below 2.
        Add(new Rule(static n => n < 2 ? PluralCategory.One : PluralCategory.Other, OneOther), "fr", "pt");
        // 0 to 1, fractions included.
        Add(new Rule(static n => n <= 1 ? PluralCategory.One : PluralCategory.Other, OneOther), "hi", "bn", "gu", "kn", "fa", "am", "zu");
        Add(new Rule(static n =>
        {
            if (!Whole(n, out long i)) return PluralCategory.Other;
            long d = i % 10, h = i % 100;
            if (d == 1 && h != 11) return PluralCategory.One;
            if (d is >= 2 and <= 4 && h is not (>= 12 and <= 14)) return PluralCategory.Few;
            return PluralCategory.Many;
        }, OneFewManyOther), "ru", "uk", "be");
        Add(new Rule(static n =>
        {
            if (!Whole(n, out long i)) return PluralCategory.Other;
            long d = i % 10, h = i % 100;
            if (d == 1 && h != 11) return PluralCategory.One;
            if (d is >= 2 and <= 4 && h is not (>= 12 and <= 14)) return PluralCategory.Few;
            return PluralCategory.Other;
        }, OneFewOther), "sr", "hr", "bs");
        Add(new Rule(static n =>
        {
            if (!Whole(n, out long i)) return PluralCategory.Other;
            if (i == 1) return PluralCategory.One;
            long d = i % 10, h = i % 100;
            if (d is >= 2 and <= 4 && h is not (>= 12 and <= 14)) return PluralCategory.Few;
            return PluralCategory.Many;
        }, OneFewManyOther), "pl");
        Add(new Rule(static n =>
        {
            if (!Whole(n, out long i)) return PluralCategory.Many;
            return i == 1 ? PluralCategory.One : i is >= 2 and <= 4 ? PluralCategory.Few : PluralCategory.Other;
        }, OneFewManyOther), "cs", "sk");
        Add(new Rule(static n =>
        {
            if (!Whole(n, out long i)) return PluralCategory.Other;
            if (i == 0) return PluralCategory.Zero;
            if (i == 1) return PluralCategory.One;
            if (i == 2) return PluralCategory.Two;
            long h = i % 100;
            return h is >= 3 and <= 10 ? PluralCategory.Few : h >= 11 ? PluralCategory.Many : PluralCategory.Other;
        }, [PluralCategory.Zero, PluralCategory.One, PluralCategory.Two, PluralCategory.Few, PluralCategory.Many, PluralCategory.Other]), "ar");
        Add(new Rule(static n => n == 1 ? PluralCategory.One : n == 2 ? PluralCategory.Two : PluralCategory.Other,
            [PluralCategory.One, PluralCategory.Two, PluralCategory.Other]), "he", "iw");
        Add(new Rule(static n =>
        {
            if (!Whole(n, out long i)) return PluralCategory.Few;
            if (i == 1) return PluralCategory.One;
            return i == 0 || i % 100 is >= 1 and <= 19 ? PluralCategory.Few : PluralCategory.Other;
        }, OneFewOther), "ro", "mo");
        Add(new Rule(static n =>
        {
            if (!Whole(n, out long i)) return PluralCategory.Many;
            long d = i % 10, h = i % 100;
            bool teens = h is >= 11 and <= 19;
            if (d == 1 && !teens) return PluralCategory.One;
            return d >= 2 && !teens ? PluralCategory.Few : PluralCategory.Other;
        }, OneFewManyOther), "lt");
        Add(new Rule(static n =>
        {
            if (!Whole(n, out long i)) return PluralCategory.Other;
            long d = i % 10, h = i % 100;
            if (d == 0 || h is >= 11 and <= 19) return PluralCategory.Zero;
            return d == 1 ? PluralCategory.One : PluralCategory.Other;
        }, [PluralCategory.Zero, PluralCategory.One, PluralCategory.Other]), "lv");
        Add(new Rule(static n =>
        {
            if (!Whole(n, out long i)) return PluralCategory.Few;
            return (i % 100) switch { 1 => PluralCategory.One, 2 => PluralCategory.Two, 3 or 4 => PluralCategory.Few, _ => PluralCategory.Other };
        }, [PluralCategory.One, PluralCategory.Two, PluralCategory.Few, PluralCategory.Other]), "sl");
    }

    private static void Add(Rule rule, params string[] languages)
    {
        foreach (var language in languages) Rules[language] = rule;
    }

    /// <summary>Whether <paramref name="n"/> (taken without its sign) is a whole number, and which.</summary>
    private static bool Whole(double n, out long whole)
    {
        whole = (long)n;
        return whole == n;
    }

    /// <summary>The rule of a tag: its own (<c>pt-PT</c>), else its language's (<c>pt</c>).</summary>
    private static Rule? Find(string language)
    {
        string tag = LanguageTag.Normalize(language);
        while (true)
        {
            if (Rules.TryGetValue(tag, out var rule)) return rule;
            if (LanguageTag.Parent(tag) is not { } parent) return null;
            tag = parent;
        }
    }

    /// <summary>The plural form <paramref name="number"/> takes in <paramref name="language"/> (a tag such as <c>en</c>, <c>pt-BR</c>, <c>zh-Hans</c>); the sign is ignored.</summary>
    public static PluralCategory Select(string language, double number)
    {
        ArgumentNullException.ThrowIfNull(language);
        if (double.IsNaN(number) || double.IsInfinity(number)) return PluralCategory.Other;
        return (Find(language) ?? English).Select(Math.Abs(number));
    }

    /// <summary>Whether the engine or the game has a rule for <paramref name="language"/>; without one it counts like English.</summary>
    public static bool Knows(string language)
    {
        ArgumentNullException.ThrowIfNull(language);
        return Find(language) is not null;
    }

    /// <summary>The forms <paramref name="language"/> uses, in the order of <see cref="PluralCategory"/>: what a plural message of that language should give.</summary>
    public static IReadOnlyList<PluralCategory> Categories(string language)
    {
        ArgumentNullException.ThrowIfNull(language);
        return (Find(language) ?? English).Categories;
    }

    /// <summary>
    /// Gives the rule of a language the engine does not know, or replaces one: <paramref name="rule"/> receives the
    /// number without its sign. <paramref name="categories"/> are the forms it can return; <see cref="PluralCategory.Other"/>
    /// is always among them.
    /// </summary>
    public static void Register(string language, Func<double, PluralCategory> rule, params PluralCategory[] categories)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        ArgumentNullException.ThrowIfNull(rule);
        var all = categories.Append(PluralCategory.Other).Distinct().Order().ToArray();
        Rules[LanguageTag.Normalize(language)] = new Rule(rule, all);
    }
}

/// <summary>Language tags as tables and players write them: <c>en</c>, <c>pt-BR</c>, <c>es_MX</c>, <c>zh-Hans</c>.</summary>
public static class LanguageTag
{
    /// <summary>The tag with <c>-</c> between its parts and surrounding space removed (<c>es_MX</c> is <c>es-MX</c>); letter case is kept.</summary>
    public static string Normalize(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        tag = tag.Trim();
        // A system's locale may carry an encoding or a variant: "es_ES.UTF-8", "ca@valencia".
        int cut = tag.IndexOfAny(['.', '@']);
        if (cut >= 0) tag = tag[..cut];
        return tag.Replace('_', '-');
    }

    /// <summary>The tag without its last part (<c>zh-Hans-CN</c>, then <c>zh-Hans</c>, then <c>zh</c>), or null for a bare language.</summary>
    public static string? Parent(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        int dash = tag.LastIndexOf('-');
        return dash > 0 ? tag[..dash] : null;
    }

    /// <summary>The language of a tag: <c>pt</c> for <c>pt-BR</c>.</summary>
    public static string Primary(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        int dash = tag.IndexOf('-');
        return dash > 0 ? tag[..dash] : tag;
    }

    /// <summary>Whether two tags name the same language and parts, whatever their letter case or separator.</summary>
    public static bool Same(string a, string b) => string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether a language is written right to left: Arabic, Hebrew, Persian, Urdu, Pashto, Sindhi, Uyghur, Yiddish, Dhivehi and Sorani Kurdish.</summary>
    public static bool IsRightToLeft(string tag) =>
        Primary(Normalize(tag)).ToLowerInvariant() is "ar" or "he" or "iw" or "fa" or "ur" or "ps" or "sd" or "ug" or "yi" or "dv" or "ckb";

    /// <summary>Whether <paramref name="text"/> has the shape of a language tag: two or three letters, then parts of letters or digits.</summary>
    public static bool IsValid(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var parts = text.Split('-', '_');
        if (parts[0].Length is < 2 or > 3 || !parts[0].All(char.IsAsciiLetter))
            // Private tags ("x-pirate") are languages of a game's own.
            if (!(parts[0] is "x" or "X" && parts.Length > 1)) return false;
        return parts.Skip(1).All(p => p.Length is >= 1 and <= 8 && p.All(char.IsAsciiLetterOrDigit));
    }
}

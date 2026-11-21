namespace MicroEngine.Localization;

/// <summary>Something wrong with a game's string tables, found by <see cref="StringTableChecks"/>.</summary>
/// <param name="Error">Whether a player would see it (a key, a raw placeholder, the wrong language); false for what is allowed but worth a look.</param>
/// <param name="Language">The language whose table has the problem.</param>
/// <param name="Key">The key, or null when the problem is the language's as a whole.</param>
/// <param name="Message">What is wrong and what to do.</param>
public sealed record LocalizationIssue(bool Error, string Language, string? Key, string Message)
{
    /// <inheritdoc/>
    public override string ToString() => $"{(Error ? "error" : "warning")}: {Language}{(Key is null ? "" : $": '{Key}'")}: {Message}";
}

/// <summary>
/// Compares the string tables of a game's languages, as <c>microengine content check</c> does: a key a language
/// lacks, a message that cannot be read, a value one translation names and the others do not, a plural without the
/// forms its language uses. A game can run it in a test of its own over the tables it loads.
/// </summary>
public static class StringTableChecks
{
    /// <summary>
    /// Checks <paramref name="tables"/> (several tables of one language add up, as when they are loaded).
    /// Values are compared with the text of <paramref name="referenceLanguage"/> (<c>en</c> by default), or of the
    /// first language that has the key.
    /// </summary>
    public static IReadOnlyList<LocalizationIssue> Check(IEnumerable<StringTableData> tables, string referenceLanguage = "en")
    {
        ArgumentNullException.ThrowIfNull(tables);
        ArgumentNullException.ThrowIfNull(referenceLanguage);
        var languages = new List<(string Code, Dictionary<string, string> Entries)>();
        foreach (var table in tables)
        {
            int at = languages.FindIndex(l => LanguageTag.Same(l.Code, table.Language));
            if (at < 0) { languages.Add((table.Language, new Dictionary<string, string>(StringComparer.Ordinal))); at = languages.Count - 1; }
            foreach (var (key, text) in table.Entries) languages[at].Entries[key] = text;
        }
        var issues = new List<LocalizationIssue>();
        var keys = new SortedSet<string>(languages.SelectMany(l => l.Entries.Keys), StringComparer.Ordinal);
        var messages = new Dictionary<(string Language, string Key), Message>();

        // Messages that cannot be read.
        foreach (var (code, entries) in languages)
            foreach (var (key, text) in entries.OrderBy(e => e.Key, StringComparer.Ordinal))
            {
                if (Message.TryParse(text, out var message, out var error)) messages[(code, key)] = message!;
                else issues.Add(new LocalizationIssue(true, code, key, $"the text cannot be read and is shown as written: {error}"));
            }

        // Keys a language lacks: its parents' texts count as its own.
        foreach (var (code, entries) in languages)
            foreach (var key in keys)
            {
                // The name of a key or control ("prompt.Space") is translated only where a language has its own word.
                if (key.StartsWith(StringTables.PromptPrefix, StringComparison.Ordinal) || Has(code, key)) continue;
                var others = languages.Where(l => l.Entries.ContainsKey(key)).Select(l => l.Code).ToList();
                issues.Add(new LocalizationIssue(true, code, key,
                    $"no text in '{code}', though {string.Join(", ", others.Select(o => $"'{o}'"))} {(others.Count == 1 ? "has" : "have")} it: the player sees another language's text. Add it to the table of '{code}'."));
            }

        // The values a text names, against the reference language's.
        foreach (var key in keys)
        {
            var reference = languages.FirstOrDefault(l => LanguageTag.Same(l.Code, referenceLanguage) && messages.ContainsKey((l.Code, key)));
            if (reference.Code is null) reference = languages.FirstOrDefault(l => messages.ContainsKey((l.Code, key)));
            if (reference.Code is null) continue;
            var expected = messages[(reference.Code, key)].Arguments.Select(a => a.Name).ToList();
            foreach (var (code, _) in languages)
            {
                if (code == reference.Code || !messages.TryGetValue((code, key), out var message)) continue;
                var named = message.Arguments.Select(a => a.Name).ToList();
                foreach (var extra in named.Except(expected))
                    issues.Add(new LocalizationIssue(true, code, key, $"the text names {{{extra}}}, which the text of '{reference.Code}' does not ({Names(expected)}): the player would see {{{extra}}} as written. Correct the name."));
                foreach (var unused in expected.Except(named))
                    issues.Add(new LocalizationIssue(false, code, key, $"the text does not use {{{unused}}}, which the text of '{reference.Code}' shows. Check the translation."));
            }
        }

        // Plurals: the forms each language uses.
        foreach (var (code, _) in languages)
        {
            bool plurals = false;
            foreach (var key in keys)
            {
                if (!messages.TryGetValue((code, key), out var message)) continue;
                foreach (var argument in message.Arguments)
                {
                    if (argument.Kind != MessageArgumentKind.Plural) continue;
                    plurals = true;
                    if (!PluralRules.Knows(code)) continue;
                    var used = PluralRules.Categories(code);
                    var lacking = used.Where(c => c != PluralCategory.Other && !Gives(code, argument.Branches, c)).Select(Word).ToList();
                    if (lacking.Count > 0)
                        issues.Add(new LocalizationIssue(false, code, key, $"the plural of {{{argument.Name}}} gives no text for {string.Join(", ", lacking)}, which '{code}' uses: those numbers take the 'other' text. Add {(lacking.Count == 1 ? "that branch" : "those branches")}."));
                    var never = argument.Branches.Where(b => Category(b) is { } c && !used.Contains(c)).ToList();
                    if (never.Count > 0)
                        issues.Add(new LocalizationIssue(false, code, key, $"the plural of {{{argument.Name}}} has a branch for {string.Join(", ", never)}, which '{code}' never chooses (it uses {string.Join(", ", used.Select(Word))}). Remove it, or use an exact number such as =2."));
                }
            }
            if (plurals && !PluralRules.Knows(code))
                issues.Add(new LocalizationIssue(false, code, null, $"the engine has no plural rule for '{code}': its plurals choose as English does (one for 1, other for the rest). Give the game its rule with PluralRules.Register."));
        }
        return issues;

        bool Has(string code, string key)
        {
            for (string? tag = code; tag is not null; tag = LanguageTag.Parent(tag))
                if (languages.FirstOrDefault(l => LanguageTag.Same(l.Code, tag)).Entries is { } entries && entries.ContainsKey(key)) return true;
            return false;
        }
    }

    private static string Names(List<string> names) => names.Count == 0 ? "it names none" : "it names " + string.Join(", ", names.Select(n => $"{{{n}}}"));

    private static string Word(PluralCategory category) => category.ToString().ToLowerInvariant();

    private static PluralCategory? Category(string branch) => branch switch
    {
        "zero" => PluralCategory.Zero,
        "one" => PluralCategory.One,
        "two" => PluralCategory.Two,
        "few" => PluralCategory.Few,
        "many" => PluralCategory.Many,
        _ => null,
    };

    /// <summary>Whether a plural's branches give a text for a form: its own branch, or an exact branch for every whole number that takes it.</summary>
    private static bool Gives(string language, IReadOnlyList<string> branches, PluralCategory category)
    {
        if (branches.Contains(Word(category))) return true;
        // A form only fractions take (Czech "many") is not asked of a text that counts whole things.
        for (int n = 0; n <= 200; n++)
            if (PluralRules.Select(language, n) == category && !branches.Contains("=" + n)) return false;
        return true;
    }
}

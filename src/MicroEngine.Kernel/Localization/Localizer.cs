using System.Text;

namespace MicroEngine.Localization;

/// <summary>A language a game has text in.</summary>
/// <param name="Code">The language tag of its table (<c>es</c>, <c>pt-BR</c>).</param>
/// <param name="Name">Its own name for a language menu ("Español"); the code when its table gives none.</param>
/// <param name="RightToLeft">Whether it reads right to left.</param>
/// <param name="Fonts">The font files its table names, the main face first; empty when it names none.</param>
public sealed record LanguageInfo(string Code, string Name, bool RightToLeft, IReadOnlyList<string> Fonts);

/// <summary>
/// The texts of a game in each of its languages, and the language in use. Text is asked for by key
/// (<c>text["menu.play"]</c>, <c>text.Format("hud.gems", ("count", 3))</c>) and comes from the table of
/// <see cref="Language"/>, then from its parent language (<c>es</c> for <c>es-MX</c>), then from
/// <see cref="FallbackLanguage"/>. Changing the language raises <see cref="Changed"/>; widgets that hold a
/// <see cref="LocalizedText"/> follow it by themselves. Games get one in <c>Game.Localization</c>, which also loads
/// tables from content files.
/// </summary>
public class Localizer
{
    private sealed class Entry(string pattern)
    {
        public readonly string Pattern = pattern;
        public Message? Message;
        public bool Broken;
    }

    private sealed class Table(string code)
    {
        public readonly string Code = code;
        public readonly Dictionary<string, Entry> Entries = new(StringComparer.Ordinal);
        public string? Name;
        public bool? RightToLeft;
        public IReadOnlyList<string> Fonts = [];
        public LanguageInfo? Info;
    }

    private readonly List<Table> _tables = [];
    private readonly HashSet<string> _reported = new(StringComparer.Ordinal);
    private readonly List<string> _missing = [];
    private readonly StringBuilder _builder = new();
    private string? _language, _default;
    private string _fallback = "en";
    private LanguageInfo[]? _languages;
    // The tables a key is looked up in, for the language in use: its own, its parents', the fallback's.
    private Table[]? _chain;
    private int _ownTables;

    /// <summary>
    /// The language in use: a tag of <see cref="Languages"/>. Until one is chosen it is <see cref="FallbackLanguage"/>.
    /// Setting it takes the table of that tag, else of its language (<c>es</c> for <c>es-AR</c>), whatever the letter
    /// case, and raises <see cref="Changed"/> when the language is another one.
    /// </summary>
    /// <exception cref="ArgumentException">No table is for that language (tables exist, and none matches).</exception>
    public string Language
    {
        get => _language ?? _default ?? EffectiveFallback;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (!TrySetLanguage(value))
                throw new ArgumentException($"The game has no text in '{value}': its languages are {string.Join(", ", _tables.Select(t => t.Code))}. Load that language's table first, or use TrySetLanguage for a choice that may be out of date (a saved setting).", nameof(value));
        }
    }

    /// <summary>
    /// Sets <see cref="Language"/> when a table matches <paramref name="language"/>, and says whether one did: for a
    /// choice read from a save or from the system, which may name a language the game does not have. Before any table
    /// is loaded every tag is taken.
    /// </summary>
    public bool TrySetLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language)) return false;
        string? chosen = _tables.Count == 0 ? LanguageTag.Normalize(language) : Match([language]);
        if (chosen is null) return false;
        string before = Language;
        _language = chosen;
        if (!LanguageTag.Same(before, chosen)) Invalidate();
        return true;
    }

    /// <summary>Whether the game or the player chose <see cref="Language"/>; false while it is still the default.</summary>
    public bool LanguageChosen => _language is not null;

    /// <summary>
    /// The language in use until one is chosen (<see cref="LanguageChosen"/>): the table matching this tag, as
    /// <see cref="Match"/> finds it. Null, or a language the game has no text in, leaves <see cref="FallbackLanguage"/>
    /// in use. The engine sets it to the system's language when tables are loaded.
    /// </summary>
    public string? DefaultLanguage
    {
        get => _default;
        set
        {
            string before = Language;
            _default = value is null ? null : Match([value]);
            if (!LanguageTag.Same(before, Language)) Invalidate();
        }
    }

    /// <summary>
    /// The language whose text is shown when the language in use lacks a key, and the language in use until another
    /// is chosen: <c>en</c> by default. When no table is for it, the first table loaded takes its place.
    /// </summary>
    public string FallbackLanguage
    {
        get => _fallback;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (LanguageTag.Same(_fallback, value)) return;
            _fallback = LanguageTag.Normalize(value);
            Invalidate();
        }
    }

    private string EffectiveFallback => TableOf(_fallback)?.Code ?? (_tables.Count > 0 ? _tables[0].Code : _fallback);

    /// <summary>The languages the game has text in, in the order their tables were added.</summary>
    public IReadOnlyList<LanguageInfo> Languages => _languages ??= [.. _tables.Select(Info)];

    /// <summary>The language in use, with its name, direction and fonts.</summary>
    public LanguageInfo Current => TableOf(Language) is { } table ? Info(table) : new LanguageInfo(Language, Language, LanguageTag.IsRightToLeft(Language), []);

    /// <summary>Whether the language in use reads right to left.</summary>
    public bool RightToLeft => Current.RightToLeft;

    /// <summary>Whether any table was added.</summary>
    public bool HasTables => _tables.Count > 0;

    /// <summary>
    /// Changes whenever a text may have changed: another language, more tables. Code that lays text out once compares
    /// it to know when to do so again.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>Raised after the language in use or its texts changed.</summary>
    public event Action? Changed;

    /// <summary>
    /// Receives, once each, a sentence about what is wrong with the text asked for: a key no language has, a key the
    /// language in use lacks (shown from the fallback language), a message that cannot be read. The engine writes
    /// them to the log.
    /// </summary>
    public Action<string>? Report { get; set; }

    /// <summary>The keys asked for that no table of the language in use, its parents or the fallback language has, in the order they were asked.</summary>
    public IReadOnlyList<string> MissingKeys => _missing;

    private static LanguageInfo Info(Table table) =>
        table.Info ??= new LanguageInfo(table.Code, table.Name ?? table.Code, table.RightToLeft ?? LanguageTag.IsRightToLeft(table.Code), table.Fonts);

    private Table? TableOf(string language)
    {
        foreach (var table in _tables)
            if (LanguageTag.Same(table.Code, language)) return table;
        return null;
    }

    private void Invalidate()
    {
        _chain = null;
        _languages = null;
        Version++;
        OnChanged();
        Changed?.Invoke();
    }

    /// <summary>Called after the language in use or its texts changed, before <see cref="Changed"/> is raised.</summary>
    protected virtual void OnChanged() { }

    /// <summary>Adds the texts of a table read from a file (<see cref="StringTables"/>) to its language; a key given again replaces the earlier text.</summary>
    public void Add(StringTableData table)
    {
        ArgumentNullException.ThrowIfNull(table);
        var target = Target(table.Language);
        if (table.Name is not null) target.Name = table.Name;
        if (table.RightToLeft is not null) target.RightToLeft = table.RightToLeft;
        if (table.Fonts.Count > 0) target.Fonts = table.Fonts;
        target.Info = null;
        foreach (var (key, pattern) in table.Entries) target.Entries[key] = new Entry(pattern);
        Invalidate();
    }

    /// <summary>Adds texts to <paramref name="language"/>: each key and its message. A key given again replaces the earlier text.</summary>
    public void Add(string language, IEnumerable<KeyValuePair<string, string>> entries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        ArgumentNullException.ThrowIfNull(entries);
        var target = Target(language);
        foreach (var (key, pattern) in entries) target.Entries[key] = new Entry(pattern);
        Invalidate();
    }

    private Table Target(string language)
    {
        if (TableOf(language) is { } table) return table;
        table = new Table(LanguageTag.Normalize(language));
        _tables.Add(table);
        return table;
    }

    /// <summary>
    /// The language of the game that best suits a player who prefers <paramref name="preferred"/>, most wanted first
    /// (a system's language list, a saved choice): the first whose exact tag has a table, else whose language has one
    /// (<c>es</c> for <c>es-AR</c>, or the only region the game has for <c>pt</c>). Null when none matches.
    /// </summary>
    public string? Match(IEnumerable<string> preferred)
    {
        ArgumentNullException.ThrowIfNull(preferred);
        foreach (var wanted in preferred)
        {
            if (string.IsNullOrWhiteSpace(wanted)) continue;
            string? tag = LanguageTag.Normalize(wanted);
            for (string? t = tag; t is not null; t = LanguageTag.Parent(t))
                if (TableOf(t) is { } exact) return exact.Code;
            string primary = LanguageTag.Primary(tag);
            foreach (var table in _tables)
                if (string.Equals(LanguageTag.Primary(table.Code), primary, StringComparison.OrdinalIgnoreCase)) return table.Code;
        }
        return null;
    }

    private Table[] Chain()
    {
        if (_chain is not null) return _chain;
        var chain = new List<Table>();
        for (string? tag = Language; tag is not null; tag = LanguageTag.Parent(tag))
            if (TableOf(tag) is { } table) chain.Add(table);
        _ownTables = chain.Count;
        if (TableOf(EffectiveFallback) is { } fallback && !chain.Contains(fallback)) chain.Add(fallback);
        return _chain = [.. chain];
    }

    /// <summary>Whether <paramref name="key"/> has a text: in the language in use, its parents or the fallback language, or in <paramref name="language"/> alone when given.</summary>
    public bool Has(string key, string? language = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (language is not null) return TableOf(language)?.Entries.ContainsKey(key) ?? false;
        foreach (var table in Chain())
            if (table.Entries.ContainsKey(key)) return true;
        return false;
    }

    /// <summary>The keys of <paramref name="language"/>'s own table (not its parents'); empty when it has none.</summary>
    public IReadOnlyCollection<string> Keys(string language)
    {
        ArgumentNullException.ThrowIfNull(language);
        return TableOf(language)?.Entries.Keys ?? (IReadOnlyCollection<string>)[];
    }

    /// <summary>The message of <paramref name="key"/> as <paramref name="language"/>'s own table writes it, or null.</summary>
    public string? Pattern(string key, string language)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(language);
        return TableOf(language) is { } table && table.Entries.TryGetValue(key, out var entry) ? entry.Pattern : null;
    }

    /// <summary>The text of <paramref name="key"/> in the language in use; the same as <see cref="Format(string, ReadOnlySpan{TextArgument})"/> without values.</summary>
    public string this[string key] => Format(key, []);

    /// <summary>The text of <paramref name="key"/> in the language in use, or null when no table has it; nothing is reported.</summary>
    public string? TextOrNull(string key, params ReadOnlySpan<TextArgument> arguments)
    {
        ArgumentNullException.ThrowIfNull(key);
        foreach (var table in Chain())
            if (table.Entries.TryGetValue(key, out var entry)) return Write(table, key, entry, arguments);
        return null;
    }

    /// <summary>
    /// The text of <paramref name="key"/> in the language in use, with <paramref name="arguments"/> in place:
    /// <c>Format("hud.gems", ("count", 3))</c>. A key no table has gives the key itself, and is reported once
    /// (<see cref="Report"/>, <see cref="MissingKeys"/>). A text without values is returned as stored: asking for it
    /// every frame allocates nothing.
    /// </summary>
    public string Format(string key, params ReadOnlySpan<TextArgument> arguments)
    {
        ArgumentNullException.ThrowIfNull(key);
        var chain = Chain();
        for (int i = 0; i < chain.Length; i++)
        {
            if (!chain[i].Entries.TryGetValue(key, out var entry)) continue;
            if (i >= _ownTables && _ownTables > 0 && _reported.Add("fallback:" + key))
                Report?.Invoke($"'{key}' has no text in '{Language}'; it is shown in '{chain[i].Code}'. Add it to that language's table ('microengine content check' lists every such key).");
            return Write(chain[i], key, entry, arguments);
        }
        if (_reported.Add("missing:" + key))
        {
            _missing.Add(key);
            Report?.Invoke($"no text for '{key}' in '{Language}'{(LanguageTag.Same(Language, EffectiveFallback) ? "" : $" or '{EffectiveFallback}'")}; the key itself is shown. Add it to the string tables, or correct the key.");
        }
        return key;
    }

    /// <summary>The text of <paramref name="text"/> in the language in use, as <see cref="Format(string, ReadOnlySpan{TextArgument})"/>.</summary>
    public string Format(LocalizedText text) => Format(text.Key, text.Arguments);

    private string Write(Table table, string key, Entry entry, ReadOnlySpan<TextArgument> arguments)
    {
        if (entry.Broken) return entry.Pattern;
        if (entry.Message is null)
        {
            if (!Message.TryParse(entry.Pattern, out entry.Message, out var error))
            {
                entry.Broken = true;
                Report?.Invoke($"the text of '{key}' in '{table.Code}' cannot be read and is shown as written: {error}");
                return entry.Pattern;
            }
        }
        var message = entry.Message!;
        if (message.IsPlain) return message.Format(table.Code, arguments);
        _builder.Clear();
        message.Format(_builder, table.Code, arguments);
        return _builder.ToString();
    }

    /// <summary>
    /// Every character the texts of <paramref name="language"/> (the language in use when null) can show, each once,
    /// in code order: what to hand to a font's <c>Prepare</c> so no glyph is rasterized during play, and what its font
    /// must cover. Includes the texts of its parents and of the fallback language, which fill in missing keys, and
    /// the printable ASCII characters (numbers and names put into messages) unless <paramref name="includeAscii"/> is false.
    /// </summary>
    public string Characters(string? language = null, bool includeAscii = true)
    {
        var tables = new List<Table>();
        for (string? tag = language ?? Language; tag is not null; tag = LanguageTag.Parent(tag))
            if (TableOf(tag) is { } table) tables.Add(table);
        if (TableOf(EffectiveFallback) is { } fallback && !tables.Contains(fallback)) tables.Add(fallback);
        var seen = new SortedSet<int>();
        if (includeAscii)
            for (int c = ' '; c <= '~'; c++) seen.Add(c);
        foreach (var table in tables)
        {
            if (table.Name is not null) AddRunes(seen, table.Name, includeAscii);
            foreach (var entry in table.Entries.Values) AddRunes(seen, entry.Pattern, includeAscii);
        }
        // The names of every language, as a language menu shows them whatever the language in use.
        foreach (var table in _tables)
            if (table.Name is not null) AddRunes(seen, table.Name, includeAscii);
        var text = new StringBuilder(seen.Count);
        foreach (int rune in seen) text.Append(char.ConvertFromUtf32(rune));
        return text.ToString();

        static void AddRunes(SortedSet<int> seen, string text, bool ascii)
        {
            foreach (var rune in text.EnumerateRunes())
                if (rune.Value >= ' ' && (ascii || rune.Value > '~')) seen.Add(rune.Value);
        }
    }
}

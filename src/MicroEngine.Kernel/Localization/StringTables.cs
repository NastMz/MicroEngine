using System.Text;
using System.Text.Json;

namespace MicroEngine.Localization;

/// <summary>The texts of one language as a file gives them, with what the file says about the language.</summary>
/// <param name="Language">The language tag (<c>en</c>, <c>pt-BR</c>).</param>
/// <param name="Entries">Each key and its message as written.</param>
public sealed record StringTableData(string Language, IReadOnlyDictionary<string, string> Entries)
{
    /// <summary>The language's own name for a language menu (<c>@name</c>: "Español"), or null when the file gives none.</summary>
    public string? Name { get; init; }
    /// <summary>Whether the language reads right to left (<c>@direction</c>: <c>rtl</c> or <c>ltr</c>), or null to go by the language tag.</summary>
    public bool? RightToLeft { get; init; }
    /// <summary>The font files of the language (<c>@fonts</c>), the main face first and then those that fill in what it lacks; empty when the file names none.</summary>
    public IReadOnlyList<string> Fonts { get; init; } = [];
}

/// <summary>
/// Reads string tables: a JSON file per language (<c>es.strings.json</c>) or a CSV file with a column per language
/// (<c>game.strings.csv</c>). Both hold a message per key (<see cref="Message"/>); keys starting with <c>@</c>
/// describe the language (<c>@name</c>, <c>@fonts</c>, <c>@direction</c>).
/// </summary>
public static class StringTables
{
    /// <summary>The ending of a JSON table's file name; what comes before it is the language tag.</summary>
    public const string JsonExtension = ".strings.json";
    /// <summary>The ending of a CSV table's file name.</summary>
    public const string CsvExtension = ".strings.csv";

    /// <summary>
    /// The start of the keys that translate the engine's names of keys and controls in action prompts:
    /// <c>prompt.Space</c>, <c>prompt.Enter</c>, <c>prompt.D-pad</c>. A language gives only those it has another word for.
    /// </summary>
    public const string PromptPrefix = "prompt.";

    /// <summary>
    /// Whether a file name is a string table's: it ends in <see cref="JsonExtension"/> or <see cref="CsvExtension"/>. A
    /// JSON table must also be named by its language (<see cref="LanguageOf"/>); one that is not is refused when
    /// loaded and reported by the content check, never skipped.
    /// </summary>
    public static bool IsTable(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path.EndsWith(CsvExtension, StringComparison.OrdinalIgnoreCase) || path.EndsWith(JsonExtension, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The language of a JSON table from its file name (<c>text/pt-BR.strings.json</c> is <c>pt-BR</c>), or null when
    /// the file is not a JSON table or its name is not a language tag (<c>menu.strings.json</c>). Any name with the
    /// shape of a tag counts: two or three letters, then parts of letters or digits.
    /// </summary>
    public static string? LanguageOf(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!path.EndsWith(JsonExtension, StringComparison.OrdinalIgnoreCase)) return null;
        string name = path[(path.LastIndexOfAny(['/', '\\']) + 1)..^JsonExtension.Length];
        return LanguageTag.IsValid(name) ? LanguageTag.Normalize(name) : null;
    }

    /// <summary>
    /// Reads a JSON table: an object whose values are messages, or objects of messages whose keys join with a dot
    /// (<c>{"menu": {"play": "Play"}}</c> is <c>menu.play</c>). Comments and trailing commas are allowed.
    /// </summary>
    /// <exception cref="FormatException">The text is not a table: it says what is wrong and on which line.</exception>
    public static StringTableData ParseJson(string language, string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        ArgumentNullException.ThrowIfNull(json);
        var bytes = Encoding.UTF8.GetBytes(json.TrimStart('﻿'));
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        var meta = new Meta();
        var path = new List<string>();
        try
        {
            var reader = new Utf8JsonReader(bytes, new JsonReaderOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) throw Problem(bytes, reader.TokenStartIndex, "a string table is an object of keys and texts: it starts with '{'");
            string? name = null;
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        name = reader.GetString()!;
                        break;
                    case JsonTokenType.StartObject:
                        path.Add(name!);
                        break;
                    case JsonTokenType.EndObject:
                        if (path.Count > 0) path.RemoveAt(path.Count - 1);
                        break;
                    case JsonTokenType.String:
                    {
                        string key = Key(path, name!);
                        string value = reader.GetString()!;
                        if (key.StartsWith('@')) { if (!meta.Set(key, value)) throw Problem(bytes, reader.TokenStartIndex, UnknownMeta(key)); }
                        else if (!entries.TryAdd(key, value)) throw Problem(bytes, reader.TokenStartIndex, $"the key '{key}' appears twice");
                        break;
                    }
                    case JsonTokenType.StartArray when Key(path, name!) == "@fonts":
                    {
                        var fonts = new List<string>();
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            if (reader.TokenType != JsonTokenType.String) throw Problem(bytes, reader.TokenStartIndex, "'@fonts' lists font files as strings");
                            fonts.Add(reader.GetString()!);
                        }
                        meta.Fonts = fonts;
                        break;
                    }
                    default:
                        throw Problem(bytes, reader.TokenStartIndex, $"the value of '{Key(path, name ?? "")}' is not a text: every value is a string between quotes, or an object of them");
                }
            }
        }
        catch (JsonException e)
        {
            throw new FormatException($"Not a JSON string table: {e.Message} (line {(e.LineNumber ?? 0) + 1}).", e);
        }
        return meta.Apply(new StringTableData(LanguageTag.Normalize(language), entries));

        static string Key(List<string> path, string name) => path.Count == 0 ? name : string.Join('.', path) + "." + name;
    }

    private static string UnknownMeta(string key) => $"'{key}' is not something a table says about its language: use @name, @fonts or @direction (keys of texts cannot start with '@')";

    private static FormatException Problem(byte[] bytes, long index, string message)
    {
        int line = 1;
        for (int i = 0; i < index && i < bytes.Length; i++)
            if (bytes[i] == (byte)'\n') line++;
        return At(line, message);
    }

    private static FormatException At(int line, string message) => new($"{char.ToUpperInvariant(message[0])}{message[1..]} (line {line}).");

    /// <summary>What a table says about its language, gathered while reading.</summary>
    private sealed class Meta
    {
        public string? Name;
        public bool? RightToLeft;
        public IReadOnlyList<string> Fonts = [];

        public bool Set(string key, string value)
        {
            switch (key)
            {
                case "@name":
                    Name = value;
                    return true;
                case "@direction" when value is "rtl" or "ltr":
                    RightToLeft = value == "rtl";
                    return true;
                case "@fonts":
                    Fonts = [.. value.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
                    return true;
                default:
                    return false;
            }
        }

        public StringTableData Apply(StringTableData table) => table with { Name = Name, RightToLeft = RightToLeft, Fonts = Fonts };
    }

    /// <summary>
    /// Reads a CSV table: the first row names the key column and then a language tag per column, every other row is a
    /// key and its message in each language. An empty cell is a text the language does not have; a row whose first
    /// cell starts with <c>#</c> is a comment. Cells follow the usual rules: commas separate them, quotes surround a
    /// cell holding commas, quotes or line breaks, and two quotes inside are one.
    /// </summary>
    /// <exception cref="FormatException">The text is not a table: it says what is wrong and on which line.</exception>
    public static IReadOnlyList<StringTableData> ParseCsv(string csv)
    {
        ArgumentNullException.ThrowIfNull(csv);
        var rows = Rows(csv.TrimStart('﻿'));
        if (rows.Count == 0) throw At(1, $"a CSV string table starts with a row naming the key column and each language (key,en,es)");
        var header = rows[0].Cells;
        if (header.Count < 2) throw At(1, $"a CSV string table needs at least one language column after the key column (key,en)");
        var languages = new string[header.Count - 1];
        for (int c = 1; c < header.Count; c++)
        {
            string tag = header[c].Trim();
            if (!LanguageTag.IsValid(tag)) throw At(1, $"'{tag}' (column {c + 1}) is not a language tag such as en, pt-BR or zh-Hans");
            languages[c - 1] = LanguageTag.Normalize(tag);
            for (int before = 0; before < c - 1; before++)
                if (LanguageTag.Same(languages[before], languages[c - 1])) throw At(1, $"the language '{tag}' has two columns");
        }
        var entries = languages.Select(_ => new Dictionary<string, string>(StringComparer.Ordinal)).ToArray();
        var meta = languages.Select(_ => new Meta()).ToArray();
        foreach (var (line, cells) in rows.Skip(1))
        {
            string key = cells[0].Trim();
            // A line of nothing but spaces (or of empty cells) is an empty line.
            if (key.Length == 0 && cells.All(string.IsNullOrWhiteSpace)) continue;
            if (key.StartsWith('#')) continue;
            if (key.Length == 0) throw At(line, $"the row has texts but no key in its first cell");
            if (cells.Count > header.Count) throw At(line, $"the row of '{key}' has {cells.Count} cells but the table has {header.Count} columns; put a text that holds commas between quotes");
            for (int c = 1; c < cells.Count; c++)
            {
                if (cells[c].Length == 0) continue;
                if (key.StartsWith('@')) { if (!meta[c - 1].Set(key, cells[c])) throw At(line, $"{UnknownMeta(key)}"); }
                else if (!entries[c - 1].TryAdd(key, cells[c])) throw At(line, $"the key '{key}' appears twice");
            }
        }
        var tables = new StringTableData[languages.Length];
        for (int i = 0; i < languages.Length; i++) tables[i] = meta[i].Apply(new StringTableData(languages[i], entries[i]));
        return tables;
    }

    private static List<(int Line, List<string> Cells)> Rows(string csv)
    {
        var rows = new List<(int, List<string>)>();
        var cells = new List<string>();
        var cell = new StringBuilder();
        int line = 1, rowLine = 1, i = 0;
        bool any = false;
        while (i < csv.Length)
        {
            char c = csv[i];
            if (c == '"' && cell.Length == 0)
            {
                int opened = line;
                i++;
                while (true)
                {
                    if (i >= csv.Length) throw At(opened, $"a quote opens a cell that is never closed");
                    if (csv[i] == '"')
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '"') { cell.Append('"'); i += 2; continue; }
                        i++;
                        break;
                    }
                    if (csv[i] == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') { i++; continue; }
                    if (csv[i] == '\n') line++;
                    cell.Append(csv[i++]);
                }
                any = true;
                continue;
            }
            if (c == ',') { cells.Add(cell.ToString()); cell.Clear(); any = true; i++; continue; }
            if (c is '\n' or '\r')
            {
                if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++;
                i++;
                if (any || cell.Length > 0) { cells.Add(cell.ToString()); rows.Add((rowLine, cells)); }
                cells = [];
                cell.Clear();
                any = false;
                line++;
                rowLine = line;
                continue;
            }
            cell.Append(c);
            any = true;
            i++;
        }
        if (any || cell.Length > 0) { cells.Add(cell.ToString()); rows.Add((rowLine, cells)); }
        return rows;
    }
}

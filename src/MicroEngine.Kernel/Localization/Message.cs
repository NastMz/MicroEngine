using System.Globalization;
using System.Text;

namespace MicroEngine.Localization;

/// <summary>How a message uses one of its arguments.</summary>
public enum MessageArgumentKind
{
    /// <summary>The value is shown: <c>{name}</c>.</summary>
    Value,
    /// <summary>The number chooses a plural form: <c>{count, plural, one {...} other {...}}</c>.</summary>
    Plural,
    /// <summary>The text chooses a branch: <c>{who, select, hero {...} other {...}}</c>.</summary>
    Select,
}

/// <summary>An argument a <see cref="Message"/> names, for tools that compare the translations of a text.</summary>
/// <param name="Name">The name between braces.</param>
/// <param name="Kind">How the message uses it.</param>
/// <param name="Branches">The branches of a plural or a selection as written (<c>=0</c>, <c>one</c>, <c>other</c>); empty for a value.</param>
public sealed record MessageArgument(string Name, MessageArgumentKind Kind, IReadOnlyList<string> Branches);

/// <summary>
/// A text with named values, plural forms and selections, in the ICU MessageFormat syntax translation tools write:
/// <c>{who} found {count, plural, =0 {nothing} one {# gem} other {# gems}}</c>. Parsed once, formatted any number of
/// times. String tables hold these; <see cref="Localizer"/> formats them in the language in use.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>{name}</c> shows a value. A name that was not given stays as written, so <c>Press {attack}</c> reaches the
/// action prompts unchanged.</item>
/// <item><c>{n, plural, ...}</c> chooses by number: <c>=N</c> for an exact number, then the language's form
/// (<see cref="PluralRules"/>), else <c>other</c>, which every plural must have. <c>#</c> inside a branch is the number.</item>
/// <item><c>{x, select, a {...} b {...} other {...}}</c> chooses by text.</item>
/// <item><c>{n, number}</c> is <c>{n}</c>. Numbers are written without grouping, with up to three decimals.</item>
/// <item>An apostrophe before <c>{</c>, <c>}</c> or <c>#</c> quotes text up to the next apostrophe (<c>'{'</c> is a
/// brace), two apostrophes are one, and any other apostrophe is itself (<c>don't</c>).</item>
/// </list>
/// Not read, and refused when parsing: <c>selectordinal</c>, <c>offset:</c>, the date, time, duration and
/// spell-out formats, and arguments nested deeper than <see cref="MaxDepth"/> levels.
/// </remarks>
public sealed class Message
{
    private abstract class Node;
    private sealed class TextNode(string text) : Node { public readonly string Text = text; }
    private sealed class ValueNode(string name, string source) : Node { public readonly string Name = name, Source = source; }
    private sealed class NumberNode(string name) : Node { public readonly string Name = name; }
    private sealed class ChoiceNode(string name, bool plural) : Node
    {
        public readonly string Name = name;
        public readonly bool Plural = plural;
        /// <summary>The argument as written, shown when its value is not given.</summary>
        public string Source = "";
        public readonly List<(double Number, Node[] Body)> Exact = [];
        public readonly List<(string Key, Node[] Body)> Cases = [];
        public readonly Node[]?[] Forms = new Node[]?[6];
        public Node[] Other = [];
    }

    private readonly Node[] _nodes;
    private readonly string? _plain;

    private Message(string pattern, Node[] nodes, List<MessageArgument> arguments)
    {
        Pattern = pattern;
        _nodes = nodes;
        Arguments = arguments;
        if (nodes.All(n => n is TextNode)) _plain = string.Concat(nodes.Select(n => ((TextNode)n).Text));
    }

    /// <summary>How deep arguments may nest inside the branches of others (32): a message nested deeper is refused when parsed.</summary>
    public const int MaxDepth = 32;

    /// <summary>The message as written.</summary>
    public string Pattern { get; }
    /// <summary>Whether the message is text alone: no value, plural or selection.</summary>
    public bool IsPlain => _plain is not null;
    /// <summary>The arguments the message names, in the order they first appear, each once.</summary>
    public IReadOnlyList<MessageArgument> Arguments { get; }

    /// <summary>Parses <paramref name="pattern"/>.</summary>
    /// <exception cref="FormatException">The pattern is not a message; the text says what is wrong and where.</exception>
    public static Message Parse(string pattern) =>
        TryParse(pattern, out var message, out var error) ? message! : throw new FormatException(error);

    /// <summary>Parses <paramref name="pattern"/>, or says in <paramref name="error"/> what is wrong with it.</summary>
    public static bool TryParse(string pattern, out Message? message, out string? error)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        var parser = new Parser(pattern);
        try
        {
            var nodes = parser.Body(0, top: true, pluralName: null);
            message = new Message(pattern, nodes, parser.Arguments);
            error = null;
            return true;
        }
        catch (FormatException e)
        {
            message = null;
            error = e.Message;
            return false;
        }
    }

    /// <summary>The message in <paramref name="language"/> with <paramref name="arguments"/>.</summary>
    public string Format(string language, ReadOnlySpan<TextArgument> arguments)
    {
        if (_plain is not null) return _plain;
        var text = new StringBuilder(Pattern.Length + 16);
        Format(text, language, arguments);
        return text.ToString();
    }

    /// <summary>Appends the message in <paramref name="language"/> with <paramref name="arguments"/> to <paramref name="text"/>.</summary>
    public void Format(StringBuilder text, string language, ReadOnlySpan<TextArgument> arguments)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(language);
        if (_plain is not null) text.Append(_plain);
        else Write(text, _nodes, language, arguments);
    }

    private static bool Find(ReadOnlySpan<TextArgument> arguments, string name, out TextArgument found)
    {
        foreach (ref readonly var argument in arguments)
            if (argument.Name == name) { found = argument; return true; }
        found = default;
        return false;
    }

    private static void Write(StringBuilder text, Node[] nodes, string language, ReadOnlySpan<TextArgument> arguments)
    {
        foreach (var node in nodes)
            switch (node)
            {
                case TextNode t:
                    text.Append(t.Text);
                    break;
                case ValueNode v:
                    if (Find(arguments, v.Name, out var value)) value.AppendTo(text);
                    else text.Append(v.Source);
                    break;
                case NumberNode n:
                    if (Find(arguments, n.Name, out var number)) number.AppendTo(text);
                    break;
                case ChoiceNode c:
                    if (!Find(arguments, c.Name, out var chosen)) { text.Append(c.Source); break; }
                    Write(text, c.Plural ? PluralBranch(c, chosen, language) : SelectBranch(c, chosen), language, arguments);
                    break;
            }
    }

    private static Node[] PluralBranch(ChoiceNode choice, TextArgument argument, string language)
    {
        double number;
        if (argument.IsNumber) number = argument.Number;
        else if (!double.TryParse(argument.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return choice.Other;
        foreach (var (exact, body) in choice.Exact)
            if (exact == number) return body;
        return choice.Forms[(int)PluralRules.Select(language, number)] ?? choice.Other;
    }

    private static Node[] SelectBranch(ChoiceNode choice, TextArgument argument)
    {
        string key = argument.ToString();
        foreach (var (name, body) in choice.Cases)
            if (name == key) return body;
        return choice.Other;
    }

    private sealed class Parser(string source)
    {
        public readonly List<MessageArgument> Arguments = [];
        private int _at, _depth;

        private FormatException Error(string message, int at) => new($"{message} (at character {at + 1} of \"{source}\").");

        /// <summary>A run of text and arguments, up to the end (top level) or to the brace that closes a branch.</summary>
        public Node[] Body(int start, bool top, string? pluralName)
        {
            _at = start;
            var nodes = new List<Node>();
            var text = new StringBuilder();
            void Flush()
            {
                if (text.Length == 0) return;
                nodes.Add(new TextNode(text.ToString()));
                text.Clear();
            }

            while (_at < source.Length)
            {
                char c = source[_at];
                if (c == '\'')
                {
                    Apostrophe(text, pluralName is not null);
                    continue;
                }
                if (c == '}' && !top) { Flush(); return [.. nodes]; }
                if (c == '#' && pluralName is not null)
                {
                    Flush();
                    nodes.Add(new NumberNode(pluralName));
                    _at++;
                    continue;
                }
                if (c == '{' && Argument(pluralName) is { } node)
                {
                    Flush();
                    nodes.Add(node);
                    continue;
                }
                text.Append(c);
                _at++;
            }
            if (!top) throw Error("A branch is never closed with '}'", start - 1);
            Flush();
            return [.. nodes];
        }

        private void Apostrophe(StringBuilder text, bool inPlural)
        {
            // '' is an apostrophe; an apostrophe before a brace (or # in a plural) quotes up to the next apostrophe;
            // any other apostrophe is itself.
            if (_at + 1 < source.Length && source[_at + 1] == '\'') { text.Append('\''); _at += 2; return; }
            if (_at + 1 < source.Length && (source[_at + 1] is '{' or '}' || (source[_at + 1] == '#' && inPlural)))
            {
                _at++;
                while (_at < source.Length)
                {
                    if (source[_at] == '\'')
                    {
                        if (_at + 1 < source.Length && source[_at + 1] == '\'') { text.Append('\''); _at += 2; continue; }
                        _at++;
                        return;
                    }
                    text.Append(source[_at++]);
                }
                return;
            }
            text.Append('\'');
            _at++;
        }

        /// <summary>The argument that starts at the brace under the cursor, or null when the brace is plain text.</summary>
        private Node? Argument(string? pluralName)
        {
            int open = _at, i = _at + 1;
            while (i < source.Length && (source[i] is ' ' or '\t' or '\r' or '\n')) i++;
            int nameStart = i;
            while (i < source.Length && source[i] is not ('{' or '}' or ',' or ' ' or '\'' or '\n' or '\r' or '\t')) i++;
            string name = source[nameStart..i];
            while (i < source.Length && (source[i] is ' ' or '\t' or '\r' or '\n')) i++;
            // Not the shape of an argument ("{{", "{ }", "{two words}"): the brace is text.
            if (name.Length == 0 || i >= source.Length || source[i] is not ('}' or ',')) return null;
            if (source[i] == '}')
            {
                _at = i + 1;
                Note(name, MessageArgumentKind.Value, []);
                return new ValueNode(name, source[open.._at]);
            }

            i++;
            while (i < source.Length && (source[i] is ' ' or '\t' or '\r' or '\n')) i++;
            int typeStart = i;
            while (i < source.Length && char.IsAsciiLetter(source[i])) i++;
            string type = source[typeStart..i];
            while (i < source.Length && (source[i] is ' ' or '\t' or '\r' or '\n')) i++;
            switch (type)
            {
                case "number":
                    // A style after "number" (", integer") is accepted and ignored: numbers are written one way.
                    int close = source.IndexOf('}', i);
                    if (close < 0) throw Error($"The argument '{name}' is never closed with '}}'", open);
                    _at = close + 1;
                    Note(name, MessageArgumentKind.Value, []);
                    return new ValueNode(name, source[open.._at]);
                case "plural" or "select":
                    if (i >= source.Length || source[i] != ',') throw Error($"'{name}, {type}' needs its branches after a comma", open);
                    return Choice(name, type == "plural", open, i + 1);
                case "selectordinal" or "date" or "time" or "duration" or "spellout" or "ordinal":
                    throw Error($"'{type}' is not read: only plural, select and number are. Format the value in the game and pass it as text", open);
                default:
                    throw Error($"'{type}' is not something an argument can be: use plural, select or number", open);
            }
        }

        private ChoiceNode Choice(string name, bool plural, int open, int start)
        {
            var node = new ChoiceNode(name, plural);
            var branches = new List<string>();
            // Named before its branches are read, so arguments are listed in the order they appear.
            Note(name, plural ? MessageArgumentKind.Plural : MessageArgumentKind.Select, branches);
            bool other = false;
            int i = start;
            while (true)
            {
                while (i < source.Length && source[i] is ' ' or '\n' or '\r' or '\t') i++;
                if (i >= source.Length) throw Error($"The argument '{name}' is never closed with '}}'", open);
                if (source[i] == '}') break;
                int keyStart = i;
                while (i < source.Length && source[i] is not ('{' or '}' or ' ' or '\n' or '\r' or '\t')) i++;
                string key = source[keyStart..i];
                while (i < source.Length && source[i] is ' ' or '\n' or '\r' or '\t') i++;
                if (key.StartsWith("offset:", StringComparison.Ordinal)) throw Error("'offset:' is not read: subtract in the game and pass the result", keyStart);
                if (i >= source.Length || source[i] != '{') throw Error($"The branch '{key}' of '{name}' needs its text between braces", keyStart);
                if (branches.Contains(key)) throw Error($"The branch '{key}' of '{name}' appears twice", keyStart);
                branches.Add(key);
                if (++_depth > MaxDepth) throw Error($"Arguments nest deeper than {MaxDepth} levels, the most a message may have", open);
                var body = Body(i + 1, top: false, plural ? name : null);
                _depth--;
                i = _at + 1;
                if (key == "other") { node.Other = body; other = true; }
                else if (!plural) node.Cases.Add((key, body));
                else if (key.StartsWith('='))
                {
                    if (!double.TryParse(key.AsSpan(1), NumberStyles.Float, CultureInfo.InvariantCulture, out double exact))
                        throw Error($"'{key}' is not a number after '='", keyStart);
                    node.Exact.Add((exact, body));
                }
                else if (TryCategory(key, out var category)) node.Forms[(int)category] = body;
                else throw Error($"'{key}' is not a plural form: use zero, one, two, few, many, other or an exact number such as =0", keyStart);
            }
            if (!other) throw Error($"The {(plural ? "plural" : "selection")} '{name}' has no 'other' branch, which every one needs", open);
            _at = i + 1;
            node.Source = source[open.._at];
            return node;
        }

        private static bool TryCategory(string key, out PluralCategory category)
        {
            (bool known, category) = key switch
            {
                "zero" => (true, PluralCategory.Zero),
                "one" => (true, PluralCategory.One),
                "two" => (true, PluralCategory.Two),
                "few" => (true, PluralCategory.Few),
                "many" => (true, PluralCategory.Many),
                _ => (false, PluralCategory.Other),
            };
            return known;
        }

        /// <summary>Records an argument the first time it is named; a later, richer use (a plural after a value) replaces it.</summary>
        private void Note(string name, MessageArgumentKind kind, IReadOnlyList<string> branches)
        {
            int at = Arguments.FindIndex(a => a.Name == name);
            if (at < 0) Arguments.Add(new MessageArgument(name, kind, branches));
            else if (Arguments[at].Kind == MessageArgumentKind.Value && kind != MessageArgumentKind.Value) Arguments[at] = new MessageArgument(name, kind, branches);
        }
    }
}

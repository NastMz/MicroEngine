using System.Globalization;
using System.Text;

namespace MicroEngine.Localization;

/// <summary>
/// A named value a message shows or chooses by: <c>("count", 3)</c>, <c>("who", "Ana")</c>. Tuples of a name and a
/// string or a number convert to it, so calls read <c>Format("gems", ("count", gems))</c>; nothing is boxed.
/// </summary>
public readonly struct TextArgument : IEquatable<TextArgument>
{
    /// <summary>A text value.</summary>
    public TextArgument(string name, string? text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Text = text ?? "";
    }

    /// <summary>A number: it is written without grouping (<c>1500</c>, <c>0.5</c>) and chooses plural forms.</summary>
    public TextArgument(string name, double number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Number = number;
    }

    /// <summary>The name the message uses between braces.</summary>
    public string Name { get; }
    /// <summary>The text, or null when the value is a number.</summary>
    public string? Text { get; }
    /// <summary>The number, when <see cref="IsNumber"/>.</summary>
    public double Number { get; }
    /// <summary>Whether the value is a number.</summary>
    public bool IsNumber => Text is null;

    /// <summary>A text value.</summary>
    public static implicit operator TextArgument((string Name, string? Text) value) => new(value.Name, value.Text);
    /// <summary>A whole number.</summary>
    public static implicit operator TextArgument((string Name, int Number) value) => new(value.Name, value.Number);
    /// <summary>A whole number.</summary>
    public static implicit operator TextArgument((string Name, long Number) value) => new(value.Name, value.Number);
    /// <summary>A number.</summary>
    public static implicit operator TextArgument((string Name, float Number) value) => new(value.Name, MathF.Round(value.Number, 3));
    /// <summary>A number.</summary>
    public static implicit operator TextArgument((string Name, double Number) value) => new(value.Name, value.Number);

    /// <summary>The value as a message shows it: the text, or the number with up to three decimals and no grouping.</summary>
    public override string ToString() => Text ?? NumberText(Number);

    internal void AppendTo(StringBuilder text)
    {
        if (Text is not null) text.Append(Text);
        else AppendNumber(text, Number);
    }

    internal static void AppendNumber(StringBuilder text, double number)
    {
        if (number == Math.Floor(number) && Math.Abs(number) < 1e15)
        {
            // Whole numbers are written into the builder directly: no string for a score that changes every frame.
            Span<char> digits = stackalloc char[24];
            ((long)number).TryFormat(digits, out int written, default, CultureInfo.InvariantCulture);
            text.Append(digits[..written]);
        }
        else text.Append(NumberText(number));
    }

    private static string NumberText(double number) =>
        number == Math.Floor(number) && Math.Abs(number) < 1e15
            ? ((long)number).ToString(CultureInfo.InvariantCulture)
            : number.ToString("0.###", CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public bool Equals(TextArgument other) => Name == other.Name && Text == other.Text && Number.Equals(other.Number);
    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is TextArgument other && Equals(other);
    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Name, Text, Number);
    /// <summary>Whether two arguments have the same name and value.</summary>
    public static bool operator ==(TextArgument left, TextArgument right) => left.Equals(right);
    /// <summary>Whether two arguments differ in name or value.</summary>
    public static bool operator !=(TextArgument left, TextArgument right) => !left.Equals(right);
}

/// <summary>
/// A text named by its key, with the values it shows: what a widget keeps so its text follows the language
/// (<c>new Label(new LocalizedText("hud.gems", ("count", gems)))</c>). <see cref="Localizer.Format(LocalizedText)"/>
/// gives the text in the language in use.
/// </summary>
public readonly struct LocalizedText : IEquatable<LocalizedText>
{
    private readonly TextArgument[]? _arguments;

    /// <summary>The text of <paramref name="key"/> with <paramref name="arguments"/>.</summary>
    public LocalizedText(string key, params TextArgument[] arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Key = key;
        _arguments = arguments;
    }

    /// <summary>The key in the string tables.</summary>
    public string Key { get; }
    /// <summary>The values the message shows or chooses by.</summary>
    public ReadOnlySpan<TextArgument> Arguments => _arguments;

    /// <summary>A key alone converts to a text without arguments.</summary>
    public static explicit operator LocalizedText(string key) => new(key);

    /// <inheritdoc/>
    public bool Equals(LocalizedText other) => Key == other.Key && Arguments.SequenceEqual(other.Arguments);
    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is LocalizedText other && Equals(other);
    /// <inheritdoc/>
    public override int GetHashCode() => Key?.GetHashCode() ?? 0;
    /// <summary>Whether two texts have the same key and values.</summary>
    public static bool operator ==(LocalizedText left, LocalizedText right) => left.Equals(right);
    /// <summary>Whether two texts differ in key or values.</summary>
    public static bool operator !=(LocalizedText left, LocalizedText right) => !left.Equals(right);
    /// <inheritdoc/>
    public override string ToString() => Key ?? "";
}

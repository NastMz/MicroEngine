using System.Runtime.CompilerServices;

namespace MicroEngine.Graphics;

/// <summary>
/// Text built from an interpolated string without making a string: where a font, a sprite atlas or another engine call
/// takes an <see cref="InterpolatedText"/>, <c>$"{hp}/{max}"</c> is written into a buffer the engine reuses, drawn or
/// looked up, and the buffer goes back, so a number shown every frame makes no garbage.
/// </summary>
/// <remarks>
/// <para>
/// The text is exactly what the interpolated string gives as a <see cref="string"/>: the same formats
/// (<c>{time:0.0}</c>), alignment (<c>{score,6}</c>) and culture. Written without allocating: whole numbers and
/// numbers with a fraction of every built-in size (<c>decimal</c> included), <c>int?</c>, <c>long?</c>,
/// <c>float?</c> and <c>double?</c>, characters, booleans, strings, spans of characters, enums written by name,
/// <see cref="TimeSpan"/>, <see cref="DateTime"/> and <see cref="Guid"/>. What allocates: an enum the first time each
/// of its values is shown (its name is kept afterwards) or written with a format (<c>{suit:D}</c>), a few bytes; any
/// other value type (a struct of the game's own, a nullable of another type), boxed to be formatted; a value held as
/// <see cref="object"/>; and a type that only has <c>ToString</c>, which is written through it.
/// </para>
/// <para>
/// The compiler makes it: write the interpolated string where the parameter is, and do not keep the value. A method
/// that receives one reads <see cref="Text"/> and then calls <see cref="Dispose"/> once. A text that was already used
/// refuses to be read again (<see cref="ObjectDisposedException"/>).
/// </para>
/// <para>
/// An interpolated string that holds an <c>await</c> or a <c>dynamic</c> value cannot be written into a buffer on the
/// stack and does not compile here: put the value in a local first (<c>var n = await Count(); Draw(r, $"{n}", ...)</c>).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// Font.Draw(r, $"{hp}/{maxHp}", new Vector2(8, 8), Color.White);
/// Font.DrawCentered(r, $"{minutes}:{seconds:00}", new Vector2(160, 12), Color.White);
/// r.Draw(atlas[$"water{mask}_{frame}"], at);
/// </code>
/// </example>
[InterpolatedStringHandler]
public ref struct InterpolatedText
{
    // The buffers of the texts being written on this thread, one per level of nesting (a value of one text that draws
    // another), each reused by the next text at its level. A text knows its level and the count of uses its level had
    // when it took the buffer: a copy that was already used finds the count moved, so it can neither read what a later
    // text wrote nor give the buffer back a second time.
    private const int Levels = 4;
    [ThreadStatic] private static char[]?[]? t_buffers;
    [ThreadStatic] private static int[]? t_uses;
    [ThreadStatic] private static int t_depth;

    private char[]? _chars;
    private int _length;
    private readonly int _level;   // -1: a buffer of its own (texts nested deeper than the levels kept)
    private readonly int _use;

    /// <summary>Called by the compiler: <paramref name="literalLength"/> characters of fixed text and <paramref name="formattedCount"/> values.</summary>
    public InterpolatedText(int literalLength, int formattedCount)
    {
        int needed = Math.Max(128, literalLength + formattedCount * 12);
        var buffers = t_buffers ??= new char[]?[Levels];
        var uses = t_uses ??= new int[Levels];
        _length = 0;
        if (t_depth >= Levels)
        {
            _level = -1;
            _use = 0;
            _chars = new char[needed];
            return;
        }
        _level = t_depth++;
        _use = ++uses[_level];
        var chars = buffers[_level];
        if (chars is null || chars.Length < needed) buffers[_level] = chars = new char[needed];
        _chars = chars;
    }

    /// <summary>Whether this text still owns its buffer: false once it, or a copy of it, was used.</summary>
    private readonly bool Live => _chars is not null && (_level < 0 || t_uses![_level] == _use);

    /// <summary>The text written so far. Valid until <see cref="Dispose"/>.</summary>
    /// <exception cref="ObjectDisposedException">The text was already used.</exception>
    public readonly ReadOnlySpan<char> Text => Chars.AsSpan(0, _length);

    /// <summary>
    /// Gives the buffer back. Called once by the method that received the text; the text must not be read afterwards.
    /// Calling it again, on this value or on a copy, does nothing.
    /// </summary>
    public void Dispose()
    {
        if (Live && _level >= 0)
        {
            t_uses![_level]++;
            // Texts deeper than this one that were never given back (a value that threw) are let go with it.
            t_depth = _level;
        }
        _chars = null;
        _length = 0;
    }

    /// <summary>The text as a string (allocates): for a caller that keeps it.</summary>
    public override readonly string ToString() => Text.ToString();

    /// <summary>Called by the compiler for the fixed text between two values.</summary>
    public void AppendLiteral(string value) => Append(value);

    /// <summary>Called by the compiler for a string value; null writes nothing.</summary>
    public void AppendFormatted(string? value) => Append(value);
    /// <summary>Called by the compiler for a string with a width (<c>{name,8}</c>).</summary>
    public void AppendFormatted(string? value, int alignment, string? format = null)
    {
        int start = _length;
        Append(value);
        Pad(start, alignment);
    }

    /// <summary>Called by the compiler for a span of characters.</summary>
    public void AppendFormatted(ReadOnlySpan<char> value) => Append(value);
    /// <summary>Called by the compiler for a span of characters with a width (<c>{span,8}</c>) or a format, which a span ignores as a string does.</summary>
    public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null)
    {
        int start = _length;
        Append(value);
        Pad(start, alignment);
    }

    /// <summary>Called by the compiler for a character.</summary>
    public void AppendFormatted(char value)
    {
        if (_length == Chars.Length) Grow(1);
        _chars![_length++] = value;
    }

    /// <summary>Called by the compiler for true or false.</summary>
    public void AppendFormatted(bool value) => Append(value ? bool.TrueString : bool.FalseString);

    /// <summary>Called by the compiler for a whole number.</summary>
    public void AppendFormatted(int value) => Number(value, null);
    /// <summary>Called by the compiler for a whole number with a format (<c>{seconds:00}</c>).</summary>
    public void AppendFormatted(int value, string? format) => Number(value, format);
    /// <inheritdoc cref="AppendFormatted(int)"/>
    public void AppendFormatted(long value) => Number(value, null);
    /// <inheritdoc cref="AppendFormatted(int, string?)"/>
    public void AppendFormatted(long value, string? format) => Number(value, format);
    /// <summary>Called by the compiler for a number with a fraction.</summary>
    public void AppendFormatted(float value) => Number(value, null);
    /// <summary>Called by the compiler for a number with a fraction and a format (<c>{time:0.0}</c>).</summary>
    public void AppendFormatted(float value, string? format) => Number(value, format);
    /// <inheritdoc cref="AppendFormatted(float)"/>
    public void AppendFormatted(double value) => Number(value, null);
    /// <inheritdoc cref="AppendFormatted(float, string?)"/>
    public void AppendFormatted(double value, string? format) => Number(value, format);

    /// <summary>Called by the compiler for a whole number that may be null; null writes nothing.</summary>
    public void AppendFormatted(int? value) { if (value is { } v) Number(v, null); }
    /// <summary>Called by the compiler for a whole number that may be null, with a format; null writes nothing.</summary>
    public void AppendFormatted(int? value, string? format) { if (value is { } v) Number(v, format); }
    /// <inheritdoc cref="AppendFormatted(int?)"/>
    public void AppendFormatted(long? value) { if (value is { } v) Number(v, null); }
    /// <inheritdoc cref="AppendFormatted(int?, string?)"/>
    public void AppendFormatted(long? value, string? format) { if (value is { } v) Number(v, format); }
    /// <summary>Called by the compiler for a number with a fraction that may be null; null writes nothing.</summary>
    public void AppendFormatted(float? value) { if (value is { } v) Number(v, null); }
    /// <summary>Called by the compiler for a number with a fraction that may be null, with a format; null writes nothing.</summary>
    public void AppendFormatted(float? value, string? format) { if (value is { } v) Number(v, format); }
    /// <inheritdoc cref="AppendFormatted(float?)"/>
    public void AppendFormatted(double? value) { if (value is { } v) Number(v, null); }
    /// <inheritdoc cref="AppendFormatted(float?, string?)"/>
    public void AppendFormatted(double? value, string? format) { if (value is { } v) Number(v, format); }

    /// <summary>Called by the compiler for a value of any other type.</summary>
    public void AppendFormatted<T>(T value) => AppendFormatted(value, format: null);

    /// <summary>Called by the compiler for a value of any other type with a format (<c>{value:format}</c>).</summary>
    public void AppendFormatted<T>(T value, string? format)
    {
        // The numbers a game shows, by their type: written without a box whatever the runtime has compiled so far.
        if (typeof(T) == typeof(int)) { Number(Unsafe.As<T, int>(ref value), format); return; }
        if (typeof(T) == typeof(float)) { Number(Unsafe.As<T, float>(ref value), format); return; }
        if (typeof(T) == typeof(long)) { Number(Unsafe.As<T, long>(ref value), format); return; }
        if (typeof(T) == typeof(double)) { Number(Unsafe.As<T, double>(ref value), format); return; }
        if (typeof(T) == typeof(uint)) { Number(Unsafe.As<T, uint>(ref value), format); return; }
        if (typeof(T) == typeof(ulong)) { Number(Unsafe.As<T, ulong>(ref value), format); return; }
        if (typeof(T) == typeof(short)) { Number(Unsafe.As<T, short>(ref value), format); return; }
        if (typeof(T) == typeof(ushort)) { Number(Unsafe.As<T, ushort>(ref value), format); return; }
        if (typeof(T) == typeof(byte)) { Number(Unsafe.As<T, byte>(ref value), format); return; }
        if (typeof(T) == typeof(sbyte)) { Number(Unsafe.As<T, sbyte>(ref value), format); return; }
        if (typeof(T) == typeof(char) && format is null) { AppendFormatted(Unsafe.As<T, char>(ref value)); return; }
        if (typeof(T) == typeof(bool)) { AppendFormatted(Unsafe.As<T, bool>(ref value)); return; }
        if (typeof(T) == typeof(int?)) { AppendFormatted(Unsafe.As<T, int?>(ref value), format); return; }
        if (typeof(T) == typeof(float?)) { AppendFormatted(Unsafe.As<T, float?>(ref value), format); return; }
        if (typeof(T) == typeof(long?)) { AppendFormatted(Unsafe.As<T, long?>(ref value), format); return; }
        if (typeof(T) == typeof(double?)) { AppendFormatted(Unsafe.As<T, double?>(ref value), format); return; }
        if (typeof(T) == typeof(TimeSpan)) { Number(Unsafe.As<T, TimeSpan>(ref value), format); return; }
        if (typeof(T) == typeof(DateTime)) { Number(Unsafe.As<T, DateTime>(ref value), format); return; }
        if (typeof(T) == typeof(decimal)) { Number(Unsafe.As<T, decimal>(ref value), format); return; }
        if (typeof(T) == typeof(Guid)) { Number(Unsafe.As<T, Guid>(ref value), format); return; }
        // An enum by its name: the name of each value is found once and kept.
        if (typeof(T).IsEnum && format is null) { Append(EnumNames<T>.Of(value)); return; }
        if (value is ISpanFormattable)
        {
            int written;
            while (!((ISpanFormattable)value).TryFormat(Chars.AsSpan(_length), out written, format, null)) Grow(Chars.Length);
            _length += written;
            return;
        }
        Append(value is IFormattable formattable ? formattable.ToString(format, null) : value?.ToString());
    }

    /// <summary>Called by the compiler for a value with a width (<c>{value,6}</c> right-aligned, <c>{value,-6}</c> left-aligned).</summary>
    public void AppendFormatted<T>(T value, int alignment, string? format = null)
    {
        int start = _length;
        AppendFormatted(value, format);
        Pad(start, alignment);
    }

    /// <summary>The names of an enum's values as its <c>ToString</c> gives them, each found the first time it is shown.</summary>
#pragma warning disable CS8714 // only enums come here, and an enum is never null
    private static class EnumNames<T>
    {
        private const int Kept = 256;
        [ThreadStatic] private static Dictionary<T, string>? t_names;

        public static string Of(T value)
        {
            var names = t_names ??= [];
            if (names.TryGetValue(value, out var name)) return name;
            name = value!.ToString() ?? "";
            // An enum used as a set of flags can show many combinations; past a few hundred they are not kept.
            if (names.Count < Kept) names[value] = name;
            return name;
        }
    }
#pragma warning restore CS8714

    private readonly char[] Chars => Live ? _chars!
        : throw new ObjectDisposedException(nameof(InterpolatedText), "The text was already used: write the interpolated string where it is drawn, once.");

    private void Number<T>(T value, string? format) where T : ISpanFormattable
    {
        int written;
        while (!value.TryFormat(Chars.AsSpan(_length), out written, format, null)) Grow(Chars.Length);
        _length += written;
    }

    private void Append(ReadOnlySpan<char> value)
    {
        if (value.Length > Chars.Length - _length) Grow(value.Length);
        value.CopyTo(_chars.AsSpan(_length));
        _length += value.Length;
    }

    /// <summary>Widens what was written from <paramref name="start"/> to |<paramref name="alignment"/>| characters with spaces: before it when positive, after it when negative.</summary>
    private void Pad(int start, int alignment)
    {
        int width = Math.Abs(alignment), written = _length - start, pad = width - written;
        if (pad <= 0) return;
        if (pad > Chars.Length - _length) Grow(pad);
        var chars = _chars.AsSpan();
        if (alignment > 0)
        {
            chars.Slice(start, written).CopyTo(chars[(start + pad)..]);
            chars.Slice(start, pad).Fill(' ');
        }
        else chars.Slice(_length, pad).Fill(' ');
        _length += pad;
    }

    private void Grow(int needed)
    {
        var old = Chars;
        var larger = new char[Math.Max(old.Length * 2, _length + needed)];
        old.AsSpan(0, _length).CopyTo(larger);
        // The level keeps the larger buffer: the next text as long as this one does not grow.
        if (_level >= 0) t_buffers![_level] = larger;
        _chars = larger;
    }
}

/// <summary>
/// A text built from a value and built again only when the value changes: for text that cannot be written as an
/// interpolated string where it is drawn (a localized message with a number in it, a wrapped paragraph, a text a
/// widget keeps). While the value stays the same, <see cref="Get"/> returns the same string and allocates nothing; a
/// change costs one call of the format.
/// </summary>
/// <typeparam name="TKey">What the text is built from: a number, or a tuple of everything that changes it (<c>(score, Text.Version)</c>).</typeparam>
/// <example>
/// <code>
/// private CachedText&lt;(int Coins, int Language)&gt; _coins;
///
/// // in Draw: built again when the count or the language changes
/// string line = _coins.Get((coins, Text.Version), Text, static (text, key) => text.Format("hud.coins", ("count", key.Coins)));
/// Font.Draw(r, line, new Vector2(8, 8), Color.White);
/// </code>
/// </example>
public struct CachedText<TKey> where TKey : IEquatable<TKey>
{
    private string? _text;
    private TKey _key;

    /// <summary>The text of <paramref name="key"/>: the one kept when the key is the one it was built from, else <paramref name="format"/>'s.</summary>
    /// <param name="key">What the text is built from.</param>
    /// <param name="format">Builds the text; pass a <c>static</c> lambda so the call allocates nothing.</param>
    public string Get(TKey key, Func<TKey, string> format)
    {
        ArgumentNullException.ThrowIfNull(format);
        if (_text is null || !key.Equals(_key)) (_text, _key) = (format(key), key);
        return _text;
    }

    /// <summary>As <see cref="Get(TKey, Func{TKey, string})"/>, with something the format needs that is not part of the key (the localizer, a font).</summary>
    public string Get<TState>(TKey key, TState state, Func<TState, TKey, string> format)
    {
        ArgumentNullException.ThrowIfNull(format);
        if (_text is null || !key.Equals(_key)) (_text, _key) = (format(state, key), key);
        return _text;
    }

    /// <summary>Forgets the text, so the next <see cref="Get(TKey, Func{TKey, string})"/> builds it.</summary>
    public void Clear() => _text = null;
}

using System.Buffers;
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
/// (<c>{time:0.0}</c>), alignment (<c>{score,6}</c>) and culture. Numbers, characters, strings and spans of characters
/// are written without allocating; a value of another type that is not <see cref="ISpanFormattable"/> is written
/// through its <c>ToString</c>, which allocates.
/// </para>
/// <para>
/// The compiler makes it: write the interpolated string where the parameter is, and do not keep the value. A method
/// that receives one reads <see cref="Text"/> and then calls <see cref="Dispose"/> once.
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
    private char[]? _chars;
    private int _length;

    /// <summary>Called by the compiler: <paramref name="literalLength"/> characters of fixed text and <paramref name="formattedCount"/> values.</summary>
    public InterpolatedText(int literalLength, int formattedCount)
    {
        _chars = ArrayPool<char>.Shared.Rent(Math.Max(64, literalLength + formattedCount * 12));
        _length = 0;
    }

    /// <summary>The text written so far. Valid until <see cref="Dispose"/>.</summary>
    public readonly ReadOnlySpan<char> Text => _chars.AsSpan(0, _length);

    /// <summary>Gives the buffer back. Called once by the method that received the text; the text must not be read afterwards.</summary>
    public void Dispose()
    {
        if (_chars is { } chars) ArrayPool<char>.Shared.Return(chars);
        _chars = null;
        _length = 0;
    }

    /// <summary>The text as a string (allocates): for a caller that keeps it.</summary>
    public override readonly string ToString() => Text.ToString();

    /// <summary>Called by the compiler for the fixed text between two values.</summary>
    public void AppendLiteral(string value) => Append(value);

    /// <summary>Called by the compiler for a string value; null writes nothing.</summary>
    public void AppendFormatted(string? value) => Append(value);
    /// <summary>Called by the compiler for a span of characters.</summary>
    public void AppendFormatted(ReadOnlySpan<char> value) => Append(value);
    /// <summary>Called by the compiler for a character.</summary>
    public void AppendFormatted(char value)
    {
        if (_length == Chars.Length) Grow(1);
        _chars![_length++] = value;
    }

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
        if (typeof(T) == typeof(char) && format is null) { AppendFormatted(Unsafe.As<T, char>(ref value)); return; }
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

    /// <summary>Called by the compiler for a string with a width.</summary>
    public void AppendFormatted(string? value, int alignment, string? format = null)
    {
        int start = _length;
        Append(value);
        Pad(start, alignment);
    }

    private readonly char[] Chars => _chars ?? throw new ObjectDisposedException(nameof(InterpolatedText), "The text was already used: write the interpolated string where it is drawn.");

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
        var larger = ArrayPool<char>.Shared.Rent(Math.Max(old.Length * 2, _length + needed));
        old.AsSpan(0, _length).CopyTo(larger);
        ArrayPool<char>.Shared.Return(old);
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

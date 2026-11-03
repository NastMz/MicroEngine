using System.Buffers.Binary;
using System.Numerics;

namespace MicroEngine.Kernel;

/// <summary>
/// Something whose simulation state takes part in the per-step state hash (<see cref="StateHasher"/>): it adds the
/// values that decide what the game does next, and leaves out what only drawing uses.
/// </summary>
public interface IStateHashed
{
    /// <summary>Adds this object's simulation state to <paramref name="hash"/>, always in the same order.</summary>
    void HashState(StateHasher hash);
}

/// <summary>The hash of one named part of the state of a step (<see cref="StepHash.Sections"/>).</summary>
/// <param name="Name">The section's name (<c>engine.time</c>, <c>scene[0]:PlayScene</c>, <c>game</c>...).</param>
/// <param name="Hash">The 64-bit hash of the values added to it.</param>
public readonly record struct SectionHash(string Name, ulong Hash);

/// <summary>
/// Accumulates a 64-bit hash (FNV-1a) of simulation state in named sections. Values are encoded in a fixed binary form
/// (little-endian; floats by their bits, so <c>-0</c> and NaN payloads count; strings length-prefixed), so the same
/// values in the same order give the same hash on every machine and run. Nothing is read by reflection: the engine and
/// the game add what they choose (<see cref="IStateHashed"/>).
/// </summary>
/// <remarks>
/// A section lasts until the next <see cref="Section"/>; values added before the first one form a section with an empty
/// name. The whole hash covers every value and every section name; each section's hash covers only its own values, so
/// two runs that differ can be told apart by the section that changed.
/// </remarks>
public sealed class StateHasher
{
    private const ulong Offset = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;
    private const byte SectionMarker = 0xfe;

    private ulong _value = Offset;
    private ulong _section = Offset;
    private string? _name;
    private bool _open;
    private readonly List<SectionHash> _sections = [];

    /// <summary>The hash of everything added so far.</summary>
    public ulong Value => _value;

    /// <summary>The hash of each section so far, in the order they were started (the current one included); a new list each time.</summary>
    public IReadOnlyList<SectionHash> Sections
    {
        get
        {
            var all = new SectionHash[_sections.Count + (_open ? 1 : 0)];
            _sections.CopyTo(all);
            if (_open) all[^1] = new SectionHash(_name ?? "", _section);
            return all;
        }
    }

    /// <summary>Forgets everything added, to hash another step with the same instance.</summary>
    public void Reset()
    {
        _value = Offset;
        _section = Offset;
        _name = null;
        _open = false;
        _sections.Clear();
    }

    /// <summary>Starts the section <paramref name="name"/>: the values added next are hashed under it.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> holds a tab or a line break (traces are written one step per line).</exception>
    public void Section(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name.AsSpan().IndexOfAny('\t', '\n', '\r') >= 0)
            throw new ArgumentException("A section name cannot hold tabs or line breaks.", nameof(name));
        Close();
        _name = name;
        _open = true;
        _value = Mix(_value, SectionMarker);
        foreach (char c in name) _value = Mix(Mix(_value, (byte)c), (byte)(c >> 8));
        _value = Mix(_value, SectionMarker);
    }

    private void Close()
    {
        if (_open) _sections.Add(new SectionHash(_name ?? "", _section));
        _section = Offset;
    }

    private static ulong Mix(ulong hash, byte value) => (hash ^ value) * Prime;

    private void Write(ReadOnlySpan<byte> bytes)
    {
        _open = true;
        ulong value = _value, section = _section;
        foreach (byte b in bytes)
        {
            value = Mix(value, b);
            section = Mix(section, b);
        }
        _value = value;
        _section = section;
    }

    /// <summary>Adds a byte.</summary>
    public void Add(byte value) => Write([value]);

    /// <summary>Adds a boolean (one byte, 0 or 1).</summary>
    public void Add(bool value) => Write([value ? (byte)1 : (byte)0]);

    /// <summary>Adds a 32-bit integer.</summary>
    public void Add(int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        Write(bytes);
    }

    /// <summary>Adds a 64-bit integer.</summary>
    public void Add(long value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
        Write(bytes);
    }

    /// <summary>Adds an unsigned 64-bit integer.</summary>
    public void Add(ulong value) => Add(unchecked((long)value));

    /// <summary>Adds a float by its bits: <c>0</c> and <c>-0</c>, or two NaNs with different payloads, are different values.</summary>
    public void Add(float value) => Add(BitConverter.SingleToInt32Bits(value));

    /// <summary>Adds a double by its bits.</summary>
    public void Add(double value) => Add(BitConverter.DoubleToInt64Bits(value));

    /// <summary>Adds both components of a vector, x first.</summary>
    public void Add(Vector2 value)
    {
        Add(value.X);
        Add(value.Y);
    }

    /// <summary>Adds a string as its length then its UTF-16 code units; null is told apart from empty.</summary>
    public void Add(string? value)
    {
        if (value is null) { Add(-1); return; }
        Add(value.Length);
        Span<byte> unit = stackalloc byte[2];
        foreach (char c in value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(unit, c);
            Write(unit);
        }
    }

    /// <summary>Adds raw bytes, preceded by their count (tile maps, grids and other bulk state).</summary>
    public void Add(ReadOnlySpan<byte> bytes)
    {
        Add(bytes.Length);
        Write(bytes);
    }

    /// <summary>Adds an object's own state (<see cref="IStateHashed.HashState"/>), preceded by whether it is there.</summary>
    public void Add(IStateHashed? value)
    {
        Add(value is not null);
        value?.HashState(this);
    }
}

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;


namespace MicroEngine.Tiled;

/// <summary>The type of a custom property, as Tiled names it.</summary>
public enum TiledPropertyType
{
    /// <summary>Text: the value is a <see cref="string"/>.</summary>
    String,
    /// <summary>A whole number: the value is an <see cref="int"/>.</summary>
    Int,
    /// <summary>A number: the value is a <see cref="float"/>.</summary>
    Float,
    /// <summary>True or false: the value is a <see cref="bool"/>.</summary>
    Bool,
    /// <summary>A colour: the value is a <see cref="MicroEngine.Color"/>.</summary>
    Color,
    /// <summary>A file: the value is its content path (a <see cref="string"/>, relative to the content root).</summary>
    File,
    /// <summary>A reference to another object: the value is its id (an <see cref="int"/>, 0 for none).</summary>
    Object,
    /// <summary>A custom enum: the value is a <see cref="string"/> (several flags are joined with commas).</summary>
    Enum,
    /// <summary>A custom class: the value is a <see cref="TiledProperties"/> of its members.</summary>
    Class,
}

/// <summary>One custom property: its name, type and value (see <see cref="TiledPropertyType"/> for the value's type).</summary>
/// <param name="Name">The property's name.</param>
/// <param name="Type">The kind of value.</param>
/// <param name="Value">The value.</param>
/// <param name="CustomType">The name of the custom enum or class, for <see cref="TiledPropertyType.Enum"/> and <see cref="TiledPropertyType.Class"/>; otherwise null.</param>
public sealed record TiledProperty(string Name, TiledPropertyType Type, object Value, string? CustomType = null);

/// <summary>
/// The custom properties of a map, layer, tileset, tile or object, read as data: the engine gives no property a
/// meaning. Each getter returns its fallback when the property is missing, and throws when it exists with another type.
/// </summary>
public sealed class TiledProperties : IReadOnlyCollection<TiledProperty>
{
    private readonly Dictionary<string, TiledProperty> _byName;

    /// <summary>No properties.</summary>
    public static TiledProperties Empty { get; } = new([]);

    /// <summary>Creates a set of properties; a name given twice keeps its last value.</summary>
    public TiledProperties(IEnumerable<TiledProperty> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        _byName = new Dictionary<string, TiledProperty>(StringComparer.Ordinal);
        foreach (var p in properties) _byName[p.Name] = p;
    }

    /// <summary>How many properties there are.</summary>
    public int Count => _byName.Count;

    /// <summary>Whether a property called <paramref name="name"/> exists.</summary>
    public bool Has(string name) => _byName.ContainsKey(name);

    /// <summary>The property called <paramref name="name"/>, if it exists.</summary>
    public bool TryGet(string name, [NotNullWhen(true)] out TiledProperty? property) => _byName.TryGetValue(name, out property);

    /// <summary>The property called <paramref name="name"/>.</summary>
    /// <exception cref="KeyNotFoundException">There is none; the message lists those there are.</exception>
    public TiledProperty this[string name] => _byName.TryGetValue(name, out var p) ? p
        : throw new KeyNotFoundException($"No property '{name}'. Properties: {(Count == 0 ? "none" : string.Join(", ", _byName.Keys.Order(StringComparer.Ordinal)))}.");

    /// <summary>A text, enum or file property.</summary>
    /// <exception cref="InvalidCastException">The property holds another type.</exception>
    public string? String(string name, string? fallback = null) =>
        Get(name, fallback, TiledPropertyType.String, TiledPropertyType.Enum, TiledPropertyType.File);

    /// <summary>A whole-number or object property.</summary>
    /// <exception cref="InvalidCastException">The property holds another type.</exception>
    public int Int(string name, int fallback = 0) => Get(name, fallback, TiledPropertyType.Int, TiledPropertyType.Object);

    /// <summary>A number property; a whole-number property converts.</summary>
    /// <exception cref="InvalidCastException">The property holds another type.</exception>
    public float Float(string name, float fallback = 0) =>
        _byName.TryGetValue(name, out var p) && p.Type == TiledPropertyType.Int ? (int)p.Value : Get(name, fallback, TiledPropertyType.Float);

    /// <summary>A true-or-false property.</summary>
    /// <exception cref="InvalidCastException">The property holds another type.</exception>
    public bool Bool(string name, bool fallback = false) => Get(name, fallback, TiledPropertyType.Bool);

    /// <summary>A colour property.</summary>
    /// <exception cref="InvalidCastException">The property holds another type.</exception>
    public MicroEngine.Color Color(string name, MicroEngine.Color fallback = default) => Get(name, fallback, TiledPropertyType.Color);

    /// <summary>A class property's members (<see cref="Empty"/> when it is missing).</summary>
    /// <exception cref="InvalidCastException">The property holds another type.</exception>
    public TiledProperties Class(string name) => Get(name, Empty, TiledPropertyType.Class);

    private T Get<T>(string name, T fallback, params TiledPropertyType[] types)
    {
        if (!_byName.TryGetValue(name, out var p)) return fallback;
        if (!types.Contains(p.Type) || p.Value is not T value)
            throw new InvalidCastException($"Property '{name}' is {p.Type}, not {string.Join(" or ", types)}.");
        return value;
    }

    /// <inheritdoc/>
    public IEnumerator<TiledProperty> GetEnumerator() => _byName.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>The properties as "name=value" pairs, for diagnostics.</summary>
    public override string ToString() =>
        string.Join(", ", _byName.Values.Select(p => $"{p.Name}={Convert.ToString(p.Value, CultureInfo.InvariantCulture)}"));
}

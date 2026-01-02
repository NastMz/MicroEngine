using System.Diagnostics.CodeAnalysis;
using MicroEngine.Core.Exceptions;

namespace MicroEngine.Core.State;

/// <summary>
/// Zero-allocation tag identifier for efficient string-based lookups.
/// Replaces string comparisons with fast hash-based comparisons.
/// </summary>
/// <remarks>
/// Tags are immutable, hashable identifiers derived from strings. The Tag system
/// provides the following benefits:
/// - Zero heap allocation: once constructed, Tag is a value type living on the stack
/// - Fast comparison: O(1) integer comparison instead of string comparison
/// - Memory efficient: stored as a single int instead of string reference
/// - Cache-friendly: primitive value type fits in CPU cache lines
///
/// Example usage in animation systems:
/// <code>
/// var idleTag = new Tag("idle");
/// var attackTag = new Tag("attack");
///
/// if (currentTag == idleTag)
/// {
///     // Fast branch prediction-friendly comparison
/// }
/// </code>
/// </remarks>
public readonly struct Tag : IEquatable<Tag>, IComparable<Tag>
{
    private static readonly Dictionary<string, int> _tagRegistry = new(StringComparer.Ordinal);
    private static int _nextId = 1;
    private static readonly object _registryLock = new();

    /// <summary>
    /// Gets the hash ID of this tag.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Creates a new tag from a string identifier.
    /// Tags with the same string will produce the same ID.
    /// Subsequent calls are O(1) lookup in the registry.
    /// </summary>
    /// <param name="name">String identifier for the tag. Must not be null or empty.</param>
    /// <exception cref="ArgumentException">Thrown if name is null or empty.</exception>
    public Tag(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Tag name cannot be null or empty.", nameof(name));
        }

        lock (_registryLock)
        {
            if (!_tagRegistry.TryGetValue(name, out int id))
            {
                id = _nextId++;
                _tagRegistry[name] = id;
            }

            Id = id;
        }
    }

    /// <summary>
    /// Gets the string representation of this tag.
    /// Note: This requires a reverse lookup which is O(n) where n is the number of tags.
    /// Use this only for debugging; prefer Tag equality for runtime checks.
    /// </summary>
    public override string ToString()
    {
        lock (_registryLock)
        {
            int idValue = Id;
            var entry = _tagRegistry.FirstOrDefault(x => x.Value == idValue);
            return entry.Key ?? $"<Unknown Tag {idValue}>";
        }
    }

    /// <summary>
    /// Returns the hash code for this tag.
    /// </summary>
    public override int GetHashCode() => Id;

    /// <summary>
    /// Compares this tag with another object for equality.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is Tag tag && Equals(tag);
    }

    /// <summary>
    /// Compares this tag with another tag for equality.
    /// This is a simple integer comparison, making it extremely fast.
    /// </summary>
    public bool Equals(Tag other)
    {
        return Id == other.Id;
    }

    /// <summary>
    /// Compares this tag with another for ordering.
    /// </summary>
    public int CompareTo(Tag other)
    {
        return Id.CompareTo(other.Id);
    }

    /// <summary>
    /// Determines if two tags are equal.
    /// </summary>
    public static bool operator ==(Tag left, Tag right) => left.Equals(right);

    /// <summary>
    /// Determines if two tags are not equal.
    /// </summary>
    public static bool operator !=(Tag left, Tag right) => !left.Equals(right);

    /// <summary>
    /// Determines if a tag is less than another.
    /// </summary>
    public static bool operator <(Tag left, Tag right) => left.Id < right.Id;

    /// <summary>
    /// Determines if a tag is greater than another.
    /// </summary>
    public static bool operator >(Tag left, Tag right) => left.Id > right.Id;

    /// <summary>
    /// Determines if a tag is less than or equal to another.
    /// </summary>
    public static bool operator <=(Tag left, Tag right) => left.Id <= right.Id;

    /// <summary>
    /// Determines if a tag is greater than or equal to another.
    /// </summary>
    public static bool operator >=(Tag left, Tag right) => left.Id >= right.Id;

    /// <summary>
    /// Clears the internal tag registry.
    /// WARNING: Use only in tests or when you need to reset all tags.
    /// Existing Tag instances will have stale IDs.
    /// </summary>
    internal static void ClearRegistry()
    {
        lock (_registryLock)
        {
            _tagRegistry.Clear();
            _nextId = 1;
        }
    }

    /// <summary>
    /// Gets the total number of registered tags.
    /// Useful for profiling and debugging.
    /// </summary>
    internal static int RegistrySize
    {
        get
        {
            lock (_registryLock)
            {
                return _tagRegistry.Count;
            }
        }
    }
}

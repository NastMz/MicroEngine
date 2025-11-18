namespace MicroEngine.Core.ECS;

/// <summary>
/// Identifier for an archetype based on its component type composition.
/// </summary>
internal readonly struct ArchetypeId : IEquatable<ArchetypeId>
{
    private readonly int _hashCode;

    public ArchetypeId(IEnumerable<Type> componentTypes)
    {
        var sorted = componentTypes.OrderBy(t => t.FullName).ToArray();
        _hashCode = CalculateHash(sorted);
    }

    private static int CalculateHash(Type[] types)
    {
        var hash = new HashCode();
        foreach (var type in types)
        {
            hash.Add(type);
        }
        return hash.ToHashCode();
    }

    public bool Equals(ArchetypeId other) => _hashCode == other._hashCode;
    public override bool Equals(object? obj) => obj is ArchetypeId other && Equals(other);
    public override int GetHashCode() => _hashCode;
}

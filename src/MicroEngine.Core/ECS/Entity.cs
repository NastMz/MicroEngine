namespace MicroEngine.Core.ECS;

/// <summary>
/// Represents a unique entity in the ECS world.
/// Entities are lightweight identifiers with versioning for safety.
/// </summary>
public readonly struct Entity : IEquatable<Entity>
{
    /// <summary>
    /// Unique identifier for this entity.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Version number to detect use-after-free bugs.
    /// Increments each time an entity with this ID is destroyed and recreated.
    /// </summary>
    public uint Version { get; }

    /// <summary>
    /// Represents a null/invalid entity.
    /// </summary>
    public static readonly Entity Null = new(0, 0);

    /// <summary>
    /// Initializes a new entity with the specified ID and version.
    /// </summary>
    public Entity(uint id, uint version)
    {
        Id = id;
        Version = version;
    }

    /// <inheritdoc/>
    public bool Equals(Entity other)
    {
        return Id == other.Id && Version == other.Version;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is Entity other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Version);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Entity({Id}v{Version})";
    }

    /// <summary>
    /// Equality operator for entities.
    /// </summary>
    public static bool operator ==(Entity left, Entity right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator for entities.
    /// </summary>
    public static bool operator !=(Entity left, Entity right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Checks if this entity is the null entity.
    /// </summary>
    public bool IsNull => Id == 0;
}

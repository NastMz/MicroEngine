namespace MicroEngine.Core.Resources;

/// <summary>
/// Represents a unique identifier for a resource.
/// </summary>
public readonly struct ResourceId : IEquatable<ResourceId>
{
    /// <summary>
    /// Gets the unique identifier value.
    /// </summary>
    public uint Value { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceId"/> struct.
    /// </summary>
    public ResourceId(uint value)
    {
        Value = value;
    }

    /// <summary>
    /// Invalid resource ID (0).
    /// </summary>
    public static readonly ResourceId Invalid = new(0);

    /// <inheritdoc/>
    public bool Equals(ResourceId other) => Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ResourceId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();

    /// <inheritdoc/>
    public override string ToString() => $"ResourceId({Value})";

    /// <summary>Equality operator.</summary>
    public static bool operator ==(ResourceId left, ResourceId right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(ResourceId left, ResourceId right) => !left.Equals(right);
}

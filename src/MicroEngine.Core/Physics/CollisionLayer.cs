namespace MicroEngine.Core.Physics;

/// <summary>
/// Represents a collision layer for physics filtering.
/// Layers are identified by an ID (0-31) and can be used to control which objects collide.
/// </summary>
/// <remarks>
/// The 32-layer limit is a deliberate design decision based on:
/// - Performance: int masks (32 bits) enable fast bitwise operations
/// - Industry standard: Unity, Unreal, Godot use 32 layers for the same reason
/// - Sufficiency: 32 layers cover virtually all game scenarios
/// - Memory efficiency: Collision matrix fits in 128 bytes (32 x uint)
/// </remarks>
public readonly struct CollisionLayer : IEquatable<CollisionLayer>
{
    /// <summary>
    /// Minimum valid layer ID.
    /// </summary>
    public const int MINIMUM_LAYER_ID = 0;

    /// <summary>
    /// Maximum valid layer ID.
    /// </summary>
    public const int MAXIMUM_LAYER_ID = 31;

    /// <summary>
    /// Total number of supported layers.
    /// </summary>
    public const int TOTAL_LAYER_COUNT = 32;

    /// <summary>
    /// Gets the layer ID (0-31).
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the layer name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CollisionLayer"/> struct.
    /// </summary>
    /// <param name="id">Layer ID (0-31).</param>
    /// <param name="name">Layer name.</param>
    /// <exception cref="ArgumentException">Thrown when id is out of range (0-31).</exception>
    public CollisionLayer(int id, string name)
    {
        if (id < MINIMUM_LAYER_ID || id > MAXIMUM_LAYER_ID)
        {
            throw new ArgumentException($"Layer ID must be between {MINIMUM_LAYER_ID} and {MAXIMUM_LAYER_ID}.", nameof(id));
        }

        Id = id;
        Name = name ?? string.Empty;
    }

    /// <summary>
    /// Gets the bit mask for this layer.
    /// </summary>
    /// <returns>Bit mask where only the bit corresponding to this layer's ID is set.</returns>
    public int GetMask() => 1 << Id;

    /// <inheritdoc/>
    public bool Equals(CollisionLayer other) => Id == other.Id;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CollisionLayer other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(CollisionLayer left, CollisionLayer right) => left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(CollisionLayer left, CollisionLayer right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => $"{Name} (Layer {Id})";
}

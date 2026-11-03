namespace Arch1.Scene;

/// <summary>
/// An entity of an <see cref="IWorld"/>: a slot index and the generation of the entity that held the slot when the id
/// was made. A destroyed entity's slot is reused with a new generation, so an old id never reaches the new entity.
/// <c>default</c> is <see cref="None"/> (generation 0 is never issued).
/// </summary>
public readonly record struct EntityId(uint Index, uint Generation)
{
    /// <summary>The id of no entity.</summary>
    public static EntityId None => default;
    /// <summary>Whether this is <see cref="None"/>.</summary>
    public bool IsNone => Generation == 0;
    /// <inheritdoc/>
    public override string ToString() => IsNone ? "entity:none" : $"entity:{Index}#{Generation}";
}

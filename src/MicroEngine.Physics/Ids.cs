namespace MicroEngine.Physics;

/// <summary>
/// A body of an <see cref="IPhysicsWorld2D"/>: a slot index and the generation of the body that held the slot when the
/// id was made. A destroyed body's slot is reused with a new generation, so an old id never reaches the new body.
/// <c>default</c> is <see cref="None"/> (generation 0 is never issued).
/// </summary>
/// <param name="Index">The slot.</param>
/// <param name="Generation">The generation of the slot's body; 0 only for <see cref="None"/>.</param>
public readonly record struct BodyId(uint Index, uint Generation)
{
    /// <summary>The id of no body.</summary>
    public static BodyId None => default;
    /// <summary>Whether this is <see cref="None"/>.</summary>
    public bool IsNone => Generation == 0;
    /// <inheritdoc/>
    public override string ToString() => IsNone ? "body:none" : $"body:{Index}#{Generation}";
}

/// <summary>
/// A shape of an <see cref="IPhysicsWorld2D"/> (a box, a tile grid, or a shape of a declared capability), attached to one
/// body. Generation-checked like <see cref="BodyId"/>; <c>default</c> is <see cref="None"/>.
/// </summary>
/// <param name="Index">The slot.</param>
/// <param name="Generation">The generation of the slot's shape; 0 only for <see cref="None"/>.</param>
public readonly record struct ShapeId(uint Index, uint Generation)
{
    /// <summary>The id of no shape.</summary>
    public static ShapeId None => default;
    /// <summary>Whether this is <see cref="None"/>.</summary>
    public bool IsNone => Generation == 0;
    /// <inheritdoc/>
    public override string ToString() => IsNone ? "shape:none" : $"shape:{Index}#{Generation}";
}

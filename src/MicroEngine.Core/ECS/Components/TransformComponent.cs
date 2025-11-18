using MicroEngine.Core.Math;

namespace MicroEngine.Core.ECS.Components;

/// <summary>
/// Component representing spatial transformation (position, rotation, scale).
/// Pure data component - contains no logic.
/// Logic is handled by MovementSystem and other systems.
/// </summary>
public struct TransformComponent : IComponent
{
    /// <summary>
    /// Gets or sets the position in world space.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Gets or sets the rotation angle in radians.
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// Gets or sets the scale factor.
    /// A scale of (1, 1) represents original size.
    /// </summary>
    public Vector2 Scale { get; set; }

    /// <summary>
    /// Gets or sets the local offset from the position (origin/pivot point).
    /// This is useful for rotation around a specific point.
    /// </summary>
    public Vector2 Origin { get; set; }
}

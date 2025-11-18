using MicroEngine.Core.Math;

namespace MicroEngine.Core.ECS.Components;

/// <summary>
/// Defines the shape type for collision detection.
/// </summary>
public enum ColliderShape
{
    /// <summary>Circle collider.</summary>
    Circle,
    
    /// <summary>Axis-aligned bounding box (AABB).</summary>
    Rectangle,
    
    /// <summary>Point collider (zero size).</summary>
    Point
}

/// <summary>
/// Component for collision detection data.
/// Pure data component - contains no logic.
/// Logic is handled by CollisionSystem.
/// </summary>
public struct ColliderComponent : IComponent
{
    /// <summary>
    /// Gets or sets the shape type of the collider.
    /// </summary>
    public ColliderShape Shape { get; set; }

    /// <summary>
    /// Gets or sets the size/radius of the collider.
    /// For Circle: X is radius (Y ignored).
    /// For Rectangle: X is width, Y is height.
    /// For Point: Ignored.
    /// </summary>
    public Vector2 Size { get; set; }

    /// <summary>
    /// Gets or sets the offset from the transform position.
    /// </summary>
    public Vector2 Offset { get; set; }

    /// <summary>
    /// Gets or sets whether the collider is a trigger (no physics response).
    /// Triggers generate collision events but don't block movement.
    /// </summary>
    public bool IsTrigger { get; set; }

    /// <summary>
    /// Gets or sets whether the collider is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the collision layer mask.
    /// Used for filtering which colliders can interact.
    /// </summary>
    public int LayerMask { get; set; }
}

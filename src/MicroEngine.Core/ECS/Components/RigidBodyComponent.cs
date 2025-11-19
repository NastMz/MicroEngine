using MicroEngine.Core.Math;

namespace MicroEngine.Core.ECS.Components;

/// <summary>
/// Component for basic 2D physics simulation data.
/// Pure data component - contains no logic.
/// Logic is handled by PhysicsSystem.
/// </summary>
public struct RigidBodyComponent : IComponent
{
    /// <summary>
    /// Gets or sets the velocity in units per second.
    /// </summary>
    public Vector2 Velocity { get; set; }

    /// <summary>
    /// Gets or sets the acceleration in units per second squared.
    /// </summary>
    public Vector2 Acceleration { get; set; }

    /// <summary>
    /// Gets or sets the mass of the body.
    /// Used in F=ma calculations.
    /// </summary>
    public float Mass { get; set; }

    /// <summary>
    /// Gets or sets the drag coefficient (air resistance).
    /// Value between 0 (no drag) and 1 (maximum drag).
    /// </summary>
    public float Drag { get; set; }

    /// <summary>
    /// Gets or sets the gravity scale.
    /// 1.0 means normal gravity, 0.0 means no gravity.
    /// </summary>
    public float GravityScale { get; set; }

    /// <summary>
    /// Gets or sets whether the body is kinematic (not affected by forces).
    /// </summary>
    public bool IsKinematic { get; set; }

    /// <summary>
    /// Gets or sets whether the body is affected by gravity.
    /// </summary>
    public bool UseGravity { get; set; }

    /// <summary>
    /// Gets or sets the restitution (bounciness) coefficient.
    /// Value between 0 (no bounce) and 1 (perfect bounce).
    /// </summary>
    public float Restitution { get; set; }

    /// <summary>
    /// Gets or sets whether to use continuous collision detection.
    /// Prevents fast-moving objects from tunneling through other objects.
    /// </summary>
    public bool UseContinuousCollision { get; set; }
}

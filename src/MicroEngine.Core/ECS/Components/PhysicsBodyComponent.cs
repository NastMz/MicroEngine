namespace MicroEngine.Core.ECS.Components;

/// <summary>
/// Links an ECS entity to a physics backend body.
/// Stores the handle returned by IPhysicsBackend.CreateBody().
/// </summary>
public record struct PhysicsBodyComponent : IComponent
{
    /// <summary>
    /// Handle to the physics body in the backend.
    /// -1 indicates no body has been created yet.
    /// </summary>
    public int BodyHandle { get; set; }

    /// <summary>
    /// Creates a new PhysicsBodyComponent with no body.
    /// </summary>
    public PhysicsBodyComponent()
    {
        BodyHandle = -1;
    }

    /// <summary>
    /// Creates a new PhysicsBodyComponent with the specified body handle.
    /// </summary>
    /// <param name="bodyHandle">The physics backend body handle.</param>
    public PhysicsBodyComponent(int bodyHandle)
    {
        BodyHandle = bodyHandle;
    }
}

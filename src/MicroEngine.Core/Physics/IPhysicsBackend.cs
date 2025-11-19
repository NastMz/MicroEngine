using MicroEngine.Core.Math;

namespace MicroEngine.Core.Physics;

/// <summary>
/// Backend-agnostic physics simulation interface.
/// Provides rigid body dynamics, collision detection, and resolution.
/// </summary>
public interface IPhysicsBackend
{
    /// <summary>
    /// Initializes the physics backend.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Shuts down the physics backend and releases resources.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Steps the physics simulation by the specified time delta.
    /// </summary>
    /// <param name="deltaTime">Time step in seconds.</param>
    void Step(float deltaTime);

    /// <summary>
    /// Sets the global gravity vector.
    /// </summary>
    /// <param name="gravity">Gravity vector (typically (0, positive) for downward).</param>
    void SetGravity(Vector2 gravity);

    /// <summary>
    /// Gets the current global gravity vector.
    /// </summary>
    /// <returns>The gravity vector.</returns>
    Vector2 GetGravity();

    /// <summary>
    /// Creates a rigid body in the physics world.
    /// </summary>
    /// <param name="position">Initial position.</param>
    /// <param name="mass">Mass (0 for static bodies).</param>
    /// <param name="isKinematic">Whether the body is kinematic (moved by code, not physics).</param>
    /// <returns>Handle to the created body.</returns>
    int CreateBody(Vector2 position, float mass, bool isKinematic = false);

    /// <summary>
    /// Destroys a rigid body.
    /// </summary>
    /// <param name="bodyHandle">Handle to the body to destroy.</param>
    void DestroyBody(int bodyHandle);

    /// <summary>
    /// Sets the body type (static, kinematic, or dynamic).
    /// </summary>
    /// <param name="bodyHandle">Handle to the body.</param>
    /// <param name="mass">Mass (0 for static).</param>
    /// <param name="isKinematic">Whether the body is kinematic.</param>
    void SetBodyType(int bodyHandle, float mass, bool isKinematic);

    /// <summary>
    /// Attaches a circle collider to a body.
    /// </summary>
    /// <param name="bodyHandle">Body handle.</param>
    /// <param name="radius">Circle radius.</param>
    /// <param name="offset">Offset from body position.</param>
    /// <param name="restitution">Bounciness (0-1).</param>
    /// <param name="friction">Surface friction (0-1).</param>
    void AddCircleCollider(int bodyHandle, float radius, Vector2 offset, float restitution = 0.5f, float friction = 0.3f);

    /// <summary>
    /// Attaches a box collider to a body.
    /// </summary>
    /// <param name="bodyHandle">Body handle.</param>
    /// <param name="width">Box width.</param>
    /// <param name="height">Box height.</param>
    /// <param name="offset">Offset from body position.</param>
    /// <param name="restitution">Bounciness (0-1).</param>
    /// <param name="friction">Surface friction (0-1).</param>
    void AddBoxCollider(int bodyHandle, float width, float height, Vector2 offset, float restitution = 0.5f, float friction = 0.3f);

    /// <summary>
    /// Gets the current position of a body.
    /// </summary>
    /// <param name="bodyHandle">Body handle.</param>
    /// <returns>The body's position.</returns>
    Vector2 GetBodyPosition(int bodyHandle);

    /// <summary>
    /// Sets the position of a body.
    /// </summary>
    /// <param name="bodyHandle">Body handle.</param>
    /// <param name="position">New position.</param>
    void SetBodyPosition(int bodyHandle, Vector2 position);

    /// <summary>
    /// Gets the current velocity of a body.
    /// </summary>
    /// <param name="bodyHandle">Body handle.</param>
    /// <returns>The body's velocity.</returns>
    Vector2 GetBodyVelocity(int bodyHandle);

    /// <summary>
    /// Sets the velocity of a body.
    /// </summary>
    /// <param name="bodyHandle">Body handle.</param>
    /// <param name="velocity">New velocity.</param>
    void SetBodyVelocity(int bodyHandle, Vector2 velocity);

    /// <summary>
    /// Applies a force to a body.
    /// </summary>
    /// <param name="bodyHandle">Body handle.</param>
    /// <param name="force">Force vector.</param>
    void ApplyForce(int bodyHandle, Vector2 force);

    /// <summary>
    /// Applies an impulse (instant velocity change) to a body.
    /// </summary>
    /// <param name="bodyHandle">Body handle.</param>
    /// <param name="impulse">Impulse vector.</param>
    void ApplyImpulse(int bodyHandle, Vector2 impulse);

    /// <summary>
    /// Sets whether a body is affected by gravity.
    /// </summary>
    /// <param name="bodyHandle">Body handle.</param>
    /// <param name="useGravity">True to enable gravity.</param>
    void SetUseGravity(int bodyHandle, bool useGravity);

    /// <summary>
    /// Sets the linear damping (air resistance) for a body.
    /// </summary>
    /// <param name="bodyHandle">Body handle.</param>
    /// <param name="damping">Damping factor (0-1).</param>
    void SetLinearDamping(int bodyHandle, float damping);
}

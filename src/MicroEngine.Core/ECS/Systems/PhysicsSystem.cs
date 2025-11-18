using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.Math;

namespace MicroEngine.Core.ECS.Systems;

/// <summary>
/// System that handles physics simulation for entities with RigidBodyComponent.
/// Applies forces, acceleration, velocity, and gravity.
/// </summary>
public sealed class PhysicsSystem : ISystem
{
    private const float DEFAULT_GRAVITY = 980f; // Pixels per second squared

    /// <summary>
    /// Gets or sets the gravity vector.
    /// Default is (0, 980) for downward gravity.
    /// </summary>
    public Vector2 Gravity { get; set; } = new Vector2(0f, DEFAULT_GRAVITY);

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicsSystem"/> class.
    /// </summary>
    public PhysicsSystem()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicsSystem"/> class with custom gravity.
    /// </summary>
    /// <param name="gravity">The gravity vector.</param>
    public PhysicsSystem(Vector2 gravity)
    {
        Gravity = gravity;
    }

    /// <summary>
    /// Updates all entities with RigidBodyComponent and TransformComponent.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="deltaTime">Time elapsed since last update in seconds.</param>
    public void Update(World world, float deltaTime)
    {
        var entities = world.GetAllEntities();

        foreach (var entity in entities)
        {
            if (!world.HasComponent<RigidBodyComponent>(entity) ||
                !world.HasComponent<TransformComponent>(entity))
            {
                continue;
            }

            ref var rigidBody = ref world.GetComponent<RigidBodyComponent>(entity);
            ref var transform = ref world.GetComponent<TransformComponent>(entity);

            // Skip kinematic bodies
            if (rigidBody.IsKinematic)
            {
                continue;
            }

            // Apply gravity
            var acceleration = rigidBody.Acceleration;
            if (rigidBody.UseGravity && rigidBody.GravityScale > 0)
            {
                acceleration = new Vector2(
                    acceleration.X + Gravity.X * rigidBody.GravityScale,
                    acceleration.Y + Gravity.Y * rigidBody.GravityScale
                );
            }

            // Update velocity from acceleration
            var velocity = new Vector2(
                rigidBody.Velocity.X + acceleration.X * deltaTime,
                rigidBody.Velocity.Y + acceleration.Y * deltaTime
            );

            // Apply drag
            if (rigidBody.Drag > 0)
            {
                var dragFactor = 1f - rigidBody.Drag * deltaTime;
                dragFactor = System.Math.Max(0f, dragFactor);
                velocity = new Vector2(velocity.X * dragFactor, velocity.Y * dragFactor);
            }

            // Update position from velocity
            var newPosition = new Vector2(
                transform.Position.X + velocity.X * deltaTime,
                transform.Position.Y + velocity.Y * deltaTime
            );

            // Update components by reference
            rigidBody.Velocity = velocity;
            rigidBody.Acceleration = Vector2.Zero; // Reset acceleration after applying
            
            transform.Position = newPosition;
        }
    }

    /// <summary>
    /// Applies a force to an entity.
    /// F = ma, so force is divided by mass to get acceleration.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="entity">The entity to apply force to.</param>
    /// <param name="force">The force vector.</param>
    public void ApplyForce(World world, Entity entity, Vector2 force)
    {
        if (!world.HasComponent<RigidBodyComponent>(entity))
        {
            return;
        }

        ref var rigidBody = ref world.GetComponent<RigidBodyComponent>(entity);
        
        if (rigidBody.IsKinematic || rigidBody.Mass <= 0f)
        {
            return;
        }

        var acceleration = new Vector2(force.X / rigidBody.Mass, force.Y / rigidBody.Mass);
        
        rigidBody.Acceleration = new Vector2(
            rigidBody.Acceleration.X + acceleration.X,
            rigidBody.Acceleration.Y + acceleration.Y);
    }

    /// <summary>
    /// Applies an impulse to an entity (instant velocity change).
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="entity">The entity to apply impulse to.</param>
    /// <param name="impulse">The impulse vector.</param>
    public void ApplyImpulse(World world, Entity entity, Vector2 impulse)
    {
        if (!world.HasComponent<RigidBodyComponent>(entity))
        {
            return;
        }

        ref var rigidBody = ref world.GetComponent<RigidBodyComponent>(entity);
        
        if (rigidBody.IsKinematic)
        {
            return;
        }

        rigidBody.Velocity = new Vector2(
            rigidBody.Velocity.X + impulse.X,
            rigidBody.Velocity.Y + impulse.Y);
    }

    /// <summary>
    /// Stops an entity's movement (sets velocity and acceleration to zero).
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="entity">The entity to stop.</param>
    public void Stop(World world, Entity entity)
    {
        if (!world.HasComponent<RigidBodyComponent>(entity))
        {
            return;
        }

        ref var rigidBody = ref world.GetComponent<RigidBodyComponent>(entity);

        rigidBody.Velocity = Vector2.Zero;
        rigidBody.Acceleration = Vector2.Zero;
    }
}

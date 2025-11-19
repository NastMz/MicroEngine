using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.Physics;

namespace MicroEngine.Core.ECS.Systems;

/// <summary>
/// Physics system that uses IPhysicsBackend for realistic physics simulation.
/// Manages synchronization between ECS entities and physics backend bodies.
/// </summary>
public sealed class PhysicsBackendSystem : ISystem
{
    private readonly IPhysicsBackend _backend;
    private bool _initialized;

    /// <summary>
    /// Creates a new PhysicsBackendSystem with the specified backend.
    /// </summary>
    /// <param name="backend">The physics backend to use.</param>
    public PhysicsBackendSystem(IPhysicsBackend backend)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
    }

    /// <summary>
    /// Initializes the physics backend.
    /// Should be called once before the first Update.
    /// </summary>
    public void Initialize(float gravity = 750f)
    {
        if (_initialized)
        {
            return;
        }

        _backend.Initialize();
        _backend.SetGravity(new Math.Vector2(0, gravity));
        _initialized = true;
    }

    /// <summary>
    /// Updates the physics simulation and synchronizes with ECS entities.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="deltaTime">Time elapsed since last update in seconds.</param>
    public void Update(World world, float deltaTime)
    {
        if (!_initialized)
        {
            Initialize();
        }

        // Step 1: Sync ECS → Physics Backend (for kinematic bodies or manual updates)
        SyncEcsToPhysics(world);

        // Step 2: Step the physics simulation
        _backend.Step(deltaTime);

        // Step 3: Sync Physics Backend → ECS (update transforms from physics)
        SyncPhysicsToEcs(world);
    }

    /// <summary>
    /// Creates a physics body for an entity if it doesn't have one.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="entity">The entity to create a body for.</param>
    public void CreateBodyForEntity(World world, Entity entity)
    {
        if (!world.HasComponent<TransformComponent>(entity) ||
            !world.HasComponent<RigidBodyComponent>(entity) ||
            !world.HasComponent<ColliderComponent>(entity))
        {
            return;
        }

        // Check if body already exists
        if (world.HasComponent<PhysicsBodyComponent>(entity))
        {
            var existingBody = world.GetComponent<PhysicsBodyComponent>(entity);
            if (existingBody.BodyHandle >= 0)
            {
                return; // Body already created
            }
        }

        var transform = world.GetComponent<TransformComponent>(entity);
        var rigidBody = world.GetComponent<RigidBodyComponent>(entity);
        var collider = world.GetComponent<ColliderComponent>(entity);

        // Create body in physics backend
        var bodyHandle = _backend.CreateBody(
            transform.Position,
            rigidBody.Mass,
            rigidBody.IsKinematic
        );

        // Configure body properties
        _backend.SetUseGravity(bodyHandle, rigidBody.UseGravity);
        _backend.SetLinearDamping(bodyHandle, rigidBody.Drag);
        _backend.SetBodyVelocity(bodyHandle, rigidBody.Velocity);

        // Add collider
        if (collider.Shape == ColliderShape.Circle)
        {
            _backend.AddCircleCollider(
                bodyHandle,
                collider.Size.X, // radius
                collider.Offset,
                rigidBody.Restitution,
                0.3f // default friction
            );
        }
        else if (collider.Shape == ColliderShape.Rectangle)
        {
            _backend.AddBoxCollider(
                bodyHandle,
                collider.Size.X, // width
                collider.Size.Y, // height
                collider.Offset,
                rigidBody.Restitution,
                0.3f // default friction
            );
        }

        // Store the body handle in a component
        world.AddComponent(entity, new PhysicsBodyComponent(bodyHandle));
    }

    /// <summary>
    /// Destroys the physics body for an entity.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="entity">The entity to destroy the body for.</param>
    public void DestroyBodyForEntity(World world, Entity entity)
    {
        if (!world.HasComponent<PhysicsBodyComponent>(entity))
        {
            return;
        }

        var physicsBody = world.GetComponent<PhysicsBodyComponent>(entity);
        if (physicsBody.BodyHandle >= 0)
        {
            _backend.DestroyBody(physicsBody.BodyHandle);
        }

        world.RemoveComponent<PhysicsBodyComponent>(entity);
    }

    /// <summary>
    /// Applies a force to an entity's physics body.
    /// </summary>
    public void ApplyForce(World world, Entity entity, Math.Vector2 force)
    {
        if (!world.HasComponent<PhysicsBodyComponent>(entity))
        {
            return;
        }

        var physicsBody = world.GetComponent<PhysicsBodyComponent>(entity);
        if (physicsBody.BodyHandle >= 0)
        {
            _backend.ApplyForce(physicsBody.BodyHandle, force);
        }
    }

    /// <summary>
    /// Applies an impulse to an entity's physics body.
    /// </summary>
    public void ApplyImpulse(World world, Entity entity, Math.Vector2 impulse)
    {
        if (!world.HasComponent<PhysicsBodyComponent>(entity))
        {
            return;
        }

        var physicsBody = world.GetComponent<PhysicsBodyComponent>(entity);
        if (physicsBody.BodyHandle >= 0)
        {
            _backend.ApplyImpulse(physicsBody.BodyHandle, impulse);
        }
    }

    /// <summary>
    /// Stops an entity's movement by setting velocity to zero.
    /// </summary>
    public void Stop(World world, Entity entity)
    {
        if (!world.HasComponent<PhysicsBodyComponent>(entity))
        {
            return;
        }

        var physicsBody = world.GetComponent<PhysicsBodyComponent>(entity);
        if (physicsBody.BodyHandle >= 0)
        {
            _backend.SetBodyVelocity(physicsBody.BodyHandle, Math.Vector2.Zero);
        }
    }

    /// <summary>
    /// Shuts down the physics backend and cleans up resources.
    /// </summary>
    public void Shutdown()
    {
        if (_initialized)
        {
            _backend.Shutdown();
            _initialized = false;
        }
    }

    private void SyncEcsToPhysics(World world)
    {
        var entities = world.GetAllEntities();

        foreach (var entity in entities)
        {
            if (!world.HasComponent<PhysicsBodyComponent>(entity) ||
                !world.HasComponent<RigidBodyComponent>(entity))
            {
                continue;
            }

            var physicsBody = world.GetComponent<PhysicsBodyComponent>(entity);
            if (physicsBody.BodyHandle < 0)
            {
                continue;
            }

            var rigidBody = world.GetComponent<RigidBodyComponent>(entity);

            // Sync body type changes (kinematic/dynamic)
            _backend.SetBodyType(physicsBody.BodyHandle, rigidBody.Mass, rigidBody.IsKinematic);

            // For kinematic bodies, sync position from ECS to physics
            if (rigidBody.IsKinematic && world.HasComponent<TransformComponent>(entity))
            {
                var transform = world.GetComponent<TransformComponent>(entity);
                _backend.SetBodyPosition(physicsBody.BodyHandle, transform.Position);
                _backend.SetBodyVelocity(physicsBody.BodyHandle, rigidBody.Velocity);
            }
        }
    }

    private void SyncPhysicsToEcs(World world)
    {
        var entities = world.GetAllEntities();

        foreach (var entity in entities)
        {
            if (!world.HasComponent<PhysicsBodyComponent>(entity) ||
                !world.HasComponent<TransformComponent>(entity) ||
                !world.HasComponent<RigidBodyComponent>(entity))
            {
                continue;
            }

            var physicsBody = world.GetComponent<PhysicsBodyComponent>(entity);
            if (physicsBody.BodyHandle < 0)
            {
                continue;
            }

            ref var rigidBody = ref world.GetComponent<RigidBodyComponent>(entity);

            // For dynamic bodies, sync position and velocity from physics to ECS
            if (!rigidBody.IsKinematic && rigidBody.Mass > 0)
            {
                var position = _backend.GetBodyPosition(physicsBody.BodyHandle);
                var velocity = _backend.GetBodyVelocity(physicsBody.BodyHandle);

                ref var transform = ref world.GetComponent<TransformComponent>(entity);
                transform.Position = position;

                rigidBody.Velocity = velocity;
            }
        }
    }
}

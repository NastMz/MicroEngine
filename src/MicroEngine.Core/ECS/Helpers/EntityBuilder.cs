using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;

namespace MicroEngine.Core.ECS.Helpers;

/// <summary>
/// Fluent API for building entities with components.
/// Simplifies entity creation by providing chainable methods.
/// </summary>
public sealed class EntityBuilder
{
    private readonly World _world;
    private readonly Entity _entity;
    private string? _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityBuilder"/> class.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    public EntityBuilder(World world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _entity = _world.CreateEntity();
    }

    /// <summary>
    /// Sets the entity name for debugging.
    /// </summary>
    /// <param name="name">The entity name.</param>
    /// <returns>This builder for chaining.</returns>
    public EntityBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Adds a transform component with position only.
    /// </summary>
    /// <param name="position">The world position.</param>
    /// <returns>This builder for chaining.</returns>
    public EntityBuilder WithTransform(Vector2 position)
    {
        return WithTransform(position, 0f, Vector2.One);
    }

    /// <summary>
    /// Adds a transform component with full configuration.
    /// </summary>
    /// <param name="position">The world position.</param>
    /// <param name="rotation">The rotation in degrees.</param>
    /// <param name="scale">The scale factor.</param>
    /// <returns>This builder for chaining.</returns>
    public EntityBuilder WithTransform(Vector2 position, float rotation, Vector2 scale)
    {
        _world.AddComponent(_entity, new TransformComponent
        {
            Position = position,
            Rotation = rotation,
            Scale = scale,
            Origin = Vector2.Zero
        });
        return this;
    }

    /// <summary>
    /// Adds a sprite component.
    /// </summary>
    /// <param name="tint">The sprite color tint.</param>
    /// <param name="visible">Whether the sprite is visible.</param>
    /// <returns>This builder for chaining.</returns>
    public EntityBuilder WithSprite(Color tint, bool visible = true)
    {
        _world.AddComponent(_entity, new SpriteComponent
        {
            Tint = tint,
            Visible = visible
        });
        return this;
    }

    /// <summary>
    /// Adds a rigid body component.
    /// </summary>
    /// <param name="mass">The mass of the body.</param>
    /// <param name="drag">The drag coefficient.</param>
    /// <param name="useGravity">Whether gravity affects this body.</param>
    /// <param name="isKinematic">Whether the body is kinematic (unaffected by forces).</param>
    /// <returns>This builder for chaining.</returns>
    public EntityBuilder WithRigidBody(
        float mass = 1f,
        float drag = 0f,
        bool useGravity = true,
        bool isKinematic = false)
    {
        _world.AddComponent(_entity, new RigidBodyComponent
        {
            Velocity = Vector2.Zero,
            Mass = mass,
            Drag = drag,
            IsKinematic = isKinematic,
            UseGravity = useGravity
        });
        return this;
    }

    /// <summary>
    /// Adds a box (rectangle) collider.
    /// </summary>
    /// <param name="width">The collider width.</param>
    /// <param name="height">The collider height.</param>
    /// <param name="isTrigger">Whether this is a trigger collider.</param>
    /// <param name="offset">The collider offset from transform position.</param>
    /// <returns>This builder for chaining.</returns>
    public EntityBuilder WithBoxCollider(
        float width,
        float height,
        bool isTrigger = false,
        Vector2? offset = null)
    {
        _world.AddComponent(_entity, new ColliderComponent
        {
            Shape = ColliderShape.Rectangle,
            Size = new Vector2(width, height),
            Offset = offset ?? Vector2.Zero,
            IsTrigger = isTrigger,
            Enabled = true,
            LayerMask = -1
        });
        return this;
    }

    /// <summary>
    /// Adds a circle collider.
    /// </summary>
    /// <param name="radius">The collider radius.</param>
    /// <param name="isTrigger">Whether this is a trigger collider.</param>
    /// <param name="offset">The collider offset from transform position.</param>
    /// <returns>This builder for chaining.</returns>
    public EntityBuilder WithCircleCollider(
        float radius,
        bool isTrigger = false,
        Vector2? offset = null)
    {
        _world.AddComponent(_entity, new ColliderComponent
        {
            Shape = ColliderShape.Circle,
            Size = new Vector2(radius, radius),
            Offset = offset ?? Vector2.Zero,
            IsTrigger = isTrigger,
            Enabled = true,
            LayerMask = -1
        });
        return this;
    }

    /// <summary>
    /// Builds and returns the entity.
    /// </summary>
    /// <returns>The created entity with all configured components.</returns>
    public Entity Build()
    {
        // Apply name if set (using internal World method if available)
        if (!string.IsNullOrEmpty(_name))
        {
            // Note: World doesn't expose SetEntityName, so we'd need to add it
            // or create the entity with name initially
            // For now, we'll recreate with name
            var namedEntity = _world.CreateEntity(_name);
            
            // Copy all components to named entity
            if (_world.HasComponent<TransformComponent>(_entity))
            {
                _world.AddComponent(namedEntity, _world.GetComponent<TransformComponent>(_entity));
            }
            if (_world.HasComponent<SpriteComponent>(_entity))
            {
                _world.AddComponent(namedEntity, _world.GetComponent<SpriteComponent>(_entity));
            }
            if (_world.HasComponent<RigidBodyComponent>(_entity))
            {
                _world.AddComponent(namedEntity, _world.GetComponent<RigidBodyComponent>(_entity));
            }
            if (_world.HasComponent<ColliderComponent>(_entity))
            {
                _world.AddComponent(namedEntity, _world.GetComponent<ColliderComponent>(_entity));
            }
            
            // Destroy the unnamed entity
            _world.DestroyEntity(_entity);
            
            return namedEntity;
        }

        return _entity;
    }
}

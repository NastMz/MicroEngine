using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Collision.Shapes;
using MicroEngine.Core.Physics;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;
using EngineVector2 = MicroEngine.Core.Math.Vector2;

namespace MicroEngine.Backend.Aether;

/// <summary>
/// Physics backend implementation using Aether.Physics2D (Box2D port).
/// Provides realistic rigid body dynamics and collision resolution.
/// </summary>
public sealed class AetherPhysicsBackend : IPhysicsBackend
{
    private World? _world;
    private readonly Dictionary<int, Body> _bodies = new();
    private int _nextBodyHandle = 1;
    private const float PixelsPerMeter = 100f; // Conversion factor

    /// <inheritdoc/>
    public void Initialize()
    {
        // Create Aether world with default gravity
        _world = new World(new AetherVector2(0, 9.8f)); // Will be set properly via SetGravity
    }

    /// <inheritdoc/>
    public void Shutdown()
    {
        if (_world == null)
        {
            return;
        }

        // Clear all bodies
        foreach (var body in _bodies.Values)
        {
            _world.Remove(body);
        }
        _bodies.Clear();

        _world = null;
    }

    /// <inheritdoc/>
    public void Step(float deltaTime)
    {
        if (_world == null)
        {
            throw new InvalidOperationException("Physics backend not initialized");
        }

        // Step the physics simulation
        _world.Step(deltaTime);
    }

    /// <inheritdoc/>
    public void SetGravity(EngineVector2 gravity)
    {
        if (_world == null)
        {
            throw new InvalidOperationException("Physics backend not initialized");
        }

        // Convert from pixels/s² to meters/s²
        _world.Gravity = new AetherVector2(
            gravity.X / PixelsPerMeter,
            gravity.Y / PixelsPerMeter
        );
    }

    /// <inheritdoc/>
    public EngineVector2 GetGravity()
    {
        if (_world == null)
        {
            throw new InvalidOperationException("Physics backend not initialized");
        }

        var gravity = _world.Gravity;
        return new EngineVector2(
            gravity.X * PixelsPerMeter,
            gravity.Y * PixelsPerMeter
        );
    }

    /// <inheritdoc/>
    public int CreateBody(EngineVector2 position, float mass, bool isKinematic = false)
    {
        if (_world == null)
        {
            throw new InvalidOperationException("Physics backend not initialized");
        }

        // Convert position from pixels to meters
        var aetherPos = new AetherVector2(
            position.X / PixelsPerMeter,
            position.Y / PixelsPerMeter
        );

        // Determine body type
        BodyType bodyType;
        if (mass <= 0f)
        {
            bodyType = BodyType.Static;
        }
        else if (isKinematic)
        {
            bodyType = BodyType.Kinematic;
        }
        else
        {
            bodyType = BodyType.Dynamic;
        }

        var body = _world.CreateBody(aetherPos, 0f, bodyType);
        
        if (bodyType == BodyType.Dynamic && mass > 0f)
        {
            body.Mass = mass;
        }

        int handle = _nextBodyHandle++;
        _bodies[handle] = body;

        return handle;
    }

    /// <inheritdoc/>
    public void DestroyBody(int bodyHandle)
    {
        if (_world == null)
        {
            throw new InvalidOperationException("Physics backend not initialized");
        }

        if (_bodies.TryGetValue(bodyHandle, out var body))
        {
            _world.Remove(body);
            _bodies.Remove(bodyHandle);
        }
    }

    /// <inheritdoc/>
    public void SetBodyType(int bodyHandle, float mass, bool isKinematic)
    {
        if (!_bodies.TryGetValue(bodyHandle, out var body))
        {
            throw new ArgumentException($"Body handle {bodyHandle} not found");
        }

        BodyType bodyType;
        if (mass <= 0f)
        {
            bodyType = BodyType.Static;
        }
        else if (isKinematic)
        {
            bodyType = BodyType.Kinematic;
        }
        else
        {
            bodyType = BodyType.Dynamic;
        }

        body.BodyType = bodyType;
        
        if (bodyType == BodyType.Dynamic && mass > 0f)
        {
            body.Mass = mass;
        }
    }

    /// <inheritdoc/>
    public void AddCircleCollider(int bodyHandle, float radius, EngineVector2 offset, float restitution = 0.5f, float friction = 0.3f)
    {
        if (!_bodies.TryGetValue(bodyHandle, out var body))
        {
            throw new ArgumentException($"Body handle {bodyHandle} not found");
        }

        // Convert radius from pixels to meters
        var radiusMeters = radius / PixelsPerMeter;
        var offsetMeters = new AetherVector2(offset.X / PixelsPerMeter, offset.Y / PixelsPerMeter);

        var fixture = body.CreateCircle(radiusMeters, 1f, offsetMeters);
        fixture.Restitution = restitution;
        fixture.Friction = friction;
    }

    /// <inheritdoc/>
    public void AddBoxCollider(int bodyHandle, float width, float height, EngineVector2 offset, float restitution = 0.5f, float friction = 0.3f)
    {
        if (!_bodies.TryGetValue(bodyHandle, out var body))
        {
            throw new ArgumentException($"Body handle {bodyHandle} not found");
        }

        // Convert from pixels to meters
        var widthMeters = width / PixelsPerMeter;
        var heightMeters = height / PixelsPerMeter;
        var offsetMeters = new AetherVector2(offset.X / PixelsPerMeter, offset.Y / PixelsPerMeter);

        var fixture = body.CreateRectangle(widthMeters, heightMeters, 1f, offsetMeters);
        fixture.Restitution = restitution;
        fixture.Friction = friction;
    }

    /// <inheritdoc/>
    public EngineVector2 GetBodyPosition(int bodyHandle)
    {
        if (!_bodies.TryGetValue(bodyHandle, out var body))
        {
            throw new ArgumentException($"Body handle {bodyHandle} not found");
        }

        var pos = body.Position;
        return new EngineVector2(
            pos.X * PixelsPerMeter,
            pos.Y * PixelsPerMeter
        );
    }

    /// <inheritdoc/>
    public void SetBodyPosition(int bodyHandle, EngineVector2 position)
    {
        if (!_bodies.TryGetValue(bodyHandle, out var body))
        {
            throw new ArgumentException($"Body handle {bodyHandle} not found");
        }

        body.Position = new AetherVector2(
            position.X / PixelsPerMeter,
            position.Y / PixelsPerMeter
        );
    }

    /// <inheritdoc/>
    public EngineVector2 GetBodyVelocity(int bodyHandle)
    {
        if (!_bodies.TryGetValue(bodyHandle, out var body))
        {
            throw new ArgumentException($"Body handle {bodyHandle} not found");
        }

        var vel = body.LinearVelocity;
        return new EngineVector2(
            vel.X * PixelsPerMeter,
            vel.Y * PixelsPerMeter
        );
    }

    /// <inheritdoc/>
    public void SetBodyVelocity(int bodyHandle, EngineVector2 velocity)
    {
        if (!_bodies.TryGetValue(bodyHandle, out var body))
        {
            throw new ArgumentException($"Body handle {bodyHandle} not found");
        }

        body.LinearVelocity = new AetherVector2(
            velocity.X / PixelsPerMeter,
            velocity.Y / PixelsPerMeter
        );
    }

    /// <inheritdoc/>
    public void ApplyForce(int bodyHandle, EngineVector2 force)
    {
        if (!_bodies.TryGetValue(bodyHandle, out var body))
        {
            throw new ArgumentException($"Body handle {bodyHandle} not found");
        }

        body.ApplyForce(new AetherVector2(
            force.X / PixelsPerMeter,
            force.Y / PixelsPerMeter
        ));
    }

    /// <inheritdoc/>
    public void ApplyImpulse(int bodyHandle, EngineVector2 impulse)
    {
        if (!_bodies.TryGetValue(bodyHandle, out var body))
        {
            throw new ArgumentException($"Body handle {bodyHandle} not found");
        }

        body.ApplyLinearImpulse(new AetherVector2(
            impulse.X / PixelsPerMeter,
            impulse.Y / PixelsPerMeter
        ));
    }

    /// <inheritdoc/>
    public void SetUseGravity(int bodyHandle, bool useGravity)
    {
        if (!_bodies.TryGetValue(bodyHandle, out var body))
        {
            throw new ArgumentException($"Body handle {bodyHandle} not found");
        }

        body.IgnoreGravity = !useGravity;
    }

    /// <inheritdoc/>
    public void SetLinearDamping(int bodyHandle, float damping)
    {
        if (!_bodies.TryGetValue(bodyHandle, out var body))
        {
            throw new ArgumentException($"Body handle {bodyHandle} not found");
        }

        body.LinearDamping = damping;
    }
}

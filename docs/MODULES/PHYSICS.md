# Physics Module

**Module:** MicroEngine.Core.Physics  
**Status:** Complete (v0.9.0)  
**Version:** 2.0  
**Last Updated:** November 2025

---

## Overview

The Physics module provides realistic rigid body dynamics and collision resolution for MicroEngine through a backend-agnostic architecture.

**Current Implementation:**

-   ✅ **Physics Backend Abstraction** (IPhysicsBackend interface)
-   ✅ **Aether.Physics2D Integration** (Box2D port for .NET)
-   ✅ **ECS Integration** (PhysicsBackendSystem, PhysicsBodyComponent)
-   ✅ **2D Rigid Body Dynamics** (gravity, forces, impulses, damping)
-   ✅ **Collision Detection** (circles, boxes with realistic resolution)
-   ✅ **Body Types** (Static, Kinematic, Dynamic)
-   ✅ **Realistic Physics** (proper stacking, falling, bouncing)

**Key Features:**

-   **Backend-Agnostic:** Swap physics engines without changing game code
-   **Professional-Grade:** Uses industry-standard Aether.Physics2D (Box2D)
-   **ECS Native:** Seamless integration with entity-component system
-   **Type-Safe:** Strong typing prevents runtime errors
-   **Performant:** Optimized for real-time simulation

**Supported Physics Backends:**

1. **Aether.Physics2D** (Current) — Box2D port, realistic 2D physics
2. **Custom Backends** (Future) — Implement IPhysicsBackend for alternatives

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Architecture](#architecture)
3. [Physics Backend Interface](#physics-backend-interface)
4. [ECS Integration](#ecs-integration)
5. [Body Types](#body-types)
6. [Collision Shapes](#collision-shapes)
7. [Forces and Impulses](#forces-and-impulses)
8. [Usage Examples](#usage-examples)
9. [Best Practices](#best-practices)
10. [API Reference](#api-reference)

---

## Quick Start

### Basic Physics Setup

```csharp
using MicroEngine.Backend.Aether;
using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Systems;
using MicroEngine.Core.ECS.Components;

// 1. Create physics backend
var physicsBackend = new AetherPhysicsBackend();

// 2. Create physics system
var physicsSystem = new PhysicsBackendSystem(physicsBackend);
physicsSystem.Initialize(gravity: 750f); // Downward gravity (pixels/s²)

// 3. Register with ECS
world.RegisterSystem(physicsSystem);

// 4. Create entity with physics
var ball = world.CreateEntity();
world.AddComponent(ball, new TransformComponent { Position = new Vector2(400, 100) });
world.AddComponent(ball, new ColliderComponent
{
    Shape = ColliderShape.Circle,
    Size = new Vector2(15, 15)
});
world.AddComponent(ball, new RigidBodyComponent
{
    Mass = 1.0f,
    UseGravity = true,
    Restitution = 0.7f // Bounciness
});

// 5. Create physics body
physicsSystem.CreateBodyForEntity(world, ball);

// 6. Update physics each frame
physicsSystem.Update(world, deltaTime);
```

---

## Architecture

### System Overview

```
Game Scene
    ↓ creates
AetherPhysicsBackend (IPhysicsBackend)
    ↓ manages
PhysicsBackendSystem
    ↓ syncs with
ECS World (Entities + PhysicsBodyComponent)
    ↓ renders
IRenderBackend2D
```

### Key Components

**IPhysicsBackend:**

-   Abstract interface for physics engines
-   18 methods covering full physics lifecycle
-   Backend implementations (Aether, custom)

**PhysicsBackendSystem:**

-   ECS system managing physics simulation
-   Bidirectional sync between ECS and physics
-   Helper methods for body management

**PhysicsBodyComponent:**

-   Links ECS entity to physics body handle
-   Enables entity-physics synchronization

### Core Classes

#### PhysicsWorld

Container for all physics objects and queries.

```csharp
public class PhysicsWorld
{
    public void AddBody(PhysicsBody body);
    public void RemoveBody(PhysicsBody body);
    public void Update(float deltaTime);

    public CollisionResult[] DetectCollisions();
    public RaycastHit? Raycast(Vector3 origin, Vector3 direction, float maxDistance);
    public PhysicsBody[] OverlapSphere(Vector3 center, float radius);
}
```

#### PhysicsBody

Component representing a physical object.

```csharp
public class PhysicsBody : IComponent
{
    public CollisionShape Shape { get; set; }
    public BodyType Type { get; set; }
    public int Layer { get; set; }
    public int LayerMask { get; set; }
    public bool IsTrigger { get; set; }
}
```

#### CollisionShape

Base class for collision shapes.

```csharp
public abstract class CollisionShape
{
    public abstract Bounds GetBounds();
    public abstract bool Intersects(CollisionShape other);
}
```

---

## Collision Detection

### Collision Detection Pipeline

```
1. Broad Phase → Identify potential collisions (spatial partitioning)
2. Narrow Phase → Precise collision testing (shape-to-shape)
3. Collision Response → Generate events and resolution data
```

### Broad Phase

Uses spatial partitioning to quickly eliminate non-colliding pairs:

```csharp
public class BroadPhase
{
    private QuadTree _quadTree;

    public List<CollisionPair> FindPotentialCollisions()
    {
        var pairs = new List<CollisionPair>();

        foreach (var body in _bodies)
        {
            var candidates = _quadTree.Query(body.Bounds);

            foreach (var candidate in candidates)
            {
                if (ShouldTest(body, candidate))
                {
                    pairs.Add(new CollisionPair(body, candidate));
                }
            }
        }

        return pairs;
    }
}
```

### Narrow Phase

Precise shape-to-shape collision testing:

```csharp
public class NarrowPhase
{
    public CollisionResult? TestCollision(PhysicsBody a, PhysicsBody b)
    {
        return a.Shape switch
        {
            CircleShape circleA when b.Shape is CircleShape circleB
                => TestCircleCircle(circleA, circleB),

            BoxShape boxA when b.Shape is BoxShape boxB
                => TestBoxBox(boxA, boxB),

            CircleShape circle when b.Shape is BoxShape box
                => TestCircleBox(circle, box),

            _ => null
        };
    }
}
```

### Collision Events

```csharp
public class PhysicsWorld
{
    public event Action<CollisionEvent> OnCollisionEnter;
    public event Action<CollisionEvent> OnCollisionStay;
    public event Action<CollisionEvent> OnCollisionExit;
}

public class CollisionEvent
{
    public PhysicsBody BodyA { get; }
    public PhysicsBody BodyB { get; }
    public Vector3 ContactPoint { get; }
    public Vector3 Normal { get; }
    public float Penetration { get; }
}
```

---

## Collision Shapes

### Box (AABB)

Axis-aligned bounding box:

```csharp
public class BoxShape : CollisionShape
{
    public Vector3 Center { get; set; }
    public Vector3 Size { get; set; }

    public override bool Intersects(CollisionShape other)
    {
        if (other is BoxShape box)
        {
            return AABB.Intersects(this.Bounds, box.Bounds);
        }
        // Other shape tests...
    }
}
```

### Circle/Sphere

2D circle or 3D sphere:

```csharp
public class CircleShape : CollisionShape
{
    public Vector3 Center { get; set; }
    public float Radius { get; set; }

    public override bool Intersects(CollisionShape other)
    {
        if (other is CircleShape circle)
        {
            var distance = Vector3.Distance(Center, circle.Center);
            return distance < Radius + circle.Radius;
        }
        // Other shape tests...
    }
}
```

### Polygon (2D)

Convex polygon:

```csharp
public class PolygonShape : CollisionShape
{
    public Vector2[] Vertices { get; set; }

    public override bool Intersects(CollisionShape other)
    {
        if (other is PolygonShape polygon)
        {
            return SAT.TestPolygonPolygon(this, polygon);
        }
        // Other shape tests...
    }
}
```

### Capsule

Pill-shaped collider:

```csharp
public class CapsuleShape : CollisionShape
{
    public Vector3 Start { get; set; }
    public Vector3 End { get; set; }
    public float Radius { get; set; }
}
```

### Compound Shape

Multiple shapes combined:

```csharp
public class CompoundShape : CollisionShape
{
    public List<CollisionShape> Children { get; set; }

    public override bool Intersects(CollisionShape other)
    {
        return Children.Any(child => child.Intersects(other));
    }
}
```

---

## Spatial Partitioning

### QuadTree (2D)

Divides 2D space into quadrants for efficient queries:

```csharp
public class QuadTree
{
    private const int MaxObjects = 10;
    private const int MaxLevels = 5;

    private int _level;
    private List<PhysicsBody> _objects;
    private Rect _bounds;
    private QuadTree[] _nodes;

    public void Insert(PhysicsBody body)
    {
        if (_nodes[0] != null)
        {
            int index = GetIndex(body.Bounds);

            if (index != -1)
            {
                _nodes[index].Insert(body);
                return;
            }
        }

        _objects.Add(body);

        if (_objects.Count > MaxObjects && _level < MaxLevels)
        {
            if (_nodes[0] == null)
                Split();

            Redistribute();
        }
    }

    public List<PhysicsBody> Query(Rect bounds)
    {
        var results = new List<PhysicsBody>();

        if (_nodes[0] != null)
        {
            int index = GetIndex(bounds);

            if (index != -1)
            {
                results.AddRange(_nodes[index].Query(bounds));
            }
            else
            {
                foreach (var node in _nodes)
                    results.AddRange(node.Query(bounds));
            }
        }

        results.AddRange(_objects.Where(o => bounds.Intersects(o.Bounds)));

        return results;
    }
}
```

### OctTree (3D, Future)

3D equivalent of QuadTree for 3D physics:

```csharp
public class OctTree
{
    // Similar to QuadTree but with 8 octants instead of 4 quadrants
}
```

---

## Raycasting

### Raycast

Cast a ray and detect the first hit:

```csharp
var origin = new Vector3(0, 0, 0);
var direction = Vector3.Right;
var maxDistance = 100.0f;

var hit = physicsWorld.Raycast(origin, direction, maxDistance);

if (hit.HasValue)
{
    Console.WriteLine($"Hit: {hit.Value.Body.Entity.Name}");
    Console.WriteLine($"Point: {hit.Value.Point}");
    Console.WriteLine($"Distance: {hit.Value.Distance}");
}
```

### RaycastHit

Result of a raycast:

```csharp
public struct RaycastHit
{
    public PhysicsBody Body { get; }
    public Vector3 Point { get; }
    public Vector3 Normal { get; }
    public float Distance { get; }
}
```

### Raycast Filtering

```csharp
public RaycastHit? Raycast(Vector3 origin, Vector3 direction, float maxDistance,
    int layerMask = ~0)
{
    var candidates = GetBodiesInRay(origin, direction, maxDistance);

    RaycastHit? closest = null;

    foreach (var body in candidates)
    {
        if ((body.Layer & layerMask) == 0)
            continue;

        var hit = body.Shape.Raycast(origin, direction, maxDistance);

        if (hit.HasValue && (!closest.HasValue || hit.Value.Distance < closest.Value.Distance))
        {
            closest = hit;
        }
    }

    return closest;
}
```

### RaycastAll

Get all hits along a ray:

```csharp
var hits = physicsWorld.RaycastAll(origin, direction, maxDistance);

foreach (var hit in hits.OrderBy(h => h.Distance))
{
    Console.WriteLine($"Hit: {hit.Body.Entity.Name} at {hit.Distance}");
}
```

### Shape Casting

Cast a shape along a direction (like a thick raycast):

```csharp
var shape = new CircleShape { Radius = 5.0f };
var hit = physicsWorld.ShapeCast(shape, origin, direction, maxDistance);
```

---

## Trigger Zones

### Trigger Setup

```csharp
var triggerEntity = world.CreateEntity("Checkpoint");
triggerEntity.AddComponent(new TransformComponent
{
    Position = new Vector3(100, 100, 0)
});
triggerEntity.AddComponent(new PhysicsBody
{
    Shape = new BoxShape { Size = new Vector3(50, 50, 1) },
    IsTrigger = true,
    Layer = PhysicsLayers.Triggers
});
```

### Trigger Events

```csharp
physicsWorld.OnTriggerEnter += (body, trigger) =>
{
    if (body.Entity.HasComponent<PlayerComponent>())
    {
        Console.WriteLine("Player entered checkpoint!");
    }
};

physicsWorld.OnTriggerExit += (body, trigger) =>
{
    if (body.Entity.HasComponent<PlayerComponent>())
    {
        Console.WriteLine("Player left checkpoint!");
    }
};
```

### Overlap Queries

Find all bodies overlapping a region:

```csharp
// Sphere overlap
var bodies = physicsWorld.OverlapSphere(center, radius);

// Box overlap
var bodies = physicsWorld.OverlapBox(center, size);

// Custom shape overlap
var bodies = physicsWorld.OverlapShape(shape);
```

---

## Physics Simulation

### RigidBodyComponent (Future)

```csharp
public class RigidBodyComponent : IComponent
{
    public Vector3 Velocity { get; set; }
    public Vector3 AngularVelocity { get; set; }
    public float Mass { get; set; }
    public float Drag { get; set; }
    public float AngularDrag { get; set; }
    public bool UseGravity { get; set; }
    public bool IsKinematic { get; set; }
}
```

### Physics Simulation (Future)

```csharp
public class PhysicsSimulation
{
    public Vector3 Gravity { get; set; } = new Vector3(0, -9.81f, 0);

    public void Step(float deltaTime)
    {
        // Apply forces
        ApplyGravity(deltaTime);
        ApplyDrag(deltaTime);

        // Integrate velocity
        IntegrateVelocity(deltaTime);

        // Detect collisions
        var collisions = DetectCollisions();

        // Resolve collisions
        ResolveCollisions(collisions);
    }
}
```

### Forces (Future)

```csharp
public void AddForce(Rigidbody body, Vector3 force)
{
    body.Velocity += force / body.Mass * deltaTime;
}

public void AddImpulse(Rigidbody body, Vector3 impulse)
{
    body.Velocity += impulse / body.Mass;
}
```

---

## Collision Layers (v0.13.0+)

### CollisionLayer Struct

Layers are defined using the `CollisionLayer` struct, which combines an ID (0-31) and a debug name.

```csharp
public readonly struct CollisionLayer
{
    public int Id { get; }
    public string Name { get; }

    public CollisionLayer(int id, string name) { ... }
}

// Predefined layers
public static class PhysicsLayers
{
    public static readonly CollisionLayer Default = new(0, "Default");
    public static readonly CollisionLayer Player = new(1, "Player");
    public static readonly CollisionLayer Enemy = new(2, "Enemy");
    public static readonly CollisionLayer Environment = new(3, "Environment");
}
```

### Collision Matrix

The `CollisionMatrix` defines which layers can collide with each other. By default, all layers collide with everything.

```csharp
public class CollisionMatrix
{
    // Enable/Disable collision between two layers
    public void SetCollision(CollisionLayer layerA, CollisionLayer layerB, bool canCollide);

    // Check if two layers should collide
    public bool CanCollide(CollisionLayer layerA, CollisionLayer layerB);
}
```

### Usage Example

```csharp
// 1. Configure Matrix
var matrix = new CollisionMatrix();

// Player collides with everything (default)
// Enemies collide with Player and Environment
// Enemies IGNORE other Enemies

matrix.SetCollision(PhysicsLayers.Enemy, PhysicsLayers.Enemy, false);

// 2. Assign Layers to Bodies
var playerBody = physicsSystem.CreateBody(playerEntity);
playerBody.Layer = PhysicsLayers.Player;

var enemyBody1 = physicsSystem.CreateBody(enemyEntity1);
enemyBody1.Layer = PhysicsLayers.Enemy;

var enemyBody2 = physicsSystem.CreateBody(enemyEntity2);
enemyBody2.Layer = PhysicsLayers.Enemy;

// 3. Result
// Player hits Enemy1 -> Collision
// Enemy1 hits Enemy2 -> No Collision (Pass through)
```

---

## Integration with ECS

### Physics System

```csharp
public class PhysicsSystem : ISystem
{
    private readonly PhysicsWorld _physicsWorld;

    public void Initialize(World world)
    {
        _physicsWorld = new PhysicsWorld();

        // Add all physics bodies to physics world
        var entities = world.Query<PhysicsBody, TransformComponent>();

        foreach (var entity in entities)
        {
            var body = entity.GetComponent<PhysicsBody>();
            _physicsWorld.AddBody(body);
        }
    }

    public void Update(World world, float deltaTime)
    {
        // Update physics world
        _physicsWorld.Update(deltaTime);

        // Handle collision events
        var collisions = _physicsWorld.DetectCollisions();

        foreach (var collision in collisions)
        {
            HandleCollision(collision);
        }
    }
}
```

### Transform Synchronization

```csharp
public class TransformSyncSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        var entities = world.Query<PhysicsBody, TransformComponent>();

        foreach (var entity in entities)
        {
            var body = entity.GetComponent<PhysicsBody>();
            var transform = entity.GetComponent<TransformComponent>();

            // Sync shape position with transform
            body.Shape.Center = transform.Position;
        }
    }
}
```

---

## Usage Examples

### Example 1: Simple Collision Detection

```csharp
public class CollisionSystem : ISystem
{
    private readonly PhysicsWorld _physics;

    public void Update(World world, float deltaTime)
    {
        var collisions = _physics.DetectCollisions();

        foreach (var collision in collisions)
        {
            if (collision.BodyA.Entity.HasComponent<PlayerComponent>() &&
                collision.BodyB.Entity.HasComponent<EnemyComponent>())
            {
                DamagePlayer(collision.BodyA.Entity);
            }
        }
    }
}
```

### Example 2: Raycast for Line of Sight

```csharp
public class AIVisionSystem : ISystem
{
    private readonly PhysicsWorld _physics;

    public void Update(World world, float deltaTime)
    {
        var enemies = world.Query<EnemyComponent, TransformComponent>();
        var player = world.Query<PlayerComponent, TransformComponent>().First();

        var playerTransform = player.GetComponent<TransformComponent>();

        foreach (var enemy in enemies)
        {
            var enemyTransform = enemy.GetComponent<TransformComponent>();

            var direction = Vector3.Normalize(
                playerTransform.Position - enemyTransform.Position
            );

            var hit = _physics.Raycast(
                enemyTransform.Position,
                direction,
                maxDistance: 200.0f,
                layerMask: PhysicsLayers.Player | PhysicsLayers.Environment
            );

            if (hit.HasValue && hit.Value.Body.Entity == player)
            {
                // Player is visible
                enemy.GetComponent<AIComponent>().State = AIState.Chasing;
            }
        }
    }
}
```

### Example 3: Pickup System with Triggers

```csharp
public class PickupSystem : ISystem
{
    private readonly PhysicsWorld _physics;

    public void Initialize(World world)
    {
        _physics.OnTriggerEnter += HandleTriggerEnter;
    }

    private void HandleTriggerEnter(PhysicsBody body, PhysicsBody trigger)
    {
        if (body.Entity.HasComponent<PlayerComponent>() &&
            trigger.Entity.HasComponent<PickupComponent>())
        {
            var pickup = trigger.Entity.GetComponent<PickupComponent>();

            if (pickup.Type == PickupType.Coin)
            {
                AddScore(10);
            }

            world.DestroyEntity(trigger.Entity);
        }
    }
}
```

### Example 4: Platformer Collision

```csharp
public class PlatformerPhysicsSystem : ISystem
{
    private readonly PhysicsWorld _physics;

    public void Update(World world, float deltaTime)
    {
        var players = world.Query<PlayerComponent, TransformComponent, VelocityComponent>();

        foreach (var player in players)
        {
            var transform = player.GetComponent<TransformComponent>();
            var velocity = player.GetComponent<VelocityComponent>();

            // Apply gravity
            velocity.Value.Y += 980.0f * deltaTime;

            // Move and check collisions
            var nextPos = transform.Position + velocity.Value * deltaTime;

            // Check ground collision
            var groundHit = _physics.Raycast(
                transform.Position,
                Vector3.Down,
                maxDistance: 10.0f
            );

            if (groundHit.HasValue && groundHit.Value.Distance < 1.0f)
            {
                player.IsGrounded = true;
                velocity.Value.Y = 0;
                transform.Position.Y = groundHit.Value.Point.Y;
            }
            else
            {
                player.IsGrounded = false;
            }
        }
    }
}
```

---

## Best Practices

### Do's

-   ✓ Use spatial partitioning for large worlds
-   ✓ Separate collision detection from physics simulation
-   ✓ Use layers to filter unnecessary collision checks
-   ✓ Cache collision queries when possible
-   ✓ Use triggers for non-physical interactions
-   ✓ Implement broad-phase optimization
-   ✓ Profile physics performance regularly

### Don'ts

-   ✗ Don't test all bodies against all other bodies (O(n²))
-   ✗ Don't perform expensive collision tests every frame for static objects
-   ✗ Don't forget to remove bodies from physics world when entities are destroyed
-   ✗ Don't use complex shapes when simple ones suffice
-   ✗ Don't ignore physics layers (test everything against everything)
-   ✗ Don't raycast every frame unless necessary

### Performance Tips

**Static vs Dynamic:**

```csharp
public enum BodyType
{
    Static,   // Never moves (walls, floors)
    Kinematic, // Moves but not affected by physics (platforms)
    Dynamic   // Fully simulated (player, enemies)
}

// Only test Dynamic vs (Static | Dynamic)
```

**Sleeping:**

```csharp
public class PhysicsBody
{
    public bool IsSleeping { get; set; }

    public void CheckSleep()
    {
        if (Velocity.LengthSquared() < 0.01f)
            IsSleeping = true;
    }
}

// Skip sleeping bodies in updates
```

---

## API Reference

### PhysicsWorld

```csharp
public class PhysicsWorld
{
    // Body management
    public void AddBody(PhysicsBody body);
    public void RemoveBody(PhysicsBody body);

    // Update
    public void Update(float deltaTime);

    // Collision detection
    public CollisionResult[] DetectCollisions();

    // Raycasting
    public RaycastHit? Raycast(Vector3 origin, Vector3 direction, float maxDistance,
        int layerMask = ~0);
    public RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance,
        int layerMask = ~0);
    public RaycastHit? ShapeCast(CollisionShape shape, Vector3 origin, Vector3 direction,
        float maxDistance, int layerMask = ~0);

    // Overlap queries
    public PhysicsBody[] OverlapSphere(Vector3 center, float radius, int layerMask = ~0);
    public PhysicsBody[] OverlapBox(Vector3 center, Vector3 size, int layerMask = ~0);
    public PhysicsBody[] OverlapShape(CollisionShape shape, int layerMask = ~0);

    // Events
    public event Action<CollisionEvent> OnCollisionEnter;
    public event Action<CollisionEvent> OnCollisionStay;
    public event Action<CollisionEvent> OnCollisionExit;
    public event Action<PhysicsBody, PhysicsBody> OnTriggerEnter;
    public event Action<PhysicsBody, PhysicsBody> OnTriggerExit;
}
```

### PhysicsBody

```csharp
public class PhysicsBody : IComponent
{
    public Entity Entity { get; set; }
    public CollisionShape Shape { get; set; }
    public BodyType Type { get; set; }
    public int Layer { get; set; }
    public int LayerMask { get; set; }
    public bool IsTrigger { get; set; }
    public bool IsSleeping { get; set; }
}
```

---

## Related Documentation

-   [Architecture](../ARCHITECTURE.md)
-   [ECS Module](ECS.md)
-   [Scenes Module](SCENES.md)

---

**Last Updated:** November 2025  
**Version:** 1.0

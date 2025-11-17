# Physics Module

**Module:** Engine.Core.Physics  
**Status:** Planned  
**Version:** 1.0  
**Last Updated:** November 2025

---

## Overview

The Physics module provides collision detection, spatial queries, and physics simulation for MicroEngine.

It supports:

- **2D collision detection** (AABB, circle, polygon)
- **3D collision detection** (future: boxes, spheres, meshes)
- **Spatial partitioning** (quadtree, octree)
- **Raycasting** and shape casting
- **Trigger zones** (non-physical collision areas)
- **Physics simulation** (optional: velocity, forces, gravity)
- **Collision layers and masks** for filtering

The physics system is designed to be:

- **Dimension-agnostic:** Core APIs support both 2D and 3D
- **Lightweight:** Optional physics simulation (can use detection-only mode)
- **Flexible:** Integrate with external physics engines (Box2D, Jolt, etc.)
- **Performant:** Efficient spatial partitioning and broad-phase detection

---

## Table of Contents

1. [Core Concepts](#core-concepts)
2. [Architecture](#architecture)
3. [Collision Detection](#collision-detection)
4. [Collision Shapes](#collision-shapes)
5. [Spatial Partitioning](#spatial-partitioning)
6. [Raycasting](#raycasting)
7. [Trigger Zones](#trigger-zones)
8. [Physics Simulation](#physics-simulation)
9. [Collision Layers](#collision-layers)
10. [Integration with ECS](#integration-with-ecs)
11. [Usage Examples](#usage-examples)
12. [Best Practices](#best-practices)
13. [API Reference](#api-reference)

---

## Core Concepts

### Physics Modes

MicroEngine supports two physics modes:

**1. Detection-Only Mode:**

- Only collision detection and queries
- No velocity, forces, or physics simulation
- Lightweight, suitable for simple games
- You manually handle movement and collisions

**2. Simulation Mode (Future):**

- Full physics simulation with velocity, forces, gravity
- Integration with external physics engines
- Automatic collision resolution
- Suitable for physics-heavy games

### Collision vs Trigger

**Collision:**

- Physical collision that prevents overlap
- Generates collision response (bounce, slide)
- Used for solid objects (walls, floors, obstacles)

**Trigger:**

- Detection-only area (objects can overlap)
- No collision response
- Used for events (pickups, checkpoints, damage zones)

---

## Architecture

### Class Diagram

```
PhysicsWorld
├── Collision Detection
│   ├── Broad Phase (spatial partitioning)
│   └── Narrow Phase (shape tests)
├── Spatial Structures
│   ├── QuadTree (2D)
│   └── OctTree (3D, future)
├── Raycasting
└── Collision Events
```

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

### Rigidbody Component (Future)

```csharp
public class Rigidbody : IComponent
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

## Collision Layers

### Layer System

```csharp
public static class PhysicsLayers
{
    public const int Default = 1 << 0;
    public const int Player = 1 << 1;
    public const int Enemy = 1 << 2;
    public const int Environment = 1 << 3;
    public const int Projectile = 1 << 4;
    public const int Triggers = 1 << 5;
}
```

### Layer Filtering

```csharp
var playerBody = new PhysicsBody
{
    Layer = PhysicsLayers.Player,
    LayerMask = PhysicsLayers.Environment | PhysicsLayers.Enemy
};

// Player collides with Environment and Enemy, but not other Players
```

### Collision Matrix

```csharp
public class CollisionMatrix
{
    private bool[,] _matrix;

    public bool ShouldCollide(int layerA, int layerB)
    {
        return _matrix[layerA, layerB];
    }

    public void SetCollision(int layerA, int layerB, bool canCollide)
    {
        _matrix[layerA, layerB] = canCollide;
        _matrix[layerB, layerA] = canCollide;
    }
}
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

- ✓ Use spatial partitioning for large worlds
- ✓ Separate collision detection from physics simulation
- ✓ Use layers to filter unnecessary collision checks
- ✓ Cache collision queries when possible
- ✓ Use triggers for non-physical interactions
- ✓ Implement broad-phase optimization
- ✓ Profile physics performance regularly

### Don'ts

- ✗ Don't test all bodies against all other bodies (O(n²))
- ✗ Don't perform expensive collision tests every frame for static objects
- ✗ Don't forget to remove bodies from physics world when entities are destroyed
- ✗ Don't use complex shapes when simple ones suffice
- ✗ Don't ignore physics layers (test everything against everything)
- ✗ Don't raycast every frame unless necessary

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

- [Architecture](../ARCHITECTURE.md)
- [ECS Module](ECS.md)
- [Scenes Module](SCENES.md)

---

**Last Updated:** November 2025  
**Version:** 1.0

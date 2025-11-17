# ECS Module (Entity-Component-System)

**Module:** Engine.Core.ECS  
**Status:** Active  
**Version:** 1.0  
**Last Updated:** November 2025

---

## Overview

The ECS (Entity-Component-System) module is the core architectural pattern used by MicroEngine for managing game
objects and their behaviors.

It provides a lightweight, data-oriented approach to game entity management that is:

- **Performant:** Cache-friendly data layout
- **Flexible:** Easy to add/remove components at runtime
- **Maintainable:** Clear separation between data and logic
- **Dimension-agnostic:** Works for both 2D and 3D entities

---

## Table of Contents

1. [Core Concepts](#core-concepts)
2. [Architecture](#architecture)
3. [Entity Management](#entity-management)
4. [Component System](#component-system)
5. [System Processing](#system-processing)
6. [Query and Filtering](#query-and-filtering)
7. [Component Lifecycle](#component-lifecycle)
8. [Performance Considerations](#performance-considerations)
9. [Usage Examples](#usage-examples)
10. [Best Practices](#best-practices)
11. [API Reference](#api-reference)

---

## Core Concepts

### What is ECS?

ECS is a design pattern that separates:

- **Entities:** Unique identifiers for game objects
- **Components:** Pure data containers (no logic)
- **Systems:** Logic that operates on entities with specific component combinations

### Key Principles

- **Composition over inheritance:** Entities are defined by their components, not class hierarchies
- **Data-oriented design:** Components are plain data structures optimized for cache locality
- **Separation of concerns:** Data (components) is separate from behavior (systems)

### Benefits

- Easy to extend and modify entity behavior
- Better performance through cache-friendly data access
- Simpler testing and debugging
- Natural parallelization opportunities

---

## Architecture

### Class Diagram

```
World
├── EntityManager
│   ├── Entity (ID + metadata)
│   └── Component Storage
├── SystemManager
│   └── Systems (update logic)
└── EventBus
    └── Component/Entity events
```

### Core Classes

#### World

The central container for all ECS data and operations.

```csharp
public class World
{
    public Entity CreateEntity();
    public void DestroyEntity(Entity entity);
    public void Update(float deltaTime);
    public void RegisterSystem<T>(T system) where T : ISystem;
}
```

#### Entity

A lightweight identifier for a game object.

```csharp
public readonly struct Entity : IEquatable<Entity>
{
    public readonly uint Id;
    public readonly uint Version;
}
```

#### Component

Marker interface for component types (pure data).

```csharp
public interface IComponent { }

public struct TransformComponent : IComponent
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
}
```

#### System

Interface for processing logic.

```csharp
public interface ISystem
{
    void Update(World world, float deltaTime);
}
```

---

## Entity Management

### Creating Entities

```csharp
var world = new World();
var entity = world.CreateEntity();
```

### Destroying Entities

```csharp
world.DestroyEntity(entity);
```

Destruction is deferred until the end of the frame to prevent mid-update issues.

### Entity Versioning

Entities use versioning to detect use-after-free bugs:

```csharp
var entity = world.CreateEntity();
world.DestroyEntity(entity);

// Attempting to use the old entity will fail
bool isValid = world.IsEntityValid(entity); // false
```

### Entity Metadata

Entities can have optional metadata for debugging:

```csharp
var entity = world.CreateEntity("Player");
string name = world.GetEntityName(entity); // "Player"
```

---

## Component System

### Adding Components

```csharp
var transform = new TransformComponent
{
    Position = new Vector3(0, 0, 0),
    Scale = Vector3.One
};

entity.AddComponent(transform);
```

### Retrieving Components

```csharp
if (entity.TryGetComponent<TransformComponent>(out var transform))
{
    // Use transform
}
```

### Updating Components

```csharp
ref var transform = ref entity.GetComponent<TransformComponent>();
transform.Position += velocity * deltaTime;
```

### Removing Components

```csharp
entity.RemoveComponent<TransformComponent>();
```

### Component Storage

Components are stored in contiguous arrays for cache efficiency:

```
ComponentArray<TransformComponent>
[Entity1.Transform] [Entity2.Transform] [Entity3.Transform] ...
```

### Component Types

Components must be:

- Structs or classes implementing `IComponent`
- Serializable (for save/load)
- Self-contained (no references to entities or world)

**Good component:**

```csharp
public struct HealthComponent : IComponent
{
    public float CurrentHealth;
    public float MaxHealth;
}
```

**Bad component (has logic):**

```csharp
// Don't do this
public struct HealthComponent : IComponent
{
    public float CurrentHealth;

    public void TakeDamage(float amount)  // Logic belongs in systems
    {
        CurrentHealth -= amount;
    }
}
```

---

## System Processing

### Defining Systems

```csharp
public class MovementSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        var query = world.Query<TransformComponent, VelocityComponent>();

        foreach (var entity in query)
        {
            ref var transform = ref entity.GetComponent<TransformComponent>();
            ref var velocity = ref entity.GetComponent<VelocityComponent>();

            transform.Position += velocity.Value * deltaTime;
        }
    }
}
```

### Registering Systems

```csharp
world.RegisterSystem(new MovementSystem());
world.RegisterSystem(new RenderSystem());
world.RegisterSystem(new PhysicsSystem());
```

### System Execution Order

Systems execute in registration order. For explicit ordering:

```csharp
world.RegisterSystem(new PhysicsSystem(), priority: 100);
world.RegisterSystem(new MovementSystem(), priority: 200);
world.RegisterSystem(new RenderSystem(), priority: 300);
```

Lower priority values execute first.

### System Lifecycle

```csharp
public interface ISystem
{
    void Initialize(World world);      // Called once on registration
    void Update(World world, float dt); // Called every frame
    void Shutdown(World world);         // Called on cleanup
}
```

---

## Query and Filtering

### Basic Queries

Query entities with specific components:

```csharp
// Entities with both Transform and Velocity
var query = world.Query<TransformComponent, VelocityComponent>();
```

### Exclusion Queries

Query entities that don't have certain components:

```csharp
// Entities with Transform but NOT Static
var query = world.Query<TransformComponent>().Exclude<StaticComponent>();
```

### Filter Predicates

Apply custom filters:

```csharp
var query = world.Query<HealthComponent>()
    .Where(e => e.GetComponent<HealthComponent>().CurrentHealth > 0);
```

### Query Caching

Queries are cached for performance:

```csharp
public class HealthSystem : ISystem
{
    private EntityQuery _livingEntities;

    public void Initialize(World world)
    {
        _livingEntities = world.Query<HealthComponent>()
            .Where(e => e.GetComponent<HealthComponent>().CurrentHealth > 0)
            .Cache();
    }

    public void Update(World world, float deltaTime)
    {
        foreach (var entity in _livingEntities)
        {
            // Process living entities
        }
    }
}
```

---

## Component Lifecycle

### Lifecycle Events

The ECS emits events for component changes:

```csharp
world.OnComponentAdded += (entity, component) => { };
world.OnComponentRemoved += (entity, component) => { };
world.OnEntityCreated += (entity) => { };
world.OnEntityDestroyed += (entity) => { };
```

### Cleanup Components

Components can implement cleanup logic:

```csharp
public struct TextureComponent : IComponent, IDisposable
{
    public Texture Texture;

    public void Dispose()
    {
        Texture?.Unload();
    }
}
```

Cleanup is called automatically when the component is removed or the entity is destroyed.

---

## Performance Considerations

### Memory Layout

Components of the same type are stored contiguously for cache efficiency.

**Good:** Sequential access

```csharp
foreach (var entity in world.Query<TransformComponent>())
{
    ref var transform = ref entity.GetComponent<TransformComponent>();
    // Process transform
}
```

**Bad:** Random access

```csharp
foreach (var entityId in randomEntityList)
{
    var entity = world.GetEntity(entityId);
    ref var transform = ref entity.GetComponent<TransformComponent>();
    // Cache misses likely
}
```

### Component Size

Keep components small and focused:

- **Good:** 16-64 bytes per component
- **Acceptable:** 64-256 bytes
- **Bad:** > 256 bytes (consider splitting)

### Component Count per Entity

Optimal: 3-8 components per entity. Too many components can indicate missing abstractions.

### Query Optimization

- Cache queries in systems instead of creating them every frame
- Use exclusion queries to reduce iteration
- Prefer struct components over class components

---

## Usage Examples

### Example 1: Player Entity

```csharp
var player = world.CreateEntity("Player");
player.AddComponent(new TransformComponent
{
    Position = new Vector3(0, 0, 0),
    Scale = Vector3.One
});
player.AddComponent(new VelocityComponent
{
    Value = Vector3.Zero
});
player.AddComponent(new SpriteComponent
{
    TextureId = "player_sprite"
});
player.AddComponent(new HealthComponent
{
    CurrentHealth = 100,
    MaxHealth = 100
});
player.AddComponent(new PlayerInputComponent());
```

### Example 2: Movement System

```csharp
public class MovementSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        var entities = world.Query<TransformComponent, VelocityComponent>();

        foreach (var entity in entities)
        {
            ref var transform = ref entity.GetComponent<TransformComponent>();
            ref var velocity = ref entity.GetComponent<VelocityComponent>();

            transform.Position += velocity.Value * deltaTime;
        }
    }
}
```

### Example 3: Health System

```csharp
public class HealthSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        var entities = world.Query<HealthComponent>();

        foreach (var entity in entities)
        {
            ref var health = ref entity.GetComponent<HealthComponent>();

            if (health.CurrentHealth <= 0)
            {
                world.DestroyEntity(entity);
            }
        }
    }
}
```

---

## Best Practices

### Do's

- ✓ Keep components as pure data structures
- ✓ Put all logic in systems
- ✓ Use composition to define entity types
- ✓ Cache queries in systems
- ✓ Use events for inter-system communication
- ✓ Keep components small and focused
- ✓ Use struct components for better performance
- ✓ Name components descriptively (e.g., `TransformComponent`, not `TC`)

### Don'ts

- ✗ Don't store logic in components
- ✗ Don't store references to entities in components
- ✗ Don't create components larger than 256 bytes
- ✗ Don't modify components during iteration without `ref`
- ✗ Don't create/destroy entities mid-system (use deferred operations)
- ✗ Don't use inheritance for component hierarchies
- ✗ Don't store World or System references in components

### Component Design Guidelines

**Single Responsibility:** Each component represents one aspect of an entity.

```csharp
// Good
public struct PositionComponent : IComponent { public Vector3 Value; }
public struct VelocityComponent : IComponent { public Vector3 Value; }

// Bad (multiple responsibilities)
public struct MovementComponent : IComponent
{
    public Vector3 Position;
    public Vector3 Velocity;
    public float Speed;
}
```

**Avoid Cross-References:**

```csharp
// Bad
public struct FollowComponent : IComponent
{
    public Entity Target; // Don't store entity references
}

// Good
public struct FollowComponent : IComponent
{
    public uint TargetId; // Store ID, resolve in system
}
```

---

## API Reference

### World

```csharp
public class World
{
    // Entity management
    public Entity CreateEntity(string name = null);
    public void DestroyEntity(Entity entity);
    public bool IsEntityValid(Entity entity);

    // Component operations
    public void AddComponent<T>(Entity entity, T component) where T : IComponent;
    public ref T GetComponent<T>(Entity entity) where T : IComponent;
    public bool TryGetComponent<T>(Entity entity, out T component) where T : IComponent;
    public void RemoveComponent<T>(Entity entity) where T : IComponent;
    public bool HasComponent<T>(Entity entity) where T : IComponent;

    // Queries
    public EntityQuery Query<T1>() where T1 : IComponent;
    public EntityQuery Query<T1, T2>() where T1 : IComponent where T2 : IComponent;

    // System management
    public void RegisterSystem<T>(T system, int priority = 0) where T : ISystem;
    public void Update(float deltaTime);

    // Events
    public event Action<Entity, IComponent> OnComponentAdded;
    public event Action<Entity, IComponent> OnComponentRemoved;
    public event Action<Entity> OnEntityCreated;
    public event Action<Entity> OnEntityDestroyed;
}
```

### Entity

```csharp
public readonly struct Entity : IEquatable<Entity>
{
    public readonly uint Id;
    public readonly uint Version;

    public void AddComponent<T>(T component) where T : IComponent;
    public ref T GetComponent<T>() where T : IComponent;
    public bool TryGetComponent<T>(out T component) where T : IComponent;
    public void RemoveComponent<T>() where T : IComponent;
    public bool HasComponent<T>() where T : IComponent;
}
```

### ISystem

```csharp
public interface ISystem
{
    void Initialize(World world);
    void Update(World world, float deltaTime);
    void Shutdown(World world);
}
```

---

## Related Documentation

- [Architecture](../ARCHITECTURE.md)
- [Scenes Module](SCENES.md)
- [Resources Module](RESOURCES.md)
- [Physics Module](PHYSICS.md)

---

**Last Updated:** November 2025  
**Version:** 1.0

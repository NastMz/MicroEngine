# ECS Module (Entity-Component-System)

**Module:** Engine.Core.ECS  
**Status:** Active  
**Version:** 1.0  
**Last Updated:** December 2025

---

## Overview

The ECS (Entity-Component-System) module is the core architectural pattern used by MicroEngine for managing game
objects and their behaviors.

It provides a lightweight, data-oriented approach to game entity management that is:

-   **Performant:** Cache-friendly data layout
-   **Flexible:** Easy to add/remove components at runtime
-   **Maintainable:** Clear separation between data and logic
-   **Dimension-agnostic:** Works for both 2D and 3D entities

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

-   **Entities:** Unique identifiers for game objects
-   **Components:** Pure data containers (no logic)
-   **Systems:** Logic that operates on entities with specific component combinations

### Key Principles

-   **Composition over inheritance:** Entities are defined by their components, not class hierarchies
-   **Data-oriented design:** Components are plain data structures optimized for cache locality
-   **Separation of concerns:** Data (components) is separate from behavior (systems)

### Benefits

-   Easy to extend and modify entity behavior
-   Better performance through cache-friendly data access
-   Simpler testing and debugging
-   Natural parallelization opportunities

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

**Note:** Entities are not destroyed immediately. They are marked for destruction and removed at the end of the frame.

### Clearing the World

The `Clear()` method removes all entities, components, and systems from the world:

```csharp
world.Clear();
```

**When to use:**
- At the beginning of `Scene.OnLoad()` to ensure a clean state
- When reloading a scene to prevent entity accumulation
- When resetting game state

**Example:**
```csharp
public override void OnLoad(SceneContext context)
{
    base.OnLoad(context);

    // IMPORTANT: Clear the world to remove any entities from previous loads
    World.Clear();

    // Now create fresh entities
    CreatePlayer();
    CreateEnemies();
}
```

**Why it's important:**
- Scenes can be cached and reloaded multiple times
- Without `Clear()`, entities from previous loads would accumulate
- Ensures deterministic scene initialization

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

-   Structs or classes implementing `IComponent`
-   Serializable (for save/load)
-   Self-contained (no references to entities or world)

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

## Event System (v0.13.0+)

The Event System provides a decoupled way for entities and systems to communicate without direct references.

### Core Components

#### IEvent Interface

All events must implement the `IEvent` interface:

```csharp
public interface IEvent
{
    DateTime Timestamp { get; }
}
```

#### EventBus
## Event System

The Event System provides decoupled communication between systems and entities using the publish-subscribe pattern.

### EventBus Lifecycle

**Important:** `EventBus` is a **scoped service** that is created per scene and automatically disposed when the scene is unloaded.

**Accessing EventBus:**

```csharp
public override void OnLoad(SceneContext context)
{
    base.OnLoad(context);

    // Get the EventBus for this scene from the service container
    var eventBus = context.Services.GetService<EventBus>();

    // Subscribe to events
    eventBus.Subscribe<PlayerJumpEvent>(OnPlayerJump);
    eventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
}

private void OnPlayerJump(PlayerJumpEvent evt)
{
    Context.Logger.LogInfo($"Player jumped with force {evt.JumpForce}");
}
```

**Automatic Cleanup:**
- EventBus is scoped to the scene
- When the scene is unloaded, the EventBus is automatically disposed
- All subscriptions are cleared automatically
- No manual cleanup required

### Defining Events

Events are simple data classes:

```csharp
public class PlayerJumpEvent
{
    public float JumpForce { get; set; }
    public Vector2 Position { get; set; }
}

public class EnemyDefeatedEvent
{
    public Entity Enemy { get; set; }
    public int ScoreValue { get; set; }
}
```

### Publishing Events

```csharp
public class PlayerSystem : ISystem
{
    private readonly EventBus _eventBus;

    public PlayerSystem(EventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void Update(World world, float deltaTime)
    {
        if (InputManager.IsKeyPressed(Key.Space))
        {
            // Publish event
            _eventBus.Publish(new PlayerJumpEvent
            {
                JumpForce = 500f,
                Position = playerPosition
            });
        }
    }
}
```

### Subscribing to Events

```csharp
public class AudioSystem : ISystem
{
    private readonly EventBus _eventBus;
    private readonly IAudioBackend _audio;

    public AudioSystem(EventBus eventBus, IAudioBackend audio)
    {
        _eventBus = eventBus;
        _audio = audio;

        // Subscribe to events
        _eventBus.Subscribe<PlayerJumpEvent>(OnPlayerJump);
    }

    private void OnPlayerJump(PlayerJumpEvent evt)
    {
        _audio.PlaySound(jumpSound);
    }

    public void Update(World world, float deltaTime)
    {
        // System update logic
    }
}
```

### Event Flow Example

```
PlayerSystem                EventBus                 AudioSystem
     |                          |                          |
     |--- Publish(JumpEvent) -->|                          |
     |                          |--- Notify Subscribers -->|
     |                          |                          |
     |                          |                    OnPlayerJump()
     |                          |                     PlaySound()
```

### Best Practices

- ✓ Use events for decoupled communication
- ✓ Keep event classes simple (data only)
- ✓ Subscribe in constructor or OnLoad
- ✓ EventBus is automatically cleaned up (scoped service)
- ✗ Don't use events for high-frequency updates (use systems instead)
- ✗ Don't store event references (they're transient)

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

### Query Caching (v0.5.0+)

**CachedQuery** provides automatic query result caching for improved performance in frequently executed systems.

#### Benefits

-   **Reduced enumeration overhead:** Query results are cached and only refreshed when components change
-   **Automatic invalidation:** Cached queries are automatically marked dirty when entities add/remove components
-   **Lazy evaluation:** Queries only refresh when accessed after being invalidated
-   **Simple API:** Drop-in replacement for repeated queries with the same component combination

#### Basic Usage

```csharp
public sealed class MovementSystem
{
    private CachedQuery _movableEntities;

    public void Initialize(World world)
    {
        // Create cached query for entities with Transform + Velocity
        _movableEntities = world.CreateCachedQuery<Transform, Velocity>();
    }

    public void Update(World world, float deltaTime)
    {
        // Automatically refreshes if dirty, otherwise uses cached results
        foreach (var entity in _movableEntities.Entities)
        {
            ref var transform = ref world.GetComponent<Transform>(entity);
            ref var velocity = ref world.GetComponent<Velocity>(entity);

            transform.Position += velocity.Value * deltaTime;
        }
    }
}
```

#### Creating Cached Queries

Three overloads are available:

```csharp
// Single component type
var query = world.CreateCachedQuery<Transform>();

// Two component types
var query = world.CreateCachedQuery<Transform, Velocity>();

// Params array (3+ component types)
var query = world.CreateCachedQuery(typeof(Transform), typeof(Velocity), typeof(Health));
```

#### Invalidation and Refresh

Cached queries are automatically invalidated when:

-   A component is added to any entity
-   A component is removed from any entity

```csharp
var query = world.CreateCachedQuery<Transform>();

// Access query (triggers initial refresh)
var count = query.Entities.Count; // Refreshes and caches results
Assert.False(query.IsDirty);

// Add component to any entity
var entity = world.CreateEntity();
world.AddComponent(entity, new Transform());

// Query is now dirty
Assert.True(query.IsDirty);

// Next access automatically refreshes
var updatedCount = query.Entities.Count; // Refreshes cache
Assert.False(query.IsDirty);
```

#### Manual Control

Manual invalidation and refresh are supported for advanced scenarios:

```csharp
// Force query to refresh on next access
query.Invalidate();

// Immediately refresh query (bypasses lazy evaluation)
query.Refresh();
```

#### Performance Characteristics

-   **First access:** O(n) where n = total entities (builds cache)
-   **Subsequent accesses (clean):** O(1) (returns cached list)
-   **Subsequent accesses (dirty):** O(n) (rebuilds cache)
-   **Invalidation:** O(m) where m = number of cached queries (marks all dirty)

#### When to Use Cached Queries

**Good candidates:**

-   Systems that run every frame with the same component query
-   Queries with many component types (expensive to enumerate repeatedly)
-   Read-heavy workloads with infrequent entity/component changes

**Poor candidates:**

-   One-time queries
-   Queries in systems that run infrequently
-   Scenarios with very frequent component add/remove (thrashing)

#### Best Practices

```csharp
// ✅ Cache query in system field, reuse across frames
public sealed class RenderSystem
{
    private CachedQuery _renderableEntities;

    public void Initialize(World world)
    {
        _renderableEntities = world.CreateCachedQuery<Transform, Sprite>();
    }

    public void Update(World world, float deltaTime)
    {
        foreach (var entity in _renderableEntities.Entities)
        {
            // Render entity
        }
    }
}

// ❌ Don't create cached query every frame
public void Update(World world, float deltaTime)
{
    var query = world.CreateCachedQuery<Transform>(); // Creates new query each frame
    foreach (var entity in query.Entities) { }
}

// ✅ Use Count property to check entity count efficiently
if (_renderableEntities.Count > 0)
{
    // Process entities
}

// ❌ Don't enumerate just to check for empty
if (_renderableEntities.Entities.Any()) // Unnecessary enumeration
{
    // Process entities
}
```

#### Multiple Queries and Invalidation

All cached queries are invalidated together when any component changes:

```csharp
var queryA = world.CreateCachedQuery<Transform>();
var queryB = world.CreateCachedQuery<Velocity>();
var queryC = world.CreateCachedQuery<Health>();

// Access all queries
_ = queryA.Entities;
_ = queryB.Entities;
_ = queryC.Entities;

// All clean
Assert.False(queryA.IsDirty);
Assert.False(queryB.IsDirty);
Assert.False(queryC.IsDirty);

// Add component to any entity
world.AddComponent(entity, new Transform());

// All queries invalidated (conservative approach)
Assert.True(queryA.IsDirty);
Assert.True(queryB.IsDirty);
Assert.True(queryC.IsDirty);
```

**Note:** Current implementation uses conservative invalidation (all queries marked dirty on any component
change). Future versions may implement fine-grained invalidation tracking specific component types.

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

-   **Good:** 16-64 bytes per component
-   **Acceptable:** 64-256 bytes
-   **Bad:** > 256 bytes (consider splitting)

### Component Count per Entity

Optimal: 3-8 components per entity. Too many components can indicate missing abstractions.

### Query Optimization

-   Cache queries in systems instead of creating them every frame
-   Use exclusion queries to reduce iteration
-   Prefer struct components over class components

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

-   ✓ Keep components as pure data structures
-   ✓ Put all logic in systems
-   ✓ Use composition to define entity types
-   ✓ Cache queries in systems
-   ✓ Use events for inter-system communication
-   ✓ Keep components small and focused
-   ✓ Use struct components for better performance
-   ✓ Name components descriptively (e.g., `TransformComponent`, not `TC`)

### Don'ts

-   ✗ Don't store logic in components
-   ✗ Don't store references to entities in components
-   ✗ Don't create components larger than 256 bytes
-   ✗ Don't modify components during iteration without `ref`
-   ✗ Don't create/destroy entities mid-system (use deferred operations)
-   ✗ Don't use inheritance for component hierarchies
-   ✗ Don't store World or System references in components

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

-   [Architecture](../ARCHITECTURE.md)
-   [Scenes Module](SCENES.md)
-   [Resources Module](RESOURCES.md)
-   [Physics Module](PHYSICS.md)

---

**Last Updated:** November 2025  
**Version:** 1.0

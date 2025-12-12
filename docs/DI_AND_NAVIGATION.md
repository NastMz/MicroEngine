# Dependency Injection & Scene Navigation

This document supplements the main ARCHITECTURE.md with details on the Dependency Injection container and Scene Navigation pattern.

---

## Dependency Injection Container

MicroEngine includes a lightweight dependency injection (DI) container for managing service lifetimes and dependencies.

### Service Lifetimes

Services can be registered with three different lifetimes:

```csharp
public enum ServiceLifetime
{
    Singleton,  // One instance for the entire application
    Scoped,     // One instance per scope (typically per scene)
    Transient   // New instance every time it's requested
}
```

### IServiceContainer

The core interface for service registration and resolution:

```csharp
public interface IServiceContainer : IDisposable
{
    // Registration
    void RegisterSingleton<TService, TImplementation>() 
        where TImplementation : class, TService;
    void RegisterSingleton<TService>(TService instance);
    
    void RegisterScoped<TService, TImplementation>() 
        where TImplementation : class, TService;
    
    void RegisterTransient<TService, TImplementation>() 
        where TImplementation : class, TService;

    // Resolution
    TService GetService<TService>();
    TService? GetServiceOrNull<TService>();
    
    // Scoping
    IServiceContainer CreateScope();
}
```

### ServiceContainer Implementation

The concrete implementation manages service lifetimes and dependency resolution:

```csharp
var services = new ServiceContainer();

// Register singleton (shared across entire application)
services.RegisterSingleton<ILogger, ConsoleLogger>();

// Register scoped (one per scene)
services.RegisterScoped<EventBus>();
services.RegisterScoped<PhysicsBackendSystem>();

// Register transient (new instance each time)
services.RegisterTransient<ICommandHandler, MyCommandHandler>();

// Resolve services
var logger = services.GetService<ILogger>();
```

### Scoped Services

Scoped services are created once per scope and disposed when the scope ends. This is particularly useful for scene-specific services:

```csharp
// In SceneManager, when loading a scene:
var sceneScope = _serviceContainer.CreateScope();

// Register scene-specific services
sceneScope.RegisterScoped<EventBus>();
sceneScope.RegisterScoped<PhysicsBackendSystem>();

// Pass scope to scene via SceneContext
var context = new SceneContext(
    // ... other services ...
    services: sceneScope,
    navigator: this
);

// When scene is unloaded:
sceneScope.Dispose(); // Automatically disposes all scoped services
```

### Integration with Scenes

Scenes access services through `SceneContext.Services`:

```csharp
public override void OnLoad(SceneContext context)
{
    base.OnLoad(context);

    // Get scoped EventBus for this scene
    var eventBus = context.Services.GetService<EventBus>();
    eventBus.Subscribe<PlayerJumpEvent>(OnPlayerJump);

    // Get scoped physics system
    var physics = context.Services.GetService<PhysicsBackendSystem>();
}
```

### Benefits

- **Automatic Lifecycle Management:** Services are automatically disposed when their scope ends
- **Testability:** Easy to mock services for unit testing
- **Decoupling:** Components depend on interfaces, not concrete implementations
- **Scene Isolation:** Each scene gets its own instances of scoped services (EventBus, PhysicsBackendSystem)

---

## Scene Navigation Pattern (ISceneNavigator)

To eliminate circular dependencies between `Scene` and `SceneManager`, MicroEngine uses the `ISceneNavigator` interface.

### The Problem

Previously, scenes had a direct dependency on `SceneManager`:

```csharp
// OLD (Circular dependency)
public class Scene
{
    private SceneManager _sceneManager; // Scene depends on SceneManager
}

public class SceneManager
{
    private Scene _currentScene; // SceneManager depends on Scene
}
```

This created a circular dependency and made scenes difficult to test.

### The Solution: ISceneNavigator

The `ISceneNavigator` interface abstracts scene navigation:

```csharp
public interface ISceneNavigator
{
    void PushScene(Scene scene);
    void PushScene(Scene scene, SceneParameters parameters);
    void PopScene();
    void ReplaceScene(Scene scene);
    void ReplaceScene(Scene scene, SceneParameters parameters);
    void SetTransition(ISceneTransitionEffect? effect);
}
```

`SceneManager` implements this interface:

```csharp
public sealed class SceneManager : ISceneNavigator
{
    // Implementation of ISceneNavigator methods
}
```

Scenes use the interface instead of the concrete class:

```csharp
public abstract class Scene
{
    private ISceneNavigator? _navigator;

    internal void SetNavigator(ISceneNavigator navigator)
    {
        _navigator = navigator;
    }

    protected void PushScene(Scene scene)
    {
        _navigator?.PushScene(scene);
    }
}
```

### Benefits

- **No Circular Dependency:** Scene depends on interface, not SceneManager
- **Testability:** Easy to mock ISceneNavigator for unit tests
- **Flexibility:** Can swap navigation implementation without changing scenes
- **Dependency Inversion:** High-level policy (Scene) doesn't depend on low-level details (SceneManager)

### Usage in Scenes

Scenes can navigate without knowing about SceneManager:

```csharp
public class MainMenuScene : Scene
{
    public override void OnUpdate(float deltaTime)
    {
        if (Context.InputBackend.IsKeyPressed(Key.Enter))
        {
            // Navigate using protected methods (internally uses ISceneNavigator)
            PushScene(new GameplayScene());
        }
    }
}
```

Or access the navigator directly via SceneContext:

```csharp
public override void OnLoad(SceneContext context)
{
    base.OnLoad(context);

    // Access navigator from context
    context.Navigator.SetTransition(new FadeTransition(context.Renderer, context.Window, 0.5f));
    context.Navigator.ReplaceScene(new LevelScene());
}
```

---

**See Also:**
- [ARCHITECTURE.md](../ARCHITECTURE.md) - Main architecture document
- [SCENES.md](MODULES/SCENES.md) - Scene system documentation
- [ECS.md](MODULES/ECS.md) - Entity-Component-System documentation

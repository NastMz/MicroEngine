# Dependency Injection & Scene Navigation

This document supplements the main ARCHITECTURE.md with details on the Dependency Injection container and Scene Navigation pattern.

---

## Dependency Injection

MicroEngine uses `Microsoft.Extensions.DependencyInjection` for robust, industry-standard dependency management.

### Service Lifetimes

Services can be registered with three standard lifetimes:

- **Singleton**: One instance for the entire application (`AddSingleton`).
- **Scoped**: One instance per scope, typically per scene (`AddScoped`).
- **Transient**: New instance every time it's requested (`AddTransient`).

### Service Registration

Services are registered using `ServiceCollection` at the application startup:

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register singleton (shared across entire application)
services.AddSingleton<ILogger>(new ConsoleLogger());
services.AddSingleton<IGameState, GameState>();

// Register scoped (created per scene)
services.AddScoped<EventBus>();
services.AddScoped<PhysicsBackendSystem>();

// Register transient (new instance each time)
services.AddTransient<ICommandHandler, MyCommandHandler>();

// Build the provider
var serviceProvider = services.BuildServiceProvider();

// Resolve services
var logger = serviceProvider.GetService<ILogger>();
```

### Scoped Services

Scoped services are crucial for scene isolation. They are created when a scope is created and disposed when the scope ends.

```csharp
// In SceneManager, when loading a scene:
using (var scope = _serviceProvider.CreateScope())
{
    // The scope provides access to scoped services like EventBus
    var eventBus = scope.ServiceProvider.GetRequiredService<EventBus>();
    
    // Pass the scoped provider to the scene via SceneContext
    var context = new SceneContext(
        // ... other services ...
        services: scope.ServiceProvider,
        navigator: this
    );
    
    // ... Load and run scene ...
}
// When scope is disposed, EventBus and other scoped services are disposed automatically
```

### Integration with Scenes

Scenes access services through `SceneContext.Services` (which is an `IServiceProvider`):

```csharp
using Microsoft.Extensions.DependencyInjection;

public override void OnLoad(SceneContext context)
{
    base.OnLoad(context);

    // Get scoped EventBus for this scene
    var eventBus = context.Services.GetRequiredService<EventBus>();
    eventBus.Subscribe<PlayerJumpEvent>(OnPlayerJump);

    // Get scoped physics system
    var physics = context.Services.GetRequiredService<PhysicsBackendSystem>();
}
```

### Benefits

- **Standardization:** Uses the standard .NET DI system familiar to most developers.
- **Automatic Lifecycle Management:** Services are automatically disposed when their scope ends.
- **Testability:** Easy to mock `IServiceProvider` for unit testing.
- **Scene Isolation:** Each scene gets its own instances of scoped services (EventBus, PhysicsBackendSystem).

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

# Scenes Module

**Module:** Engine.Core.Scenes  
**Status:** Active  
**Version:** 1.0  
**Last Updated:** December 2025

---

## Overview

The Scenes module provides a robust system for managing game states and transitions in MicroEngine.

A scene represents a discrete game state (main menu, gameplay, pause screen, etc.) with its own:

- Entity world
- Resource set
- Update logic
- Lifecycle hooks

The module ensures:

- **Deterministic transitions:** Scene changes are predictable and controlled
- **Clean separation:** Each scene is isolated and self-contained
- **Resource safety:** Proper cleanup of scene-specific resources
- **State preservation:** Support for scene stacking and pausing

---

## Table of Contents

1. [Core Concepts](#core-concepts)
2. [Architecture](#architecture)
3. [Scene Lifecycle](#scene-lifecycle)
4. [Scene Transitions](#scene-transitions)
5. [Scene Stack](#scene-stack)
6. [Resource Management](#resource-management)
7. [Usage Examples](#usage-examples)
8. [Best Practices](#best-practices)
9. [API Reference](#api-reference)

---

## Core Concepts

### What is a Scene?

A scene is a self-contained game state that encapsulates:

- A world with entities and systems
- Scene-specific resources (textures, sounds, etc.)
- Initialization and cleanup logic
- Update and render behavior

### Scene Types

MicroEngine supports various scene types:

- **Menu scenes:** Title screen, settings, pause menu
- **Gameplay scenes:** Game levels, battles, exploration
- **Transition scenes:** Loading screens, cutscenes
- **Overlay scenes:** HUD, dialogs, tooltips (rendered on top of other scenes)

---

## Architecture

### Class Diagram

```
SceneManager
├── Scene Stack
│   ├── Active Scene
│   ├── Paused Scenes
│   └── Background Scenes
└── Transition Controller
    ├── Transition Type
    ├── Duration
    └── Callbacks
```

### Core Classes

#### SceneContext

Provides scenes with access to all engine services. Passed to scenes during `OnLoad()`.

```csharp
public sealed class SceneContext
{
    // Rendering
    public IWindow Window { get; }
    public IRenderer2D Renderer { get; }

    // Input
    public IInputBackend InputBackend { get; }

    // Time
    public ITimeService TimeService { get; }

    // Logging
    public ILogger Logger { get; }

    // Resources
    public ResourceCache<ITexture> TextureCache { get; }
    public ResourceCache<IAudioClip> AudioCache { get; }

    // Audio
    public IAudioDevice AudioDevice { get; }
    public ISoundPlayer SoundPlayer { get; }
    public IMusicPlayer MusicPlayer { get; }

    // State
    public IGameState GameState { get; }

    // Dependency Injection
    public IServiceContainer Services { get; }

    // Navigation
    public ISceneNavigator Navigator { get; }
}
```

**Usage Example:**

```csharp
public override void OnLoad(SceneContext context)
{
    base.OnLoad(context);

    // Access services
    var texture = context.TextureCache.Load("player.png");
    var mousePos = context.InputBackend.GetMousePosition();
    context.Logger.LogInfo("Scene loaded");

    // Navigate to another scene
    context.Navigator.PushScene(new OtherScene());
}
```

#### Scene

Base class for all scenes.

```csharp
public abstract class Scene : IScene
{
    public string Name { get; }
    protected World World { get; }
    protected SceneContext Context { get; private set; } = null!;

    // Lifecycle methods
    public virtual void OnLoad(SceneContext context) { }
    public virtual void OnLoad(SceneContext context, SceneParameters parameters) { }
    public virtual void OnUnload() { }
    public virtual void OnUpdate(float deltaTime) { }
    public virtual void OnFixedUpdate(float fixedDeltaTime) { }
    public virtual void OnRender() { }

    // Navigation methods (use ISceneNavigator internally)
    protected void PushScene(Scene scene);
    protected void PushScene(Scene scene, SceneParameters parameters);
    protected void PopScene();
    protected void ReplaceScene(Scene scene);
    protected void ReplaceScene(Scene scene, SceneParameters parameters);
}
```

    public virtual void Render(IRenderBackend renderer) { }
}
```

#### SceneManager

Manages scene lifecycle and transitions. Implements `ISceneNavigator` for scene navigation.

```csharp
public sealed class SceneManager : ISceneNavigator
{
    public Scene? CurrentScene { get; }
    public int SceneCount { get; }

    // ISceneNavigator methods
    public void PushScene(Scene scene);
    public void PushScene(Scene scene, SceneParameters parameters);
    public void PopScene();
    public void ReplaceScene(Scene scene);
    public void ReplaceScene(Scene scene, SceneParameters parameters);
    public void SetTransition(ISceneTransitionEffect? effect);

    // Internal engine methods
    public void Initialize(SceneContext context);
    public void Update(float deltaTime);
    public void Render();
    public void Shutdown();
}
```

#### Transition

Defines how scenes transition.

```csharp
public abstract class Transition
{
    public float Duration { get; set; }
    public abstract void Update(float progress);
}
```

---

## Scene Lifecycle

### Lifecycle Phases

```
[Created] → OnLoad → [Active] → OnUnload → [Destroyed]
```

**Note:** The current implementation does not have pause/resume functionality. Scenes are either active or destroyed.

### Lifecycle Hooks

#### OnLoad(SceneContext context)

Called when the scene is loaded and becomes active. This is where you initialize your scene.

```csharp
public override void OnLoad(SceneContext context)
{
    base.OnLoad(context); // IMPORTANT: Call base to set Context property

    // Access engine services via context
    _renderer = context.Renderer;
    _input = context.InputBackend;
    _logger = context.Logger;

    // Load resources
    _backgroundTexture = context.TextureCache.Load("backgrounds/level1.png");

    // Initialize entities
    World.Clear(); // Clear any existing entities (important for scene reloading)
    CreatePlayer();
    CreateEnemies();
}
```

#### OnLoad(SceneContext context, SceneParameters parameters)

Overload that receives parameters passed from the previous scene.

```csharp
public override void OnLoad(SceneContext context, SceneParameters parameters)
{
    base.OnLoad(context, parameters);

    // Access passed parameters
    if (parameters.TryGet<int>("level", out var level))
    {
        _currentLevel = level;
    }

    // Continue initialization
    LoadLevel(_currentLevel);
}
```

#### OnUnload

Called when the scene is being destroyed. Clean up resources here.

```csharp
public override void OnUnload()
{
    // Unload resources (if manually managed)
    // Note: ResourceCache handles most cleanup automatically

    // Stop any ongoing processes
    _musicPlayer?.Stop();

    // Log cleanup
    Context.Logger.LogInfo($"Scene '{Name}' unloaded");

    base.OnUnload();
}
```

#### OnUpdate(float deltaTime)

Called every frame for game logic updates.

```csharp
public override void OnUpdate(float deltaTime)
{
    // Update game logic
    HandleInput();
    UpdatePlayer(deltaTime);
    UpdateEnemies(deltaTime);

    // Update ECS systems
    World.Update(deltaTime);
}
```

#### OnFixedUpdate(float fixedDeltaTime)

Called at a fixed timestep (typically 60 Hz) for physics updates.

```csharp
public override void OnFixedUpdate(float fixedDeltaTime)
{
    // Physics updates happen here
    // This is called by the engine at a fixed rate
}
```

#### OnRender

Called every frame for rendering. The renderer is already set up by the engine.

```csharp
public override void OnRender()
{
    // Rendering is handled by the engine
    // You typically don't need to override this unless doing custom rendering
}
```

public override void OnEnter()
{
    // Initialize scene-specific resources
    _backgroundTexture = ResourceManager.Load<Texture>("backgrounds/menu.png");

    // Create entities
    _player = World.CreateEntity("Player");
    _player.AddComponent(new TransformComponent());

    // Register systems
    World.RegisterSystem(new PlayerInputSystem());
}
```

#### OnExit

Called when the scene is being removed.

```csharp
public override void OnExit()
{
    // Cleanup scene-specific resources
    ResourceManager.Unload(_backgroundTexture);

    // Dispose of the world (automatically destroys all entities)
    World.Dispose();
}
```

#### OnPause

Called when the scene is pushed to the background.

```csharp
public override void OnPause()
{
    // Pause game logic
    _isPaused = true;

    // Stop background music
    AudioManager.Pause("background_music");
}
```

#### OnResume

Called when the scene returns to foreground.

```csharp
public override void OnResume()
{
    // Resume game logic
    _isPaused = false;

    // Resume background music
    AudioManager.Resume("background_music");
}
```

### Update Loop

```csharp
public override void Update(float deltaTime)
{
    // Update world (ECS systems)
    World.Update(deltaTime);

    // Scene-specific logic
    UpdateUI(deltaTime);
}
```

### Rendering

```csharp
public override void Render(IRenderBackend renderer)
{
    // Render background
    renderer.DrawTexture(_backgroundTexture, Vector2.Zero);

    // Render entities (handled by render system)
    World.Render(renderer);

    // Render UI overlay
    RenderUI(renderer);
}
```

---

## Scene Transitions

### Transition Types

#### Instant Transition

Immediate scene change with no animation.

```csharp
sceneManager.ChangeScene(new GameplayScene(), Transition.Instant);
```

#### Fade Transition

Fade out current scene, fade in new scene.

```csharp
sceneManager.ChangeScene(
    new GameplayScene(),
    new FadeTransition(duration: 0.5f, color: Color.Black)
);
```

#### Slide Transition

Slide scenes horizontally or vertically.

```csharp
sceneManager.ChangeScene(
    new GameplayScene(),
    new SlideTransition(direction: SlideDirection.Left, duration: 0.3f)
);
```

#### Custom Transition

Define custom transition effects.

```csharp
public class CircleWipeTransition : Transition
{
    public CircleWipeTransition(float duration) : base(duration) { }

    public override void Update(float progress)
    {
        // Implement circular wipe effect
        float radius = Lerp(0, MaxRadius, progress);
        // Render logic...
    }
}
```

### Transition Callbacks

```csharp
sceneManager.ChangeScene(
    new GameplayScene(),
    new FadeTransition(0.5f),
    onTransitionComplete: () =>
    {
        // Called when transition finishes
        Debug.Log("Transition complete");
    }
);
```

---

## Scene Stack

### Stack-Based Scene Management

MicroEngine supports scene stacking for overlay scenes (pause menus, dialogs, etc.).

```
┌─────────────────┐
│  Pause Menu     │ ← Top (active, renders on top)
├─────────────────┤
│  Gameplay       │ ← Paused (still renders below)
└─────────────────┘
```

### Push Scene

Add a scene on top of the current scene.

```csharp
// In gameplay scene, player pauses the game
sceneManager.PushScene(new PauseMenuScene());
```

The current scene is paused (OnPause called) but remains in memory.

### Pop Scene

Remove the top scene and return to the previous one.

```csharp
// In pause menu, player resumes
sceneManager.PopScene();
```

The previous scene is resumed (OnResume called).

### Stack Operations

```csharp
// Check stack depth
int depth = sceneManager.SceneStackDepth;

// Get scene at specific index
Scene scene = sceneManager.GetSceneAtIndex(index);

// Clear entire stack (except current scene)
sceneManager.ClearStack();
```

---

## Resource Management

### Scene-Specific Resources

Resources loaded in a scene should be unloaded when the scene exits.

```csharp
public class GameplayScene : Scene
{
    private Texture _backgroundTexture;
    private Sound _backgroundMusic;

    public override void OnEnter()
    {
        _backgroundTexture = ResourceManager.Load<Texture>("background.png");
        _backgroundMusic = ResourceManager.Load<Sound>("music.ogg");
    }

    public override void OnExit()
    {
        ResourceManager.Unload(_backgroundTexture);
        ResourceManager.Unload(_backgroundMusic);
    }
}
```

### Shared Resources

Resources used across multiple scenes should be managed globally.

```csharp
// Load shared resources at startup
public class GameBootstrap
{
    public void Initialize()
    {
        // These persist across scene changes
        ResourceManager.LoadGlobal<Font>("fonts/default.ttf");
        ResourceManager.LoadGlobal<Texture>("ui/button.png");
    }
}
```

### Resource Scoping

Use resource scopes to automatically manage scene resources:

```csharp
public override void OnEnter()
{
    using (ResourceManager.BeginScope("GameplayScene"))
    {
        // All resources loaded in this scope are tied to the scene
        var texture = ResourceManager.Load<Texture>("player.png");
        var sound = ResourceManager.Load<Sound>("jump.wav");
    }
    // Scope is registered, resources will be unloaded on OnExit
}

public override void OnExit()
{
    ResourceManager.EndScope("GameplayScene");
    // All scope resources automatically unloaded
}
```

---

## Usage Examples

### Example 1: Simple Menu Scene

```csharp
public class MainMenuScene : Scene
{
    private Texture _backgroundTexture;
    private UIButton _playButton;
    private UIButton _exitButton;

    public MainMenuScene() : base("MainMenu") { }

    public override void OnEnter()
    {
        _backgroundTexture = ResourceManager.Load<Texture>("menu_bg.png");

        _playButton = new UIButton("Play", new Vector2(400, 300));
        _playButton.OnClick += () =>
        {
            SceneManager.ChangeScene(new GameplayScene(), new FadeTransition(0.5f));
        };

        _exitButton = new UIButton("Exit", new Vector2(400, 400));
        _exitButton.OnClick += () =>
        {
            Application.Quit();
        };
    }

    public override void Update(float deltaTime)
    {
        _playButton.Update(deltaTime);
        _exitButton.Update(deltaTime);
    }

    public override void Render(IRenderBackend renderer)
    {
        renderer.DrawTexture(_backgroundTexture, Vector2.Zero);
        _playButton.Render(renderer);
        _exitButton.Render(renderer);
    }

    public override void OnExit()
    {
        ResourceManager.Unload(_backgroundTexture);
    }
}
```

### Example 2: Gameplay Scene with ECS

```csharp
public class GameplayScene : Scene
{
    public GameplayScene() : base("Gameplay") { }

    public override void OnEnter()
    {
        // Register systems
        World.RegisterSystem(new PlayerInputSystem());
        World.RegisterSystem(new MovementSystem());
        World.RegisterSystem(new PhysicsSystem());
        World.RegisterSystem(new RenderSystem());

        // Create player entity
        var player = World.CreateEntity("Player");
        player.AddComponent(new TransformComponent
        {
            Position = new Vector3(100, 100, 0)
        });
        player.AddComponent(new SpriteComponent
        {
            TextureId = "player_sprite"
        });
        player.AddComponent(new VelocityComponent());
        player.AddComponent(new PlayerInputComponent());

        // Create enemies
        for (int i = 0; i < 5; i++)
        {
            var enemy = World.CreateEntity($"Enemy_{i}");
            enemy.AddComponent(new TransformComponent
            {
                Position = new Vector3(200 + i * 50, 200, 0)
            });
            enemy.AddComponent(new SpriteComponent
            {
                TextureId = "enemy_sprite"
            });
            enemy.AddComponent(new AIComponent());
        }
    }

    public override void Update(float deltaTime)
    {
        World.Update(deltaTime);

        // Check pause input
        if (InputManager.IsKeyPressed(Key.Escape))
        {
            SceneManager.PushScene(new PauseMenuScene());
        }
    }

    public override void Render(IRenderBackend renderer)
    {
        World.Render(renderer);
    }
}
```

### Example 3: Pause Menu Scene (Overlay)

```csharp
public class PauseMenuScene : Scene
{
    private UIPanel _panel;

    public PauseMenuScene() : base("PauseMenu") { }

    public override void OnEnter()
    {
        _panel = new UIPanel(new Rect(300, 200, 400, 300));
        _panel.AddButton("Resume", () => SceneManager.PopScene());
        _panel.AddButton("Settings", () => SceneManager.PushScene(new SettingsScene()));
        _panel.AddButton("Main Menu", () =>
        {
            SceneManager.ClearStack();
            SceneManager.ChangeScene(new MainMenuScene());
        });
    }

    public override void Update(float deltaTime)
    {
        _panel.Update(deltaTime);

        // ESC to resume
        if (InputManager.IsKeyPressed(Key.Escape))
        {
            SceneManager.PopScene();
        }
    }

    public override void Render(IRenderBackend renderer)
    {
        // Render semi-transparent overlay
        renderer.DrawRectangle(new Rect(0, 0, 1280, 720), new Color(0, 0, 0, 128));

        _panel.Render(renderer);
    }
}
```

### Example 4: Loading Scene

```csharp
public class LoadingScene : Scene
{
    private Scene _targetScene;
    private float _progress;

    public LoadingScene(Scene targetScene) : base("Loading")
    {
        _targetScene = targetScene;
    }

    public override void OnEnter()
    {
        // Start async resource loading
        ResourceManager.LoadAsync(_targetScene.GetRequiredResources(),
            progress => _progress = progress,
            onComplete: () =>
            {
                SceneManager.ChangeScene(_targetScene, Transition.Instant);
            }
        );
    }

    public override void Update(float deltaTime)
    {
        // Loading happens asynchronously
    }

    public override void Render(IRenderBackend renderer)
    {
        renderer.Clear(Color.Black);
        renderer.DrawText($"Loading... {_progress * 100:F0}%", new Vector2(400, 300));
    }
}
```

---

## Best Practices

### Do's

- ✓ Keep scenes self-contained and independent
- ✓ Load scene resources in `OnEnter`, unload in `OnExit`
- ✓ Use scene stacking for overlays (pause, dialogs)
- ✓ Use transitions for better user experience
- ✓ Implement proper cleanup in `OnExit`
- ✓ Use descriptive scene names for debugging
- ✓ Separate UI logic from game logic

### Don'ts

- ✗ Don't share World instances between scenes
- ✗ Don't hold references to entities from other scenes
- ✗ Don't forget to unload scene-specific resources
- ✗ Don't perform heavy initialization in constructors (use `OnEnter`)
- ✗ Don't assume scene order or dependencies
- ✗ Don't perform synchronous blocking operations in Update
- ✗ Don't mix global and scene resources without careful management

### Scene Design Guidelines

**Single Responsibility:** Each scene should represent one game state.

```csharp
// Good
MainMenuScene, GameplayScene, PauseMenuScene, SettingsScene

// Bad
GameScene (does everything: menu, gameplay, pause, settings)
```

**Resource Isolation:** Scene resources should not leak.

```csharp
// Good
public override void OnExit()
{
    ResourceManager.UnloadAll(scope: Name);
}

// Bad
public override void OnExit()
{
    // Forgot to unload resources
}
```

**Clear Transitions:** Always use appropriate transitions.

```csharp
// Good
SceneManager.ChangeScene(new GameplayScene(), new FadeTransition(0.5f));

// Acceptable
SceneManager.ChangeScene(new GameplayScene()); // Instant transition

// Bad
SceneManager.ChangeScene(new GameplayScene(), new FadeTransition(3.0f)); // Too slow
```

---

## API Reference

### Scene

```csharp
public abstract class Scene
{
    public string Name { get; }
    public World World { get; }
    public bool IsPaused { get; }

    // Lifecycle
    public virtual void OnEnter();
    public virtual void OnExit();
    public virtual void OnPause();
    public virtual void OnResume();

    // Update loop
    public abstract void Update(float deltaTime);
    public virtual void Render(IRenderBackend renderer);
}
```

### SceneManager

```csharp
public class SceneManager
{
    public Scene CurrentScene { get; }
    public int SceneStackDepth { get; }

    // Scene transitions
    public void ChangeScene(Scene scene, Transition transition = null,
        Action onTransitionComplete = null);

    // Scene stack
    public void PushScene(Scene scene);
    public void PopScene();
    public void ClearStack();
    public Scene GetSceneAtIndex(int index);

    // Update loop
    public void Update(float deltaTime);
    public void Render(IRenderBackend renderer);
}
```

### Transition

```csharp
public abstract class Transition
{
    public float Duration { get; set; }
    public float Progress { get; }
    public bool IsComplete { get; }

    public abstract void Update(float progress);
    public virtual void Render(IRenderBackend renderer);
}

// Built-in transitions
public class InstantTransition : Transition;
public class FadeTransition : Transition;
public class SlideTransition : Transition;
public class CrossfadeTransition : Transition;
```

---

## Related Documentation

- [Architecture](../ARCHITECTURE.md)
- [ECS Module](ECS.md)
- [Resources Module](RESOURCES.md)
- [Graphics Backend](GRAPHICS_BACKEND.md)

---

**Last Updated:** November 2025  
**Version:** 1.0

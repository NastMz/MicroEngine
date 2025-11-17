# MicroEngine – Architecture Overview

**Version:** 1.0  
**Status:** Reference  
**Author:** Kevin Martínez  
**Last Updated:** 2024  

---

## Overview

MicroEngine uses a clean, layered, dimension-agnostic architecture designed for clarity, maintainability, and long-term scalability.

This document describes how the engine is structured internally and how all its subsystems interact.

**Related Documents:**
- ?? [Core Requirements](CORE_REQUIREMENTS.md) – Mandatory technical rules
- ?? [Engine Design Document](ENGINE_DESIGN_DOCUMENT.md) – Vision and goals
- ?? [Roadmap](ROADMAP.md) – Development timeline

---

## Table of Contents

1. [Architectural Principles](#1-architectural-principles)
2. [High-Level Layer Diagram](#2-high-level-layer-diagram)
3. [Engine Core Structure](#3-engine-core-structure)
4. [Backend Architecture](#4-backend-architecture)
5. [Game Layer Architecture](#5-game-layer-architecture)
6. [Update & Render Cycle](#6-update--render-cycle)
7. [Resource Handling](#7-resource-handling)
8. [Data Flow and Communication](#8-data-flow-and-communication)
9. [Dependency Management](#9-dependency-management)
10. [Extensibility Points](#10-extensibility-points)
11. [Error Handling Architecture](#11-error-handling-architecture)
12. [Threading Model](#12-threading-model)
13. [Memory Management](#13-memory-management)
14. [Summary](#14-summary)

---

# 1. Architectural Principles

MicroEngine follows these core principles:

### **1.1 Dimension-Agnostic Core**

The engine does not assume 2D or 3D.

**Key design decisions:**
- Current modules are 2D-focused
- All systems are built to support 3D extensions in the future without redesign
- APIs use generic types (`Vector3` even if Z is unused in 2D)
- Transform hierarchies support any dimensionality
- Camera systems are abstracted from projection type

**Rationale:**
Prevents architectural rewrites when adding 3D support in version 2.x.

### **1.2 Strict Layered Design**

The engine is divided into three distinct layers:

1. **Engine Core** – Platform-independent logic, ECS, scenes, update loop
2. **Backends** – Concrete implementations for rendering, input, audio
3. **Game Layer** – User code, assets, gameplay

**Layer isolation rules:**
- ? Upper layers can depend on lower layers
- ? Lower layers cannot depend on upper layers
- ? Backends implement interfaces defined by core
- ? Core never imports backend-specific code

### **1.3 Separation of Concerns**

Each subsystem has one clear responsibility:

| Subsystem | Responsibility |
|-----------|----------------|
| **Render** | Drawing primitives, textures, geometry |
| **Input** | User interaction (keyboard, mouse, gamepad) |
| **Audio** | Sound effects and music playback |
| **ECS** | Entity-component-system framework |
| **Scene Manager** | Game state transitions |
| **Resource Manager** | Asset lifetime and caching |
| **Engine Loop** | Update timing and fixed timestep |
| **Physics** | Collision detection and spatial queries |

**Benefits:**
- Easy to understand
- Simple to test in isolation
- Replaceable subsystems
- Clear API boundaries

### **1.4 Backend Independence**

The engine core **never** imports rendering libraries (Raylib, OpenGL, SDL, etc.).

**Implementation:**
- Core defines abstract interfaces (`IRenderBackend`, `IInputBackend`, `IAudioBackend`)
- Backends implement these interfaces
- Game selects backend at startup
- Backend can be swapped without changing engine code

**Example:**
```csharp
// Engine Core (no dependency on Raylib)
public interface IRenderBackend
{
    void DrawSprite(TextureHandle texture, Vector2 position);
}

// Backend implementation
public class RaylibRenderBackend : IRenderBackend
{
    public void DrawSprite(TextureHandle texture, Vector2 position)
    {
        Raylib.DrawTexture(GetRaylibTexture(texture), position.X, position.Y, WHITE);
    }
}

// Game startup
var backend = new RaylibRenderBackend();
var engine = new GameEngine(backend);
```

### **1.5 Deterministic Behaviour**

Logic uses a fixed timestep; rendering is decoupled.

**Guarantees:**
- ? Same inputs produce same outputs
- ? Gameplay is frame-rate independent
- ? Physics simulation is consistent
- ? Enables replay systems
- ? Supports network deterministic lockstep

**Implementation:**
See [Section 6: Update & Render Cycle](#6-update--render-cycle)

---

# 2. High-Level Layer Diagram

```
??????????????????????????????????????????????????????????
?                    Game Layer                          ?
?  (Scenes, entities, gameplay logic, assets)            ?
?                                                        ?
?  Examples:                                             ?
?  - MainMenuScene, GameplayScene                        ?
?  - Player, Enemy entities                              ?
?  - Game-specific components and systems                ?
??????????????????????????????????????????????????????????
         ?                           ?
         ? depends on                ? selects backend
         ?                           ?
??????????????????????????????????????????????????????????
?              Engine Backends Layer                     ?
?  Render, Input, Audio implementations                  ?
?                                                        ?
?  Concrete implementations:                             ?
?  - MicroEngine.Backend.Raylib                          ?
?  - MicroEngine.Backend.OpenGL                          ?
?  - MicroEngine.Backend.SDL                             ?
?  - MicroEngine.Backend.Null (for testing)              ?
??????????????????????????????????????????????????????????
                        ?
                        ? implements interfaces
                        ?
??????????????????????????????????????????????????????????
?                  Engine Core                           ?
?  ECS • Scenes • Time • Resources • Physics             ?
?                                                        ?
?  Defines interfaces:                                   ?
?  - IRenderBackend                                      ?
?  - IInputBackend                                       ?
?  - IAudioBackend                                       ?
?  - IWindowBackend                                      ?
??????????????????????????????????????????????????????????
```

**Dependency flow:**
```
Game ? Engine Core ? Backends (implements interfaces)
Game ? Backends (selects concrete implementation)
```

---

# 3. Engine Core Structure

The **MicroEngine.Core** project contains all platform-independent logic.

## **3.1 Core Module Structure**

```
MicroEngine.Core/
?
??? Loop/
?   ??? GameLoop.cs              ? Main update loop
?   ??? FixedTimestep.cs         ? Timestep accumulator
?   ??? Time.cs                  ? Time utilities
?
??? Scenes/
?   ??? Scene.cs                 ? Base scene class
?   ??? SceneManager.cs          ? Scene lifecycle
?   ??? SceneTransition.cs       ? Transition helpers
?
??? ECS/
?   ??? Entity.cs                ? Entity identifier
?   ??? Component.cs             ? Base component
?   ??? System.cs                ? Base system
?   ??? World.cs                 ? ECS world manager
?   ??? Query.cs                 ? Entity queries
?
??? Resources/
?   ??? ResourceManager.cs       ? Asset loading/unloading
?   ??? Handles/
?   ?   ??? TextureHandle.cs
?   ?   ??? SoundHandle.cs
?   ?   ??? MusicHandle.cs
?   ??? Loaders/
?       ??? IResourceLoader.cs
?       ??? ResourceValidator.cs
?
??? Physics/
?   ??? Collider.cs              ? Base collider
?   ??? AABB.cs                  ? 2D axis-aligned box
?   ??? CollisionSystem.cs       ? Collision detection
?   ??? SpatialHash.cs           ? Spatial partitioning
?
??? Input/
?   ??? IInputBackend.cs         ? Input interface
?   ??? InputManager.cs          ? Input state tracking
?   ??? KeyCode.cs               ? Key definitions
?
??? Audio/
?   ??? IAudioBackend.cs         ? Audio interface
?   ??? AudioManager.cs          ? Audio system wrapper
?
??? Graphics/
?   ??? IRenderBackend.cs        ? Render interface
?   ??? IWindowBackend.cs        ? Window interface
?   ??? Renderer.cs              ? High-level render API
?   ??? Camera.cs                ? Camera abstraction
?   ??? Color.cs                 ? Color utilities
?   ??? Primitives/
?       ??? Rectangle.cs
?       ??? Circle.cs
?
??? Math/
?   ??? Vector2.cs               ? 2D vector
?   ??? Vector3.cs               ? 3D vector (future-ready)
?   ??? Matrix3x2.cs             ? 2D transformations
?   ??? Matrix4x4.cs             ? 3D transformations (future)
?   ??? MathHelper.cs            ? Utility functions
?
??? Utilities/
    ??? Logger.cs                ? Logging system
    ??? Profiler.cs              ? Performance profiling
    ??? EventBus.cs              ? Event system
```

---

## **3.2 Core Module Details**

### **3.2.1 Loop Module**

**Responsibilities:**
- Manages fixed timestep logic
- Handles variable-rate rendering
- Accumulates delta time
- Implements frame skipping when needed
- Provides profiling hooks

**Key classes:**
```csharp
public class GameLoop
{
    private const double FixedTimeStep = 1.0 / 60.0;
    private double _accumulator = 0.0;
    
    public void Run()
    {
        while (_running)
        {
            double frameTime = GetFrameTime();
            _accumulator += frameTime;
            
            // Fixed update
            while (_accumulator >= FixedTimeStep)
            {
                Update(FixedTimeStep);
                _accumulator -= FixedTimeStep;
            }
            
            // Variable render
            double interpolation = _accumulator / FixedTimeStep;
            Render(interpolation);
        }
    }
}
```

### **3.2.2 Scene System**

**A scene encapsulates:**
- World state (entities, components)
- ECS systems specific to that scene
- Initialization and cleanup logic
- Independent update/draw cycles

**Scene lifecycle:**
```
Create ? OnEnter ? Update/Draw loop ? OnExit ? Dispose
```

**SceneManager responsibilities:**
- Ensures clean transitions without state leakage
- Validates new scene before switching
- Handles scene stack (for overlays)
- Manages async scene loading

**Example:**
```csharp
public abstract class Scene
{
    public abstract void OnEnter();
    public abstract void Update(float deltaTime);
    public abstract void Draw(float interpolation);
    public abstract void OnExit();
}

public class SceneManager
{
    private Scene? _currentScene;
    private Scene? _nextScene;
    
    public void TransitionTo(Scene newScene)
    {
        _nextScene = newScene;
    }
    
    private void PerformTransition()
    {
        _currentScene?.OnExit();
        _currentScene = _nextScene;
        _currentScene?.OnEnter();
        _nextScene = null;
    }
}
```

### **3.2.3 ECS Module**

A simple but robust ECS implementation:

**Design philosophy:**
- **Entity**: Lightweight identifier (just an ID)
- **Component**: Pure data containers (no logic)
- **System**: Logic applied to entities with specific components

**Benefits:**
- Clear separation of data and logic
- Easy to reason about
- Deterministic execution order
- Simple debugging

**Example:**
```csharp
// Component (data only)
public struct TransformComponent
{
    public Vector2 Position;
    public float Rotation;
    public Vector2 Scale;
}

// System (logic only)
public class MovementSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.Query<TransformComponent, VelocityComponent>())
        {
            ref var transform = ref entity.Get<TransformComponent>();
            ref var velocity = ref entity.Get<VelocityComponent>();
            
            transform.Position += velocity.Value * deltaTime;
        }
    }
}
```

### **3.2.4 Resource Manager**

**Handles asset lifetimes using:**
- **Handles** – Opaque references to resources
- **Validation** – Check resource integrity before use
- **Atomic loading** – All-or-nothing resource creation
- **Reference counting** – Optional automatic cleanup

**Resource handle pattern:**
```csharp
public struct TextureHandle
{
    internal uint Id { get; }
    internal int Version { get; }
    
    public bool IsValid => Version >= 0;
}

public class ResourceManager
{
    private Dictionary<uint, TextureData> _textures = new();
    private uint _nextId = 1;
    
    public TextureHandle LoadTexture(string path)
    {
        // Validate, load, register
        var data = LoadAndValidate(path);
        var id = _nextId++;
        _textures[id] = data;
        return new TextureHandle { Id = id, Version = 0 };
    }
    
    public void Unload(TextureHandle handle)
    {
        if (_textures.Remove(handle.Id))
        {
            // Cleanup
        }
    }
}
```

### **3.2.5 Physics Module**

**Current implementation:**
- 2D collision detection (AABB, circles)
- Simple spatial queries
- Overlap tests
- Ray casting

**Future-ready design:**
- Module is structured so a 3D physics backend can be added later
- Abstraction allows swapping physics engines
- Deterministic for networking support

**Example:**
```csharp
public struct AABB
{
    public Vector2 Min;
    public Vector2 Max;
    
    public bool Overlaps(AABB other)
    {
        return Min.X < other.Max.X && Max.X > other.Min.X &&
               Min.Y < other.Max.Y && Max.Y > other.Min.Y;
    }
}
```

### **3.2.6 Backend Interfaces**

The core defines abstract interfaces that backends must implement:

**Rendering:**
```csharp
public interface IRenderBackend
{
    void BeginFrame();
    void Clear(Color color);
    void DrawSprite(TextureHandle texture, Vector2 position, Rectangle? source = null);
    void DrawRectangle(Rectangle rect, Color color);
    void DrawText(string text, Vector2 position, FontHandle font, Color color);
    void EndFrame();
    
    // Texture management
    TextureHandle CreateTexture(int width, int height, byte[] data);
    void DeleteTexture(TextureHandle handle);
}
```

**Input:**
```csharp
public interface IInputBackend
{
    bool GetKeyDown(KeyCode key);
    bool GetKeyUp(KeyCode key);
    bool GetKeyHeld(KeyCode key);
    
    Vector2 GetMousePosition();
    bool GetMouseButtonDown(MouseButton button);
}
```

**Audio:**
```csharp
public interface IAudioBackend
{
    void PlaySound(SoundHandle sound, float volume = 1.0f);
    void PlayMusic(MusicHandle music, bool loop = true);
    void StopMusic();
    
    SoundHandle LoadSound(string path);
    MusicHandle LoadMusic(string path);
}
```

**Window:**
```csharp
public interface IWindowBackend
{
    void Create(string title, int width, int height);
    void Close();
    bool ShouldClose();
    Vector2 GetSize();
}
```

**Note:** Using `Vector2`/`Vector3` keeps the architecture 3D-capable even if current implementations use only X/Y.

---

# 4. Backend Architecture

Backends exist as **separate assemblies** (projects):

```
MicroEngine.Backend.Raylib/
MicroEngine.Backend.OpenGL/
MicroEngine.Backend.SDL/
MicroEngine.Backend.Null/      (headless for testing)
```

## **4.1 Backend Responsibilities**

Each backend implements:
- ? Rendering (window management, drawing)
- ? Input handling (keyboard, mouse, gamepad)
- ? Audio playback (sounds, music)
- ? Resource loading (textures, fonts)

## **4.2 Backend Requirements**

Backends must:
- ? Remain isolated from engine core internals
- ? Be changeable at compile time or runtime
- ? Never modify engine logic
- ? Fail gracefully with clear errors
- ? Validate resources before creating handles
- ? Pass conformance test suite

## **4.3 Example Backend Structure**

```
MicroEngine.Backend.Raylib/
?
??? Rendering/
?   ??? RaylibRenderBackend.cs   ? Implements IRenderBackend
?   ??? TextureManager.cs         ? Handle to Raylib texture mapping
?   ??? WindowManager.cs          ? Window creation and events
?
??? Input/
?   ??? RaylibInputBackend.cs    ? Implements IInputBackend
?   ??? InputMapper.cs            ? Map engine keys to Raylib keys
?
??? Audio/
?   ??? RaylibAudioBackend.cs    ? Implements IAudioBackend
?   ??? AudioCache.cs             ? Sound/music caching
?
??? RaylibBackendFactory.cs      ? Creates all backend instances
```

## **4.4 Backend Factory Pattern**

```csharp
public class RaylibBackendFactory
{
    public IRenderBackend CreateRenderBackend() => new RaylibRenderBackend();
    public IInputBackend CreateInputBackend() => new RaylibInputBackend();
    public IAudioBackend CreateAudioBackend() => new RaylibAudioBackend();
    public IWindowBackend CreateWindowBackend() => new RaylibWindowBackend();
}

// Usage in game
var factory = new RaylibBackendFactory();
var engine = new GameEngine(
    factory.CreateRenderBackend(),
    factory.CreateInputBackend(),
    factory.CreateAudioBackend(),
    factory.CreateWindowBackend()
);
```

## **4.5 Backend Isolation Rules**

**Allowed:**
- ? Backend can use third-party libraries (Raylib, SDL, OpenGL)
- ? Backend can cache internal state
- ? Backend can optimize rendering
- ? Backend can provide extensions via optional interfaces

**Forbidden:**
- ? Backend cannot modify engine state
- ? Backend cannot access core internals
- ? Backend cannot change engine behavior
- ? Backend cannot bypass validation

---

# 5. Game Layer Architecture

The game layer is any project that uses MicroEngine.

## **5.1 Typical Game Structure**

```
MyGame/
?
??? Scenes/
?   ??? MainMenuScene.cs         ? Menu UI
?   ??? GameplayScene.cs         ? Main game loop
?   ??? PauseMenuScene.cs        ? Pause overlay
?   ??? GameOverScene.cs         ? End screen
?
??? Entities/
?   ??? Player.cs                ? Player entity factory
?   ??? Enemy.cs                 ? Enemy types
?   ??? Projectile.cs            ? Bullets, etc.
?
??? Components/
?   ??? HealthComponent.cs       ? HP system
?   ??? DamageComponent.cs       ? Damage on collision
?   ??? AIComponent.cs           ? AI behavior data
?
??? Systems/
?   ??? PlayerControlSystem.cs  ? Handle player input
?   ??? AISystem.cs              ? Enemy AI logic
?   ??? CombatSystem.cs          ? Damage calculation
?   ??? RenderSystem.cs          ? Drawing entities
?
??? Assets/
?   ??? sprites/
?   ?   ??? player.png
?   ?   ??? enemy.png
?   ??? audio/
?   ?   ??? shoot.wav
?   ?   ??? music.ogg
?   ??? data/
?       ??? levels.json
?
??? Program.cs                   ? Entry point
```

## **5.2 Game Layer Responsibilities**

The game layer is responsible for:

1. **Backend Selection**
   ```csharp
   var backend = new RaylibBackendFactory();
   var engine = new GameEngine(backend);
   ```

2. **Scene Registration**
   ```csharp
   engine.SceneManager.Register("menu", new MainMenuScene());
   engine.SceneManager.Register("game", new GameplayScene());
   engine.SceneManager.SetActive("menu");
   ```

3. **Asset Management**
   ```csharp
   var player = resources.LoadTexture("sprites/player.png");
   var music = resources.LoadMusic("audio/theme.ogg");
   ```

4. **Component Definition**
   ```csharp
   public struct PlayerComponent
   {
       public int Health;
       public int Score;
   }
   ```

5. **System Implementation**
   ```csharp
   public class PlayerControlSystem : ISystem
   {
       public void Update(World world, float deltaTime)
       {
           // Game-specific logic
       }
   }
   ```

## **5.3 Game Entry Point**

```csharp
public class Program
{
    public static void Main()
    {
        // Create backend
        var backendFactory = new RaylibBackendFactory();
        
        // Create engine
        var engine = new GameEngine(
            backendFactory.CreateRenderBackend(),
            backendFactory.CreateInputBackend(),
            backendFactory.CreateAudioBackend(),
            backendFactory.CreateWindowBackend()
        );
        
        // Configure
        engine.Window.Create("My Game", 800, 600);
        
        // Register scenes
        engine.SceneManager.Register("menu", new MainMenuScene());
        engine.SceneManager.Register("game", new GameplayScene());
        engine.SceneManager.SetActive("menu");
        
        // Run
        engine.Run();
    }
}
```

---

# 6. Update & Render Cycle

MicroEngine uses the **"Fix Your Timestep"** pattern for deterministic gameplay.

## **6.1 Fixed Update Loop**

**Runs at constant frequency (default: 60 updates/s)**

**Executed during update:**
- ? ECS systems update
- ? Physics simulation
- ? Input processing
- ? Scene logic
- ? Audio updates

**Characteristics:**
- Fixed delta time (always 0.01666s for 60 Hz)
- Frame-rate independent
- Deterministic
- Predictable

## **6.2 Variable Render Loop**

**Runs as fast as the backend allows (uncapped or V-synced)**

**Executed during render:**
- ? Draws current scene
- ? Interpolates between states for smoothness
- ? UI rendering
- ? Debug overlays

**Characteristics:**
- Variable delta time
- Never modifies game state
- May skip frames if update falls behind
- Can run faster than update for smooth visuals

## **6.3 Timing Diagram**

```
Time ?????????????????????????????????????????????????

Frame 1: ???? Update ???? Render ??
Frame 2: ???? Update ???? Update ???? Render ????
Frame 3: ???? Update ???? Render ??
Frame 4: ???? Update ???? Render ?? Render ??
         ?              ?
         ?              ?? Extra render (high FPS)
         ?? Accumulator reached 2x fixed timestep
```

## **6.4 Implementation**

```csharp
public class GameLoop
{
    private const double FixedTimeStep = 1.0 / 60.0;  // 60 Hz
    private const double MaxAccumulator = 0.25;        // Spiral of death prevention
    
    private double _accumulator = 0.0;
    private double _currentTime;
    private double _newTime;
    
    public void Run()
    {
        _currentTime = GetTime();
        
        while (!ShouldQuit())
        {
            // Measure frame time
            _newTime = GetTime();
            double frameTime = _newTime - _currentTime;
            _currentTime = _newTime;
            
            // Accumulate time
            _accumulator += frameTime;
            
            // Prevent spiral of death
            if (_accumulator > MaxAccumulator)
                _accumulator = MaxAccumulator;
            
            // Fixed update (may run multiple times or not at all)
            while (_accumulator >= FixedTimeStep)
            {
                ProcessInput();
                UpdateSystems(FixedTimeStep);
                _accumulator -= FixedTimeStep;
            }
            
            // Variable render (always runs once per frame)
            double interpolation = _accumulator / FixedTimeStep;
            RenderScene(interpolation);
        }
    }
}
```

## **6.5 Interpolation**

For smooth rendering at high frame rates:

```csharp
public void Render(double alpha)
{
    // Interpolate between previous and current state
    var renderPos = Vector2.Lerp(previousPosition, currentPosition, alpha);
    renderer.DrawSprite(sprite, renderPos);
}
```

---

# 7. Resource Handling

## **7.1 Resource Loading Pipeline**

**All loading is:**
1. **Validated** – Check format, size, corruption
2. **Atomic** – All-or-nothing (no partial loads)
3. **Explicit** – No silent failures

**Loading flow:**
```
Request ? Validate Path ? Load Raw Data ? Validate Format ? 
Create Backend Resource ? Register Handle ? Return Handle
```

## **7.2 Handle System**

The core **never stores raw backend data**; instead, it uses handles:

| Resource Type | Handle Type | Backend Mapping |
|---------------|-------------|-----------------|
| Textures | `TextureHandle` | GPU texture ID |
| Audio | `SoundHandle` | Audio buffer ID |
| Music | `MusicHandle` | Stream handle |
| Fonts | `FontHandle` | Font atlas |
| Meshes (future) | `MeshHandle` | Vertex buffer ID |

**Handle structure:**
```csharp
public struct TextureHandle
{
    internal uint Id { get; }
    internal int Version { get; }
    
    public bool IsValid => Version >= 0;
}
```

**Benefits:**
- ? Opaque references (no direct memory access)
- ? Detect use-after-free (version check)
- ? Enable hot-reloading
- ? Backend-independent
- ? Type-safe

## **7.3 Resource Lifecycle**

```
Load ? Validate ? Use ? Unload ? Invalidate Handle
```

**Example:**
```csharp
// Load
TextureHandle player = resources.LoadTexture("player.png");

// Use
renderer.DrawSprite(player, position);

// Unload
resources.Unload(player);

// Handle is now invalid
if (!player.IsValid)
    throw new InvalidOperationException("Texture was unloaded");
```

## **7.4 Asset Organization**

```
Assets/
??? textures/
?   ??? sprites/
?   ??? ui/
?   ??? backgrounds/
??? audio/
?   ??? sfx/
?   ??? music/
??? fonts/
??? data/
?   ??? levels/
?   ??? config/
??? shaders/  (future)
```

---

# 8. Data Flow and Communication

## **8.1 System Communication Patterns**

### **8.1.1 Direct Communication (ECS)**
```csharp
// Systems operate on shared components
public class DamageSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.Query<HealthComponent, DamageComponent>())
        {
            ref var health = ref entity.Get<HealthComponent>();
            var damage = entity.Get<DamageComponent>();
            
            health.Value -= damage.Amount;
        }
    }
}
```

### **8.1.2 Event-Based Communication**
```csharp
// Decoupled communication via events
public class EventBus
{
    private Dictionary<Type, List<Delegate>> _handlers = new();
    
    public void Subscribe<T>(Action<T> handler)
    {
        // Register handler
    }
    
    public void Publish<T>(T evt)
    {
        // Invoke all handlers
    }
}

// Usage
eventBus.Subscribe<PlayerDiedEvent>(evt => 
{
    sceneManager.TransitionTo(new GameOverScene());
});

eventBus.Publish(new PlayerDiedEvent { Player = playerEntity });
```

### **8.1.3 Message Queue (Threading)**
```csharp
// Cross-thread communication
public class MessageQueue
{
    private ConcurrentQueue<IMessage> _queue = new();
    
    public void Enqueue(IMessage message)
    {
        _queue.Enqueue(message);
    }
    
    public void ProcessAll()
    {
        while (_queue.TryDequeue(out var message))
        {
            HandleMessage(message);
        }
    }
}
```

## **8.2 Data Flow Diagram**

```
???????????????
?   Input     ?
?  Backend    ?
???????????????
       ? Raw input events
       ?
???????????????
? Input       ?
? Manager     ?
???????????????
       ? Processed input state
       ?
???????????????      ????????????????
? ECS Systems ???????? Components   ?
???????????????      ????????????????
       ? Render data
       ?
???????????????      ????????????????
?  Renderer   ????????   Render     ?
?             ?      ?   Backend    ?
???????????????      ????????????????
```

---

# 9. Dependency Management

## **9.1 Dependency Graph**

```
Game Layer
    ??? depends on ? MicroEngine.Core
    ??? depends on ? MicroEngine.Backend.* (selected backend)

MicroEngine.Backend.Raylib
    ??? depends on ? MicroEngine.Core (interfaces only)
    ??? depends on ? Raylib-cs (third-party)

MicroEngine.Core
    ??? depends on ? .NET Standard 2.1 (no external dependencies)
```

## **9.2 Dependency Injection**

```csharp
// Core defines interfaces
public interface IRenderer
{
    void Draw(TextureHandle texture, Vector2 position);
}

// Backend implements
public class RaylibRenderer : IRenderer { }

// Game injects dependency
public class GameplayScene
{
    private readonly IRenderer _renderer;
    
    public GameplayScene(IRenderer renderer)
    {
        _renderer = renderer;
    }
}

// Wiring at startup
var renderer = new RaylibRenderer();
var scene = new GameplayScene(renderer);
```

## **9.3 Package References**

**MicroEngine.Core.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <!-- No external dependencies -->
</Project>
```

**MicroEngine.Backend.Raylib.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\MicroEngine.Core\MicroEngine.Core.csproj" />
    <PackageReference Include="Raylib-cs" Version="6.0.0" />
  </ItemGroup>
</Project>
```

---

# 10. Extensibility Points

MicroEngine is designed for long-term growth through well-defined extension points.

## **10.1 Current Extension Points**

### **Custom Backends**
```csharp
// Create your own backend
public class MyCustomBackend : IRenderBackend
{
    // Implement interface
}
```

### **Custom Components**
```csharp
// Add game-specific components
public struct WeaponComponent
{
    public int Damage;
    public float FireRate;
}
```

### **Custom Systems**
```csharp
// Add game-specific systems
public class WeaponSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        // Custom logic
    }
}
```

### **Custom Resource Loaders**
```csharp
// Add custom resource types
public class LevelLoader : IResourceLoader<LevelData>
{
    public LevelData Load(string path)
    {
        // Custom loading logic
    }
}
```

## **10.2 Future Extension Points**

### **3D Support**
- 3D transforms (`Matrix4x4`)
- 3D cameras (perspective projection)
- Mesh rendering
- 3D physics
- Shader/material system

### **Advanced Features**
- Particle systems
- Animation systems
- UI framework
- Scripting integration (Lua, C#)
- Networking layer
- Editor tools

## **10.3 Plugin Architecture (Future)**

```csharp
public interface IEnginePlugin
{
    void Initialize(GameEngine engine);
    void Update(float deltaTime);
    void Shutdown();
}

// Usage
engine.RegisterPlugin(new PhysicsDebugPlugin());
engine.RegisterPlugin(new ProfilingPlugin());
```

---

# 11. Error Handling Architecture

## **11.1 Error Handling Strategy**

MicroEngine uses a **fail-fast** approach with explicit errors.

### **Exception Hierarchy**
```csharp
public class MicroEngineException : Exception { }
    ??? ResourceLoadException
    ??? SceneTransitionException
    ??? BackendException
    ??? InvalidStateException
    ??? ComponentNotFoundException
```

### **Error Boundaries**
```csharp
public class GameLoop
{
    public void Update(float deltaTime)
    {
        try
        {
            // Update all systems
            foreach (var system in _systems)
            {
                system.Update(_world, deltaTime);
            }
        }
        catch (MicroEngineException ex)
        {
            Log.Error("Engine error during update", ex);
            // Attempt recovery or enter safe state
        }
        catch (Exception ex)
        {
            Log.Fatal("Unexpected error during update", ex);
            // Critical failure - shutdown gracefully
        }
    }
}
```

## **11.2 Validation Points**

1. **API Boundaries** – Validate all public method inputs
2. **Resource Loading** – Validate file format and integrity
3. **Scene Transitions** – Validate new scene before switching
4. **Handle Access** – Validate handle is still valid
5. **Backend Calls** – Validate parameters before passing to backend

## **11.3 Error Recovery**

```csharp
// Resource loading with fallback
public TextureHandle LoadTextureOrDefault(string path, TextureHandle fallback)
{
    try
    {
        return LoadTexture(path);
    }
    catch (ResourceLoadException ex)
    {
        Log.Warning($"Failed to load {path}, using fallback", ex);
        return fallback;
    }
}
```

---

# 12. Threading Model

## **12.1 Core Threading Rules**

MicroEngine follows a **single-threaded core** model:

1. **Main thread** – All engine logic (update, render, ECS)
2. **Worker threads** – Async operations (loading, heavy computation)
3. **Backend threads** – Platform-specific (audio, rendering)

## **12.2 Thread Safety Boundaries**

| Component | Thread Safety |
|-----------|---------------|
| Game Loop | Main thread only |
| ECS (World, Entities, Components) | Main thread only |
| Scene Manager | Main thread only |
| Resource Loading | Can be async (worker thread) |
| Event Bus | Thread-safe |
| Logger | Thread-safe |
| Message Queue | Thread-safe |

## **12.3 Async Resource Loading**

```csharp
public async Task<TextureHandle> LoadTextureAsync(string path)
{
    // Load on worker thread
    var data = await Task.Run(() => LoadFileData(path));
    
    // Create texture on main thread (backend call)
    return await MainThread.InvokeAsync(() => 
        CreateTextureFromData(data)
    );
}
```

## **12.4 Message Queue Pattern**

```csharp
// Worker thread
Task.Run(() =>
{
    var level = LoadLevel("level1.json");
    MessageQueue.Post(new LevelLoadedMessage(level));
});

// Main thread (in Update)
void ProcessMessages()
{
    while (MessageQueue.TryDequeue(out var message))
    {
        switch (message)
        {
            case LevelLoadedMessage msg:
                ActivateLevel(msg.Level);
                break;
        }
    }
}
```

---

# 13. Memory Management

## **13.1 Memory Strategy**

MicroEngine primarily uses **managed memory** (C# GC):

**Benefits:**
- ? Automatic cleanup
- ? Memory safety
- ? Simplicity

**Trade-offs:**
- ? GC pauses (mitigated by object pooling)
- ? Less control (acceptable for target performance)

## **13.2 Object Pooling**

For frequently allocated objects:

```csharp
public class ObjectPool<T> where T : new()
{
    private Stack<T> _pool = new();
    
    public T Rent()
    {
        return _pool.Count > 0 ? _pool.Pop() : new T();
    }
    
    public void Return(T obj)
    {
        _pool.Push(obj);
    }
}

// Usage
var projectile = _projectilePool.Rent();
// Use projectile
_projectilePool.Return(projectile);
```

## **13.3 Resource Disposal**

```csharp
public class ResourceManager : IDisposable
{
    private Dictionary<TextureHandle, TextureData> _textures = new();
    
    public void Dispose()
    {
        foreach (var texture in _textures.Values)
        {
            texture.Dispose(); // Backend cleanup
        }
        _textures.Clear();
    }
}
```

## **13.4 Memory Budgets (Future)**

```csharp
public class MemoryBudget
{
    public long TextureMemoryLimit { get; set; } = 256 * 1024 * 1024; // 256 MB
    public long AudioMemoryLimit { get; set; } = 64 * 1024 * 1024;    // 64 MB
    
    public void ValidateAllocation(long size)
    {
        if (CurrentUsage + size > Limit)
            throw new OutOfMemoryException("Exceeded memory budget");
    }
}
```

---

# 14. Summary

MicroEngine's architecture is designed with the following characteristics:

## **14.1 Core Strengths**

? **Modular** – Each subsystem is independent and replaceable  
? **Deterministic** – Fixed timestep ensures consistent behavior  
? **Backend-Agnostic** – Core never depends on specific libraries  
? **Dimension-Agnostic** – Ready for 3D without redesign  
? **Educational** – Clear, readable, well-documented code  
? **Extensible** – Well-defined extension points  
? **Testable** – Isolated components, dependency injection  

## **14.2 Design Philosophy**

1. **Simplicity over Complexity** – Prefer clear code over clever code
2. **Explicit over Implicit** – Make behavior obvious
3. **Safety over Performance** – Correct first, fast second
4. **Future-Proof** – Design for extensibility

## **14.3 Architectural Guarantees**

The architecture ensures:

- ? Engine can be tested without backends
- ? Backends can be swapped without changing engine code
- ? Games can be built without touching engine internals
- ? Adding 3D support requires no architectural changes
- ? All subsystems can be understood independently

## **14.4 When to Use MicroEngine**

**Good fit for:**
- 2D games (today)
- Learning game engine architecture
- Prototyping and experimentation
- Projects requiring backend flexibility
- Future 3D games (when support is added)

**Not ideal for:**
- AAA 3D games (not the target)
- Projects requiring maximum performance
- Existing games built on other engines

---

## Related Documents

- ?? [Core Requirements](CORE_REQUIREMENTS.md) – Mandatory technical rules
- ?? [Engine Design Document](ENGINE_DESIGN_DOCUMENT.md) – Vision and goals
- ?? [Roadmap](ROADMAP.md) – Development timeline

---

**End of Document**

# Resources Module

**Module:** Engine.Core.Resources  
**Status:** Active  
**Version:** 1.0  
**Last Updated:** November 2025

---

## Overview

The Resources module provides a centralized system for loading, caching, and managing game assets in MicroEngine.

It handles:

- **Safe resource loading** with validation
- **Automatic caching** to avoid redundant loads
- **Lifecycle management** and cleanup
- **Asynchronous loading** for large assets
- **Resource scoping** for scene-specific assets
- **Type-safe access** through generic APIs

The module ensures that resources are:

- Loaded once and shared across consumers
- Properly validated before use
- Automatically cleaned up when no longer needed
- Thread-safe for async operations

---

## Table of Contents

1. [Core Concepts](#core-concepts)
2. [Architecture](#architecture)
3. [Resource Types](#resource-types)
4. [Loading Resources](#loading-resources)
5. [Resource Caching](#resource-caching)
6. [Resource Lifecycle](#resource-lifecycle)
7. [Asynchronous Loading](#asynchronous-loading)
8. [Resource Scoping](#resource-scoping)
9. [Error Handling](#error-handling)
10. [Usage Examples](#usage-examples)
11. [Best Practices](#best-practices)
12. [API Reference](#api-reference)

---

## Core Concepts

### What is a Resource?

A resource is any external asset used by the game:

- **Textures:** Images, sprites, spritesheets
- **Sounds:** Sound effects, music
- **Fonts:** TrueType, bitmap fonts
- **Data:** JSON, XML, binary configuration files
- **Shaders:** Vertex and fragment shaders
- **Models:** 3D meshes (future support)

### Resource Identity

Resources are identified by:

- **Path:** Relative path from the content root (`assets/textures/player.png`)
- **Type:** Resource type (`Texture`, `Sound`, etc.)
- **Handle:** Unique identifier returned on load

### Resource States

```
[Not Loaded] → Loading → [Loaded] → Unloading → [Disposed]
                  ↓                      ↑
                Error → [Failed]
```

---

## Architecture

### Class Diagram

```
ResourceManager
├── Resource Cache
│   ├── Loaded Resources
│   └── Reference Counts
├── Resource Loaders
│   ├── TextureLoader
│   ├── SoundLoader
│   ├── FontLoader
│   └── DataLoader
└── Resource Scopes
    ├── Global Scope
    └── Scene Scopes
```

### Core Classes

#### ResourceManager

Central manager for all resource operations.

```csharp
public class ResourceCache<T> where T : class
{
    public T Load(string path);
    public bool TryLoad(string path, out T resource);
    public void Unload(string path);
    public bool IsLoaded(string path);
    public void Clear();
}
```

#### IResource

Base interface for all resources.

```csharp
public interface IResource : IDisposable
{
    string Path { get; }
    bool IsLoaded { get; }
    void Load();
    Task LoadAsync();
}
```

#### ResourceHandle

Type-safe handle to a loaded resource.

```csharp
public struct ResourceHandle<T> where T : IResource
{
    public string Path { get; }
    public T Resource { get; }
    public bool IsValid { get; }
}
```

---

## Resource Types

### Texture

```csharp
public class Texture : IResource
{
    public int Width { get; }
    public int Height { get; }
    public TextureFormat Format { get; }

    public void Dispose();
}
```

### Sound

```csharp
public class Sound : IResource
{
    public float Duration { get; }
    public int SampleRate { get; }
    public int Channels { get; }

    public void Dispose();
}
```

### Font

```csharp
public class Font : IResource
{
    public int Size { get; }
    public FontStyle Style { get; }

    public Vector2 MeasureString(string text);
    public void Dispose();
}
```

### DataFile

```csharp
public class DataFile : IResource
{
    public string Content { get; }

    public T Deserialize<T>();
    public void Dispose();
}
```

---

## Loading Resources

### Synchronous Loading

Load resources via SceneContext caches:

```csharp
public override void OnLoad(SceneContext context)
{
    base.OnLoad(context);
    
    var texture = context.TextureCache.Load("textures/player.png");
    var sound = context.AudioCache.Load("sounds/jump.wav");
}
```

### Type Safety

The generic API ensures type safety:

```csharp
// Correct
Texture texture = ResourceManager.Load<Texture>("player.png");

// Compile error: type mismatch
Sound texture = ResourceManager.Load<Texture>("player.png");
```

### Validation

Resources are validated before being returned:

```csharp
public T Load<T>(string path) where T : IResource
{
    if (string.IsNullOrEmpty(path))
        throw new ArgumentException("Path cannot be null or empty");

    if (!File.Exists(ResolvePath(path)))
        throw new ResourceNotFoundException(path);

    // Load and validate
    var resource = LoadInternal<T>(path);

    if (!resource.IsValid())
        throw new ResourceLoadException(path, "Resource validation failed");

    return resource;
}
```

### Error Handling

```csharp
try
{
    var texture = ResourceManager.Load<Texture>("missing.png");
}
catch (ResourceNotFoundException ex)
{
    Logger.Error($"Resource not found: {ex.Path}");
    // Use fallback resource
    texture = ResourceManager.Load<Texture>("textures/fallback.png");
}
```

---

## Resource Caching

### Automatic Caching

Resources are automatically cached to avoid redundant loads:

```csharp
// First load: reads from disk
var texture1 = ResourceManager.Load<Texture>("player.png");

// Second load: returns cached instance
var texture2 = ResourceManager.Load<Texture>("player.png");

// texture1 == texture2 (same instance)
```

### Reference Counting

The manager tracks how many consumers hold each resource:

```csharp
// Load increments reference count
var texture = ResourceManager.Load<Texture>("player.png"); // RefCount = 1

// Same resource loaded again
var texture2 = ResourceManager.Load<Texture>("player.png"); // RefCount = 2

// Unload decrements reference count
ResourceManager.Unload(texture); // RefCount = 1
ResourceManager.Unload(texture2); // RefCount = 0 → resource disposed
```

### Cache Inspection

```csharp
// Check if resource is loaded
bool isLoaded = ResourceManager.IsLoaded<Texture>("player.png");

// Get cache statistics
var stats = ResourceManager.GetCacheStats();
Console.WriteLine($"Loaded: {stats.LoadedCount}, Memory: {stats.MemoryUsage}");

// Clear cache (unloads all unreferenced resources)
ResourceManager.ClearCache();
```

---

## Resource Lifecycle

### Load → Use → Unload Pattern

```csharp
public class GameplayScene : Scene
{
    private Texture _backgroundTexture;

    public override void OnEnter()
    {
        // Load resource
        _backgroundTexture = ResourceManager.Load<Texture>("background.png");
    }

    public override void Update(float deltaTime)
    {
        // Use resource
        Renderer.DrawTexture(_backgroundTexture, Vector2.Zero);
    }

    public override void OnExit()
    {
        // Unload resource
        ResourceManager.Unload(_backgroundTexture);
    }
}
```

### Automatic Cleanup with Using Pattern

```csharp
using (var texture = ResourceManager.LoadScoped<Texture>("temp.png"))
{
    // Use texture
    Renderer.DrawTexture(texture.Resource, Vector2.Zero);
} // Automatically unloaded
```

### Disposal

Resources implement `IDisposable` for deterministic cleanup:

```csharp
public class Texture : IResource, IDisposable
{
    private IntPtr _nativeHandle;

    public void Dispose()
    {
        if (_nativeHandle != IntPtr.Zero)
        {
            NativeMethods.UnloadTexture(_nativeHandle);
            _nativeHandle = IntPtr.Zero;
        }
    }
}
```

---

## Asynchronous Loading

### Async Loading API

Load resources without blocking the main thread:

```csharp
public async Task LoadAssetsAsync()
{
    var textureTask = ResourceManager.LoadAsync<Texture>("large_texture.png");
    var soundTask = ResourceManager.LoadAsync<Sound>("music.ogg");

    await Task.WhenAll(textureTask, soundTask);

    _texture = textureTask.Result;
    _sound = soundTask.Result;
}
```

### Progress Tracking

```csharp
public async Task LoadWithProgressAsync()
{
    var progress = new Progress<float>(p =>
    {
        Console.WriteLine($"Loading: {p * 100:F0}%");
    });

    var texture = await ResourceManager.LoadAsync<Texture>(
        "large_texture.png",
        progress
    );
}
```

### Batch Loading

Load multiple resources asynchronously:

```csharp
public async Task LoadLevelAssetsAsync(string[] assetPaths)
{
    var tasks = assetPaths.Select(path =>
        ResourceManager.LoadAsync<Texture>(path)
    );

    var textures = await Task.WhenAll(tasks);

    foreach (var texture in textures)
    {
        // Use loaded textures
    }
}
```

### Loading Scenes

```csharp
public class LoadingScene : Scene
{
    private float _progress;

    public override async void OnEnter()
    {
        var progress = new Progress<float>(p => _progress = p);

        await ResourceManager.LoadBatchAsync(
            new[]
            {
                "textures/player.png",
                "textures/enemy.png",
                "sounds/music.ogg"
            },
            progress
        );

        SceneManager.ChangeScene(new GameplayScene());
    }

    public override void Render(IRenderBackend renderer)
    {
        renderer.DrawText($"Loading... {_progress * 100:F0}%", new Vector2(400, 300));
    }
}
```

---

## Resource Scoping

### Global Resources

Resources that persist across scenes:

```csharp
public class GameBootstrap
{
    public void Initialize()
    {
        // Load global resources (never unloaded automatically)
        ResourceManager.LoadGlobal<Font>("fonts/default.ttf");
        ResourceManager.LoadGlobal<Texture>("ui/button.png");
    }
}
```

### Scene Scopes

Resources tied to specific scenes:

```csharp
public class GameplayScene : Scene
{
    public override void OnEnter()
    {
        // Begin scope for this scene
        ResourceManager.BeginScope("Gameplay");

        // All resources loaded in this scope
        var background = ResourceManager.Load<Texture>("backgrounds/level1.png");
        var music = ResourceManager.Load<Sound>("music/level1.ogg");

        ResourceManager.EndScope();
    }

    public override void OnExit()
    {
        // Unload all resources in "Gameplay" scope
        ResourceManager.UnloadScope("Gameplay");
    }
}
```

### Automatic Scope Management

```csharp
public override void OnEnter()
{
    using (ResourceManager.BeginScope("Gameplay"))
    {
        // Load resources
        var texture = ResourceManager.Load<Texture>("player.png");

        // Resources automatically tracked in scope
    }
}

public override void OnExit()
{
    ResourceManager.UnloadScope("Gameplay");
}
```

---

## Error Handling

### Error Types

#### ResourceNotFoundException

Thrown when a resource file doesn't exist:

```csharp
try
{
    var texture = ResourceManager.Load<Texture>("missing.png");
}
catch (ResourceNotFoundException ex)
{
    Logger.Error($"Resource not found: {ex.Path}");
}
```

#### ResourceLoadException

Thrown when resource loading fails:

```csharp
try
{
    var texture = ResourceManager.Load<Texture>("corrupted.png");
}
catch (ResourceLoadException ex)
{
    Logger.Error($"Failed to load resource: {ex.Path}, Reason: {ex.Message}");
}
```

#### InvalidResourceTypeException

Thrown when attempting to load a resource as the wrong type:

```csharp
try
{
    var texture = ResourceManager.Load<Texture>("file.wav"); // WAV is a sound
}
catch (InvalidResourceTypeException ex)
{
    Logger.Error($"Invalid resource type: expected {ex.ExpectedType}, got {ex.ActualType}");
}
```

### Fallback Resources

Provide fallback resources for missing assets:

```csharp
public T LoadWithFallback<T>(string path, string fallbackPath) where T : IResource
{
    try
    {
        return ResourceManager.Load<T>(path);
    }
    catch (ResourceNotFoundException)
    {
        Logger.Warning($"Resource not found: {path}, using fallback: {fallbackPath}");
        return ResourceManager.Load<T>(fallbackPath);
    }
}
```

### Safe Loading

```csharp
public bool TryLoad<T>(string path, out T resource) where T : IResource
{
    try
    {
        resource = ResourceManager.Load<T>(path);
        return true;
    }
    catch (Exception ex)
    {
        Logger.Error($"Failed to load resource: {path}", ex);
        resource = default;
        return false;
    }
}
```

---

## Usage Examples

### Example 1: Loading Textures

```csharp
public class SpriteComponent : IComponent
{
    private Texture _texture;

    public void Initialize(string texturePath)
    {
        _texture = ResourceManager.Load<Texture>(texturePath);
    }

    public void Render(IRenderBackend renderer, Vector2 position)
    {
        renderer.DrawTexture(_texture, position);
    }

    public void Cleanup()
    {
        ResourceManager.Unload(_texture);
    }
}
```

### Example 2: Loading Sounds

```csharp
public class AudioSystem : ISystem
{
    private Sound _jumpSound;

    public void Initialize(World world)
    {
        _jumpSound = ResourceManager.Load<Sound>("sounds/jump.wav");
    }

    public void Update(World world, float deltaTime)
    {
        var entities = world.Query<PlayerInputComponent>();

        foreach (var entity in entities)
        {
            if (InputManager.IsKeyPressed(Key.Space))
            {
                AudioBackend.Play(_jumpSound);
            }
        }
    }

    public void Shutdown(World world)
    {
        ResourceManager.Unload(_jumpSound);
    }
}
```

### Example 3: Loading Data Files

```csharp
public class LevelLoader
{
    public Level LoadLevel(string levelName)
    {
        var dataFile = ResourceManager.Load<DataFile>($"levels/{levelName}.json");
        var levelData = dataFile.Deserialize<LevelData>();

        return new Level
        {
            Name = levelData.Name,
            Width = levelData.Width,
            Height = levelData.Height,
            Tiles = levelData.Tiles
        };
    }
}
```

### Example 4: Async Loading with Progress

```csharp
public class AssetLoadingScene : Scene
{
    private float _progress;
    private List<string> _assetPaths;

    public AssetLoadingScene(List<string> assetPaths)
    {
        _assetPaths = assetPaths;
    }

    public override async void OnEnter()
    {
        var progress = new Progress<float>(p => _progress = p);

        var textures = await ResourceManager.LoadBatchAsync<Texture>(
            _assetPaths,
            progress
        );

        SceneManager.ChangeScene(new GameplayScene());
    }

    public override void Render(IRenderBackend renderer)
    {
        renderer.Clear(Color.Black);

        var barWidth = 400;
        var barHeight = 40;
        var barX = (ScreenWidth - barWidth) / 2;
        var barY = (ScreenHeight - barHeight) / 2;

        // Background
        renderer.DrawRectangle(new Rect(barX, barY, barWidth, barHeight), Color.DarkGray);

        // Progress
        var fillWidth = barWidth * _progress;
        renderer.DrawRectangle(new Rect(barX, barY, fillWidth, barHeight), Color.Green);

        // Text
        var text = $"Loading... {_progress * 100:F0}%";
        renderer.DrawText(text, new Vector2(barX, barY - 50), Color.White);
    }
}
```

---

## Best Practices

### Do's

- ✓ Load resources in scene `OnEnter`, unload in `OnExit`
- ✓ Use resource scopes for scene-specific assets
- ✓ Cache frequently-used resources globally
- ✓ Use async loading for large assets
- ✓ Validate resources before use
- ✓ Provide fallback resources for missing assets
- ✓ Track resource memory usage
- ✓ Use type-safe generic APIs

### Don'ts

- ✗ Don't load resources synchronously on the main thread for large files
- ✗ Don't forget to unload scene-specific resources
- ✗ Don't load the same resource multiple times unnecessarily
- ✗ Don't hold references to unloaded resources
- ✗ Don't hardcode resource paths (use constants or config)
- ✗ Don't ignore resource loading errors
- ✗ Don't mix global and scene-scoped resources without clear separation

### Resource Organization

```
assets/
├── textures/
│   ├── ui/
│   ├── characters/
│   └── environments/
├── sounds/
│   ├── sfx/
│   └── music/
├── fonts/
├── data/
│   ├── levels/
│   └── config/
└── shaders/
```

### Path Constants

```csharp
public static class AssetPaths
{
    public const string PlayerTexture = "textures/characters/player.png";
    public const string JumpSound = "sounds/sfx/jump.wav";
    public const string DefaultFont = "fonts/default.ttf";
}

// Usage
var texture = ResourceManager.Load<Texture>(AssetPaths.PlayerTexture);
```

---

## API Reference

### ResourceManager

```csharp
public class ResourceManager
{
    // Synchronous loading
    public static T Load<T>(string path) where T : IResource;
    public static void Unload<T>(string path) where T : IResource;
    public static void Unload<T>(T resource) where T : IResource;

    // Asynchronous loading
    public static Task<T> LoadAsync<T>(string path, IProgress<float> progress = null)
        where T : IResource;
    public static Task<T[]> LoadBatchAsync<T>(string[] paths, IProgress<float> progress = null)
        where T : IResource;

    // Cache management
    public static bool IsLoaded<T>(string path) where T : IResource;
    public static void ClearCache();
    public static CacheStats GetCacheStats();

    // Scoping
    public static IDisposable BeginScope(string scopeName);
    public static void EndScope(string scopeName = null);
    public static void UnloadScope(string scopeName);

    // Global resources
    public static T LoadGlobal<T>(string path) where T : IResource;
}
```

### IResource

```csharp
public interface IResource : IDisposable
{
    string Path { get; }
    bool IsLoaded { get; }
    ResourceState State { get; }

    void Load();
    Task LoadAsync();
    void Unload();
}
```

### ResourceHandle<T>

```csharp
public struct ResourceHandle<T> : IDisposable where T : IResource
{
    public string Path { get; }
    public T Resource { get; }
    public bool IsValid { get; }

    public void Dispose();
}
```

---

## Related Documentation

- [Architecture](../ARCHITECTURE.md)
- [Scenes Module](SCENES.md)
- [ECS Module](ECS.md)
- [Graphics Backend](GRAPHICS_BACKEND.md)

---

**Last Updated:** November 2025  
**Version:** 1.0

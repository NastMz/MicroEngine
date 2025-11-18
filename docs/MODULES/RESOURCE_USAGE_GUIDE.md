# Resource Management Usage Guide

This guide covers best practices, patterns, and common scenarios for using the MicroEngine resource system.

---

## Table of Contents

1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [Core Concepts](#core-concepts)
4. [Loading Resources](#loading-resources)
5. [Resource Lifecycle](#resource-lifecycle)
6. [Hot-Reloading](#hot-reloading)
7. [Validation and Error Handling](#validation-and-error-handling)
8. [Performance Best Practices](#performance-best-practices)
9. [Common Patterns](#common-patterns)
10. [Troubleshooting](#troubleshooting)

---

## Overview

The MicroEngine resource system provides:

-   **Automatic reference counting** - Resources are loaded once and shared
-   **Explicit lifecycle management** - Manual Load/Unload with ref counting
-   **Type-safe loaders** - Each resource type has a dedicated loader
-   **Metadata and validation** - Rich metadata and validation before loading
-   **Hot-reloading** - Automatic reload when files change on disk
-   **Memory tracking** - Built-in memory usage monitoring

---

## Quick Start

### Basic Resource Loading

```csharp
using MicroEngine.Core.Resources;
using MicroEngine.Backend.Raylib.Resources;

// Create logger
var logger = new ConsoleLogger(LogLevel.Info);

// Create cache for textures
var textureLoader = new RaylibTextureLoader();
var textureCache = new ResourceCache<RaylibTexture>(textureLoader, logger);

// Load a texture
var texture = textureCache.Load("assets/sprites/player.png");

// Use the texture
renderBackend.DrawTexture(texture, position);

// Unload when done
textureCache.Unload("assets/sprites/player.png");

// Clean up
textureCache.Dispose();
```

### With Hot-Reloading

```csharp
// Create hot-reload cache
var hotReloadCache = new HotReloadableResourceCache<RaylibTexture>(
    textureLoader,
    logger
);

// Subscribe to reload events
hotReloadCache.ResourceReloaded += (sender, e) =>
{
    Console.WriteLine($"Reloaded: {e.Path}");
    // Update references to the new resource
};

// Load with hot-reload enabled
var texture = hotReloadCache.Load("assets/sprites/player.png", enableHotReload: true);

// Texture will automatically reload when file changes
```

---

## Core Concepts

### Resource Identification

Resources are identified by their **normalized file path**:

```csharp
// These all resolve to the same resource
cache.Load("assets/texture.png");
cache.Load("assets\\texture.png");
cache.Load("./assets/texture.png");
```

Paths are normalized using `Path.GetFullPath()`.

### Reference Counting

Each `Load()` call increments the reference count:

```csharp
var tex1 = cache.Load("texture.png"); // refs = 1
var tex2 = cache.Load("texture.png"); // refs = 2 (same instance)

cache.Unload("texture.png"); // refs = 1
cache.Unload("texture.png"); // refs = 0, resource disposed
```

### Resource Metadata

All resources carry metadata about their source:

```csharp
var texture = cache.Load("texture.png");

var metadata = texture.Metadata;
Console.WriteLine($"Extension: {metadata.Extension}");
Console.WriteLine($"Size: {metadata.FileSizeBytes} bytes");
Console.WriteLine($"Loaded at: {metadata.LoadedAt}");

// Format-specific metadata
if (metadata.CustomMetadata.TryGetValue("Width", out var width))
{
    Console.WriteLine($"Width: {width}");
}
```

---

## Loading Resources

### Standard Loading

```csharp
// Load with validation (default)
var texture = cache.Load("texture.png");

// Skip validation for trusted sources
var texture = cache.Load("texture.png", validateFirst: false);
```

### Validation Process

Validation checks:

1. **Path validity** - Not null, empty, or whitespace
2. **File existence** - File must exist on disk
3. **Extension support** - Must match loader's supported extensions
4. **File size** - Must not exceed max size (default 100MB)
5. **File accessibility** - Must be readable and not locked

```csharp
var validator = new ResourceValidator(maxFileSizeBytes: 50 * 1024 * 1024); // 50MB

var result = validator.Validate("texture.png", new[] { ".png", ".jpg" });

if (!result.IsValid)
{
    Console.WriteLine($"Validation failed: {result.ErrorMessage}");
    Console.WriteLine($"Error code: {result.ErrorCode}");
}
```

### Error Handling

```csharp
try
{
    var texture = cache.Load("missing.png");
}
catch (InvalidOperationException ex)
{
    // Validation failed
    Console.WriteLine($"Validation error: {ex.Message}");
}
catch (FileNotFoundException ex)
{
    // File not found (if validation was skipped)
    Console.WriteLine($"File not found: {ex.Message}");
}
catch (InvalidDataException ex)
{
    // Loader failed to parse file
    Console.WriteLine($"Load error: {ex.Message}");
}
```

---

## Resource Lifecycle

### Load Phase

1. Path normalization
2. Check cache for existing resource
3. If cached: increment ref count, return existing
4. If new: validate (optional) → load → cache → return

### Usage Phase

Resources remain in memory as long as ref count > 0.

### Unload Phase

1. Decrement ref count
2. If ref count > 0: keep in cache
3. If ref count = 0: dispose resource, remove from cache

### Disposal

```csharp
// Unload specific resource
cache.Unload("texture.png");

// Clear all resources
cache.Clear();

// Dispose entire cache
cache.Dispose();
```

---

## Hot-Reloading

### Setup

```csharp
var hotReloadCache = new HotReloadableResourceCache<RaylibTexture>(
    textureLoader,
    logger
);

// Subscribe to events
hotReloadCache.ResourceReloaded += OnResourceReloaded;
hotReloadCache.ResourceReloadFailed += OnReloadFailed;

void OnResourceReloaded(object? sender, ResourceReloadedEventArgs<RaylibTexture> e)
{
    Console.WriteLine($"✓ Reloaded: {e.Path}");
    // Update any cached references to e.Resource
}

void OnReloadFailed(object? sender, ResourceReloadFailedEventArgs e)
{
    Console.WriteLine($"✗ Reload failed: {e.Path} - {e.Error.Message}");
}
```

### Per-Resource Control

```csharp
// Enable hot-reload for this specific resource
var texture = hotReloadCache.Load("texture.png", enableHotReload: true);

// Disable hot-reload for static assets
var staticTexture = hotReloadCache.Load("logo.png", enableHotReload: false);
```

### Global Control

```csharp
// Disable all hot-reloading temporarily
hotReloadCache.HotReloadEnabled = false;

// Re-enable
hotReloadCache.HotReloadEnabled = true;
```

### Reference Counting with Hot-Reload

Hot-reloading maintains reference counts:

```csharp
var tex1 = hotReloadCache.Load("texture.png", enableHotReload: true); // refs = 1
var tex2 = hotReloadCache.Load("texture.png", enableHotReload: true); // refs = 2

// File changes on disk → ResourceReloaded event fires
// Old resource disposed, new resource has refs = 2

hotReloadCache.Unload("texture.png"); // refs = 1, still watching
hotReloadCache.Unload("texture.png"); // refs = 0, stops watching
```

---

## Validation and Error Handling

### Validation Errors

```csharp
public enum ResourceValidationError
{
    FileNotFound,         // File does not exist
    UnsupportedExtension, // Extension not supported by loader
    FileTooLarge,         // Exceeds max file size
    InvalidFileData,      // File is empty or corrupt
    InvalidPath,          // Path is invalid
    AccessDenied,         // No permission to read file
    FileLocked,           // File is locked by another process
    InvalidFormat         // File format is invalid for loader
}
```

### Custom Validation

```csharp
public class MyResourceLoader : IResourceLoader<MyResource>
{
    public IReadOnlyList<string> SupportedExtensions => new[] { ".custom" };

    public ResourceValidationResult Validate(string path)
    {
        if (!File.Exists(path))
        {
            return ResourceValidationResult.Failure(
                ResourceValidationError.FileNotFound,
                $"File not found: {path}"
            );
        }

        // Custom validation logic
        var content = File.ReadAllText(path);
        if (!content.StartsWith("MAGIC"))
        {
            return ResourceValidationResult.Failure(
                ResourceValidationError.InvalidFormat,
                "Missing magic header"
            );
        }

        return ResourceValidationResult.Success();
    }

    public MyResource Load(string path, ResourceMetadata? metadata = null)
    {
        // Load implementation
    }
}
```

---

## Performance Best Practices

### 1. Preload Resources at Startup

```csharp
// Bad: Load during gameplay
void Update()
{
    var explosion = cache.Load("effects/explosion.png"); // Causes stutter
    DrawExplosion(explosion);
}

// Good: Preload during loading screen
void LoadLevel()
{
    explosionTexture = cache.Load("effects/explosion.png");
    playerTexture = cache.Load("sprites/player.png");
    enemyTexture = cache.Load("sprites/enemy.png");
}
```

### 2. Batch Loads

```csharp
// Load all level assets together
var levelAssets = new[]
{
    "sprites/player.png",
    "sprites/enemy.png",
    "sprites/item.png",
    "audio/music.ogg",
    "audio/jump.wav"
};

foreach (var asset in levelAssets)
{
    // Determine type and load accordingly
}
```

### 3. Use Reference Counting Wisely

```csharp
// Bad: Load/unload every frame
void Update()
{
    var tex = cache.Load("texture.png");
    Draw(tex);
    cache.Unload("texture.png"); // Unnecessary work
}

// Good: Load once, keep reference
private RaylibTexture playerTexture;

void Initialize()
{
    playerTexture = cache.Load("texture.png");
}

void Update()
{
    Draw(playerTexture);
}

void Cleanup()
{
    cache.Unload("texture.png");
}
```

### 4. Monitor Memory Usage

```csharp
Console.WriteLine($"Cached resources: {cache.CachedCount}");
Console.WriteLine($"Memory usage: {cache.TotalMemoryUsage / 1024 / 1024} MB");

// Clear unused resources periodically
if (cache.TotalMemoryUsage > maxMemoryUsage)
{
    cache.Clear();
}
```

### 5. Disable Hot-Reload in Production

```csharp
#if DEBUG
var cache = new HotReloadableResourceCache<RaylibTexture>(loader, logger);
#else
var cache = new ResourceCache<RaylibTexture>(loader, logger);
#endif
```

---

## Common Patterns

### Scene-Based Resource Management

```csharp
public abstract class Scene
{
    protected ResourceCache<RaylibTexture> TextureCache { get; }
    protected ResourceCache<RaylibFont> FontCache { get; }
    private List<string> loadedResources = new();

    protected T LoadResource<T>(ResourceCache<T> cache, string path) where T : IResource
    {
        loadedResources.Add(path);
        return cache.Load(path);
    }

    public virtual void OnExit()
    {
        // Unload all resources loaded by this scene
        foreach (var path in loadedResources)
        {
            TextureCache.Unload(path);
        }
        loadedResources.Clear();
    }
}
```

### Resource Pool Pattern

```csharp
public class EffectPool
{
    private readonly ResourceCache<RaylibTexture> cache;
    private readonly Dictionary<string, int> refCounts = new();

    public void Acquire(string effectName)
    {
        if (!refCounts.ContainsKey(effectName))
        {
            cache.Load($"effects/{effectName}.png");
            refCounts[effectName] = 0;
        }
        refCounts[effectName]++;
    }

    public void Release(string effectName)
    {
        if (refCounts.TryGetValue(effectName, out var count))
        {
            refCounts[effectName]--;
            if (count <= 1)
            {
                cache.Unload($"effects/{effectName}.png");
                refCounts.Remove(effectName);
            }
        }
    }
}
```

### Lazy Loading

```csharp
public class AssetManager
{
    private readonly Dictionary<string, Lazy<RaylibTexture>> lazyTextures = new();

    public void RegisterTexture(string name, string path)
    {
        lazyTextures[name] = new Lazy<RaylibTexture>(() => cache.Load(path));
    }

    public RaylibTexture GetTexture(string name)
    {
        return lazyTextures[name].Value; // Loads on first access
    }
}
```

---

## Troubleshooting

### Resource Not Unloading

**Problem:** Resource stays in memory after unload.

**Solution:** Check reference count matches load count:

```csharp
// Load called 3 times
cache.Load("texture.png");
cache.Load("texture.png");
cache.Load("texture.png");

// Must unload 3 times
cache.Unload("texture.png");
cache.Unload("texture.png");
cache.Unload("texture.png"); // Now disposed
```

### Hot-Reload Not Working

**Problem:** File changes don't trigger reload.

**Checklist:**

1. Is `HotReloadEnabled` true?
2. Was resource loaded with `enableHotReload: true`?
3. Is event handler subscribed before loading?
4. Does file system support change notifications?

```csharp
// Correct order
cache.ResourceReloaded += OnReloaded;
cache.Load("texture.png", enableHotReload: true);
```

### Validation Failures

**Problem:** Valid files fail validation.

**Solutions:**

```csharp
// Check supported extensions
var loader = new RaylibTextureLoader();
Console.WriteLine(string.Join(", ", loader.SupportedExtensions)); // .png, .jpg, .bmp, ...

// Increase max file size
var validator = new ResourceValidator(maxFileSizeBytes: 200 * 1024 * 1024); // 200MB

// Skip validation if trusted
cache.Load("texture.png", validateFirst: false);
```

### Memory Leaks

**Problem:** Memory usage keeps growing.

**Diagnosis:**

```csharp
// Check cache state
Console.WriteLine($"Cached: {cache.CachedCount}");
Console.WriteLine($"Memory: {cache.TotalMemoryUsage}");

// Find unbalanced load/unload
// Enable debug logging
var logger = new ConsoleLogger(LogLevel.Debug);
```

**Solution:** Ensure every Load has matching Unload, or use `Clear()` between scenes.

---

## Summary

**Key Takeaways:**

-   ✅ **Always match Load with Unload** for correct ref counting
-   ✅ **Preload resources** during loading screens, not gameplay
-   ✅ **Use validation** to catch errors early
-   ✅ **Enable hot-reload** only in development builds
-   ✅ **Monitor memory usage** with `CachedCount` and `TotalMemoryUsage`
-   ✅ **Handle reload events** to update cached references
-   ✅ **Clear caches** between scene transitions
-   ✅ **Dispose caches** when shutting down

For implementation details, see [RESOURCES.md](RESOURCES.md).

# Graphics Backend Module

**Module:** Engine.Backend.Graphics  
**Status:** Active  
**Version:** 1.0  
**Last Updated:** November 2025

---

## Overview

The Graphics Backend module defines the rendering abstraction layer for MicroEngine.

It provides:

- **Backend-agnostic rendering API** through `IRenderer2D`
- **2D rendering primitives** with future 3D extensibility
- **Support for multiple rendering implementations** (Raylib, SDL, OpenGL, etc.)
- **Consistent rendering interface** across platforms
- **Resource management** for textures, shaders, and render targets

The graphics backend is designed to be:

- **Dimension-agnostic:** Core APIs work for both 2D and 3D
- **Implementation-independent:** Engine core never depends on specific backends
- **Performant:** Minimal overhead, batch-friendly design
- **Extensible:** Easy to add new rendering features

---

## Table of Contents

1. [Core Concepts](#core-concepts)
2. [Architecture](#architecture)
3. [IRenderBackend Interface](#irenderbackend-interface)
4. [2D Rendering](#2d-rendering)
5. [3D Rendering (Future)](#3d-rendering-future)
6. [Textures and Materials](#textures-and-materials)
7. [Render Targets](#render-targets)
8. [Shaders](#shaders)
9. [Camera System](#camera-system)
10. [Backend Implementations](#backend-implementations)
11. [Usage Examples](#usage-examples)
12. [Best Practices](#best-practices)
13. [API Reference](#api-reference)

---

## Core Concepts

### What is a Render Backend?

A render backend is a concrete implementation of the `IRenderBackend` interface that translates engine rendering
commands into platform-specific graphics API calls.

**Supported backends:**

- **Raylib Backend:** Default, simple, cross-platform
- **SDL Backend:** Hardware-accelerated 2D rendering
- **OpenGL Backend:** Custom OpenGL renderer (future)
- **Vulkan Backend:** High-performance modern API (future)

### Backend Independence

The engine core never depends on specific backends:

```csharp
// Engine core
public class RenderSystem : ISystem
{
    private readonly IRenderBackend _renderer;

    public RenderSystem(IRenderBackend renderer)
    {
        _renderer = renderer; // Interface only
    }
}

// Game project
var renderer = new RaylibRenderBackend();
var renderSystem = new RenderSystem(renderer);
```

### Dimension-Agnostic Design

All rendering APIs are designed to support both 2D and 3D:

```csharp
// 2D usage
renderer.DrawSprite(texture, new Vector3(100, 100, 0));

// Future 3D usage
renderer.DrawMesh(mesh, new Vector3(10, 5, -20));
```

---

## Architecture

### Class Diagram

```
IRenderBackend (interface)
├── Initialize()
├── Clear()
├── BeginFrame()
├── EndFrame()
├── Draw operations
└── Resource management

Implementations:
├── RaylibRenderBackend
├── SDLRenderBackend
└── OpenGLRenderBackend (future)
```

### Core Interfaces

#### IRenderBackend

Main rendering interface.

```csharp
public interface IRenderBackend : IDisposable
{
    void Initialize(RenderConfig config);
    void Shutdown();

    void BeginFrame();
    void EndFrame();
    void Clear(Color color);

    // 2D primitives
    void DrawRectangle(Rect rect, Color color);
    void DrawCircle(Vector2 center, float radius, Color color);
    void DrawLine(Vector2 start, Vector2 end, Color color, float thickness);
    void DrawTexture(Texture texture, Vector2 position, Color tint);

    // Text
    void DrawText(string text, Vector2 position, Font font, Color color);

    // Advanced
    void SetViewport(Rect viewport);
    void SetCamera(Camera camera);
}
```

#### IRenderable

Interface for objects that can be rendered.

```csharp
public interface IRenderable
{
    void Render(IRenderBackend renderer);
}
```

---

## IRenderBackend Interface

### Lifecycle Methods

#### Initialize

Set up the rendering backend.

```csharp
public void Initialize(RenderConfig config)
{
    var config = new RenderConfig
    {
        WindowTitle = "MicroEngine Game",
        Width = 1280,
        Height = 720,
        Fullscreen = false,
        VSync = true
    };

    renderer.Initialize(config);
}
```

#### Shutdown

Clean up rendering resources.

```csharp
public void Shutdown()
{
    renderer.Shutdown();
}
```

### Frame Management

#### BeginFrame / EndFrame

Wrap all rendering calls in a frame:

```csharp
public void Render()
{
    renderer.BeginFrame();

    renderer.Clear(Color.CornflowerBlue);

    // Draw operations
    renderer.DrawTexture(background, Vector2.Zero);
    renderer.DrawSprite(playerSprite, playerPosition);

    renderer.EndFrame();
}
```

### Clear

Clear the screen with a color:

```csharp
renderer.Clear(Color.Black);
renderer.Clear(new Color(0.1f, 0.2f, 0.3f, 1.0f));
```

---

## 2D Rendering

### Drawing Primitives

#### Rectangle

```csharp
renderer.DrawRectangle(new Rect(100, 100, 50, 50), Color.Red);
renderer.DrawRectangleOutline(new Rect(200, 100, 50, 50), Color.Blue, 2.0f);
```

#### Circle

```csharp
renderer.DrawCircle(new Vector2(400, 300), radius: 50, Color.Green);
renderer.DrawCircleOutline(new Vector2(500, 300), radius: 50, Color.Yellow, 2.0f);
```

#### Line

```csharp
renderer.DrawLine(new Vector2(0, 0), new Vector2(800, 600), Color.White, thickness: 2.0f);
```

#### Polygon

```csharp
var points = new Vector2[]
{
    new Vector2(100, 100),
    new Vector2(200, 150),
    new Vector2(150, 250)
};
renderer.DrawPolygon(points, Color.Purple);
```

### Drawing Textures

#### Simple Texture Draw

```csharp
var texture = ResourceManager.Load<Texture>("player.png");
renderer.DrawTexture(texture, new Vector2(100, 100));
```

#### Texture with Tint

```csharp
renderer.DrawTexture(texture, new Vector2(100, 100), Color.Red);
```

#### Texture with Transform

```csharp
renderer.DrawTexture(
    texture,
    position: new Vector2(100, 100),
    rotation: 45.0f,
    scale: new Vector2(2.0f, 2.0f),
    origin: new Vector2(texture.Width / 2, texture.Height / 2),
    tint: Color.White
);
```

#### Sprite from Spritesheet

```csharp
var spritesheet = ResourceManager.Load<Texture>("characters.png");
var sourceRect = new Rect(0, 0, 32, 32); // First sprite

renderer.DrawTextureRegion(
    spritesheet,
    sourceRect,
    position: new Vector2(100, 100)
);
```

### Drawing Text

```csharp
var font = ResourceManager.Load<Font>("default.ttf");
renderer.DrawText("Hello, World!", new Vector2(100, 100), font, Color.White);
```

#### Measured Text

```csharp
var text = "Score: 1000";
var size = font.MeasureString(text);
var position = new Vector2(screenWidth - size.X - 10, 10);
renderer.DrawText(text, position, font, Color.Yellow);
```

---

## 3D Rendering (Future)

The backend interface is designed to support 3D rendering in future versions:

### Planned 3D APIs

```csharp
// Mesh rendering
void DrawMesh(Mesh mesh, Matrix4x4 transform, Material material);
void DrawModel(Model model, Vector3 position, Vector3 rotation, Vector3 scale);

// 3D primitives
void DrawCube(Vector3 position, Vector3 size, Color color);
void DrawSphere(Vector3 center, float radius, Color color);

// Lighting
void SetAmbientLight(Color color);
void AddLight(Light light);

// 3D camera
void SetCamera3D(Camera3D camera);
```

### Dimension-Agnostic Current APIs

These APIs already support 3D through `Vector3`:

```csharp
// Works for both 2D and 3D
public void DrawTexture(Texture texture, Vector3 position, Color tint)
{
    // 2D: Z is ignored
    // 3D: Z is depth
}
```

---

## Textures and Materials

### Texture Loading

Textures are loaded through the Resource Manager:

```csharp
var texture = ResourceManager.Load<Texture>("textures/player.png");
```

### Texture Properties

```csharp
public class Texture : IResource
{
    public int Width { get; }
    public int Height { get; }
    public TextureFormat Format { get; }
    public TextureFilter Filter { get; set; }
    public TextureWrap WrapMode { get; set; }
}
```

### Texture Filtering

```csharp
texture.Filter = TextureFilter.Nearest; // Pixel-perfect
texture.Filter = TextureFilter.Linear;  // Smooth scaling
```

### Texture Wrapping

```csharp
texture.WrapMode = TextureWrap.Repeat;
texture.WrapMode = TextureWrap.Clamp;
texture.WrapMode = TextureWrap.Mirror;
```

### Materials (Future)

```csharp
public class Material
{
    public Texture DiffuseMap { get; set; }
    public Texture NormalMap { get; set; }
    public Texture SpecularMap { get; set; }
    public Shader Shader { get; set; }
    public Dictionary<string, object> Properties { get; set; }
}
```

---

## Render Targets

### Creating Render Targets

Render to texture for post-processing effects:

```csharp
var renderTarget = renderer.CreateRenderTarget(1280, 720);
```

### Rendering to Target

```csharp
renderer.BeginRenderTarget(renderTarget);

// Draw to render target
renderer.Clear(Color.Transparent);
renderer.DrawTexture(sceneTexture, Vector2.Zero);

renderer.EndRenderTarget();

// Use render target as texture
renderer.DrawTexture(renderTarget.Texture, Vector2.Zero);
```

### Post-Processing Example

```csharp
// Render scene to target
renderer.BeginRenderTarget(sceneTarget);
RenderScene();
renderer.EndRenderTarget();

// Apply blur effect
renderer.BeginRenderTarget(blurTarget);
renderer.SetShader(blurShader);
renderer.DrawTexture(sceneTarget.Texture, Vector2.Zero);
renderer.EndRenderTarget();

// Draw final result
renderer.DrawTexture(blurTarget.Texture, Vector2.Zero);
```

---

## Shaders

### Shader Support

Backends can support custom shaders:

```csharp
var shader = ResourceManager.Load<Shader>("shaders/outline.glsl");
renderer.SetShader(shader);
renderer.DrawTexture(texture, position);
renderer.SetShader(null); // Reset to default
```

### Shader Parameters

```csharp
shader.SetParameter("time", Time.TotalSeconds);
shader.SetParameter("color", new Vector4(1, 0, 0, 1));
shader.SetParameter("texture", texture);
```

### Built-in Shaders

```csharp
// Default shader
renderer.SetShader(Shader.Default);

// Grayscale
renderer.SetShader(Shader.Grayscale);

// Sepia
renderer.SetShader(Shader.Sepia);
```

---

## Camera System

### 2D Camera

```csharp
public class Camera2D
{
    public Vector2 Position { get; set; }
    public float Rotation { get; set; }
    public float Zoom { get; set; }
    public Vector2 Offset { get; set; }
}
```

### Setting Camera

```csharp
var camera = new Camera2D
{
    Position = new Vector2(playerX, playerY),
    Zoom = 2.0f,
    Offset = new Vector2(screenWidth / 2, screenHeight / 2)
};

renderer.SetCamera(camera);

// Draw with camera transform applied
renderer.DrawTexture(worldTexture, worldPosition);

// Reset camera
renderer.SetCamera(null);

// Draw UI without camera transform
renderer.DrawText("Score: 100", new Vector2(10, 10), font, Color.White);
```

### Camera Follow

```csharp
public void UpdateCamera(Vector2 targetPosition)
{
    var lerpSpeed = 0.1f;
    camera.Position = Vector2.Lerp(camera.Position, targetPosition, lerpSpeed);
}
```

### 3D Camera (Future)

```csharp
public class Camera3D
{
    public Vector3 Position { get; set; }
    public Vector3 Target { get; set; }
    public Vector3 Up { get; set; }
    public float FieldOfView { get; set; }
    public CameraProjection Projection { get; set; }
}
```

---

## Backend Implementations

### Raylib Backend

Default backend using Raylib:

```csharp
var renderer = new RaylibRenderBackend();
renderer.Initialize(config);
```

**Features:**

- Simple API
- Cross-platform
- Good for prototyping
- Built-in 2D and 3D support

### SDL Backend (Future)

Hardware-accelerated 2D rendering:

```csharp
var renderer = new SDLRenderBackend();
renderer.Initialize(config);
```

**Features:**

- Hardware acceleration
- Cross-platform
- Optimized for 2D

### Custom Backend

Implement `IRenderBackend` for custom renderers:

```csharp
public class CustomRenderBackend : IRenderBackend
{
    public void Initialize(RenderConfig config)
    {
        // Initialize your renderer
    }

    public void DrawTexture(Texture texture, Vector2 position, Color tint)
    {
        // Implement texture drawing
    }

    // Implement other methods...
}
```

---

## Usage Examples

### Example 1: Basic Rendering

```csharp
public class RenderSystem : ISystem
{
    private readonly IRenderBackend _renderer;

    public RenderSystem(IRenderBackend renderer)
    {
        _renderer = renderer;
    }

    public void Update(World world, float deltaTime)
    {
        _renderer.BeginFrame();
        _renderer.Clear(Color.CornflowerBlue);

        var entities = world.Query<TransformComponent, SpriteComponent>();

        foreach (var entity in entities)
        {
            var transform = entity.GetComponent<TransformComponent>();
            var sprite = entity.GetComponent<SpriteComponent>();

            _renderer.DrawTexture(
                sprite.Texture,
                transform.Position,
                transform.Rotation,
                transform.Scale,
                Color.White
            );
        }

        _renderer.EndFrame();
    }
}
```

### Example 2: Camera with Player Follow

```csharp
public class CameraSystem : ISystem
{
    private readonly IRenderBackend _renderer;
    private readonly Camera2D _camera;

    public CameraSystem(IRenderBackend renderer)
    {
        _renderer = renderer;
        _camera = new Camera2D
        {
            Zoom = 1.0f,
            Offset = new Vector2(640, 360)
        };
    }

    public void Update(World world, float deltaTime)
    {
        var player = world.Query<PlayerComponent, TransformComponent>().First();
        var transform = player.GetComponent<TransformComponent>();

        // Smooth follow
        _camera.Position = Vector2.Lerp(
            _camera.Position,
            new Vector2(transform.Position.X, transform.Position.Y),
            0.1f
        );

        _renderer.SetCamera(_camera);
    }
}
```

### Example 3: Particle System

```csharp
public class ParticleRenderSystem : ISystem
{
    private readonly IRenderBackend _renderer;

    public void Update(World world, float deltaTime)
    {
        var particles = world.Query<ParticleComponent, TransformComponent>();

        foreach (var particle in particles)
        {
            var transform = particle.GetComponent<TransformComponent>();
            var particleData = particle.GetComponent<ParticleComponent>();

            var alpha = particleData.LifeRemaining / particleData.InitialLife;
            var color = new Color(
                particleData.Color.R,
                particleData.Color.G,
                particleData.Color.B,
                alpha
            );

            _renderer.DrawCircle(
                new Vector2(transform.Position.X, transform.Position.Y),
                particleData.Size,
                color
            );
        }
    }
}
```

### Example 4: UI Rendering

```csharp
public class UIRenderSystem : ISystem
{
    private readonly IRenderBackend _renderer;
    private readonly Font _font;

    public void Update(World world, float deltaTime)
    {
        // Render UI without camera transform
        _renderer.SetCamera(null);

        // Health bar
        var healthEntity = world.Query<HealthComponent>().First();
        var health = healthEntity.GetComponent<HealthComponent>();

        var barWidth = 200;
        var barHeight = 20;
        var fillWidth = barWidth * (health.Current / health.Max);

        _renderer.DrawRectangle(new Rect(10, 10, barWidth, barHeight), Color.DarkGray);
        _renderer.DrawRectangle(new Rect(10, 10, fillWidth, barHeight), Color.Green);

        // Score text
        var scoreText = $"Score: {world.GetScore()}";
        _renderer.DrawText(scoreText, new Vector2(10, 40), _font, Color.White);
    }
}
```

---

## Best Practices

### Do's

- ✓ Use `IRenderBackend` interface, never concrete implementations in core
- ✓ Batch similar draw calls together for performance
- ✓ Clear the screen at the start of each frame
- ✓ Use render targets for complex effects
- ✓ Reset camera after rendering world objects (before UI)
- ✓ Dispose of render resources properly
- ✓ Use texture atlases to reduce draw calls

### Don'ts

- ✗ Don't call rendering methods outside `BeginFrame`/`EndFrame`
- ✗ Don't create/destroy textures every frame
- ✗ Don't draw UI with camera transforms applied
- ✗ Don't forget to reset shader after use
- ✗ Don't use hardcoded screen dimensions (use config)
- ✗ Don't render invisible objects (cull off-screen entities)

### Performance Tips

**Batch rendering:**

```csharp
// Bad: Many draw calls
foreach (var sprite in sprites)
{
    renderer.DrawTexture(sprite.Texture, sprite.Position);
}

// Good: Batch with instancing
renderer.DrawTextureBatch(sprites);
```

**Sprite sorting:**

```csharp
// Sort by texture to minimize state changes
var sortedSprites = sprites.OrderBy(s => s.Texture.Id);
foreach (var sprite in sortedSprites)
{
    renderer.DrawTexture(sprite.Texture, sprite.Position);
}
```

---

## API Reference

### IRenderBackend

```csharp
public interface IRenderBackend : IDisposable
{
    // Lifecycle
    void Initialize(RenderConfig config);
    void Shutdown();

    // Frame management
    void BeginFrame();
    void EndFrame();
    void Clear(Color color);

    // Primitives
    void DrawRectangle(Rect rect, Color color);
    void DrawRectangleOutline(Rect rect, Color color, float thickness);
    void DrawCircle(Vector2 center, float radius, Color color);
    void DrawCircleOutline(Vector2 center, float radius, Color color, float thickness);
    void DrawLine(Vector2 start, Vector2 end, Color color, float thickness);
    void DrawPolygon(Vector2[] points, Color color);

    // Textures
    void DrawTexture(Texture texture, Vector2 position, Color tint = default);
    void DrawTexture(Texture texture, Vector2 position, float rotation, Vector2 scale,
        Vector2 origin, Color tint);
    void DrawTextureRegion(Texture texture, Rect sourceRect, Vector2 position, Color tint = default);

    // Text
    void DrawText(string text, Vector2 position, Font font, Color color);

    // Advanced
    void SetViewport(Rect viewport);
    void SetCamera(Camera camera);
    void SetShader(Shader shader);
    RenderTarget CreateRenderTarget(int width, int height);
    void BeginRenderTarget(RenderTarget target);
    void EndRenderTarget();
}
```

---

## Related Documentation

- [Architecture](../ARCHITECTURE.md)
- [ECS Module](ECS.md)
- [Resources Module](RESOURCES.md)
- [Input Backend](INPUT_BACKEND.md)

---

**Last Updated:** November 2025  
**Version:** 1.0

# Changelog

All notable changes to MicroEngine will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.4.0-alpha] - 2025-11-17

### Added

-   **Resource System**: Generic resource management with reference counting (`ResourceCache<T>`)
-   **Resource Interfaces**: `IResource`, `ITexture`, `IFont`, `IAudioClip` for type-safe resource handling
-   **Resource Loaders**: `IResourceLoader<T>` interface for backend-specific loading
-   **Raylib Resource Loaders**: Complete implementations for textures (PNG/JPG/BMP/TGA/GIF), fonts (TTF/OTF),
    and audio (WAV/OGG/MP3/FLAC)
-   **Sprite Rendering**: `DrawTexture` and `DrawTexturePro` methods in `IRenderBackend` for texture/sprite
    rendering with rotation, scaling, and tinting
-   **Custom Font Rendering**: `DrawTextEx` method for rendering text with custom fonts
-   **Audio Backend**: `IAudioBackend` interface for sound effects and streaming music playback
-   **Raylib Audio Backend**: Complete implementation with volume control, pause/resume, and streaming support
-   **Math Rectangle**: New `Rectangle` struct with collision detection (`Contains`, `Intersects`)

### Technical Details

-   `ResourceCache<T>`: Reference-counted cache with automatic unloading when ref count reaches zero
-   Resource path normalization for cross-platform compatibility
-   Memory tracking: `SizeInBytes` property for all resources
-   Audio streaming: Separate code paths for sound effects (loaded) vs music (streamed)
-   Texture formats: Support for common image formats with RGBA32 internal format
-   Font loading: TrueType/OpenType fonts with customizable size and spacing
-   Resource disposal: Proper cleanup with `IDisposable` pattern
-   Error handling: File not found and invalid data exceptions with clear messages

## [0.3.0-alpha] - 2025-11-17

### Added

-   **Input System**: Backend-agnostic input abstraction with `IInputBackend` interface
-   **Input Enums**: `Key` (keyboard), `MouseButton`, `GamepadButton`, `GamepadAxis` enumerations
-   **Graphics System**: Backend-agnostic rendering abstraction with `IRenderBackend` interface
-   **Color Struct**: RGBA color representation with common predefined colors (White, Black, Red, Green, Blue, etc.)
-   **Raylib Backend**: Complete implementation of input and graphics backends using Raylib-cs v7.0.2
-   **Visual Demo**: Interactive demo showcasing ECS + rendering with player control (WASD/arrows),
    bouncing enemies, and real-time UI (FPS, entity count)
-   **Input Tests**: Unit tests for Key, MouseButton, GamepadButton, GamepadAxis enums
-   **Graphics Tests**: Unit tests for Color struct (equality, hash code, string representation)

### Technical Details

-   `IRenderBackend`: Window management, frame control, 2D primitives (rectangle, circle, line, text), FPS control
-   `IInputBackend`: Keyboard state (down/pressed/released), mouse position/delta/buttons, gamepad support
-   `RaylibRenderBackend`: Raylib implementation with window lifecycle, 60 FPS target, delta time tracking
-   `RaylibInputBackend`: Raylib implementation with full keyboard/mouse/gamepad input handling
-   Visual demo: 7 entities (1 player, 5 enemies, 1 UI), 4 systems (Movement, PlayerInput, BoundsCheck, Render)
-   Proper rendering flow: update systems in `OnFixedUpdate`, render in `OnRender` (between `BeginFrame`/`EndFrame`)
-   Zero compilation warnings (full XML documentation compliance)

## [0.2.0-alpha] - 2025-11-17

### Added

-   **ECS Module**: Complete Entity-Component-System implementation with World, Entity, Component,
    and System abstractions
-   **Entity Management**: Lightweight entity struct with ID and version for use-after-free
    detection
-   **Component Storage**: Efficient ComponentArray with cache-friendly data layout
-   **System Processing**: ISystem interface for update logic with automatic lifecycle management
-   **ECS Integration**: Scene base class now includes World instance with automatic system updates
-   **ECS Testing**: 27 new unit tests for Entity, World, components, and system processing
-   **ECS Demo**: Interactive demo with 9 entities, 4 component types, and 2 systems
    (MovementSystem, LifetimeSystem)

### Technical Details

-   World manages entities, components, and systems centrally
-   Deferred entity destruction (end-of-frame)
-   Query system for entities by component type
-   Optional entity naming for debugging
-   Public component fields (ECS standard pattern)

## [0.1.0-alpha] - 2025-11-17

### Added

-   **Math Module**: Vector2, Vector3 with comprehensive operations (arithmetic, dot/cross product,
    normalization, distance, interpolation)
-   **Logging System**: ILogger interface with ConsoleLogger implementation, structured LogEntry
    with metadata
-   **Time Management**: GameTime for delta tracking, PrecisionTimer for high-precision timing
-   **Engine Loop**: GameEngine with fixed timestep loop (60 FPS default), configurable
    EngineConfiguration
-   **Scene System**: IScene interface, Scene base class, SceneManager with lifecycle management
    and transitions
-   **Testing**: 64 unit tests covering Math (31), Scenes (24), Time (9) modules
-   **Demo Game**: Sample application demonstrating engine initialization and scene usage
-   **Versioning**: Nerdbank.GitVersioning integration for automated version stamping
-   **Build Configuration**: EditorConfig, Directory.Build.props/targets for consistent C# standards

### Technical Details

-   .NET 9.0 target framework
-   File-scoped namespaces
-   Nullable reference types enabled
-   Clean architecture: 3-layer design (Core → Backends → Game)
-   Dimension-agnostic design
-   Backend-independent implementation

[Unreleased]: https://github.com/NastMz/MicroEngine/compare/v0.2.0-alpha...HEAD
[0.2.0-alpha]: https://github.com/NastMz/MicroEngine/compare/v0.1.0-alpha...v0.2.0-alpha
[0.1.0-alpha]: https://github.com/NastMz/MicroEngine/releases/tag/v0.1.0-alpha

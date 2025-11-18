# Changelog

All notable changes to MicroEngine will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.4.3-alpha] - 2025-11-18

### Added

-   **Input Mapping System**: Action-based input abstraction for multi-device support
-   **Input Actions**: `InputAction` class for defining logical game actions with multiple bindings
-   **Input Bindings**: `InputBinding` struct for keyboard, mouse, and gamepad input bindings
-   **Input Binding Types**: Enum for Keyboard, Mouse, and Gamepad binding types
-   **IInputMap Interface**: Interface for input mapping with action state queries
-   **InputMap Implementation**: Core input mapper with state tracking (pressed, released, held)
-   **Action Chaining**: Fluent API for building actions with multiple bindings
-   **Input State Tracking**: Per-frame tracking of pressed, released, and held states
-   **Multi-Binding Support**: Single action can be triggered by multiple inputs
-   **Remapping Support**: Dynamic control remapping at runtime
-   **Input Action Tests**: 23 comprehensive unit tests for action and binding systems

### Technical Details

-   **Action-Based Design**: Map logical actions ("Jump", "Shoot") to physical inputs
-   **State Machine**: Tracks previous and current states to detect pressed/released events
-   **Binding Flexibility**: Keyboard keys, mouse buttons, gamepad buttons per action
-   **Gamepad Index Support**: Multiple gamepads with independent bindings (0-3)
-   **Fluent Interface**: Chainable `AddKeyboardBinding()`, `AddMouseBinding()`, `AddGamepadBinding()`
-   **Query Methods**: `IsActionPressed()`, `IsActionReleased()`, `IsActionHeld()`, `GetActionValue()`
-   **Backend Integration**: Updates from `IInputBackend` each frame via `Update()` method
-   **Zero Allocations**: Struct-based bindings and pooled state dictionary
-   **Test Coverage**: Constructor validation, binding CRUD, state transitions, equals/hashcode
-   **Full XML Documentation**: All public APIs documented

## [0.4.2-alpha] - 2025-11-18

### Added

-   **Sprite Batching System**: Deferred rendering with automatic batching for optimal performance
-   **Sprite Atlas System**: Texture atlas management with named regions and grid-based frame generation
-   **Sprite Region**: `SpriteRegion` struct for defining rectangular regions within texture atlases with normalized pivot points
-   **Sprite Batch Item**: `SpriteBatchItem` struct for storing all rendering parameters in deferred batching
-   **Sprite Sort Modes**: Five sorting strategies - `Deferred` (no sort), `Texture` (minimize texture switches), `BackToFront` (alpha blending), `FrontToBack` (depth optimization), `Immediate` (no batching)
-   **ISpriteBatch Interface**: Unified interface for sprite batch operations (`Begin`, `Draw`, `DrawRegion`, `End`, `Flush`)
-   **SpriteBatch Implementation**: Core batch renderer with automatic sorting, texture grouping, and minimal draw calls
-   **SpriteAtlas Dictionary**: Region management with `AddRegion`, `GetRegion`, `TryGetRegion`, `HasRegion`, `RemoveRegion`, `Clear`
-   **Grid Generation**: `CreateGrid` static method for automatic spritesheet splitting with spacing and margin support
-   **Sprite Batching Demo**: Performance demo with 1000 animated sprites showcasing batching benefits
-   **Sprite Batching Tests**: 30 comprehensive unit tests covering `SpriteRegion`, `SpriteBatchItem`, and `SpriteAtlas`

### Technical Details

-   **Deferred Rendering**: Sprites queued in `List<SpriteBatchItem>` and rendered on `End()` or `Flush()`
-   **Sort Algorithms**: Texture ID grouping, layer depth sorting (ascending/descending), immediate rendering
-   **Pivot Points**: Normalized (0-1) pivot coordinates for sprite rotation and scaling
-   **Layer Depth**: Clamped 0-1 range (0 = back, 1 = front) for depth-based sorting
-   **Atlas Grid**: Automatic frame calculation with configurable frame size, spacing between frames, and margin from edges
-   **Performance**: Batch 1000 sprites at 60 FPS with minimal draw calls (texture-grouped rendering)
-   **Demo Controls**: B (toggle batching ON/OFF), S (cycle sort modes), R (respawn sprites), ESC (exit)
-   **Test Coverage**: Region equality, atlas CRUD operations, grid frame generation, batch item validation
-   **Zero Compilation Errors**: Full XML documentation and style compliance (14 minor braces warnings in demo)

## [0.4.1-alpha] - 2025-11-18

### Added

-   **Camera2D System**: Full 2D camera implementation with position, rotation, zoom, and follow
-   **Camera Transformations**: `ScreenToWorld` and `WorldToScreen` coordinate conversion methods
-   **Camera Controls**: `Move`, `Rotate`, `AdjustZoom`, `Follow`, `LookAt`, `Reset` utility methods
-   **Visible Bounds**: `GetVisibleBounds` method to query camera viewport in world space
-   **Camera Rendering**: `BeginCamera2D`/`EndCamera2D` methods in `IRenderBackend` for camera-based rendering
-   **Raylib Camera Support**: Complete Raylib backend implementation using `BeginMode2D`/`EndMode2D`
-   **Camera Demo Scene**: Interactive demo showcasing camera movement, zoom, rotation, and follow mode
-   **Camera Unit Tests**: 17 comprehensive tests covering all camera functionality

### Technical Details

-   `Camera2D` class with properties: Position, Offset, Rotation (degrees), Zoom (clamped > 0)
-   Zoom clamping: Prevents zero/negative zoom (min 0.0001f) for stability
-   Smooth camera follow: Lerp-based interpolation with configurable speed
-   Camera rotation: Applied around camera center point
-   Viewport calculation: Automatic visible bounds based on screen size and zoom
-   Demo controls: WASD (move player), arrows (manual camera), Z/X (zoom), F (toggle follow), C (reset), R (rotate)
-   Full coordinate transformation pipeline: screen ↔ world with zoom, rotation, and offset support
-   Zero compilation errors and warnings (full XML documentation compliance)
-   Test coverage: Constructor, properties, transformations, follow, bounds calculation

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

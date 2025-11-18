# Changelog

All notable changes to MicroEngine will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.4.9-alpha] - 2025-11-18

### Changed

-   **Demo Refactoring**: Complete restructure of demo scenes for better organization
-   **Unified Entry Point**: Single `MainMenuScene` for navigating all demos
-   **Scene Navigation**: Menu-based demo selection (press 1-5) with ESC to return
-   **Modern Patterns**: EcsBasicsDemo showcases EntityBuilder and EntityFactory
-   **Clean Architecture**: Organized demos in `Scenes/Demos/` directory

### Removed

-   **Obsolete Demos**: Deleted 8 legacy demo scenes with outdated code patterns
    -   `CameraDemoScene`, `ComponentHelpersDemoScene`, `DemoScene`, `EcsDemoScene`
    -   `InputMapDemoScene`, `ResourceDemoScene`, `SpriteBatchDemoScene`, `VisualDemoScene`
-   **Manual Entity Creation**: Replaced 42 manual `AddComponent` calls with builders

### Added

-   **MainMenuScene**: Central navigation hub with clean menu UI
-   **EcsBasicsDemo**: Live demonstration of EntityBuilder and EntityFactory patterns
-   **Demo Placeholders**: Graphics, Physics, Input, and Tilemap demos (awaiting assets)
-   **Scene Switching**: Basic scene management via `Program.RequestedScene` property

### Technical Details

-   **Navigation**: Keyboard-based (1-5 for demos, ESC for menu/exit)
-   **EcsBasicsDemo**: Shows 15 entities created with Phase 3 patterns
    -   1 Player (EntityBuilder), 4 Enemies (EntityFactory), 6 Collectibles (EntityFactory)
    -   4 Obstacles (EntityBuilder)
-   **Placeholder Demos**: Ready for asset integration (sprites, tilesets)
-   **Code Reduction**: ~800 LOC legacy code replaced with ~600 LOC modern code
-   **Maintainability**: Centralized demo structure simplifies future updates

### Developer Experience

-   **Single Command**: `dotnet run` launches unified demo showcase
-   **Easy Navigation**: No need to modify `Program.cs` to switch demos
-   **Extensibility**: Adding new demos only requires scene creation + menu entry

## [0.4.8-alpha] - 2025-11-18

### Added

-   **Tilemap Support**: Complete grid-based tilemap system for 2D games
-   **Tilemap Class**: Grid storage for tile-based worlds with efficient rendering
-   **TilemapRenderer**: Culling-optimized renderer for visible tiles only
-   **Tilemap Tests**: 18 comprehensive unit tests (13 Tilemap + 5 TilemapRenderer)

### Technical Details

-   **Tilemap**:
    -   Grid storage (width × height) with configurable tile size
    -   `SetTile(x, y, tileId)` and `GetTile(x, y)` for tile manipulation
    -   `WorldToTile(position)` converts world coordinates to grid coordinates
    -   `TileToWorld(x, y)` converts grid coordinates to world position
    -   `Fill(x, y, width, height, tileId)` for bulk tile operations
    -   `Clear()` resets all tiles to empty (ID 0)
    -   `IsEmpty(x, y)` checks for empty tiles
    -   `TotalTileCount` property for grid size
    -   Validation: throws exceptions for out-of-bounds access
-   **TilemapRenderer**:
    -   Integrates with SpriteAtlas for tile textures
    -   `GetVisibleBounds(camera, screenWidth, screenHeight)` calculates visible tiles
    -   `Render(spriteBatch, camera, screenWidth, screenHeight)` draws only visible tiles
    -   `SetTileOffset(x, y)` for parallax scrolling or fine-tuning
    -   Automatic culling skips off-screen tiles
    -   Tile ID 0 reserved for empty (not rendered)
    -   Tile naming convention: `tile_{id}` in SpriteAtlas
-   **Test Coverage**: 18 tests covering construction, validation, coordinate conversion,
    tile manipulation, rendering setup, and culling bounds

### Integration

-   Works with existing SpriteAtlas and SpriteBatch systems
-   Camera2D integration for viewport culling
-   Ready for tilemap-based games (platformers, RPGs, puzzle games)

## [0.4.7-alpha] - 2025-11-18

### Added

-   **Collision Layer System**: Complete physics layer filtering for selective collision detection
-   **CollisionLayer Struct**: Immutable layer representation with ID (0-31) and name
-   **CollisionMatrix Class**: 32x32 bit matrix for configurable layer-to-layer collision rules
-   **PhysicsLayers**: Pre-defined layers (Default, Player, Enemy, Environment, Projectile,
    Trigger, UI, Particle)
-   **Layer Masking**: Bit-based layer masks for efficient collision filtering
-   **Collision Layer Tests**: 19 comprehensive unit tests (6 CollisionLayer + 7 CollisionMatrix
    -   6 PhysicsLayers)

### Changed

-   **Code Standards Compliance**: All constants refactored to ALL_UPPER naming convention per
    .editorconfig requirements (MINIMUM_LAYER_ID, MAXIMUM_LAYER_ID, TOTAL_LAYER_COUNT, etc.)

### Technical Details

-   **CollisionLayer**:
    -   Readonly struct with Id (0-31) and Name properties
    -   `GetMask()` returns bit mask (1 << Id) for layer
    -   Equality comparison based on layer ID
    -   Validation: throws ArgumentException for invalid IDs
-   **CollisionMatrix**:
    -   32x32 uint array for collision rules (1024 bits total)
    -   `SetCollision(layer1, layer2, bool)` configures layer pairs
    -   `CanCollide(layer1, layer2)` queries collision permission
    -   `CanCollide(mask1, mask2)` supports multi-layer masks
    -   `IgnoreLayerCollision(id1, id2)` helper for disabling collisions
    -   `EnableLayerCollision(id1, id2)` helper for enabling collisions
    -   `Clear()` resets to default (all collisions enabled)
    -   Symmetric collision rules (if A collides with B, B collides with A)
-   **PhysicsLayers**:
    -   8 pre-defined layers for common game scenarios
    -   Layer 0: Default (general-purpose objects)
    -   Layer 1: Player (player-controlled entities)
    -   Layer 2: Enemy (AI-controlled enemies)
    -   Layer 3: Environment (static world geometry)
    -   Layer 4: Projectile (bullets, missiles)
    -   Layer 5: Trigger (trigger zones, collectibles)
    -   Layer 6: UI (UI elements with physics)
    -   Layer 7: Particle (particle effects)
-   **Integration**:
    -   ColliderComponent already has `LayerMask` property (int)
    -   Ready for physics system integration
    -   Supports up to 32 simultaneous layers per entity
-   **Test Coverage**: 19 tests covering construction, validation, equality, bit masking,
    collision matrix operations, and pre-defined layers

### Notes

-   Collision layer system designed for flexible, performant collision filtering
-   Bit-based implementation allows efficient multi-layer collision checks
-   Symmetric collision rules ensure consistent behavior
-   Pre-defined layers cover 90% of common game scenarios
-   Custom layers (8-31) available for game-specific needs

## [0.4.6-alpha] - 2025-11-18

### Added

-   **EntityBuilder**: Fluent API for declarative entity creation with method chaining
-   **EntityFactory**: Pre-configured archetypes for common game entities (Player, Platform,
    Obstacle, Collectible, Projectile, Enemy, Particle)
-   **Entity Creation Helpers**: Streamlined entity creation reducing boilerplate by ~70%
-   **EntityBuilder Tests**: 11 comprehensive unit tests for fluent API and component composition
-   **EntityFactory Tests**: 8 unit tests validating archetype creation and default configurations

### Technical Details

-   **EntityBuilder Fluent API**: `WithName()`, `WithTransform()`, `WithSprite()`,
    `WithRigidBody()`, `WithBoxCollider()`, `WithCircleCollider()`, `Build()`
-   **Method Chaining**: All builder methods return `EntityBuilder` for seamless composition
-   **Transform Overloads**: Position-only and full transform (position, rotation, scale) variants
-   **Named Entities**: `WithName()` support via entity recreation with identity preservation
-   **Physics Defaults**: Sensible defaults for mass (1.0), drag (0), gravity (enabled)
-   **Collider Factories**: Box and circle colliders with optional trigger mode
-   **EntityFactory Archetypes**:
    -   `CreatePlayer()`: 32x32 sprite, physics enabled, box collider
    -   `CreatePlatform()`: Static platform with configurable size
    -   `CreateObstacle()`: Static obstacle (50x70 default)
    -   `CreateCollectible()`: Trigger-based collectible with circle collider (radius 16)
    -   `CreateProjectile()`: Small moving entity (8x8) with initial velocity
    -   `CreateEnemy()`: 32x32 entity with physics and AI-ready setup
    -   `CreateParticle()`: Tiny entity (0.5x scale) with physics and custom velocity
-   **Code Reduction**: ComponentHelpersDemoScene can reduce from ~100 LOC to ~25 LOC using helpers
-   **Test Coverage**: 19 total tests (11 builder + 8 factory) with 100% pass rate
-   **Zero Compilation Errors**: Full XML documentation compliance
-   **TDD Methodology**: Tests written before implementation (RED→GREEN→REFACTOR)

### Notes

-   EntityBuilder and EntityFactory designed for ergonomic game development workflows
-   Helpers eliminate repetitive `CreateEntity()` + `AddComponent()` patterns
-   Archetypes provide consistent defaults while remaining fully customizable
-   Future work: Add more archetypes (NPC, Weapon, PowerUp, etc.) and builder methods for
    additional components

## [0.4.5-alpha] - 2025-11-18

### Added

-   **Event System**: Complete event-driven architecture with EventBus for decoupled communication
-   **EventBus**: Thread-safe publish/subscribe system with queued and immediate dispatch modes
-   **Game Events**: Pre-defined events for entity lifecycle (Created, Destroyed, ComponentAdded,
    ComponentRemoved)
-   **Collision Events**: CollisionEnterEvent and CollisionExitEvent with entity and contact info
-   **IEvent Interface**: Base event contract with Timestamp and IsHandled properties
-   **Event Queue**: Deferred event processing with `PublishDeferred()` and `ProcessEvents()`
-   **Event Subscriptions**: Type-safe `Subscribe<T>()` and `Unsubscribe<T>()` with delegate
    management
-   **Resource Hot-Reload**: File system monitoring with automatic resource reloading on changes
-   **FileSystemResourceWatcher**: Cross-platform file watching using FileSystemWatcher
-   **HotReloadableResourceCache**: Resource cache with automatic reload on file modifications
-   **IResourceWatcher**: Interface for resource file monitoring with change event notifications
-   **Resource Change Events**: Modified, Deleted, and Renamed event types for resource tracking
-   **Resource Metadata**: Comprehensive metadata system for resources (file size, modified date,
    custom data)
-   **ResourceMetadata Class**: Encapsulates resource metadata with custom key-value storage
-   **Resource Validation**: Pre-load validation system for file existence, format, and security
-   **ResourceValidator**: Validates resource files before loading (file exists, readable, size
    limits)
-   **ResourceValidationResult**: Validation result with success/failure status and error messages
-   **Performance Profiling Guide**: Complete documentation for profiling engine performance
    (PERFORMANCE_PROFILING.md)

### Technical Details

-   **EventBus Features**:
    -   Thread-safe with lock-based synchronization
    -   Immediate dispatch via `Publish<T>()` for real-time events
    -   Deferred dispatch via `PublishDeferred<T>()` for frame-end processing
    -   `ProcessEvents()` processes all queued events in order
    -   `Clear()` empties event queue and removes all subscriptions
    -   `GetSubscriberCount<T>()` queries subscription counts per event type
    -   Full exception handling with detailed error messages
-   **Game Events**:
    -   `EntityCreatedEvent` with entity ID
    -   `EntityDestroyedEvent` with entity ID
    -   `ComponentAddedEvent` with entity ID and component type name
    -   `ComponentRemovedEvent` with entity ID and component type name
    -   `CollisionEnterEvent` with both entity IDs and contact point
    -   `CollisionExitEvent` with both entity IDs
-   **Resource Hot-Reload**:
    -   `Watch(string path)` monitors individual files or directories
    -   `Unwatch(string path)` stops monitoring specific paths
    -   `ResourceChanged` event with change type (Modified, Deleted, Renamed)
    -   Debouncing to avoid duplicate change events
    -   Automatic cleanup on disposal
    -   `HotReloadableResourceCache.EnableAutoReload` toggle for runtime control
-   **Resource Metadata**:
    -   `FilePath`, `FileSize`, `LastModified` standard properties
    -   `SetCustomData<T>()` and `GetCustomData<T>()` for extensibility
    -   Immutable metadata snapshots for thread safety
-   **Resource Validation**:
    -   File existence checks before loading
    -   Readable file validation (access permissions)
    -   File size limits (configurable, default 100MB for textures)
    -   Format validation via file extension
    -   `IsValid` boolean and `ErrorMessage` for diagnostic feedback
-   **Test Coverage**:
    -   EventBus: 15 tests (subscribe, publish, queue, unsubscribe, thread safety)
    -   FileSystemResourceWatcher: 10 tests (watch, unwatch, change detection, disposal)
    -   HotReloadableResourceCache: 12 tests (load, reload, failure scenarios, memory leaks)
    -   ResourceMetadata: 8 tests (construction, custom data, immutability)
    -   ResourceValidator: 9 tests (validation rules, error messages, edge cases)
-   **Documentation**:
    -   PERFORMANCE_PROFILING.md with 333 lines covering profiling strategies, tools, and
        benchmarks

### Notes

-   EventBus designed for decoupled, event-driven game architecture
-   Hot-reload system enables live asset editing without engine restart (development only)
-   Resource validation prevents loading corrupted or malicious files
-   Metadata system extensible for custom resource attributes (compression, format version, etc.)
-   All systems follow thread-safe patterns for potential multi-threaded usage

## [0.4.4-alpha] - 2025-11-18

### Added

-   **Component Helpers**: Reusable game object components for common functionality
-   **TransformComponent**: Spatial transformation with position, rotation, scale, and origin
-   **SpriteComponent**: 2D sprite rendering with texture atlas support, tint, visibility, layer, and flip options
-   **RigidBodyComponent**: 2D physics simulation with velocity, acceleration, mass, drag, gravity scale
-   **ColliderComponent**: Collision detection with Circle, Rectangle, and Point shapes
-   **AnimatorComponent**: Frame-based sprite animation with clips, speed, looping, and sprite integration
-   **Component Helpers Tests**: 43 unit tests for transform, rigidbody, and collider components
-   **ComponentHelpersDemoScene**: Interactive demo showcasing physics, collision, and component composition

### Technical Details

-   **Modular Design**: Components can be used independently or composed together
-   **TransformComponent**: `Translate()`, `Rotate()`, `ScaleBy()`, `GetForward()`, `GetRight()` helpers
-   **SpriteComponent**: `SetSpriteFromAtlas()`, `GetSourceRectangle()`, `GetSize()` for atlas integration
-   **RigidBodyComponent**: `ApplyForce()` (F=ma), `ApplyImpulse()` (instant velocity change), `Stop()` helper
-   **ColliderComponent**: `CreateCircle()`, `CreateRectangle()`, `CreatePoint()` factory methods
-   **ColliderComponent**: `Overlaps()`, `ContainsPoint()`, `GetBounds()` for collision queries
-   **ColliderComponent**: Layer masking, trigger support, offset positioning
-   **AnimatorComponent**: `Play()`, `Stop()`, `Pause()`, `Resume()` with restart control
-   **AnimatorComponent**: `AnimationClip` with frame duration, looping, FPS conversion
-   **Physics Integration**: Gravity, jumping, ground collision, screen bounds in demo
-   **Class-Based Components**: Designed for manual composition (not struct-based ECS)
-   **Full XML Documentation**: All public APIs documented
-   **Demo Features**: Player movement, physics, obstacle collisions, collectibles, score

### Notes

-   Component Helpers are designed for manual composition, not for use with the struct-based ECS system
-   Future work may include struct-based component variants for ECS integration
-   Components follow the Single Responsibility Principle for maximum reusability

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

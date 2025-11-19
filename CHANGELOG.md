# Changelog

All notable changes to MicroEngine will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.11.0-alpha] - 2025-11-19

### Added

-   **TextLayoutHelper**: Automatic text positioning helper for UI rendering
    -   Eliminates manual Y-coordinate tracking for text rendering
    -   Fluent API with method chaining support
    -   Methods: `DrawText`, `AddSpacing`, `SetX/SetY`, `DrawSection`, `DrawKeyValue`, `NewColumn`
    -   Automatic line height advancement with customizable spacing
    -   Properties: `CurrentX`, `CurrentY`, `DefaultLineHeight`
    -   18 comprehensive unit tests with 100% pass rate
    -   Integrated in all 8 scenes (7 demos + MainMenu)
    -   Average 30% reduction in UI rendering code

### Changed

-   **All Demos & MainMenu**: Refactored to use TextLayoutHelper
    -   `GraphicsDemo`: 77 → 43 lines (44% reduction)
    -   `TilemapDemo`: Cleaner legend rendering with helper function
    -   `InputDemo`: Improved dynamic list rendering with `SetX()` and custom line heights
    -   `EcsBasicsDemo`, `AudioDemo`, `PhysicsDemo`, `PhysicsBackendDemo`: Simplified UI code
    -   `MainMenuScene`: Refactored menu and cache status rendering
    -   Eliminated manual `yPos`/`uiY` tracking across all scenes
    -   More readable and maintainable UI rendering code

### Improved

-   **Code Quality**: Reduced UI rendering code verbosity by ~30% across all demos
-   **Maintainability**: Centralized text layout logic in reusable helper class
-   **Developer Experience**: Fluent API makes UI code more readable and less error-prone

### Fixed

-   **ECS Entity Queries**: `World.GetEntitiesWith<T>()` now automatically filters out entities pending destruction
    -   Prevents accessing destroyed entities without manual validation
    -   Developers no longer need to call `IsEntityValid()` manually
    -   Fixes potential crashes when iterating over entities that were just destroyed
    -   Added comprehensive unit test coverage for this edge case

## [0.10.0-alpha] - 2025-11-19

### Added

-   **ECS Components & Systems**: Camera and drag abstractions for reusable game logic

    -   `CameraComponent`: Data component for camera control commands
        -   Camera2D instance with movement and zoom settings
        -   Input-agnostic commands: MoveDirection, ZoomDelta, ResetRequested
        -   Configurable speed, zoom limits, and default position
    -   `CameraControllerSystem`: Stateless system for camera updates
        -   Processes movement, zoom, and reset commands from CameraComponent
        -   Input-agnostic design (works with WASD, arrows, gamepad, touch, etc.)
        -   Automatic command clearing after processing
    -   `DraggableComponent`: Data component for entity dragging
        -   Drag state tracking (IsDragging, DragOffset)
        -   Optional kinematic switching during drag
        -   Input-agnostic commands: StartDragRequested, DragPosition, StopDragRequested
    -   `DragSystem`: Stateless system for entity dragging logic
        -   Processes drag commands from DraggableComponent
        -   Automatic kinematic/dynamic switching for physics bodies
        -   Input-agnostic design (works with mouse, touch, gamepad, etc.)

-   **Unit Tests**: Comprehensive test coverage for new systems
    -   `CameraControllerSystemTests`: 8 tests covering movement, zoom, reset, clamping
    -   `DragSystemTests`: 10 tests covering drag start/stop, kinematic switching, edge cases

### Changed

-   **GraphicsDemo**: Refactored to use CameraControllerSystem
    -   Replaced ~50 lines of manual camera logic with ECS integration
    -   Camera movement now managed by CameraControllerSystem
    -   Input translated to camera commands (MoveDirection, ZoomDelta, ResetRequested)
-   **TilemapDemo**: Refactored to use CameraControllerSystem
    -   Migrated from Vector2 offset to Camera2D with CameraControllerSystem
    -   Simplified camera movement and reset logic
    -   Consistent camera API across all demos
-   **PhysicsDemo**: Refactored to use DragSystem
    -   Replaced ~40 lines of manual drag logic with ECS integration
    -   Entity dragging now managed by DragSystem
    -   Automatic kinematic switching when dragging physics bodies
    -   Added DraggableComponent to spawned balls

### Removed

-   **RigidBodyComponent**: Cleaned up unused properties
    -   Removed `Acceleration` property (not used by physics backends)
    -   Removed `GravityScale` property (UseGravity boolean is sufficient)
    -   Removed `UseContinuousCollision` property (handled internally by backends)
    -   Simplified component to only include actively used properties
-   **EntityBuilder**: Removed gravityScale parameter from WithRigidBody method

### Improved

-   **Code Reduction**: Removed ~150 lines of duplicate logic from demos
-   **Maintainability**: Camera and drag logic now centralized in reusable systems
-   **Testability**: Input-agnostic design enables easy unit testing (16 new tests added)
-   **Extensibility**: Easy to add new features (camera boundaries, drag constraints, etc.)
-   **Consistency**: All demos now follow ECS patterns (Component + System > Helper)

## [0.9.0-alpha] - 2025-11-18

### Added

-   **Physics Backend Abstraction**: Professional-grade physics engine integration

    -   `IPhysicsBackend`: Backend-agnostic physics simulation interface
        -   World management (Initialize, Shutdown, Step)
        -   Gravity control (SetGravity, GetGravity)
        -   Body lifecycle (CreateBody, DestroyBody, SetBodyType)
        -   Collider attachment (AddCircleCollider, AddBoxCollider)
        -   Position and velocity manipulation
        -   Force and impulse application
        -   Gravity and damping configuration
    -   `MicroEngine.Backend.Aether`: Aether.Physics2D integration (Box2D port)
        -   Realistic rigid body dynamics and collision resolution
        -   Pixels-to-meters conversion (100:1 ratio)
        -   Body type support (Static, Kinematic, Dynamic)
        -   Integration with MonoGame.Framework
    -   `PhysicsBackendSystem`: ECS integration for physics simulation
        -   `PhysicsBodyComponent`: Links ECS entities to physics bodies
        -   Bidirectional synchronization (ECS ↔ Physics)
        -   Helper methods: CreateBodyForEntity, DestroyBodyForEntity
        -   Physics operations: ApplyForce, ApplyImpulse, Stop
    -   `PhysicsBackendDemo`: Advanced physics demonstration scene
        -   Interactive ball spawning and drag-and-drop
        -   Realistic stacking and falling physics
        -   Kinematic body switching during user interaction

### Changed

-   **PhysicsDemo**: Migrated to use PhysicsBackendSystem with Aether.Physics2D
    -   Replaced custom physics with professional backend
    -   Improved ball stacking and collision resolution
    -   Fixed drag system with proper kinematic/dynamic body switching
    -   Simplified drag logic (no manual collision checks needed)

### Removed

-   **Legacy Physics System**: Removed custom physics implementation
    -   Removed `PhysicsSystem.cs` (replaced by PhysicsBackendSystem)
    -   Removed `CollisionSystem.cs` (handled by physics backend)
    -   Custom CCD implementation superseded by Aether.Physics2D

### Fixed

-   **Drag Bug**: Fixed physics bodies not responding after drag-and-drop
    -   Added `SetBodyType` method to IPhysicsBackend for dynamic type changes
    -   PhysicsBackendSystem now synchronizes IsKinematic changes to backend
    -   Bodies properly transition between kinematic and dynamic states
-   **Demo OnUnload**: Fixed null reference crashes during scene preloading
    -   All demos updated with null-safe OnUnload methods
    -   Logger calls use null-conditional operator (?.)
    -   PhysicsSystem cleanup checks for null before operations

## [0.8.0-alpha] - 2025-11-18

### Added

-   **Improved Physics Accuracy**: Continuous Collision Detection (CCD) to prevent tunneling

    -   `CollisionInfo`: Struct containing collision normal, penetration, contact point, and time of impact
    -   `SweptCollision`: Static class providing swept AABB collision detection
        -   Prevents fast-moving objects from passing through other objects
        -   Ray vs AABB intersection testing for precise collision detection
        -   Time of impact calculation (0-1 range for interpolated collision points)
    -   Enhanced `PhysicsSystem` with automatic CCD integration
        -   Checks all potential collisions along movement path
        -   Resolves collisions at earliest time of impact
        -   Applies restitution (bounciness) coefficient
    -   Enhanced `CollisionSystem` with collision resolution
        -   `CheckSweptCollision`: Performs swept collision detection for moving entities
        -   `ResolveCollision`: Adjusts position and velocity based on collision normal
    -   New `RigidBodyComponent` properties:
        -   `Restitution`: Bounciness coefficient (0-1 range)
        -   `UseContinuousCollision`: Toggle for CCD (prevents tunneling when enabled)
    -   `RenderComponent`: Visual rendering data component for basic shapes (Rectangle, Circle, Line)
    -   **PhysicsDemo**: Fully functional physics demonstration
        -   Spawns bouncing balls with gravity and collisions
        -   Ground platform with static collider
        -   Click to spawn balls or automatic spawning
        -   Visual feedback with random colors
        -   Demonstrates CCD preventing tunneling

-   **Savegame System**: Complete save/load system with versioning and backward compatibility
    -   `SaveMetadata`: Metadata container with versioning, timestamps, and custom data
        -   Semantic versioning for save format (default: 1.0.0)
        -   Engine version tracking for compatibility checks
        -   Creation and modification timestamps (UTC)
        -   Optional user-defined save names and custom key-value data
    -   `SaveContainer<T>`: Generic save file wrapper with metadata and game data
    -   `ISaveSerializer`: Abstraction for serialization format (JSON, binary, etc.)
    -   `JsonSaveSerializer`: JSON-based serializer using System.Text.Json
        -   Configurable indentation for human-readable saves
        -   Camelcase property naming convention
        -   Null value omission for smaller file sizes
    -   `ISavegameManager`: Interface for savegame operations (save, load, delete, list, metadata)
    -   `SavegameManager`: Full-featured savegame manager implementation
        -   Configurable save directory (default: ./Saves)
        -   Automatic .sav file extension handling
        -   Preserves creation timestamp on overwrite
        -   Updates last modified timestamp
        -   List all saves in directory
        -   Lightweight metadata-only loading for save listings
        -   Type-safe generic save/load operations
        -   Comprehensive error handling with result types (`SaveResult`, `LoadResult<T>`)
    -   10 comprehensive tests covering all savegame functionality (645 total tests)

### Changed

-   `PhysicsSystem.ApplyForce`: Made static for consistency
-   `PhysicsSystem.ApplyImpulse`: Made static for consistency
-   `PhysicsSystem.Stop`: Made static for consistency
-   `PhysicsSystem.Update`: Enhanced with CCD integration and collision resolution
-   `CollisionSystem`: Added swept collision and resolution methods

## [0.7.5-alpha] - 2025-11-18

### Added

-   **Memory Profiling Tools**: Comprehensive memory profiling and leak detection system
    -   `MemorySnapshot`: Immutable snapshots of memory state with GC statistics
        -   Captures total memory, generation-specific GC statistics (Gen0/1/2)
        -   Custom metrics dictionary for domain-specific tracking
        -   Delta calculation for memory regression detection
    -   `MemoryProfiler`: Time-series memory tracking with leak detection
        -   Configurable snapshot history with automatic LRU eviction (default: 1000)
        -   Baseline tracking for delta calculations
        -   Leak detection using monotonic increase analysis (10 sample window, 5 MB threshold)
        -   CSV export for external analysis
        -   Statistical analysis (min/max/avg memory, GC collections)
    -   `EcsMemoryProfiler`: ECS-specific memory profiling
        -   Entity and system count tracking
        -   Component memory estimation (160 bytes per entity)
        -   Async monitoring over configurable time periods
        -   Formatted memory report generation with leak detection
    -   `ProfilingException`: Structured exceptions for profiling errors (PROF-xxx)
        -   `InvalidProfilingOperationException`: Invalid profiling operation (PROF-400)
        -   `InvalidProfilingConfigurationException`: Invalid configuration parameter (PROF-422)
    -   **GameEngine Integration**: Automatic memory profiling when enabled
        -   `EnableMemoryProfiling` configuration option (default: false)
        -   `MemorySnapshotInterval` for capture frequency (default: 60 frames)
        -   Accessible via `GameEngine.MemoryProfiler` property
        -   Non-intrusive: Zero performance impact when disabled
    -   **Structured Exception Migration**: ECS module now uses domain-specific exceptions
        -   Updated `World`, `ComponentArray`, `Archetype` to use ECS exceptions
        -   Enhanced error context with entity IDs, component types, and archetype information
        -   Overloaded constructors for Type and string-based component type names
    -   Parameter validation with descriptive error messages and context
    -   Thread-safe profiler implementation with lock-based protection
    -   43 comprehensive tests covering all profiling components and exception handling (635 total)

## [0.7.4-alpha] - 2025-11-18

### Added

-   **Structured Error Codes & Exception Hierarchy**: Comprehensive exception system with error codes and context
    -   `MicroEngineException`: Base exception with error code and context support
    -   `EcsException`: ECS-related errors (ECS-404, ECS-405, ECS-409, ECS-400, ECS-500)
        -   `EntityNotFoundException`: Entity not found (ECS-404)
        -   `ComponentNotFoundException`: Component not found on entity (ECS-405)
        -   `DuplicateComponentException`: Duplicate component (ECS-409)
        -   `InvalidEntityOperationException`: Invalid entity operation (ECS-400)
        -   `WorldException`: World operation failures (ECS-500)
    -   `ResourceException`: Resource-related errors (RES-404, RES-500, RES-400, RES-422)
        -   `ResourceNotFoundException`: Resource file not found (RES-404)
        -   `ResourceLoadException`: Resource loading failed (RES-500)
        -   `InvalidResourceFormatException`: Invalid resource format (RES-400)
        -   `ResourceValidationException`: Validation failed (RES-422)
    -   `SceneException`: Scene-related errors (SCENE-500, SCENE-400, SCENE-409)
        -   `SceneTransitionException`: Scene transition failed (SCENE-500)
        -   `SceneLifecycleException`: Scene lifecycle error (SCENE-400)
        -   `InvalidSceneOperationException`: Invalid scene operation (SCENE-409)
    -   `PhysicsException`: Physics-related errors (PHYS-400, PHYS-500)
        -   `InvalidCollisionConfigurationException`: Invalid collision config (PHYS-400)
        -   `PhysicsSimulationException`: Physics simulation error (PHYS-500)
    -   `BackendException`: Backend-related errors (BACKEND-500, BACKEND-400)
        -   `BackendInitializationException`: Backend initialization failed (BACKEND-500)
        -   `BackendOperationException`: Backend operation failed (BACKEND-400)
    -   Context management with `WithContext(key, value)` for structured logging
    -   Enhanced `ToString()` with error code, context, and inner exception details
    -   28 comprehensive exception tests covering all exception types

## [0.7.3-alpha] - 2025-11-18

### Added

-   **Performance Benchmarking Suite**: Integrated BenchmarkDotNet with comprehensive ECS performance tests
    -   ECS Query benchmarks: 1-4 component queries with 100-10,000 entities
    -   Entity lifecycle benchmarks: Creation and destruction with 0-3 components
    -   Component operation benchmarks: Access, modification, and system simulation
    -   Validated archetype optimization: Linear O(n) scaling, zero allocations during queries
    -   Benchmark results: ~270 μs for 10,000 entity queries (37,000 queries/sec), ~2.4 μs for 100 entities (424,000 queries/sec)
    -   Complete documentation with usage guide and performance interpretation

## [0.7.2-alpha] - 2025-11-18

### Added

-   **Archetype-Based ECS Optimization** (simple implementation, no reflection/dynamic)
    -   `ArchetypeId`: Component composition hash for archetype identification
    -   `Archetype`: Groups entities with identical component sets using existing `IComponentArray`
    -   `ArchetypeManager`: Manages archetype lifecycle and entity-archetype mapping
    -   `AddBoxed(Entity, object)` method in `IComponentArray` for type-safe boxed component storage
    -   Query optimization: `CachedQuery.Refresh()` iterates matching archetypes instead of all entities
    -   Automatic archetype updates when components are added/removed
    -   14 comprehensive archetype tests (composition, queries, lifecycle)
    -   Expected 3-10x performance improvement for queries on large entity counts
    -   Zero overhead for existing code: transparent integration with `World` and `CachedQuery`

## [0.7.1-alpha] - 2025-11-18

### Added

-   **Scene Preloading (Async Background Loading)**: Complete implementation
    -   `PreloadAsync<T>` for single scene background loading with CancellationToken support
    -   `PreloadMultipleAsync` for parallel batch preloading of multiple scenes
    -   `ScenePreloaded` event with success/failure tracking and exception details
    -   `IsPreloading(sceneKey)` status checking
    -   MainMenuScene automatically preloads all 5 demo scenes on startup (~8ms total)
    -   Zero-latency scene transitions: all demos load instantly from cache
    -   14 comprehensive async tests (cancellation, events, concurrency, duplicate handling)
    -   Task-based async API fully integrated with existing synchronous Preload

### Changed

-   **Version Management**: Migrated to Nerdbank.GitVersioning (industry standard)
    -   Removed custom PowerShell script (`generate-version.ps1`)
    -   Removed MSBuild target for manual version generation
    -   EngineInfo now wraps Nerdbank's `ThisAssembly` class
    -   Added git metadata: `GitCommitId`, `GitCommitDate`
    -   No PowerShell dependency for build process
    -   VERSION_MANAGEMENT.md updated with Nerdbank documentation

### Fixed

-   **EcsBasicsDemo**: Entity duplication on cache hits
    -   Added `ClearEntities()` method to properly reset scene state
    -   `OnLoad()` now cleans up existing entities before creating new ones
    -   Consistent 30 entities on every scene load (cache hit or miss)

## [0.7.0-alpha] - 2025-11-18

### Added

-   **Scene Caching System**: ISceneCache and SceneCache for scene reuse and lazy loading
    -   Reduces memory allocation overhead by caching scene instances
    -   LRU (Least Recently Used) eviction policy with configurable max cache size (default 10)
    -   Thread-safe implementation using ConcurrentDictionary and locks
    -   Methods: `GetOrCreate<T>`, `Preload<T>`, `TryGet<T>`, `Contains`, `Remove`, `Clear`, `GetCachedKeys`
    -   Automatic OnUnload() calls on evicted scenes
    -   Integrated in MainMenuScene for demo navigation (max 5 scenes)
    -   Visual cache status display showing hits/misses and stored count
    -   Use cases: level caching, menu caching, background preloading
    -   30 comprehensive unit tests including concurrency tests
-   **Global State Management**: IGameState and GameState for persistent data across scenes
    -   Thread-safe implementation using ConcurrentDictionary
    -   Type-safe API: `Get<T>`, `TryGet<T>`, `Set<T>`, `Contains`, `Remove`, `Clear`, `GetKeys`
    -   Integrated into SceneContext as 6th core service
    -   Use cases: player progress, settings, high scores, session data
    -   21 comprehensive unit tests
-   **State Machine Framework**: Generic `StateMachine<TState, TTrigger>` for finite state machines
    -   Fluent configuration API via `Configure(state)` method
    -   Guarded transitions with `PermitIf(trigger, targetState, guard)`
    -   Entry and exit actions for state lifecycle management
    -   Operations: `Fire`, `FireStrict`, `CanFire`, `Reset`
    -   Use cases: AI behaviors, UI flows, game states, player controllers
    -   23 comprehensive unit tests
-   **GameEngine Refactoring**: Full integration with current architecture
    -   GameEngine now accepts IRenderBackend2D and IInputBackend in constructor
    -   Initialize(SceneContext) method for proper service injection
    -   Fixed timestep accumulator (60 Hz / 16.67ms) for deterministic physics
    -   Variable render rate (uncapped or V-sync) as per engine design
    -   Spiral of death prevention (max 5 fixed updates per frame)
    -   Separation of concerns: FixedUpdate (physics), Update (logic), Render
    -   Program.cs migrated from manual loop to GameEngine.Run()
    -   Frame-rate independent gameplay guaranteed

### Changed

-   **SceneContext**: Breaking change - constructor now requires 6 parameters (was 5)
    -   Added `IGameState GameState` property and constructor parameter
    -   All creation sites updated (Program.cs, SceneManagerTests.cs)
-   **Program.cs**: Simplified to use GameEngine instead of manual game loop
    -   Removed manual while loop with input/update/render calls
    -   GameEngine.Run() now handles entire game loop lifecycle
    -   Rendering now uncapped (was artificially limited to 60 FPS)
    -   Fixed timestep properly implemented for future physics integration

## [0.6.0-alpha] - 2025-11-18

### Added

-   **SceneContext Class**: Dependency injection container for engine services
    -   Contains 5 core services: RenderBackend, InputBackend, TimeService, Logger, TextureCache
    -   Constructor validates all parameters are non-null
    -   Explicit dependency declaration replaces static service locator pattern
    -   Enables proper unit testing with mock contexts
    -   **No circular dependency** - SceneManager is NOT part of SceneContext
-   **Scene Navigation Methods**: Protected methods in Scene base class
    -   `PushScene(Scene)`: Push a new scene onto the stack
    -   `PopScene()`: Pop the current scene from the stack
    -   `ReplaceScene(Scene)`: Replace the current scene with a new one
    -   Scenes delegate to SceneManager internally via `SetSceneManager()`
    -   Clean API: scenes don't need direct access to SceneManager
-   **Anisotropic Filtering Support**: Proper OpenGL implementation for texture filtering
    -   P/Invoke calls to `glBindTexture` and `glTexParameterf` for direct GL control
    -   Correct configuration of `GL_TEXTURE_MAX_ANISOTROPY_EXT` parameter
    -   Support for 4x, 8x, and 16x anisotropic filtering levels
    -   Base filter set to Trilinear (required for anisotropic filtering)
    -   Fixes issue where anisotropic filter inherited behavior from previous filter
-   **Automatic Mipmap Generation**: For filters requiring mipmaps
    -   Trilinear and Anisotropic filters auto-generate mipmaps if needed
    -   GenerateMipmaps() now re-applies current filter after generation
    -   Prevents filter reset during mipmap generation
    -   User-friendly: F3/F4 work immediately without manual mipmap generation
-   **Scene Transition Effects**: Multiple transition effects for scene changes
    -   **SlideTransition**: Slides scene in 4 directions (Left, Right, Up, Down)
    -   **WipeTransition**: 6 wipe modes (LeftToRight, RightToLeft, TopToBottom, BottomToTop, CenterOut, EdgeIn)
    -   **ZoomTransition**: 2 zoom modes (ZoomIn, ZoomOut) with vignette effect
    -   All transitions implement ISceneTransitionEffect interface
    -   Configurable duration, direction/mode, and colors
    -   MainMenuScene now includes F6-F9 selector for transition effects
-   **SceneManager.SetTransition()**: Runtime transition effect changes
    -   Allows changing transition effect dynamically during gameplay
    -   Resets transition state when changed
    -   Logs transition changes for debugging
    -   Enables user-selectable transition preferences
-   **Scene Parameter Passing**: Type-safe data transfer between scenes
    -   **SceneParameters** class: Immutable, fluent builder pattern for parameter construction
    -   `Get<T>(key)`: Type-safe parameter retrieval with compile-time checking
    -   `TryGet<T>(key, out value)`: Safe retrieval without exceptions
    -   `Contains(key)`: Check parameter existence
    -   **Scene.OnLoad(context, parameters)**: Overload receives optional parameters
    -   **SceneManager overloads**: `PushScene(scene, parameters)` and `ReplaceScene(scene, parameters)`
    -   **Backward compatible**: Existing code without parameters continues to work
    -   **Demo**: InputDemo showcases parameter reception from MainMenu
-   **ITimeService Interface**: Platform-agnostic time management abstraction
    -   `DeltaTime` property (time since last frame in seconds)
    -   `CurrentFPS` property (actual frames per second)
    -   `TargetFPS` property (desired frame rate)
    -   `TotalTime` property (elapsed time since engine start)
    -   `Update()` method (calculates delta time and manages frame rate)
    -   `Reset()` method (resets timing state)
-   **TimeService Implementation**: High-precision timing using .NET Stopwatch
    -   Uses `Stopwatch` for microsecond-accurate frame timing
    -   Frame rate limiting with configurable target FPS
    -   Automatic FPS calculation (updates every second)
    -   Delta time clamping (max 100ms to prevent spiral of death)
    -   Thread-safe read operations
-   **Two-Phase SceneManager Initialization**: Solves circular dependency
    -   Constructor: Takes optional transition effect only
    -   Initialize(SceneContext): Receives full context after construction
    -   Enables SceneManager to be part of SceneContext without recursion

### Changed

-   **IScene.OnLoad Signature**: Now receives SceneContext parameter
    -   Old: `void OnLoad()` - no parameters, dependencies implicit
    -   New: `void OnLoad(SceneContext context)` - explicit dependency injection
    -   Scenes store context in protected `Context` property
    -   Breaking change: All scenes must update OnLoad signature
-   **Scene Base Class**: Added protected Context property
    -   `protected SceneContext Context { get; private set; } = null!;`
    -   Available after OnLoad completes
    -   Accessible to all derived scenes for service access
-   **Program.cs Architecture**: No longer a static service locator
    -   Removed 6 public static properties: SceneManager, InputBackend, RenderBackend, TimeService, Logger, TextureCache
    -   All services created as private fields
    -   SceneContext assembled from services
    -   SceneManager initialized with context via Initialize(context)
    -   Follows SOLID principles (no static coupling)
-   **All 6 Scene Implementations Updated**: Use dependency injection
    -   MainMenuScene, GraphicsDemo, EcsBasicsDemo, PhysicsDemo, InputDemo, TilemapDemo
    -   Fields changed from `readonly` to `null!` (assigned in OnLoad)
    -   Services accessed via `Context.RenderBackend`, `Context.InputBackend`, etc.
    -   SceneManager calls via `Context.SceneManager.PopScene()` instead of `Program.SceneManager.PopScene()`
-   **IRenderBackend → IRenderBackend2D**: Renamed to clarify 2D-only scope
    -   Updated all implementations (RaylibRenderBackend, MockRenderBackend)
    -   Updated all usages across 17 files (scenes, utilities, tests)
    -   Documentation now explicit about 2D rendering focus
    -   Future 3D support will use separate IRenderBackend3D interface
-   **Timing Moved Out of Render Backend**: Clean separation of concerns
    -   Removed `GetFPS()`, `GetDeltaTime()`, `SetTargetFPS()` from IRenderBackend2D
    -   RaylibRenderBackend no longer manages frame timing
    -   Engine core controls time flow, not individual backends
-   **Main Loop Refactored**: Uses TimeService instead of backend timing
    -   `Program.cs` creates TimeService with target 60 FPS
    -   `TimeService.Update()` called at loop start
    -   Delta time from `TimeService.DeltaTime` instead of backend
    -   Cleaner architecture: backend only draws, doesn't manage time
-   **GraphicsDemo UI**: Updated texture filtering controls
    -   F3/F4 now indicate auto-generation of mipmaps
    -   Help text updated: "Trilinear (crisp, auto-generates mipmaps)"
    -   Help text updated: "Anisotropic (best for rotated, auto-mipmaps)"
    -   Manual mipmap generation still available with M key

### Fixed

-   **Anisotropic Texture Filtering**: Now works correctly with proper OpenGL configuration
    -   Previous issue: Anisotropic filter inherited behavior from previous filter
    -   Root cause: Raylib's TextureFilter enum doesn't set GL_TEXTURE_MAX_ANISOTROPY_EXT
    -   Solution: Direct OpenGL calls via P/Invoke to configure anisotropy level
    -   Visual difference now clear between Point, Bilinear, Trilinear, and Anisotropic
-   **Mipmap Filter Reset**: Filter preserved after GenerateMipmaps()
    -   Previous issue: Generating mipmaps would reset texture filter to default
    -   Solution: Re-apply current filter after mipmap generation
    -   Ensures consistent visual quality when switching filters

### Removed

-   **Static Service Locator Antipattern**: Program.\* static properties eliminated
    -   No more `Program.InputBackend`, `Program.RenderBackend`, `Program.SceneManager`, etc.
    -   All dependencies now explicit via SceneContext
    -   Improves testability and follows dependency inversion principle
-   **SceneManager from SceneContext**: Eliminated circular dependency
    -   SceneManager is NO LONGER part of SceneContext
    -   Scenes use `PushScene()`, `PopScene()`, `ReplaceScene()` methods instead
    -   Cleaner architecture: scenes don't directly access SceneManager
-   **Timing Methods from IRenderBackend2D**:
    -   `int GetFPS()` → Use `ITimeService.CurrentFPS`
    -   `float GetDeltaTime()` → Use `ITimeService.DeltaTime`
    -   `void SetTargetFPS(int fps)` → Use `ITimeService.TargetFPS` property
-   **Backend FPS Management**: RaylibRenderBackend no longer calls `SetTargetFPS`

### Technical Details

-   **SceneContext.cs**: 60 lines, 5 service properties + constructor validation (reduced from 6)
-   **Scene.cs**: 165 lines, navigation methods + SceneManager injection + parameter overloads
-   **SceneParameters.cs**: 156 lines, immutable builder pattern, type-safe parameter storage
-   **RaylibTexture.cs**: 165 lines, P/Invoke for OpenGL anisotropic filtering
-   **SlideTransition.cs**: 145 lines, 4 directional slide modes, 10 tests
-   **WipeTransition.cs**: 196 lines, 6 wipe modes including circular effects, 11 tests
-   **ZoomTransition.cs**: 172 lines, 2 zoom modes with vignette, 10 tests
-   **SceneManager.cs**: Two-phase init + SetTransition() + parameter overloads (PushScene/ReplaceScene)
-   **ITimeService.cs**: 73 lines, comprehensive documentation
-   **TimeService.cs**: 127 lines, high-precision Stopwatch-based implementation
-   **IRenderBackend2D**: Renamed from IRenderBackend, 22 methods (3 removed)
-   **Architecture**: Time management decoupled from rendering, dependency injection throughout
-   **Performance**: Stopwatch provides microsecond-accurate timing
-   **Frame Rate Limiting**: Smart sleep with high-precision timing
-   **Delta Time Clamping**: Prevents huge time steps on lag spikes
-   **OpenGL Integration**: Direct GL calls for anisotropic filtering (GL_TEXTURE_MAX_ANISOTROPY_EXT)

### Testing

-   **916 tests passing**: All tests updated with mock SceneContext (77 new tests total)
-   **SceneParametersTests**: 20 tests covering builder pattern, type safety, and immutability
-   **SceneManagerTests**: 6 new tests for parameter passing in Push/Replace scenarios
-   **SlideTransitionTests**: 10 tests covering all 4 directions and lifecycle
-   **WipeTransitionTests**: 11 tests covering all 6 modes including EdgeIn
-   **ZoomTransitionTests**: 10 tests covering both zoom modes and vignette rendering
-   **Manual testing**: Application runs correctly with dependency injection
-   **MSAA verified**: Still working (configured at startup)
-   **Texture filtering verified**: All filters (Point, Bilinear, Trilinear, Anisotropic) working correctly
-   **Anisotropic filtering verified**: Visual difference clear at different zoom levels
-   **Scene transitions verified**: Smooth navigation between demos with all 4 transition effects
-   **Transition selector verified**: F6-F9 keys change transition effect in real-time
-   **Parameter passing verified**: InputDemo receives and displays parameters from MainMenu
-   **Mipmap generation verified**: Auto-generation working for Trilinear and Anisotropic filters

### Architecture Improvements

-   **Explicit Dependencies**: No hidden static coupling, all dependencies visible in OnLoad
-   **Testability**: Scenes can be tested with mock SceneContext
-   **SOLID Compliance**: Dependency inversion, single responsibility
-   **Professional Pattern**: Industry-standard dependency injection
-   **Cleaner Separation**: Backend focuses solely on rendering
-   **Platform Independence**: Time management uses .NET Stopwatch, not backend-specific APIs
-   **Scalability**: Easy to add new services to SceneContext
-   **Future-Ready**: Room for IRenderBackend3D without naming confusion

### Migration Notes

For scenes using the old service locator pattern:

```csharp
// Old (v0.5.1 and earlier) - Static service locator
public class MyScene : Scene
{
    private readonly IInputBackend _inputBackend;

    public MyScene()
    {
        _inputBackend = Program.InputBackend; // Static access
    }

    public override void OnLoad()
    {
        base.OnLoad();
        Program.SceneManager.PushScene(new OtherScene()); // Static access
    }
}

// New (v0.6.0+) - Dependency injection with Scene navigation methods
public class MyScene : Scene
{
    private IInputBackend _inputBackend = null!;

    public MyScene() { } // No constructor assignments

    public override void OnLoad(SceneContext context)
    {
        base.OnLoad(context); // Sets Context property
        _inputBackend = context.InputBackend; // Explicit injection
        PushScene(new OtherScene()); // Use protected Scene method (NO circular dependency)
    }
}
```

For accessing time services:

```csharp
// Old (v0.5.1 and earlier)
var fps = renderBackend.GetFPS();
var deltaTime = renderBackend.GetDeltaTime();
renderBackend.SetTargetFPS(60);

// New (v0.6.0+)
var fps = Context.TimeService.CurrentFPS;
var deltaTime = Context.TimeService.DeltaTime;
Context.TimeService.TargetFPS = 60;
```

**Important**: SceneManager is NOT part of SceneContext (no circular dependency). Use `PushScene()`, `PopScene()`, and `ReplaceScene()` methods from the Scene base class.

## [0.5.1-alpha] - 2025-11-18

### Added

-   **MSAA Support**: Anti-aliasing for smoother rendering (Phase 4 - Graphics improvements)
    -   `AntiAliasingMode` enum with 2 modes: None(0), MSAA4X(4)
    -   `IRenderBackend.AntiAliasing` property for configuration
    -   `RaylibRenderBackend` implementation using ConfigFlags.Msaa4xHint
    -   **Startup configuration only**: Must be set in Program.cs before Initialize()
    -   **Raylib limitation**: ConfigFlags must be set BEFORE InitWindow() call
    -   Read-only status display in GraphicsDemo UI

### Changed

-   **GraphicsDemo UI**: Removed non-functional runtime MSAA controls (F5/F6)
    -   MSAA now displayed as read-only information
    -   Shows "MSAA: 4X (configured at startup)" or "Off"
-   **IRenderBackend.AntiAliasing**: Throws InvalidOperationException if set after initialization

### Removed

-   **Runtime MSAA controls**: F5/F6 key handlers removed (not supported by Raylib at runtime)
-   **SetAntiAliasing() method**: Removed unused method from GraphicsDemo

### Technical Details

-   **AntiAliasingMode.cs**: 22 lines, 2 modes only (simplified from initial 5 modes)
-   **IRenderBackend.AntiAliasing**: Property with validation, prevents runtime changes
-   **Configuration**: Program.cs sets MSAA at line 30, before Initialize() at line 36
-   **Design principle**: Honest UX - only expose features that actually work

### Testing

-   **Manual testing**: Verified MSAA 4X enabled at startup via Raylib logs
-   **Integration**: All texture filtering (F1-F4) and mipmap (M) controls working correctly
-   **Stability**: No crashes, clean startup/shutdown

## [0.5.0-alpha] - 2025-11-18

### Added

-   **Texture Filtering System**: Configurable filter modes for improved visual quality (Phase 4 - Graphics improvements)
    -   `TextureFilterMode` enum with 6 modes:
        -   Point (0): Nearest neighbor, sharp pixels
        -   Bilinear (1): Linear interpolation, smooth scaling
        -   Trilinear (2): Bilinear + mipmap interpolation
        -   Anisotropic4X (3): 4x anisotropic filtering
        -   Anisotropic8X (4): 8x anisotropic filtering
        -   Anisotropic16X (5): 16x anisotropic filtering
    -   `ITexture.Filter` property for runtime filter changes
    -   `RaylibTexture` implementation with Raylib filter mapping
    -   Real-time filter switching in GraphicsDemo (F1-F4 keys)
-   **Mipmap Support**: Automatic and manual mipmap generation
    -   `ITexture.HasMipmaps` property for mipmap detection
    -   `ITexture.GenerateMipmaps()` method for on-demand generation
    -   Auto-detection of preloaded mipmaps (no duplicate generation)
    -   Required for Trilinear and Anisotropic filtering
    -   GraphicsDemo: M key generates mipmaps for all loaded textures
-   **GraphicsDemo Enhancements**:
    -   F1: Point filtering (sharp pixels)
    -   F2: Bilinear filtering (smooth)
    -   F3: Trilinear filtering (requires mipmaps)
    -   F4: Anisotropic 16X filtering (highest quality)
    -   M: Generate mipmaps for all textures
    -   Visual feedback showing current filter mode and mipmap status
    -   Camera zoom controls for testing filtering at different scales
-   **ECS Query Caching**: Significant performance optimization
    -   `CachedQuery<T>` class for automatic query result caching
    -   Automatic invalidation on component add/remove operations
    -   Reduces redundant entity filtering in systems
    -   94% performance improvement in PhysicsSystem (verified via benchmarks)
-   **Scene Transition Effects**: Fade in/out transitions
    -   `ISceneTransition` interface for extensible transition system
    -   `FadeTransition` implementation with configurable duration and color
    -   Async transition support with proper lifecycle management
    -   18 comprehensive transition tests (all passing)

### Changed

-   **GraphicsDemo**: Increased sprite spawn radius from 200px to 500px
    -   Better distribution for testing filtering at distance
    -   Removed debug rectangles for cleaner visuals
-   **ITexture interface**: Extended with Filter, HasMipmaps, GenerateMipmaps()
-   **RaylibTexture**: Implemented texture filtering and mipmap support
-   **MockTexture**: Updated with filter/mipmap properties for testing

### Fixed

-   **Sprite rendering bug**: Fixed critical switch expression issue in RaylibRenderBackend.DrawSprite()
    -   Incorrect texture application due to missing break/return in filter switch
    -   Now correctly applies texture filtering before rendering
-   **Debug code cleanup**: Removed Console.WriteLine statements from production code
-   **Mipmap warnings**: Trilinear/Anisotropic modes now check for mipmaps and warn if missing

### Technical Details

-   **TextureFilterMode.cs**: 47 lines with comprehensive documentation
-   **ITexture filter support**: 3 new members (Filter property, HasMipmaps, GenerateMipmaps)
-   **RaylibTexture**: Full Raylib filter mode mapping (Point↔Nearest, Bilinear↔Linear, etc.)
-   **CachedQuery**: Generic caching system with automatic invalidation
-   **FadeTransition**: Async/await pattern with configurable parameters
-   **No auto-generation**: Mipmaps generated only on explicit request (design decision)

### Testing

-   **789 tests passing**: All existing tests remain green
-   **18 new transition tests**: Complete FadeTransition coverage
-   **Manual testing**: All 6 filter modes verified visually in GraphicsDemo
-   **Mipmap testing**: Auto-detection and manual generation both functional
-   **Integration testing**: Filter changes work seamlessly with sprite rendering

### Performance

-   **Query caching**: 94% improvement in PhysicsSystem with 100+ entities
-   **Texture filtering**: Minimal performance impact, significant visual quality gain
-   **Mipmaps**: Improved texture sampling performance for scaled sprites

## [0.4.9-alpha] - 2025-11-18

### Added

-   **SceneManager Stack-Based System**: Complete scene lifecycle management (Phase 4 start)
    -   `PushScene(scene)`: Push new scene onto stack
    -   `PopScene()`: Return to previous scene (guards against popping last scene)
    -   `ReplaceScene(scene)`: Swap current scene with new one
    -   Scene stack with `CurrentScene` and `SceneCount` properties
    -   Deferred transition processing (prevents mid-update state corruption)
    -   Automatic scene lifecycle (`OnLoad`/`OnUnload`) management
    -   Clean shutdown (unloads all scenes in stack)
-   **MainMenuScene**: Central navigation hub with clean menu UI
-   **Interactive EcsBasicsDemo**: Playable demonstration of EntityBuilder/EntityFactory
    -   Player movement (WASD/Arrows, 200px/s with diagonal normalization)
    -   Enemy AI (horizontal patrol, 50px/s with edge detection)
    -   Collectible animations (sine wave bounce effect)
    -   Collection system (30px proximity detection, 6/6 tracking)
    -   Visual feedback (labels, trail effect, instant destruction feedback)
    -   Reset functionality (R key) with proper cleanup
-   **Demo Placeholders**: Graphics, Physics, Input, and Tilemap demos (awaiting assets)
-   **ESC Key Control**: Application-controlled ESC behavior (disabled Raylib default)

### Changed

-   **Demo Refactoring**: Complete restructure of demo scenes for better organization
-   **Scene Navigation**: Stack-based navigation with Push/Pop (ESC returns to previous scene)
-   **Program.cs**: Static backend properties for scene access to Input/Render/Logger
-   **All Scenes**: Refactored to parameterless constructors using `Program` statics
-   **Entity Destruction**: Immediate visual feedback via `_entitiesPendingDestruction` tracking
-   **World.IsEntityValid()**: Filters destroyed entities from queries and rendering

### Removed

-   **Obsolete Demos**: Deleted 8 legacy demo scenes with outdated code patterns
    -   `CameraDemoScene`, `ComponentHelpersDemoScene`, `DemoScene`, `EcsDemoScene`
    -   `InputMapDemoScene`, `ResourceDemoScene`, `SpriteBatchDemoScene`, `VisualDemoScene`
-   **Manual Entity Creation**: Replaced 42 manual `AddComponent` calls with builders
-   **Program.RequestedScene**: Temporary solution replaced by SceneManager stack

### Fixed

-   **Black Screen on Launch**: ProcessPendingTransitions moved to Update() (from FixedUpdate)
-   **ESC Closes Window**: SetExitKey(KeyboardKey.Null) in RaylibRenderBackend
-   **Reset Duplication**: World.Update(0f) forces cleanup before creating new entities
-   **Collectibles Not Disappearing**: IsEntityValid() now checks pending destruction

### Testing

-   **14 New SceneManager Tests**: Complete coverage of stack-based API
    -   Stack operations (Push/Pop/Replace with multiple scenes)
    -   Lifecycle management (OnLoad/OnUnload call verification)
    -   Edge cases (pop last scene, empty stack, null arguments)
    -   Transition processing and rendering delegation
    -   Shutdown cleanup (all scenes unloaded)
-   **Test Results**: 350/352 passing (2 skipped FileSystemWatcher tests)

### Technical Details

-   **SceneManager Architecture**: 235 lines, stack-based with deferred transitions
-   **EcsBasicsDemo**: 413 lines, fully interactive with 15 entities
    -   1 Player (blue 16px), 4 Enemies (red 14px), 6 Collectibles (yellow 8px), 4 Obstacles (gray 25px)
-   **Navigation**: Keyboard-based (1-5 for demos, ESC returns to menu/previous scene)
-   **Code Quality**: All tests passing, no build warnings (except 3 IDE suggestions)
-   **Phase 4 Progress**: SceneManager basic complete (transitions deferred to v0.5.0)

### Developer Experience

-   **Single Command**: `dotnet run` launches unified demo showcase
-   **Smooth Navigation**: Push/Pop scene stack with clean lifecycle management
-   **Extensibility**: Adding demos only requires scene creation + menu entry
-   **Debugging**: ESC key controlled by application (no unexpected window close)

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

# MicroEngine — Development Roadmap

**Version:** 3.0
**Status:** Reference  
**Author:** Kevin Martínez  
**Last Updated:** November 2025

---

## Overview

This roadmap outlines the planned evolution of MicroEngine from early prototype to a stable,
production-ready engine with optional future 3D capabilities.

Milestones are grouped into phases to reflect realistic engine development workflows and align
with the architecture principles defined in the [Architecture document](ARCHITECTURE.md).

**Related Documents:**

-   📘 [Architecture](ARCHITECTURE.md) — Engine structure and design
-   📘 [Core Requirements](CORE_REQUIREMENTS.md) — Mandatory technical rules
-   📘 [Engine Design Document](ENGINE_DESIGN_DOCUMENT.md) — Vision and goals
-   📘 [Versioning](VERSIONING.md) — Release and versioning strategy

---

## Table of Contents

1. [Phase 0 — Foundations (0.1.x)](#phase-0--foundations-01x)
2. [Phase 1 — Minimum Engine Functionality (0.2.x)](#phase-1--minimum-engine-functionality-02x)
3. [Phase 2 — Backend Implementations (0.3.x)](#phase-2--backend-implementations-03x)
4. [Phase 3 — Engine Usability & Tools (0.4.x)](#phase-3--engine-usability--tools-04x)
5. [Phase 4 — Stability, Optimization & API Freeze (0.5.x → 1.0.0)](#phase-4--stability-optimization--api-freeze-05x--100)
6. [Phase 5 — Ecosystem Expansion (1.1.x → 1.5.x)](#phase-5--ecosystem-expansion-11x--15x)
7. [Phase 6 — Optional 3D Foundations (2.x Roadmap)](#phase-6--optional-3d-foundations-2x-roadmap)
8. [Phase 7 — Advanced Features (3.x+)](#phase-7--advanced-features-3x)
9. [Summary of Milestones](#summary-of-milestones)
10. [Development Philosophy](#development-philosophy)
11. [Acceptance Criteria](#acceptance-criteria)
12. [Risk Management](#risk-management)

---

## Phase 0 — Foundations (0.1.x)

**Goal:** Establish the architectural foundation of the engine.

**Status:** ✅ Complete

### Features

-   ✅ Project structure created (`MicroEngine.Core`, `MicroEngine.Backend.*`, `MicroEngine.Game`)
-   ✅ Dimension-agnostic architecture defined
-   ✅ Engine/Game separation implemented
-   ✅ Basic update loop (fixed timestep + variable render rate)
-   ✅ Basic Scene system with transitions
-   ✅ Structured logging system (ILogger interface, no `Console.WriteLine`)
-   ✅ Basic math primitives (Vector2, Vector3, Matrix types, Rectangle, Color)
-   ✅ Configuration management (external config, no hardcoded values)
-   ✅ Basic error handling infrastructure

### Testing Requirements

-   ✅ Unit tests for core math primitives (Vector2, Vector3, etc.)
-   ✅ Update loop determinism tests
-   ✅ Scene transition validation tests

### Documentation

-   ✅ Architecture document finalized
-   ✅ Core requirements document created
-   ✅ API documentation for public interfaces
-   ✅ Copilot instructions for development standards

### Deliverable

**v0.1.0** — "Engine Skeleton"

### Acceptance Criteria

-   ✅ Engine runs without game-specific dependencies
-   ✅ Update loop maintains fixed timestep
-   ✅ Scene transitions work without memory leaks
-   ✅ All core modules are testable in isolation

---

## Phase 1 — Minimum Engine Functionality (0.2.x)

**Goal:** Build the first functional engine capable of running simple 2D demos.

**Status:** ✅ Complete

### Features

-   ✅ ECS implementation (World, Entity, ComponentArray, Systems)
-   ✅ Transform component (2D implementation, 3D-ready structure)
-   ✅ Core ECS components (TransformComponent, RigidBodyComponent, ColliderComponent, SpriteComponent, AnimatorComponent)
-   ✅ ECS Systems (PhysicsSystem, MovementSystem, CollisionSystem)
-   ✅ Input backend interface (`IInputBackend`)
-   ✅ Render backend interface (`IRenderBackend`) (dimension-agnostic)
-   ✅ Basic physics (AABB collision, circle collision, overlap tests, bounds calculation)
-   ✅ Timing utilities (delta time, frame counting)
-   ✅ Resource management system (`IResource`, `ResourceId`, `ResourceCache<T>`, `IResourceLoader<T>`)
-   ✅ Resource types defined (`ITexture`, `IFont`, `IAudioClip`)
-   ✅ Audio backend interface (`IAudioBackend`)
-   ✅ Event system for component communication (`EventBus`, 16 tests)

### Testing Requirements

-   ✅ Internal test scenes (ComponentHelpersDemoScene, CameraDemoScene, SpriteBatchDemoScene, VisualDemoScene)
-   ✅ Debug overlays (FPS, entities count, player position)
-   ✅ ECS system benchmarks (1000+ entities tested)
-   ✅ Physics collision tests (ground, obstacles, triggers)
-   ✅ Resource system unit tests (ResourceCache, ResourceId)
-   ✅ Performance profiling (PERFORMANCE_PROFILING.md guide with frame timing, memory tracking, optimization techniques)

### Documentation

-   ✅ ECS module documentation (inline XML comments)
-   ✅ Backend interface specifications
-   ✅ Resource system documentation (XML comments)

### Deliverable

**v0.2.0** — "First Playable Core"

### Acceptance Criteria

-   ✅ ECS can manage 1000+ entities at 60 FPS
-   ✅ Transform hierarchy works correctly
-   ✅ Physics detects basic collisions (Rectangle-Rectangle, Circle-Circle, Point)
-   ✅ All backend interfaces defined and documented
-   ✅ Resource system functional with reference counting

---

## Phase 2 — Backend Implementations (0.3.x)

**Goal:** Make the engine usable with at least one fully functional backend.

**Status:** ✅ Complete

### Features

#### Raylib Backend Implementation

-   ✅ Rendering (rectangles, text, basic shapes)
-   ✅ Input handling (keyboard, mouse, gamepad)
-   ✅ Window management (initialization, frame control, shutdown)
-   ✅ Texture loading and rendering (`RaylibTexture`, `RaylibTextureLoader`)
-   ✅ Font loading and text rendering (`RaylibFont`, `RaylibFontLoader`)
-   ✅ Audio clip loading (`RaylibAudioClip`, `RaylibAudioClipLoader`)
-   ✅ Audio playback system (`RaylibAudioBackend` - sound effects and music)
-   ✅ Sprite rendering with textures (`DrawTexture`, `DrawTexturePro`)
-   ✅ Sprite batching (`SpriteBatch` with sorting modes)
-   ✅ Gamepad support (buttons, axes, availability detection)

#### Resource System

-   ✅ Resource architecture (`IResource`, `ResourceId`, `ResourceCache<T>`)
-   ✅ Resource loader interface (`IResourceLoader<T>`)
-   ✅ Resource types:
    -   ✅ Textures (PNG, JPG support via Raylib)
    -   ✅ Fonts (TTF support via Raylib)
    -   ✅ Audio clips (WAV, OGG support via Raylib)
-   ✅ Reference counting and automatic unloading
-   ✅ Path normalization and caching
-   ✅ Error handling for missing files
-   ✅ Demo scene (`ResourceDemoScene`) showing resource loading
-   ✅ Resource metadata and validation (`ResourceMetadata`, `ResourceValidator`, 17 tests)
-   ✅ Asset hot-reloading (`HotReloadableResourceCache`, `FileSystemResourceWatcher`, 8 tests)

### Testing Requirements

-   ✅ Create Raylib-based demo games (multiple scenes working)
-   ✅ Stress-test scene transitions and memory
-   ✅ Resource cache unit tests (reference counting, loading/unloading)
-   ✅ Resource loading failure handling tests (11 tests)
-   ✅ Memory leak testing for resources (10 tests)

### Documentation

-   ✅ Raylib backend implementation (XML comments)
-   ✅ Resource system documentation (XML comments)
-   ✅ Resource usage guide and best practices (RESOURCE_USAGE_GUIDE.md)

### Deliverable

**v0.3.0** — "First Rendered Game"

### Acceptance Criteria

-   ✅ Raylib backend fully functional (rendering, input, audio, resources)
-   ✅ Demo game runs at stable 60 FPS
-   ✅ Resources load with proper reference counting
-   ✅ Input handling responds correctly
-   ✅ Texture-based sprite rendering working
-   ✅ Audio playback functional (sound effects and music)

---

## Phase 3 — Engine Usability & Tools (0.4.x)

**Goal:** Make the engine ergonomic for game developers.

**Status:** ✅ COMPLETE (9/9 features, 1 deferred)

**Current Version:** v0.4.9-alpha

### Features

-   ✅ Sprite atlases & sprite batching (v0.4.2)
-   ✅ Camera system (2D version, 3D-ready) (v0.4.1)
-   ✅ Input mapping (action → key abstraction) (v0.4.3)
-   ✅ Component helpers & factory patterns (v0.4.6)
-   ✅ Event system with EventBus (v0.4.5)
-   ✅ Asset pipeline improvements (metadata, validation) (v0.4.5)
-   ✅ Hot-reload of resources (v0.4.5)
-   ✅ Built-in debugging tools (performance profiling guide complete)
-   ✅ Collision layer system (v0.4.7)
-   ✅ Tilemap support (v0.4.8)
-   ✅ Demo refactoring & SceneManager basic (v0.4.9)

### Testing Requirements

-   ✅ Playable sample game (platformer or similar) — ComponentHelpersDemoScene demonstrates features
-   ✅ API refinement based on usage feedback — Completed through TDD iteration
-   ✅ Performance benchmarks for sprite batching — SpriteBatchDemoScene with 1000+ sprites
-   ✅ Input mapping tests — 23 tests passing

### Documentation

-   ✅ Developer guide with examples — RESOURCE_USAGE_GUIDE.md, PERFORMANCE_PROFILING.md
-   ✅ Sample game walkthrough — Multiple demo scenes with interactive controls
-   ⚠️ Best practices guide — Partially complete (needs entity factory guide)

### Deliverable

**v0.4.6-alpha** — "Developer Comfort Update" (RELEASED)

### Acceptance Criteria

-   ✅ Sample game demonstrates all core features
-   ✅ Developer feedback incorporated
-   ✅ Sprite batching improves draw call performance
-   ✅ Camera system works smoothly
-   ✅ Entity creation simplified with helpers (70% code reduction)
-   ✅ Event-driven architecture enabled
-   ✅ Resource hot-reload functional

### Remaining Work

-   Entity factory usage guide (documentation)
-   Refactor demo scenes to use EntityBuilder/EntityFactory (optional)
-   Tilemap demo scene (optional)

**Deferred to Phase 5+:**

-   Entity inspector UI (requires editor infrastructure - see Phase 7)

**Phase 3 Complete:** All core features implemented (9/9 features ✅)

---

## Phase 4 — Stability, Optimization & API Freeze (0.5.x → 1.0.0)

**Goal:** Prepare the engine for production use and stabilize the API.

**Status:** IN PROGRESS (15/16 features complete)

**Current Version:** v0.12.0-alpha

### Features

-   ✅ **SceneManager system** — COMPLETE v0.6.0
    -   ✅ Scene stack for back navigation (Push/Pop/Replace)
    -   ✅ Proper scene lifecycle management (OnLoad/OnUnload)
    -   ✅ Deferred transition processing (prevents mid-update corruption)
    -   ✅ Scene transitions with effects (fade in/out) — v0.5.0
    -   ✅ **Additional transition effects** (slide, wipe, zoom) — COMPLETE v0.6.0
        -   SlideTransition: 4 directions (Left, Right, Up, Down)
        -   WipeTransition: 6 modes (LeftToRight, RightToLeft, TopToBottom, BottomToTop, CenterOut, EdgeIn)
        -   ZoomTransition: 2 modes (ZoomIn, ZoomOut) with vignette effect
        -   SceneManager.SetTransition() for runtime transition changes
        -   MainMenuScene transition selector (F6-F9)
    -   ✅ **Scene parameter passing** (data transfer between scenes) — COMPLETE v0.6.0
        -   SceneParameters: Immutable, type-safe builder pattern
        -   Get<T>/TryGet<T>/Contains for safe parameter retrieval
        -   PushScene(scene, parameters) and ReplaceScene(scene, parameters) overloads
        -   OnLoad(context, parameters) overload in Scene base class
        -   Backward compatible with existing parameter-less code
        -   Demo: InputDemo receives parameters from MainMenu
    -   ✅ **Dependency injection architecture** — v0.6.0
        -   SceneContext service container (6 core services)
        -   No circular dependencies (removed SceneManager from SceneContext)
        -   Scene navigation methods (PushScene/PopScene/ReplaceScene)
        -   Hollywood Principle pattern (SetSceneManager internal injection)
    -   ✅ **Scene caching and lazy loading** — COMPLETE v0.7.0
        -   ISceneCache interface with GetOrCreate, Preload, TryGet operations
        -   SceneCache implementation with LRU eviction policy
        -   Thread-safe ConcurrentDictionary backing
        -   Configurable cache size (default 10 scenes)
        -   Automatic OnUnload() calls on evicted scenes
        -   Integrated in MainMenuScene for demo navigation
        -   Visual cache status display (hits/misses, stored count)
        -   30 comprehensive tests including concurrency
    -   ✅ **Scene preloading and background loading** — COMPLETE v0.7.0
        -   PreloadAsync<T> for single scene background loading
        -   PreloadMultipleAsync for parallel batch preloading
        -   CancellationToken support for preload operations
        -   ScenePreloaded event for completion tracking
        -   IsPreloading status checking
        -   MainMenuScene preloads all 5 demo scenes on startup
        -   14 comprehensive async tests
        -   Zero-latency scene transitions (instant cache hits)
-   ✅ **Global State Management** — COMPLETE v0.7.0
    -   ✅ IGameState interface for persistent data across scenes
    -   ✅ GameState implementation with thread-safe ConcurrentDictionary
    -   ✅ Type-safe API: `Get<T>`, `TryGet<T>`, `Set<T>`, Contains, Remove, Clear, GetKeys
    -   ✅ Integrated into SceneContext as optional service
    -   ✅ Use cases: player progress, settings, high scores, session data
    -   ✅ 21 comprehensive tests
-   ✅ **State Machine Framework** — COMPLETE v0.7.0
    -   ✅ Generic `StateMachine<TState, TTrigger>` for finite state machines
    -   ✅ Guarded transitions with PermitIf
    -   ✅ Entry and exit actions for state lifecycle management
    -   ✅ Fluent configuration API
    -   ✅ Fire, FireStrict, CanFire, Reset operations
    -   ✅ Use cases: AI behaviors, UI flows, game states, player controllers
    -   ✅ 23 comprehensive tests
-   ✅ **GameEngine Integration** — COMPLETE v0.7.0
    -   ✅ Refactored GameEngine to accept IRenderBackend2D and IInputBackend
    -   ✅ SceneContext initialization support
    -   ✅ Fixed timestep accumulator (60 Hz) for deterministic physics
    -   ✅ Variable render rate (uncapped or V-sync)
    -   ✅ Spiral of death prevention (max 5 fixed updates per frame)
    -   ✅ Input/Update/Render separation in ProcessFrame
    -   ✅ Program.cs migrated from manual loop to GameEngine.Run()
    -   ✅ Frame-rate independent gameplay guaranteed
-   ✅ **Graphics rendering improvements** — COMPLETE v0.5.0
    -   ✅ **Texture filtering** (Point, Bilinear, Trilinear, Anisotropic16X)
    -   ✅ Configurable texture filter modes via ITexture.Filter property
    -   ✅ Real-time filter switching in GraphicsDemo (F1-F4 keys)
    -   ✅ **Mipmap support** with auto-detection and manual generation (M key in GraphicsDemo)
    -   ✅ **MSAA support** (startup configuration only, 4X mode) — COMPLETE v0.5.1
        -   IRenderBackend.AntiAliasing property (None/MSAA4X)
        -   RaylibRenderBackend implementation with ConfigFlags.Msaa4xHint
        -   Note: Must be configured before Initialize() due to Raylib limitations
    -   ✅ **TextLayoutHelper** (automatic text positioning) — COMPLETE v0.11.0
        -   Fluent API for text rendering without manual coordinate tracking
        -   Automatic Y-coordinate advancement with customizable line heights
        -   Methods: DrawText, AddSpacing, SetX/SetY, DrawSection, DrawKeyValue, NewColumn
        -   Integrated in all 8 scenes (7 demos + MainMenu)
        -   18 comprehensive unit tests
        -   30% average reduction in UI rendering code
-   ✅ **ECS optimizations** — COMPLETE v0.7.2
    -   ✅ Query caching (CachedQuery class with automatic invalidation) — COMPLETE v0.5.0
    -   ✅ **Archetype optimization** — COMPLETE v0.7.2
        -   ✅ ArchetypeId: Component composition hashing for archetype identification
        -   ✅ Archetype: Groups entities with identical component sets (reuses IComponentArray)
        -   ✅ ArchetypeManager: Entity-archetype mapping and lifecycle management
        -   ✅ AddBoxed method in IComponentArray for type-safe boxed component storage
        -   ✅ Query optimization: CachedQuery iterates matching archetypes (not all entities)
        -   ✅ Automatic archetype updates on component add/remove
        -   ✅ 14 comprehensive archetype tests (composition, queries, lifecycle)
        -   ✅ Simple implementation: No reflection, no dynamic types, ~30 minutes work
        -   ✅ Expected 3-10x performance improvement for large entity counts
-   ✅ **Performance Benchmarking Suite** — COMPLETE v0.7.3
    -   ✅ BenchmarkDotNet integration with comprehensive ECS benchmarks
    -   ✅ ECS Query benchmarks: 1-4 component queries, 100-10,000 entities
    -   ✅ Entity lifecycle benchmarks: Creation and destruction with 0-3 components
    -   ✅ Component operation benchmarks: Access, modification, system simulation
    -   ✅ Validated archetype optimization: O(n) linear scaling, zero allocations
    -   ✅ Results: 37,000 queries/sec (10k entities), 424,000 queries/sec (100 entities)
    -   ✅ Complete documentation with usage guide and interpretation
-   ✅ **Structured Error Codes & Exception Hierarchy** — COMPLETE v0.7.4
    -   ✅ MicroEngineException: Base exception with error codes (ME-xxx)
    -   ✅ EcsException: ECS-specific exceptions (ECS-xxx)
        -   EntityNotFoundException (ECS-404)
        -   ComponentNotFoundException (ECS-410)
        -   DuplicateComponentException (ECS-409)
        -   InvalidEntityOperationException (ECS-400)
        -   WorldException (ECS-500)
    -   ✅ SceneException: Scene system exceptions (SCN-xxx)
        -   SceneNotFoundException (SCN-404)
        -   SceneTransitionException (SCN-500)
        -   SceneParameterException (SCN-422)
    -   ✅ ResourceException: Resource management exceptions (RES-xxx)
        -   ResourceNotFoundException (RES-404)
        -   ResourceLoadException (RES-500)
    -   ✅ StateException: State machine exceptions (STATE-xxx)
        -   InvalidStateException (STATE-404)
        -   InvalidTransitionException (STATE-400)
    -   ✅ Context preservation with entity IDs, component types, scene names
    -   ✅ ADR-002 documenting exception design decisions
    -   ✅ 22 comprehensive exception tests
-   ✅ **Memory Profiling Tools** — COMPLETE v0.7.5
    -   ✅ MemorySnapshot: GC statistics and custom metrics tracking
    -   ✅ MemoryProfiler: Time-series memory tracking with leak detection
    -   ✅ EcsMemoryProfiler: ECS-specific memory profiling and reporting
    -   ✅ ProfilingException hierarchy (PROF-xxx error codes)
    -   ✅ GameEngine integration with automatic profiling when enabled
    -   ✅ 43 comprehensive profiling tests
-   ✅ **Improved Physics Accuracy** — COMPLETE v0.8.0
    -   ✅ Continuous Collision Detection (CCD) to prevent tunneling
        -   CollisionInfo struct with normal, penetration, contact point, time of impact
        -   SweptCollision class with swept AABB collision detection
        -   Ray vs AABB intersection testing for precise collision times
        -   Time of impact calculation (0-1 range) for interpolated collisions
    -   ✅ Collision resolution system
        -   Position adjustment to prevent penetration
        -   Velocity reflection based on collision normal
        -   Restitution (bounciness) coefficient support
    -   ✅ Enhanced RigidBodyComponent with Restitution and UseContinuousCollision
    -   ✅ RenderComponent for basic visual rendering (Rectangle, Circle, Line)
    -   ✅ PhysicsDemo: Functional demonstration with falling balls and ground collisions
-   ✅ **Savegame System** — COMPLETE v0.8.0
    -   ✅ SaveMetadata: Versioning, timestamps, and custom metadata
        -   Semantic versioning for save format compatibility (default: 1.0.0)
        -   Engine version tracking for backward compatibility checks
        -   Creation and modification timestamps (UTC)
        -   Optional user-defined save names
        -   Custom key-value metadata dictionary
    -   ✅ SaveContainer<T>: Generic save file wrapper with metadata
    -   ✅ ISaveSerializer: Abstraction for serialization format
    -   ✅ JsonSaveSerializer: JSON-based serializer with System.Text.Json
        -   Configurable indentation for human-readable saves
        -   Camelcase naming convention
        -   Null value omission for smaller files
    -   ✅ ISavegameManager: Interface for save operations
    -   ✅ SavegameManager: Complete implementation
        -   Configurable save directory (default: ./Saves)
        -   Automatic .sav file extension handling
        -   Creation timestamp preservation on overwrite
        -   Last modified timestamp updates
        -   List all saves, lightweight metadata loading
        -   Type-safe generic save/load with result types
        -   Comprehensive error handling (SaveResult, LoadResult<T>)
    -   ✅ 10 comprehensive savegame tests
-   ✅ **Physics Backend Abstraction** — COMPLETE v0.9.0
    -   ✅ IPhysicsBackend interface (backend-agnostic physics API)
        -   World management (Initialize, Shutdown, Step)
        -   Gravity control (SetGravity, GetGravity)
        -   Body lifecycle (CreateBody, DestroyBody, SetBodyType)
        -   Collider attachment (AddCircleCollider, AddBoxCollider)
        -   Position and velocity manipulation
        -   Force and impulse application
        -   Gravity and damping configuration
    -   ✅ MicroEngine.Backend.Aether project
        -   AetherPhysicsBackend implementation (nkast.Aether.Physics2D v2.0.0)
        -   Pixels-to-meters conversion (100 pixels = 1 meter)
        -   Body type support (Static, Kinematic, Dynamic)
        -   Realistic rigid body dynamics and collision resolution
        -   Integration with MonoGame.Framework for Aether.Physics2D compatibility
    -   ✅ PhysicsBackendSystem for ECS integration
        -   PhysicsBodyComponent: Links ECS entities to physics bodies
        -   Bidirectional synchronization (ECS ↔ Physics)
        -   Helper methods: CreateBodyForEntity, DestroyBodyForEntity
        -   Physics operations: ApplyForce, ApplyImpulse, Stop
    -   ✅ PhysicsDemo migrated to use Aether backend
    -   ✅ Error codes follow [MODULE]-[NUMBER] format (e.g., "ECS-404", "RES-500")
    -   ✅ Context management with `WithContext(key, value)` for structured logging
    -   ✅ Enhanced diagnostics with error code, context, and inner exception details
    -   ✅ 28 comprehensive tests covering all exception types
-   ✅ **Memory Profiling Tools** — COMPLETE v0.7.5
    -   ✅ `MemorySnapshot`: Immutable snapshots with GC statistics and custom metrics
    -   ✅ `MemoryProfiler`: Time-series tracking with leak detection and CSV export
    -   ✅ Leak detection: Monotonic increase analysis (10 sample window, 5 MB threshold)
    -   ✅ Baseline tracking for delta calculations and memory regression detection
    -   ✅ `EcsMemoryProfiler`: ECS-specific tracking with entity/component estimation
    -   ✅ Statistical analysis: min/max/avg memory, generation-specific GC collections
    -   ✅ Async monitoring support with cancellable time periods
    -   ✅ 36 comprehensive tests covering all profiling components
-   ⏳ Stable public API surface (breaking changes frozen) — PLANNED v1.0
-   ⏳ Comprehensive documentation of all public APIs — PLANNED v1.0
-   ⏳ Improved physics accuracy (continuous collision detection) — PLANNED
-   ⏳ Determinism audit across all modules — PLANNED
-   ⏳ Savegame system (versioned, backward-compatible) — PLANNED

### Testing Requirements

-   Full test coverage for public APIs
-   Stress tests (10,000+ entities)
-   Savegame compatibility tests
-   Long-running stability tests (24+ hours)

### Documentation

-   Complete API reference documentation
-   Performance optimization guide

### Deliverable

**v1.0.0** — "Stable API Release"

### Acceptance Criteria

-   ✅ No breaking API changes after this version
-   ✅ All public APIs documented
-   ✅ Savegame system working reliably
-   ✅ Performance targets met (60 FPS with 5000+ entities)
-   ✅ Zero critical bugs in issue tracker

---

## v0.13.0 — Capability Demonstrations & Polish

**Goal:** Demonstrate hidden capabilities and fill implementation gaps.

**Status:** ✅ Complete (November 2025)

### Features

-   **Save/Load Demo** (`SaveLoadDemo`)
    -   Demonstrate `ISavegameManager` usage
    -   Save entity positions and restore on restart
    -   Show metadata handling
-   **Event System Demo**
    -   Integrate `EventBus` into `EcsBasicsDemo`
    -   Demonstrate decoupled communication (e.g., "PlayerDead", "ItemCollected")
    -   Remove direct system coupling
-   **Physics Collision Filtering**
    -   Update `PhysicsBackendSystem` to use `CollisionMatrix`
    -   Extend `PhysicsDemo` to show layer filtering (e.g., Player vs Enemy, Enemy vs Enemy ignored)
-   **Spatial Audio Verification**
    -   Verify/Implement distance attenuation in audio backend
    -   Update `AudioDemo` to demonstrate sound variation with distance
-   **Missing Core Features Analysis**
    -   Document requirements for UI System, Animation System, and Particle System

### Deliverable

**v0.13.0** — "Capabilities Showcase"

### Acceptance Criteria

-   ✅ Save/Load works reliably in demo
-   ✅ Event system used effectively in at least one demo
-   ✅ Collision filtering demonstrated visually
-   ✅ Spatial audio attenuation verified

---

## Phase 5 — Ecosystem Expansion (1.1.x → 1.5.x)

**Goal:** Build complementary tools and improve developer experience.

**Status:** Future

### Features

-   **Asset management system**
    -   Asset browser and discovery system
    -   Asset validation and metadata generation
    -   Asset organization conventions and folder structure
    -   Asset dependency tracking
    -   Recommended asset packs integration (Kenney.nl, OpenGameArt)
    -   Asset licensing guide and compliance tools
-   Asset importer CLI (automated asset conversion)
-   **Scene template system**
    -   Base scene classes with common functionality (`BaseDemoScene`, `BaseGameScene`)
    -   Scene templates for common patterns (menu, gameplay, loading)
    -   Scene bootstrapping helpers and factory methods
-   **Scene serialization/deserialization**
    -   Scene file format (JSON/YAML)
    -   Scene loading and saving system
    -   Entity prefab system for reusable templates
    -   Scene validation and error reporting
-   **In-game developer console**
    -   Console overlay with command parsing
    -   Runtime variable inspection and modification
    -   Command history and autocomplete
    -   Custom command registration API
    -   Debug command library (spawn entities, toggle systems, etc.)
-   Project templates (boilerplate generators)
-   Visual debugging tools (entity visualizer, physics debug renderer)
-   Input recorder/playback for testing
-   Optional scripting layer (C# scripting or Lua integration)
-   Content pipeline enhancements (compressed textures, asset metadata)
-   Scene graph (hierarchical transforms with optimized updates)
-   Animation system (sprite animation, tweening)
-   Particle system (2D particle effects)
-   UI framework (basic GUI widgets)

### Testing Requirements

-   CLI tool integration tests
-   Scripting layer sandbox tests
-   Animation system validation

### Documentation

-   CLI tool documentation
-   Scripting API reference
-   Animation system guide
-   UI framework tutorial

### Deliverables

-   **v1.1.0** — "Scene Management & Templates"
-   **v1.2.0** — "Asset Management & Developer Tools"
-   **v1.3.0** — "Scene Serialization & Content Pipeline"
-   **v1.5.0** — "Full Ecosystem Maturity"

### Acceptance Criteria

-   ✅ SceneManager handles scene transitions smoothly (replaces temporary solution)
-   ✅ Scene templates reduce boilerplate by 50%+
-   ✅ Asset management system validates and organizes assets automatically
-   ✅ Developer console allows runtime debugging without recompilation
-   ✅ Scene serialization supports save/load without data loss
-   ✅ Project templates generate working projects
-   ✅ Asset pipeline supports common formats
-   ✅ Scripting layer is sandboxed and safe
-   ✅ UI framework supports common widgets

---

## Phase 6 — Optional 3D Foundations (2.x Roadmap)

**Goal:** Extend the engine to support 3D rendering, physics, and workflows.

**Status:** Future

> **Note:** These features are long-term goals.  
> The engine is architected to support them without architectural breakage.

### Future Features (3D)

-   3D transforms & cameras (perspective projection)
-   Mesh loading (OBJ, GLTF, FBX support)
-   Material & shader pipeline (PBR support)
-   3D physics backend (via plugin, e.g., Bullet, PhysX wrapper)
-   Scene graph optimized for spatial data (octree, BVH)
-   Lighting models (directional, point, spot lights)
-   Render batching and culling (frustum culling, occlusion)
-   GPU abstraction layer for custom pipelines
-   Skeletal animation system
-   Shadow mapping

### Deliverable

**v2.0.0** — "MicroEngine 3D Foundations"

### Acceptance Criteria

-   ✅ 3D rendering works without breaking 2D functionality
-   ✅ Performance targets met (60 FPS with 1000+ 3D objects)
-   ✅ 3D physics integrated successfully
-   ✅ Existing 2D games continue to work

---

## Phase 7 — Advanced Features (3.x+)

**Goal:** Expand into advanced tooling and new domains.

**Status:** Exploratory

### Phase 5 — Editor & Tooling Infrastructure

**Prerequisites:** Requires dedicated editor project (`MicroEngine.Editor`) with UI framework integration.

-   **Entity Inspector** (runtime entity debugging - **deferred from Phase 3**)
    -   Visual entity browser with component inspection
    -   Runtime value editing for debugging
    -   Hierarchy view for entity relationships
    -   Performance overlay with real-time metrics
    -   Requires: ImGui.NET or Avalonia UI integration
-   Node-based visual editor (graph-based scene editing)
-   Integrated engine IDE (full development environment)
-   In-editor hot-reload (edit code while running)
-   Behavior tree editor (AI scripting tool)
-   Network module for multiplayer games (client/server architecture)
-   VR/AR experimental backends (OpenXR integration)
-   GPU compute pipeline (GPGPU support)
-   Procedural generation tools (noise, terrain, dungeons)

**Note:** These are exploratory features and not guaranteed.

---

## Summary of Milestones

| Version | Milestone                                   | Target Date | Status      |
| ------- | ------------------------------------------- | ----------- | ----------- |
| v0.1.0  | Engine skeleton                             | TBD         | ✅ Complete |
| v0.2.0  | Playable core (ECS, scenes)                 | TBD         | ✅ Complete |
| v0.3.0  | Raylib backend + rendered demo              | TBD         | ✅ Complete |
| v0.4.9  | Developer comfort update                    | Nov 2025    | ✅ Complete |
| v0.5.0  | Texture filtering & mipmaps                 | Nov 2025    | ✅ Complete |
| v0.5.1  | MSAA support                                | Nov 2025    | ✅ Complete |
| v0.6.0  | Architecture refinement + Scene transitions | Nov 2025    | ✅ Complete |
| v0.7.0  | State Management + Caching + GameEngine     | Nov 2025    | ✅ Complete |
| v0.7.5  | Memory Profiling Tools                      | Nov 2025    | ✅ Complete |
| v0.8.0  | Physics & Savegame Systems                  | Nov 2025    | ✅ Complete |
| v0.9.0  | Physics Backend Abstraction                 | Nov 2025    | ✅ Complete |
| v0.10.0 | ECS Refactoring & Demo Abstractions         | Nov 2025    | ✅ Complete |
| v0.11.0 | TextLayoutHelper & UI Improvements         | Nov 2025    | ✅ Complete |
| v0.12.0 | Audio System Integration & Real Playback    | Nov 2025    | ✅ Complete |
| v0.13.0 | Capability Demonstrations & Polish          | Nov 2025    | ✅ Complete |
| v0.x.x  | Stabilization & polish                      | TBD         | In Progress |
| v1.0.0  | Stable public API                           | TBD         | Planned     |
| v1.1.0  | Scene management & templates                | TBD         | Planned     |
| v1.2.0  | Asset management & developer tools          | TBD         | Planned     |
| v1.3.0  | Scene serialization & pipeline              | TBD         | Planned     |
| v1.5.0  | Full ecosystem maturity                     | TBD         | Future      |
| v2.0.0  | 3D architecture foundation                  | TBD         | Exploratory      |
| v3.x+   | Editors, advanced tools, new domains        | TBD         | Exploratory |

---

## Development Philosophy

The roadmap follows these core principles:

### Iterate Quickly Early

-   Build feedback loops with working prototypes
-   Validate architecture decisions with real implementations
-   Fail fast and learn from mistakes
-   Release early, release often during pre-1.0 development

### Stabilize Late

-   API changes are allowed and expected before 1.0.0
-   Breaking changes are discouraged but acceptable if justified
-   User feedback shapes the API during 0.x versions
-   Freeze API only when it has been battle-tested

### Deliver Real Value Early

-   Each version must provide tangible functionality
-   Backends and sample games demonstrate real-world usage
-   Documentation accompanies every feature
-   No "placeholder" implementations

### Avoid Premature Complexity

-   Start simple, expand later
-   3D support comes only after 2D is mature and stable
-   Advanced features require solid foundations
-   Resist feature creep during early phases

### Allow Future Growth

-   Architecture decisions consider long-term extensibility
-   Abstractions designed for multiple implementations
-   No hardcoded assumptions about dimensionality
-   Plugin system supports custom backends and extensions

---

## Acceptance Criteria

Each phase must meet these criteria before moving to the next:

### Code Quality

-   ✅ All code follows project standards (see `.github/copilot-instructions.md`)
-   ✅ No compiler warnings or errors
-   ✅ Linter/formatter compliance
-   ✅ No TODO/FIXME without linked issues

### Testing

-   ✅ All public APIs have unit tests
-   ✅ Integration tests cover major workflows
-   ✅ No failing tests in CI/CD pipeline
-   ✅ Performance benchmarks meet targets

### Documentation

-   ✅ All public APIs documented with XML comments
-   ✅ User-facing guides updated
-   ✅ Architecture documents reflect current state
-   ✅ Migration guides for breaking changes

### Security & Stability

-   ✅ No critical vulnerabilities
-   ✅ Input validation on all boundaries
-   ✅ Error handling for all I/O operations
-   ✅ Memory safety validated (no leaks)

---

## Risk Management

### Technical Risks

| Risk                          | Impact | Probability | Mitigation Strategy                             |
| ----------------------------- | ------ | ----------- | ----------------------------------------------- |
| ECS performance bottleneck    | High   | Medium      | Early benchmarking, archetype optimization      |
| Backend abstraction overhead  | Medium | Low         | Profile and optimize hot paths                  |
| 3D architecture compatibility | High   | Medium      | Design reviews, prototyping                     |
| Resource loading complexity   | Medium | Medium      | Fail-safe loading, comprehensive validation     |
| API design mistakes           | High   | High        | User feedback loops, iterative refinement       |
| Determinism breaks            | High   | Low         | Automated tests, strict floating-point handling |
| Memory leaks                  | High   | Medium      | Automated memory profiling, RAII patterns       |
| Breaking changes in 0.x       | Medium | High        | Clear versioning, migration guides              |

### Project Risks

| Risk                     | Impact | Probability | Mitigation Strategy                          |
| ------------------------ | ------ | ----------- | -------------------------------------------- |
| Scope creep              | High   | High        | Strict phase boundaries, feature freeze      |
| Documentation lag        | Medium | Medium      | Documentation required before release        |
| Third-party dependencies | Medium | Low         | Backend abstraction, vendor evaluation       |
| Breaking upstream APIs   | Medium | Low         | Pin versions, test against multiple versions |
| Community engagement     | Low    | Medium      | Regular releases, transparent roadmap        |

---

## Final Notes

This roadmap is a living document and will evolve based on:

-   User feedback and community contributions
-   Technical discoveries during implementation
-   Changing requirements and priorities
-   New opportunities and technologies

However, the core philosophy and architectural principles remain fixed:

-   **Dimension-agnostic design**
-   **Backend independence**
-   **Deterministic behavior**
-   **Modular architecture**
-   **Long-term maintainability**

The roadmap provides a clear long-term vision while remaining flexible enough to adapt to real-world development needs.

---

## Revision History

| Version | Date              | Author         | Changes                                                                                                                                        |
| ------- | ----------------- | -------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| 3.2     | November 19, 2025 | Kevin Martínez | ECS Refactoring & Demo Abstractions (v0.10.0): CameraComponent/System, DraggableComponent/DragSystem, RigidBodyComponent cleanup, 16 new tests |
| 3.1     | January 19, 2025  | Kevin Martínez | Physics Backend Abstraction (v0.9.0 WIP): IPhysicsBackend interface, Aether.Physics2D integration, MicroEngine.Backend.Aether project          |
| 3.0     | January 19, 2025  | Kevin Martínez | Physics & Savegame (v0.8.0): CCD with swept AABB, collision resolution, PhysicsDemo, complete savegame system with versioning, 10 new tests    |
| 2.9     | January 19, 2025  | Kevin Martínez | Memory Profiling Tools (v0.7.5): MemorySnapshot, MemoryProfiler with leak detection, EcsMemoryProfiler, statistical analysis, 36 tests         |
| 2.8     | November 18, 2025 | Kevin Martínez | Structured Error Codes & Exception Hierarchy (v0.7.4): MicroEngineException base, domain exceptions, error codes, context management           |
| 2.7     | November 18, 2025 | Kevin Martínez | Performance Benchmarking Suite (v0.7.3): BenchmarkDotNet integration, validated archetype optimization with 37k-424k queries/sec               |
| 2.6     | November 18, 2025 | Kevin Martínez | Semantic versioning fix: Split features into patch releases (0.7.1 preloading, 0.7.2 archetypes)                                               |
| 2.5     | November 18, 2025 | Kevin Martínez | Archetype implementation: Simple solution without reflection/dynamic, ArchetypeId/Archetype/ArchetypeManager, query optimization (v0.7.2)      |
| 2.4     | November 18, 2025 | Kevin Martínez | Scene Preloading: Async background loading with PreloadAsync/PreloadMultipleAsync, Version management migration to Nerdbank (v0.7.1)           |
| 2.3     | November 18, 2025 | Kevin Martínez | GameEngine refactoring: Full integration with backends, fixed timestep (60Hz), Program.cs migration, frame-rate independence (v0.7.0)          |
| 2.2     | November 18, 2025 | Kevin Martínez | Phase 4 Scene Caching: ISceneCache/SceneCache with LRU eviction, MainMenuScene integration, visual status display (v0.7.0)                     |
| 2.1     | November 18, 2025 | Kevin Martínez | Phase 4 Global State + StateMachine: IGameState/GameState for persistent data, StateMachine framework (v0.7.0)                                 |
| 2.0     | November 18, 2025 | Kevin Martínez | Phase 4 Scene Parameters: Type-safe parameter passing, SceneParameters builder (v0.6.0)                                                        |
| 1.8     | November 18, 2025 | Kevin Martínez | Phase 4 Transitions: Additional scene transition effects (Slide, Wipe, Zoom), SetTransition() runtime changes, transition selector UI (v0.6.0) |
| 1.7     | November 18, 2025 | Kevin Martínez | Architecture refinement (v0.6.0): Eliminated circular dependency, SceneContext DI pattern, Scene navigation methods                            |
| 1.6     | November 18, 2025 | Kevin Martínez | Phase 4 Graphics: MSAA support (v0.5.1), texture filtering & mipmaps (v0.5.0), ECS query caching, fade transitions                             |
| 1.5     | November 18, 2025 | Kevin Martínez | Added missing features: SceneManager (Phase 4), Asset Management, Scene Templates, Developer Console, Scene Serialization (Phase 5)            |
| 1.4     | November 18, 2025 | Kevin Martínez | Phase 3 COMPLETE: Tilemap Support (v0.4.8), all 9/9 features implemented                                                                       |
| 1.5     | November 18, 2025 | Kevin Martínez | Phase 4 STARTED: SceneManager stack-based system (v0.4.9), demo refactoring, interactive EcsBasicsDemo                                         |
| 1.3     | November 18, 2025 | Kevin Martínez | Phase 3 status: 8/9 features (88%), Entity Inspector deferred to Phase 5+                                                                      |
| 1.2     | November 18, 2025 | Kevin Martínez | Added Collision Layer System (v0.4.7), updated Phase 3 progress                                                                                |
| 1.1     | November 17, 2025 | Kevin Martínez | Updated Phase 3 with EntityBuilder/Factory (v0.4.6), retroactive docs                                                                          |
| 1.0     | November 2025     | Kevin Martínez | Initial comprehensive roadmap with detailed phases                                                                                             |

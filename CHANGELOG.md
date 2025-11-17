# Changelog

All notable changes to MicroEngine will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

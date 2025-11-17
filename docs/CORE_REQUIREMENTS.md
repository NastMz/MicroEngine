# MicroEngine – Core Requirements

**Version:** 1.0  
**Status:** Mandatory  
**Author:** Kevin Martínez  
**Last Updated:** 2025  

---

## Overview

These are the **mandatory technical rules** that define how the engine must behave.

All subsystems, features, and implementations must adhere to these requirements.  
Violations of these rules are considered **architectural bugs** and must be fixed.

---

## Table of Contents

0. [Dimension-Agnostic Architecture](#0-dimension-agnostic-architecture)
1. [Engine/Game Isolation](#1-enginegame-isolation)
2. [Non-Blocking Script Execution](#2-non-blocking-script-execution)
3. [Memory Safety Rules](#3-memory-safety-rules)
4. [Deterministic Update Loop](#4-deterministic-update-loop)
5. [Scene Transition Safety](#5-scene-transition-safety)
6. [Controlled Concurrency](#6-controlled-concurrency)
7. [Backend Abstraction](#7-backend-abstraction)
8. [Resource Validation](#8-resource-validation)
9. [Savegame Requirements](#9-savegame-requirements)
10. [Error Handling Requirements](#10-error-handling-requirements)
11. [API Design Requirements](#11-api-design-requirements)
12. [Performance Requirements](#12-performance-requirements)

---

# 0. Dimension-Agnostic Architecture

### **R0.1 – Dimension-Agnostic Design**

The engine core must not assume 2D-only constraints.

**Requirements:**
- ✅ All high-level systems (ECS, scenes, resource management, backends) must be extendable to 3D without requiring architectural rewrites
- ✅ Transforms can be 2D initially but must be upgradeable to 3D
- ✅ Camera systems must not hardcode 2D projection
- ✅ Collision systems must be dimension-aware (2D for now, 3D in future)
- ✅ Rendering backends must support both 2D and 3D primitives (even if 3D is not implemented yet)

**Rationale:**
Future-proofing the architecture prevents costly rewrites when adding 3D support.

**Validation:**
- Code review must check for hardcoded 2D assumptions
- API design must consider 3D use cases
- Type systems should use generic math where applicable

---

# 1. Engine/Game Isolation

### **R1.1 – Engine Independence**

The engine must never depend on game-level assets or logic.

**Requirements:**
- ✅ Engine code must not reference specific game assets (sprites, sounds, levels)
- ✅ Engine must not contain game-specific logic (player movement, enemy AI)
- ✅ Engine must work with any game built on top of it
- ✅ Engine tests must not require game assets to run

**Example Violation:**
```csharp
// ❌ WRONG: Engine code referencing game-specific asset
public class Renderer
{
    private Texture _playerSprite = LoadTexture("game/player.png");
}
```

**Correct Approach:**
```csharp
// ✅ CORRECT: Engine provides API, game provides assets
public class Renderer
{
    public void DrawTexture(TextureHandle texture, Vector2 position);
}

// Game code
var playerSprite = resourceManager.LoadTexture("game/player.png");
renderer.DrawTexture(playerSprite, playerPosition);
```

### **R1.2 – Game Encapsulation**

The game must not modify internal engine structures.

**Requirements:**
- ✅ Game code must only use public engine APIs
- ✅ Engine internals must be marked as `internal` or `private`
- ✅ Game cannot subclass engine internal classes (unless designed for extension)
- ✅ Game cannot directly access engine data structures

**Rationale:**
Clear separation enables:
- Engine updates without breaking games
- Multiple games using the same engine
- Engine testing in isolation
- Better modularity and maintainability

---

# 2. Non-Blocking Script Execution

### **R2.1 – Main Thread Protection**

Scripts cannot block the main thread.

**Requirements:**
- ✅ No infinite loops in script code
- ✅ No blocking I/O operations (file, network) on main thread
- ✅ No long-running computations without yielding
- ✅ Execution budgets must be enforced

### **R2.2 – Execution Budgets**

The engine must enforce time limits on script execution.

**Requirements:**
- ✅ Maximum execution time per script per frame (e.g., 5ms)
- ✅ Scripts exceeding budget must be paused or terminated
- ✅ Warning/error must be logged when budget exceeded
- ✅ Developer mode should report budget violations

**Example:**
```csharp
public interface IScriptRunner
{
    /// <summary>
    /// Maximum milliseconds a script can execute per frame
    /// </summary>
    int ExecutionBudgetMs { get; set; }
    
    /// <summary>
    /// Executes script with timeout protection
    /// </summary>
    void Execute(IScript script, float deltaTime);
}
```

### **R2.3 – Async Operations**

Long-running operations must be asynchronous.

**Requirements:**
- ✅ File I/O must use async APIs or background threads
- ✅ Network requests must be non-blocking
- ✅ Heavy computations (pathfinding, procedural generation) must use worker threads
- ✅ Results must be communicated via message queues

**Rationale:**
Prevents frame drops and maintains consistent 60 FPS target.

---

# 3. Memory Safety Rules

### **R3.1 – Memory Management**

No custom memory pools unless justified.

**Requirements:**
- ✅ Use C# managed memory by default
- ✅ Custom allocators only for proven performance bottlenecks
- ✅ All custom memory must be properly disposed
- ✅ No manual pointer arithmetic unless absolutely necessary
- ✅ Document ownership semantics for all resources

### **R3.2 – Atomic Loading**

All loading operations must be atomic and fail-safe.

**Requirements:**
- ✅ Resource loading is all-or-nothing (no partial loads)
- ✅ Failed loads must not corrupt engine state
- ✅ Failed loads must clean up allocated resources
- ✅ Loading errors must be reported clearly
- ✅ Engine must remain stable after load failure

**Example:**
```csharp
public TextureHandle LoadTexture(string path)
{
    Texture? texture = null;
    try
    {
        // Validate file exists
        if (!File.Exists(path))
            throw new ResourceLoadException($"File not found: {path}");
        
        // Load and validate
        texture = LoadTextureInternal(path);
        ValidateTexture(texture);
        
        // Register handle
        return RegisterTexture(texture);
    }
    catch
    {
        // Cleanup on failure
        texture?.Dispose();
        throw;
    }
}
```

### **R3.3 – Resource Lifetime**

Resources must have clear ownership and lifetime.

**Requirements:**
- ✅ Use handles instead of direct pointers
- ✅ Detect use-after-free via handle versioning
- ✅ Reference counting or explicit lifetime management
- ✅ Automatic cleanup when owner is destroyed
- ✅ No dangling references

**Rationale:**
Prevents memory leaks, use-after-free bugs, and crashes.

---

# 4. Deterministic Update Loop

### **R4.1 – Fixed Timestep**

All logic uses a fixed timestep.

**Requirements:**
- ✅ Update loop runs at constant frequency (default: 60 Hz)
- ✅ Timestep is independent of frame rate
- ✅ Accumulator pattern prevents time drift
- ✅ Physics uses the same fixed timestep
- ✅ Same inputs produce same outputs (determinism)

**Example:**
```csharp
const double FIXED_TIMESTEP = 1.0 / 60.0;
double accumulator = 0.0;

while (running)
{
    double frameTime = GetFrameTime();
    accumulator += frameTime;
    
    while (accumulator >= FIXED_TIMESTEP)
    {
        Update(FIXED_TIMESTEP); // Always 0.016666s
        accumulator -= FIXED_TIMESTEP;
    }
    
    Render(accumulator / FIXED_TIMESTEP);
}
```

### **R4.2 – Render Decoupling**

Rendering is decoupled from updating.

**Requirements:**
- ✅ Render can run at different frequency than update
- ✅ Render never modifies game state
- ✅ Render only reads state (immutable view)
- ✅ Interpolation between states for smooth rendering
- ✅ Render can skip frames without affecting simulation

### **R4.3 – Determinism Guarantee**

Same inputs must produce same outputs.

**Requirements:**
- ✅ No use of wall-clock time in game logic (use game time)
- ✅ No undefined behavior (division by zero, uninitialized variables)
- ✅ No random without seeded RNG
- ✅ Consistent floating-point behavior
- ✅ Deterministic ordering of entity processing

**Rationale:**
Enables:
- Replay systems
- Network deterministic lockstep
- Automated testing
- Bug reproduction

---

# 5. Scene Transition Safety

### **R5.1 – Clean State Transitions**

Scenes must fully clean previous state before activating the next.

**Requirements:**
- ✅ Call `OnExit()` on current scene before switching
- ✅ Unload all scene-specific resources
- ✅ Clear ECS world (unless persistent)
- ✅ Reset scene-local state variables
- ✅ Call `OnEnter()` on new scene after cleanup complete

### **R5.2 – Atomic Transitions**

Scene transitions must be atomic.

**Requirements:**
- ✅ No update or render calls during transition
- ✅ Transition is all-or-nothing (no partial switches)
- ✅ If transition fails, engine must remain in stable state
- ✅ No state leaks between scenes
- ✅ Validate new scene before deactivating old scene

**Example Violation:**
```csharp
// ❌ WRONG: Partial transition
currentScene.OnExit();
currentScene = newScene;
// BUG: If OnEnter() throws, engine is in invalid state
currentScene.OnEnter();
```

**Correct Approach:**
```csharp
// ✅ CORRECT: Atomic transition
try
{
    newScene.Validate(); // Check before switching
    currentScene?.OnExit();
    var oldScene = currentScene;
    currentScene = newScene;
    currentScene.OnEnter();
    oldScene?.Dispose();
}
catch (Exception ex)
{
    // Rollback or enter error state
    Log.Error("Scene transition failed", ex);
    currentScene = fallbackScene;
    currentScene.OnEnter();
}
```

### **R5.3 – No State Leaks**

No data must leak between scene transitions.

**Requirements:**
- ✅ Static fields must be cleared or managed carefully
- ✅ Event listeners must be unsubscribed
- ✅ Timers and coroutines must be stopped
- ✅ Cached data must be invalidated
- ✅ References must be nulled to prevent accidental use

**Rationale:**
Prevents bugs where old scene data affects new scene behavior.

---

# 6. Controlled Concurrency

### **R6.1 – Single-Threaded Core**

Core engine is single-threaded.

**Requirements:**
- ✅ Main game loop runs on single thread
- ✅ ECS operations are single-threaded
- ✅ Scene management is single-threaded
- ✅ No concurrent modification of game state
- ✅ Thread safety is not a concern for core systems

### **R6.2 – Message Queue Communication**

Worker threads communicate via message queues only.

**Requirements:**
- ✅ No shared mutable state between threads
- ✅ Messages are immutable or copied
- ✅ Main thread processes messages in update loop
- ✅ Thread-safe queue implementation (lock-free preferred)
- ✅ Messages have clear ownership semantics

**Example:**
```csharp
// Worker thread
Task.Run(() =>
{
    var levelData = LoadLevelFromDisk(path);
    MessageQueue.Enqueue(new LevelLoadedMessage(levelData));
});

// Main thread (in Update)
while (MessageQueue.TryDequeue(out var message))
{
    switch (message)
    {
        case LevelLoadedMessage msg:
            ProcessLoadedLevel(msg.Data);
            break;
    }
}
```

### **R6.3 – Thread Safety Boundaries**

Clear boundaries for thread-safe operations.

**Requirements:**
- ✅ Document which APIs are thread-safe
- ✅ Document which APIs must be called from main thread
- ✅ Use attributes or naming conventions (`ThreadSafe`, `MainThreadOnly`)
- ✅ Throw exceptions if called from wrong thread (in debug mode)
- ✅ Logging and diagnostics are thread-safe

**Rationale:**
Simplifies reasoning, prevents race conditions, enables performance.

---

# 7. Backend Abstraction

### **R7.1 – Interface-Based Backends**

Core must define interfaces, backends implement them.

**Requirements:**
- ✅ Core defines `IRenderBackend`
- ✅ Core defines `IInputBackend`
- ✅ Core defines `IAudioBackend`
- ✅ Core defines `IWindowBackend`
- ✅ Backends implement all required interfaces

### **R7.2 – Interchangeable Modules**

Backends must be interchangeable without engine changes.

**Requirements:**
- ✅ Swapping backends requires zero engine code changes
- ✅ Engine code only references interface types, never concrete backends
- ✅ Backends are selected at startup via dependency injection
- ✅ Multiple backends can coexist (selected at runtime or compile time)
- ✅ Backend-specific code stays in backend assembly

**Example:**
```csharp
// ✅ CORRECT: Depends on interface
public class Renderer
{
    private readonly IRenderBackend _backend;
    
    public Renderer(IRenderBackend backend)
    {
        _backend = backend;
    }
}

// Backend selection at startup
var backend = new RaylibBackend(); // or new SDLBackend()
var engine = new GameEngine(backend);
```

### **R7.3 – Backend Isolation**

Backends cannot modify engine logic.

**Requirements:**
- ✅ Backends only implement interfaces, no engine internals access
- ✅ Backends cannot change engine behavior
- ✅ Backends handle platform-specific details only
- ✅ Backends report errors via exceptions (caught by engine)
- ✅ Backends can provide optional extensions via separate interfaces

### **R7.4 – Backend Requirements**

All backends must meet minimum requirements.

**Requirements:**
- ✅ Implement all required interface methods
- ✅ Pass conformance test suite
- ✅ Handle errors gracefully (no crashes)
- ✅ Support headless/null mode for testing
- ✅ Document platform-specific limitations

**Rationale:**
Enables platform portability, backend experimentation, and testing flexibility.

---

# 8. Resource Validation

### **R8.1 – Pre-Use Validation**

All resources must be validated before use.

**Requirements:**
- ✅ Check file format and magic numbers
- ✅ Validate file size and dimensions
- ✅ Verify checksums if available
- ✅ Sanitize file paths (prevent directory traversal)
- ✅ Reject malformed or corrupted files

**Example:**
```csharp
public TextureHandle LoadTexture(string path)
{
    // Validate path
    if (!IsValidPath(path))
        throw new ArgumentException("Invalid path", nameof(path));
    
    // Load raw data
    byte[] data = File.ReadAllBytes(path);
    
    // Validate format
    if (!IsSupportedImageFormat(data))
        throw new ResourceLoadException("Unsupported image format");
    
    // Validate dimensions
    var (width, height) = GetImageDimensions(data);
    if (width <= 0 || height <= 0 || width > MAX_TEXTURE_SIZE)
        throw new ResourceLoadException("Invalid texture dimensions");
    
    // Decode and create texture
    return CreateTextureFromData(data, width, height);
}
```

### **R8.2 – Safe Rejection**

Invalid or corrupted files must be rejected safely.

**Requirements:**
- ✅ No crashes on invalid input
- ✅ Clear error messages explaining rejection
- ✅ Engine remains stable after rejection
- ✅ Partial data is cleaned up
- ✅ Fallback resources can be used (e.g., default texture)

### **R8.3 – Trusted Sources**

Resources should come from trusted sources.

**Requirements:**
- ✅ Document expected asset sources
- ✅ Warn about loading from untrusted paths
- ✅ Sandbox resource loading if possible
- ✅ Prevent arbitrary code execution via assets
- ✅ Consider signing/checksumming critical assets

**Rationale:**
Prevents crashes, exploits, and undefined behavior from malformed assets.

---

# 9. Savegame Requirements

### **R9.1 – Game-Level Data Only**

Only game-level data may be serialized.

**Requirements:**
- ✅ Save files contain only game state, not engine internals
- ✅ No serialization of engine objects (entities, components, systems)
- ✅ Game defines serialization schema
- ✅ Engine provides serialization utilities (JSON, binary)
- ✅ Clear separation between game save data and engine state

### **R9.2 – Version Metadata**

Every save must include a version field.

**Requirements:**
- ✅ Save format version (e.g., "1.2.0")
- ✅ Game version that created the save
- ✅ Timestamp of creation
- ✅ Platform information (optional)
- ✅ Checksum/hash for corruption detection

**Example:**
```json
{
  "saveVersion": "1.0.0",
  "gameVersion": "0.5.2",
  "timestamp": "2024-01-15T14:30:00Z",
  "checksum": "abc123...",
  "data": {
    "playerLevel": 5,
    "position": {"x": 100, "y": 200},
    "inventory": []
  }
}
```

### **R9.3 – Graceful Degradation**

Saves must fail gracefully if invalid.

**Requirements:**
- ✅ Validate save format before loading
- ✅ Check version compatibility
- ✅ Verify checksum to detect corruption
- ✅ Provide clear error message if load fails
- ✅ Don't corrupt game state on failed load
- ✅ Optionally attempt migration from old versions

### **R9.4 – Forward/Backward Compatibility**

Support loading saves from different versions.

**Requirements:**
- ✅ Handle missing fields (use defaults)
- ✅ Ignore unknown fields (forward compatibility)
- ✅ Migrate old formats to new (versioned converters)
- ✅ Warn user about version mismatches
- ✅ Document breaking save format changes

### **R9.5 – Cross-Platform Portability**

Saves must work across platforms.

**Requirements:**
- ✅ Use platform-independent formats (JSON preferred)
- ✅ Consistent endianness for binary formats
- ✅ Normalize path separators
- ✅ No absolute file paths
- ✅ Text encoding must be UTF-8

**Rationale:**
Prevents data loss, enables cloud saves, supports cross-platform play.

---

# 10. Error Handling Requirements

### **R10.1 – Explicit Errors**

Fail with explicit errors, never silently.

**Requirements:**
- ✅ Throw exceptions for exceptional conditions
- ✅ Use specific exception types (not just `Exception`)
- ✅ Include detailed error messages
- ✅ Log all errors with context
- ✅ No empty catch blocks

**Example:**
```csharp
// ❌ WRONG: Silent failure
try
{
    LoadResource(path);
}
catch { }

// ✅ CORRECT: Explicit error
try
{
    LoadResource(path);
}
catch (FileNotFoundException ex)
{
    Log.Error($"Resource not found: {path}", ex);
    throw new ResourceLoadException($"Failed to load {path}", ex);
}
```

### **R10.2 – Error Recovery**

Engine must remain stable after errors.

**Requirements:**
- ✅ Catch errors at system boundaries
- ✅ Prevent one system's error from crashing others
- ✅ Attempt recovery or enter safe state
- ✅ Notify user of critical errors
- ✅ Enable error reporting/diagnostics

### **R10.3 – Validation at Boundaries**

Validate inputs at all public API boundaries.

**Requirements:**
- ✅ Null checks for reference parameters
- ✅ Range checks for numeric values
- ✅ Validate enum values
- ✅ Check preconditions and postconditions
- ✅ Use guard clauses

**Example:**
```csharp
public void DrawSprite(TextureHandle texture, Vector2 position)
{
    if (!IsValid(texture))
        throw new ArgumentException("Invalid texture handle", nameof(texture));
    
    if (float.IsNaN(position.X) || float.IsNaN(position.Y))
        throw new ArgumentException("Position contains NaN", nameof(position));
    
    // Safe to proceed
    DrawSpriteInternal(texture, position);
}
```

**Rationale:**
Makes debugging easier, prevents cascading failures, improves stability.

---

# 11. API Design Requirements

### **R11.1 – Consistency**

APIs must be consistent across the engine.

**Requirements:**
- ✅ Naming conventions (PascalCase for public, camelCase for private)
- ✅ Parameter ordering (e.g., handle first, data second)
- ✅ Return types (consistent use of handles, results, etc.)
- ✅ Error handling patterns (exceptions vs. return codes)
- ✅ Documentation style

### **R11.2 – Discoverability**

APIs must be easy to discover and understand.

**Requirements:**
- ✅ IntelliSense-friendly names
- ✅ XML documentation on all public members
- ✅ Usage examples in documentation
- ✅ Logical grouping of related APIs
- ✅ Minimal required knowledge to get started

### **R11.3 – Safety**

APIs must prevent common mistakes.

**Requirements:**
- ✅ Type safety (strong typing, avoid `object`)
- ✅ Immutability where possible
- ✅ Fail-fast on invalid usage
- ✅ No error-prone patterns (out parameters, nullable references)
- ✅ Compiler warnings for dangerous operations

### **R11.4 – Extensibility**

APIs must support future growth.

**Requirements:**
- ✅ Virtual methods for overriding
- ✅ Interfaces for abstraction
- ✅ Event systems for decoupling
- ✅ Extension points clearly documented
- ✅ Avoid sealed classes unless necessary

**Rationale:**
Good API design reduces learning curve and prevents misuse.

---

# 12. Performance Requirements

### **R12.1 – Acceptable Targets**

Minimum performance for typical games.

**Requirements:**
- ✅ Maintain 60 FPS for 2D games
- ✅ Handle 10,000+ simple entities
- ✅ Level load times <1 second
- ✅ Memory usage <100 MB for small games
- ✅ No frame hitches during gameplay

### **R12.2 – Performance Monitoring**

Engine must expose performance metrics.

**Requirements:**
- ✅ FPS counter
- ✅ Frame time graph
- ✅ Memory usage tracking
- ✅ System timing (update, render, physics)
- ✅ Entity/component counts

### **R12.3 – Optimization Strategy**

Optimize only after measuring.

**Requirements:**
- ✅ Profile before optimizing
- ✅ Focus on hot paths (update loop)
- ✅ Document optimization decisions
- ✅ Don't sacrifice clarity for micro-optimizations
- ✅ Regression testing for performance

**Rationale:**
"Premature optimization is the root of all evil" – but performance still matters.

---

## Enforcement

These requirements are enforced through:

1. **Code Reviews** – All PRs checked against requirements
2. **Automated Tests** – Unit and integration tests verify compliance
3. **Static Analysis** – Linters and analyzers catch violations
4. **Documentation** – Requirements referenced in code comments
5. **Architecture Audits** – Periodic review of system design

---

## Exceptions

Exceptions to these requirements must be:

1. **Documented** – Explain why exception is necessary
2. **Justified** – Provide technical reasoning
3. **Approved** – Reviewed by maintainers
4. **Time-Boxed** – Plan to resolve temporary violations
5. **Tracked** – Filed as technical debt

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025 | Initial requirements document |

---

**End of Document**


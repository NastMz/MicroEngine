# MicroEngine – Engine Design Document (EDD)

**Version:** v0.13.0 (Dev)  
**Status:** Draft  
**Author:** Kevin Martínez  
**Last Updated:** 2025  

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Purpose and Goals](#2-purpose-and-goals)
3. [Non-Goals](#3-non-goals)
4. [Core Requirements (Summary)](#4-core-requirements-summary)
5. [High-Level Architecture](#5-high-level-architecture)
6. [Engine Loop](#6-engine-loop)
7. [Scenes System](#7-scenes-system)
8. [ECS (Entity-Component-System)](#8-ecs-entity-component-system)
9. [Resource Management](#9-resource-management)
10. [Backend Interfaces](#10-backend-interfaces)
11. [Savegame Architecture](#11-savegame-architecture)
12. [Error Handling and Safety Rules](#12-error-handling-and-safety-rules)
13. [Concurrency Model](#13-concurrency-model)
14. [Performance Considerations](#14-performance-considerations)
15. [Versioning and Release Strategy](#15-versioning-and-release-strategy)
16. [Roadmap Summary](#16-roadmap-summary)
17. [Testing Strategy](#17-testing-strategy)
18. [Documentation Standards](#18-documentation-standards)
19. [Conclusion](#19-conclusion)

---

# 1. Introduction

MicroEngine is a modular, backend-agnostic game engine written in C#.  
It is designed to be **2D-focused today**, but fully capable of supporting **3D systems** in the future without architectural rewrites.

This document defines the technical vision, architecture, and constraints of the engine.

### **Document Scope**

This EDD covers:
- Overall architecture and design principles  
- Core subsystems and their interactions  
- Technical constraints and requirements  
- Extension points and future-proofing strategies  

---

# 2. Purpose and Goals

MicroEngine aims to be:

### **2.1 Educational**
- Readable, understandable, and maintainable code  
- Well-documented with clear examples  
- Suitable for learning game engine architecture  
- Simple enough to understand, complex enough to be useful  

### **2.2 Modular**
- Each subsystem is isolated and replaceable  
- Clear interfaces between components  
- Dependency injection for flexibility  
- Plugin-based backend system  

### **2.3 Dimension-Agnostic**
The engine core must not assume a fixed dimensionality.  
All systems must be written in a way that can be extended to 3D in future versions.

**Key principles:**
- Use generic math types where possible  
- Avoid hardcoding 2D assumptions in core systems  
- Design APIs that work for both 2D and 3D  
- Allow backends to implement dimension-specific optimizations  

### **2.4 Deterministic**
- Logic is executed using a fixed timestep  
- Same inputs produce same outputs  
- Enables replay systems and network synchronization  
- Predictable behavior for debugging  

### **2.5 Backend-Independent**
- Rendering, audio, and input are handled through interfaces only  
- No direct dependencies on specific libraries (Raylib, SDL, etc.)  
- Backends can be swapped at compile time or runtime  
- Engine core remains platform-agnostic  

### **2.6 Safe and Robust**
- Explicit error handling  
- No silent failures  
- Resource validation  
- Memory safety  

---

# 3. Non-Goals

MicroEngine explicitly does **not** aim to:

- Full 3D support in the **initial** implementation (reserved for v2.x)  
- Building an all-in-one commercial engine competing with Unity/Unreal  
- Advanced physics or animation pipelines for 3D (yet)  
- Visual editors or GUI tools (initial versions are code-first)  
- Supporting every platform from day one  
- Maximum performance at the cost of code clarity  

These features are reserved for future expansions.

---

# 4. Core Requirements (Summary)

See full details in:  
📘 `CORE_REQUIREMENTS.md`

Key architectural rules include:

### **R0.1 – Dimension-Agnostic Architecture**
The engine core must not assume 2D-only constraints.

### **R1 – Engine/Game Isolation**
- The engine must never depend on game-level assets or logic  
- The game must not modify internal engine structures  

### **R2 – Non-Blocking Script Execution**
- Scripts cannot block the main thread  
- Execution budgets must be enforced  

### **R3 – Memory Safety**
- All loading operations must be atomic and fail-safe  
- No unsafe memory operations without justification  

### **R4 – Fixed Timestep**
- All logic uses a fixed timestep  
- Rendering is decoupled from updating  

### **R5 – Scene Transition Safety**
- Scenes must fully clean previous state  
- No state leaks between scenes  

### **R6 – Controlled Concurrency**
- Core engine is single-threaded  
- Worker threads communicate via message queues only  

### **R7 – Backend Abstraction**
- Core defines interfaces, backends implement them  
- Backends are interchangeable modules  

### **R8 – Resource Validation**
- All resources must be validated before use  
- Invalid or corrupted files must be rejected safely  

---

# 5. High-Level Architecture

MicroEngine follows a **three-layer architecture**:

```
+-----------------------------+
|        Game Layer           |
| (Scenes, gameplay logic)    |
|  - Game-specific code       |
|  - Asset management         |
|  - Gameplay systems         |
+-----------------------------+
           ▲ ▼
+-----------------------------+
|   Engine Backends Layer     |
| (Render/Input/Audio impl.)  |
|  - Raylib, SDL, OpenGL      |
|  - Platform-specific code   |
+-----------------------------+
           ▲ ▼
+-----------------------------+
|       Engine Core           |
| (ECS, Scenes, Loop, Time)   |
|  - Platform-independent     |
|  - Pure C# logic            |
+-----------------------------+
```

### **5.1 Engine Core**
Contains platform-independent logic:

- **Update loop** – Fixed timestep game loop  
- **Scene management** – Scene lifecycle and transitions  
- **ECS** – Entity-Component-System framework  
- **Resource management** – Loading, caching, unloading  
- **Physics/collision** – 2D collision detection (3D in future)  
- **Time system** – Delta time, fixed time, timers  
- **Backend interface definitions** – Abstract interfaces for backends  
- **Event system** – Decoupled communication between systems  
- **Backends must remain isolated** – No cross-contamination  
- **No backend can modify engine logic** – Only implement interfaces  
- **Replacing a backend must require zero changes in engine code** – Pure interface compliance  
- **Backends can add extensions** – Via optional interfaces (e.g., `IRenderBackendExtended`)  
- **Backends must handle their own errors** – Convert to engine exceptions  
- **Backends must be stateless where possible** – Engine owns state  

### **10.6 Backend Selection**

Backends are selected at application startup:

```csharp
var engine = new GameEngine(new RaylibBackend());
engine.Run(new MyGame());
```

---

# 11. Savegame Architecture

### **11.1 Savegame Rules**

- **Only game-level data may be serialized** – No engine internals  
- **Engine internals must never be saved** – Prevents version conflicts  
- **Every save must include a version field** – Enable forward/backward compatibility  
- **Saves must fail gracefully if invalid** – Don't crash on corrupted saves  
- **Saves must be portable across platforms** – Endianness, path separators  

### **11.2 Serialization Goals**

- **Predictable structure** – Well-defined schema  
- **Versioned evolution** – Handle format changes  
- **Portable and human-readable** – JSON preferred, binary optional  
- **Compressed storage** – Reduce file size  
- **Incremental saves** – Support autosave without full serialization  

### **11.3 Save Data Structure**

```json
{
  "version": "1.0.0",
  "timestamp": "2024-01-15T12:00:00Z",
  "gameData": {
    "playerName": "Hero",
    "level": 5,
    "position": { "x": 100.5, "y": 200.3 },
    "inventory": [ ... ],
    "questStates": { ... }
  }
}
```

### **11.4 Serialization API**

```csharp
public interface ISaveGameManager
{
    void Save(string slotName, GameState state);
    GameState Load(string slotName);
    bool Exists(string slotName);
    void Delete(string slotName);
    string[] GetAllSlots();
}
```

---

# 12. Error Handling and Safety Rules

MicroEngine must prioritize **explicit errors** over silent failures.

### **12.1 Error Handling Principles**

- **Fail with explicit errors** – Throw exceptions with clear messages  
- **Detect invalid resources** – Validate before use  
- **Refuse inconsistent scene transitions** – Atomic transitions  
- **Prevent undefined behaviour caused by timing or concurrency** – Locks and validation  
- **Avoid silent corruption of state** – Assert invariants  
- **Log all errors** – Structured logging for debugging  

### **12.2 Exception Hierarchy**

```csharp
public class MicroEngineException : Exception { }
public class ResourceLoadException : MicroEngineException { }
public class SceneTransitionException : MicroEngineException { }
public class BackendException : MicroEngineException { }
public class InvalidStateException : MicroEngineException { }
```

### **12.3 Validation Strategy**

- **Validate inputs at API boundaries** – Public methods check arguments  
- **Use assertions for internal invariants** – Debug builds only  
- **Null checks for all references** – Prevent NullReferenceException  
- **Range checks for indices** – Prevent IndexOutOfRangeException  

### **12.4 Logging**

```csharp
public interface ILogger
{
    void LogDebug(string message);
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? ex = null);
    void LogFatal(string message, Exception? ex = null);
}
```

---

# 13. Concurrency Model

### **13.1 Core Principles**

- **Core engine is single-threaded** – Main game loop runs on one thread  
- **Worker threads for heavy tasks** – Asset loading, pathfinding, etc.  
- **Message queues for communication** – Thread-safe data transfer  
- **No shared mutable state** – Prevent race conditions  

### **13.2 Allowed Concurrency**

- **Async resource loading** – Background threads load assets  
- **Audio processing** – Separate audio thread (backend-managed)  
- **Rendering** – May run on separate thread (backend-specific)  

### **13.3 Forbidden Operations**

- ❌ Modifying ECS from worker threads  
- ❌ Creating entities outside main thread  
- ❌ Directly mutating game state from async code  

### **13.4 Communication Pattern**

```csharp
// Worker thread
Task.Run(() => {
    var data = LoadHeavyData();
    MessageQueue.Post(new DataLoadedMessage(data));
});

// Main thread (in Update)
while (MessageQueue.TryDequeue(out var message))
{
    ProcessMessage(message);
}
```

---

# 14. Performance Considerations

While **clarity is prioritized over performance**, the engine must remain practical.

### **14.1 Performance Goals**

- Maintain 60 FPS for typical 2D games  
- Handle 10,000+ entities (with simple systems)  
- Load levels in <1 second  
- Memory footprint <100 MB for small games  

### **14.2 Optimization Strategy**

1. **Measure first** – Profile before optimizing  
2. **Optimize hot paths** – Focus on update loop  
3. **Use object pooling** – For frequently allocated objects  
4. **Cache lookups** – Avoid redundant searches  
5. **Batch rendering** – Minimize draw calls  

### **14.3 Profiling Support**

The engine should expose profiling hooks:

```csharp
public interface IProfiler
{
    void BeginSample(string name);
    void EndSample();
    ProfileResult GetResults();
}
```

---

# 15. Versioning and Release Strategy

MicroEngine uses:

- **SemVer 2.0** for public packages  
- **Nerdbank.GitVersioning (NBGV)** for automatic version stamping  

### **15.1 Version Format**

**Tagged releases:**
- `vX.Y.Z` – Stable releases  
- `vX.Y.Z-alpha.N` – Alpha releases  
- `vX.Y.Z-beta.N` – Beta releases  
- `vX.Y.Z-rc.N` – Release candidates  

### **15.2 Semantic Versioning Rules**

- **Major (X)** – Breaking API changes  
- **Minor (Y)** – New features, backward compatible  
- **Patch (Z)** – Bug fixes only  

### **15.3 Package Names**

- `MicroEngine.Core` – Core engine library  
- `MicroEngine.Backend.Raylib` – Raylib backend  
- `MicroEngine.Backend.SDL` – SDL2 backend  
- `MicroEngine.Tools` – Development tools  

### **15.4 API Stability**

- **v0.x** – Experimental, API may change  
- **v1.x** – Stable API, backward compatible  
- **v2.x** – Major rework, possible breaking changes  

---

# 16. Roadmap Summary

See full roadmap in:  
📘 `ROADMAP.md`

**High-level milestones:**

- **0.1** — Initial architecture & core loop  
  - Game loop with fixed timestep  
  - Basic window and input  
  - Simple rendering  

- **0.2** — ECS + scenes + resources  
  - Complete ECS implementation  
  - Scene management system  
  - Resource loading and handles  

- **0.3** — First backend (Raylib)  
  - Full Raylib integration  
  - 2D sprite rendering  
  - Audio playback  

- **0.4** — Tools, debugging, profiling  
  - Debug overlay  
  - Performance profiler  
  - Entity inspector  

- **0.5** — Example game  
  - Complete 2D game demo  
  - Documentation  
  - Tutorials  

- **1.0** — Stable API freeze  
  - Production-ready  
  - Full documentation  
  - Breaking changes locked  

---

# 17. Testing Strategy

### **17.1 Testing Levels**

**Unit Tests:**
- Test individual components in isolation  
- ECS operations  
- Math utilities  
- Resource loading  

**Integration Tests:**
- Test subsystem interactions  
- Scene transitions  
- Backend integration  

**End-to-End Tests:**
- Test complete game scenarios  
- Performance benchmarks  

### **17.2 Testing Requirements**

- All public APIs must have unit tests  
- Critical paths must have integration tests  
- Backends must have conformance tests  
- Determinism must be tested (replay tests)  

### **17.3 Continuous Integration**

- Automated builds on commit  
- All tests must pass before merge  
- Code coverage reporting  
- Performance regression detection  

---

# 18. Documentation Standards

### **18.1 Code Documentation**

- All public APIs must have XML comments  
- Include usage examples  
- Document thread safety  
- Specify ownership semantics  

```csharp
/// <summary>
/// Loads a texture from the specified file path.
/// </summary>
/// <param name="path">Relative path to the texture file</param>
/// <returns>Handle to the loaded texture</returns>
/// <exception cref="ResourceLoadException">Thrown if file is invalid or corrupted</exception>
/// <remarks>
/// This operation is synchronous. For async loading, use <see cref="LoadTextureAsync"/>.
/// Thread-safe: No. Must be called from main thread.
/// </remarks>
public TextureHandle LoadTexture(string path);
```

### **18.2 Architectural Documentation**

- High-level design documents (like this one)  
- Architecture decision records (ADRs)  
- Tutorial series for beginners  
- API reference (generated from XML comments)  

### **18.3 Example Code**

- Provide working samples for all major features  
- Keep examples simple and focused  
- Include commented explanations  
- Ensure examples compile and run  

---

# 19. Conclusion

MicroEngine is designed to be **small, robust, and educational**.

This document defines the **rules, constraints, and structure** that guarantee long-term maintainability and clean architecture.

### **Core Tenets**

1. **Simplicity over complexity** – Prefer clear code over clever code  
2. **Modularity over monoliths** – Keep systems independent  
3. **Explicit over implicit** – Make behavior obvious  
4. **Safety over performance** – Correct first, fast second  
5. **Future-proof over immediate** – Design for extensibility  

### **Development Philosophy**

- Write code as if teaching someone  
- Document as you go, not after  
- Test early and often  
- Refactor mercilessly  
- Accept that first versions won't be perfect  

### **Final Note**

Any future feature or subsystem must follow the principles and requirements outlined here.

**When in doubt:**
- Read the design documents  
- Consult the requirements  
- Ask: "Does this make the engine simpler or more complex?"  
- Choose simplicity  

---

**End of Document**

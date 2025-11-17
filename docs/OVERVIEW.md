# MicroEngine — Overview

MicroEngine is a modular, lightweight game engine written in C#.  
It currently focuses on **2D gameplay**, but its entire design is **dimension-agnostic**, allowing smooth expansion into **3D systems** in future versions.

This overview explains the engine’s philosophy, goals, and high-level architecture.

---

## ✨ Core Principles

### **1. Dimension-Agnostic Architecture**
The engine core must never assume 2D-specific constraints.  
All high-level systems—ECS, scenes, resource management, backend interfaces—must remain extendable to 3D.

### **2. Strict Separation Between Engine and Game**
The engine core is independent from game-specific assets or logic.  
The game interacts with the engine only through public APIs.

### **3. Backend Independence**
The engine defines interfaces for:

- rendering  
- input  
- audio  

Backends (Raylib, OpenGL, SDL, etc.) implement these interfaces as external modules.

### **4. Deterministic Update Loop**
Logic runs on a fixed timestep, ensuring predictable and stable behaviour.

### **5. Clear and Maintainable ECS**
Entities, components, and systems are kept simple and easy to understand.

### **6. Safe Resource Loading**
All resources must be validated before use, and loading must be atomic.

---

## 🧱 Architecture Layers

- Engine.Core → Platform-independent logic
- Engine.Backend.* → Concrete backend implementations
- Game/ → Game project using the engine


### **Engine.Core**
Contains:

- update loop  
- scene system  
- ECS  
- transform system  
- resource manager  
- backend interface definitions  
- time and scheduling logic  

### **Backends**
Optional plug-in modules that implement:

- `IRenderBackend`
- `IInputBackend`
- `IAudioBackend`

### **Game Layer**
Projects built using MicroEngine:

- scenes  
- components  
- gameplay logic  
- assets  

---

## 🔭 Future 3D Support

MicroEngine’s architecture is designed so that all fundamental systems can evolve to support:

- 3D transforms  
- 3D rendering  
- 3D physics  
- spatial trees  
- mesh/material pipelines  
- 3D scene graphs  

This forward compatibility is intentional and ensures that the engine grows without architectural rewrites.

---

## 📚 See Also

- [Engine Design Document](ENGINE_DESIGN_DOCUMENT.md)  
- [Core Requirements](CORE_REQUIREMENTS.md)  
- [Architecture](ARCHITECTURE.md)


# MicroEngine  
*A lightweight, modular game engine built with C# (2D-focused, 3D-capable).*

**Version:** v0.13.0 (Dev)

---

## 🚀 Overview

**MicroEngine** is a clean, modular, backend-agnostic game engine designed for clarity, maintainability, and long-term scalability.

It currently focuses on **2D games**, but its architecture is intentionally **dimension-agnostic**, enabling future extension into **3D rendering, physics, and scene management** without architectural rewrites.

The engine cleanly separates:

- **Engine Core** (logic, ECS, scenes, update loop)
- **External Backends** (render, input, audio)
- **Game Layer** (your game code)

---

## ✨ Key Features

- 🔸 **Fixed-step update loop** (decoupled from rendering)
- 🔸 **Dimension-agnostic architecture (2D today, 3D-ready)**
- 🔸 **Scene system** with deterministic transitions  
- 🔸 **Lightweight, readable ECS**

- Engine.Core → Pure engine logic
- Engine.Backend.* → Optional backend implementations
- Game/ → Game project using the engine

The core contains **no rendering, audio, or input dependencies**.  
Backends implement interfaces and can be swapped without modifying engine code.

For a full breakdown, see:  
📘 [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)

---

## 🔭 Long-Term Vision: Future 3D Support

While MicroEngine is currently 2D-focused, the architecture is designed to evolve into:

- 3D rendering pipelines  
- 3D physics  
- 3D cameras  
- mesh importers  
- material/shader systems  
- spatial scene graphs  

This forward-compatible design ensures that new capabilities can be added without breaking the current 2D engine.

---

## 📦 Installation (Future)

Available as NuGet packages:

- MicroEngine.Core
- MicroEngine.Backend.Raylib

Versioning uses **SemVer** + **Nerdbank.GitVersioning (NBGV)**.

---

## 📚 Documentation

- 📘 [Overview](docs/OVERVIEW.md)  
- 📘 [Engine Design Document](docs/ENGINE_DESIGN_DOCUMENT.md)  
- 📘 [Core Requirements](docs/CORE_REQUIREMENTS.md)  
- 📘 [Architecture](docs/ARCHITECTURE.md)  
- 📁 [Modules](docs/MODULES/)  
- 🛣 [Roadmap](docs/ROADMAP.md)

---

## 🤝 Contributing

Contributions are welcome!  
Please read:

- [`CONTRIBUTING.md`](CONTRIBUTING.md)  
- [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md)

---

## 📝 License

Licensed under the MIT License.  
See the [`LICENSE`](LICENSE) file for details.

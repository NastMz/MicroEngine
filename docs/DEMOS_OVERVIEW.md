# MicroEngine Demos Overview

This document provides a comprehensive overview of all interactive demos available in MicroEngine.

## Demo Access

All demos are accessible from the **MainMenuScene**:

-   Launch `MicroEngine.Game`
-   Press keys **1-6** to load the corresponding demo
-   Press **ESC** from any demo to return to the main menu
-   Press **F6-F9** to test scene transition features

## Available Demos

### 1. ECS Basics Demo (`[1]`)

**Purpose:** Demonstrates Entity Component System fundamentals and Phase 3 entity creation patterns.

**Features:**

-   **EntityBuilder Pattern:** Clean entity construction with fluent API
-   **EntityFactory Pattern:** Reusable entity templates
-   **Interactive Player:** WASD/Arrow keys for movement (200 units/sec)
-   **Enemy Patrol:** 4 red enemies with horizontal patrol behavior (50 units/sec)
-   **Collectible Items:** 6 yellow items with bounce animation (staggered phases)
-   **Static Obstacles:** 4 gray obstacles demonstrating position-only entities
-   **Collection System:** Collect items by proximity detection
-   **Reset Functionality:** Press **R** to regenerate all entities

**Key Concepts:**

-   Entity lifecycle management (create, update, destroy)
-   Component-based architecture (TransformComponent)
-   System processing (MovementSystem)
-   Entity queries and filtering

**Controls:**

-   **WASD/Arrows:** Move player
-   **R:** Reset scene
-   **ESC:** Exit to menu

---

### 2. Graphics Demo (`[2]`)

**Purpose:** Showcases Camera2D, sprite rendering, texture management, and resource caching.

**Features:**

-   **Camera System:** Full 2D camera with position, zoom, and rotation
-   **World Grid:** 2000x2000 world with 100-unit grid spacing
-   **Random Sprites:** 50 procedurally placed sprites across the world
-   **Texture Loading:** Real texture files from `Assets/Textures/` directory
-   **Texture Filters:** Point, Bilinear, Trilinear filtering modes
-   **Resource Management:** Demonstrates texture caching and hot-reload
-   **Camera Movement:** Smooth WASD movement with zoom-adjusted speed
-   **Zoom Controls:** Dynamic zoom from 0.25x to 4.0x

**Sprite Types:**

-   Player, Enemy, Coin, Star, Box, Heart (loaded from individual PNG files)

**Controls:**

-   **WASD:** Move camera
-   **Q/E:** Zoom out/in
-   **R:** Reset camera to center
-   **SPACE:** Regenerate sprites
-   **F1-F4:** Change texture filter (Point, Bilinear, Trilinear, Anisotropic)
-   **ESC:** Exit to menu

---

### 3. Physics Demo (`[3]`)

**Purpose:** Demonstrates physics simulation with Continuous Collision Detection (CCD).

**Features:**

-   **Gravity System:** 980 units/sec² downward acceleration
-   **CCD Implementation:** Swept AABB collision to prevent tunneling
-   **Dynamic Spawning:** Auto-spawn balls every 0.5 seconds (max 10)
-   **Click Spawning:** Click anywhere to spawn a ball at that position
-   **Restitution:** 0.6 bounciness coefficient for realistic collisions
-   **Ground Collider:** 600x20 static platform for collision testing
-   **Visual Feedback:** Color-coded balls (random colors)
-   **Off-screen Cleanup:** Automatic removal when y > 700
-   **Collision Resolution:** Position adjustment + velocity reflection

**Technical Details:**

-   **CollisionInfo:** Normal, penetration, contact point, time of impact
-   **SweptCollision:** Ray vs AABB intersection for precise TOI
-   **RenderComponent:** Rectangle, Circle, Line shape rendering
-   **RigidBodyComponent:** Mass, velocity, acceleration, restitution, CCD flag

**Controls:**

-   **Click:** Spawn ball at mouse position
-   **ESC:** Exit to menu

---

### 4. Input Demo (`[4]`)

**Purpose:** Real-time input state visualization for keyboard, mouse, and gamepad.

**Features:**

-   **Keyboard Tracking:** Last 5 pressed keys with history
-   **Mouse Position:** Real-time X/Y coordinates
-   **Mouse Buttons:** Left, Right, Middle button press history
-   **Mouse Wheel:** Cumulative scroll delta tracking
-   **Gamepad Axes:** Left/Right stick values (threshold: 0.1)
-   **History System:** Rolling 5-item buffer for recent inputs
-   **Clear Function:** SPACE to clear all history and accumulators
-   **Parameter Passing:** Demonstrates SceneParameters with `welcomeMessage`

**Input Sources:**

-   Keyboard: All keys in `Key` enum
-   Mouse: Position, Left/Right/Middle buttons, Wheel
-   Gamepad: LeftX/Y, RightX/Y axes (player 0)

**Controls:**

-   **Any Key:** Add to keyboard history
-   **Mouse Buttons:** Add to button history
-   **Mouse Wheel:** Accumulate scroll delta
-   **SPACE:** Clear all input history
-   **ESC:** Exit to menu

---

### 5. Tilemap Demo (`[5]`)

**Purpose:** Demonstrates tile-based rendering with procedural generation and camera movement.

**Features:**

-   **Procedural Generation:** Random tilemap using simple noise algorithm
-   **Grid Size:** 25x19 tiles (800x608 pixels at 32px/tile)
-   **Camera Movement:** Smooth scrolling with WASD/Arrows
-   **Viewport Culling:** Only renders visible tiles for performance
-   **Grid Rendering:** 1px spacing between tiles for visual clarity
-   **Tile Types:** Grass (60%), Water (15%), Dirt (15%), Stone (10%)
-   **Legend Display:** Color-coded tile type reference
-   **Regeneration:** SPACE to generate new random tilemap

**Tile Palette:**

-   **Grass:** RGB(80, 160, 60) - Green
-   **Water:** RGB(50, 100, 200) - Blue
-   **Dirt:** RGB(140, 90, 50) - Brown
-   **Stone:** RGB(120, 120, 130) - Gray

**Technical Details:**

-   Camera offset: Vector2 with smooth translation
-   Culling: Skip tiles outside visible area
-   Tile size: 32x32 pixels (constant)
-   World size: 800x608 pixels (25x19 tiles)

**Controls:**

-   **WASD/Arrows:** Move camera (200 units/sec)
-   **SPACE:** Regenerate tilemap
-   **R:** Reset camera to origin
-   **ESC:** Exit to menu

---

### 6. Audio Demo (`[6]`)

**Purpose:** Simulated audio system demonstration (audio backend integration pending).

**Features:**

-   **Music Playback:** Simulated play/pause toggle
-   **Music Volume:** 0-100% control with up/down arrows
-   **Sound Effects:** 3 simulated sounds (Jump, Collect, Hit)
-   **Volume Bars:** Visual feedback for music and SFX levels
-   **Playback Status:** "Playing" / "Paused" indicator
-   **Sound Feedback:** 0.5-second visual flash on SFX trigger

**Simulated Sounds:**

-   **Jump (J):** Player jump sound effect
-   **Collect (C):** Item collection sound
-   **Hit (H):** Damage/impact sound

**Note:** This demo uses simulated controls as the audio backend (`IAudioBackend`, `IMusic`, `ISound`) is not yet fully integrated. The demo shows the intended user experience and interface design.

**Controls:**

-   **SPACE:** Toggle music play/pause
-   **↑/↓:** Adjust music volume (±10%)
-   **J:** Trigger jump sound
-   **C:** Trigger collect sound
-   **H:** Trigger hit sound
-   **ESC:** Exit to menu

---

## Scene System Features

All demos leverage MicroEngine's scene system capabilities:

### Scene Caching

-   **Background Preloading:** All 6 demos preloaded on MainMenu load
-   **Cache Status:** Displayed in menu (e.g., "6 scenes cached")
-   **Instant Transitions:** Cached scenes load instantly with no delay

### Scene Stack

-   **Push/Pop Navigation:** Demos push onto stack, ESC pops back to menu
-   **Stack Preservation:** Return to exact menu state after demo

### Scene Transitions (F6-F9)

-   **F6:** Fade to black
-   **F7:** Horizontal wipe
-   **F8:** Vertical wipe
-   **F9:** Instant cut (no transition)

### Scene Parameters

-   **Type-Safe:** Generic `SceneParameters` dictionary
-   **InputDemo Example:** `welcomeMessage` and `fromMenu` parameters
-   **Flexible:** Any serializable type supported

---

## Performance Metrics

All demos maintain **60 FPS** with the following entity counts:

| Demo       | Entity Count | Systems | Notes                                            |
| ---------- | ------------ | ------- | ------------------------------------------------ |
| ECS Basics | 15           | 1       | 1 player, 4 enemies, 6 collectibles, 4 obstacles |
| Graphics   | 50 sprites   | 0       | Camera-based, no ECS systems                     |
| Physics    | 1-10 dynamic | 2       | PhysicsSystem, CollisionSystem (CCD)             |
| Input      | 0            | 0       | Pure input visualization, no entities            |
| Tilemap    | 0            | 0       | Render-only, 475 tiles (with culling)            |
| Audio      | 0            | 0       | Simulated controls, no entities                  |

---

## Code Organization

```
src/MicroEngine.Game/Scenes/
├── MainMenuScene.cs          # Main menu with demo navigation
└── Demos/
    ├── EcsBasicsDemo.cs      # 421 lines - Interactive ECS patterns
    ├── GraphicsDemo.cs       # 516 lines - Camera & textures
    ├── PhysicsDemo.cs        # 252 lines - CCD physics
    ├── InputDemo.cs          # 237 lines - Input visualization
    ├── TilemapDemo.cs        # 210 lines - Procedural tilemap
    └── AudioDemo.cs          # 195 lines - Simulated audio
```

**Total Demo Code:** ~1,831 lines  
**Average Demo Size:** ~305 lines

---

## Testing Demos

### Quick Test Sequence

1. **Launch:** `dotnet run --project src/MicroEngine.Game`
2. **ECS [1]:** Move player, collect items, press R to reset
3. **Graphics [2]:** Move camera with WASD, zoom with Q/E, change filters F1-F3
4. **Physics [3]:** Watch balls fall, click to spawn, observe collisions
5. **Input [4]:** Press keys, move mouse, scroll wheel, test gamepad if available
6. **Tilemap [5]:** Scroll with WASD, press SPACE to regenerate
7. **Audio [6]:** Toggle music, adjust volume, trigger sounds J/C/H
8. **Scene Transitions:** Press F6-F9 while in any demo
9. **Navigation:** Press ESC from each demo to return to menu

### Expected Results

✅ All demos load instantly (scene caching)  
✅ No compilation errors or warnings (except 1 pre-existing)  
✅ 60 FPS stable performance  
✅ Smooth transitions between demos  
✅ ESC always returns to menu  
✅ All controls responsive

---

## Future Enhancements

### Short Term

-   **Audio Integration:** Connect AudioDemo to real audio backend
-   **Tilemap Assets:** Load real tileset textures instead of procedural colors
-   **InputMap:** Implement rebindable input actions
-   **More Physics:** Add forces (wind, explosions), constraints (springs, hinges)

### Medium Term

-   **Particle Systems:** Add to Physics and Graphics demos
-   **Networking Demo:** Multiplayer synchronization showcase
-   **UI Demo:** Widget library and layout systems
-   **Animation Demo:** Sprite animation and tweening

### Long Term

-   **Game Templates:** Use demos as starting points for real games
-   **Benchmarking:** Performance comparison across different hardware
-   **Tutorial Mode:** Step-by-step guided tours of each demo
-   **Editor Integration:** In-game scene/entity editing

---

## Conclusion

MicroEngine v0.8.0-alpha provides **6 comprehensive, interactive demos** showcasing the engine's core capabilities:

1. **ECS Architecture** - Modern entity patterns with interactive gameplay
2. **Graphics Pipeline** - Camera system, textures, filtering, caching
3. **Physics Simulation** - CCD collision with realistic bouncing
4. **Input Handling** - Multi-device input visualization
5. **Tile Rendering** - Procedural generation with camera scrolling
6. **Audio Preview** - Simulated audio controls and UI design

All demos are **fully functional**, **well-documented**, and **ready for testing**. They demonstrate best practices for scene management, entity patterns, and game systems architecture.

---

**Version:** 0.8.0-alpha  
**Last Updated:** 2025-01-19  
**Status:** All demos complete and tested ✅

# MicroEngine Demos Overview

This document provides a comprehensive overview of all interactive demos available in MicroEngine.

## Demo Access

All demos are accessible from the **MainMenuScene**:

-   Launch `MicroEngine.Game`
-   Press keys **1-6** to load the corresponding demo
-   Press **ESC** from any demo to return to the main menu
-   Press **F6-F9** to test scene transition effects

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

**Purpose:** Demonstrates realistic rigid body physics with proper collision resolution.

**Features:**

-   **Realistic Dynamics:** Proper stacking, falling, and collision resolution
-   **Gravity System:** 750 pixels/sec² downward gravity
-   **Dynamic Bodies:** Mass-based physics simulation
-   **Static Platform:** 600x20 ground for collision testing
-   **Click Spawning:** Click anywhere to spawn balls (max 20)
-   **Drag System:** Click and drag balls with kinematic body switching
-   **Restitution:** 0.7 bounciness for realistic bouncing
-   **Visual Feedback:** Random colored balls
-   **Off-screen Cleanup:** Automatic removal when y > 700

**Technical Details:**

-   **Body Types:** Static (ground), Dynamic (balls), Kinematic (during drag)
-   **Collision Resolution:** Accurate physics simulation with realistic responses
-   **Drag Mechanics:** Physics disabled during user control, restored on release

**Controls:**

-   **Click:** Spawn ball at mouse position
-   **Click + Drag:** Move existing ball (becomes kinematic)
-   **Release:** Drop ball (restores dynamic physics)
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

**Purpose:** Demonstrates the `Tilemap` and `TilemapRenderer` systems with texture-based tile
rendering, procedural generation, and viewport culling.

**Features:**

-   **Tilemap System Usage:** Uses `MicroEngine.Core.Graphics.Tilemap` for grid management
-   **Sprite Batch Rendering:** Demonstrates `SpriteBatch` for efficient tile rendering
-   **Texture-Based Tiles:** Loads individual tile textures (grass, water, dirt, stone)
-   **Procedural Generation:** Random tilemap using noise-based algorithm
-   **Viewport Culling:** Only renders tiles visible to camera (performance optimization)
-   **Grid Size:** 25x19 tiles (800x608 pixels at 32px/tile)
-   **Camera Movement:** Smooth scrolling with WASD/Arrows
-   **Tile Types:** Grass (60%), Water (15%), Dirt (15%), Stone (10%)
-   **Legend Display:** Color-coded tile type reference
-   **Regeneration:** SPACE to generate new random tilemap

**Tile Types & Assets:**

-   **Grass (ID: 1):** `tile_grass.png` - RGB(80, 160, 60) Green
-   **Water (ID: 2):** `tile_water.png` - RGB(50, 100, 200) Blue
-   **Dirt (ID: 3):** `tile_dirt.png` - RGB(140, 90, 50) Brown
-   **Stone (ID: 4):** `tile_stone.png` - RGB(120, 120, 130) Gray

**Technical Details:**

-   **Tilemap Class:** Manages tile grid, coordinate conversion (world ↔ tile)
-   **Culling:** Uses `Camera2D.GetVisibleBounds()` and `Tilemap.WorldToTile()`
-   **SpriteBatch:** Deferred rendering mode with batched draw calls
-   **Tile Size:** 32x32 pixels (configurable constant)
-   **Empty Tiles:** ID 0 (skipped during rendering)
-   **World Coordinates:** Tiles positioned using `Tilemap.TileToWorld()`

**Key Demonstrated Concepts:**

1. **Tilemap API:** `SetTile()`, `GetTile()`, `WorldToTile()`, `TileToWorld()`
2. **Viewport Culling:** Calculates visible tile bounds to avoid rendering off-screen tiles
3. **SpriteBatch Integration:** Begin/Draw/End pattern for efficient rendering
4. **Resource Management:** Texture loading and caching via `ResourceCache<ITexture>`

**Controls:**

-   **WASD/Arrows:** Move camera (200 units/sec)
-   **SPACE:** Regenerate tilemap procedurally
-   **R:** Reset camera to origin (0, 0)
-   **ESC:** Exit to menu

---

---

### 6. Audio Demo (`[6]`)

**Purpose:** Demonstrates real audio system with music streaming and sound effect playback.

**Features:**

-   **Music Playback:** Real streaming music with play/pause/resume controls
-   **Music Volume:** 0-100% control with up/down arrows
-   **Sound Effects:** 3 real sounds (Jump, Collect, Hit)
-   **Volume Bars:** Visual feedback for music and SFX levels
-   **Playback Status:** "Playing" / "Stopped" indicator
-   **Sound Feedback:** 0.5-second visual flash on SFX trigger
-   **Resource Loading:** Uses `ResourceCache<IAudioClip>` for audio management
-   **Streaming Audio:** OGG format for music (efficient streaming)
-   **Sound Effects:** WAV format for instant playback

**Audio Files Required:**

-   **Background Music:** `assets/audio/music/background.ogg`
-   **Jump (J):** `assets/audio/sfx/jump.wav`
-   **Collect (C):** `assets/audio/sfx/collect.wav`
-   **Hit (H):** `assets/audio/sfx/hit.wav`

**Controls:**

-   **SPACE:** Toggle music play/pause
-   **↑/↓:** Adjust music volume (±10%)
-   **←/→:** Adjust SFX volume (±10%)
-   **J:** Play jump sound
-   **C:** Play collect sound
-   **H:** Play hit sound
-   **ESC:** Exit to menu

---

### 7. Save/Load Demo (`[7]`)

**Purpose:** Demonstrates the `SavegameManager` system for persisting game state.

**Features:**

-   **Entity Persistence:** Saves/loads positions of 5 draggable entities
-   **Metadata Handling:** Displays save timestamp and entity count
-   **Visual Feedback:** "Saved!" and "Loaded!" notifications
-   **Drag & Drop:** Interactive entity positioning (centered drag)
-   **File Management:** Uses JSON serialization to `saves/demo_save.json`

**Controls:**

-   **Click + Drag:** Move entities
-   **S:** Save current state
-   **L:** Load saved state
-   **D:** Delete save file
-   **ESC:** Exit to menu

---

### 8. Event System Demo (`[8]`)

**Purpose:** Showcases the decoupled `EventBus` architecture for entity communication.

**Features:**

-   **Event Chain:** Button -> Trigger -> Target interaction flow
-   **Custom Events:** `ButtonPressed`, `TriggerEntered`, `TargetActivated`
-   **Visual Logging:** Real-time log of event flow and payload data
-   **Event Statistics:** Tracks total events processed
-   **Decoupling:** Entities communicate without direct references

**Controls:**

-   **Click Button:** Fires `ButtonPressed` event
-   **Hover Trigger:** Fires `TriggerEntered` event
-   **C:** Clear event log
-   **ESC:** Exit to menu

---

### 9. Collision Filtering Demo (`[9]`)

**Purpose:** Demonstrates advanced physics collision layers and filtering rules.

**Features:**

-   **Platformer Physics:** Gravity, jumping, and solid collisions
-   **Collision Layers:** Player, Enemy, Obstacle, Ground
-   **Filtering Rules:**
    -   **Player:** Collides with everything
    -   **Enemies:** Collide with Player/Obstacles, **Pass through each other**
-   **Visual Proof:** Enemies spawn moving towards each other and cross paths
-   **Collision Log:** Real-time reporting of collision events

**Controls:**

-   **Left/Right:** Move Player
-   **SPACE:** Jump
-   **C:** Clear collision log
-   **ESC:** Exit to menu

---

### 10. Spatial Audio Demo (`[0]`)

**Purpose:** Verifies 3D positional audio with distance-based attenuation.

**Features:**

-   **3D Sound Sources:** 3 distinct emitters at different positions
-   **Distance Attenuation:** Volume drops as player moves away
-   **Continuous Looping:** Independent loop timers for each source
-   **Visual Indicators:** Source positions and player listener range
-   **Listener Update:** Updates audio listener position to match player

**Controls:**

-   **WASD/Arrows:** Move listener (Player)
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
| Audio      | 0            | 0       | Real audio playback, no entities                 |

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
    └── AudioDemo.cs          # 320 lines - Real audio playback
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

-   ~~**Audio Integration:** Connect AudioDemo to real audio backend~~ ✅ COMPLETE
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

MicroEngine provides **10 comprehensive, interactive demos** showcasing the engine's core capabilities:

1. **ECS Architecture** - Modern entity patterns with interactive gameplay
2. **Graphics Pipeline** - Camera system, textures, filtering, caching
3. **Physics Simulation** - CCD collision with realistic bouncing
4. **Input Handling** - Multi-device input visualization
5. **Tile Rendering** - Procedural generation with camera scrolling
6. **Audio Preview** - Simulated audio controls and UI design
7. **Save/Load System** - Entity persistence and state management
8. **Event System** - Decoupled entity communication
9. **Collision Filtering** - Complex physics interaction rules
10. **Spatial Audio** - 3D positional sound and attenuation

All demos are **fully functional**, **well-documented**, and **ready for testing**. They demonstrate best practices for scene management, entity patterns, and game systems architecture.

---

**Version:** 0.13.0
**Last Updated:** 2025-11-20
**Status:** All demos complete and tested ✅

# DEMO-MODERN-1: Courtyard Key on the current SDK

Part of DEMO-MODERN-1 (issue #182, CONSOLIDATION-1 T14). Branch `feat/demo-courtyard`. Constraint: nothing a player
sees or a script records changes. Test runner: `dotnet test tests/CourtyardKey.Tests -c Release`.

## Result

Two pieces of private infrastructure were replaced, with captures and state traces byte-identical. Every other
candidate was measured against the engine's capability and kept, because the capability gives a different result
(the numbers are below) or because the code is the game's own rule or content. Courtyard Key was already on the
scene world, the world's clock, coroutines, `SaveSchema`, `ActionPrompts`, the mixer and the achievements module
before this pass.

## Decisions

| Private infrastructure | Where | Decision |
| --- | --- | --- |
| A save folder as a constructor parameter, `CreateSaves` override, `COURTYARD_SAVE_DIR`, temporary folders in tests | `CourtyardKeyGame.cs`, `Program.cs`, six test files | **Replaced** with the run's own store: `Game.Saves` as the run decides it, `InitializeForTest(platform, saves:)` with a `MemorySaveStorage` in tests, `--saves DIR` for a run |
| Hand-typed crop rectangles, cell size and anchors of the pack images | `Visuals/PackArt.cs`, `Visuals/MonsterArt.cs` | **Replaced** with `Content/pack.sheet` read by `Assets.Sheet`; the code keeps the repainting |
| Hand-written save version | `Model/Progress.cs` | Already `SaveSchema<Progress>` version 1 (SAVE-2). Nothing to do; five saves written before this pass are now fixtures |
| Hero, Warden and enemy states: an enum, a float timer, a `switch` | `Entities/Hero.cs`, `Boss.cs`, `Enemies.cs` | **Kept.** `StateMachine` measures the time in a state as the difference of a `double` clock; the game adds `dt` to a `float`. At 60 Hz the two cross 9 of the Warden's 12 thresholds and 1 of the hero's 8 on a different step (table below), and the time's bits differ in 184 of 199 steps. Phases would last one step more or less |
| `PlayMode` (play, scroll, cutscene) | `Scenes/PlayScene.cs` | **Kept**: a flag set from coroutines and read by one three-line `switch`; no timer, no transition rule to declare |
| Movement through tiles and solid actors over the static `TileMotion` | `PlayScene.MoveActor`, `World/Room.cs` (`RoomGrid`) | **Kept.** `TopDownController2D` on the tile world gives positions that differ in 1704 of 6000 steps (at most 0.00037 px) for the same walk, and its contacts name the sides the box touches, not the side that stopped it (differs in 1077 steps), which the push rule reads. Gaps 2 to 4 |
| Enemy chase (knight, slime, bat, Warden stalk) | `Entities/Enemies.cs`, `Boss.cs` | **Kept**: the game's rule. A `NavAgent` sent to the hero shares no position with the knight in 900 steps (worst 25.9 px apart), walks 288 px where the knight walks 431 px and arrives 15 steps earlier. The knight's slow re-aim along one axis is the opening the player uses to get behind its shield |
| Draw order: a list sorted by feet Y with `List.Sort` | `Scenes/PlayScene.Draw.cs` | **Kept, owner's call.** `SortedRenderer` keeps call order among equal depths; `List.Sort` does not. Over the test suite and the scripts the two orders differ in 6701 of 22711 steps, and in 1369 of them the actors that change places are within reach of each other, so which one is on top could change |
| Heartbeat and ambience timers (`t -= dt; if (t <= 0) t = period`) | `Scenes/PlayScene.cs` | **Kept.** They drop the remainder when they fire (ambience every 11 steps for 0.18 s); a repeating timer that keeps it fires on other steps, and each firing draws from the room's generator, which also rolls loot |
| HUD toast, banner, room tag and flash timers | `UI/Hud.cs` | **Kept**: each is a fade envelope read every frame (`min(t, length - t)`), not an action to run later |
| Screen fades as coroutine loops | `Scenes/PlayScene.cs` | **Kept.** Cutscenes wait for them (`yield return FadeOut(0.45f)`); a tween has no handle to wait on. Gap 6 |
| Cutscenes, item-get, death and boss sequences | `PlayScene.Events.cs`, `PlayScene.Combat.cs` | Already coroutines with `Wait.Seconds` and `Wait.Until` |
| Hitstop, camera shake, particles, light map | `Scenes/PlayScene.cs` | Already the engine's (`Objects.Clock.Freeze`, `Camera2D`, `ParticleSystem`, `LightMap`) |
| `RoomActors<T>`: the actors of a room, typed, without allocating | `Scenes/PlayScene.cs` (55 lines) | **Gap 1.** Kept |
| `CachedPrompt`, `NumberText` | `UI/TextCache.cs` (48 lines) | **Gap 5.** Kept |
| `DialogueBox` (typewriter, pages, name tag) and the menus over `DemoKit.OptionList` | `UI/DialogueBox.cs`, `Scenes/*.cs`, `games/Shared` | **Kept.** Both say they are the game's copy of what the engine used to ship, drawn on the 320x180 canvas inside the game's own frames. Widgets lay themselves out in the UI view with their own look; not built to compare. `OptionList` and `Typewriter` are in `games/Shared`, outside this work |
| Strings in code | room definitions, scenes, HUD (about 90 texts) | **Kept.** Signs, names and talk trees are part of each room's definition beside its map (code-first content, ADR 0002). Loading a table adds `engine.localization` to every step's hash, and the game has one language |
| Sounds and music | `Audio/*.cs` | Already the mixer (`Play`, `PlayMusic`, `StopMusic` with a fade, `PauseMusic`). The banks are the game's content, synthesized. `Scene.Sounds` would pan and fade sounds by distance: a change of what is heard |
| Frame choice by `(int)(time * fps) % n` | actors' `Draw` | **Kept**: one line each over frames drawn in code; clips are for frames described in a file |
| The play scene hashes the hero and the actors itself | `PlayScene.HashState` | **Kept.** The scene world already hashes attachments that are `IStateHashed`, so `play.hero` and `play.actors` repeat it. Removing them changes every trace and a test that names the sections |

## Lines of code

| Area | Before | After |
| --- | --- | --- |
| `games/CourtyardKey` (C#) | 8544 | 8528 |
| `CourtyardKeyGame.cs` + `Program.cs` | 180 | 174 |
| `PackArt.cs` + `MonsterArt.cs` | 182 | 169 (plus `pack.sheet`, 14 lines of data) |
| `tests/CourtyardKey.Tests` (C#) | 1676 | 1682 (36 fewer for folders, 42 more for the fixture test) |

## Measurements

Thresholds at 60 Hz: the first step on which the time in a state is over the threshold, for the game's `float`
accumulator and for `StateMachine.Time`.

| Threshold (s) | Game | Machine | | Threshold (s) | Game | Machine |
| --- | --- | --- | --- | --- | --- | --- |
| 0.2 (hero, swoosh) | 13 | 12 | | 0.7 | 43 | 42 |
| 0.5 | 30 | 31 | | 0.75 | 46 | 45 |
| 0.6 | 36 | 37 | | 0.9 | 55 | 54 |
| 1.4 | 85 | 84 | | 1.6 | 97 | 96 |
| 1.8 | 109 | 108 | | 0.02, 0.05, 0.08, 0.16, 0.18, 0.19, 0.26, 0.45, 1.1, 1.2, 2.4 | same | same |

Hero walk, 6000 steps of seeded directions in the road room, tiles only: `TileMotion.Move` against
`TopDownController2D` on `TilePhysicsWorld2D` with the same grid. Free running, 1704 steps differ, worst 0.00037 px;
put back on the game's position before every step, 37 differ, worst 0.00006 px. No player would see it; the state
hash would, and so would a rounding that falls on the other side one day.

Knight against agent, Trial of Wind, 900 steps: 0 identical positions; the agent walks the diagonal.

## Log

- L1: The first baseline run gave two captures of `shop.script` that four later runs did not repeat: the item slot's
  key label was "?" instead of "Z". The state traces were identical. A `--hidden` scripted run shows the prompts of
  the device that last sent input, and something on this machine other than the script was read as input in that one
  run. Not reproduced since (five full runs). The baseline is the repeated one; the odd run is kept beside it.
- L2: Save proof. The build before this pass wrote five saves with `--saves` (a new game with the sword, the village,
  two dungeon rooms, the boss room). Each was continued by a script with the old build and with the new one: captures,
  state traces and the save written back are byte-identical (28 files). The five files are
  `tests/CourtyardKey.Tests/Fixtures/saves`; a test loads each, compares every property and writes it back unchanged.
- L3: `packaging/verify.mjs`, `packaging/linux-distros.mjs`, `web/tests/desktop.mjs` and `media/tools/capture-footage.mjs`
  still set `COURTYARD_SAVE_DIR`, and `docs/reference/configuration.md` still lists it. The game no longer reads it.
  Their runs have `--frames` or a script, so they keep their own saves in memory and never reach the player's; the
  variable and the temporary folders they make are now unused. Not edited: outside this work's files.
- L4: The scratch tests behind the measurements were not committed (they need a reference to
  `MicroEngine.Navigation`, which the game does not use).

## Engine gaps

1. The actors of a room are the attachments of the children of the room's entity. `IWorld` gives `Children(parent)`
   (ids) and `All<T>()` (every attachment of a type), not the two together, so the game wrote `RoomActors<T>`, a
   55-line enumerator: `foreach (var enemy in Objects.Children<Enemy>(room))` is what it stands for.
2. `MoveResult.Contacts` lists what the box touches where it ends. A push rule needs "my movement was stopped on the
   side I face this step", which `TileMoveResult.HitLeft` and the others say and the contract does not.
3. A top-down game with walkers and flyers needs two views of the same tiles (water blocks a walker, a bush does not
   block a bat). `AddTiles` takes one grid; the game would add the tiles twice in two layers and keep both in step.
4. `Scene.Physics` writes a body's position, the centre of its box, to the owner's transform. A top-down actor's
   position is its feet (draw order, hitboxes around it); the game would turn `SyncTransforms` off and convert.
5. `ActionPrompts.Format` builds a string on every call and `BitmapFont` draws only strings, so a hint or a counter
   drawn every frame needs a cache keyed on `Prompts.Version` or on the number (`CachedPrompt`, `NumberText`).
6. A coroutine cannot wait for a tween. A cutscene that fades and then goes on writes the fade as its own loop.
7. Not a gap, a property to know: a game whose timers are `t += dt` in a `float` cannot move to `StateMachine` and
   keep its step counts. The machine's are the regular ones (`After(0.6f)` is 36 steps at 60 Hz).

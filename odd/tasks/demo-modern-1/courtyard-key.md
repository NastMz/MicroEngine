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

## Second pass (after DEMO-GAPS-1, issue #218)

Branch `feat/demo2-courtyard`. Every row above that was kept or reported as a gap was decided again against the
engine as it is now. Rule of the pass: captures byte-identical; a state trace may change where state moves into an
engine piece, with proof that the game does the same things on the same updates.

### Decisions

| Row | Decision now |
| --- | --- |
| `RoomActors<T>` (gap 1) | **Replaced** with `Objects.Children<Actor>(room)`. 55 lines gone. A test pins the order of the walk (written against the old code first). Actors are attached as `Actor`, so a kind is picked with `is`; a room that is not there (none is leaving) needs a guard, see gap C |
| `CachedPrompt`, `NumberText` (gap 5) | **Replaced** with `font.Draw(r, $"…")` and `Prompts.Cached`; `UI/TextCache.cs` deleted, and the hand-kept status texts of the pause screen and the ending with it |
| Draw order | **Replaced** with `SortedRenderer` (shadows in a layer under the actors, actors at the Y of their feet). Actors at the same depth are now drawn in a stable order: the leaving room, the room, the hero, each in the order of the walk. No picture changed: 71 captures, 1,730 taken every seven updates and 2,955 taken on every update of five scripts are byte-identical |
| Screen fades (gap 6) | **Replaced** with tweens the cutscenes yield. The opening fade is started from a one-line coroutine: a tween added in `Enter` runs one update earlier than the old coroutine did (65 of 1,730 captures differed) |
| Hero's states | **Replaced** with `StateMachine<HeroState>`: `After(SwingTime)`, `After(ThrowTime)`, the walk and the knockback as state updates. Its thresholds agree with the engine's rule at 60 updates a second (table below), so seconds stay seconds |
| Warden's states | **Kept.** Six of its thresholds are `t > seconds` on a float sum and fall one update after the engine's rule at 60 Hz (table below), so only `AfterSteps` with counts measured at 60 Hz lands on the same updates, and then its phases are counts of updates: the game's times hold in seconds at any update rate (its tests run at 30, 60 and 120). Its ways out also choose their target (the attack rotation) or do something on the way (the volley), which a declared way out does not; and the leap moves by `time / LeapTime`, whose bits differ between the machine's time and the float sum |
| Slime, bat, spitter, knight | **Kept**: none has states; each is one or two countdowns of a random length drawn from the room's generator when it starts, read by the draw for the telegraph |
| Movement over `TileMotion` | **Kept, and no longer because of a difference.** The differential is zero: 13,513 moves of two whole playthroughs repeated on `TilePhysicsWorld2D` (origin at the box's top-left corner) end on the same bits, and `StoppedGoing` names the same sides (`MoverDifferentialTests`, committed). Not replaced because it would add code: `TileMotion.Move(box, delta, grid, solids)` is the engine's own function and takes the solid actors as a list gathered per move; on the world each solid actor (a door while closed, a pot until it breaks, a block that slides) needs a body kept in step with a solidity the game computes. Gap A |
| Chase | **Kept** (the game's rule), as instructed |
| Heartbeat and ambience timers | **Kept.** The heartbeat counts only while the hero has one heart or less and keeps what it counted; a timer has no pause, and its first beat is at once. The ambience changes its period with the room while it runs. Their periods at 60 Hz: 0.9 s is 55 updates (rule 54), 0.2 is 13 (12), 0.25 is 16 (15), 1 is 61 (60); 0.18, 0.22, 0.35 agree |
| HUD toast, banner, room tag, flash | **Kept**: envelopes read every frame; the room tag's time stops while an achievement banner shows, and a tween has no pause |
| `PlayMode` | **Kept.** `Go` from one of a machine's own handlers waits until the handler returns; the play update sets the mode and reads it again in the same update (`if (Mode == PlayMode.Play) CheckExits()`) |
| Menus over `DemoKit.OptionList` | **Replaced** with `MenuCursor` and the rows drawn by the game (`UiStyle.Menu`, 12 lines). Not widgets: the shop's sold-out row keeps the cursor on it, which a `MenuCursor` does and the widgets' focus does not |
| `DialogueBox` over `DemoKit.Typewriter` | **Replaced** the typewriter with `TextReveal`. The box itself stays drawn by the game (panel, name tag, arrow at its own offsets): as widgets it would be the same drawing inside three skins. Not built as widgets to compare |
| Strings, sounds, frame choice, `Effect`, the scene's own hash of hero and actors | Not tried in this pass (`Scene.Effects` for the puffs would take them out of the room's actors and of the draw order) |

### Lines of code

| Area | First pass | Now |
| --- | --- | --- |
| `games/CourtyardKey` (C#) | 8528 | 8440 |
| `Scenes/PlayScene.cs` | 363 | 322 |
| `UI/TextCache.cs` | 48 | deleted |
| `Scenes/MenuScenes.cs` + `TitleScene.cs` | 564 | 544 |
| `Entities/Hero.cs` | 273 | 264 |
| `Scenes/PlayScene.Draw.cs` | 90 | 105 (the lights walk the actors themselves) |
| `UI/UiStyle.cs` | 43 | 59 (the menu rows the shared list drew) |
| `tests/CourtyardKey.Tests` (C#) | 1682 | 1832 (order of the walk, hero timeline, mover differential) |

### Measurements

The game's float timers at 60 updates a second against the engine's rule (`ceil(seconds x 60)`), computed in single
precision as the game adds them:

| Timer | Game | Rule | | Timer | Game | Rule |
| --- | --- | --- | --- | --- | --- | --- |
| `t > 1.6` (Warden) | 97 | 96 | | `t >= 0.26` (hero swing) | 16 | 16 |
| `t > 1.4` | 85 | 84 | | `t >= 0.18` (hero throw) | 11 | 11 |
| `t > 0.9` | 55 | 54 | | `t < 0.08`, `0.02 .. 0.19` (hero) | same | same |
| `t > 0.7` | 43 | 42 | | `t > 0.5`, `0.45`, `0.6`, `0.05` (Warden) | 30, 27, 36, 3 | same |
| `t > 2.4` | 145 | 144 | | daze from 0.9 to 1.8 / 2.4 | 55 / 91 | `SetSteps(54)` of 109 / 145 |
| `t > 1.8` | 109 | 108 | | | | |

Hero timeline pinned by `HeroTimingTests` (old code first, unchanged after): swing on update 5, blade from 7 to 17,
walking again at 21; throw 60 to 71; hit 120 to 135; fall at 200, game over at 309.

### State traces

Baseline: 17 runs of `main` (the twelve scripts and five fixture saves continued from the title), twice, identical.

- Fades as tweens: 15 runs differ, only in the engine's `scene[0]:PlayScene` section and only during the 30 updates
  of the opening fade.
- Hero on a machine: the same 15 runs differ, in `scene[0]:PlayScene` and `play.hero`, on every update the hero
  exists.
- In both, `play.progress`, `play.room`, `play.actors`, `engine.input`, `engine.audio` and `engine.random` are
  identical on every update, the 17 sound captures are byte-identical, and the five saves are written back identical.
- `ending` and `look` never reach a play scene and are identical throughout.

### What of `games/Shared` the game still uses

`AchievementBanners` (the game's banner over the HUD) and `AchievementList` (the title's achievements screen). Both
draw the achievements module; the SDK has no widget for either. `OptionList` and `Typewriter` are no longer used.

### Log

- L5: `cmp.sh` first compared against a relative path and reported every file as different; fixed before any decision
  was taken on it.
- L6: A background capture run was in progress while a later step was built; every comparison quoted above was run
  again on the final commit.
- L7: The save runs: each of the five fixture saves is copied to a folder, continued from the title by a script
  (walk, swing, throw, pause, change rooms) with `--saves`, on `main` and on this branch: 35 captures and the five
  saves written back are byte-identical.
- L8: Two commit messages (draw order, hero) say 1,755 captures every seven updates: that count is 1,730 captures
  plus the 25 traces and saves of the same runs.

### Engine gaps that remain

A. A developer making a top-down adventure whose obstacles are plain objects with a box of their own (a door while it
   is closed, a pot until it breaks, a block that slides) has to keep a static body per object in step by hand
   (create it, move it, switch it off, destroy it) to move on the physics world, because `IPhysicsWorld2D.Move` takes
   no solids for one move and a shape cannot follow an entity's own box and predicate. `TileMotion.Move` takes them as
   a list, which is why the game stays on it with the differential at zero.
B. A developer moving a hand-kept `t += dt; if (t > seconds)` timer to a `StateMachine` or `Timers` and keeping
   recorded runs has to measure each count at one update rate and write `AfterSteps(97)`, because `After(seconds)`
   has one rule and there is no form that counts as the float sum did at whatever rate the game runs. For a new game
   nothing is missing.
C. `Objects.Children<T>(EntityId.None)` and of a destroyed entity throws, so a group that may not exist (the room
   scrolling away) needs `IsAlive` before every walk; the enumerable has no count and no first, so a hash writes
   the count with a loop. An empty walk for no parent would remove the guards.
D. A tween added in `Scene.Enter` advances one update before a coroutine started there does, so a scene that opens
   with a fade and must keep its frames starts the tween from a coroutine. A `delay` in updates would say it.
E. A timer cannot be paused (the heartbeat that counts only while a condition holds), and a tween neither (the room
   tag that waits for an achievement banner).
F. A machine's `Go` from its own handler is deferred, so a mode that is set and read again in the same update
   cannot be a machine without reordering the game's update.

# DEMO-MODERN-1: Neon Rift on the current SDK

Part of issue #182 (CONSOLIDATION-1, T14). Branch `feat/demo-neon`, based on `824fcf44`. Constraint: nothing a player
sees or a script records changes. Test runner: `dotnet test tests/NeonRift.Tests -c Release`.

Neon Rift was already on several contracts before this pass: its actors are entities of the play scene's world
(`Attachments<T>`), its sprites come from `Content/sprites.sheet` (CONTENT-1), its record is a `SaveSchema`, its
controls panel reads `ActionPrompts`, its camera shake is `Camera2D.Shake`, its tiles and parallax are the engine's.

## Decisions

| Private infrastructure | Where | Decision |
| --- | --- | --- |
| `saveDirectory` constructor parameter, `CreateSaves` override, `RIFT_SAVE_DIR`, temporary folders in six test classes | `RiftGame.cs`, `Program.cs`, tests | **Replaced** with the run's own store: tests pass a `MemorySaveStorage` to `InitializeForTest` where a restart must find the save; `--saves DIR` on the command line |
| Frame rates typed in code (shots 18, enemy bullet 12, explosions 18 and 14, impact sparks 20/24/28, muzzle flash 40, somersault 16, credits run 10, run cycle 12 over 8 frames) | `Actor.cs`, `Stage.cs`, `Weapons.cs`, `Projectiles.cs`, `Player.cs`, `MenuScenes.cs` | **Replaced** with clips of `sprites.sheet` (`fps`, `loop: false`, three entries under `clips`); the code names them through `GameContent` |
| The hero's cell size and anchor typed a second time, and the hero images decoded and cropped a second time for the arm rigs | `Art/Assets.cs` | **Replaced**: the rigs are cut from `SpriteSheet.Frames` (image and origin as the sheet gives them) |
| Content paths as strings (backdrops, sounds, music) | `Art/Assets.cs`, `Art/Sounds.cs` | **Replaced** with the constants of `GameContent.g.cs` |
| `microengine content check` | `Content/` | Passes: 85 files, 0 errors, 0 warnings; `content names --check` up to date |
| Stage phase, player life state, boss patterns, egg pod cycle (enums with float timers) | `Stage.cs`, `Player.cs`, `Bosses.cs`, `Enemies.cs` | **Kept**. `StateMachine.Time` is a difference of a double clock and `After` has an epsilon; the game's timers add a float step. At 60 updates a second 10 of 16 thresholds the game uses land one update apart (table below), so the boss would arrive an update earlier. Not identical |
| `Stage.Schedule` (a list of times and actions, 12 lines) | `Stage.cs` | **Kept, gap 1 and 2**. `SceneTimers` cannot be created or stepped outside a scene, and the stage is a rule object tests step with a bare `SceneWorld`; and its countdown fires up to one update before the stage's absolute comparison (7 of the 21 delays the game uses, depending on the stage clock) |
| `Body` (gravity, `TileMotion.Move`, grounded) for Vela, troopers and capsules; Vela's jump, hop, drop-through | `Actor.cs`, `Player.cs` | **Kept**. A probe drove `Body` and a `CharacterController2D` on a `TilePhysicsWorld2D` over stage 1 with the same intent for 900 updates: no grounded mismatch, worst distance 0.00098 px, but the bits differ from the first jump (one unit in the last place of X). Positions are hashed, so the trace changes. Feasible if a sub-pixel change is accepted; gaps 3 and 4 are what the game would still write |
| `Ui.Scanlines` (135 rectangles a frame) | `Scenes/Ui.cs` | **Kept, gap 5**. The engine's `Scanlines` darkens the odd rows, the game's the even rows: with `Strength = 0.07` on the title, all 1,166,400 pixels of the capture differ (up to 17/255 per channel). No phase setting |
| Hit flash (additive second draw), warning band, neon glow of titles | `Actor.cs`, `StageView.cs`, `Ui.cs` | **Kept**: per-sprite and per-text drawing, not full-screen passes |
| Draw order of a stage (pickups, bosses, enemies, player, shots, effects) | `StageView.cs` | **Kept**: six fixed layers in six lines, nothing is sorted; `SortedRenderer` would add keys to state the same order |
| Shots against enemies: a walk over the living enemies with a reused buffer | `Projectiles.cs` | **Kept**: at most a few dozen enemies, and hits must happen in spawn order (the trace depends on it); `SpatialIndex2D` answers in its own order |
| Actors as classes with a virtual `Update` over the scene world | `Gameplay/` | **Kept**: it is the scene world, the engine's default model. `MicroEngine.Ecs` walks in the store's order, not spawn order, and its evaluation found effects that come and go dearer there |
| Music and sounds | `RiftGame.cs`, `Sounds.cs` | **Kept**: already the mixer's `PlayMusic`/`StopMusic` fades and `PauseMusic`; `Sfx` and `Music` only record missing names for a test. A crossfade or a muffled pause would change what is heard |
| `DemoKit.OptionList` and `Ui.Menu` (cursor menus drawn by hand) | `games/Shared`, `Scenes/Ui.cs` | **Kept, reported**: the kit is outside this game's surface (gap 6) |
| `NumberText` (formats a HUD number only when it changes) | `Scenes/Ui.cs` | **Kept**: 12 lines, no engine counterpart found |
| Strings in code | scenes | **Kept**: one language, no table would change what is drawn; not attempted |
| `Autopilot`, `StageBuilder`, `Tiles.For`, weapon table, hit boxes | | **Kept**: the game's rules and data. Hit boxes stay in code: a `.sheet` has no rectangles or points beside the anchor (gap 7) |

Thresholds, float accumulation of 1/60 against the machine's clock (first update at which the comparison holds):
2.6 → 157/156, 1.3 → 79/78, 2.4 → 145/144, 3.2 → 193/192, 1.0 → 61/60, 2.8 → 169/168, 4 → 241/240, 2.2 → 133/132,
1.2 (≥) → 73/72; equal for 5.5, 0.6, 0.35, 0.2, 0.7 and 1.2 (>).

## Engine gaps

1. Scene timers, machines and systems live only in a `Scene` on a game's stack (`SceneTimers.Update` is protected and
   reads `Scene.EngineStep`). A game whose rules are a plain object stepped in tests writes its own scheduler:
   `Stage.Schedule(0.9f, () => Sound("alarm", 0.7f))` over `List<(float At, Action Action)>`.
2. Moving a float timer to `StateMachine.After` or `Timers.After` moves the event by one update for many durations;
   nothing in the pages says so, and a game with recorded traces cannot adopt them without re-recording.
3. `CharacterMotion` shortens a released jump with a gravity scale only. Vela's hop cuts the upward speed to 45 % at
   once (`if (!held && v.Y < -JumpSpeed * 0.45f) v.Y = -JumpSpeed * 0.45f`); the game would write `Velocity` before
   each step.
4. `CharacterController2D.Teleport` clears `IsGrounded`, so keeping a character inside the camera's view each step
   (`Math.Clamp(pos.X, view.Left + 8, view.Right - 8)`) must go through `World.SetPosition` behind the controller's back.
5. `Scanlines` has no phase: a game whose lines start on row 0 cannot use it.
6. `MenuItem` draws the theme's panel or a nine-slice; a row drawn as a tinted bar, an underline and a bobbing cursor
   needs a control of the game's own, so the demos keep `DemoKit.OptionList`.
7. A `.sheet` frame has an anchor and nothing else: no named point or rectangle. Neon Rift finds the shoulder and the
   gun tip by scanning pixels at load (`Assets.AddRig`) and keeps every hit box in code.
8. `microengine content names` lists a strip `PropTank = "prop_tank-"` the sheet does not declare (three single
   sprites `prop_tank-1..3`), and listed no strip for `hit1`..`hit5` before they had a rate.

## Lines of game code (C#, generated file left out)

| Area | Before | After |
| --- | --- | --- |
| `Program.cs`, `RiftGame.cs` | 206 | 203 |
| `Art/` | 244 | 258 |
| `Gameplay/` | 2216 | 2221 |
| `Levels/` | 238 | 238 |
| `Scenes/` | 720 | 720 |
| `tests/NeonRift.Tests` | 1242 | 1223 |
| `Content/sprites.sheet` | 72 | 82 |

The code did not get shorter: the numbers moved to the sheet and the strings became constants, and what would have
removed code (machines, timers, the character controller) fails the identity constraint.

## Verification

- Baseline before any change: the seven scripts with their environments, `--hidden --mute --captures --state-trace
  --audio-capture`: 67 captures, 7 traces with section hashes, 7 WAV files. Two runs of the baseline are identical.
- Eleven more scenarios (the other weapons, stages 1 and 3 on the autopilot, mortal runs, the card and game over
  starts) recorded from a build of `824fcf44` in a scratch copy: 236 files.
- After each commit: 81 of 81 and 236 of 236 byte-identical.
- `dotnet test tests/NeonRift.Tests -c Release`: 73 passed, 0 failed, before and after; `dotnet build MicroEngine.slnx
  -c Release`: 0 warnings.

## Log

- L1 One run of `menus.script` drew `WA?D` for `WASD` in `controls.png` (kept in the scratch folder as
  `step2-flake-controls`); the next five runs matched the baseline. The label of a key comes from the system's
  keyboard layout while the run draws, and seven runs and other agents shared the desktop. Cause not established.
- L2 A run with `--audio-capture` and no `--captures` writes its captures into `captures/` of the working directory.
- L3 `RIFT_SAVE_DIR` is still passed by `packaging/verify.mjs`, `packaging/linux-distros.mjs`, `web/tests/desktop.mjs`,
  `media/tools/capture-footage.mjs` and two experiments, and listed in `docs/reference/configuration.md`. It now does
  nothing; every one of those runs has a script or `--frames`, so it keeps its saves in memory. Not edited (outside
  the surface).
- L4 A stage without art (tests only) plays an effect as one frame at 16 a second; before, at the rate its caller
  passed. Two tests of `DrawBlendTests` changed with it.

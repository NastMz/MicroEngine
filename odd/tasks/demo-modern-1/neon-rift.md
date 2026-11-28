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

## Second pass (2026-11-27, after DEMO-GAPS-1 #218)

Branch `feat/demo2-neon`, based on `debaa4a7`. Every row above that was kept or reported as a gap was decided again
with the engine as it is now. Constraint: captures byte-identical to `main`; a trace may change only with proof that
the game does the same things on the same updates.

### Decisions

| Row | Decision now | Evidence |
| --- | --- | --- |
| Shoulder and gun-tip pixel scan; hit boxes in code | **Replaced.** `Content/sprites.sheet` holds the points `shoulder` and `tip` of the nine gun poses and the boxes `hit` (seven enemy sheets, three bosses; `hit_carrier` on the skiff) and `hurt` (three hero poses). `Assets.AddRig` cuts the arm from the points; actors read `Sprite.Box` when they join. The stage takes the sheet's `SpriteAtlas`, so the optional `Rigs` and `Strips` lookups, the `Strip` record and their no-art fallbacks are gone. Still in code: shots, capsule, the hull turret (not frames of the sheet) and the movers' collision boxes | 339 of 339 files identical, traces included; `content check`: 85 files, 0 errors, 0 warnings; `content names --check`: up to date |
| `DemoKit.OptionList` | **Replaced** with `MenuCursor` in the three menus (same wrap, same `Moved`, confirm consumed). The row (bar, underline, bobbing cursor) stays `Ui.Menu`, 14 lines: a `ControlSkin` on a canvas widget would hold the same lines, and its bob would follow the focus time, not the scene's age the row bobs on today. Not attempted | 339 of 339 identical, traces included |
| `Stage.Schedule` | **Kept.** Two things stop `Timers`, measured below | pinned runs and audio captures differ |
| Stage phase, player state, boss patterns, egg cycle | **Kept.** The exits are countable now (`AfterSteps` 157, 79, 36...), but the time in the state is also read as a number, and that number is the float sum | table below |
| `Body` and Vela's movement | **Kept.** The differential is 113 of 25,849 steps apart, not zero | `ControllerDifferentialTests` |
| Explosions, sparks, muzzle flashes | **Kept, gap.** `SpriteEffects` can be owned and stepped by the stage (a plain class, `Update(dt)`, `Draw`), and it counts a frame as `(int)(time * rate)` on a float sum as `Effect` does. It has no scale: four of the seven effect kinds are drawn at 0.5 or 0.75 (`hit1`, `hit2`, `hit3`, `hit4`). Splitting the effects in two lists would change their draw order | read of `SpriteEffects.Play` and `Draw` |
| `Ui.Scanlines` | **Kept, gap.** `Scanlines { Strength = 0.07, Phase = 0.5 }` on `Scene.PostProcess` is pixel-identical on `sdl` and `raylib` once the scene paints its own background (a scene's chain does not darken the canvas colour showing through: 324 to 2,466 pixels 1 or 2 of 255 apart before that). On `sdl-software` the play scene then differs: 2,268 pixels of `s1-run.png` up to 21 of 255, all in the additive glow circles of the capsules; 25 of 40 captures of that platform | `kept/scanlines` in the scratch folder |

### Why `Timers` cannot take the schedule

1. **The update depends on the stage clock.** The game compares `_time + delay` with a float sum of steps. For the 14
   delays whose product with 60 is not whole, the count is constant and equals the engine's rule. For the 7 that are
   whole (0.9, 1.8, 2.6, 3.4, 0.2, 5 × 0.12, 5 × 0.09) it is N or N + 1 depending on when it is scheduled: over the
   first 30 minutes of a stage, 0.9 s is 54 updates from 50,724 starting updates and 55 from 57,276. A fixed
   `AfterSteps` cannot be right; counting the updates on the game's clock before adding the timer (six lines) is.
2. **Order within one update.** With that count, every action fires on its update, but `Timers` calls the due ones in
   the order added and the game's loop calls the last added first. In the Leviathan fight a hull turret's second shot
   and a shot of the sweeping cannon fall on one update (pinned runs: update 2428 of `mortal3`; 2106, 3755 and 5791 of
   `auto3`): the two enemy shots are spawned in the other order and the two `enemy-shot` sounds swap their pitch
   jitter. 59 of 3,074 and 248 of 10,181 lines of the pinned runs change, and the audio captures of `auto3` and
   `mortal3` differ. `Timers` has no setting for the order.

### Why machines cannot take the timed states

Float sum of 1/60 against what a machine gives (`calc2.cs` in the scratch folder):

- `StateMachine.Time` is the rounded product, the game's timers are sums: they differ on 64 of the first 79 updates
  (Vela's death spin is drawn as `_stateTime * -9`) and on 225 of the first 240 (the Hauler's strafe is
  `sin(_strafe / 4 * π) * 250`, the Raptor's dash `InOutQuad(_dash / 1.3)`); positions are hashed and shots leave from them.
- The warning band blinks on `(int)(t * 4) % 2`: on the float sum it toggles on updates 15, 30, 46, 61, 76, 91, 106,
  121, 136, 151, not every 15.
- The clear sequence explodes when `(int)(t * 10)` changes: updates 6, 12 ... 42, then 49, 55 ... 139.
- The egg pod's cycle keeps the remainder of `% 4.2f`: its first cycle is 253 updates (open at 133, fire at 169), the
  later ones 252 (132, 168).

A machine with `AfterSteps` would hold the exits and the game would still keep the float for the rest.

### Vela and `CharacterController2D`

`tests/NeonRift.Tests/ControllerDifferentialTests.cs` drives a controller on a `TilePhysicsWorld2D` beside
`Player.Step` with one motion (`MoveSpeed 118`, `Gravity 1000`, `MaxFallSpeed 430`, `JumpSpeed 400`,
`JumpReleaseSpeedScale 0.45`, `JumpReleaseGravityScale 1`, no coyote or buffer, `LandingReach 1`,
`DropThroughTime 0.25`), the body's origin at the box's top-left corner, and the game's rules around the step: the
box placed from the feet every step, the drop decided by the game and started two pixels down, the camera clamp and a
respawn as `Position =`, a hit and a death as `Velocity =` (the grounded flag stays as it was, as in the game), a
death's fall as a second motion. Compared exactly: position, velocity, ground.

- Ten small stages, one per rule (run, full jump, hop, tap, ceiling, wall, ledge, platform and drop, kneel and plant,
  pit): 0 apart.
- The three stages with the autopilot, mortal and not, and the scripts `stage1`, `poses`, `tour`: 25,849 steps, 113
  apart, all in height only and by under 0.0001 px. First: stage 1, step 1509: Vela at y = 224, the controller at
  223.99991.
- Cause: Vela is grounded when ground is within a pixel under her feet and keeps her falling speed until the floor
  stops her on the next step, which puts her on it exactly. The game writes that on the controller as one line (a
  landing that nothing stopped gets `LandingSpeed` back). When such a landing ends within the tile world's contact
  distance of the floor and she is not running, the controller takes her as at rest and never makes that move.
- Three things found on the way that the mapping needs: `Kill` during a jump must not be cut by a released jump
  (the death motion sets `JumpReleaseSpeedScale = 1`); the ground must not stop a death's slide before it moves
  (`Deceleration = 0`); the feet-to-box conversion every step is part of the game's arithmetic.

### Engine gaps that remain

1. A game whose character lands on ground a pixel under its feet and falls the rest on the next step is 0.00009 px
   off on the controller: the tile world's `Move` by zero reports ground within its contact distance, where
   `TileMotion` needs the floor to stop the move.
2. A game whose scheduler calls the last added of two actions due on one update first cannot move to `Timers`
   without swapping them; there is no order setting.
3. A game with a hand-kept float clock gets N or N + 1 updates for a delay whose product with the rate is whole; it
   has to count the updates on its own clock before `AfterSteps`. Neither `Timers` nor a machine can be told "this
   clock is a float sum".
4. `StateMachine.Time` cannot be the sum a hand-kept timer has, so a game that draws or moves with the time in a
   state keeps its float beside the machine.
5. `SpriteEffects.Play` has no scale (nor rotation or blend): a game with scaled impact sparks keeps its own effects.
6. `Scene.PostProcess` on `sdl-software` changes the additive shapes a scene draws; and a scene's chain leaves the
   canvas colour behind an unpainted pixel untouched, so the scene must paint its background.
7. `microengine content names` writes no constants for the names of points and boxes: `"hit"`, `"hurt"`,
   `"shoulder"` and `"tip"` are strings in the game.

### `games/Shared` still used

`DemoKit.AchievementBanners` (`RiftGame`) and `DemoKit.AchievementList` (the title's achievements screen): not looked
at in this pass. `OptionList` is no longer referenced by the game's own code; `AchievementList` may use it inside
the kit.

### Lines of game code (C#, generated file left out)

| Area | Before | After |
| --- | --- | --- |
| `Program.cs`, `RiftGame.cs` | 203 | 203 |
| `Art/` | 258 | 228 |
| `Gameplay/` | 2221 | 2233 |
| `Levels/` | 238 | 238 |
| `Scenes/` | 720 | 723 |
| `tests/NeonRift.Tests` | 1223 | 1680 |
| `Content/sprites.sheet` | 82 | 91 |

Deleted: the pixel scan and the pending-rig list (`Assets`, 30 lines), eleven typed hit boxes, the `Strip` record and
the two optional lookups with their fallbacks. Added: one `Joined` line per kind that reads its box. The game is 5
lines shorter; what would have shortened it more fails the identity constraint for the reasons above.

### Verification

- Baseline from a build of `origin/main` (`debaa4a7`) in a scratch copy: 19 scenarios (the seven scripts with their
  environments, the three other weapons, stages 1 and 3 on the autopilot, three mortal runs, a mortal tour, the card
  and game over starts, a checkpoint start): 339 files (captures, traces with sections, audio captures). Two
  recordings identical. The other desktop platforms (`raylib`, `sdl`, `sdl-software`): six scenarios each, 120 captures.
- After each commit: 339 of 339 identical; at the end 120 of 120 on the other platforms.
- `PinnedBehaviourTests`: 14 runs of the real game, one line per update (actors, pose, sounds, effect frames), digests
  recorded before any change; unchanged by every commit.
- A save written by `main` (`--saves`, high score 3750) gives the same title capture in both builds.
- `dotnet test tests/NeonRift.Tests -c Release`: 88 passed, 0 failed. `dotnet build MicroEngine.slnx -c Release`: 0 warnings.

### Log

- L5 An audio capture of a muted run is silence: `--mute --audio-capture` writes zeros. The first pass's seven WAV
  files, recorded that way, compared nothing. This pass records audio in a second run without `--mute`.
- L6 `sdl-software` is not a CI reference but a platform players fall back to; the scanlines attempt passed on the
  default platform and failed there.
- L7 The test of an effect on a stage without art (L4) went with that case: a stage always has the sheet now.

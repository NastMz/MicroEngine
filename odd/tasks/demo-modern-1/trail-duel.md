# DEMO-MODERN-1: Trail Duel on the current SDK

Objective: replace the private infrastructure of Trail Duel (`games/TrailDuel`, a creature-collecting, turn-based
adventure; hybrid of code-drawn and CC0 / CC BY content, ADR 0005) with what the engine now provides, without changing
anything a player sees or a script records (issue #182, CONSOLIDATION-1 T14). Branch `feat/demo-trail`. Test runner:
`dotnet test tests/TrailDuel.Tests -c Release`.

## Method

- Baseline, before any change (`main` at `824fcf44`): the eight scripts of `games/TrailDuel/scripts`, each with the
  debug start the tests give it, run with `--script --hidden --mute --captures --state-trace`: 118 captures and 8
  state traces, run twice and identical to the byte.
- A wider set written for this work (kept in the session's scratch folder, not in the repository), because the
  game's scripts reach neither most battle effects nor most screens: the `fx` debug start with a capture every 3
  updates for 1500 updates; a generic input script (confirm, directions, cancel, pause, holds) on 20 debug starts and
  the title; a battle script (fight, move card, bag, party, run) on five battle starts. 874 captures and 27 traces
  from a build of `main`.
- After each step: `dotnet build MicroEngine.slnx -c Release` (0 warnings), the game's tests, both sets compared byte
  for byte with the baseline.

## Map: what the game carries and what was decided

"Game code" is `games/TrailDuel/**/*.cs`. The rules (`Rules/`, 1,469 lines: battle, formulas, species, moves, items)
and the art built in code (`Visuals/`, 3,249 lines) are the game itself and are not listed file by file.

| Piece | Where | What it does | Engine capability | Decision |
| --- | --- | --- | --- | --- |
| Save folder parameter | `TrailDuelGame` constructor, `CreateSaves` override, `TRAIL_SAVE_DIR` in `Program.cs`, temporary folders in 8 test files | Kept tests and capture runs away from the player's saves | The run's own store, `--saves DIR`, `InitializeForTest(platform, seed, saves)`, `MemorySaveStorage` (ADR 0073) | **Replaced** |
| Effect slicing table and rates | `Visuals/FxArt.cs` (16 rows of cell size and pivot), a rate at each of 14 `SpriteFx.Play` calls | Cut the 16 effect images into frames and timed them | `.sheet` description with clips, `SpriteAtlasBuilder.AddSheet`, `SpriteAtlas.Clip` | **Replaced** (`Content/fx/fx.sheet`) |
| Row sorting of the overworld | `OverworldScene.Draw.cs`: a loop per row, a list of the row's actors, `List.Sort` by Y | Drew objects, item balls, buildings and characters so that lower ones cover higher ones | `SortedRenderer` (depth, then call order) | **Replaced** |
| Ending flow | `CreditsScene`: `_champion`, `_phase`, `_leaving` | Three parts (card, roll, leaving) with a clock per part | `StateMachine<T>`, `Scene.Machines` | **Replaced** |
| Save version | `GameState.Schema` | Version 1, no steps | `SaveSchema<T>` | Already on the engine (SAVE-2) |
| Cutscenes | `OverworldScene.Scripts.cs`, `BattleScene.Flow`, `EvolutionScene.Run` | Sequences of dialogue, walks, waits, battles | `Scene.Coroutines`, `Wait.Seconds` | Already on the engine |
| Drawing between steps | `Actor`, the camera's entity, the battle's sliding things, the credits' scroll | Positions the world interpolates | `Scene.Objects`, `Camera2D.DrawPosition` | Already on the engine |
| Prompts and the controls screen | `TitleScene`, `ControlsScene` | Key, button or touch names of the device in use | `Game.Prompts` (`ActionPrompts`) | Already on the engine |
| Input | `CreateInput`, `CreateTouchLayout` | Default actions, four-way stick, touch layout | `InputMap.CreateDefault`, `TouchLayout` | Already on the engine; the game has one player and no rebinding screen |
| Music and sound | `Sound/MusicBank.cs`, `Sound/SoundBank.cs` | Songs and effects written as data for the engine's synthesizer, four recorded jingles | `Song`, `Synth`, `AudioMixer.PlayMusic` with its fade | Already on the engine; the banks are the game's content |
| Achievements | `Rules/Achievements.cs` | The catalog | `Game.TrackAchievements` | Already on the engine |
| Battle menu mode | `BattleScene`: `enum Mode { Busy, Actions, Moves }` | Which menu takes input while the flow coroutine waits | `StateMachine<T>` | **Kept**: see L4 |
| Title menu flags | `TitleScene`: `_menu`, `_confirm` | Press-to-start, menu, overwrite question | `StateMachine<T>` | **Kept**: see L4 |
| Battle phase | `Rules/Battle.cs`: `BattlePhase` | Whose decision the rules wait for | — | **Kept**: a rule of the game (headless, tested by itself), not infrastructure |
| Tween and fade coroutines | `BattleScene.Tween` (24 uses), `OverworldScene.FadeOut`, `FadeIn`, `ShowBubble` | A value eased over seconds that a cutscene waits for | `Scene.Tweens` | **Gap** G1 |
| One-shot sprite effects | `Scenes/SpriteFx.cs` | Plays a clip once at a point (delay, flip, tint), then drops it | `Animator` (one object), `ParticleSystem` (no clips) | **Gap** G2 |
| Text caches | `Ui/NumberTexts.cs`, `Ui/CachedText.cs`, `Visuals/SpriteKeys.cs` (115 lines) | Numbers and atlas keys built once so drawing makes no garbage | — | **Gap** G3 |
| Typewriter and list cursor | `games/Shared/Typewriter.cs`, `OptionList.cs` used by `Ui/TextPanel.cs` and nine screens | Reveal of text by letters; a cursor over a list | `Label.VisibleCharacters`, widget focus | **Gap** G4 (not this game's files) |
| Text box and menus | `Ui/TextPanel.cs`, `Ui/UiKit.cs`, the menu screens | The message box with pages, the boxes, bars and badges | `Scene.Widgets` (`Panel`, `Label`, `MenuItem`, `Grid`) | **Kept**, left as work: see L5 |
| Two-by-two menu grid | `BattleScene.GridMove` | Cursor of the action and move menus | `Grid` focus | **Kept** with the menus (L5) |
| Hand timers | `_turnTimer`, `_bumpTimer`, `_npcClock`, `_lowHpBeep`, `_messageTime` | Delays inside movement and menu rules | `Scene.Timers` | **Kept**: see L6 |
| Grid movement | `World/Actor.cs`, `World/MapRuntime.cs` | Tile steps, turn in place, ledge hops, trainer sight | `TopDownController2D`, `MicroEngine.Navigation` | **Kept**: the game's rule (a step is a tile and cannot be interrupted); no free movement, no pathfinding (wanderers take one random step) |
| Tile drawing | `OverworldScene.Draw.cs` (`DrawGround`, `DrawObject`) | Picks animated and autotiled sprites per tile | `TileLayer`, `Autotile` (used) | **Kept**: the choice of sprite per tile is the game's art direction; the engine's parts are already used |
| Art derived in code | `Visuals/*` | Recolours, composes and scales pieces of the content images into the atlas | `SpriteAtlasBuilder`, `PixelImage` (used) | **Kept**: the hybrid's art (ADR 0005); the crops are formulas over a grid, not a table of frames |
| Battle wipe | `Scenes/Transitions.cs` | The band wipe into a battle | `Transition` (subclassed) | Already on the engine's extension point |
| Debug starts | `Scenes/DebugStart.cs`, `GalleryScene.cs` | Jump to a screen for captures | — | **Kept**: the game's development shortcuts |
| Strings in code | dialogue in `World/Maps.cs`, the scenes, `Rules/MoveText.cs` | English text | String tables (LOC-1) | **Kept**, left as work: see L7 |

## Tasks

| ID | Task | Route | Status |
| --- | --- | --- | --- |
| T1 | Map and baseline | inline | [x] |
| T2 | Saves follow the run | inline | [x] `ae6e8533` |
| T3 | Effects from a sheet file | inline | [x] `d0f01287` |
| T4 | Overworld order by `SortedRenderer` | inline | [x] `42ba2150` |
| T5 | Ending flow on a state machine | inline | [x] `bc8cbd89` |

## Lines of game code

Lines of `*.cs`, `main` at `824fcf44` against the branch:

| Area | Before | After |
| --- | --- | --- |
| `TrailDuelGame.cs`, `Program.cs` | 170 | 163 |
| `Scenes/` | 3,712 | 3,724 |
| `Ui/` | 541 | 541 |
| `World/` | 858 | 855 |
| `Visuals/` | 3,249 | 3,223 |
| `Rules/`, `Sound/` | 1,722 | 1,722 |
| Game total | 10,252 | 10,228 |
| `tests/TrailDuel.Tests` | 2,439 | 2,391 |

The game did not get shorter: it was already written on the engine's scenes, coroutines, world, prompts, saves and
synthesizer, and what it still carries is mostly what the engine does not have (G1 to G4). What left the code is a
kind of thing, not a number of lines: the save folder plumbing, the table that cut the effect images, a rate at
every effect call, the sort by hand and three flags with a clock. `Scenes/` grew by the depth assignments of the order and by
comments.

## Engine gaps

Written as what a developer outside the repository has to write. None was worked around in the engine.

- G1 **A tween a cutscene can wait for.** A developer who writes cutscenes as coroutines has to write
  `for (float t = 0; t < seconds; t += EngineStep) { apply(ease(t / seconds)); yield return null; } apply(1);`
  (`BattleScene.Tween`, 24 uses; `OverworldScene.FadeOut`, `FadeIn`, `ShowBubble`) because `Tweens.Add` returns no
  handle and a coroutine can yield only `null`, `Wait.Seconds`, `Wait.Until` and another routine. `Tweens.Add(...,
  done: () => finished = true)` with `Wait.Until(() => finished)` is not the same thing: the engine's tween first
  applies `ease(dt / duration)` on the step after it is added, and the loop applies `ease(0)` on the step it starts,
  so every battle animation would move one update (the captures of `master-battle` are 4 updates apart). What is
  missing: a yieldable tween (`yield return Tweens.Run(seconds, apply, ease)` or `Wait.Tween`), which applies 0 on the
  step it starts, or a handle with `IsFinished`.
- G2 **One-shot sprite effects.** A developer making any game with impacts, puffs of smoke or sparkles writes a list
  of "this clip, once, at this point, after this delay, flipped, tinted, then gone" (`Scenes/SpriteFx.cs`, 55 lines)
  because an `Animator` is one clip on one object that stays, and a `ParticleSystem` has no clips. What is missing: a
  player of many one-shot clips (`Effects.Play(clip, at, delay, flipX, tint, fps)`), drawn into a renderer, cleared
  with the scene and seeded into the state hash or left out of it on purpose.
- G3 **Text and sprite names without garbage.** A developer who shows numbers every frame (HP "12/20", money,
  levels, counts) and wants no allocation per frame writes caches of strings (`Ui/NumberTexts.cs`,
  `Ui/CachedText.cs`, 78 lines) because `BitmapFont.Draw`, `Measure` and the UI take only `string`. What is missing:
  drawing from a `ReadOnlySpan<char>` or a formatter that writes into a buffer the font draws. The same for sprites:
  `Visuals/SpriteKeys.cs` (37 lines) keeps `"t:path" + n` strings because the atlas is indexed by name;
  `SpriteAtlas.Frames(name)` covers runs named `name0..nameN` but not a name made of two numbers (`t:water{mask}_{frame}`)
  or of an id (`front:{species}`).
- G4 **The typewriter and the list cursor are not in the SDK.** The four demos share `games/Shared/Typewriter.cs` and
  `OptionList.cs` (138 lines), and the engine's own dialogue example writes its reveal loop by hand. A developer
  making a game with dialogue writes the reveal (letters per second, a callback per letter for the blip, finish on
  confirm) and, for menus drawn in the game's own look, the cursor (wrap, repeat, a sound per move). What is
  missing: the reveal as a type (or on `Label`), and focus navigation usable by a screen that draws itself.
  `games/Shared` is not this game's to edit.
- G5 **Prompt text in a scripted run depends on the keyboard layout of the machine.** One run of twelve of the same
  build and script drew the controls screen with `WAS?` where the others drew `WASD` (seen once and looked at; the
  file was overwritten when the baseline was repaired, and five more runs of each build did not show it again). `ActionPrompts.KeyText` asks the platform for the label the
  layout prints on a key (`SdlInput.KeyLabel`: `SDL_GetKeyFromScancode`, `SDL_GetKeyName`), also in a run with a
  script, so the title's "PRESS Z TO START", the controls screen and the bag and summary hints are not a function
  of the script and the seed. The game cannot fix it without giving up the layout's labels for players. What is
  missing: a scripted run (or `--time deterministic`) uses the engine's own key names, as `HeadlessPlatform` does.

## Looked for: what could make a capture differ between two runs (consolidation L14)

- In the game's code nothing reads a clock, the date, a file's time or the environment while it runs; every
  generator is a `SeededRandom` derived from the run's (`OverworldScene._rng` from `Game.Random`, each `Battle` from
  it, the three `ParticleSystem`s with fixed seeds); nothing is asynchronous. The sets and dictionaries that feed
  the hash are sorted (`GameState.AddSorted`, `Battle._participants`); those that feed drawing are looked up by key,
  never enumerated, except while building the atlas at load (`AtlasFactory.Mini`, `Village.Recolouring`: a
  `Dictionary` enumerated in insertion order with ties resolved by first seen, the same every run because nothing is
  removed from them).
- `OverworldScene` sorted a row's actors with `List.Sort`, which is not a stable sort in general (it is for the 16
  elements or fewer a row can hold). It is gone (T4): equal depths keep the order of the calls.
- Timing in the scenes accumulates the step it is given (`t += EngineStep`, `_progress += dt`), so it depends on the
  update rate and not on frame pacing, as long as a scripted run does one step per frame.
- What is drawn and is not a function of the simulation alone: G5 (key labels), found by accident and not on the
  screens of `master-battle`; and every sliding thing of the battle (platforms, trainers, lunges, the orb) and the
  overworld's actors and camera, which are drawn where the world interpolates them between steps
  (`Objects.DrawPosition2D`). If a browser frame is ever drawn with a blend other than the one of a whole step, a
  capture of `master-battle` taken during a lunge is where it would show. Not proven: the browser was not run here.
- The 126 files of the game's scripts and the 900 of the wider set were written identically by every run on
  this machine in this work, except the two captures of the controls screen of G5 in one run.

## Log

- L1 The brief describes Trail Duel as a game of AI opponents, rounds, trails, particles and local multiplayer. It is
  not: it is a single-player, turn-based creature adventure on a tile grid. There is no per-player device mapping,
  no rebinding code, no moving-object collision to index and no pathfinding. The corresponding candidates were looked
  for and are absent.
- L2 T2. `new TrailDuelGame(saves, debug)` became `new TrailDuelGame(debug)`: both arguments are strings, so a caller
  left behind would compile and take the folder for the debug start. Every caller is in `tests/TrailDuel.Tests`
  (searched: none in `web/`, `packaging/`, `benchmarks/`, other tests). Still naming `TRAIL_SAVE_DIR`, outside this
  work's surface and now without effect: `docs/guide/project-structure.md` (quotes the old `Program.cs`),
  `docs/reference/configuration.md`, `media/tools/capture-footage.mjs`,
  `experiments/arch-1/browser-determinism/run-desktop.mjs`, `experiments/arch-1/p1-sdl-gpu-driver/run.mjs`. The two
  tools pass `--script`, so their runs keep their saves to themselves without the variable.
- L3 T3. The sheet gives each effect the rate all its uses had; the spark ring had three (20 when a creature is sent
  out, 18 in the big Volt impact, 16 when an orb closes), so the file has 18 and two calls say theirs. The pivots are
  written as fractions (`"pivot": [0.5, 0.9]`) because that is what the table held and the engine multiplies them
  by the cell size in the same order, to the same `float`; `"origin": [15, 36.9]` would differ in the last bit. The
  frames enter the builder at the same place and in the same order, so the atlas is packed as before. `SpriteFx`
  keeps its own rule for the end of an effect (`time * fps >= frames`) with the clip's rate: `AnimationClip.Duration`
  is `frames / fps`, which is not the same comparison in `float`.
- L4 Kept as they are, after writing each on paper as a `StateMachine`: the battle's `Mode` and the title's flags.
  The battle's flow is already the coroutine `Flow`; `Mode` only says which of two menus reads the input while the
  coroutine waits, and four of its seven assignments are made by the coroutine or by the callback of a screen pushed
  over the battle. As a machine it is three state declarations and the same assignments spelled `Go`, with a clock
  in the state hash that nothing reads (`_menuTime` is not the time in the state: it is not reset between the two
  menus). The title is two booleans. Neither gets shorter nor clearer. The ending (T5) is the one place where the
  shape was a machine: three parts, each with a clock and a way out.
- L5 Left as work, not done: the text box and the menus on `Scene.Widgets`. They can be: `Panel`, `Label` with
  `VisibleCharacters`, `MenuItem` and `Grid` exist, and a look of the game's own is a control that draws itself.
  It was not attempted because the constraint is identical captures on about 15 screens whose boxes are drawn with
  rounded positions, a double border and a shadow of the game's own (`UiKit.Box`), and the state hash of every one
  of them would change; it is a unit of its own, a screen at a time, the bag or the shop first. `BattleScene.GridMove`
  goes with it (`Grid` moves the focus by direction).
- L6 The timers inside rules (`_turnTimer`, `_bumpTimer`, `_npcClock`, `_lowHpBeep`) are not `Timers.After` or
  `Every`: each is reset or held by the rule around it (the wander clock runs while a cutscene plays and fires on
  the first free step; the bump timer counts only while a direction is held against a wall). They are in the state
  hash by name, which a divergence then names.
- L7 Strings stay in code. A string table would not change what is drawn, but the dialogue is data inside
  `World/Maps.cs` records and `Rules/` (several hundred strings, many built with names and numbers), and no second
  language exists to check the result against. Left as work with LOC-1's sample as the model.
- L8 T4. Identical captures and traces (126 of 126, 900 of 900). The visible range is still tested per row, as
  before: a building whose bottom row is more than two rows under the screen is not drawn while its roof could be
  in view. No map has a building that tall near an edge the camera reaches; not changed, since changing it could
  change a capture.
- L9 T5. The ending's hash changed, on purpose: `_champion`, `_phase` and `_leaving` left `HashState`, and the scene
  now hashes the machine (its state and the time in it). No test holds a hash of the ending; the state trace tests
  compare two runs of the same build. That the game states are the same on the same updates is pinned by
  `CreditsFlowTests`, written first against the old code: the card leaves at update 420 and the credits end at 3867
  without input, 200 and 3647 with confirm pressed at 200 and held. The new code gives the same four numbers.
- L10 A capture differed between two runs of the same build (G5); found while comparing T3 with the baseline, which
  had the odd one. The baseline's capture was replaced by one of five later identical runs of the same `main` build;
  the odd file itself was overwritten by that repair and is not kept.

## Second pass (after DEMO-GAPS-1, issue #218)

Branch `feat/demo2-trail`, from `main` at `debaa4a7`. The engine closed G1 to G5 (ADR 0074 to 0077 and the scripted
run's own key names); this pass goes through every row of the map again with the engine as it is now and deletes
what it makes unnecessary.

### Method

- Baseline from a build of `debaa4a7` exported outside the worktree: the game's eight scripts with captures, state
  traces and `--audio-capture`, and the first pass's wider set (the `fx` start with a capture every 3 updates, a
  generic input script on 20 debug starts and the title, a battle script on five battle starts): 991 captures, 35
  traces and 8 audio captures, run twice and identical to the byte (1,034 files).
- `tests/TrailDuel.Tests/PresentationTimelineTests.cs`, written first against the old code and seen passing: five
  runs (the master's battle, a wild battle with a thrown orb, the move showcase, a lost trainer battle and the
  healing shrine) list every sound that starts, every time the battle menu opens or closes and every change of
  screen, each with its update (222, 109, 54, 106 and 93 events). It has not changed since.
- After each step: `dotnet build MicroEngine.slnx -c Release` (0 warnings), the game's tests, the 1,034 files
  compared byte for byte, and for every trace that changed, which sections changed and on which updates.

### The map again

| Piece | First pass | Now |
| --- | --- | --- |
| Tween and fade coroutines (G1) | gap | **Replaced**: the battle's 24 `Tween(...)` calls are `yield return Tweens.Add(...)` and `BattleScene.Tween` is deleted; the overworld's `FadeOut` and `FadeIn` are one tween each. **Kept**: `ShowBubble` and the shrine's glow, see L11; `DrainHp`, a rate and not a length |
| One-shot sprite effects (G2) | gap | **Replaced**: `Scenes/SpriteFx.cs` deleted; `Effects.Play(clip, ...)` with `Effects.SnapToPixels` |
| Text caches (G3) | gap | **Replaced**: `Ui/NumberTexts.cs`, `Ui/CachedText.cs` and `Visuals/SpriteKeys.cs` deleted, with the key tables of the tiles, trees, actors, buildings, battle creatures, level panel and dex numbers. Text is `$"{hp}/{max}"` given to `UiKit.Text`, `TextRight` (the font's `DrawRight`) and `Title`; a sprite is `atlas[$"t:water{mask}_{frame}"]`; hints are `Prompts.Cached`. Kept: the wrapped lines of the move card and the summary, the per-map tree layout (worked out, not formatted) |
| Typewriter and list cursor (G4) | gap | **Replaced**: `TextReveal` in `TextPanel` (its line split cache is gone too) and `MenuCursor` in the title, pause menu, bag, shop, party, trail log and the yes/no box |
| Two-by-two menu grid | kept | **Replaced**: `MenuCursor(4, columns: 2)` with `Wrap = false`; `BattleScene.GridMove` deleted. One difference, see L13 |
| Text box and menus on widgets | kept, left as work | **Kept**, decided: see L14 |
| Battle menu mode, title flags | kept | **Kept**: nothing in either leaves by time, so `AfterSteps` has nothing to count; see L15 |
| Hand timers | kept | **Kept**: L6 stands with `Timers` as a plain object too; see L15 |
| Strings in code | kept, left as work | **Kept**: see L16 |
| Prompt text and the keyboard layout (G5) | gap | Closed in the engine; nothing to change in the game |
| Every other row | | Unchanged |

### Tasks

| ID | Task | Route | Status |
| --- | --- | --- | --- |
| T6 | Pin the updates of the presentation (old code) | inline | [x] `34c30bc8` |
| T7 | Eases and fades on the scene's tweens | inline | [x] `d3d7872a` |
| T8 | One-shot effects on `Scene.Effects` | inline | [x] `204d1f3d` |
| T9 | Numbers and sprite names written where they are drawn | inline | [x] `fc71bfd0` |
| T10 | `TextReveal` and `MenuCursor` | inline | [x] `41315cb7` |
| T11 | The differing frames of `master-battle` looked at | inline | [x] L17 |

### Lines of game code

Lines of `*.cs`, `debaa4a7` against the branch:

| Area | Before | After |
| --- | --- | --- |
| `TrailDuelGame.cs`, `Program.cs` | 163 | 163 |
| `Scenes/` | 3,724 | 3,584 |
| `Ui/` | 541 | 468 |
| `World/` | 855 | 840 |
| `Visuals/` | 3,223 | 3,182 |
| `Rules/`, `Sound/` | 1,722 | 1,722 |
| Game total | 10,228 | 9,959 |
| `tests/TrailDuel.Tests` | 2,391 | 2,524 |

Deleted: four files (`SpriteFx.cs`, `NumberTexts.cs`, `CachedText.cs`, `SpriteKeys.cs`, 170 lines), `BattleScene.Tween`
and `GridMove`, 12 arrays of tile keys and the water key table, the trees' key arrays, `Actor.SpriteKey` with its four
fields, the battle's creature key records and backdrop and platform key fields, the level panel's ten kept strings,
`BagScene.SyncList`, the text box's line cache. The tests grew by the timeline (116 lines) and the grid test.

### Captures, traces, sound

- Captures: 991 of 991 identical to `main` after every step. Audio captures: 8 of 8 identical.
- State traces: 22 of 35 identical. 13 changed, in T7 and T8 only, and only in the engine's part of the scene's hash
  (`scene[n]:BattleScene`, `scene[0]:OverworldScene`): a coroutine that waits for a tween instead of a nested one
  (on the updates an ease or a fade runs: 29 to 564 updates of a run) and the effects, now a hashed scene system (on
  every update of a battle). The game's own sections (`battle.rules`, `battle.screen`, `overworld`,
  `overworld.player`, `overworld.npcs`, `game`) are equal on every update of every run. T9 and T10 change no trace:
  the scenes hash the same values (cursor index, characters shown) as before.
- Behaviour on the same updates: `PresentationTimelineTests` passes unchanged after every step.
- Saves: a save written by the `main` build (`--saves`, the pause menu's SAVE from the `town` start) continued from
  the title by both builds gives the same 2 captures and the same state trace, byte for byte (430 updates).
- Drawing makes no garbage: `FrameGarbageTests` (town, route, lab, party, shop, summary, the battle menu, the
  shrine's glow, the controls page), with the bag added.

### What the game still takes from `games/Shared`

`AchievementList` (the achievements screen, pushed from the title) and `AchievementBanners` (the unlock banner, drawn
above the scenes). Neither uses `OptionList` or `Typewriter`. The SDK has no achievements screen and no banner: to
stop using them the game would carry about 170 lines of its own for both (the list's clamped cursor and its window
are `MenuCursor { Wrap = false }` and `Top(visible)`). The banner outlives scenes (a frame system, drawn in
`DrawAboveScenes`), and widgets belong to a scene's root, so it cannot be a scene's canvas widgets. `OptionList` and
`Typewriter` are no longer referenced by this game.

### Engine gaps that remain

- G6 **An achievements screen and an unlock banner.** A developer whose game has achievements writes the list
  (rows with icon, name, description, progress bar, a scrolling window) and the banner queue that slides in over
  any scene (`games/Shared/AchievementList.cs`, `AchievementBanners.cs`, 197 lines, shared by the four demos)
  because the engine tracks achievements and shows nothing, and has no widget root above the scenes for something
  that stays up while they change.
- G7 **A label's line pitch.** A developer whose dialogue box has lines 13 pixels apart cannot use `Label` with
  `Reveal`: a label's lines are `Font.LineHeight + Unit` apart (11 pixels with the built-in font on the canvas)
  and nothing sets it. The game keeps `TextPanel.Draw` over `TextReveal.VisibleLines`. Read in
  `src/MicroEngine.UI/Widgets.cs`, not run.
- G8 **Seconds since a tween or a wait began.** A developer whose cutscene shows something for 0.7 s and draws it
  from the seconds elapsed (a bubble that pops in during the first 0.12 s, a glow that pulses with `sin(t * 9)`)
  still writes `for (float t = 0; t < 0.7f; t += EngineStep) { _time = t; yield return null; }` because a tween
  gives only eased progress (`Tween.Progress` is time divided by length, and `progress * length` is not the same
  `float` as the accumulated time). Missing: `Tween.Time`, or the elapsed seconds given to `apply`.
- G9 **`VisibleLines` enumerated with `foreach` allocates.** `TextReveal.VisibleLines` and `Label.VisibleLines`
  are `IReadOnlyList<string>` over an array: `foreach` boxes an enumerator (32 bytes a draw, caught by the game's
  allocation test); a `for` over the index does not. A type with its own enumerator, or a note in the manual.
- G10 **`SnapToPixels` rounds the point, not the picture.** `SpriteEffects.Draw` rounds the effect's position and
  then subtracts the frame's origin; a clip whose pivot is a fraction of an odd cell (the flame's 25 x 30 cell with
  pivot 0.5, 0.75: origin 12.5, 22.5) is drawn exactly between pixels, which two rasterizers resolve differently
  (L17). `SpriteFx` did the same, so the game's pictures are unchanged; rounding the top-left would change them.

### Log

- L11 `ShowBubble` (0.7 s) and the shrine's glow (1.8 s) keep their loops: what they give drawing is the seconds
  elapsed, not progress (G8). `Tweens.Add(0.7f, p => _bubbleTime = p * 0.7f, Ease.Linear)` would run the same
  number of updates with values that differ from the accumulated `t` in the last bits, and the glow feeds `sin`.
- L12 T8. The scene steps its effects after the coroutines and before `Update`, where the game stepped `SpriteFx`
  from `Update`: the same order relative to the flow coroutine that plays them. The battle never waited on its
  effects (`SpriteFx.Busy` had no caller); they are in the hash now because the engine's are.
- L13 T10. `GridMove` and `MenuCursor` differ in one case: opposite directions pressed in the same update. The game
  moved (left won over right, up over down); the cursor cancels them, as `OptionList` did in every other list of
  the game. No script of the game presses opposite directions on the same update (checked: 8 scripts, 70 presses
  and holds), so no capture or trace can show it; a player can, by pressing both keys within one update. Not
  papered over: `MoveInspectionTests.BattleMoveMenuIsATwoByTwoGridThatStopsAtItsEdges` states the new behaviour.
- L14 The text box and the menus stay drawn by the game, over `TextReveal` and `MenuCursor`, which is the path the
  manual gives a scene that draws its own boxes. Judged by reading the widgets, not by building them. The text box
  cannot be a `Label` with the same pixels (G7: 11 against 13 pixels between lines). The menus can be buttons with
  a skin on the canvas (`WrapFocus`, `FocusChanged` for the sound, `UiContext.Time` for the cursor's bob), with
  three costs: a panel has one padding and the game's boxes have 6 above and 4 below the rows (a fixed height
  works around it); the rows would take the mouse and touch, which they do not today, and `touch.script` taps the
  canvas; and each of the nine screens trades a draw loop of about five lines for the construction of its rows,
  while `UiKit.Box` and `UiKit.Cursor` stay as the skins. No private infrastructure would leave.
- L15 `Mode` and the title's flags again, with `AfterSteps`: neither has a way out by time. The low-HP beep
  (`_lowHpBeep`) is the one clock near them: it fires on the first update a menu is up, then every 0.55 s, runs
  only while a menu is open and keeps what is left between turns (it is in the hash). As `Timers.EverySteps` owned
  by the scene it needs the first call by hand and a step of its own behind the same two conditions: three lines
  for three lines. The overworld's timers are consulted and reset by the walking rule in several places (L6).
- L16 Strings: about 330 sentences in code, 66 of them built with names or numbers, most inside the map records
  (`World/Maps.cs`), the cutscenes and the rules (`Rules/Battle.cs` emits `MessageEvent(text)`, and the rules'
  tests read the text). A table would not change a pixel; it would move the rules from text to keys with
  arguments, which is a change of the rules' contract and not the removal of infrastructure, and there is no
  second language to check it against. Not done.
- L17 The frames `b_732` to `b_748` of `master-battle`. Looked at (captures read, both rasterizers run here):
  - What is on screen: Blazehound's Ember reaches Gustwing. The trail of round flame particles ends, the impact
    bursts (72 round particles, half of them additive) and the one-shot `flame` effect plays over the enemy; from
    `b_740` the enemy is knocked back 6 pixels and slides home.
  - This machine's GPU against the stored WARP reference: all 81 frames differ a little; `b_732` to `b_748` differ
    by 7 to 17 in the mean of the cells (22..24, 3..5), over the tolerance of 3; the others by at most 3. Run here
    with `SDL_RENDER_DIRECT3D11_WARP=1 SDL_RENDER_DRIVER=direct3d11`, the same build gives all 81 frames within 1
    of the WARP reference, and two such runs are identical. So the five frames are the rasterizer, not the run.
  - Pixel by pixel, GPU against WARP on this machine: in every frame where the enemy is drawn, 108 pixels (12
    canvas pixels under the enemy: its shadow or feet) differ by up to 56; during the trail, up to 2,100 pixels
    differ by 1 (blending of the translucent particles rounds differently); in `b_732` to `b_752`, 360 to 1,683
    pixels differ by more than 16 and up to 243, whole pixels of the picture.
  - The cause of the large difference is the `flame` effect: its sheet gives a 25 x 30 cell the pivot (0.5, 0.75),
    an origin of (12.5, 22.5), so the sprite is drawn exactly half a pixel off in both axes, and at three device
    pixels a canvas pixel every third device pixel's centre falls on a texel edge, which each rasterizer resolves
    its own way. Tried, not committed: with `"origin": [12, 22]` in a scratch build the pixels more than 16 apart
    in `b_732` to `b_752` fall from 360..1,683 to the constant 108, and the largest difference from 243 to 56.
    Other effects have such origins too (geyser, sparkle, boost, gale, aura, thunder, splash).
  - Frame pacing: the particles are drawn where the last update left them (`ParticleSystem.Draw` with its default
    blend), the effects and the text too. What is drawn between updates in these frames is the enemy's knockback
    (its offset is an entity of the scene's objects, set 6 pixels away at once and eased home over 10 updates) and
    nothing else; a frame drawn mid-step would show the enemy up to 6 pixels off in `b_740` to `b_748`, over the
    whole silhouette. The differences seen here are not that shape and start at `b_732`, before the knockback.
    Whether a browser ever draws mid-step in a scripted run was not tested here; a browser that fell back to its
    software rasterizer for one run would give the half-pixel difference above. Not proven.
  - Every stored reference (`sdl.json`, `sdl-windows-ci.json`) differs by 1 from this build in the same 15 cells of
    all 81 frames (the corners and shadows of the two HUD boxes), with either rasterizer. Within the tolerance; the
    cause was not looked for.

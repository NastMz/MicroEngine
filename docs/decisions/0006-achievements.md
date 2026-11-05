# 0006 — Engine-owned achievements, game-owned rules

Status: Accepted (2026-10-30). Amended on 2026-11-03 (below): achievements are declared before the engine is composed and are an optional engine module; saves are a module assembly of their own.

## Context

The owner asked for a reusable achievement system: the engine handles the registry, progress, unlocking,
persistence and queries; each game decides which actions earn an achievement. The engine must stay
ignorant of gameplay concepts, the API must be one clear call from the game, progress must survive
restarts and evolving catalogs, each game keeps its own data, unlocks show a temporary notification,
and each game offers a screen listing its achievements. No event bus, cloud services or speculative
platform abstractions.

## Decision

- **`AchievementTracker` in `MicroEngine.Core`.** It holds a catalog of `Achievement(Id, Name,
  Description, Goal = 1) { Icon }` records. Games report with `Unlock(id)`, `AddProgress(id, n)` or
  `SetProgress(id, value)` (monotonic absolute counts). Each achievement unlocks, and raises `Unlocked`,
  exactly once; later reports and reloaded unlocks are ignored. Unknown ids throw, so typos show up in tests.
- **Persistence goes through the existing `SaveStore`** (slot `achievements`). Each game already has its
  own save folder keyed by game id, which gives isolation for free and keeps one place to adapt when a
  platform without a file system (WebAssembly) arrives. Records are keyed by id: new catalog entries start
  locked, removed ones are kept untouched, missing or corrupt files start fresh (`SaveStore` moves a corrupt
  file aside to `<slot>.corrupt.json` instead of letting the next save destroy it), a file that exists but
  cannot be read is never overwritten in that session, and a goal lowered in a later version unlocks quietly. Unlocks are written at once; plain progress is flushed by the game loop
  about once a second and on exit, so a coin pickup does not cost a disk write. A failing disk keeps
  progress in memory.
- **`Game.UseAchievements(catalog, store, theme)`** turns the system on from `Load`. `Game` owns an
  `AchievementToasts` queue that it updates every step and draws above every scene and transition, so a
  banner never depends on which scene is active and never reads input.
- **`AchievementsScene` in `MicroEngine.UI`** is an overlay built only from existing pieces (`UiTheme`,
  `Panel`, `BitmapFont`): locked/unlocked state, `n/goal` progress with a bar, scrolling, configurable
  close actions and `Moved`/`Closed` events for each game's sounds.
- Games report events with direct calls where the event already happens (play scenes, battle scripts) or
  through the sink properties their stages already use (`StompSink`, `DefeatSink`), never through a
  generic event bus.

## Consequences

- A fifth game adds achievements with a catalog, one `UseAchievements` call, a few report calls and a menu
  entry that pushes `AchievementsScene`; the engine does not change.
- Achievements are per profile, not per save slot: starting a new adventure keeps them, which matches
  how players expect them to behave.
- The tracker writes JSON through `SaveStore`; a browser backend will need a `SaveStore` that uses
  browser storage, which also serves game saves.

## Amendment: achievements and saves as modules (2026-11-03)

Context: the engine is composed from modules on an `EngineBuilder` ([0026](0026-kernel-modules-and-schedule.md)) and
the frame schedule is fixed when the engine is built, before `Load`. Every game paid for achievements: the
`engine.achievements` system ran in every game, with or without a catalog.

- **Saves are a module assembly.** `SaveStore`, `ISaveStorage` and `FileSaveStorage` move to `MicroEngine.Saves`
  (kernel and platform contracts only; the namespace stays `MicroEngine.Core` and `MicroEngine.dll` forwards the
  types). `SavesModule(store)` registers a store as the engine's `SaveStore` service; it schedules nothing.
- **Achievements are an opt-in module.** `AchievementsModule(tracker)` registers the tracker as a service, schedules
  `engine.achievements` in `PostFixed` (before `engine.input.end`, its place in the step), writes progress about
  once a second of simulated time and on suspension, and raises `Stepped` each step for the banners.
  The module and the catalog and tracker types live in the `MicroEngine.Achievements` assembly (namespace of the
  types unchanged, forwarded from `MicroEngine.dll`); it references the kernel, the platform contracts, saves (the
  tracker persists through `SaveStore`) and 2D graphics (`Achievement.Icon` is a `Sprite`).
- **Declared before composition.** `UseAchievements(catalog, store, theme)` is called from the game's constructor or
  from `Configure`. `Game` then adds `SavesModule` (unless the composition already has a store) and
  `AchievementsModule`, and wires the banners to it. A call from `Load` or later throws `InvalidOperationException`:
  the schedule is already fixed, and an always-present system is what this amendment removes. Code that needs the
  running game (banner position, unlock sounds) stays in `Load`.
- The banners (`AchievementToasts`) and the list scene (`AchievementsScene`) are UI and move with the UI module, not
  with the tracker.

Consequences: a game without achievements has no achievement or save service, no `engine.achievements` system and no
file; a game with achievements keeps the same banners, persistence rules and state hash. Games written for the
earlier SDK move one call (the migration notes say how). Compositions without the `Game` façade add the modules
themselves.

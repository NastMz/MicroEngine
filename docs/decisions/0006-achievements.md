# 0006 — Engine-owned achievements, game-owned rules

Status: Accepted (2026-10-30).

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

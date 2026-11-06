# Achievements

The engine keeps score and the game decides what counts. A game declares a catalog of `Achievement`s, turns the
system on with `UseAchievements`, and reports events where they already happen. `AchievementTracker` owns
progress, guarantees that each achievement unlocks exactly once across sessions, and persists through the game's
`SaveStore`; `AchievementToasts` shows a banner for every unlock and `AchievementsScene` lists them all. Each of
the four sample games ships five achievements.

## Declaring a catalog

```csharp
public sealed record Achievement(string Id, string Name, string Description, int Goal = 1)
{
    /// <summary>Optional picture for the toast and the list screen.</summary>
    public Sprite? Icon { get; init; }
}
```

Source: [`src/MicroEngine.Achievements/Achievements.cs`](../../src/MicroEngine.Achievements/Achievements.cs)

`Goal` 1 is an immediate achievement; a larger goal is progressive. `Id` is the key stored in saves: names and
descriptions may change freely, ids must stay stable. The catalog order is the display order.

```csharp
public static class RiftAchievements
{
    public const string FirstScrap = "first-scrap", ScrapDealer = "scrap-dealer", Rooftops = "rooftops", Untouched = "untouched", Leviathan = "leviathan";
    public const int ScrapGoal = 100;

    public static readonly Achievement[] Catalog =
    [
        new(FirstScrap, "FIRST SCRAP", "Destroy your first robot."),
        new(ScrapDealer, "SCRAP DEALER", $"Destroy {ScrapGoal} robots.", ScrapGoal),
        new(Rooftops, "ROOFTOP RUNNER", "Clear the Neon Rooftops."),
        new(Untouched, "NOT A SCRATCH", "Clear a stage from its start without losing a life."),
        new(Leviathan, "LEVIATHAN DOWN", "Destroy the Leviathan dreadnought."),
    ];
}
```

Source: [`games/NeonRift/Gameplay/Achievements.cs`](../../games/NeonRift/Gameplay/Achievements.cs)

The tracker rejects empty ids, goals below 1 and duplicate ids at construction.

## Turning it on

Call `UseAchievements(catalog, store, theme?)` before the engine is composed: from the game's constructor or from its
`Configure(EngineBuilder)` override. It creates the tracker over the `"achievements"` slot of the store and the banner
queue styled with the theme. What only exists once the game runs (sounds, the banners' position) stays in
`Load`:

```csharp
public override void Configure(EngineBuilder builder) => UseAchievements(RiftAchievements.Catalog, Store, Ui.Theme);

protected override void Load()
{
    // ...
    AchievementToasts!.Top = 22;   // below the HUD gradient
    Achievements.Unlocked += _ => Sfx("powerup", 0.8f);
    // ...
}
```

Source: [`games/NeonRift/RiftGame.cs`](../../games/NeonRift/RiftGame.cs)

Achievements are an optional engine module. Declared before composition, the engine adds the module: the tracker and the store become services
(`Services.Get<AchievementTracker>()`, `Services.Get<SaveStore>()`), and the `engine.achievements` frame system writes
progress. A game that never calls `UseAchievements` has no achievement service, no frame system and no achievement
file, and calling it from `Load` or later throws an `InvalidOperationException` that names the fix.

`Game.Achievements` throws until `UseAchievements` has been called; `Game.HasAchievements` tells whether it was.
A composition of your own (without the `Game` façade) adds the same module itself:
`builder.Add(new AchievementsModule(tracker))` (namespace `MicroEngine.Achievements`).

## Reporting progress

| Method | Use | Returns `true` |
| --- | --- | --- |
| `Unlock(id)` | Something happened once ("defeat the boss") | Only on the first unlock |
| `AddProgress(id, amount = 1)` | Count events ("destroy 100 robots") | Only on the call that reaches the goal |
| `SetProgress(id, value)` | Report an absolute count ("rooms visited: 7"); progress never goes down | Only on the call that unlocks |

```csharp
G.Achievements.Unlock(CourtyardAchievements.FirstFoe);
G.Achievements.AddProgress(CourtyardAchievements.Defender);
```

Source: [`games/CourtyardKey/Scenes/PlayScene.Combat.cs`](../../games/CourtyardKey/Scenes/PlayScene.Combat.cs)

```csharp
G.Achievements.SetProgress(CourtyardAchievements.Cartographer, CourtyardAchievements.VisitedDungeonRooms(Progress));
```

Source: [`games/CourtyardKey/Scenes/PlayScene.Rooms.cs`](../../games/CourtyardKey/Scenes/PlayScene.Rooms.cs)

Unknown ids throw `KeyNotFoundException`, so a typo fails tests instead of silently doing nothing. Reports for an
achievement that is already unlocked are ignored. Read state with `Get(id)` (an `AchievementStatus` with `Progress`,
`Unlocked`, `Goal`, `IsProgressive`, `Fraction`), `IsUnlocked`, `Progress`, `All`, `Count` and `UnlockedCount`.

## Persistence rules

| Rule | Detail |
| --- | --- |
| Unlocks are written at once | `Raise` calls `Flush` on every unlock |
| Progress is written about once a second | The `engine.achievements` frame system calls `Flush` every second of simulated time; the game also flushes on suspension and on shutdown |
| `Unlocked` fires once | Never for achievements loaded as already unlocked |
| Missing or corrupt data starts fresh | The store sets corrupt data aside ([Persistence](persistence.md#failure-handling)) |
| An unreadable save is never overwritten | `Flush` returns `false` and keeps progress in memory for that session |
| Catalog changes are safe | New entries start locked; entries no longer in the catalog are kept untouched; a goal lowered below saved progress unlocks quietly on load |

A tracker created without a store (`new AchievementTracker(catalog)`) keeps progress in memory only, which is
convenient in tests.

## Banners

`AchievementToasts` queues "ACHIEVEMENT UNLOCKED" banners that slide in from the top, one at a time, each shown
for 3.4 seconds. They never read input, so play continues underneath. The game draws them above every scene and
any transition.

| Property | Default | Purpose |
| --- | --- | --- |
| `Theme` | from `UseAchievements` | Panel and colours |
| `Heading` | `"ACHIEVEMENT UNLOCKED"` | First line |
| `Top` | 4 | Distance from the top edge; move it below a HUD |
| `Current`, `Pending` | — | Banner on screen and queue length |

An achievement's optional `Icon` sprite is drawn in the banner and in the list.

## The list screen

`AchievementsScene(tracker, theme)` is an overlay that dims the scene below and lists every achievement: unlocked
ones highlighted, locked ones dimmed, progressive ones with `progress/goal` and a bar. Up and down move the
cursor and scroll; any action in `CloseActions` (default: the input map's own names for `cancel`, `pause` and `confirm`, see
[Default actions](input.md#default-actions)) closes it. Init properties:
`Title`, `CloseActions`, `Dim`, `LockedLabel`, `UnlockedLabel`; events `Moved` and `Closed` for sounds.

```csharp
var list = new AchievementsScene(game.Achievements, Ui.Theme) { CloseActions = ["cancel", "pause", "confirm", "fire"] };
```

Source: [`games/NeonRift/Scenes/MenuScenes.cs`](../../games/NeonRift/Scenes/MenuScenes.cs)

Push it with `Game.Scenes.Push(list)` from a title or pause menu.

## Limitations

- Local only: no platform services (Steam, consoles, web portals) and no cloud sync.
- No hidden or secret achievements and no per-achievement unlock timestamps.
- The banner layout is fixed: only its theme, heading, top offset and the achievement icon can be changed.
  `UseAchievements` always creates and draws the banners.

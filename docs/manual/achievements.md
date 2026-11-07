# Achievements

The engine keeps score and the game decides what counts. A game declares a catalog of `Achievement`s, turns the
system on with `TrackAchievements`, and reports events where they already happen. `AchievementTracker` owns
progress, guarantees that each achievement unlocks exactly once across sessions, and persists through the game's
`SaveStore`. How unlocks look is the game's: a banner on `Unlocked`, a list built from widgets
([Showing achievements](#showing-achievements)). Each of the four sample games ships five achievements.

## Declaring a catalog

```csharp
public sealed record Achievement(string Id, string Name, string Description, int Goal = 1)
{
    /// <summary>Optional picture for the game's banner and list.</summary>
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

Call `TrackAchievements(catalog, store)` before the engine is composed: from the game's constructor or from its
`Configure(EngineBuilder)` override. It creates the tracker over the `"achievements"` slot of the store. What only
exists once the game runs (sounds) stays in `Load`:

```csharp
public override void Configure(EngineBuilder builder) => Banners.Attach(builder, TrackAchievements(RiftAchievements.Catalog, Store));

protected override void Load()
{
    // ...
    Achievements.Unlocked += _ => Sfx("powerup", 0.8f);
    // ...
}
```

Source: [`games/NeonRift/RiftGame.cs`](../../games/NeonRift/RiftGame.cs)

Achievements are an optional engine module. Declared before composition, the engine adds the module: the tracker and the store become services
(`Services.Get<AchievementTracker>()`, `Services.Get<SaveStore>()`), and the `engine.achievements` frame system writes
progress. A game that never calls `TrackAchievements` has no achievement service, no frame system and no achievement
file, and calling it from `Load` or later throws an `InvalidOperationException` that names the fix.

`Game.Achievements` throws until `TrackAchievements` has been called; `Game.HasAchievements` tells whether it was.
A composition of your own (without the `Game` façade) adds the same module itself:
`builder.Add(new AchievementsModule(tracker))` (namespace `MicroEngine.Achievements`).

`UseAchievements(catalog, store, theme?)` does the same and also shows the engine's own unlock banners
(`AchievementToasts`); both are deprecated and leave in a later SDK release ([migration notes](../migrations.md)).

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

## Showing achievements

`AchievementTracker.Unlocked` is raised once per unlock: show a banner, play a sound. A screen that lists every
achievement reads `All` (each one's `Progress`, `Goal`, `Unlocked` and `Fraction`). Both are a few widgets of the
game's own. The Achievements example builds the list as rows in a scrolling view:

<!-- code: samples/Examples/Examples/AchievementsExample.cs#list -->
```csharp
/// <summary>A row per achievement in a scrolling list, and two buttons that make progress.</summary>
private Widget Screen()
{
    var list = new Stack { Spacing = 2, Align = Align.Start };
    foreach (var a in Catalog)
    {
        var row = (Name: new Label(a.Name), State: new Label { Color = UiTheme.Default.Muted });
        _rows[a.Id] = row;
        list.Add(new Panel(new Stack { Spacing = 1, Align = Align.Start }
            .Add(new Stack { Orientation = Orientation.Horizontal, Spacing = 8 }.Add(row.Name).Add(row.State))
            .Add(new Label(a.Description) { Color = UiTheme.Default.Muted })) { Width = 180, Padding = 4 });
    }
    var buttons = new Stack { Orientation = Orientation.Horizontal, Spacing = 6 }
        .Add(new Button("COLLECT A GEM", () => { Tracker.Unlock("first"); Tracker.AddProgress("five"); }))
        .Add(new Button("START OVER", () => Tracker.Unlock("reset")));
    return new Stack { Spacing = 6 }.Add(new ScrollView(list) { Height = 90 }).Add(buttons);
}

public override void Update(float dt)
{
    if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
    foreach (var s in Tracker.All)
    {
        var (name, state) = _rows[s.Definition.Id];
        name.Color = s.Unlocked ? UiTheme.Default.Accent : null;
        state.Text = s.Unlocked ? "UNLOCKED" : s.IsProgressive ? $"{s.Progress}/{s.Goal}" : "LOCKED";
    }
}
```

and a banner per unlock, one at a time, that slides in with `Offset` and `Opacity`:

<!-- code: samples/Examples/Examples/AchievementsExample.cs#banner -->
```csharp
/// <summary>The banner: hidden until an unlock slides it in.</summary>
private Panel CreateBanner()
{
    _bannerName = new Label();
    return Banner = new Panel(new Stack { Spacing = 1, Align = Align.Start }
        .Add(new Label("ACHIEVEMENT UNLOCKED") { Color = UiTheme.Default.Accent })
        .Add(_bannerName)) { Opacity = 0 };
}

private void ShowNext()
{
    if (!_waiting.TryDequeue(out var achievement)) { _showing = false; return; }
    _showing = true;
    _bannerName.Text = achievement.Name;
    Audio.TryPlay("select", 0.5f);
    Tweens.Add(0.3f, t => { Banner.Offset = new Vector2(0, (t - 1) * 20); Banner.Opacity = t; }, Ease.OutCubic);
    Tweens.Add(0.3f, t => Banner.Opacity = 1 - t, delay: 2f, done: ShowNext);
}
```

A banner that must stay up while scenes change belongs to the game, not to a scene: draw it from
`Game.DrawAboveScenes(canvas)`, which runs after every scene and transition, and advance it from a frame system added
in `Configure` (`builder.Schedule.Add`). The four demos do that with banners of their own:

```csharp
protected override void DrawAboveScenes(IRenderer canvas) => Banners.Draw(canvas, Font);
```

Source: [`games/NeonRift/RiftGame.cs`](../../games/NeonRift/RiftGame.cs)

An achievement's optional `Icon` sprite is there for the game to draw in its banner and its list.

## Limitations

- Local only: no platform services (Steam, consoles, web portals) and no cloud sync.
- No hidden or secret achievements and no per-achievement unlock timestamps.
- No banner or list screen of the engine: the game draws its own ([Showing achievements](#showing-achievements)).

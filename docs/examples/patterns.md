# Usage patterns

The four games use the engine the same way in a handful of places. This page collects those recurring patterns
with real excerpts, so a new game can follow proven shapes instead of inventing its own: overlays for pause
menus, actions instead of keys, entity lists, procedural art, saves and achievements.

Excerpts are trimmed with `// ...`; follow the links for the full source.

## Pause menus are overlay scenes

Push a scene that returns `IsOverlay => true`. The scene below keeps drawing and is frozen (unless the overlay
sets `BlocksUpdateBelow => false`); popping the overlay calls `Resume` on it.

The gameplay side, from [`games/MeadowRun/Scenes/PlayScene.cs`](../../games/MeadowRun/Scenes/PlayScene.cs):

```csharp
public override void Resume() => Audio.PauseMusic(false);

public override void Update(float dt)
{
    if (_leaving) return;
    if (Input.Pressed("pause") && _stage.Phase == StagePhase.Playing)
    {
        game.Sfx("pause", 0.8f);
        Audio.PauseMusic(true);
        Game.Scenes.Push(new PauseScene(game, this));
        return;
    }
    // ...
}
```

The overlay itself, from the same file:

```csharp
internal sealed class PauseScene(MeadowGame game, PlayScene play) : Scene
{
    private readonly SelectionList _menu = new("Resume", "Restart level", "Quit to map");
    public override bool IsOverlay => true;
    // ...
    public override void Update(float dt)
    {
        if (Input.Pressed("pause") || Input.Pressed("cancel")) { Close(); return; }
        if (_menu.Update(Input) is not { } choice) return;
        // ...
    }
}
```

Scene changes that leave the current screen use `Replace` or `Reset` with a transition, for example
`Game.Scenes.Replace(new MapScene(...), Transition.Diamonds(0.8f, Pal.Ink))`. More in [Scenes](../manual/scenes.md).

## Actions, never keys

Game code reads named actions. A game that needs more than the default actions overrides `CreateInput` and adds
its own, keeping keyboard and gamepad bindings in one place. From
[`games/MeadowRun/MeadowGame.cs`](../../games/MeadowRun/MeadowGame.cs):

```csharp
protected override InputMap CreateInput() => InputMap.CreateDefault()
    .Bind("jump", [Key.Z, Key.Space, Key.J], [PadButton.A])
    .Bind("run", [Key.X, Key.C, Key.K, Key.L, Key.LeftShift], [PadButton.X, PadButton.B]);
```

and then in `PlayScene`:

```csharp
var input = new PlayerInput(
    Input.Down("left"), Input.Down("right"), Input.Down("down"),
    Input.Pressed("jump"), Input.Down("jump"), Input.Down("run"), Input.Pressed("run"));
```

Because scripts force the same action names (`470 press jump`, `640 hold run 120` in
[`smoke.script`](../../games/MeadowRun/scripts/smoke.script)), automated runs exercise exactly the code a player
does. See [Input](../manual/input.md).

## Entities in an `EntityList<T>`

Plain classes deriving from `Entity` live in an `EntityList<T>`. Adds and removes made while the list updates are
deferred, so entities can spawn and destroy each other safely. Courtyard Key queues spawns and flushes them
between update phases. From [`games/CourtyardKey/Scenes/PlayScene.cs`](../../games/CourtyardKey/Scenes/PlayScene.cs):

```csharp
private EntityList<Actor> _actors = new();
// ...
private void UpdatePlay(float dt)
{
    Hero.Update(dt);
    FlushSpawns();
    _actors.Update(dt);
    FlushSpawns();
    ResolveCombat();
    // ...
}

/// <summary>Queues an actor; it joins the room after the current update phase (safe during iteration).</summary>
public T Spawn<T>(T actor) where T : Actor
{
    _pending.Add(actor);
    return actor;
}
```

Queries such as `_actors.OfType<Enemy>()` skip destroyed entities. See [Architecture](../architecture.md) for why
the engine has no ECS.

## Procedural art built once

Art is built in `Load` (or in a factory called from it) as `PixelImage`s and packed into atlases. Noise and
dithering give texture without image files. From
[`games/CourtyardKey/Visuals/TileArt.cs`](../../games/CourtyardKey/Visuals/TileArt.cs):

```csharp
public static PixelImage Floor(int variant)
{
    var img = new PixelImage(S, S);
    for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float n = Art.ValueNoise(x + variant * 16, y, 6, 101);
            var c = n > 0.62f && Art.Dither(x, y, 0.5f) ? Pal.Stone[3] : Pal.Stone[2];
            if (x == 15 || y == 15) c = Pal.Stone[1];
            else if (x == 0 || y == 0) c = Color.Lerp(Pal.Stone[2], Pal.Stone[4], 0.5f);
            img[x, y] = c;
        }
    // ...
    return img;
}
```

Games that use files load them through `ContentFolder` in the same `Load`. See [Rendering](../manual/rendering.md)
and [Assets](../manual/assets.md).

## Saving progress through `SaveStore`

Each game creates one `SaveStore` with its own id, loads its slot in `Load` and falls back to fresh data. From
[`games/MeadowRun/MeadowGame.cs`](../../games/MeadowRun/MeadowGame.cs):

```csharp
private const string SaveSlot = "progress";
// ...
public SaveStore Store { get; } = new("MeadowRun", saveDirectory);
// ...
Save = Store.Load<SaveData>(SaveSlot) ?? new SaveData();
// ...
public void Persist()
{
    try { Store.Save(SaveSlot, Save); }
    catch (IOException) { /* A read-only disk must not crash the game; progress stays in memory. */ }
    catch (UnauthorizedAccessException) { }
}
```

`Load` returns null for a missing or unreadable slot; `Save` returns false when the medium refuses the write.
Trail Duel keeps the same calls on its state type, from
[`games/TrailDuel/Rules/GameState.cs`](../../games/TrailDuel/Rules/GameState.cs):

```csharp
public void Save(SaveStore store) => store.Save(SaveSlot, this);
public static GameState? Load(SaveStore store) => store.Load<GameState>(SaveSlot);
```

The same code saves to `localStorage` in a browser. See [Persistence](../manual/persistence.md).

## Achievements: declare, turn on, report

A static catalog, from [`games/MeadowRun/Gameplay/Achievements.cs`](../../games/MeadowRun/Gameplay/Achievements.cs):

```csharp
public static readonly Achievement[] Catalog =
[
    new(FirstCourse, "First Steps", "Clear the first course."),
    new(Stomper, "Stomp Squad", $"Stomp {StompGoal} enemies.", StompGoal),
    new(CoinPurse, "Coin Purse", $"Collect {CoinGoal} coins.", CoinGoal),
    // ...
];
```

Turned on in `Load` with the game's store and theme, from `MeadowGame.cs`:

```csharp
UseAchievements(MeadowAchievements.Catalog, Store, MeadowRun.Scenes.Ui.Theme);
AchievementToasts!.Top = 17;   // below the HUD row
Achievements.Unlocked += _ => Sfx("checkpoint", 0.8f);
```

Reported where the events already happen, from `PlayScene.cs`:

```csharp
private void OnCoin() => game.Achievements.AddProgress(MeadowAchievements.CoinPurse);
// ...
if (levelIndex == 0) game.Achievements.Unlock(MeadowAchievements.FirstCourse);
```

Courtyard Key reports an absolute count with `SetProgress`, which never goes down
([`PlayScene.Rooms.cs`](../../games/CourtyardKey/Scenes/PlayScene.Rooms.cs)):

```csharp
G.Achievements.SetProgress(CourtyardAchievements.Cartographer, CourtyardAchievements.VisitedDungeonRooms(Progress));
```

And the list screen is an engine scene pushed from a menu, from
[`games/MeadowRun/Scenes/TitleScene.cs`](../../games/MeadowRun/Scenes/TitleScene.cs):

```csharp
var list = new AchievementsScene(game.Achievements, Ui.Theme) { Title = "ACHIEVEMENTS" };
list.Moved += () => game.Sfx("menu_move", 0.7f);
list.Closed += () => game.Sfx("menu_back", 0.7f);
Game.Scenes.Push(list);
```

See [Achievements](../manual/achievements.md).

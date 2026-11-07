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
    private readonly DemoKit.OptionList _menu = new("Resume", "Restart level", "Quit to map");
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

## Game objects in the scene world

A scene keeps its objects in `Scene.Objects`, an `IWorld`
([The scene world](../manual/scenes.md#the-scene-world)): each object is one of your own classes, attached to an
entity that has an id and a transform. Courtyard Key's actors create their entity as a child of the room being
played, so leaving a room destroys its actors with it. From
[`games/CourtyardKey/Entities/Actor.cs`](../../games/CourtyardKey/Entities/Actor.cs):

```csharp
protected Actor(PlayScene world, bool inRoom = true)
{
    World = world;
    Id = world.Objects.Create(inRoom ? world.RoomGroup : EntityId.None);
    world.Objects.Attach(Id, this);
}
// ...
public void Destroy() => World.Objects.Destroy(Id);
```

The play scene updates the room's actors in creation order. From
[`games/CourtyardKey/Scenes/PlayScene.cs`](../../games/CourtyardKey/Scenes/PlayScene.cs):

```csharp
private void UpdatePlay(float dt)
{
    Hero.Update(dt);
    foreach (var a in Actors) a.Update(dt);
    ResolveCombat();
    // ...
}
```

A created entity is visible at once and a destroyed one is skipped at once, so actors can spawn and destroy each
other during the walk; there is no spawn queue to flush. See [Architecture](../architecture.md) for why the engine
has no ECS.

## Procedural art built once

Art is built in `Load` (or in a factory called from it) as `PixelImage`s and packed into atlases. Noise and
hashing give texture without image files. Courtyard Key paves its ruins with a block of stones that tiles
seamlessly, built once and kept. From
[`games/CourtyardKey/Visuals/GroundArt.cs`](../../games/CourtyardKey/Visuals/GroundArt.cs):

```csharp
public static PixelImage RuinStones()
{
    if (_ruinStones is not null) return _ruinStones;
    const int P = 32;
    var img = new PixelImage(P, P);
    var r = Pal.Boulder;
    Color[] body = [r[2], r[3], r[4]];
    // ...
                for (int ly = 0; ly < h; ly++)
                    for (int lx = 0; lx < w; lx++)
                    {
                        int x = (left + lx) % P, y = top + ly;
                        Color c;
                        if (lx == w - 1 || ly == h - 1) c = Art.Hash(x, y, 572) > 0.45f ? Pal.Turf[1] : Pal.Turf[2];
                        else if (lx == 0 || ly == 0) c = r[5];
                        else if (lx == w - 2 || ly == h - 2) c = r[2];
                        else c = ArtKit.Band(body, ArtKit.PeriodicNoise(x, y, 8, P, 573) * 0.6f + Art.Hash(x, y, 574) * 0.2f + tone, x, y, 0.3f);
                        img[x, y] = c;
                    }
    // ...
    return _ruinStones = img;
}
```

Games that use files load them through `Game.Assets` in the same `Load`,
as all four demos do. See [Rendering](../manual/rendering.md) and [Assets](../manual/assets.md).

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
public const int CurrentSaveVersion = 1;

/// <summary>The format of the save this state was read from (0 for a save written before versions existed); stamped on saving.</summary>
public int SaveVersion { get; set; }

public void Save(SaveStore store)
{
    SaveVersion = CurrentSaveVersion;
    store.Save(SaveSlot, this);
}

/// <summary>The saved adventure, or null without one; an older save is brought up to <see cref="CurrentSaveVersion"/>.</summary>
public static GameState? Load(SaveStore store)
{
    var state = store.Load<GameState>(SaveSlot);
    // Version 0 (no field) has the same layout as version 1: nothing to convert.
    if (state is { SaveVersion: < CurrentSaveVersion }) state.SaveVersion = CurrentSaveVersion;
    return state;
}
```

The saved data carries a format version. A save written before the field existed reads as version 0; when the
format changes, the game raises the number and converts older saves in `Load`, so players keep their progress.

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

Declared before the engine is composed, with the game's store: in `Configure` or in the game's constructor. The
game's own banners (`OptionList`, `AchievementBanners` and `AchievementList` are code the four demos share in
`games/Shared`) are advanced by a frame system and drawn above every scene. What needs the running game (the
banners' position, the unlock sound) stays in `Load`, from `MeadowGame.cs`:

```csharp
public override void Configure(EngineBuilder builder) => Banners.Attach(builder, TrackAchievements(MeadowAchievements.Catalog, Store));

protected override void DrawAboveScenes(IRenderer canvas) => Banners.Draw(canvas, Font);

protected override void Load()
{
    // ...
    Banners.Top = 17;   // below the HUD row
    Achievements.Unlocked += _ => Sfx("checkpoint", 0.8f);
    // ...
}
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

And the list screen is the demos' own scene, pushed from a menu, from
[`games/MeadowRun/Scenes/TitleScene.cs`](../../games/MeadowRun/Scenes/TitleScene.cs):

```csharp
var list = new DemoKit.AchievementList(game.Achievements, Ui.Theme) { Title = "ACHIEVEMENTS" };
list.Moved += () => game.Sfx("menu_move", 0.7f);
list.Closed += () => game.Sfx("menu_back", 0.7f);
Game.Scenes.Push(list);
```

See [Achievements](../manual/achievements.md).

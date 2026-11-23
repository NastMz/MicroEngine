# 11. Saving the best score

The best score should survive closing the game. Every game has a store, `Saves`, that writes small JSON save slots to
the right place on every platform: a file in the player's data folder on desktops and phones, the browser's storage on
the web.

## What to save

A save is any class `System.Text.Json` can write: public properties with getters and setters. Gem Garden saves one
number:

<!-- code: samples/GemGarden/part4-complete/MainGame.cs#save-data -->
```csharp title="MainGame.cs"
/// <summary>What the game saves: a plain class that System.Text.Json writes as JSON.</summary>
public sealed class SaveData
{
    public int BestScore { get; set; }
}
```

Bigger games save a class with the whole progress (levels, inventory, settings), still in one slot. Keep slots small:
they are written in one piece.

## The store

`MainGame` loads the best score in `Load`, from the store every game has:

<!-- code: samples/GemGarden/part4-complete/MainGame.cs#save-store -->
```csharp title="MainGame.cs"
private const string SaveSlot = "progress";

/// <summary>The id the saves are kept under: it names the save folder, so it never changes once players have saves.</summary>
protected override string SaveId => "GemGarden";

public int BestScore { get; private set; }
```

<!-- code: samples/GemGarden/part4-complete/MainGame.cs#load -->
```csharp title="MainGame.cs"
protected override void Load()
{
    var content = new ContentFolder();
    Sprites = new SpriteAtlasBuilder()
        .AddSheet(Assets.Sheet("hero.sheet"))
        .Add("gem", GardenArt.Gem(), new Vector2(4, 4))
        .AddFrames("slime", GardenArt.Slime(), new Vector2(6, 8))
        .Add("grass", GardenArt.Grass())
        .Add("hedge", GardenArt.Hedge())
        .Build(Graphics);

    LoadSounds(content);
    BestScore = Saves.Load<SaveData>(SaveSlot)?.BestScore ?? 0;
    Scenes.Push(new TitleScene(this));
}

protected override void Unload() => Sprites.Dispose();
```

- `Saves` is the game's store. It keeps its slots under `SaveId`, which names the folder (or the browser storage
  keys), so two games never read each other's saves. Without the override it is the project's application id; Gem
  Garden names its own, `"GemGarden"`. Use one id per game and never change it after release.
- The store uses the player's data folder: `%LOCALAPPDATA%\MicroEngine\GemGarden` on Windows,
  `~/.local/share/MicroEngine/GemGarden` on Linux (or `$XDG_DATA_HOME`),
  `~/Library/Application Support/MicroEngine/GemGarden` on macOS, the app's own storage on phones, and
  `localStorage` in a browser. Each slot is `<slot>.json` there.
- That is where the saves are when somebody plays. A run with an input script and a test keep them in memory
  instead, empty at the start, so they never read or change the player's saves and the game needs no code for it
  ([Scripted runs and tests](../manual/persistence.md#scripted-runs-and-tests)).
- `Saves.Load<SaveData>("progress")` returns the saved object, or `null` when there is no save yet. `?.BestScore ?? 0`
  turns "no save" into a best score of 0.
- Use `Saves` from `Load` or later, not from the constructor.

## Saving

<!-- code: samples/GemGarden/part4-complete/MainGame.cs#record-score -->
```csharp title="MainGame.cs"
/// <summary>Keeps the score if it beats the best one, and writes it to the save file. True for a new best.</summary>
public bool RecordScore(int score)
{
    if (score <= BestScore) return false;
    BestScore = score;
    Saves.Save(SaveSlot, new SaveData { BestScore = score });
    return true;
}
```

`Save` serializes the object and replaces the slot in one piece; a crash during the write never leaves half a save.
It returns `false` when the disk or the browser refuses (full disk, storage blocked in a private window): the game keeps
running with the score in memory, which is the right behaviour for a best score. A game with more at stake could tell
the player.

## When things go wrong

`SaveStore` is built so a save problem never crashes the game and never destroys progress:

- A slot with invalid JSON loads as `null`, and the bad data is kept next to it (`<slot>.corrupt.json`, or
  `<slot>.corrupt` in a browser) instead of being deleted.
- A slot that exists but cannot be read (a locked file, blocked storage) also loads as `null`, and the store then
  **refuses to overwrite it**, so a temporary glitch cannot replace a real save with a fresh one.

`Exists(slot)` and `Delete(slot)` complete the basics (a "new game" option deletes the slot), and `Slots()` lists the
slots for a "load game" screen. When a later release changes what `SaveData` holds, give the save a version so the
saves players already have keep loading: [Versions and migrations](../manual/persistence.md#versions-and-migrations).
Formats, locations and custom storage (a cloud service, a console's save API):
[Saving data](../manual/persistence.md).

## Run part 4

The game is complete: your project matches [`part4-complete`](../../samples/GemGarden/part4-complete), apart from the
identity you set in the next step. Play a round, close the game, start it again: the title shows your best score.

## Next

[Step 12](12-packaging.md) turns the project into files players can install.

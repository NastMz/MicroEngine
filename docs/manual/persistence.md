# Persistence

`SaveStore` gives each game small JSON save slots: write a whole object to a named slot, read it back, check or
delete it. Where the slots live is decided by an `ISaveStorage`: one JSON file per slot on desktop and in tests,
one `localStorage` entry per slot in a browser. The store is built so that a save problem never crashes the game
and never destroys existing progress: corrupt data is set aside, unreadable slots are never overwritten, and
refused writes return `false`.

## `SaveStore`

| Member | Behaviour |
| --- | --- |
| `new SaveStore(gameId, directory = null)` | Files under `directory`, or the platform's default medium (`DefaultStorage(gameId)`) |
| `new SaveStore(ISaveStorage storage)` | Any storage implementation |
| `Save<T>(slot, data)` | Serialises `data` to indented JSON and writes the slot whole; `false` if refused |
| `Load<T>(slot)` | The object, or `null` when missing, corrupt or unreadable |
| `Exists(slot)`, `Delete(slot)` | Slot management |
| `Directory` | The folder (file storage) or a description of the medium |
| `SaveStore.DefaultStorage(gameId)` | The platform's default medium (below); a host composes another one instead (`host.Storage`) |
| `SaveStore.DefaultDirectory(gameId)` | The user's save folder for a game on this system (see below) |
| `host.SaveStore(gameId)` | A store on the composition's medium (`host.Storage`), else the platform's default ([One composition for every platform](backends.md#one-composition-for-every-platform)) |
| `new SavesModule(store)` | Saves as an optional engine module (namespace `MicroEngine.Saves`): `builder.Add(new SavesModule(store))` registers the store as the engine's `SaveStore` service. It runs no frame system and writes nothing by itself. A game that declares achievements gets it with their store; a game that uses neither has no store service and writes no file |

Courtyard Key keeps its adventure in one slot:

```csharp
public sealed class SaveSlots(SaveStore store)
{
    public const string Slot = "adventure";
    public SaveStore Store { get; } = store;
    public bool HasSave => Store.Exists(Slot);
    public Progress? Load() => Store.Load<Progress>(Slot);
    public void Save(Progress progress) => Store.Save(Slot, progress);
    public void Delete() => Store.Delete(Slot);
}
```

Source: [`games/CourtyardKey/Model/Progress.cs`](../../games/CourtyardKey/Model/Progress.cs)

Use one `gameId` per game: it names the save folder and the browser key prefix, which keeps the games isolated.
Pass a `directory` in tests so they never touch the player's real saves.

## Where saves live

```csharp
public SaveStore(string gameId, string? directory = null)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
    _storage = directory is null ? DefaultStorage(gameId) : new FileSaveStorage(directory);
}
```

Source: [`src/MicroEngine.Saves/SaveStore.cs`](../../src/MicroEngine.Saves/SaveStore.cs)

| Platform | Storage | Location |
| --- | --- | --- |
| Windows (default) | `FileSaveStorage` | `%LOCALAPPDATA%\MicroEngine\<gameId>\<slot>.json` |
| Linux and other Unix systems (default) | `FileSaveStorage` | `$XDG_DATA_HOME/MicroEngine/<gameId>/<slot>.json`, or `~/.local/share/...` when `XDG_DATA_HOME` is unset or not an absolute path |
| macOS (default) | `FileSaveStorage` | `~/Library/Application Support/MicroEngine/<gameId>/<slot>.json` |
| Android | `FileSaveStorage` | the app's private files folder, `files/MicroEngine/<gameId>/<slot>.json` |
| iOS | `FileSaveStorage` | `Library/Application Support/MicroEngine/<gameId>/<slot>.json` |
| Explicit directory (tests, dev tools) | `FileSaveStorage` | `<directory>/<slot>.json` |
| Browser | `BrowserStorage` (`MicroEngine.Platform`) | `localStorage` key `microengine/<gameId>/<slot>` |

`SaveStore.DefaultDirectory(gameId)` returns that folder outside browsers. It is always an absolute path, also on a new account
where the data folder does not exist yet: the first save creates it, so saves never land in the working directory.
Only a system with no home folder at all falls back to the temporary folder.

`FileSaveStorage` writes a temporary file and moves it over the slot, so a crash never leaves a half-written save.
An explicit `directory` always means files.

Nothing is set for the process. A store created without a directory takes the platform's default medium,
`SaveStore.DefaultStorage(gameId)`: the page's `localStorage` in browsers, files in `DefaultDirectory(gameId)`
elsewhere, so games construct their store the same way on every platform. A host that wants another medium composes it
(`host.Storage`) and the game creates its store with `host.SaveStore(gameId)`.

## `ISaveStorage`

```csharp
public interface ISaveStorage
{
    /// <summary>The save folder, or a description of the medium for diagnostics.</summary>
    string Location { get; }
    /// <summary>The slot's text, or null when it does not exist.</summary>
    string? Read(string slot);
    /// <summary>Replaces the slot atomically.</summary>
    void Write(string slot, string text);
    bool Exists(string slot);
    void Delete(string slot);
    /// <summary>Moves unreadable data out of the slot, keeping a copy; best effort, never throws.</summary>
    void SetAside(string slot);
}
```

Source: [`src/MicroEngine.Saves/SaveStore.cs`](../../src/MicroEngine.Saves/SaveStore.cs)

Implementations report failures as `IOException` or `UnauthorizedAccessException`; the browser storage converts
JavaScript storage errors (quota, privacy mode) into `IOException`. A new medium (a cloud service, a console save
API) would implement this interface and be installed through the composition's `host.Storage` or the
`SaveStore(ISaveStorage)` constructor.
No such implementation exists today.

## Failure handling

| Situation | `Load` | Later `Save` to the same slot |
| --- | --- | --- |
| Slot missing | `null` | Writes normally |
| Corrupt JSON (or deserialises to `null`) | `null`; data set aside as `<slot>.corrupt.json` (files) or `<slot>.corrupt` (browser) | Writes a fresh slot |
| Slot exists but cannot be read (locked file, blocked storage) | `null`; slot remembered as unreadable | Refused, returns `false` |
| Write refused (full disk, storage quota, read-only folder) | — | Returns `false`; the game keeps running with progress in memory |

The third rule matters: a game that could not read a save is running with fresh progress, and writing it would
replace the real save. If setting corrupt data aside fails, it stays in place and callers that check `Exists`
will not overwrite it either.

## In the browser

The web host reports storage problems to the player: "Progress will not be kept" when the browser blocks site
storage (a session-only store is used), and "Progress could not be saved: site storage is full" on a quota error.
Closing or reloading the page ends the run properly so progress written at shutdown is saved. Browser storage
holds about 5 MB per origin, can be evicted and is discarded by private windows; see
[Requirements and limitations](../reference/limitations.md#saves).

## Limitations

- Saves are local to one machine or one browser profile; there is no cloud sync, export or import.
- JSON only, through `System.Text.Json`; no versioning or migration helpers beyond what your data classes do.
- Writes are synchronous; keep slots small (the sample games' saves are a few kilobytes).

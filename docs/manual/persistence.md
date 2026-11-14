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

### Slot names

A slot is a plain name, never a path, and the same names are valid on every system, so a save copied from one system
to another keeps working. Letters, digits, spaces and punctuation such as `-`, `_` and `.` are fine: `"progress"`,
`"input-bindings"`, `"profile.v2"`, `"Slot 2"`. Every call that takes a slot (`Save`, `Load`, `Exists`, `Delete`, and
the members of `FileSaveStorage`) throws `ArgumentException`, naming the slot, when the name:

- is `null`, empty or only white space;
- contains `/`, `\`, `:`, `*`, `?`, `"`, `<`, `>`, `|` or a control character, which rules out folders, drive letters
  and absolute paths (`"saves/1"`, `"../other"`, `"C:\save"`);
- ends with a dot or a space, which includes `"."` and `".."`;
- is a Windows device name, with or without an extension: `CON`, `PRN`, `AUX`, `NUL`, `COM0` to `COM9`, `LPT0` to
  `LPT9` (`"NUL"`, `"con.v2"`), in any letter case.

A slot never reaches a file outside the save folder. To group saves, put the group in the name (`"profile1.progress"`)
or give each group its own store over its own directory.

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

## Changing what a save holds

Players keep their saves across updates, so a release that renames, removes or reinterprets a saved field has to read
the shape older releases wrote. Adding a property needs nothing: a missing field keeps the value the class gives it.
For anything else, give the slot a version number and upgrade old saves when they load. `SaveStore` does not do this
for you; the steps are a few lines of game code, and they stay in the game because only the game knows what each
version meant.

The saved class carries the version it was written in. A save from before versions existed has no `Version` field and
reads as 0:

<!-- code: samples/Examples/SaveVersions.cs#data -->
```csharp
/// <summary>The saved data in its current shape. <see cref="Version"/> records the shape a slot was written in.</summary>
public sealed class Profile
{
    public int Version { get; set; }
    public int Gold { get; set; }
    public int Lives { get; set; } = 3;
}
```

Loading reads the slot as JSON first (`Load<JsonObject>`), because an old save may not fit the current class. One
step per version turns version *i* into *i* + 1, and the steps run in order from the save's version up to the current
one:

<!-- code: samples/Examples/SaveVersions.cs#upgrade -->
```csharp
/// <summary>
/// One slot whose shape changes between releases. Old saves are read as JSON, copied to a backup slot, upgraded one
/// version at a time and only then turned into a <see cref="Profile"/>; saves from a newer build are left alone.
/// </summary>
public static class ProfileSaves
{
    public const string Slot = "profile";

    // Each entry turns version i into version i + 1. Append a step for every change; never edit a published one.
    private static readonly Action<JsonObject>[] Upgrades =
    [
        // 0 -> 1: lives arrived; saves from before start with three.
        json => json["Lives"] = 3,
        // 1 -> 2: "Coins" was renamed "Gold".
        json => { json["Gold"] = json["Coins"]?.DeepClone(); json.Remove("Coins"); },
    ];

    /// <summary>The shape this build writes: one more than the last upgrade step.</summary>
    public static int CurrentVersion => Upgrades.Length;

    public static Profile? Load(SaveStore store, out SaveCheck check)
    {
        check = SaveCheck.Missing;
        if (store.Load<JsonObject>(Slot) is not { } json) return null;   // missing, corrupt or unreadable

        int version = 0;                                                  // saves from before versions existed
        if (json["Version"] is { } field && !(field is JsonValue value && value.TryGetValue(out version)))
        {
            check = SaveCheck.Protected;                                  // a version this build cannot read
            return null;
        }
        if (version > CurrentVersion)
        {
            check = SaveCheck.Protected;                                  // written by a newer build
            return null;
        }
        if (version < CurrentVersion)
        {
            // Keep the original before changing anything; without a copy, do not upgrade.
            if (!store.Save($"{Slot}.v{version}", json))
            {
                check = SaveCheck.Protected;
                return null;
            }
            for (int v = version; v < CurrentVersion; v++) Upgrades[v](json);
            json["Version"] = CurrentVersion;
        }
        try
        {
            var profile = json.Deserialize<Profile>();
            check = profile is null ? SaveCheck.Protected : version < CurrentVersion ? SaveCheck.Upgraded : SaveCheck.Current;
            return profile;
        }
        catch (JsonException)
        {
            check = SaveCheck.Protected;                                  // a field holds a value of the wrong type
            return null;
        }
    }

    /// <summary>Writes the profile in the current shape; false when the medium refused.</summary>
    public static bool Save(SaveStore store, Profile profile)
    {
        profile.Version = CurrentVersion;
        return store.Save(Slot, profile);
    }
}
```

The rules the example follows:

- **Upgrade one step at a time.** A save from any release reaches the current shape through the same steps. Append a
  step for every change and never edit one that has shipped: saves written in that shape are already on players'
  machines.
- **Keep the original before changing it.** The old JSON is copied to `profile.v<version>` first. If that copy is
  refused, the save is not upgraded at all.
- **Loading does not write the upgraded save.** The slot changes on the next ordinary save, when the game knows it is
  running correctly.
- **Never save over a save you could not read.** A save written by a newer build (a player who went back to an older
  release, or a synchronised folder shared by two machines) has a higher version than this build knows. Fields this build
  does not know would be dropped by the next save, so the example refuses it, as it refuses a version or a field of the
  wrong type.

`Load` reports which case it met:

<!-- code: samples/Examples/SaveVersions.cs#check -->
```csharp
/// <summary>What <see cref="ProfileSaves.Load"/> found.</summary>
public enum SaveCheck
{
    /// <summary>No usable save: start a new game (missing, or corrupt and set aside by the store).</summary>
    Missing,
    /// <summary>A save in the current shape.</summary>
    Current,
    /// <summary>An older save, upgraded in memory; the original is kept in a backup slot.</summary>
    Upgraded,
    /// <summary>A save this build cannot upgrade safely (written by a newer build, or the backup failed): never save over it.</summary>
    Protected,
}
```

A game that receives `Protected` can still be played, but it must not call `ProfileSaves.Save` in that session: tell the
player the save belongs to a newer version and keep progress in memory. `Missing` means a new game, as with any
`Load`: corrupt data has already been set aside by the store, and an unreadable slot is protected by the store itself.

Test each step with a save written by every version you have released: write the old JSON into a memory storage
([A save storage](extending.md#a-save-storage)), load it and check the upgraded values, the backup and the untouched
slot.

## In the browser

The web host reports storage problems to the player: "Progress will not be kept" when the browser blocks site
storage (a session-only store is used), and "Progress could not be saved: site storage is full" on a quota error.
Closing or reloading the page ends the run properly so progress written at shutdown is saved. Browser storage
holds about 5 MB per origin, can be evicted and is discarded by private windows; see
[Requirements and limitations](../reference/limitations.md#saves).

## Limitations

- Saves are local to one machine or one browser profile; there is no cloud sync, export or import.
- JSON only, through `System.Text.Json`. There are no built-in version numbers or migrations; a game upgrades its own
  saves ([Changing what a save holds](#changing-what-a-save-holds)).
- A slot is a plain name; folders inside the save folder are not supported ([Slot names](#slot-names)).
- Writes are synchronous; keep slots small (the sample games' saves are a few kilobytes).

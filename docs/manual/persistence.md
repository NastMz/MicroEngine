# Persistence

Every game has a store, `Saves`: small JSON save slots. Write a whole object to a named slot, read it back, list the
slots, check or delete one. A saved type can carry a version, so a save written by an earlier release of the game
loads in a later one ([Versions and migrations](#versions-and-migrations)). Where the slots live is decided by the
platform: one JSON file per slot on desktops and phones, one `localStorage` entry per slot in a browser. The store is
built so that a save problem never crashes the game and never destroys existing progress: corrupt data is set aside,
slots it could not read or use are never overwritten, and refused writes return `false`.

## The game's store

`Game.Saves` is a `SaveStore`, created the first time the game uses it:

<!-- code: samples/Examples/Examples/SaveExample.cs#data -->
```csharp
/// <summary>The saved data: any class System.Text.Json can write (public properties with getters and setters).</summary>
public sealed class Progress
{
    public int Coins { get; set; }
    public List<string> Visited { get; set; } = [];
    public DateTime SavedAt { get; set; }
}
```

<!-- code: samples/Examples/Examples/SaveExample.cs#use -->
```csharp
private SaveStore _store = null!;
private Progress _progress = null!;
private string _status = "";

public override void Enter()
{
    // The game's store: one per game, in the save folder of the system it runs on (the page's storage in a browser).
    _store = Game.Saves;
    _progress = _store.Load<Progress>("example") ?? new Progress();   // null: no save yet, or unreadable
    _status = _store.Exists("example") ? "LOADED" : "NO SAVE YET";
}

public override void Update(float dt)
{
    if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
    if (Input.Pressed("right")) _progress.Coins++;
    if (Input.Pressed("confirm"))
    {
        _progress.SavedAt = DateTime.UtcNow;
        _progress.Visited.Add($"room {_progress.Visited.Count + 1}");
        // Save writes the whole slot at once; false means the disk or the browser refused (keep playing).
        _status = _store.Save("example", _progress) ? "SAVED" : "COULD NOT SAVE";
    }
    if (Input.Pressed("cancel"))
    {
        _store.Delete("example");
        _progress = new Progress();
        _status = "DELETED";
    }
}
```

A scene reaches it as `Game.Saves`, the game itself as `Saves`. One store holds everything the game keeps: its progress, its settings
([Settings](#settings)), its achievements and its key bindings, each in a slot of its own.

| Member of `Game` | Behaviour |
| --- | --- |
| `Saves` | The game's store, on the medium of the run: the player's saves when somebody plays, a store of the run's own for a scripted run or a test ([Where saves live](#where-saves-live), [Scripted runs and tests](#scripted-runs-and-tests)). Use it from `Configure`, `Load` or later, not from the game's constructor |
| `SaveId` (protected, virtual) | The id the saves are kept under: the application id of the game's project ([Application identity](../guide/app-identity.md)), else the title. It names the save folder and the browser's key prefix, so a game that changes its application id after release overrides `SaveId` with the old one and its players keep their saves |
| `CreateSaves()` (protected, virtual) | Creates `Saves`, once. Override it only for a game that must keep its saves in one place whatever the run says: a store built there is the game's own, and neither `--saves` nor a scripted run's own store reaches it |

A game needs no code to keep its tests and its scripted runs away from the player's saves: the run decides
([Scripted runs and tests](#scripted-runs-and-tests)).

## `SaveStore`

| Member | Behaviour |
| --- | --- |
| `Save<T>(slot, data)` | Serialises `data` to indented JSON and writes the slot whole; `false` if refused, and for a slot that holds a versioned save |
| `Load<T>(slot)` | The object, or `null` when missing, corrupt or unreadable |
| `Save<T>(slot, data, typeInfo)`, `Load<T>(slot, typeInfo)` | The same, with the metadata of the game's `JsonSerializerContext` instead of reflection: for games published trimmed or with NativeAOT ([Trimmed and NativeAOT publishing](../guide/platforms.md#trimmed-and-nativeaot-publishing)) |
| `Save<T>(slot, data, schema)`, `Load<T>(slot, schema)` | A versioned save: `Load` brings an older save up to date and reports what it found ([Versions and migrations](#versions-and-migrations)) |
| `Exists(slot)`, `Delete(slot)` | Slot management |
| `Slots(prefix = "")` | The slots that hold a save, with their size and last write time ([Listing slots](#listing-slots)) |
| `Backups(slot)`, `LoadBackup(name, schema)`, `DeleteBackup(name)` | The copies kept of the saves older versions wrote ([Copies of older saves](#copies-of-older-saves)) |
| `Directory` | The folder (file storage) or a description of the medium |
| `Storage` | The medium itself (`IStorage`), for what keeps its own text beside the saves: `Input.SaveBindings(Saves.Storage)` ([A rebinding screen](input.md#a-rebinding-screen)) |
| `new SaveStore(gameId, directory = null)` | A store built by hand: files under `directory`, or the platform's default medium (`DefaultStorage(gameId)`) |
| `new SaveStore(storage)` | A store built by hand over any storage implementation |
| `SaveStore.DefaultStorage(gameId)` | The platform's default medium (below) |
| `SaveStore.DefaultDirectory(gameId)` | The user's save folder for a game on this system (see below) |
| `host.SaveStore(gameId)` | A store on the composition's medium, for a game that takes its store as a constructor argument ([One composition for every platform](backends.md#one-composition-for-every-platform)) |
| `new SavesModule(store)` | Saves as an optional engine module (namespace `MicroEngine.Saves`): `builder.Add(new SavesModule(store))` registers the store as the engine's `SaveStore` service. It runs no frame system and writes nothing by itself. A game that declares achievements gets it with its own store (`TrackAchievements(catalog)`); a game that uses neither has no store service and writes no file |

Use one save id per game: it keeps the games isolated.

### Slot names

A slot is a plain name, never a path, and the same names are valid on every system, so a save copied from one system
to another keeps working. Letters, digits, spaces and punctuation such as `-`, `_` and `.` are fine: `"progress"`,
`"input-bindings"`, `"profile.v2"`, `"Slot 2"`. Every call that takes a slot (`Save`, `Load`, `Exists`, `Delete`, and
the members of `FileSaveStorage`) throws `ArgumentException`, naming the slot, when the name:

- is `null`, empty or only white space;
- contains `/`, `\`, `:`, `*`, `?`, `"`, `<`, `>`, `|` or a control character below U+0020, which rules out
  folders, drive letters and absolute paths (`"saves/1"`, `"../other"`, `"C:\save"`);
- contains half of a surrogate pair;
- ends with a dot or a space, which includes `"."` and `".."`;
- ends with `.corrupt`, in any letter case: that is the name a slot's unreadable data is set aside under
  ([Failure handling](#failure-handling)), so a set-aside copy is never a slot;
- ends with `.backup`, in any letter case (`SaveStore` only): that is the name the original of a migrated save is
  kept under ([Copies of older saves](#copies-of-older-saves)); `LoadBackup` and `DeleteBackup` take those names and
  no others;
- is a Windows device name, with or without an extension: `CON`, `PRN`, `AUX`, `NUL`, `CONIN$`, `CONOUT$`, `COM0` to
  `COM9`, `LPT0` to `LPT9` and their superscript-digit forms (`"NUL"`, `"con.v2"`), in any letter case.

A slot never reaches a file outside the save folder. To group saves, put the group in the name (`"profile1.progress"`)
or give each group its own store over its own directory.

Names that pass the rule can still meet in one file, so keep them apart yourself:

- **Letter case.** `"Progress"` and `"progress"` are one file on Windows and macOS and two files on Linux and two
  keys in a browser. Use one spelling per slot, and never two slots that differ only by case.
- **Accented letters.** A letter typed as one character (`é`) and as a letter plus a combining accent are the same
  file on macOS and different files and keys elsewhere. Write slot names as constants in the code rather than
  building them from text the player types.
- **Length.** A file name holds about 240 characters, fewer when the name has letters outside ASCII. A longer slot is
  not an error: `Save` returns `false`, `Load` returns `null` and `Exists` returns `false`. Keep slot names short.

A game that lets the player name a save keeps that name inside the saved data and uses its own slot names
(`"profile1"`, `"profile2"`).

### Who checks the name

| Called | What it accepts |
| --- | --- |
| `SaveStore` (every call that takes a slot) | Plain names only, and none that ends with `.backup`, over any storage: files, the browser or a storage of your own. It checks before it touches the storage |
| `FileSaveStorage` (every member) | Plain names only, also when it is used without a store, so no slot reaches a file outside its folder |
| `BrowserStorage` (every member) | Any name that is not `null` or empty. A key is `microengine/<gameId>/` followed by the slot as given; keys are flat text, so a slot with `/` or `..` is one more key of the same game |
| A storage of your own | Whatever it decides. It throws `ArgumentException` for a name it cannot hold |
| `InputMap.SaveBindings`, `LoadBindings` | Any name that is not `null`, empty or white space; the storage they are given then applies its own rule ([A rebinding screen](input.md#a-rebinding-screen)) |

`ArgumentNullException` and `ArgumentException` mean the calling code passed a wrong name, and they are thrown on
every system. `IOException` and `UnauthorizedAccessException` mean the medium failed; `SaveStore` turns those into
`false` and `null` ([Failure handling](#failure-handling)).

## Where saves live

| Platform | Storage | Location |
| --- | --- | --- |
| Windows (default) | `FileSaveStorage` | `%LOCALAPPDATA%\MicroEngine\<save id>\<slot>.json` |
| Linux and other Unix systems (default) | `FileSaveStorage` | `$XDG_DATA_HOME/MicroEngine/<save id>/<slot>.json`, or `~/.local/share/...` when `XDG_DATA_HOME` is unset or not an absolute path |
| macOS (default) | `FileSaveStorage` | `~/Library/Application Support/MicroEngine/<save id>/<slot>.json` |
| Android | `FileSaveStorage` | the app's private files folder, `files/MicroEngine/<save id>/<slot>.json` |
| iOS | `FileSaveStorage` | `Library/Application Support/MicroEngine/<save id>/<slot>.json` |
| A folder the run names (`--saves DIR`) | `FileSaveStorage` | `<directory>/<slot>.json` |
| A scripted run, a run in deterministic time, a test | `MemorySaveStorage` | memory: empty when the run starts, gone when it ends |
| Browser | `BrowserStorage` (`MicroEngine.Platform`) | `localStorage` key `microengine/<save id>/<slot>` |

`SaveStore.DefaultDirectory(gameId)` returns that folder outside browsers. It is always an absolute path, also on a new account
where the data folder does not exist yet: the first save creates it, so saves never land in the working directory.
Only a system with no home folder at all falls back to the temporary folder.

`FileSaveStorage` writes a temporary file and moves it over the slot, so a crash never leaves a half-written save.
An explicit `directory` always means files.

Nothing is set for the process. `Game.Saves` takes the medium its host composed (`host.Storage`, a function from the
save id to an `IStorage`) and, when the host composed none, the platform's default medium,
`SaveStore.DefaultStorage(gameId)`: the page's `localStorage` in browsers, files in `DefaultDirectory(gameId)`
elsewhere. So a game is written once and saves the same way on every platform, and a host that wants another medium
(a cloud service, a console's save API, memory for a kiosk) composes it without the game changing:

```csharp
public static void Configure(HostBuilder host)
{
    host.Storage = saveId => new MyCloudStorage(saveId);
    host.UseGame(() => new MyGame());
}
```

A store the game builds by hand (`new SaveStore(...)`) is outside the composition and keeps whatever it was built
with.

### Scripted runs and tests

A run nobody plays never reads or writes the player's saves. A run that has an input script (`--script`) or
advances in deterministic time (`--time deterministic`, `--headless`, a game run or stepped on `HeadlessPlatform`)
keeps its saves in a `MemorySaveStorage` of its own: empty when the run starts, gone when it ends. So a capture run
shows the game as a new player sees it, two runs of the same script start from the same state, and a test needs no
folder. Progress, settings, achievements and key bindings are all slots of the same store, so all of them follow.

`--saves DIR` names a folder for the saves of one run, with or without a script:

```bash
# Starts from the saves in fixtures/before-boss and leaves what it saved there to look at.
dotnet run -- --script boss.script --hidden --captures captures --saves fixtures/before-boss
# A second set of saves beside the player's: a clean start while developing.
dotnet run -- --saves .saves/clean
```

The folder is created when the game first saves. In code the same choice is `RunOptions.Saves`, and a test that
steps a game hands it to `InitializeForTest`. A test that starts the game twice over the same saves gives both the
same medium:

```csharp
var saves = new MemorySaveStorage();
var game = new MyGame();
game.InitializeForTest(new HeadlessPlatform(), saves: saves);
// ... play until the game saves ...
var relaunched = new MyGame();
relaunched.InitializeForTest(new HeadlessPlatform(), saves: saves);   // finds what the first one saved
```

`new FileSaveStorage(folder)` in its place keeps the files to open.

Where the saves of a run go is decided when it starts, in this order:

| The run | Its saves |
| --- | --- |
| Names a medium (`--saves DIR`, `RunOptions.Saves`, `InitializeForTest(..., saves:)`) | That medium |
| Its composition sets `host.Storage` | The composition's medium, in a scripted run too |
| Has a script or is in deterministic time | A `MemorySaveStorage` of its own |
| Any other | The platform's default: the player's saves |

A composition that sets `host.Storage` decides for its scripted runs as well, because only it knows what its medium
is. To leave them their own store, set it only for a launch without a script:

```csharp
public static void Configure(HostBuilder host)
{
    if (!host.Request.HasScript) host.Storage = saveId => new MyCloudStorage(saveId);
    host.UseGame(() => new MyGame());
}
```

A game that used `Saves` in its constructor has its store before any run could say where it goes; a scripted run or
a test of such a game throws `InvalidOperationException` instead of using the player's saves.

In a browser page a scripted run (development packages only) keeps the page's storage: what it starts from is the
browser profile the page is opened in, and a new profile is an empty store.

## `ISaveStorage`

```csharp
public interface IStorage
{
    /// <summary>The save folder, or a description of the medium for diagnostics.</summary>
    string Location { get; }
    /// <summary>The slot's text, or null when it does not exist.</summary>
    string? Read(string slot);
    /// <summary>Replaces the slot atomically.</summary>
    void Write(string slot, string text);
    /// <summary>Whether the slot has data.</summary>
    bool Exists(string slot);
    /// <summary>Removes the slot; does nothing when it does not exist.</summary>
    void Delete(string slot);
    /// <summary>Moves unreadable data out of the slot, keeping a copy; best effort, never throws for a failure of the medium.</summary>
    void SetAside(string slot);
    /// <summary>
    /// Every slot that has data, in no particular order, with what the storage knows about each without reading the
    /// save in it. A copy set aside by <see cref="SetAside"/> is not a slot and is not listed.
    /// </summary>
    IReadOnlyList<SlotInfo> List();
}
```

Source: [`src/MicroEngine.Platform/Files/IStorage.cs`](../../src/MicroEngine.Platform/Files/IStorage.cs).
`ISaveStorage` (`MicroEngine.Core`) is the same contract under the name saves have always used.

Implementations report failures as `IOException` or `UnauthorizedAccessException`; the browser storage converts
JavaScript storage errors (quota, privacy mode) into `IOException`. Each storage decides which slot names it can
hold and throws `ArgumentException` for the others ([Who checks the name](#who-checks-the-name)). `List` returns a
`SlotInfo(Name, Size, LastWrite)` for each slot: the size is the file's size in bytes, or the length of the stored
text for a storage that keeps text, and `LastWrite` is `null` when the storage does not know. A new medium (a cloud
service, a console save API) implements this interface and is installed through the composition's `host.Storage`, or
handed to one store with `new SaveStore(storage)` ([A save storage](extending.md#a-save-storage)).
The SDK has three: `FileSaveStorage`, `BrowserStorage` and `MemorySaveStorage`, which keeps its slots in memory,
takes the names the file storage takes, reports no time of writing and keeps what `SetAside` took out of a slot
(`ReadSetAside`).

## Failure handling

| Situation | `Load` | Later `Save` to the same slot |
| --- | --- | --- |
| Slot missing | `null` | Writes normally |
| Corrupt JSON (or deserialises to `null`) | `null`; data set aside as `<slot>.corrupt.json` (files) or `<slot>.corrupt` (browser) | Writes a fresh slot |
| Slot exists but cannot be read (locked file, blocked storage) | `null`; slot remembered as unreadable | Refused, returns `false`, until a later load of the slot succeeds |
| Slot holds a versioned save ([Versions and migrations](#versions-and-migrations)) | The object; the version is ignored | Refused, returns `false`: save it through its schema |
| Write refused (full disk, storage quota, read-only folder) | — | Returns `false`; the game keeps running with progress in memory |

The third rule matters: a game that could not read a save is running with fresh progress, and writing it would
replace the real save. If setting corrupt data aside fails, it stays in place and callers that check `Exists`
will not overwrite it either. A later load of the slot that succeeds lifts the protection, and deleting the slot is
the game's explicit choice to start over: after `Delete`, the slot can be saved again. A save reads the slot before
it writes, to know whether it holds a versioned save. A versioned load tells these cases apart and adds two more
([What a load reports](#what-a-load-reports)).

## Versions and migrations

Players keep their saves across updates, so a release that renames, removes or reinterprets a saved property has to
read what older releases wrote. A `SaveSchema<T>` declares it: the version this build writes, and one step for each
earlier version that turns a save of that version into the next. The saved class holds the data in its current shape
and nothing about versions:

<!-- code: samples/Examples/SaveVersions.cs#data -->
```csharp
/// <summary>The saved data in its current shape, version 3. It holds no version itself: the schema does.</summary>
public sealed class Profile
{
    public int Gold { get; set; }
    public int Lives { get; set; } = 3;
}
```

<!-- code: samples/Examples/SaveVersions.cs#schema -->
```csharp
public const string Slot = "profile";

/// <summary>
/// This build writes version 3. One step for each earlier version: append a step for every change, and never edit
/// one that has shipped.
/// </summary>
public static readonly SaveSchema<Profile> Schema = new SaveSchema<Profile>(version: 3, SaveTypes.Default.Profile)
    .Migrate(from: 1, AddLives)
    .Migrate(from: 2, RenameCoins);

/// <summary>1 to 2: lives arrived; saves from before start with three.</summary>
public static void AddLives(JsonObject save) => save["Lives"] = 3;

/// <summary>2 to 3: "Coins" was renamed "Gold".</summary>
public static void RenameCoins(JsonObject save)
{
    save["Gold"] = save["Coins"]?.DeepClone();
    save.Remove("Coins");
}
```

Load and save through the schema, and the store does the rest:

<!-- code: samples/Examples/SaveVersions.cs#use -->
```csharp
/// <summary>The saved profile, or a new one; <paramref name="notice"/> is what to tell the player, if anything.</summary>
public static Profile Load(SaveStore store, out string notice)
{
    var loaded = store.Load(Slot, Schema);
    notice = loaded.Status switch
    {
        SaveLoadStatus.Migrated => $"Your save was updated from version {loaded.Version}.",
        SaveLoadStatus.TooNew => "This save is from a newer version of the game. Progress will not be saved.",
        SaveLoadStatus.MigrationFailed or SaveLoadStatus.Unreadable => "The save could not be read. Progress will not be saved.",
        SaveLoadStatus.Corrupt => "The save was damaged and has been set aside.",
        _ => "",   // Loaded, Missing
    };
    return loaded.Value ?? new Profile();
}

/// <summary>False when nothing was written: the medium refused, or the slot holds a save this build must not replace.</summary>
public static bool Save(SaveStore store, Profile profile) => store.Save(Slot, profile, Schema);
```

- **Versions are whole numbers from 1.** The store writes the version as a property of the saved JSON object,
  `"@version"`: the first one, or the first after the serializer's own (`"$type"` for a class with derived types). It
  removes it before the class is built: the class never sees it, and must not have a property serialized under that
  name. A build of the game without the schema reads past it like any property it does not know.
- **A save with no version is version 1.** That is every save written before the type had a schema, and every save
  written by `Save(slot, data)`. So a game adopts a schema at version 1 with no steps, at any time, and its players'
  saves keep loading; declaring it from the first release is what lets that release refuse a save from a later one.
- **Raise the version by one for each change of shape**, and add the step from the previous version. Adding a
  property needs neither: a missing property keeps the value the class gives it. A schema with a step missing throws
  `InvalidOperationException` every time it is used, so the gap is found in development and not by a player's old
  save.
- **A step changes the save as JSON** (`JsonObject`), before the class exists, because an old save may not fit the
  current class. A save from any release reaches the current shape through the same steps, in order. Never edit a step
  that has shipped: saves of that version are on players' machines.
- **Loading writes nothing.** The slot changes with the next save, when the game knows it is running correctly.
- **A save reads the slot first**, as a load would, whether or not the game loaded it, and acts on what it finds
  ([What a save does with what the slot holds](#what-a-save-does-with-what-the-slot-holds)).
- **The original of an older save is kept.** Before a save replaces a save of an older version, the store copies what
  the slot held to `<slot>.v<version>.backup` (`profile.v1.backup.json` for files). If the copy cannot be written, the
  save returns `false` and the slot is left alone ([Copies of older saves](#copies-of-older-saves)).
- **A newer save is never replaced.** A save written by a newer release (a player who went back to an older one, or a
  folder shared by two machines) holds things this build does not know and would drop. The store refuses to load it
  and refuses to save over it, with or without a schema, also when the game saves without having loaded first.
- **A versioned slot belongs to its schema.** `Save(slot, data)` and `Save(slot, data, typeInfo)` return `false`,
  and leave the slot alone, when the slot holds a save with a version, or when the game has loaded or saved that slot
  through a schema: written without the schema, the save would lose its version and be migrated again. The loads
  without a schema read a versioned slot, ignoring the version, and change nothing. Use one way of saving per slot.
- **The saved type is a JSON object**: a class with properties, or a dictionary. A schema for a list or a text
  throws `InvalidOperationException` when it is declared, before any slot is touched; a type with a converter of its
  own that writes something else throws the first time it is saved.

With `new SaveSchema<T>(version, typeInfo)` nothing uses reflection: steps work on JSON nodes and the class is read
and written with the game's own metadata, so versioned saves work in games published trimmed or with NativeAOT
([Trimmed and NativeAOT publishing](../guide/platforms.md#trimmed-and-nativeaot-publishing)). `new SaveSchema<T>(version)`
finds the properties by reflection, like `Save(slot, data)`, for games that are not published that way. Both write
the same text. Build a schema once and keep it in a static field.

### What a load reports

`Load(slot, schema)` returns a `SaveLoadResult<T>`: `Value` (the save, or `null`), `Status`, `Version` (the version
the save was written in) and `Error` (what went wrong, to log).

| `Status` | The slot held | `Value` | The slot afterwards | Later `Save` to the slot |
| --- | --- | --- | --- | --- |
| `Missing` | Nothing | `null` | — | Writes normally |
| `Loaded` | A save of the current version | The save | Unchanged | Writes normally |
| `Migrated` | A save of an older version | The save, brought up to date | Unchanged | Keeps a copy of the original, then writes |
| `TooNew` | A save from a newer release | `null` | Unchanged | Refused, returns `false` |
| `MigrationFailed` | An older save that a step threw on, or that does not fit the class after its steps | `null` | Unchanged | Refused, returns `false` |
| `Corrupt` | Invalid JSON, something that is not a JSON object, a version that is not a whole number from 1, or a save of the current version that does not fit the class | `null` | Set aside (`<slot>.corrupt`) | Writes a fresh slot |
| `Unreadable` | Something the medium could not read | `null` | Unchanged | Refused, returns `false` |

"Does not fit the class" covers whatever building the class throws: the serializer on a value of the wrong kind,
and the class's own code refusing a value (a property setter that throws). The exception is in `Error`.

`result.Protected` is `true` for the three statuses the store refuses to save over. The game can still be played
with a new save in memory; tell the player why progress will not be kept, as the example's `notice` does. The
protection follows the last load of the slot: a later load that succeeds lifts it (a file a synchronisation client
held at startup, read again once it is free), and so does `Delete`, the game's explicit choice to start over. It is
kept per store, and two names that differ only by letter case count as one slot.

### What a save does with what the slot holds

`Save(slot, data, schema)` reads the slot before it writes, as a load would, also when the game never loaded it or
another program wrote it since:

| The slot holds | The save |
| --- | --- |
| Nothing, or a save of the current version | Writes |
| A save of an older version | Copies the original ([Copies of older saves](#copies-of-older-saves)), then writes; returns `false` and writes nothing if the copy is refused |
| A save from a newer release | Returns `false`; the slot is left alone |
| An older save its steps fail on | Returns `false`; the slot is left alone |
| Something that is not a save (what a load reports as `Corrupt`) | Sets it aside (`<slot>.corrupt`), then writes |
| Something the medium cannot read | Returns `false` |

A slot the last load found `Protected` is refused before any of this. A save is therefore a read and a write; keep
slots small.

### Copies of older saves

The copy of an older save is named `<slot>.v<version>.backup`. A copy is never written over: when that name already
holds a different save of the same version, the new copy is `<slot>.v<version>.2.backup`, then `.3`, and a copy
identical to one already kept is not made twice. Copies are not slots: `Slots()` does not list them, and a slot name
cannot end with `.backup`. Nothing removes them but the game:

| Member | Behaviour |
| --- | --- |
| `Backups(slot)` | The copies kept of a slot, by name, with size and last write time |
| `LoadBackup(name, schema)` | Reads a copy through the schema and returns what a load would; it changes nothing (a copy is never set aside) |
| `DeleteBackup(name)` | Removes a copy |

To restore a copy, load it and save the value to the slot:

```csharp
var copy = Saves.LoadBackup("profile.v1.backup", ProfileSaves.Schema);
if (copy.Value is { } profile) Saves.Save("profile", profile, ProfileSaves.Schema);
```

If the slot was refused as `Protected`, delete it first (`Saves.Delete("profile")`); that is the point of asking the
player.

### Testing a schema

A step is an ordinary method that takes a `JsonObject`: call it with a JSON literal and check what it left. The whole
path, from the text a release wrote to the current class, needs no storage: `schema.Read(text)` returns the same
result a load would. Keep one literal save per released version in the game's tests:

```csharp
[TestMethod]
public void ASaveFromTheFirstReleaseStillLoads()
{
    var loaded = ProfileSaves.Schema.Read("""{ "Coins": 7 }""");   // what version 1 wrote
    Assert.AreEqual(SaveLoadStatus.Migrated, loaded.Status);
    Assert.AreEqual(7, loaded.Value!.Gold);
    Assert.AreEqual(3, loaded.Value.Lives);
}
```

`schema.Write(value)` is the other half: the text a save would hold.

### A save that already has a version of its own

A game that kept its own version number in the saved class (a `Version` property, say) reads that property in its
first step. To the store those saves have no `"@version"`, so all of them are version 1: the step from 1 finds the
game's own number in the JSON and brings each of its shapes up to date, and the schema is at version 2 or later. Then
remove the property from the class; a property of a save that the class does not have is ignored.

## Listing slots

`Saves.Slots()` returns the slots that hold a save, ordered by name, each with its `Name`, its `Size` and the time of
its `LastWrite`, without building any save. That is what a "load game" screen lists:

<!-- code: samples/Examples/SaveSlots.cs#slots -->
```csharp
/// <summary>The save games, newest first: every slot whose name starts with "save-".</summary>
public static IReadOnlyList<SlotInfo> SaveGames(SaveStore store) =>
    [.. store.Slots("save-").OrderByDescending(slot => slot.LastWrite)];

/// <summary>A line of the list: the slot and when it was last saved, when the medium knows.</summary>
public static string Describe(SlotInfo slot) =>
    slot.LastWrite is { } time ? $"{slot.Name}   {time.LocalDateTime:g}" : slot.Name;
```

- The `prefix` keeps the list to one kind of slot. Name save games alike (`"save-1"`, `"save-2"`, `"save-auto"`), so
  the settings, the achievements and the bindings, which share the store, stay out of the list.
- Copies the store keeps (corrupt data set aside, the original of a migrated save) are not slots and are not listed.
- `LastWrite` is the file's time on desktops and phones. A browser's `localStorage` keeps no times, so the storage
  stamps one on each write; a slot written before it did has `null`. A time that must survive copying a save to
  another machine belongs inside the save.
- `Size` is the file's size in bytes. In a browser it is the length of the stored text, and listing reads each
  slot's text to measure it, because `localStorage` has no size to ask for.
- The `prefix` is compared exactly, letter case included: `Slots("save-")` does not list `"SAVE-1"`.
- What a slot holds (the level reached, the play time, a save from a newer release) is in the save: load it, and
  show the `Status` when there is no value.
- A medium that cannot be listed gives an empty list; `Slots` does not throw for it.

## Settings

Settings (volume, language, window mode) are a save like any other: one slot of the game's store, with a class and a
schema of its own. There is no separate settings store, because nothing about settings needs one: they live where
the saves live, on every platform, and the same rules protect them.

<!-- code: samples/Examples/SaveSlots.cs#settings -->
```csharp
/// <summary>What the player chose on the options screen.</summary>
public sealed class Settings
{
    public float MusicVolume { get; set; } = 1;
    public float SoundVolume { get; set; } = 1;
    public bool Fullscreen { get; set; }
    /// <summary>The language the player chose, as a tag ("es"); null follows the system's language.</summary>
    public string? Language { get; set; }
}

public static readonly SaveSchema<Settings> SettingsSchema = new(version: 1, SaveTypes.Default.Settings);

/// <summary>The saved settings, or the defaults when there are none this build can use.</summary>
public static Settings LoadSettings(SaveStore store) =>
    store.Load("settings", SettingsSchema).Value ?? new Settings();

/// <summary>Called when the player changes a setting, not when the game saves its progress.</summary>
public static bool SaveSettings(SaveStore store, Settings settings) =>
    store.Save("settings", settings, SettingsSchema);
```

What sets settings apart is only how the game treats them:

- **They are not a save game.** Keep them out of the "load game" list by giving save games a prefix
  ([Listing slots](#listing-slots)), and out of "new game": deleting the adventure's slot leaves `"settings"` alone.
- **A value is always needed.** `?? new Settings()` covers every case where the load has none; the game starts with
  defaults and, if the slot is protected, does not replace it.
- **Save them when they change**, not with the game's progress.

Two things the engine saves for a game follow the same convention, each in its own slot of the same medium: key
bindings in `"input-bindings"` (`Input.SaveBindings(Saves.Storage)`, [A rebinding screen](input.md#a-rebinding-screen))
and achievements in `"achievements"` ([Achievements](achievements.md)).

## In the browser

The web host reports storage problems to the player: "Progress will not be kept" when the browser blocks site
storage (a session-only store is used), and "Progress could not be saved: site storage is full" on a quota error.
Closing or reloading the page ends the run properly so progress written at shutdown is saved. Browser storage
holds about 5 MB per origin, can be evicted and is discarded by private windows; see
[Requirements and limitations](../reference/limitations.md#saves).

## Limitations

- Saves are local to one machine or one browser profile; there is no cloud sync, export or import.
- JSON only, through `System.Text.Json`. A versioned save is a JSON object: a class with properties or a dictionary,
  not a list or a single value.
- A step changes one save at a time, as JSON; there is no migration across slots (splitting one slot into two, or
  merging two). A game does that itself, with `Load`, `Save` and `Delete`.
- A save reads the slot and then writes it; two programs writing the same slot between those two moments are not
  coordinated.
- A build of the game released before it used a schema knows nothing of versions: it reads a versioned save as it
  reads any save, and saves over it. Only builds with the schema refuse a newer save.
- `"@version"` is the store's own name at the root of every save, with or without a schema: a saved class or
  dictionary that has a property or key with that name at its root loses it on load. Name it something else.
- A type whose own converter writes something other than a JSON object cannot be told apart when its schema is
  declared: saving it through a schema throws, and loading an existing slot through that schema reports `Corrupt` and
  sets the slot aside. Save such a type without a schema, or wrap it in a class.
- A JSON object that names the same property twice at its root is `Corrupt` to a versioned load; a load without a
  schema keeps the last one.
- The loads without a schema catch what the serializer reports (`JsonException`) and set the slot aside; an exception
  thrown by the saved class's own code (a setter that refuses a value) reaches the caller. A versioned load reports
  both as a save that does not fit.
- One copy of corrupt data is kept per slot: setting a slot aside again replaces the earlier `<slot>.corrupt`.
- Text is read as UTF-8. Bytes that are not valid UTF-8 inside a text value load as the replacement character
  (U+FFFD); they do not make the save corrupt.
- A write the file system refuses after the temporary file was written (a read-only slot file) returns `false` and
  can leave `<slot>.json.tmp` in the save folder. It is not a slot and is not listed.
- `Save(slot, data)`, `Load<T>(slot)` and `new SaveSchema<T>(version)` find the saved type's properties by reflection,
  which a game published with .NET's `PublishTrimmed` or `PublishAot` does not have: such a game declares its saved
  types and passes their metadata (`Save(slot, data, typeInfo)`, `Load(slot, typeInfo)`,
  `new SaveSchema<T>(version, typeInfo)`), reading and writing the same slots
  ([Trimmed and NativeAOT publishing](../guide/platforms.md#trimmed-and-nativeaot-publishing)).
- A slot is a plain name; folders inside the save folder are not supported ([Slot names](#slot-names)).
- Two slots whose names differ only by letter case, or only by how an accented letter is encoded, are the same file on
  some systems.
- The save id names the save folder as given: use the game's application id (`com.example.mygame`). An id that is a
  path out of the data folder (it starts with `/` or `\`, starts with a drive such as `C:`, or has a `.` or `..`
  segment) throws `ArgumentException` when the folder is computed (`DefaultDirectory`, `DefaultStorage` and
  `new SaveStore(gameId)` outside browsers).
- Writes are synchronous; keep slots small (the sample games' saves are a few kilobytes).

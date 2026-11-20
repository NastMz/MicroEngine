# SAVE-2: versioned saves, migrations, slots and settings

Evaluation for NastMz/MicroEngine#176 (2026-11-19), part of
[CONSOLIDATION-1](consolidation-1.md) (task T6 of [`odd/tasks/consolidation-1.md`](../../odd/tasks/consolidation-1.md)).
Decision: [ADR 0063](../decisions/0063-versioned-saves-and-the-games-store.md). It reopens SAVE-1
(NastMz/MicroEngine#123), which left migrations to the game. No benchmark was run: saves are a few kilobytes written
on a player's action, and no decision here rests on speed.

## 1. Need

Any game with a second release. A saved class gains, renames or reinterprets a property, and the saves on players'
machines were written by every earlier release. Three things recur in every such game: reading the version, running
the upgrades in order, and not destroying a save the running build cannot understand (a player who went back a
version, or a folder synchronised between two machines). A game with several save games also needs to list them.

## 2. Current experience

- The manual told the developer to add a `Version` property, read the slot as `JsonObject`, loop over an array of
  upgrade lambdas, copy a backup and refuse newer saves: about sixty lines, repeated in `samples/Examples`.
- Trail Duel and Courtyard Key each kept a version property; neither had a migration path beyond a comment.
- A newer save loaded by an older build lost its unknown properties on the next save, silently.
- `ISaveStorage` could not be listed.
- Options and bindings had no stated home; `InputMap.SaveBindings` takes an `IStorage`, which `SaveStore` did not
  expose, so the rebinding example built a second storage by hand.
- All four demos and the tutorial constructed `new SaveStore(id, directory)`, bypassing `HostBuilder.Storage`.

## 3. Alternatives, and what others do

| Where | Approach |
| --- | --- |
| Unity, Godot, MonoGame | No save versioning in the engine. Games write a version field and upgrade by hand; store assets fill the gap |
| Unreal | `USaveGame` with custom versions per object (`FCustomVersion`); code branches on the version while reading |
| EF Core, Flyway, Room | Numbered, ordered, append-only migration steps registered with the store; refuse a database newer than the code |
| Protobuf, FlatBuffers | Schema evolution by rule (field numbers, optional fields); no steps, but no renames or reinterpretation either |
| `System.Text.Json` | Tolerant reading only: unknown properties ignored, missing ones defaulted |

The database model fits: the store owns the version and the order, the application owns each step. Tolerant reading
already covers added properties and stays as it is. Considered and rejected (ADR 0063, Alternatives): an envelope
around the data, a version property on the type, typed migrations between one class per version, a separate
settings store.

## 4. Target experience

```csharp
public static readonly SaveSchema<Profile> Schema = new SaveSchema<Profile>(version: 3, MySaves.Default.Profile)
    .Migrate(from: 1, save => save["Lives"] = 3)
    .Migrate(from: 2, save => { save["Gold"] = save["Coins"]?.DeepClone(); save.Remove("Coins"); });

var loaded = Saves.Load("profile", Schema);
if (loaded.Status == SaveLoadStatus.TooNew) Tell("This save is from a newer version.");
var profile = loaded.Value ?? new Profile();
Saves.Save("profile", profile, Schema);

foreach (var slot in Saves.Slots("save-")) List(slot.Name, slot.LastWrite);
```

## 5. Responsibility boundary

| The engine | The game |
| --- | --- |
| Where the version lives, reading it, the order of the steps | What each version meant: the body of each step |
| Refusing and protecting newer, failed and unreadable saves | What to tell the player |
| The copy of the original before a migrated slot is replaced | Whether and when to delete copies |
| Listing slots with size and time | Slot names, and what a save shows on a list (it is in the save) |
| One store per game on the composition's medium | The save id, when it must differ from the application id |

## 6. Integration

- `SaveSchema<T>`, `SaveLoadResult<T>`, `SaveLoadStatus` in `MicroEngine.Saves` (namespace `MicroEngine.Core`), beside
  `SaveStore`. `IStorage.List` and `SlotInfo` in `MicroEngine.Platform`; `FileSaveStorage` and `BrowserStorage`
  implement it, and the browser host's page gains `storage.keys`.
- `Game.Saves`, `Game.SaveId`, `Game.CreateSaves` and the optional store of `TrackAchievements` in `MicroEngine`;
  `GameHost.CreateGame` attaches the composition.
- The unversioned calls and their text are unchanged. Achievements keep their own format (ADR 0054, decision 5).

## 7. Trade-offs

- `"@version"` is a reserved property name in every versioned save; a type that serializes a property under it is
  refused at save time. `"$version"` was the first choice and was dropped: `System.Text.Json` rejects any unknown
  `$` property in a type with derived types or preserved references, so a build without the schema could not read
  the file at all.
- Steps are untyped JSON edits. A mistake shows as `MigrationFailed` (the slot protected, the error reported), which
  is why the schema is testable without a store.
- Every save reads the slot first, and a versioned save runs the schema over what it read: a second parse of a few
  kilobytes per save, in exchange for rules that do not depend on what the store remembers.
- A slot belongs either to a schema or to the calls without one. Mixing them is refused instead of being made to
  work.
- Copies of older saves accumulate until the game deletes them.
- `Game.Saves` cannot be used in a game's constructor when the composition has a medium; the alternative was
  ambient state.
- The browser's write time is a second key per slot and best effort; its listing reads every slot's text for the
  size.
- A build released before the game declared a schema cannot be taught to refuse a newer save.

## 8. Decision

Extend (ADR 0063): versioned saves with registered steps, a typed result, protection of newer saves, slot listing,
settings as a slot, and the store reached through the game.

## 9. Validation

Run on Windows x64, .NET SDK 11.0.100, this branch:

- Unit tests: `SaveSchemaTests`, `SaveVersionRulesTests`, `GameSavesTests`, and the browser storage's key handling
  and write order in `StorageSlotContractTests`; each rule was seen failing before it was implemented.
- An independent verification probed the first implementation outside the repository and found that the calls
  without a schema ignored versions, that protection outlived its cause, that a save without a load replaced older
  and unmigratable saves, that a copy could be written over or mislabelled, and that a `$`-prefixed version property
  broke polymorphic types. Each finding is a test in `SaveVersionRulesTests` and a decision in ADR 0063 (4 to 8).
- Each demo loads a literal save in the format its previous release wrote, saves it back with only the version line
  added, refuses a newer one, and saves on a storage its host composed. Courtyard Key and Trail Duel do not offer a
  save they cannot load as "continue", and say when a save was refused (`tests/<Game>.Tests`).
- `node sdk/tests/trimmed.mjs --aot`, whose program now keeps a versioned save (two steps, a property with derived
  types, a copy, a newer save refused): all checks passed, trimmed and as a NativeAOT executable.

Not verified:

- The browser: `BrowserStorage.List`, the time stamp and the page's `storage.keys` were not run in a browser. Their
  key handling and write order are unit-tested apart from the page.
- NativeAOT and trimming on any system but Windows x64.
- Android and iOS: nothing platform-specific changed, and nothing was run there.
- The demos' new title entries ("Newer save", "Unreadable") and messages were checked by tests, not looked at on
  screen.

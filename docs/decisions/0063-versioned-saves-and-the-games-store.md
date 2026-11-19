# 0063 — Versioned saves with registered migrations, slot listing, and the game's own store

Status: Accepted (2026-11-19), under the CONSOLIDATION-1 mandate. Evidence: [SAVE-2](../evaluations/save-2.md).
Supersedes the conclusion of SAVE-1 (NastMz/MicroEngine#123) that migrations stay in game code, and amends
[ADR 0054](0054-saves-without-reflection.md) (decision 2: "there is no migration").

## Context

`SaveStore` was safe (whole-slot writes, corrupt data set aside, an unreadable slot never overwritten) and had no
notion of a save's version. A game whose saved class changed shape between releases wrote the upgrade itself: the
manual showed about sixty lines of `JsonObject` code, the examples sample carried it, and two demos kept a version
property of their own. Nothing protected a save written by a newer release from an older one. A storage could not be
listed, so a "load game" screen kept its own index. All four demos built their store with `new SaveStore(id, dir)`,
so the composition's storage (`HostBuilder.Storage`, ADR 0038) reached none of them.

## Decision

1. **A saved type declares its versions.** `SaveSchema<T>` holds the version a build writes and one step per earlier
   version, `Migrate(from, Action<JsonObject>)`, which turns a save of version `from` into `from + 1`.
   `SaveStore.Load(slot, schema)` runs the steps a save needs, in order, and returns a `SaveLoadResult<T>`;
   `SaveStore.Save(slot, data, schema)` writes the current version. A schema with a step missing throws when it is
   used. Steps are plain methods over JSON and `SaveSchema<T>.Read(text)` needs no storage, so each step and each
   released format is unit-testable.
2. **The version is a property of the saved object: `"$version"`, written first.** The store adds it on save and
   removes it before the type is built, so the type never sees it. The rest of the text is what the unversioned
   calls write. No envelope: an existing file stays a valid save, and a versioned file still loads through
   `Load<T>(slot)`.
3. **A save without the property is version 1.** Versions are whole numbers from 1. A game adopts a schema at
   version 1 with no steps and every save it ever wrote loads unchanged; nothing on disk has to be rewritten.
4. **The result is typed.** `Missing`, `Loaded`, `Migrated`, `TooNew`, `MigrationFailed`, `Corrupt`, `Unreadable`,
   with the stored version and the error. Corrupt data is set aside as before. A save from a newer version, an older
   save whose steps failed and an unreadable slot are left untouched and the store refuses every save to that slot
   until `Delete`. A versioned save also looks at a slot it has not loaded before first replacing it, so a newer save
   is not replaced by a game that saves without loading.
5. **The original of a migrated save is kept.** Loading writes nothing. The first save over a slot loaded from an
   older version copies the stored text to `<slot>.v<version>.backup` first; without the copy the save is refused.
   Slot names ending in `.backup` are reserved by `SaveStore`.
6. **No reflection on the metadata path.** `new SaveSchema<T>(version, JsonTypeInfo<T>)` uses `JsonNode.Parse`,
   `JsonSerializer.SerializeToNode(value, typeInfo)`, `JsonSerializer.Deserialize(node, typeInfo)` and
   `JsonNode.WriteTo(Utf8JsonWriter)`, none of which is annotated for trimming or AOT. `new SaveSchema<T>(version)`
   is the reflection twin and carries the same annotations as `Save(slot, data)` (ADR 0054, decision 3).
7. **A storage lists its slots.** `IStorage.List()` returns `SlotInfo(Name, Size, LastWrite)`; `SaveStore.Slots(prefix)`
   filters out the store's copies and foreign names and orders by name. Files report their size and time; the
   browser storage stamps a time per write under a separate key root, because `localStorage` keeps none. The version
   is not in the listing: it would mean reading and parsing every slot, and `Load` reports it.
8. **Settings are a slot, not a type.** A settings class with its own schema in the game's store; save games take a
   name prefix so listings leave it out. Bindings and achievements already follow this shape. `SaveStore.Storage`
   exposes the medium so `InputMap.SaveBindings` writes where the saves are.
9. **A game has its store.** `Game.Saves` is created on first use by `Game.CreateSaves()` for `Game.SaveId` (the
   application id, else the title) on the composition's medium, else the platform's default. `GameHost.CreateGame`
   hands the composition to the game after constructing it; a game that used `Saves` in its constructor while the
   composition has a medium gets an exception that says so. `TrackAchievements(catalog)` uses that store. Building a
   `SaveStore` by hand remains, as the explicit alternative (tests, a folder of the game's choice).

## Alternatives

- **Leave migrations to the game (SAVE-1).** Every game that ships a second release writes the same version read,
  the same ordered loop, the same "do not overwrite a newer save" rule, and gets the last one wrong first.
- **An envelope (`{ "version": n, "data": { ... } }`).** Clean separation, but every existing file becomes a second
  format to detect, and the unversioned calls stop reading versioned files.
- **A version property on the saved class** (an interface or attribute). The type then carries persistence state,
  the store must reach it by reflection or a second generic constraint, and a too-new file is deserialized before
  the store can refuse it.
- **Typed migrations between versioned classes** (`ProfileV1 -> ProfileV2`). Type-safe, but every released shape
  stays in the game as a class and in its `JsonSerializerContext` forever; a rename is a line over JSON.
- **A separate settings store.** A second type with the same members over the same medium. Nothing a settings file
  needs differs from a slot.
- **An ambient "current composition" read by the `Game` constructor.** It would make `Saves` usable in constructors,
  through process state the composition path removed (ADR 0038).

## Consequences

- On disk: a slot written through a schema starts with `"$version": n`; a migrated slot gains a
  `<slot>.v<n>.backup` copy beside it; the browser gains one `microengine.time/<game>/<slot>` key per slot. Slots
  written by the unversioned calls are byte-for-byte what they were. No existing file is rewritten until the game
  saves it.
- Breaking, recorded in the migration notes: `IStorage` has a seventh member; slot names ending in `.backup` are
  refused; a game class that declares its own `Saves` member hides `Game.Saves`; `Save`/`Load` with a literal
  `null` third argument are ambiguous.
- The four demos and the samples use `Game.Saves` and a schema at version 1; their save ids and folders are
  unchanged. Trail Duel's `SaveVersion` and Courtyard Key's `Version` properties are gone; files that carry them
  still load.
- A JSON object with the same property name twice is corrupt to a versioned load (a JSON object in memory cannot
  hold it); the unversioned load keeps the last one.
- Not covered: migrations across slots, two processes writing one slot, cloud conflict resolution.
- Checked by `tests/MicroEngine.Tests/SaveSchemaTests.cs`, `Hosting/GameSavesTests.cs`,
  `StorageSlotContractTests.cs`, one save-format test per demo (`tests/<Game>.Tests`), `tests/Samples.Tests`, and
  the build's trim and AOT analyzers on `MicroEngine.Saves`. `node sdk/tests/trimmed.mjs` does not yet exercise a
  schema (evaluation, section 9).

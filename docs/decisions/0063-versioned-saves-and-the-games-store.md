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
2. **The version is a property of the saved object: `"@version"`.** The store adds it on save, after the
   serializer's own leading properties (`"$type"`), and removes it before the type is built, so the type never sees
   it. The rest of the text is what the unversioned calls write. No envelope: an existing file stays a valid save.
   The name does not start with `$`: `System.Text.Json` refuses every unknown `$` property in a type that reads
   metadata (derived types, preserved references), wherever it sits, so a `"$version"` would make the file
   unreadable to a build of the game that has no schema. A type that serializes a property under the name is refused
   when it is saved.
3. **A save without the property is version 1.** Versions are whole numbers from 1. A game adopts a schema at
   version 1 with no steps and every save it ever wrote loads unchanged; nothing on disk has to be rewritten. A
   version property that is present and is not a whole number from 1 (`null` included) is corrupt.
4. **The result is typed, and protection follows the last load.** `Missing`, `Loaded`, `Migrated`, `TooNew`,
   `MigrationFailed`, `Corrupt`, `Unreadable`, with the stored version and the error. Corrupt data is set aside as
   before. A save from a newer version, an older save whose steps failed and an unreadable slot are left untouched,
   and the store refuses every save to that slot until a later load of it succeeds or the game deletes it. Whatever
   building the type throws, except the serializer's `NotSupportedException` (a type it cannot read at all), is a
   save that does not fit: `Corrupt` at the current version, `MigrationFailed` below it.
5. **A versioned save reads the slot as a load would, every time.** No state remembered from an earlier call
   decides it: nothing or a current save is replaced; an older save is copied and replaced; a newer save, or an older
   one its steps fail on, is refused; text that is not a save is set aside and replaced; an unreadable slot is
   refused. The rule therefore holds on a fresh store, under any spelling of the name the storage accepts, and when
   another program wrote the slot since the load.
6. **A versioned slot belongs to its schema.** The calls without a schema refuse to write a slot whose stored text
   carries a version, or that this store has loaded or saved through a schema: they would drop the version, and the
   next versioned load would run every step again over data already in the current shape. They read a versioned
   slot, without its version. A slot that never had a version is written exactly as before.
7. **The original of an older save is kept, and no copy is ever written over.** Loading writes nothing. Before a
   save replaces an older save it copies the stored text to `<slot>.v<version>.backup`; when that name holds a
   different text, to `<slot>.v<version>.2.backup` and so on; an identical copy is not made twice; without the copy
   the save is refused. The version in the name is the one read from the text being copied. Slot names ending in
   `.backup` are reserved by `SaveStore`, and `Backups(slot)`, `LoadBackup(name, schema)` (read-only) and
   `DeleteBackup(name)` reach the copies.
8. **A schema is for a type that is a JSON object.** A list or a text is refused when the schema is declared
   (`InvalidOperationException`), before any slot is touched; a type with a converter of its own, when it is first
   written.
9. **No reflection on the metadata path.** `new SaveSchema<T>(version, JsonTypeInfo<T>)` uses `JsonNode.Parse`,
   `JsonSerializer.SerializeToNode(value, typeInfo)`, `JsonSerializer.Deserialize(node, typeInfo)` and
   `JsonNode.WriteTo(Utf8JsonWriter)`, none of which is annotated for trimming or AOT. `new SaveSchema<T>(version)`
   is the reflection twin and carries the same annotations as `Save(slot, data)` (ADR 0054, decision 3); it throws
   when reflection is switched off, at declaration, so that failure is never read as a save that does not fit.
10. **A storage lists its slots.** `IStorage.List()` returns `SlotInfo(Name, Size, LastWrite)`; `SaveStore.Slots(prefix)`
   filters out the store's copies and foreign names and orders by name. Files report their size and time; the
   browser storage stamps a time after each write under a separate key root, because `localStorage` keeps none, and
   reads each slot's text to give its size. The version
   is not in the listing: it would mean reading and parsing every slot, and `Load` reports it.
11. **Settings are a slot, not a type.** A settings class with its own schema in the game's store; save games take a
   name prefix so listings leave it out. Bindings and achievements already follow this shape. `SaveStore.Storage`
   exposes the medium so `InputMap.SaveBindings` writes where the saves are.
12. **A game has its store.** `Game.Saves` is created on first use by `Game.CreateSaves()` for `Game.SaveId` (the
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

- On disk: a slot written through a schema carries `"@version": n`; a migrated slot gains a
  `<slot>.v<n>.backup` copy beside it (numbered when one exists); the browser gains one
  `microengine.time/<game>/<slot>` key per slot. Slots written by the unversioned calls are byte-for-byte what they
  were. No existing file is rewritten until the game saves it.
- Every save is a read and a write. A versioned save also runs the schema over what it read.
- Breaking, recorded in the migration notes: `IStorage` has a seventh member; slot names ending in `.backup` are
  refused; the calls without a schema refuse a slot that holds a version; a game class that declares its own `Saves`
  member hides `Game.Saves`; `Save`/`Load` with a literal `null` third argument are ambiguous.
- The four demos use `Game.Saves` and a schema at version 1; their save ids and folders are unchanged, and so are
  the samples' (`SaveId`). Trail Duel's `SaveVersion` and Courtyard Key's `Version` properties are gone; files that
  carry them still load. Those two games offer "continue" only for a save they can load, and say when a save was
  refused.
- A build released before a game declared a schema knows nothing of versions and replaces a versioned save like any
  other; only builds with the schema refuse a newer one.
- A JSON object with the same property name twice is corrupt to a versioned load (a JSON object in memory cannot
  hold it); the unversioned load keeps the last one.
- Not covered: migrations across slots, two processes writing one slot, cloud conflict resolution.
- Checked by `tests/MicroEngine.Tests/SaveSchemaTests.cs`, `SaveVersionRulesTests.cs`, `Hosting/GameSavesTests.cs`,
  `StorageSlotContractTests.cs`, the save tests of each demo (`tests/<Game>.Tests`), `tests/Samples.Tests`, the
  build's trim and AOT analyzers on `MicroEngine.Saves`, and `node sdk/tests/trimmed.mjs`, whose program keeps a
  versioned save (`--aot` adds the NativeAOT publish and run).

# 0054 — Saves without reflection through the game's own type metadata

Status: Accepted (2026-11-13), by the owner. Evidence: [AOT-1](../evaluations/aot-1.md).

## Context

`SaveStore` turns a game's own type into a slot's JSON text with `System.Text.Json`'s reflection API. A program
published trimmed or with NativeAOT has that API switched off: the first save throws `InvalidOperationException`, and
the game's build said nothing beforehand (the only sign was an aggregate warning naming `MicroEngine.Saves` at publish
time). The web, Android and iOS hosts avoid it by keeping partial trimming and the reflection switch; a developer who
publishes a desktop game trimmed, or with NativeAOT, has no such host. The engine had the same problem in its own
code: `AchievementTracker` kept its state through those reflection calls.

The storage side (`IStorage`: named texts, written whole) uses no reflection. The problem is only the conversion of an
arbitrary game-defined type, which no change of container (binary, custom format) removes.

Four designs were prototyped and run trimmed (evaluation, section 3): source-generated `JsonTypeInfo<T>` passed by the
game, hand-written codecs, a hand-written binary layout, and a resolver given to the store once with unchanged call
sites.

## Decision

1. **A second pair of calls takes the game's metadata**: `SaveStore.Save<T>(slot, data, JsonTypeInfo<T>)` and
   `SaveStore.Load<T>(slot, JsonTypeInfo<T>)`. The game declares its saved root types in a `JsonSerializerContext`; the
   C# compiler generates the metadata. No reflection, no dynamic code, no new package, no build step.
2. **One slot format.** The store writes the value through its own indented JSON writer, so the text is what
   `Save(slot, data)` writes for the same type, whatever the context's options say about indentation. Either pair
   reads the other's slots, and slots written by earlier versions load unchanged. JSON stays; there is no migration.
3. **The reflection pair stays, unchanged in how it serializes**, as the convenient default for games that are not
   trimmed (every game the SDK packages today). It carries `RequiresUnreferencedCode` and `RequiresDynamicCode`, so a
   project that opts into trimming or AOT analysis is told, at its own call, which overload to use.
4. **No second serialization contract.** A game that wants hand-written conversion puts a `JsonConverter<T>` in its
   context or writes its own text to `IStorage`.
5. **The engine's own saved types use the same path.** `AchievementTracker` saves `AchievementSaveData` with an
   internal context (`AchievementSaveTypes`) and the metadata calls; no engine code calls the reflection pair. Its file
   is byte-for-byte what it was, and the game declares nothing for it.
6. **No NativeAOT claim.** The SDK states what was run: declared types and achievements save and load in trimmed
   programs. NativeAOT is not a packaging target and is not advertised until a game has been published and exercised
   end to end on each claimed target; one run on one system (below) is not that.

## Alternatives

- **A resolver on the store, call sites unchanged.** The friendliest to adopt, but the analyzers cannot check it: a
  type missing from the context throws `NotSupportedException` when its slot is first touched, on the player's machine.
  With the metadata as an argument the same mistake is a compile error.
- **A codec interface implemented by the game.** Works, and is already possible through `IStorage` or a converter;
  every property is written twice by hand.
- **A binary or schema-driven format.** Smaller and faster (76 against 219 bytes, about 2 µs against 11 µs for the
  prototype's model), on data that is a few kilobytes written on a player's action. It needs a new persisted format, a
  migration for every existing save, base64 in browser storage, and the same per-type code.
- **Splitting Saves into its own package.** Not needed: once the reflection calls are annotated, `MicroEngine.Saves`
  produces no trim or AOT warning for a consumer that does not call them.

## Consequences

- A trimmed or NativeAOT game adds one context class and one argument per call; its existing saves keep loading.
- A project that calls `Save(slot, data)` or `Load<T>(slot)` gets `IL2026` at that call once it sets `PublishTrimmed`,
  `IsTrimmable` or `EnableTrimAnalyzer`, and also `IL3050` with `PublishAot`, `IsAotCompatible` or
  `EnableAotAnalyzer`; with warnings as errors its build fails there instead of the game failing at run time.
- `MicroEngine.Saves` and `MicroEngine.Achievements` are marked `IsAotCompatible`: the analyzers guard them from now
  on, so a reflection call added to either fails the build.
- A trimmed or NativeAOT game that tracks achievements needs no reflection switch and no partial trimming.
- Observed, on Windows x64 only (evaluation, sections 4 and 5): the external consumer of the packed packages saves,
  loads and tracks achievements as a trimmed program and as a NativeAOT executable; `samples/HelloEngine` runs as a
  NativeAOT executable. Open: every other system, and Box2D under NativeAOT (no Box2D native library on the machine
  used).
- Checked by `tests/MicroEngine.Tests/SaveStoreTypeInfoTests.cs`, `AchievementSaveFormatTests.cs` (the achievements
  file, byte for byte, and files earlier versions wrote) and, from the packed packages outside the repository, by
  `node sdk/tests/trimmed.mjs` (`--aot` adds the NativeAOT publish and run).

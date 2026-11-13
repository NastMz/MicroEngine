# AOT-1: NativeAOT and trimming compatibility; saves without reflection

Evaluation for NastMz/MicroEngine#150 (2026-11-13). Task record: [`odd/tasks/aot-1.md`](../../odd/tasks/aot-1.md).
Decision: [ADR 0054](../decisions/0054-saves-without-reflection.md).

Two findings are kept apart throughout, as the issue asks:

- **A. Saves needed reflection.** `SaveStore` serialized the game's types with `System.Text.Json`'s reflection API. A
  trimmed or NativeAOT program has that API switched off, so saving failed at run time. This is a property of the SDK,
  on every machine. It is fixed by this work for the game's own types (section 4).
- **B. This Windows machine cannot link NativeAOT programs.** The NativeAOT compiler (ILC) runs here and reports its
  analysis, but the native link step fails (section 1.3). This is a property of the machine. It says nothing about the
  SDK, and it means no NativeAOT program was *run* in this work.

Fixing A is not a certification of the SDK under NativeAOT (section 6).

## 1. Baseline

### 1.1 Environment

| | |
| --- | --- |
| Machine | Windows 11 Pro 10.0.26300, x64 |
| .NET SDK | 10.0.404 inside the repository (`global.json`); 11.0.100 outside it (the external consumers) |
| Target framework, runtime identifier | `net10.0`, `win-x64` |
| NativeAOT compiler | `Microsoft.DotNet.ILCompiler` 10.0.15 (restored by `PublishAot`) |
| Packages | `dotnet pack MicroEngine.slnx -c Release` of `main` at `16af5af` (`0.6.0-alpha.1`), into a scratch feed |

### 1.2 The external consumer

A console project outside the repository, `MicroEngine` package only, with its own types (`Progress` holding a `Hero`
with a list), calling `store.Save("progress", progress)` and `store.Load<Progress>("progress")`. With
`<PublishAot>true</PublishAot>` in the project:

| Command | Result on `main` |
| --- | --- |
| `dotnet build -c Release` | **0 warnings.** The analyzers see no annotation on `SaveStore`, so the game's build says nothing |
| `dotnet publish -c Release -r win-x64` (NativeAOT) | ILC: `IL2104: Assembly 'MicroEngine.Saves' produced trim warnings`, `IL3053: Assembly 'MicroEngine.Saves' produced AOT analysis warnings`; then the link fails (1.3) |
| the same with `-p:TrimmerSingleWarn=false` | `SaveStore.cs(145)`: `IL2026` and `IL3050` on `JsonSerializer.Serialize<T>(T, JsonSerializerOptions)` in `SaveStore.Save<T>(String, T)`; `SaveStore.cs(122)`: `IL2026` and `IL3050` on `JsonSerializer.Deserialize<T>(String, JsonSerializerOptions)` in `SaveStore.Load<T>(String)` |
| `dotnet publish -c Release -r win-x64 -p:PublishAot=false -p:PublishTrimmed=true -p:SelfContained=true` | ILLink: the same two `IL2026` |
| running that trimmed program | **`InvalidOperationException: Reflection-based serialization has been disabled for this application.`** thrown out of `SaveStore.Save` |

So the confirmed failure is at run time, in the first save, and the only build-time sign was an aggregate warning
naming an engine assembly the developer cannot change. `PublishTrimmed` (which `PublishAot` implies) sets
`System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault` to false; that switch, not the trimming of the game's
types, is what stops the save.

Setting `<JsonSerializerIsReflectionEnabledByDefault>true</JsonSerializerIsReflectionEnabledByDefault>` and
`<TrimMode>partial</TrimMode>` makes the trimmed program save and load again (run here, from clean intermediates). That
is what the web, Android and iOS hosts do (`src/MicroEngine.Cli/Hosts/*/Host.csproj`), which is why the shipped
packages work. NativeAOT has no such escape: it has no reflection serializer to switch back on.

### 1.3 The NativeAOT link on this machine

Every `PublishAot` publish here ends, after `Generating native code`, with

```
"vswhere.exe" no se reconoce como un comando interno o externo
link: extra operand '/DEF:obj\\Release\\net10.0\\win-x64\\native\\<name>.def'
Microsoft.NETCore.Native.targets(396,5): error MSB3073: El comando ""link" @"obj\Release\net10.0\win-x64\native\link.rsp"" salió con el código 1.
```

ILC's `findvcvarsall.bat` fails inside `vcvarsall.bat`, so the `link` found on `PATH` is the GNU coreutils one. Visual
Studio 18 Community has the MSVC linker (`VC\Tools\MSVC\14.52.37025\bin\Hostx64\x64\link.exe`) and `vswhere.exe` is
in its usual folder, but no Windows SDK import libraries (`kernel32.lib`) are under `Windows Kits`, so pointing the
build at that linker would not link either. Nothing was installed. **Consequence: on this machine ILC's whole-program
analysis is observable, a NativeAOT executable is not.** Trimmed self-contained programs (ILLink, no native linker)
were built and run instead; they exercise the same "no reflection serializer" condition.

## 2. Inventory of Saves

| Entry point | What it does | Reflection |
| --- | --- | --- |
| `SaveStore.Save<T>(string slot, T data)` | `JsonSerializer.Serialize(data, Options)` then `IStorage.Write`; false on `IOException`/`UnauthorizedAccessException` or when the slot was unreadable | **yes**: `IL2026`, `IL3050` |
| `SaveStore.Load<T>(string slot) where T : class` | `IStorage.Read` then `JsonSerializer.Deserialize<T>(text, Options)`; null and set aside on `JsonException` or JSON `null`; null and slot protected on a read failure | **yes**: `IL2026`, `IL3050` |
| `Exists`, `Delete`, `Directory`, the constructors, `DefaultStorage`, `DefaultDirectory` | storage only | no |
| `FileSaveStorage` | `<slot>.json`; write to `<slot>.json.tmp` then `File.Move(overwrite)`; corrupt file renamed `<slot>.corrupt.json` | no |
| `SavesModule` | registers the store as a service | no |

- **Generics.** `T` is unconstrained on `Save` and a class on `Load`. Nothing about `T` is known to the engine at
  build time; this is the whole problem.
- **JSON options.** One static `JsonSerializerOptions { WriteIndented = true }`: property names as declared,
  case-sensitive, default encoder, the platform's newline. No converters, no naming policy, no resolver.
- **Nested and polymorphic models.** Whatever the reflection serializer accepts: public properties, collections,
  dictionaries, `[JsonPolymorphic]`/`[JsonDerivedType]`. `JsonObject` and other `JsonNode` types work as `T`.
- **Versioning and migrations.** None built in. The documented pattern (`samples/Examples/SaveVersions.cs`) loads the
  slot as `JsonObject`, upgrades it, and calls `json.Deserialize<Profile>()`: a second reflection call, in game code.
- **Error handling.** Missing: null. Corrupt: null, data set aside. Unreadable: null, slot never written by that store
  until deleted. Refused write: false. With reflection switched off, `InvalidOperationException` escapes both calls
  (it is not a `JsonException`); that is deliberate to keep: it is a build configuration error, not bad data.
- **File compatibility.** A slot is plain JSON text; nothing in it names a format version or a .NET type.
- **Other callers in the SDK.** `AchievementTracker` saves and loads `AchievementSaveData` through the two reflection
  calls (`src/MicroEngine.Achievements/Achievements.cs:157`, `:185`). Input bindings do not: `InputMap` writes its own
  JSON (`Bindings.cs:230`).

**Storage format and object serialization are separate problems.** The storage side (`IStorage`: named texts written
whole) uses no reflection and needed no change. What needs reflection is turning an arbitrary `T` into that text and
back. Replacing JSON with a binary or custom container changes the text, not that conversion: prototype 3 below still
needed the game to write, by hand, exactly the per-type code that reflection was providing.

**Consumers that do not use Saves.** On `main` they could not avoid the warning: `MicroEngine.Saves.dll` ships inside
the `MicroEngine` package, and PHYS-1 recorded the `PhysicsSubstitution` sample, which never saves, failing its NativeAOT
publish "in `MicroEngine.Saves`" (`odd/tasks/phys-1.md:64`). Nothing fails at run time for them, since the reflection call is never executed. No package split is
needed to fix this: once the two calls carry `RequiresUnreferencedCode`/`RequiresDynamicCode`, `MicroEngine.Saves`
produces no warning of its own (section 5). What remains for such a consumer is the achievements caller above.

## 3. Alternatives, prototyped

One prototype project (`proto/` in the scratch folder), published trimmed and run, with the same model for every
option: a nested object, a list, a dictionary, an enum and a nullable string. Times and allocations are per call on
this machine, one run, for comparison between options only.

| | Option | Runs trimmed | Slot bytes | Write | Read | A missing type is found |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | Reflection (`main`) | **no**, `InvalidOperationException` | – | – | – | – |
| 1 | `System.Text.Json` source generation: the game declares a `JsonSerializerContext` and passes `JsonTypeInfo<T>` | yes | 219 | 11.1 µs, 1448 B | 10.6 µs, 1232 B | at **compile time** (`Default.X` does not exist) |
| 2 | A codec the game writes by hand (`Utf8JsonWriter`/`JsonDocument`), same JSON | yes | 219 | 8.1 µs, 936 B | 7.4 µs, 680 B | at compile time, but every property is written twice by hand |
| 3 | A hand-written binary layout, base64 in the text slot | yes | 76 | 1.7 µs, 584 B | 1.7 µs, 808 B | at compile time; any change to the type silently breaks old saves |
| 4 | The store is given the game's resolver once; `Save(slot, data)`/`Load<T>(slot)` keep their shape and look `T` up | yes | 219 | 19.2 µs, 5948 B | 8.0 µs, 7625 B (options built per call in the prototype) | at **run time**, on the player's machine: `NotSupportedException: JsonTypeInfo metadata for type 'Forgotten' was not provided` |

Both JSON options read a slot in today's format (`compat 1: True`, `compat 2: True`).

| Criterion | 1 Source generation | 2 Hand-written codec | 3 Binary / own format | 4 Resolver on the store |
| --- | --- | --- | --- | --- |
| Simple project | one attribute per saved root type, one extra argument per call | dozens of lines per type | dozens of lines per type, plus versioning | one attribute per type, one line at start-up |
| Advanced project | nested, generic, polymorphic and `JsonNode` types, converters and naming policies through the context | unlimited, all by hand | unlimited, all by hand | as 1 |
| Tooling | the C# compiler's built-in generator; no package, no build step | none | none (or a schema compiler the SDK would have to ship) | as 1 |
| Diagnostics | compile error for an undeclared type; `IL2026`/`IL3050` on the old calls | none | none | none at build; exception when the slot is touched |
| Old saves | same JSON, read as is | same JSON if written carefully | **a new persisted format**: a migration for every existing save | same JSON |
| Forward/backward compatibility | unknown properties ignored, missing ones keep defaults (as today) | whatever the author writes | positional: needs explicit versions | as 1 |
| Desktop, phones, WebAssembly | `System.Text.Json` is in the framework on all of them | same | same; base64 costs 33% in `localStorage` | as 1 |
| Migration cost for a game | add a context, add an argument | rewrite saving | rewrite saving and migrate files | add a context and one line |

Reading:

- **3 does not earn its cost.** Saves are a few kilobytes written on a player's action; microseconds and a hundred
  bytes are not a measurable need, and the price is a new persisted format plus hand-written or generated per-type code
  anyway.
- **2 is already possible without a new contract**: a game can write any text to `IStorage` itself, or put a custom
  `JsonConverter<T>` in its context. A second public codec interface would only wrap that.
- **4 keeps call sites untouched but moves the failure to the player.** The analyzers cannot prove the lookup, so a
  type missing from the context ships and throws when that slot is first saved. Saves are exactly where a late failure
  costs the most.
- **1 fails at compile time, needs no tooling beyond the compiler, and keeps the file.**

## 4. What is implemented

Option 1, additively (`src/MicroEngine.Saves/SaveStore.cs`):

- `bool Save<T>(string slot, T data, JsonTypeInfo<T> typeInfo)` and `T? Load<T>(string slot, JsonTypeInfo<T> typeInfo)`.
  Same semantics as the existing pair in every respect but the conversion: missing, corrupt (set aside), unreadable
  (protected), refused write, atomic file write.
- The store, not the game's context, decides the text: the value is written through a `Utf8JsonWriter` with
  `Indented = true`, so a slot is byte-for-byte what `Save(slot, data)` writes for the same type, whatever
  `WriteIndented` the context declares. One file format; either pair reads the other's slots; files written by earlier
  versions load unchanged. No migration exists because none is needed.
- `Save<T>(string, T)` and `Load<T>(string)` are unchanged in behaviour and now carry `RequiresUnreferencedCode` and
  `RequiresDynamicCode`. A project that sets `PublishTrimmed`, `PublishAot` or `IsAotCompatible` gets `IL2026` and
  `IL3050` on its own line that calls them, with a message naming the overload to use. Other projects see nothing.
- `MicroEngine.Saves` is marked `IsAotCompatible`, so the analyzers check it on every build.
- `samples/Examples/SaveTypes.cs` declares the examples' saved types and is what the guide quotes
  ([Trimmed and NativeAOT publishing](../guide/platforms.md#trimmed-and-nativeaot-publishing)).

Tests: `tests/MicroEngine.Tests/SaveStoreTypeInfoTests.cs` (round trip of a nested model with collections, an enum and
derived types; identical text on both paths; slots as earlier versions wrote them, with either newline, compact,
reordered and with a removed property; three kinds of corrupt data; unreadable slot; refused write; file slots with no
temporary file left and a truncated file set aside; a slot read as `JsonObject` and upgraded; null metadata) and
`TheDeclaredSaveTypesReadAndWriteTheSameSlot` in `tests/Samples.Tests`. The existing `SaveStore` tests are untouched
and pass.

External consumer, `node sdk/tests/trimmed.mjs --feed <feed> [--aot]`, from the packed packages, outside the
repository, observed here:

| Check | Result |
| --- | --- |
| typed game: `dotnet publish -r win-x64 -p:PublishTrimmed=true --self-contained -warnaserror` | passes, no warning |
| typed game: the trimmed program | `missing: True`, `save: True`, `load: 3 Ada 2 Hard 1200 5 cellar`, `old: 7 Pip 0`, `corrupt: True True False` |
| plain game (reflection calls): `dotnet build -p:PublishAot=true` | `IL2026` and `IL3050` at `Program.cs`, naming `SaveStore.Save`/`Load` |
| plain game: trimmed publish | no `IL2104`/`IL3053` for an engine assembly any more; the trimmed program reports `InvalidOperationException` |
| typed game, `--aot`: NativeAOT compilation (ILC) | **no trim or AOT warning** |
| typed game, `--aot`: NativeAOT link and run | **not runnable on this machine** (1.3) |

## 5. Audit of the rest of the SDK

Three kinds of evidence, in increasing strength; none is an end-to-end NativeAOT run.

**5.1 Analyzers, per project** (`dotnet build src/<P>/<P>.csproj -c Release -p:IsAotCompatible=true`). Seventeen projects
already set `IsAotCompatible` and build without warnings under `TreatWarningsAsErrors`: Kernel, Platform, Hosting,
Content, Graphics, Graphics2D, Graphics3D, Scene, Input, Audio, Text, Text.Shaping, UI, Physics, Physics.Tiles,
Physics.Box2D, Tiled. Forcing the property on the ones that do not set it, on `main`:

| Project | Own warnings |
| --- | --- |
| `MicroEngine` (the facade), `MicroEngine.Achievements`, `MicroEngine.Desktop`, `MicroEngine.Sdl`, `MicroEngine.Raylib`, `MicroEngine.Graphics.SdlGpu`, `MicroEngine.Input.Touch` | none of their own (only the four in `SaveStore.cs`, for those that reference it) |
| `MicroEngine.Saves` | `SaveStore.cs(122)`, `(145)`: `IL2026`, `IL3050` — fixed here |
| `MicroEngine.Cli` | `Hosts.cs(32, 54, 72, 160, 222, 227, 407)`, `Packaging.cs(150)`: `IL2026`, `IL3050` on `JsonSerializer.Serialize`. A build tool that runs on the developer's machine under the .NET SDK; never part of a game |

After the change, `MicroEngine.Achievements` would report the two calls at `Achievements.cs:157` and `:185` if it set
the property; it does not, so the solution builds clean.

**5.2 Whole-program analysis** (ILC and ILLink over a real program, `-p:TrimmerSingleWarn=false`, after the change):

| Program | Tool | Warnings |
| --- | --- | --- |
| `samples/HelloEngine` (window, SDL and raylib platforms, sprites, synth audio, scenes) | ILC (`PublishAot`) | **none** |
| `samples/HelloEngine` | ILLink (`PublishTrimmed`, full trim mode) | `Achievements.cs(157)`, `(185)`: `IL2026` |
| `samples/PhysicsSubstitution` (tile and Box2D physics, console) | ILC | `Achievements.cs(157)`, `(185)`: `IL2026`, `IL3050` |
| external typed consumer (`MicroEngine` package, saves) | ILC and ILLink | none |

**5.3 Trimmed programs run:** the external typed consumer (section 4), and `samples/HelloEngine` published
`PublishTrimmed`, self-contained, full trim mode (31 MB), run with
`--script hello.script --hidden --mute --captures captures`: `done: 70 updates, 70 frames`, and the capture shows the
title, the sprite and the hint text.

Classification:

| | |
| --- | --- |
| **Confirmed failure, fixed** | Saving the game's own types in a trimmed or NativeAOT program (`MicroEngine.Saves`) |
| **Confirmed blocker, open** | `AchievementTracker` with a store: reflection calls at `Achievements.cs:157`, `:185`. In a trimmed or NativeAOT program, saving achievements throws. The fix is the same pattern (a context for `AchievementSaveData` inside `MicroEngine.Achievements`, calling the new overloads) and is outside this work's edit surface |
| **Warnings, not a game's concern** | `MicroEngine.Cli` |
| **Game code, not SDK** | `json.Deserialize<Profile>()` in the documented upgrade pattern (`samples/Examples/SaveVersions.cs`, quoted by `docs/manual/persistence.md`): a trimmed game uses `json.Deserialize(SaveTypes.Default.Profile)` |
| **Verified** | A game's own types save and load in a trimmed self-contained Windows x64 program; a windowed sample runs trimmed on Windows x64; ILC compiles a windowed sample and the saves consumer without warnings |
| **Not tested: toolchain gap** | Any NativeAOT executable, on any system (no native link here). NativeAOT of the native dependencies' static or dynamic linking (SDL3, raylib, HarfBuzz, Box2D) is therefore unknown |
| **Not tested: no packages or platform here** | A Box2D-only external consumer from packages: the Box2D package needs its native libraries built (`artifacts/box2d`), which this machine does not have; `PhysicsSubstitution` was analyzed from source instead. Linux, macOS, Android, iOS and WebAssembly under full trimming or AOT. `MicroEngine.Tiled` (DotTiled) and `MicroEngine.Text.Shaping` (HarfBuzzSharp) in a published trimmed program |

## 6. Claims

The SDK does not claim NativeAOT support. What the public documentation states is limited to what was run: saves with
declared types work in trimmed and NativeAOT builds *as far as serialization is concerned*, the calls without metadata
do not, achievements need the reflection switch, and NativeAOT is not a packaging target. A claim of NativeAOT support
for a target needs an end-to-end game published and exercised on that target; `node sdk/tests/trimmed.mjs --aot` on a
machine with a native toolchain is the first step for the saves part, not the certification.

## 7. Needs the owner's decision

1. **Achievements.** Apply the same pattern in `MicroEngine.Achievements` (internal context for `AchievementSaveData`,
   no public API change, same file). Small; outside this work's surface.
2. **NativeAOT run.** Run `node sdk/tests/trimmed.mjs --feed <feed> --aot` where a native toolchain exists (the Linux
   CI runner has clang), and decide whether it becomes a CI check. CI files were outside this work's surface.
3. **Box2D-only external consumer under NativeAOT** needs the Box2D natives and the same toolchain; it stays open.
4. **The manual** (`docs/manual/persistence.md`) should link the new section from its limitations and show the typed
   upgrade call; it was outside this work's surface.
5. **Hosts.** The web, Android and iOS hosts keep `TrimMode=partial` and the reflection switch. Moving them to full
   trimming would make every game declare its saved types: a breaking change for games, not proposed.

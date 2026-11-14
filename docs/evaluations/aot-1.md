# AOT-1: NativeAOT and trimming compatibility; saves without reflection

Evaluation for NastMz/MicroEngine#150 (2026-11-13). Task record: [`odd/tasks/aot-1.md`](../../odd/tasks/aot-1.md).
Decision: [ADR 0054](../decisions/0054-saves-without-reflection.md).

Two findings are kept apart throughout, as the issue asks:

- **A. Saves needed reflection.** `SaveStore` serialized the game's types with `System.Text.Json`'s reflection API. A
  trimmed or NativeAOT program has that API switched off, so saving failed at run time. This is a property of the SDK,
  on every machine. It is fixed by this work for the game's own types (section 4).
- **B. This Windows machine did not link NativeAOT programs as it was first used.** The NativeAOT compiler (ILC) ran
  and reported its analysis, but the native link step failed (section 1.3). This is a property of the machine's
  environment (the length of the account's `PATH`), not of the SDK and not a missing toolchain. With a short `PATH`
  for the publishing process the link works, and NativeAOT programs were published and run (sections 4 and 5.3).

Fixing A is not a certification of the SDK under NativeAOT (section 6).

Every finding below is one of three kinds, and is labelled where the kind is not obvious: a **demonstrated failure**
(reproduced here, with the command or test that shows it), a **hypothesis** (reasoned, not run), or a
**user-perceptible difference** (something a game or its developer sees change).

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

ILC's `findvcvarsall.bat` fails inside `vcvarsall.bat`, so the `link` found on `PATH` is the GNU coreutils one.

**Cause (demonstrated, second pass).** The toolchain is installed; nothing had to be installed, and nothing was:

- `vswhere -all -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`
  answers `C:\Program Files\Microsoft Visual Studio\18\Community` (MSVC 14.52.37025 and 14.44.35207, `link.exe`
  present) and the 2019 Build Tools.
- The Windows SDK import libraries are present: `C:\Program Files (x86)\Windows Kits\10\Lib\10.0.26100.0\um\x64\kernel32.lib`
  and `...\ucrt\x64\ucrt.lib` (also 10.0.19041.0). The first pass's statement that there were none was wrong.
- The account's `PATH` is 7578 characters (machine 2152, user 5426, 148 entries). Run by hand,
  `findvcvarsall.bat x64` prints `La línea escrita es demasiado larga` ("the input line is too long") from
  `vcvarsall.bat` and exits 255. The same command with `PATH` set, for that process only, to
  `C:\Windows\System32;C:\Windows;C:\Program Files\dotnet` exits 0 and prints the MSVC `link.exe` folder and the
  library paths.
- `vcvarsall.bat` also calls `vswhere.exe` by name; without its folder on `PATH` its "not recognized" message is
  captured as part of the linker's path and the link fails with exit code 123.

**What works.** Publishing with the process's `PATH` reduced to
`C:\Windows\System32;C:\Windows;C:\Program Files\dotnet;C:\Program Files (x86)\Microsoft Visual Studio\Installer`
(plus Node's folder for the script). No system or user setting was changed. A console program published that way with
`PublishAot` links and prints `RuntimeFeature.IsDynamicCodeSupported` as `False`. A machine with an ordinary `PATH`
(or a Visual Studio developer prompt) needs none of this.

In the first pass, trimmed self-contained programs (ILLink, no native linker) were built and run instead; they
exercise the same "no reflection serializer" condition. The NativeAOT runs of the second pass are in sections 4 and
5.3.

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
- **Other callers in the SDK.** `AchievementTracker` saved and loaded `AchievementSaveData` through the two reflection
  calls (`src/MicroEngine.Achievements/Achievements.cs:157`, `:185` before this work); fixed in section 4.1. Input
  bindings do not: `InputMap` writes its own JSON (`Bindings.cs:230`).
- **Slot names.** No entry point checked the slot: it went straight into `Path.Combine(directory, slot + ".json")`.
  Section 4.2.

**Storage format and object serialization are separate problems.** The storage side (`IStorage`: named texts written
whole) uses no reflection and needed no change. What needs reflection is turning an arbitrary `T` into that text and
back. Replacing JSON with a binary or custom container changes the text, not that conversion: prototype 3 below still
needed the game to write, by hand, exactly the per-type code that reflection was providing.

**Consumers that do not use Saves.** On `main` they could not avoid the warning: `MicroEngine.Saves.dll` ships inside
the `MicroEngine` package, and PHYS-1 recorded the `PhysicsSubstitution` sample, which never saves, failing its NativeAOT
publish "in `MicroEngine.Saves`" (`odd/tasks/phys-1.md:64`). Nothing fails at run time for them, since the reflection call is never executed. No package split is
needed to fix this: once the two calls carry `RequiresUnreferencedCode`/`RequiresDynamicCode`, `MicroEngine.Saves`
produces no warning of its own (section 5). What remained for such a consumer was the achievements caller above, fixed
in section 4.1: `PhysicsSubstitution` now publishes under NativeAOT with no warning at all.

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
| typed game: the trimmed program | `missing: True`, `save: True`, `load: 3 Ada 2 Hard 1200 5 cellar`, `old: 7 Pip 0`, `corrupt: True True False`, and the three achievements lines of 4.1 |
| plain game (reflection calls): `dotnet build -p:PublishAot=true` | `IL2026` and `IL3050` at `Program.cs`, naming `SaveStore.Save`/`Load` |
| plain game: trimmed publish | no `IL2104`/`IL3053` for an engine assembly any more; the trimmed program reports `InvalidOperationException` |
| typed game, `--aot`: NativeAOT compilation (ILC) | **no trim or AOT warning** |
| typed game, `--aot`: NativeAOT link | **links**, with the short `PATH` of 1.3 |
| typed game, `--aot`: the NativeAOT executable | **runs**: the same eight lines as the trimmed program, exit code 0 |

All eight checks pass (`all checks passed`), Windows x64, .NET SDK 11.0.100, packages packed from this branch.

### 4.1 Achievements

**Demonstrated failure (before the fix).** The typed external consumer, extended to track two achievements with its
store, packed from the branch before the change:

- `dotnet publish -p:PublishTrimmed=true --self-contained -warnaserror` fails with `IL2026` in
  `AchievementTracker.Flush()` and `AchievementTracker.Load()`;
- published without `-warnaserror` and run, the program prints the five saves lines and then
  `FAILED: InvalidOperationException: Reflection-based serialization has been disabled for this application`, thrown
  by the tracker's constructor;
- `dotnet build src/MicroEngine.Achievements -p:IsAotCompatible=true` reports `IL2026` and `IL3050` at
  `Achievements.cs(157)` and `(185)`.

**Fix.** `MicroEngine.Achievements` declares an internal `JsonSerializerContext` (`AchievementSaveTypes`) for
`AchievementSaveData` and calls `Save(slot, data, typeInfo)` and `Load(slot, typeInfo)`. No public API changes. The
project is marked `IsAotCompatible`, so a reflection call added later fails its build.

**Compatibility (demonstrated).** `tests/MicroEngine.Tests/AchievementSaveFormatTests.cs` was written and passed
against the reflection implementation first, then against the change, unmodified: the exact bytes of the file the
tracker writes (ids with characters JSON escapes, an entry no longer in the catalog); files as earlier versions wrote
them, indented with either newline, and compact, reordered, with unknown properties and a null entry; and which
contents are set aside as corrupt (`null`, an array, a wrong value type, truncated, empty) and which are not (`{}`, a
null `Entries`). The 21 existing achievement tests pass unchanged.

**After the fix (demonstrated).** The same consumer, trimmed in full trim mode with no reflection switch, and as a
NativeAOT executable, prints
`achievements: True True {"Version":1,"Entries":{"ten":{"Progress":3,"Unlocked":false},"first":{"Progress":1,"Unlocked":true}}}`,
`achievements again: True 3 False` (a second tracker reads it back) and `achievements earlier: False 7 True True` (a
compact file with an unknown property and an entry outside the catalog loads, and that entry survives the next save).
The guide no longer tells trimmed games that track achievements to turn the reflection serializer back on.

**User-perceptible difference.** A trimmed or NativeAOT game that tracks achievements with a store now starts and
keeps them, instead of throwing at start-up. For every other game: none; the file is byte-identical.

### 4.2 Slot names

**Demonstrated failures (before the fix)**, with `new SaveStore("game", "<root>/saves")` on Windows (scratch `escape/`):

| Call | Result before |
| --- | --- |
| `Save("../up", data)` | `True`; wrote `<root>/up.json`, outside the save folder; `Exists` then `True` |
| `Save("..\up2", data)` | `True`; wrote `<root>/up2.json` |
| `Save("<root>\absolute", data)` (a rooted path) | `True`; wrote `<root>/absolute.json` |
| `Save("", data)` | `True`; wrote `saves/.json` |
| `Save("sub/name", data)` | `False` (the folder does not exist; the file medium never creates it) |
| `Save("NUL", data)`, `Save("name.", data)` | `True`; wrote `saves/NUL.json` and `saves/name..json` on Windows 11 |

`Load`, `Exists` and `Delete` resolved the same paths, so a slot could also read and delete a file outside the folder.
A `null` slot behaved as the empty one with the file medium (`null + ".json"`), by reading the code; not run.

**Rule implemented** (`SlotName` in `src/MicroEngine.Saves/SaveStore.cs`, used by every `SaveStore` call that takes a
slot and by every member of `FileSaveStorage`): a slot is a plain name. `ArgumentNullException` for `null`;
`ArgumentException` (parameter `slot`, the message quotes the slot) when it is empty or white space, contains `/` or
`\`, contains `: * ? " < > |`, a control character or half of a surrogate pair, ends with a dot or a space (which
covers `.` and `..`), or is a Windows device name with or without an extension (`CON`, `PRN`, `AUX`, `NUL`, `CONIN$`,
`CONOUT$`, `COM0`-`COM9`, `LPT0`-`LPT9` and the superscript-digit forms), in any letter case and on every system. The
store checks before it touches the medium, so the rule holds for browser storage and for a game's own `ISaveStorage`.

**Which shapes are in legitimate use, and how that was established.** Every slot passed in `src`, `games`, `samples`,
`templates`, the tests and the documentation was listed (`grep`): `progress`, `adventure`, `record`, `achievements`,
`input-bindings`, `example`, `profile`, and `profile.v<n>` (the backup slot of the documented upgrade pattern, a dot
inside the name). All are accepted and resolve to the same `<slot>.json`
(`TheNamesGamesUseKeepTheirFile`, which also covers spaces, a leading dot, non-ASCII letters and ASCII punctuation
outside the refused set). Slots with a folder (`folder/name`) are **not** preserved: the documentation describes a
slot as a name, no code in the repository uses one, and the file medium refused them already unless the folder
happened to exist. The full test suite passes with the rule in place.

**User-perceptible differences.**

- A call with a refused name now throws `ArgumentException` where it returned a value or wrote a file. Listed in the
  migration notes.
- `"NUL"`, `"name."` and similar worked on Windows 11 and are now refused there too, so that a slot valid on one
  system is valid on all. **Hypothesis**, not run: on Windows 10 and earlier `NUL.json` is the null device, so such a
  save was silently lost; trailing dots are stripped by Win32 path normalization in some APIs.
- A slot with a folder worked in browser storage (a key `microengine/<game>/a/b`) and in a custom medium, and in file
  storage when the folder already existed. **Hypothesis**: no shipped game depends on it; a game that does renames the
  slot (migration notes).
- `FileSaveStorage.SetAside` documents "never throws" for storage failures; with a refused name it now throws
  `ArgumentException`, like its other members. The store never calls it with one.

**Not changed.** `BrowserStorage` and `InputMap.SaveBindings(IStorage, slot)` used without a `SaveStore` do not check
the name themselves (`MicroEngine.Platform` and `MicroEngine.Input` are outside this work's surface). With the default
file medium the check in `FileSaveStorage` covers them; browser keys cannot leave the game's prefix.

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

After the saves change alone, `MicroEngine.Achievements` reported the two calls at `Achievements.cs:157` and `:185`
when the property was forced; with 4.1 it sets the property itself and builds without warnings.

**5.2 Whole-program analysis** (ILC and ILLink over a real program, `-p:TrimmerSingleWarn=false`):

| Program | Tool | After the saves change | After 4.1 |
| --- | --- | --- | --- |
| `samples/HelloEngine` (window, SDL and raylib platforms, sprites, synth audio, scenes) | ILC (`PublishAot`) | none | **none** |
| `samples/HelloEngine` | ILLink (`PublishTrimmed`, full trim mode) | `Achievements.cs(157)`, `(185)`: `IL2026` | not run again |
| `samples/PhysicsSubstitution` (tile and Box2D physics, console) | ILC | `Achievements.cs(157)`, `(185)`: `IL2026`, `IL3050` | **none** |
| external typed consumer (`MicroEngine` package, saves and achievements) | ILC and ILLink | none (saves only) | **none** |

**5.3 Programs run.**

- Trimmed: the external typed consumer (section 4), and `samples/HelloEngine` published `PublishTrimmed`,
  self-contained, full trim mode (31 MB), run with `--script hello.script --hidden --mute --captures captures`:
  `done: 70 updates, 70 frames`, and the capture shows the title, the sprite and the hint text (first pass).
- NativeAOT, Windows x64, with the short `PATH` of 1.3 (second pass):
  - the external typed consumer (section 4): links and runs;
  - `dotnet publish samples/HelloEngine/HelloEngine.csproj -c Release -r win-x64 -p:PublishAot=true -p:TrimmerSingleWarn=false`:
    no warning; a 9.7 MB `HelloEngine.exe` beside `SDL3.dll` and `raylib.dll`. Run with
    `--script hello.script --hidden --mute --captures captures`: `capture: captures\hello.png`,
    `done: 70 updates, 70 frames`, exit code 0; the capture, looked at, shows the title, the sprite and the hint text;
  - `samples/PhysicsSubstitution`, the same publish: no warning. Run: the tile backend's whole scenario
    (`tiles: 740 steps, end at (618, 121), coins 2/3 ... goal at step 653`), then
    `box2d: no Box2D native library for this runtime.`, exit code 0. Box2D itself did not run: this machine has no
    Box2D native library (`artifacts/box2d`).

Classification:

| | |
| --- | --- |
| **Confirmed failure, fixed** | Saving the game's own types in a trimmed or NativeAOT program (`MicroEngine.Saves`) |
| **Confirmed failure, fixed** | `AchievementTracker` with a store in a trimmed or NativeAOT program threw at start-up (4.1) |
| **Confirmed failure, fixed** | A slot could read, write and delete a file outside the save folder (4.2) |
| **Warnings, not a game's concern** | `MicroEngine.Cli` |
| **Game code, not SDK** | `json.Deserialize<Profile>()` in the documented upgrade pattern (`samples/Examples/SaveVersions.cs`, quoted by `docs/manual/persistence.md`): a trimmed game uses `json.Deserialize(SaveTypes.Default.Profile)` |
| **Verified, Windows x64 only** | A game's own types and its achievements save and load in a trimmed self-contained program and in a NativeAOT executable, from the packed packages; a windowed sample (`HelloEngine`, with SDL3 and raylib loaded as DLLs) runs trimmed and as a NativeAOT executable; the tile physics scenario runs as a NativeAOT executable; ILC reports no warning for the three programs |
| **Not tested: no native library here** | Box2D under NativeAOT, from source or as a Box2D-only external consumer from packages: the Box2D package and sample need its native libraries built (`artifacts/box2d`), which this machine does not have |
| **Not tested: no platform here** | Linux, macOS, Android, iOS and WebAssembly under full trimming or AOT. `MicroEngine.Tiled` (DotTiled) and `MicroEngine.Text.Shaping` (HarfBuzzSharp) in a published trimmed or NativeAOT program. The four demo games under NativeAOT |

## 6. Claims

The SDK does not claim NativeAOT support. What the public documentation states is limited to what was run: saves with
declared types and achievements work in trimmed and NativeAOT builds, the calls without metadata do not, and NativeAOT
is not a packaging target. The NativeAOT runs of this work are one system (Windows x64), two samples and a console
consumer; a claim of NativeAOT support for a target needs an end-to-end game published and exercised on that target.

## 7. Owner's decisions (2026-11-13) and what stays open

Decided and done in the second pass (task record, L8): the achievements fix (4.1), the slot-name rule (4.2), ADR 0054
accepted, and the NativeAOT publish and run on this machine (1.3, 4, 5.3), for which nothing had to be installed.

Open, not started, no issue created:

1. **CI.** Wired in the third pass (section 8); not run yet.
2. **Box2D under NativeAOT** needs the Box2D native libraries; untested.
3. **Hosts.** The web, Android and iOS hosts keep `TrimMode=partial` and the reflection switch. Moving them to full
   trimming would make every game declare its saved types: a breaking change for games, not proposed.
4. **`BrowserStorage` and `InputMap.SaveBindings`** used without a `SaveStore` do not check slot names themselves
   (4.2); no failure was demonstrated there.

## 8. NativeAOT in CI (third pass, 2026-11-14)

The owner's request is in the task record (L12). Design, triggers and what each check proves:
[CI, NativeAOT](../ci.md#nativeaot).

**What changed.**

- `sdk.yml` has a `trimmed` job beside the external project: it downloads the feed the `pack` job made and runs
  `node sdk/tests/trimmed.mjs --feed sdk-feed --aot`. Linux in daily CI (`ci.yml` passes `daily: true`); Linux and
  Windows in validate, by hand, and in the **Check** of `sdk-release.yml`, whose draft needs it. The `sdk` evidence
  needs the job too. No new workflow, trigger, permission, secret or cache.
- `sdk/tests/trimmed.mjs --aot` also publishes and plays a game with a window (below), requires a NativeAOT publish to
  leave a native executable and no managed assembly of the game, builds without build servers and no longer lets a
  failed removal of its scratch folder decide its exit code.

**Demonstrated failure, fixed (the script, on Windows).** Run without `--keep`, the script ended with
`Error: EPERM, Permission denied` from `fs.rmSync` on its scratch folder after printing its checks, and exit code 1
with no summary line (seen on three runs here, each with failing checks; **hypothesis** for the cause: a build server started
by `dotnet` still held the folder). On a Windows runner that could report a failed check for a run whose checks all
passed. Now `dotnet` runs with `--disable-build-servers`, the removal retries and, if it still fails, prints
`left behind` and leaves the result alone. Observed after the change, on four runs: the folder is removed and the
exit code is the checks'.

**The game with a window.** `samples/HelloEngine` was published and run by hand in the second pass (5.3). The check
uses a game of the same kind written inside the script (`MicroEngine.Desktop` from the feed: an image made in code, an
atlas, a synthesized sound, a scene moved by input, text) instead of copying the sample, because `samples/` is not an
input of the `sdk` check: a copied sample could change without the check running. It is played with
`--platform sdl-software --hidden --mute --script play.script --captures <dir>`.

**Run here** (Windows x64, .NET SDK 11.0.100, packages packed from this branch, the short `PATH` of 1.3, the command
the workflow runs):

| Run | Result |
| --- | --- |
| `node sdk/tests/trimmed.mjs --feed <feed> --aot` | 12 `PASS`, `all checks passed`, exit code 0, 63 s; scratch folder removed |
| the capture `after.png` of the windowed NativeAOT executable, looked at | the title text and the sprite, moved right of the centre |
| negative 1: a scratch copy of the script expecting `old: 7 Pip 1` | `FAIL` for the trimmed and for the NativeAOT program, `2 check(s) failed`, exit code 1 |
| negative 2: a scratch copy whose typed program returns 3 after printing the right lines | the same two `FAIL`, exit code 1 |
| negative 3: a scratch copy whose windowed game draws off screen | `FAIL windowed game: its captures hold a drawn frame ...: 1 different rows`, `1 check(s) failed`, exit code 1 |
| the step's shell lines (`... \| tee trimmed.log \|\| status=$?`, then `exit "$status"`) under `bash -eo pipefail`, with a command that fails and one that passes | exit code 1 and 0; the summary lines are written in both cases |
| `actionlint` 1.7.7 (the version `ci.yml` runs) over `.github/workflows` | no finding |
| `node --test .github/scripts/*.test.mjs` | 59 of 59 |

**Not run.** The workflows: nothing is pushed. So these are **hypotheses** until the first run: that `ubuntu-latest`
links with its clang (the job installs `clang zlib1g-dev` by name, so a missing toolchain fails there and not in the
link), that `windows-latest` links with its Visual Studio tools without the `PATH` problem of 1.3, that the windowed
NativeAOT executable runs under Xvfb on Linux as the packaged games of the external project already do, and the job's
duration. Linux NativeAOT has not been run anywhere by this work.

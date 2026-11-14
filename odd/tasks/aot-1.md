# AOT-1: NativeAOT and trimming compatibility; saves without reflection

Objective: establish, with evidence, where the SDK stands under NativeAOT and trimmed publishing, and give games a way to
save and load their own types without runtime reflection. Issue NastMz/MicroEngine#150. Branch `feat/aot-1`.
Test runner: `dotnet test MicroEngine.slnx -c Release`. Evaluation: [`docs/evaluations/aot-1.md`](../../docs/evaluations/aot-1.md).

## Specs

Investigation (issue, verbatim):

- S1 "Reproduce the failure with the actual `SaveStore` API and an **external consumer** defining its own data types.
  Capture the diagnostics (`IL2026`, `IL3050`, etc. if applicable), build command, target framework, runtime identifier,
  and toolchain."
- S2 "Inventory all save/load entry points, generics, JSON options, nested and polymorphic models,
  versioning/migrations, error handling, atomic writes, and existing file compatibility. Identify exactly where
  reflection or dynamic-code assumptions occur."
- S3 "Separate **storage format** from **object serialization**: swapping JSON for text, binary, a custom container, or
  a custom extension does not by itself solve the conversion of arbitrary game-defined types without reflection."
- S4 "Evaluate alternatives based on real prototypes rather than choosing a favorite upfront: 1. `System.Text.Json`
  source-generated metadata (`JsonSerializerContext`, `JsonTypeInfo<T>`, or resolvers supplied by the consuming game).
  2. Explicit codecs/serializers or registration contracts provided by game code, potentially preserving a convenient
  default path. 3. Schema-driven/generated serialization and/or a binary or custom save format, if the trade-offs
  justify it. 4. Alternative designs that preserve compatibility and avoid runtime reflection."
- S5 "Compare developer ergonomics (simple projects and advanced projects), generated-code/tooling burden, runtime
  allocations/performance, file size, diagnostics, forward/backward compatibility, portability across
  desktop/mobile/WebAssembly, and migration costs."
- S6 "Inspect whether consumers who do not use Saves can avoid any save-related AOT limitation, without assuming a
  package split is required."
- S7 "Audit other SDK code and dependencies for NativeAOT/trimming hazards, distinguishing **confirmed failures**,
  **warnings**, **untested platforms/toolchain gaps**, and **verified supported scenarios**. Track newly discovered
  blockers separately if they warrant their own issues."
- S8 "Consider documentation and distribution claims: do not advertise global NativeAOT support until an end-to-end game
  has been published and exercised on each claimed target."

Acceptance criteria (issue, verbatim):

- S9 "A reproducible baseline and clear inventory of NativeAOT/trimming blockers, starting with `MicroEngine.Saves`."
- S10 "A documented decision comparing credible serialization/storage approaches, with rationale and compatibility plan;
  no assumption that JSON must be removed."
- S11 "A supported save/load path for **consumer-defined models** that works without runtime reflection under NativeAOT
  and trimmed publishing (within explicitly documented limitations)."
- S12 "Existing supported save files remain readable or have a tested, documented migration mechanism."
- S13 "Regression tests cover round-trip, nested/custom models, missing/corrupt data and the existing
  atomic-write/recovery behavior as applicable."
- S14 "An external package consumer compiles, publishes **and runs** a minimal NativeAOT example; trimming warnings are
  assessed rather than merely suppressed."
- S15 "An external consumer that uses Box2D but not Saves is tested separately, so tooling problems and unrelated
  dependencies are not misattributed."
- S16 "Target-specific support, unsupported cases, diagnostics, and required consumer setup are documented."
- S17 "Broader SDK NativeAOT conclusions are backed by separate end-to-end validation, not inferred from a passing Saves
  test."

Constraints of this work unit (delegation brief, not the issue): additive public API only; existing `SaveStore` calls
keep compiling and behaving the same; files written by the current version still load; no edits to other packages'
source; no toolchains installed on this machine; ADR number 0054.

Second pass, the owner's request of 2026-11-13 (L8, verbatim there; it lifts "no edits to other packages' source" for
Achievements and "no toolchains installed"):

- S18 "Aplicar el patrón sin reflexión a Achievements."
- S19 "Aceptar ADR 0054 verificando que refleje la implementación."
- S20 "Corregir la validación de slots para impedir escapes del directorio de guardado, rutas inválidas y nombres
  vacíos, preservando la compatibilidad legítima."
- S21 "Intentar instalar el toolchain NativeAOT localmente y ejecutar una publicación real. Si hay un impedimento
  concreto, documentarlo."
- S22 "Criterio general: corregir problemas reales dentro del lote cuando el alcance sea razonable. No ampliar
  innecesariamente la arquitectura ni crear issues preventivos. Si algo exige un rediseño considerable, presenta
  evidencia, alcance y alternativas antes de implementarlo. Distingue expresamente los fallos demostrados, las
  hipótesis y las diferencias perceptibles para el usuario."

Third pass, the owner's request of 2026-11-14 (L12, verbatim there):

- S23 "Revisa a fondo las responsabilidades de `SaveStore`, `IStorage`, `BrowserStorage` e
  `InputMap.SaveBindings/LoadBindings`."
- S24 "Determina dónde corresponde validar nombres y rutas. No apliques las restricciones de SaveStore a todo IStorage
  si rompe usos legítimos."
- S25 "Comprueba seguridad, compatibilidad entre plataformas y contratos de excepciones."
- S26 "Corrige los defectos demostrados que tengan una solución localizada. Si descubres que hace falta modificar el
  contrato público de almacenamiento, documenta las alternativas y no hagas ese rediseño sin aprobación."
- S27 "Cada defecto corregido debe tener una prueba de regresión que demuestre el problema anterior." "No introduzcas
  rediseños arquitectónicos innecesarios." "No hagas push, PR ni release sin mi autorización."

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 Baseline: external consumer with its own save types, analyzer and publish diagnostics | S1 S9 | inline | done | evaluation §1; scratch `baseline/` |
| T2 Inventory of the Saves entry points and where reflection occurs | S2 S3 S6 | inline | done | evaluation §2 |
| T3 Prototypes of the alternatives, compared | S4 S5 S10 | inline | done | evaluation §3; scratch `proto/`, published trimmed and run |
| T4 Implement the evidence-backed option in `MicroEngine.Saves`, tests first | S11 S12 S13 | inline | done | `d97f506`; RED: CS1501 "no overload of Load takes 2 arguments"; `SaveStoreTypeInfoTests` and the existing save tests 48/48 |
| T5 External consumer on the new path: trimmed publish and run; NativeAOT as far as this machine goes | S14 S15 S17 | inline | partial | first pass: `sdk/tests/trimmed.mjs`, 6 of 7 checks; reopened by S21 and closed for S14 by T11 (8 of 8, NativeAOT run included). S15 stays open: no Box2D native library here (L5, L11) |
| T6 Audit of the other SDK packages' analyzer warnings (inventory) | S7 S8 | inline | done | evaluation §5 |
| T7 Evaluation, ADR 0054, guide, API reference, release notes, sample | S10 S16 | inline | done | this branch |
| T8 Slot names: refuse what is not a plain name at every entry point, keep the names in use | S20 S22 | delegated (one bounded worker, the whole second pass) | done | `d7e6f06`; RED: 4 of 5 `SaveSlotNameTests` failed, and scratch `escape/` wrote three files outside the folder; GREEN 5/5; evaluation §4.2 |
| T9 Achievements without reflection, file byte-identical | S18 S11 S12 | delegated (same worker) | done | `0d21b94`; pin: `AchievementSaveFormatTests` 4/4 before and after; RED: `trimmed.mjs` typed publish `IL2026` in `AchievementTracker`, and the run threw `InvalidOperationException`; GREEN: all checks; evaluation §4.1 |
| T10 ADR 0054 checked against the code and accepted | S19 | delegated (same worker) | done | the commit that closes this pass; corrections in L10 |
| T11 NativeAOT toolchain on this machine: real publish and run | S21 S14 S17 | delegated (same worker) | done | nothing to install (L9); `trimmed.mjs --aot` 8 of 8; `HelloEngine` and `PhysicsSubstitution` published and run as NativeAOT executables; evaluation §1.3, §5.3 |
| T12 Responsibilities of the store, the storage contract, the browser storage and the bindings calls; where names are checked | S23 S24 | delegated (one bounded worker, the whole third pass) | done | evaluation §4.3 (table and reasons) |
| T13 Security, systems and exception contracts, by test and by probe | S25 | delegated (same worker) | done | `StorageSlotContractTests` (7); Windows probe (set-aside collision, case, normalization, length, game id), evaluation §4.3. Browser, Linux and macOS behaviour is read, not run |
| T14 Localized fixes with their regression tests | S26 S27 | delegated (same worker) | done | `b9f6638`; RED: 2 of 7 (`slot` reported as `key` and written; `BrowserStorage` reached the page call); GREEN 7/7 |
| T15 Alternatives for what needs the storage contract changed; manual, release notes, migration, API reference | S26 | delegated (same worker) | done | evaluation §4.3 "Needs owner decision" (four); the documentation commit of this pass |
| T16 Refuse a slot that ends with the set-aside suffix | S26 S27 | delegated (same worker) | done | RED: `SettingASlotAsideNeverReplacesAnotherSlot`, no exception; GREEN; evaluation §4.3 |

## Log

- L1 (2026-11-13) The issue, verbatim: see `gh issue view 150` (NastMz/MicroEngine#150, "AOT-1 — Audit NativeAOT/trimming
  compatibility and redesign reflection-dependent Saves"); every requirement of its Investigation and Acceptance
  criteria sections is quoted in full under Specs (S1-S17). Its framing, verbatim: "This is a **cross-cutting SDK
  compatibility concern**, not necessarily a Box2D defect. Separately, an isolated Box2D NativeAOT consumer could not be
  validated on the current Windows machine because the NativeAOT toolchain/linker was unavailable. Do not conflate these
  two findings." "NativeAOT compatibility is **not currently established for the complete SDK**. Fixing Saves alone must
  not be treated as certification of the engine." "This is a **discovery + implementation issue, not an upfront mandate
  to change formats or split packages**." "No premature commitment to JSON, binary, proprietary format, or package
  redesign."
- L2 (2026-11-13) Baseline on `main` (`16af5af`), external console project on the packed `MicroEngine` package with
  `PublishAot`: `dotnet build` reports nothing; the NativeAOT publish reports `IL2104` and `IL3053` for
  `MicroEngine.Saves` (`IL2026`/`IL3050` at `SaveStore.cs:122` and `:145` with `TrimmerSingleWarn=false`); a trimmed
  self-contained publish runs and fails in the first save with `InvalidOperationException: Reflection-based
  serialization has been disabled for this application`. The failure is the switched-off reflection serializer, not
  the trimming of the game's types.
- L3 (2026-11-13) Prototypes (scratch `proto/`, trimmed and run): source-generated `JsonTypeInfo<T>`, a hand-written
  JSON codec, a hand-written binary layout and a resolver on the store all work without reflection; only the first
  turns a forgotten type into a compile error, keeps the file format and needs no hand-written per-type code. The
  resolver variant fails at run time (`NotSupportedException`) for a type missing from the context. Decision: the first,
  additively; ADR 0054 (Proposed, for the owner).
- L4 (2026-11-13) NativeAOT on this machine: ILC runs and reports its analysis; the link fails (`findvcvarsall.bat`
  fails in `vcvarsall.bat`, the `link` on `PATH` is GNU coreutils', and no Windows SDK import libraries are installed).
  Nothing was installed. No NativeAOT executable was run; S14 is met for trimmed publishing and for NativeAOT
  compilation without warnings, not for a NativeAOT run.
- L5 (2026-11-13) S15: the Box2D package cannot be packed here (no `artifacts/box2d` natives), so no external
  Box2D-only consumer. `samples/PhysicsSubstitution` from source under ILC reports only `Achievements.cs(157)` and
  `(185)` (`IL2026`, `IL3050`): nothing from Box2D, Physics or Saves. It did not link (L4).
- L6 (2026-11-13) Whole-program checks after the change: `samples/HelloEngine` under ILC, no warning; under ILLink
  (full trim), only the two achievements calls; published trimmed and run with its script, `done: 70 updates, 70
  frames`, capture inspected. Open blocker outside this unit's surface: `AchievementTracker` saves through the
  reflection calls.
- L7 (2026-11-13) Not covered: S14 (NativeAOT run), S15 (Box2D-only package consumer), S17 beyond one trimmed windowed
  sample on Windows x64. Left for the owner (evaluation §7): achievements, running `sdk/tests/trimmed.mjs --aot` where a
  native toolchain exists, the manual page, CI wiring.
- L8 (2026-11-13) The owner's request for the second pass, verbatim:

  ```
  AOT-1

  * Aplicar el patrón sin reflexión a Achievements.
  * Aceptar ADR 0054 verificando que refleje la implementación.
  * Corregir la validación de slots para impedir escapes del directorio de guardado, rutas inválidas y nombres vacíos, preservando la compatibilidad legítima.
  * Intentar instalar el toolchain NativeAOT localmente y ejecutar una publicación real. Si hay un impedimento concreto, documentarlo.

  Criterio general: corregir problemas reales dentro del lote cuando el alcance sea razonable. No ampliar innecesariamente la arquitectura ni crear issues preventivos. Si algo exige un rediseño considerable, presenta evidencia, alcance y alternativas antes de implementarlo. Distingue expresamente los fallos demostrados, las hipótesis y las diferencias perceptibles para el usuario.
  ```

  Specs S18-S22; tasks T8-T11; T5 reopened for the NativeAOT run.
- L9 (2026-11-13) NativeAOT toolchain (S21). Nothing was installed, because nothing was missing: `vswhere` finds the
  x64 C++ tools in Visual Studio 18 Community (and the 2019 Build Tools), and `Windows Kits\10\Lib\10.0.26100.0` holds
  `kernel32.lib` and `ucrt.lib`. L4's "no Windows SDK import libraries are installed" was wrong. The demonstrated
  cause of the failed link is the account's `PATH` (7578 characters): `vcvarsall.bat` stops with "La línea escrita es
  demasiado larga". With `PATH` set for the publishing process only to Windows, `dotnet` and the Visual Studio
  Installer folder (where `vswhere.exe` is), the link works. No system or user setting was changed.
- L10 (2026-11-13) ADR 0054 against the code (S19). Corrected: the status; the context now names the engine's own
  caller; decision 3 said the reflection pair was "unchanged in behaviour", which the slot rule makes untrue (it is
  unchanged in how it serializes); a new decision 5 for the engine's own saved types; the consequences listed the
  achievements caller and the NativeAOT run as open and named only three of the six properties that turn the analyzers
  on. Checked and left as written: decisions 1, 2 and 4, the alternatives, the hosts' settings
  (`TrimMode=partial` and the reflection switch in the three `Host.csproj`).
- L11 (2026-11-13) Second pass, observed. Slots: `Save("../up")`, `Save("..\up2")` and a rooted path wrote outside the
  save folder and `Save("")` wrote `.json` (scratch `escape/`); refused now, with the names in use keeping their file
  (`d7e6f06`). Folder slots (`a/b`) are refused: undocumented, unused in the repository, and already refused by the
  file medium unless the folder existed. Achievements: `0d21b94`. NativeAOT on Windows x64: the external consumer
  (saves and achievements), `HelloEngine` (capture inspected) and `PhysicsSubstitution`'s tile scenario run; Box2D did
  not run (`box2d: no Box2D native library for this runtime.`). Not covered: S15, and S17 beyond Windows x64. No issue
  was created and no CI file was touched.
- L12 (2026-11-14) The owner's request for the third pass, verbatim:

  ```
  3. Almacenamiento

  * Revisa a fondo las responsabilidades de `SaveStore`, `IStorage`, `BrowserStorage` e `InputMap.SaveBindings/LoadBindings`.
  * Determina dónde corresponde validar nombres y rutas. No apliques las restricciones de SaveStore a todo IStorage si rompe usos legítimos.
  * Comprueba seguridad, compatibilidad entre plataformas y contratos de excepciones.
  * Corrige los defectos demostrados que tengan una solución localizada. Si descubres que hace falta modificar el contrato público de almacenamiento, documenta las alternativas y no hagas ese rediseño sin aprobación.

  Criterios generales
  * Cada defecto corregido debe tener una prueba de regresión que demuestre el problema anterior.
  * No introduzcas rediseños arquitectónicos innecesarios.
  * No hagas push, PR ni release sin mi autorización.
  ```

  Specs S23-S27; tasks T12-T15.
- L13 (2026-11-14) Third pass, observed. The storage is the layer that keeps a slot inside the save location
  (`FileSaveStorage` in every member; a browser key cannot leave the game's prefix); `SaveStore` is the layer that keeps
  names portable, over any storage; the rule is not imposed on `IStorage`. Fixed (`b9f6638`): the bindings calls and
  `BrowserStorage` accepted a slot with no name; the exception documentation of `SaveBindings`, `LoadBindings` and
  `IStorage` contradicted the code. Demonstrated on Windows and left for the owner (evaluation §4.3): a slot named
  `<other>.corrupt` is replaced when the other slot is set aside; two slots that differ only by case are one file; a
  game id with `..` or a rooted path leaves the `MicroEngine` data folder. `IStorage`'s documentation now says a
  storage refuses a name it cannot hold with `ArgumentException`: it describes what `FileSaveStorage` does since
  `d7e6f06`, adds no member and changes no signature. Not run: a browser, Linux, macOS.

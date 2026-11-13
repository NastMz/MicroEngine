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

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 Baseline: external consumer with its own save types, analyzer and publish diagnostics | S1 S9 | inline | pending | |
| T2 Inventory of the Saves entry points and where reflection occurs | S2 S3 S6 | inline | pending | |
| T3 Prototypes of the alternatives, compared | S4 S5 S10 | inline | pending | |
| T4 Implement the evidence-backed option in `MicroEngine.Saves`, tests first | S11 S12 S13 | inline | pending | |
| T5 External consumer on the new path: trimmed publish and run; NativeAOT as far as this machine goes | S14 S15 S17 | inline | pending | |
| T6 Audit of the other SDK packages' analyzer warnings (inventory) | S7 S8 | inline | pending | |
| T7 Evaluation, ADR 0054, guide, API reference, release notes | S10 S16 | inline | pending | |

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

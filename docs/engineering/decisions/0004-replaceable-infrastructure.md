# ADR-0004 — Replaceable infrastructure and bounded capability contracts

- Status: accepted.
- Date: 2026-10-15.
- Approval: Codex root,2026-10-15, under explicit owner architectural directive and [technical delegation](../autonomy.md); independent technical_authority_review PASS.
- Related: FR-001/002, NFR-001/002/004/005; [manifesto](../manifesto.md), [ADR-0001](0001-greenfield-direction.md), [ADR-0003](0003-next-validation-tracer.md), [H1-R1](../specs/h1-runtime.md).
- Supersedes: none. Complements product direction and constrains subsequent technical evolution; does not replace H0/H1 scopes or acceptance evidence.

## Context: existing coverage and gaps

Owner direction: "MicroEngine debe evolucionar como un motor de videojuegos extensible, modular y agnóstico a las implementaciones de infraestructura." The owner also requires selectable, replaceable and developer-authored backends and architectural room for Windows/Linux/macOS/browser/WebAssembly, without immediate implementation commitments.

The manifesto already requires extensibility, public contracts and keeping platform details/game rules out of ordinary engine consumers. ADR-0003 isolates Raylib in Desktop and keeps Hosting/Loop independent of native loading. This is useful but incomplete: no permanent rule explicitly covers all infrastructure families, heterogeneous optional features, user-authored replacement or future target assumptions. A single tested adapter does not establish general portability or interchangeable implementations.

## Decision

1. **Dependency direction:** necessary shared engine foundations depend on owned contracts/data, not concrete rendering, physics, audio, input or platform implementations. Concrete infrastructure belongs in optional modules/adapters. The current project name Loop is not a decree that every capability belongs in a monolithic Core project.
2. **Cohesive contracts:** specify minimal contracts for demonstrated consumer needs, including ownership, errors and ordering. Do not build a universal backend interface. A bounded profile may compose several contracts for a concrete scenario; it must not become mandatory for unrelated consumers.
3. **Public replacement:** developers select/compose provided or custom implementations through public contracts at the application boundary. Replacing a conforming provider must not require editing engine foundations, naming a new friend assembly, or exposing native types in ordinary game logic. A sample's selected provider is not the engine's required provider.
4. **Heterogeneous capabilities:** a provider is not assumed to support all features. Specify mandatory behavior only for the capability/profile actually requested. Add optional/specialized contracts when a concrete need exists; consumers explicitly require them or choose a documented fallback. Unsupported behavior must not be silently represented as successful support. No capability registry, DI container, discovery service or SupportsEverything interface is prescribed now.
5. **Opt-in composition:** games may omit unused systems and supply their own components. Headless remains a valid mode. No mandatory renderer/physics/audio/input stack or genre framework follows from a validation game.
6. **Future portability:** consider Windows, Linux, macOS and browser/WebAssembly as architectural possibilities. Identify assumptions about scheduling, blocking loops, native loading, threading, clocks, filesystem and deployment when relevant. Record adapter/profile restrictions rather than exporting them as universal engine requirements. Universal compatibility is not required of each component/game; no target support claim without actual evidence.
7. **Permanent review:** every evolution checks new dependencies/coupling, public native leakage, replacement seams, optionality, target restrictions and consumer impact. Significant decisions retain alternatives/reasons and independent review; concrete capabilities retain specs/tests/evidence. Technical autonomy is unchanged.

## Alternatives considered

- Keep only current implicit extensibility language: smallest edit, but allows sample/backend assumptions to become de facto engine requirements. Rejected as insufficient for this explicit directive.
- Establish these constraints and narrowly clarify the present design: chosen; preserves delivered behavior while guiding future decisions with an observable review checklist.
- Redesign all subsystems around hypothetical universal providers or implement another backend/exporter now: rejected; speculative interfaces and unapproved scope, not evidence of useful extensibility.

## Current H1 design assessment and minimal adjustments

Assessment of baseline e5df4fe (source inspected with CodeGraph; project references cross-checked in review):

| Boundary / assumption | Assessment and design disposition |
| --- | --- |
| Hosting/Loop versus Raylib | Native package remains in Desktop; public loop contracts use owned data/interfaces. Keep dependency direction; no source or package change needed for this directive. |
| Public IFramePlatform / ICanvas and factory | Already permit externally implemented primitive-graphical providers. Document this as the extension route, not the private IDesktopApi testing seam. No backend registry or new interface now. |
| IFramePlatform combines Poll/input/time with Begin/Canvas/End | A bounded synchronous primitive-presentation profile, NOT universal platform/input/render/Core abstraction. Headless/other capabilities need not implement it. Split or compose further only when a real nonmatching consumer requires it; do not add methods for hypothetical backends. |
| Desktop maps fixed keys, one window, native entry-thread obligation | Adapter/sample restrictions, not universal engine input, threading or platform rules. Games may provide another conforming provider; generic remapping/multiwindow are not promised. |
| Loop-owned synchronous Run | Opt-in runner for current scenario; browser callback scheduling is not validated. Do not force a future browser provider into a blocking loop; evaluate a separate execution driver when that horizon is real. No browser runner/exporter now. |
| Loop grants Desktop friendship for internal FrameApiException construction | A real adapter-name coupling, not an extension mechanism. Existing external providers can implement the public interfaces without friendship; their ordinary exceptions are execution failures, not privileged engine-origin misuse. Do not claim classification parity or add friend names for new providers. Record the limitation (F-011); any evolution of this boundary must remove/generalize the adapter privilege through a reviewed backend-independent error/guard design, preserving origin distinction. No speculative guard API, changed exception classification or retroactive acceptance in this documentation-only change. |
| Specialized providers / optional features | Primitive canvas support is mandatory only when selecting this profile, never for every game/backend. No promise of sprites/audio/physics or uniform feature sets. Additional capabilities require their own justified contract. |

The friend-name limitation is disclosed, not declared resolved or evidence of full infrastructure independence. Current H0/H1 technical evidence remains valid only for its original scope; this review does not upgrade it to multi-backend conformance. See [findings](../findings.md).

## Consequences, checks and limits

The permanent constraint prevents accidental vendor lock-in while accepting finite, explicitly scoped profiles. More contracts are not inherently better: separation is driven by concrete consumers and verified ownership/error behavior. A future replacement should be checked with a public-only consumer/project graph, applicable contract suite and provider-specific smoke evidence; tests for one provider do not prove all others.

This change adds no runtime code, APIs, packages, adapters, exporter or platform support. It does not alter H0/H1 acceptance criteria, product feedback gate, autonomy, roadmap functional scope or existing runtime behavior. Windowsx64 remains the only tested native target. Runtime reruns are N/A for this documentary change, not a new claim that64tests were rerun. Required evidence: independent document/design consistency review, links, dependency readback, unchanged executable surfaces and git diff --check.

## Approval and review record

Owner supplied the architectural principle2026-10-15. Independent technical_authority_review PASS recommended documentary adoption: all seven constraints/non-goals represented, current extension route and unresolved coupling honestly stated, no runtime/API or acceptance change. Codex root records technical acceptance2026-10-15 under autonomy.md. Readback:15affected documents/91relative targets,0missing; dependency/source readback and git diff --check passed. Executable diff from e5df4fe empty; runtime tests N/A/not rerun. This is architectural adoption, not full implementation conformance or any new milestone acceptance.

Subsequent F-011 remediation2026-10-16: [ADR-0005](0005-provider-neutral-validation.md) approves a one-method provider-neutral validation contract, motivated by existing Desktop checks rather than a speculative backend. ME-015 removes named friendship; verification recorded in backlog/current state. Baseline e5df4fe assessment above remains historically accurate and not retroactively implementation proof.

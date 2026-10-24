# Product manifesto

Status: confirmed product direction. Origin: repository owner instructions, 2026-10-12; autonomous three-game completion mission, 2026-10-24.

## Purpose

MicroEngine is a general-purpose C#/.NET 2D game engine/framework. It provides reusable,
configurable and extensible technical foundations so game developers do not repeatedly
rebuild infrastructure. Its audience includes independent developers, maintainers and
external extension authors, working alone or collaboratively.

## Scope and boundaries

- The core establishes necessary shared foundations; its exact composition remains open.
- General capabilities solve technical needs shared across games, with public contracts.
- External libraries may implement genre conventions using those contracts.
- Games own their mechanics, content and rules.
- RPG, platformer, metroidvania, Pokemon-like and Zelda-like libraries are not mandatory engine parts.
- 3D is outside current scope. Future possibilities do not justify premature generalization.
- No obligation exists to preserve prototype APIs, implementations, patterns or dependencies.

## Reference-game completion mission

MicroEngine is validated by completing three small but coherent reference games: Meadow Run
(platformer), Courtyard Key (top-down adventure) and Trail Duel (turn-based RPG). They are
independently runnable consumers integrated through the same public engine foundations, not a
single required launcher and not genre modules inside Core. Each game must progress beyond a
technical tracer into a recognizable, integrated experience with a beginning, progression,
failure/recovery where appropriate and a conclusion.

The games drive evidence-based engine evolution. A shared capability is justified by a concrete
consumer need, an identifiable cross-consumer need, a verified composition/ergonomics limit or
an infrastructure responsibility that belongs in the engine. This mission does not justify
speculative universal systems, backends or abstractions.

## Principles

Correctness, predictable behavior, clear responsibilities and unambiguous ownership take
precedence over breadth. Public APIs should facilitate correct use; platform details should
not leak into ordinary game logic. Testability, diagnostics and measurable performance are
product goals, not prescriptions for particular technologies.

Architecture may evolve. Significant changes require reasons and consideration of consumers;
compatibility is not a promise of immutability. Do not impose constraints on a future public
versioning policy before it is decided.

## Infrastructure independence (owner directive,2026-10-15)

MicroEngine is modular and implementation-agnostic: developers may select, replace or implement rendering, physics, audio, input and platform providers through cohesive public contracts. Necessary foundations do not depend on concrete providers; unused systems are not mandatory. Providers may expose different optional/specialized capabilities. Design for demonstrated needs, not a universal interface.

Windows/Linux/macOS/browser/WebAssembly are future architectural possibilities, not current delivery commitments or universal compatibility requirements. Preserve room for different execution/platform models without speculative backends/exporters/abstractions. Evaluate new coupling and target restrictions in every evolution. [ADR-0004](decisions/0004-replaceable-infrastructure.md) records rationale, limits and current-design assessment; existing functional scope/acceptance stays unchanged.

## Relationship to the prototype

The prototype is a laboratory and historical reference, not the target architecture.
Reuse is considered only after independent design, contract conformance, demonstrated
technical benefit and absence of incompatible inherited restrictions. Neither reuse nor
rewriting is a goal by itself.

See [requirements](requirements.md), [ADR-0001](decisions/0001-greenfield-direction.md)
and the [roadmap](roadmap.md). Earlier agent recommendations remain proposals only.

## Artistic-style independence (owner directive,2026-10-18)

MicroEngine must leave room for pixel art, high-resolution sprites/illustrations, spritesheets/animation and programmatic primitives, with ordinary ordered layers/backgrounds and later optional parallax. Source dimensions and art style are not Core genre/scale rules. Assess real sampling/scaling/resolution constraints in every horizon; adapter budgets/capabilities must be explicit rather than universal promises. This does not require speculative advanced rendering or immediate support for every import/filter/effect. NFR-006/ADR-0012 apply this directive to KPF-1.

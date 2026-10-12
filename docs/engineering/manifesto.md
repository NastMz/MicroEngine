# Product manifesto

Status: confirmed product direction. Origin: repository owner instructions, 2026-10-12.

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

## Principles

Correctness, predictable behavior, clear responsibilities and unambiguous ownership take
precedence over breadth. Public APIs should facilitate correct use; platform details should
not leak into ordinary game logic. Testability, diagnostics and measurable performance are
product goals, not prescriptions for particular technologies.

Architecture may evolve. Significant changes require reasons and consideration of consumers;
compatibility is not a promise of immutability. Do not impose constraints on a future public
versioning policy before it is decided.

## Relationship to the prototype

The prototype is a laboratory and historical reference, not the target architecture.
Reuse is considered only after independent design, contract conformance, demonstrated
technical benefit and absence of incompatible inherited restrictions. Neither reuse nor
rewriting is a goal by itself.

See [requirements](requirements.md), [ADR-0001](decisions/0001-greenfield-direction.md)
and the [roadmap](roadmap.md). Earlier agent recommendations remain proposals only.

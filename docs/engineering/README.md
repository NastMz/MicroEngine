# MicroEngine engineering foundation

This directory is the entry point for the next, greenfield version. It records intent,
authority and evidence without selecting implementation technologies prematurely.

## Start here

1. Read the [product manifesto](manifesto.md).
2. Read the [requirements](requirements.md) and [decision register](decisions/README.md).
3. Check the [roadmap](roadmap.md) for current work and scenario candidates.
4. Follow the [contribution guide](contributing.md) and [document policy](document-policy.md).

## Current state — 2026-10-12

| Category | State |
| --- | --- |
| Confirmed direction | General-purpose 2D C#/.NET framework; greenfield; legacy retained as reference |
| Current authorized work | Documentary foundation; no new engine implementation |
| Accepted direction record | [ADR-0001](decisions/0001-greenfield-direction.md), based on explicit owner instruction |
| Technical architecture | Open: no ECS, backend, runtime API, DI or project layout approved |
| New capabilities | None implemented or verified as part of the greenfield version |
| Existing code | Historical prototype in `src/`, `tests/`, `benchmarks/` and `assets/` |
| Existing automation | Prototype build/test workflow; not proof of greenfield behavior |

Earlier architecture documents under `docs/`, the root README and existing contribution
guidelines remain historical references where they describe the prototype. Their
technical choices do not become greenfield contracts. Preserve them; do not silently rewrite history.
The repository owner is the product approval authority; record explicit approval evidence.

## Artifact inventory

| Artifact | Purpose |
| --- | --- |
| [Manifesto](manifesto.md) | Confirmed product intention and limits |
| [Requirements](requirements.md) | IDs, status, rationale and acceptance evidence |
| [Decisions](decisions/README.md) | ADR lifecycle and current decision index |
| [Capability specification template](templates/capability.md) | Behavior, conceptual contracts and verification |
| [ADR template](templates/adr.md) | Alternatives, consequences and approval evidence |
| [Milestone template](templates/milestone.md) | Scenario-based vertical delivery and exit evidence |
| [Roadmap](roadmap.md) | Current work, candidate scenarios and open decisions |
| [Contribution guide](contributing.md) | Fresh-session continuation and checks |
| [Document policy](document-policy.md) | Lightweight traceability and evolution rules |

Create actual capability or milestone files only when there is concrete scope to record.
Templates are reusable instructions, not approved specifications or scheduled work.

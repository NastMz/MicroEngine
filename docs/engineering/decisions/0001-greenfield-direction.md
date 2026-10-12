# ADR-0001: Greenfield direction and historical prototype

- Status: accepted
- Date: 2026-10-12
- Approver: repository owner, by explicit instruction
- Related: [manifesto](../manifesto.md), [SCOPE-001](../requirements.md)
- Supersedes: no repository ADR; replaces the earlier, unapproved agent recommendation to refactor incrementally

## Context and problem

The repository originally contained a prototype with useful lessons. Treating existing implementations
as mandatory would constrain an independently designed general-purpose 2D framework.

## Alternatives considered

1. Incremental modernization with preservation as a default: prior agent proposal, not approved.
2. Independent greenfield design using the prototype as a historical laboratory: owner-selected direction.

## Decision and justification

Design the next version independently, without compatibility obligations to prototype APIs,
patterns, modules or dependencies. Keep the prototype as reference. Reuse is evaluated only
after target design, new-contract conformance, demonstrated technical advantage and absence
of incompatible inherited restrictions.

## Approval evidence

Repository-owner instruction, 2026-10-12:

> MicroEngine NO debe evolucionarse mediante refactorización incremental. Vamos a tratar su siguiente versión como un proyecto greenfield.

The documentary phase additionally authorizes documents and templates, not engine implementation.
This record does not approve any technical architecture from prior agent proposals.

## Consequences

Requirements and consumer experiences precede technical selection. Historical tests/demos
do not prove the new engine. Greenfield does not require rewriting suitable implementations
by dogma, nor migrating them automatically. New implementation needs separate authorization.

## Transition implementation

The owner approved active-tree retirement on 2026-10-12. The prototype is preserved
at `legacy-final`; see [history](../history.md). This does not approve technical architecture.

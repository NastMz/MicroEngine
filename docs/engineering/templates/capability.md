# CAP-NNN: Capability title

Create under docs/engineering/capabilities/ only for a concrete capability.

- Proposal status: proposed | investigating | approved | deferred | rejected
- Delivery status: not-started | in-progress | implemented | verified
- Approval: owner or delegated technical actor/date/source/review evidence, or pending
- Requirements and decisions: linked IDs
- Implementation and verification: links/evidence, or none

## Problem, responsibilities and limits

Describe the consumer need, owned responsibilities, exclusions and game-owned behavior.

## Expected behavior and use cases

Specify observable behavior and examples. Label API sketches as conceptual proposals;
separate approved behavior, implemented behavior and open questions.

## Public contracts, configuration and extensibility

Describe inputs, outputs, ordering, ownership and extension points without prematurely
fixing classes or dependencies. Link the actual API once implemented.

## Invariants and errors

Define lifecycle, invalid inputs, failure behavior, cleanup and diagnostics.
Include threading and performance constraints only when justified.

## Dependencies, tests and acceptance

Link dependent requirements/capabilities. List observable acceptance cases, unit and
integration checks and external-consumer scenarios. Record commands, environment,
outcomes and limitations; implementation is not verification.

## Evolution and open questions

Record material scope changes and consumer impact. Re-establish affected verification
evidence after relevant changes.

## Authority and acceptance gate

Apply autonomy.md: technical decisions and objective acceptance are delegated; significant ADRs need independent review. Record technical approval actor/source and review separately from subjective product acceptance. Use the complete human-exception list in [autonomy](../autonomy.md); do not create additional technical approval gates. Local commits authorized; no remote/main integration.

## Infrastructure-independence assessment

Apply [ADR-0004](../decisions/0004-replaceable-infrastructure.md)/NFR-005: identify concrete use case, dependency direction and native type exposure, public replacement seam, mandatory versus optional/specialized features, and adapter-specific target/thread/scheduling assumptions. Explain limitations or justified N/A; no universal-interface, additional-backend or exporter requirement. Preserve functional acceptance scope and link affected consumer tests/evidence.

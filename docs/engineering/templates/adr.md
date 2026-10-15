# ADR-NNNN: Decision title

Copy only for a significant choice; replace instructions with real evidence.

- Status: proposed | accepted | rejected | superseded
- Date: YYYY-MM-DD
- Approval: owner or delegated technical actor, date/source and independent review evidence, or pending
- Related requirements, capabilities and ADRs: links
- Supersedes / superseded by: links, or none

## Context and problem

Describe the need, constraints and evidence; separate facts from assumptions.

## Alternatives considered

Compare viable choices, complexity, consumer impact, maintenance and performance implications.
Do not invent measurements.

## Decision and justification

Identify the proposed or accepted choice, rationale and conditions for reconsideration.
A recommendation remains proposed until explicit approval.

## Consequences

Record benefits, costs, risks, compatibility and verification implications.

## Authority and acceptance gate

Apply autonomy.md: technical decisions and objective acceptance are delegated; significant ADRs need independent review. Record technical approval actor/source and review separately from subjective product acceptance. Use the complete human-exception list in [autonomy](../autonomy.md); do not create additional technical approval gates. Local commits authorized; no remote/main integration.

## Infrastructure-independence assessment

Apply [ADR-0004](../decisions/0004-replaceable-infrastructure.md)/NFR-005: identify concrete use case, dependency direction and native type exposure, public replacement seam, mandatory versus optional/specialized features, and adapter-specific target/thread/scheduling assumptions. Explain limitations or justified N/A; no universal-interface, additional-backend or exporter requirement. Preserve functional acceptance scope and link affected consumer tests/evidence.

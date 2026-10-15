# MS-NNN: Validation scenario

Create under docs/engineering/milestones/ only for a bounded milestone.

- Status: candidate | approved | in-progress | verified | deferred
- Approval and origin: owner or delegated technical actor/date/source/review evidence, or pending
- Scenario: external consumer and observable goal
- Requirements, capabilities and decisions: linked IDs

## Scope and dependencies

Define the smallest vertical experience, general capabilities, prerequisites and exclusions.
Keep scenario mechanics in the game, not mandatory engine libraries.

## Risks and decisions

Record relevant risks and decisions required before dependent work.

## Acceptance and evidence

Define public-interface tests, integration checks and exit criteria. Record implementation
links, commands/results, failures, skipped checks and environment limitations.

## Next work

State the next bounded action and blockers without exhaustively planning future milestones.

## Authority and acceptance gate

Apply autonomy.md: technical decisions and objective acceptance are delegated; significant ADRs need independent review. Record technical approval actor/source and review separately from subjective product acceptance. Use the complete human-exception list in [autonomy](../autonomy.md); do not create additional technical approval gates. Local commits authorized; no remote/main integration.

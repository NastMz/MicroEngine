# Architecture decision register

ADRs capture significant choices, not every edit. A generated proposal is never acceptance.

| ID | Title | Status | Date | Approval basis |
| --- | --- | --- | --- | --- |
| [ADR-0001](0001-greenfield-direction.md) | Greenfield direction and historical prototype | accepted | 2026-10-12 | Explicit repository-owner direction, quoted in the record |
| [ADR-0002](0002-initial-execution-scope.md) | Initial execution scope | accepted | 2026-10-12 | Owner scoped approval and explicit technical delegation recorded in ADR |
| [ADR-0003](0003-next-validation-tracer.md) | Next validation tracer boundary | accepted | 2026-10-15 | Codex root under owner delegation; independent design PASS |
| [ADR-0004](0004-replaceable-infrastructure.md) | Replaceable infrastructure and bounded capability contracts | accepted | 2026-10-15 | Owner directive; Codex root delegated adoption after independent review PASS |
| [ADR-0005](0005-provider-neutral-validation.md) | Provider-neutral frame validation | accepted | 2026-10-16 | Delegated approval after push_policy_review design PASS |

## Convention

Use `NNNN-short-title.md`, with monotonically increasing IDs and the [ADR template](../templates/adr.md).
States: proposed, accepted, rejected, superseded. Accepted records name approver, date and
approval evidence. The owner approves product/architecture decisions unless delegation is recorded.

To replace an accepted decision, write a new ADR and obtain explicit approval. Mark the
old ADR superseded and link both directions. Preserve its original reasoning and decision;
do not rewrite the old conclusion as though the new decision always existed.

ECS, entity storage, scenes/worlds, DI, rendering, platforms, physics and packaging remain
open. No prior chat recommendation is registered as accepted. See [open decisions](../roadmap.md#decisions-still-open).

## Current technical delegation2026-10-15

Under [autonomy](../autonomy.md), agents may accept/supersede technical ADRs after independent review. Record actor/date/source and review evidence; preserve replaced decisions with bidirectional links. Strategic product vision and subjective product acceptance remain human-owned.

[ADR-0006](0006-consumer-checkpoints.md) accepted2026-10-17: consumer-owned checkpoints, independent checkpoint_design_review PASS, Codex root delegated approval.

[ADR-0007](0007-checkpoint-file-boundary.md) accepted2026-10-17 by Codex root: optional consumer file boundary, checkpoint_design_review PASS before implementation.

[ADR-0008](0008-optional-label-canvas.md) accepted2026-10-17 by Codex root after checkpoint_design_review PASS: optional short-label profile, primitive providers unchanged.

[ADR-0009](0009-optional-image-resources.md) accepted2026-10-17 by Codex root after checkpoint_design_review PASS: optional immutable image values/context-owned resource leases.

- [ADR-0010 — Optional source-region image drawing](0010-optional-image-regions.md): accepted after independent SPR-1 design review.

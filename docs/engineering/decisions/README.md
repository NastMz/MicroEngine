# Architecture decision register

ADRs capture significant choices, not every edit. A generated proposal is never acceptance.

| ID | Title | Status | Date | Approval basis |
| --- | --- | --- | --- | --- |
| [ADR-0001](0001-greenfield-direction.md) | Greenfield direction and historical prototype | accepted | 2026-10-12 | Explicit repository-owner direction, quoted in the record |
| [ADR-0002](0002-initial-execution-scope.md) | Initial execution scope | accepted | 2026-10-12 | Owner scoped approval and explicit technical delegation recorded in ADR |

## Convention

Use `NNNN-short-title.md`, with monotonically increasing IDs and the [ADR template](../templates/adr.md).
States: proposed, accepted, rejected, superseded. Accepted records name approver, date and
approval evidence. The owner approves product/architecture decisions unless delegation is recorded.

To replace an accepted decision, write a new ADR and obtain explicit approval. Mark the
old ADR superseded and link both directions. Preserve its original reasoning and decision;
do not rewrite the old conclusion as though the new decision always existed.

ECS, entity storage, scenes/worlds, DI, rendering, platforms, physics and packaging remain
open. No prior chat recommendation is registered as accepted. See [open decisions](../roadmap.md#decisions-still-open).

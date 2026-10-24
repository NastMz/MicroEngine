# Living documentation policy

Status: active initial documentary operating convention, established under the owner's
2026-10-12 request to prepare and validate AI-first execution. It governs authorized
sessions; current technical execution/approval authority is recorded in autonomy.md; ADR-0002 retains historical H0 scope.

## Sources of truth

| Question | Record |
| --- | --- |
| Product intent | Manifesto |
| Required behavior | Requirements and approved capability specifications |
| Long-running mission scope and recovery | `odd/tasks/microengine-three-game-mission.md` plus its Engram mirror when available |
| Significant choice | Accepted ADR with explicit approval evidence |
| Current scenario and next work | Roadmap and scoped milestone |
| What actually works | Implementation links and observed verification evidence |

If code contradicts a contract, record the discrepancy. Do not silently change the
approved contract to match code; resolve authority and scope first.

## Lifecycle and authority

- Proposed: suggestion without commitment.
- Investigating: evidence gathering, not approval.
- Approved: owner or delegated technical authority accepted scope; record actor/date/source and independent review for significant decisions.
- Implemented: code exists; link it. Checks may still be pending.
- Verified: acceptance observed; record command, environment, outcome and evidence.
- Deferred/rejected: preserve ID and reason.

Accepted ADRs do not prove implementation. Milestones need observed exit criteria.
Changed behavior or acceptance invalidates affected verification: preserve the old result
historically and reopen only affected work. Delegated agents may approve technical proposals only after required independent review, with rationale/evidence; subjective product acceptance remains human-owned.

## Proportional updates

Small corrections update existing records. Concrete capabilities need specifications;
significant tradeoffs need ADRs. No empty capability files, documents per class,
duplicate trackers or speculative future specifications.

Link requirement, specification, ADR and milestone IDs when related artifacts exist.
Keep evidence in the relevant record; do not duplicate entire documents.

## Changes and completion

Update relevant documentation with code changes; completion requires consistent docs
and applicable checks. Record architectural rationale and consumer impact. Preserve
superseded ADRs and link replacements in both directions. Changed approved scope
requires renewed approval. Report failures and limitations instead of promoting status.

The prototype is reference, not a greenfield contract. External memory may assist
navigation but cannot be the only home of authoritative project context.

## AI-first planning records

[Backlog](backlog.md) owns task state; [execution](execution.md) defines transitions.
Approval of a requirement/spec/ADR, task completion and milestone manual acceptance
are distinct. Ready does not authorize unapproved implementation. Changes to task
scope/spec invalidate affected readiness and proof. Capture new needs in [findings](findings.md)
without expanding current work. The owner requested this operating model on 2026-10-12;
its conventions are active for authorized work and may evolve through recorded changes;
technical acceptance now follows the2026-10-15 delegation in autonomy.md, not automatic promotion.
The 2026-10-24 mission supplies the durable completion outcomes: when no task is ready, refine
the next evidence-backed mission slice in the backlog rather than treating the empty queue as
completion or inventing unrelated scope.

## Architectural review baseline

[ADR-0004](decisions/0004-replaceable-infrastructure.md)/NFR-005 is permanent for subsequent horizons. Significant ADRs and affected capability specs record provider replacement/optionality, dependency and native-type boundaries, target assumptions and scope-preserving tradeoffs. Existing evidence is not retroactively evidence of all backends or targets.

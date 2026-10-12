# Living documentation policy

Status: initial convention authored for owner review, not approval of technical architecture.

## Sources of truth

| Question | Record |
| --- | --- |
| Product intent | Manifesto |
| Required behavior | Requirements and approved capability specifications |
| Significant choice | Accepted ADR with explicit approval evidence |
| Current scenario and next work | Roadmap and scoped milestone |
| What actually works | Implementation links and observed verification evidence |

If code contradicts a contract, record the discrepancy. Do not silently change the
approved contract to match code; resolve authority and scope first.

## Lifecycle and authority

- Proposed: suggestion without commitment.
- Investigating: evidence gathering, not approval.
- Approved: owner accepted scope; record person/date/evidence.
- Implemented: code exists; link it. Checks may still be pending.
- Verified: acceptance observed; record command, environment, outcome and evidence.
- Deferred/rejected: preserve ID and reason.

Accepted ADRs do not prove implementation. Milestones need observed exit criteria.
Changed behavior or acceptance invalidates affected verification: preserve the old result
historically and reopen only affected work. Agents cannot approve their own proposals.

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

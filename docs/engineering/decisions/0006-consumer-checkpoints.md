# ADR-0006 — Consumer-owned checkpoints before engine storage

Status: proposed2026-10-17. Technical authority: [autonomy](../autonomy.md). C-003/CAP-013 discovery: [CP-1](../specs/consumer-checkpoint.md).

## Proposed decision and alternatives

Serialize actual TurnSession durable state to versioned consumer-owned bytes. Validated additive factories on Explorer/TurnSession retain default behavior; codec lives in TurnExplorer.Model. No engine persistence API/file backend/native/dependency changes. Host chooses where bytes live. Transient update notifications restore as false.

Input replay rejected as checkpoint format: unbounded logs depend on historical update semantics. JSON deferred: readable/extensible, but needs missing/duplicate/unknown-field policy. Fixed24-byte little-endian format selected for bounded schema/exact validation; not an engine format or future migration promise. Generic ISaveStore/file adapter deferred: would conflate serialization/platform durability before a concrete IO requirement. Independent bytes provide replacement freedom without speculative interfaces.

## Architectural consequences

No concrete infrastructure leaks into engine, mandatory system or private reflection/friend access. Public factory enables validated consumer state; existing constructor/update behavior preserved. Future filesystem/browser adapters may consume bytes without core edits, not a support/export commitment. Version evolution requires explicit review; no global serialization/versioning policy. Resource bound24bytes, no concurrency/session mutation/untrusted paths. No encryption/authenticity/disk durability claims.

## Approval

Pending independent design review; no source construction before recorded acceptance.

Accepted2026-10-17 by Codex root under technical delegation after independent checkpoint_design_review PASS (no blockers). Proposed status above is historical drafting context; operative status accepted. CP-1 additive; TM-1 historical acceptance unchanged.

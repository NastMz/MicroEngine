# ADR-0011 — Durable turn completion with compatible checkpoints

Status: accepted2026-10-17 by Codex root after checkpoint_design_review PASS. [TURN-2](../specs/turn-outcome.md), C-003/CAP-013, supersedes CP-1 version1-only Capture/Restore scope and TM-1 infinite phase wrap only where explicitly noted. ADR-0004/0006 foundations remain.

## Need and choice

The external turn tracer currently cycles an indicator without lasting outcome. Choose one deterministic four-confirmation completion, owned entirely by TurnExplorer, and persist its boolean outcome without engine battle/progression abstractions. Existing phase is pending progress; fourth confirmation closes the menu/completes once. Restored old pending phase is preserved; historic cycles cannot be reconstructed.

Use Version2 snapshots, same24byte format and offsets, additional flagbit3; preserve exact valid Version1 encode/decode and read existing files. Capture normalizes to2; reads never rewrite input. Add optional record field to retain old construction sites, but document capture equality/version change and reject inconsistent completion state. No authenticity/durability or old-binary-forward-compatibility claim.

## Alternatives and consequences

- Reject all old version1: unnecessary data incompatibility; keep a narrow explicit reader instead.
- Reinterpret v1 spare bit without version bump: rejected because old semantics and strictmask would become ambiguous.
- Separate outcome file or generic migration registry: unnecessary lifetime/consistency/complexity for one boolean.
- New battle/stat/quest system: outside demonstrated need; no game rules in Core.
- Never complete, merely add cosmetic turns: fails the next bounded meaningful interaction proof.

The public consumer contract and persisted output evolve, so independent format/behavior review, exact v1 fixtures, v2 validation and process/no-overwrite proof are mandatory. No user files are migrated/deleted/replaced. Reverting binaries after new v2 saves requires preserving a compatibility reader, not silently pretending those files are v1. Technical approval is delegated; subjective completion clarity/fullH3 acceptance is not.

## Approval

Independent checkpoint_design_review PASS/no blockers before root technical approval. Optional positional field preserves eight-argument source construction only, not binary constructor/deconstruction shape; no current consumer requires a deconstruction overload.

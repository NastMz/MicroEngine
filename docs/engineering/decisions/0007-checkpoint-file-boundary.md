# ADR-0007 — Optional consumer checkpoint file boundary

Status: accepted2026-10-17 by Codex root after independent checkpoint_design_review PASS; technical authority [autonomy](../autonomy.md). Refines accepted [ADR-0006](0006-consumer-checkpoints.md) via [CF-1](../specs/checkpoint-files.md).

## Proposed decision

Put synchronous create-new/read file operations in optional examples/TurnExplorer.Storage, referencing only TurnExplorer.Model. Existing model/engine/executables unchanged. Consumer supplies path explicitly, no automatic save directory, slots, per-frame IO or keybinding. Separate bytes from filesystem assumptions; no general engine ISaveStore before demonstrated cross-consumer need.

Alternatives: direct File.WriteAllBytes on destination risks destructive overwrite/partial target; rejected. Replacement/backup protocol useful later, but not required for first disk round-trip; deferred. Generic injected filesystem abstraction solely for this small adapter would widen contracts; defer. Write owned unique sibling temp with CreateNew, close, then File.Move without overwrite; clean only owned temp and preserve both primary/cleanup failures. Existing destinations remain untouched. Read at most25bytes before exact CP-1 decoding.

## Guarantees and limits

Normal-process complete-file publication, no success before close/move; not crash/power-loss durability or portable transactional atomicity. No concurrent writers, hostile directories/symlink sandbox, encryption or filesystem portability claim. Explicit Windows local filesystem validation, future browser storage can consume CP-1 bytes without this optional library. New library is a consumer adapter, not mandatory framework infrastructure. No existing user files manipulated in tests.

Primary sources checked2026-10-17: [File.Move](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.move?view=net-10.0) documents existing-destination failure when overwrite=false; [FileMode.CreateNew](https://learn.microsoft.com/en-us/dotnet/api/system.io.filemode?view=net-10.0) fails rather than truncates an existing file. These primitives support the bounded process contract, not a universal atomicity guarantee.

## Approval

Independent checkpoint_design_review PASS/no blockers before construction. Advisory incorporated: preserve write error if close also fails, close attempt precedes cleanup. Approval under documented technical delegation; no engine/global storage contract.

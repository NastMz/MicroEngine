# CP-1 — Versioned consumer checkpoint

Status: approved2026-10-17 by Codex root under technical delegation after checkpoint_design_review PASS under ADR-0006; C-003/CAP-013/FR-002/NFR-001/002/004/005. Next bounded horizon only, not engine storage/full H3.

## Public contracts

Explorer.Create(Room room, float x, float y, bool markerActivated) returns a new Explorer with durable state and InteractionOccurred=false. Validate defined Room, finite x in[0,616], finite y in[0,328], else ArgumentOutOfRangeException. Existing default constructor/Update unchanged.

Immutable public sealed record TurnCheckpoint(int Version, Room Room, float X, float Y, bool MarkerActivated, bool MenuOpen, MenuOption SelectedOption, int TurnPhase). TurnSession.Capture() returns current durable values, Version1. Static TurnSession.Restore(TurnCheckpoint checkpoint) returns independent new session; null -> ArgumentNullException; Version1, valid Room/coordinates, defined selection, phase0..3 required, otherwise ArgumentOutOfRangeException. Closed Return and menu away from marker are valid durable states; no historical-input reachability constraint. TurnResolved/InteractionOccurred are transient, restoredfalse. Next valid Update follows unchanged TM-1; no in-place load or existing-instance mutation.

Public static TurnCheckpointCodec.Encode(TurnCheckpoint checkpoint) validates through Restore and returns fresh byte[24]. Decode(byte[] data): null -> ArgumentNullException; non24 length -> FormatException; invalid/unsupported fields -> FormatException with original ArgumentException retained. Six32-bit little-endian words at offsets0/4/8/12/16/20: Version1, Room0/1, IEEE754float X, IEEE754float Y, flags(bit0 marker, bit1 menu, bit2 Return selection), phase0..3. Other flags rejected. No trailing bytes, mutable array retention, native-layout casts, unbounded stream reading, migrations or authenticity claim.

## Task packets and proof

ME-030 P1 design, claimed Codex root751686a ready->in-progress: independent review of this contract/ADR before acceptance. Links/readback/diff checks, runtimeN/A passive design.

ME-031 P1 implementation pending ME-030 approval. Source surfaces: examples/RoomExplorer.Model/Explorer.cs; examples/TurnExplorer.Model/{TurnSession.cs,TurnCheckpoint.cs,TurnCheckpointCodec.cs}; checks/Loop.Contracts/CheckpointTests.cs. Docs surfaces: engineering/{backlog.md,current-state.md,roadmap.md,capabilities.md,milestones/H3.md,decisions/README.md,decisions/0006-consumer-checkpoints.md,specs/consumer-checkpoint.md}; odd/tasks/h3-checkpoint.md. No engine/other executable/native edits.

Acceptance: initial/exploration/marker/menu/selected/all phases/Hall/boundary round-trip; exact endian fixture/length/flags; instance/array independence; reject bad version/enums/phase/NaN/infinity/bounds/null/truncation/trailing/unknown flags preserving source/input; transient reset; identical durable post-restore behavior under same ordered input; default behavior/all157baseline tests preserved. Focused RED before behavior then GREEN; full serial Release tests checks/Loop.Contracts/Loop.Contracts.csproj, checks/Desktop.Contracts/Desktop.Contracts.csproj, checks/Hosting.Contracts/Hosting.Contracts.csproj with --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false. Existing three native harnesses prove shared-consumer regression. Independent fresh scratch public probes/baseline comparison; parent spotcheck. No new subjective gate: objective acceptance delegated, main integration still owner delivery policy.

Risk items1/3 serialized format/additive shared consumer contract; independent design/verification required. Rollback own additive factories/checkpoint source/tests/docs only. No user save files written; disk replacement/durability/IO-error semantics explicitly not provided. Future disk tracer remains unprepared until need/failure boundaries refined.

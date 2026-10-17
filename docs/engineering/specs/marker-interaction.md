# IM-1 — Room-local marker interaction

Status approved2026-10-17 by Codex root under delegation after independent marker_design_review PASS; ME-026/027 completed, implemented and independently verified. Owner continuation2026-10-17, H2/C-002/FR-002, ADR-0004. Small consumer-only delta to [RE-1](room-explorer.md), not a generic engine interaction capability.

## Outcome and exclusions

An orange marker in the Courtyard responds to Space while the explorer overlaps it, toggles green/orange and emits one interaction trace. This probes H2 manual interaction without NPCs, dialogue/text rendering, combat/progression/inventory, durable storage, scene/world/asset systems, new inputs or engine API. Retain all136baseline tests, source project dependencies and movement/portal/window/CLI behavior. No FirstPlayable, Desktop or Loop source changes.

Existing Desktop names Space as "jump". RoomExplorer locally interprets that Pressed edge as interaction; it is not a mandatory engine vocabulary or a new platformer rule. RE-1 still ignores Pressed/jump for locomotion; only the new consumer interaction state responds. No public scene/interaction/provider contract or new ADR is needed for this bounded consumer behavior under ADR-0004.

## Exact consumer state/content

In Explorer add public const string MarkerId="courtyard-marker", public const Room MarkerRoom=Room.Courtyard, public const float MarkerX=300,MarkerY=150,MarkerWidth=40,MarkerHeight=60. These source-authored consumer values are shared by collision eligibility and executable presentation; do not use Loop.Rect as gameplay geometry or introduce a level format.

Add public bool MarkerActivated and InteractionOccurred getters with private mutation, initially false. Marker is nonblocking; retain existing Explorer APIs, bounds, normalization, portal semantics and validation order. No separate TryInteract method or new project/package. State belongs to each Explorer instance; retained during that instance's room transitions, fresh instance resets both flags. This is in-memory game state, not CAP-013 persistence or a save format.

## Update semantics

1. Preserve input-null then positive-delta validation before changing any field, including interaction flags. Invalid calls preserve all state.
2. Each valid Update clears InteractionOccurred, then resolves movement/transition exactly as RE-1. A room-transition update produces no interaction (existing early return); MarkerActivated remains unchanged. No buffered request/edge/event crosses updates or room changes.
3. Only the final current position is tested. In MarkerRoom require strict interior overlap: X<MarkerX+MarkerWidth && X+24>MarkerX && Y<MarkerY+MarkerHeight && Y+32>MarkerY. Exact edge/corner touch is not overlap. Crossing a marker without ending overlapped does not interact; no swept trigger claim.
4. If eligible and input.Pressed contains exact case-sensitive "jump", toggle MarkerActivated once and set InteractionOccurred=true. Duplicate edges in one snapshot coalesce; Held alone never interacts. Separate valid updates with fresh edges may toggle repeatedly, no debounce/cooldown. Outside-room/range/unrelated/case-mismatched edges do nothing, no latent intent.
5. InteractionOccurred describes the last valid Update, not an event queue. It remains readable until the next valid Update. Presentation never consumes, repeats or mutates it; Program emits trace only from its fixed update callback, once on success.

## Presentation and trace

Program draws the marker in its room using the model constants, before the explorer rectangle: orange Rgba(180,100,70) while inactive, green(80,200,120) while activated. Existing room/passage/player rendering unchanged. Marker does not block traversal. Controls line documents "Space interacts with marker" alongside existing movement/exit controls.

After game.Update and existing room-change trace, if InteractionOccurred print exactly interaction:courtyard-marker;active:true or interaction:courtyard-marker;active:false using the new state. Render callbacks never print. No new command flags; existing usage/outcome/error/exits and both native harness expectations stay unchanged. Marker color is direct visual confirmation; no text renderer/assets/UI framework.

## Acceptance and evidence

New checks/Loop.Contracts/MarkerInteractionTests.cs tests public Explorer:
- Fresh flags/instance isolation; Pressed in range toggles, next valid empty/held clears occurrence but retains activation; two fresh edges toggle back; duplicate edges coalesce.
- Eligibility exact strict overlap on all four edges/corners; both room/range rejection; move-then-interact final-position semantics, no swept request; transitions clear occurrence/preserve activation, round trip retains only the instance state.
- Case/unknown/Held input rejection and no delayed action when later entering range. Invalid null/zero/negative calls preserve room/X/Y/flags, including occurrence=true after success.
- Existing movement/replay/portal tests unchanged; independent actual-baseline comparison verifies room/X/Y traces unchanged for mixed movement and jump edges. Program uses same constants for drawing and traces only at successful updates.

Runnable RED before model behavior; focused/full GREEN serial dotnet test checks/{Loop.Contracts,Desktop.Contracts,Hosting.Contracts}/respective.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false. Both checks/Desktop.Contracts/NativeSmoke.ps1 and checks/RoomExplorerSmoke.ps1 rebuild and prove native lifecycle/CLI. No physical keyboard automation or subjective success inferred from fake input tests/native startup.

Independent design and fresh scratch implementation verification plus parent spotcheck before technical closure. Subjective handoff only interaction discoverability/response/color readability, not repetition of technical checks. Positive owner feedback after delivery triggers main integration under autonomy; no automatic full-H2 acceptance.

## Scope, risk and rollback

Allowed source surfaces: examples/RoomExplorer.Model/Explorer.cs, examples/RoomExplorer/Program.cs, new checks/Loop.Contracts/MarkerInteractionTests.cs. Docs: engineering/{specs/marker-interaction.md,specs/room-explorer.md,backlog.md,current-state.md,roadmap.md,milestones/H2.md,capabilities.md}; odd/tasks/h2-marker-interaction.md. Risk3shared consumer behavior; no engine/dependency/control binding change. Rollback only own marker state/eligibility/render/trace/tests/doc deltas; keep all RE-1 movement/portal, H1 and unrelated work. No significant engine architecture decision; do not extract this single-consumer policy into Core.

Verification2026-10-17: author149tests/both native seven-case harnessesPASS; independent marker_verify98Loop/98 plus marker probes/1000actual-baseline movement comparison/native7PASS, build0warnings/0errors. No engine/project/dependency changes. Subjective interaction unobserved, new feature awaits own positive feedback for main integration.

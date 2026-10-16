# PO-1 — Static solids in the external platformer

Status: approved2026-10-16 by Codex root under technical delegation; independent obstacle_design_review targeted recheck PASS after three contract corrections. ME-018 completed, ME-019 ready. Owner continuation2026-10-16, roadmap H1/F-010, FR-002 and ADR-0003/0004 authorize this bounded refinement. No engine geometry/physics API is selected.

## Outcome and boundaries

The external sample can stop against and jump onto a small collection of static rectangular solids. Movement resolution and jump policy remain consumer-owned. Keep all93existing tests, PC-1 optional buffer, movement180/impulse-300/gravity900, player24x32, floor300 and x0..616. No backend/dependency/new engine project, ECS, scene, spatial index, dynamic bodies, slopes, one-way surfaces, animation, coyote time or asset pipeline. Full H1 is not accepted by this increment.

## Exact surface and ownership

- Add consumer-local `public readonly record struct StaticObstacle` with get-only properties `float X,Y,Width,Height` (no init setters or invalidating with expressions) and explicit four-float constructor. All values and sums X+Width/Y+Height must be finite; X,Y >=0, Width,Height >0, right<=640 and bottom<=300. Invalid arguments throw ArgumentOutOfRangeException. Default struct is invalid and must also be rejected when supplied to GameModel.
- Retain GameModel() and GameModel(PlatformerOptions). Add `GameModel(PlatformerOptions options, IEnumerable<StaticObstacle> obstacles)`; validate options first, then obstacles using ArgumentNullException with the respective parameter name. Enumerate exactly once into owned storage and validate elements in enumeration order. Invalid element geometry (including default) throws ArgumentOutOfRangeException(nameof(obstacles)); strict interior overlap with initial player(40,268,24,32) throws ArgumentException with parameter obstacles. Enumeration exceptions propagate unchanged. Edge contact is valid. No arbitrary count limit; finite inputs are caller responsibility.
- Add `IReadOnlyList<StaticObstacle> Obstacles` backed by a read-only copy (not the caller's array). Existing constructors delegate to an empty set, preserving their observable behavior. Duplicate/overlapping obstacles are allowed; equivalent geometry in another enumeration order must yield the same motion.
- Do not import Loop.Rect for geometry. Program converts obstacle coordinates into the existing presentation Rect only at drawing time. No new provider interfaces or inheritance hooks for a single consumer case.

## Resolution contract

Coordinates are existing canvas units, Y positive down. Strict positive overlap on the perpendicular axis is required; touching at a corner or along a nonmoving edge is not interior overlap. Contact along the movement axis blocks further travel. No epsilon/rounding tolerance; exact float contact positions are assigned from geometry. This is a bounded axis-separated kinematic example, not continuous diagonal rigid-body simulation.

Each Update retains input validation and PC-1 aging/refresh order. Then:

1. Compute and clamp requested X using existing speed/delta. Sweep horizontally from old X to requested X at the existing Y. For each solid with strict vertical overlap, select the nearest face crossed in the direction of travel, including an initially touching face. Stop at face-playerWidth when moving right, or faceRight when moving left. No tunneling through a wall even for a large positive delta. Choose min/max geometric position, independent of enumeration order.
2. Revalidate support only when the model entered the update Grounded: floor when Y==268, or solid top when Y+32==top with strict horizontal overlap at resolved X. Set Grounded = Grounded && hasSupport. Never promote an airborne touching model to Grounded by position alone; only downward sweep contact may do that. A PC-1 contact launch with negative velocity/Groundedfalse continues upward on the next empty update. Walking off a raised solid removes Grounded immediately. No grace interval or coyote eligibility is introduced. The full-width floor remains support everywhere in bounds.
3. Apply existing direct/pending grounded-jump eligibility; consume request once. If airborne, update gravity and desired Y exactly once as before.
4. Sweep vertically at resolved X. Falling selects nearest crossed top (top-32), with floor268 as an additional candidate. Rising selects nearest crossed underside (bottom). Set Y to contact and vertical velocity0. Only downward contact sets Grounded. Wall/ceiling contact never grants jump eligibility. No tunneling along this axis; sequential X/Y resolution does not claim true diagonal time-of-impact.
5. On downward contact, PC-1 pending valid intent may launch with -300/Groundedfalse at contact, without another integration. Cancellation and single-use rules remain unchanged. Standing support does not integrate gravity. No collision events/public physics report.

Directional crossing predicates (with strict perpendicular overlap): right oldRight<=solid.Left and requestedRight>=solid.Left; left oldLeft>=solid.Right and requestedLeft<=solid.Right; falling oldBottom<=solid.Top and requestedBottom>=solid.Top; rising oldTop>=solid.Bottom and requestedTop<=solid.Bottom. Floor uses the same falling predicate with top300. No sweep for zero displacement. Movement away from an initially touching face is not blocked. Select nearest contact coordinate; equal coordinates are observationally identical.

If no contact, retain computed Y and Groundedfalse. Two solids with the same stopping face produce identical state; no object identity tie breaker is observable. This scope does not support changing solids, initial penetration, teleportation or pushes; supplied geometry stays immutable for the model lifetime.

## Sample integration

Only FirstPlayable opts into one demonstrable box at(180,260,64,40), drawn in a distinct solid color using the same immutable model list used for collision. Its40-unit rise is below the existing jump height. Start/floor/player/keys/CLI/window behavior remain unchanged. This is not a level-authoring format. New obstacle feel requires actual subjective feedback if evaluated; automated evidence cannot accept the full Mario milestone.

## Acceptance and evidence

Public deterministic tests in new checks/Loop.Contracts/ObstacleTests.cs must demonstrate:

- Empty-list and existing constructors produce identical X/Y/velocity/grounded traces for mixed movement/jump input; preserve all93baseline tests.
- Constructor validation including default struct, NaN/infinity, negative/zero/out-of-bounds geometry, nulls and initial overlap; source-array mutation does not alter the model list and exposed list cannot mutate geometry.
- Right/left nearest wall stop, initial-face contact, no wall tunneling at large delta, corner touch does not block, order/duplicate invariance, movement away from initially touching faces.
- Land on a top, jump off it, walk off and fall, hit underside with zero upward velocity but not Grounded; no side-contact support.
- Buffered floor/raised-solid launches move upward on the next empty update without re-grounding; moving away from vertical initial contact remains possible. Landing buffer consumes once on raised solid; expired/disabled buffer does not invent a jump; pause cancellation still passes existing tests. Separate horizontal/vertical expectations, no diagonal physics claim.
- Actual sample draws the collision list; existing native seven scenarios still pass. No engine source/dependency/config changes.

Observe runnable RED tests before behavior implementation, then focused/full GREEN. Commands: serial `dotnet test checks/Loop.Contracts/Loop.Contracts.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false`; Desktop.Contracts and Hosting.Contracts same flags; `checks/Desktop.Contracts/NativeSmoke.ps1`. Independent scratch verification after own checks, parent spotcheck, diff/check-only validation. No subjective check blocks objectively verified work.

## Alternatives and review

Choose consumer-local immutable rectangles and axis sweeps: concrete F-010 need without committing Core to platformer rules. Reject ad-hoc extra clamps because they obscure ownership and cannot test multiple solids. Defer extraction into engine geometry until another concrete consumer demonstrates reusable semantics; Loop.Rect remains a presentation value. General physics/backend/ECS introduces unjustified scope. Axis ordering is intentionally simple and may produce different corner motion than a continuous solver; keep this limitation explicit rather than promising universal collision.

Risk item3: additive public consumer contracts/shared movement behavior; independent design and implementation review required. No significant engine architecture change, so PO-1 suffices under existing ADRs rather than a new engine ADR. Rollback only own new rectangle/tests and GameModel/Program deltas plus associated task/docs; preserve unrelated files and all prior buffered behavior. Findings go to findings.md; do not add coyote time or animation to fix this slice implicitly.

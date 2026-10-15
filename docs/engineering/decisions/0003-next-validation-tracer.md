# ADR-0003 — Next validation tracer boundary

Status: proposed. Prepared 2026-10-15 under ME-007; delegated technical review/approval pending.
This record does not supersede accepted ADR-0002 or alter H0 acceptance.

## Context and evidence

H0 is owner accepted; its headless lifecycle is not yet a game loop or native host.
The next roadmap candidate is H1, an external platformer validating general capabilities.
Reviewed ME-005 [scenario](../specs/h1-first-playable.md) and ME-006
[options report](../investigations/h1-native-options.md) bound the next horizon.
Neither document approves public signatures or dependencies.

## Technical decision under delegated review

Approve a bounded **H1.0 graphical tracer next**, on Windows only: external consumer,
primitive window/presentation, input, fixed-step execution and consumer-owned floor/jump
rules. Authorize subsequent technical specification and reversible private native
binding/version selection after independent review. This is new scope/dependency
authority, not routine approval of names, keys, dimensions or methods.

H1.0 is not full H1. No ECS, scenes, solver, animation/assets pipeline, audio, distribution,
multiplatform promise or broad game implementation is included. H0 remains permanent
and usable without loading native graphics libraries. No backend name is accepted here.

## Alternatives and recommendation

1. **Graphical H1.0 next (recommended):** obtains early evidence about native lifecycle,
   input and presentation boundaries, but adds desktop/hardware/deployment risk.
2. **Headless CAP-002 first:** smaller native risk and deterministic timing evidence;
   postpones real event/presentation integration. Requires a revised bounded packet,
   not automatic approval of the existing frame proposal.
3. **Defer both:** retain accepted H0 and pause further construction; no evidence lost.

Full H1 immediately is not recommended: too many unrefined capabilities.
Raylib-cs is a provisional investigation preference, SDL3-CS a viable comparison;
neither is selected. Exact package/native compatibility and licenses need evidence.

## Contract and coupling consequences

Potential new public boundaries concern timing, input snapshots/actions and presentation
commands; **none is committed yet**. ME-008 must specify exact signatures, units,
ordering, ownership and failure behavior. Avoid public native handles/types and
mandatory graphics initialization in headless consumers. A private backend is more
reversible than exported native APIs, but still entails deployment and maintenance cost.
Review H0 compatibility explicitly; breaking its contracts is outside this delegation.

## Technical discretion under owner delegation2026-10-15

Within approved scope agents may decide names, project partition, keys/units/defaults,
private mechanisms, exact supported dependency versions and deterministic tests,
with rationale, independent review and reproducible verification. Escalate scope
growth, public native coupling, H0 breaks, new platform promises or difficult-to-reverse
architecture. ECS/scenes/physics choices are not silently delegated.

## Readiness and response to rejection/modification

ME-008 readiness awaits independent technical review and delegated acceptance of this record. Then finalize exact
paths/signatures, input-edge/time/stop/focus semantics, selected dependency/native assets,
licenses, Windows runner/environment, failure/deployment checks and rollback. Obtain
independent contract review before separate implementation tasks become ready.
No installation or native code is authorized by mere existence of this proposal.

If technical review selects timing-first, revise only affected proposal/tasks and preserve
graphical research as historical input. If deferred, leave ME-008 blocked. If modified,
update linked scope and review impacted contracts; do not treat partial agreement as
blanket approval. H0 acceptance and its evidence remain unchanged.

## Approval record

Pending independent technical review. Approval may be recorded by the delegated agent under autonomy.md; no subjective product acceptance is implied.

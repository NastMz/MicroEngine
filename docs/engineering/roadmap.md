<!-- Current authority:2026-10-15 technical delegation supersedes earlier approval-only restrictions; see autonomy.md. Historical dated evidence is not rewritten. -->
# Evolutionary roadmap

H0 is accepted by the owner (2026-10-15). Current horizon: prepare the next H1 candidate through bounded specifications/investigations and task packets; reviewed future technical construction is delegated within the roadmap. The prototype remains at [legacy-final](history.md).

## Current executable horizon

[Backlog](backlog.md) is the only task-state source. ME-001/002/003 preparation is complete; ME-004 automated implementation is completed; H0 owner acceptance is recorded; H1 candidate preparation is authorized by the current owner request. See [current state](current-state.md).
An authorized future development session follows [execution](execution.md) and may continue
independent ready work. No background scheduler is installed; H0 is implemented, not a playable game.

## Milestones and candidate validation scenarios

| ID | Scenario/outcome | Candidate capabilities | Exit criteria to refine | Status |
| --- | --- | --- | --- | --- |
| H0 | External application starts/runs/stops safely | CAP-001 headless slice only | Lifecycle/failure traces, runnable public consumer and31 tests; explicit owner acceptance | Verified / owner accepted2026-10-15 |
| H1 | Simple Mario-inspired platformer | CAP-002/003/004/005/006/007/008 | Movement, obstacles, jump and animation in an external game; game owns jump rules; manual playability | Candidate |
| H2 | Zelda-inspired top-down adventure | CAP-009/010 plus refined presentation/input | Exploration and transitions without engine-owned combat/progression; manual interaction | Candidate |
| H3 | Pokemon-inspired RPG | CAP-012/013 plus validated shared capabilities | Exploration, menus and turn interaction; game owns battle/turn rules; manual flow | Candidate |
| H4 | Metroidvania / modern platformer stress | Needs discovered from earlier milestones | Greater content/state/movement demands with evidence, not presumed Core modules | Deferred candidate |

H0 is an engineering prerequisite, not proof of game generality. H1-H4 are a proposed
refinement sequence, not approved game scopes, dates or capability commitments.
Select/refine each milestone only when prior evidence identifies necessary work.
Use [capability catalog](capabilities.md), [milestone template](templates/milestone.md)
and [manual delivery](milestone-delivery.md); no exhaustive future task list.

## Decisions still open

Future platform support/package distribution; broader native backends and execution beyond H1-R1;
entity model/storage; scenes/worlds; rendering, input, audio, physics and asset formats.
Decide only what a concrete increment needs. No ECS/DI/backend/solver, editor, hot reload,
networking or 3D commitment is inherited or approved here.

## Refinement

[Findings](findings.md) captures evidence and debt without adding arbitrary work.
Move concrete approved needs into bounded backlog packets with dependencies. Requirement,
ADR approval, task completion and milestone acceptance are separate records.

## Next refinement only

Proposed [H1.0 tracer](specs/h1-first-playable.md) is a subset, not completion of [H1](milestones/H1.md). ME-005/006 prepare scenario/options; ME-007 prepares [ADR-0003](decisions/0003-next-validation-tracer.md). ME-008 technical packet awaits independent review and delegated ADR-0003 approval under autonomy.md. H1.0 construction is now technically verified under accepted H1-R1; no broader H1 construction task is ready without further bounded specification.

H1.0 technical slice accepted2026-10-15 by delegated authority after independent verification; subjective experience pending. Full H1 remains candidate. Next refinement: static obstacle/spatial interaction and animation/content based on delivered tracer evidence/feedback; no distant tasks generated.

## Active bounded refinement2026-10-16

Baseline H1.0 subjective feedback received under ME-012; optional consumer jump buffer delivered under ME-016/017, without full-H1 acceptance. Owner authorized integrating the verified baseline into main (81de6a9). Next ME-018/019: [PO-1](specs/platformer-obstacles.md), consumer-owned static solids/axis movement. Specification review precedes construction; no engine solver, reusable geometry contract, coyote time, animation or broader level system inferred. Later H1 content/animation remains candidate, not a parallel speculative task list.

## Animation horizon2026-10-16

After positive PO-1 owner feedback, ME-020/021 refine [AV-1](specs/avatar-animation.md): consumer-only procedural idle/walk/air/facing and deterministic playback through existing primitive canvas. No texture/resource/provider method or generic animation library. This narrow visual demonstration does not fulfill all CAP-004/006/008 or accept full H1. Broader asset/clip needs remain candidates until concrete requirements justify their contracts.

## Next independent consumer2026-10-16

Positive AV-1 owner feedback recorded, full-H1 human acceptance not inferred. [RE-1](specs/room-explorer.md) defines H2.0 two-room exploration through the same public Loop/Desktop, separate from FirstPlayable.Model. ME-022 reviewed; ME-023 additive native up/down actions precede ME-024. [H2 packet](milestones/H2.md) separates objective tests and subjective navigation. No engine world/scene/map/asset abstraction or broad Zelda scope; later content is not yet prepared.

## Bounded manual interaction2026-10-17

H2/C-002 includes interaction beyond RE-1 traversal. ME-026/027 [IM-1](specs/marker-interaction.md) probe one source-authored room-local marker using existing Space/jump edge, no Desktop binding/engine interaction/NPC/dialogue/world/persistence API. Positive RE-1 delivery and integration policy are in main8ff1f0a; marker technical/subjective states are distinct in backlog/H2 packet. H3 menus/turn behavior remains candidate, not silently prepared here.

H3 preparation2026-10-17: H2 traversal/manual-marker slices positively evaluated/integrated; ME-028/029 [TM-1](specs/turn-menu.md) refine only first menu/turn flow via a new external consumer, private composition of existing exploration. No additional H2 gameplay or premature engine text/UI framework. [H3 packet](milestones/H3.md) separates technical/subjective evidence; no full-H3 or persistence commitment.

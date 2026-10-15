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

Future platform support/package distribution; native backend and CAP-002;
entity model/storage; scenes/worlds; rendering, input, audio, physics and asset formats.
Decide only what a concrete increment needs. No ECS/DI/backend/solver, editor, hot reload,
networking or 3D commitment is inherited or approved here.

## Refinement

[Findings](findings.md) captures evidence and debt without adding arbitrary work.
Move concrete approved needs into bounded backlog packets with dependencies. Requirement,
ADR approval, task completion and milestone acceptance are separate records.

## Next refinement only

Proposed [H1.0 tracer](specs/h1-first-playable.md) is a subset, not completion of [H1](milestones/H1.md). ME-005/006 prepare scenario/options; ME-007 prepares [ADR-0003](decisions/0003-next-validation-tracer.md). ME-008 technical packet awaits independent review and delegated ADR-0003 approval under autonomy.md. No H1 construction task is ready.

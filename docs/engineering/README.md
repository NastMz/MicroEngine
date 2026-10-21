<!-- Current authority:2026-10-15 technical delegation supersedes earlier approval-only restrictions; see autonomy.md. Historical dated evidence is not rewritten. -->
# MicroEngine engineering foundation

This is the active context for the greenfield C#/.NET 2D framework.

## Start here

1. Read [manifesto](manifesto.md) and [requirements](requirements.md).
2. Read [accepted decisions](decisions/README.md), including the cross-cutting [infrastructure directive](decisions/0004-replaceable-infrastructure.md).
3. Check [roadmap](roadmap.md), [contributing](contributing.md) and [document policy](document-policy.md).
4. Consult [history](history.md) for the retired prototype.

## Current state — 2026-10-21

Latest: [H3 bounded RPG accepted](evidence/rpg-1/acceptance.md) functionally and visually by the owner. [Cross-scenario assessment and LAUNCH-1 closure](evidence/scenario-maturation/assessment.md) records actual reuse, consumer policy, limits and next candidates;283 tests and independent compatibility proof. Earlier horizon notes below are historical, not pending gates.

The prototype is retired and preserved at `legacy-final`. These bounded increments are
implemented and technically verified; [current state](current-state.md) records exact
revisions, evidence and delivery boundaries.

| Increment | What exists | Contract / evidence |
| --- | --- | --- |
| H0 | Permanent headless lifecycle; owner accepted | [Lifecycle](specs/lifecycle.md), [H0 packet](milestones/H0.md) |
| H1/C-001 | Meadow Run first real-art playable scenario accepted/closed; historical tracers retained | [Scenario acceptance](evidence/kpf-1/acceptance.md), [H1 packet](milestones/H1.md) |
| H2 / ADV-1 | Courtyard Key topdown lever/gate/key/altar/replay; engine unchanged | [Run/evidence](evidence/adv-1/README.md), [H2 packet](milestones/H2.md) |
| H3 tracers | Menu/turn flow, optional in-window labels; versioned consumer checkpoints, optional create-new/read adapter and opt-in process startup/exit integration | [H3 packet](milestones/H3.md), [NL-1](specs/optional-labels.md), [CP-1](specs/consumer-checkpoint.md), [CF-1](specs/checkpoint-files.md), [CL-1](specs/checkpoint-launch.md) |

CL-1 evidence includes169 managed regressions, the prior native harnesses and a targeted
independent parser correction with17 persistence cases. Full H1/H2/H3 acceptance is not
inferred from these slices. Owner positively evaluated NL-1 delivery1dce7b3, including its
verified checkpoint stack; main-integration evidence is in current-state/backlog.
Windows native validation only; Raylib-cs is isolated to Desktop. ECS/scenes/DI, general
UI/text/assets/storage systems and other platform support remain unselected or candidate.
NL-1 adds only a bounded optional label profile:175 managed tests/native RGB readback and
independent provider/fallback proof passed; owner feedback on this bounded presentation is
positive, not full text/UI or milestone acceptance. Native-only verification code may reference the adapter's dependency.

## Templates

- [ADR](templates/adr.md): significant alternatives and explicit approval.
- [Capability](templates/capability.md): behavior, conceptual contracts and verification.
- [Milestone](templates/milestone.md): bounded external validation scenario.

Create actual specs only for concrete scope. The owner is the approval authority unless
explicit delegation is recorded. A generated recommendation does not approve itself.

## AI-first development entry

Read [backlog](backlog.md), [execution protocol](execution.md) and [autonomy policy](autonomy.md).
The backlog table is the sole task-state source; dated notes preserve prior states, not
current blockers. Read actual approved packets and evidence before selecting work. If no
task is ready, refine only a concrete next roadmap need under delegated technical authority;
do not manufacture scope merely to stay busy. Technical acceptance and subjective product
acceptance are separate. Consult autonomy.md for publication/main-delivery authority.
[Capabilities](capabilities.md) describes general needs without genre modules.
[Lifecycle](specs/lifecycle.md) is the approved delegated H0-1 contract. [Frame execution](specs/frame-execution.md) preserves earlier proposals; [H1-R1](specs/h1-runtime.md) is the accepted narrow Loop/Desktop contract, outside H0. [Toolchain](toolchain.md) records versions and checks. Use [task template](templates/task.md),
[manual milestone delivery](milestone-delivery.md), [findings intake](findings.md) and
[planning validation](planning-validation.md). This is a protocol, not background automation.

## Find runnable examples and verification

[Contribution guide](contributing.md) contains example commands, managed checks and native
harnesses, including opt-in checkpoint startup/exit. [Toolchain](toolchain.md) owns SDK and
serial build/test instructions; do not infer installation requirements from historical logs.
Agents run reproducible technical checks themselves; manual experience is reserved for
genuinely subjective product evidence. Passing tests does not authorize main integration
or full milestone acceptance. Follow [autonomy](autonomy.md), not older dated restrictions.

IMG-1 is verified and positively evaluated/integrated. SPR-1 has now received positive owner feedback; current-main reconciliation and verification status are recorded in backlog/current-state. Historical verified feature: [optional source-region presentation SPR-1](https://github.com/NastMz/MicroEngine/blob/aa0243b577bcdee7c9e1b895325b8813fa9a3e45/docs/engineering/specs/sprite-regions.md), ME-041/042, under [ADR-0010](https://github.com/NastMz/MicroEngine/blob/aa0243b577bcdee7c9e1b895325b8813fa9a3e45/docs/engineering/decisions/0010-optional-image-regions.md). Refer to backlog/current-state for actual closure evidence; no asset pipeline or additional backend commitment.

Latest technically verified validation: [MAP-1 larger maps and viewport](specs/map-viewport.md), separate MapExplorer consumer; no engine camera/map abstraction. See backlog for ME-043/044/045 and contributing for commands. Earlier current-candidate notes describe prior horizons.

Current bounded horizon: [MAP-2 explicit solid navigation](specs/map-navigation.md) in MapExplorer only. Main includes owner-positive MAP-1; SPR-1 is still independent/unintegrated. Backlog owns ME-046/047 technical status; contributing explains subjective-only navigation handoff.

Current horizon: [TURN-2 durable turn completion](specs/turn-outcome.md)/ADR-0011, ME-048/049. MAP-2 is owner-positive/integrated; TURN-2 is a new independently verified technical candidate, not implicit fullH3 acceptance. See backlog for closure status and contributing for playability-only handoff.

Current bounded horizon: [MAP-3 switch-controlled map shortcut](specs/map-switch-gate.md), ME-050/051. TURN-2 is owner-positive/integrated; MAP-3 status/evidence is in backlog. This is consumer-owned topology, not a new engine module or map-persistence contract.

Current bounded horizon: [GOAL-1 platformer course completion and replay](specs/platformer-goal.md), ME-052/053. MAP-3 is owner-positive/integrated; GOAL-1 status/evidence is in backlog. This is optional consumer gameplay, not engine lifecycle/reset or a shared level framework.

Current delivery integrates [SPR-1](specs/sprite-regions.md) with GOAL-1, not a new gameplay horizon. Consult the backlog for ME-054 verification and actual main delivery.

Current selected playable horizon: [KPF-1 real-art platformer](specs/kenney-platformer.md), [run/evidence/subjective handoff](evidence/kpf-1/README.md), [ADR0012 classification](decisions/0012-playable-platformer-infrastructure.md). Optional TwoD utilities are not Core genre modules. Only local commits authorized for this horizon.

Selected C002 playable horizon: [Courtyard Key ADV1](specs/adventure.md), [run/reuse/evidence](evidence/adv-1/README.md). Meadow Run is closed after explicit owner feedback and public-boundary audit; new topdown consumer reuses existing engine without changes. Backlog ME060 owns technical closure; subjective adventure evaluation remains pending.

Current selected H3 playable candidate: [RPG-1 Trail Duel](specs/rpg-encounter.md), [run/evidence](evidence/rpg-1/README.md). Closed tileset assessment remains candidate input only; existing public exploration/canvases/image/animation services support game-owned encounter states without engine changes. Backlog ME064/065/066 owns actual closure status.

Asset Readiness is a permanent lightweight gate: [procedure](asset-readiness.md).
Phase2 current packet: [MR-2](specs/meadow-progression.md), [staged roadmap](evidence/meadow-2/roadmap.md).

Current2026-10-21: [Meadow Run MR-2](evidence/meadow-2/README.md) implements two-level progression
with289managed/independent native proof, feature-only pending new subjective experience.
[Asset Readiness](asset-readiness.md) now applies to future visual/sonic tasks; [phase roadmap](evidence/meadow-2/roadmap.md)
keeps enemies/audio/powers/UI as candidate horizons, not simultaneously prepared implementation.

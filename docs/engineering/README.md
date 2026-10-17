<!-- Current authority:2026-10-15 technical delegation supersedes earlier approval-only restrictions; see autonomy.md. Historical dated evidence is not rewritten. -->
# MicroEngine engineering foundation

This is the active context for the greenfield C#/.NET 2D framework.

## Start here

1. Read [manifesto](manifesto.md) and [requirements](requirements.md).
2. Read [accepted decisions](decisions/README.md), including the cross-cutting [infrastructure directive](decisions/0004-replaceable-infrastructure.md).
3. Check [roadmap](roadmap.md), [contributing](contributing.md) and [document policy](document-policy.md).
4. Consult [history](history.md) for the retired prototype.

## Current state — 2026-10-17

The prototype is retired and preserved at `legacy-final`. These bounded increments are
implemented and technically verified; [current state](current-state.md) records exact
revisions, evidence and delivery boundaries.

| Increment | What exists | Contract / evidence |
| --- | --- | --- |
| H0 | Permanent headless lifecycle; owner accepted | [Lifecycle](specs/lifecycle.md), [H0 packet](milestones/H0.md) |
| H1 tracers | Fixed-step Loop, Desktop adapter, external platformer with static solids and procedural animation | [H1 packet](milestones/H1.md) |
| H2 tracers | Separate two-room exploration and marker interaction | [H2 packet](milestones/H2.md) |
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

IMG-1 is verified and positively evaluated/integrated. Current candidate: [optional source-region presentation SPR-1](specs/sprite-regions.md), ME-041/042, under [ADR-0010](decisions/0010-optional-image-regions.md). Refer to backlog/current-state for actual closure evidence; no asset pipeline or additional backend commitment.

Latest technically verified validation: [MAP-1 larger maps and viewport](specs/map-viewport.md), separate MapExplorer consumer; no engine camera/map abstraction. See backlog for ME-043/044/045 and contributing for commands. Earlier current-candidate notes describe prior horizons.

Current bounded horizon: [MAP-2 explicit solid navigation](specs/map-navigation.md) in MapExplorer only. Main includes owner-positive MAP-1; SPR-1 is still independent/unintegrated. Backlog owns ME-046/047 technical status; contributing explains subjective-only navigation handoff.

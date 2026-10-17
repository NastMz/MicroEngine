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
| H3 tracers | Menu/turn flow; versioned consumer checkpoints, optional create-new/read adapter and opt-in process startup/exit integration | [H3 packet](milestones/H3.md), [CP-1](specs/consumer-checkpoint.md), [CF-1](specs/checkpoint-files.md), [CL-1](specs/checkpoint-launch.md) |

CL-1 evidence includes169 managed regressions, the prior native harnesses and a targeted
independent parser correction with17 persistence cases. Full H1/H2/H3 acceptance is not
inferred from these slices. Last recorded main delivery includes positively evaluated TM-1;
checkpoint work remains on task branches, as detailed in current-state/backlog.
Windows native validation only; Raylib-cs is isolated to Desktop. ECS/scenes/DI, general
UI/text/assets/storage systems and other platform support remain unselected or candidate.

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

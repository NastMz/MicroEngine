<!-- Current authority:2026-10-15 technical delegation supersedes earlier approval-only restrictions; see autonomy.md. Historical dated evidence is not rewritten. -->
# MicroEngine engineering foundation

This is the active context for the greenfield C#/.NET 2D framework.

## Start here

1. Read [manifesto](manifesto.md) and [requirements](requirements.md).
2. Read [accepted decisions](decisions/README.md), including the cross-cutting [infrastructure directive](decisions/0004-replaceable-infrastructure.md).
3. Check [roadmap](roadmap.md), [contributing](contributing.md) and [document policy](document-policy.md).
4. Consult [history](history.md) for the retired prototype.

## Current state — 2026-10-15

The owner approved the repository transition. The historical prototype is preserved at
`legacy-final`, not present as active projects. H0 hosting implementation is automatically verified under accepted ADR-0002; see [current state](current-state.md). H1.0 Loop/Desktop/FirstPlayable now technically verified under accepted ADR-0003; Raylib-cs8.1.0 isolated to Desktop. ECS/DI/scenes remain open.
ADR-0001 records product direction; ADR-0002 records bounded H0 approval; autonomy.md records current technical delegation and ADR-0003 the next bounded tracer.

## Templates

- [ADR](templates/adr.md): significant alternatives and explicit approval.
- [Capability](templates/capability.md): behavior, conceptual contracts and verification.
- [Milestone](templates/milestone.md): bounded external validation scenario.

Create actual specs only for concrete scope. The owner is the approval authority unless
explicit delegation is recorded. A generated recommendation does not approve itself.

## AI-first development entry

Read [backlog](backlog.md), [execution protocol](execution.md) and [autonomy policy](autonomy.md).
Ready packets are self-contained; ME-001/002/003 preparation is complete; ME-004 is completed with evidence in the backlog; H0 owner acceptance was recorded2026-10-15; the next H1 horizon may be specified, independently reviewed, technically approved and built under delegated authority.
[Capabilities](capabilities.md) describes general needs without genre modules.
[Lifecycle](specs/lifecycle.md) is the approved delegated H0-1 contract. [Frame execution](specs/frame-execution.md) preserves earlier proposals; [H1-R1](specs/h1-runtime.md) is the accepted narrow Loop/Desktop contract, outside H0. [Toolchain](toolchain.md) records versions and checks. Use [task template](templates/task.md),
[manual milestone delivery](milestone-delivery.md), [findings intake](findings.md) and
[planning validation](planning-validation.md). This is a protocol, not background automation.

## Validate the current increment

[H0 delivery packet](milestones/H0.md) contains tested commands, expected traces/exits and the owner acceptance record. Current capability/state is in current-state.md. No later engine task is implicitly prepared by this delivery.

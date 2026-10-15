# Requirements catalog

Status records approval separately from implementation and verification. Priority is not
a schedule. `Foundation` means a confirmed product constraint; `Candidate` means discovery only.
Lifecycle: proposed, investigating, approved, implemented, verified, deferred, rejected.
Evidence and approval rules: [document policy](document-policy.md).

## Confirmed requirements

Rows originate in explicit repository-owner instructions (2026-10-12 foundation;2026-10-15 NFR-005 directive), represented
in the [manifesto](manifesto.md) and [ADR-0001](decisions/0001-greenfield-direction.md).
They are approved intent, not claims of greenfield implementation.

| ID | Description and rationale | Priority | Status | Origin | Acceptance criteria | Dependencies |
| --- | --- | --- | --- | --- | --- | --- |
| FR-001 | Provide a general-purpose C#/.NET 2D framework; avoid repeated infrastructure | Foundation | approved | Owner product/direction instruction, 2026-10-12 | External consumers can use supported capabilities through documented public APIs | None |
| FR-002 | Permit external genre extensions; keep game rules outside mandatory engine capabilities | Foundation | approved | Owner product/direction instruction, 2026-10-12 | Scenario examples contain their own rules; extension consumption needs no private engine access | FR-001 |
| NFR-001 | Make lifecycle and ownership explicit to prevent hidden ordering and cleanup obligations | Foundation | approved | Owner product/direction instruction, 2026-10-12 | Each approved capability specifies ownership, order, errors and observable lifecycle tests | FR-001 |
| NFR-002 | Make capabilities testable and diagnosable | Foundation | approved | Owner product/direction instruction, 2026-10-12 | Implemented capabilities have observable acceptance tests and actionable failure evidence | FR-001 |
| NFR-003 | Measure reasonable performance instead of assuming it | Foundation | approved | Owner product/direction instruction, 2026-10-12 | Performance-sensitive capabilities identify workload, environment, measurements and agreed budgets before verification | FR-001 |
| NFR-004 | Support evolutionary architecture with proportionate documentation | Foundation | approved | Owner product/direction instruction, 2026-10-12 | Significant decisions retain rationale/history; changed contracts and consumer impact are documented | None |
| NFR-005 | Preserve replaceable infrastructure and cohesive opt-in capability contracts | Foundation | approved | Owner architectural directive,2026-10-15; ADR-0004 | Each evolution records dependency direction, public-only provider substitution, optional/specialized feature assumptions and future target restrictions; no vendor/universal-platform guarantee inferred from a single adapter; implementation conformance evidenced per slice | FR-001; NFR-004 |
| DOC-001 | Enable onboarding without previous chats or agent memory | Foundation | approved | Owner product/direction instruction, 2026-10-12 | A collaborator finds intent, current decisions, actual implementation state and next work from repository links | NFR-004 |
| SCOPE-001 | Treat the next engine as greenfield; preserve the prototype as reference | Foundation | approved | Owner product/direction instruction, 2026-10-12 | No prototype technology is declared mandatory merely because it exists; reuse follows ADR-0001 criteria | None |

## Discovery candidates

Origin: owner-requested validation scenarios and capability exploration; not implementation commitments.

| ID | Description and rationale | Priority | Status | Origin | Acceptance criteria to refine | Dependencies |
| --- | --- | --- | --- | --- | --- | --- |
| C-001 | Explore movement, collision and animation for a simple platformer | Candidate | proposed | Owner scenario exploration, 2026-10-12 | External scenario validates the approved general capabilities without moving jump rules into the core | FR-002 |
| C-002 | Explore maps, transitions and interaction for a top-down adventure | Candidate | proposed | Owner scenario exploration, 2026-10-12 | External scenario validates approved contracts without engine-specific combat rules | FR-002 |
| C-003 | Explore menus, persistence and turn-based interaction for an RPG | Candidate | proposed | Owner scenario exploration, 2026-10-12 | Game owns turn/combat rules; shared technical needs are specified independently | FR-002 |

No runtime, ECS, renderer, scene API, UI toolkit or backend requirement is inferred from
these candidates. Investigations must establish a need before expanding approved scope.

## Adding or updating a requirement

Use the next stable ID; never recycle IDs. Record description, rationale, priority, status,
origin (owner instruction, issue or evidence), observable acceptance criteria and dependencies.
For approval, add who/when/evidence. For implemented or verified status, add code/spec links
and verification results. Changed scope requires renewed approval; retain a short change note.

## H0 evidence slice — 2026-10-13

FR-001: independent project-reference consumer uses public API only. NFR-001/NFR-002: H0-1.1 ownership, partial initialization, reverse cleanup, origin-classified misuse and execution reports exercised by31 public cases. NFR-004/DOC-001: accepted scoped ADR/toolchain/spec/backlog and delivery packet synchronized. Links: [contract](specs/lifecycle.md), [implementation](../../engine/Hosting/Hosting.csproj), [tests](../../checks/Hosting.Contracts/LifecycleTests.cs), [H0 delivery](milestones/H0.md). These are verified H0 slices, not global fulfillment of general engine requirements. NFR-003 budgets not applicable to this correctness increment; no performance claim. Future/game capabilities remain candidates.

NFR-005 approval is a permanent architectural constraint, not verification of all existing/future providers. [ADR-0004](decisions/0004-replaceable-infrastructure.md) identifies current conformity and limitations; no current functional acceptance criterion changed.

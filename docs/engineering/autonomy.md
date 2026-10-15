# Agent autonomy policy

Status: active initial policy for authorized sessions.

This policy implements AI-first direction. ADR-0002 now delegates H0 local contracts/toolchain choices and authorizes construction after documentary review.

## Delegated technical discretion

Default: agents perform authorized work and technical verification; human intervention is
the exception. Owner instruction, 2026-10-15: "autonomía por defecto e intervención humana por excepción".
Within approved scope, implement, test, correct defects, obtain required independent review,
record reproducible evidence and continue eligible independent tasks without routine prompts.
Do not ask the owner to execute checks available to the agent. A technical limitation must
be diagnosed safely and recorded before escalation, not handed off as routine operation.

Within a ready task's approved scope, agents may choose internal algorithms, naming,
test cases, local refactoring and diagnostics that preserve accepted contracts.
They may execute recorded checks, update evidence/status, document discoveries and
continue independent ready work in the active authorized session.

They may perform public-source research within an investigation task. Network access
does not authorize publication, private-account access or remote execution.

## Human decisions

Human approval is required for product scope, platform/distribution targets, new runtime
dependencies, material public contracts, architecture tradeoffs not delegated by an ADR,
compatibility policy, destructive operations, remote/publishing operations and release.
Genre rules remain outside mandatory engine capabilities.

Escalate only an unresolved scope, architecture, risk, authorization or product-acceptance
decision outside delegated authority. State the exact decision and evidence. Product acceptance
is not a requirement to repeat passing automated checks. Neither autonomy nor successful tests
authorize the agent to accept an owner-owned milestone, weaken acceptance or reduce coverage.

Technical inability alone does not create a new strategic requirement. Exhaust bounded
safe diagnosis, record evidence and alternatives, then request input for genuinely
unresolvable blockers. Do not repeatedly ask about already approved routine execution.

## Decision authority and delivery

A proposal is not acceptance. Record approval actor/date/source in ADR/spec/task.
Readiness cannot stand in for approval. The phrase 'develop ready tasks' authorizes only
already approved tasks in that session, not pending designs or missing dependencies.
The 2026-10-12 scoped approval authorizes H0 construction and delegates minimal APIs, mechanisms and toolchain; preserve architectural limits recorded in ADR-0002.

Commits require explicit applicable authorization. Push, PR, remote transfer, merge and
release remain separate human decisions unless a specifically scoped policy grants them.
Do not infer credentials/session consent from their availability.
Do not change review switches or approval policies automatically.

## Scope and failure control

Preserve unrelated edits. Narrow allowed surfaces cannot be expanded because a shortcut
is convenient. Reassess readiness when facts invalidate the packet. A safe implementation
detail is autonomous; a changed observable contract is a proposal.

Record [findings](findings.md); completing the current task must not depend on unrelated
feature expansion. Stop only affected tasks and continue independent eligible ones.
Product acceptance is human-owned; do not mark it passed automatically. Document technically
completed milestones as awaiting acceptance and continue ready work that does not depend on
that acceptance. Do not invent new scope merely to remain busy.

## Policy verification — 2026-10-15

Independent autonomy_policy_verify readback passed after one documentary correction: explicit fourth H0 invalid-scenario reproduction command and automated-evidence row. All original behavior criteria retained. Six changed-document link checks found no missing targets; git diff --check passed. All10 delivered source/config hashes matched; no engine/test changes or coverage reduction, so no redundant runtime rerun. H0 remains awaiting owner product acceptance; backlog ME-001..ME-004 completed, no eligible independent task. CodeGraph initialization added derived index state only, not product scope or authority.

Subsequent owner decision2026-10-15: H0 was explicitly accepted against existing evidence/criteria. Earlier pending-acceptance references in dated verification notes are historical, not an active H0 blocker. This does not accept any future milestone.

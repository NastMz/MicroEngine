# Technical autonomy policy

Status: active, owner delegation2026-10-15. Supersedes narrower approval rules below; product manifesto unchanged.

## Default authority

**Technical autonomy by default; human intervention by exception.** The owner authorizes agents to investigate/select justified solutions; create/review/approve/supersede technical ADRs; define/evolve public contracts; choose dependencies/tools/backends; create projects/components/tests/examples; refactor/replace justified implementations; plan/prepare/execute/close tasks; refine next horizons from the roadmap; accept objectively verifiable technical milestones; make coherent local commits; continue ready work in the active session.

This is technical authority within the general-purpose C#/.NET 2D vision and Mario/Zelda/Pokemon validation roadmap, not permission for arbitrary capabilities or mandatory genre modules. An architectural consequence alone does not require owner approval. Recommendations become decisions only through recorded approval under this authority, not through code existence.

## Mandatory controls

Respect manifesto/requirements. Significant architectural changes require an ADR, alternatives/consumer impact and independent review before approval. New capabilities require specifications and observable acceptance criteria before construction. Run applicable automated/regression checks, fix defects without weakening criteria/coverage, record actual evidence and synchronize documentation. Close tasks only with sufficient proof and traceable local commits. Preserve unrelated work and superseded decisions. Review switches remain user-owned.

## Human exceptions

Ask only for substantial product vision/strategic scope changes; destructive/external/irreversible operations requiring authorization; indispensable subjective playability/ergonomics/presentation evaluation; blockers not reasonably resolvable by bounded research, alternatives or independent review; or no useful roadmap work when continuing would invent unrelated requirements. Stop only affected work and continue safe independent work. Available credentials do not authorize remote access. Remote operations require explicit destination/operation/authentication authorization. The owner has authorized non-force pushes of this project work to origin (https://github.com/NastMz/MicroEngine.git) using the configured Git authentication; standard task-branch pushes no longer require a routine prompt. Owner2026-10-17 additionally authorizes normal integration/push to main after positive feedback on the evaluated technically verified increment, as specified below. This does not authorize PR creation, release, force-push, deletion of remote branches/tags or publishing unrelated work.

## Two acceptance gates

Technical acceptance is delegated: criteria-by-criteria objective evidence, environment, failed/skipped checks and independent review where applicable. Subjective product acceptance remains human-owned and cannot be fabricated. A technically verified increment may await subjective acceptance while independent tasks continue. Do not ask the owner to repeat checks the agent can automate. Do not treat a future subjective gate as an automatic technical blocker.

## Operation

Use [execution](execution.md) and [backlog](backlog.md); refine only the next useful horizon. Document discoveries/debt through [findings](findings.md), not hidden scope growth. Local commits are authorized; narrow surfaces and rollback boundaries still apply. Changed contracts require new rationale/review/regression proof, not silent specification edits to fit failing code. A session protocol is not a background scheduler.

## Branch and push convention — owner2026-10-15

Use work-type branches such as feat/<topic>, fix/<topic> and docs/<topic>, not codex/*. No unrequested develop/release branches or merge topology is imposed by this naming convention. The existing branch was renamed locally to feat/h1-runtime with commit history unchanged; historical records retain the original branch names as evidence. Normal development push targets the corresponding task branch; main delivery follows the positive-feedback authority below, never agent-inferred subjective acceptance. Audit exact staged/outgoing scope, preserve unrelated edits, do not print credentials, and record actual push results. Remote rejection/conflict is diagnosed without force/history rewriting.

## Positive-feedback integration — owner2026-10-17

Owner instruction: "De ahora en adelante integra a main cada vez que te de una valoracion positiva, sigue con el sigueinte trabajo e integra lo existente en main". Clear positive owner feedback on the delivered increment grants continuing authorization to integrate that evaluated, technically verified increment into main and normally push main to origin https://github.com/NastMz/MicroEngine.git using configured Git authentication. No routine integration approval prompt is required. The current existing delivery9db127f was explicitly included and integrated.

1. Associate feedback with the delivered revision/scope from the handoff; record the owner's words, source revision and limitations. If the evaluated increment is ambiguous, clarify rather than guess. Positive feedback is not full-milestone acceptance unless explicitly stated.
2. Require the increment's approved contract, applicable successful checks and required independent review. Feedback does not waive failures/coverage or bless unrelated/incomplete/unevaluated features. Technical acceptance alone is not positive owner feedback.
3. Inspect local/staged state and fetch authorized origin. Integrate only reviewed commits, preserving unrelated work; prefer fast-forward. If main diverged, reconcile safely with review/retesting of actual merge changes, never reset/force/rewrite history or silently overwrite conflicts. Stop only unsafe/ambiguous work.
4. Verify main/source identity and remote synchronization; record resulting commits/evidence and actual push result. Already integrated revisions are a no-op, not another acceptance or permission to include later features. Feedback-record documentation may accompany the evaluated increment; new behavioral scope may not.
5. Continue the next eligible task on feat/fix/docs branches under the roadmap. No PR/release, force-push, deletion, scheduler or unrelated publication follows from this authority. Historical "no main integration" delivery records retain their dated meaning.

## Historical policy (superseded2026-10-15)

<details><summary>Earlier bounded H0 authority and evidence, preserved verbatim</summary>

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

</details>

# Agent execution protocol

Operational scope: the owner requested this planning system on 2026-10-12.
The2026-10-15 owner delegation in autonomy.md authorizes reviewed technical decisions, construction, local commits and objective acceptance within the roadmap. ADR-0002 preserves H0 scope. This is a repository
protocol executed by an agent, not a scheduler, daemon or guarantee of background work.

## Fresh-session entry

Read AGENTS.md, the engineering index, manifesto, accepted ADRs, requirements,
[backlog](backlog.md), [autonomy](autonomy.md), affected specs and [findings](findings.md).
Inspect Git before editing. Record unrelated local changes and leave them untouched.
Do not require chat history or external memory.

## Work states

Canonical tokens are English; Spanish equivalents are shown for conversation.

| State | Meaning | Entry / exit evidence |
| --- | --- | --- |
| pending (pendiente) | Not executable yet | Missing preparation fields or incomplete dependencies |
| ready (preparada) | Complete execution packet, approved authority and satisfied dependencies | Readiness checklist below |
| in-progress (en progreso) | Claimed by a named session/agent | Owner, baseline revision, start time and allowed surfaces |
| blocked (bloqueada) | A necessary condition cannot be met | Concrete blocker, attempts, impact and unblock condition |
| in-review (en revisión) | Outcome produced; closure checks/review underway | Implementation/result and check evidence |
| completed (completada) | Acceptance, review and relevant docs satisfied | Evidence, reviewer and handoff; commit if authorized |

Requirement and ADR states remain separate: a completed investigation does not accept
its recommendation. A completed task does not imply a milestone passed manual testing.

Normal transitions: pending -> ready -> in-progress -> in-review -> completed.
Any active task may become blocked. Unblocking returns to pending for readiness recheck,
not automatically ready. Review corrections return to in-progress within the same scope.
Scope changes reopen preparation and the applicable delegated technical or reserved human approval; do not silently redefine acceptance.

## Readiness gate

All conditions must hold:
- Kind and explicit execution authority recorded (investigation, docs or implementation).
- Objective, exclusions, linked requirements/spec revision and accepted decisions recorded.
- Every dependency completed with evidence; genuine strategic/product human dependencies explicitly resolved, technical dependencies resolved through delegated decisions.
- Narrow edit surfaces and deliverable locations identified; no unresolved contract choice.
- Observable acceptance, applicable checks/commands or justified N/A recorded.
- Risk and review route, failure/rollback boundaries and evidence packet defined.
- No unresolved decision needed: technical choices are resolved autonomously with review; strategic changes outside the manifesto require owner input.
- Fresh-session context complete; no promises to obtain context from a previous chat.

Agents may mark ready when these objective conditions hold; they cannot fabricate authority.
Implementation authorization and accepted contracts are separate mandatory fields.

## Select and claim

1. Reconcile stale task claims and evidence before selecting.
2. Validate dependency graph for cycles/missing IDs and check readiness.
3. Choose highest priority eligible ready task; break ties by ascending task ID.
4. Skip blocked/dependency-unsatisfied tasks; do not weaken their gates.
5. Claim with agent/session label, baseline Git revision and timestamp before editing.
6. One writer owns a task. The default is sequential execution in one checkout.
   Concurrent sessions must use coordinated claims/isolated worktrees; Markdown is not
   a distributed lock. If ownership is uncertain, do not edit the disputed surface.

An interrupted in-progress claim is not permission to overwrite work. Inspect diff and
evidence, identify the previous owner and confirm inactivity before resuming. If that
cannot be established, block only that task and continue independent eligible work.

## Execute and validate

Read the task packet and approved spec, then work only inside its allowed surfaces.
For runnable behavioral changes observe a failing public-behavior test, implement, then
refactor green. If no runnable test exists, record why and the alternative check.
Run task-focused and applicable integration checks, normalize before final verification,
and compare public behavior with acceptance, not only internal tests.

Use check-only commands after freezing a reviewed candidate. Update affected docs with
implementation and tests. Do not change technologies/contracts merely to evade tests; justified changes require rationale, independent review and renewed affected proof.
Missing runners, failures and unavailable review are recorded, never labeled passed.

Execute every pertinent check that available tools can automate or reproduce. Implement fixes
within scope and preserve required review, acceptance criteria and coverage. Reproduction commands
are evidence for collaborators, not an instruction for the owner to do the agent's verification.

## Permanent architecture check

Apply [ADR-0004](decisions/0004-replaceable-infrastructure.md)/NFR-005 on each evolution: dependency direction/native type leakage; public provider substitution without foundational edits/friend names; cohesive opt-in contracts and optional/specialized support; adapter-specific scheduling/thread/platform restrictions; concrete use case versus speculative abstraction. Record applicable conclusions, limitations and consumer impact in the affected ADR/spec/task, not a duplicate universal checklist artifact. Technical scope/acceptance is unchanged; no extra backends or export work implied.

## Review and close

Move to in-review with exact change/result and evidence. Low-risk documentary work needs
structural readback; ordinary code needs a distinct review pass. High-risk security,
persistence, concurrency, shared contracts or delivery changes require independent
verification. Existing native review policy remains applicable and never grants execution
or publication authority. If required review is unavailable, block closure.

One scoped correction batch may address reported blockers, followed by targeted recheck.
New findings are recorded separately, not an unbounded review loop. Unresolved blockers
keep the task blocked; unaffected work can continue.

Before completed, record criteria-by-criteria results, commands/exit codes, environment,
review disposition, documentation updates and change locator. If commits are authorized,
use Conventional Commits and record SHA; otherwise record baseline plus changed paths
and leave the work explicitly uncommitted. Never publish implicitly.

## Continue and stop

After closing a task, recompute eligible work and continue during the authorized active
session without asking permission for routine task steps. Under the active three-game mission,
an empty ready queue triggers proportionate refinement of the next concrete mission need; it is
not itself a stopping condition. Stop on user request, exhausted session/resources, unsafe
ownership, a genuine strategic ambiguity, or absence of any safe mission-aligned refinement.
At a milestone/manual gate, prepare its packet and continue unrelated tasks if they remain eligible.

Technical verification/acceptance (delegated) and subjective product acceptance (human) are separate gates. An owner-acceptance dependency
blocks only tasks that actually need that decision; it does not freeze the whole backlog.
Check that a claimed dependency is real. Do not request manual repetition of automated checks
or claim subjective product acceptance. Technical acceptance is delegated after sufficient objective evidence. If no independent task is ready, refine from the approved mission and observed evidence without creating speculative capabilities or asking the owner to perform operational checks.

Do not bypass a blocked dependency, start speculative tasks or schedule later runs.
When human input is necessary, report one concrete decision with evidence and ask at most
one question at a time. Once asked, stop and wait. Batch informational blockers in the
handoff, not multiple simultaneous questions.

## Evidence packet

Task ID; executor/reviewer; baseline and change locator; accepted spec/ADR revisions;
each acceptance result; exact commands and exit codes; environment; skipped/failed checks;
doc changes; findings IDs; remaining manual checks; next eligible IDs. No secrets.

## Delivery after positive owner feedback (2026-10-17)

Apply autonomy.md positive-feedback integration: record evaluated revision and exact feedback, confirm technical checks/review complete, preserve unrelated work, fetch origin and integrate/push only the evaluated scope to main (prefer fast-forward). Verify/source-record main and remote synchronization. Reconcile divergent history safely with applicable review/retests, no force/reset or inferred approval of later work. If already integrated, record no-op. This is explicit standing owner authorization, not agent acceptance or a mandatory PR; full milestone product acceptance remains separate. Continue the next eligible scoped task after delivery.

## Asset Readiness
Before content-dependent implementation, apply the [lightweight content gate](asset-readiness.md)
in the task/spec. Existing/missing/selected resources, licenses/provenance/formats, interpretation
and technical limits must be explicit. Validate changed visual/sonic composition in context,
not merely successful loading. Reuse prior evidence; justified N/A for no external content.
Public free compatible assets do not require routine owner approval. No speculative asset
inventory or audio/particle subsystem follows from this gate.

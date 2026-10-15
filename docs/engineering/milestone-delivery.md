# Milestone delivery and human validation

Milestones are consumer experiences, not merely completed task lists.
See [roadmap](roadmap.md) for candidate scenarios.

## Delivery packet

Provide milestone ID/scope and approved requirements, exact revision/artifact, environment,
install/run steps tested by the agent, controls, expected observations and known limits.
Include automated test results, public-consumer integration evidence, performance workload
and measurements where applicable, and a short manual checklist for significant behavior.
Do not deliver an untested run command or require the owner to diagnose basic startup.

The agent owns all verification available through tools. Classify each delivery item as
automatically verified, genuinely human-only evidence, or owner product acceptance. Keep
reproduction commands optional when the agent has already verified them. A manual checklist
must explain what additional evidence a person can provide; do not duplicate automated cases
as mandatory human work. Preserve the behavior criteria and coverage regardless of evaluator.

## Human review

State 'awaiting manual validation' separately from automated task completion.
For technically completed headless milestones, clarify this as 'awaiting owner product acceptance',
not pending repetition of technical tests. A human-only check is justified by evidence that tools
cannot legitimately establish, not simply by the milestone label.
The owner records date/revision, passed cases, failures and decision. Manual failure opens
a linked defect; it does not erase historical evidence or authorize unrelated features.
Changes invalidate affected manual cases, not automatically every previous milestone.
Close milestone only after required automated and manual criteria pass.

## Continue without unnecessary coordination

While manual feedback is pending, continue independent ready tasks whose dependencies
do not require that milestone's acceptance. Never mark a gate passed on the owner's behalf.
H0 now has a runnable [manual delivery packet](milestones/H0.md); automated task completion is not manual milestone or game acceptance.

Owner policy clarification, 2026-10-15: autonomy by default, intervention by exception.
H0's four console scenarios are already automated; their repetition provides no new functional
coverage on the same environment. The remaining owner decision is whether the delivered scope
and limitations satisfy the product milestone. Do not accept it on the owner's behalf.

Subsequent owner decision2026-10-15: H0 was explicitly accepted against existing evidence/criteria. Earlier pending-acceptance references in dated verification notes are historical, not an active H0 blocker. This does not accept any future milestone.

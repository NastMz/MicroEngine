# ME-NNN — Outcome-oriented task title

Use a concrete execution packet, not a vague feature name. Store canonical state in backlog;
link a separate packet only when its detail warrants it.

- Kind: investigation | specification | implementation | validation | documentation
- Priority/state: P0/P1/...; pending | ready | in-progress | blocked | in-review | completed
- Authority: person/date/source; scope and implementation authorization separately
- Requirements/capability/spec revision/accepted ADRs: links
- Dependencies: task IDs plus human/external conditions
- Claim: owner/session label, timestamp, baseline revision; none until claimed

## Objective and exclusions

Describe one observable result. List what must not change and what remains game-owned.

## Allowed edit surfaces and deliverables

Exact repository-relative files/narrow globs, including authorized new paths. Avoid a
whole-repository wildcard. Name output locations and public-contract constraints.

## Acceptance and tests

Observable criteria, public-interface examples, exact focused/integration commands and
environment. If unavailable, justified N/A and alternative evidence. No invented runner.
List documentation changes and required manual checks.

## Risk, review and recovery

Risk and proportional review route, known baseline failures, safe rollback boundary.
Required independent verification cannot be replaced by author confidence.

## Evidence and state log

Dated transitions, owner/reviewer, baseline/change locator, criteria results, command/exit
codes, environment, docs updated, findings IDs, remaining blockers and next work.
Readiness requires the execution protocol gate; blank fields prohibit ready.

## Authority and acceptance gate

Apply autonomy.md: technical decisions and objective acceptance are delegated; significant ADRs need independent review. Record technical approval actor/source and review separately from subjective product acceptance. Use the complete human-exception list in [autonomy](../autonomy.md); do not create additional technical approval gates. Local commits/normal authorized task-branch pushes allowed. Positive owner feedback authorizes evaluated verified increment integration/push to main under autonomy.md; record feedback/revision, technical evidence and integration result. No PR/release/force or unrelated work implied.

## Asset Readiness (only when relevant)
Existing resources and prior provenance; actual gaps; selected file roles/source/license/format;
interpretation/backend limits; contextual composition checks and evidence. Record ready/gap/N/A
with reason. See [Asset Readiness](../asset-readiness.md); no extra ceremony for non-content work.

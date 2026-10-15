<!-- Current authority:2026-10-15 technical delegation supersedes earlier approval-only restrictions; see autonomy.md. Historical dated evidence is not rewritten. -->
# Planning-system validation

This record concerns documentary workflow, not runtime correctness.
Setup date: 2026-10-12. Validation scope: links, IDs, dependency graph, task packet
readiness and tabletop state transitions. No backlog task is executed by these simulations.

## Tabletop cases

| Input condition | Expected behavior |
| --- | --- |
| ME-001/ME-002 ready; equal P0 | Select ME-001 by ID |
| ME-001 blocked | ME-002 eligible; ME-003/ME-004 not eligible |
| Preparation tasks completed, no owner approvals | ME-004 stays blocked |
| Missing tests/spec/allowed surfaces in a task | Do not promote ready |
| Interrupted in-progress task with unknown owner activity | Do not overwrite; reconcile or block; consider independent work |
| Required check fails or review unavailable | Do not close completed |
| Out-of-scope feature discovered | Record intake; no arbitrary scope expansion |
| Manual milestone feedback pending | Gate stays pending; independent eligible tasks may continue |
| Current setup request only | No engine task execution or auto-publication |
| No eligible task | Summarize blockers/next decision; stop without speculative work |

The cases define expected protocol results, not a deployed automatic scheduler.
Record observed readback/consistency results below before claiming setup validated.

## Evidence

Validation evidence is recorded after checks; automated engine tests are N/A.

Observed setup checks (2026-10-12): all 23 active onboarding/engineering Markdown
files read for relative-link targets; no missing targets. Four unique tasks, no missing
dependencies or cycles. Six bounded model assertions passed: initial selection, independent
fallback, no ready engine task, human gate retention, no-work stop and catalog identity.
These assertions test the documented planning model, not a production scheduler.
Git diff --check passed. Runtime/build tests N/A; independent readback recorded separately.

Independent verification: initial readback found ambiguous task edit surfaces and workflow
activation wording. Both corrected; scoped recheck passed. Exact backlog/intake paths
are now named, operational rules are active for authorized planning, and technical
approvals remain pending. No engine task was executed; no commit or publication performed.
The pre-existing user change to .gitignore was left untouched.

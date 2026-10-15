# Discoveries, debt and new needs

This is a lightweight intake ledger, not a second backlog. No item authorizes implementation.
Record concrete discoveries during work; link their eventual task or decision rather than
copying it. Keep old IDs when resolved.

## Record format and triage

ID (F-NNN), date/source task, kind (defect/debt/discovery/need), evidence, impact,
affected criteria, proposed next action, status (recorded/triaged/linked/resolved/deferred)
and task/ADR links. Never record secrets or unsupported root causes.

An in-scope acceptance defect may be corrected in the current task with evidence.
An out-of-scope need remains intake until refinement and approval. Prioritize by blocked
accepted behavior and consequence, not opportunistic enthusiasm. Significant product or
contract changes go to the owner. Preserve technical debt intentionally deferred with reason.
Review relevant entries at selection and milestone refinement; no periodic meeting required.

## Known planning constraints

| ID | Source | Kind/evidence | Impact and next action | State |
| --- | --- | --- | --- | --- |
| F-001 | Planning setup, 2026-10-12 | Platform/distribution targets not approved | ME-001 investigates; ME-003 prepares owner decision | linked |
| F-002 | Planning setup, 2026-10-12 | No greenfield projects or runnable engine test commands | ME-004 remains blocked until explicit contracts/authority and runner packet exist | linked |

| F-003 | ME-002, 2026-10-12 | discovery: cleanup attempt versus successful native release must be distinguished | L4 reports failed releases instead of claiming they succeeded; approval pending | linked |
| F-004 | ME-002, 2026-10-12 | discovery: retained whole-tick backlog would make naive interpolation exceed one | F4 separates fractional remainder; overload/pause/edge policies remain ME-003 decisions | linked |

## F-005 — H0 runner environment differs by tool

Origin: ME-004. Node child dotnet commands fail NuGet path resolution; shell commands restore and run actual tests despite FileSystem warning. Public sources only. Escalation failed unresolved sandbox readonly mount; no policy changed. Use observed shell test summaries, never test discovery alone as PASS. Workload inventory failure does not imply managed build failure. Status: recorded environment limitation, no scope expansion.

## F-006 — Argument validation needs origin classification

Origin: ME-004 independent targeted authority review, 2026-10-13. Earlier H0-1 narrowed misuse to own invalid-operation errors; own ArgumentNull/Argument validation escaping callbacks incorrectly became execution failures. Corrected H0-1.1 with internal origin marker; ordinary consumer exceptions remain execution failures. RED4/31, GREEN31/31; targeted independent15/15 pass. Status: resolved within scope; no public surface/dependency added.

## F-007 — Serial MSBuild and sandboxed testhost connection

Origin: ME-004, 2026-10-13. Independent h0_build_diagnosis reproduced silent graph failure with >1MSBuild node around build-only executable consumer reference; serial graph builds. Use -m:1 in documented checks. Sandboxed VSTest aborted90s connecting to testhost; identical authorized outside-sandbox command passed31 tests. Status: environment limitation recorded, not a need for engine concurrency or global sandbox changes. Revisit parallel build only when actual workload requires it; no new speculative task.
